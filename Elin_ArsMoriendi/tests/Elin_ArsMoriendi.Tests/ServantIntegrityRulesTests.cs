using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class ServantIntegrityRulesTests
    {
        [Test]
        public void ShouldOfferButcherAction_NormalHomeMember_AllowsButcher()
        {
            bool result = ServantIntegrityRules.ShouldOfferButcherAction(
                isPcFaction: true,
                isPc: false,
                hasHost: false,
                isServant: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldOfferButcherAction_Servant_BlocksButcher()
        {
            bool result = ServantIntegrityRules.ShouldOfferButcherAction(
                isPcFaction: true,
                isPc: false,
                hasHost: false,
                isServant: true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldOfferButcherAction_Player_BlocksButcher()
        {
            bool result = ServantIntegrityRules.ShouldOfferButcherAction(
                isPcFaction: true,
                isPc: true,
                hasHost: false,
                isServant: false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldKeepTrackedServant_NormalOwnerState_KeepsTracked()
        {
            bool result = ServantIntegrityRules.ShouldKeepTrackedServant(
                isDestroyed: false,
                isPcFactionOrMinion: true,
                hasPcMasterUid: false,
                hasResolvedPcMaster: false,
                hasLocalOrCarryoverRuntimeRecord: false,
                isInReserve: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldKeepTrackedServant_DeadOffMapMasterUidFallback_KeepsTracked()
        {
            bool result = ServantIntegrityRules.ShouldKeepTrackedServant(
                isDestroyed: false,
                isPcFactionOrMinion: false,
                hasPcMasterUid: true,
                hasResolvedPcMaster: false,
                hasLocalOrCarryoverRuntimeRecord: false,
                isInReserve: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldKeepTrackedServant_ResolvedMasterFallback_KeepsTracked()
        {
            bool result = ServantIntegrityRules.ShouldKeepTrackedServant(
                isDestroyed: false,
                isPcFactionOrMinion: false,
                hasPcMasterUid: false,
                hasResolvedPcMaster: true,
                hasLocalOrCarryoverRuntimeRecord: false,
                isInReserve: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldKeepTrackedServant_HearthReserve_KeepsTracked()
        {
            bool result = ServantIntegrityRules.ShouldKeepTrackedServant(
                isDestroyed: false,
                isPcFactionOrMinion: false,
                hasPcMasterUid: false,
                hasResolvedPcMaster: false,
                hasLocalOrCarryoverRuntimeRecord: false,
                isInReserve: true);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldKeepTrackedServant_TemporarySummonLocalOrCarryover_KeepsTracked()
        {
            bool result = ServantIntegrityRules.ShouldKeepTrackedServant(
                isDestroyed: false,
                isPcFactionOrMinion: false,
                hasPcMasterUid: false,
                hasResolvedPcMaster: false,
                hasLocalOrCarryoverRuntimeRecord: true,
                isInReserve: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldKeepTrackedServant_UnownedCard_DropsTracked()
        {
            bool result = ServantIntegrityRules.ShouldKeepTrackedServant(
                isDestroyed: false,
                isPcFactionOrMinion: false,
                hasPcMasterUid: false,
                hasResolvedPcMaster: false,
                hasLocalOrCarryoverRuntimeRecord: false,
                isInReserve: false);

            Assert.That(result, Is.False);
        }

        [TestCase(false, ExpectedResult = true)]
        [TestCase(true, ExpectedResult = false)]
        public bool ShouldPersistServantRecord_DistinguishesPermanentFromTemporary(bool isTemporarySummon)
        {
            return ServantIntegrityRules.ShouldPersistServantRecord(isTemporarySummon);
        }

        [Test]
        public void ShouldPurgeBrokenServantRemnant_UntrackedTraitCarrier_Purges()
        {
            var snapshot = new ServantIntegrityRules.RemnantSnapshot(
                isTrackedServant: false,
                isDestroyed: false,
                hasUndeadServantTrait: true,
                hasUndeadServantPresence: false,
                isInReserve: false);

            Assert.That(ServantIntegrityRules.ShouldPurgeBrokenServantRemnant(snapshot), Is.True);
        }

        [Test]
        public void ShouldPurgeBrokenServantRemnant_UntrackedPresenceCarrier_Purges()
        {
            var snapshot = new ServantIntegrityRules.RemnantSnapshot(
                isTrackedServant: false,
                isDestroyed: false,
                hasUndeadServantTrait: false,
                hasUndeadServantPresence: true,
                isInReserve: false);

            Assert.That(ServantIntegrityRules.ShouldPurgeBrokenServantRemnant(snapshot), Is.True);
        }

        [Test]
        public void ShouldPurgeBrokenServantRemnant_TrackedServant_DoesNotPurge()
        {
            var snapshot = new ServantIntegrityRules.RemnantSnapshot(
                isTrackedServant: true,
                isDestroyed: false,
                hasUndeadServantTrait: true,
                hasUndeadServantPresence: true,
                isInReserve: false);

            Assert.That(ServantIntegrityRules.ShouldPurgeBrokenServantRemnant(snapshot), Is.False);
        }

        [Test]
        public void ShouldPurgeBrokenServantRemnant_DestroyedCard_DoesNotPurge()
        {
            var snapshot = new ServantIntegrityRules.RemnantSnapshot(
                isTrackedServant: false,
                isDestroyed: true,
                hasUndeadServantTrait: true,
                hasUndeadServantPresence: true,
                isInReserve: false);

            Assert.That(ServantIntegrityRules.ShouldPurgeBrokenServantRemnant(snapshot), Is.False);
        }

        [Test]
        public void ShouldPurgeBrokenServantRemnant_HearthReserve_DoesNotPurge()
        {
            var snapshot = new ServantIntegrityRules.RemnantSnapshot(
                isTrackedServant: false,
                isDestroyed: false,
                hasUndeadServantTrait: true,
                hasUndeadServantPresence: true,
                isInReserve: true);

            Assert.That(ServantIntegrityRules.ShouldPurgeBrokenServantRemnant(snapshot), Is.False);
        }

        [Test]
        public void ShouldPurgeBrokenServantRemnant_UntrackedWithoutMarkers_DoesNotPurge()
        {
            var snapshot = new ServantIntegrityRules.RemnantSnapshot(
                isTrackedServant: false,
                isDestroyed: false,
                hasUndeadServantTrait: false,
                hasUndeadServantPresence: false,
                isInReserve: false);

            Assert.That(ServantIntegrityRules.ShouldPurgeBrokenServantRemnant(snapshot), Is.False);
        }
    }
}
