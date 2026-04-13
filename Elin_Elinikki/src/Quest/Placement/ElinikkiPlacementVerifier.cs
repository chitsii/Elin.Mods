using System;
using System.Collections.Generic;
using Elin_Elinikki.Quest.DramaKeys;
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
                VerifyAudio(result);
                VerifyEndingDecisionTable(result);
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
        /// Task 4.4 audio consistency pass. In the absence of the
        /// Phase 5 devmode maps a live in-game audio verify is not
        /// possible, so this static pass asserts the structure the
        /// zone placement manager will feed into the audio bridge:
        /// <list type="bullet">
        /// <item><description>Every chapter layer that the story
        /// spec calls out as needing a BGM override has an entry in
        /// <see cref="ElinikkiBgmMap"/>.</description></item>
        /// <item><description>The yuu_camp entry is the silent
        /// sentinel, matching the story spec's 演出なし requirement.
        /// </description></item>
        /// <item><description>nefia_entrance has no entry so it
        /// falls through to Elin's normal scene playlist in chapters
        /// 0 and 5.</description></item>
        /// <item><description>No BGM id collisions across chapter
        /// layers — each chapter keeps its own musical identity.
        /// </description></item>
        /// </list>
        /// Values that fail are logged and counted but do not throw.
        /// Task 6.1 playthrough still has to verify the actual
        /// SoundManager asset ids resolve in-game.
        /// </summary>
        private static void VerifyAudio(VerifyResult result)
        {
            // Chapter layers that MUST have a map entry. yuu_camp
            // is expected to be SilentSentinel; the three real
            // layers must be non-null, non-empty, and not the
            // sentinel.
            string[] bgmRequired =
            {
                ElinikkiZoneIds.LayerWaterstone,
                ElinikkiZoneIds.LayerEcho,
                ElinikkiZoneIds.LayerBloom,
            };

            System.Collections.Generic.HashSet<string> seenBgmIds =
                new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < bgmRequired.Length; i++)
            {
                string zoneId = bgmRequired[i];
                string bgmId = ElinikkiBgmMap.GetBgmId(zoneId);
                if (string.IsNullOrEmpty(bgmId))
                {
                    result.ErrorCount++;
                    QuestModLog.Error(
                        "Missing BGM map entry for chapter zone " + zoneId);
                    continue;
                }

                if (string.Equals(
                        bgmId,
                        ElinikkiBgmMap.SilentSentinel,
                        StringComparison.Ordinal))
                {
                    result.ErrorCount++;
                    QuestModLog.Error(
                        "Chapter zone " + zoneId +
                        " is mapped to SilentSentinel but needs a real BGM track");
                    continue;
                }

                if (!seenBgmIds.Add(bgmId))
                {
                    // Same BGM id reused across chapters is not a
                    // hard error — the story might deliberately
                    // share a track — but warn so the conflict is
                    // obvious during tuning.
                    result.WarningCount++;
                    QuestModLog.Warn(
                        "Chapter zone " + zoneId +
                        " reuses BGM id " + bgmId +
                        " already mapped to an earlier chapter");
                }
            }

            // yuu_camp must exist AND be the silent sentinel.
            string yuuCampId = ElinikkiBgmMap.GetBgmId(ElinikkiZoneIds.YuuCamp);
            if (string.IsNullOrEmpty(yuuCampId))
            {
                result.ErrorCount++;
                QuestModLog.Error(
                    "Missing BGM map entry for yuu_camp (expected SilentSentinel)");
            }
            else if (!string.Equals(
                         yuuCampId,
                         ElinikkiBgmMap.SilentSentinel,
                         StringComparison.Ordinal))
            {
                result.ErrorCount++;
                QuestModLog.Error(
                    "yuu_camp should be mapped to SilentSentinel but has " + yuuCampId);
            }

            // nefia_entrance must NOT have an entry — it falls
            // through to Elin's normal scene playlist. A non-null
            // value here would override the shared chapter 0/5
            // entrance map and bleed Elinikki audio into vanilla
            // play, so this is an Error (not just a Warn): a
            // future regression that adds an entry here must fail
            // Verify() loud enough to surface in ErrorCount-based
            // callers and CI checks.
            string entranceId = ElinikkiBgmMap.GetBgmId(ElinikkiZoneIds.NefiaEntrance);
            if (!string.IsNullOrEmpty(entranceId))
            {
                result.ErrorCount++;
                QuestModLog.Error(
                    "NefiaEntrance unexpectedly has a BGM map entry (" + entranceId +
                    "); the shared chapter 0/5 entrance should use Elin's normal playlist");
            }
        }

        /// <summary>
        /// Task 6.1/6.3 ending-resolver consistency check. Exercises
        /// <see cref="ElinikkiEndingResolver.ResolveEndingFromTruthCount"/>
        /// against the decision table spelled out in
        /// <c>story/chapters/_index.md</c> so a future refactor of
        /// that method gets caught at mod load instead of waiting
        /// for the Phase 6 live playthrough.
        ///
        /// <para>The table the story spec demands:</para>
        /// <list type="bullet">
        /// <item><description><c>count == 0</c> → Silence (nothing
        /// heard)</description></item>
        /// <item><description><c>1 ≤ count ≤ 7</c> → Silence
        /// (partial-knowledge bucket, shares the silence
        /// drama)</description></item>
        /// <item><description><c>count == 8</c> → Return (every
        /// truth heard)</description></item>
        /// </list>
        /// Plus a set of structural invariants:
        /// <list type="bullet">
        /// <item><description><see cref="ElinikkiEndingResolver.TruthFlagKeys"/>
        /// length matches <c>TotalTruthFlags</c>.</description></item>
        /// <item><description>every key in that array starts with
        /// the <c>chitsii.elinikki.quest.event.truth_</c> prefix so a
        /// typo in the source does not silently drop a flag from
        /// the count.</description></item>
        /// <item><description><see cref="FlagKeys.ELINIKKI_QUEST_ENDING"/>
        /// resolves to a non-empty string (generated constant
        /// sanity).</description></item>
        /// </list>
        /// </summary>
        private static void VerifyEndingDecisionTable(VerifyResult result)
        {
            // Structural: the key array length must equal the
            // constant used by the count loop. A mismatch between
            // these two would silently drop a truth flag from
            // counting without any runtime symptom until the
            // decision table misfires at the ending.
            //
            // Both sides are compile-time constants today, so the
            // compiler would flag the equality body as unreachable
            // under CS0162. The whole point of the check is to
            // catch a FUTURE edit that introduces a divergence
            // between them, so suppress the warning locally.
#pragma warning disable CS0162
            if (ElinikkiEndingResolver.TruthFlagKeys.Length
                != ElinikkiEndingResolver.TotalTruthFlags)
            {
                result.ErrorCount++;
                QuestModLog.Error(
                    "TruthFlagKeys length (" +
                    ElinikkiEndingResolver.TruthFlagKeys.Length +
                    ") does not match TotalTruthFlags (" +
                    ElinikkiEndingResolver.TotalTruthFlags + ")");
            }
#pragma warning restore CS0162

            // Structural: every truth flag key must carry the
            // expected prefix. A typo here would read someone
            // else's flag and silently miscount.
            const string truthPrefix = "chitsii.elinikki.quest.event.truth_";
            for (int i = 0; i < ElinikkiEndingResolver.TruthFlagKeys.Length; i++)
            {
                string key = ElinikkiEndingResolver.TruthFlagKeys[i];
                if (string.IsNullOrEmpty(key))
                {
                    result.ErrorCount++;
                    QuestModLog.Error("TruthFlagKeys[" + i + "] is null/empty");
                    continue;
                }
                if (!key.StartsWith(truthPrefix, StringComparison.Ordinal))
                {
                    result.ErrorCount++;
                    QuestModLog.Error(
                        "TruthFlagKeys[" + i + "] missing prefix: " + key);
                }
            }

            // Structural: the quest.ending flag constant must exist
            // and point at the right key. QuestStateService writes
            // through this constant on every ending transition.
            if (string.IsNullOrEmpty(FlagKeys.ELINIKKI_QUEST_ENDING))
            {
                result.ErrorCount++;
                QuestModLog.Error("FlagKeys.ELINIKKI_QUEST_ENDING is null/empty");
            }
            else if (FlagKeys.ELINIKKI_QUEST_ENDING != "chitsii.elinikki.quest.ending")
            {
                result.ErrorCount++;
                QuestModLog.Error(
                    "FlagKeys.ELINIKKI_QUEST_ENDING unexpected value: " +
                    FlagKeys.ELINIKKI_QUEST_ENDING);
            }

            // Behavioural: exercise the decision table for each of
            // the 9 possible truth counts (0 through 8) and assert
            // the expected ending kind. Any future logic change
            // that re-introduces a third ending bucket (1..7) will
            // have to update this table alongside the spec.
            var expected = new (int truthCount, ElinikkiEndingKind ending)[]
            {
                (0, ElinikkiEndingKind.Silence),
                (1, ElinikkiEndingKind.Silence),
                (2, ElinikkiEndingKind.Silence),
                (3, ElinikkiEndingKind.Silence),
                (4, ElinikkiEndingKind.Silence),
                (5, ElinikkiEndingKind.Silence),
                (6, ElinikkiEndingKind.Silence),
                (7, ElinikkiEndingKind.Silence),
                (8, ElinikkiEndingKind.Return),
            };

            for (int i = 0; i < expected.Length; i++)
            {
                var probe = expected[i];
                ElinikkiEndingKind actual =
                    ElinikkiEndingResolver.ResolveEndingFromTruthCount(probe.truthCount);
                if (actual != probe.ending)
                {
                    result.ErrorCount++;
                    QuestModLog.Error(
                        "Ending decision mismatch: truthCount=" + probe.truthCount +
                        " expected=" + probe.ending +
                        " actual=" + actual);
                }
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
