using Isa.Flow.Interact;
using Isa.Flow.Interact.EsIndexer;
using Isa.Flow.Interact.Extractor.Rpc;
using Isa.Flow.Interact.TelegramCollector;
using Isa.Flow.Interact.VkCollector;
using Isa.Flow.Manager.Models;
using Microsoft.Extensions.Caching.Memory;
using RabbitMQ.Client;

namespace Isa.Flow.Manager
{
    /// <summary>
    /// Класс, представляющий методы для взаимодействия с Rpc.
    /// </summary>
    public class ManagerActor : BaseActor
    {
        private readonly ConnectionFactory _factory;
        private readonly IConfiguration _config;
        private readonly IConfigurationSection _actorToInteract;
        private readonly IConfigurationSection _timeout;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="connectionFactory"></param>
        /// <param name="config"></param>
        /// <param name="actorId"></param>
        public ManagerActor(ConnectionFactory connectionFactory, IConfiguration config, IMemoryCache memoryCache, string? actorId = null) : base(connectionFactory, actorId)
        {
            _factory = connectionFactory;
            _config = config;
            _memoryCache = memoryCache;
            _actorToInteract = _config.GetSection("ActorToInteract");
            _timeout = _config.GetSection("Timeouts");

            SetActorsStateToCache();
        }

        private void SetActorsStateToCache()
        {
            _ = Task.Run(async () =>
            {
                var queues = _config.GetSection("QueueNames");
                var count = GetQueuesCountAsync();

                bool? tgCollectorStarted = null;
                var t1 = IsTgCollectorStartedCallAsync().ContinueWith(t => tgCollectorStarted = t.IsCompletedSuccessfully ? t.Result : null);

                bool? indexerStarted = null;
                var t2 = IsEsIndexerStartedCallAsync().ContinueWith(t => indexerStarted = t.IsCompletedSuccessfully ? t.Result : null);

                FuncStateResponse? extractorStarted = null;
                var t3 = IsExtractorFuncsStartedCallAsync().ContinueWith(t => extractorStarted = t.IsCompletedSuccessfully ? t.Result : null);

                bool? vkCollectorStarted = null;
                var t4 = IsVkCollectorStartedCallAsync().ContinueWith(t => vkCollectorStarted = t.IsCompletedSuccessfully ? t.Result : null);

                await Task.WhenAll(t1, t2, t3, t4);

                var state = new StateViewModel
                {
                    NewAndUpdatedQueueName = queues["NewAndUpdatedQueueName"]!,
                    DeletedQueueName = queues["DeletedQueueName"]!,
                    NewCount = count.NewAndUpdatedQueueCount,
                    DeletedCount = count.DeletedQueueCount,
                    ExtractorStarted = extractorStarted,
                    TgCollectorStarted = tgCollectorStarted,
                    VkCollectorStarted = vkCollectorStarted,
                    IndexerStarted = indexerStarted
                };

                _memoryCache.Set("state", state, TimeSpan.FromSeconds(30));

                await Task.Delay(int.Parse(_timeout["DelayTimeout"]!));
            }).ContinueWith(_ => SetActorsStateToCache());
            
        }

        /// <summary>
        /// Метод запуска SQLExtractor.
        /// </summary>
        /// <param name="startParameters">Параметры запуска.</param>
        /// <returns></returns>
        public async Task CallExtractorAsync(StartViewModel startParameters)
        {
            var request = new StartSqlExtractionRequest
            {
                BatchSize = 50,
                Func = (SqlExtractionFunc)startParameters.Func
            };

            if (startParameters.ArticleId != null)
            {
                request.LastArticleId = (int)startParameters.ArticleId;
            }

            await _rpcClient.CallAsync<StartSqlExtractionRequest, SqlExtractionResponse>(
                requestedActorId: _actorToInteract["SQLExtractor"]!, 
                request: request, 
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );
            SetExtractorStateInCache(request.Func, true);
        }

        /// <summary>
        /// Метод установки в кеше состояния функци извлечения новых статей.
        /// </summary>
        /// <param name="newState">Состояние функции.</param>
        private void SetExtractorStateInCache(SqlExtractionFunc func, bool newState)
        {
            var state = _memoryCache.Get<StateViewModel>("state") ?? new StateViewModel();
            state.ExtractorStarted ??= new FuncStateResponse();
            switch (func)
            {
                case SqlExtractionFunc.New:
                    state.ExtractorStarted.NewState = newState;
                    break;
                case SqlExtractionFunc.Updated:
                    state.ExtractorStarted.ModifiedState = newState;
                    break;
                case SqlExtractionFunc.Deleted:
                    state.ExtractorStarted.DeletedState = newState;
                    break;
                case SqlExtractionFunc.Reindex:
                    state.ExtractorStarted.ReindexState = newState;
                    break;
                case SqlExtractionFunc.Clean:
                    state.ExtractorStarted.CleanState = newState;
                    break;
            }            
            _memoryCache.Set("state", state, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Метод, запрашивающий статус работы функций SQLExtractor.
        /// </summary>
        /// <returns>Статус работы функций.</returns>
        public async Task<FuncStateResponse> IsExtractorFuncsStartedCallAsync()
        {
            FuncStateResponse? result;

            try
            {
                result = await _rpcClient.CallAsync<FuncStatusRequest, FuncStateResponse>(
                    requestedActorId: _actorToInteract["SQLExtractor"]!,
                    request: new FuncStatusRequest(),
                    timeout: int.Parse(_timeout["CallTimeout"]!)
                );
            }
            catch (TimeoutException)
            {
                throw;
            }

            return result;
        }

        /// <summary>
        /// Метод остановки указанной функции SQLExtractor.
        /// </summary>
        /// <param name="funcToStop">Функция, которую необходимо остановить.</param>
        /// <returns></returns>
        public async Task StopExtractorCallAsync(SqlExtractionFunc funcToStop)
        {
            var request = new StopSqlExtractionRequest { Func = funcToStop };

            await _rpcClient.CallAsync<StopSqlExtractionRequest, SqlExtractionResponse>(
                requestedActorId: _actorToInteract["SQLExtractor"]!,
                request: request,
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );
            SetExtractorStateInCache(request.Func, false);
        }

        /// <summary>
        /// Метод запуска EsIndexer.
        /// </summary>
        /// <returns></returns>
        public async Task CallIndexerAsync()
        {
            var queues = _config.GetSection("QueueNames");
            var request = new StartEsIndexRequest
            {
                ArticlesQueue = queues["NewAndUpdatedQueueName"]!,
                DeleteQueue = queues["DeletedQueueName"]!
            };

            await _rpcClient.CallAsync<StartEsIndexRequest, EsIndexResponse>(
                requestedActorId: _actorToInteract["EsIndexer"]!, 
                request: request, 
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );          
        }

        public async Task<bool> IsEsIndexerStartedCallAsync()
        {
            IndexerCurrentStateResponse? result;

            try
            {
                result = await _rpcClient.CallAsync<IndexerCurrentStateRequest, IndexerCurrentStateResponse>(
                    requestedActorId: _actorToInteract["EsIndexer"]!,
                    request: new IndexerCurrentStateRequest(),
                    timeout: int.Parse(_timeout["CallTimeout"]!)
                );
            }
            catch (TimeoutException)
            {
                throw;
            }

            return result.IsStarted;
        }

        /// <summary>
        /// Метод остановки EsIndexer.
        /// </summary>
        /// <returns></returns>
        public async Task StopIndexingCallAsync()
        {
            await _rpcClient.CallAsync<StopEsIndexRequest, EsIndexResponse>(
                requestedActorId: _actorToInteract["EsIndexer"]!,
                request: new StopEsIndexRequest(),
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );
        }

        /// <summary>
        /// Метод запуска TelegramCollector.
        /// </summary>
        /// <param name="number">Номер телефона.</param>
        /// <returns>Ответ от Rpc.</returns>
        public async Task<string> CallTgCollectorAsync(string number)
        {
            var request = new StartTgCollectorRequest { Number = number };

            var result = await _rpcClient.CallAsync<StartTgCollectorRequest, TgCollectorResponse>(
                requestedActorId: _actorToInteract["TelegramCollector"]!, 
                request: request, 
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );

            return result.Message!;
        }

        /// <summary>
        /// Метод установки верификационного кода.
        /// </summary>
        /// <param name="verificationCode"></param>
        /// <returns>Статус выполнения.</returns>
        public async Task<TgCollectorStatusCode> SetVerificationCodeCallAsync(string verificationCode)
        {
            var request = new SetTgCollectorVerificationRequest { VerificationCode = verificationCode };

            var result = await _rpcClient.CallAsync<SetTgCollectorVerificationRequest, TgCollectorResponse>(
                requestedActorId: _actorToInteract["TelegramCollector"]!,
                request: request, 
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );

            return result.Status;
        }

        /// <summary>
        /// Метод остановки TelegramCollector.
        /// </summary>
        /// <returns>Ответ от Rpc.</returns>
        public async Task<string> StopTgCollectorCallAsync()
        {
            var result = await _rpcClient.CallAsync<StopTgCollectorRequest, TgCollectorResponse>(
                requestedActorId: _actorToInteract["TelegramCollector"]!, 
                request: new StopTgCollectorRequest(), 
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );

            return result.Message!;
        }

        /// <summary>
        /// Метод, запрашивающий статус работы TelegramCollector.
        /// </summary>
        /// <returns>Статус работы.</returns>
        public async Task<bool> IsTgCollectorStartedCallAsync()
        {
            TgCollectorCurrentStateResponse? result;

            try
            {
                result = await _rpcClient.CallAsync<TgCollectorCurrentStateRequest, TgCollectorCurrentStateResponse>(
                    requestedActorId: _actorToInteract["TelegramCollector"]!,
                    request: new TgCollectorCurrentStateRequest(),
                    timeout: int.Parse(_timeout["CallTimeout"]!)
                );
            }
            catch (TimeoutException)
            {
                throw;
            }

            return result.IsStarted;
        }

        /// <summary>
        /// Метод запуска VkCollector.
        /// </summary>
        /// <returns>Ответ от Rpc.</returns>
        public async Task<string?> CallVkCollectorAsync()
        {
            var result = await _rpcClient.CallAsync<StartVkCollectorRequest, VkCollectorResponse>(
                requestedActorId: _actorToInteract["VkCollector"]!,
                request: new StartVkCollectorRequest(),
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );

            return result.Response;
        }

        /// <summary>
        /// Метод установки токена доступа.
        /// </summary>
        /// <param name="code">Код.</param>
        /// <returns>Ответ от Rpc.</returns>
        public async Task<string?> AccessTokenCallAsync(string code)
        {
            var request = new AccessTokenRequest { Code = code };
            VkCollectorResponse result;

            try
            {
                result = await _rpcClient.CallAsync<AccessTokenRequest, VkCollectorResponse>(
                    requestedActorId: _actorToInteract["VkCollector"]!,
                    request: request,
                    timeout: int.Parse(_timeout["CallTimeout"]!)
                );
            }
            catch(TimeoutException)
            {
                throw;
            }
            
            return result.Response;
        }

        /// <summary>
        /// Метод, запрашивающий статус работы VkCollector.
        /// </summary>
        /// <returns>Статус работы.</returns>
        public async Task<bool> IsVkCollectorStartedCallAsync()
        {
            VkCollectorCurrentStateResponse? result;

            try
            {
                result = await _rpcClient.CallAsync<VkCollectorCurrentStateRequest, VkCollectorCurrentStateResponse>(
                    requestedActorId: _actorToInteract["VkCollector"]!,
                    request: new VkCollectorCurrentStateRequest(),
                    timeout: int.Parse(_timeout["CallTimeout"]!)
                );
            }
            catch (TimeoutException)
            {
                throw;
            }

            return result.IsStarted;
        }

        /// <summary>
        /// Метод остановки VkCollector.
        /// </summary>
        /// <returns></returns>
        public async Task StopVkCollectorCallAsync()
        {
            await _rpcClient.CallAsync<StopVkCollectorRequest, VkCollectorResponse>(
                requestedActorId: _actorToInteract["VkCollector"]!,
                request: new StopVkCollectorRequest(),
                timeout: int.Parse(_timeout["CallTimeout"]!)
            );
        }

        /// <summary>
        /// Метод получения количества сообщений в очередях, имена которых указаны в appsettings.json в секции QueueNames.
        /// </summary>
        /// <returns>Модель, представляющая количество сообщений в очередях.</returns>
        public QueueCountViewModel GetQueuesCountAsync()
        {
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();
            
            var queues = _config.GetSection("QueueNames");

            QueueCountViewModel resultQueueModel = new QueueCountViewModel();

            QueueDeclareOk? newQueueCount = null;
            QueueDeclareOk? deletedQueueCount = null;

            try
            {
                newQueueCount = channel.QueueDeclarePassive(queues["NewAndUpdatedQueueName"]);
                resultQueueModel.NewAndUpdatedQueueCount = (int)newQueueCount.MessageCount;
            }
            catch (Exception)
            {
                resultQueueModel.NewAndUpdatedQueueCount = null;
            }

            try
            {
                deletedQueueCount = channel.QueueDeclarePassive(queues["DeletedQueueName"]);
                resultQueueModel.DeletedQueueCount = (int)deletedQueueCount.MessageCount;
            }
            catch (Exception)
            {
                resultQueueModel.DeletedQueueCount = null;
            }

            return resultQueueModel;
            
        }

        /// <summary>
        /// Метод объявления очереди.
        /// </summary>
        /// <param name="queueName">Имя очереди.</param>
        /// <param name="limit">Лимит сообщений.</param>
        public string? DeclareQueue(string queueName, int limit)
        {
            try
            {
                BaseHandler.DeclareQueue(_factory.CreateConnection(), queueName, limit);

                var queues = _config.GetSection("QueueNames");
                var state = _memoryCache.Get<StateViewModel>("state") ?? new StateViewModel();
                if (string.IsNullOrWhiteSpace(state.DeletedQueueName))
                    state.DeletedQueueName = queues["DeletedQueueName"];
                if (string.IsNullOrWhiteSpace(state.NewAndUpdatedQueueName))
                    state.NewAndUpdatedQueueName = queues["NewAndUpdatedQueueName"];
                if (queueName == state.DeletedQueueName)
                    state.DeletedCount = 0;
                if (queueName == state.NewAndUpdatedQueueName)
                    state.NewCount = 0;

                _memoryCache.Set("state", state, TimeSpan.FromSeconds(30));

            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return null;
        }
    }
}
