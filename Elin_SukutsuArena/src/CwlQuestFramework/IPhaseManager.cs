using System;

namespace CwlQuestFramework
{
    /// <summary>
    /// フェーズ（ストーリー進行状態）を管理するインターフェース
    /// </summary>
    /// <typeparam name="TPhase">フェーズを表すEnum型</typeparam>
    public interface IPhaseManager<TPhase> where TPhase : struct, Enum
    {
        /// <summary>
        /// 現在のフェーズ
        /// </summary>
        TPhase CurrentPhase { get; }

        /// <summary>
        /// フェーズを設定
        /// </summary>
        void SetPhase(TPhase phase);

        /// <summary>
        /// 現在のフェーズの整数値を取得
        /// </summary>
        int GetPhaseOrdinal();

        /// <summary>
        /// フェーズが変更された時に発火するイベント
        /// </summary>
        event Action<TPhase, TPhase> OnPhaseChanged;
    }
}
