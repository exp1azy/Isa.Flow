using Isa.Flow.Interact.Resources;

namespace Isa.Flow.Interact.Extensions
{
    /// <summary>
    /// Методы расширения для объктов типа <see cref="string"/>.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Метод проверки корректности имени очереди.
        /// </summary>
        /// <param name="queueName">Имя очереди для проверки.</param>
        /// <exception cref="ArgumentException">В случае, если имя очереди недопустимо.</exception>
        public static void ThrowIfInvalidQueueName(this string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException(Error.QueueNameCannotBeNullEmptyOrBlank, nameof(queueName));

            if (queueName.StartsWith(Constant.RpcQueueNamePrefix) ||
                queueName.StartsWith(Constant.BroadcastExchangeNamePrefix) ||
                queueName == Constant.WhoAliveExchangeName)
                throw new ArgumentException(Error.QueueNameError, nameof(queueName));
        }
    }
}