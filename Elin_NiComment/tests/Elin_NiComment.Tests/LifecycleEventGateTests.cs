using NUnit.Framework;

namespace Elin_NiComment.Tests
{
    [TestFixture]
    public class LifecycleEventGateTests
    {
        [Test]
        public void FinishFiresOnceWhenNewLifecycleReachesFinalState()
        {
            var gate = new LifecycleEventGate();
            var instance = new object();

            var state = gate.Begin(instance, alreadyFinalized: false);

            Assert.That(gate.Finish(state, isFinalized: true), Is.True);
            Assert.That(gate.Finish(state, isFinalized: true), Is.False);
        }

        [Test]
        public void BeginSuppressesAlreadyFinalizedInstances()
        {
            var gate = new LifecycleEventGate();

            var state = gate.Begin(new object(), alreadyFinalized: true);

            Assert.That(gate.Finish(state, isFinalized: true), Is.False);
        }

        [Test]
        public void FinishSuppressesCallsThatNeverReachFinalState()
        {
            var gate = new LifecycleEventGate();

            var state = gate.Begin(new object(), alreadyFinalized: false);

            Assert.That(gate.Finish(state, isFinalized: false), Is.False);
        }

        [Test]
        public void ReentrantSameInstanceFiresOnlyForOuterLifecycle()
        {
            var gate = new LifecycleEventGate();
            var instance = new object();

            var outer = gate.Begin(instance, alreadyFinalized: false);
            var inner = gate.Begin(instance, alreadyFinalized: false);

            Assert.That(gate.Finish(inner, isFinalized: true), Is.False);
            Assert.That(gate.Finish(outer, isFinalized: true), Is.True);
        }

        [Test]
        public void NestedDifferentInstancesBothFire()
        {
            var gate = new LifecycleEventGate();
            var first = gate.Begin(new object(), alreadyFinalized: false);
            var second = gate.Begin(new object(), alreadyFinalized: false);

            Assert.That(gate.Finish(second, isFinalized: true), Is.True);
            Assert.That(gate.Finish(first, isFinalized: true), Is.True);
        }

        [Test]
        public void SameInstanceCanFireAgainAfterSeparateLifecycleStarts()
        {
            var gate = new LifecycleEventGate();
            var instance = new object();

            var first = gate.Begin(instance, alreadyFinalized: false);
            Assert.That(gate.Finish(first, isFinalized: true), Is.True);

            var second = gate.Begin(instance, alreadyFinalized: false);
            Assert.That(gate.Finish(second, isFinalized: true), Is.True);
        }

        [Test]
        public void AbortAfterExceptionReleasesTheActiveLifecycle()
        {
            var gate = new LifecycleEventGate();
            var instance = new object();
            var interrupted = gate.Begin(instance, alreadyFinalized: false);

            gate.Abort(interrupted);
            var restarted = gate.Begin(instance, alreadyFinalized: false);

            Assert.That(gate.Finish(restarted, isFinalized: true), Is.True);
        }
    }
}
