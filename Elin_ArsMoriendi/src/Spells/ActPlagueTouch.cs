using System;

namespace Elin_ArsMoriendi
{
    public class ActPlagueTouch : Spell
    {
        public override bool IsHostileAct => true;

        public override string GetDetail()
        {
            int power = NecroSpellDetailUtil.GetCurrentPower(this);
            int turns = NecroSpellDetailUtil.EvaluateTurns("ConPlagueTouch", power, 5);
            int perTickBaseDamage = Math.Max(1, power / 15);
            double hpRate = Math.Min(0.020, 0.006 + power / 120000.0);
            int spreadPower = Math.Max(1, (int)Math.Floor(power * 0.65));
            int spreadTurns = NecroSpellDetailUtil.EvaluateTurns("ConPlagueTouch", spreadPower, 5);
            string line = NecroSpellDetailUtil.L(
                $"現在威力 {power} / 毒(毎ターン) 最低{perTickBaseDamage} か 最大HP×{hpRate * 100:0.0}% (上限3.0%) / 感染 {turns}ターン (拡散:25%判定→隣接各33%、威力{spreadPower}/{spreadTurns}ターン)",
                $"Power {power} / Poison per turn min {perTickBaseDamage} or MaxHP x {hpRate * 100:0.0}% (cap 3.0%) / Infection {turns} turns (spread: 25% check -> each adjacent 33%, power {spreadPower}/{spreadTurns} turns)",
                $"当前威力 {power} / 每回合毒伤 最低{perTickBaseDamage} 或 最大HP×{hpRate * 100:0.0}% (上限3.0%) / 感染 {turns} 回合 (扩散:25%判定 -> 相邻各33%，威力{spreadPower}/{spreadTurns}回合)");
            return NecroSpellDetailUtil.AppendLine(base.GetDetail(), line);
        }

        public override bool Perform()
        {
            if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;

            try
            {
                var power = GetPower(Act.CC);
                var target = Act.TC.Chara;

                LangHelper.Say("castPlagueTouch", Act.CC, Act.TC);
                NecroVFX.PlayCurse(target, NecroVFX.PlagueGreen);
                target.PlayEffect("mutation");

                target.AddCondition<ConPlagueTouch>(power, force: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActPlagueTouch.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
