namespace Elin_JustDoomIt
{
    public enum DoomRewardRateSelectionMode
    {
        AskEveryMap = 0,
        FixedLow = 1,
        FixedMid = 2,
        FixedHigh = 3
    }

    public static class DoomLaunchPreferences
    {
        public const string RewardRateAskEveryMap = "ask_every_map";
        public const string RewardRateFixedLow = "fixed_low";
        public const string RewardRateFixedMid = "fixed_mid";
        public const string RewardRateFixedHigh = "fixed_high";

        public static DoomRewardRateSelectionMode ParseRewardRateSelectionMode(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case RewardRateFixedLow:
                    return DoomRewardRateSelectionMode.FixedLow;
                case RewardRateFixedMid:
                    return DoomRewardRateSelectionMode.FixedMid;
                case RewardRateFixedHigh:
                    return DoomRewardRateSelectionMode.FixedHigh;
                default:
                    return DoomRewardRateSelectionMode.AskEveryMap;
            }
        }

        public static string SerializeRewardRateSelectionMode(DoomRewardRateSelectionMode mode)
        {
            switch (mode)
            {
                case DoomRewardRateSelectionMode.FixedLow:
                    return RewardRateFixedLow;
                case DoomRewardRateSelectionMode.FixedMid:
                    return RewardRateFixedMid;
                case DoomRewardRateSelectionMode.FixedHigh:
                    return RewardRateFixedHigh;
                default:
                    return RewardRateAskEveryMap;
            }
        }

        public static DoomRewardRate ResolveConfiguredRate(DoomRewardRateSelectionMode mode)
        {
            switch (mode)
            {
                case DoomRewardRateSelectionMode.FixedLow:
                    return DoomRewardRate.Low;
                case DoomRewardRateSelectionMode.FixedMid:
                    return DoomRewardRate.Mid;
                case DoomRewardRateSelectionMode.FixedHigh:
                    return DoomRewardRate.High;
                default:
                    return DoomRewardRate.None;
            }
        }
    }
}
