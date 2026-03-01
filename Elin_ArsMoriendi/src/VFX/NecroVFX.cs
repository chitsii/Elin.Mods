using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public static class NecroVFX
    {
        public static readonly Color DrainRed    = new Color(0.6f, 0f, 0.1f);
        public static readonly Color DrainBlue   = new Color(0.2f, 0.1f, 0.6f);
        public static readonly Color DrainAmber  = new Color(0.6f, 0.4f, 0f);
        public static readonly Color CursePurple = new Color(0.4f, 0.1f, 0.6f);
        public static readonly Color PlagueGreen = new Color(0.2f, 0.5f, 0.1f);
        public static readonly Color SoulBlue    = new Color(0.4f, 0.6f, 0.9f);
        public static readonly Color BoneWhite   = new Color(0.9f, 0.85f, 0.7f);
        public static readonly Color DeathBlack  = new Color(0.15f, 0.05f, 0.2f);
        public static readonly Color QuagmireKhaki = new Color(0.60f, 0.58f, 0.28f, 0.90f);
        public static readonly Color ShadowTentacle = new Color(0.07f, 0.06f, 0.09f, 0.68f);
        public static readonly Color ShadowMist = new Color(0.10f, 0.10f, 0.12f, 0.45f);

        public static void PlayDrain(Card target, Card caster, Color color)
        {
            target.PlayEffect("blood");
            target.PlaySound("curse3");
            target.renderer.PlayAnime(AnimeID.Shiver);
            Effect.Get("trail1").SetParticleColor(color, true, "_TintColor")
                .Play(target.pos, 0f, caster.pos);
            caster.PlayEffect("revive");
        }

        public static void PlayCurse(Card target, Color color, string sound = "curse3")
        {
            target.PlayEffect("curse");
            target.PlaySound(sound);
            target.renderer.PlayAnime(AnimeID.Shiver);
        }

        public static void PlayDarkBuff(Card caster, string effectId = "darkwomb",
                                         string sound = "spell_funnel")
        {
            caster.PlayEffect(effectId);
            caster.PlaySound(sound);
        }

        public static void PlaySummon(Card summoned)
        {
            summoned.PlayEffect("darkwomb3");
            summoned.PlaySound("spell_funnel");
            summoned.pos.Animate(AnimeID.Quake);
        }

        public static void PlayTickDamage(Card target)
        {
            target.PlayEffect("debuff");
        }

        public static void PlayTickHeal(Card target)
        {
            target.PlayEffect("heal_tick");
        }

        public static void PlayAuraZone(Card owner, float radius, Color color, int count = 3)
        {
            var points = EClass._map.ListPointsInCircle(owner.pos, radius);
            if (points == null || points.Count == 0) return;
            int n = Math.Min(count, points.Count);
            for (int i = 0; i < n; i++)
            {
                var p = points[EClass.rnd(points.Count)];
                Effect.Get("smoke").Play(p).SetParticleColor(color);
            }
        }

        public static void PlayDeathZoneCast(Card caster, Point center, int radius)
        {
            if (caster == null || center == null) return;
            caster.PlayEffect("smoke_shockwave");
            PlayRangeRing(center, radius, new Color(0.10f, 0.30f, 0.14f, 0.70f), maxPoints: 16);
            Effect.Get("smoke").Play(center).SetParticleColor(DeathBlack);
        }

        public static void PlayDeathZoneTick(Card owner, Point center, int radius)
        {
            if (owner == null || center == null) return;
            // Keep this subtle on each tick to avoid overdraw.
            PlayRangeRing(center, radius, new Color(0.12f, 0.24f, 0.13f, 0.52f), maxPoints: 8);
            Effect.Get("smoke").Play(center).SetParticleColor(new Color(0.08f, 0.09f, 0.10f, 0.45f));
        }

        public static void PlaySoulTrapMassBurst(Card caster, int radius, List<Chara>? targets)
        {
            if (caster == null) return;

            caster.PlayEffect("telekinesis");
            PlayRangeRing(caster.pos, radius, new Color(0.36f, 0.72f, 0.96f, 0.76f), maxPoints: 22);

            if (targets == null || targets.Count == 0) return;

            int maxLinks = Math.Min(8, targets.Count);
            for (int i = 0; i < maxLinks; i++)
            {
                var target = targets[i];
                if (target?.pos == null) continue;
                Effect.Get("trail1")
                    .SetParticleColor(SoulBlue, true, "_TintColor")
                    .Play(caster.pos, 0f, target.pos);
            }
        }

        private static void PlayRangeRing(Point center, int radius, Color color, int maxPoints)
        {
            if (center == null) return;

            var all = EClass._map.ListPointsInCircle(center, radius);
            if (all == null || all.Count == 0) return;

            var ring = new List<Point>(all.Count);
            int minR = Math.Max(1, radius - 1);
            foreach (var p in all)
            {
                if (p == null) continue;
                int d = center.Distance(p);
                if (d >= minR && d <= radius)
                    ring.Add(p);
            }

            var source = ring.Count > 0 ? ring : all;
            int count = Math.Min(maxPoints, source.Count);
            if (count <= 0) return;

            int step = Math.Max(1, source.Count / count);
            for (int i = 0, emitted = 0; i < source.Count && emitted < count; i += step, emitted++)
            {
                Effect.Get("smoke").Play(source[i]).SetParticleColor(color);
            }
        }

        public static void PlayPlagueSpread(Card target)
        {
            target.PlayEffect("mutation");
            target.PlaySound("vomit");
        }

        public static void PlayBoneAegisCaster(Card caster)
        {
            if (caster == null) return;
            caster.PlayEffect("scream");
            caster.PlaySound("curse3");
            caster.pos.Animate(AnimeID.Quake);
        }

        public static void PlayBoneAegisShield(Card target, float radius = 1.8f, int trailCount = 2)
        {
            if (target == null || target.pos == null || EClass._map == null) return;
            var points = EClass._map.ListPointsInCircle(target.pos, radius);

            var renderer = target.renderer;
            if (renderer != null)
                renderer.PlayAnime(AnimeID.Shiver);

            // Bone-white trails converging from surroundings
            if (points != null && points.Count > 0)
            {
                int n = Math.Min(trailCount, points.Count);
                for (int i = 0; i < n; i++)
                {
                    var p = points[EClass.rnd(points.Count)];
                    if (p == null || p == target.pos) continue;
                    Effect.Get("trail1")
                        .SetParticleColor(BoneWhite, true, "_TintColor")
                        .Play(p, 0f, target.pos);
                }
            }

            // Bone dust mist at target
            Effect.Get("smoke").Play(target.pos).SetParticleColor(BoneWhite);
        }

        public static void PlayShadowTentacleAura(Card owner, float radius = 1.8f, int tendrilCount = 3)
        {
            if (owner == null) return;
            var points = EClass._map.ListPointsInCircle(owner.pos, radius);
            if (points == null || points.Count == 0) return;

            // Core pulse on the body.
            owner.PlayEffect("darkwomb3");

            int n = Math.Min(tendrilCount, points.Count);
            for (int i = 0; i < n; i++)
            {
                var p = points[EClass.rnd(points.Count)];
                if (p == null || p == owner.pos) continue;

                // Shadow tendril: flow from nearby shadow tile to servant center.
                Effect.Get("trail1")
                    .SetParticleColor(ShadowTentacle, true, "_TintColor")
                    .Play(p, 0f, owner.pos);

                // Soft dark mist around the start point.
                Effect.Get("smoke").Play(p).SetParticleColor(ShadowMist);
            }
        }
    }
}
