using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Isa.Flow.Interact.Exceptions;
using RabbitMQ.Client.Events;
using Isa.Flow.Interact.Entities;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client.Exceptions;
using Isa.Flow.Interact.Resources;

namespace Isa.Flow.Interact
{
    /// <summary>
    /// Клиент для выполнения RPC-запросов.
    /// </summary>
    public class RpcClient : BaseHandler
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="actorId">Идентификатор актора.</param>
        /// <param name="connection">Соединение Rabbit.</param>
        /// <exception cref="ArgumentNullException">В случае, если в параметр <paramref name="connection"/> передан null.</exception>
        /// <exception cref="ArgumentException">В случае, если в параметр <paramref name="actorId"/> передана нулевая, пустая или состоящая из одних пробелов строка.</exception>
        public RpcClient(string actorId, IConnection connection)
            : base(actorId, connection)
        {
            // Устанавливаем продолжительность ожидания ответа на запрос по умолчанию.
            Timeout = 15;
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
        /// <exception cref="TimeoutException">В случае истечения времени ожидания ответа.</exception>
        /// <exception cref="SendingException">В случае ошибки отправки запроса.</exception>
        /// <exception cref="AlreadyClosedException">В случае обрыва соединения с Rabbit в процессе ожидания ответа или попытки выполнить запрос на уже закрытом соединении.</exception>
        /// <exception cref="OperationCanceledException">В случае принудительной отмены ожидания ответа через <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="RpcHandlingException">В случае если произошла ошибка обработки запроса на стороне сервера (обработчика).</exception>
        /// <exception cref="SerializationException">В случае ошибки десериализации полученого ответа.</exception>
        /// <exception cref="ValidationException">В случае ошибки валидации полученного ответа.</exception>
        public async Task<TResponse> CallAsync<TRequest, TResponse>(string requestedActorId, TRequest request, int timeout = 0, CancellationToken cancellationToken = default)
            where TRequest : IValidatableObject
            where TResponse : IValidatableObject
        {
            var responseEvent = new AsyncAutoResetEvent();
            TResponse? response = default;
            Exception? exception = null;
            EventingBasicConsumer? consumer = null;
            IModel? channel = null;
            
            var messageTTL = (timeout == 0 ? Timeout : timeout) * 1000;

            #region Отправка запроса

            try
            {
                channel = Connection.CreateModel();

                var replyQueueName = channel.QueueDeclare().QueueName;
                var correlationId = Guid.NewGuid().ToString();

                consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        if (ea.BasicProperties.CorrelationId != correlationId)
                            return;

                        var body = ea.Body.ToArray();

                        RpcHandlingError? rpcHandlingError = null;
                        try
                        {
                            var resp = Message<RpcHandlingError>.FromBytes(body);
                            resp.ThrowIfInvalid();
                            rpcHandlingError = resp.Payload;
                        }
                        catch { }

                        if (rpcHandlingError != null)
                            throw new RpcHandlingException(rpcHandlingError.ErrorMessage);

                        var deserialized = Message<TResponse>.FromBytes(body);
                        deserialized.ThrowIfInvalid();
                        response = deserialized.Payload;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    finally
                    {
                        responseEvent.Set();
                    }
                };
                channel.BasicConsume(replyQueueName, true, consumer);

                var serialized = new Message<TRequest>
                {
                    Payload = request
                };
                var body = serialized.ToBytes();

                var queueName = $"{Constant.RpcQueueNamePrefix}{requestedActorId}_{typeof(TRequest).AssemblyQualifiedName}";

                IBasicProperties props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyQueueName;
                props.Expiration = messageTTL.ToString();

                channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: props, body: body);
            }
            catch (Exception ex)
            {
                channel?.Dispose();
                throw new SendingException(Error.SendingError, ex);
            }

            #endregion

            #region Ожидание и обработка ответа

            try
            {
                var timeoutTask = Task.Delay(messageTTL, cancellationToken);
                var responseTask = responseEvent.WaitAsync(cancellationToken);
                var endTask = await Task.WhenAny(responseTask, timeoutTask);

                cancellationToken.ThrowIfCancellationRequested();

                if (ReferenceEquals(responseTask, endTask))
                {
                    if (exception != null)
                        throw exception;

                    return response!;
                }

                if (channel.CloseReason != null)
                    throw new AlreadyClosedException(channel.CloseReason);

                throw new TimeoutException();
            }
            finally
            {
                channel?.Dispose();
            }

            #endregion
        }

        /// <summary>
        /// Продолжительность ожидания ответа на запрос в секундах, после которого возникает <see cref="TimeoutException"/>.
        /// </summary>
        public int Timeout { get; set; }

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