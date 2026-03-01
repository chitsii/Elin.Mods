using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_JumpAndBop
{
    [BepInPlugin(ModGuid, "Dynamic Jump & Bop", "0.1.0")]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "tishisylph.elin_jumpandbop";

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

        private void InitModOptions()
        {
            var bridge = new ModOptionsBridge();
            var controller = ModOptionController.Register(ModGuid, "ModTooltip");
            bridge.SetTranslations(controller);
            controller.OnBuildUI += builder =>
            {
                var rootVLG = builder.Root?.Base;

                // Toggle: EnableMod
                var enableMod = builder.Root.AddToggle(
                    controller.Tr("EnableMod"), ModConfig.EnableMod.Value,
                    16, controller.Tr("EnableMod_tooltip"));
                enableMod.OnValueChanged += v => { ModConfig.EnableMod.Value = v; };

                // Slider: JumpHeight
                string heightLabel(float v) =>
                    controller.Tr("JumpHeight") + " (" + v.ToString("F2") + ")";
                var jumpSlider = builder.Root.AddSlider(
                    heightLabel(ModConfig.JumpHeight.Value),
                    false, 0.01f, 0.10f, ModConfig.JumpHeight.Value);
                jumpSlider.Step = 0.005f;
                jumpSlider.OnValueChanged += v =>
                {
                    ModConfig.JumpHeight.Value = v;
                    jumpSlider.Title = heightLabel(v);
                };

                // Toggle: EnableOnWait
                var enableWait = builder.Root.AddToggle(
                    controller.Tr("EnableOnWait"), ModConfig.EnableOnWait.Value,
                    16, controller.Tr("EnableOnWait_tooltip"));
                enableWait.OnValueChanged += v => { ModConfig.EnableOnWait.Value = v; };

                // Toggle: EnableOnSex
                var enableSex = builder.Root.AddToggle(
                    controller.Tr("EnableOnSex"), ModConfig.EnableOnSex.Value,
                    16, controller.Tr("EnableOnSex_tooltip"));
                enableSex.OnValueChanged += v => { ModConfig.EnableOnSex.Value = v; };

                // Fix left-side clipping
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
