using System.Linq;
using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class ServantRuntimeTrackingTests
    {
        [Test]
        public void GetRecoverableTemporaryServantUids_RecoversOnlyArsOwnedLiveTemporarySummons()
        {
            var candidates = new[]
            {
                Candidate(101, ServantRuntimeLocation.CurrentMap),
                Candidate(102, ServantRuntimeLocation.CarryoverMap),
                Candidate(103, ServantRuntimeLocation.CurrentMap, hasUndeadServantPresence: false),
                Candidate(104, ServantRuntimeLocation.CurrentMap, hasPcMasterUid: false, hasResolvedPcMaster: false, isPcFactionOrMinion: false),
                Candidate(105, ServantRuntimeLocation.CurrentMap, isSummon: false, summonDuration: 0),
                Candidate(106, ServantRuntimeLocation.Global),
                Candidate(101, ServantRuntimeLocation.CarryoverMap),
            };

            var recovered = ServantRuntimeTracking.GetRecoverableTemporaryServantUids(candidates).ToArray();

            Assert.That(recovered, Is.EqualTo(new[] { 101, 102 }));
        }

        [Test]
        public void ShouldRecoverTemporaryServant_RejectsDestroyedDeadExpiredAndGlobalCandidates()
        {
            Assert.That(ServantRuntimeTracking.ShouldRecoverTemporaryServant(
                Candidate(201, ServantRuntimeLocation.CurrentMap, isDestroyed: true)), Is.False);
            Assert.That(ServantRuntimeTracking.ShouldRecoverTemporaryServant(
                Candidate(202, ServantRuntimeLocation.CurrentMap, isDead: true)), Is.False);
            Assert.That(ServantRuntimeTracking.ShouldRecoverTemporaryServant(
                Candidate(203, ServantRuntimeLocation.CurrentMap, isSummon: true, summonDuration: 0)), Is.False);
            Assert.That(ServantRuntimeTracking.ShouldRecoverTemporaryServant(
                Candidate(204, ServantRuntimeLocation.CurrentMap, isGlobal: true)), Is.False);
        }

        [Test]
        public void ShouldUntrackTemporaryServant_UntracksOnlyTrackedArsTemporarySummonsThatEnded()
        {
            Assert.That(ServantRuntimeTracking.ShouldUntrackTemporaryServant(
                Candidate(301, ServantRuntimeLocation.CurrentMap, isDestroyed: true),
                isTracked: true), Is.True);
            Assert.That(ServantRuntimeTracking.ShouldUntrackTemporaryServant(
                Candidate(302, ServantRuntimeLocation.CurrentMap, isDead: true),
                isTracked: true), Is.True);
            Assert.That(ServantRuntimeTracking.ShouldUntrackTemporaryServant(
                Candidate(303, ServantRuntimeLocation.CurrentMap, isSummon: true, summonDuration: 0),
                isTracked: true), Is.True);

            Assert.That(ServantRuntimeTracking.ShouldUntrackTemporaryServant(
                Candidate(304, ServantRuntimeLocation.CurrentMap, isSummon: false, summonDuration: 0),
                isTracked: true), Is.False);
            Assert.That(ServantRuntimeTracking.ShouldUntrackTemporaryServant(
                Candidate(305, ServantRuntimeLocation.CurrentMap, isDestroyed: true, hasUndeadServantPresence: false),
                isTracked: true), Is.False);
            Assert.That(ServantRuntimeTracking.ShouldUntrackTemporaryServant(
                Candidate(306, ServantRuntimeLocation.CurrentMap, isDestroyed: true),
                isTracked: false), Is.False);
        }

        private static ServantRuntimeSnapshot Candidate(
            int uid,
            ServantRuntimeLocation location,
            bool isDestroyed = false,
            bool isDead = false,
            bool isGlobal = false,
            bool isSummon = true,
            int summonDuration = 10,
            bool hasUndeadServantPresence = true,
            bool hasUndeadServantTraitMarker = false,
            bool isPcFactionOrMinion = true,
            bool hasPcMasterUid = true,
            bool hasResolvedPcMaster = false)
        {
            return new ServantRuntimeSnapshot(
                uid,
                location,
                isDestroyed,
                isDead,
                isGlobal,
                isSummon,
                summonDuration,
                hasUndeadServantPresence,
                hasUndeadServantTraitMarker,
                isPcFactionOrMinion,
                hasPcMasterUid,
                hasResolvedPcMaster);
        }
    }
}
