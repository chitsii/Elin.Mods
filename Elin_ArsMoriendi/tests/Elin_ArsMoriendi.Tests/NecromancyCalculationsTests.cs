using NUnit.Framework;
using Elin_ArsMoriendi;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class NecromancyCalculationsTests
    {
        // ── ResurrectionLevel ──

        [Test]
        public void ResurrectionLevel_ZeroSU_ReturnsOne()
        {
            Assert.AreEqual(1, NecromancyCalculations.CalculateResurrectionLevel(0, 50));
        }

        [Test]
        public void ResurrectionLevel_ZeroCorpseLv_ReturnsOne()
        {
            Assert.AreEqual(1, NecromancyCalculations.CalculateResurrectionLevel(100, 0));
        }

        [Test]
        public void ResurrectionLevel_MaxSU_RestoresFullLevel()
        {
            // 1200 SU = 100% restoration → corpseLv * 1.0 = 50
            int result = NecromancyCalculations.CalculateResurrectionLevel(1200, 50);
            Assert.AreEqual(50, result);
        }

        [Test]
        public void ResurrectionLevel_MinSU_AppliesBaseRecovery()
        {
            // Very small SU → recoveryRate close to BaseRecoveryRate (0.05)
            // ratio = 1/1200 ≈ 0.00083, sqrt ≈ 0.0289
            // recoveryRate = 0.05 + 0.95 * 0.0289 ≈ 0.0774
            // floor(100 * 0.0774) = 7
            int result = NecromancyCalculations.CalculateResurrectionLevel(1, 100);
            Assert.That(result, Is.GreaterThanOrEqualTo(1));
            Assert.That(result, Is.LessThan(20)); // Much less than full restoration
        }

        [Test]
        public void ResurrectionLevel_OverMaxSU_CapsAtFullRestore()
        {
            // 2400 SU (2x max) should still cap at corpseLv
            int result = NecromancyCalculations.CalculateResurrectionLevel(2400, 50);
            Assert.AreEqual(50, result);
        }

        // ── VoidScaledLevel ──

        [Test]
        public void VoidScaledLevel_AtOrBelow50_UsesSourceLevel()
        {
            int result = NecromancyCalculations.CalculateVoidScaledLevel(50, 100);
            Assert.AreEqual(100, result);
        }

        [Test]
        public void VoidScaledLevel_DeepFloor_UsesSukutsuFormula()
        {
            // deepest=200 => floor((200-1)/50)=3
            // (50 + 100) * 3 = 450
            int result = NecromancyCalculations.CalculateVoidScaledLevel(200, 100);
            Assert.AreEqual(450, result);
        }

        [Test]
        public void VoidScaledLevel_Boss_Applies150PercentMultiplier()
        {
            // Base from above: 450 -> boss 150% => 675
            int result = NecromancyCalculations.CalculateVoidScaledLevel(200, 100, isBoss: true);
            Assert.AreEqual(675, result);
        }

        [Test]
        public void ResurrectionCap_Deepest2626_IsNearDeepestAndNotExplosive()
        {
            // Legacy simple formula: floor(deepest * 1.5)
            int result = NecromancyCalculations.CalculateResurrectionCapFromDeepest(2626);
            Assert.AreEqual(3939, result);
            Assert.That(result, Is.LessThan(10000));
        }

        [Test]
        public void ResurrectionCap_MatchesLegacyLinearDeepestRule()
        {
            int deepest = 1000;
            int cap = NecromancyCalculations.CalculateResurrectionCapFromDeepest(deepest);
            int expected = (int)System.Math.Floor(deepest * 1.5);
            Assert.AreEqual(expected, cap);
        }

        // ── AugmentRate ──

        [Test]
        public void AugmentRate_ZeroCorpses_ReturnsZero()
        {
            Assert.AreEqual(0, NecromancyCalculations.CalculateAugmentRate(0, 0));
        }

        [Test]
        public void AugmentRate_OneCorpse_NoResonance_ReturnsBase()
        {
            // rate = 0.10 * (1 + 0) + 0 = 0.10
            double rate = NecromancyCalculations.CalculateAugmentRate(1, 0);
            Assert.AreEqual(0.10, rate, 0.001);
        }

        [Test]
        public void AugmentRate_ManyCorpses_HighResonance_CapsAt95Percent()
        {
            double rate = NecromancyCalculations.CalculateAugmentRate(10, 9);
            Assert.AreEqual(0.95, rate, 0.001);
        }

        [Test]
        public void AugmentRate_WithResonance_IncludesPityBonus()
        {
            // rate = 0.10 * (1 + 0) + 3 * 0.05 = 0.10 + 0.15 = 0.25
            double rate = NecromancyCalculations.CalculateAugmentRate(1, 3);
            Assert.AreEqual(0.25, rate, 0.001);
        }

        // ── RampageThreshold ──

        [Test]
        public void RampageThreshold_MAG50_Kills0()
        {
            // 50/5 + sqrt(0) = 10 + 0 = 10
            Assert.AreEqual(10, NecromancyCalculations.CalculateRampageThreshold(50, 0));
        }

        [Test]
        public void RampageThreshold_MAG0_Kills100()
        {
            // 0/5 + sqrt(100) = 0 + 10 = 10
            Assert.AreEqual(10, NecromancyCalculations.CalculateRampageThreshold(0, 100));
        }

        [Test]
        public void RampageThreshold_MAG50_Kills100_Combined()
        {
            // 50/5 + sqrt(100) = 10 + 10 = 20
            Assert.AreEqual(20, NecromancyCalculations.CalculateRampageThreshold(50, 100));
        }

        [Test]
        public void RampageThresholdBreakdown_ReturnsCorrectComponents()
        {
            var (magPart, killsPart, total) = NecromancyCalculations.GetRampageThresholdBreakdown(50, 100);
            Assert.AreEqual(10, magPart);
            Assert.AreEqual(10, killsPart);
            Assert.AreEqual(20, total);
        }

        // ── RampageChance ──

        [Test]
        public void RampageChance_WithinThreshold_ReturnsZero()
        {
            Assert.AreEqual(0, NecromancyCalculations.CalculateRampageChance(5, 10));
        }

        [Test]
        public void RampageChance_AtThreshold_ReturnsZero()
        {
            Assert.AreEqual(0, NecromancyCalculations.CalculateRampageChance(10, 10));
        }

        [Test]
        public void RampageChance_Excess3_Returns30Percent()
        {
            Assert.AreEqual(30, NecromancyCalculations.CalculateRampageChance(13, 10));
        }

        [Test]
        public void RampageChance_Excess10_CapsAt90Percent()
        {
            Assert.AreEqual(90, NecromancyCalculations.CalculateRampageChance(20, 10));
        }

        [Test]
        public void RampageChance_HighExcess_CapsAt90Percent()
        {
            Assert.AreEqual(90, NecromancyCalculations.CalculateRampageChance(100, 10));
        }

        // ── InjectionBoost ──

        [Test]
        public void InjectionBoost_FirstInjection_FullEfficiency()
        {
            // efficiency = 1.0/(1+0*0.2) = 1.0, boost = floor(√5 * 1.0) = floor(2.23) = 2
            int boost = NecromancyCalculations.CalculateInjectionBoost(5, 1, 0);
            Assert.AreEqual(2, boost);
        }

        [Test]
        public void InjectionBoost_FifthInjection_ReducedEfficiency()
        {
            // efficiency = 1.0/(1+5*0.2) = 0.5
            // boost = max(1, floor(√5 * 0.5)) = max(1, floor(1.11)) = 1
            int boost = NecromancyCalculations.CalculateInjectionBoost(5, 1, 5);
            Assert.AreEqual(1, boost);
        }

        [Test]
        public void InjectionBoost_DarkAwakeningMultiplier()
        {
            // efficiency = 1.0, multiplier = 2.0
            // boost = floor(√5 * 1.0 * 2.0) = floor(4.47) = 4
            int boost = NecromancyCalculations.CalculateInjectionBoost(5, 1, 0, 2.0);
            Assert.AreEqual(4, boost);
        }

        [Test]
        public void InjectionBoost_MinimumOne()
        {
            // Very small values should still return at least 1
            int boost = NecromancyCalculations.CalculateInjectionBoost(1, 1, 100);
            Assert.AreEqual(1, boost);
        }

        [Test]
        public void InjectionBoost_ZeroInputs_ReturnsZero()
        {
            Assert.AreEqual(0, NecromancyCalculations.CalculateInjectionBoost(0, 1, 0));
            Assert.AreEqual(0, NecromancyCalculations.CalculateInjectionBoost(5, 0, 0));
        }

        [Test]
        public void InjectionBoost_MultipleSouls_ScalesSublinearly()
        {
            // 3 souls: efficiency = 1.0, boost = floor(√(5*3)) = floor(√15) = floor(3.87) = 3
            int boost = NecromancyCalculations.CalculateInjectionBoost(5, 3, 0);
            Assert.AreEqual(3, boost);
        }

        [Test]
        public void InjectionEfficiency_BatchPenalty_AppliesForLargeCount()
        {
            double single = NecromancyCalculations.CalculateInjectionEfficiency(0, 1);
            double batch = NecromancyCalculations.CalculateInjectionEfficiency(0, 10);
            Assert.AreEqual(1.0, single, 0.0001);
            Assert.That(batch, Is.LessThan(single));
        }

        [Test]
        public void InjectionBoost_BatchCount_IsPenalizedComparedToSingle()
        {
            int single = NecromancyCalculations.CalculateInjectionBoost(40, 1, 0);
            int batch = NecromancyCalculations.CalculateInjectionBoost(40, 10, 0);
            Assert.That(batch, Is.GreaterThan(single));
            Assert.That(batch, Is.LessThan(single * 10));
        }
    }
}
