using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EventArgs
{
    /// <summary>
    /// Параметры события, сигнализирующего об ошибке при работе обработчика Rabbit.
    /// </summary>
    /// <typeparam name="TIncoming">Тип объекта, представляющего входящий объект.</typeparam>
    /// <typeparam name="TOutgoing">Тип объекта, представляющего исходящий объект.</typeparam>
    public class ErrorEventArgs<TIncoming, TOutgoing> : System.EventArgs
        where TIncoming : IValidatableObject
        where TOutgoing : IValidatableObject
    {
        /// <summary>
        /// Идентификатор актора.
        /// </summary>
        public string? ActorId { get; set; }

        /// <summary>
        /// Идентификатор контрактора.
        /// Может содержать идентификатор актора-транслятора широковещательных сообщений или имя очереди.
        /// </summary>
        public string? ContrActorId { get; set; }

        /// <summary>
        /// Тип ошибки.
        /// </summary>
        public ErrorType Type { get; set; }

        /// <summary>
        /// Исключение, ставшее причиной ошибки.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// "Сырое" (не прошедшее десериализацию) входящее сообщение.
        /// </summary>
        public byte[]? RawIncoming { get; set; }

        /// <summary>
        /// Входящее сообщение.
        /// </summary>
        public TIncoming? Incoming { get; set; }

        /// <summary>
        /// Исходящее сообщение.
        /// </summary>
        public TOutgoing? Outgoing { get; set; }
    }

    /// <summary>
    /// Типы ошибок при работе обработчика RPC.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Неизвестный тип.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Ошибка десериализации входящего сообщения.
        /// </summary>
        Deserializing,

        /// <summary>
        /// Ошибка валидации входящего сообщения.
        /// </summary>
        Validating,

        /// <summary>
        /// Ошибка обработки сообщения.
        /// </summary>
        Handling,

        /// <summary>
        /// Ошибка сериализации исходящего сообщения.
        /// </summary>
        Serializing,

        /// <summary>
        /// Ошибка отправки исходящего сообщения.
        /// </summary>
        Sending,

        /// <summary>
        /// Ошибка при отправке подтверждения обработки сообщения.
        /// </summary>
        Acking
    }
}