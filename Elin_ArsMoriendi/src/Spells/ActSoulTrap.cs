using System;

namespace Elin_ArsMoriendi
{
    public class ActSoulTrap : Spell
    {
        public override bool IsHostileAct => true;

        public override string GetDetail()
        {
            int power = NecroSpellDetailUtil.GetCurrentPower(this);
            int turns = NecroSpellDetailUtil.EvaluateTurns("ConSoulTrapped", power, 5);
            string line = NecroSpellDetailUtil.L(
                $"現在威力 {power} / 魂魄保存 {turns}ターン",
                $"Power {power} / Preserve Soul {turns} turns",
                $"当前威力 {power} / 灵魂封存 {turns} 回合");
            return NecroSpellDetailUtil.AppendLine(base.GetDetail(), line);
        }

        public override bool Perform()
        {
            if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;

            try
            {
                var power = GetPower(Act.CC);
                var target = Act.TC.Chara;

                LangHelper.Say("castSoulTrap", Act.CC, Act.TC);
                target.PlayEffect("vanish");
                target.PlayEffect("telekinesis");
                target.PlaySound("curse3");

                target.AddCondition<ConSoulTrapped>(power, force: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActSoulTrap.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
