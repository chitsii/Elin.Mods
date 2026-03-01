using System;

namespace Elin_ArsMoriendi
{
    public class ActFuneralMarch : Spell
    {
        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(caster);
                var servants = NecromancyManager.Instance.GetCombatServantsInCurrentZone();

                int duration = 30 + power / 250;
                double pcSlowRate = Math.Min(0.18, Math.Max(0.10, 0.10 + power * 0.00010));
                int pcSlowDeltaRaw = Math.Max(8, (int)Math.Floor(caster.Speed * pcSlowRate));
                int pcSlowDeltaCap = Math.Max(1, (int)Math.Floor(caster.Speed * (2.0 / 3.0)));
                int pcSlowDelta = Math.Min(pcSlowDeltaRaw, pcSlowDeltaCap);

                var pcCon = Condition.Create<ConFuneralMarchPc>(power, con =>
                {
                    con.DurationTurns = duration;
                    con.SpeedPenalty = pcSlowDelta;
                });
                caster.AddCondition(pcCon, force: true);

                double servantHasteRate = Math.Max(0.20, 0.20 + power * 0.00025);
                double servantMainAttrRate = Math.Min(0.28, Math.Max(0.10, 0.10 + power * 0.00015));

                foreach (var servant in servants)
                {
                    int servantSpdDelta = Math.Max(10, (int)Math.Floor(servant.Speed * servantHasteRate));
                    int servantStrDelta = Math.Max(6, (int)Math.Floor(servant.STR * servantMainAttrRate));
                    int servantEndDelta = Math.Max(6, (int)Math.Floor(servant.END * servantMainAttrRate));

                    var servantCon = Condition.Create<ConFuneralMarchServant>(power, con =>
                    {
                        con.DurationTurns = duration;
                        con.SpeedBonus = servantSpdDelta;
                        con.StrBonus = servantStrDelta;
                        con.EndBonus = servantEndDelta;
                    });
                    servant.AddCondition(servantCon, force: true);
                }

                LangHelper.Say("castFuneralMarch", caster);
                NecroVFX.PlayDarkBuff(caster, "darkwomb2");
                foreach (var servant in servants)
                {
                    CustomAssetFx.PlayAtCard("FxFuneralMarch", servant);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActFuneralMarch.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

