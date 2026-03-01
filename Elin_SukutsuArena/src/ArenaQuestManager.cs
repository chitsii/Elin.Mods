using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Flags;
using Elin_SukutsuArena.Generated;
using CwlQuestFramework;
using StoryPhase = Elin_SukutsuArena.Quests.StoryPhase;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// クエスト管理クラス
    /// QuestManager<StoryPhase>のラッパー（Mod固有処理を追加）
    /// </summary>
    public class ArenaQuestManager : IQuestStateObserver
    {
        private static ArenaQuestManager _instance;
        public static ArenaQuestManager Instance => _instance ?? (_instance = new ArenaQuestManager());

        private QuestManager<StoryPhase> _questManager;
        private List<QuestDefinition> _allQuests = new List<QuestDefinition>();

        private const string QuestDefinitionsPath = "Package/quest_definitions.json";
        private static string QuestCompletedFlagPrefix => ArenaFlagKeys.QuestDonePrefix;
        private static string CurrentPhaseFlagKey => ArenaFlagKeys.CurrentPhase;

        /// <summary>
        /// NPCクエスト更新イベント（マーカー管理用）
        /// </summary>
        public event Action OnQuestStateChanged;

        /// <summary>
        /// 全クエスト定義を取得
        /// </summary>
        public IReadOnlyList<QuestDefinition> GetAllQuests() => _allQuests;

        private ArenaQuestManager()
        {
            LoadQuestDefinitions();
        }

        /// <summary>
        /// quest_definitions.jsonを読み込み、QuestManagerを初期化
        /// </summary>
        private void LoadQuestDefinitions()
        {
            try
            {
                var modPath = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                var jsonPath = Path.Combine(modPath, QuestDefinitionsPath);

                if (!File.Exists(jsonPath))
                {
                    Debug.LogError($"[ArenaQuest] Quest definitions not found: {jsonPath}");
                    InitializeEmptyQuestManager();
                    return;
                }

                var json = File.ReadAllText(jsonPath);
                var questDataList = JsonConvert.DeserializeObject<List<QuestDataDto>>(json);

                _allQuests.Clear();
                foreach (var data in questDataList)
                {
                    _allQuests.Add(new QuestDefinition(data));
                }

                InitializeQuestManager();

                ModLog.Log($"[ArenaQuest] Loaded {_allQuests.Count} quest definitions");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArenaQuest] Failed to load quest definitions: {ex}");
                InitializeEmptyQuestManager();
            }
        }

        /// <summary>
        /// QuestManagerを初期化
        /// </summary>
        private void InitializeQuestManager()
        {
            var enumMappingProvider = new ArenaEnumMappingAdapter();
            var flagSetter = new FlagStorageAdapter(ArenaContext.I.Storage, enumMappingProvider);
            var phaseManager = new StandardPhaseManager<StoryPhase>(ArenaContext.I.Storage, CurrentPhaseFlagKey);

            // QuestDefinition → IQuestDefinition 変換
            var quests = _allQuests.Select(q => (IQuestDefinition)new QuestDefinitionAdapter(q)).ToList();

            _questManager = new QuestManager<StoryPhase>(flagSetter, phaseManager, quests, QuestCompletedFlagPrefix);
            _questManager.AddObserver(this);
            _questManager.OnQuestStateChanged += () => OnQuestStateChanged?.Invoke();
        }

        /// <summary>
        /// 空のQuestManagerを初期化（エラー時）
        /// </summary>
        private void InitializeEmptyQuestManager()
        {
            var enumMappingProvider = new ArenaEnumMappingAdapter();
            var flagSetter = new FlagStorageAdapter(ArenaContext.I.Storage, enumMappingProvider);
            var phaseManager = new StandardPhaseManager<StoryPhase>(ArenaContext.I.Storage, CurrentPhaseFlagKey);

            _questManager = new QuestManager<StoryPhase>(flagSetter, phaseManager, new List<IQuestDefinition>(), QuestCompletedFlagPrefix);
            _questManager.AddObserver(this);
        }

        // === IQuestStateObserver implementation ===

        void IQuestStateObserver.OnQuestCompleted(string questId, IQuestDefinition quest)
        {
            ModLog.Log($"[ArenaQuest] Quest completed: {questId}");

            // 次のクエストが自動開始できるようにトラッキングをリセット
            ArenaZonePatches.ResetQuestTriggerTracking();

            // 現在のゾーンがアリーナの場合、NPC可視性を即時更新
            if (EClass._zone is Zone_SukutsuArena arenaZone)
            {
                ModLog.Log($"[ArenaQuest] Updating NPC visibility after quest completion");
                arenaZone.HandleNpcVisibility();
            }
        }

        void IQuestStateObserver.OnPhaseAdvanced(int oldPhase, int newPhase)
        {
            ModLog.Log($"[ArenaQuest] Phase advanced: {(StoryPhase)oldPhase} -> {(StoryPhase)newPhase}");
        }

        void IQuestStateObserver.OnFlagSet(string key, object value)
        {
            ModLog.Log($"[ArenaQuest] Flag set: {key} = {value}");
        }

        // === Public API (backward compatible) ===

        /// <summary>
        /// クエストが完了済みかどうかをdialogFlagsから確認
        /// </summary>
        public bool IsQuestCompleted(string questId) => _questManager.IsQuestCompleted(questId);

        /// <summary>
        /// 現在のストーリーフェーズを取得
        /// </summary>
        public StoryPhase GetCurrentPhase() => _questManager.PhaseManager.CurrentPhase;

        /// <summary>
        /// ストーリーフェーズを設定
        /// </summary>
        public void SetCurrentPhase(StoryPhase phase) => _questManager.PhaseManager.SetPhase(phase);

        /// <summary>
        /// 利用可能なクエストを取得（フェーズベース依存関係を考慮）
        /// </summary>
        public List<QuestDefinition> GetAvailableQuests()
        {
            var available = _questManager.GetAvailableQuests();

            // IQuestDefinition → QuestDefinition 変換
            var result = new List<QuestDefinition>();
            foreach (var quest in available)
            {
                var originalQuest = _allQuests.FirstOrDefault(q => q.QuestId == quest.QuestId);
                if (originalQuest != null)
                {
                    result.Add(originalQuest);
                }
            }

            // デバッグログ
            var currentPhase = GetCurrentPhase();
            int completedCount = _allQuests.Count(q => IsQuestCompleted(q.QuestId));
            ModLog.Log($"[ArenaQuest] Checking {_allQuests.Count} quests, {completedCount} completed, Phase: {currentPhase}");

            return result;
        }

        /// <summary>
        /// 自動発動クエストを取得（ゾーン入場時に発動）
        /// </summary>
        public List<QuestDefinition> GetAutoTriggerQuests()
        {
            return GetAvailableQuests().Where(q => q.AutoTrigger).ToList();
        }

        /// <summary>
        /// 特定NPCが持つ利用可能なクエストを取得
        /// </summary>
        public List<QuestDefinition> GetQuestsForNpc(string npcId)
        {
            return GetAvailableQuests().Where(q => q.QuestGiver == npcId).ToList();
        }

        /// <summary>
        /// クエストを持っているNPCのIDリストを取得（マーカー表示用）
        /// </summary>
        public List<string> GetNpcsWithQuests()
        {
            return GetAvailableQuests()
                .Where(q => !string.IsNullOrEmpty(q.QuestGiver))
                .Select(q => q.QuestGiver)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 特定のクエストが利用可能かチェック
        /// </summary>
        public bool IsQuestAvailable(string questId) => _questManager.IsQuestAvailable(questId);

        /// <summary>
        /// クエストを開始 (現時点では利用可能性のチェックのみ)
        /// </summary>
        public bool StartQuest(string questId)
        {
            if (!IsQuestAvailable(questId))
            {
                Debug.LogWarning($"[ArenaQuest] Cannot start quest '{questId}' - not available");
                return false;
            }

            ModLog.Log($"[ArenaQuest] Starting quest: {questId}");
            return true;
        }

        /// <summary>
        /// クエストを完了
        /// </summary>
        public void CompleteQuest(string questId)
        {
            ModLog.Log($"[ArenaQuest] CompleteQuest called for: {questId}");
            _questManager.CompleteQuest(questId);
            SyncBalgasFlagsFromQuestCompletion(questId);
        }

        private void SyncBalgasFlagsFromQuestCompletion(string questId)
        {
            // クエスト完了時に即座に派生フラグを同期（リアルタイム整合性）
            if (questId == "15_vs_balgas_spared")
            {
                ArenaContext.I.Storage.SetInt(ArenaFlagKeys.BalgasBattleComplete, 1);
                ArenaContext.I.Storage.SetInt(ArenaFlagKeys.BalgasKilled, 0);
            }
            else if (questId == "15_vs_balgas_killed")
            {
                ArenaContext.I.Storage.SetInt(ArenaFlagKeys.BalgasBattleComplete, 1);
                ArenaContext.I.Storage.SetInt(ArenaFlagKeys.BalgasKilled, 1);
            }
        }

        /// <summary>
        /// クエスト定義を取得
        /// </summary>
        public QuestDefinition GetQuest(string questId)
        {
            return _allQuests.FirstOrDefault(q => q.QuestId == questId);
        }

        /// <summary>
        /// デバッグ: 全クエストを完了済みにする
        /// </summary>
        public void DebugCompleteAllQuests()
        {
            ModLog.Log("[ArenaQuest] DEBUG: Completing all quests...");
            int count = 0;
            foreach (var quest in _allQuests)
            {
                if (!IsQuestCompleted(quest.QuestId))
                {
                    _questManager.CompleteQuest(quest.QuestId);
                    count++;
                }
            }
            // フェーズを最終段階に
            SetCurrentPhase(StoryPhase.Climax);
            ModLog.Log($"[ArenaQuest] DEBUG: Completed {count} quests, set phase to Climax");
        }

        /// <summary>
        /// デバッグ: 全クエスト状態をログ出力
        /// </summary>
        public void DebugLogQuestState()
        {
            ModLog.Log("[ArenaQuest] === Quest State ===");
            ModLog.Log($"  Total Quests: {_allQuests.Count}");
            ModLog.Log($"  Current Phase: {GetCurrentPhase()}");

            int completedCount = _allQuests.Count(q => IsQuestCompleted(q.QuestId));
            ModLog.Log($"  Completed: {completedCount}");

            // 完了済みクエストを表示
            var completedQuests = _allQuests.Where(q => IsQuestCompleted(q.QuestId)).ToList();
            foreach (var quest in completedQuests)
            {
                ModLog.Log($"    [DONE] {quest.QuestId}");
            }

            var available = GetAvailableQuests();
            ModLog.Log($"  Available: {available.Count}");

            foreach (var quest in available)
            {
                string marker = quest.AutoTrigger ? "[AUTO]" : !string.IsNullOrEmpty(quest.QuestGiver) ? $"[{quest.QuestGiver}]" : "";
                ModLog.Log($"    - {quest.QuestId} ({quest.Phase}) {marker} [Priority: {quest.Priority}]");
            }

            // NPCクエスト情報
            var npcsWithQuests = GetNpcsWithQuests();
            if (npcsWithQuests.Count > 0)
            {
                ModLog.Log($"  NPCs with quests: {string.Join(", ", npcsWithQuests)}");
            }

            ModLog.Log("[ArenaQuest] === End ===");
        }
    }

    /// <summary>
    /// QuestDefinitionをIQuestDefinitionにアダプト
    /// </summary>
    internal class QuestDefinitionAdapter : IQuestDefinition
    {
        private readonly QuestDefinition _quest;

        public QuestDefinitionAdapter(QuestDefinition quest)
        {
            _quest = quest;
        }

        public string QuestId => _quest.QuestId;
        public string QuestType => _quest.QuestType;
        public string DramaId => _quest.DramaId;
        public string DisplayNameJP => _quest.DisplayNameJP;
        public string DisplayNameEN => _quest.DisplayNameEN;
        public string Description => _quest.Description;
        public int Phase => (int)_quest.Phase;
        public string QuestGiver => _quest.QuestGiver;
        public bool AutoTrigger => _quest.AutoTrigger;
        public int? AdvancesPhase => _quest.AdvancesPhase.HasValue ? (int?)_quest.AdvancesPhase.Value : null;
        public IReadOnlyList<IFlagCondition> RequiredFlags => _quest.RequiredFlags.Select(f => (IFlagCondition)f).ToList();
        public IReadOnlyList<string> RequiredQuests => _quest.RequiredQuests;
        public IReadOnlyDictionary<string, object> CompletionFlags => _quest.CompletionFlags;
        public IReadOnlyList<string> BlocksQuests => _quest.BlocksQuests;
        public int Priority => _quest.Priority;
    }

    /// <summary>
    /// クエスト定義 (内部使用)
    /// JSON deserialize用のDTOは src/Generated/QuestDataContract.cs (QuestDataDto, FlagConditionDto) を使用
    /// </summary>
    public class QuestDefinition
    {
        public string QuestId { get; }
        public string QuestType { get; }
        public string DramaId { get; }
        public string DisplayNameJP { get; }
        public string DisplayNameEN { get; }
        public string Description { get; }

        // Phase system properties
        public StoryPhase Phase { get; }
        public string QuestGiver { get; }
        public bool AutoTrigger { get; }
        public StoryPhase? AdvancesPhase { get; }

        // Requirements
        public List<FlagCondition> RequiredFlags { get; }
        public List<string> RequiredQuests { get; }
        public Dictionary<string, object> CompletionFlags { get; }
        public List<string> BranchChoices { get; }
        public List<string> BlocksQuests { get; }
        public int Priority { get; }

        // Convenience properties
        public bool IsAutoTrigger => AutoTrigger && string.IsNullOrEmpty(QuestGiver);
        public bool IsNpcQuest => !string.IsNullOrEmpty(QuestGiver);

        public QuestDefinition(QuestDataDto data)
        {
            QuestId = data.QuestId;
            QuestType = data.QuestType;
            DramaId = data.DramaId;
            DisplayNameJP = data.DisplayNameJP;
            DisplayNameEN = data.DisplayNameEN;
            Description = data.Description;

            // Parse phase ordinal to enum
            Phase = (StoryPhase)data.PhaseOrdinal;
            QuestGiver = data.QuestGiver;
            AutoTrigger = data.AutoTrigger;

            // Parse advances phase (-1 means null/no advancement)
            if (data.AdvancesPhaseOrdinal >= 0 && Enum.IsDefined(typeof(StoryPhase), data.AdvancesPhaseOrdinal))
            {
                AdvancesPhase = (StoryPhase)data.AdvancesPhaseOrdinal;
            }
            else
            {
                AdvancesPhase = null;
            }

            RequiredFlags = data.RequiredFlags?.Select(f => new FlagCondition(f)).ToList() ?? new List<FlagCondition>();
            RequiredQuests = data.RequiredQuests ?? new List<string>();
            CompletionFlags = data.CompletionFlags ?? new Dictionary<string, object>();
            BranchChoices = data.BranchChoices ?? new List<string>();
            BlocksQuests = data.BlocksQuests ?? new List<string>();
            Priority = data.Priority;
        }
    }

    /// <summary>
    /// フラグ条件
    /// </summary>
    public class FlagCondition : IFlagCondition
    {
        public string FlagKey { get; }
        public string Operator { get; }
        public object Value { get; }

        public FlagCondition(FlagConditionDto data)
        {
            FlagKey = data.FlagKey;
            Operator = data.Operator;
            Value = data.Value;
        }
    }
}

