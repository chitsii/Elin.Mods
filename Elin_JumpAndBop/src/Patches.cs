using HarmonyLib;
using UnityEngine;

namespace Elin_JumpAndBop
{
    /// <summary>
    /// Trigger bounce on ActWait.Perform (Space key wait/stomp).
    /// </summary>
    [HarmonyPatch(typeof(ActWait), nameof(ActWait.Perform))]
    public static class ActWaitPerformPatch
    {
        static void Postfix(bool __result)
        {
            try
            {
                if (!ModConfig.EnableMod.Value) return;
                if (!ModConfig.EnableOnWait.Value) return;
                if (!__result) return;
                if (Act.CC == null || !Act.CC.IsPC) return;

                BounceController.TriggerBounce();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[JumpAndBop] ActWait patch error: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Apply Y offset to PC sprite and manage AI_Fuck sustained bounce state.
    /// </summary>
    [HarmonyPatch(typeof(CharaRenderer), nameof(CharaRenderer.UpdatePosition))]
    public static class CharaRendererUpdatePositionPatch
    {
        static void Postfix(CharaRenderer __instance, RenderParam p)
        {
            try
            {
                if (!ModConfig.EnableMod.Value) return;
                if (__instance.owner == null || !__instance.owner.IsPC) return;

                // AI_Fuck state detection
                bool isSex = EClass.pc?.ai is AI_Fuck;
                if (isSex && !BounceController.sustainedMode && ModConfig.EnableOnSex.Value)
                {
                    BounceController.StartSustained();
                }
                else if (!isSex && BounceController.sustainedMode)
                {
                    BounceController.StopSustained();
                }

                if (!BounceController.IsActive) return;

                float offset = BounceController.GetCurrentOffset();
                if (offset > 0f)
                {
                    p.y += offset;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[JumpAndBop] UpdatePosition patch error: {e.Message}");
            }
        }
    }
}
