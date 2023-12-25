using Isa.Flow.Interact;
using Isa.Flow.SQLExtractor.Resources;
using Isa.Flow.Interact.Extractor.Rpc;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Serilog;

namespace Isa.Flow.SQLExtractor
{
    /// <summary>
    /// Реализация актора, выполняющего функции извлечения информации из БД.
    /// </summary>
    public partial class Extractor : BaseActor
    {
        /// <summary>
        /// Конфигуратор.
        /// </summary>
        private readonly IConfiguration _config;

        /// <summary>
        /// Объект с параметрами запуска.
        /// </summary>
        private readonly FuncParams _funcParams;

        /// <summary>
        /// Токен отмены для функции извлечения новых статей.
        /// </summary>
        private CancellationTokenSource? _cancellationTokenNew;

        /// <summary>
        /// Токен отмены для функции извлечения идентификаторов удаленных статей.
        /// </summary>
        private CancellationTokenSource? _cancellationTokenDeleted;

        /// <summary>
        /// Токен отмены для функции извлечения измененных статей.
        /// </summary>
        private CancellationTokenSource? _cancellationTokenModified;

        /// <summary>
        /// Задача, связанная с извлечением новых статей.
        /// </summary>
        private Task? _newTask;

        /// <summary>
        /// Задача, связанная с извлечением идентификаторов удаленных статей.
        /// </summary>
        private Task? _deletedTask;

        /// <summary>
        /// Задача, связанная с извлечением измененных статей.
        /// </summary>
        private Task? _modifiedTask;

        /// <summary>
        /// Задача, связанная с переиндексацией интервала.
        /// </summary>
        private Task? _reindexTask;

        /// <summary>
        /// Задача, связанная с очисткой интервала.
        /// </summary>
        private Task? _cleanTask;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="funcParams">Параметры запуска функций экстрактора.</param>
        /// <param name="logger">Логгер.</param>
        /// <param name="config">Конфигурационная информация.</param>
        public Extractor(FuncParams funcParams, IConfiguration config)
            : base(new ConnectionFactory() { Uri = new Uri(config.GetSection("RabbitMq")["Uri"]!) }, config["ActorId"])
        {
            _config = config;
            _funcParams = funcParams;     

            AddRpcHandler<StartSqlExtractionRequest, SqlExtractionResponse>(HandleStartRequest);
            AddRpcHandler<StopSqlExtractionRequest, SqlExtractionResponse>(HandleStopRequest);
            AddRpcHandler<FuncStatusRequest, FuncStateResponse>(HandleStatusRequest);
            AddRpcHandler<IntervalRequest, SqlExtractionResponse>(request => request.Func switch
            {
                SqlExtractionFunc.Reindex => HandleReindexRequest(request),
                SqlExtractionFunc.Clean => HandleClearRequest(request),
                _ => new SqlExtractionResponse { DateTime = DateTime.UtcNow },
            });
        }

        /// <summary>
        /// Метод обработки запроса на запуск функции экстрактора.
        /// </summary>
        /// <param name="request">RPC-запрос.</param>
        /// <returns>RPC-ответ.</returns>
        private SqlExtractionResponse HandleStartRequest(StartSqlExtractionRequest request)
        {
            StartFunction(request);
            return new SqlExtractionResponse() { DateTime = DateTime.UtcNow };
        }

        /// <summary>
        /// Метод обработки запроса на запуск функции экстрактора.
        /// </summary>
        /// <param name="request">RPC-запрос.</param>
        /// <returns>RPC-ответ.</returns>
        private SqlExtractionResponse HandleStopRequest(StopSqlExtractionRequest request)
        {
            if (request.Func == SqlExtractionFunc.New)
            {
                if (_cancellationTokenNew != null)
                {
                    _cancellationTokenNew.Cancel();

                    _newTask?.Wait();

                    _cancellationTokenNew?.Dispose();
                    _cancellationTokenNew = null;
                    _newTask = null;
                }
            }
            else if (request.Func == SqlExtractionFunc.Deleted)
            {
                if (_cancellationTokenDeleted != null)
                {
                    _cancellationTokenDeleted.Cancel();

                    _deletedTask?.Wait();

                    _cancellationTokenDeleted.Dispose();
                    _cancellationTokenDeleted = null;
                }
            }
            else if (request.Func == SqlExtractionFunc.Updated)
            {
                if (_cancellationTokenModified != null)
                {
                    _cancellationTokenModified.Cancel();

                    _modifiedTask?.Wait();

                    _cancellationTokenModified.Dispose();
                    _cancellationTokenModified = null;
                }
            }

            return new SqlExtractionResponse() { DateTime = DateTime.UtcNow };
        }

        /// <summary>
        /// Метод обработки запроса статуса экстрактора.
        /// </summary>
        /// <param name="request">RPC-запрос.</param>
        /// <returns>RPC-ответ.</returns>
        private FuncStateResponse HandleStatusRequest(FuncStatusRequest _)
        {
            Log.Logger.Information((_cancellationTokenNew != null).ToString());
            return new FuncStateResponse
            {
                NewState = _cancellationTokenNew != null,
                ModifiedState = _cancellationTokenModified != null,
                DeletedState = _cancellationTokenDeleted != null,
                ReindexState = _reindexTask != null && !_reindexTask.IsCompleted,
                CleanState = _cleanTask != null && !_cleanTask.IsCompleted,
                LastArticleId = _funcParams.LastArticleId
            };
        }

        /// <summary>
        /// Метод обработки запроса на реиндексацию.
        /// </summary>
        /// <param name="request">RPC-запрос.</param>
        /// <returns>RPC-ответ.</returns>
        private SqlExtractionResponse HandleReindexRequest(IntervalRequest request)
        {
            var queue = (string.IsNullOrWhiteSpace(request.Queue) ? _funcParams.UpdatedArticleQueueName : request.Queue)
                ?? throw new ApplicationException(Message.QueueNameNotDefined);

            if (_reindexTask != null && !_reindexTask.IsCompleted)
                throw new ApplicationException(Message.ReindexAlreadyStarted);

            _reindexTask = Task.Run(async () => await ReindexAsync(request.From, request.To, queue));

            return new SqlExtractionResponse { DateTime = DateTime.UtcNow };
        }

        /// <summary>
        /// Метод обработки запроса на очистку.
        /// </summary>
        /// <param name="request">RPC-запрос.</param>
        /// <returns>RPC-ответ.</returns>
        private SqlExtractionResponse HandleClearRequest(IntervalRequest request)
        {
            var queue = (string.IsNullOrWhiteSpace(request.Queue) ? _funcParams.DeletedArticleQueueName : request.Queue)
                ?? throw new ApplicationException(Message.QueueNameNotDefined);

            if (_cleanTask != null && !_cleanTask.IsCompleted)
                throw new ApplicationException(Message.CleanAlreadyStarted);

            _cleanTask = Task.Run(async () => await ClearAsync(request.From, request.To, queue));

            return new SqlExtractionResponse { DateTime = DateTime.UtcNow };
        }
    }
}