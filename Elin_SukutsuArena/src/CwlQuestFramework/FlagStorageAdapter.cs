using System;
using Elin_SukutsuArena.Core;

namespace CwlQuestFramework
{
    /// <summary>
    /// IFlagStorageをIFlagSetterにアダプトするクラス
    /// Enumマッピング付きの型変換を提供
    /// </summary>
    public class FlagStorageAdapter : IFlagSetter
    {
        private readonly IFlagStorage _storage;
        private readonly IEnumMappingProvider _enumMappingProvider;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="storage">フラグストレージ</param>
        /// <param name="enumMappingProvider">Enumマッピングプロバイダー（null可）</param>
        public FlagStorageAdapter(IFlagStorage storage, IEnumMappingProvider enumMappingProvider = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _enumMappingProvider = enumMappingProvider;
        }

        /// <summary>
        /// フラグの整数値を取得
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            return _storage.GetInt(key, defaultValue);
        }

        /// <summary>
        /// フラグが存在するか確認
        /// </summary>
        public bool HasKey(string key)
        {
            return _storage.HasKey(key);
        }

        /// <summary>
        /// フラグの整数値を設定
        /// </summary>
        public void SetInt(string key, int value)
        {
            _storage.SetInt(key, value);
        }

        /// <summary>
        /// JSONからの値を適切に変換して設定
        /// </summary>
        public void SetFromJsonValue(string key, object value)
        {
            if (value is int intValue)
            {
                _storage.SetInt(key, intValue);
                return;
            }

            if (value is long longValue)
            {
                _storage.SetInt(key, (int)longValue);
                return;
            }

            if (value is bool boolValue)
            {
                _storage.SetInt(key, boolValue ? 1 : 0);
                return;
            }

            if (value is string strValue)
            {
                // Enumマッピングを試行
                if (_enumMappingProvider != null &&
                    _enumMappingProvider.TryGetMapping(key, out var mapping))
                {
                    if (mapping.TryGetValue(strValue, out int mappedValue))
                    {
                        _storage.SetInt(key, mappedValue);
                        return;
                    }
                    // マッピングは存在するが、値が見つからない
                    throw new InvalidOperationException(
                        $"Unknown enum value '{strValue}' for flag '{key}'. " +
                        $"Valid values: {string.Join(", ", mapping.Keys)}");
                }

                // 文字列を整数としてパース試行
                if (int.TryParse(strValue, out int parsedValue))
                {
                    _storage.SetInt(key, parsedValue);
                    return;
                }

                // マッピングもなく、整数パースも失敗 = 設定ミス
                throw new InvalidOperationException(
                    $"Cannot resolve string value '{strValue}' for flag '{key}'. " +
                    $"No enum mapping exists and value is not a valid integer.");
            }

            // その他の型は整数に変換を試みる
            _storage.SetInt(key, Convert.ToInt32(value));
        }
    }
}
