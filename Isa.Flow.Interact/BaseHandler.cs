using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Isa.Flow.Interact.Resources;
using Isa.Flow.Interact.Extensions;
using RabbitMQ.Client.Exceptions;

namespace Isa.Flow.Interact
{
    /// <summary>
    /// Базовый класс обработчика элементарной функции взаимодействия.
    /// </summary>
    public abstract class BaseHandler : IDisposable
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="actorId">Идентификатор актора.</param>
        /// <param name="connection">Соединение Rabbit.</param>
        /// <exception cref="ArgumentNullException">В случае, если <paramref name="connection"/> = null/></exception>
        /// <exception cref="ArgumentException">В случае, если соединение закрыто или пустой <paramref name="actorId"/></exception>
        protected BaseHandler(string actorId, IConnection connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (!connection.IsOpen)
                throw new ArgumentException(Error.ConnectionClosed, nameof(connection));

            if (string.IsNullOrWhiteSpace(actorId))
                throw new ArgumentException(Error.ActorIdCannotBeNullEmptyOrBlank, nameof(actorId));

            Connection = connection;
            ActorId = actorId;
            Consumers = new List<EventingBasicConsumer>();
        }

        /// <summary>
        /// Канал Rabbit.
        /// </summary>
        protected IConnection Connection { get; }

        /// <summary>
        /// Канал Rabbit.
        /// </summary>
        protected IModel? Channel { get; set; }

        /// <summary>
        /// Идентификатор актора.
        /// </summary>
        protected string ActorId { get; }

        /// <summary>
        /// Список потребителей.
        /// </summary>
        protected List<EventingBasicConsumer> Consumers { get; set; }

        /// <summary>
        /// Метод создания потребителя RabbitMq.
        /// </summary>
        /// <returns>Потребитель RabbitMq.</returns>
        /// <remarks>Используйте этот метод в унаследованных классах для создания потребителей,
        /// если хотите, чтобы потребители генерировали событие <seealso cref="Stopped"/> и влияли на свойство <seealso cref="IsRunning"/>.</remarks>
        protected EventingBasicConsumer CreateConsumer()
        {
            var consumer = new EventingBasicConsumer(Channel);
            consumer.ConsumerCancelled += OnConsumerCancelled;
            consumer.Registered += OnConsumerRegistered;
            Consumers.Add(consumer);
            return consumer;
        }

        /// <summary>
        /// Метод обработки события запуска потребителя.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Параметры события.</param>
        private void OnConsumerRegistered(object? sender, ConsumerEventArgs e)
        {
            if (IsRunning)
                Started?.Invoke(this, new System.EventArgs());
        }

        /// <summary>
        /// Метод обработки события отмены потребителя.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Параметры события.</param>
        private void OnConsumerCancelled(object? sender, ConsumerEventArgs e)
        {
            if (!IsRunning)
                Stopped?.Invoke(this, new System.EventArgs());
        }

        /// <summary>
        /// Событие возникает, когда обработчик по каким-либо причинам выходит из состояния "активный".
        /// </summary>
        public event EventHandler<System.EventArgs> Stopped;

        /// <summary>
        /// Событие возникает, когда обработчик переходит в состояние активен.
        /// </summary>
        public event EventHandler<System.EventArgs> Started;

        /// <summary>
        /// Имя очереди.
        /// </summary>
        public string? QueueName { get; protected set; }

        /// <summary>
        /// Признак того, что обработчик активен,
        /// т.е. имеет хотя бы одного потребителя в состоянии ожидания входящих сообщений.
        /// </summary>
        public bool IsRunning => Consumers is not null && Consumers.Any(c => c.IsRunning);

        /// <summary>
        /// Метод создания очереди.
        /// </summary>
        /// <param name="queueName">Имя очереди.</param>
        /// <param name="limit">Максимальный размер очереди.</param>
        /// <exception cref="ArgumentException">В случае недопустимого имени очереди.</exception>
        /// <exception cref="AlreadyClosedException">При попытке создать очередь на закрытом соединении.</exception>
        public void DeclareQueue(string queueName, int limit = 0) => DeclareQueue(Connection, queueName, limit);

        /// <summary>
        /// Метод создания очереди.
        /// </summary>
        /// <param name="connection">Соединение с RabbitMq.</param>
        /// <param name="queueName">Имя очереди.</param>
        /// <param name="limit">Максимальный размер очереди.</param
        /// <exception cref="ArgumentNullException">В случае, если <paramref name="connection"/> = null.</exception>
        /// <exception cref="ArgumentException">В случае недопустимого имени очереди.</exception>
        /// <exception cref="AlreadyClosedException">При попытке создать очередь на закрытом соединении.</exception>
        public static void DeclareQueue(IConnection connection, string queueName, int limit = 0)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            queueName.ThrowIfInvalidQueueName();

            using var channel = connection.CreateModel();
            channel.QueueDeclare(
                queueName,
                true,
                false,
                false,
                limit > 0 ? new Dictionary<string, object>() { { "x-max-length", limit }, { "x-overflow", "reject-publish" } } : null
            );
        }

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
                if (Channel != null)
                {
                    foreach (var c in Consumers)
                    {
                        c.ConsumerCancelled -= OnConsumerCancelled;

                        if (!c.IsRunning)
                            continue;

                        foreach (var t in c.ConsumerTags)
                        {
                            try
                            {
                                Channel.BasicCancel(t);
                            }
                            catch { }
                        }
                    }

                    if (Channel!.IsOpen && !string.IsNullOrWhiteSpace(QueueName))
                        Channel!.QueueDeleteNoWait(QueueName, true, false);

                    try
                    {
                        Channel.Dispose();
                    }
                    catch { }
                }

                Consumers.Clear();
            }

            disposed = true;
        }

        #endregion
    }
}