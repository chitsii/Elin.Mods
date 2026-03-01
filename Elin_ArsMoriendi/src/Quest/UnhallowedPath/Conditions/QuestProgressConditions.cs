namespace Elin_ArsMoriendi
{
    internal static class QuestProgressConditions
    {
        public static bool IsCooldownElapsed(UnhallowedPath quest, int cooldownDays = 1)
        {
            return !quest.ShouldWaitBeforeAdvance(cooldownDays);
        }

        public static bool IsCurrentStage(UnhallowedPath quest, UnhallowedStage stage)
        {
            return quest.CurrentStage == stage;
        }

        public static bool HasKnightsSpawned(UnhallowedPath quest)
        {
            return quest.KnightsSpawned;
        }

        public static bool HasKarenJournal(UnhallowedPath quest)
        {
            return quest.KarenJournalSpawned;
        }

        public static bool HasMinimumServants(int count)
        {
            return NecromancyManager.Instance.ServantCount >= count;
        }

        public static bool HasErenosDefeated(UnhallowedPath quest)
        {
            return quest.ErenosDefeated;
        }

        public static bool HasApotheosisApplied(UnhallowedPath quest)
        {
            return quest.ApotheosisApplied;
        }
    }
}
