using Elin_SukutsuArena.Core;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// アリーナコマンドのインターフェース
    /// ExcelドラマスクリプトからのmodInvoke呼び出しを型安全に処理
    /// </summary>
    public interface IArenaCommand
    {
        /// <summary>
        /// コマンド名 (modInvokeで使用する名前)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// コマンドを実行
        /// </summary>
        /// <param name="ctx">アリーナコンテキスト</param>
        /// <param name="drama">DramaManagerインスタンス</param>
        /// <param name="args">コマンド引数</param>
        void Execute(ArenaContext ctx, DramaManager drama, string[] args);
    }
}
