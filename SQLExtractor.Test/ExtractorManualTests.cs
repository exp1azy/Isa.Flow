using Isa.Flow.Interact.Extractor.Rpc;
using Isa.Flow.Interact;
using Isa.Flow.Test.Common;

namespace SQLExtractor.Test
{
    [Ignore]
    [TestClass]
    public class ExtractorManualTests
    {
        [TestMethod]
        public void StopNewArticlesExtraction()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StopSqlExtractionRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new StopSqlExtractionRequest { Func = SqlExtractionFunc.New }
            );

            rpcTask.Wait();
        }

        [TestMethod]
        public void StartNewArticlesExtraction()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StartSqlExtractionRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new StartSqlExtractionRequest { Func = SqlExtractionFunc.New }
            );

            rpcTask.Wait();
        }


        [TestMethod]
        public void StopUpdatedArticlesExtraction()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StopSqlExtractionRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new StopSqlExtractionRequest { Func = SqlExtractionFunc.Updated }
            );

            rpcTask.Wait();
        }

        [TestMethod]
        public void StartUpdatedArticlesExtraction()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StartSqlExtractionRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new StartSqlExtractionRequest { Func = SqlExtractionFunc.Updated }
            );

            try
            {
                rpcTask.Wait();
            }
            catch (Exception ex)
            {
                ;
            }
            ;
        }

        [TestMethod]
        public void StopDeletedArticlesExtraction()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StopSqlExtractionRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new StopSqlExtractionRequest { Func = SqlExtractionFunc.Deleted }
            );

            try
            {
                rpcTask.Wait();
            }
            catch (Exception ex)
            {
                ;
            }
            ;
        }

        [TestMethod]
        public void StartDeletedArticlesExtraction()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<StartSqlExtractionRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new StartSqlExtractionRequest { Func = SqlExtractionFunc.Deleted }
            );

            try
            {
                rpcTask.Wait();
            }
            catch (Exception ex)
            {
                ;
            }
            ;
        }

        [TestMethod]
        public void Reindex()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<IntervalRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new IntervalRequest { Func = SqlExtractionFunc.Reindex, From = 90000000, To = 95935000 }
            );

            try
            {
                rpcTask.Wait();
            }
            catch (Exception ex)
            {
                ;
            }
            ;
        }

        [TestMethod]
        public void Clean()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var rpcClient = new RpcClient(Guid.NewGuid().ToString(), connection) { Timeout = 30 };

            var rpcTask = rpcClient.CallAsync<IntervalRequest, SqlExtractionResponse>(
                "SQLExtractor",
                new IntervalRequest { Func = SqlExtractionFunc.Clean, From = 9200000, To = 9300000 }
            );

            try
            {
                rpcTask.Wait();
            }
            catch (Exception ex)
            {
                ;
            }
            ;
        }
    }
}