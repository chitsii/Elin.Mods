using BepInEx;
using Elin_NiComment.Llm;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_NiComment
{
    [BepInPlugin(ModGuid, "NiComment", "0.1.0")]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "chitsii.elin_nicomment";

        private static readonly string[] TestComments =
        {
            "888888", "wwwww", "草", "ここすき", "GJ", "ktkr",
            "それな", "わかる", "うぽつ", "いいぞ", "すごい",
            "ナイス", "かわいい", "つよい", "神", "ワロタ"
        };

        private CommentOrchestrator _orchestrator;
        private LlmReactionService _llmService;
        private bool _initialized;

        private void Awake()
        {
            ModConfig.LoadConfig(Config);
            var harmony = new Harmony(ModGuid);
            harmony.PatchAll();
        }

        private void Start()
        {
            foreach (var obj in ModManager.ListPluginObject)
            {
                var plugin = obj as BaseUnityPlugin;
                if (plugin.Info.Metadata.GUID == "evilmask.elinplugins.modoptions")
                {
                    InitModOptions();
                    break;
                }
            }
        }

        private void Update()
        {
            if (!ModConfig.EnableMod.Value) return;

            if (!_initialized)
            {
                TryInitialize();
            }

            if (NiCommentAPI.IsReady && Input.GetKeyDown(KeyCode.F9))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (_llmService != null && _llmService.IsActive)
                    {
                        _llmService.EnqueueGameEvent("ロミアスがパンを食べた");
                        Logger.LogInfo("[NiComment] LLM test event sent.");
                    }
                    else
                    {
                        Logger.LogInfo("[NiComment] LLM not active.");
                    }
                }
                else
                {
                    var text = TestComments[Random.Range(0, TestComments.Length)];
                    NiCommentAPI.Send(text);
                }
            }
        }

        private void TryInitialize()
        {
            if (SkinManager.Instance == null) return;

            _orchestrator = CommentOrchestrator.Create();
            if (_orchestrator != null)
            {
                NiCommentAPI.Bind(_orchestrator);

                var trigger = _orchestrator.gameObject.AddComponent<CommentTrigger>();
                trigger.Initialize();

                if (LlmConfig.EnableLlm.Value)
                {
                    var provider = LlmProviderFactory.Create();
                    if (provider.IsAvailable)
                    {
                        _llmService = _orchestrator.gameObject.AddComponent<LlmReactionService>();
                        _llmService.Initialize(provider);
                    }
                    else
                    {
                        Logger.LogInfo("LLM enabled but provider not available (API key missing?).");
                    }
                }

                _initialized = true;
                Logger.LogInfo("NiComment overlay initialized.");
            }
        }

        private void OnDestroy()
        {
            NiCommentAPI.Unbind();
            CoroutineRunner.Cleanup();
            if (_orchestrator != null)
            {
                Destroy(_orchestrator.gameObject);
                _orchestrator = null;
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

                var speedSlider = builder.Root.AddSlider(
                    controller.Tr("ScrollSpeed") + " (" + ModConfig.ScrollSpeed.Value + ")",
                    false, 50f, 800f, ModConfig.ScrollSpeed.Value);
                speedSlider.Step = 10f;
                speedSlider.OnValueChanged += v =>
                {
                    ModConfig.ScrollSpeed.Value = v;
                    speedSlider.Title = controller.Tr("ScrollSpeed") + " (" + v + ")";
                };

                var fontSlider = builder.Root.AddSlider(
                    controller.Tr("FontSize") + " (" + ModConfig.FontSize.Value + ")",
                    false, 16f, 72f, ModConfig.FontSize.Value);
                fontSlider.Step = 1f;
                fontSlider.OnValueChanged += v =>
                {
                    ModConfig.FontSize.Value = (int)v;
                    fontSlider.Title = controller.Tr("FontSize") + " (" + (int)v + ")";
                };

                var laneSlider = builder.Root.AddSlider(
                    controller.Tr("MaxLanes") + " (" + ModConfig.MaxLanes.Value + ")",
                    false, 1f, 30f, ModConfig.MaxLanes.Value);
                laneSlider.Step = 1f;
                laneSlider.OnValueChanged += v =>
                {
                    ModConfig.MaxLanes.Value = (int)v;
                    laneSlider.Title = controller.Tr("MaxLanes") + " (" + (int)v + ")";
                };

                var poolSlider = builder.Root.AddSlider(
                    controller.Tr("PoolSize") + " (" + ModConfig.PoolSize.Value + ")",
                    false, 10f, 200f, ModConfig.PoolSize.Value);
                poolSlider.Step = 1f;
                poolSlider.OnValueChanged += v =>
                {
                    ModConfig.PoolSize.Value = (int)v;
                    poolSlider.Title = controller.Tr("PoolSize") + " (" + (int)v + ")";
                };

                var marginSlider = builder.Root.AddSlider(
                    controller.Tr("TopMargin") + " (" + ModConfig.TopMargin.Value + ")",
                    false, 0f, 200f, ModConfig.TopMargin.Value);
                marginSlider.Step = 5f;
                marginSlider.OnValueChanged += v =>
                {
                    ModConfig.TopMargin.Value = v;
                    marginSlider.Title = controller.Tr("TopMargin") + " (" + v + ")";
                };

                var llmToggle = builder.Root.AddToggle(
                    controller.Tr("EnableLlm"), LlmConfig.EnableLlm.Value,
                    16, controller.Tr("EnableLlm_tooltip"));
                llmToggle.OnValueChanged += v => { LlmConfig.EnableLlm.Value = v; };

                var batchSlider = builder.Root.AddSlider(
                    controller.Tr("BatchInterval") + " (" + LlmConfig.BatchInterval.Value + "s)",
                    false, 1f, 30f, LlmConfig.BatchInterval.Value);
                batchSlider.Step = 1f;
                batchSlider.OnValueChanged += v =>
                {
                    LlmConfig.BatchInterval.Value = v;
                    batchSlider.Title = controller.Tr("BatchInterval") + " (" + v + "s)";
                };

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
