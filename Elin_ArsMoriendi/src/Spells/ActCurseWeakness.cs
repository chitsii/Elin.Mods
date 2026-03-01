using System;

namespace Elin_ArsMoriendi
{
    public class ActCurseWeakness : Spell
    {
        public override bool IsHostileAct => true;

        public override bool Perform()
        {
            if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;

            try
            {
                var power = GetPower(Act.CC);
                var target = Act.TC.Chara;

                LangHelper.Say("castCurseWeakness", Act.CC, Act.TC);
                NecroVFX.PlayCurse(target, NecroVFX.CursePurple);

                target.AddCondition<ConCurseWeakness>(power, force: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActCurseWeakness.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
