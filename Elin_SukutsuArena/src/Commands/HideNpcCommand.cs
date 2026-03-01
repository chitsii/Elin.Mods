using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// NPCを非表示にする（somewhereに移動）
    /// 使用: modInvoke hide_npc(npc_id)
    /// </summary>
    public class HideNpcCommand : IArenaCommand
    {
        public string Name => "hide_npc";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
            {
                Debug.LogError("[HideNpcCommand] Missing npc_id argument");
                return;
            }

            DebugHelper.HideNpc(args[0]);
        }
    }
}
