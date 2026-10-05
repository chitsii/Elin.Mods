using System;

namespace Elin_ArsMoriendi
{
    internal static class TeleportPosCompat
    {
        // Cache both successful binding and binding failures. Game-call exceptions are
        // outside the Lazy factory and still reach the spells' existing fail-soft catch.
        private static readonly Lazy<Func<Point, int, Point>> Invoke = new(CreateInvoker);

        [CompatibilityPatch("EA23.351.2Nightly", "ActEffect.GetTeleportPos gained an optional Chara target (2/3 args).")]
        public static Point GetTeleportPos(Point origin, int radius = 6)
            => Invoke.Value(origin, radius);

        private static Func<Point, int, Point> CreateInvoker()
        {
            var method = MethodResolver.Resolve(CompatSymbol.ActEffectGetTeleportPos).Method;
            if (method == null)
                throw new MissingMethodException("ActEffect.GetTeleportPos: no supported 2/3 argument signature.");

            if (method.GetParameters().Length == 3)
            {
                var nightly = (Func<Point, int, Chara?, Point>)Delegate.CreateDelegate(
                    typeof(Func<Point, int, Chara?, Point>), method);
                // null bypasses Nightly's new LOS restriction, preserving stable behavior.
                return (origin, radius) => nightly(origin, radius, null);
            }

            return (Func<Point, int, Point>)Delegate.CreateDelegate(typeof(Func<Point, int, Point>), method);
        }
    }
}
