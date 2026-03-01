using System;
using System.Collections.Generic;
using UnityEngine;
using Elin_SukutsuArena.RandomBattle;
using Debug = UnityEngine.Debug;

namespace Elin_SukutsuArena.Debugging
{
    /// <summary>
    /// ランダムバトルのデバッグ用ヘルパークラス
    /// </summary>
    public static class RandomBattleDebug
    {
        // デバッグ用の一時的な最深層オーバーライド
        private static int? _overrideDeepest = null;

        // デバッグ用の強制ギミック
        private static List<ArenaGimmickType> _forceGimmicks = null;

        // デバッグ用の強制敵ID
        private static string _forceEnemyId = null;

        // デバッグ用の強制配置パターン
        private static SpawnPattern? _forcePattern = null;

        /// <summary>
        /// デバッグ用に最深層をオーバーライド
        /// </summary>
        public static void SetDeepestOverride(int? deepest)
        {
            _overrideDeepest = deepest;
            if (deepest.HasValue)
            {
                ModLog.Log($"[RandomBattleDebug] Deepest override set to: {deepest.Value}");
                Msg.Say($"[DEBUG] 最深層を {deepest.Value} に設定しました");
            }
            else
            {
                ModLog.Log("[RandomBattleDebug] Deepest override cleared");
                Msg.Say("[DEBUG] 最深層オーバーライドを解除しました");
            }
        }

        /// <summary>
        /// デバッグ用にギミックを強制設定
        /// </summary>
        public static void SetForceGimmicks(List<ArenaGimmickType> gimmicks)
        {
            _forceGimmicks = gimmicks;

            // キャッシュをクリアして次回生成時に新しいギミックが反映されるようにする
            TodaysBattleCache.ClearCache();

            if (gimmicks != null && gimmicks.Count > 0)
            {
                var names = ArenaGimmickSystem.GetGimmickDisplayText(gimmicks, true);
                ModLog.Log($"[RandomBattleDebug] Force gimmicks set: {names}");
                Msg.Say($"[DEBUG] ギミック強制: {names}");
            }
            else
            {
                ModLog.Log("[RandomBattleDebug] Force gimmicks cleared");
                Msg.Say("[DEBUG] ギミック強制を解除しました");
            }
        }

        /// <summary>
        /// デバッグ用に敵を強制設定
        /// </summary>
        public static void SetForceEnemy(string enemyId)
        {
            _forceEnemyId = enemyId;
            if (!string.IsNullOrEmpty(enemyId))
            {
                ModLog.Log($"[RandomBattleDebug] Force enemy set: {enemyId}");
                Msg.Say($"[DEBUG] 敵強制: {enemyId}");
            }
            else
            {
                ModLog.Log("[RandomBattleDebug] Force enemy cleared");
                Msg.Say("[DEBUG] 敵強制を解除しました");
            }
        }

        /// <summary>
        /// デバッグ用に配置パターンを強制設定
        /// </summary>
        public static void SetForcePattern(SpawnPattern? pattern)
        {
            _forcePattern = pattern;
            if (pattern.HasValue)
            {
                string patternName = pattern.Value switch
                {
                    SpawnPattern.Horde => "群れ (Horde)",
                    SpawnPattern.Mixed => "混成 (Mixed)",
                    SpawnPattern.BossWithMinions => "ボス+取り巻き (BossWithMinions)",
                    SpawnPattern.Random => "ランダム (Random)",
                    _ => pattern.Value.ToString()
                };
                ModLog.Log($"[RandomBattleDebug] Force pattern set: {pattern.Value}");
                Msg.Say($"[DEBUG] パターン強制: {patternName}");
            }
            else
            {
                ModLog.Log("[RandomBattleDebug] Force pattern cleared");
                Msg.Say("[DEBUG] パターン強制を解除しました");
            }
        }

        /// <summary>
        /// デバッグ用に配置パターンを強制設定（int版 - ドラマから呼び出し用）
        /// </summary>
        public static void SetForcePatternByIndex(int index)
        {
            if (index < 0 || index > 3)
            {
                SetForcePattern(null);
            }
            else
            {
                SetForcePattern((SpawnPattern)index);
            }
        }

        /// <summary>
        /// オーバーライドされた最深層を取得（オーバーライドがなければ実際の値）
        /// </summary>
        public static int GetEffectiveDeepest()
        {
            return _overrideDeepest ?? EClass.player.stats.deepest;
        }

        /// <summary>
        /// 強制ギミックを取得
        /// </summary>
        public static List<ArenaGimmickType> GetForceGimmicks()
        {
            return _forceGimmicks;
        }

        /// <summary>
        /// 強制敵IDを取得
        /// </summary>
        public static string GetForceEnemyId()
        {
            return _forceEnemyId;
        }

        /// <summary>
        /// 強制配置パターンを取得
        /// </summary>
        public static SpawnPattern? GetForcePattern()
        {
            return _forcePattern;
        }

        /// <summary>
        /// 全てのデバッグオーバーライドをクリア
        /// </summary>
        public static void ClearAllOverrides()
        {
            _overrideDeepest = null;
            _forceGimmicks = null;
            _forceEnemyId = null;
            _forcePattern = null;
            ModLog.Log("[RandomBattleDebug] All overrides cleared");
            Msg.Say("[DEBUG] 全てのオーバーライドを解除しました");
        }

        /// <summary>
        /// 即座にランダムバトルを開始（デバッグ用）
        /// </summary>
        public static void StartDebugBattle()
        {
            try
            {
                // アリーナマスターを検索
                var master = EClass._zone.FindChara(Arena.ArenaConfig.NpcIds.Balgas);
                if (master == null)
                {
                    // PC自身を使用
                    ArenaManager.StartRandomBattle(EClass.pc);
                }
                else
                {
                    ArenaManager.StartRandomBattle(master);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[RandomBattleDebug] Error starting debug battle: {ex.Message}");
                Msg.Say($"[DEBUG] エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// デバッグ用に単一ギミックを強制設定（int版 - ドラマから呼び出し用）
        /// </summary>
        /// <param name="index">ギミックインデックス (0=なし, 1=AntiMagic, 2=Critical, ...)</param>
        public static void SetForceGimmickByIndex(int index)
        {
            if (index <= 0)
            {
                SetForceGimmicks(null);
            }
            else
            {
                var gimmickType = (ArenaGimmickType)index;
                SetForceGimmicks(new List<ArenaGimmickType> { gimmickType });
            }
        }

    }
}

