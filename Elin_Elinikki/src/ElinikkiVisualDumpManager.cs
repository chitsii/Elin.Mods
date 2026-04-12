using System;
using System.Collections;
using System.IO;
using System.Text;
using BeautifyEffect;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class ElinikkiVisualDumpManager : MonoBehaviour
    {
        private static ElinikkiVisualDumpManager _instance;
        private bool _isCapturing;
        private string _lastDumpRoot;
        private string _lastFailure;

        public static void EnsureCreated()
        {
            if (_instance != null)
            {
                if (!_instance.gameObject.activeSelf)
                {
                    _instance.gameObject.SetActive(true);
                }

                return;
            }

            ElinikkiVisualDumpManager existing = FindObjectOfType<ElinikkiVisualDumpManager>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            GameObject managerObject = new GameObject("ElinikkiVisualDumpManager");
            DontDestroyOnLoad(managerObject);
            managerObject.hideFlags = HideFlags.HideAndDontSave;
            _instance = managerObject.AddComponent<ElinikkiVisualDumpManager>();
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

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (_isCapturing)
            {
                return;
            }

            if (Plugin.Settings == null || !Input.GetKeyDown(Plugin.Settings.VisualDumpKey.Value))
            {
                return;
            }

            if (!IsRuntimeReady())
            {
                Plugin.Log?.LogWarning("Visual dump skipped because the game runtime is not ready.");
                return;
            }

            StartCoroutine(CaptureRoutine());
        }

        internal bool IsCapturingForAutomation => _isCapturing;

        internal static ElinikkiVisualDumpManager InstanceForAutomation => _instance;

        internal string LastDumpRoot => _lastDumpRoot;

        internal string LastFailure => _lastFailure;

        internal bool TryCaptureForAutomation()
        {
            if (_isCapturing || !IsRuntimeReady())
            {
                return false;
            }

            StartCoroutine(CaptureRoutine());
            return true;
        }

        private IEnumerator CaptureRoutine()
        {
            _isCapturing = true;

            FpsViewManager fpsManager = FpsViewManager.Instance;
            SharedWorldObjectManager sharedManager = SharedWorldObjectManager.Instance;
            bool wasVisible = fpsManager != null && fpsManager.IsOverlayVisibleForTests;
            FpsViewState originalViewState = fpsManager != null ? fpsManager.CaptureViewStateForTests() : default;
            ScreenCaptureState screenState = CaptureScreenState();

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dumpRoot = Path.Combine(GetModDirectory(), "visual_dumps", timestamp);
            Directory.CreateDirectory(dumpRoot);

            string normalPath = Path.Combine(dumpRoot, "normal_view.png");
            string fpsOverlayPath = Path.Combine(dumpRoot, "fps_overlay.png");
            string fpsRtPath = Path.Combine(dumpRoot, "fps_rendertexture.png");
            string metaPath = Path.Combine(dumpRoot, "meta.json");
            Exception failure = null;

            try
            {
                if (sharedManager != null)
                {
                    sharedManager.ForceUpdateForTests();
                }

                ClearUiForCapture();
                PrepareViewForCapture(sharedManager, fpsManager);

                if (fpsManager != null)
                {
                    fpsManager.HideOverlayForTests();
                }

                yield return WaitForStableFrames(4);

                try
                {
                    using (SceneCaptureScope.Begin())
                    {
                        CaptureSceneCameraToPng(normalPath);
                    }
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                if (failure == null && fpsManager != null)
                {
                    fpsManager.ShowOverlayForTests();
                    PrepareViewForCapture(sharedManager, fpsManager);
                    fpsManager.ForceRenderForTests();
                }

                if (failure == null)
                {
                    if (fpsManager != null)
                    {
                        yield return WaitForStableFrames(2);
                    }

                    try
                    {
                        CaptureScreenToPng(fpsOverlayPath);
                        if (fpsManager != null && fpsManager.GpuPreviewRendererForTests != null)
                        {
                            SaveRenderTextureToPng(fpsManager.GpuPreviewRendererForTests.OutputTexture as RenderTexture, fpsRtPath);
                        }

                        WriteMetadata(metaPath, timestamp, fpsManager, sharedManager, normalPath, fpsOverlayPath, fpsRtPath, wasVisible);
                        Plugin.Log?.LogInfo($"Visual dump captured: {dumpRoot}");
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                }
            }
            finally
            {
                RestoreViewAfterCapture(fpsManager, originalViewState);
                RestoreScreenState(screenState);

                if (fpsManager != null)
                {
                    if (wasVisible)
                    {
                        fpsManager.ShowOverlayForTests();
                        fpsManager.ForceRenderForTests();
                    }
                    else
                    {
                        fpsManager.HideOverlayForTests();
                    }
                }

                if (failure != null)
                {
                    _lastFailure = failure.ToString();
                    Plugin.Log?.LogError($"Visual dump failed: {failure}");
                }
                else
                {
                    _lastFailure = null;
                    _lastDumpRoot = dumpRoot;
                }

                _isCapturing = false;
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

        private static IEnumerator WaitForStableFrames(int frameCount)
        {
            int count = Mathf.Max(1, frameCount);
            for (int i = 0; i < count; i++)
            {
                yield return null;
                yield return new WaitForEndOfFrame();
            }
        }

        private static void ClearUiForCapture()
        {
            try
            {
                if (EClass.ui != null)
                {
                    EClass.ui.RemoveLayers();
                    if (EClass.ui.layerFloat != null)
                    {
                        EClass.ui.layerFloat.RemoveLayers(removeImportant: true);
                    }
                }

                TooltipManager.Instance?.HideTooltips(immediate: true);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to clear UI before capture: {ex.Message}");
            }
        }

        private static void PrepareViewForCapture(SharedWorldObjectManager sharedManager, FpsViewManager fpsManager)
        {
            Vector3 centerTile;
            if (sharedManager != null && sharedManager.TryGetDefinitionCenterForTests(out centerTile))
            {
                Point focusPoint = new Point(
                    Mathf.Clamp(Mathf.RoundToInt(centerTile.x), 0, EClass._map.Size - 1),
                    Mathf.Clamp(Mathf.RoundToInt(centerTile.z), 0, EClass._map.Size - 1));
                EClass.screen?.FocusImmediate(focusPoint);

                if (fpsManager != null)
                {
                    float pitchOffset = ResolvePitchOffsetForTarget(centerTile);
                    fpsManager.AimAtTileForTests(centerTile, 0f, pitchOffset);
                    fpsManager.ForceRenderForTests();
                    AlignPreviewYawForCapture(fpsManager, centerTile, pitchOffset);
                }
            }
            else
            {
                EClass.screen?.FocusPC();
            }
        }

        private static float ResolvePitchOffsetForTarget(Vector3 centerTile)
        {
            if (EClass.pc == null)
            {
                return 0.02f;
            }

            Vector3 targetWorld = ResolvePreviewTargetWorld(centerTile);
            Vector2 pcOrigin = new Vector2(EClass.pc.pos.x + 0.5f, EClass.pc.pos.z + 0.5f);
            float surface = FpsIdealizedWorld.GetSurfaceHeightAt(pcOrigin);
            float eyeHeight = Plugin.Settings != null ? Plugin.Settings.EyeHeight.Value : 1.55f;
            Vector3 eye = new Vector3(pcOrigin.x, surface + eyeHeight, pcOrigin.y);
            Vector3 delta = targetWorld - eye;
            float horizontal = Mathf.Max(0.01f, new Vector2(delta.x, delta.z).magnitude);
            float pitchRadians = Mathf.Atan2(delta.y, horizontal);
            return Mathf.Clamp(pitchRadians / (Mathf.PI * 0.5f), -0.65f, 0.55f);
        }

        private static Vector3 ResolvePreviewTargetWorld(Vector3 centerTile)
        {
            float surface = FpsIdealizedWorld.GetSurfaceHeightAt(new Vector2(centerTile.x, centerTile.z));
            return new Vector3(centerTile.x, surface + centerTile.y, centerTile.z);
        }

        private static void AlignPreviewYawForCapture(FpsViewManager fpsManager, Vector3 centerTile, float pitchOffset)
        {
            Camera previewCamera = fpsManager.GpuPreviewRendererForTests?.PreviewCameraForTests;
            if (previewCamera == null)
            {
                return;
            }

            Vector3 targetWorld = ResolvePreviewTargetWorld(centerTile);
            FpsViewState baseline = fpsManager.CaptureViewStateForTests();
            float[] offsets = { 0f, Mathf.PI * 0.5f, Mathf.PI, Mathf.PI * 1.5f };
            float bestYaw = baseline.YawRadians;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < offsets.Length; i++)
            {
                FpsViewState candidate = baseline;
                candidate.YawRadians = baseline.YawRadians + offsets[i];
                candidate.PitchOffset = pitchOffset;
                fpsManager.ApplyViewStateForTests(candidate);
                fpsManager.ForceRenderForTests();

                previewCamera = fpsManager.GpuPreviewRendererForTests?.PreviewCameraForTests;
                if (previewCamera == null)
                {
                    continue;
                }

                Vector3 viewport = previewCamera.WorldToViewportPoint(targetWorld);
                float score = (viewport.z > 0f ? 1000f : -1000f)
                    - Mathf.Abs(viewport.x - 0.5f) * 100f
                    - Mathf.Abs(viewport.y - 0.5f) * 100f
                    + Mathf.Min(viewport.z, 20f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestYaw = candidate.YawRadians;
                }
            }

            FpsViewState best = baseline;
            best.YawRadians = bestYaw;
            best.PitchOffset = pitchOffset;
            fpsManager.ApplyViewStateForTests(best);
            fpsManager.ForceRenderForTests();
        }

        private static void RestoreViewAfterCapture(FpsViewManager fpsManager, FpsViewState originalViewState)
        {
            if (fpsManager == null)
            {
                return;
            }

            fpsManager.ApplyViewStateForTests(originalViewState);
            fpsManager.ForceRenderForTests();
        }

        private static void CaptureScreenToPng(string path)
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            Texture2D screenTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                screenTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                screenTexture.Apply(false, false);
                File.WriteAllBytes(path, screenTexture.EncodeToPNG());
            }
            finally
            {
                Destroy(screenTexture);
            }
        }

        private static void CaptureSceneCameraToPng(string path)
        {
            Camera camera = EClass.scene?.camSupport?.cam;
            if (camera == null)
            {
                CaptureScreenToPng(path);
                return;
            }

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                hideFlags = HideFlags.HideAndDontSave,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            Texture2D copy = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                copy.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                copy.Apply(false, false);
                File.WriteAllBytes(path, copy.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Destroy(copy);
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        private static void SaveRenderTextureToPng(RenderTexture renderTexture, string path)
        {
            if (renderTexture == null)
            {
                return;
            }

            RenderTexture previous = RenderTexture.active;
            Texture2D copy = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = renderTexture;
                copy.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0, false);
                copy.Apply(false, false);
                File.WriteAllBytes(path, copy.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Destroy(copy);
            }
        }

        private static void WriteMetadata(
            string path,
            string timestamp,
            FpsViewManager fpsManager,
            SharedWorldObjectManager sharedManager,
            string normalPath,
            string fpsOverlayPath,
            string fpsRtPath,
            bool wasVisible)
        {
            StringBuilder sb = new StringBuilder(4096);
            Camera normalCamera = EClass.scene?.camSupport?.cam;
            Camera previewCamera = fpsManager?.GpuPreviewRendererForTests?.PreviewCameraForTests;
            sb.Append('{');
            AppendJsonProp(sb, "timestamp", timestamp);
            sb.Append(',');
            AppendJsonProp(sb, "pc_name", EClass.pc?.Name ?? string.Empty);
            sb.Append(',');
            AppendJsonProp(sb, "save_id", Game.id ?? string.Empty);
            sb.Append(',');
            AppendJsonProp(sb, "normal_view_png", Path.GetFileName(normalPath));
            sb.Append(',');
            AppendJsonProp(sb, "fps_overlay_png", Path.GetFileName(fpsOverlayPath));
            sb.Append(',');
            AppendJsonProp(sb, "fps_rendertexture_png", Path.GetFileName(fpsRtPath));
            sb.Append(',');
            AppendJsonProp(sb, "overlay_was_visible", wasVisible ? "true" : "false");
            sb.Append(',');
            AppendJsonProp(sb, "overlay_is_visible_after_capture", fpsManager != null && fpsManager.IsOverlayVisibleForTests ? "true" : "false");
            sb.Append(',');
            AppendJsonProp(sb, "dream_test_enabled", Plugin.Settings != null && Plugin.Settings.EnableDreamTestSet.Value ? "true" : "false");
            sb.Append(',');
            AppendJsonProp(sb, "player_tile_x", EClass.pc != null ? EClass.pc.pos.x.ToString() : string.Empty);
            sb.Append(',');
            AppendJsonProp(sb, "player_tile_z", EClass.pc != null ? EClass.pc.pos.z.ToString() : string.Empty);
            sb.Append(',');
            AppendJsonProp(sb, "player_dir", EClass.pc != null ? EClass.pc.dir.ToString() : string.Empty);
            sb.Append(',');
            AppendPlayerProjection(sb, normalCamera);
            sb.Append(',');
            AppendJsonProp(sb, "shared_definition_count", sharedManager != null ? sharedManager.DefinitionCountForTests.ToString() : "0");
            sb.Append(',');
            AppendJsonProp(sb, "shared_handle_count", sharedManager != null ? sharedManager.HandleCountForTests.ToString() : "0");
            sb.Append(',');
            AppendJsonProp(sb, "normal_root_children", sharedManager != null && sharedManager.NormalRootForTests != null ? sharedManager.NormalRootForTests.transform.childCount.ToString() : "0");
            sb.Append(',');
            AppendJsonProp(sb, "preview_root_children", sharedManager != null && sharedManager.PreviewRootForTests != null ? sharedManager.PreviewRootForTests.transform.childCount.ToString() : "0");
            sb.Append(',');
            AppendJsonProp(sb, "scene_capture_mode", "scene_camera_direct");
            sb.Append(',');
            AppendJsonProp(sb, "preview_capture_mode", "screen_plus_rendertexture");
            if (sharedManager != null)
            {
                sb.Append(',');
                sb.Append("\"objects\":");
                sb.Append(sharedManager.BuildDebugMetricsJsonForTests(normalCamera, previewCamera));
            }

            sb.Append('}');
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static void AppendPlayerProjection(StringBuilder sb, Camera normalCamera)
        {
            Rect rect;
            bool hasRect = TryGetPlayerScreenRect(normalCamera, out rect);
            AppendJsonProp(sb, "player_has_projected_rect", hasRect ? "true" : "false");
            sb.Append(',');
            sb.Append("\"player_projected_rect\":");
            AppendJsonRect(sb, rect);
            sb.Append(',');
            AppendPlayerActorMetrics(sb);
        }

        private static void AppendPlayerActorMetrics(StringBuilder sb)
        {
            Vector3 actorWorld = Vector3.zero;
            Vector3 spriteLocal = Vector3.zero;
            Vector3 spriteBounds = Vector3.zero;
            SpriteRenderer spriteRenderer = EClass.pc?.renderer?.actor?.sr;
            if (spriteRenderer != null)
            {
                actorWorld = spriteRenderer.transform.position;
                spriteLocal = spriteRenderer.transform.localPosition;
                spriteBounds = spriteRenderer.bounds.size;
            }

            sb.Append("\"player_actor_world_position\":");
            AppendJsonVector3(sb, actorWorld);
            sb.Append(',');
            sb.Append("\"player_sprite_local_position\":");
            AppendJsonVector3(sb, spriteLocal);
            sb.Append(',');
            sb.Append("\"player_sprite_bounds_size\":");
            AppendJsonVector3(sb, spriteBounds);
        }

        private static bool TryGetPlayerScreenRect(Camera camera, out Rect rect)
        {
            rect = default;
            if (camera == null || EClass.pc == null || EClass.pc.renderer == null || EClass.pc.renderer.actor == null)
            {
                return false;
            }

            CardActor actor = EClass.pc.renderer.actor;
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            bool hasRect = false;
            Rect combined = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || renderer.sprite == null || !renderer.enabled)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                bool hasAny = false;

                for (int ix = -1; ix <= 1; ix += 2)
                {
                    for (int iy = -1; iy <= 1; iy += 2)
                    {
                        for (int iz = -1; iz <= 1; iz += 2)
                        {
                            Vector3 world = center + Vector3.Scale(extents, new Vector3(ix, iy, iz));
                            Vector3 screen = camera.WorldToScreenPoint(world);
                            if (screen.z <= 0f)
                            {
                                continue;
                            }

                            hasAny = true;
                            minX = Mathf.Min(minX, screen.x);
                            minY = Mathf.Min(minY, screen.y);
                            maxX = Mathf.Max(maxX, screen.x);
                            maxY = Mathf.Max(maxY, screen.y);
                        }
                    }
                }

                if (!hasAny)
                {
                    continue;
                }

                Rect current = Rect.MinMaxRect(minX, minY, maxX, maxY);
                if (!hasRect)
                {
                    combined = current;
                    hasRect = true;
                }
                else
                {
                    combined = Rect.MinMaxRect(
                        Mathf.Min(combined.xMin, current.xMin),
                        Mathf.Min(combined.yMin, current.yMin),
                        Mathf.Max(combined.xMax, current.xMax),
                        Mathf.Max(combined.yMax, current.yMax));
                }
            }

            rect = combined;
            return hasRect;
        }

        private static void AppendJsonRect(StringBuilder sb, Rect rect)
        {
            sb.Append('{');
            AppendJsonProp(sb, "x", rect.x.ToString("F4"));
            sb.Append(',');
            AppendJsonProp(sb, "y", rect.y.ToString("F4"));
            sb.Append(',');
            AppendJsonProp(sb, "width", rect.width.ToString("F4"));
            sb.Append(',');
            AppendJsonProp(sb, "height", rect.height.ToString("F4"));
            sb.Append('}');
        }

        private static void AppendJsonVector3(StringBuilder sb, Vector3 value)
        {
            sb.Append('{');
            AppendJsonProp(sb, "x", value.x.ToString("F4"));
            sb.Append(',');
            AppendJsonProp(sb, "y", value.y.ToString("F4"));
            sb.Append(',');
            AppendJsonProp(sb, "z", value.z.ToString("F4"));
            sb.Append('}');
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

        private static ScreenCaptureState CaptureScreenState()
        {
            ScreenCaptureState state = new ScreenCaptureState
            {
                ScreenPosition = EClass.screen != null ? EClass.screen.position : Vector3.zero,
                BlurActive = EClass.ui != null && EClass.ui.blur != null && EClass.ui.blur.activeSelf
            };

            CameraSupport support = EClass.scene?.camSupport;
            if (support != null)
            {
                state.HasCameraSupport = true;
                state.TiltShiftEnabled = support.tiltShift != null && support.tiltShift.enabled;
                state.BlurEnabled = support.blur != null && support.blur.enabled;
                state.BloomEnabled = support.bloom != null && support.bloom.enabled;
                state.KuwaharaEnabled = support.kuwahara != null && support.kuwahara.enabled;
                state.BeautifyEnabled = support.beautify != null && support.beautify.enabled;
            }

            return state;
        }

        private static void RestoreScreenState(ScreenCaptureState state)
        {
            if (EClass.screen != null)
            {
                EClass.screen.position = state.ScreenPosition;
                EClass.screen.RefreshPosition();
            }

            if (EClass.ui != null && EClass.ui.blur != null)
            {
                EClass.ui.blur.SetActive(state.BlurActive);
            }

            CameraSupport support = EClass.scene?.camSupport;
            if (support == null || !state.HasCameraSupport)
            {
                return;
            }

            if (support.tiltShift != null)
            {
                support.tiltShift.enabled = state.TiltShiftEnabled;
            }

            if (support.blur != null)
            {
                support.blur.enabled = state.BlurEnabled;
            }

            if (support.bloom != null)
            {
                support.bloom.enabled = state.BloomEnabled;
            }

            if (support.kuwahara != null)
            {
                support.kuwahara.enabled = state.KuwaharaEnabled;
            }

            if (support.beautify != null)
            {
                support.beautify.enabled = state.BeautifyEnabled;
            }
        }

        private static string GetModDirectory()
        {
            return Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? ".";
        }

        private readonly struct SceneCaptureScope : IDisposable
        {
            private readonly bool _hasCameraSupport;
            private readonly bool _tiltShiftEnabled;
            private readonly bool _blurEnabled;
            private readonly bool _bloomEnabled;
            private readonly bool _kuwaharaEnabled;
            private readonly bool _beautifyEnabled;
            private readonly bool _blurGameObjectActive;

            private SceneCaptureScope(
                bool hasCameraSupport,
                bool tiltShiftEnabled,
                bool blurEnabled,
                bool bloomEnabled,
                bool kuwaharaEnabled,
                bool beautifyEnabled,
                bool blurGameObjectActive)
            {
                _hasCameraSupport = hasCameraSupport;
                _tiltShiftEnabled = tiltShiftEnabled;
                _blurEnabled = blurEnabled;
                _bloomEnabled = bloomEnabled;
                _kuwaharaEnabled = kuwaharaEnabled;
                _beautifyEnabled = beautifyEnabled;
                _blurGameObjectActive = blurGameObjectActive;
            }

            public static SceneCaptureScope Begin()
            {
                CameraSupport support = EClass.scene?.camSupport;
                bool blurGameObjectActive = EClass.ui != null && EClass.ui.blur != null && EClass.ui.blur.activeSelf;
                if (EClass.ui != null && EClass.ui.blur != null)
                {
                    EClass.ui.blur.SetActive(false);
                }

                if (support == null)
                {
                    return new SceneCaptureScope(false, false, false, false, false, false, blurGameObjectActive);
                }

                bool tiltShiftEnabled = support.tiltShift != null && support.tiltShift.enabled;
                bool blurEnabled = support.blur != null && support.blur.enabled;
                bool bloomEnabled = support.bloom != null && support.bloom.enabled;
                bool kuwaharaEnabled = support.kuwahara != null && support.kuwahara.enabled;
                bool beautifyEnabled = support.beautify != null && support.beautify.enabled;

                if (support.tiltShift != null)
                {
                    support.tiltShift.enabled = false;
                }

                if (support.blur != null)
                {
                    support.blur.enabled = false;
                }

                if (support.bloom != null)
                {
                    support.bloom.enabled = false;
                }

                if (support.kuwahara != null)
                {
                    support.kuwahara.enabled = false;
                }

                if (support.beautify != null)
                {
                    support.beautify.enabled = false;
                }

                return new SceneCaptureScope(
                    true,
                    tiltShiftEnabled,
                    blurEnabled,
                    bloomEnabled,
                    kuwaharaEnabled,
                    beautifyEnabled,
                    blurGameObjectActive);
            }

            public void Dispose()
            {
                CameraSupport support = EClass.scene?.camSupport;
                if (_hasCameraSupport && support != null)
                {
                    if (support.tiltShift != null)
                    {
                        support.tiltShift.enabled = _tiltShiftEnabled;
                    }

                    if (support.blur != null)
                    {
                        support.blur.enabled = _blurEnabled;
                    }

                    if (support.bloom != null)
                    {
                        support.bloom.enabled = _bloomEnabled;
                    }

                    if (support.kuwahara != null)
                    {
                        support.kuwahara.enabled = _kuwaharaEnabled;
                    }

                    if (support.beautify != null)
                    {
                        support.beautify.enabled = _beautifyEnabled;
                    }
                }

                if (EClass.ui != null && EClass.ui.blur != null)
                {
                    EClass.ui.blur.SetActive(_blurGameObjectActive);
                }
            }
        }

        private struct ScreenCaptureState
        {
            public bool HasCameraSupport;
            public bool TiltShiftEnabled;
            public bool BlurEnabled;
            public bool BloomEnabled;
            public bool KuwaharaEnabled;
            public bool BeautifyEnabled;
            public bool BlurActive;
            public Vector3 ScreenPosition;
        }
    }
}
