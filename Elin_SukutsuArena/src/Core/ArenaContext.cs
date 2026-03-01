using Elin_SukutsuArena.Migration;
using Elin_SukutsuArena.State;

namespace Elin_SukutsuArena.Core
{
    /// <summary>
    /// アリーナの状態管理の中心
    /// 全ての状態アクセスはここを経由する
    /// </summary>
    public class ArenaContext
    {
        private static ArenaContext _instance;

        /// <summary>
        /// シングルトンインスタンス
        /// 本番環境ではDialogFlagsStorageを使用
        /// </summary>
        public static ArenaContext I => _instance ??= new ArenaContext(new DialogFlagsStorage());

        /// <summary>
        /// テスト用: インスタンスを差し替え
        /// </summary>
        public static void SetInstance(ArenaContext context)
        {
            _instance = context;
        }

        /// <summary>
        /// テスト用: インスタンスをリセット
        /// </summary>
        public static void ResetInstance()
        {
            _instance = null;
        }

        private readonly IFlagStorage _storage;

        public ArenaContext(IFlagStorage storage)
        {
            _storage = storage;

            // 古いフラグキーのクリーンアップ
            FlagMigration.TryCleanup();

            Player = new PlayerState(storage);
            Quest = new QuestState(storage);
            Session = new SessionState(storage);
        }

        /// <summary>プレイヤー状態</summary>
        public PlayerState Player { get; }

        /// <summary>クエスト状態</summary>
        public QuestState Quest { get; }

        /// <summary>セッション用一時フラグ</summary>
        public SessionState Session { get; }

        /// <summary>低レベルストレージへのアクセス (特殊な用途向け)</summary>
        public IFlagStorage Storage => _storage;
    }
}
