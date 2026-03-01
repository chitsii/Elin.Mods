using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// complete_quest(questId)
    /// クエストを完了
    /// </summary>
    public class CompleteQuestCommand : IArenaCommand
    {
        public string Name => "complete_quest";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            if (args.Length < 1)
            {
                Debug.LogError("[CompleteQuest] Requires 1 arg: questId");
                return;
            }

            var questId = args[0];
            ModLog.Log($"[CompleteQuest] Completing quest: {questId}");

            // ArenaQuestManagerを使用してクエストを完了
            ArenaQuestManager.Instance.CompleteQuest(questId);

            // Contextにもマーク
            ctx.Quest.MarkCompleted(questId);
        }
    }
}

