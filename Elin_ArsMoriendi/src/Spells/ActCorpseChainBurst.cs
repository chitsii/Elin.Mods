using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public class ActCorpseChainBurst : Spell
    {
        private const float BlastRadius = 6f;
        private const float BlastVisualInterval = 0.06f;
        private const float WallBreakRadius = 4f;
        private static readonly Color CorpseBurstFlameTint = new Color(1f, 0.2f, 0.2f, 1f);

        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(caster);
                var center = caster.pos;

                var corpses = FindVisibleCorpses(caster);
                int corpseUsed = corpses.Count;
                if (corpseUsed <= 0)
                {
                    // No corpse nearby: fizzle without fake explosion damage/heal.
                    LangHelper.Say("castCorpseChainBurst", caster);
                    caster.PlayEffect("debuff");
                    return true;
                }

                var blastPoints = corpses
                    .Select(c => c.pos)
                    .Where(p => p != null && p.IsValid)
                    .ToList();

                ApplyCorpseBasedChainDamage(caster, blastPoints, corpseUsed, power);
                PlayChainExplosionVfxSequentially(caster, blastPoints);
                ConsumeCorpses(corpses, corpseUsed);

                double healPct = Clamp(0.12 + 0.06 * corpseUsed + power * 0.0004, 0.20, 0.55);
                HealLegion(caster, healPct);
                ApplyOneHpProtection(caster);

                LangHelper.Say("castCorpseChainBurst", caster);
                caster.PlaySound("ars_se_corpse_chain_burst");
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActCorpseChainBurst.Perform failed: {ex.Message}");
            }

            return true;
        }

        private static void ApplyCorpseBasedChainDamage(
            Chara caster,
            List<Point> blastPoints,
            int corpseUsed,
            int power
        )
        {
            if (blastPoints.Count <= 0) return;

            double basePct = Clamp(0.05 + power * 0.0004, 0.05, 0.25);
            double corpsePctBonus = 0.02 * Math.Max(0, corpseUsed - 1);
            double hitPct = basePct + corpsePctBonus;
            var (diceNum, diceSides) = NecromancyCalculations.CorpseBurstDice(power);
            var dealtByTargetUid = new Dictionary<int, double>();
            var confusedTargetUids = new HashSet<int>();

            for (int step = 0; step < blastPoints.Count; step++)
            {
                Point blastCenter = blastPoints[step];
                if (blastCenter == null || !blastCenter.IsValid) continue;

                var targets = EClass._map.ListCharasInCircle(blastCenter, BlastRadius, los: false)
                    .Where(c => c != null && c.IsAliveInCurrentZone && c.IsHostile(caster))
                    .OrderBy(c => blastCenter.Distance(c.pos))
                    .ThenBy(c => c.uid)
                    .ToList();

                // No chain attenuation: each corpse explosion keeps full direct damage.
                const double chainMul = 1.0;
                foreach (var target in targets)
                {
                    if (confusedTargetUids.Add(target.uid))
                    {
                        target.AddCondition<ConConfuse>(power, force: true);
                    }

                    bool isBoss = target.IsPowerful || target.rarity >= Rarity.Legendary;
                    double capPct = isBoss ? 0.35 : 0.60;
                    double capDamage = target.MaxHP * capPct;
                    dealtByTargetUid.TryGetValue(target.uid, out double dealt);
                    if (dealt >= capDamage) continue;

                    double rawPctDamage = target.MaxHP * hitPct * chainMul;
                    int rolledDice = Dice.Roll(diceNum, diceSides, 0, caster);
                    double hitDamage = Math.Max(rawPctDamage, (double)rolledDice);
                    long apply = (long)Math.Floor(Math.Min(hitDamage, capDamage - dealt));
                    if (apply <= 0) continue;

                    target.DamageHP(apply, 916, power, AttackSource.None, caster);
                    dealtByTargetUid[target.uid] = dealt + apply;
                    target.PlayEffect("curse");
                }

                DestroyBreakableWalls(blastCenter, power, WallBreakRadius);
            }
        }

        private static void PlayChainExplosionVfxSequentially(Chara caster, List<Point> blastPoints)
        {
            if (blastPoints == null || blastPoints.Count == 0) return;

            for (int step = 0; step < blastPoints.Count; step++)
            {
                Point p = blastPoints[step];
                if (p == null || !p.IsValid) continue;

                float delay = step * BlastVisualInterval;
                DOVirtual.DelayedCall(delay, () =>
                {
                    try
                    {
                        if (p == null || !p.IsValid) return;
                        if (EClass._zone == null || EClass._map == null) return;

                        p.Animate(AnimeID.Quake, animeBlock: true);
                        EClass.pc?.pos?.Animate(AnimeID.Quake, animeBlock: true);
                        // Vanilla blast effect across the same effective radius as damage.
                        var areaPoints = EClass._map.ListPointsInCircle(
                            p,
                            BlastRadius,
                            mustBeWalkable: false,
                            los: false
                        );
                        if (areaPoints == null || areaPoints.Count == 0)
                        {
                            areaPoints = new List<Point> { p };
                        }
                        foreach (var ep in areaPoints)
                        {
                            if (ep == null || !ep.IsValid) continue;
                            ep.PlayEffect("explosion");
                        }

                        // Play a blast SFX for each chain reaction step.
                        p.PlaySound("explosion");
                        // Match drama "shake" behavior (DramaManager -> Shaker.ShakeCam()).
                        Shaker.ShakeCam();
                        // Custom FX disabled temporarily for vanilla-only visual comparison.
                        // CustomAssetFx.PlayAt("FxCorpseChainBurst", p.PositionCenter(), tint: CorpseBurstFlameTint);
                    }
                    catch (Exception ex)
                    {
                        ModLog.Warn($"CorpseChainBurst VFX step failed: {ex.Message}");
                    }
                });
            }
        }

        private static List<Thing> FindVisibleCorpses(Chara caster)
        {
            var viewer = EClass.pc ?? caster;
            var center = viewer.pos;
            int radius = Math.Max(6, viewer.GetSightRadius() + 1);
            var points = EClass._map.ListPointsInCircle(center, radius, mustBeWalkable: false, los: false);
            var corpses = new List<Thing>();
            foreach (var p in points)
            {
                    if (p?.Things == null) continue;
                    foreach (var thing in p.Things)
                    {
                        if (thing == null || thing.isDestroyed || !thing.pos.IsValid) continue;
                        if (!(thing.trait is TraitFoodMeat) || string.IsNullOrEmpty(thing.c_idRefCard)) continue;
                        if (EClass.pc == null || !EClass.pc.CanSee(thing)) continue;
                        if (thing.Num <= 0) continue;

                        // Treat stacked corpses as multiple corpse units:
                        // one corpse item consumed => one chain-reaction step.
                        int units = Math.Max(1, thing.Num);
                        for (int i = 0; i < units; i++)
                            corpses.Add(thing);
                    }
                }

            return corpses
                .OrderBy(t => center.Distance(t.pos))
                .ThenBy(t => t.uid)
                .ToList();
        }

        private static void ConsumeCorpses(List<Thing> corpses, int corpseUsed)
        {
            for (int i = 0; i < corpseUsed; i++)
            {
                var corpse = corpses[i];
                if (corpse == null || corpse.isDestroyed) continue;
                if (corpse.Num > 1) corpse.ModNum(-1);
                else corpse.Destroy();
            }
        }

        private static void HealLegion(Chara caster, double healPct)
        {
            int casterHeal = Math.Max(1, (int)Math.Floor(caster.MaxHP * healPct));
            caster.HealHP(casterHeal);

            foreach (var servant in NecromancyManager.Instance.GetCombatServantsInCurrentZone())
            {
                int heal = Math.Max(1, (int)Math.Floor(servant.MaxHP * healPct));
                servant.HealHP(heal);
                servant.PlayEffect("heal_tick");
            }
        }

        private static void ApplyOneHpProtection(Chara caster)
        {
            caster.AddCondition<ConInvulnerable>(2000, force: true);
            foreach (var servant in NecromancyManager.Instance.GetCombatServantsInCurrentZone())
                servant.AddCondition<ConInvulnerable>(2000, force: true);
        }

        private static void DestroyBreakableWalls(Point center, int power, float radius)
        {
            int hardness = Math.Min(120, Math.Max(30, 40 + power / 30));
            var points = EClass._map.ListPointsInCircle(center, radius, mustBeWalkable: false, los: false);
            foreach (var p in points)
            {
                if (p.HasObj && p.cell.matObj.hardness <= hardness)
                {
                    EClass._map.MineObj(p);
                    continue;
                }
                if (!p.HasObj && p.HasBlock && p.matBlock.hardness <= hardness)
                {
                    EClass._map.MineBlock(p);
                }
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

    }
}

