using HarmonyLib;
using System;
using System.Reflection;
using System.Text;

namespace Elin_LogRefined
{
    [HarmonyPatch]
    public static class PatchChara
    {
        private static readonly Type[] StrictAddConditionSignature = new[] { typeof(Condition), typeof(bool) };

        private static int _idxConditionArg = -1;

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

        [HarmonyPostfix]
        public static void Postfix(Chara __instance, object __result, object[] __args)
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.ShowConditionLog.Value)
            {
                return;
            }

            Condition condition = ResolveCondition(__result, __args);
            if (condition == null)
            {
                // Condition was not added (e.g., resisted, nullified)
                return;
            }

            bool isRelatedToPC = __instance.IsPC || __instance.IsPCFaction || EClass.pc.CanSee(__instance);
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
                if (buffStats.isDebuff) val = -val;

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

        private static Condition ResolveCondition(object result, object[] args)
        {
            if (result is bool ok && !ok)
                return null;
            if (result is Condition c)
                return c;

            if (args == null)
                return null;
            if (_idxConditionArg < 0 || _idxConditionArg >= args.Length)
                return null;

            return args[_idxConditionArg] as Condition;
        }
    }
}
