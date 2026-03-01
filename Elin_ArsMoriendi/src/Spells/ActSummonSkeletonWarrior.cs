using System;

namespace Elin_ArsMoriendi
{
    public class ActSummonSkeletonWarrior : Spell
    {
        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(Act.CC);
                int summonLv = Math.Max(1, power / 5);
                if (caster.IsPC)
                {
                    int deepest = EClass.player?.stats?.deepest ?? 1;
                    summonLv = Math.Max(summonLv, deepest);
                }

                if (EClass._zone.CountMinions(caster) >= caster.MaxSummon)
                {
                    caster.Say("summon_ally_fail", caster);
                    return true;
                }

                var chara = CharaGen.Create("skeleton_berserker", summonLv);
                if (chara == null)
                {
                    ModLog.Warn("ActSummonSkeletonWarrior: CharaGen.Create returned null");
                    return true;
                }

                if (chara.LV < summonLv)
                {
                    chara.SetLv(summonLv);
                    chara.hp = chara.MaxHP;
                }

                var point = caster.pos.GetNearestPointCompat(allowBlock: false, allowChara: false);
                EClass._zone.AddCard(chara, point);

                chara.MakeMinion(caster);
                chara.SetSummon(300 + power * 2 / 3);

                NecromancyManager.Instance.AddServant(chara);

                NecroVFX.PlaySummon(chara);
                LangHelper.Say("castSummonSkeletonWarrior", caster);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActSummonSkeletonWarrior.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
