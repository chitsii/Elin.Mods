using System.Collections.Generic;
using System.Linq;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// check_available_quests(npcId)
    /// NPCごとの利用可能クエストをチェックし、フラグを設定
    /// ドラマ側で条件分岐に使用する
    /// </summary>
    public class CheckAvailableQuestsCommand : IArenaCommand
    {
        public string Name => "check_available_quests";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            string npcId = args.Length > 0 ? args[0] : "";

            ModLog.Log($"[CheckAvailableQuests] Checking for NPC: {npcId}");

            // NPCに対応するクエストを取得
            var quests = string.IsNullOrEmpty(npcId)
                ? ArenaQuestManager.Instance.GetAvailableQuests()
                : ArenaQuestManager.Instance.GetQuestsForNpc(npcId);

            // 利用可能クエスト数を設定
            ctx.Storage.SetInt(SessionFlagKeys.AvailableQuestCount, quests.Count);

            // 各クエストタイプの有無をフラグに設定
            bool hasRankUp = quests.Any(q => q.QuestType == "rank_up");
            bool hasCharacterEvent = quests.Any(q => q.QuestType == "character_event");
            bool hasSubQuest = quests.Any(q => q.QuestType == "sub_quest");

            ctx.Storage.SetInt(SessionFlagKeys.HasRankUp, hasRankUp ? 1 : 0);
            ctx.Storage.SetInt(SessionFlagKeys.HasCharacterEvent, hasCharacterEvent ? 1 : 0);
            ctx.Storage.SetInt(SessionFlagKeys.HasSubQuest, hasSubQuest ? 1 : 0);

            // 最優先のクエストIDを設定（選択肢表示用）
            if (quests.Count > 0)
            {
                var topQuest = quests.OrderByDescending(q => q.Priority).First();
                ctx.Storage.SetInt(SessionFlagKeys.TopQuestId, topQuest.QuestId.GetHashCode());

                ModLog.Log($"[CheckAvailableQuests] Found {quests.Count} quests, top: {topQuest.QuestId}");
            }
            else
            {
                ctx.Storage.SetInt(SessionFlagKeys.TopQuestId, 0);
                ModLog.Log("[CheckAvailableQuests] No quests available");
            }

            // ランクアップクエストの番号をマッピング（quest_id -> rank値）
            // Note: クエストIDはflag_definitions.py QuestIdsと一致させること
            var rankUpMapping = new Dictionary<string, int>
            {
                { "02_rank_up_G", 1 },  // QuestIds.RANK_UP_G
                { "04_rank_up_F", 2 },  // QuestIds.RANK_UP_F
                { "06_rank_up_E", 3 },  // QuestIds.RANK_UP_E
                { "10_rank_up_D", 4 },  // QuestIds.RANK_UP_D
                { "09_rank_up_C", 5 },  // QuestIds.RANK_UP_C
                { "11_rank_up_B", 6 },  // QuestIds.RANK_UP_B
                { "12_rank_up_A", 7 },  // QuestIds.RANK_UP_A
            };

            // 次に受けられるランクアップクエストを検索
            var nextRankUp = quests
                .Where(q => q.QuestType == "rank_up" && rankUpMapping.ContainsKey(q.QuestId))
                .OrderBy(q => rankUpMapping[q.QuestId])
                .FirstOrDefault();

            int nextRankUpValue = nextRankUp != null ? rankUpMapping[nextRankUp.QuestId] : 0;
            ctx.Storage.SetInt(SessionFlagKeys.NextRankUp, nextRankUpValue);

            ModLog.Log($"[CheckAvailableQuests] Next rank up: {nextRankUpValue} ({nextRankUp?.QuestId ?? "none"})");

            // ここでは「利用可能クエストの集計」のみを行い、状態同期は行わない
        }
    }
}

