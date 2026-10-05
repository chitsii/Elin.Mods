using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_ArsMoriendi
{
    [BepInPlugin(ModGuid, "Ars Moriendi", "0.1.1")]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string TomeItemId = "ars_moriendi_tome";
        private static readonly KeyCode[] TomeHotkeyCandidates =
        {
            KeyCode.None,
            KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G,
            KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N,
            KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U,
            KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12,
        };

        public const string ModGuid = "chitsii.elin.ars_moriendi";
        private static readonly List<Type> FailedPatchTypes = new();
        private static readonly HashSet<Type> CriticalPatchTypes = new()
        {
            typeof(Patch_Chara_Die_SoulDrop),
            typeof(Patch_Chara_Die_ApotheosisSoulHarvest),
            typeof(Patch_Chara_Die_PreserveCorpse),
            typeof(Patch_Chara_Die_SoulBind),
            typeof(Patch_Chara_Die_QuestNPC),
        };
        private static readonly Dictionary<string, int> RuntimePatchFailureCounts = new(StringComparer.Ordinal);
        private static readonly HashSet<string> CriticalRuntimePatchNames = new(StringComparer.Ordinal)
        {
            nameof(Patch_Chara_Die_SoulDrop),
            nameof(Patch_Chara_Die_ApotheosisSoulHarvest),
            nameof(Patch_Chara_Die_PreserveCorpse),
            nameof(Patch_Chara_Die_SoulBind),
            nameof(Patch_Chara_Die_QuestNPC),
        };

        private bool _patchFailureNoticeShown;

        public static bool HasAnyPatchFailures => FailedPatchTypes.Count > 0;

        public static bool HasCriticalPatchFailures =>
            FailedPatchTypes.Any(type => CriticalPatchTypes.Contains(type));

        public static bool HasAnyRuntimePatchFailures => RuntimePatchFailureCounts.Count > 0;

        public static bool HasCriticalRuntimePatchFailures =>
            RuntimePatchFailureCounts.Keys.Any(name => CriticalRuntimePatchNames.Contains(name));

        public static string GetPatchFailureSummary(int maxItems = 4)
        {
            if (FailedPatchTypes.Count == 0) return string.Empty;
            int take = Math.Max(1, maxItems);
            var shown = FailedPatchTypes.Take(take).Select(t => t.Name).ToArray();
            string suffix = FailedPatchTypes.Count > shown.Length ? $" +{FailedPatchTypes.Count - shown.Length}" : string.Empty;
            return string.Join(", ", shown) + suffix;
        }

        public static string GetRuntimePatchFailureSummary(int maxItems = 4)
        {
            if (RuntimePatchFailureCounts.Count == 0) return string.Empty;
            int take = Math.Max(1, maxItems);
            var shown = RuntimePatchFailureCounts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Take(take)
                .Select(kv => $"{kv.Key} x{kv.Value}")
                .ToArray();
            string suffix = RuntimePatchFailureCounts.Count > shown.Length ? $" +{RuntimePatchFailureCounts.Count - shown.Length}" : string.Empty;
            return string.Join(", ", shown) + suffix;
        }

        public static void ReportPatchRuntimeFailure(string patchName)
        {
            if (string.IsNullOrEmpty(patchName)) return;

            RuntimePatchFailureCounts.TryGetValue(patchName, out int count);
            RuntimePatchFailureCounts[patchName] = count + 1;
        }

        private void Awake()
        {
#if DEBUG
            ModLog.Warn("DEBUG build plugin loaded");
#endif
            // Mod アセンブリを ClassCache に登録
            // ApplyTrait() が ClassCache 経由でカスタム Trait 型を解決するために必要
            ClassCache.assemblies.Add(Assembly.GetExecutingAssembly().GetName().Name);

            ModConfig.LoadConfig(Config);
            CompatBootstrap.Initialize();
            NecromancyManager.Instance.Init();
            Elin_CommonDrama.DramaRuntime.ConfigureResolver(
                new ArsDramaResolver(new GameArsDramaRuntimeContext()));
            var harmony = new Harmony(ModGuid);
            ApplyHarmonyPatchesFailSoft(harmony);
        }

        private static void ApplyHarmonyPatchesFailSoft(Harmony harmony)
        {
            int success = 0;
            int failed = 0;
            FailedPatchTypes.Clear();
            RuntimePatchFailureCounts.Clear();
            var patchTypes = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0);

            foreach (var type in patchTypes)
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    FailedPatchTypes.Add(type);
                    ModLog.Error($"Failed to apply patch class {type.FullName}: {ex.Message}");
                }
            }

            if (failed > 0)
            {
                ModLog.Error($"Harmony patching completed with failures: {success} succeeded, {failed} failed.");
            }
        }

        private void Start()
        {
            try
            {
                InitModOptions();
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Mod Options UI setup skipped: {ex.GetType().Name}");
            }
        }

        private void Update()
        {
            if (ArsMoriendiGUI.IsVisible)
                EInput.haltInput = true;

            if (ShouldToggleTomeUiHotkey())
            {
                if (ArsMoriendiGUI.IsVisible)
                    ArsMoriendiGUI.Hide();
                else
                    ArsMoriendiGUI.ShowServants();

                EInput.Consume(true, 2);
                return;
            }

            if (!_patchFailureNoticeShown
                && (HasCriticalPatchFailures || HasCriticalRuntimePatchFailures)
                && TryGetPlayerChara(out _))
            {
                _patchFailureNoticeShown = true;
                Msg.Say(
                    Lang.isJP
                        ? "Ars Moriendi: 重要パッチで障害を検知しました。ログを確認してください。"
                        : (Lang.langCode == "CN"
                            ? "Ars Moriendi：检测到关键补丁异常。请检查日志。"
                            : "Ars Moriendi: Critical patch issue detected. Check logs.")
                );
            }
        }

        private void OnGUI()
        {
            try
            {
                ArsMoriendiGUI.Draw();
            }
            catch (Exception ex)
            {
                ModLog.Error($"ArsMoriendiGUI draw failed: {ex}");
                ArsMoriendiGUI.Hide();
            }

            try
            {
                ServantStatusGUI.Draw();
            }
            catch (Exception ex)
            {
                ModLog.Error($"ServantStatusGUI draw failed: {ex}");
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
                var keyTexts = TomeHotkeyCandidates.Select(GetHotkeyKeyLabel).ToList();

                void AddSectionHeader(string translationKey)
                {
                    var header = builder.Root!.AddText(controller.Tr(translationKey), TextAnchor.MiddleLeft, 15);
                    header.PrefferedWidth = 1f;
                }

                AddSectionHeader("ModSectionServants");

                var servantAuraToggle = builder.Root!.AddToggle(
                    controller.Tr("ShowServantAura"), ModConfig.ShowServantAura.Value,
                    16, controller.Tr("ShowServantAura_tooltip"));
                servantAuraToggle.OnValueChanged += v =>
                {
                    ModConfig.ShowServantAura.Value = v;
                    NecromancyManager.Instance.RefreshServantVisualStateCurrentZone();
                };

                var stashedContributionToggle = builder.Root!.AddToggle(
                    controller.Tr("EnableStashedServantHomeContribution"), ModConfig.EnableStashedServantHomeContribution.Value,
                    16, controller.Tr("EnableStashedServantHomeContribution_tooltip"));
                stashedContributionToggle.OnValueChanged += v =>
                {
                    ModConfig.EnableStashedServantHomeContribution.Value = v;
                    NecromancyManager.Instance.ReconcileServantRuntimeStates();
                };

                AddSectionHeader("ModSectionHotkey");

                var hotkeyRow = builder.Root.AddHLayout();
                hotkeyRow.Base.childScaleWidth = true;
                var hotkeyLabel = hotkeyRow.AddText(controller.Tr("TomeHotkeyKey"), TextAnchor.MiddleLeft, 14);
                hotkeyLabel.PrefferedWidth = 0.5f;
                var hotkeyDropdown = hotkeyRow.AddDropdown(keyTexts, Array.IndexOf(TomeHotkeyCandidates, ModConfig.TomeHotkeyKey.Value));
                hotkeyDropdown.PrefferedWidth = 0.5f;
                hotkeyDropdown.OnValueChanged += index =>
                {
                    if (index >= 0 && index < TomeHotkeyCandidates.Length)
                        ModConfig.TomeHotkeyKey.Value = TomeHotkeyCandidates[index];
                };

                var modifierRow = builder.Root.AddHLayout();
                modifierRow.Base.childScaleWidth = true;
                var modifierLabel = modifierRow.AddText(controller.Tr("TomeHotkeyModifiers"), TextAnchor.MiddleLeft, 14);
                modifierLabel.PrefferedWidth = 0.32f;

                var shiftToggle = modifierRow.AddToggle(
                    controller.Tr("TomeHotkeyShift"), ModConfig.TomeHotkeyShift.Value,
                    16, controller.Tr("TomeHotkeyShift_tooltip"));
                shiftToggle.PrefferedWidth = 0.22f;
                shiftToggle.OnValueChanged += v => { ModConfig.TomeHotkeyShift.Value = v; };

                var ctrlToggle = modifierRow.AddToggle(
                    controller.Tr("TomeHotkeyCtrl"), ModConfig.TomeHotkeyCtrl.Value,
                    16, controller.Tr("TomeHotkeyCtrl_tooltip"));
                ctrlToggle.PrefferedWidth = 0.22f;
                ctrlToggle.OnValueChanged += v => { ModConfig.TomeHotkeyCtrl.Value = v; };

                var altToggle = modifierRow.AddToggle(
                    controller.Tr("TomeHotkeyAlt"), ModConfig.TomeHotkeyAlt.Value,
                    16, controller.Tr("TomeHotkeyAlt_tooltip"));
                altToggle.PrefferedWidth = 0.22f;
                altToggle.OnValueChanged += v => { ModConfig.TomeHotkeyAlt.Value = v; };

                AddSectionHeader("ModSectionOther");

                var uiCompatToggle = builder.Root!.AddToggle(
                    controller.Tr("EnableUiCompatibilityMode"), ModConfig.EnableUiCompatibilityMode.Value,
                    16, controller.Tr("EnableUiCompatibilityMode_tooltip"));
                uiCompatToggle.OnValueChanged += v => { ModConfig.EnableUiCompatibilityMode.Value = v; };

                var apotheosisBonusToggle = builder.Root!.AddToggle(
                    controller.Tr("EnableApotheosisStatBonuses"), ModConfig.EnableApotheosisStatBonuses.Value,
                    16, controller.Tr("EnableApotheosisStatBonuses_tooltip"));
                apotheosisBonusToggle.OnValueChanged += v =>
                {
                    ModConfig.EnableApotheosisStatBonuses.Value = v;
                    if (TryGetPlayerChara(out Chara playerChara))
                        ApotheosisFeatBonus.SyncWithConfigAndFeat(playerChara);
                };

                var debugToggle = builder.Root!.AddToggle(
                    controller.Tr("DebugMode"), ModConfig.DebugMode.Value,
                    16, controller.Tr("DebugMode_tooltip"));
                debugToggle.OnValueChanged += v => { ModConfig.DebugMode.Value = v; };

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

        private static bool ShouldToggleTomeUiHotkey()
        {
            if (!TryGetPlayerChara(out Chara playerChara))
                return false;
            if (IsTextInputFocused())
                return false;

            var key = ModConfig.TomeHotkeyKey.Value;
            if (key == KeyCode.None || !Input.GetKeyDown(key))
                return false;

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            if (shift != ModConfig.TomeHotkeyShift.Value
                || ctrl != ModConfig.TomeHotkeyCtrl.Value
                || alt != ModConfig.TomeHotkeyAlt.Value)
                return false;

            if (!ArsMoriendiGUI.IsVisible && !HasTomeInInventory(playerChara))
                return false;

            return true;
        }

        private static bool HasTomeInInventory(Chara playerChara)
        {
            return playerChara?.things?.Find(TomeItemId) != null;
        }

        private static bool TryGetPlayerChara(out Chara playerChara)
        {
            playerChara = default!;

            Core core = EClass.core;
            if (core == null || !core.IsGameStarted)
                return false;

            Game game = core.game;
            if (game?.player?.chara == null)
                return false;

            Chara chara = game.player.chara;
            if (chara == null)
                return false;

            playerChara = chara;
            return true;
        }

        private static bool IsTextInputFocused()
        {
            var current = EventSystem.current?.currentSelectedGameObject;
            if (current == null)
                return false;

            return current.GetComponent<InputField>() != null;
        }

        private static string GetHotkeyKeyLabel(KeyCode key)
        {
            return key == KeyCode.None ? "None" : key.ToString();
        }
    }
}
