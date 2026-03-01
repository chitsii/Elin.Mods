using System.Collections.Generic;

namespace Elin_SukutsuArena.Core
{
    /// <summary>
    /// テスト用のインメモリストレージ実装
    /// </summary>
    public class InMemoryFlagStorage : IFlagStorage
    {
        private readonly Dictionary<string, int> _flags = new Dictionary<string, int>();

        public int GetInt(string key, int defaultValue = 0)
        {
            return _flags.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _flags[key] = value;
        }

        public bool HasKey(string key)
        {
            return _flags.ContainsKey(key);
        }

        /// <summary>テスト用: 全フラグをクリア</summary>
        public void Clear()
        {
            _flags.Clear();
        }
    }
}
