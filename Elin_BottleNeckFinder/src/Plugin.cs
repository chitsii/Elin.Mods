using BepInEx;
using UnityEngine;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine.UI;

namespace Elin_BottleNeckFinder
{
    [BepInPlugin(ModGuid, "BottleNeckFinder", "0.2.0")]
    [BepInDependency("evilmask.elinplugins.modoptions")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "tishi.elin_bottleneck_finder";

        private bool _profilerActive;
        private bool _initialized;
        private int _frameCount;

        private float _fps;
        private float _frameMs;
        private float _fpsTimer;
        private int _fpsFrames;

        public static float ProfilingStartTime { get; private set; }

        private void Awake()
        {
            ModConfig.LoadConfig(Config);
        }

        private void Start()
        {
            InitModOptions();
            PatchRegistry.Build();
            ErrorMonitor.Start(ModConfig.MaxErrorHistory.Value);
            _initialized = true;

            Debug.Log("[BNF] BottleNeckFinder initialized");
        }

        private void Update()
        {
            if (!ModConfig.EnableMod.Value || !_initialized) return;

            // FPS calculation
            _fpsFrames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _fps = _fpsFrames / _fpsTimer;
                _fpsTimer = 0f;
                _fpsFrames = 0;
            }
            _frameMs = Time.unscaledDeltaTime * 1000f;

            // Profiling per frame
            if (_profilerActive)
            {
                _frameCount++;
                if (_frameCount >= 1000000) _frameCount = 0;  // prevent overflow
            }
        }

        private void LateUpdate()
        {
            if (!ModConfig.EnableMod.Value || !_initialized) return;
            if (!_profilerActive) return;

            if (_frameCount % ModConfig.SampleInterval.Value == 0)
            {
                ProfilingData.Update();          // Read data FIRST
                HarmonyProfiler.BeginFrame();    // Then clear for next cycle
                UpdateProfiler.BeginFrame();
            }
        }

        private void OnGUI()
        {
            if (!ModConfig.EnableMod.Value || !_initialized) return;
            if (!ModConfig.ShowOverlay.Value) return;

            bool toggle = OverlayRenderer.Draw(
                _fps, _frameMs,
                ProfilingData.Ranking,
                ErrorMonitor.Errors,
                PatchRegistry.TotalPatches,
                ErrorMonitor.PatchFailureCount,
                _profilerActive,
                ModConfig.TopModCount.Value
            );
            if (toggle) ToggleProfiler();
        }

        private void OnDestroy()
        {
            ErrorMonitor.Stop();
            if (_profilerActive)
            {
                HarmonyProfiler.Stop();
                UpdateProfiler.Stop();
            }
        }

        private void ToggleProfiler()
        {
            try
            {
                if (_profilerActive)
                {
                    HarmonyProfiler.Stop();
                    UpdateProfiler.Stop();
                    ProfilingData.Clear();
                    _profilerActive = false;
                }
                else
                {
                    HarmonyProfiler.Start();
                    UpdateProfiler.Start();
                    ProfilingStartTime = Time.realtimeSinceStartup;
                    _profilerActive = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BNF] ToggleProfiler failed: {ex.Message}");
            }
        }

        private void InitModOptions()
        {
            var bridge = new ModOptionsBridge();
            var controller = ModOptionController.Register(ModGuid, "ModTooltip");
            bridge.SetTranslations(controller);
            controller.OnBuildUI += builder =>
            {
                var rootVLG = builder.Root?.Base;

                var toggle = builder.Root.AddToggle(
                    controller.Tr("EnableMod"), ModConfig.EnableMod.Value,
                    16, controller.Tr("EnableMod_tooltip"));
                toggle.OnValueChanged += v => { ModConfig.EnableMod.Value = v; };

                var overlayToggle = builder.Root.AddToggle(
                    controller.Tr("ShowOverlay"),
                    ModConfig.ShowOverlay.Value,
                    16, controller.Tr("ShowOverlay_tooltip"));
                overlayToggle.OnValueChanged += v =>
                    { ModConfig.ShowOverlay.Value = v; };

                if (rootVLG != null)
                {
                    rootVLG.padding = new RectOffset(
                        30, 10, rootVLG.padding.top, rootVLG.padding.bottom);
                    var contentRect = rootVLG.GetComponent<RectTransform>();
                    if (contentRect != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                }
            };
        }
    }
}
