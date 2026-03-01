using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Elin_BottleNeckFinder
{
    public static class ErrorMonitor
    {
        public struct ErrorEntry
        {
            public float Timestamp;
            public string ModName;
            public string Summary;
            public bool IsPatchFailure;
        }

        private static readonly List<ErrorEntry> _errors = new List<ErrorEntry>();
        private static int _maxHistory = 20;

        private static readonly Regex AsmPattern =
            new Regex(@"at\s+(\w[\w.]*?)\.\w+\s*\(", RegexOptions.Compiled);

        public static IReadOnlyList<ErrorEntry> Errors => _errors;
        public static int PatchFailureCount { get; private set; }

        public static void Start(int maxHistory)
        {
            _maxHistory = maxHistory;
            Application.logMessageReceived += OnLogMessage;
        }

        public static void Stop()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        public static void Clear()
        {
            _errors.Clear();
            PatchFailureCount = 0;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;

            bool isPatchFailure = IsPatchError(message, stackTrace);
            string modName = TryResolveModName(stackTrace);

            if (isPatchFailure)
                PatchFailureCount++;

            var entry = new ErrorEntry
            {
                Timestamp = Time.realtimeSinceStartup,
                ModName = modName,
                Summary = TruncateMessage(message),
                IsPatchFailure = isPatchFailure
            };

            _errors.Add(entry);

            while (_errors.Count > _maxHistory)
                _errors.RemoveAt(0);
        }

        private static bool IsPatchError(string message, string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return false;
            return stackTrace.Contains("HarmonyLib")
                || (stackTrace.Contains("Harmony")
                   && (stackTrace.Contains("PatchAll")
                    || stackTrace.Contains("PatchProcessor")
                    || message.Contains("patch")));
        }

        private static string TryResolveModName(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return null;

            var matches = AsmPattern.Matches(stackTrace);
            foreach (Match m in matches)
            {
                string fullType = m.Groups[1].Value;
                var parts = fullType.Split('.');
                foreach (var part in parts)
                {
                    var resolved = PatchRegistry.ResolveModFromAsm(part);
                    if (resolved != null) return resolved;
                }
            }
            return null;
        }

        private static string TruncateMessage(string message)
        {
            if (message == null) return "";
            int nl = message.IndexOf('\n');
            if (nl >= 0) message = message.Substring(0, nl);
            return message.Length > 80 ? message.Substring(0, 77) + "..." : message;
        }
    }
}
