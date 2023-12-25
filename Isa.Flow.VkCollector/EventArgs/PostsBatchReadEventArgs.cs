using Isa.Flow.VkCollector.Entities;

namespace Isa.Flow.VkCollector.EventArgs
{
    /// <summary>
    /// Параметры события попытки чтения новых сообщений с канала.
    /// </summary>
    public class PostsBatchReadEventArgs : System.EventArgs
    {
        /// <summary>
        /// Информация о канале.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// Количество прочитанных сообщений.
        /// </summary>
        public int Count { get; set; }
    }
}
