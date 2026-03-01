using NUnit.Framework;
using CwlQuestFramework;
using Elin_SukutsuArena.Core;

namespace Elin_SukutsuArena.Tests.CwlQuestFramework
{
    /// <summary>
    /// テスト用のフェーズEnum
    /// </summary>
    public enum TestPhase
    {
        Start = 0,
        Middle = 1,
        End = 2
    }

    [TestFixture]
    public class PhaseManagerTests
    {
        private InMemoryFlagStorage _storage;
        private StandardPhaseManager<TestPhase> _phaseManager;
        private const string PhaseKey = "test_phase";

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryFlagStorage();
            _phaseManager = new StandardPhaseManager<TestPhase>(_storage, PhaseKey);
        }

        [Test]
        public void GetPhaseOrdinal_ReturnsStoredValue()
        {
            _storage.SetInt(PhaseKey, 1);
            Assert.That(_phaseManager.GetPhaseOrdinal(), Is.EqualTo(1));
        }

        [Test]
        public void GetPhaseOrdinal_ReturnsZero_WhenNotSet()
        {
            Assert.That(_phaseManager.GetPhaseOrdinal(), Is.EqualTo(0));
        }

        [Test]
        public void CurrentPhase_ReturnsCorrectEnum()
        {
            _storage.SetInt(PhaseKey, 2);
            Assert.That(_phaseManager.CurrentPhase, Is.EqualTo(TestPhase.End));
        }

        [Test]
        public void CurrentPhase_ReturnsFirstEnum_WhenNotSet()
        {
            Assert.That(_phaseManager.CurrentPhase, Is.EqualTo(TestPhase.Start));
        }

        [Test]
        public void SetPhase_UpdatesStorage()
        {
            _phaseManager.SetPhase(TestPhase.Middle);
            Assert.That(_storage.GetInt(PhaseKey), Is.EqualTo(1));
        }

        [Test]
        public void SetPhase_FiresOnPhaseChanged()
        {
            TestPhase? oldPhase = null;
            TestPhase? newPhase = null;
            int eventCount = 0;

            _phaseManager.OnPhaseChanged += (old, @new) =>
            {
                oldPhase = old;
                newPhase = @new;
                eventCount++;
            };

            _phaseManager.SetPhase(TestPhase.End);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(oldPhase, Is.EqualTo(TestPhase.Start));
            Assert.That(newPhase, Is.EqualTo(TestPhase.End));
        }

        [Test]
        public void SetPhase_DoesNotFireEvent_WhenSamePhase()
        {
            int eventCount = 0;
            _phaseManager.OnPhaseChanged += (_, __) => eventCount++;

            _phaseManager.SetPhase(TestPhase.Start); // Default is Start

            Assert.That(eventCount, Is.EqualTo(0));
        }

        [Test]
        public void SetPhase_FiresMultipleChanges()
        {
            int eventCount = 0;
            _phaseManager.OnPhaseChanged += (_, __) => eventCount++;

            _phaseManager.SetPhase(TestPhase.Middle);
            _phaseManager.SetPhase(TestPhase.End);

            Assert.That(eventCount, Is.EqualTo(2));
        }
    }
}
