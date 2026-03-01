using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 神罰（ConWrath）があるかチェックし、フラグにセットするコマンド
    /// modInvoke: check_has_wrath()
    /// 結果は temp_has_wrath フラグにセット（branch_ifで使用）
    /// </summary>
    public class CheckHasWrathCommand : IArenaCommand
    {
        public string Name => "check_has_wrath";

        private const string FLAG_HAS_WRATH = "temp_has_wrath";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            var pc = EClass.pc;
            bool hasWrath = false;

            if (pc != null && pc.conditions != null)
            {
                foreach (var condition in pc.conditions)
                {
                    if (condition is ConWrath)
                    {
                        hasWrath = true;
                        ModLog.Log($"[CheckHasWrath] Found ConWrath: {condition.Name}");
                        break;
                    }
                }
            }

            // フラグにセット（branch_ifで参照可能）
            ctx.Storage.SetInt(FLAG_HAS_WRATH, hasWrath ? 1 : 0);
            ModLog.Log($"[CheckHasWrath] Has wrath: {hasWrath}, flag set to: {(hasWrath ? 1 : 0)}");
        }
    }
}

