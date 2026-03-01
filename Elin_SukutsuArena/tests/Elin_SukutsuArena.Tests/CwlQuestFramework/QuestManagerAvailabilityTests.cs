using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CwlQuestFramework;
using Elin_SukutsuArena.Core;

namespace Elin_SukutsuArena.Tests.CwlQuestFramework
{
    [TestFixture]
    public class QuestManagerAvailabilityTests
    {
        private InMemoryFlagStorage _storage;
        private FlagStorageAdapter _flagSetter;
        private StandardPhaseManager<TestPhase> _phaseManager;
        private const string QuestCompletedPrefix = "quest_done_";
        private const string PhaseKey = "test_phase";

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryFlagStorage();
            _flagSetter = new FlagStorageAdapter(_storage, new TestEnumMappingProvider());
            _phaseManager = new StandardPhaseManager<TestPhase>(_storage, PhaseKey);
        }

        // === GetAvailableQuests filtering tests ===

        [Test]
        public void GetAvailableQuests_FiltersCompleted()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start),
                CreateQuest("quest_2", TestPhase.Start)
            };
            var manager = CreateManager(quests);

            // Complete quest_1
            _storage.SetInt(QuestCompletedPrefix + "quest_1", 1);

            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(1));
            Assert.That(available[0].QuestId, Is.EqualTo("quest_2"));
        }

        [Test]
        public void GetAvailableQuests_FiltersPhase()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_start", TestPhase.Start),
                CreateQuest("quest_middle", TestPhase.Middle),
                CreateQuest("quest_end", TestPhase.End)
            };
            var manager = CreateManager(quests);

            // Phase is Start by default
            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(1));
            Assert.That(available[0].QuestId, Is.EqualTo("quest_start"));
        }

        [Test]
        public void GetAvailableQuests_IncludesEarlierPhases()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_start", TestPhase.Start),
                CreateQuest("quest_middle", TestPhase.Middle)
            };
            var manager = CreateManager(quests);

            // Set phase to Middle
            _phaseManager.SetPhase(TestPhase.Middle);

            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAvailableQuests_FiltersRequiredQuests()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start),
                CreateQuest("quest_2", TestPhase.Start, requiredQuests: new List<string> { "quest_1" })
            };
            var manager = CreateManager(quests);

            // quest_1 not completed yet
            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(1));
            Assert.That(available[0].QuestId, Is.EqualTo("quest_1"));
        }

        [Test]
        public void GetAvailableQuests_IncludesQuest_WhenRequiredQuestsCompleted()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start),
                CreateQuest("quest_2", TestPhase.Start, requiredQuests: new List<string> { "quest_1" })
            };
            var manager = CreateManager(quests);

            // Complete quest_1
            _storage.SetInt(QuestCompletedPrefix + "quest_1", 1);

            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(1));
            Assert.That(available[0].QuestId, Is.EqualTo("quest_2"));
        }

        [Test]
        public void GetAvailableQuests_FiltersRequiredFlags()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start),
                CreateQuest("quest_2", TestPhase.Start,
                    requiredFlags: new List<IFlagCondition>
                    {
                        new TestFlagCondition("flag_a", "==", 1)
                    })
            };
            var manager = CreateManager(quests);

            // flag_a not set yet
            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(1));
            Assert.That(available[0].QuestId, Is.EqualTo("quest_1"));
        }

        [Test]
        public void GetAvailableQuests_IncludesQuest_WhenRequiredFlagsMet()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start),
                CreateQuest("quest_2", TestPhase.Start,
                    requiredFlags: new List<IFlagCondition>
                    {
                        new TestFlagCondition("flag_a", "==", 1)
                    })
            };
            var manager = CreateManager(quests);

            // Set flag_a
            _storage.SetInt("flag_a", 1);

            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAvailableQuests_FiltersBlocked()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_blocker", TestPhase.Start, blocksQuests: new List<string> { "quest_blocked" }),
                CreateQuest("quest_blocked", TestPhase.Start)
            };
            var manager = CreateManager(quests);

            // Complete quest_blocker
            _storage.SetInt(QuestCompletedPrefix + "quest_blocker", 1);

            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetAvailableQuests_IncludesBlocked_WhenBlockerNotCompleted()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_blocker", TestPhase.Start, blocksQuests: new List<string> { "quest_blocked" }),
                CreateQuest("quest_blocked", TestPhase.Start)
            };
            var manager = CreateManager(quests);

            // Blocker not completed
            var available = manager.GetAvailableQuests();

            Assert.That(available.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAvailableQuests_SortsByPriority()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_low", TestPhase.Start, priority: 1),
                CreateQuest("quest_high", TestPhase.Start, priority: 100),
                CreateQuest("quest_mid", TestPhase.Start, priority: 50)
            };
            var manager = CreateManager(quests);

            var available = manager.GetAvailableQuests();

            Assert.That(available[0].QuestId, Is.EqualTo("quest_high"));
            Assert.That(available[1].QuestId, Is.EqualTo("quest_mid"));
            Assert.That(available[2].QuestId, Is.EqualTo("quest_low"));
        }

        // === GetQuestsForNpc tests ===

        [Test]
        public void GetQuestsForNpc_ReturnsOnlyNpcsQuests()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_npc1", TestPhase.Start, questGiver: "npc_1"),
                CreateQuest("quest_npc2", TestPhase.Start, questGiver: "npc_2"),
                CreateQuest("quest_auto", TestPhase.Start, autoTrigger: true)
            };
            var manager = CreateManager(quests);

            var npc1Quests = manager.GetQuestsForNpc("npc_1");

            Assert.That(npc1Quests.Count, Is.EqualTo(1));
            Assert.That(npc1Quests[0].QuestId, Is.EqualTo("quest_npc1"));
        }

        // === GetAutoTriggerQuests tests ===

        [Test]
        public void GetAutoTriggerQuests_ReturnsOnlyAutoTrigger()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_manual", TestPhase.Start),
                CreateQuest("quest_auto", TestPhase.Start, autoTrigger: true)
            };
            var manager = CreateManager(quests);

            var autoQuests = manager.GetAutoTriggerQuests();

            Assert.That(autoQuests.Count, Is.EqualTo(1));
            Assert.That(autoQuests[0].QuestId, Is.EqualTo("quest_auto"));
        }

        // === GetNpcsWithQuests tests ===

        [Test]
        public void GetNpcsWithQuests_ReturnsDistinctNpcIds()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start, questGiver: "npc_1"),
                CreateQuest("quest_2", TestPhase.Start, questGiver: "npc_1"),
                CreateQuest("quest_3", TestPhase.Start, questGiver: "npc_2")
            };
            var manager = CreateManager(quests);

            var npcs = manager.GetNpcsWithQuests();

            Assert.That(npcs.Count, Is.EqualTo(2));
            Assert.That(npcs, Contains.Item("npc_1"));
            Assert.That(npcs, Contains.Item("npc_2"));
        }

        // === IsQuestAvailable tests ===

        [Test]
        public void IsQuestAvailable_ReturnsTrue_WhenAvailable()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start)
            };
            var manager = CreateManager(quests);

            Assert.That(manager.IsQuestAvailable("quest_1"), Is.True);
        }

        [Test]
        public void IsQuestAvailable_ReturnsFalse_WhenCompleted()
        {
            var quests = new List<IQuestDefinition>
            {
                CreateQuest("quest_1", TestPhase.Start)
            };
            var manager = CreateManager(quests);

            _storage.SetInt(QuestCompletedPrefix + "quest_1", 1);

            Assert.That(manager.IsQuestAvailable("quest_1"), Is.False);
        }

        [Test]
        public void IsQuestAvailable_ReturnsFalse_WhenQuestNotFound()
        {
            var quests = new List<IQuestDefinition>();
            var manager = CreateManager(quests);

            Assert.That(manager.IsQuestAvailable("nonexistent"), Is.False);
        }

        // === Helper methods ===

        private QuestManager<TestPhase> CreateManager(List<IQuestDefinition> quests)
        {
            return new QuestManager<TestPhase>(
                _flagSetter,
                _phaseManager,
                quests,
                QuestCompletedPrefix);
        }

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

        private class TestFlagCondition : IFlagCondition
        {
            public string FlagKey { get; }
            public string Operator { get; }
            public object Value { get; }

            public TestFlagCondition(string flagKey, string op, object value)
            {
                FlagKey = flagKey;
                Operator = op;
                Value = value;
            }
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
