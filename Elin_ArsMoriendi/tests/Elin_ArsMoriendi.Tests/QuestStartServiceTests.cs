using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class QuestStartServiceTests
    {
        private sealed class InMemoryStartStore : IQuestStartStateStore
        {
            private readonly HashSet<string> _started = new();

            public bool IsStarted(string startId) => _started.Contains(startId);

            public void MarkStarted(string startId)
            {
                _started.Add(startId);
            }
        }

        private sealed class FixedPolicy : IQuestStartPolicy
        {
            private readonly bool _canStart;

            public FixedPolicy(bool canStart)
            {
                _canStart = canStart;
            }

            public bool CanStart(string startId) => _canStart;
        }

        private sealed class RecorderExecutor : IQuestStartExecutor
        {
            public int Calls;
            public string? LastStartId;
            public bool ThrowOnStart;
            public bool ReturnValue = true;

            public bool TryStart(string startId)
            {
                Calls++;
                LastStartId = startId;
                if (ThrowOnStart)
                    throw new InvalidOperationException("boom");
                return ReturnValue;
            }
        }

        [Test]
        public void TryStart_StartsOnce_WhenCalledTwice()
        {
            var store = new InMemoryStartStore();
            var policy = new FixedPolicy(true);
            var executor = new RecorderExecutor();
            var sut = new QuestStartService(store, policy, executor);

            bool first = sut.TryStart("ars.karen_shadow");
            bool second = sut.TryStart("ars.karen_shadow");

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(executor.Calls, Is.EqualTo(1));
            Assert.That(store.IsStarted("ars.karen_shadow"), Is.True);
        }

        [Test]
        public void TryStart_DoesNotStart_WhenConditionIsFalse()
        {
            var store = new InMemoryStartStore();
            var sut = new QuestStartService(store, new FixedPolicy(false), new RecorderExecutor());

            bool started = sut.TryStart("ars.karen_shadow");

            Assert.That(started, Is.False);
            Assert.That(store.IsStarted("ars.karen_shadow"), Is.False);
        }

        [Test]
        public void TryStart_DoesNotMarkStarted_WhenExecutorThrows()
        {
            var store = new InMemoryStartStore();
            var executor = new RecorderExecutor { ThrowOnStart = true };
            var sut = new QuestStartService(store, new FixedPolicy(true), executor);

            bool started = sut.TryStart("ars.karen_shadow");

            Assert.That(started, Is.False);
            Assert.That(store.IsStarted("ars.karen_shadow"), Is.False);
            Assert.That(executor.Calls, Is.EqualTo(1));
        }

        [Test]
        public void TryStart_DoesNotMarkStarted_WhenExecutorReturnsFalse()
        {
            var store = new InMemoryStartStore();
            var executor = new RecorderExecutor { ReturnValue = false };
            var sut = new QuestStartService(store, new FixedPolicy(true), executor);

            bool started = sut.TryStart("ars.karen_shadow");

            Assert.That(started, Is.False);
            Assert.That(store.IsStarted("ars.karen_shadow"), Is.False);
            Assert.That(executor.Calls, Is.EqualTo(1));
        }
    }
}
