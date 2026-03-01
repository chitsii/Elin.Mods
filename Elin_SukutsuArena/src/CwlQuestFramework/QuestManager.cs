using System;
using System.Collections.Generic;
using System.Linq;

namespace CwlQuestFramework
{
    /// <summary>
    /// 汎用クエストマネージャー
    /// フェーズベースのクエスト管理を提供
    /// </summary>
    /// <typeparam name="TPhase">フェーズを表すEnum型</typeparam>
    public class QuestManager<TPhase> : IQuestManager
        where TPhase : struct, Enum
    {
        private readonly IFlagSetter _flagSetter;
        private readonly IPhaseManager<TPhase> _phaseManager;
        private readonly List<IQuestDefinition> _quests;
        private readonly string _questCompletedPrefix;
        private readonly List<IQuestStateObserver> _observers = new List<IQuestStateObserver>();
        private readonly QuestAvailabilityEvaluator _evaluator;

        /// <summary>
        /// クエスト状態が変更された時に発火するイベント
        /// </summary>
        public event Action OnQuestStateChanged;

        /// <summary>
        /// フェーズマネージャーへのアクセス
        /// </summary>
        public IPhaseManager<TPhase> PhaseManager => _phaseManager;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="flagSetter">フラグ設定アダプター</param>
        /// <param name="phaseManager">フェーズマネージャー</param>
        /// <param name="quests">クエスト定義リスト</param>
        /// <param name="questCompletedPrefix">クエスト完了フラグのプレフィックス</param>
        public QuestManager(
            IFlagSetter flagSetter,
            IPhaseManager<TPhase> phaseManager,
            IEnumerable<IQuestDefinition> quests,
            string questCompletedPrefix)
        {
            _flagSetter = flagSetter ?? throw new ArgumentNullException(nameof(flagSetter));
            _phaseManager = phaseManager ?? throw new ArgumentNullException(nameof(phaseManager));
            _quests = quests?.ToList() ?? throw new ArgumentNullException(nameof(quests));
            _questCompletedPrefix = questCompletedPrefix ?? throw new ArgumentNullException(nameof(questCompletedPrefix));

            // QuestAvailabilityEvaluatorを作成
            var conditionChecker = new QuestConditionChecker(_flagSetter);
            _evaluator = new QuestAvailabilityEvaluator(conditionChecker, IsQuestCompleted);
        }

        /// <summary>
        /// オブザーバーを追加
        /// </summary>
        public void AddObserver(IQuestStateObserver observer)
        {
            if (observer != null && !_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        /// <summary>
        /// オブザーバーを削除
        /// </summary>
        public void RemoveObserver(IQuestStateObserver observer)
        {
            if (observer != null)
            {
                _observers.Remove(observer);
            }
        }

        /// <summary>
        /// クエストが完了済みかどうかを確認
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            string flagKey = GetQuestCompletedFlagKey(questId);
            return _flagSetter.GetInt(flagKey, 0) == 1;
        }

        /// <summary>
        /// クエストが現在利用可能かどうかを確認
        /// </summary>
        public bool IsQuestAvailable(string questId)
        {
            var quest = _quests.FirstOrDefault(q => q.QuestId == questId);
            if (quest == null) return false;

            return _evaluator.IsAvailable(quest, _phaseManager.GetPhaseOrdinal(), _quests);
        }

        /// <summary>
        /// クエストを完了
        /// </summary>
        public void CompleteQuest(string questId)
        {
            // 既に完了済みなら何もしない
            if (IsQuestCompleted(questId))
            {
                return;
            }

            // クエストを検索
            var quest = _quests.FirstOrDefault(q => q.QuestId == questId);
            if (quest == null)
            {
                return;
            }

            // 完了フラグを設定
            string flagKey = GetQuestCompletedFlagKey(questId);
            _flagSetter.SetInt(flagKey, 1);

            // CompletionFlagsを適用
            foreach (var kvp in quest.CompletionFlags)
            {
                _flagSetter.SetFromJsonValue(kvp.Key, kvp.Value);
                NotifyObservers_OnFlagSet(kvp.Key, kvp.Value);
            }

            // フェーズ進行
            if (quest.AdvancesPhase.HasValue)
            {
                int oldPhase = _phaseManager.GetPhaseOrdinal();
                int newPhase = quest.AdvancesPhase.Value;

                if (oldPhase != newPhase)
                {
                    TPhase newPhaseEnum = (TPhase)Enum.ToObject(typeof(TPhase), newPhase);
                    _phaseManager.SetPhase(newPhaseEnum);
                    NotifyObservers_OnPhaseAdvanced(oldPhase, newPhase);
                }
            }

            // オブザーバーに通知
            NotifyObservers_OnQuestCompleted(questId, quest);

            // イベント発火
            OnQuestStateChanged?.Invoke();
        }

        /// <summary>
        /// 現在利用可能な全クエストを取得
        /// </summary>
        public IReadOnlyList<IQuestDefinition> GetAvailableQuests()
        {
            int currentPhase = _phaseManager.GetPhaseOrdinal();

            var available = _quests
                .Where(q => _evaluator.IsAvailable(q, currentPhase, _quests))
                .OrderByDescending(q => q.Priority)
                .ToList();

            return available;
        }

        /// <summary>
        /// 特定NPCが提供する利用可能なクエストを取得
        /// </summary>
        public IReadOnlyList<IQuestDefinition> GetQuestsForNpc(string npcId)
        {
            return GetAvailableQuests()
                .Where(q => q.QuestGiver == npcId)
                .ToList();
        }

        /// <summary>
        /// 自動発動クエストを取得
        /// </summary>
        public IReadOnlyList<IQuestDefinition> GetAutoTriggerQuests()
        {
            return GetAvailableQuests()
                .Where(q => q.AutoTrigger)
                .ToList();
        }

        /// <summary>
        /// クエストを持っているNPCのIDリストを取得
        /// </summary>
        public IReadOnlyList<string> GetNpcsWithQuests()
        {
            return GetAvailableQuests()
                .Where(q => !string.IsNullOrEmpty(q.QuestGiver))
                .Select(q => q.QuestGiver)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// クエスト定義を取得
        /// </summary>
        public IQuestDefinition GetQuest(string questId)
        {
            return _quests.FirstOrDefault(q => q.QuestId == questId);
        }

        /// <summary>
        /// 全クエスト定義を取得
        /// </summary>
        public IReadOnlyList<IQuestDefinition> GetAllQuests()
        {
            return _quests;
        }

        // === Private methods ===

        private string GetQuestCompletedFlagKey(string questId)
        {
            return _questCompletedPrefix + questId;
        }

        private void NotifyObservers_OnQuestCompleted(string questId, IQuestDefinition quest)
        {
            foreach (var observer in _observers)
            {
                observer.OnQuestCompleted(questId, quest);
            }
        }

        private void NotifyObservers_OnPhaseAdvanced(int oldPhase, int newPhase)
        {
            foreach (var observer in _observers)
            {
                observer.OnPhaseAdvanced(oldPhase, newPhase);
            }
        }

        private void NotifyObservers_OnFlagSet(string key, object value)
        {
            foreach (var observer in _observers)
            {
                observer.OnFlagSet(key, value);
            }
        }
    }
}
