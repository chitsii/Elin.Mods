namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Pure helper for quest advance cooldown checks.
    /// </summary>
    public static class QuestCooldownPolicy
    {
        public const int RawMinutesPerDay = 1440;

        public static int DaysToRawMinutes(int cooldownDays)
        {
            if (cooldownDays <= 0) return 0;
            return cooldownDays * RawMinutesPerDay;
        }

        public static bool ShouldWaitBeforeAdvance(int lastAdvanceRaw, int currentRaw, int thresholdRawMinutes)
        {
            if (thresholdRawMinutes <= 0) return false;
            if (lastAdvanceRaw <= 0) return false;
            int elapsed = currentRaw - lastAdvanceRaw;
            return elapsed < thresholdRawMinutes;
        }

        public static bool ShouldWaitBeforeAdvanceDays(int lastAdvanceRaw, int currentRaw, int cooldownDays)
        {
            int threshold = DaysToRawMinutes(cooldownDays);
            return ShouldWaitBeforeAdvance(lastAdvanceRaw, currentRaw, threshold);
        }
    }
}
