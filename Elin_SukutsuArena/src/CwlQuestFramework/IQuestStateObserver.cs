namespace CwlQuestFramework
{
    /// <summary>
    /// クエスト状態変更を監視するオブザーバーインターフェース
    /// Mod固有の処理を実装するために使用
    /// </summary>
    public interface IQuestStateObserver
    {
        /// <summary>
        /// クエストが完了した時に呼ばれる
        /// </summary>
        /// <param name="questId">完了したクエストID</param>
        /// <param name="quest">クエスト定義</param>
        void OnQuestCompleted(string questId, IQuestDefinition quest);

        /// <summary>
        /// フェーズが進行した時に呼ばれる
        /// </summary>
        /// <param name="oldPhase">変更前のフェーズ（整数値）</param>
        /// <param name="newPhase">変更後のフェーズ（整数値）</param>
        void OnPhaseAdvanced(int oldPhase, int newPhase);

        /// <summary>
        /// フラグが設定された時に呼ばれる
        /// </summary>
        /// <param name="key">フラグキー</param>
        /// <param name="value">設定された値</param>
        void OnFlagSet(string key, object value);
    }
}
