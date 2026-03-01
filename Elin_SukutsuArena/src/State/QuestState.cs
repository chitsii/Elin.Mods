using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;

namespace Elin_SukutsuArena.State
{
    /// <summary>
    /// クエスト状態管理
    /// クエストの完了/開始状態を管理
    /// </summary>
    public class QuestState
    {
        private readonly IFlagStorage _storage;

        // フラグキープレフィックス
        private static string QuestDonePrefix => ArenaFlagKeys.QuestDonePrefix;
        private static string QuestActivePrefix => SessionFlagKeys.QuestActive;

        public QuestState(IFlagStorage storage)
        {
            _storage = storage;
        }

        /// <summary>クエストが完了しているか</summary>
        public bool IsCompleted(string questId)
        {
            return _storage.GetInt(QuestDonePrefix + questId) == 1;
        }

        /// <summary>クエストを完了としてマーク</summary>
        public void MarkCompleted(string questId)
        {
            _storage.SetInt(QuestDonePrefix + questId, 1);
            // アクティブフラグをクリア
            _storage.SetInt(QuestActivePrefix + questId, 0);
        }

        /// <summary>クエストがアクティブ（進行中）か</summary>
        public bool IsActive(string questId)
        {
            return _storage.GetInt(QuestActivePrefix + questId) == 1;
        }

        /// <summary>クエストをアクティブとしてマーク</summary>
        public void MarkActive(string questId)
        {
            _storage.SetInt(QuestActivePrefix + questId, 1);
        }

        /// <summary>クエストのアクティブ状態をクリア</summary>
        public void ClearActive(string questId)
        {
            _storage.SetInt(QuestActivePrefix + questId, 0);
        }
    }
}
