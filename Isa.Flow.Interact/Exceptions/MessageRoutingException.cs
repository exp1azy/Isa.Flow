using Isa.Flow.Interact.Resources;
using System.Runtime.Serialization;

namespace Isa.Flow.Interact.Exceptions
{
    /// <summary>
    /// Исключение, мозникающее при попытке отправить сообщение в несуществующую очередь.
    /// </summary>
    public class MessageRoutingException : Exception
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        public MessageRoutingException() : base(Error.MessageRoutingError) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        public MessageRoutingException(string? message) : base(message) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="innerException">Исключение, ставшее причиной ошибки.</param>
        public MessageRoutingException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="info">Параметры сериализации.</param>
        /// <param name="context">Контекст сериализации.</param>
        protected MessageRoutingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}