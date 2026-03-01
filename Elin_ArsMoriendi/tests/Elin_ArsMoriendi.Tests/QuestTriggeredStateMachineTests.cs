using System;
using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class QuestTriggeredStateMachineTests
    {
        private enum TestState
        {
            A = 0,
            B = 1,
            C = 2,
            D = 3,
            Invalid = 99,
        }

        [Test]
        public void TryHandle_Advances_WhenStateAndTriggerMatchAndConditionTrue()
        {
            TestState current = TestState.A;
            var sut = new QuestTriggeredStateMachine<TestState, string>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.A, TestState.B, "tome_open", () => true),
                });

            bool advanced = sut.TryHandle("tome_open");

            Assert.That(advanced, Is.True);
            Assert.That(current, Is.EqualTo(TestState.B));
        }

        [Test]
        public void TryHandle_DoesNotAdvance_WhenTriggerDoesNotMatch()
        {
            TestState current = TestState.A;
            var sut = new QuestTriggeredStateMachine<TestState, string>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.A, TestState.B, "tome_open", () => true),
                });

            bool advanced = sut.TryHandle("enemy_defeated");

            Assert.That(advanced, Is.False);
            Assert.That(current, Is.EqualTo(TestState.A));
        }

        [Test]
        public void TryHandle_CallsOnBlocked_WhenConditionFails()
        {
            TestState current = TestState.A;
            bool blockedCalled = false;
            var sut = new QuestTriggeredStateMachine<TestState, string>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.A, TestState.B, "tome_open", () => false, onBlocked: () => blockedCalled = true),
                });

            bool advanced = sut.TryHandle("tome_open");

            Assert.That(advanced, Is.False);
            Assert.That(blockedCalled, Is.True);
            Assert.That(current, Is.EqualTo(TestState.A));
        }

        [Test]
        public void Constructor_Throws_WhenDuplicateStateAndTriggerAreDefined()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new QuestTriggeredStateMachine<TestState, string>(
                    () => TestState.A,
                    _ => { },
                    new[]
                    {
                        new QuestTriggeredTransitionRule<TestState, string>(
                            TestState.A, TestState.B, "tome_open", () => true),
                        new QuestTriggeredTransitionRule<TestState, string>(
                            TestState.A, TestState.C, "tome_open", () => true),
                    });
            });
        }

        [Test]
        public void Constructor_Throws_WhenTransitionDoesNotProgressForward()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new QuestTriggeredStateMachine<TestState, string>(
                    () => TestState.A,
                    _ => { },
                    new[]
                    {
                        new QuestTriggeredTransitionRule<TestState, string>(
                            TestState.B, TestState.B, "tome_open", () => true),
                    });
            });
        }

        [Test]
        public void TryHandle_Advances_WhenBranchResolverSelectsAllowedTarget()
        {
            TestState current = TestState.A;
            int branchFlag = 1;
            var sut = new QuestTriggeredStateMachine<TestState, string>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.A,
                        "branch_select",
                        new[] { TestState.B, TestState.C },
                        resolveTo: () => branchFlag == 1 ? TestState.B : TestState.C,
                        canAdvance: () => true),
                });

            bool advancedB = sut.TryHandle("branch_select");
            Assert.That(advancedB, Is.True);
            Assert.That(current, Is.EqualTo(TestState.B));

            current = TestState.A;
            branchFlag = 2;
            bool advancedC = sut.TryHandle("branch_select");
            Assert.That(advancedC, Is.True);
            Assert.That(current, Is.EqualTo(TestState.C));
        }

        [Test]
        public void TryHandle_DoesNotAdvance_WhenBranchResolverReturnsDisallowedTarget()
        {
            TestState current = TestState.A;
            bool invalidBranchCalled = false;
            var sut = new QuestTriggeredStateMachine<TestState, string>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.A,
                        "branch_select",
                        new[] { TestState.B, TestState.C },
                        resolveTo: () => TestState.Invalid,
                        canAdvance: () => true,
                        onInvalidBranch: () => invalidBranchCalled = true),
                });

            bool advanced = sut.TryHandle("branch_select");

            Assert.That(advanced, Is.False);
            Assert.That(invalidBranchCalled, Is.True);
            Assert.That(current, Is.EqualTo(TestState.A));
        }

        [Test]
        public void Constructor_Throws_WhenBranchContainsNonForwardTarget()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new QuestTriggeredStateMachine<TestState, string>(
                    () => TestState.A,
                    _ => { },
                    new[]
                    {
                        new QuestTriggeredTransitionRule<TestState, string>(
                            TestState.B,
                            "branch_select",
                            new[] { TestState.B, TestState.C },
                            resolveTo: () => TestState.C,
                            canAdvance: () => true),
                    });
            });
        }

        [Test]
        public void TryHandle_AllowsConvergence_FromMultipleForwardBranches()
        {
            TestState current = TestState.A;
            int branchFlag = 1;
            var sut = new QuestTriggeredStateMachine<TestState, string>(
                () => current,
                next => current = next,
                new[]
                {
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.A,
                        "branch_select",
                        new[] { TestState.B, TestState.C },
                        resolveTo: () => branchFlag == 1 ? TestState.B : TestState.C,
                        canAdvance: () => true),
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.B, TestState.D, "converge", () => true),
                    new QuestTriggeredTransitionRule<TestState, string>(
                        TestState.C, TestState.D, "converge", () => true),
                });

            bool branchToB = sut.TryHandle("branch_select");
            bool convergeFromB = sut.TryHandle("converge");
            Assert.That(branchToB, Is.True);
            Assert.That(convergeFromB, Is.True);
            Assert.That(current, Is.EqualTo(TestState.D));

            current = TestState.A;
            branchFlag = 2;
            bool branchToC = sut.TryHandle("branch_select");
            bool convergeFromC = sut.TryHandle("converge");
            Assert.That(branchToC, Is.True);
            Assert.That(convergeFromC, Is.True);
            Assert.That(current, Is.EqualTo(TestState.D));
        }
    }
}
