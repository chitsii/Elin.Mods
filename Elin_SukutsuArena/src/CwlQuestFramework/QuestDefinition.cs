using System.Collections.Generic;

namespace CwlQuestFramework
{
    /// <summary>
    /// CWL汎用クエスト定義クラス
    /// IQuestDefinitionの標準実装
    /// </summary>
    public class GenericQuestDefinition : IQuestDefinition
    {
        public string QuestId { get; set; }
        public string QuestType { get; set; }
        public string DramaId { get; set; }
        public string DisplayNameJP { get; set; }
        public string DisplayNameEN { get; set; }
        public string Description { get; set; }
        public int Phase { get; set; }
        public string QuestGiver { get; set; }
        public bool AutoTrigger { get; set; }
        public int? AdvancesPhase { get; set; }
        public int Priority { get; set; }

        private List<IFlagCondition> _requiredFlags = new List<IFlagCondition>();
        private List<string> _requiredQuests = new List<string>();
        private Dictionary<string, object> _completionFlags = new Dictionary<string, object>();
        private List<string> _blocksQuests = new List<string>();

        public IReadOnlyList<IFlagCondition> RequiredFlags => _requiredFlags;
        public IReadOnlyList<string> RequiredQuests => _requiredQuests;
        public IReadOnlyDictionary<string, object> CompletionFlags => _completionFlags;
        public IReadOnlyList<string> BlocksQuests => _blocksQuests;

        public GenericQuestDefinition() { }

        public GenericQuestDefinition(string questId)
        {
            QuestId = questId;
        }

        public void AddRequiredFlag(IFlagCondition condition)
        {
            _requiredFlags.Add(condition);
        }

        public void AddRequiredQuest(string questId)
        {
            _requiredQuests.Add(questId);
        }

        public void AddCompletionFlag(string key, object value)
        {
            _completionFlags[key] = value;
        }

        public void AddBlocksQuest(string questId)
        {
            _blocksQuests.Add(questId);
        }

        public void SetRequiredFlags(List<IFlagCondition> flags)
        {
            _requiredFlags = flags ?? new List<IFlagCondition>();
        }

        public void SetRequiredQuests(List<string> quests)
        {
            _requiredQuests = quests ?? new List<string>();
        }

        public void SetCompletionFlags(Dictionary<string, object> flags)
        {
            _completionFlags = flags ?? new Dictionary<string, object>();
        }

        public void SetBlocksQuests(List<string> quests)
        {
            _blocksQuests = quests ?? new List<string>();
        }
    }
}
