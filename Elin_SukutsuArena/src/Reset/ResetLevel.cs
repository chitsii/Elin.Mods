namespace Elin_SukutsuArena.Reset
{
    /// <summary>
    /// リセット操作のレベル
    /// </summary>
    public enum ResetLevel
    {
        /// <summary>
        /// 周回プレイ用（NewGame+）
        /// ストーリー・クエストをリセット、ランク・貢献度は保持
        /// </summary>
        NewGamePlus,

        /// <summary>
        /// Mod削除準備
        /// 全フラグ・フィート・ゾーンを削除
        /// </summary>
        Uninstall
    }
}
