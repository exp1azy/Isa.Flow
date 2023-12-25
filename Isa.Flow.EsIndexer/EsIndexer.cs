using Elasticsearch.Net;
using Isa.Flow.EsIndexer.Dto;
using Isa.Flow.EsIndexer.Resources;
using Isa.Flow.EsIndexer.Services;
using Isa.Flow.Interact;
using Isa.Flow.Interact.EsIndexer;
using Isa.Flow.Interact.Extractor.Models;
using Microsoft.Extensions.Configuration;
using Nest;
using RabbitMQ.Client;
using Serilog;
using System.Configuration;
using Error = Isa.Flow.EsIndexer.Resources.Error;

namespace Isa.Flow.EsIndexer
{
    /// <summary>
    /// Класс индексации статей.
    /// </summary>
    internal class EsIndexer : BaseActor
    {
        private readonly StartParams _startParams;

        private readonly IConfiguration _config;

        private QueueHandler<ArticleModel>? _indexQueueHandler;

        private QueueHandler<DeletedArticleModel>? _deletedQueueHandler;

        private ElasticClient? _elasticClient;

        private DateTime _startTime;

        private DateTime _endTime;

        private readonly List<ArticleModel> _articles = new();

        private readonly object _lockObject = new();

        private CancellationTokenSource? _delayCancellation;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="startParams">Параметры запуска.</param>
        /// <param name="config">Конфигурация.</param>
        public EsIndexer(StartParams startParams, IConfiguration config)
            : base(new ConnectionFactory() { Uri = new Uri(config.GetSection("RabbitMq")["Uri"]!) }, config["ActorId"])
        {
            _config = config;
            _startParams = startParams;

            AddRpcHandler<StartEsIndexRequest, EsIndexResponse>(req =>
            {
                StartFunctions(req);
                return new EsIndexResponse { DateTime = DateTime.UtcNow };
            });

            AddRpcHandler<StopEsIndexRequest, EsIndexResponse>(req =>
            {
                _indexQueueHandler?.Dispose();
                _indexQueueHandler = null;

                _deletedQueueHandler?.Dispose();
                _deletedQueueHandler = null;

                Indexing();

                _endTime = DateTime.UtcNow;

                Log.Logger.Information(Message.FuncStopped);

                return new EsIndexResponse { DateTime = DateTime.UtcNow };
            });

            AddRpcHandler<IndexerCurrentStateRequest, IndexerCurrentStateResponse>(req =>
            {
                var isStarted = _indexQueueHandler != null || _deletedQueueHandler != null;

                return new IndexerCurrentStateResponse
                {
                    TimeSpan = (isStarted ? DateTime.UtcNow : _endTime) - _startTime,
                    IsStarted = isStarted
                };
            });
        }

        /// <summary>
        /// Метод запуска функции индексатора.
        /// </summary>
        /// <param name="req">Запрос на запуск.</param>
        private void StartFunctions(StartEsIndexRequest req)
        {
            _startParams.Set(req);
            StartFunctions();
            _startParams.ToFile();
        }

        /// <summary>
        /// Метод запуска функций.
        /// </summary>
        public void StartFunctions()
        {
            if (_indexQueueHandler != null || _deletedQueueHandler != null)
                return;

            _startParams.Validate();

            GetElastic();            

            StartIndex();
            StartDelete();

            _startTime = DateTime.UtcNow;

            Log.Logger.Information(Message.FuncStarted);
        }

        private void StartIndex()
        {
            _indexQueueHandler = AddQueueHandler<ArticleModel>(_startParams.ArticlesQueue, article =>
            {
                lock(_lockObject)
                {
                    _articles.Add(article);
                    if (_articles.Count >= 200)
                    {
                        Indexing();
                    }
                    else
                    {
                        if (_delayCancellation != null)
                        {
                            _delayCancellation.Cancel();
                            _delayCancellation.Dispose();
                            _delayCancellation = null;
                        }
                        _delayCancellation = new CancellationTokenSource();
                        _ = Task.Delay(TimeSpan.FromMinutes(1), _delayCancellation.Token).ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                                return;

                            lock (_lockObject)
                            {
                                Indexing();
                            }
                        });

                    }            
                }               
            });                    
        }

        private void Indexing()
        {
            try
            {
                if (_articles.Count == 0)
                {
                    return;
                }

                var esService = new ElasticService(_config, _elasticClient);

                // Читаем из ES существующие документы.
                var existingDocsTask = esService.GetByIdAsync(_articles.Select(a => (long)a.Id).ToList());
                existingDocsTask.Wait();
                var existingDocs = existingDocsTask.Result;

                // Определяем, какие документы расположены не в своих индексах
                existingDocs.ForEach(d =>
                {
                    var article = _articles.FirstOrDefault(a => a.Id == d.Document_id);
                    if (article != null)
                    {
                        d.Pubdate = article.PubDate;
                        esService.SetNeedsToMove(d);
                    }
                });

                // Удаляем из ES документы, которые расположены не в своих индексах.
                esService.DeleteAsync(existingDocs.Where(d => d.NeedsToMove)).Wait();

                // Индексируем документы (или обновляем, если они уже существуют).
                esService.IndexAsync(_articles.Select(a => (Document)a), default).Wait();

                Log.Logger.Information(string.Format(Message.Indexed, _articles.Count, _articles.MinBy(a => a.Id)?.Id ?? 0, _articles.MaxBy(a => a.Id)?.Id ?? 0));

                _articles.Clear();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, Error.IndexingError);
            }
        }

        private void StartDelete()
        {        
            var esService = new ElasticService(_config, _elasticClient);

            _deletedQueueHandler = AddQueueHandler<DeletedArticleModel>(_startParams.DeleteQueue, deleteArticle =>
            {
                if (deleteArticle == null || deleteArticle.ArticleId == null)
                {
                    return;
                }

                var ids = deleteArticle.ArticleId.Select(i => long.TryParse(i.ToString(), out var id) ? id : -1).Where(id => id > 0).ToList();

                var itemsTask = esService.GetByIdAsync(ids);
                itemsTask.Wait();
                var items = itemsTask?.Result ?? new List<Document>();
                esService.DeleteAsync(items).Wait();

                if (items.Count > 0)
                {
                    Log.Logger.Information(string.Format(Message.Deleted, items.Count, items.MinBy(i => i.Document_id)?.Document_id ?? 0, items.MaxBy(i => i.Document_id)?.Document_id ?? 0));
                }
            });
        }

        private void GetElastic()
        {
            var elasticConfig = _config.GetSection("ElasticSearch");

            var esHosts = elasticConfig.GetSection("Hosts").AsEnumerable()
                .Where(h => !string.IsNullOrWhiteSpace(h.Value))
                .Select(h => new Uri(h.Value));

            if (!esHosts.Any())
            {
                throw new ConfigurationErrorsException(Error.ESIndexUrlsNotSpecifiedError);
            }
                
            var connectionSettings = new ConnectionSettings(new SniffingConnectionPool(esHosts));
            connectionSettings.DisableDirectStreaming();
            if (int.TryParse(elasticConfig["Timeout"], out var timeout))
            {
                connectionSettings.RequestTimeout(TimeSpan.FromSeconds(timeout));
                connectionSettings.MaxRetryTimeout(TimeSpan.FromSeconds(timeout));
            }

            var login = elasticConfig["Login"];
            var password = elasticConfig["Password"];
            if (!string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(password))
            {
                connectionSettings.BasicAuthentication(login, password);
            }

            connectionSettings.ServerCertificateValidationCallback((o, certificate, chain, errors) => true)
                .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

            _elasticClient = new ElasticClient(connectionSettings);
        }
    }
}