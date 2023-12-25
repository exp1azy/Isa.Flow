using Isa.Flow.Interact.Entities;
using Isa.Flow.Interact.Exceptions;
using Isa.Flow.Interact.Resources;
using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Isa.Flow.Interact
{
    /// <summary>
    /// Базовый класс актора.
    /// </summary>
    public abstract class BaseActor : IDisposable
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="connectionFactory">Фабрика соединений с Rabbit.</param>
        /// <param name="actorId">Идентификатор актора. Если указан null, идентификатор сгенерируется автоматически.</param>
        public BaseActor(ConnectionFactory connectionFactory, string? actorId = null)
        {
            _connectionFactory = connectionFactory;
            _connection = connectionFactory.CreateConnection();

            Id = actorId ?? Guid.NewGuid().ToString();

            _emitter = new Emitter(actorId: Id, connection: _connection);
            _rpcClient = new RpcClient(actorId: Id, connection: _connection);

            _handlers = new List<BaseHandler>();

            _ctsAlive = new CancellationTokenSource();

            SameIdActorChecking();

            _ = BroadcastAliveAsync(_ctsAlive.Token);
        }

        /// <summary>
        /// Публикация широковещательного сообщения.
        /// </summary>
        /// <typeparam name="TNotification">Тип широковещательного сообщения.</typeparam>
        /// <param name="notification">Сообщение.</param>
        public void Broadcast<TNotification>(TNotification notification)
            where TNotification : IValidatableObject
        {
            _emitter.Broadcast(notification);
        }

        /// <summary>
        /// Отправка сообщения в очередь.
        /// </summary>
        /// <typeparam name="TPayload">Тип сообщения.</typeparam>
        /// <param name="queueName">Имя очереди.</param>
        /// <param name="payload">Сообщение.</param>
        public void Enqueue<TPayload>(string queueName, TPayload payload)
            where TPayload : IValidatableObject
        {
            _emitter.Enqueue(queueName: queueName, payload: payload);
        }

        /// <summary>
        /// Метод выполнения RPC-запроса.
        /// </summary>
        /// <typeparam name="TRequest">Тип объекта, представляющего запрос.</typeparam>
        /// <typeparam name="TResponse">Тип объекта, представляющего ответ.</typeparam>
        /// <param name="requestedActorId">Идентификатор актора, к которому адресован запрос.</param>
        /// <param name="request">Запрос.</param>
        /// <param name="timeout">Таймаут запроса в секундах, после которого возникает <see cref="TimeoutException"/>.
        /// Если значение меньше или равно 0, значение таймаута берётся из свойства <see cref="Timeout"/>.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию выполнения запроса.</returns>
        public async Task<TResponse> CallAsync<TRequest, TResponse>(string requestedActorId, TRequest request, int timeout = 0, CancellationToken cancellationToken = default)
            where TRequest : IValidatableObject
            where TResponse : IValidatableObject
        {
            return await _rpcClient.CallAsync<TRequest, TResponse>(requestedActorId:  requestedActorId, request: request, timeout: timeout, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Метод добавления актору возможности обработки RPC-запросов.
        /// </summary>
        /// <typeparam name="TRequest">Тип, представляющий запрос.</typeparam>
        /// <typeparam name="TResponse">Тип, представляющий ответ.</typeparam>
        /// <param name="func">Функция-обработчик запросов.</param>
        /// <param name="concurrency">Степень параллелизма.</param>
        /// <returns>Обработчик RPC-запросов.</returns>
        public RpcHandler<TRequest, TResponse> AddRpcHandler<TRequest, TResponse>(Func<TRequest, TResponse> func, int concurrency = 1)
            where TRequest : IValidatableObject
            where TResponse : IValidatableObject
        {
            var rpcHandler = new RpcHandler<TRequest, TResponse>(actorId: Id, connection: _connection, concurrency: concurrency, func: func);
            _handlers.Add(rpcHandler);

            return rpcHandler;
        }

        /// <summary>
        /// Метод добавления актору возможности обработки широковещательных сообщений.
        /// </summary>
        /// <typeparam name="TNotification">Тип сообщения.</typeparam>
        /// <param name="broadcastActorId">Идентификатор широковещателя.</param>
        /// <param name="action">Функция-обработчик сообщений.</param>
        /// <returns>Обработчик широковещательных сообщений.</returns>
        public BroadcastHandler<TNotification> AddBroadcastHandler<TNotification>(string broadcastActorId, Action<TNotification> action)
            where TNotification : IValidatableObject
        {
            var broadcastHandler = new BroadcastHandler<TNotification>(actorId: Id, connection: _connection, broadcastActorId: broadcastActorId, action: action);
            _handlers.Add(broadcastHandler);

            return broadcastHandler;
        }

        /// <summary>
        /// Метод добавления актору возможности обработки очередей.
        /// </summary>
        /// <typeparam name="TPayload">Тип ожидаемых из очереди сообщений.</typeparam>
        /// <param name="queueName">Имя очереди.</param>
        /// <param name="func">Функция-обработчик сообщений.</param>
        /// <param name="concurrency">Степень параллелизма.</param>
        /// <returns>Обработчик очереди.</returns>
        public QueueHandler<TPayload> AddQueueHandler<TPayload>(string queueName, Action<TPayload> func, int concurrency = 1)
            where TPayload : IValidatableObject
        {
            var queueHandler = new QueueHandler<TPayload>(actorId: Id, connection: _connection, queueName: queueName, concurrency: concurrency, func: func);
            _handlers.Add(queueHandler);

            return queueHandler;
        }

        /// <summary>
        /// Метод пингования указаного актора.
        /// </summary>
        /// <param name="requestedActorId">Идентификатор актора, которому направляется пинг.</param>
        /// <param name="timeout">Период времени в секундах, в течении которого ожидается ответ.</param>
        /// <returns>Задача, представляющая асинхронную операцию пингования.</returns>
        public async Task<bool> PingAsync(string requestedActorId, int timeout)
        {
            var request = new Ping { Time = DateTime.UtcNow };
            try
            {
                var response = await _rpcClient.CallAsync<Ping, Pong>(requestedActorId, request, timeout);

                return response.ActorInfo.Id == requestedActorId && response.Time > request.Time;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Идентификатор актора.
        /// </summary>
        public string Id { get; private set; }

        protected readonly ConnectionFactory _connectionFactory;

        protected IConnection _connection;

        protected Emitter _emitter;

        protected RpcClient _rpcClient;

        protected List<BaseHandler> _handlers;

        ///// <summary>
        ///// Список "живых" акторов.
        ///// </summary>
        //protected Dictionary<string, string>? _alives;

        ///// <summary>
        ///// Канал Rabbit для мониторинга "живых".
        ///// </summary>
        //protected IModel? _channelForAliveMonitiring;

        /// <summary>
        /// Источник токенов отмены для задачи трансляции сигнала "я жив".
        /// </summary>
        private readonly CancellationTokenSource _ctsAlive;

        /// <summary>
        /// Метод остановки обработчика.
        /// </summary>
        /// <param name="baseHandler">Обработчик.</param>
        protected void StopHandler(BaseHandler baseHandler)
        {
            _handlers.Remove(baseHandler);
            baseHandler.Dispose();
        }

        /// <summary>
        /// Метод проверки наличия запущенного или запускаемого актора с таким же идентификатором.
        /// </summary>
        /// <remarks>В случае, если уникальность идентификатора подтверждена, метод завершает работу, оставляя активным обработчик пинга.</remarks>
        /// <exception cref="ActorAlreadyStartedException">В случае, если актор стаким же идентификатором уже работает или находится на стадии запуска.</exception>
        private void SameIdActorChecking()
        {
            // Пингуем актора под идентификатором, с которым собираемся стартовать.
            var pingTask = PingAsync(Id, 10);
            pingTask.Wait();

            //  Прерываем запуск, если пришел ответ на пинг.
            if (pingTask.Result)
                throw new ActorAlreadyStartedException();

            // Запускаем собственный обработчик пинга.
            var pingHandler = AddRpcHandler<Ping, Pong>(f => GetPong());

            var launchId = Guid.NewGuid();
            var launchNotificationCancellationToken = new CancellationTokenSource();
            var launchNotificationAccepted = new AutoResetEvent(false);

            // Запускаем транслятор оповещений о намерении запуститься.
            var launchNotificationTask = Task.Run(async () =>
            {
                while (!launchNotificationCancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500);
                    Broadcast(new ActorLaunchNotification() { LaunchId = launchId });
                }
            });

            // Запускаем прослушивание оповещений о намерении запустится.
            var launchNotificationHandler = AddBroadcastHandler<ActorLaunchNotification>(Id, l =>
            {
                // Игнорируем собственные оповещения о намерении запустится.
                if (l.LaunchId != launchId)
                    // Если получено чужое оповещение - сигнализируем.
                    launchNotificationAccepted.Set();
            });

            // 10 секунд ждём сигнала о том, что от кого-то получено оповещение о намерении запуститься с таким же Id.
            var sameIdActorIsRunning = launchNotificationAccepted.WaitOne(10000);

            // Завершаем трансляцию собственных оповещений о намерении запуститься,
            // завершаем обработчик оповещений.
            launchNotificationCancellationToken.Cancel();
            StopHandler(launchNotificationHandler);
            launchNotificationTask.Wait();

            // Если был получен сигнал отом, что кото-то еще запускается с нашим Id, прерываем запуск.
            if (sameIdActorIsRunning)
                throw new ActorAlreadyStartedException(Error.OtherActorWithTheSameIdLaunching);
        }

        /// <summary>
        /// Метод запуска трансляции сигнала "я жив".
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <remarks>Метод выполняет публикацию одного сигнала,
        /// ожидание длиной <seealso cref="Constant.AliveSignalPeriol"/> миллисекунд и запуск следующей итерации.</remarks>
        /// <returns>Задача, представляющая асинхронную операцию трансляции.</returns>
        private async Task BroadcastAliveAsync(CancellationToken cancellationToken)
        {
            var body = new Message<Pong> { Payload = GetPong() }.ToBytes();

            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(Constant.WhoAliveExchangeName, ExchangeType.Fanout);
            channel.BasicPublish(Constant.WhoAliveExchangeName, string.Empty, null, body);

            await Task.Delay(Constant.AliveSignalPeriol, cancellationToken);

            _ = BroadcastAliveAsync(cancellationToken);
        }

        /// <summary>
        /// Метод генерации сообщения типа <see cref="Pong"/> как ответа на сообщение <see cref="Ping"/>, которое так же применяется в качестве оповещения "я жив";
        /// </summary>
        /// <returns>Сообщение типа <see cref="Pong"/>.</returns>
        private Pong GetPong() => new () { Time = DateTime.UtcNow, ActorInfo = new ActorInfo { Id = Id, Type = GetType().AssemblyQualifiedName } };

        #region Реализация интерфейса IDisposable

        bool disposed = false;

        /// <summary>
        /// Метод завершения работы обработчика.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Метод завершения работы обработчика.
        /// </summary>
        /// <param name="disposing">Признак того, что выполняется завершение.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _ctsAlive.Cancel();

                foreach (var handler in _handlers)                
                    handler.Dispose();

                _emitter.Dispose();
                _rpcClient.Dispose();
                _connection.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}