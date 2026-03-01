using System.Collections.Generic;
using System.Linq;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Flags;
using UnityEngine;

namespace Elin_SukutsuArena;

/// <summary>
/// デバッグメニューから呼び出されるヘルパーメソッド
/// evalアクションから呼び出し可能
/// </summary>
public static class DebugHelper
{
    /// <summary>
    /// アリーナNPCのID一覧
    /// </summary>
    private static readonly string[] ArenaNpcIds = {
        ArenaConfig.NpcIds.Lily,
        ArenaConfig.NpcIds.Balgas,
        ArenaConfig.NpcIds.Zek,
        ArenaConfig.NpcIds.Nul,
        ArenaConfig.NpcIds.Astaroth
    };

    /// <summary>
    /// NPC状態を表示
    /// </summary>
    public static void ShowNpcStatus()
    {
        ModLog.Log("[DebugHelper] === NPC Status ===");
        var messages = new List<string>();

        foreach (var npcId in ArenaNpcIds)
        {
            string status = GetNpcLocationStatus(npcId);
            string name = GetNpcDisplayName(npcId);
            messages.Add($"{name}: {status}");
            ModLog.Log($"  {npcId} -> {status}");
        }

        // ゲーム内メッセージで表示
        Msg.Say($"[NPC状態] {string.Join(" / ", messages)}");
    }

    /// <summary>
    /// NPCの現在位置を取得
    /// </summary>
    private static string GetNpcLocationStatus(string npcId)
    {
        // パーティ内をチェック
        var partyMember = EClass.pc?.party?.members?.FirstOrDefault(c => c.id == npcId);
        if (partyMember != null)
            return "パーティ";

        // グローバルキャラから検索（Findはstring IDを受け取る）
        var globalChara = EClass.game?.cards?.globalCharas?.Find(npcId);
        if (globalChara != null)
        {
            if (globalChara.currentZone == null)
                return "somewhere(非表示)";

            return globalChara.currentZone.Name ?? globalChara.currentZone.id;
        }

        // 現在のマップから検索
        var mapChara = EClass._map?.charas?.Find(c => c.id == npcId);
        if (mapChara != null)
            return "現在のゾーン";

        return "不明";
    }

    /// <summary>
    /// NPC表示名を取得
    /// </summary>
    private static string GetNpcDisplayName(string npcId)
    {
        if (npcId == ArenaConfig.NpcIds.Lily) return "Lily";
        if (npcId == ArenaConfig.NpcIds.Balgas) return "Balgas";
        if (npcId == ArenaConfig.NpcIds.Zek) return "Zek";
        if (npcId == ArenaConfig.NpcIds.Nul) return "Nul";
        if (npcId == ArenaConfig.NpcIds.Astaroth) return "Astaroth";
        return npcId;
    }

    /// <summary>
    /// 指定NPCを非表示（somewhereに移動）
    /// </summary>
    public static void HideNpc(string npcId)
    {
        var npc = FindNpc(npcId);
        if (npc == null)
        {
            Msg.Say($"{GetNpcDisplayName(npcId)}が見つかりません");
            return;
        }

        if (npc.IsPCParty)
        {
            Msg.Say($"{npc.Name}はパーティ内のため非表示にできません");
            return;
        }

        if (npc.currentZone?.id == "somewhere")
        {
            Msg.Say($"{npc.Name}は既に非表示です");
            return;
        }

        npc.MoveZone("somewhere");
        Msg.Say($"{npc.Name}を非表示にしました");
        ModLog.Log($"[DebugHelper] Hid {npcId} to somewhere");
    }

    /// <summary>
    /// 全NPCをアリーナに再表示
    /// </summary>
    public static void RestoreAllNpcs()
    {
        // アリーナゾーンを取得
        var arena = EClass.game?.spatials?.Find(ArenaConfig.ZoneId) as Zone;
        if (arena == null)
        {
            Msg.Say("アリーナゾーンが見つかりません");
            Debug.LogWarning("[DebugHelper] Arena zone not found");
            return;
        }

        int restored = 0;
        foreach (var npcId in ArenaNpcIds)
        {
            var npc = FindNpc(npcId);
            if (npc == null) continue;

            // somewhereにいるNPCのみ復元
            if (npc.currentZone?.id == "somewhere")
            {
                npc.MoveZone(arena);
                npc.homeZone = arena;
                restored++;
                ModLog.Log($"[DebugHelper] Restored {npcId} to arena");
            }
        }

        if (restored > 0)
            Msg.Say($"{restored}人のNPCをアリーナに復元しました");
        else
            Msg.Say("復元対象のNPCはいませんでした");
    }

    /// <summary>
    /// NPCを検索（グローバル→マップの順）
    /// </summary>
    private static Chara FindNpc(string npcId)
    {
        return EClass.game?.cards?.globalCharas?.Find(npcId)
            ?? EClass._map?.charas?.Find(c => c.id == npcId);
    }

    /// <summary>
    /// 特定クエストまで完了
    /// </summary>
    public static void CompleteQuestsUpTo(string targetQuestId)
    {
        var qm = ArenaQuestManager.Instance;
        if (qm == null)
        {
            Msg.Say("クエストマネージャーが初期化されていません");
            return;
        }

        // クエスト順序（ID順でソート）
        var questOrder = new[]
        {
            "01_opening",
            "02_rank_up_G",
            "03_first_match",
            "04_rank_up_F",
            "05_zek_intro",
            "05_2_zek_steal_bottle",
            "06_rank_up_E",
            "07_lily_experiment",
            "08_lily_private",
            "09_balgas_training",
            "10_rank_up_D",
            "11_rank_up_C",
            "12_rank_b_trial",
            "13_makuma2",
            "14_rank_a_trial",
            "15_vs_balgas",
            "16_lily_real_name",
            "17_upper_existence",
            "18_last_battle",
            "19_epilogue"
        };

        int completed = 0;
        foreach (var questId in questOrder)
        {
            if (!qm.IsQuestCompleted(questId))
            {
                qm.CompleteQuest(questId);
                completed++;
            }

            if (questId == targetQuestId)
                break;
        }

        Msg.Say($"{targetQuestId}まで{completed}個のクエストを完了しました");
        ModLog.Log($"[DebugHelper] Completed {completed} quests up to {targetQuestId}");
    }

    /// <summary>
    /// ランクSに設定（全ランクアップクエストを完了）
    /// </summary>
    public static void SetRankS()
    {
        var qm = ArenaQuestManager.Instance;
        if (qm == null)
        {
            Msg.Say("クエストマネージャーが初期化されていません");
            return;
        }

        // 全ランクアップクエストを完了
        // Note: クエストIDはflag_definitions.py QuestIdsと一致させること
        var rankUpQuests = new[]
        {
            "01_opening",
            "02_rank_up_G",  // QuestIds.RANK_UP_G
            "04_rank_up_F",  // QuestIds.RANK_UP_F
            "06_rank_up_E",  // QuestIds.RANK_UP_E
            "10_rank_up_D",  // QuestIds.RANK_UP_D
            "09_rank_up_C",  // QuestIds.RANK_UP_C
            "11_rank_up_B",  // QuestIds.RANK_UP_B
            "12_rank_up_A",  // QuestIds.RANK_UP_A
        };

        int completed = 0;
        foreach (var questId in rankUpQuests)
        {
            if (!qm.IsQuestCompleted(questId))
            {
                qm.CompleteQuest(questId);
                completed++;
            }
        }

        // 闘技者フラグも設定
        var flags = EClass.player?.dialogFlags;
        if (flags != null)
        {
            flags[SessionFlagKeys.Gladiator] = 1;
        }

        Msg.Say($"ランクS設定完了（{completed}クエスト完了）");
        ModLog.Log($"[DebugHelper] Set rank S: completed {completed} quests");
    }

    /// <summary>
    /// アスタロト打倒後（Postgame）の状態に設定
    /// クリア後クエストのテスト用
    /// </summary>
    public static void SetPostgame()
    {
        var qm = ArenaQuestManager.Instance;
        if (qm == null)
        {
            Msg.Say("クエストマネージャーが初期化されていません");
            return;
        }

        var flags = EClass.player?.dialogFlags;
        if (flags == null)
        {
            Msg.Say("フラグシステムが利用できません");
            return;
        }

        // メインストーリークエストを全て完了
        var mainQuests = new[]
        {
            "01_opening",
            "02_rank_up_G",
            "03_zek_intro",
            "04_rank_up_F",
            "05_1_lily_experiment",
            "05_2_zek_steal_bottle",
            "06_rank_up_E",
            "06_2_zek_steal_soulgem",
            "07_upper_existence",
            "08_lily_private",
            "09_balgas_training",
            "10_rank_up_D",
            "09_rank_up_C",  // Note: 順序が入れ替わっている
            "11_rank_up_B",
            "12_makuma",
            "13_makuma2",
            "12_rank_up_A",
            "15_vs_balgas",
            "16_lily_real_name",
            "17_vs_astaroth",
            "18_last_battle",
        };

        int completed = 0;
        foreach (var questId in mainQuests)
        {
            if (!qm.IsQuestCompleted(questId))
            {
                qm.CompleteQuest(questId);
                completed++;
            }
        }

        // フラグ設定
        flags[SessionFlagKeys.Gladiator] = 1;
        flags[SessionFlagKeys.OpeningSeen] = 1;
        flags[ArenaFlagKeys.CurrentPhase] = 7;  // Postgame phase
        flags[ArenaFlagKeys.Rank] = 8;  // SS rank

        // バルガス殺害フラグ（バルガス蘇生クエストの前提条件）
        flags[ArenaFlagKeys.BalgasKilled] = 1;

        // セッションフラグをクリア（エピローグ再発動防止）
        flags[SessionFlagKeys.QuestBattle] = 0;
        flags[SessionFlagKeys.ArenaResult] = 0;
        flags[SessionFlagKeys.IsQuestBattleResult] = 0;

        Msg.Say($"Postgame状態に設定完了（{completed}クエスト完了、バルガス殺害ルート）");
        ModLog.Log($"[DebugHelper] Set postgame: completed {completed} quests, phase=7, rank=SS, balgas_killed=1");
    }

    /// <summary>
    /// 全クエストをリセット
    /// </summary>
    public static void ResetAllQuests()
    {
        var flags = EClass.player?.dialogFlags;
        if (flags == null)
        {
            Msg.Say("フラグシステムが利用できません");
            return;
        }

        // クエスト完了フラグを削除
        var questFlags = flags.Keys
            .Where(k => k.StartsWith(ArenaFlagKeys.QuestDonePrefix))
            .ToList();

        foreach (var flag in questFlags)
        {
            flags.Remove(flag);
        }

        // フェーズをリセット
        if (flags.ContainsKey(ArenaFlagKeys.CurrentPhase))
            flags[ArenaFlagKeys.CurrentPhase] = 0;

        Msg.Say($"{questFlags.Count}個のクエストフラグをリセットしました");
        ModLog.Log($"[DebugHelper] Reset {questFlags.Count} quest flags");
    }

    /// <summary>
    /// 主要フラグの状態を表示
    /// </summary>
    public static void ShowFlagStatus()
    {
        var flags = EClass.player?.dialogFlags;
        if (flags == null)
        {
            Msg.Say("フラグシステムが利用できません");
            return;
        }

        ModLog.Log("[DebugHelper] === Flag Status ===");

        // シナリオフラグ
        var scenarioFlags = new Dictionary<string, string>
        {
            { "balgas_dead", "バルガス死亡" },
            { "lily_betrayed", "リリィ離反" },
            { SessionFlagKeys.OpeningSeen, "オープニング済" },
            { SessionFlagKeys.Gladiator, "闘技者登録" }
        };

        var messages = new List<string>();
        foreach (var kvp in scenarioFlags)
        {
            string key = kvp.Key;
            string label = kvp.Value;
            bool isSet = flags.TryGetValue(key, out int val) && val != 0;
            messages.Add($"{label}:{(isSet ? "ON" : "OFF")}");
            ModLog.Log($"  {key} = {(isSet ? val.ToString() : "0/未設定")}");
        }

        // ランク
        if (flags.TryGetValue(ArenaFlagKeys.Rank, out int rank))
        {
            messages.Add($"ランク:{rank}");
            ModLog.Log($"  rank = {rank}");
        }

        // フェーズ
        if (flags.TryGetValue(ArenaFlagKeys.CurrentPhase, out int phase))
        {
            messages.Add($"フェーズ:{phase}");
            ModLog.Log($"  phase = {phase}");
        }

        Msg.Say($"[フラグ] {string.Join(" / ", messages)}");
    }
}

