using System;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using CwlQuestFramework;
using Elin_SukutsuArena.Core;

namespace Elin_SukutsuArena.Tests.CwlQuestFramework
{
    [TestFixture]
    public class QuestManagerCoreTests
    {
        private InMemoryFlagStorage _storage;
        private FlagStorageAdapter _flagSetter;
        private StandardPhaseManager<TestPhase> _phaseManager;
        private QuestManager<TestPhase> _questManager;
        private List<IQuestDefinition> _quests;
        private const string QuestCompletedPrefix = "quest_done_";
        private const string PhaseKey = "test_phase";

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryFlagStorage();
            _flagSetter = new FlagStorageAdapter(_storage, new TestEnumMappingProvider());
            _phaseManager = new StandardPhaseManager<TestPhase>(_storage, PhaseKey);

            _quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start, advancesPhase: null),
                CreateQuest("quest_2", TestPhase.Start, advancesPhase: TestPhase.Middle,
                    completionFlags: new Dictionary<string, object> { ["flag_a"] = 1 }),
                CreateQuest("quest_3", TestPhase.Middle, advancesPhase: null)
            };

            _questManager = new QuestManager<TestPhase>(
                _flagSetter,
                _phaseManager,
                _quests,
                QuestCompletedPrefix);
        }

        // === IsQuestCompleted tests ===

        [Test]
        public void IsQuestCompleted_ReturnsFalse_WhenNotCompleted()
        {
            Assert.That(_questManager.IsQuestCompleted("quest_1"), Is.False);
        }

        [Test]
        public void IsQuestCompleted_ReturnsTrue_WhenCompleted()
        {
            _storage.SetInt(QuestCompletedPrefix + "quest_1", 1);
            Assert.That(_questManager.IsQuestCompleted("quest_1"), Is.True);
        }

        [Test]
        public void IsQuestCompleted_ReturnsTrue_AfterCompleteQuest()
        {
            _questManager.CompleteQuest("quest_1");
            Assert.That(_questManager.IsQuestCompleted("quest_1"), Is.True);
        }

        // === CompleteQuest tests ===

        [Test]
        public void CompleteQuest_SetsCompletionFlag()
        {
            _questManager.CompleteQuest("quest_1");
            Assert.That(_storage.GetInt(QuestCompletedPrefix + "quest_1"), Is.EqualTo(1));
        }

        [Test]
        public void CompleteQuest_AppliesCompletionFlags()
        {
            // quest_2 has completionFlags: { "flag_a": 1 }
            _questManager.CompleteQuest("quest_2");
            Assert.That(_storage.GetInt("flag_a"), Is.EqualTo(1));
        }

        [Test]
        public void CompleteQuest_AdvancesPhase_WhenConfigured()
        {
            // quest_2 advances to Middle
            Assert.That(_phaseManager.CurrentPhase, Is.EqualTo(TestPhase.Start));
            _questManager.CompleteQuest("quest_2");
            Assert.That(_phaseManager.CurrentPhase, Is.EqualTo(TestPhase.Middle));
        }

        [Test]
        public void CompleteQuest_DoesNotAdvancePhase_WhenNotConfigured()
        {
            // quest_1 does not advance phase
            _questManager.CompleteQuest("quest_1");
            Assert.That(_phaseManager.CurrentPhase, Is.EqualTo(TestPhase.Start));
        }

        [Test]
        public void CompleteQuest_NotifiesObservers()
        {
            var observer = new Mock<IQuestStateObserver>();
            _questManager.AddObserver(observer.Object);

            _questManager.CompleteQuest("quest_1");

            observer.Verify(o => o.OnQuestCompleted("quest_1", It.IsAny<IQuestDefinition>()), Times.Once);
        }

        [Test]
        public void CompleteQuest_NotifiesObservers_OnPhaseAdvance()
        {
            var observer = new Mock<IQuestStateObserver>();
            _questManager.AddObserver(observer.Object);

            _questManager.CompleteQuest("quest_2");

            observer.Verify(o => o.OnPhaseAdvanced(0, 1), Times.Once);
        }

        [Test]
        public void CompleteQuest_NotifiesObservers_OnFlagSet()
        {
            var observer = new Mock<IQuestStateObserver>();
            _questManager.AddObserver(observer.Object);

            _questManager.CompleteQuest("quest_2");

            observer.Verify(o => o.OnFlagSet("flag_a", 1), Times.Once);
        }

        [Test]
        public void CompleteQuest_FiresOnQuestStateChanged()
        {
            int eventCount = 0;
            _questManager.OnQuestStateChanged += () => eventCount++;

            _questManager.CompleteQuest("quest_1");

            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void CompleteQuest_DoesNothing_WhenAlreadyCompleted()
        {
            _questManager.CompleteQuest("quest_1");

            var observer = new Mock<IQuestStateObserver>();
            _questManager.AddObserver(observer.Object);

            int eventCount = 0;
            _questManager.OnQuestStateChanged += () => eventCount++;

            // Complete again
            _questManager.CompleteQuest("quest_1");

            // Should not fire again
            Assert.That(eventCount, Is.EqualTo(0));
            observer.Verify(o => o.OnQuestCompleted(It.IsAny<string>(), It.IsAny<IQuestDefinition>()), Times.Never);
        }

        [Test]
        public void CompleteQuest_DoesNothing_WhenQuestNotFound()
        {
            int eventCount = 0;
            _questManager.OnQuestStateChanged += () => eventCount++;

            _questManager.CompleteQuest("nonexistent_quest");

            Assert.That(eventCount, Is.EqualTo(0));
        }

        // === Observer management ===

        [Test]
        public void RemoveObserver_StopsNotifications()
        {
            var observer = new Mock<IQuestStateObserver>();
            _questManager.AddObserver(observer.Object);
            _questManager.RemoveObserver(observer.Object);

            _questManager.CompleteQuest("quest_1");

            observer.Verify(o => o.OnQuestCompleted(It.IsAny<string>(), It.IsAny<IQuestDefinition>()), Times.Never);
        }

        // === Helper methods ===

        private static TestQuestDefinition CreateQuest(
            string questId,
            TestPhase phase,
            TestPhase? advancesPhase = null,
            Dictionary<string, object> completionFlags = null,
            List<string> requiredQuests = null,
            List<IFlagCondition> requiredFlags = null,
            List<string> blocksQuests = null,
            string questGiver = null,
            bool autoTrigger = false,
            int priority = 0)
        {
            return new TestQuestDefinition
            {
                QuestId = questId,
                Phase = (int)phase,
                AdvancesPhase = advancesPhase.HasValue ? (int?)advancesPhase.Value : null,
                CompletionFlags = completionFlags != null
                    ? new Dictionary<string, object>(completionFlags)
                    : new Dictionary<string, object>(),
                RequiredQuests = requiredQuests ?? new List<string>(),
                RequiredFlags = requiredFlags ?? new List<IFlagCondition>(),
                BlocksQuests = blocksQuests ?? new List<string>(),
                QuestGiver = questGiver,
                AutoTrigger = autoTrigger,
                Priority = priority
            };
        }

        private class TestQuestDefinition : IQuestDefinition
        {
            public string QuestId { get; set; }
            public string QuestType { get; set; } = "test";
            public string DramaId { get; set; } = "drama";
            public string DisplayNameJP { get; set; } = "テスト";
            public string DisplayNameEN { get; set; } = "Test";
            public string Description { get; set; } = "desc";
            public int Phase { get; set; }
            public string QuestGiver { get; set; }
            public bool AutoTrigger { get; set; }
            public int? AdvancesPhase { get; set; }
            public IReadOnlyList<IFlagCondition> RequiredFlags { get; set; } = new List<IFlagCondition>();
            public IReadOnlyList<string> RequiredQuests { get; set; } = new List<string>();
            public IReadOnlyDictionary<string, object> CompletionFlags { get; set; } = new Dictionary<string, object>();
            public IReadOnlyList<string> BlocksQuests { get; set; } = new List<string>();
            public int Priority { get; set; }
        }

        private class TestEnumMappingProvider : IEnumMappingProvider
        {
            public bool TryGetMapping(string flagKey, out IDictionary<string, int> mapping)
            {
                mapping = null;
                return false;
            }
        }
    }
}
