using Isa.Flow.Interact.Resources;

namespace Isa.Flow.Interact.Exceptions
{
    /// <summary>
    /// Исключение, выбрасываемое в процессе запуска актора в случае обнаружения другого актора с тем же идентификатором.
    /// </summary>
    public class ActorAlreadyStartedException : Exception
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        public ActorAlreadyStartedException() : base(Error.ActorAlreadyStartedError) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        public ActorAlreadyStartedException(string? message) : base(message) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="innerException">Внутреннее исключение.</param>
        public ActorAlreadyStartedException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}