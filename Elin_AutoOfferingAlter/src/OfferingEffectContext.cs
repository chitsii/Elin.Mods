using System;
using HarmonyLib;

namespace Elin_AutoOfferingAlter
{
    internal sealed class OfferingEffectContext
    {
        private readonly Thing container;
        private readonly Chara actor;
        private readonly Religion faith;
        private readonly Zone zone;
        private readonly Map map;
        private readonly Array cells;
        private readonly Point ownerPoint;

        private OfferingEffectContext(Thing container, Chara actor, Religion faith, Zone zone, Map map)
        {
            this.container = container; this.actor = actor; this.faith = faith; this.zone = zone; this.map = map;
            cells = map.cells; ownerPoint = container.pos;
        }

        internal static bool TryCreate(Thing container, Chara actor, out OfferingEffectContext context)
        {
            context = null;
            if (container == null || actor == null || EClass.game == null || EClass.player == null
                || EClass._zone == null || EClass._map == null || container.pos == null) return false;
            var candidate = new OfferingEffectContext(container, actor, actor.faith, EClass._zone, EClass._map);
            if (!candidate.IsCurrent()) return false;
            context = candidate;
            return true;
        }

        internal bool IsCurrent()
        {
            return EClass.pc == actor && !actor.isDead && faith != null && actor.faith == faith
                && !container.isDestroyed && ReferenceEquals(container.pos, ownerPoint) && container.GetRootCard() == actor
                && EClass.game != null && EClass.player != null && EClass.game.activeZone == zone
                && EClass._zone == zone && EClass.player.zone == zone && actor.currentZone == zone
                && ReferenceEquals(EClass._map, map) && ReferenceEquals(Point.map, map) && ReferenceEquals(map.cells, cells)
                && actor.pos != null && OfferingMapBounds.Contains(cells, actor.pos.x, actor.pos.z);
        }

        internal IDisposable Enter()
        {
            return new OfferingEffectScope<Point>(ownerPoint, IsCurrent, () => actor.pos.Copy());
        }
    }

    // Registered with the product, but inert outside its synchronous, exact-owner offer scope.
    [HarmonyPatch(typeof(Effect), nameof(Effect.Play), new Type[] {
        typeof(Point), typeof(float), typeof(Point), typeof(UnityEngine.Sprite)
    })]
    public static class PatchOfferingEffectPosition
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static void Prefix(ref Point from)
        {
            OfferingEffectScope<Point>.TryRedirect(ref from);
        }
    }
}
