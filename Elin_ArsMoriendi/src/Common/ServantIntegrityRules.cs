namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Pure safety rules for servant lifecycle edge cases.
    /// Kept game-type free so unit tests can pin the intended behavior.
    /// </summary>
    public static class ServantIntegrityRules
    {
        public readonly struct RemnantSnapshot
        {
            public RemnantSnapshot(
                bool isTrackedServant,
                bool isDestroyed,
                bool hasUndeadServantTrait,
                bool hasUndeadServantPresence)
            {
                IsTrackedServant = isTrackedServant;
                IsDestroyed = isDestroyed;
                HasUndeadServantTrait = hasUndeadServantTrait;
                HasUndeadServantPresence = hasUndeadServantPresence;
            }

            public bool IsTrackedServant { get; }

            public bool IsDestroyed { get; }

            public bool HasUndeadServantTrait { get; }

            public bool HasUndeadServantPresence { get; }
        }

        public static bool ShouldOfferButcherAction(
            bool isPcFaction,
            bool isPc,
            bool hasHost,
            bool isServant)
        {
            if (!isPcFaction || isPc || hasHost)
                return false;

            return !isServant;
        }

        public static bool ShouldKeepTrackedServant(
            bool isDestroyed,
            bool isPcFactionOrMinion,
            bool hasPcMasterUid,
            bool hasResolvedPcMaster)
        {
            if (isDestroyed)
                return false;

            return isPcFactionOrMinion || hasPcMasterUid || hasResolvedPcMaster;
        }

        public static bool ShouldPurgeBrokenServantRemnant(RemnantSnapshot snapshot)
        {
            if (snapshot.IsDestroyed || snapshot.IsTrackedServant)
                return false;

            return snapshot.HasUndeadServantTrait || snapshot.HasUndeadServantPresence;
        }
    }
}
