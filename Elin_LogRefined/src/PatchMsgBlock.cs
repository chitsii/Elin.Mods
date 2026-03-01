using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

namespace Elin_LogRefined
{
    [HarmonyPatch]
    public static class PatchMsgBlock
    {
        private static readonly Type[] StrictAppendSignature = new[] { typeof(string), typeof(Color) };

        private static int _idxColorArg = -1;

        static MethodBase TargetMethod()
        {
            _idxColorArg = -1;

            return PatchResolver.FindDeclaredMethod(
                typeof(MsgBlock),
                "Append",
                StrictAppendPredicate,
                FallbackAppendPredicate,
                "PatchMsgBlock.Append"
            );
        }

        private static bool StrictAppendPredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            if (ps.Length != StrictAppendSignature.Length)
                return false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType != StrictAppendSignature[i])
                    return false;
            }

            _idxColorArg = 1;
            return true;
        }

        private static bool FallbackAppendPredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            if (ps.Length < 2)
                return false;
            if (ps[0].ParameterType != typeof(string))
                return false;

            for (int i = 1; i < ps.Length; i++)
            {
                var t = ps[i].ParameterType;
                if (t == typeof(Color) || t == typeof(Color32))
                {
                    _idxColorArg = i;
                    return true;
                }
            }

            return false;
        }

        [HarmonyPrefix]
        public static void Prefix(object[] __args)
        {
            if (!ModConfig.EnableMod.Value) return;
            if (__args == null || _idxColorArg < 0 || _idxColorArg >= __args.Length) return;

            object colorArg = __args[_idxColorArg];
            Color sourceColor;

            if (colorArg is Color c)
            {
                sourceColor = c;
            }
            else if (colorArg is Color32 c32)
            {
                sourceColor = c32;
            }
            else
            {
                return;
            }

            var remapped = RefinedLogUtil.RemapColor(sourceColor);
            if (!remapped.HasValue)
                return;

            if (colorArg is Color)
            {
                __args[_idxColorArg] = remapped.Value;
            }
            else
            {
                __args[_idxColorArg] = (Color32)remapped.Value;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(MsgBlock __instance)
        {
            if (!ModConfig.EnableMod.Value) return;
            var surfaceColor = RefinedLogUtil.GetSurfaceColor();
            if (surfaceColor.HasValue)
            {
                __instance.bg.color = surfaceColor.Value;
            }
            if (__instance.txt != null)
            {
                var shadow = __instance.txt.GetComponent<Shadow>();
                if (shadow != null)
                    shadow.enabled = !RefinedLogUtil.IsLightTheme();
            }
        }
    }
}
