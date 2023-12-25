using Isa.Flow.Interact.Entities;
using Isa.Flow.Interact.Exceptions;
using Isa.Flow.Interact.Resources;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact
{
    /// <summary>
    /// Обработчик RPC-запроса
    /// </summary>
    /// <typeparam name="TRequest">Тип объекта, представляющий RPC-запрос.</typeparam>
    /// <typeparam name="TResponse">Тип объекта, представляющий ответ на запрос.</typeparam>
    public class RpcHandler<TRequest, TResponse> : BaseHandler
        where TRequest : IValidatableObject
        where TResponse : IValidatableObject
    {
        /// <summary>
        /// Функция обработки входящего запроса.
        /// </summary>
        protected Func<TRequest, TResponse> Func { get; set; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="actorId">Идентификатор актора.</param>
        /// <param name="connection">Соединение Rabbit.</param>
        /// <param name="concurrency">Кол-во одновременно обрабатываемых запросов.</param>
        /// <param name="func">Функция-обработчик RPC-запроса.</param>
        /// <exception cref="SubscriptionException">В случае ошибки в процессе запуска прослушивания запросов.</exception>
        /// <exception cref="ArgumentNullException">В случае, если в параметры <paramref name="connection"/> или <paramref name="func"/> передан null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">В случае, если значение параметра <paramref name="concurrency"/> выходит за допустимый диапазон />.</exception>
        public RpcHandler(string actorId, IConnection connection, int concurrency, Func<TRequest, TResponse> func)
            : base(actorId, connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (concurrency < 1 || concurrency > 10)
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, Error.ConcurrentCustomersOutOfRange);

            Func = func ?? throw new ArgumentNullException(nameof(func));

            QueueName = $"{Constant.RpcQueueNamePrefix}{actorId}_{typeof(TRequest).AssemblyQualifiedName}";

            try
            {
                Channel = Connection.CreateModel();
                Channel.QueueDeclare(queue: QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                Channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                for (int i = 0; i < concurrency; i++)
                {
                    var consumer = CreateConsumer();
                    consumer.Received += OnRecieved;
                    Channel.BasicConsume(QueueName, false, consumer);
                }
            }
            catch (Exception ex)
            {
                throw new SubscriptionException(Error.SubscriptionError, ex);
            }
        }

        /// <summary>
        /// Метод обработки входящего RPC-запроса.
        /// </summary>
        /// <param name="model">Канал Rabbit.</param>
        /// <param name="ea">Параметры события.</param>
        private void OnRecieved(object? model, BasicDeliverEventArgs ea)
        {
            Message<TRequest>? msgRequest = null;
            var msgResponse = new Message<IValidatableObject>();
            byte[]? incomingBytes = null;

            try
            {
                incomingBytes = ea.Body.ToArray();

                try
                {
                    msgRequest = Message<TRequest>.FromBytes(incomingBytes);
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                    {
                        Type = EventArgs.ErrorType.Deserializing,
                        ActorId = ActorId,
                        Exception = e,
                        RawIncoming = incomingBytes
                    });
                    Channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                try
                {
                    msgRequest.ThrowIfInvalid();
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                    {
                        Type = EventArgs.ErrorType.Validating,
                        ActorId = ActorId,
                        Exception = e,
                        Incoming = msgRequest.Payload
                    });
                    msgResponse.Payload = new RpcHandlingError
                    {
                        ErrorMessage = Error.InvalidRpcRequest
                    };
                }

                if (msgResponse.Payload == null)
                {
                    OnRequest?.Invoke(this, new EventArgs.IncomingEventArgs<TRequest> { ActorId = ActorId, Incoming = msgRequest.Payload });

                    try
                    {
                        msgResponse.Payload = Func.Invoke(msgRequest.Payload!);
                    }
                    catch (Exception e)
                    {
                        OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                        {
                            Type = EventArgs.ErrorType.Handling,
                            ActorId = ActorId,
                            Exception = e,
                            Incoming = msgRequest.Payload
                        });
                        msgResponse.Payload = new RpcHandlingError
                        {
                            ErrorMessage = e.Message
                        };
                    }
                }

                byte[] responseBytes;
                try
                {
                    responseBytes = msgResponse.ToBytes();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                    {
                        Type = EventArgs.ErrorType.Serializing,
                        ActorId = ActorId,
                        Exception = ex,
                        Incoming = msgRequest.Payload,
                        Outgoing = (TResponse)msgResponse.Payload
                    });
                    Channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                try
                {
                    var replyProps = Channel!.CreateBasicProperties();
                    replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

                    Channel.BasicPublish(exchange: string.Empty, routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps, body: responseBytes);
                    OnResponse?.Invoke(this, new EventArgs.OutgoingEventArgs<TRequest, TResponse> { ActorId = ActorId, Incoming = msgRequest.Payload, Outgoing = (TResponse)msgResponse.Payload });
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                    {
                        Type = EventArgs.ErrorType.Sending,
                        ActorId = ActorId,
                        Exception = e,
                        Incoming = msgRequest.Payload,
                        Outgoing = (TResponse)msgResponse.Payload
                    });
                }
                finally
                {
                    if (Channel!.IsOpen)
                        Channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                {
                    Type = EventArgs.ErrorType.Acking,
                    ActorId = ActorId,
                    Exception = e,
                    RawIncoming = incomingBytes,
                    Incoming = msgRequest != null ? msgRequest.Payload : default,
                    Outgoing = (TResponse)msgResponse.Payload!
                });
            }
        }

        /// <summary>
        /// Событие сигнализирует об ошибке в процессе работы обработчика.
        /// </summary>
        public event EventHandler<EventArgs.ErrorEventArgs<TRequest, TResponse>>? OnError;

        /// <summary>
        /// Событие сигнализирует о получении запроса.
        /// </summary>
        public event EventHandler<EventArgs.IncomingEventArgs<TRequest>>? OnRequest;

        /// <summary>
        /// Событие сигнализирует об отправке ответа.
        /// </summary>
        public event EventHandler<EventArgs.OutgoingEventArgs<TRequest, TResponse>>? OnResponse;

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
                foreach(var c in Consumers)
                    c.Received -= OnRecieved;

                //if (Channel!.IsOpen)
                //    Channel!.QueueDeleteNoWait(QueueName, false, false);
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}