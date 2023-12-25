using Isa.Flow.Interact.Exceptions;
using Isa.Flow.Test.Common;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Isa.Flow.Interact.Test
{
    [TestClass]
    public class QueueTests
    {
        static readonly string TestQueueName = "queue1";

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(MessageRoutingException))]
        public void EmittToNonExistentQueueTest()
        {
            try
            {
                using var connection = Env.RabbitConnectionFactory.CreateConnection();

                using var emitter = new Emitter("1", connection);
                
                emitter.Enqueue(TestQueueName, new Message
                {
                    Text = "confirm"
                });
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(MessageNackException))]
        public void EmittToFullLimitQueueTest()
        {
            try
            {
                using var connection = Env.RabbitConnectionFactory.CreateConnection();

                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(TestQueueName, true, false, false, new Dictionary<string, object>() { { "x-max-length", 1 }, { "x-overflow", "reject-publish" } });
                    channel.BasicPublish(string.Empty, TestQueueName, true, null, new byte[] { 1 });
                }

                using var emitter = new Emitter("1", connection);
                
                emitter.Enqueue(TestQueueName, new Message
                {
                    Text = "confirm"
                });
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void SuccessfulEmittTest()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            using var emitter = new Emitter("1", connection);
            
            emitter.DeclareQueue(TestQueueName, 2);

            emitter.Enqueue(TestQueueName, new Message
            {
                Text = "confirm"
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public void SuccessfulQueueTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);

            var incomings = new List<object>();
            var handled = new List<object>();
            var errors = new List<object>();

            string? msg = null;

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 10);

                var handlerTask = Task.Run(() =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();
                    using var handler = new QueueHandler<Message>("1", conn, TestQueueName, 5, q =>
                    {
                        msg = q.Text;
                        msgEvent.Set();
                    });
                    handler.OnIncoming += (s, ea) => incomings.Add(ea);
                    handler.OnHandled += (s, ea) => handled.Add(ea);
                    handler.OnError += (s, ea) => errors.Add(ea);

                    readyEvent.Set();
                    endEvent.WaitOne();
                });

                readyEvent.WaitOne();

                emitter.Enqueue(TestQueueName, new Message
                {
                    Text = "confirm"
                });
                msgEvent.WaitOne();
                endEvent.Set();

                handlerTask.Wait();
            }

            Assert.IsTrue(msg == "confirm");
            Assert.AreEqual(errors.Count, 0);
            Assert.AreEqual(incomings.Count, 1);
            Assert.AreEqual(handled.Count, 1);
        }

        [TestMethod]
        [Timeout(30000)]
        public void MultiMessageTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var msg = new ConcurrentBag<string?>();
            int counter = 100;

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 100);

                var handlerTask = Task.Run(() =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();

                    using var queue = new QueueHandler<Message>("1", conn, TestQueueName, 10, q =>
                    {
                        msg.Add(q.Text);
                    });

                    readyEvent.Set();

                    endEvent.WaitOne();
                });

                readyEvent.WaitOne();

                for (int i = 0; i < counter; i++)
                {
                    emitter.Enqueue(TestQueueName, new Message
                    {
                        Text = $"msg_{i}"
                    });
                }

                while (msg.Count < counter)
                    Task.Delay(1000).Wait();

                endEvent.Set();

                handlerTask.Wait();
            }
            
            Assert.IsTrue(msg.Count == counter);            
        }

        [TestMethod]
        [Timeout(60000)]
        public void MultiHandlerTest()
        {
            var readyOneEvent = new AutoResetEvent(false);
            var endOneEvent = new AutoResetEvent(false);

            var readyTwoEvent = new AutoResetEvent(false);
            var endTwoEvent = new AutoResetEvent(false);

            int counter = 100;

            var msg1 = new ConcurrentBag<string?>();
            var msg2 = new ConcurrentBag<string?>();

            var random = new Random();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 50);

                var handlerOneTask = Task.Run(async () =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();

                    using var queue = new QueueHandler<Message>("1", conn, TestQueueName, 10, q =>
                    {
                        Task.Delay(random.Next(100, 2000)).Wait();
                        msg1.Add(q.Text);
                    });

                    readyOneEvent.Set();

                    endOneEvent.WaitOne();
                });

                var handlerTwoTask = Task.Run(async () =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();

                    using var queue = new QueueHandler<Message>("1", conn, TestQueueName, 10, q =>
                    {
                        Task.Delay(random.Next(100, 2000)).Wait();
                        msg2.Add(q.Text);
                    });

                    readyTwoEvent.Set();

                    endTwoEvent.WaitOne();
                });

                readyOneEvent.WaitOne();
                readyTwoEvent.WaitOne();

                for (int i = 0; i < counter; i++)
                {
                    emitter.Enqueue(TestQueueName, new Message
                    {
                        Text = $"msg_{i}"
                    });
                }


                while (counter > msg1.Count + msg2.Count)
                    Task.Delay(1000).Wait();

                endOneEvent.Set();
                endTwoEvent.Set();

                Task.WaitAll(handlerOneTask, handlerTwoTask);
            }

            Assert.IsTrue(counter == msg1.Count + msg2.Count);
            Assert.IsTrue(!msg1.IsEmpty);
            Assert.IsTrue(!msg2.IsEmpty);
        }

        /// <summary>
        /// Этот тест требует ручного отключения / включения сети.
        /// Запускать только в режиме отладка, закомментировав атрибут [Ignore]
        /// и расставив точки останова в местах включения / выключения сети.
        /// </summary>
        [Ignore]
        [TestMethod]
        public void SuccessfulQueueAfterConnectionRecoveryTest()
        {
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            var stoppedEvent = new AutoResetEvent(false);
            var startedEvent = new AutoResetEvent(false);
            var readyEvent = new AutoResetEvent(false);
            var needToStartedSwitchOnEvent = new AutoResetEvent(false);

            var incomings = new List<object>();
            var handled = new List<object>();
            var errors = new List<object>();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 10);

                var handlerTask = Task.Run(() =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();
                    using var handler = new QueueHandler<Message>("1", conn, TestQueueName, 5, q =>
                    {
                        msgEvent.Set();
                    });
                    handler.OnIncoming += (s, ea) => incomings.Add(ea);
                    handler.OnHandled += (s, ea) => handled.Add(ea);
                    handler.OnError += (s, ea) => errors.Add(ea);
                    handler.Stopped += (s, ev) => stoppedEvent.Set();

                    readyEvent.Set();

                    needToStartedSwitchOnEvent.WaitOne();
                    handler.Started += (s, ev) => startedEvent.Set();

                    endEvent.WaitOne();
                });

                readyEvent.WaitOne();

                emitter.Enqueue(TestQueueName, new Message
                {
                    Text = "confirm"
                });
                msgEvent.WaitOne();

                needToStartedSwitchOnEvent.Set();

                //тут нужно вручную отключить сеть
                ;
                stoppedEvent.WaitOne();

                try
                {
                    emitter.Enqueue(TestQueueName, new Message
                    {
                        Text = "lost confirm"
                    });
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType<SendingException>(ex);
                    Assert.IsInstanceOfType<AlreadyClosedException>(ex.InnerException);
                }

                //тут нужно вручную включить сеть
                ;
                startedEvent.WaitOne();

                emitter.Enqueue(TestQueueName, new Message
                {
                    Text = "confirm"
                });
                msgEvent.WaitOne();

                endEvent.Set();

                handlerTask.Wait();
            }

            Assert.AreEqual(errors.Count, 0);
            Assert.AreEqual(incomings.Count, 2);
            Assert.AreEqual(handled.Count, 2);
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDelete(TestQueueName, false, false);
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDelete(TestQueueName, false, false);
        }
    }
}