using System;
using System.Collections.Generic;
using Elin_Elinikki.Quest.Quest;

namespace Elin_Elinikki.Quest.Placement
{
    /// <summary>
    /// Static consistency checks for the Elinikki placement and
    /// atmosphere tables. Runs at mod load so any data-layer
    /// regressions (missing prefix, duplicate id, out-of-bounds
    /// coordinate, missing atmosphere profile) are caught before the
    /// player enters a chapter zone — well before Phase 5 creates
    /// the real devmode maps and live FPS verification becomes
    /// possible.
    ///
    /// <para>Task 3.6 replaces the originally-planned "verify FPS
    /// visual output" pass while the devmode maps are blocked on
    /// user action. The static verifier does not replace live
    /// verification (that still has to happen in-game once the
    /// maps land), but it catches the subset of regressions that
    /// do not require a running game and gives the loop a concrete
    /// pass/fail signal for Phase 3 completion.</para>
    ///
    /// <para>Fail-soft: any inconsistency is logged through
    /// <see cref="QuestModLog"/> at Warn/Error level and returned in
    /// the result struct; the method never throws so it is safe to
    /// call from <c>QuestBootstrap.Initialize</c>.</para>
    /// </summary>
    internal static class ElinikkiPlacementVerifier
    {
        /// <summary>
        /// Inclusive lower bound for tile coordinates in the
        /// provisional layout. Phase 5's real devmode maps will set
        /// their own size; for now the verifier assumes a ~40x40
        /// map and rejects coordinates too close to the edge.
        /// </summary>
        private const float MinTileCoordinate = 2f;

        /// <summary>Inclusive upper bound for tile coordinates.</summary>
        private const float MaxTileCoordinate = 38f;

        /// <summary>
        /// Elinikki zones that must have at least one placement.
        /// The Nefia entrance is excluded by design (no 3D traces
        /// in chapter 0 / 5 — the map is the default entrance).
        /// </summary>
        private static readonly string[] PlacementRequiredZones =
        {
            ElinikkiZoneIds.LayerWaterstone,
            ElinikkiZoneIds.LayerEcho,
            ElinikkiZoneIds.LayerBloom,
            ElinikkiZoneIds.YuuCamp,
        };

        /// <summary>
        /// Elinikki zones that must have a registered atmosphere
        /// profile. yuu_camp and nefia_entrance are intentionally
        /// omitted because the story spec describes both as
        /// "演出なし" ordinary caves.
        /// </summary>
        private static readonly string[] AtmosphereRequiredZones =
        {
            ElinikkiZoneIds.LayerWaterstone,
            ElinikkiZoneIds.LayerEcho,
            ElinikkiZoneIds.LayerBloom,
        };

        /// <summary>
        /// Returns true if every static check passes. Logs any
        /// findings along the way. Counts of errors and warnings
        /// are exposed through <see cref="LastResult"/> for tools
        /// or later retry logic.
        /// </summary>
        public static bool Verify()
        {
            VerifyResult result = new VerifyResult();

            try
            {
                VerifyPlacements(result);
                VerifyAtmosphere(result);
                VerifyPlaceholderTextureConstructs(result);
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                QuestModLog.Error("Placement verifier crashed: " + ex.Message);
            }

            LastResult = result;

            string summary =
                "Placement verify finished. errors=" + result.ErrorCount +
                " warnings=" + result.WarningCount +
                " definitions=" + result.DefinitionCount;
            if (result.ErrorCount > 0)
            {
                QuestModLog.Error(summary);
            }
            else if (result.WarningCount > 0)
            {
                QuestModLog.Warn(summary);
            }
            else
            {
                QuestModLog.Info(summary);
            }

            return result.ErrorCount == 0;
        }

        /// <summary>
        /// Last verification result. Null before <see cref="Verify"/>
        /// is called.
        /// </summary>
        public static VerifyResult LastResult { get; private set; }

        private static void VerifyPlacements(VerifyResult result)
        {
            HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < PlacementRequiredZones.Length; i++)
            {
                string zoneId = PlacementRequiredZones[i];
                int countForZone = 0;

                foreach (SharedWorldPrimitiveDefinition definition
                         in ElinikkiPlacementData.GetDefinitionsForZone(zoneId))
                {
                    if (definition == null)
                    {
                        result.ErrorCount++;
                        QuestModLog.Error(
                            "Null definition returned for zone " + zoneId);
                        continue;
                    }

                    result.DefinitionCount++;
                    countForZone++;

                    if (string.IsNullOrEmpty(definition.Id))
                    {
                        result.ErrorCount++;
                        QuestModLog.Error(
                            "Definition with empty id in zone " + zoneId);
                        continue;
                    }

                    if (!definition.Id.StartsWith(
                            ElinikkiZonePlacementManager.PlacementPrefix,
                            StringComparison.Ordinal))
                    {
                        result.ErrorCount++;
                        QuestModLog.Error(
                            "Definition id missing prefix: " + definition.Id);
                    }

                    if (!seenIds.Add(definition.Id))
                    {
                        result.ErrorCount++;
                        QuestModLog.Error(
                            "Duplicate definition id across zones: " + definition.Id);
                    }

                    if (definition.TilePosition.x < MinTileCoordinate
                        || definition.TilePosition.x > MaxTileCoordinate
                        || definition.TilePosition.z < MinTileCoordinate
                        || definition.TilePosition.z > MaxTileCoordinate)
                    {
                        result.WarningCount++;
                        QuestModLog.Warn(
                            "Definition out of provisional tile bounds: " +
                            definition.Id + " pos=" + definition.TilePosition);
                    }

                    if (definition.Scale.x <= 0f
                        || definition.Scale.y <= 0f
                        || definition.Scale.z <= 0f)
                    {
                        result.ErrorCount++;
                        QuestModLog.Error(
                            "Definition has non-positive scale: " +
                            definition.Id + " scale=" + definition.Scale);
                    }
                }

                if (countForZone == 0)
                {
                    result.ErrorCount++;
                    QuestModLog.Error(
                        "Zone " + zoneId + " has no placement definitions");
                }
            }
        }

        private static void VerifyAtmosphere(VerifyResult result)
        {
            for (int i = 0; i < AtmosphereRequiredZones.Length; i++)
            {
                string zoneId = AtmosphereRequiredZones[i];
                ElinikkiAtmosphereProfile profile =
                    ElinikkiAtmosphereData.GetProfile(zoneId);
                if (profile == null)
                {
                    result.ErrorCount++;
                    QuestModLog.Error(
                        "Missing atmosphere profile for zone " + zoneId);
                }
            }

            // yuu_camp and nefia_entrance are expected to return null.
            if (ElinikkiAtmosphereData.GetProfile(ElinikkiZoneIds.YuuCamp) != null)
            {
                result.WarningCount++;
                QuestModLog.Warn(
                    "YuuCamp has an atmosphere profile — story spec says 演出なし");
            }

            if (ElinikkiAtmosphereData.GetProfile(ElinikkiZoneIds.NefiaEntrance) != null)
            {
                result.WarningCount++;
                QuestModLog.Warn(
                    "NefiaEntrance has an atmosphere profile — ordinary cave expected");
            }
        }

        private static void VerifyPlaceholderTextureConstructs(VerifyResult result)
        {
            // Pure construction check: asking the texture factory
            // for the neutral bitmap forces the SetPixel loop to
            // run once so any null-ref or out-of-memory regression
            // shows up as an Error now instead of the first time
            // a player walks into a chapter zone.
            UnityEngine.Texture2D texture =
                ElinikkiPlaceholderTextures.GetOrCreateNeutral();
            if (texture == null)
            {
                result.ErrorCount++;
                QuestModLog.Error("Placeholder texture factory returned null");
                return;
            }

            if (texture.width <= 0 || texture.height <= 0)
            {
                result.ErrorCount++;
                QuestModLog.Error(
                    "Placeholder texture has invalid dimensions: " +
                    texture.width + "x" + texture.height);
            }
        }

        /// <summary>
        /// Pass/warn/error summary returned by <see cref="Verify"/>.
        /// </summary>
        public sealed class VerifyResult
        {
            public int ErrorCount;
            public int WarningCount;
            public int DefinitionCount;
        }
    }
}
