using System.Collections.Generic;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Small helper for dialogFlags access with null-safe semantics.
    /// </summary>
    public static class DialogFlagStore
    {
        public static int GetInt(IDictionary<string, int>? flags, string key)
        {
            if (flags == null) return 0;
            return flags.TryGetValue(key, out int value) ? value : 0;
        }

        public static void SetInt(IDictionary<string, int>? flags, string key, int value)
        {
            if (flags == null) return;
            flags[key] = value;
        }

        public static bool IsTrue(IDictionary<string, int>? flags, string key)
        {
            return GetInt(flags, key) == 1;
        }

        public static void SetBool(IDictionary<string, int>? flags, string key, bool value)
        {
            SetInt(flags, key, value ? 1 : 0);
        }
    }
}
