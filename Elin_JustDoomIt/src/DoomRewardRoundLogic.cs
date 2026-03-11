using System;

namespace Elin_JustDoomIt
{
    public enum DoomRewardRate
    {
        None = 0,
        Low = 1,
        Mid = 2,
        High = 3
    }

    public readonly struct DoomRewardRateConfig
    {
        public readonly string Code;
        public readonly int EntryCost;
        public readonly int BaseReward;
        public readonly int HitLossPercent;

        public DoomRewardRateConfig(string code, int entryCost, int baseReward, int hitLossPercent)
        {
            Code = code ?? string.Empty;
            EntryCost = Math.Max(0, entryCost);
            BaseReward = Math.Max(0, baseReward);
            HitLossPercent = Math.Max(0, hitLossPercent);
        }
    }

    public static class DoomRewardRoundLogic
    {
        private const int SecretPoolGain = 500;
        private const float KillBonusGrowthDivisor = 10f;
        private static readonly float[] DifficultyMultipliers = { 0.50f, 0.75f, 1.00f, 1.25f, 1.50f };

        public static DoomRewardRateConfig GetConfig(DoomRewardRate rate)
        {
            switch (rate)
            {
                case DoomRewardRate.Low:
                    return new DoomRewardRateConfig("LOW", 100, 30, 18);
                case DoomRewardRate.Mid:
                    return new DoomRewardRateConfig("MID", 500, 70, 24);
                case DoomRewardRate.High:
                    return new DoomRewardRateConfig("HIGH", 1000, 110, 30);
                default:
                    return new DoomRewardRateConfig("-", 0, 0, 0);
            }
        }

        public static float GetDifficultyMultiplier(int skill)
        {
            var index = Math.Max(1, Math.Min(5, skill)) - 1;
            return DifficultyMultipliers[index];
        }

        public static float GetKillBonusCapPercent(DoomRewardRate rate)
        {
            switch (rate)
            {
                case DoomRewardRate.Low:
                    return 400f;
                case DoomRewardRate.Mid:
                    return 600f;
                case DoomRewardRate.High:
                    return 999f;
                default:
                    return 0f;
            }
        }

        private static float GetKillBonusProgress(int stageIndex)
        {
            if (stageIndex <= 0)
            {
                return 0f;
            }

            return stageIndex / (stageIndex + KillBonusGrowthDivisor);
        }

        public static float GetKillMultiplier(DoomRewardRate rate, int stageIndex)
        {
            var bonusPercent = GetKillBonusCapPercent(rate) * GetKillBonusProgress(stageIndex);
            return 1f + (bonusPercent / 100f);
        }

        public static int GetDisplayedKillBonusPercent(DoomRewardRate rate, int stageIndex)
        {
            return Math.Max(0, RoundToInt((GetKillMultiplier(rate, stageIndex) - 1f) * 100f));
        }

        public static int ResetMultiplierStage()
        {
            return 0;
        }

        public static int AdvanceMultiplierStage(int stageIndex)
        {
            return Math.Max(0, Math.Min(int.MaxValue - 1, stageIndex + 1));
        }

        public static string FormatMultiplier(DoomRewardRate rate, int stageIndex)
        {
            return "x" + GetKillMultiplier(rate, stageIndex).ToString("0.0");
        }

        public static int CalculateKillPoolGain(DoomRewardRate rate, int skill, int multiplierStage)
        {
            var config = GetConfig(rate);
            if (config.BaseReward <= 0)
            {
                return 0;
            }

            var reward = config.BaseReward * GetDifficultyMultiplier(skill) * GetKillMultiplier(rate, multiplierStage);
            return Math.Max(0, RoundToInt(reward));
        }

        public static int CalculateHitLoss(DoomRewardRate rate, int pool)
        {
            if (pool <= 0)
            {
                return 0;
            }

            var config = GetConfig(rate);
            if (config.HitLossPercent <= 0)
            {
                return 0;
            }

            var loss = pool * (config.HitLossPercent / 100f);
            return Math.Max(0, Math.Min(pool, RoundToInt(loss)));
        }

        public static int CalculateRunNet(DoomRewardRate rate, int pool)
        {
            return pool - GetConfig(rate).EntryCost;
        }

        public static int GetEntryCost(DoomRewardRate rate)
        {
            return GetConfig(rate).EntryCost;
        }

        public static int GetHitLossPercent(DoomRewardRate rate)
        {
            return GetConfig(rate).HitLossPercent;
        }

        public static int GetSecretPoolGain()
        {
            return SecretPoolGain;
        }

        private static int RoundToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
