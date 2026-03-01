using System;

namespace Elin_ArsMoriendi
{
    public class ActLifeDrain : Spell
    {
        public override bool IsHostileAct => true;

        public override bool Perform()
        {
            if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;

            try
            {
                var power = GetPower(Act.CC);
                var target = Act.TC.Chara;
                var caster = Act.CC;

                int flatAmount = Math.Max(1, power / 3);
                double hpRate = Math.Min(0.08, Math.Max(0.03, 0.03 + power * 0.00001));
                int scaledByMaxHp = Math.Max(1, (int)Math.Floor(target.MaxHP * hpRate));
                int rawAmount = Math.Max(flatAmount, scaledByMaxHp);
                int available = Math.Max(0, target.hp);
                int amount = Math.Min(rawAmount, available);

                LangHelper.Say("castLifeDrain", caster, Act.TC);

                if (amount > 0)
                {
                    NecroVFX.PlayDrain(target, caster, NecroVFX.DrainRed);
                    target.hp -= amount;
                    caster.HealHP(amount);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActLifeDrain.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
