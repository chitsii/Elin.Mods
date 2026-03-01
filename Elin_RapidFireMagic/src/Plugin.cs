using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_RapidFireMagic
{
    [BepInPlugin(ModGuid, "Rapid Fire Magic", "0.2.0")]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "tishi.elin_rapid_fire_magic";

        private const float HoldThreshold = 0.5f;

        private KeyCode _heldKey = KeyCode.None;
        private int _heldIndex = -1;
        private float _holdTimer;
        private float _pollTimer;

        private static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };

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
            // Don't pass bridge to Register — build UI manually to avoid
            // auto-build's stale-snapshot bug and left-side clipping
            var controller = ModOptionController.Register(ModGuid, "ModTooltip");
            bridge.SetTranslations(controller);
            controller.OnBuildUI += builder =>
            {
                // Fix left-side clipping: root VLayout has padding.left=0 by default.
                // LayoutRebuilder.ForceRebuildLayoutImmediate is required for the
                // padding change to take effect within the same frame.
                var rootVLG = builder.Root?.Base;

                // Toggle
                var toggle = builder.Root.AddToggle(
                    controller.Tr("EnableMod"), ModConfig.EnableMod.Value,
                    16, controller.Tr("EnableMod_tooltip"));
                toggle.OnValueChanged += v => { ModConfig.EnableMod.Value = v; };

                // Slider
                var slider = builder.Root.AddSlider(
                    controller.Tr("PollInterval") + " (" + ModConfig.PollInterval.Value + ")",
                    false, 0.01f, 0.5f, ModConfig.PollInterval.Value);
                slider.Step = 0.01f;
                slider.OnValueChanged += v =>
                {
                    ModConfig.PollInterval.Value = v;
                    slider.Title = controller.Tr("PollInterval") + " (" + ModConfig.PollInterval.Value + ")";
                };

                // Description
                builder.Root.AddText(controller.Tr("PollInterval_desc"), TextAnchor.MiddleLeft, 12);

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

        private void Update()
        {
            if (!ModConfig.EnableMod.Value)
                return;

            // Detect new number key press
            for (int i = 0; i < NumberKeys.Length; i++)
            {
                if (Input.GetKeyDown(NumberKeys[i]))
                {
                    _heldKey = NumberKeys[i];
                    _heldIndex = i;
                    _holdTimer = 0f;
                    return;
                }
            }

            // No key being tracked
            if (_heldKey == KeyCode.None)
                return;

            // Key released
            if (!Input.GetKey(_heldKey))
            {
                ResetState();
                return;
            }

            _holdTimer += Time.deltaTime;

            if (_holdTimer >= HoldThreshold)
            {
                _pollTimer -= Time.deltaTime;
                if (_pollTimer <= 0f)
                {
                    _pollTimer = ModConfig.PollInterval.Value;
                    TryRepeatCast();
                }
            }
        }

        private void TryRepeatCast()
        {
            if (EClass.core == null || EClass.core.game == null)
                return;
            if (!EClass.player.CanAcceptInput())
                return;

            var widget = WidgetCurrentTool.Instance;
            if (widget == null)
                return;
            if (_heldIndex < 0 || _heldIndex >= widget.list.buttons.Count)
                return;

            var buttonGrid = widget.list.buttons[_heldIndex].component as ButtonGrid;
            if (buttonGrid?.card == null)
                return;

            var traitAbility = buttonGrid.card.trait as TraitAbility;
            if (traitAbility == null || !traitAbility.CanUse(EClass.pc))
                return;

            if (traitAbility.OnUse(EClass.pc))
            {
                EClass.player.EndTurn();
            }
        }

        private void ResetState()
        {
            _heldKey = KeyCode.None;
            _heldIndex = -1;
            _holdTimer = 0f;
            _pollTimer = 0f;
        }

    }
}
