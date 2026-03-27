using System;

namespace Elin_JustDoomIt
{
    internal static class DoomArcadeMenuContentBuilder
    {
        public static string BuildHelpText(
            DoomArcadeMenuUI.MenuState state,
            DoomArcadeMenuUI.RowTag tag,
            DoomRewardRateSelectionMode? hoveredRewardMode,
            bool hasSaveSummary,
            DoomSaveSummary saveSummary,
            Func<string, string, string, string> localize)
        {
            switch (state)
            {
                case DoomArcadeMenuUI.MenuState.Main:
                    switch (tag)
                    {
                        case DoomArcadeMenuUI.RowTag.Continue:
                            return localize(
                                "保存済みのDOOM進行を再開します。固定参加費はCONTINUE時に1回だけ支払い、その後はマップごとに追加徴収されません。",
                                "Resume the saved DOOM progress. The fixed entry fee is charged once on CONTINUE, with no extra charge on later maps.",
                                "继续已保存的DOOM进度。固定入场费只会在CONTINUE时支付一次，之后的地图不会重复收费。");
                        case DoomArcadeMenuUI.RowTag.NewRun:
                            return localize(
                                "最初のマップから開始します。現在のIWAD / MOD / 難易度設定を使います。",
                                "Start from the first map using the current IWAD / MOD / difficulty settings.",
                                "从第一张地图开始，使用当前的IWAD / MOD / 难度设置。");
                        case DoomArcadeMenuUI.RowTag.Iwad:
                            return localize(
                                "ベースゲームを切り替えます。IWADが変わると互換性のないMODは自動で外れます。",
                                "Switch the base game. Incompatible mods will be removed automatically if the IWAD changes.",
                                "切换基础游戏。更换IWAD时，不兼容的MOD会被自动移除。");
                        case DoomArcadeMenuUI.RowTag.Skill:
                            return localize(
                                "DOOM本体の難易度を変更します。連キル報酬の上限にも使われます。",
                                "Change the DOOM game difficulty. It also drives the kill-streak reward cap.",
                                "更改DOOM本体难度，也会影响连杀奖励上限。");
                        case DoomArcadeMenuUI.RowTag.Mods:
                            return localize(
                                "外部PWADや設定済みエントリを切り替えます。複数WAD構成の初期設定もここで行います。",
                                "Switch external PWADs and configured entries. Multi-WAD setup is also managed here.",
                                "切换外部PWAD和已配置条目，多WAD设置也在这里完成。");
                        case DoomArcadeMenuUI.RowTag.GeneralSettings:
                            return localize(
                                "表示まわりを調整します。普段使いのUXをここで整えます。",
                                "Adjust display settings. This is where you tune the day-to-day UX.",
                                "调整显示设置，在这里优化平时游玩的UX。");
                        default:
                            return localize(
                                "前回使った構成を確認してから、再開か新規開始を選びます。",
                                "Review the last-used setup, then choose continue or start over.",
                                "先确认上次使用的配置，再选择继续或重新开始。");
                    }

                case DoomArcadeMenuUI.MenuState.GeneralSettings:
                    switch (tag)
                    {
                        case DoomArcadeMenuUI.RowTag.Resolution:
                            return localize(
                                "DOOMの内部レンダリング解像度を変更します。変更は次回DOOM起動時から反映されます。",
                                "Change DOOM's internal render resolution. Applies from the next DOOM launch.",
                                "更改DOOM内部渲染分辨率，将在下次启动DOOM时生效。");
                        case DoomArcadeMenuUI.RowTag.Brightness:
                            return localize(
                                "DOOM画面の明るさを調整します。暗いWADや見づらいマップ向けです。",
                                "Adjust the DOOM screen brightness. Useful for dark WADs or low-visibility maps.",
                                "调整DOOM画面亮度，适合较暗的WAD或能见度差的地图。");
                        case DoomArcadeMenuUI.RowTag.Cheats:
                            return localize(
                                "無敵やBFG無限など、プレイ補助用の設定を開きます。",
                                "Open play-assist toggles such as invincibility and infinite BFG.",
                                "打开无敌和BFG无限等辅助游玩设置。");
                        default:
                            return localize(
                                "普段の遊びやすさに直結する設定です。よく変える項目だけをここにまとめています。",
                                "These settings shape the day-to-day feel of the arcade flow. Only the most frequently changed items live here.",
                                "这些设置会直接影响日常游玩体验，这里只放最常调整的项目。");
                    }

                case DoomArcadeMenuUI.MenuState.CheatSettings:
                    switch (tag)
                    {
                        case DoomArcadeMenuUI.RowTag.Invincible:
                            return localize(
                                "DOOM中の被ダメージを無効化します。",
                                "Prevents the player from taking damage during DOOM.",
                                "使玩家在DOOM中不会受到伤害。");
                        case DoomArcadeMenuUI.RowTag.InfiniteBfg:
                            return localize(
                                "BFGの発射でセルを消費しなくなります。起動時とマップ開始時に未取得でも使えるよう補正します。",
                                "Makes the BFG stop consuming cells and grants access on launch and map start even if it was not picked up.",
                                "让BFG发射时不再消耗电池，并在启动和地图开始时补正为可用，即使尚未拾取。");
                        default:
                            return localize(
                                "プレイ補助や検証向けの設定です。通常プレイ向けではありません。",
                                "These are assistance/debug options intended for testing or casual experimentation.",
                                "这些是辅助/调试选项，主要用于测试或轻松体验。");
                    }

                case DoomArcadeMenuUI.MenuState.RewardRatePicker:
                    if (hoveredRewardMode.HasValue)
                    {
                        switch (hoveredRewardMode.Value)
                        {
                            case DoomRewardRateSelectionMode.FixedLow:
                                return localize(
                                    "賭け金が安く被弾の痛みも軽い。堅実に稼ぎたいならここから。",
                                    "Low stakes and forgiving on damage. A safe start for steady earnings.",
                                    "赌注低，受伤惩罚也轻。想稳定赚取筹码就从这里开始。");
                            case DoomRewardRateSelectionMode.FixedMid:
                                return localize(
                                    "報酬とリスクのバランス型。慣れてきたらこれが安定。",
                                    "Balanced risk and reward. A solid pick once you know the maps.",
                                    "报酬和风险均衡。熟悉地图后这是稳定之选。");
                            case DoomRewardRateSelectionMode.FixedHigh:
                                return localize(
                                    "大きく稼げるが、被弾すると稼ぎが一気に吹き飛ぶ。腕に自信があるなら。",
                                    "Huge payoff, but taking hits wipes your earnings fast. For the confident.",
                                    "收益巨大，但被击中会迅速清空收益。有自信就选这个。");
                        }
                    }

                    return localize(
                        "旧レート選択UIです。現在の報酬ルールは固定参加費 + 連キル固定加算方式です。",
                        "Legacy rate picker UI. The current reward rules use a fixed entry fee plus flat kill-streak increases.",
                        "这是旧的赌法选择界面。当前奖励规则改为固定入场费 + 连杀固定加成。");

                case DoomArcadeMenuUI.MenuState.SkillPicker:
                    return localize(
                        "難易度はDOOM本体の進行と、連キル報酬の上限に影響します。CONTINUE時は保存側の難易度が優先されます。",
                        "Difficulty affects both the DOOM run and the kill-streak payout cap. On CONTINUE, the difficulty stored in the save takes priority.",
                        "难度会同时影响DOOM进程和连杀奖励上限。CONTINUE 时会优先使用存档中的难度。");

                case DoomArcadeMenuUI.MenuState.IwadPicker:
                    return localize(
                        "遊ぶDOOM本体を選びます。IWADによってステージ構成や対応MODが変わります。",
                        "Choose which base DOOM game to play. The IWAD changes map sets and mod compatibility.",
                        "选择要游玩的DOOM本体。不同IWAD会影响地图结构和MOD兼容性。");

                case DoomArcadeMenuUI.MenuState.PwadPicker:
                    return localize(
                        "MODごとの差分や互換性を確認しながら切り替えます。複数WAD構成が必要なものは SETUP を使います。",
                        "Switch mods while checking their compatibility notes. Use SETUP for entries that need multiple WAD files.",
                        "切换MOD时可以同时查看兼容性说明。需要多WAD的条目请使用 SETUP。");

                case DoomArcadeMenuUI.MenuState.ModSetup:
                    return localize(
                        "使用するファイルと順序を決めます。上から順に読み込まれるため、並び順がそのまま優先度になります。",
                        "Choose which files to use and in what order. Files are loaded top to bottom, so order defines priority.",
                        "决定要使用的文件及其顺序。文件会按从上到下加载，因此顺序就是优先级。");

                case DoomArcadeMenuUI.MenuState.ResetSetupConfirm:
                    return localize(
                        "このMODエントリに保存されたファイル選択と順序を消去します。元には戻せません。",
                        "This clears the saved file selection and order for the mod entry. It cannot be undone.",
                        "这会清除该MOD条目已保存的文件选择和顺序，无法撤销。");

                default:
                    return string.Empty;
            }
        }

        public static string GetRewardRateModeLabel(DoomRewardRateSelectionMode mode, Func<string, string, string, string> localize)
        {
            return localize("開始料 100", "Session Entry 100", "会话入场 100");
        }

        public static string BuildRateSummaryRow(DoomRewardRateSelectionMode mode, Func<string, string, string, string> localize)
        {
            var summary = GetRewardRateModeLabel(mode, localize);
            return localize(
                "報酬      " + summary,
                "REWARD   " + summary,
                "奖励      " + summary);
        }

        public static string BuildTotalPlaytimeSummary(int totalSeconds, Func<string, string, string, string> localize)
        {
            return localize(
                FormatDuration(totalSeconds),
                FormatDuration(totalSeconds),
                FormatDuration(totalSeconds));
        }

        public static string BuildSkillOptionLabel(string[] skillNames, int skill, Func<string, string, string, string> localize)
        {
            if (skillNames == null || skillNames.Length == 0)
            {
                return string.Empty;
            }

            var index = Math.Max(1, Math.Min(skillNames.Length, skill)) - 1;
            var cap = DoomRewardRoundLogic.GetDifficultyRewardCap(skill);
            return skillNames[index] + localize(
                "  /  1キル上限 ",
                "  /  Kill cap ",
                "  /  每杀上限 ") + cap;
        }

        public static string BuildSaveSummaryLine(DoomSaveSummary summary, Func<string, string, string, string> localize)
        {
            var playtimePart = localize(
                "合計 " + FormatDuration(summary.TotalPlaySeconds),
                "Playtime " + FormatDuration(summary.TotalPlaySeconds),
                "总时长 " + FormatDuration(summary.TotalPlaySeconds));

            string timePart;
            if (summary.SavedUtcTicks > 0)
            {
                try
                {
                    timePart = new DateTime(summary.SavedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                }
                catch
                {
                    timePart = "-";
                }
            }
            else
            {
                timePart = "-";
            }

            return playtimePart + "  @" + timePart;
        }

        private static string FormatDuration(int totalSeconds)
        {
            var sec = Math.Max(0, totalSeconds);
            var hours = sec / 3600;
            var minutes = (sec % 3600) / 60;
            var seconds = sec % 60;

            if (hours > 0)
            {
                return hours + "h " + minutes.ToString("00") + "m";
            }

            if (minutes > 0)
            {
                return minutes + "m " + seconds.ToString("00") + "s";
            }

            return seconds + "s";
        }
    }
}
