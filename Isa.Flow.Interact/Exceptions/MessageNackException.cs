using Isa.Flow.Interact.Resources;
using System.Runtime.Serialization;

namespace Isa.Flow.Interact.Exceptions
{
    /// <summary>
    /// Исключение, возникающее при отсутствии подтверждения отправки сообщения в очередь.
    /// </summary>
    public class MessageNackException : Exception
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        public MessageNackException() : base(Error.MessageNackError) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        public MessageNackException(string? message) : base(message) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="innerException">Исключение, ставшее причиной ошибки.</param>
        public MessageNackException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="info">Параметры сериализации.</param>
        /// <param name="context">Контекст сериализации.</param>
        protected MessageNackException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}