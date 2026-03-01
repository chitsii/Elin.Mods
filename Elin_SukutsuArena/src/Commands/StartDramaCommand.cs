using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// start_drama(dramaName)
    /// 別のドラマを開始する
    /// </summary>
    public class StartDramaCommand : IArenaCommand
    {
        public string Name => "start_drama";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            if (args.Length < 1)
            {
                Debug.LogError("[StartDrama] Requires 1 arg: dramaName");
                return;
            }

            var dramaName = args[0];
            ModLog.Log($"[StartDrama] Starting drama: {dramaName}");

            // ArenaManager経由でドラマを開始
            ArenaManager.StartDrama(dramaName);
        }
    }
}

