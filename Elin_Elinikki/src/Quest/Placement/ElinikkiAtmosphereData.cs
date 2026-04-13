using System.Collections.Generic;
using Elin_Elinikki.Quest.Quest;
using UnityEngine;

namespace Elin_Elinikki.Quest.Placement
{
    /// <summary>
    /// Per-zone atmosphere profile applied by
    /// <see cref="ElinikkiAtmosphereRuntime"/> on top of Elin's scene
    /// profile output. Each profile describes a fog tint, a clear/sky
    /// tint, and a simple tint grade. Blend strengths are 0..1; zero
    /// means "let the scene profile through unchanged".
    ///
    /// <para>Values are distilled from the "見た目（プレイヤーの知覚）"
    /// sections of <c>story/worldbuilding/locations/*.md</c>. They are
    /// kept in code rather than parsed from the markdown files so
    /// every tuning change flows through the normal build + codex
    /// review loop instead of freeform text edits. Task 3.6 is expected
    /// to tune these against in-game screenshots.</para>
    /// </summary>
    internal sealed class ElinikkiAtmosphereProfile
    {
        /// <summary>Content id of the zone this profile applies to.</summary>
        public string ZoneId;

        /// <summary>Blend target for <see cref="FpsGpuPreviewRenderer"/>'s fog color.</summary>
        public Color FogColor;

        /// <summary>Fraction [0,1] of the fog color to blend in. 0 = disabled.</summary>
        public float FogColorStrength;

        /// <summary>Blend target for the camera clear/sky color.</summary>
        public Color ClearColor;

        /// <summary>Fraction [0,1] of the clear color to blend in. 0 = disabled.</summary>
        public float ClearColorStrength;

        /// <summary>
        /// Scene-tone tint multiplier. Applied as a multiply-and-lerp
        /// on top of the final shaded color so an RGB of (0.9, 1.0, 1.1)
        /// with strength 0.3 gives a subtle cool cast.
        /// </summary>
        public Color LutTint;

        /// <summary>Blend fraction [0,1] for <see cref="LutTint"/>. 0 = disabled.</summary>
        public float LutStrength;
    }

    internal static class ElinikkiAtmosphereData
    {
        /// <summary>
        /// Static profile table keyed by zone content id. Zones not
        /// in the map return <c>null</c> from <see cref="GetProfile"/>
        /// and the renderer falls back to the raw scene profile. The
        /// Nefia entrance and Yuu's camp deliberately have no entries:
        /// the story spec describes both as "演出なし" ordinary caves.
        /// </summary>
        private static readonly Dictionary<string, ElinikkiAtmosphereProfile> Profiles
            = new Dictionary<string, ElinikkiAtmosphereProfile>(System.StringComparer.Ordinal)
            {
                // layer_waterstone: 水と苔の洞窟。薄く青い光が充満。
                // 青緑系fogと青系LUT、やや重めの霧。
                [ElinikkiZoneIds.LayerWaterstone] = new ElinikkiAtmosphereProfile
                {
                    ZoneId = ElinikkiZoneIds.LayerWaterstone,
                    FogColor = new Color(0.42f, 0.58f, 0.70f, 1f),
                    FogColorStrength = 0.55f,
                    ClearColor = new Color(0.18f, 0.26f, 0.34f, 1f),
                    ClearColorStrength = 0.55f,
                    LutTint = new Color(0.92f, 1.02f, 1.12f, 1f),
                    LutStrength = 0.35f,
                },

                // layer_echo: 巨大な地下空洞。暗い。焚き火の点光源。
                // ほぼ黒に近い低明度、焚き火由来のわずかな暖色残光。
                [ElinikkiZoneIds.LayerEcho] = new ElinikkiAtmosphereProfile
                {
                    ZoneId = ElinikkiZoneIds.LayerEcho,
                    FogColor = new Color(0.12f, 0.11f, 0.14f, 1f),
                    FogColorStrength = 0.70f,
                    ClearColor = new Color(0.06f, 0.05f, 0.08f, 1f),
                    ClearColorStrength = 0.70f,
                    LutTint = new Color(1.04f, 0.95f, 0.90f, 1f),
                    LutStrength = 0.25f,
                },

                // layer_bloom: 発光する花畑。橙と紫。暖かく甘い空気。
                // 暖色fog、発光感、最小の霧で花の光を引き立てる。
                [ElinikkiZoneIds.LayerBloom] = new ElinikkiAtmosphereProfile
                {
                    ZoneId = ElinikkiZoneIds.LayerBloom,
                    FogColor = new Color(0.88f, 0.58f, 0.70f, 1f),
                    FogColorStrength = 0.45f,
                    ClearColor = new Color(0.36f, 0.22f, 0.40f, 1f),
                    ClearColorStrength = 0.55f,
                    LutTint = new Color(1.08f, 0.96f, 1.06f, 1f),
                    LutStrength = 0.40f,
                },

                // yuu_camp: 演出が全て消える。fog/LUT/色調補正なし。
                // nefia_entrance: 通常のネフィア入口。演出なし。
                // どちらも profile 未登録 → 強制的に scene profile のまま。
            };

        /// <summary>
        /// Returns the Elinikki atmosphere override for
        /// <paramref name="zoneId"/>, or <c>null</c> when the zone has
        /// no override and the scene profile should pass through
        /// unchanged.
        /// </summary>
        public static ElinikkiAtmosphereProfile GetProfile(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                return null;
            }

            return Profiles.TryGetValue(zoneId, out ElinikkiAtmosphereProfile profile)
                ? profile
                : null;
        }
    }
}
