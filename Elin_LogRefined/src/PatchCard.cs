using HarmonyLib;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace Elin_LogRefined
{
    [HarmonyPatch]
    public static class PatchCardDamage
    {
        private static readonly Type[] StrictDamageSignature = new[]
        {
            typeof(long), typeof(int), typeof(int), typeof(AttackSource),
            typeof(Card), typeof(bool), typeof(Thing), typeof(Chara)
        };

        private static int _idxOrigin = -1;
        private static int _idxEle = -1;

        static MethodBase TargetMethod()
        {
            _idxOrigin = -1;
            _idxEle = -1;

            return PatchResolver.FindDeclaredMethod(
                typeof(Card),
                "DamageHP",
                StrictDamagePredicate,
                FallbackDamagePredicate,
                "PatchCard.DamageHP"
            );
        }

        private static bool StrictDamagePredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            if (ps.Length != StrictDamageSignature.Length)
                return false;

            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType != StrictDamageSignature[i])
                    return false;
            }

            _idxOrigin = 4;
            _idxEle = 1;
            return true;
        }

        private static bool FallbackDamagePredicate(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var ps = method.GetParameters();
            if (!ps.Any(p => p.ParameterType == typeof(long)))
                return false;

            _idxOrigin = FindOriginIndex(ps);
            _idxEle = FindElementIndex(ps);
            return true;
        }

        [HarmonyPrefix]
        public static void Prefix(Card __instance, out int __state)
        {
            __state = __instance.hp;
        }

        [HarmonyPostfix]
        public static void Postfix(Card __instance, int __state, object[] __args)
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.ShowDamageLog.Value)
            {
                return;
            }

            int damage = __state - __instance.hp;
            if (damage <= 0)
            {
                return;
            }

            bool isRelatedToPC = __instance.IsPC || __instance.IsPCFaction || EClass.pc.CanSee(__instance);

            if (isRelatedToPC)
            {
                Card origin = TryReadOrigin(__args);
                int ele = TryReadElementId(__args);

                string targetName = __instance.Name;
                string attackerName = origin?.Name ?? "???";

                string elementName = null;
                if (ele != 0 && ele != 926)
                {
                    if (EClass.sources.elements.map.TryGetValue(ele, out var eleRow))
                    {
                        elementName = eleRow.GetName();
                        Color? eleColor = RefinedLogUtil.GetElementColor(eleRow.alias);
                        if (eleColor.HasValue)
                        {
                            elementName = RefinedLogUtil.Colorize(elementName, eleColor.Value);
                        }
                    }
                }

                // Rich Textで統合: ダメージ文 + コメント を1回のSayRawで出力
                string damageText = RefinedLogUtil.FormatDamageLog(damage, targetName, attackerName, elementName);
                string combined = RefinedLogUtil.Colorize(damageText, RefinedLogUtil.GetTextColor(LogType.Damage));

                if (ModConfig.EnableCommentary.Value && CommentaryData.IsInCombat())
                {
                    string comment = CommentaryData.GetRandomDamage();
                    combined += " " + RefinedLogUtil.Colorize(comment, RefinedLogUtil.GetTextColor(LogType.Commentary));
                }

                Msg.SayRaw(combined + " ");
                RefinedLogUtil.TintLastBlock(LogType.Damage);
            }
        }

        private static int FindOriginIndex(ParameterInfo[] ps)
        {
            int named = Array.FindIndex(ps, p => p.ParameterType == typeof(Card) && p.Name != null && p.Name.IndexOf("origin", StringComparison.OrdinalIgnoreCase) >= 0);
            if (named >= 0)
                return named;

            return Array.FindLastIndex(ps, p => p.ParameterType == typeof(Card));
        }

        private static int FindElementIndex(ParameterInfo[] ps)
        {
            int named = Array.FindIndex(ps, p =>
                p.ParameterType == typeof(int) &&
                p.Name != null &&
                (string.Equals(p.Name, "ele", StringComparison.OrdinalIgnoreCase) ||
                 p.Name.IndexOf("element", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 p.Name.IndexOf("ele", StringComparison.OrdinalIgnoreCase) >= 0));
            if (named >= 0)
                return named;

            // Legacy signature compatibility: DamageHP(long, int, int, ...)
            if (ps.Length >= 3 && ps[0].ParameterType == typeof(long) && ps[1].ParameterType == typeof(int) && ps[2].ParameterType == typeof(int))
                return 2;

            return -1;
        }

        private static Card TryReadOrigin(object[] args)
        {
            if (args == null)
                return null;
            if (_idxOrigin >= 0 && _idxOrigin < args.Length)
            {
                var byIndex = args[_idxOrigin] as Card;
                if (byIndex != null)
                    return byIndex;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is Card c)
                    return c;
            }

            return null;
        }

        private static int TryReadElementId(object[] args)
        {
            if (args == null)
                return 0;

            if (_idxEle >= 0 && _idxEle < args.Length && args[_idxEle] is int byIndex)
            {
                if (byIndex == 0 || byIndex == 926 || EClass.sources.elements.map.ContainsKey(byIndex))
                    return byIndex;
            }

            int found = 0;
            int count = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (!(args[i] is int candidate))
                    continue;
                if (candidate == 0)
                    continue;
                if (EClass.sources.elements.map.ContainsKey(candidate))
                {
                    found = candidate;
                    count++;
                    if (count > 1)
                        return 0;
                }
            }

            if (count == 1)
                return found;

            return 0;
        }
    }

    [HarmonyPatch]
    public static class PatchCardHeal
    {
        private static readonly Type[] StrictHealSignature = new[] { typeof(int), typeof(HealSource) };

        static MethodBase TargetMethod()
        {
            return PatchResolver.FindDeclaredMethod(
                typeof(Card),
                "HealHP",
                method => HasExactSignature(method, typeof(void), StrictHealSignature),
                method => method.ReturnType == typeof(void),
                "PatchCard.HealHP"
            );
        }

        private static bool HasExactSignature(MethodInfo method, Type returnType, Type[] paramTypes)
        {
            if (method.ReturnType != returnType)
                return false;

            var ps = method.GetParameters();
            if (ps.Length != paramTypes.Length)
                return false;

            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType != paramTypes[i])
                    return false;
            }

            return true;
        }

        [HarmonyPrefix]
        public static void Prefix(Card __instance, out int __state)
        {
            __state = __instance.hp;
        }

        [HarmonyPostfix]
        public static void Postfix(Card __instance, int __state)
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.ShowHealLog.Value)
            {
                return;
            }

            int healed = __instance.hp - __state;
            if (healed <= 0)
            {
                return;
            }

            bool isRelatedToPC = __instance.IsPC || __instance.IsPCFaction || EClass.pc.CanSee(__instance);

            if (isRelatedToPC)
            {
                string targetName = __instance.Name;
                string healerName = targetName;

                // Rich Textで統合: 回復文 + コメント を1回のSayRawで出力
                string healText = RefinedLogUtil.FormatHealLog(healed, targetName, healerName);
                string combined = RefinedLogUtil.Colorize(healText, RefinedLogUtil.GetTextColor(LogType.Heal));

                if (ModConfig.EnableCommentary.Value && CommentaryData.IsInCombat())
                {
                    string comment = CommentaryData.GetRandomHeal();
                    combined += " " + RefinedLogUtil.Colorize(comment, RefinedLogUtil.GetTextColor(LogType.Commentary));
                }

                Msg.SayRaw(combined + " ");
                RefinedLogUtil.TintLastBlock(LogType.Heal);
            }
        }
    }
}
