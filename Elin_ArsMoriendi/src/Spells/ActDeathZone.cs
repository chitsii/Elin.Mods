using System;

namespace Elin_ArsMoriendi
{
    public class ActDeathZone : Spell
    {
        public override bool Perform()
        {
            try
            {
                var power = GetPower(Act.CC);
                var caster = Act.CC;

                LangHelper.Say("castDeathZone", caster);
                var anchorOwner = caster;
                var anchorPos = caster.pos;

                int duration = 30 + power / 500;
                var con = Condition.Create<ConDeathZone>(power, c =>
                {
                    c.AnchorX = anchorPos.x;
                    c.AnchorZ = anchorPos.z;
                    c.Radius = 4;
                    c.DurationTurns = duration;
                });
                anchorOwner.AddCondition(con, force: true);
                NecroVFX.PlayDeathZoneCast(anchorOwner, anchorPos, 4);
                anchorOwner.PlaySound("ars_se_death_zone");
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActDeathZone.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

