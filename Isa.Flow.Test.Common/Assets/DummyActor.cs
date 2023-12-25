using RabbitMQ.Client;
using Isa.Flow.Interact;

namespace Isa.Flow.Test.Common
{
    public class DummyActor : BaseActor
    {
        public DummyActor(ConnectionFactory connectionFactory, string? actorId = null) : base(connectionFactory, actorId)
        {
        }
    }
}
