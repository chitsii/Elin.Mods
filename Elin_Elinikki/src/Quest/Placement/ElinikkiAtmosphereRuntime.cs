using UnityEngine;

namespace Elin_Elinikki.Quest.Placement
{
    /// <summary>
    /// Static holder for the currently-active Elinikki atmosphere
    /// override. <see cref="ElinikkiZonePlacementManager"/> sets the
    /// override on entry to a chapter zone and clears it on exit,
    /// while <c>FpsGpuPreviewRenderer.AtmospherePass</c> reads it from
    /// its <c>ResolveFogColor</c> / <c>ResolveClearColor</c> hooks to
    /// blend the per-zone tint on top of Elin's scene profile output.
    ///
    /// <para>Kept as a plain static so every system that needs to
    /// consult the override (renderer, diagnostics, self-test runner)
    /// can do so without plumbing a reference through the call chain.
    /// There is only ever one "current zone" at a time, so a single
    /// field is sufficient. Thread safety is not a concern because
    /// both producers (Harmony postfix on the main thread) and
    /// consumers (renderer on the main thread) run on Unity's main
    /// thread.</para>
    /// </summary>
    public static class ElinikkiAtmosphereRuntime
    {
        private static ElinikkiAtmosphereProfile _current;

        /// <summary>
        /// Returns the active override profile, or <c>null</c> when no
        /// Elinikki zone is currently loaded. Callers that want to
        /// read fields from the profile should null-check first.
        /// </summary>
        internal static ElinikkiAtmosphereProfile Current => _current;

        /// <summary>
        /// Swaps in the profile for <paramref name="zoneId"/>, or
        /// clears the override when the zone has no registered profile
        /// (see <see cref="ElinikkiAtmosphereData.GetProfile"/>).
        /// Called by the zone-placement manager on every zone
        /// activation.
        /// </summary>
        public static void SetZone(string zoneId)
        {
            _current = ElinikkiAtmosphereData.GetProfile(zoneId);
        }

        /// <summary>
        /// Drops the active override. Called by the placement manager
        /// when the new zone is not Elinikki-owned or when the zone id
        /// could not be resolved.
        /// </summary>
        public static void Clear()
        {
            _current = null;
        }

        /// <summary>
        /// Blends <paramref name="sceneColor"/> toward the active
        /// override's fog color by that profile's fog strength. When
        /// no override is active the scene color passes through
        /// untouched. Used by the renderer's fog resolution path.
        /// </summary>
        internal static Color BlendFogColor(Color sceneColor)
        {
            ElinikkiAtmosphereProfile profile = _current;
            if (profile == null || profile.FogColorStrength <= 0.001f)
            {
                return sceneColor;
            }

            Color blended = Color.Lerp(sceneColor, profile.FogColor, Mathf.Clamp01(profile.FogColorStrength));
            blended.a = sceneColor.a;
            return blended;
        }

        /// <summary>
        /// Blends <paramref name="sceneColor"/> toward the active
        /// override's clear/sky color. Used by the renderer's clear
        /// resolution path so the player's distant haze horizon picks
        /// up the chapter's tint.
        /// </summary>
        internal static Color BlendClearColor(Color sceneColor)
        {
            ElinikkiAtmosphereProfile profile = _current;
            if (profile == null || profile.ClearColorStrength <= 0.001f)
            {
                return sceneColor;
            }

            Color blended = Color.Lerp(sceneColor, profile.ClearColor, Mathf.Clamp01(profile.ClearColorStrength));
            blended.a = sceneColor.a;
            return blended;
        }

        /// <summary>
        /// Applies the active LUT tint to <paramref name="color"/>.
        /// The tint is a multiply-and-lerp: a full-strength tint of
        /// (0.9, 1.0, 1.1) multiplies RGB and then blends back toward
        /// the input at (1 - strength), giving a mild color cast
        /// rather than a hard replacement.
        /// </summary>
        internal static Color ApplyLutTint(Color color)
        {
            ElinikkiAtmosphereProfile profile = _current;
            if (profile == null || profile.LutStrength <= 0.001f)
            {
                return color;
            }

            Color tinted = new Color(
                color.r * profile.LutTint.r,
                color.g * profile.LutTint.g,
                color.b * profile.LutTint.b,
                color.a);
            Color blended = Color.Lerp(color, tinted, Mathf.Clamp01(profile.LutStrength));
            blended.a = color.a;
            return blended;
        }
    }
}
