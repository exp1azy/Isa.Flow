namespace Isa.Flow.Manager.Models
{
    /// <summary>
    /// Модель, представляющая названия очередей. 
    /// </summary>
    public class QueueViewModel
    {
        /// <summary>
        /// Свойство, хранящее название очереди NewAndUpdatedQueueName.
        /// </summary>
        public string NewAndUpdatedQueueName { get; set; }

        /// <summary>
        /// Свойство, хранящее название очереди DeletedQueueName.
        /// </summary>
        public string DeletedQueueName { get; set; }
    }
}
