using System.Collections.Generic;

namespace Elin_ArsMoriendi
{
    public enum ServantRuntimeLocation
    {
        Missing,
        Global,
        CurrentMap,
        CarryoverMap,
    }

    public readonly struct ServantRuntimeSnapshot
    {
        public readonly int Uid;
        public readonly ServantRuntimeLocation Location;
        public readonly bool IsDestroyed;
        public readonly bool IsDead;
        public readonly bool IsGlobal;
        public readonly bool IsSummon;
        public readonly int SummonDuration;
        public readonly bool HasUndeadServantPresence;
        public readonly bool HasUndeadServantTraitMarker;
        public readonly bool IsPcFactionOrMinion;
        public readonly bool HasPcMasterUid;
        public readonly bool HasResolvedPcMaster;

        public ServantRuntimeSnapshot(
            int uid,
            ServantRuntimeLocation location,
            bool isDestroyed,
            bool isDead,
            bool isGlobal,
            bool isSummon,
            int summonDuration,
            bool hasUndeadServantPresence,
            bool hasUndeadServantTraitMarker,
            bool isPcFactionOrMinion,
            bool hasPcMasterUid,
            bool hasResolvedPcMaster)
        {
            Uid = uid;
            Location = location;
            IsDestroyed = isDestroyed;
            IsDead = isDead;
            IsGlobal = isGlobal;
            IsSummon = isSummon;
            SummonDuration = summonDuration;
            HasUndeadServantPresence = hasUndeadServantPresence;
            HasUndeadServantTraitMarker = hasUndeadServantTraitMarker;
            IsPcFactionOrMinion = isPcFactionOrMinion;
            HasPcMasterUid = hasPcMasterUid;
            HasResolvedPcMaster = hasResolvedPcMaster;
        }

        public bool HasArsServantMarker => HasUndeadServantPresence || HasUndeadServantTraitMarker;
        public bool HasPcOwnership => IsPcFactionOrMinion || HasPcMasterUid || HasResolvedPcMaster;
        public bool IsLiveTemporarySummon => !IsDestroyed && !IsDead && !IsGlobal && IsSummon && SummonDuration > 0;
        public bool IsTemporarySummonLike => !IsGlobal && (IsSummon || SummonDuration > 0);
    }

    public static class ServantRuntimeTracking
    {
        public static IEnumerable<int> GetRecoverableTemporaryServantUids(
            IEnumerable<ServantRuntimeSnapshot> candidates)
        {
            var seen = new HashSet<int>();
            foreach (var candidate in candidates)
            {
                if (!ShouldRecoverTemporaryServant(candidate)) continue;
                if (seen.Add(candidate.Uid))
                    yield return candidate.Uid;
            }
        }

        public static bool ShouldRecoverTemporaryServant(ServantRuntimeSnapshot snapshot)
        {
            if (snapshot.Location != ServantRuntimeLocation.CurrentMap
                && snapshot.Location != ServantRuntimeLocation.CarryoverMap)
                return false;

            return snapshot.Uid > 0
                && snapshot.IsLiveTemporarySummon
                && snapshot.HasArsServantMarker
                && snapshot.HasPcOwnership;
        }

        public static bool ShouldUntrackTemporaryServant(
            ServantRuntimeSnapshot snapshot,
            bool isTracked)
        {
            if (!isTracked) return false;
            if (snapshot.Uid <= 0) return false;
            if (!snapshot.HasArsServantMarker) return false;
            if (!snapshot.IsTemporarySummonLike) return false;

            return snapshot.IsDestroyed || snapshot.IsDead || snapshot.SummonDuration <= 0;
        }
    }
}
