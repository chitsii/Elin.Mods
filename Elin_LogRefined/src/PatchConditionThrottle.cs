using HarmonyLib;
using System;
using System.Reflection;

namespace Elin_LogRefined
{
    /// <summary>
    /// Patch A: Suppress repeated BaseCondition.PhaseMsg calls.
    /// When throttled, skips both PopText() and owner.Say() entirely.
    /// Strict: void PhaseMsg(bool)
    /// Fallback: any PhaseMsg with at least one parameter
    /// </summary>
    [HarmonyPatch]
    public static class PatchPhaseMsgThrottle
    {
        private static readonly Type[] StrictPhaseMsgSignature = new[] { typeof(bool) };

        static MethodBase TargetMethod()
        {
            return PatchResolver.FindDeclaredMethod(
                typeof(BaseCondition),
                "PhaseMsg",
                StrictPhaseMsgPredicate,
                FallbackPhaseMsgPredicate,
                "PatchPhaseMsgThrottle.PhaseMsg"
            );
        }

        private static bool StrictPhaseMsgPredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            if (ps.Length != StrictPhaseMsgSignature.Length)
                return false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType != StrictPhaseMsgSignature[i])
                    return false;
            }
            return true;
        }

        private static bool FallbackPhaseMsgPredicate(MethodInfo method)
        {
            return method.ReturnType == typeof(void) && method.GetParameters().Length >= 1;
        }

        [HarmonyPrefix]
        public static bool Prefix(BaseCondition __instance)
        {
            if (!ModConfig.EnableMod.Value || !RuntimeGuard.IsGameplayReady())
                return true;

            if (__instance.owner == null)
                return true;

            return !ConditionThrottle.IsThrottled(__instance.id, __instance.owner.uid);
        }
    }

    /// <summary>
    /// Patch B: Suppress the "textEnd" message in Condition.Kill
    /// by forcing silent=true when throttled.
    /// Kill logic (removal, effects, refresh) still executes normally.
    /// Strict: void Kill(bool)
    /// Fallback: any Kill with a bool parameter
    /// </summary>
    [HarmonyPatch]
    public static class PatchConditionKillThrottle
    {
        private static readonly Type[] StrictKillSignature = new[] { typeof(bool) };

        private static int _idxSilentArg = -1;

        static MethodBase TargetMethod()
        {
            _idxSilentArg = -1;

            return PatchResolver.FindDeclaredMethod(
                typeof(Condition),
                "Kill",
                StrictKillPredicate,
                FallbackKillPredicate,
                "PatchConditionKillThrottle.Kill"
            );
        }

        private static bool StrictKillPredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            if (ps.Length != StrictKillSignature.Length)
                return false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType != StrictKillSignature[i])
                    return false;
            }

            _idxSilentArg = 0;
            return true;
        }

        private static bool FallbackKillPredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            _idxSilentArg = Array.FindIndex(ps, p => p.ParameterType == typeof(bool));
            return _idxSilentArg >= 0;
        }

        [HarmonyPrefix]
        public static void Prefix(Condition __instance, ref bool silent)
        {
            if (!ModConfig.EnableMod.Value || !RuntimeGuard.IsGameplayReady() || silent)
                return;

            if (__instance.owner == null)
                return;

            if (ConditionThrottle.IsThrottled(__instance.id, __instance.owner.uid))
                silent = true;
        }
    }
}
