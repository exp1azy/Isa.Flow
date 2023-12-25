using System.Diagnostics;
using System.Management;
using System.Reflection.Metadata;
using Isa.Flow.Interact.Exceptions;
using Isa.Flow.Test.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Isa.Flow.Interact.Test
{
    [TestClass]
    public class RpcTests
    {
        private readonly string RequestedActorId = "2";

        [TestInitialize()]
        public void Initialize()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDelete($"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(Numbers).AssemblyQualifiedName}", false, false);
        }

        [TestMethod]
        [Timeout(30000)]
        public void SuccessRpcTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var requests = new List<object>();
            var responses = new List<object>();
            var errors = new List<object>();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 100;            

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req => new Result() { ResultNumber = req.FirstNumber * req.SecondNumber });
                handler.OnRequest += (s, ea) => requests.Add(ea);
                handler.OnResponse += (s, ea) => responses.Add(ea);
                handler.OnError += (s, ea) => errors.Add(ea);
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();            

            using var rpcClient = new RpcClient("1", connection);

            var request = new Numbers()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var task = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
            task.Wait();
            var response = task.Result;

            endEvent.Set();

            handlerTask.Wait();

            Assert.AreEqual(response.ResultNumber, request.FirstNumber * request.SecondNumber);
            Assert.AreEqual(errors.Count, 0);
            Assert.AreEqual(requests.Count, 1);
            Assert.AreEqual(responses.Count, 1);
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(TimeoutException))]
        public void TimeoutRpcTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 10;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay((timeout + 10) * 1000).Wait();
                    return new Result() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            Exception? resultException = null;

            using (var rpcClient = new RpcClient("1", connection))
            {

                var request = new Numbers()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };

                Exception? ex = null;
                var task = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                try
                {
                    task.Wait();
                }
                catch (Exception e)
                {
                    resultException = e.InnerException;
                }

                endEvent.Set();

                handlerTask.Wait();
            }

            if (resultException != null)
                throw resultException;
        }

        [TestMethod]
        [Timeout(30000)]
        public void ParallelRpcTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 20;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 2, req =>
                {
                    Task.Delay(5000).Wait();

                    return new Result() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });

                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using (var rpcClient = new RpcClient("1", connection))
            {
                var request1 = new Numbers()
                {
                    FirstNumber = 1,
                    SecondNumber = 3
                };
                var request2 = new Numbers()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };
               
                var watch = new Stopwatch();
                watch.Start();

                var task1 = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request1, timeout: timeout);
                var task2 = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request2, timeout: timeout);
                Task.WaitAll(task1, task2);

                watch.Stop();

                Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(10));

                endEvent.Set();
            }

            handlerTask.Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(RpcHandlingException))]
        public void ValidateRpcTest()
        {
            Exception? resultException = null;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 30;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req => new Result() { ResultNumber = req.FirstNumber * req.SecondNumber });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using (var rpcClient = new RpcClient("1", connection))
            {

                var request = new Numbers()
                {
                    FirstNumber = 20,
                    SecondNumber = 3
                };

                var task = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                try
                {
                    task.Wait();
                }
                catch (Exception ex)
                {
                    resultException = ex.InnerException;
                }
            }

            endEvent.Set();

            handlerTask.Wait();

            if (resultException != null)
                throw resultException;
        }

        [TestMethod]
        [Timeout(30000)]
        public void CancelRpcTest()
        {
            Task<Result>? rpcCallTask = null;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 30;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                
                using var _ = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay(TimeSpan.FromSeconds(15)).Wait();
                    return new Result() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();
                
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using (var rpcClient = new RpcClient("1", connection))
            {

                var request = new Numbers()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                rpcCallTask = rpcClient.CallAsync<Numbers, Result>(RequestedActorId, request, timeout, cts.Token);
                try
                {
                    rpcCallTask.Wait();
                }
                catch { }
                endEvent.Set();
            }

            handlerTask.Wait();

            Assert.IsTrue(rpcCallTask.IsCanceled);
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(AlreadyClosedException))]
        public void RpcClientConnectionDisposedWhileHandlingTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 10;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay(5000).Wait();
                    return new Result() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var rpcClient = new RpcClient("1", connection);

            var request = new Numbers()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };
           
            var task = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
            connection.Dispose();

            Exception? ex = null;

            try
            {
                task.Wait();
                var response = task.Result;
            }
            catch (Exception e)
            {
                ex = e.InnerException;
            }

            endEvent.Set();
            handlerTask.Wait();

            throw ex;
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(SendingException))]
        public void RpcClientConnectionDisposedBeforeCallTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 10;

            var handlerTask = Task.Run(async () =>
            {
                await Task.Delay(5000);

                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req => new Result() { ResultNumber = req.FirstNumber * req.SecondNumber });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var rpcClient = new RpcClient("1", connection);

            var request = new Numbers()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            connection.Dispose();
                        
            Exception? ex = null;

            try
            {
                var task = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                task.Wait();
                var response = task.Result;
            }
            catch (Exception e)
            {
                ex = e.InnerException;
            }

            endEvent.Set();
            handlerTask.Wait();

            Assert.IsInstanceOfType<ObjectDisposedException>(ex.InnerException);
            throw ex;
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(AlreadyClosedException))]
        public void RpcHandlerConnectionDisposedWhileHandlingTest()
        {
            IConnection handlerConnection = null;
            Exception handlerException = null;
            Exception clientException = null;
            EventArgs.ErrorType errorType = EventArgs.ErrorType.Unknown;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 10;

            var handlerTask = Task.Run(() =>
            {
                handlerConnection = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<Numbers, Result>(RequestedActorId, handlerConnection, 1, req =>
                {
                    msgEvent.Set();
                    Task.Delay(5000).Wait();
                    return new Result() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                handler.OnError += (s, ea) =>
                {
                    handlerException = ea.Exception;
                    errorType = ea.Type;
                };

                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var rpcClient = new RpcClient("1", connection);
            var request = new Numbers()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };
            var callTask = rpcClient.CallAsync<Numbers, Result>(RequestedActorId, request, timeout);
            msgEvent.WaitOne();
            handlerConnection.Dispose();

            try
            {
                callTask.Wait();
            }
            catch (Exception e)
            {
                clientException = e.InnerException;
            }

            endEvent.Set();
            handlerTask.Wait();

            Assert.IsInstanceOfType(clientException, typeof(TimeoutException));
            Assert.AreEqual(errorType, EventArgs.ErrorType.Sending);
            if (handlerException != null)
                throw handlerException;
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(ArgumentException))]
        public void CreationRpcHandlerWithClosedConnectionTest()
        {
            using var conn = Env.RabbitConnectionFactory.CreateConnection();
            conn.Dispose();
            using var _ = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req => new Result() { ResultNumber = req.FirstNumber * req.SecondNumber });
        }

        [TestMethod]
        [Timeout(30000)]
        public void CreationRpcHandlerWithNonEmptyQueueTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 15;

            using (var connection = Env.RabbitConnectionFactory.CreateConnection())
            {
                using var channel = connection.CreateModel();
                channel.QueueDeclare(queue: $"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(Numbers).AssemblyQualifiedName}", durable: false, exclusive: false, autoDelete: false, arguments: null);

                using var rpcClient = new RpcClient("1", connection);

                var request = new Numbers()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };
                var callTask = rpcClient.CallAsync<Numbers, Result>(RequestedActorId, request, timeout);
                
                Assert.IsFalse(callTask.Wait(TimeSpan.FromSeconds(1)));                
            }

            var handlerTask = Task.Delay(2000).ContinueWith(_ =>
            {
                using var handlerConnection = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<Numbers, Result>(RequestedActorId, handlerConnection, 1, req =>
                {
                    msgEvent.Set();
                    return new Result() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                endEvent.WaitOne();
            });           

            msgEvent.WaitOne();
            endEvent.Set();
            handlerTask.Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void RpcCallExpirationTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 10;

            using (var connection = Env.RabbitConnectionFactory.CreateConnection())
            {
                using var channel = connection.CreateModel();
                channel.QueueDeclare(queue: $"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(Numbers).AssemblyQualifiedName}", durable: false, exclusive: false, autoDelete: false, arguments: null);

                using var rpcClient = new RpcClient("1", connection);

                var request = new Numbers()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };
                var callTask = rpcClient.CallAsync<Numbers, Result>(RequestedActorId, request, timeout);

                Assert.IsFalse(callTask.Wait(TimeSpan.FromSeconds(1)));
            }

            var handlerTask = Task.Delay(TimeSpan.FromSeconds(11)).ContinueWith(_ =>
            {
                using var handlerConnection = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<Numbers, Result>(RequestedActorId, handlerConnection, 1, req =>
                {
                    msgEvent.Set();
                    return new Result() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                endEvent.WaitOne();
            });

            Assert.IsFalse(msgEvent.WaitOne(TimeSpan.FromSeconds(20)));
            endEvent.Set();
            handlerTask.Wait();
        }

        /// <summary>
        /// Этот тест требует ручного отключения / включения сети.
        /// Запускать только в режиме отладка, закоментировав атрибут [Ignore]
        /// и расставив точки останова в местах включения / выключения сети.
        /// </summary>
        [Ignore]
        [TestMethod]
        public void SuccessRpcAfterConnectionRecoveryTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var stoppedEven = new AutoResetEvent(false);
            var needToStartedSwitchOnEvent = new AutoResetEvent(false);
            var startedEven = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 100;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<Numbers, Result>(RequestedActorId, conn, 1, req => new Result() { ResultNumber = req.FirstNumber * req.SecondNumber });

                handler.Stopped += (s, ev) => stoppedEven.Set();

                readyEvent.Set();
                needToStartedSwitchOnEvent.WaitOne();
                handler.Started += (s, ev) => startedEven.Set();

                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var rpcClient = new RpcClient("1", connection);

            var request = new Numbers()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var task = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
            task.Wait();
            var response = task.Result;

            needToStartedSwitchOnEvent.Set();

            //тут нужно вручную отключить сеть
            ;
            stoppedEven.WaitOne();

            try
            {
                var lostRequest = new Numbers()
                {
                    FirstNumber = 4,
                    SecondNumber = 6
                };

                var lostTask = rpcClient.CallAsync<Numbers, Result>(RequestedActorId, lostRequest, timeout);
                lostTask.Wait();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType<SendingException>(ex.InnerException);
                Assert.IsInstanceOfType<AlreadyClosedException>(ex.InnerException.InnerException);
            }

            //тут нужно вручную включить сеть
            ;
            startedEven.WaitOne();

            var request1 = new Numbers()
            {
                FirstNumber = 3,
                SecondNumber = 9
            };
            var task1 = rpcClient.CallAsync<Numbers, Result>(requestedActorId: RequestedActorId, request: request1, timeout: timeout);
            task1.Wait();
            var response1 = task1.Result;

            endEvent.Set();

            Assert.AreEqual(response.ResultNumber, request.FirstNumber * request.SecondNumber);
            Assert.AreEqual(response1.ResultNumber, request1.FirstNumber * request1.SecondNumber);

            handlerTask.Wait();
        }
    }
}