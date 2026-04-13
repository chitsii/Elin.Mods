using Elin_Elinikki.Quest.DramaKeys;

namespace Elin_Elinikki.Quest.Quest
{
    /// <summary>
    /// Ending enumeration matching the story/chapters/_index.md
    /// "quest.ending" table. Values are persisted to dialogFlags
    /// through <see cref="FlagKeys.ELINIKKI_QUEST_ENDING"/>, so
    /// any change here also requires updating the story spec and
    /// the drama scripts that set the flag.
    /// </summary>
    public enum ElinikkiEndingKind
    {
        None = 0,
        Return = 1,
        Silence = 2,
        Revisit = 3,
    }

    /// <summary>
    /// Reads the eight <c>chitsii.elinikki.quest.event.truth_*</c>
    /// flags from the player's dialogFlags, counts them, and picks
    /// which chapter-5 ending should fire. Also owns the resolution
    /// rules for the hidden revisit ending that triggers when the
    /// player re-enters a chapter zone after reaching the return
    /// ending.
    ///
    /// <para>Pure static — no internal state. Tests and the Phase 6
    /// verifier both call <see cref="ResolveEndingFromTruthCount"/>
    /// directly without a game runtime; the game-side hooks in
    /// <see cref="ElinikkiQuestFlow"/> and
    /// <c>Patch_Zone_Activate_QuestPulse</c> read truth counts via
    /// <see cref="CountTruthFlags"/> which falls back to zero when
    /// dialogFlags is unavailable (mod load, headless session).</para>
    /// </summary>
    public static class ElinikkiEndingResolver
    {
        /// <summary>
        /// Ordered list of the eight truth flag keys counted by
        /// <see cref="CountTruthFlags"/>. Kept as a single-source
        /// array so Phase 6 playthrough verification can print the
        /// subset of flags the player actually collected.
        /// </summary>
        public static readonly string[] TruthFlagKeys =
        {
            FlagKeys.ELINIKKI_TRUTH_MARKS,
            FlagKeys.ELINIKKI_TRUTH_CHANNEL,
            FlagKeys.ELINIKKI_TRUTH_STONES,
            FlagKeys.ELINIKKI_TRUTH_ECHO,
            FlagKeys.ELINIKKI_TRUTH_MAP,
            FlagKeys.ELINIKKI_TRUTH_SHADOW,
            FlagKeys.ELINIKKI_TRUTH_FLOWERS,
            FlagKeys.ELINIKKI_TRUTH_WEAVE,
        };

        /// <summary>
        /// Total number of truth flags in the set. Matches
        /// <see cref="TruthFlagKeys"/>'s length; exposed as a
        /// constant for the Task 4.4 / Task 6.2 verifier passes
        /// so an accidental desync between the key array and the
        /// flag count is a compile error, not a runtime drift.
        /// </summary>
        public const int TotalTruthFlags = 8;

        /// <summary>
        /// Counts how many of the eight truth flags are currently
        /// set to a non-zero value in the player's dialogFlags.
        /// Returns 0 if dialogFlags is unavailable.
        /// </summary>
        public static int CountTruthFlags()
        {
            int count = 0;
            for (int i = 0; i < TruthFlagKeys.Length; i++)
            {
                if (QuestStateService.GetFlagInt(TruthFlagKeys[i], 0) != 0)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Pure decision rule: given a truth count, returns the
        /// ending kind that should fire on return to the entrance.
        /// </summary>
        /// <remarks>
        /// Per <c>story/chapters/_index.md</c>:
        /// <list type="bullet">
        /// <item><description><c>count == 8</c> → Return ending —
        /// every truth heard, "全部くだらなかった。たぶん。" beat.</description></item>
        /// <item><description><c>count == 0</c> → Silence ending —
        /// nothing heard, "見たものは、見たままでいい" beat.</description></item>
        /// <item><description><c>1..7</c> → still Silence — the
        /// spec folds partial-knowledge runs into the silence bucket
        /// rather than authoring a third ending drama.</description></item>
        /// </list>
        /// </remarks>
        public static ElinikkiEndingKind ResolveEndingFromTruthCount(int truthCount)
        {
            if (truthCount >= TotalTruthFlags)
            {
                return ElinikkiEndingKind.Return;
            }

            return ElinikkiEndingKind.Silence;
        }

        /// <summary>
        /// Reads the current persisted ending value from
        /// <see cref="FlagKeys.ELINIKKI_QUEST_ENDING"/>. Safe to
        /// call at any time — returns <see cref="ElinikkiEndingKind.None"/>
        /// when the flag is missing.
        /// </summary>
        public static ElinikkiEndingKind GetCurrentEnding()
        {
            int raw = QuestStateService.GetFlagInt(FlagKeys.ELINIKKI_QUEST_ENDING, 0);
            if (raw < (int)ElinikkiEndingKind.None || raw > (int)ElinikkiEndingKind.Revisit)
            {
                return ElinikkiEndingKind.None;
            }
            return (ElinikkiEndingKind)raw;
        }

        /// <summary>
        /// Persists the ending value without firing any drama.
        /// Used by the revisit-detection path to mark the hidden
        /// ending as reached (the revisit itself has no drama, per
        /// story spec).
        /// </summary>
        public static void SetCurrentEnding(ElinikkiEndingKind kind)
        {
            QuestStateService.SetFlagInt(FlagKeys.ELINIKKI_QUEST_ENDING, (int)kind);
        }
    }
}
