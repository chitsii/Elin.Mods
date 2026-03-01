using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class QuestCooldownPolicyTests
    {
        [TestCase(0, 1000, 1440, false)]
        [TestCase(-1, 1000, 1440, false)]
        [TestCase(1000, 2439, 1440, true)]
        [TestCase(1000, 2440, 1440, false)]
        [TestCase(1000, 2441, 1440, false)]
        public void ShouldWaitBeforeAdvance_MatchesExpectedBoundary(
            int lastAdvanceRaw,
            int currentRaw,
            int threshold,
            bool expected)
        {
            bool actual = QuestCooldownPolicy.ShouldWaitBeforeAdvance(
                lastAdvanceRaw,
                currentRaw,
                threshold);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(1, 1440)]
        [TestCase(2, 2880)]
        [TestCase(0, 0)]
        [TestCase(-1, 0)]
        public void DaysToRawMinutes_ConvertsDays(int days, int expectedRaw)
        {
            int actual = QuestCooldownPolicy.DaysToRawMinutes(days);
            Assert.That(actual, Is.EqualTo(expectedRaw));
        }

        [TestCase(1000, 2439, 1, true)]
        [TestCase(1000, 2440, 1, false)]
        [TestCase(1000, 3879, 2, true)]
        [TestCase(1000, 3880, 2, false)]
        public void ShouldWaitBeforeAdvanceDays_MatchesDayThreshold(
            int lastAdvanceRaw,
            int currentRaw,
            int cooldownDays,
            bool expected)
        {
            bool actual = QuestCooldownPolicy.ShouldWaitBeforeAdvanceDays(
                lastAdvanceRaw,
                currentRaw,
                cooldownDays);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
