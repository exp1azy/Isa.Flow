using Isa.Flow.Interact.Exceptions;
using Isa.Flow.Test.Common;

namespace Isa.Flow.Interact.Test
{
    [TestClass]
    public class ActorTests
    {
        [TestMethod]
        [Timeout(30000)]
        public void PingTest()
        {
            var actor1Created = new AutoResetEvent(false);
            var actor2Created = new AutoResetEvent(false);
            var actor1Response = new AutoResetEvent(false);
            var actor2Response = new AutoResetEvent(false);

            bool res1 = false, res2 = false;

            var connectionFactory = Env.RabbitConnectionFactory;

            var task1 = Task.Run(() =>
            {
                using var actor1 = new DummyActor(connectionFactory, "1");

                actor1Created.Set();
                actor2Created.WaitOne();

                var task = actor1.PingAsync("2", 10);
                task.Wait();
                res1 = task.Result;

                actor1Response.Set();
                actor2Response.WaitOne();
            });

            using var actor2 = new DummyActor(connectionFactory, "2");

            actor2Created.Set();
            actor1Created.WaitOne();

            var task = actor2.PingAsync("1", 10);
            task.Wait();
            res2 = task.Result;

            actor2Response.Set();
            actor1Response.WaitOne();

            task1.Wait();

            Assert.IsTrue(res1 && res2);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith0SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(1);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith1SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(1000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith2SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(2000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith3SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(3000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith4SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(4000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith5SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(5000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith6SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(6000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith7SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(7000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith8SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(8000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith9SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(9000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith10SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(10000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith11SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(11000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith12SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(12000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith13SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(13000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith14SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(14000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith15SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(15000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith16SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(16000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith17SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(17000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith18SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(18000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith19SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(19000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith20SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(20000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith30SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(30000);
        }

        [TestMethod]
        [Timeout(135000)]
        [ExpectedException(typeof(ActorAlreadyStartedException))]
        public void SameIdActorsWith60SecGapTest()
        {
            LaunchingSameIdActorsWithGapTest(60000);
        }

        private static void LaunchingSameIdActorsWithGapTest(int gap)
        {
            var started1 = false;
            var started2 = false;
            Exception? ex = null;

            var end1 = new AutoResetEvent(false);
            var end2 = new AutoResetEvent(false);

            var connectionFactory = Env.RabbitConnectionFactory;

            var task1 = Task.Delay(gap).ContinueWith(_ =>
            {
                try
                {
                    using var actor1 = new DummyActor(connectionFactory, "1");
                    started1 = true;

                    end1.WaitOne();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });

            var task2 = Task.Run(() =>
            {
                try
                {
                    using var actor2 = new DummyActor(connectionFactory, "1");
                    started2 = true;

                    end2.WaitOne();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });

            Task.WaitAny(new[] { task1, task2 });

            using (var pingClient = new DummyActor(connectionFactory, "2"))
            {
                var task = pingClient.PingAsync("1", 10);
                task.Wait();
                Assert.IsTrue(task.Result || (task1.IsCompleted && task2.IsCompleted));
            }

            end1.Set();
            end2.Set();

            Task.WaitAll(new[] { task1, task2 });

            Assert.IsFalse(started1 && started2);

            throw ex ?? new ApplicationException();
        }
    }
}
