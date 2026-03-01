using System.Collections.Generic;
using System.Linq;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using Elin_SukutsuArena.Localization;
using UnityEngine;

namespace Elin_SukutsuArena.Reset
{
    /// <summary>
    /// アリーナModのリセット機能を管理
    /// </summary>
    public static class ArenaResetManager
    {
        // CWL SourceQuest.xlsx で定義したクエストIDプレフィックス
        private const string QuestIdPrefix = "sukutsu_arena";

        // フラグプレフィックス
        private static readonly string[] FlagPrefixes =
        {
            ArenaFlagKeys.Prefix,             // "chitsii.arena"
            SessionFlagKeys.Prefix            // "sukutsu_"
        };

        // NG+で保持するフラグ（ランクのみ）
        private static readonly HashSet<string> NewGamePlusKeepFlags = new()
        {
            ArenaFlagKeys.Rank                // "chitsii.arena.player.rank"
        };

        // 削除対象フィートID（カスタムフィートのみ。バニラフィートは削除しない）
        private static readonly int[] ArenaFeatIds =
        {
            10001,  // 闘志（カスタムフィート）
        };

        // Mod固有Conditionの型名プレフィックス
        private static readonly string[] ModConditionPrefixes =
        {
            "ConSukutsu",      // ConSukutsuBleed, ConSukutsuBoost, ConSukutsuPoison, ConSukutsuResistBuff
            "ConAstaroth",     // ConAstarothDeletion, ConAstarothDenial, ConAstarothTyranny
            "ConIris"          // ConIrisSixthSense, ConIrisIronLegs, ConIrisBoundaryPeace
        };

        // Mod固有ゾーンの型名（アリーナ以外のカスタムゾーン）
        private static readonly string[] ModZoneTypeNames =
        {
            "Zone_FieldFine",
            "Zone_FieldSnow"
        };

        // Mod NPC ID一覧
        private static readonly string[] ModNpcIds =
        {
            ArenaConfig.NpcIds.Lily,
            ArenaConfig.NpcIds.Balgas,
            ArenaConfig.NpcIds.Zek,
            ArenaConfig.NpcIds.Nul,
            ArenaConfig.NpcIds.Astaroth,
            ArenaConfig.NpcIds.Iris
        };

        /// <summary>
        /// リセット結果
        /// </summary>
        public struct ResetResult
        {
            public int FlagsRemoved;
            public int FeatsRemoved;
            public bool ZoneRemoved;
            public int PetsRemoved;
            public int QuestsRemoved;
            public int ConditionsRemoved;
            public int ExtraZonesRemoved;
        }

        /// <summary>
        /// リセットを実行
        /// </summary>
        /// <param name="level">リセットレベル</param>
        /// <param name="showGameLog">ゲーム内ログを表示するか（デフォルト: true）</param>
        public static ResetResult Execute(ResetLevel level, bool showGameLog = true)
        {
            var result = new ResetResult();

            // 開始ログ
            if (showGameLog)
            {
                if (level == ResetLevel.Uninstall)
                    Msg.Say(ArenaLocalization.UninstallStarting);
                else
                    Msg.Say(ArenaLocalization.NewGamePlusStarting);
            }

            // 1. ペット化NPCを離脱（両レベル共通）
            result.PetsRemoved = RemoveModPetsFromParty();
            if (showGameLog && result.PetsRemoved > 0)
                Msg.Say(ArenaLocalization.UninstallPetsDismissed(result.PetsRemoved));

            // 2. フラグ削除
            result.FlagsRemoved = ResetDialogFlags(level);
            if (showGameLog && result.FlagsRemoved > 0)
                Msg.Say(ArenaLocalization.UninstallFlagsRemoved(result.FlagsRemoved));

            // 3. ゲーム本体のQuestリストからMODクエストを削除（両レベル共通）
            result.QuestsRemoved = RemoveModQuests();
            if (showGameLog && result.QuestsRemoved > 0)
                Msg.Say(ArenaLocalization.UninstallQuestsRemoved(result.QuestsRemoved));

            if (level == ResetLevel.Uninstall)
            {
                // 4. フィート削除
                result.FeatsRemoved = ResetFeats();
                if (showGameLog && result.FeatsRemoved > 0)
                    Msg.Say(ArenaLocalization.UninstallFeatsRemoved(result.FeatsRemoved));

                // 5. Mod固有Condition削除
                result.ConditionsRemoved = RemoveModConditions();
                if (showGameLog && result.ConditionsRemoved > 0)
                    Msg.Say(ArenaLocalization.UninstallConditionsRemoved(result.ConditionsRemoved));

                // 6. ゾーン削除（アリーナ本体）
                result.ZoneRemoved = RemoveArenaZone();
                if (showGameLog && result.ZoneRemoved)
                    Msg.Say(ArenaLocalization.UninstallArenaZoneRemoved);

                // 7. 追加ゾーン削除（Zone_FieldFine, Zone_FieldSnow）
                result.ExtraZonesRemoved = RemoveExtraZones();
                if (showGameLog && result.ExtraZonesRemoved > 0)
                    Msg.Say(ArenaLocalization.UninstallExtraZonesRemoved(result.ExtraZonesRemoved));
            }

            // 8. コンテキストをリフレッシュ
            ArenaContext.ResetInstance();

            // 完了ログ
            if (showGameLog && level == ResetLevel.Uninstall)
                Msg.Say(ArenaLocalization.UninstallComplete);

            ModLog.Log($"[ArenaReset] Level={level}, Flags={result.FlagsRemoved}, " +
                      $"Feats={result.FeatsRemoved}, Zone={result.ZoneRemoved}, " +
                      $"Pets={result.PetsRemoved}, Quests={result.QuestsRemoved}, " +
                      $"Conditions={result.ConditionsRemoved}, ExtraZones={result.ExtraZonesRemoved}");

            return result;
        }

        /// <summary>
        /// Mod NPCをパーティから離脱させる
        /// </summary>
        private static int RemoveModPetsFromParty()
        {
            if (EClass.pc?.party?.members == null)
                return 0;

            int removed = 0;
            var members = EClass.pc.party.members.ToList();

            // 移動先: アリーナゾーン（存在すれば）
            var arenaZone = EClass.game?.spatials?.Find(ArenaConfig.ZoneId) as Zone;

            foreach (var member in members)
            {
                if (member == null || member == EClass.pc)
                    continue;

                if (ModNpcIds.Contains(member.id))
                {
                    ModLog.Log($"[ArenaReset] Removing pet: {member.Name} ({member.id})");

                    // パーティから除外
                    EClass.pc.party.RemoveMember(member);

                    // アリーナに戻す（存在する場合）
                    // Uninstall時はアリーナが後で削除されるため、
                    // NPCはマップ上に残らない（ゾーン削除で一緒に消える）
                    if (arenaZone != null)
                    {
                        member.MoveZone(arenaZone);
                        member.homeZone = arenaZone;
                    }

                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// dialogFlagsをリセット
        /// </summary>
        private static int ResetDialogFlags(ResetLevel level)
        {
            var flags = EClass.player?.dialogFlags;
            if (flags == null)
                return 0;

            var keysToRemove = flags.Keys
                .Where(k => FlagPrefixes.Any(p => k.StartsWith(p)))
                .Where(k => level == ResetLevel.Uninstall ||
                            !NewGamePlusKeepFlags.Contains(k))
                .ToList();

            foreach (var key in keysToRemove)
            {
                flags.Remove(key);
            }

            return keysToRemove.Count;
        }

        /// <summary>
        /// フィートを削除
        /// </summary>
        private static int ResetFeats()
        {
            if (EClass.pc?.elements == null)
                return 0;

            int removed = 0;
            foreach (var featId in ArenaFeatIds)
            {
                if (EClass.pc.elements.Has(featId))
                {
                    EClass.pc.elements.Remove(featId);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// アリーナゾーンを削除
        /// </summary>
        private static bool RemoveArenaZone()
        {
            var zone = EClass.game?.spatials?.Find(ArenaConfig.ZoneId) as Zone;
            if (zone == null)
                return false;

            // プレイヤーがゾーン内なら安全なゾーンへ移動
            if (EClass._zone?.id == ArenaConfig.ZoneId)
            {
                // 優先順位: ホーム拠点 > 開始ゾーン（ダルフィ）
                Zone safeZone = EClass.pc.homeZone ?? EClass.game.StartZone;
                if (safeZone != null && safeZone.id != ArenaConfig.ZoneId)
                {
                    EClass.pc.MoveZone(safeZone, ZoneTransition.EnterState.Return);
                    ModLog.Log($"[ArenaReset] Moved player to {safeZone.id}");
                }
            }

            // ゾーン削除
            zone.DeleteMapRecursive();
            EClass.game.spatials.Remove(zone);
            return true;
        }

        /// <summary>
        /// Mod固有のConditionをPCおよびパーティメンバーから削除
        /// </summary>
        private static int RemoveModConditions()
        {
            int removed = 0;

            // PCのCondition削除
            removed += RemoveConditionsFromChara(EClass.pc);

            // パーティメンバーのCondition削除
            if (EClass.pc?.party?.members != null)
            {
                foreach (var member in EClass.pc.party.members)
                {
                    if (member != null && member != EClass.pc)
                    {
                        removed += RemoveConditionsFromChara(member);
                    }
                }
            }

            ModLog.Log($"[ArenaReset] Removed {removed} mod conditions");
            return removed;
        }

        /// <summary>
        /// 指定キャラからMod固有Conditionを削除
        /// </summary>
        private static int RemoveConditionsFromChara(Chara chara)
        {
            if (chara?.conditions == null)
                return 0;

            int removed = 0;
            var toRemove = chara.conditions
                .Where(c => c != null && IsModCondition(c.GetType().Name))
                .ToList();

            foreach (var condition in toRemove)
            {
                ModLog.Log($"[ArenaReset] Removing condition {condition.GetType().Name} from {chara.Name}");
                condition.Kill(silent: true);
                removed++;
            }

            return removed;
        }

        /// <summary>
        /// Mod固有のConditionかどうか判定
        /// </summary>
        private static bool IsModCondition(string typeName)
        {
            return ModConditionPrefixes.Any(prefix => typeName.StartsWith(prefix));
        }

        /// <summary>
        /// 追加のMod固有ゾーン（Zone_FieldFine, Zone_FieldSnow）を削除
        /// </summary>
        private static int RemoveExtraZones()
        {
            if (EClass.game?.spatials == null)
                return 0;

            int removed = 0;

            // 全ゾーンからMod固有のものを探して削除
            var zonesToRemove = EClass.game.spatials.map.Values
                .OfType<Zone>()
                .Where(z => z != null && ModZoneTypeNames.Contains(z.GetType().Name))
                .ToList();

            foreach (var zone in zonesToRemove)
            {
                ModLog.Log($"[ArenaReset] Removing extra zone: {zone.GetType().Name} (id: {zone.id})");

                // プレイヤーがそのゾーン内にいる場合は移動
                if (EClass._zone == zone)
                {
                    Zone safeZone = EClass.pc.homeZone ?? EClass.game.StartZone;
                    if (safeZone != null)
                    {
                        EClass.pc.MoveZone(safeZone, ZoneTransition.EnterState.Return);
                    }
                }

                zone.DeleteMapRecursive();
                EClass.game.spatials.Remove(zone);
                removed++;
            }

            ModLog.Log($"[ArenaReset] Removed {removed} extra zones");
            return removed;
        }

        /// <summary>
        /// ゲーム本体のQuestリストからMODクエストを削除
        /// </summary>
        private static int RemoveModQuests()
        {
            var quests = EClass.game?.quests;
            if (quests == null) return 0;

            int removed = 0;

            // list から削除
            var toRemoveList = quests.list
                .Where(q => q.id != null && q.id.StartsWith(QuestIdPrefix))
                .ToList();
            foreach (var q in toRemoveList)
            {
                quests.list.Remove(q);
                removed++;
            }

            // globalList から削除
            var toRemoveGlobal = quests.globalList
                .Where(q => q.id != null && q.id.StartsWith(QuestIdPrefix))
                .ToList();
            foreach (var q in toRemoveGlobal)
            {
                quests.globalList.Remove(q);
                removed++;
            }

            // completedIDs から削除
            var toRemoveCompleted = quests.completedIDs
                .Where(id => id.StartsWith(QuestIdPrefix))
                .ToList();
            foreach (var id in toRemoveCompleted)
            {
                quests.completedIDs.Remove(id);
            }

            ModLog.Log($"[ArenaReset] Removed {removed} quests from game quest system");
            return removed;
        }

        /// <summary>
        /// プレイヤーがアリーナ内にいるかチェック
        /// Uninstall時はアリーナ内からの実行をブロックする
        /// </summary>
        public static bool IsPlayerInArena()
        {
            return EClass._zone?.id == ArenaConfig.ZoneId;
        }

        // ========================================
        // ドラマから呼び出し用の簡易メソッド
        // ========================================

        /// <summary>
        /// NewGame+リセットを実行（ドラマから呼び出し）
        /// </summary>
        public static void ExecuteNewGamePlus()
        {
            Execute(ResetLevel.NewGamePlus, showGameLog: true);
        }

        /// <summary>
        /// Uninstallリセットを実行（ドラマから呼び出し）
        /// アリーナ内チェック付き
        /// </summary>
        public static void ExecuteUninstall()
        {
            // アリーナ内チェック
            if (IsPlayerInArena())
            {
                Msg.Say(ArenaLocalization.CannotUninstallInArena);
                return;
            }

            Execute(ResetLevel.Uninstall, showGameLog: true);
        }

        /// <summary>
        /// ドライラン: 削除対象を数えるだけで実際には削除しない
        /// コンソールからの確認用
        /// </summary>
        public static ResetResult DryRun(ResetLevel level)
        {
            var result = new ResetResult();

            // フラグ数をカウント
            var flags = EClass.player?.dialogFlags;
            if (flags != null)
            {
                result.FlagsRemoved = flags.Keys
                    .Where(k => FlagPrefixes.Any(p => k.StartsWith(p)))
                    .Where(k => level == ResetLevel.Uninstall ||
                                !NewGamePlusKeepFlags.Contains(k))
                    .Count();
            }

            // ペット数をカウント
            if (EClass.pc?.party?.members != null)
            {
                result.PetsRemoved = EClass.pc.party.members
                    .Where(m => m != null && m != EClass.pc && ModNpcIds.Contains(m.id))
                    .Count();
            }

            // クエスト数をカウント
            var quests = EClass.game?.quests;
            if (quests != null)
            {
                result.QuestsRemoved = quests.list.Count(q => q.id?.StartsWith(QuestIdPrefix) == true)
                                     + quests.globalList.Count(q => q.id?.StartsWith(QuestIdPrefix) == true);
            }

            if (level == ResetLevel.Uninstall)
            {
                // フィート数をカウント
                if (EClass.pc?.elements != null)
                {
                    result.FeatsRemoved = ArenaFeatIds.Count(id => EClass.pc.elements.Has(id));
                }

                // Condition数をカウント
                result.ConditionsRemoved = CountModConditions();

                // ゾーン存在チェック
                result.ZoneRemoved = EClass.game?.spatials?.Find(ArenaConfig.ZoneId) != null;

                // 追加ゾーン数をカウント
                if (EClass.game?.spatials != null)
                {
                    result.ExtraZonesRemoved = EClass.game.spatials.map.Values
                        .OfType<Zone>()
                        .Count(z => z != null && ModZoneTypeNames.Contains(z.GetType().Name));
                }
            }

            return result;
        }

        /// <summary>
        /// Mod固有Conditionの数をカウント
        /// </summary>
        private static int CountModConditions()
        {
            int count = 0;

            // PCのCondition
            if (EClass.pc?.conditions != null)
            {
                count += EClass.pc.conditions.Count(c => c != null && IsModCondition(c.GetType().Name));
            }

            // パーティメンバーのCondition
            if (EClass.pc?.party?.members != null)
            {
                foreach (var member in EClass.pc.party.members)
                {
                    if (member != null && member != EClass.pc && member.conditions != null)
                    {
                        count += member.conditions.Count(c => c != null && IsModCondition(c.GetType().Name));
                    }
                }
            }

            return count;
        }
    }
}

