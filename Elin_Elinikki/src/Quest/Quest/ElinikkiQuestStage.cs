namespace Elin_Elinikki.Quest.Quest
{
    /// <summary>
    /// Main quest progression stages for "帰らなかった遠足". Values are persisted
    /// as integers via <see cref="QuestStateService.SetCurrentPhase(int)"/> under
    /// the flag key <c>chitsii.elinikki.quest.current_phase</c>. The int values
    /// MUST stay stable — saved games depend on them — so new stages should be
    /// appended at the end rather than inserted.
    ///
    /// Matches the Quest Stage Transitions table in
    /// <c>story/chapters/_index.md</c>.
    /// </summary>
    public enum ElinikkiQuestStage
    {
        /// <summary>Quest has not been accepted. Default initial state.</summary>
        NotStarted = 0,

        /// <summary>Mina has delivered the request; party formed. Intro drama complete.</summary>
        Accepted = 1,

        /// <summary>Player has cleared the Waterstone layer (chapter 1).</summary>
        Layer1Clear = 2,

        /// <summary>Player has cleared the Echo layer (chapter 2).</summary>
        Layer2Clear = 3,

        /// <summary>Player has cleared the Bloom layer (chapter 3).</summary>
        Layer3Clear = 4,

        /// <summary>Player has found Yuu at the campsite (chapter 4 reunion drama complete).</summary>
        YuuFound = 5,

        /// <summary>Player has returned to the Nefia entrance with Yuu (chapter 5 start).</summary>
        Returned = 6,

        /// <summary>Ending drama has played. Quest is effectively complete.</summary>
        EndingSeen = 7,
    }

    /// <summary>
    /// Convenience helpers for converting between <see cref="ElinikkiQuestStage"/>
    /// and the raw integer stored in player dialogFlags. Keep this tiny — the
    /// actual transition logic lives in <c>ElinikkiQuestFlow</c> (Task 1.4).
    ///
    /// IMPORTANT: These helpers write to a dedicated <c>quest.stage</c> key, NOT
    /// to <c>quest.current_phase</c>. The latter is reserved for the lower-level
    /// <c>QuestStateMachine</c> phase (<c>Bootstrap</c>/<c>Intro</c>/…), while
    /// <c>quest.stage</c> is the chapter-level progression matching
    /// <c>story/chapters/_index.md</c>. The two must stay decoupled.
    /// </summary>
    public static class ElinikkiQuestStageExtensions
    {
        /// <summary>
        /// Local (unprefixed) flag key for the chapter stage. The full key is
        /// constructed via <see cref="QuestStateService.BuildFlagKey(string)"/>,
        /// producing <c>chitsii.elinikki.quest.stage</c>.
        /// </summary>
        public const string StageLocalKey = "quest.stage";

        /// <summary>
        /// Full dialogFlags key. Resolved lazily each call so that any runtime
        /// change to the default prefix via
        /// <see cref="QuestStateService.SetDefaultPrefix(string)"/> is honored.
        /// </summary>
        public static string StageFlagKey => QuestStateService.BuildFlagKey(StageLocalKey);

        /// <summary>
        /// Reads the current stage from the dedicated <see cref="StageFlagKey"/>.
        /// Values outside the defined enum range fall back to
        /// <see cref="ElinikkiQuestStage.NotStarted"/>.
        /// </summary>
        public static ElinikkiQuestStage GetCurrentStage()
        {
            int raw = QuestStateService.GetFlagInt(StageFlagKey, 0);
            if (raw < (int)ElinikkiQuestStage.NotStarted || raw > (int)ElinikkiQuestStage.EndingSeen)
            {
                return ElinikkiQuestStage.NotStarted;
            }

            return (ElinikkiQuestStage)raw;
        }

        /// <summary>
        /// Persists the given stage under the dedicated <see cref="StageFlagKey"/>.
        /// Does nothing if the requested stage is not strictly greater than the
        /// current stage — the flow is forward-only.
        /// </summary>
        public static void AdvanceToStage(ElinikkiQuestStage stage)
        {
            ElinikkiQuestStage current = GetCurrentStage();
            if ((int)stage <= (int)current)
            {
                return;
            }

            QuestStateService.SetFlagInt(StageFlagKey, (int)stage);
        }

        /// <summary>
        /// True when <paramref name="stage"/> is strictly later in the flow than
        /// <see cref="GetCurrentStage"/>. Used by transition guards that should
        /// never move the quest backwards.
        /// </summary>
        public static bool IsAheadOfCurrent(this ElinikkiQuestStage stage)
        {
            return (int)stage > (int)GetCurrentStage();
        }
    }
}
