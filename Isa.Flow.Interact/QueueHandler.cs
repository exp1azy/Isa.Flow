using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;
using Isa.Flow.Interact.Exceptions;
using RabbitMQ.Client.Events;
using Isa.Flow.Interact.Resources;
using Isa.Flow.Interact.Entities;
using Isa.Flow.Interact.Extensions;

namespace Isa.Flow.Interact
{
    /// <summary>
    /// Обработчик очереди.
    /// </summary>
    /// <typeparam name="TPayload">Тип сообщения.</typeparam>
    public class QueueHandler<TPayload> : BaseHandler
        where TPayload : IValidatableObject
    {
        /// <summary>
        /// Функция обработки входящих сообщений.
        /// </summary>
        private readonly Action<TPayload> Action;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="actorId">Идентификатор актора.</param>
        /// <param name="queueName">Имя очереди.</param>
        /// <param name="connection">Соединение Rabbit.</param>
        /// <param name="concurrency">Степень паралеллизма (кол-во потребителей, обрабатывающих входящие сообщения параллельно).</param>
        /// <param name="func">Функция-обработчик сообщений.</param>
        /// <exception cref="SubscriptionException">В случае ошибки в процессе запуска прослушивания запросов.</exception>
        /// <exception cref="ArgumentNullException">В случае, если в параметры <paramref name="connection"/> или <paramref name="func"/> передан null.</exception>
        /// <exception cref="ArgumentException">В случае, если в параметры переданы недопустимые значения.</exception>
        /// <exception cref="ArgumentOutOfRangeException">В случае, если значение параметра <paramref name="concurrency"/> выходит за допустимые пределы.</exception>
        public QueueHandler(string actorId, IConnection connection, string queueName, int concurrency, Action<TPayload> func)
            : base(actorId, connection)
        {
            queueName.ThrowIfInvalidQueueName();

            if (concurrency < 1 || concurrency > 10)
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, Error.ConcurrentCustomersOutOfRange);

            Action = func ?? throw new ArgumentNullException(nameof(func));
            QueueName = queueName;

            try
            {
                Channel = Connection.CreateModel();

                for (int i = 0; i < concurrency; i++)
                {
                    var consumer = CreateConsumer();
                    consumer.Received += OnReceived;
                    Channel.BasicConsume(QueueName, false, consumer);
                }
            }
            catch (Exception ex)
            {
                throw new SubscriptionException(Error.SubscriptionError, ex);
            }
        }

        /// <summary>
        /// Метод обработки входящего сообщения.
        /// </summary>
        /// <param name="sender">Канал Rabbit.</param>
        /// <param name="ea">Параметры события.</param>
        private void OnReceived(object? sender, BasicDeliverEventArgs ea)
        {
            Message<TPayload>? message = null;
            byte[]? incomingBytes = null;

            try
            {
                incomingBytes = ea.Body.ToArray();

                try
                {
                    message = Message<TPayload>.FromBytes(incomingBytes);

                    OnIncoming?.Invoke(this, new EventArgs.IncomingEventArgs<TPayload>
                    {
                        ActorId = ActorId,
                        Incoming = message.Payload
                    });
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                    {
                        ActorId = ActorId,
                        Exception = e,
                        RawIncoming = incomingBytes,
                        Type = EventArgs.ErrorType.Deserializing
                    });
                    Channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                try
                {
                    message.ThrowIfInvalid();
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                    {
                        ActorId = ActorId,
                        Exception = e,
                        Incoming = message.Payload,
                        Type = EventArgs.ErrorType.Validating
                    });
                }

                Channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                try
                {
                    Action(message.Payload!);

                    OnHandled?.Invoke(this, new EventArgs.IncomingEventArgs<TPayload>
                    {
                        ActorId = ActorId,
                        Incoming = message.Payload
                    });
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                    {
                        ActorId = ActorId,
                        Exception = e,
                        Incoming = message.Payload,
                        Type = EventArgs.ErrorType.Handling
                    });
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                {
                    ActorId = ActorId,
                    Exception = e,
                    Incoming = message.Payload == null ? default : message.Payload,
                    Outgoing = message.Payload,
                    RawIncoming = incomingBytes,
                    Type = EventArgs.ErrorType.Acking
                });
            }
        }

        /// <summary>
        /// Событие сигнализирует об ошибке в процессе работы обработчика.
        /// </summary>
        public event EventHandler<EventArgs.ErrorEventArgs<TPayload, TPayload>>? OnError;

        /// <summary>
        /// Событие сигнализирует о получении сообщения.
        /// </summary>
        public event EventHandler<EventArgs.IncomingEventArgs<TPayload>>? OnIncoming;

        /// <summary>
        /// Событие сигнализирует об окончании обработки сообщения.
        /// </summary>
        public event EventHandler<EventArgs.IncomingEventArgs<TPayload>>? OnHandled;

        #region Реализация паттерна IDisposable

        bool disposed = false;

        /// <summary>
        /// Метод завершения работы обработчика.
        /// </summary>
        /// <param name="disposing">Признак того, что выполняется завершение.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (var c in Consumers)
                    c.Received -= OnReceived;

                QueueName = null;
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}