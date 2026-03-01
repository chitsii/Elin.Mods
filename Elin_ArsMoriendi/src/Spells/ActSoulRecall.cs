using System;
using System.Collections.Generic;
using System.Linq;

namespace Elin_ArsMoriendi
{
    public class ActSoulRecall : Spell
    {
        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(caster);
                int reviveCount = 1 + (int)Math.Floor(Math.Sqrt(Math.Max(power, 0)) / 12.0);

                var mgr = NecromancyManager.Instance;
                var deadServants = mgr.GetAllServants()
                    .Where(s => !s.isAlive && s.chara != null && !s.chara.isDestroyed)
                    .Select(s => s.chara)
                    .OrderByDescending(s => s.turn)
                    .ThenBy(s => s.uid)
                    .ToList();

                if (deadServants.Count == 0)
                {
                    LangHelper.Say("soulRecallNoTarget", caster);
                    return true;
                }

                var anchor = caster;
                var toRevive = deadServants.Take(Math.Min(reviveCount, deadServants.Count)).ToList();
                var positions = ResolveSpawnPositions(anchor.pos, caster.pos, toRevive.Count);

                for (int i = 0; i < toRevive.Count; i++)
                {
                    var servant = toRevive[i];
                    var spawn = positions[i];

                    servant.GetRevived();
                    if (spawn != null && spawn.IsValid)
                        servant.Teleport(spawn, silent: true, force: true);

                    servant.hp = Math.Max(1, servant.MaxHP / 2);
                    servant.AddCondition<ConInvulnerable>(2000, force: true);
                    servant.PlayEffect("revive");
                }

                LangHelper.Say("castSoulRecall", caster);
                NecroVFX.PlayDarkBuff(caster, "revive");
                caster.PlaySound("ars_se_soul_recall");
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActSoulRecall.Perform failed: {ex.Message}");
            }

            return true;
        }

        private static List<Point> ResolveSpawnPositions(Point primaryCenter, Point fallbackCenter, int count)
        {
            var positions = new List<Point>(count);
            var reserved = new HashSet<int>();

            FillPositions(primaryCenter, count, positions, reserved);
            if (positions.Count < count)
                FillPositions(fallbackCenter, count, positions, reserved);

            while (positions.Count < count)
            {
                positions.Add(ActEffect.GetTeleportPos(fallbackCenter, 6));
            }

            return positions;
        }

        private static void FillPositions(Point center, int targetCount, List<Point> positions, HashSet<int> reserved)
        {
            foreach (var p in EnumerateSpiral(center, 12))
            {
                if (positions.Count >= targetCount) break;
                if (!IsValidSpawn(p, reserved)) continue;

                reserved.Add(p.index);
                positions.Add(p);
            }
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

        private static bool IsValidSpawn(Point p, HashSet<int> reserved)
        {
            if (p == null || !p.IsValid || !p.IsInBounds) return false;
            if (reserved.Contains(p.index)) return false;
            if (p.IsBlocked || p.HasChara) return false;
            return true;
        }
    }
}

