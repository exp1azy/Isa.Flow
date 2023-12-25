using Isa.Flow.Interact;
using Isa.Flow.Interact.EsIndexer;
using Isa.Flow.Test.Common;

namespace IsaIndexerTest
{
    //[Ignore]
    [TestClass]
    public class IndexerManualTests
    {
        [TestMethod]
        public void StopTest()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StopEsIndexRequest, EsIndexResponse>(
                "EsIndexer",
                new StopEsIndexRequest()
            );

            rpcTask.Wait();
            ;
        }

        [TestMethod]
        public void StartTest()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StartEsIndexRequest, EsIndexResponse>(
                "EsIndexer",
                new StartEsIndexRequest()
            );

            rpcTask.Wait();
            ;
        }

        [TestMethod]
        public void StatusTest()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<IndexerCurrentStateRequest, IndexerCurrentStateResponse>(
                "EsIndexer",
                new IndexerCurrentStateRequest()
            );

            rpcTask.Wait();
            ;
        }
    }
}