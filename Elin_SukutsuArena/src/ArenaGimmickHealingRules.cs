namespace Elin_SukutsuArena
{
    public static class ArenaGimmickHealingRules
    {
        public static bool TryBlockHealing(bool isArenaBattle, bool isHealingBlocked, ref long amount)
        {
            if (!isArenaBattle || !isHealingBlocked || amount <= 0)
                return false;

            amount = 0;
            return true;
        }
    }
}
