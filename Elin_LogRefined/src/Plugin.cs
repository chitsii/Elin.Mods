using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_LogRefined
{
    [BepInPlugin(ModGuid, "Log Refined", "0.23.252")]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "elin_log_refined";

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

        private static void SyncToggle(OptionUIBuilder builder, string id, bool value)
        {
            var toggle = builder.GetPreBuild<OptToggle>(id);
            if (toggle != null)
                toggle.Checked = value;
        }

        private static void SyncDropdown(OptionUIBuilder builder, string id, int value)
        {
            var dropdown = builder.GetPreBuild<OptDropdown>(id);
            if (dropdown != null)
                dropdown.Value = value;
        }

        private static string ColorButtonLabel(string label, string hex)
        {
            return $"<color={hex}>\u2588\u2588</color> {label}";
        }

        private void InitModOptions()
        {
            var bridge = new ModOptionsBridge();
            var controller = ModOptionController.Register(ModGuid, "elin_log_refined", bridge);
            bridge.SetTranslations(controller);
            controller.OnBuildUI += builder =>
            {
                // Sync toggles with current config values
                SyncToggle(builder, "EnableModToggle", ModConfig.EnableMod.Value);
                SyncToggle(builder, "ShowDamageLogToggle", ModConfig.ShowDamageLog.Value);
                SyncToggle(builder, "ShowHealLogToggle", ModConfig.ShowHealLog.Value);
                SyncToggle(builder, "ShowConditionLogToggle", ModConfig.ShowConditionLog.Value);
                SyncToggle(builder, "ThrottleConditionLogToggle", ModConfig.ThrottleConditionLog.Value);
                SyncToggle(builder, "EnableCommentaryToggle", ModConfig.EnableCommentary.Value);
                SyncToggle(builder, "GenerateTemplatesToggle", ModConfig.GenerateTemplates.Value);

                // Sync dropdowns with current config values
                SyncDropdown(builder, "FormatModeDropdown", (int)ModConfig.FormatMode.Value);
                SyncDropdown(builder, "DetailLevelDropdown", (int)ModConfig.DetailLevel.Value);

                var themeDropdown = builder.GetPreBuild<OptDropdown>("ThemeDropdown");
                if (themeDropdown != null)
                {
                    themeDropdown.Value = (int)ModConfig.Theme.Value;
                    // Widen dropdown (~2x): label 11% / dropdown 89%
                    themeDropdown.PrefferedWidth = 2.0f;
                    var hlayout = ((Component)themeDropdown.Base).transform.parent;
                    var labelLE = hlayout.GetChild(0).GetComponent<LayoutElement>();
                    if (labelLE != null)
                        labelLE.flexibleWidth = 0.25f;
                }

                // --- Custom theme color picker (single section, 2-column grid) ---
                var colorSection = builder.Root.AddVLayoutWithBorder(controller.Tr("CustomTheme_Colors"), null);
                var colorSectionGO = ((Component)colorSection.Base).gameObject;

                var colorPairs = new[]
                {
                    (CustomColorSlot.DamageText, CustomColorSlot.HealText),
                    (CustomColorSlot.DebuffText, CustomColorSlot.BuffText),
                    (CustomColorSlot.CommentaryText, CustomColorSlot.SurfaceColor),
                    (CustomColorSlot.Default, CustomColorSlot.Talk),
                    (CustomColorSlot.Negative, CustomColorSlot.Ono),
                };

                foreach (var pair in colorPairs)
                {
                    var row = colorSection.AddHLayout();
                    AddColorButton(row, controller, pair.Item1);
                    AddColorButton(row, controller, pair.Item2);
                }

                // Show/hide section based on current theme
                bool isCustom = ModConfig.Theme.Value == LogTheme.Custom;
                colorSectionGO.SetActive(isCustom);

                // React to theme dropdown changes
                if (themeDropdown != null)
                {
                    themeDropdown.OnValueChanged += newValue =>
                    {
                        bool show = (LogTheme)newValue == LogTheme.Custom;
                        colorSectionGO.SetActive(show);
                    };
                }
            };
        }

        private static void AddColorButton(OptHLayout row, ModOptionController controller, CustomColorSlot slot)
        {
            var config = ModConfig.GetConfigEntry(slot);
            string label = controller.Tr("CustomColor_" + slot);
            string hex = config.Value;

            var button = row.AddButton(ColorButtonLabel(label, hex));
            button.Base.mainText.supportRichText = true;
            button.PrefferedWidth = 1.0f;

            // Reduce button height from default 52px to 40px
            var le = ((Component)button.Base).GetComponent<LayoutElement>();
            if (le != null) le.minHeight = 40;

            button.OnClicked += () =>
            {
                ColorUtility.TryParseHtmlString(config.Value, out Color startColor);
                ColorUtility.TryParseHtmlString(CustomThemeBuilder.DefaultHex[slot], out Color resetColor);

                EMono.ui.AddLayer<LayerColorPicker>().SetColor(startColor, resetColor, (state, color) =>
                {
                    if (state == PickerState.Confirm || state == PickerState.Reset)
                    {
                        string newHex = "#" + ColorUtility.ToHtmlStringRGB(color);
                        config.Value = newHex;
                        button.Text = ColorButtonLabel(label, newHex);
                    }
                });
            };
        }
    }
}
