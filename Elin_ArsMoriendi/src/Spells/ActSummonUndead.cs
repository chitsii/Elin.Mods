using System;

namespace Elin_ArsMoriendi
{
    public class ActSummonUndead : Spell
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

                var chara = CharaGen.CreateFromFilter(
                    SpawnListChara.Get(
                        "summon_undead",
                        (SourceChara.Row r) =>
                            r.HasTag(CTAG.undead)
                            || (EClass.sources.races.map.TryGetValue(r.race)?.IsUndead ?? false)
                    ),
                    summonLv
                );

                if (chara == null)
                {
                    chara = CharaGen.Create("skeleton_berserker", summonLv);
                }

                if (chara.LV < summonLv)
                {
                    chara.SetLv(summonLv);
                    chara.hp = chara.MaxHP;
                }

                var point = caster.pos.GetNearestPointCompat(allowBlock: false, allowChara: false);
                EClass._zone.AddCard(chara, point);

                chara.MakeMinion(caster);
                chara.SetSummon(200 + power / 2);

                // Track as servant (UI listing)
                // Die patch will clean up when summon expires
                NecromancyManager.Instance.AddServant(chara);

                NecroVFX.PlaySummon(chara);
                caster.Say("summon_ally", caster);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActSummonUndead.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
