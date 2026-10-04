using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Elin_LogRefined
{
    [HarmonyPatch]
    public static class PatchChara
    {
        private static readonly Type[] StrictAddConditionSignature = new[] { typeof(Condition), typeof(bool) };

        private static int _idxConditionArg = -1;

        [ThreadStatic]
        private static Stack<ConditionApplicationScope> _conditionScopes;

        private static readonly StringBuilder _sb = new StringBuilder(128);

        static MethodBase TargetMethod()
        {
            _idxConditionArg = -1;

            return PatchResolver.FindDeclaredMethod(
                typeof(Chara),
                "AddCondition",
                StrictAddConditionPredicate,
                FallbackAddConditionPredicate,
                "PatchChara.AddCondition"
            );
        }

        private static bool StrictAddConditionPredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(Condition))
                return false;

            var ps = method.GetParameters();
            if (ps.Length != StrictAddConditionSignature.Length)
                return false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType != StrictAddConditionSignature[i])
                    return false;
            }

            _idxConditionArg = 0;
            return true;
        }

        private static bool FallbackAddConditionPredicate(MethodInfo method)
        {
            var ps = method.GetParameters();
            _idxConditionArg = Array.FindIndex(ps, p => p.ParameterType == typeof(Condition));

            bool hasConditionArg = _idxConditionArg >= 0;
            bool hasConditionReturn = method.ReturnType == typeof(Condition) || method.ReturnType == typeof(bool);
            return hasConditionArg || hasConditionReturn;
        }

        [HarmonyPrefix]
        static void Prefix(Chara __instance, object[] __args, out ConditionApplicationScope __state)
        {
            __state = CreateConditionApplicationScope(__instance, __args);
            PushConditionApplicationScope(__state);
        }

        [HarmonyPostfix]
        static void Postfix(Chara __instance, object __result, object[] __args, ConditionApplicationScope __state)
        {
            try
            {
                if (!ModConfig.EnableMod.Value || !ModConfig.ShowConditionLog.Value || !RuntimeGuard.IsGameplayReady())
                {
                    return;
                }

                Condition condition = ResolveCondition(__result, __args, __state);
                if (condition == null)
                {
                    // Condition was not added (e.g., resisted, nullified)
                    return;
                }

                bool isRelatedToPC = RuntimeGuard.CanInspectCard(__instance);
                if (!isRelatedToPC)
                {
                    return;
                }

                if (ModConfig.ThrottleConditionLog.Value &&
                    ConditionThrottle.ShouldThrottle(condition.id, __instance.uid))
                {
                    return;
                }

                string detail = "";

                // Checks for conditions with elements (ConBuffStats, ConHero, etc.)
                if (condition.elements != null && condition.elements.dict != null && condition.elements.dict.Count > 0)
                {
                    _sb.Clear();
                    bool first = true;
                    foreach (var kvp in condition.elements.dict)
                    {
                        int eleId = kvp.Key;
                        int val = kvp.Value.Value;
                        if (val == 0) continue;

                        if (!first) _sb.Append(", ");

                        // Get element name
                        if (!EClass.sources.elements.map.TryGetValue(eleId, out var eleRow)) continue;
                        string statName = eleRow.GetName();
                        string sign = val >= 0 ? "+" : "";

                        _sb.Append(statName).Append(' ').Append(sign).Append(RefinedLogUtil.FormatNumber(val));
                        first = false;
                    }
                    detail = _sb.ToString();
                }

                // Fallback for ConBuffStats if dict was empty but CalcValue works
                if (string.IsNullOrEmpty(detail) && condition is ConBuffStats buffStats)
                {
                    int val = buffStats.CalcValue();
                    if (condition.Type == ConditionType.Debuff) val = -val;

                    if (val != 0 && EClass.sources.elements.map.TryGetValue(buffStats.refVal, out var refRow))
                    {
                        string sign = val >= 0 ? "+" : "";
                        string statName = refRow.GetName();
                        detail = $"{statName} {sign}{RefinedLogUtil.FormatNumber(val)}";
                    }
                }

                // Ultimate fallback to condition name
                if (string.IsNullOrEmpty(detail))
                {
                    detail = condition.Name;
                }
                else
                {
                    // Prepend name if we have details
                    detail = $"{condition.Name} : {detail}";
                }

                string targetName = __instance.Name;
                // 付与者は取得困難なので、self として扱う
                string inflicterName = targetName;

                // Rich Textで統合して出力 + 背景ティント
                if (condition.Type == ConditionType.Debuff)
                {
                    string text = RefinedLogUtil.FormatDebuffLog(detail, targetName, inflicterName);
                    string combined = RefinedLogUtil.Colorize(text, RefinedLogUtil.GetTextColor(LogType.Debuff));

                    if (ModConfig.EnableCommentary.Value && CommentaryData.IsInCombat())
                    {
                        string comment = CommentaryData.GetRandomDebuff();
                        combined += " " + RefinedLogUtil.Colorize("「" + comment + "」", RefinedLogUtil.GetTextColor(LogType.Commentary));
                    }

                    Msg.SayRaw(combined + " ");
                    RefinedLogUtil.TintLastBlock(LogType.Debuff);
                }
                else
                {
                    string text = RefinedLogUtil.FormatBuffLog(detail, targetName, inflicterName);
                    string combined = RefinedLogUtil.Colorize(text, RefinedLogUtil.GetTextColor(LogType.Buff));

                    Msg.SayRaw(combined + " ");
                    RefinedLogUtil.TintLastBlock(LogType.Buff);
                }
            }
            finally
            {
                PopConditionApplicationScope(__state);
            }
        }

        [HarmonyFinalizer]
        static void Finalizer(ConditionApplicationScope __state)
        {
            PopConditionApplicationScope(__state);
        }

        private static Condition ResolveCondition(object result, object[] args)
        {
            return ResolveCondition(result, args, null);
        }

        private static Condition ResolveCondition(object result, object[] args, ConditionApplicationScope scope)
        {
            if (result is bool ok && !ok)
                return null;
            if (result is Condition c)
                return c;
            if (scope != null && scope.StackedCondition != null)
                return scope.StackedCondition;

            return null;
        }

        private static ConditionApplicationScope CreateConditionApplicationScope(Chara owner, object[] args)
        {
            Condition attempted = GetConditionArgument(args);
            if (owner == null || attempted == null)
                return null;

            return new ConditionApplicationScope(owner, attempted);
        }

        internal static void RecordStackedCondition(Condition condition)
        {
            if (condition == null || _conditionScopes == null || _conditionScopes.Count == 0)
                return;

            foreach (var scope in _conditionScopes)
            {
                if (!scope.Matches(condition))
                    continue;

                scope.StackedCondition = condition;
                return;
            }
        }

        private static void PushConditionApplicationScope(ConditionApplicationScope state)
        {
            if (state == null)
                return;

            if (_conditionScopes == null)
                _conditionScopes = new Stack<ConditionApplicationScope>();

            _conditionScopes.Push(state);
        }

        private static void PopConditionApplicationScope(ConditionApplicationScope state)
        {
            if (state == null || _conditionScopes == null || _conditionScopes.Count == 0)
                return;

            if (ReferenceEquals(_conditionScopes.Peek(), state))
            {
                _conditionScopes.Pop();
            }
        }

        private static Condition GetConditionArgument(object[] args)
        {
            if (args == null)
                return null;
            if (_idxConditionArg < 0 || _idxConditionArg >= args.Length)
                return null;

            return args[_idxConditionArg] as Condition;
        }

        private sealed class ConditionApplicationScope
        {
            private readonly Chara _owner;
            private readonly int _ownerUid;
            private readonly int _conditionId;

            public ConditionApplicationScope(Chara owner, Condition attempted)
            {
                _owner = owner;
                _ownerUid = owner.uid;
                _conditionId = attempted.id;
            }

            public Condition StackedCondition { get; set; }

            public bool Matches(Condition condition)
            {
                if (condition.id != _conditionId || condition.owner == null)
                    return false;

                return ReferenceEquals(condition.owner, _owner) || condition.owner.uid == _ownerUid;
            }
        }
    }

    [HarmonyPatch]
    public static class PatchConditionOnStackedEvidence
    {
        private static readonly Type[] StrictOnStackedSignature = new[] { typeof(int) };

        static MethodBase TargetMethod()
        {
            return PatchResolver.FindDeclaredMethod(
                typeof(Condition),
                "OnStacked",
                StrictOnStackedPredicate,
                FallbackOnStackedPredicate,
                "PatchConditionOnStackedEvidence.OnStacked"
            );
        }

        private static bool StrictOnStackedPredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            if (ps.Length != StrictOnStackedSignature.Length)
                return false;

            return ps[0].ParameterType == StrictOnStackedSignature[0];
        }

        private static bool FallbackOnStackedPredicate(MethodInfo method)
        {
            return method.ReturnType == typeof(void) && method.GetParameters().Length == 1;
        }

        [HarmonyPrefix]
        public static void Prefix(Condition __instance)
        {
            PatchChara.RecordStackedCondition(__instance);
        }
    }
}
