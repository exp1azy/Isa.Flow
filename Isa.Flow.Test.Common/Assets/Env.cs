using RabbitMQ.Client;

namespace Isa.Flow.Test.Common
{
    public static class Env
    {
        public static Uri RabbitUri = new("amqp://user:user@shost152:5672/");

        public static ConnectionFactory RabbitConnectionFactory = new()
        {
            Uri = RabbitUri,
            ConsumerDispatchConcurrency = 10,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(10)
        };

        public static string SqlServerConnection = "Data Source=SHOST152;Initial Catalog=trend-test;Persist Security Info=True;User ID=trend-admin;Password=Trend2017!;TrustServerCertificate=true;Command Timeout=300";
    }
}