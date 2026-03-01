using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// start_quest(questId)
    /// クエストを開始
    /// </summary>
    public class StartQuestCommand : IArenaCommand
    {
        public string Name => "start_quest";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            if (args.Length < 1)
            {
                Debug.LogError("[StartQuest] Requires 1 arg: questId");
                return;
            }

            var questId = args[0];
            ModLog.Log($"[StartQuest] Starting quest: {questId}");

            // ArenaQuestManagerを使用してクエストを開始
            ArenaQuestManager.Instance.StartQuest(questId);

            // Contextにもマーク
            ctx.Quest.MarkActive(questId);
        }
    }
}

