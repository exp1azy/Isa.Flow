using Isa.Flow.VkCollector.Entities;

namespace Isa.Flow.VkCollector.EventArgs
{
    /// <summary>
    /// Параметры события успешного окончания сбора сообщений с отдельнеого канала.
    /// </summary>
    public class ChannelSuccessEventArg : System.EventArgs
    {
        /// <summary>
        /// Информация о канале.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// Кол-во сообщений, собраных за последнюю итерацию.
        /// </summary>
        public int Messages { get; set; }
    }
}
