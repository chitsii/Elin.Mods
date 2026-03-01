using System;
using System.Collections.Generic;
using System.Linq;

namespace Elin_ArsMoriendi
{
    public class ConGraveQuagmireField : Condition
    {
        public int AnchorX;
        public int AnchorZ;
        public int Radius = 4;
        public int DurationTurns = 3;
        public int SilenceTurns = 1;
        public int SuppressTurns = 2;

        public override bool ShouldRefresh => true;
        public override bool ShouldOverride(Condition c) => true;
        public override bool IsOverrideConditionMet(Condition c, int turn) => true;
        public override int GainResistFactor => 0;
        public override int EvaluateTurn(int p) => Math.Max(1, DurationTurns);

        public override void Tick()
        {
            try
            {
                var center = ResolveCenter();
                if (center == null || !center.IsValid)
                {
                    Mod(-1);
                    return;
                }

                CustomAssetFx.PlayAt("FxGraveQuagmire", center.PositionCenter(), tint: NecroVFX.QuagmireKhaki);

                // Area visualization: random smoke across the zone
                var areaPoints = EClass._map.ListPointsInCircle(center, Radius);
                if (areaPoints != null)
                {
                    int maxFx = Math.Min(8, areaPoints.Count);
                    for (int i = 0; i < maxFx; i++)
                    {
                        var p = areaPoints[EClass.rnd(areaPoints.Count)];
                        if (p != null)
                            Effect.Get("smoke").Play(p).SetParticleColor(NecroVFX.QuagmireKhaki);
                    }
                }

                List<Chara>? targets = center.ListCharasInRadius(
                    owner,
                    Radius,
                    c => c != null && !c.isDead && c.IsAliveInCurrentZone && c.IsHostile(owner),
                    onlyVisible: false);

                if (targets != null && targets.Count > 0)
                {
                    targets = targets
                        .OrderBy(c => center.Distance(c.pos))
                        .ThenBy(c => c.uid)
                        .ToList();

                    double spdDownRate = Math.Min(0.42, Math.Max(0.18, 0.18 + power * 0.00015));

                    foreach (var target in targets)
                    {
                        int spdDownDelta = Math.Max(10, (int)Math.Floor(target.Speed * spdDownRate));
                        target.AddCondition<ConGraveQuagmireSlow>(spdDownDelta, force: true);

                        var silenceCon = Condition.Create<ConGraveQuagmireSilence>(power, con =>
                        {
                            con.DurationTurns = Math.Max(1, SilenceTurns);
                        });
                        var silenceApplied = target.AddCondition(silenceCon, force: false);
                        if (silenceApplied == null)
                        {
                            int suppressPower = Math.Max(20, SuppressTurns * 20);
                            target.AddCondition<ConSupress>(suppressPower, force: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ConGraveQuagmireField.Tick error: {ex.Message}");
            }

            Mod(-1);
        }

        private Point ResolveCenter()
        {
            // Follow owner's current position for PC-tracking area
            if (owner != null && owner.pos != null && owner.pos.IsValid)
                return owner.pos;
            return EClass.pc?.pos ?? new Point(AnchorX, AnchorZ);
        }
    }
}
