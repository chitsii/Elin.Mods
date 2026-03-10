namespace Elin_LogRefined
{
    public static class RuntimeGuard
    {
        public static bool IsGameplayReady()
        {
            try
            {
                return EClass.core != null &&
                       EClass.core.IsGameStarted &&
                       EClass.game != null &&
                       EClass.pc != null &&
                       EClass.sources != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanInspectCard(Card card)
        {
            if (card == null || !IsGameplayReady())
                return false;

            try
            {
                return card.IsPC || card.IsPCFaction || EClass.pc.CanSee(card);
            }
            catch
            {
                return false;
            }
        }
    }
}
