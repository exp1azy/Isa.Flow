using Isa.Flow.Interact.Resources;
using System.Runtime.Serialization;

namespace Isa.Flow.Interact.Exceptions
{
    /// <summary>
    /// Исключение, представляющее ошибку обработки RPC-запроса.
    /// </summary>
    public class RpcHandlingException : Exception
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        public RpcHandlingException() : base(Error.RpcHandlingError) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        public RpcHandlingException(string? message) : base(message) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="innerException">Исключение, ставшее причиной данной ошибки.</param>
        public RpcHandlingException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="info">Контекст сериализации.</param>
        /// <param name="context">Контекст сохранения.</param>
        protected RpcHandlingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}