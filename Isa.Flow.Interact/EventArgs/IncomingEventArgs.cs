using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EventArgs
{
    /// <summary>
    /// Параметры события, сигнализируещего о новом входящем сообщении.
    /// </summary>
    /// <typeparam name="TIncoming">Тип объекта, представляющего входящее сообщение.</typeparam>
    public class IncomingEventArgs<TIncoming> : System.EventArgs
        where TIncoming : IValidatableObject
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
        /// Входящее сообщение.
        /// </summary>
        public TIncoming? Incoming { get; set; }
    }
}