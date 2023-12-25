using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EventArgs
{
    /// <summary>
    /// Событие, сигнализирующее об отправке исходящего сообщения.
    /// </summary>
    /// <typeparam name="TIncoming">Тип объекта, представляющего входящее сообщение, в ответ на которое было отправлено исходящее.</typeparam>
    /// <typeparam name="TOutgoing">Тип объекта, представляющего исходящее сообщение.</typeparam>
    public class OutgoingEventArgs<TIncoming, TOutgoing> : System.EventArgs
        where TIncoming  :IValidatableObject
        where TOutgoing :IValidatableObject
    {
        /// <summary>
        /// Идентификатор актора.
        /// </summary>
        public string? ActorId { get; set; }

        /// <summary>
        /// Входящее сообщение, в ответ на которое было отправлено исходящее.
        /// </summary>
        public TIncoming? Incoming { get; set; }

        /// <summary>
        /// Исходящее сообщение.
        /// </summary>
        public TOutgoing? Outgoing { get; set;}
    }
}