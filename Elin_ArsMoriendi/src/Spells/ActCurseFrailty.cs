using System;

namespace Elin_ArsMoriendi
{
    public class ActCurseFrailty : Spell
    {
        public override bool IsHostileAct => true;

        public override bool Perform()
        {
            if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;

            try
            {
                var power = GetPower(Act.CC);
                var target = Act.TC.Chara;

                LangHelper.Say("castCurseFrailty", Act.CC, Act.TC);
                NecroVFX.PlayCurse(target, NecroVFX.PlagueGreen);

                target.AddCondition<ConCurseFrailty>(power, force: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActCurseFrailty.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
