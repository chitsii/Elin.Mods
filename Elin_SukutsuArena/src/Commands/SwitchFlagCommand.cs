using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// switch_flag(flagKey, jump0, jump1, jump2, ..., fallback)
    /// フラグ値に対応するジャンプ先を決定
    /// flag=0ならjump0へ、flag=1ならjump1へ...
    ///
    /// 注意: このコマンドは直接ジャンプせず、PendingJumpTargetに結果を保存。
    /// DramaManager_PatchのjumpFuncがこの値を使用してジャンプを実行する。
    /// </summary>
    public class SwitchFlagCommand : IArenaCommand
    {
        public string Name => "switch_flag";

        /// <summary>
        /// 次のジャンプ先（jumpFuncで使用）
        /// </summary>
        public static string PendingJumpTarget { get; set; } = null;

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            PendingJumpTarget = null;

            if (args.Length < 2)
            {
                Debug.LogError("[SwitchFlag] Requires at least 2 args: flagKey, jump0[, jump1, ..., fallback]");
                return;
            }

            string flagKey = args[0];
            int flagValue = ctx.Storage.GetInt(flagKey, 0);

            ModLog.Log($"[SwitchFlag] flagKey={flagKey}, value={flagValue}, args count={args.Length}");

            // フラグ値に対応するジャンプ先（args[1]からargs[n-1]まで）
            int jumpIndex = flagValue + 1;  // args[0]はflagKeyなので+1

            if (jumpIndex > 0 && jumpIndex < args.Length)
            {
                string jumpTarget = args[jumpIndex];
                if (!string.IsNullOrEmpty(jumpTarget))
                {
                    ModLog.Log($"[SwitchFlag] Setting jump target: {jumpTarget}");
                    PendingJumpTarget = jumpTarget;
                    return;
                }
            }

            // フォールバック: 最後の引数
            string fallback = args[args.Length - 1];
            if (!string.IsNullOrEmpty(fallback))
            {
                ModLog.Log($"[SwitchFlag] Setting fallback target: {fallback}");
                PendingJumpTarget = fallback;
            }
        }
    }
}

