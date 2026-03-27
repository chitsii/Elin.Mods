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
        private const int FixedEntryCost = 100;
        private const int FixedBaseReward = 70;
        private const int KillStreakIncrement = 35;
        private static readonly int[] DifficultyRewardCaps = { 140, 175, 210, 280, 350 };

        public static DoomRewardRate GetFixedRate()
        {
            return DoomRewardRate.Mid;
        }

        public static DoomRewardRateConfig GetConfig(DoomRewardRate rate)
        {
            if (rate == DoomRewardRate.None)
            {
                return new DoomRewardRateConfig("-", 0, 0, 0);
            }

            return new DoomRewardRateConfig("FIXED", FixedEntryCost, FixedBaseReward, 0);
        }

        public static int GetDifficultyRewardCap(int skill)
        {
            var index = Math.Max(1, Math.Min(5, skill)) - 1;
            return DifficultyRewardCaps[index];
        }

        public static int GetKillRewardBonus(int skill, int stageIndex)
        {
            var reward = CalculateKillPoolGain(skill, stageIndex);
            return Math.Max(0, reward - FixedBaseReward);
        }

        public static int GetDisplayedKillBonusPercent(int skill, int stageIndex)
        {
            return Math.Max(0, RoundToInt((GetKillRewardBonus(skill, stageIndex) / (float)FixedBaseReward) * 100f));
        }

        public static int GetKillReward(int skill, int stageIndex)
        {
            if (stageIndex <= 0)
            {
                return FixedBaseReward;
            }

            var unclamped = FixedBaseReward + Math.Max(0, stageIndex) * KillStreakIncrement;
            return Math.Min(GetDifficultyRewardCap(skill), unclamped);
        }

        public static int ResetMultiplierStage()
        {
            return 0;
        }

        public static int AdvanceMultiplierStage(int stageIndex)
        {
            return Math.Max(0, Math.Min(int.MaxValue - 1, stageIndex + 1));
        }

        public static string FormatKillBonus(int skill, int stageIndex)
        {
            var bonus = GetKillRewardBonus(skill, stageIndex);
            return bonus > 0 ? "+" + bonus : string.Empty;
        }

        public static int CalculateKillPoolGain(int skill, int multiplierStage)
        {
            var config = GetConfig(GetFixedRate());
            if (config.BaseReward <= 0)
            {
                return 0;
            }

            return Math.Max(0, GetKillReward(skill, multiplierStage));
        }

        public static int CalculateHitLoss(DoomRewardRate rate, int pool)
        {
            return 0;
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
