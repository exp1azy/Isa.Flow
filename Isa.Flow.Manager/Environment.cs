using RabbitMQ.Client;

namespace Isa.Flow.Manager
{
    /// <summary>
    /// Класс, представляющий поля для подключения к серверу RabbitMq.
    /// </summary>
    public class Environment
    {
        /// <summary>
        /// RabbitMq URI.
        /// </summary>
        public static Uri RabbitUri = new("amqp://user:user@shost152:5672/");

        /// <summary>
        /// RabbitMq connection factory.
        /// </summary>
        public static ConnectionFactory RabbitConnectionFactory = new()
        {
            Uri = RabbitUri,
            ConsumerDispatchConcurrency = 10,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(10)
        };
    }
}
