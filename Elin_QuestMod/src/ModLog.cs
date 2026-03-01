using BepInEx.Logging;
using UnityEngine;

namespace Elin_QuestMod
{
    public static class ModLog
    {
        private const string Prefix = "[QuestMod] ";
        private static ManualLogSource _logger;

        public static void SetLogger(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void Info(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo(message ?? string.Empty);
                return;
            }

            Debug.Log(Prefix + (message ?? string.Empty));
        }

        public static void Warn(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning(message ?? string.Empty);
                return;
            }

            Debug.LogWarning(Prefix + (message ?? string.Empty));
        }

        public static void Error(string message)
        {
            if (_logger != null)
            {
                _logger.LogError(message ?? string.Empty);
                return;
            }

            Debug.LogError(Prefix + (message ?? string.Empty));
        }
    }
}
