using System;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    internal static class HecatiaPartyVFX
    {
        private const string PartyPrefabName = "FxHecatiaPartyStage";
        private const float PartyFxLifetimeSeconds = 127f;
        private const float PartyFxScaleMultiplier = 0.25f;
        private const float PartyFallbackRadius = 7f; // 14f -> 7f: visible area ~1/4
        private const int PartyFallbackBurstCount = 24; // 96 -> 24

        private static readonly Color[] PartyPalette =
        {
            new Color(1.00f, 0.42f, 0.58f, 0.60f),
            new Color(0.36f, 0.78f, 1.00f, 0.60f),
            new Color(1.00f, 0.86f, 0.36f, 0.58f),
            new Color(0.52f, 1.00f, 0.72f, 0.58f),
            new Color(0.88f, 0.56f, 1.00f, 0.58f),
        };

        public static void Play()
        {
            var pc = EClass.pc;
            var map = EClass._map;
            if (pc == null)
            {
                ModLog.Warn("HecatiaPartyVFX skipped: pc is null.");
                return;
            }

            bool hasCustomFx = false;
            try
            {
                hasCustomFx = TryPlayLoopFx(pc);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"HecatiaPartyVFX custom prefab failed: {ex.Message}");
            }

            // Area burst is optional: if this fails, keep going and play guaranteed cues.
            try
            {
                if (map != null && pc.pos != null)
                {
                    var points = map.ListPointsInCircle(pc.pos, PartyFallbackRadius);
                    if (points != null && points.Count > 0)
                    {
                        int burstCount = Math.Min(PartyFallbackBurstCount, points.Count);
                        for (int i = 0; i < burstCount; i++)
                        {
                            var p = points[EClass.rnd(points.Count)];
                            if (p == null) continue;

                            var color = PartyPalette[EClass.rnd(PartyPalette.Length)];
                            Effect.Get("smoke").Play(p).SetParticleColor(color);

                            if ((i % 6) == 0)
                            {
                                Effect.Get("smoke_shockwave").Play(p);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"HecatiaPartyVFX area burst failed: {ex.Message}");
            }

            // Always try to emit an unmistakable local cue even if area/custom FX fail.
            try
            {
                pc.PlayEffect("telekinesis");
                pc.PlayEffect("buff");
                pc.PlayEffect("buff");
                pc.PlaySound("spell_funnel");
                Shaker.ShakeCam();
                Shaker.ShakeCam();
            }
            catch (Exception ex)
            {
                ModLog.Warn($"HecatiaPartyVFX guaranteed cue failed: {ex.Message}");
            }

            if (!hasCustomFx)
            {
                ModLog.Warn($"HecatiaPartyVFX prefab not found: {PartyPrefabName}. Fallback-only mode.");
            }
        }

        private static bool TryPlayLoopFx(Card pc)
        {
            return CustomAssetFx.TryPlayAttachedToCard(
                PartyPrefabName,
                pc,
                fallbackLifetime: PartyFxLifetimeSeconds,
                replaceExisting: true,
                forceLoop: true,
                scaleMultiplier: PartyFxScaleMultiplier,
                destroyOnZoneChange: true);
        }
    }
}
