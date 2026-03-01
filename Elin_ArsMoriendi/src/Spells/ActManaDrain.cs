using System;

namespace Elin_ArsMoriendi
{
    public class ActManaDrain : Spell
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

                int available = Math.Max(0, target.mana.value);
                int flatAmount = Math.Max(1, power / 4);
                double poolRate = Math.Min(0.22, Math.Max(0.08, 0.08 + power * 0.00003));
                int scaledByPool = Math.Max(1, (int)Math.Floor(available * poolRate));
                int rawAmount = Math.Max(flatAmount, scaledByPool);
                int amount = Math.Min(rawAmount, available);

                LangHelper.Say("castManaDrain", caster, Act.TC);

                if (amount > 0)
                {
                    NecroVFX.PlayDrain(target, caster, NecroVFX.DrainBlue);
                    target.mana.Mod(-amount);
                    caster.mana.Mod(amount / 2);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActManaDrain.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
