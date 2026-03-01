using System.Collections.Generic;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using Elin_SukutsuArena.Localization;
using UnityEngine;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// 本日のランダムバトルデータ（キャッシュ用）
    /// </summary>
    public class TodaysBattleData
    {
        public BattleStageData StageData { get; set; }
        public List<ArenaGimmickType> Gimmicks { get; set; }
        public SpawnPattern SpawnPattern { get; set; }
        public int RewardPlat { get; set; }
        public int RewardPotion { get; set; }

        /// <summary>
        /// バトルロイヤルモード（敵同士も敵対）
        /// </summary>
        public bool IsFreeForAll => SpawnPattern == SpawnPattern.FreeForAll;

        /// <summary>
        /// 終末モード（5Wave制）
        /// </summary>
        public bool IsApocalypse => SpawnPattern == SpawnPattern.Apocalypse;
    }

    /// <summary>
    /// 本日のランダムバトルキャッシュ管理
    /// 1日1回制限とリロール機能を提供
    /// </summary>
    public static class TodaysBattleCache
    {
        /// <summary>
        /// フラグキー: 最後にランダムバトルを生成した日
        /// </summary>
        public const string FLAG_LAST_RANDOM_DAY = SessionFlagKeys.LastRandomDay;

        /// <summary>
        /// リロールコスト（プラチナコイン）
        /// </summary>
        public const int REROLL_COST = 10;

        /// <summary>
        /// メモリキャッシュ
        /// </summary>
        private static TodaysBattleData _cachedBattle;
        private static int _cachedDay = -1;

        /// <summary>
        /// 本日のバトルを取得（ゲーム内1日キャッシュ）
        /// </summary>
        /// <returns>本日のバトルデータ</returns>
        public static TodaysBattleData GetOrGenerateTodaysBattle()
        {
            int currentDay = GetCurrentDay();

            // キャッシュが有効かチェック（ゲーム内日付ベース）
            if (_cachedBattle != null && _cachedDay == currentDay)
            {
                ModLog.Log($"[TodaysBattleCache] Using cached battle (day: {currentDay})");
                return _cachedBattle;
            }

            // 新規生成（日付が変わった or キャッシュが空）
            ModLog.Log($"[TodaysBattleCache] Generating new battle (day: {_cachedDay} -> {currentDay})");
            GenerateNewBattle();
            _cachedDay = currentDay;
            return _cachedBattle;
        }

        /// <summary>
        /// プラチナコイン消費でリロール
        /// </summary>
        /// <returns>リロール成功したらtrue</returns>
        public static bool RerollTodaysBattle()
        {
            // コイン確認
            if (EClass.pc.GetCurrency("plat") < REROLL_COST)
            {
                Msg.Say(ArenaLocalization.NotEnoughPlatinum);
                return false;
            }

            // コイン消費
            EClass.pc.ModCurrency(-REROLL_COST, "plat");
            Msg.Say(ArenaLocalization.PaidPlatinum(REROLL_COST));

            // 再生成
            GenerateNewBattle();
            ModLog.Log("[TodaysBattleCache] Battle rerolled");

            return true;
        }

        /// <summary>
        /// 新しいバトルを生成してキャッシュに保存
        /// </summary>
        private static void GenerateNewBattle()
        {
            // ランクに応じたギミックを先に選択（敵生成に影響するため）
            int rank = (int)ArenaContext.I.Player.Rank;

            // デバッグオーバーライドを確認
            var gimmicks = Debugging.RandomBattleDebug.GetForceGimmicks()
                ?? ArenaGimmickSystem.SelectGimmicks(rank);

            ModLog.Log($"[TodaysBattleCache] Selected gimmicks: {string.Join(", ", gimmicks)}");

            // バトルデータ生成（ギミック情報を渡す）
            var (stageData, spawnPattern) = RandomEnemyGenerator.GenerateRandomBattle(gimmicks);

            // ギミックをstageDataに追加
            stageData.Gimmicks = ArenaGimmickSystem.ConvertToConfigs(gimmicks);

            // 報酬を計算（平方根による逓減 + キャップ）
            int deepest = Debugging.RandomBattleDebug.GetEffectiveDeepest();
            var (basePlat, basePotion) = RandomEnemyGenerator.GetRankBaseReward(rank);
            float gimmickMod = ArenaGimmickSystem.CalculateDifficultyModifier(gimmicks);
            float patternMod = RandomEnemyGenerator.GetSpawnPatternRewardModifier(spawnPattern);
            float totalMod = gimmickMod * patternMod;

            ModLog.Log($"[TodaysBattleCache] Reward modifiers: gimmick={gimmickMod:F2}, pattern={patternMod:F2}, total={totalMod:F2}");

            // プラチナコイン: ln(深度+1) * 1.5（対数スケール）
            int depthBonusPlat = (int)(System.Math.Log(deepest + 1) * 1.5);
            int rewardPlat = (int)((basePlat + depthBonusPlat) * totalMod);

            // 媚薬: √(深度) * 0.16（深度1000でキャップ、上限6本）
            int effectiveDepthPotion = System.Math.Min(deepest, 1000);
            int depthBonusPotion = (int)(System.Math.Sqrt(effectiveDepthPotion) * 0.16);
            int rewardPotion = System.Math.Min((int)((basePotion + depthBonusPotion) * totalMod), 6);

            stageData.RewardPlat = rewardPlat;

            // キャッシュに保存
            _cachedBattle = new TodaysBattleData
            {
                StageData = stageData,
                Gimmicks = gimmicks,
                SpawnPattern = spawnPattern,
                RewardPlat = rewardPlat,
                RewardPotion = rewardPotion
            };

            ModLog.Log($"[TodaysBattleCache] Generated battle: enemies={stageData.TotalEnemyCount}, plat={rewardPlat}, potion={rewardPotion}");
        }

        /// <summary>
        /// 現在のゲーム内日付を取得
        /// </summary>
        private static int GetCurrentDay()
        {
            return EClass.world?.date?.day ?? 0;
        }

        /// <summary>
        /// 最後にランダムバトルを生成した日を保存
        /// </summary>
        private static void SaveLastRandomDay(int day)
        {
            ArenaContext.I?.Storage?.SetInt(FLAG_LAST_RANDOM_DAY, day);
        }

        /// <summary>
        /// 最後にランダムバトルを生成した日を取得
        /// </summary>
        public static int GetLastRandomDay()
        {
            return ArenaContext.I?.Storage?.GetInt(FLAG_LAST_RANDOM_DAY, -1) ?? -1;
        }

        /// <summary>
        /// 本日のバトルのプレビュー情報を取得
        /// </summary>
        public static (string enemyInfo, string spawnInfo, string gimmickInfo, int rewardPlat, int rewardPotion) GetTodaysBattlePreview()
        {
            var battle = GetOrGenerateTodaysBattle();
            if (battle == null)
            {
                return (ArenaLocalization.Unknown, ArenaLocalization.Unknown, ArenaLocalization.NoGimmicks, 0, 0);
            }

            // 敵情報
            int enemyCount = battle.StageData.TotalEnemyCount;
            string firstEnemyId = battle.StageData.Enemies.Count > 0
                ? battle.StageData.Enemies[0].CharaId
                : "unknown";

            // 敵名を取得（GetName()でローカライズされた名前を取得）
            string enemyName = ArenaLocalization.Unknown;
            if (EClass.sources?.charas?.map != null &&
                EClass.sources.charas.map.TryGetValue(firstEnemyId, out var charaRow))
            {
                enemyName = charaRow.GetName() ?? charaRow.id;
            }

            string enemyInfo = battle.StageData.Enemies.Count == 1
                ? ArenaLocalization.FormatEnemyInfoSingle(enemyName, enemyCount)
                : ArenaLocalization.FormatEnemyInfoMultiple(enemyName, enemyCount);

            // スポーンパターン情報
            string spawnInfo = RandomEnemyGenerator.GetSpawnPatternDisplayName(battle.SpawnPattern, true);

            // ギミック情報（詳細説明付き）
            string gimmickInfo = ArenaGimmickSystem.GetGimmickDisplayTextWithDetails(battle.Gimmicks, true);

            return (enemyInfo, spawnInfo, gimmickInfo, battle.RewardPlat, battle.RewardPotion);
        }

        /// <summary>
        /// キャッシュをクリア（デバッグ用・リロール後）
        /// </summary>
        public static void ClearCache()
        {
            _cachedBattle = null;
            _cachedDay = -1;
            ModLog.Log("[TodaysBattleCache] Cache cleared");
        }
    }
}

