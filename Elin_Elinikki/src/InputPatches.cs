using HarmonyLib;
using UnityEngine;

namespace Elin_Elinikki
{
    [HarmonyPatch]
    internal static class InputPatches
    {
        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EInput), "UpdateAxis");
        }

        [HarmonyPostfix]
        private static void RotateAxisForFpsView()
        {
            if (!FpsViewManager.IsFpsViewActive)
            {
                return;
            }

            if (FpsViewManager.IsDungeonCrawlerModeActive)
            {
                EInput.axis = Vector2.zero;
                return;
            }

            Vector2 axis = EInput.axis;
            if (axis == Vector2.zero)
            {
                return;
            }

            EInput.axis = RotateAxis(axis, FpsViewManager.CurrentYawRadians);
        }

        private static Vector2 RotateAxis(Vector2 axis, float yawRadians)
        {
            Vector2 forward = new Vector2(Mathf.Cos(yawRadians), Mathf.Sin(yawRadians));
            Vector2 right = new Vector2(-forward.y, forward.x);
            Vector2 world = forward * axis.y + right * axis.x;
            return QuantizeToEightDirections(world);
        }

        private static Vector2 QuantizeToEightDirections(Vector2 vector)
        {
            if (vector == Vector2.zero)
            {
                return Vector2.zero;
            }

            float angle = Mathf.Atan2(vector.y, vector.x);
            float step = Mathf.PI / 4f;
            int octant = Mathf.RoundToInt(angle / step);
            float snapped = octant * step;
            int x = Mathf.RoundToInt(Mathf.Cos(snapped));
            int y = Mathf.RoundToInt(Mathf.Sin(snapped));
            return new Vector2(x, y);
        }
    }

    [HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv.AxisMove))]
    internal static class DungeonCrawlerAxisPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(AM_Adv __instance)
        {
            return true;
        }
    }
}
