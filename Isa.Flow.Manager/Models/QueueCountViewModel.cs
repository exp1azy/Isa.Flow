namespace Isa.Flow.Manager.Models
{
    /// <summary>
    /// Модель, представляющая количество сообщений в очередях.
    /// </summary>
    public class QueueCountViewModel
    {
        /// <summary>
        /// Свойство, хранящее количество сообщений в очереди NewAndUpdatedQueueCount.
        /// </summary>
        public int? NewAndUpdatedQueueCount { get; set; }

        /// <summary>
        /// Свойство, хранящее количество сообщений в очереди DeletedQueueCount.
        /// </summary>
        public int? DeletedQueueCount { get; set; }
    }
}
