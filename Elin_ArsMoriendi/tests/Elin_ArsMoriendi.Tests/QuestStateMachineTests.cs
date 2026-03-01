using System;
using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class QuestStateMachineTests
    {
        private enum TestState
        {
            A = 0,
            B = 1,
            C = 2,
        }

        [Test]
        public void TryAdvanceFromCurrent_Advances_WhenRuleMatches()
        {
            TestState current = TestState.A;
            var sut = new QuestStateMachine<TestState>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTransitionRule<TestState>(
                        TestState.A,
                        TestState.B,
                        () => true),
                });

            bool advanced = sut.TryAdvanceFromCurrent();

            Assert.That(advanced, Is.True);
            Assert.That(current, Is.EqualTo(TestState.B));
        }

        [Test]
        public void TryAdvanceFromCurrent_DoesNotAdvance_WhenRuleConditionFails()
        {
            TestState current = TestState.A;
            bool blockedCalled = false;
            var sut = new QuestStateMachine<TestState>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTransitionRule<TestState>(
                        TestState.A,
                        TestState.B,
                        () => false,
                        onBlocked: () => blockedCalled = true),
                });

            bool advanced = sut.TryAdvanceFromCurrent();

            Assert.That(advanced, Is.False);
            Assert.That(blockedCalled, Is.True);
            Assert.That(current, Is.EqualTo(TestState.A));
        }

        [Test]
        public void TryAdvanceFromCurrent_DoesNothing_WhenNoRuleForCurrentState()
        {
            TestState current = TestState.A;
            var sut = new QuestStateMachine<TestState>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTransitionRule<TestState>(
                        TestState.B,
                        TestState.C,
                        () => true),
                });

            bool advanced = sut.TryAdvanceFromCurrent();

            Assert.That(advanced, Is.False);
            Assert.That(current, Is.EqualTo(TestState.A));
        }

        [Test]
        public void Constructor_Throws_WhenDuplicateFromStateIsDefined()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new QuestStateMachine<TestState>(
                    () => TestState.A,
                    _ => { },
                    new[]
                    {
                        new QuestTransitionRule<TestState>(
                            TestState.A,
                            TestState.B,
                            () => true),
                        new QuestTransitionRule<TestState>(
                            TestState.A,
                            TestState.C,
                            () => true),
                    });
            });
        }

        [Test]
        public void Constructor_Throws_WhenTransitionDoesNotProgressForward()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new QuestStateMachine<TestState>(
                    () => TestState.A,
                    _ => { },
                    new[]
                    {
                        new QuestTransitionRule<TestState>(
                            TestState.B,
                            TestState.B,
                            () => true),
                    });
            });
        }
    }
}
