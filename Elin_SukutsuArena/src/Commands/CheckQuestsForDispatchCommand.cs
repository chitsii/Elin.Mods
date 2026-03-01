using System.Collections.Generic;
using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// check_quests_for_dispatch(flagName, questId1, questId2, ...)
    /// 指定されたクエストIDリストの中で、利用可能な最初のクエストのインデックスをフラグに設定
    ///
    /// 設定されるフラグ値:
    /// - 0: 利用可能なクエストなし（fallback）
    /// - 1: リストの1番目のクエストが利用可能
    /// - 2: リストの2番目のクエストが利用可能
    /// - ...
    /// </summary>
    public class CheckQuestsForDispatchCommand : IArenaCommand
    {
        public string Name => "check_quests_for_dispatch";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            if (args.Length < 2)
            {
                Debug.LogError("[CheckQuestsForDispatch] Requires at least 2 args: flagName, questId1[, questId2, ...]");
                return;
            }

            string flagName = args[0];

            // クエストIDリストを取得（args[1]以降）
            var questIdList = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                questIdList.Add(args[i].Trim());
            }

            // 利用可能なクエストを取得
            var availableQuests = ArenaQuestManager.Instance.GetAvailableQuests();
            var availableIds = new HashSet<string>();
            foreach (var q in availableQuests)
            {
                availableIds.Add(q.QuestId);
            }

            ModLog.Log($"[CheckQuestsForDispatch] Available quests: {string.Join(", ", availableIds)}");
            ModLog.Log($"[CheckQuestsForDispatch] Checking: {string.Join(", ", questIdList)}");

            // 最初に見つかった利用可能なクエストのインデックスを設定
            int selectedIndex = 0;  // 0はfallback
            for (int i = 0; i < questIdList.Count; i++)
            {
                if (availableIds.Contains(questIdList[i]))
                {
                    selectedIndex = i + 1;  // 1から開始（0はfallback）
                    ModLog.Log($"[CheckQuestsForDispatch] Found available quest: {questIdList[i]} at index {selectedIndex}");
                    break;
                }
            }

            ctx.Storage.SetInt(flagName, selectedIndex);
            ModLog.Log($"[CheckQuestsForDispatch] {flagName} = {selectedIndex} (checked {questIdList.Count} quests)");
        }
    }
}

