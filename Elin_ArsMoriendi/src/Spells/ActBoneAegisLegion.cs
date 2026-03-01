using System;

namespace Elin_ArsMoriendi
{
    public class ActBoneAegisLegion : Spell
    {
        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(caster);
                var servants = NecromancyManager.Instance.GetCombatServantsInCurrentZone();
                int duration = 30 + power / 300;

                caster.AddCondition<ConInvulnerable>(2000, force: true);
                var casterCon = Condition.Create<ConBoneShield>(power, c => { c.DurationTurns = duration; });
                caster.AddCondition(casterCon, force: true);
                NecroVFX.PlayBoneAegisCaster(caster);

                foreach (var servant in servants)
                {
                    servant.AddCondition<ConInvulnerable>(2000, force: true);
                    var servantCon = Condition.Create<ConBoneShield>(power, c => { c.DurationTurns = duration; });
                    servant.AddCondition(servantCon, force: true);
                    NecroVFX.PlayBoneAegisShield(servant);
                }

                LangHelper.Say("castBoneAegisLegion", caster);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActBoneAegisLegion.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

