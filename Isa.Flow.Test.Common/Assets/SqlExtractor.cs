namespace Isa.Flow.Test.Common.Assets
{
    public class SqlExtractor
    {
        public ConnectionStrings ConnectionStrings { get; set; }

        public RabbitMq RabbitMq { get; set; }

        public string ActorId { get; set; }
    }

    public class ConnectionStrings
    {
        public string SqlServerConnection { get; set; }
    }

    public class RabbitMq
    {
        public string Uri { get; set; }
    }
}
