using Isa.Flow.VkCollector.Entities;

namespace Isa.Flow.VkCollector.EventArgs
{
    /// <summary>
    /// Параметры события аварийного окончания процесса сбора сообщений с отдельного канала.
    /// </summary>
    public class ChannelErrorEventArgs : System.EventArgs
    {
        /// <summary>
        /// Информация о канале.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// Исключение, ставшее причиной аварийной остановки.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
