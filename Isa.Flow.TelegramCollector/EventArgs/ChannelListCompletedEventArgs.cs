namespace Isa.Flow.TelegramCollector.EventArgs
{
    /// <summary>
    /// Параметры события окончания обработки списка каналов.
    /// </summary>
    public class ChannelListCompletedEventArgs
    {
        /// <summary>
        /// Количество обработанных каналов.
        /// </summary>
        public int Count { get; set; }
    }
}