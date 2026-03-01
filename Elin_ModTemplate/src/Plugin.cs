using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_ModTemplate
{
    [BepInPlugin(ModGuid, "My Mod Name", "0.1.0")]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        // TODO: Change this GUID to match your mod
        public const string ModGuid = "yourname.elin_mod_template";

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
            // Don't pass bridge to Register -- build UI manually to avoid
            // auto-build's stale-snapshot bug and left-side clipping
            var controller = ModOptionController.Register(ModGuid, "ModTooltip");
            bridge.SetTranslations(controller);
            controller.OnBuildUI += builder =>
            {
                // Fix left-side clipping: root VLayout has padding.left=0 by default.
                // LayoutRebuilder.ForceRebuildLayoutImmediate is required for the
                // padding change to take effect within the same frame.
                var rootVLG = builder.Root?.Base;

                // Toggle: EnableMod
                var toggle = builder.Root.AddToggle(
                    controller.Tr("EnableMod"), ModConfig.EnableMod.Value,
                    16, controller.Tr("EnableMod_tooltip"));
                toggle.OnValueChanged += v => { ModConfig.EnableMod.Value = v; };

                // TODO: Add your ModOptions UI controls here
                // Example slider:
                // var slider = builder.Root.AddSlider(
                //     controller.Tr("MySlider") + " (" + ModConfig.MyValue.Value + ")",
                //     false, 0f, 100f, ModConfig.MyValue.Value);
                // slider.Step = 1f;
                // slider.OnValueChanged += v =>
                // {
                //     ModConfig.MyValue.Value = v;
                //     slider.Title = controller.Tr("MySlider") + " (" + v + ")";
                // };

                // Apply left/right padding and force layout recalculation
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

        // TODO: Add your logic here (e.g. Update, OnDestroy, etc.)
    }
}
