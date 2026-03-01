using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Mod-wide logging helpers.
    /// Debug/Trace logs are compiled only in DEBUG builds.
    /// Warn/Error always appear regardless of build configuration.
    /// </summary>
    public static class ModLog
    {
        private const string Tag = "[ArsMoriendi]";

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Log(string message)
        {
            Debug.Log($"{Tag} {message}");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Log(string format, params object[] args)
        {
            Debug.Log($"{Tag} {string.Format(format, args)}");
        }

        public static void Warn(string message)
        {
            Debug.LogWarning($"{Tag} {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"{Tag} {message}");
        }
    }
}
