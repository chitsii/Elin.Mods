using System;

namespace Elin_ArsMoriendi
{
    public class ActUnholyVigor : Spell
    {
        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(caster);

                int mpCost = Math.Max(1, caster.mana.value / 3);
                if (caster.mana.value < mpCost)
                {
                    return true;
                }

                int efficiencyPct = Math.Min(200, 100 + Math.Max(0, power / 20));
                int rawHeal = Math.Max(1, mpCost * 2 * efficiencyPct / 100);
                int rawStamina = Math.Max(1, mpCost * efficiencyPct / 100);

                int minHeal = Math.Max(1, (int)Math.Floor(caster.MaxHP * 0.15));
                int maxHeal = Math.Max(minHeal, (int)Math.Floor(caster.MaxHP * 0.45));
                int healAmount = Math.Min(maxHeal, Math.Max(minHeal, rawHeal));

                int maxStamina = Math.Max(1, caster.stamina.max);
                int minStamina = Math.Max(1, (int)Math.Floor(maxStamina * 0.15));
                int maxStaminaGain = Math.Max(minStamina, (int)Math.Floor(maxStamina * 0.45));
                int staminaAmount = Math.Min(maxStaminaGain, Math.Max(minStamina, rawStamina));

                LangHelper.Say("castUnholyVigor", caster);
                caster.PlayEffect("revive");
                caster.PlaySound("curse3");

                caster.mana.Mod(-mpCost);
                caster.HealHP(healAmount);
                caster.stamina.Mod(staminaAmount);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActUnholyVigor.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

