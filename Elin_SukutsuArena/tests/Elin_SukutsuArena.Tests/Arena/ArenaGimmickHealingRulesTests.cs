using NUnit.Framework;

namespace Elin_SukutsuArena.Tests.Arena
{
    [TestFixture]
    public class ArenaGimmickHealingRulesTests
    {
        [Test]
        public void TryBlockHealing_BlockedArenaHealAboveInt32Max_ZeroesLongAmount()
        {
            long amount = (long)int.MaxValue + 1;

            bool blocked = ArenaGimmickHealingRules.TryBlockHealing(
                isArenaBattle: true,
                isHealingBlocked: true,
                amount: ref amount);

            Assert.That(blocked, Is.True);
            Assert.That(amount, Is.EqualTo(0L));
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void TryBlockHealing_ZeroOrNegativeHeal_DoesNotBlock(long initialAmount)
        {
            long amount = initialAmount;

            bool blocked = ArenaGimmickHealingRules.TryBlockHealing(
                isArenaBattle: true,
                isHealingBlocked: true,
                amount: ref amount);

            Assert.That(blocked, Is.False);
            Assert.That(amount, Is.EqualTo(initialAmount));
        }

        [Test]
        public void TryBlockHealing_GimmickDisabled_DoesNotChangePositiveAmount()
        {
            long amount = 500;

            bool blocked = ArenaGimmickHealingRules.TryBlockHealing(
                isArenaBattle: true,
                isHealingBlocked: false,
                amount: ref amount);

            Assert.That(blocked, Is.False);
            Assert.That(amount, Is.EqualTo(500L));
        }

        [Test]
        public void TryBlockHealing_OutsideArena_DoesNotChangePositiveAmount()
        {
            long amount = 500;

            bool blocked = ArenaGimmickHealingRules.TryBlockHealing(
                isArenaBattle: false,
                isHealingBlocked: true,
                amount: ref amount);

            Assert.That(blocked, Is.False);
            Assert.That(amount, Is.EqualTo(500L));
        }
    }
}
