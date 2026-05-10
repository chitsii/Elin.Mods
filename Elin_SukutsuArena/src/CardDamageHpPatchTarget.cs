using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Elin_SukutsuArena
{
    public static class CardDamageHpPatchTarget
    {
        private static readonly Type[] StablePrefixSignature =
        {
            typeof(long), typeof(int), typeof(int), typeof(AttackSource),
            typeof(Card), typeof(bool), typeof(Thing), typeof(Chara)
        };

        private static MethodInfo _damageHpMethod;
        private static int _damageHpParameterCount;
        private static bool _warnedApplyFailure;

        public static MethodBase Resolve(string patchName)
        {
            var method = ResolveMethod();

            if (method == null)
            {
                Debug.LogError($"[SukutsuArena] {patchName}: Card.DamageHP target not found");
                return null;
            }

            var parameterCount = method.GetParameters().Length;
            if (parameterCount == StablePrefixSignature.Length)
            {
                Debug.Log($"[SukutsuArena] {patchName}: Card.DamageHP stable signature resolved");
            }
            else
            {
                Debug.LogWarning($"[SukutsuArena] {patchName}: Card.DamageHP compatible signature resolved ({parameterCount} parameters)");
            }

            return method;
        }

        public static bool Apply(
            Card target,
            long damage,
            int element,
            int elementPower = 100,
            AttackSource attackSource = AttackSource.None,
            Card origin = null,
            bool showEffect = true,
            Thing weapon = null,
            Chara originalTarget = null,
            int resistPenetrationLevel = 0)
        {
            if (target == null)
                return false;

            var method = ResolveMethod();
            if (method == null)
            {
                WarnApplyFailure("Card.DamageHP target not found");
                return false;
            }

            try
            {
                object[] args = _damageHpParameterCount >= 9
                    ? new object[]
                    {
                        damage,
                        element,
                        elementPower,
                        attackSource,
                        origin,
                        showEffect,
                        weapon,
                        originalTarget,
                        resistPenetrationLevel
                    }
                    : new object[]
                    {
                        damage,
                        element,
                        elementPower,
                        attackSource,
                        origin,
                        showEffect,
                        weapon,
                        originalTarget
                    };

                method.Invoke(target, args);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                WarnApplyFailure(ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                WarnApplyFailure(ex.Message);
            }

            return false;
        }

        private static MethodInfo ResolveMethod()
        {
            if (_damageHpMethod != null)
                return _damageHpMethod;

            _damageHpMethod = AccessTools.GetDeclaredMethods(typeof(Card))
                .OfType<MethodInfo>()
                .Where(m => m.Name == nameof(Card.DamageHP))
                .Where(IsCompatibleDamageHp)
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (_damageHpMethod != null)
                _damageHpParameterCount = _damageHpMethod.GetParameters().Length;

            return _damageHpMethod;
        }

        private static void WarnApplyFailure(string message)
        {
            if (_warnedApplyFailure)
                return;

            _warnedApplyFailure = true;
            Debug.LogWarning($"[SukutsuArena] Card.DamageHP apply failed: {message}");
        }

        private static bool IsCompatibleDamageHp(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var parameters = method.GetParameters();
            if (parameters.Length < StablePrefixSignature.Length)
                return false;

            for (int i = 0; i < StablePrefixSignature.Length; i++)
            {
                if (parameters[i].ParameterType != StablePrefixSignature[i])
                    return false;
            }

            for (int i = StablePrefixSignature.Length; i < parameters.Length; i++)
            {
                if (!parameters[i].HasDefaultValue)
                    return false;
            }

            return true;
        }
    }
}
