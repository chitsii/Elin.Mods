namespace Elin_SukutsuArena.Core
{
    /// <summary>
    /// フラグストレージの抽象化インターフェース
    /// テスト時にInMemory実装に差し替え可能
    /// </summary>
    public interface IFlagStorage
    {
        /// <summary>整数値を取得</summary>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>整数値を設定</summary>
        void SetInt(string key, int value);

        /// <summary>キーが存在するか確認</summary>
        bool HasKey(string key);
    }
}
