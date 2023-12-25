using Isa.Flow.VkCollector.Entities;

namespace Isa.Flow.VkCollector.EventArgs
{
    /// <summary>
    /// Параметры события начала сбора сообщений с отдельного канала.
    /// </summary>
    public class ChannelStartedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Информация о канале.
        /// </summary>
        public Channel Channel { get; set; }
    }
}
