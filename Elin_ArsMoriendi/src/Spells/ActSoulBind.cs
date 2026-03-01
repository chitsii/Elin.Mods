using System;

namespace Elin_ArsMoriendi
{
    public class ActSoulBind : Spell
    {
        public override string GetDetail()
        {
            int power = NecroSpellDetailUtil.GetCurrentPower(this);
            int turns = NecroSpellDetailUtil.EvaluateTurns("ConSoulBind", power, 3);
            string line = NecroSpellDetailUtil.L(
                $"現在威力 {power} / 持続 {turns}ターン / 身代わり 従者1体",
                $"Power {power} / Duration {turns} turns / Sacrifice 1 servant",
                $"当前威力 {power} / 持续 {turns} 回合 / 代死 1名仆从");
            return NecroSpellDetailUtil.AppendLine(base.GetDetail(), line);
        }

        public override bool Perform()
        {
            try
            {
                var power = GetPower(Act.CC);
                var caster = Act.CC;
                var mgr = NecromancyManager.Instance;

                var servants = mgr.GetCombatServantsInCurrentZone();
                if (servants.Count == 0)
                {
                    mgr.ClearSoulBindSacrificeUid();
                    LangHelper.Say("noServants", caster);
                    return true;
                }

                var sacrifice = servants[0];
                for (int i = 1; i < servants.Count; i++)
                {
                    var candidate = servants[i];
                    if (candidate.LV < sacrifice.LV
                        || (candidate.LV == sacrifice.LV && candidate.uid < sacrifice.uid))
                    {
                        sacrifice = candidate;
                    }
                }

                LangHelper.Say("castSoulBind", caster);
                caster.PlaySound("ars_se_soul_bind");
                CustomAssetFx.TryPlayAttachedToCard("FxSoulBind", caster, fallbackLifetime: 2f, replaceExisting: true);
                CustomAssetFx.TryPlayAttachedToCard("FxSoulBind", sacrifice, fallbackLifetime: 2f, replaceExisting: true);

                mgr.SetSoulBindSacrificeUid(sacrifice.uid);
                caster.AddCondition<ConSoulBind>(power, force: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActSoulBind.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

