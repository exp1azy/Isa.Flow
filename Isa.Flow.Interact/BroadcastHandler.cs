using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;
using RabbitMQ.Client.Events;
using Isa.Flow.Interact.Entities;

namespace Isa.Flow.Interact
{
    /// <summary>
    /// Обработчик широковещательных сообщений.
    /// </summary>
    public class BroadcastHandler<TNotification> : BaseHandler
        where TNotification : IValidatableObject
    {
        /// <summary>
        /// Идентификатор актора-источника широковещательных сообщений, предполагаемых к обработке.
        /// </summary>
        protected string BroadcastActorId { get; set; }

        /// <summary>
        /// Функция обработчик.
        /// </summary>
        protected Action<TNotification> Action { get; set; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="actorId">Идентификатор актора.</param>
        /// <param name="connection">Соединение Rabbit</param>
        /// <param name="broadcastActorId">Идентификатор актора-транслятора широковещательных сообщений.</param>
        public BroadcastHandler(string actorId, IConnection connection, string broadcastActorId, Action<TNotification> action)
            : base(actorId, connection)
        {
            BroadcastActorId = broadcastActorId;
            Action = action;
            var exchangeName = $"{Constant.BroadcastExchangeNamePrefix}{BroadcastActorId}";

            Channel = Connection.CreateModel();

            Channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
            QueueName = Channel.QueueDeclare().QueueName;
            Channel.QueueBind(QueueName, exchangeName, string.Empty);

            var consumer = CreateConsumer();
            consumer.Received += Received;
            Channel.BasicConsume(QueueName, true, consumer);
        }

        /// <summary>
        /// Метод обработки события получения сообщения.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="ea">Параметры события.</param>
        private void Received(object? sender, BasicDeliverEventArgs ea)
        {
            byte[]? incomingBytes = null;
            Message<TNotification>? message = null;

            try
            {
                incomingBytes = ea.Body.ToArray();

                try
                {
                    message = Message<TNotification>.FromBytes(incomingBytes);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<IValidatableObject, IValidatableObject>
                    {
                        ActorId = ActorId,
                        ContrActorId = BroadcastActorId,
                        Exception = ex,
                        Incoming = message.Payload == null ? default : message.Payload,
                        Outgoing = message.Payload,
                        RawIncoming = incomingBytes,
                        Type = EventArgs.ErrorType.Deserializing
                    });

                    return;
                }

                try
                {
                    message.ThrowIfInvalid();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<IValidatableObject, IValidatableObject>
                    {
                        ActorId = ActorId,
                        ContrActorId = BroadcastActorId,
                        Exception = ex,
                        Incoming = message.Payload == null ? default : message.Payload,
                        Outgoing = message.Payload,
                        RawIncoming = incomingBytes,
                        Type = EventArgs.ErrorType.Validating
                    });

                    return;
                }

                OnIncoming?.Invoke(this, new EventArgs.IncomingEventArgs<IValidatableObject>
                {
                    ActorId = ActorId,
                    ContrActorId = BroadcastActorId,
                    Incoming = message.Payload
                });

                Action(message.Payload!);

                OnHandled?.Invoke(this, new EventArgs.IncomingEventArgs<IValidatableObject>
                {
                    ActorId = ActorId,
                    ContrActorId = BroadcastActorId,
                    Incoming = message.Payload
                });
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new EventArgs.ErrorEventArgs<IValidatableObject, IValidatableObject>
                {
                    ActorId = ActorId,
                    ContrActorId = BroadcastActorId,
                    Exception = ex,
                    Incoming = message.Payload == null ? default : message.Payload,
                    Outgoing = message.Payload,
                    RawIncoming = incomingBytes,
                    Type = EventArgs.ErrorType.Handling
                });
            }
        }

        /// <summary>
        /// Событие сигнализирует об ошибке в процессе работы обработчика.
        /// </summary>
        public event EventHandler<EventArgs.ErrorEventArgs<IValidatableObject, IValidatableObject>>? OnError;

        /// <summary>
        /// Событие сигнализирует о получении сообщения.
        /// </summary>
        public event EventHandler<EventArgs.IncomingEventArgs<IValidatableObject>>? OnIncoming;

        /// <summary>
        /// Событие сигнализирует об окончании обработки сообщения.
        /// </summary>
        public event EventHandler<EventArgs.IncomingEventArgs<IValidatableObject>>? OnHandled;

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
                    c.Received -= Received;

                Channel.QueueDelete(QueueName);
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}