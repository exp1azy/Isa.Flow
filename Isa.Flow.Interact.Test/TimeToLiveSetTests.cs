using Isa.Flow.Interact.Entities;
using Isa.Flow.Interact.Utils;

namespace Isa.Flow.Interact.Test
{
    [TestClass]
    public class TimeToLiveSetTests
    {
        [TestMethod]
        public void ExpirationTest()
        {
            IEnumerable<ActorInfo>? expired = null;
            var eventExpired = new AutoResetEvent(false);

            var set = new TimeToLiveSet<ActorInfo>(2000, new ActorInfoEqualityComparer());

            Assert.IsTrue(set.Add(new ActorInfo { Id = "1" }));
            Assert.IsTrue(set.Add(new ActorInfo { Id = "2" }));
            Assert.IsTrue(set.Add(new ActorInfo { Id = "3" }, 5000));

            set.Expired += (s, e) =>
            {
                expired = e.ExpiredItems;
                eventExpired.Set();
            };

            Task.Delay(2500).Wait();

            var active = set.ToList();
            eventExpired.WaitOne();
            
            Assert.IsNotNull(active);
            Assert.IsTrue(active.Count == 1);
            Assert.IsFalse(active.Any(i => i.Id == "1"));
            Assert.IsFalse(active.Any(i => i.Id == "2"));
            Assert.IsTrue(active.Any(i => i.Id == "3"));

            Assert.IsNotNull(expired);
            Assert.IsTrue(expired.Count() == 2);
            Assert.IsTrue(expired.Any(i => i.Id == "1"));
            Assert.IsTrue(expired.Any(i => i.Id == "2"));
            Assert.IsFalse(expired.Any(i => i.Id == "3"));
        }

        [TestMethod]
        public void AppendedTest()
        {
            IEnumerable<ActorInfo>? added = null;
            var eventAppended = new AutoResetEvent(false);

            var set = new TimeToLiveSet<ActorInfo>(2000, new ActorInfoEqualityComparer());
            set.Appended += (s, e) =>
            {
                added = e.AppendedItems;
                eventAppended.Set();
            };

            Assert.IsTrue(set.Add(new ActorInfo { Id = "1" }));

            var actual = set.ToList();
            eventAppended.WaitOne();

            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Count == 1);
            Assert.IsTrue(actual.Any(i => i.Id == "1"));

            Assert.IsNotNull(added);
            Assert.IsTrue(added.Count() == 1);
            Assert.IsTrue(added.Any(i => i.Id == "1"));
        }

        [TestMethod]
        public void UpdateExpiredTest()
        {
            var set = new TimeToLiveSet<ActorInfo>(20000, new ActorInfoEqualityComparer());
            Assert.IsTrue(set.Add(new ActorInfo { Id = "1" }));
            Assert.IsFalse(set.Add(new ActorInfo { Id = "1" }, 2000));
            Task.Delay(2500).Wait();
            var actual = set.ToList();
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.Any());

            set = new TimeToLiveSet<ActorInfo>(2000, new ActorInfoEqualityComparer());
            Assert.IsTrue(set.Add(new ActorInfo { Id = "1" }));
            Assert.IsFalse(set.Add(new ActorInfo { Id = "1" }, 20000));
            Task.Delay(2500).Wait();
            actual = set.ToList();
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Count == 1);
        }
    }
}