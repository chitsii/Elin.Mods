using UnityEngine;

namespace Elin_JustDoomIt
{
    internal static class DoomRewardPresentationBuilder
    {
        public static DoomHudViewModel BuildHudViewModel(DoomRewardRoundState state, System.Func<string, string, string, string> localize)
        {
            var config = DoomRewardRoundLogic.GetConfig(DoomRewardRoundLogic.GetFixedRate());
            if (state.RoundActive && state.CurrentRate != DoomRewardRate.None)
            {
                return new DoomHudViewModel(
                    localize("開始料 ", "SESSION ENTRY ", "会话入场 ") + config.EntryCost,
                    BuildKillRewardDisplay(state.Skill, state.MultiplierStage, localize),
                    localize("獲得チップ", "Earned Chips", "已获筹码"),
                    state.CurrentPool.ToString(),
                    state.CurrentPool,
                    DoomRewardRoundLogic.CalculateHitLoss(state.CurrentRate, state.CurrentPool),
                    roundActive: true);
            }

            return new DoomHudViewModel(
                localize("開始料 ", "SESSION ENTRY ", "会话入场 ") + config.EntryCost,
                localize("1キル報酬 " + config.BaseReward, "Per-Kill Payout " + config.BaseReward, "每杀奖励 " + config.BaseReward),
                localize("獲得チップ", "Earned Chips", "已获筹码"),
                "-",
                0,
                0,
                roundActive: false);
        }

        public static DoomRateSelectionViewModel BuildRateSelectionViewModel(
            DoomRewardRoundState state,
            int selectedIndex,
            int availableChips,
            System.Func<string, string, string, string> localize)
        {
            var rate = CursorToRate(selectedIndex);
            var config = DoomRewardRoundLogic.GetConfig(rate);
            var bonusCap = DoomRewardRoundLogic.GetDifficultyRewardCap(state.Skill);
            var rates = new[] { DoomRewardRate.Low, DoomRewardRate.Mid, DoomRewardRate.High };
            var options = new string[rates.Length];
            for (var i = 0; i < rates.Length; i++)
            {
                var optionRate = rates[i];
                var optionConfig = DoomRewardRoundLogic.GetConfig(optionRate);
                var optionBonusCap = DoomRewardRoundLogic.GetDifficultyRewardCap(state.Skill);
                var option = localize(
                    GetRewardRateDisplayName(optionRate, localize) + "\n<size=18>参加 " + optionConfig.EntryCost +
                    "   1キル " + optionConfig.BaseReward +
                    "   +35/kill" +
                    "   最大 " + optionBonusCap +
                    "   被弾で連キルリセット</size>",
                    GetRewardRateDisplayName(optionRate, localize) + "\n<size=18>ENTRY " + optionConfig.EntryCost +
                    "   PAYOUT " + optionConfig.BaseReward +
                    "   +35/kill" +
                    "   MAX " + optionBonusCap +
                    "   HIT RESETS STREAK</size>",
                    GetRewardRateDisplayName(optionRate, localize) + "\n<size=18>入场 " + optionConfig.EntryCost +
                    "   每杀 " + optionConfig.BaseReward +
                    "   每杀 +35" +
                    "   最大 " + optionBonusCap +
                    "   受击重置连杀</size>");
                if (availableChips < optionConfig.EntryCost)
                {
                    option += localize("  <size=18>  不足</size>", "  <size=18>  LOCK</size>", "  <size=18>  不足</size>");
                }

                options[i] = option;
            }

            var helper = localize(
                "参加 " + config.EntryCost + " を払って開始。1キルごとに報酬が +35 ずつ伸び、現在難易度では最大 " + bonusCap + " まで。被弾すると連キルボーナスだけが初期化されます。",
                "Pay " + config.EntryCost + " to start. Each kill grows payout by +35 up to " + bonusCap + " at this difficulty. Taking damage only resets the kill-streak bonus.",
                "支付 " + config.EntryCost + " 开始。每次连杀都会让奖励额外 +35，在当前难度下最高到 " + bonusCap + "。受伤只会重置连杀奖励。");

            return new DoomRateSelectionViewModel(
                localize("賭けを選択", "SELECT WAGER", "选择赌法"),
                helper,
                options,
                selectedIndex);
        }

        public static string GetRewardRateDisplayName(DoomRewardRate rate, System.Func<string, string, string, string> localize)
        {
            switch (rate)
            {
                case DoomRewardRate.Low:
                    return localize("安全重視", "Safe Play", "稳扎稳打");
                case DoomRewardRate.Mid:
                    return localize("標準勝負", "Standard Play", "标准胜负");
                case DoomRewardRate.High:
                    return localize("大勝負", "High Stakes", "放手一搏");
                default:
                    return "-";
            }
        }

        public static string BuildKillRewardDisplay(int skill, int multiplierStage, System.Func<string, string, string, string> localize)
        {
            var reward = DoomRewardRoundLogic.CalculateKillPoolGain(skill, multiplierStage);
            var bonus = DoomRewardRoundLogic.FormatKillBonus(skill, multiplierStage);
            if (string.IsNullOrWhiteSpace(bonus))
            {
                return localize(
                    "1キル報酬 " + reward,
                    "Per-Kill Payout " + reward,
                    "每杀奖励 " + reward);
            }

            return localize(
                "1キル報酬 " + reward + " (" + bonus + ")",
                "Per-Kill Payout " + reward + " (" + bonus + ")",
                "每杀奖励 " + reward + " (" + bonus + ")");
        }

        private static DoomRewardRate CursorToRate(int cursor)
        {
            switch (Mathf.Clamp(cursor, 0, 2))
            {
                case 0: return DoomRewardRate.Low;
                case 1: return DoomRewardRate.Mid;
                default: return DoomRewardRate.High;
            }
        }
    }
}
