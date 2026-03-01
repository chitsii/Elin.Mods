using System;

namespace Elin_ArsMoriendi
{
    public class ActSoulTrapMass : Spell
    {
        public override string GetDetail()
        {
            int power = NecroSpellDetailUtil.GetCurrentPower(this);
            int duration = NecromancyCalculations.CalculateSoulTrapMassDuration(power);
            int conditionPower = Math.Max(1, duration * 5);
            int turns = NecroSpellDetailUtil.EvaluateTurns("ConSoulTrapped", conditionPower, 5);
            string line = NecroSpellDetailUtil.L(
                $"現在威力 {power} / 半径6の敵全体 / 魂魄保存 {turns}ターン",
                $"Power {power} / All enemies in radius 6 / Preserve Soul {turns} turns",
                $"当前威力 {power} / 半径6内全体敌人 / 灵魂封存 {turns} 回合");
            return NecroSpellDetailUtil.AppendLine(base.GetDetail(), line);
        }

        public override bool Perform()
        {
            try
            {
                var power = GetPower(Act.CC);
                var caster = Act.CC;
                int duration = 30 + power / 120;
                int trapPower = Math.Max(1, duration * 5); // ConSoulTrapped uses duration p/5.

                LangHelper.Say("castSoulTrapMass", caster);
                caster.PlaySound("curse3");

                var targets = caster.pos.ListCharasInRadius(caster, 6,
                    c => c != caster && c.IsHostile(caster) && NecroSpellUtil.HasSoul(c));
                NecroVFX.PlaySoulTrapMassBurst(caster, 6, targets);

                if (targets != null)
                {
                    foreach (var target in targets)
                    {
                        target.AddCondition<ConSoulTrapped>(trapPower, force: true);
                        target.PlayEffect("vanish");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActSoulTrapMass.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

