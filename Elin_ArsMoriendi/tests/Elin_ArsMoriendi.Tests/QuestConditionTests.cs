using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class QuestConditionTests
    {
        [Test]
        public void All_ReturnsTrue_OnlyWhenAllConditionsAreTrue()
        {
            var cond = QuestCondition.All(
                () => true,
                () => true,
                () => true);
            var cond2 = QuestCondition.All(
                () => true,
                () => false);

            Assert.That(cond(), Is.True);
            Assert.That(cond2(), Is.False);
        }

        [Test]
        public void Any_ReturnsTrue_WhenAtLeastOneConditionIsTrue()
        {
            var cond = QuestCondition.Any(
                () => false,
                () => true,
                () => false);
            var cond2 = QuestCondition.Any(
                () => false,
                () => false);

            Assert.That(cond(), Is.True);
            Assert.That(cond2(), Is.False);
        }

        [Test]
        public void Not_InvertsCondition()
        {
            var cond = QuestCondition.Not(() => true);
            var cond2 = QuestCondition.Not(() => false);

            Assert.That(cond(), Is.False);
            Assert.That(cond2(), Is.True);
        }
    }
}
