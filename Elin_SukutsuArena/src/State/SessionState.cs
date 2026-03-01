using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Data;
using Elin_SukutsuArena.Flags;

namespace Elin_SukutsuArena.State
{
    /// <summary>
    /// セッション用の一時フラグ管理
    /// ドラマ処理中などの一時的な状態を管理
    /// </summary>
    public class SessionState
    {
        private readonly IFlagStorage _storage;

        // フラグキー定数
        private static string KeyQuestFound => SessionFlagKeys.QuestFound;
        private static string KeyQuestTargetName => SessionFlagKeys.QuestTargetName;
        private static string KeyAutoDialog => SessionFlagKeys.AutoDialog;
        private static string KeyArenaResult => SessionFlagKeys.ArenaResult;

        public SessionState(IFlagStorage storage)
        {
            _storage = storage;
        }

        /// <summary>クエスト検索で見つかったかどうか</summary>
        public bool QuestFound
        {
            get => _storage.GetInt(KeyQuestFound) == 1;
            set => _storage.SetInt(KeyQuestFound, value ? 1 : 0);
        }

        /// <summary>見つかったクエストのジャンプ先</summary>
        public JumpLabel QuestJumpTarget
        {
            get => (JumpLabel)_storage.GetInt(KeyQuestTargetName, 0);
            set => _storage.SetInt(KeyQuestTargetName, (int)value);
        }

        /// <summary>自動ダイアログのトリガー用NPC UID</summary>
        public int AutoDialogNpcUid
        {
            get => _storage.GetInt(KeyAutoDialog, 0);
            set => _storage.SetInt(KeyAutoDialog, value);
        }

        /// <summary>アリーナ戦闘結果 (0=未設定, 1=勝利, 2=敗北)</summary>
        public ArenaResult ArenaResult
        {
            get => (ArenaResult)_storage.GetInt(KeyArenaResult, 0);
            set => _storage.SetInt(KeyArenaResult, (int)value);
        }

        /// <summary>クエスト検索状態をクリア</summary>
        public void ClearQuestSearch()
        {
            QuestFound = false;
            QuestJumpTarget = JumpLabel.None;
        }

        /// <summary>アリーナ戦闘結果をクリア</summary>
        public void ClearArenaResult()
        {
            ArenaResult = ArenaResult.None;
        }
    }

    /// <summary>アリーナ戦闘結果</summary>
    public enum ArenaResult
    {
        None = 0,
        Victory = 1,
        Defeat = 2
    }
}
