using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class ElinikkiSelfTestRunner : MonoBehaviour
    {
        private const string SaveGuardToken = "RUNTIME_TEST";
        private const string TriggerFileName = "run_selftest.flag";
        private static ElinikkiSelfTestRunner _instance;
        private bool _runStarted;

        public static void EnsureCreated()
        {
            if (_instance != null)
            {
                return;
            }

            ElinikkiSelfTestRunner existing = FindObjectOfType<ElinikkiSelfTestRunner>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            GameObject runnerObject = new GameObject("ElinikkiSelfTestRunner");
            DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            _instance = runnerObject.AddComponent<ElinikkiSelfTestRunner>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Update()
        {
            if (_runStarted)
            {
                return;
            }

            if (!ShouldRun())
            {
                return;
            }

            _runStarted = true;
            StartCoroutine(RunSelfTests());
        }

        private static bool ShouldRun()
        {
            if (!IsRuntimeReady())
            {
                return false;
            }

            if (Plugin.Settings?.RunSelfTestOnStartup?.Value == true)
            {
                return true;
            }

            return File.Exists(GetTriggerFilePath());
        }

        private static bool IsRuntimeReady()
        {
            return EClass.core != null
                && EClass.core.IsGameStarted
                && EClass.pc != null
                && EClass.game != null
                && EClass.scene != null
                && EClass._map != null;
        }

        private IEnumerator RunSelfTests()
        {
            yield return null;
            yield return new WaitForSeconds(0.5f);

            string resultPath = GetResultFilePath();
            string triggerPath = GetTriggerFilePath();
            if (File.Exists(triggerPath))
            {
                TryDeleteFile(triggerPath);
            }

            SelfTestRunResult run = new SelfTestRunResult
            {
                suite = "elinikki_selftest",
                startedUtc = DateTime.UtcNow.ToString("o"),
                pcName = EClass.pc?.Name ?? string.Empty
            };

            float startedRealtime = Time.realtimeSinceStartup;
            try
            {
                if (run.pcName.IndexOf(SaveGuardToken, StringComparison.Ordinal) < 0)
                {
                    run.status = "failed";
                    run.runFailureReason = "save_guard_rejected";
                    run.cases.Add(new SelfTestCaseResult
                    {
                        id = "elinikki.guard.player_name",
                        status = "failed",
                        reason = "Player name does not contain RUNTIME_TEST."
                    });
                    yield break;
                }

                yield return RunCase(run, "elinikki.plugin.boot.managers_available", VerifyManagersAvailable);
                yield return RunCase(run, "elinikki.shared_world.demo_spawned", VerifySharedWorldDemo);
                yield return RunCase(run, "elinikki.gpu_preview.render_texture_available", VerifyGpuPreview);
                yield return RunCase(run, "elinikki.shared_world.reload_reattaches_roots", VerifyReloadReattachesRoots);
            }
            finally
            {
                run.durationMs = ToMilliseconds(Time.realtimeSinceStartup - startedRealtime);
                if (run.status == "passed")
                {
                    for (int i = 0; i < run.cases.Count; i++)
                    {
                        if (!string.Equals(run.cases[i].status, "passed", StringComparison.Ordinal))
                        {
                            run.status = "failed";
                            if (string.IsNullOrEmpty(run.runFailureReason))
                            {
                                run.runFailureReason = "case_failed";
                            }
                            break;
                        }
                    }
                }

                WriteResult(resultPath, run);
                Plugin.Log.LogInfo($"Elinikki self-test finished: {run.status} -> {resultPath}");
            }
        }

        private static IEnumerator RunCase(SelfTestRunResult run, string id, Func<SelfTestCaseResult, IEnumerator> routine)
        {
            SelfTestCaseResult result = new SelfTestCaseResult
            {
                id = id
            };
            float start = Time.realtimeSinceStartup;
            IEnumerator iterator = null;
            Exception failure = null;

            try
            {
                iterator = routine(result);
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            if (failure == null && iterator != null)
            {
                while (true)
                {
                    object current = null;
                    bool moved;
                    try
                    {
                        moved = iterator.MoveNext();
                        if (moved)
                        {
                            current = iterator.Current;
                        }
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                        break;
                    }

                    if (!moved)
                    {
                        break;
                    }

                    yield return current;
                }
            }

            if (failure != null)
            {
                result.status = "failed";
                result.reason = failure.GetType().Name + ": " + failure.Message;
                result.logs.Add(failure.ToString());
            }

            result.durationMs = ToMilliseconds(Time.realtimeSinceStartup - start);
            run.cases.Add(result);
        }

        private static IEnumerator VerifyManagersAvailable(SelfTestCaseResult result)
        {
            FpsViewManager fpsManager = FpsViewManager.Instance;
            SharedWorldObjectManager sharedManager = SharedWorldObjectManager.Instance;

            Require(fpsManager != null, "FpsViewManager missing.");
            Require(sharedManager != null, "SharedWorldObjectManager missing.");
            Require(string.Equals(fpsManager.name, "ElinikkiManager", StringComparison.Ordinal), "Unexpected FpsViewManager object name.");
            Require(string.Equals(sharedManager.name, "ElinikkiSharedWorldManager", StringComparison.Ordinal), "Unexpected SharedWorldObjectManager object name.");

            result.logs.Add("Managers available.");
            yield break;
        }

        private static IEnumerator VerifySharedWorldDemo(SelfTestCaseResult result)
        {
            SharedWorldObjectManager sharedManager = SharedWorldObjectManager.Instance;
            Require(sharedManager != null, "SharedWorldObjectManager missing.");

            bool previousDream = Plugin.Settings.EnableDreamTestSet.Value;
            Plugin.Settings.EnableDreamTestSet.Value = true;
            try
            {
                sharedManager.ForceUpdateForTests();
                yield return null;

                GameObject normalRoot = sharedManager.NormalRootForTests;
                GameObject previewRoot = sharedManager.PreviewRootForTests;
                Transform previewParent = FpsViewManager.SharedWorldPreviewRoot;

                Require(sharedManager.DefinitionCountForTests >= 4, "Expected dream definitions to be registered.");
                Require(sharedManager.HandleCountForTests >= 4, "Expected dream handles to be created.");
                Require(normalRoot != null, "Normal root missing.");
                Require(previewRoot != null, "Preview root missing.");
                Require(EClass.scene != null && normalRoot.transform.parent == EClass.scene.transform, "Normal root parent mismatch.");
                Require(previewParent != null, "SharedWorldPreviewRoot missing.");
                Require(previewRoot.transform.parent == previewParent, "Preview root parent mismatch.");
                Require(normalRoot.transform.childCount >= 4, "Normal root has too few children.");
                Require(previewRoot.transform.childCount >= 4, "Preview root has too few children.");
                result.logs.Add("Shared-world demo objects verified.");
            }
            finally
            {
                Plugin.Settings.EnableDreamTestSet.Value = previousDream;
            }
        }

        private static IEnumerator VerifyGpuPreview(SelfTestCaseResult result)
        {
            FpsViewManager fpsManager = FpsViewManager.Instance;
            SharedWorldObjectManager sharedManager = SharedWorldObjectManager.Instance;
            Require(fpsManager != null, "FpsViewManager missing.");
            Require(sharedManager != null, "SharedWorldObjectManager missing.");

            bool previousDream = Plugin.Settings.EnableDreamTestSet.Value;
            bool wasVisible = fpsManager.IsOverlayVisibleForTests;
            Plugin.Settings.EnableDreamTestSet.Value = true;
            try
            {
                sharedManager.ForceUpdateForTests();
                fpsManager.ShowOverlayForTests();
                fpsManager.ForceRenderForTests();
                yield return null;
                yield return null;

                FpsGpuPreviewRenderer gpuRenderer = fpsManager.GpuPreviewRendererForTests;
                Require(gpuRenderer != null, "GPU preview renderer missing.");
                RenderTexture outputTexture = gpuRenderer.OutputTexture as RenderTexture;
                Require(outputTexture != null, "OutputTexture is null.");
                Require(outputTexture.width > 0, "RenderTexture width is invalid.");
                Require(outputTexture.height > 0, "RenderTexture height is invalid.");
                Require(outputTexture.IsCreated(), "RenderTexture is not created.");
                Transform sharedPreviewRoot = gpuRenderer.SharedWorldRoot;
                Require(sharedPreviewRoot != null, "SharedWorldRoot is null.");
                Require(sharedPreviewRoot.childCount > 0, "SharedWorldRoot has no children.");
                result.logs.Add("GPU preview availability verified.");
            }
            finally
            {
                Plugin.Settings.EnableDreamTestSet.Value = previousDream;
                if (!wasVisible)
                {
                    fpsManager.HideOverlayForTests();
                }
            }
        }

        private static IEnumerator VerifyReloadReattachesRoots(SelfTestCaseResult result)
        {
            FpsViewManager fpsManager = FpsViewManager.Instance;
            SharedWorldObjectManager sharedManager = SharedWorldObjectManager.Instance;
            Require(fpsManager != null, "FpsViewManager missing.");
            Require(sharedManager != null, "SharedWorldObjectManager missing.");

            string saveId = Game.id ?? string.Empty;
            bool saveCloud = EClass.game != null && EClass.game.isCloud;
            Require(!string.IsNullOrEmpty(saveId), "Game.id is empty.");

            bool previousDream = Plugin.Settings.EnableDreamTestSet.Value;
            bool wasVisible = fpsManager.IsOverlayVisibleForTests;
            Plugin.Settings.EnableDreamTestSet.Value = true;
            try
            {
                sharedManager.ForceUpdateForTests();
                fpsManager.ShowOverlayForTests();
                fpsManager.ForceRenderForTests();
                yield return null;
                yield return null;

                Game.Load(saveId, saveCloud);
                yield return WaitUntil(IsRuntimeReady, 900, "Reload timed out waiting for runtime.");
                yield return WaitFrames(20);

                fpsManager = FpsViewManager.Instance;
                sharedManager = SharedWorldObjectManager.Instance;
                Require(fpsManager != null, "FpsViewManager missing after reload.");
                Require(sharedManager != null, "SharedWorldObjectManager missing after reload.");

                sharedManager.ForceUpdateForTests();
                fpsManager.ShowOverlayForTests();
                fpsManager.ForceRenderForTests();
                yield return WaitFrames(10);

                GameObject normalRoot = sharedManager.NormalRootForTests;
                GameObject previewRoot = sharedManager.PreviewRootForTests;
                Transform sharedPreviewRoot = fpsManager.GpuPreviewRendererForTests != null
                    ? fpsManager.GpuPreviewRendererForTests.SharedWorldRoot
                    : null;

                Require(normalRoot != null, "Normal root missing after reload.");
                Require(previewRoot != null, "Preview root missing after reload.");
                Require(EClass.scene != null && normalRoot.transform.parent == EClass.scene.transform, "Normal root did not reattach to current scene.");
                Require(sharedPreviewRoot != null, "SharedWorldRoot missing after reload.");
                Require(previewRoot.transform.parent == sharedPreviewRoot, "Preview root did not reattach to GPU shared root.");
                Require(sharedManager.HandleCountForTests >= 4, "Shared handles missing after reload.");
                Require(normalRoot.transform.childCount >= 4, "Normal root children missing after reload.");
                Require(previewRoot.transform.childCount >= 4, "Preview root children missing after reload.");
                result.logs.Add("Shared-world reload reattachment verified.");
            }
            finally
            {
                Plugin.Settings.EnableDreamTestSet.Value = previousDream;
                if (!wasVisible && fpsManager != null)
                {
                    fpsManager.HideOverlayForTests();
                }
            }
        }

        private static IEnumerator WaitFrames(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitUntil(Func<bool> predicate, int maxFrames, string failureMessage)
        {
            for (int i = 0; i < maxFrames; i++)
            {
                if (predicate())
                {
                    yield break;
                }

                yield return null;
            }

            Require(predicate(), failureMessage);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void WriteResult(string path, SelfTestRunResult run)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllText(path, BuildJson(run), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to write self-test result: {ex}");
            }
        }

        private static string BuildJson(SelfTestRunResult run)
        {
            int passed = 0;
            int failed = 0;
            for (int i = 0; i < run.cases.Count; i++)
            {
                if (string.Equals(run.cases[i].status, "passed", StringComparison.Ordinal))
                {
                    passed++;
                }
                else
                {
                    failed++;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            AppendJsonProp(sb, "suite", run.suite);
            sb.Append(',');
            AppendJsonProp(sb, "status", run.status);
            sb.Append(',');
            AppendJsonProp(sb, "pc_name", run.pcName);
            sb.Append(',');
            AppendJsonProp(sb, "run_failure_reason", run.runFailureReason);
            sb.Append(',');
            AppendJsonProp(sb, "started_utc", run.startedUtc);
            sb.Append(',');
            AppendJsonProp(sb, "duration_ms", run.durationMs);
            sb.Append(',');
            sb.Append("\"summary\":{");
            AppendJsonProp(sb, "total", run.cases.Count);
            sb.Append(',');
            AppendJsonProp(sb, "passed", passed);
            sb.Append(',');
            AppendJsonProp(sb, "failed", failed);
            sb.Append('}');
            sb.Append(',');
            sb.Append("\"cases\":[");
            for (int i = 0; i < run.cases.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                SelfTestCaseResult c = run.cases[i];
                sb.Append('{');
                AppendJsonProp(sb, "id", c.id);
                sb.Append(',');
                AppendJsonProp(sb, "status", c.status);
                sb.Append(',');
                AppendJsonProp(sb, "reason", c.reason);
                sb.Append(',');
                AppendJsonProp(sb, "duration_ms", c.durationMs);
                sb.Append(',');
                sb.Append("\"logs\":[");
                for (int logIndex = 0; logIndex < c.logs.Count; logIndex++)
                {
                    if (logIndex > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append('"').Append(JsonEscape(c.logs[logIndex])).Append('"');
                }
                sb.Append("]}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendJsonProp(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":\"");
            sb.Append(JsonEscape(value ?? string.Empty)).Append('"');
        }

        private static void AppendJsonProp(StringBuilder sb, string key, int value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":");
            sb.Append(value);
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

        private static int ToMilliseconds(float seconds)
        {
            return seconds <= 0f ? 0 : (int)(seconds * 1000f);
        }

        private static string GetTriggerFilePath()
        {
            return Path.Combine(GetModDirectory(), TriggerFileName);
        }

        private static string GetResultFilePath()
        {
            return Path.Combine(GetModDirectory(), "selftest_result.json");
        }

        private static string GetModDirectory()
        {
            return Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? ".";
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to delete self-test trigger file: {ex.Message}");
            }
        }

        [Serializable]
        private sealed class SelfTestRunResult
        {
            public string suite = string.Empty;
            public string status = "passed";
            public string pcName = string.Empty;
            public string runFailureReason = string.Empty;
            public string startedUtc = string.Empty;
            public int durationMs;
            public List<SelfTestCaseResult> cases = new List<SelfTestCaseResult>();
        }

        [Serializable]
        private sealed class SelfTestCaseResult
        {
            public string id = string.Empty;
            public string status = "passed";
            public string reason = string.Empty;
            public int durationMs;
            public List<string> logs = new List<string>();
        }
    }
}
