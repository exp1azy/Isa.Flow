using Isa.Flow.TelegramCollector.Entities;

namespace Isa.Flow.TelegramCollector.EventArgs
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