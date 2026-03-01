using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace Elin_ArsMoriendi
{
    [BepInPlugin(ModGuid, "Ars Moriendi", "0.1.0")]
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
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

            if (!_patchFailureNoticeShown
                && (HasCriticalPatchFailures || HasCriticalRuntimePatchFailures)
                && EClass.pc != null)
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

                var servantAuraToggle = builder.Root!.AddToggle(
                    controller.Tr("ShowServantAura"), ModConfig.ShowServantAura.Value,
                    16, controller.Tr("ShowServantAura_tooltip"));
                servantAuraToggle.OnValueChanged += v =>
                {
                    ModConfig.ShowServantAura.Value = v;
                    NecromancyManager.Instance.RefreshServantVisualStateCurrentZone();
                };

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
                    if (EClass.pc != null)
                        ApotheosisFeatBonus.SyncWithConfigAndFeat(EClass.pc);
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
    }
}
