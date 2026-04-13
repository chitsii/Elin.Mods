using System;
using System.Collections.Generic;
using Elin_Elinikki.Quest.Quest;

namespace Elin_Elinikki.Quest.Placement
{
    /// <summary>
    /// Per-zone BGM mapping consumed by
    /// <see cref="ElinikkiZonePlacementManager"/>. The map pairs each
    /// Elinikki chapter zone with the Elin BGM asset id it should
    /// play on entry. Zones that should deliberately go silent have
    /// <see cref="SilentSentinel"/> as their id; zones that are not
    /// in the map let the scene's default zone playlist through.
    ///
    /// <para>Ids are strings so they can be looked up through
    /// <c>SoundManager.current.GetData(id)</c>, which matches what
    /// the existing drama DSL's <c>play_bgm</c> helper uses for mod
    /// BGM assets. Vanilla Elin BGMs are usually referenced by
    /// integer id through <c>EMono.core.refs.dictBGM</c>, but the
    /// string lookup also covers them provided the asset is loaded
    /// in the <c>Media/Sound/BGM/</c> Resources path.</para>
    ///
    /// <para><b>Task 4.2 status:</b> the ids below are provisional
    /// placeholders picked to match the story spec's mood per layer.
    /// Phase 6 playthrough (Task 6.1) will verify each id against the
    /// live game and swap in the actual vanilla Elin asset if the
    /// placeholder does not resolve. <c>GameQuestDramaRuntimeContext.PlayBgm</c>
    /// already logs a Warn with the missing id, so failed lookups
    /// fail loud without breaking the zone transition.</para>
    /// </summary>
    internal static class ElinikkiBgmMap
    {
        /// <summary>
        /// Sentinel string for "deliberate silence". When the mapping
        /// returns this value the placement manager calls
        /// <c>StopBgm</c> and does not start a replacement track.
        /// Matches the story spec's 演出なし marker for yuu_camp.
        /// </summary>
        public const string SilentSentinel = "__elinikki_silent__";

        private static readonly Dictionary<string, string> Map
            = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // layer_waterstone: 水と苔の洞窟、薄く青い光、水音。
                // Intent: quiet exploration bed with water/cavern
                // ambience. Provisional: BGM/Cave_Quiet_Exploration.
                // Phase 6 playthrough will swap in the real vanilla
                // cave track if this id does not resolve.
                [ElinikkiZoneIds.LayerWaterstone] = "BGM/Cave_Quiet_Exploration",

                // layer_echo: 巨大な地下空洞、ほぼ暗闇、焚き火の残光。
                // Intent: slow dark-cavern drone under the handclap
                // beats. Provisional: BGM/Dungeon_Dark_Echo.
                [ElinikkiZoneIds.LayerEcho] = "BGM/Dungeon_Dark_Echo",

                // layer_bloom: 発光する花畑、橙と紫、ゆめにっき的ピーク。
                // Intent: dreamy warm-major cue that the reunion beat
                // tears down again. Provisional: BGM/Bloom_Warm_Dream.
                [ElinikkiZoneIds.LayerBloom] = "BGM/Bloom_Warm_Dream",

                // yuu_camp: 演出なし。焚き火のパチパチ音と水滴だけ。
                // Explicit silence — the drop-off from the bloom
                // layer's BGM to zero is the story spec's core
                // atmosphere trick here, so we stop audio rather
                // than fall through to whatever the scene playlist
                // would start.
                [ElinikkiZoneIds.YuuCamp] = SilentSentinel,

                // nefia_entrance: not in the map — the entrance is
                // an ordinary map shared with chapters 0 and 5 and
                // should use Elin's normal playlist.
            };

        /// <summary>
        /// Returns the BGM asset id mapped to <paramref name="zoneId"/>,
        /// or null when the zone has no override. A return of
        /// <see cref="SilentSentinel"/> means "stop BGM, do not start
        /// a replacement".
        /// </summary>
        public static string GetBgmId(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                return null;
            }

            return Map.TryGetValue(zoneId, out string id) ? id : null;
        }
    }
}
