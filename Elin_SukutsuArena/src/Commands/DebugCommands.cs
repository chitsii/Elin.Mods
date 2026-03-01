using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// debug_log_flags()
    /// 全フラグをログ出力
    /// </summary>
    public class DebugLogFlagsCommand : IArenaCommand
    {
        public string Name => "debug_log_flags";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            ModLog.Log("[ArenaFlags] === Current Flag State ===");
            ModLog.Log($"  Rank: {ctx.Player.Rank}");
            ModLog.Log($"  Phase: {ctx.Player.CurrentPhase}");
            ModLog.Log($"  Fugitive: {ctx.Player.IsFugitive}");
            ModLog.Log($"  BottleChoice: {ctx.Player.GetBottleChoice()?.ToString() ?? "null"}");
            ModLog.Log($"  KainSoulChoice: {ctx.Player.GetKainSoulChoice()?.ToString() ?? "null"}");
            ModLog.Log($"  BalgasChoice: {ctx.Player.GetBalgasChoice()?.ToString() ?? "null"}");
            ModLog.Log("[ArenaFlags] === End ===");
        }
    }

    /// <summary>
    /// debug_log_quests()
    /// クエスト状態をログ出力
    /// </summary>
    public class DebugLogQuestsCommand : IArenaCommand
    {
        public string Name => "debug_log_quests";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            ArenaQuestManager.Instance.DebugLogQuestState();
        }
    }
}

