using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_ModTemplate
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "chitsii.elin_justdoomit";
        public const string ModName = "Elin_JustDoomIt";
        public const string ModVersion = "0.23.254";

        private void Awake()
        {
            ModConfig.LoadConfig(Config);
            DoomDiagnostics.Initialize(Logger);
            DoomSessionManager.Ensure(Logger);
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

                var invincibleToggle = builder.Root.AddToggle(
                    controller.Tr("InvincibleMode"), ModConfig.InvincibleMode.Value,
                    16, controller.Tr("InvincibleMode_tooltip"));
                invincibleToggle.OnValueChanged += v => { ModConfig.InvincibleMode.Value = v; };

                var sensitivityTitle = controller.Tr("MouseTurnSensitivity") +
                    " (" + ModConfig.MouseTurnSensitivity.Value.ToString("0.0") + ")";
                var sensitivity = builder.Root.AddSlider(
                    sensitivityTitle,
                    false,
                    1f,
                    40f,
                    ModConfig.MouseTurnSensitivity.Value);
                sensitivity.Step = 0.5f;
                sensitivity.OnValueChanged += v =>
                {
                    ModConfig.MouseTurnSensitivity.Value = v;
                    sensitivity.Title = controller.Tr("MouseTurnSensitivity") +
                        " (" + v.ToString("0.0") + ")";
                };

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

        private void OnDestroy()
        {
            DoomSessionManager.Instance?.StopSession();
        }
    }
}
