using System;
using System.Collections.Generic;

namespace CwlQuestFramework
{
    /// <summary>
    /// CWL汎用クエスト管理インターフェース
    /// 他のModでも再利用可能な抽象化レイヤー
    /// </summary>
    public interface IQuestManager
    {
        /// <summary>
        /// クエストが完了済みかどうかを確認
        /// </summary>
        bool IsQuestCompleted(string questId);

        /// <summary>
        /// クエストが現在利用可能かどうかを確認
        /// </summary>
        bool IsQuestAvailable(string questId);

        /// <summary>
        /// クエストを完了としてマーク
        /// </summary>
        void CompleteQuest(string questId);

        /// <summary>
        /// 現在利用可能な全クエストを取得
        /// </summary>
        IReadOnlyList<IQuestDefinition> GetAvailableQuests();

        /// <summary>
        /// 特定NPCが提供する利用可能なクエストを取得
        /// </summary>
        IReadOnlyList<IQuestDefinition> GetQuestsForNpc(string npcId);

        /// <summary>
        /// 自動発動クエストを取得
        /// </summary>
        IReadOnlyList<IQuestDefinition> GetAutoTriggerQuests();

        /// <summary>
        /// クエストを持っているNPCのIDリストを取得
        /// </summary>
        IReadOnlyList<string> GetNpcsWithQuests();

        /// <summary>
        /// クエスト状態が変更された時に発火するイベント
        /// </summary>
        event Action OnQuestStateChanged;
    }

    /// <summary>
    /// クエスト定義のインターフェース
    /// </summary>
    public interface IQuestDefinition
    {
        string QuestId { get; }
        string QuestType { get; }
        string DramaId { get; }
        string DisplayNameJP { get; }
        string DisplayNameEN { get; }
        string Description { get; }
        int Phase { get; }
        string QuestGiver { get; }
        bool AutoTrigger { get; }
        int? AdvancesPhase { get; }
        IReadOnlyList<IFlagCondition> RequiredFlags { get; }
        IReadOnlyList<string> RequiredQuests { get; }
        IReadOnlyDictionary<string, object> CompletionFlags { get; }
        IReadOnlyList<string> BlocksQuests { get; }
        int Priority { get; }
    }

    /// <summary>
    /// フラグ条件のインターフェース
    /// </summary>
    public interface IFlagCondition
    {
        string FlagKey { get; }
        string Operator { get; }
        object Value { get; }
    }
}
