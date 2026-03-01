namespace CwlQuestFramework
{
    /// <summary>
    /// フラグ値を設定するためのインターフェース
    /// IFlagValueProviderを継承し、読み取り/書き込み両方をサポート
    /// </summary>
    public interface IFlagSetter : IFlagValueProvider
    {
        /// <summary>
        /// フラグの整数値を設定
        /// </summary>
        void SetInt(string key, int value);

        /// <summary>
        /// JSONからの値を適切に変換して設定
        /// int/long → そのまま設定
        /// bool → 1/0に変換
        /// string → Enumマッピングまたは整数パース
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// 文字列値がEnumマッピングに存在せず、整数としてもパースできない場合
        /// </exception>
        void SetFromJsonValue(string key, object value);
    }
}
