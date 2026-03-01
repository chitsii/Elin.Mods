using System;
using Elin_SukutsuArena.Core;

namespace CwlQuestFramework
{
    /// <summary>
    /// IFlagStorageを使用する標準フェーズマネージャー
    /// </summary>
    /// <typeparam name="TPhase">フェーズを表すEnum型</typeparam>
    public class StandardPhaseManager<TPhase> : IPhaseManager<TPhase>
        where TPhase : struct, Enum
    {
        private readonly IFlagStorage _storage;
        private readonly string _phaseKey;

        /// <summary>
        /// フェーズが変更された時に発火するイベント
        /// </summary>
        public event Action<TPhase, TPhase> OnPhaseChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="storage">フラグストレージ</param>
        /// <param name="phaseKey">フェーズを保存するキー</param>
        public StandardPhaseManager(IFlagStorage storage, string phaseKey)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _phaseKey = phaseKey ?? throw new ArgumentNullException(nameof(phaseKey));
        }

        /// <summary>
        /// 現在のフェーズ
        /// </summary>
        public TPhase CurrentPhase
        {
            get
            {
                int ordinal = GetPhaseOrdinal();
                // Enumに変換（範囲外の場合はデフォルト値を返す）
                if (Enum.IsDefined(typeof(TPhase), ordinal))
                {
                    return (TPhase)Enum.ToObject(typeof(TPhase), ordinal);
                }
                // デフォルト値（最初のEnum値）
                return default;
            }
        }

        /// <summary>
        /// フェーズを設定
        /// </summary>
        public void SetPhase(TPhase phase)
        {
            TPhase oldPhase = CurrentPhase;
            int newOrdinal = Convert.ToInt32(phase);
            int oldOrdinal = GetPhaseOrdinal();

            if (oldOrdinal != newOrdinal)
            {
                _storage.SetInt(_phaseKey, newOrdinal);
                OnPhaseChanged?.Invoke(oldPhase, phase);
            }
        }

        /// <summary>
        /// 現在のフェーズの整数値を取得
        /// </summary>
        public int GetPhaseOrdinal()
        {
            return _storage.GetInt(_phaseKey, 0);
        }
    }
}
