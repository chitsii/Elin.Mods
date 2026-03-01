using System.Collections.Generic;
#if !NUNIT
using UnityEngine;
#endif

namespace Elin_SukutsuArena.Migration
{
    /// <summary>
    /// 古いフラグキーのクリーンアップとマイグレーション
    /// </summary>
    public static class FlagMigration
    {
        private const string CleanupVersionKey = "chitsii.arena.cleanup_version";
        private const int CurrentCleanupVersion = 2;

        /// <summary>
        /// 削除対象の古いキー
        /// これらは設定されていたが読み取りが行われていなかったバグがあったキー
        /// </summary>
        private static readonly string[] LegacyKeysToRemove = new[]
        {
            "chitsii.arena.lily_trust_rebuild",   // 旧: player.プレフィックスなし
            "chitsii.arena.balgas_trust_broken",  // 旧: player.プレフィックスなし
            "chitsii.arena.balgas_killed",        // 旧: player.プレフィックスなし
            "chitsii.arena.ending",               // 旧: player.プレフィックスなし
            "chitsii.arena.story.phase",          // 旧: current_phaseに統一
            // 削除済みフラグ（旧データ掃除用）
            "chitsii.arena.player.motivation",
            "chitsii.arena.player.karma",
            "chitsii.arena.player.null_chip_obtained",
            "chitsii.arena.player.lily_true_name",
            "chitsii.arena.player.kain_soul_confession",
            "chitsii.arena.player.lily_trust_rebuilding",
            "chitsii.arena.player.lily_hostile",
            "chitsii.arena.player.balgas_trust_broken",
        };

#if !NUNIT
        /// <summary>
        /// 古いフラグキーをクリーンアップ（ゲーム内呼び出し用）
        /// </summary>
        public static void TryCleanup()
        {
            var flags = EClass.player?.dialogFlags;
            if (flags == null) return;

            TryCleanup(flags);
        }
#endif

        /// <summary>
        /// 古いフラグキーをクリーンアップ（テスト用オーバーロード）
        /// </summary>
        /// <param name="flags">フラグ辞書</param>
        /// <returns>削除されたキーの数</returns>
        public static int TryCleanup(IDictionary<string, int> flags)
        {
            if (flags == null) return 0;

            // 既にクリーンアップ済みならスキップ
            if (flags.TryGetValue(CleanupVersionKey, out int version) && version >= CurrentCleanupVersion)
                return 0;

            var removedCount = 0;
            foreach (var key in LegacyKeysToRemove)
            {
                if (flags.Remove(key))
                {
                    removedCount++;
                }
            }

            flags[CleanupVersionKey] = CurrentCleanupVersion;

            if (removedCount > 0)
            {
#if !NUNIT
                ModLog.Log($"[SukutsuArena] Cleanup: Removed {removedCount} legacy flags");
#endif
            }

            return removedCount;
        }

        /// <summary>
        /// 現在のクリーンアップバージョンを取得
        /// </summary>
        public static int GetCurrentCleanupVersion() => CurrentCleanupVersion;

        /// <summary>
        /// クリーンアップバージョンキーを取得
        /// </summary>
        public static string GetCleanupVersionKey() => CleanupVersionKey;

        /// <summary>
        /// 削除対象のレガシーキーを取得（テスト用）
        /// </summary>
        public static IReadOnlyList<string> GetLegacyKeysToRemove() => LegacyKeysToRemove;
    }
}

