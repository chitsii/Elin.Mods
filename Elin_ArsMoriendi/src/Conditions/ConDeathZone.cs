using System;
using System.Collections.Generic;
using System.Linq;

namespace Elin_ArsMoriendi
{
    public class ConDeathZone : Condition
    {
        public int AnchorX;
        public int AnchorZ;
        public int Radius = 4;
        public int DurationTurns = 3;

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

                NecroVFX.PlayDeathZoneTick(owner, center, Radius);

                List<Chara>? nearby = center.ListCharasInRadius(
                    owner,
                    Radius,
                    c => c != null && !c.isDead && c.IsAliveInCurrentZone && c != owner,
                    onlyVisible: false);
                if (nearby != null && nearby.Count > 0)
                {
                    nearby = nearby
                        .OrderBy(c => center.Distance(c.pos))
                        .ThenBy(c => c.uid)
                        .ToList();

                    double enemyPct = Math.Min(0.035, Math.Max(0.015, 0.015 + power * 0.000015));
                    double allyPct = Math.Min(0.050, Math.Max(0.020, 0.020 + power * 0.000020));

                    foreach (var chara in nearby)
                    {
                        if (chara.IsHostile(owner))
                        {
                            int enemyTickDamage = (int)Math.Max(
                                1.0,
                                Math.Max(power * 0.08, chara.MaxHP * enemyPct));
                            chara.DamageHP(enemyTickDamage, 915, power, AttackSource.Condition, owner);
                            NecroVFX.PlayTickDamage(chara);
                        }
                        else if (chara.IsFriendOrAbove(owner))
                        {
                            int allyTickHeal = (int)Math.Max(
                                1.0,
                                Math.Max(power * 0.05, chara.MaxHP * allyPct));
                            chara.HealHP(allyTickHeal);
                            NecroVFX.PlayTickHeal(chara);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ConDeathZone.Tick error: {ex.Message}");
            }

            Mod(-1);
        }

        private Point ResolveCenter()
        {
            if (owner != null && owner.pos != null && owner.pos.IsValid)
                return owner.pos;
            return EClass.pc?.pos ?? new Point(AnchorX, AnchorZ);
        }
    }
}
