using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Flags;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// アリーナ関連のHarmonyパッチまとめ
    /// フェーズベースのクエスト管理とNPCマーカーをサポート
    /// </summary>
    public static class ArenaZonePatches
    {
        // 一度のセッションで開始済みのクエストを追跡（連続トリガー防止）
        private static string lastTriggeredQuestId = null;

        /// <summary>
        /// ゾーンがアリーナかどうかを判定
        /// </summary>
        public static bool IsArenaZone(Zone zone)
        {
            if (zone == null) return false;
            return zone.id == Plugin.ZoneId ||
                   zone.id?.Contains("sukutsu") == true ||
                   zone.id?.Contains("arena") == true;
        }

        /// <summary>
        /// クエスト完了時にトラッキングをリセット（次のクエストが自動開始可能になる）
        /// </summary>
        public static void ResetQuestTriggerTracking()
        {
            lastTriggeredQuestId = null;
            ModLog.Log("[SukutsuArena] Quest trigger tracking reset");
        }

        /// <summary>
        /// ゾーンがアクティブになった時のパッチ
        /// アリーナから敗北・勝利して戻った時に自動でアリーナマスターと会話を開始
        /// 自動発動クエストの開始とNPCマーカーの更新
        /// </summary>
        [GameDependency("Patch", "Zone.Activate", "High", "Method signature may change")]
        [HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
        public static class ArenaZoneActivatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Zone __instance)
            {
                int directDramaFlagDebug = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.DirectDrama) ? EClass.player.dialogFlags[SessionFlagKeys.DirectDrama] : -1;
                ModLog.Log($"[SukutsuArena] Zone.Activate Postfix: zone={__instance?.id}, DirectDrama flag={directDramaFlagDebug}, PendingDirectDrama={ZoneInstanceArenaBattle.PendingDirectDrama}");

                // 直接ドラマ開始（最終決戦など、NPCに依存しないドラマ）
                if (HandleDirectDrama())
                {
                    return;
                }

                // アリーナゾーン以外では自動会話処理をスキップ
                if (!IsArenaZone(__instance))
                {
                    return;
                }

                // 戦闘結果からの帰還かチェック
                // AutoDialogフラグ または IsQuestBattleResult でチェック
                // （StartBattleByStageWithoutMaster の場合は AutoDialog がセットされないため）
                bool hasAutoDialog = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.AutoDialog)
                    && EClass.player.dialogFlags[SessionFlagKeys.AutoDialog] > 0;
                bool hasQuestBattleResult = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.IsQuestBattleResult)
                    && EClass.player.dialogFlags[SessionFlagKeys.IsQuestBattleResult] > 0;
                bool isReturningFromBattle = hasAutoDialog || hasQuestBattleResult;

                // 戦闘結果の自動会話処理
                if (isReturningFromBattle)
                {
                    // NPC配置処理の完了を待ってからダイアログを開始
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        HandleBattleResultDialog();
                    });
                    return; // 戦闘帰還時は自動クエストをスキップ
                }

                // アリーナゾーンの処理
                if (IsArenaZone(__instance))
                {
                    // NPCマーカーをリフレッシュ
                    DOVirtual.DelayedCall(0.3f, () =>
                    {
                        ArenaQuestMarkerManager.Instance.RefreshAllMarkers();
                    });

                    // 自動発動クエストのチェックと開始
                    CheckAndTriggerAutoQuest();
                }
            }

            /// <summary>
            /// 直接ドラマ開始処理（NPCダイアログをスキップ）
            /// </summary>
            /// <returns>ドラマを開始した場合true</returns>
            private static bool HandleDirectDrama()
            {
                if (!EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.DirectDrama))
                    return false;

                int directDramaFlag = EClass.player.dialogFlags[SessionFlagKeys.DirectDrama];
                if (directDramaFlag != 1)
                    return false;

                string dramaId = ZoneInstanceArenaBattle.PendingDirectDrama;
                if (string.IsNullOrEmpty(dramaId))
                    return false;

                ModLog.Log($"[SukutsuArena] Direct drama triggered: {dramaId}");

                // フラグとドラマIDをクリア
                EClass.player.dialogFlags[SessionFlagKeys.DirectDrama] = 0;
                ZoneInstanceArenaBattle.PendingDirectDrama = "";

                // 少し遅延してドラマを開始（ゾーン移動完了を待つ）
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    try
                    {
                        // LayerDrama.Instanceがない状態から直接開始
                        // ArenaManager.StartDramaはドラマ終了後のチェーン用なのでここでは使えない
                        ModLog.Log($"[SukutsuArena] Direct activating drama: {dramaId}");
                        LayerDrama.Activate(dramaId, null, null, EClass.pc, null, null);
                        ModLog.Log($"[SukutsuArena] Direct drama activated successfully");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SukutsuArena] Failed to start direct drama: {ex.Message}");
                    }
                });

                return true;
            }

            /// <summary>
            /// 戦闘結果後の自動会話処理
            /// </summary>
            private static void HandleBattleResultDialog()
            {
                int masterUid = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.AutoDialog)
                    ? EClass.player.dialogFlags[SessionFlagKeys.AutoDialog] : 0;

                ModLog.Log($"[SukutsuArena] Auto-dialog triggered for master UID: {masterUid}");

                // AutoDialogフラグをクリア（ダイアログ開始のトリガーなので、ここでクリア）
                // 注意: IsQuestBattleResult と IsRankUpResult はドラマ側でクリアするため、ここではクリアしない
                if (EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.AutoDialog))
                {
                    EClass.player.dialogFlags[SessionFlagKeys.AutoDialog] = 0;
                }

                // アリーナマスターを探す
                Chara master = null;
                if (masterUid > 0)
                {
                    master = EClass.game.cards.Find(masterUid) as Chara;
                }

                // マスターが見つからない場合、マップ上のNPCを探す
                // バルガス → リリィ → ゼク の順で探す（死亡ルート対応）
                string[] npcSearchOrder = new[] {
                    ArenaConfig.NpcIds.Balgas,
                    ArenaConfig.NpcIds.Lily,
                    ArenaConfig.NpcIds.Zek
                };

                if (master == null || !master.ExistsOnMap)
                {
                    foreach (var npcId in npcSearchOrder)
                    {
                        foreach (var c in EClass._map.charas)
                        {
                            if (c.id == npcId)
                            {
                                master = c;
                                ModLog.Log($"[SukutsuArena] Found NPC for dialog: {npcId}");
                                break;
                            }
                        }
                        if (master != null && master.ExistsOnMap) break;
                    }
                }

                if (master != null && master.ExistsOnMap)
                {
                    // 戦闘結果ダイアログを直接指定（NpcQuestDialogPatchをバイパス）
                    // これによりクエストドラマではなく結果ダイアログが表示される
                    ModLog.Log($"[SukutsuArena] Showing battle result dialog with {master.Name}");
                    master.ShowDialog(ArenaConfig.DramaIds.ArenaMaster);
                }
                else
                {
                    // NPCが見つからない場合（全員死亡ルートなど）は直接ドラマを開始
                    ModLog.Log($"[SukutsuArena] No NPC found, starting arena_master drama directly");
                    LayerDrama.Activate(ArenaConfig.DramaIds.ArenaMaster, null, null, EClass.pc, null, null);
                }
            }

            /// <summary>
            /// 自動発動クエストをチェックして開始（フェーズベース）
            /// </summary>
            private static void CheckAndTriggerAutoQuest()
            {
                // 闘士未登録なら何もしない
                if (Core.ArenaContext.I.Storage.GetInt(SessionFlagKeys.Gladiator) == 0)
                    return;

                // 戦闘結果表示中はスキップ
                if (Core.ArenaContext.I.Storage.GetInt(SessionFlagKeys.ArenaResult) != 0)
                    return;

                // ランクアップ結果表示中はスキップ
                if (Core.ArenaContext.I.Storage.GetInt(SessionFlagKeys.IsRankUpResult) != 0)
                    return;

                // 自動発動クエストを取得（フェーズと条件でフィルタリング済み）
                var autoQuests = ArenaQuestManager.Instance.GetAutoTriggerQuests();

                // 連続トリガー防止と優先度でフィルタリング
                var quest = autoQuests
                    .Where(q => q.QuestId != lastTriggeredQuestId)
                    .OrderByDescending(q => q.Priority)
                    .FirstOrDefault();

                if (quest == null)
                {
                    ModLog.Log("[SukutsuArena] No auto-trigger quest available");
                    return;
                }

                ModLog.Log($"[SukutsuArena] Auto-triggering quest: {quest.QuestId} (Phase: {quest.Phase}, Drama: {quest.DramaId})");
                lastTriggeredQuestId = quest.QuestId;

                // 少し遅延させてからドラマを開始（ゾーン遷移の完了を待つ）
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    TriggerQuestDrama(quest);
                });
            }

            /// <summary>
            /// クエストのドラマを開始
            /// </summary>
            private static void TriggerQuestDrama(QuestDefinition quest)
            {
                // ドラマファイル名は "drama_{dramaId}" 形式
                string dramaName = $"drama_{quest.DramaId}";
                ModLog.Log($"[SukutsuArena] Triggering drama: {dramaName}");

                try
                {
                    // ドラマIDに基づいて対応するキャラクターを見つける
                    string targetCharaId = GetQuestTargetChara(quest);
                    Chara targetChara = null;

                    if (!string.IsNullOrEmpty(targetCharaId))
                    {
                        foreach (var c in EClass._map.charas)
                        {
                            if (c.id == targetCharaId)
                            {
                                targetChara = c;
                                break;
                            }
                        }
                    }

                    if (targetChara != null)
                    {
                        ModLog.Log($"[SukutsuArena] Starting dialog with {targetChara.Name} for quest {quest.QuestId}");
                        // キャラクターのShowDialogを使用してドラマを開始（actorが正しく設定される）
                        targetChara.ShowDialog(dramaName);
                    }
                    else
                    {
                        ModLog.Log($"[SukutsuArena] Target character not found, starting drama directly: {dramaName}");
                        // キャラクターが見つからない場合は直接開始（フォールバック）
                        LayerDrama.Activate(dramaName, null, null, null, null, null);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SukutsuArena] Failed to trigger drama: {ex}");
                }
            }

            /// <summary>
            /// クエストに対応する対話相手のキャラクターIDを取得
            /// quest_giverを優先し、未定義の場合はフォールバック
            /// </summary>
            private static string GetQuestTargetChara(QuestDefinition quest)
            {
                // quest_giverが設定されていればそれを使用
                if (!string.IsNullOrEmpty(quest.QuestGiver))
                {
                    return quest.QuestGiver;
                }

                // 自動発動クエスト（quest_giver = null）の場合はフォールバック
                return quest.QuestId switch
                {
                    // オープニング（リリィがナビゲーター）
                    "01_opening" => ArenaConfig.NpcIds.Lily,

                    // メインストーリー（リリィが語り手）
                    "13_makuma2" => ArenaConfig.NpcIds.Lily,
                    "17_escape" => ArenaConfig.NpcIds.Balgas,
                    "18_last_battle" => ArenaConfig.NpcIds.Balgas,

                    // デフォルトはリリィ
                    _ => ArenaConfig.NpcIds.Lily
                };
            }
        }

        // NpcQuestDialogPatchNoArgs は削除されました
        // クエスト選択はドラマファイル内の選択肢で対応します
        // 理由: return false で他Modと競合するリスクが高いため

        /// <summary>
        /// アリーナ戦闘ゾーンでは自動復活（ゲームオーバー回避）を有効にするパッチ
        /// </summary>
        [GameDependency("Patch", "Zone.get_ShouldAutoRevive", "High", "Property getter patch, may become method or change name")]
        [HarmonyPatch(typeof(Zone), "get_ShouldAutoRevive")]
        public static class ArenaZoneRevivePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Zone __instance, ref bool __result)
            {
                if (__instance.instance is ZoneInstanceArenaBattle)
                {
                    // アリーナバトル中は強制的に復活可能（ゲームオーバーにならない）
                    // 実際の復活処理と退出はZoneEventArenaBattle.OnCharaDieで行う
                    __result = true;
                }
            }
        }

        /// <summary>
        /// ダイアログ終了時にマーカーをリフレッシュするパッチ
        /// </summary>
        [GameDependency("Patch", "LayerDrama.OnKill", "High", "Drama layer lifecycle method")]
        [HarmonyPatch(typeof(LayerDrama), nameof(LayerDrama.OnKill))]
        public static class DramaEndMarkerRefreshPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // アリーナ外では更新不要。遅延Call予約自体を避ける。
                if (!IsArenaZone(EClass._zone))
                {
                    return;
                }

                ModLog.Log("[ArenaMarker] Drama layer closed, refreshing markers");

                // 少し遅延させてリフレッシュ（ダイアログ終了処理完了後）
                DOVirtual.DelayedCall(0.1f, () =>
                {
                    if (IsArenaZone(EClass._zone))
                    {
                        ArenaQuestMarkerManager.Instance.RefreshAllMarkers();
                    }
                });
            }
        }

        /// <summary>
        /// TickConditionsの直後にマーカーを設定するパッチ
        /// TickConditionsでemoIconがnoneにリセットされるので、直後に再設定する
        /// これにより点滅を防ぐ
        /// </summary>
        [GameDependency("Patch", "Chara.TickConditions", "High", "Character tick processing, emoIcon reset behavior")]
        [HarmonyPatch(typeof(Chara), nameof(Chara.TickConditions))]
        public static class CharaTickConditionsMarkerPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Chara __instance)
            {
                try
                {
                    // アリーナゾーン外ではスキップ
                    if (!IsArenaZone(EClass._zone)) return;

                    // このキャラがクエストマーカーを持つべきかチェック
                    var npcsWithQuests = ArenaQuestMarkerManager.Instance.GetNpcsWithQuestsList();
                    if (npcsWithQuests == null || !npcsWithQuests.Contains(__instance.id)) return;

                    // emoIcon = Emo2.hint で「！」マーカーを直接表示
                    // TickConditionsでnoneにリセットされた直後に設定するので点滅しない
                    __instance.emoIcon = Emo2.hint;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ArenaZonePatches.TickConditionsPatch] Quest marker update failed: {ex.Message}");
                }
            }
        }
    }
}

