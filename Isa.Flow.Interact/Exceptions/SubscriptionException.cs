using Isa.Flow.Interact.Resources;
using System.Runtime.Serialization;

namespace Isa.Flow.Interact.Exceptions
{
    /// <summary>
    /// Исключение, возникающее в случае ошибки при подписке на какие-либо сообщения.
    /// </summary>
    public class SubscriptionException : Exception
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        public SubscriptionException() : base(Error.SubscriptionError) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        public SubscriptionException(string? message) : base(message) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="innerException">Исключение, ставшее причиной ошибки.</param>
        public SubscriptionException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="info">Параметры сериализации.</param>
        /// <param name="context">Контекст сериализации.</param>
        protected SubscriptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}