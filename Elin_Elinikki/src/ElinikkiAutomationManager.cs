using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class ElinikkiAutomationManager : MonoBehaviour
    {
        private const string RequestFileName = "automation_request.json";
        private const string ResultFileName = "automation_result.json";

        private static ElinikkiAutomationManager _instance;

        private AutomationRequest _request;
        private AutomationPhase _phase = AutomationPhase.Idle;
        private float _phaseStartedRealtime;
        private bool _loadIssued;

        public static void EnsureCreated()
        {
            if (_instance != null)
            {
                return;
            }

            ElinikkiAutomationManager existing = FindObjectOfType<ElinikkiAutomationManager>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            GameObject managerObject = new GameObject("ElinikkiAutomationManager");
            DontDestroyOnLoad(managerObject);
            managerObject.hideFlags = HideFlags.HideAndDontSave;
            _instance = managerObject.AddComponent<ElinikkiAutomationManager>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            TryLoadRequest();
        }

        private void Update()
        {
            if (_request == null)
            {
                return;
            }

            switch (_phase)
            {
                case AutomationPhase.PendingLoad:
                    TryStartLoad();
                    break;
                case AutomationPhase.WaitingForGame:
                    AdvanceAfterLoad();
                    break;
                case AutomationPhase.WaitingForDump:
                    TryStartDump();
                    break;
                case AutomationPhase.CapturingDump:
                    ObserveDumpCompletion();
                    break;
            }
        }

        private void TryLoadRequest()
        {
            string path = GetRequestFilePath();
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                _request = AutomationRequest.FromJson(json);
                if (_request == null || string.IsNullOrEmpty(_request.SaveId))
                {
                    WriteResult("failed", "Invalid automation request.");
                    TryDeleteFile(path);
                    _request = null;
                    return;
                }

                _phase = AutomationPhase.PendingLoad;
                _phaseStartedRealtime = Time.realtimeSinceStartup;
                Plugin.Log?.LogInfo($"Automation request loaded. save={_request.SaveId} cloud={_request.Cloud} autoDump={_request.AutoDump}");
            }
            catch (Exception ex)
            {
                WriteResult("failed", "Failed to parse automation request: " + ex.Message);
                TryDeleteFile(path);
                _request = null;
            }
        }

        private void TryStartLoad()
        {
            if (_loadIssued || EClass.core == null || EClass.scene == null)
            {
                return;
            }

            if (EClass.core.IsGameStarted && Game.id == _request.SaveId)
            {
                _phase = _request.AutoDump ? AutomationPhase.WaitingForDump : AutomationPhase.Completed;
                _phaseStartedRealtime = Time.realtimeSinceStartup;
                if (!_request.AutoDump)
                {
                    Complete("passed", "Requested save already loaded.");
                }

                return;
            }

            bool started = Game.TryLoad(_request.SaveId, _request.Cloud, delegate
            {
                Game.Load(_request.SaveId, _request.Cloud);
            });

            if (started)
            {
                _loadIssued = true;
                _phase = AutomationPhase.WaitingForGame;
                _phaseStartedRealtime = Time.realtimeSinceStartup;
                Plugin.Log?.LogInfo($"Automation load started for save {_request.SaveId}.");
            }
            else
            {
                Complete("failed", $"Save '{_request.SaveId}' could not be loaded.");
            }
        }

        private void AdvanceAfterLoad()
        {
            if (!IsRuntimeReady() || Game.id != _request.SaveId)
            {
                if (Time.realtimeSinceStartup - _phaseStartedRealtime > Mathf.Max(10f, _request.LoadTimeoutSeconds))
                {
                    Complete("failed", $"Timed out waiting for save '{_request.SaveId}' to load.");
                }

                return;
            }

            _phase = _request.AutoDump ? AutomationPhase.WaitingForDump : AutomationPhase.Completed;
            _phaseStartedRealtime = Time.realtimeSinceStartup;
            Plugin.Log?.LogInfo($"Automation load completed for save {_request.SaveId}.");

            if (!_request.AutoDump)
            {
                Complete("passed", "Requested save loaded.");
            }
        }

        private void TryStartDump()
        {
            if (!IsRuntimeReady())
            {
                return;
            }

            if (Time.realtimeSinceStartup - _phaseStartedRealtime < Mathf.Max(0f, _request.DumpDelaySeconds))
            {
                return;
            }

            FpsViewManager.EnsureCreated();
            SharedWorldObjectManager.EnsureCreated();
            ElinikkiVisualDumpManager.EnsureCreated();

            ElinikkiVisualDumpManager dumpManager = ElinikkiVisualDumpManager.InstanceForAutomation;
            if (dumpManager == null)
            {
                if (Time.realtimeSinceStartup - _phaseStartedRealtime > Mathf.Max(5f, _request.DumpTimeoutSeconds))
                {
                    Complete("failed", "Visual dump manager is not available.");
                }

                return;
            }

            if (!dumpManager.TryCaptureForAutomation())
            {
                return;
            }

            _phase = AutomationPhase.CapturingDump;
            _phaseStartedRealtime = Time.realtimeSinceStartup;
            Plugin.Log?.LogInfo("Automation visual dump started.");
        }

        private void ObserveDumpCompletion()
        {
            ElinikkiVisualDumpManager dumpManager = ElinikkiVisualDumpManager.InstanceForAutomation;
            if (dumpManager == null)
            {
                Complete("failed", "Visual dump manager disappeared during capture.");
                return;
            }

            if (dumpManager.IsCapturingForAutomation)
            {
                if (Time.realtimeSinceStartup - _phaseStartedRealtime > Mathf.Max(20f, _request.DumpTimeoutSeconds))
                {
                    Complete("failed", "Timed out while waiting for visual dump capture.");
                }

                return;
            }

            if (!string.IsNullOrEmpty(dumpManager.LastFailure))
            {
                Complete("failed", dumpManager.LastFailure);
                return;
            }

            if (!string.IsNullOrEmpty(dumpManager.LastDumpRoot))
            {
                Complete("passed", "Automation dump complete.", dumpManager.LastDumpRoot);
                return;
            }

            Complete("failed", "Visual dump did not produce any output.");
        }

        private void Complete(string status, string message, string dumpRoot = "")
        {
            WriteResult(status, message, dumpRoot);
            bool shouldCloseGame = _request != null && _request.CloseGameAfterDump;
            TryDeleteFile(GetRequestFilePath());
            _phase = AutomationPhase.Completed;
            _request = null;
            _loadIssued = false;
            if (shouldCloseGame)
            {
                Plugin.Log?.LogInfo("Automation requested game shutdown.");
                Application.Quit();
            }
        }

        private static bool IsRuntimeReady()
        {
            return EClass.core != null
                && EClass.core.IsGameStarted
                && EClass.pc != null
                && EClass.scene != null
                && EClass._map != null;
        }

        private static string GetRequestFilePath()
        {
            return Path.Combine(GetModDirectory(), RequestFileName);
        }

        private static string GetResultFilePath()
        {
            return Path.Combine(GetModDirectory(), ResultFileName);
        }

        private static string GetModDirectory()
        {
            return Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? ".";
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to delete automation file '{path}': {ex.Message}");
            }
        }

        private static void WriteResult(string status, string message, string dumpRoot = "")
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('{');
                AppendJsonProp(sb, "status", status);
                sb.Append(',');
                AppendJsonProp(sb, "message", message);
                sb.Append(',');
                AppendJsonProp(sb, "save_id", Game.id ?? string.Empty);
                sb.Append(',');
                AppendJsonProp(sb, "dump_root", dumpRoot ?? string.Empty);
                sb.Append(',');
                AppendJsonProp(sb, "written_utc", DateTime.UtcNow.ToString("o"));
                sb.Append('}');
                File.WriteAllText(GetResultFilePath(), sb.ToString(), new UTF8Encoding(false));
                Plugin.Log?.LogInfo($"Automation result written: {status} {message}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to write automation result: {ex}");
            }
        }

        private static void AppendJsonProp(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":\"");
            sb.Append(JsonEscape(value ?? string.Empty)).Append('"');
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(value.Length + 16);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            return sb.ToString();
        }

        private enum AutomationPhase
        {
            Idle,
            PendingLoad,
            WaitingForGame,
            WaitingForDump,
            CapturingDump,
            Completed
        }

        [Serializable]
        private sealed class AutomationRequest
        {
            public string SaveId = string.Empty;
            public bool Cloud;
            public bool AutoDump = true;
            public bool CloseGameAfterDump = true;
            public float DumpDelaySeconds = 4f;
            public float LoadTimeoutSeconds = 20f;
            public float DumpTimeoutSeconds = 20f;

            public static AutomationRequest FromJson(string json)
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                AutomationRequest request = new AutomationRequest();
                request.SaveId = ReadJsonString(json, "save_id") ?? string.Empty;
                request.Cloud = ReadJsonBool(json, "cloud", false);
                request.AutoDump = ReadJsonBool(json, "auto_dump", true);
                request.CloseGameAfterDump = ReadJsonBool(json, "close_game_after_dump", true);
                request.DumpDelaySeconds = ReadJsonFloat(json, "dump_delay_seconds", 4f);
                request.LoadTimeoutSeconds = ReadJsonFloat(json, "load_timeout_seconds", 20f);
                request.DumpTimeoutSeconds = ReadJsonFloat(json, "dump_timeout_seconds", 20f);
                return request;
            }

            private static string ReadJsonString(string json, string key)
            {
                string marker = "\"" + key + "\"";
                int keyIndex = json.IndexOf(marker, StringComparison.Ordinal);
                if (keyIndex < 0)
                {
                    return null;
                }

                int colon = json.IndexOf(':', keyIndex + marker.Length);
                if (colon < 0)
                {
                    return null;
                }

                int startQuote = json.IndexOf('"', colon + 1);
                if (startQuote < 0)
                {
                    return null;
                }

                int endQuote = startQuote + 1;
                while (endQuote < json.Length)
                {
                    if (json[endQuote] == '"' && json[endQuote - 1] != '\\')
                    {
                        break;
                    }

                    endQuote++;
                }

                if (endQuote >= json.Length)
                {
                    return null;
                }

                string raw = json.Substring(startQuote + 1, endQuote - startQuote - 1);
                return raw.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }

            private static bool ReadJsonBool(string json, string key, bool fallback)
            {
                string raw = ReadJsonRawValue(json, key);
                if (string.IsNullOrEmpty(raw))
                {
                    return fallback;
                }

                if (bool.TryParse(raw, out bool parsed))
                {
                    return parsed;
                }

                return fallback;
            }

            private static float ReadJsonFloat(string json, string key, float fallback)
            {
                string raw = ReadJsonRawValue(json, key);
                if (string.IsNullOrEmpty(raw))
                {
                    return fallback;
                }

                if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                {
                    return parsed;
                }

                return fallback;
            }

            private static string ReadJsonRawValue(string json, string key)
            {
                string marker = "\"" + key + "\"";
                int keyIndex = json.IndexOf(marker, StringComparison.Ordinal);
                if (keyIndex < 0)
                {
                    return null;
                }

                int colon = json.IndexOf(':', keyIndex + marker.Length);
                if (colon < 0)
                {
                    return null;
                }

                int start = colon + 1;
                while (start < json.Length && char.IsWhiteSpace(json[start]))
                {
                    start++;
                }

                int end = start;
                while (end < json.Length && json[end] != ',' && json[end] != '}' && !char.IsWhiteSpace(json[end]))
                {
                    end++;
                }

                return json.Substring(start, end - start).Trim();
            }
        }
    }
}
