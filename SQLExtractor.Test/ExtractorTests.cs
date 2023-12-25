using Isa.Flow.Interact;
using Isa.Flow.Interact.Entities;
using Isa.Flow.SQLExtractor;
using Isa.Flow.SQLExtractor.Data;
using Isa.Flow.Interact.Extractor.Rpc;
using Isa.Flow.Test.Common;
using Isa.Flow.Test.Common.Assets;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;

namespace SQLExtractor.Test
{
    [TestClass]
    public class ExtractorTests
    {
        private DataContext _dataContext;

        private FuncParams _sqlexObject;

        private SqlExtractor _sqlexConfObject;

        [TestInitialize()]
        public void Initialize()
        {
            string connectionString = "Data Source=SHOST152;Initial Catalog=trend-test;Persist Security Info=True;User ID=trend-admin;Password=Trend2017!;TrustServerCertificate=true;Command Timeout=300";

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ConnectionStrings:SqlServerConnection", connectionString}
                }).Build();

            _dataContext = new DataContext(configuration);

            var updatedArticles = _dataContext.UpdatedArticles.ToList();
            var deletedArticles = _dataContext.DeletedArticles.ToList();
            var articleIds = _dataContext.Articles.Select(a => a.Id).ToList();

            if (updatedArticles.Count < 50)
            {
                int count = 0;

                while (count < 50)
                {
                    if (count < 25)
                    {
                        _dataContext.UpdatedArticles.Add(new UpdatedArticle
                        {
                            ArticleId = new Random().Next(300000, 400000)
                        });
                    }                       
                    else
                    {
                        int randomIndex = new Random().Next(1, articleIds.Count);

                        _dataContext.UpdatedArticles.Add(new UpdatedArticle
                        {
                            ArticleId = articleIds[randomIndex]
                        });
                    }

                    count++;
                }

                _dataContext.SaveChanges();
            }

            if (deletedArticles.Count < 50)
            {
                int count = 0;

                while (count < 50)
                {
                    if (count < 25)
                    {
                        _dataContext.DeletedArticles.Add(new DeletedArticle
                        {
                            ArticleId = new Random().Next(300000, 400000)
                        });
                    }
                    else
                    {
                        int randomIndex = new Random().Next(1, articleIds.Count);

                        _dataContext.DeletedArticles.Add(new DeletedArticle
                        {
                            ArticleId = articleIds[randomIndex]
                        });
                    }

                    count++;
                }

                _dataContext.SaveChanges();
            }

            DeserializeJson();

            Process[] processes = Process.GetProcessesByName("Isa.Flow.SQLExtractor");

            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch { }
                }
            }

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDelete($"{Constant.RpcQueueNamePrefix}{_sqlexConfObject.ActorId}_{typeof(Ping).AssemblyQualifiedName}", false, false);
            channel.QueueDelete($"{Constant.RpcQueueNamePrefix}{_sqlexConfObject.ActorId}_{typeof(StartSqlExtractionRequest).AssemblyQualifiedName}", false, false);
            channel.QueueDelete($"{Constant.RpcQueueNamePrefix}{_sqlexConfObject.ActorId}_{typeof(StopSqlExtractionRequest).AssemblyQualifiedName}", false, false);
            channel.QueueDelete($"{Constant.RpcQueueNamePrefix}{_sqlexConfObject.ActorId}_{typeof(FuncStatusRequest).AssemblyQualifiedName}", false, false);
            channel.QueueDelete($"{_sqlexObject.NewArticleQueueName}");
            channel.QueueDelete($"{_sqlexObject.UpdatedArticleQueueName}");
            channel.QueueDelete($"{_sqlexObject.DeletedArticleQueueName}");
        }

        [TestMethod]
        [Timeout(45000)]
        public void StartProcessWithRpc()
        {
            var process = new Process();

            process.StartInfo.FileName = "Isa.Flow.SQLExtractor.exe";
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.Start();

            using (var connection = Env.RabbitConnectionFactory.CreateConnection())
            {
                CheckExistingQueues(connection, $"{Constant.RpcQueueNamePrefix}{_sqlexConfObject.ActorId}_{typeof(Ping).AssemblyQualifiedName}",
                    $"{Constant.RpcQueueNamePrefix}{_sqlexConfObject.ActorId}_{typeof(StartSqlExtractionRequest).AssemblyQualifiedName}",
                    $"{Constant.RpcQueueNamePrefix}{_sqlexConfObject.ActorId}_{typeof(StopSqlExtractionRequest).AssemblyQualifiedName}");

                var rpcClient = new RpcClient(_sqlexConfObject.ActorId, connection) { Timeout = 120 };
                rpcClient.CallAsync<StartSqlExtractionRequest, SqlExtractionResponse>(_sqlexConfObject.ActorId, new StartSqlExtractionRequest
                {
                    BatchSize = 50,
                    Func = SqlExtractionFunc.New,
                    IterationTimeout = _sqlexObject.NewArticleIterationTimeout,
                    LastArticleId = _sqlexObject.LastArticleId
                }).Wait();

                CheckExistingQueues(connection, $"{_sqlexObject.NewArticleQueueName}");

                rpcClient.CallAsync<StartSqlExtractionRequest, SqlExtractionResponse>(_sqlexConfObject.ActorId, new StartSqlExtractionRequest
                {
                    BatchSize = 50,
                    Func = SqlExtractionFunc.Updated,
                    IterationTimeout = _sqlexObject.UpdatedArticleIterationTimeout,
                    LastArticleId = _sqlexObject.LastArticleId
                }).Wait();

                CheckExistingQueues(connection, $"{_sqlexObject.UpdatedArticleQueueName}");

                rpcClient.CallAsync<StartSqlExtractionRequest, SqlExtractionResponse>(_sqlexConfObject.ActorId, new StartSqlExtractionRequest
                {
                    BatchSize = 50,
                    Func = SqlExtractionFunc.Deleted,
                    IterationTimeout = _sqlexObject.DeletedArticleIterationTimeout,
                    LastArticleId = _sqlexObject.LastArticleId
                }).Wait();

                CheckExistingQueues(connection, $"{_sqlexObject.DeletedArticleQueueName}");

                Task.Delay(10000).Wait();
                Assert.IsTrue(CheckForEmptyTables());

                rpcClient.CallAsync<StopSqlExtractionRequest, SqlExtractionResponse>(_sqlexConfObject.ActorId, new StopSqlExtractionRequest
                {
                    Func = SqlExtractionFunc.New
                }).Wait();

                rpcClient.CallAsync<StopSqlExtractionRequest, SqlExtractionResponse>(_sqlexConfObject.ActorId, new StopSqlExtractionRequest
                {
                    Func = SqlExtractionFunc.Updated
                }).Wait();

                rpcClient.CallAsync<StopSqlExtractionRequest, SqlExtractionResponse>(_sqlexConfObject.ActorId, new StopSqlExtractionRequest
                {
                    Func = SqlExtractionFunc.Deleted
                }).Wait();

                var resp = rpcClient.CallAsync<FuncStatusRequest, FuncStateResponse>(_sqlexConfObject.ActorId, new FuncStatusRequest
                {
                    DateTime = DateTime.Now
                }).GetAwaiter().GetResult();

                Assert.IsFalse(resp.NewState);
                Assert.IsFalse(resp.ModifiedState);
                Assert.IsFalse(resp.DeletedState);
            }
                
            process.Kill();
            process.WaitForExit();
        }

        private void DeserializeJson()
        {
            string json = File.ReadAllText("sqlextractor.json");
            string confJson = File.ReadAllText("sqlextractor.conf.json");

            _sqlexObject = JsonConvert.DeserializeObject<FuncParams>(json)!;
            _sqlexConfObject = JsonConvert.DeserializeObject<SqlExtractor>(confJson)!;
        }

        private bool CheckForEmptyTables()
        {
            var checkUpdated = _dataContext.UpdatedArticles.ToList().Count;
            var checkDeleted = _dataContext.DeletedArticles.ToList().Count;

            return checkUpdated == 0 && checkDeleted == 0;
        }

        private void CheckExistingQueues(IConnection connection, params string[] queueNames)
        {
            Exception? ex = null;

            foreach (var queue in queueNames)
            {
                do
                {
                    try
                    {
                        using (var channel = connection.CreateModel())
                        {
                            var queueOk = channel.QueueDeclarePassive(queue);
                            ex = null;
                        }                          
                    }
                    catch (OperationInterruptedException e)
                    {
                        ex = e;
                    }
                }
                while (ex is OperationInterruptedException);
            }
        }
    }
}
