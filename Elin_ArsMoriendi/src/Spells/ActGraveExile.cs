using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public class ActGraveExile : Spell
    {
        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(caster);
                var mgr = NecromancyManager.Instance;

                var servant = Act.TC?.Chara;
                if (servant == null || servant.isDead || !servant.IsAliveInCurrentZone || !mgr.IsServant(servant.uid))
                {
                    LangHelper.Say("graveExileNeedServant", caster);
                    return true;
                }

                var candidates = servant.pos.ListCharasInRadius(
                    servant,
                    3,
                    c => c != null && c != servant && c.IsAliveInCurrentZone && c.IsHostile(servant),
                    onlyVisible: false)
                    ?.OrderBy(c => servant.pos.Distance(c.pos))
                    .ThenBy(c => c.uid)
                    .ToList()
                    ?? new List<Chara>();

                int enemyMoveCount = Math.Min(10, Math.Min(2 + power / 220, candidates.Count));

                int mapWidth = EClass._map?.bounds?.Width ?? 50;
                int teleportDistance = Math.Max(16, Math.Min(48, (int)Math.Floor(
                    mapWidth * (0.30 + Math.Min(0.15, power / 5000.0)))));

                Point destination = FindFarPoint(servant.pos, teleportDistance)
                    ?? ActEffect.GetTeleportPos(servant.pos, teleportDistance);

                Vector3 originFxPos = servant.renderer != null
                    ? servant.renderer.PositionCenter()
                    : (Vector3)servant.pos.PositionCenter();

                servant.Teleport(destination, silent: true, force: true);

                var reserved = new HashSet<int> { destination.index };
                for (int i = 0; i < enemyMoveCount; i++)
                {
                    var enemy = candidates[i];
                    var spot = FindNearbyPlacement(destination, reserved)
                        ?? ActEffect.GetTeleportPos(destination, 6);
                    enemy.Teleport(spot, silent: true, force: true);
                    reserved.Add(spot.index);
                }

                LangHelper.Say("castGraveExile", caster);
                CustomAssetFx.PlayAt("FxGraveExile", originFxPos);
                NecroVFX.PlayDarkBuff(servant, "darkwomb3", "spell_funnel");
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActGraveExile.Perform failed: {ex.Message}");
            }

            return true;
        }

        private static Point? FindFarPoint(Point origin, int minDistance)
        {
            foreach (var p in EnumerateSpiral(origin, minDistance + 16))
            {
                if (!IsValidPoint(p)) continue;
                if (origin.Distance(p) < minDistance) continue;
                return p;
            }

            return null;
        }

        private static Point? FindNearbyPlacement(Point center, HashSet<int> reserved)
        {
            foreach (var p in EnumerateSpiral(center, 8))
            {
                if (!IsValidPoint(p)) continue;
                if (reserved.Contains(p.index)) continue;
                return p;
            }
            return null;
        }

        private static IEnumerable<Point> EnumerateSpiral(Point center, int maxRadius)
        {
            yield return center;

            for (int r = 1; r <= maxRadius; r++)
            {
                int minX = center.x - r;
                int maxX = center.x + r;
                int minZ = center.z - r;
                int maxZ = center.z + r;

                for (int x = minX; x <= maxX; x++) yield return new Point(x, minZ);
                for (int z = minZ + 1; z <= maxZ; z++) yield return new Point(maxX, z);
                for (int x = maxX - 1; x >= minX; x--) yield return new Point(x, maxZ);
                for (int z = maxZ - 1; z > minZ; z--) yield return new Point(minX, z);
            }
        }

        private static bool IsValidPoint(Point p)
        {
            if (p == null || !p.IsValid || !p.IsInBounds) return false;
            if (p.IsBlocked || p.HasChara) return false;
            return true;
        }
    }
}

