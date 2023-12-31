﻿namespace Isa.Flow.TelegramCollector.EventArgs
{
    /// <summary>
    /// Параметры события начала обработки списка каналов.
    /// </summary>
    public class ChannelListStartedEventArgs
    {
        /// <summary>
        /// Количество каналов в списке.
        /// </summary>
        public int Count { get; set; }
    }
}