using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Isa.Flow.Interact.Exceptions;
using Isa.Flow.Interact.Entities;
using Isa.Flow.Interact.Resources;
using Isa.Flow.Interact.Extensions;

namespace Isa.Flow.Interact
{
    /// <summary>
    /// Клиент для отправки сообщений.
    /// </summary>
    public class Emitter : BaseHandler
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="actorId">Идентификатор актора.</param>
        /// <param name="connection">Соединение Rabbit</param>
        public Emitter(string actorId, IConnection connection)
            : base(actorId, connection)
        {
        }

        /// <summary>
        /// Публикация широковещательного сообщения.
        /// </summary>
        /// <typeparam name="TNotification">Тип широковещательного сообщения.</typeparam>
        /// <param name="notification">Сообщение.</param>
        /// <exception cref="SerializationException">В случае ошибки сериализации сообщения перед отправкой.</exception>
        /// <exception cref="SendingException">В случае ошибки отправки сообщения.</exception>
        public void Broadcast<TNotification>(TNotification notification)
            where TNotification : IValidatableObject
        {
            var exchange = $"{Constant.BroadcastExchangeNamePrefix}{ActorId}";
            var message = new Message<TNotification>
            {
                Payload = notification
            };
            var body = message.ToBytes();

            try
            {
                using var channel = Connection.CreateModel();
                channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
                channel.BasicPublish(exchange, string.Empty, null, body);
            }
            catch (Exception ex)
            {
                throw new SendingException(Error.SendingError, ex);
            }
        }

        /// <summary>
        /// Отправка сообщения в очередь.
        /// </summary>
        /// <typeparam name="TPayload">Тип сообщения.</typeparam>
        /// <param name="queueName">Имя очереди.</param>
        /// <param name="payload">Сообщение.</param>
        /// <exception cref="SendingException">В случае ошибки сериализации сообщения перед отправкой.</exception>
        /// <exception cref="SerializationException">В случае ошибки сериализации сообщения.</exception>
        /// <exception cref="ArgumentException">В случае недопустимого имени очереди.</exception>
        public void Enqueue<TPayload>(string queueName, TPayload payload)
            where TPayload : IValidatableObject
        {
            queueName.ThrowIfInvalidQueueName();

            var message = new Message<TPayload>
            {
                Payload = payload
            };
            var body = message.ToBytes();

            try
            {
                using var channel = Connection.CreateModel();
                channel.ConfirmSelect();

                var eventAck = new AutoResetEvent(false);
                var eventNack = new AutoResetEvent(false);
                var eventReturned = new AutoResetEvent(false);

                channel.BasicAcks += (s, ea) => eventAck.Set();
                channel.BasicNacks += (s, ea) => eventNack.Set();
                channel.BasicReturn += (s, ea) => eventReturned.Set();

                channel.BasicPublish(string.Empty, queueName, true, null, body);
                var i = WaitHandle.WaitAny(new[] { eventAck, eventReturned, eventNack }, TimeSpan.FromSeconds(5));
                if (i == 1)
                    throw new MessageRoutingException();
                else if (i > 1)
                    throw new MessageNackException();                    
            }
            catch (Exception ex)
            {
                throw new SendingException(Error.SendingError, ex);
            }
        }

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
                // тут вызывать Dispose для управляемых объектов, созданных обработчиком.
                QueueName = null;
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}