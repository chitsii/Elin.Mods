using System;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Pure calculation functions extracted from NecromancyManager for testability.
    /// No EClass or Unity dependencies — all inputs are passed as parameters.
    /// </summary>
    public static class NecromancyCalculations
    {
        /// <summary>100%復元に必要なSU（強い魂30個相当）</summary>
        public const int MaxResurrectionSU = 1200;

        /// <summary>最低復元率（魂1個でも元Lvの5%は保証）</summary>
        public const double BaseRecoveryRate = 0.05;

        /// <summary>Base success rate for body augmentation (10%).</summary>
        public const double AugmentBaseRate = 0.10;

        /// <summary>
        /// Per-step decay for attribute injection efficiency.
        /// Tuned down so repeated injections remain meaningfully rewarding.
        /// </summary>
        public const double InjectionDecayPerStep = 0.08;

        /// <summary>
        /// Batch injections count as partial "virtual repeats" for efficiency.
        /// Kept as a mild penalty so batch use is viable without being dominant.
        /// </summary>
        public const double InjectionBatchWeight = 0.35;

        /// <summary>
        /// Calculate resurrection level from total SU using recovery-rate model.
        /// SU determines how much of the corpse's original level is restored.
        /// 30 strong souls (1200 SU) = 100% restoration.
        /// </summary>
        public static int CalculateResurrectionLevel(int totalSU, int corpseLv)
        {
            if (totalSU <= 0 || corpseLv <= 0) return 1;

            double ratio = Math.Min(1.0, (double)totalSU / MaxResurrectionSU);
            double recoveryRate = BaseRecoveryRate + (1.0 - BaseRecoveryRate) * Math.Sqrt(ratio);

            return Math.Max(1, (int)(corpseLv * recoveryRate));
        }

        /// <summary>
        /// Calculate augmentation success rate for given corpse count and resonance.
        /// rate = base * (1 + (count-1) * 0.5) + resonance * 0.05, capped at 0.95
        /// </summary>
        public static double CalculateAugmentRate(int corpseCount, int resonance)
        {
            if (corpseCount <= 0) return 0;
            double rate = AugmentBaseRate * (1.0 + (corpseCount - 1) * 0.5) + resonance * 0.05;
            return Math.Min(rate, 0.95);
        }

        /// <summary>
        /// Calculate rampage threshold from PC's MAG and kill count.
        /// threshold = MAG/5 + sqrt(kills)
        /// </summary>
        public static int CalculateRampageThreshold(int mag, int kills)
        {
            return mag / 5 + (int)Math.Sqrt(kills);
        }

        /// <summary>
        /// Get rampage threshold breakdown as individual components.
        /// </summary>
        public static (int magComponent, int killsComponent, int total) GetRampageThresholdBreakdown(int mag, int kills)
        {
            int magPart = mag / 5;
            int killsPart = (int)Math.Sqrt(kills);
            return (magPart, killsPart, magPart + killsPart);
        }

        /// <summary>
        /// Calculate rampage chance (0-90%) based on enhancement level vs threshold.
        /// Returns 0 if within safe range.
        /// </summary>
        public static int CalculateRampageChance(int enhancementLevel, int threshold)
        {
            int excess = enhancementLevel - threshold;
            if (excess <= 0) return 0;
            return Math.Min(excess * 10, 90);
        }

        /// <summary>
        /// Calculate efficiency for attribute enhancement injection.
        /// effectiveInjections = injectionCount + (count - 1) * InjectionBatchWeight
        /// efficiency = 1.0 / (1 + effectiveInjections * InjectionDecayPerStep)
        /// </summary>
        public static double CalculateInjectionEfficiency(int injectionCount, int count = 1)
        {
            if (count <= 0) return 0;
            if (injectionCount < 0) injectionCount = 0;
            double effectiveInjections = injectionCount + Math.Max(0, count - 1) * InjectionBatchWeight;
            return 1.0 / (1.0 + effectiveInjections * InjectionDecayPerStep);
        }

        /// <summary>
        /// Calculate injection boost for attribute enhancement.
        /// efficiency = CalculateInjectionEfficiency(injectionCount, count)
        /// boost = max(1, floor(√(suPerUnit * count) * efficiency * multiplier))
        /// </summary>
        public static int CalculateInjectionBoost(int suPerUnit, int count, int injectionCount, double multiplier = 1.0)
        {
            if (suPerUnit <= 0 || count <= 0) return 0;
            double efficiency = CalculateInjectionEfficiency(injectionCount, count);
            return Math.Max(1, (int)(Math.Sqrt(suPerUnit * count) * efficiency * multiplier));
        }

        /// <summary>
        /// Preserve Corpse spell duration in turns from computed spell power.
        /// Mirrors ActPreserveCorpse.Perform.
        /// </summary>
        public static int CalculatePreserveCorpseDuration(int spellPower)
        {
            return 30 + spellPower / 100;
        }

        /// <summary>
        /// Soul Trap Mass duration in turns from computed spell power.
        /// Mirrors ActSoulTrapMass.Perform.
        /// </summary>
        public static int CalculateSoulTrapMassDuration(int spellPower)
        {
            return 30 + spellPower / 120;
        }

        /// <summary>
        /// Corpse Chain Burst dice parameters matching Meteor formula.
        /// num = 1 + power/150, sides = 4 + (power/10) * 3
        /// </summary>
        public static (int num, int sides) CorpseBurstDice(int power)
        {
            int num = Math.Max(1, 1 + power / 150);
            int sides = Math.Max(1, 4 + (power / 10) * 3);
            return (num, sides);
        }

        /// <summary>
        /// Per-turn poison damage for Plague Touch.
        /// Mirrors ConPlagueTouch.Tick.
        /// </summary>
        public static int CalculatePlagueTickDamage(int spellPower)
        {
            return Math.Max(1, spellPower / 15);
        }

        /// <summary>
        /// Power used when Plague Touch spreads to nearby targets.
        /// Mirrors ConPlagueTouch.Tick.
        /// </summary>
        public static int CalculatePlagueSpreadPower(int spellPower)
        {
            return Math.Max(1, (int)Math.Floor(spellPower * 0.65));
        }

        /// <summary>
        /// Sukutsu-style void scaling used for deep-floor level progression.
        /// Matches the random battle scaling rule in Elin_SukutsuArena.
        /// </summary>
        public static int CalculateVoidScaledLevel(int deepest, int sourceLevel, bool isBoss = false)
        {
            deepest = Math.Max(1, deepest);
            sourceLevel = Math.Max(1, sourceLevel);

            long scaled;
            if (deepest <= 50)
            {
                scaled = sourceLevel;
            }
            else
            {
                scaled = (50L + sourceLevel) * Math.Max(1, (deepest - 1) / 50);
            }

            if (isBoss)
                scaled = scaled * 150 / 100;

            return (int)Math.Min(Math.Max(1L, scaled), 100000000L);
        }

        /// <summary>
        /// Calculate resurrection level cap from the player's deepest reached floor.
        /// Legacy simple rule: floor(deepest * 1.5).
        /// </summary>
        public static int CalculateResurrectionCapFromDeepest(int deepest)
        {
            deepest = Math.Max(1, deepest);
            return Math.Max(1, (int)Math.Floor(deepest * 1.5));
        }
    }
}
