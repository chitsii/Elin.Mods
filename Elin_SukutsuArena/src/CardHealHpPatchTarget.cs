using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Elin_SukutsuArena
{
    public static class CardHealHpPatchTarget
    {
        private static readonly Type[] StableSignature =
        {
            typeof(long), typeof(HealSource)
        };

        private static MethodInfo _healHpMethod;

        public static MethodBase Resolve(string patchName)
        {
            var method = ResolveMethod();

            if (method == null)
            {
                Debug.LogError($"[SukutsuArena] {patchName}: Card.HealHP(long, HealSource) target not found");
                return null;
            }

            Debug.Log($"[SukutsuArena] {patchName}: Card.HealHP stable signature resolved");
            return method;
        }

        private static MethodInfo ResolveMethod()
        {
            if (_healHpMethod != null)
                return _healHpMethod;

            _healHpMethod = AccessTools.GetDeclaredMethods(typeof(Card))
                .OfType<MethodInfo>()
                .Where(m => m.Name == nameof(Card.HealHP))
                .Where(IsStableHealHp)
                .FirstOrDefault();

            return _healHpMethod;
        }

        private static bool IsStableHealHp(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var parameters = method.GetParameters();
            if (parameters.Length != StableSignature.Length)
                return false;

            for (int i = 0; i < StableSignature.Length; i++)
            {
                if (parameters[i].ParameterType != StableSignature[i])
                    return false;
            }

            return true;
        }
    }
}
