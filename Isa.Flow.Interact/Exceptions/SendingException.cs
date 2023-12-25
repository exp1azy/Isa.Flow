using Isa.Flow.Interact.Resources;
using System.Runtime.Serialization;

namespace Isa.Flow.Interact.Exceptions
{
    /// <summary>
    /// Исключение, возникающее в случае ошибки отправки сообщения.
    /// </summary>
    public class SendingException : Exception
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        public SendingException() : base(Error.SendingError) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        public SendingException(string? message) : base(message) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="innerException">Исключение, ставшее причиной ошибки.</param>
        public SendingException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="info">Параметры сериализации.</param>
        /// <param name="context">Контекст сериализации.</param>
        protected SendingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}