using System;
using System.Collections.Generic;

namespace Elin_ArsMoriendi
{
    public class ConPlagueTouch : Condition
    {
        public override bool ShouldRefresh => true;
        public override int GainResistFactor => 0;
        public override int EvaluateTurn(int p) => Math.Min(20, Math.Max(8, 8 + p / 150));

        public override void Tick()
        {
            try
            {
                int flatDamage = Math.Max(1, power / 15);
                double hpRate = Math.Min(0.020, 0.006 + power / 120000.0);
                int scaledByMaxHp = Math.Max(1, (int)Math.Floor(owner.MaxHP * hpRate));
                int rawDamage = Math.Max(flatDamage, scaledByMaxHp);
                int capDamage = Math.Max(1, (int)Math.Floor(owner.MaxHP * 0.03));
                int damage = Math.Min(rawDamage, capDamage);
                owner.DamageHP(damage, 915, power, AttackSource.Condition);

                // Low chance to spread to adjacent characters
                if (EClass.rnd(4) == 0)
                {
                    var nearby = owner.pos.ListCharasInRadius(owner, 1,
                        c => c != owner && !c.IsPCFactionOrMinion && !c.HasCondition<ConPlagueTouch>());
                    if (nearby != null)
                    {
                        foreach (var chara in nearby)
                        {
                            if (EClass.rnd(3) == 0)
                            {
                                int spreadPower = Math.Max(1, (int)Math.Floor(power * 0.65));
                                chara.AddCondition<ConPlagueTouch>(spreadPower, force: true);
                                NecroVFX.PlayPlagueSpread(chara);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ConPlagueTouch.Tick error: {ex.Message}");
            }

            Mod(-1);
        }
    }
}
