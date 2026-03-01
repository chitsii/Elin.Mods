using System;

namespace Elin_ArsMoriendi
{
    public class ActGraveQuagmire : Spell
    {
        public override bool Perform()
        {
            try
            {
                var caster = Act.CC;
                var power = GetPower(caster);
                var anchorOwner = caster;
                var anchorPoint = caster.pos;

                int radius = Math.Min(5, Math.Max(4, 4 + power / 500));
                int duration = 30 + power / 450;
                int silenceTurns = 5 + power / 300;
                int suppressTurns = 6 + power / 400;

                var con = Condition.Create<ConGraveQuagmireField>(power, c =>
                {
                    c.AnchorX = anchorPoint.x;
                    c.AnchorZ = anchorPoint.z;
                    c.Radius = radius;
                    c.DurationTurns = duration;
                    c.SilenceTurns = silenceTurns;
                    c.SuppressTurns = suppressTurns;
                });
                anchorOwner.AddCondition(con, force: true);

                LangHelper.Say("castGraveQuagmire", caster);
                CustomAssetFx.PlayAt("FxGraveQuagmire", anchorPoint.PositionCenter(), tint: NecroVFX.QuagmireKhaki);
                caster.PlaySound("ars_se_grave_quagmire");

                // Area visualization (initial burst)
                var areaPoints = EClass._map.ListPointsInCircle(anchorPoint, radius);
                if (areaPoints != null)
                {
                    int maxFx = Math.Min(8, areaPoints.Count);
                    for (int i = 0; i < maxFx; i++)
                    {
                        var p = areaPoints[EClass.rnd(areaPoints.Count)];
                        if (p != null)
                            Effect.Get("smoke").Play(p).SetParticleColor(NecroVFX.QuagmireKhaki);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActGraveQuagmire.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}

