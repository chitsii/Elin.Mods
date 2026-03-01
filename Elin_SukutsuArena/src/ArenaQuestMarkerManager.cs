using System;
using System.Collections.Generic;
using System.Linq;
using Elin_SukutsuArena.Arena;
using UnityEngine;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// アリーナNPCのクエストマーカー（!マーク）を管理する
    /// ElinのEmo2.hintシステムを利用
    /// </summary>
    public class ArenaQuestMarkerManager
    {
        private static ArenaQuestMarkerManager _instance;
        public static ArenaQuestMarkerManager Instance => _instance ?? (_instance = new ArenaQuestMarkerManager());

        // 現在マーカーが表示されているNPC
        private HashSet<string> npcsWithMarkers = new HashSet<string>();

        private ArenaQuestMarkerManager()
        {
            // クエスト状態変更イベントにサブスクライブ
            ArenaQuestManager.Instance.OnQuestStateChanged += RefreshAllMarkers;
        }

        /// <summary>
        /// 全マーカーをリフレッシュ
        /// </summary>
        public void RefreshAllMarkers()
        {
            ModLog.Log($"[ArenaMarker] RefreshAllMarkers called. Stack: {new System.Diagnostics.StackTrace(1, true).ToString().Split('\n')[0]}");

            if (!IsInArenaZone())
            {
                // アリーナゾーン外ではマーカーをすべてクリア
                ModLog.Log("[ArenaMarker] Not in arena zone, clearing markers");
                ClearAllMarkers();
                return;
            }

            // クエストを持っているNPCを取得
            var npcsWithQuests = ArenaQuestManager.Instance.GetNpcsWithQuests();
            var currentNpcsWithQuests = new HashSet<string>(npcsWithQuests);

            // 詳細デバッグ: 利用可能なクエストをすべて表示
            var availableQuests = ArenaQuestManager.Instance.GetAvailableQuests();
            ModLog.Log($"[ArenaMarker] Available quests ({availableQuests.Count}): {string.Join(", ", availableQuests.Select(q => $"{q.QuestId}[giver={q.QuestGiver}]"))}");
            ModLog.Log($"[ArenaMarker] NPCs with quests: [{string.Join(", ", npcsWithQuests)}], Currently marked: [{string.Join(", ", npcsWithMarkers)}]");

            // 新しくクエストを持ったNPCにマーカーを追加
            foreach (var npcId in currentNpcsWithQuests)
            {
                if (!npcsWithMarkers.Contains(npcId))
                {
                    ModLog.Log($"[ArenaMarker] +ADD marker for: {npcId}");
                    ShowMarkerForNpc(npcId);
                    npcsWithMarkers.Add(npcId);
                }
            }

            // クエストがなくなったNPCからマーカーを削除
            var toRemove = new List<string>();
            foreach (var npcId in npcsWithMarkers)
            {
                if (!currentNpcsWithQuests.Contains(npcId))
                {
                    ModLog.Log($"[ArenaMarker] -REMOVE marker for: {npcId} (no longer has quests)");
                    HideMarkerForNpc(npcId);
                    toRemove.Add(npcId);
                }
            }
            foreach (var npcId in toRemove)
            {
                npcsWithMarkers.Remove(npcId);
            }

            ModLog.Log($"[ArenaMarker] Refresh complete. Final marked NPCs: [{string.Join(", ", npcsWithMarkers)}]");
        }

        /// <summary>
        /// 特定NPCにマーカーを表示
        /// emoIcon = Emo2.hint で「！」マーカーを直接設定
        /// GetInt(71)は弁当渡しがトリガーされるため使用しない
        /// </summary>
        private void ShowMarkerForNpc(string npcId)
        {
            var npc = FindNpcById(npcId);
            if (npc != null)
            {
                var oldIcon = npc.emoIcon;
                npc.emoIcon = Emo2.hint;
                RefreshNpcIcon(npc);
                ModLog.Log($"[ArenaMarker] Showing marker for NPC: {npcId} (was: {oldIcon}, now: Emo2.hint)");
            }
            else
            {
                Debug.LogWarning($"[ArenaMarker] NPC not found for marker: {npcId}");
            }
        }

        /// <summary>
        /// 特定NPCのマーカーを非表示
        /// </summary>
        private void HideMarkerForNpc(string npcId)
        {
            var npc = FindNpcById(npcId);
            if (npc != null)
            {
                npc.emoIcon = Emo2.none;
                RefreshNpcIcon(npc);
                ModLog.Log($"[ArenaMarker] Hiding marker for NPC: {npcId}");
            }
        }

        /// <summary>
        /// 全マーカーをクリア
        /// </summary>
        private void ClearAllMarkers()
        {
            foreach (var npcId in npcsWithMarkers)
            {
                var npc = FindNpcById(npcId);
                if (npc != null)
                {
                    npc.emoIcon = Emo2.none;
                    RefreshNpcIcon(npc);
                }
            }
            npcsWithMarkers.Clear();
        }

        /// <summary>
        /// NPCのアイコン表示をリフレッシュ
        /// </summary>
        private void RefreshNpcIcon(Chara npc)
        {
            try
            {
                if (npc.renderer != null)
                {
                    npc.renderer.RefreshStateIcon();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ArenaMarker] Failed to refresh NPC icon: {ex.Message}");
            }
        }

        /// <summary>
        /// IDからNPCを検索
        /// </summary>
        private Chara FindNpcById(string npcId)
        {
            try
            {
                // 現在のゾーン内のNPCを検索
                var zone = EClass._zone;
                if (zone == null) return null;

                foreach (var chara in zone.map?.charas ?? new List<Chara>())
                {
                    if (chara.id == npcId)
                    {
                        return chara;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ArenaMarker] Error finding NPC: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// アリーナゾーン内かどうかをチェック
        /// </summary>
        private bool IsInArenaZone()
        {
            try
            {
                return ArenaZonePatches.IsArenaZone(EClass._zone);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// NPCがクエストを持っているかチェック
        /// </summary>
        public bool NpcHasQuest(string npcId)
        {
            return npcsWithMarkers.Contains(npcId);
        }

        /// <summary>
        /// マーカーを持つべきNPCのリストを取得（Update用）
        /// </summary>
        public HashSet<string> GetNpcsWithQuestsList()
        {
            return npcsWithMarkers;
        }

        /// <summary>
        /// NPCの次のクエストを取得
        /// </summary>
        public QuestDefinition GetNextQuestForNpc(string npcId)
        {
            var quests = ArenaQuestManager.Instance.GetQuestsForNpc(npcId);
            return quests.Count > 0 ? quests[0] : null;
        }
    }
}

