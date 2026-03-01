using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Commands;
using Elin_SukutsuArena.Effects;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;

namespace Elin_SukutsuArena;

/// <summary>
/// 巣窟アリーナ Mod プラグインエントリ
/// </summary>
[GameDependency("Reflection", "Cwl.API.Custom.CustomZone.Managed", "High", "CWL internal API, may change between CWL versions")]
[BepInPlugin(ArenaConfig.ModGuid, "Sukutsu Arena", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    // ArenaConfigを参照するための便利エイリアス
    public const string ModGuid = ArenaConfig.ModGuid;
    public const string ZoneId = ArenaConfig.ZoneId;

    private void Awake()
    {
        // Mod アセンブリを ClassCache に登録
        // バニラの SpatialGen.Create が ClassCache 経由でカスタムゾーン型を解決するために必要
        // （BepInEx Mod は自動登録されないため）
        ClassCache.assemblies.Add(Assembly.GetExecutingAssembly().GetName().Name);

        new Harmony(ModGuid).PatchAll();
        ArenaConsole.Register();
        ModLog.Log("[SukutsuArena] Plugin loaded.");
        ValidatePhaseEnums();
        ValidateHarmonyPatches();
#if DEBUG
        ModLog.Log("[SukutsuArena] Debug Keys:");
        ModLog.Log("[SukutsuArena]   F7: Cycle LUT (color grading)");
        ModLog.Log("[SukutsuArena]   F8: Test LUT flash effect (LUT_Invert)");
        ModLog.Log("[SukutsuArena]   F9: Arena status (rank/flags/quests)");
        ModLog.Log("[SukutsuArena]   F11: Complete next available quest");
        ModLog.Log("[SukutsuArena]   F12: Cycle rank up");
#endif
    }

    private static void ValidatePhaseEnums()
    {
        var flagPhases = System.Enum.GetValues(typeof(Elin_SukutsuArena.Flags.Phase)).Cast<Elin_SukutsuArena.Flags.Phase>().ToArray();
        var questPhases = System.Enum.GetValues(typeof(Elin_SukutsuArena.Quests.StoryPhase)).Cast<Elin_SukutsuArena.Quests.StoryPhase>().ToArray();

        if (flagPhases.Length != questPhases.Length)
        {
            Debug.LogWarning($"[SukutsuArena] Phase enum length mismatch: Flags.Phase={flagPhases.Length}, Quests.StoryPhase={questPhases.Length}");
            LogPhaseEnumDetails(flagPhases, questPhases);
            return;
        }

        for (int i = 0; i < flagPhases.Length; i++)
        {
            int flagValue = (int)flagPhases[i];
            int questValue = (int)questPhases[i];
            if (flagValue != questValue)
            {
                Debug.LogWarning($"[SukutsuArena] Phase enum ordinal mismatch at index {i}: Flags.Phase={flagPhases[i]}({flagValue}), Quests.StoryPhase={questPhases[i]}({questValue})");
                LogPhaseEnumDetails(flagPhases, questPhases);
                return;
            }
        }
    }

    private static void LogPhaseEnumDetails(
        Elin_SukutsuArena.Flags.Phase[] flagPhases,
        Elin_SukutsuArena.Quests.StoryPhase[] questPhases)
    {
        var max = Mathf.Max(flagPhases.Length, questPhases.Length);
        for (int i = 0; i < max; i++)
        {
            var flagLabel = i < flagPhases.Length ? $"{flagPhases[i]}({(int)flagPhases[i]})" : "<missing>";
            var questLabel = i < questPhases.Length ? $"{questPhases[i]}({(int)questPhases[i]})" : "<missing>";
            Debug.LogWarning($"[SukutsuArena]   Phase[{i}]: Flags={flagLabel}, Quests={questLabel}");
        }
    }

    private static void ValidateHarmonyPatches()
    {
        var checks = new List<(MethodBase method, string label)>
        {
            (AccessTools.Method(typeof(Zone), nameof(Zone.Activate)), "Zone.Activate"),
            (AccessTools.PropertyGetter(typeof(Zone), "ShouldAutoRevive") ?? AccessTools.Method(typeof(Zone), "get_ShouldAutoRevive"),
                "Zone.ShouldAutoRevive"),
            (AccessTools.Method(typeof(LayerDrama), nameof(LayerDrama.OnKill)), "LayerDrama.OnKill"),
            (AccessTools.Method(typeof(Chara), nameof(Chara.TickConditions)), "Chara.TickConditions"),
            (AccessTools.Method(typeof(Region), nameof(Region.CheckRandomSites)), "Region.CheckRandomSites"),
            (AccessTools.Method(typeof(DramaManager), "ParseLine"), "DramaManager.ParseLine"),
            (AccessTools.Method(typeof(Card), nameof(Card.DamageHP),
                new[] { typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara) }),
                "Card.DamageHP"),
            (AccessTools.Method(typeof(Card), nameof(Card.HealHP),
                new[] { typeof(int), typeof(HealSource) }),
                "Card.HealHP"),
        };

        foreach (var (method, label) in checks)
        {
            CheckHarmonyPatch(method, label);
        }
    }

    private static void CheckHarmonyPatch(MethodBase method, string label)
    {
        if (method == null)
        {
            Debug.LogWarning($"[SukutsuArena] Patch target not found: {label}");
            return;
        }

        var info = Harmony.GetPatchInfo(method);
        bool hasOurPatch = info?.Owners?.Contains(ModGuid) ?? false;
        if (!hasOurPatch)
        {
            Debug.LogWarning($"[SukutsuArena] Patch missing or not applied: {label}");
        }
    }

#if DEBUG
    private void Update()
    {
        // ゲームがロードされていない場合はスキップ
        if (EMono.core == null || EMono.game == null) return;

        // F7: LUT切り替え
        if (Input.GetKeyDown(KeyCode.F7))
        {
            CycleLut();
        }

        // F8: LUTフラッシュテスト
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Msg.Say("LUT Flash: LUT_Invert (10秒)");
            LutEffect.Flash("LUT_Invert", 10f, 0.3f);
        }

        // F9: アリーナステータス表示
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ShowArenaStatus();
        }

        // F11: 次の利用可能なクエストを完了
        if (Input.GetKeyDown(KeyCode.F11))
        {
            CompleteNextQuest();
        }

        // F12: ランクを1つ上げる
        if (Input.GetKeyDown(KeyCode.F12))
        {
            CycleRankUp();
        }
    }
#endif

    /// <summary>
    /// CWLのManagedゾーンからSourceZone.Rowを取得
    /// 公開APIを優先し、非公開フィールドはフォールバックとして使用
    /// </summary>
    [GameDependency("Reflection", "Cwl.API.Custom.CustomZone", "High", "AccessTools.TypeByName for CWL type discovery")]
    [GameDependency("Reflection", "CustomZone.Managed property/field", "High", "May be property, public field, or non-public field")]
    private static bool TryGetCwlManagedZone(string zoneId, out SourceZone.Row row)
    {
        row = null;
        try
        {
            var customZoneType = AccessTools.TypeByName("Cwl.API.Custom.CustomZone");
            if (customZoneType == null)
            {
                ModLog.Log("[SukutsuArena] CWL CustomZone type not found");
                return false;
            }

            // 1. 公開プロパティを優先
            var managedProp = customZoneType.GetProperty("Managed", BindingFlags.Static | BindingFlags.Public);
            if (managedProp != null)
            {
                var managed = managedProp.GetValue(null) as IDictionary;
                if (managed != null && managed.Contains(zoneId))
                {
                    row = managed[zoneId] as SourceZone.Row;
                    return row != null;
                }
            }

            // 2. 公開フィールドを試す
            var managedFieldPublic = customZoneType.GetField("Managed", BindingFlags.Static | BindingFlags.Public);
            if (managedFieldPublic != null)
            {
                var managed = managedFieldPublic.GetValue(null) as IDictionary;
                if (managed != null && managed.Contains(zoneId))
                {
                    row = managed[zoneId] as SourceZone.Row;
                    return row != null;
                }
            }

            // 3. フォールバック: 非公開フィールド（警告付き）
            var managedFieldNonPublic = customZoneType.GetField("Managed", BindingFlags.Static | BindingFlags.NonPublic);
            if (managedFieldNonPublic != null)
            {
                Debug.LogWarning("[SukutsuArena] Using non-public CWL field - may break with CWL updates");
                var managed = managedFieldNonPublic.GetValue(null) as IDictionary;
                if (managed != null && managed.Contains(zoneId))
                {
                    row = managed[zoneId] as SourceZone.Row;
                    return row != null;
                }
            }

            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SukutsuArena] CWL access failed: {ex.Message}");
            return false;
        }
    }

    private static void InjectZoneData()
    {
        if (EMono.sources.zones.map.ContainsKey(ZoneId))
        {
            return;
        }

        ModLog.Log($"[SukutsuArena] Injecting zone data from CWL Managed...");

        if (TryGetCwlManagedZone(ZoneId, out var row))
        {
            EMono.sources.zones.rows.Add(row);
            EMono.sources.zones.map[ZoneId] = row;
            ModLog.Log($"[SukutsuArena] Successfully injected {ZoneId} from CustomZone!");
        }
        else
        {
            Debug.LogError($"[SukutsuArena] Failed to inject: ZoneId not in CWL Managed.");
        }
    }

    private void EnterZone()
    {
        var zone = EMono.world?.region?.FindZone(ZoneId);
        if (zone == null)
        {
            Debug.LogError($"[SukutsuArena] Zone '{ZoneId}' not found. Try F7 first.");
            return;
        }

        ModLog.Log($"[SukutsuArena] Entering zone '{ZoneId}'...");
        EMono.player.MoveZone(zone);
    }

#if DEBUG
    private void ShowArenaStatus()
    {
        var sb = new System.Text.StringBuilder();
        var flags = EClass.player?.dialogFlags;

        // === 全アリーナフラグ（生データ） ===
        sb.AppendLine("=== Arena Flags (Raw) ===");
        if (flags != null)
        {
            // chitsii.arena.* と sukutsu_* プレフィックスのフラグを収集
            var arenaFlags = flags
                .Where(kvp => kvp.Key.StartsWith(ArenaFlagKeys.Prefix + ".") || kvp.Key.StartsWith("sukutsu_"))
                .OrderBy(kvp => kvp.Key)
                .ToList();

            if (arenaFlags.Count > 0)
            {
                foreach (var kvp in arenaFlags)
                {
                    // プレフィックスを短縮して表示
                    var displayKey = kvp.Key
                        .Replace(ArenaFlagKeys.Prefix + ".", "")
                        .Replace("sukutsu_", "");
                    sb.AppendLine($"{displayKey} = {kvp.Value}");
                }
            }
            else
            {
                sb.AppendLine("(なし)");
            }
        }
        else
        {
            sb.AppendLine("(flags unavailable)");
        }
        sb.AppendLine();

        // === 完了クエスト ===
        sb.AppendLine("=== 完了クエスト ===");
        var completedQuests = ArenaQuestManager.Instance.GetAllQuests()
            .Where(q => ArenaQuestManager.Instance.IsQuestCompleted(q.QuestId))
            .ToList();

        if (completedQuests.Count > 0)
        {
            foreach (var quest in completedQuests)
            {
                sb.AppendLine($"[完了] {quest.QuestId}");
            }
        }
        else
        {
            sb.AppendLine("(なし)");
        }
        sb.AppendLine();

        // === 利用可能クエスト ===
        sb.AppendLine("=== 利用可能クエスト ===");
        var available = ArenaQuestManager.Instance.GetAvailableQuests();
        if (available.Count > 0)
        {
            foreach (var quest in available)
            {
                var marker = !string.IsNullOrEmpty(quest.QuestGiver) ? $" [{quest.QuestGiver}]" : "";
                sb.AppendLine($"- {quest.QuestId}{marker}");
            }
        }
        else
        {
            sb.AppendLine("(なし)");
        }

        // デバッグログにも出力
        ModLog.Log($"[SukutsuArena] Arena Status:\n{sb}");

        // OKダイアログで表示
        var dialog = Layer.Create<Dialog>();
        dialog.textDetail.SetText(sb.ToString());
        dialog.list.AddButton(null, "OK", dialog.Close);
        ELayer.ui.AddLayer(dialog);
    }

    private void CycleRankUp()
    {
        var ctx = ArenaContext.I;
        var currentRank = ctx.Player.Rank;
        var nextRank = currentRank + 1;

        // 最大ランクを超えないようにする
        if (nextRank > Flags.Rank.S)
        {
            nextRank = Flags.Rank.Unranked;
        }

        ctx.Player.Rank = nextRank;
        ModLog.Log($"[SukutsuArena] Rank changed: {currentRank} -> {nextRank}");

        // 闘士登録も確認
        if (ctx.Storage.GetInt(SessionFlagKeys.Gladiator) == 0)
        {
            ctx.Storage.SetInt(SessionFlagKeys.Gladiator, 1);
            ModLog.Log("[SukutsuArena] Gladiator status set to true");
        }

        // デバッグ用: 前提クエストを完了済みにする
        CompletePrerequisiteQuests(nextRank);
    }

    /// <summary>
    /// 指定ランクまでの前提クエストを全て完了済みにする（デバッグ用）
    /// </summary>
    private void CompletePrerequisiteQuests(Flags.Rank rank)
    {
        // 常にオープニングを完了
        ArenaQuestManager.Instance.CompleteQuest("01_opening");

        // ランクに応じて昇格試合クエストを完了
        if (rank >= Flags.Rank.G)
        {
            ArenaQuestManager.Instance.CompleteQuest("02_rank_up_G");
        }
        if (rank >= Flags.Rank.F)
        {
            ArenaQuestManager.Instance.CompleteQuest("04_rank_up_F");
        }
        if (rank >= Flags.Rank.E)
        {
            ArenaQuestManager.Instance.CompleteQuest("06_rank_up_E");
        }
        if (rank >= Flags.Rank.D)
        {
            ArenaQuestManager.Instance.CompleteQuest("10_rank_up_D");
        }
        if (rank >= Flags.Rank.C)
        {
            ArenaQuestManager.Instance.CompleteQuest("09_rank_up_C");
        }
        if (rank >= Flags.Rank.B)
        {
            ArenaQuestManager.Instance.CompleteQuest("11_rank_up_B");
        }

        ModLog.Log($"[SukutsuArena] Prerequisite quests completed for rank {rank}");
    }

    private void CompleteNextQuest()
    {
        var available = ArenaQuestManager.Instance.GetAvailableQuests();
        if (available.Count == 0)
        {
            ModLog.Log("[SukutsuArena] No quests available to complete.");
            return;
        }

        var quest = available[0];
        ArenaQuestManager.Instance.CompleteQuest(quest.QuestId);
        ModLog.Log($"[SukutsuArena] Completed quest: {quest.QuestId} ({quest.DisplayNameJP})");
    }

    // LUT切り替え用
    private static string[] _lutList;
    private static int _lutIndex = 0;

    private void CycleLut()
    {
        if (EClass._map == null) return;

        // 初回のみLUT一覧をロード
        if (_lutList == null)
        {
            var luts = Resources.LoadAll<Texture2D>("Scene/Profile/Lut/");
            // おすすめLUTを先頭に配置
            var recommended = new[]
            {
                "LUT_Desaturated", "LUT_Dimmed", "LUT_Dimmed2", "LUT_Dimmed3",
                "LUT_DayNighttime1", "LUT_DayNighttime2", "LUT_Horror1", "LUT_Horror2",
                "LUT_Colder", "LUT_BlueTint", "LUT_Blueforest", "LUT_FullMoon",
                "LUT_Cine1", "LUT_Cine2", "LUT_Default"
            };
            var others = luts.Select(l => l.name)
                .Where(n => !recommended.Contains(n))
                .OrderBy(n => n)
                .ToArray();
            _lutList = new[] { "None" }.Concat(recommended).Concat(others).ToArray();

            // 現在のLUTのインデックスを探す
            var currentLut = EClass._map.config.idLut ?? "None";
            _lutIndex = System.Array.IndexOf(_lutList, currentLut);
            if (_lutIndex < 0) _lutIndex = 0;
        }

        // 次のLUTへ
        _lutIndex = (_lutIndex + 1) % _lutList.Length;
        var newLut = _lutList[_lutIndex];

        // 設定を適用
        EClass._map.config.idLut = newLut == "None" ? null : newLut;
        EClass.scene.ApplyZoneConfig();

        // 表示
        Msg.Say($"LUT: {newLut} ({_lutIndex + 1}/{_lutList.Length})");
        ModLog.Log($"[SukutsuArena] LUT changed to: {newLut}");
    }
#endif

    /// <summary>
    /// Region.CheckRandomSites でゾーンが存在しなければ生成する
    /// </summary>
    [GameDependency("Patch", "Region.CheckRandomSites", "High", "Method signature may change")]
    [HarmonyPatch(typeof(Region), nameof(Region.CheckRandomSites))]
    public static class CheckRandomSitesPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Region __instance)
        {
            // まずデータを注入
            InjectZoneData();

            if (__instance.FindZone(ZoneId) != null)
            {
                return;
            }

            ModLog.Log($"[SukutsuArena] Creating zone '{ZoneId}'...");
            try
            {
                SpatialGen.Create(ZoneId, __instance, register: true, x: -99999, y: -99999, 0);
                ModLog.Log($"[SukutsuArena] Zone '{ZoneId}' created successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SukutsuArena] Failed to create zone: {ex}");
            }
        }
    }
}

