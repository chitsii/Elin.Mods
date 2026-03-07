using System;
using BepInEx.Logging;
using UnityEngine;

namespace Elin_JustDoomIt
{
    public static class DoomDiagnostics
    {
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        public static void Info(string message)
        {
            _logger?.LogInfo(message);
            Debug.Log(message);
        }

        public static void Warn(string message)
        {
            _logger?.LogWarning(message);
            Debug.LogWarning(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (ex == null)
            {
                _logger?.LogError(message);
                Debug.LogError(message);
            }
            else
            {
                var full = message + Environment.NewLine + ex;
                _logger?.LogError(full);
                Debug.LogError(full);
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Error("[JustDoomIt] Unhandled exception.", e.ExceptionObject as Exception);
        }
    }
}

