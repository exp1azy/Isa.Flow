using Isa.Flow.TelegramCollector.Entities;

namespace Isa.Flow.TelegramCollector.EventArgs
{
    /// <summary>
    /// Параметры события остановки процесса автоматической сборки.
    /// </summary>
    public class StoppedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Информация о канале, на котором произошла остановка.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// Исключение, ставшее причиной остановки. Если остановка произошла в штатном режиме - null.
        /// </summary>
        public Exception? Exception { get; set; }
    }
}