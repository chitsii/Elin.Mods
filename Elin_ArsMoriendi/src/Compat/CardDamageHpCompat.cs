using System;
using System.Reflection;

namespace Elin_ArsMoriendi
{
    internal static class CardDamageHpCompat
    {
        private static MethodInfo? _method;
        private static int _parameterCount;
        private static bool _warnedUnresolved;

        public static bool Apply(
            Card target,
            long damage,
            int element,
            int elementPower = 100,
            AttackSource attackSource = AttackSource.None,
            Card? origin = null,
            bool showEffect = true,
            Thing? weapon = null,
            Chara? originalTarget = null,
            int resistPenetrationLevel = 0)
        {
            if (target == null) return false;

            var method = Resolve();
            if (method == null)
            {
                if (!_warnedUnresolved)
                {
                    _warnedUnresolved = true;
                    ModLog.Error("CardDamageHpCompat: Card.DamageHP was not resolved.");
                }
                return false;
            }

            try
            {
                object?[] args = _parameterCount >= 9
                    ? new object?[]
                    {
                        damage,
                        element,
                        elementPower,
                        attackSource,
                        origin,
                        showEffect,
                        weapon,
                        originalTarget,
                        resistPenetrationLevel,
                    }
                    : new object?[]
                    {
                        damage,
                        element,
                        elementPower,
                        attackSource,
                        origin,
                        showEffect,
                        weapon,
                        originalTarget,
                    };

                method.Invoke(target, args);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                ModLog.Warn($"CardDamageHpCompat.Apply failed: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                ModLog.Warn($"CardDamageHpCompat.Apply failed: {ex.Message}");
            }

            return false;
        }

        private static MethodInfo? Resolve()
        {
            if (_method != null) return _method;

            var resolved = MethodResolver.Resolve(CompatSymbol.CardDamageHp);
            if (!resolved.IsResolved || resolved.Method == null)
                return null;

            _method = resolved.Method;
            _parameterCount = _method.GetParameters().Length;
            return _method;
        }
    }
}
