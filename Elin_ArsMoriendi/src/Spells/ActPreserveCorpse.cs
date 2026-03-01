using System;

namespace Elin_ArsMoriendi
{
    public class ActPreserveCorpse : Spell
    {
        public override string GetDetail()
        {
            int power = NecroSpellDetailUtil.GetCurrentPower(this);
            int duration = NecromancyCalculations.CalculatePreserveCorpseDuration(power);
            int conditionPower = Math.Max(1, duration * 5);
            int turns = NecroSpellDetailUtil.EvaluateTurns("ConPreserveCorpse", conditionPower, 5);
            string line = NecroSpellDetailUtil.L(
                $"現在威力 {power} / 持続 {turns}ターン",
                $"Power {power} / Duration {turns} turns",
                $"当前威力 {power} / 持续 {turns} 回合");
            return NecroSpellDetailUtil.AppendLine(base.GetDetail(), line);
        }

        public override bool Perform()
        {
            try
            {
                var power = GetPower(Act.CC);
                var caster = Act.CC;
                int duration = 30 + power / 100;
                int preservePower = Math.Max(1, duration * 5); // ConPreserveCorpse uses duration p/5.

                LangHelper.Say("castPreserveCorpse", caster);
                NecroVFX.PlayDarkBuff(caster, "buff", "spell_buff");

                caster.AddCondition<ConPreserveCorpse>(preservePower, force: true);
                ConPreserveCorpse.EnsureAura(caster);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActPreserveCorpse.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

