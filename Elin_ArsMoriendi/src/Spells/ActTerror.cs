using System;

namespace Elin_ArsMoriendi
{
    public class ActTerror : Spell
    {
        public override bool IsHostileAct => true;

        public override string GetDetail()
        {
            int power = NecroSpellDetailUtil.GetCurrentPower(this);
            int turns = NecroSpellDetailUtil.EvaluateTurns("ConFear", power, 5);
            string line = NecroSpellDetailUtil.L(
                $"現在威力 {power} / 恐怖 {turns}ターン",
                $"Power {power} / Terror {turns} turns",
                $"当前威力 {power} / 恐惧 {turns} 回合");
            return NecroSpellDetailUtil.AppendLine(base.GetDetail(), line);
        }

        public override bool Perform()
        {
            if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;

            try
            {
                var power = GetPower(Act.CC);
                var target = Act.TC.Chara;

                LangHelper.Say("castTerror", Act.CC, Act.TC);
                NecroVFX.PlayCurse(target, NecroVFX.CursePurple, "scream");
                target.PlayEffect("scream");

                target.AddCondition<ConFear>(power, force: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActTerror.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
