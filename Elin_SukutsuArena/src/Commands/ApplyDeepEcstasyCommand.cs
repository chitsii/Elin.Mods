using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 深い恍惚を付与するコマンド
    /// modInvoke: apply_deep_ecstasy()
    ///
    /// 効果:
    /// - 強い狂気を付与
    /// - 幻覚を付与
    /// </summary>
    public class ApplyDeepEcstasyCommand : IArenaCommand
    {
        public string Name => "apply_deep_ecstasy";

        private const int INSANITY_POWER = 1500;
        private const int HALLUCINATION_POWER = 200;

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            var pc = EClass.pc;
            if (pc == null)
            {
                Debug.LogError("[ApplyDeepEcstasy] PC is null");
                return;
            }

            // 強い狂気を付与（耐性無視）
            pc.AddCondition<ConInsane>(INSANITY_POWER, force: true);
            ModLog.Log($"[ApplyDeepEcstasy] Applied ConInsane power={INSANITY_POWER} (forced) to {pc.Name}");

            // 幻覚を付与（耐性無視）
            pc.AddCondition<ConHallucination>(HALLUCINATION_POWER, force: true);
            ModLog.Log($"[ApplyDeepEcstasy] Applied ConHallucination power={HALLUCINATION_POWER} (forced) to {pc.Name}");
        }
    }
}

