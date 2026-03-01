using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.MapWeapon;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 大部屋の巻物効果を発動する（ドラマ演出用）
    /// 使用: modInvoke invoke_oheya_scroll(radius)
    /// - radius=0: 全範囲
    /// - radius=5: 半径5マス（小規模）
    /// </summary>
    public class InvokeOheyaScrollCommand : IArenaCommand
    {
        public string Name => "invoke_oheya_scroll";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            float radius = 0f; // デフォルト: 全範囲

            if (args.Length > 0 && float.TryParse(args[0], out float parsedRadius))
            {
                radius = parsedRadius;
            }

            var param = MapWeaponParams.HarvestPreset();
            param.Radius = radius;
            param.ApplyKarma = false; // ドラマ演出なのでカルマ変動なし
            param.ProtectPCZone = false; // 闘技場内での演出なので保護なし

            var result = MapWeaponEngine.Execute(EClass.pc, param);

            if (result.Success)
            {
                ModLog.Log($"[InvokeOheyaScroll] Executed with radius={radius}, harvested={result.HarvestCount}");
            }
            else
            {
                ModLog.Log($"[InvokeOheyaScroll] No effect (radius={radius})");
            }
        }
    }
}

