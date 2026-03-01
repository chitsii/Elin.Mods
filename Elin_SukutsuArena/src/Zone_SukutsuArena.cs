using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Flags;

namespace Elin_SukutsuArena;

/// <summary>
/// 巣窟アリーナ専用のカスタムゾーン
/// Zone_sssMain (StrangeSpellShop) と同じ構造
/// </summary>
[GameDependency("Inheritance", "Zone_Civilized", "High", "Base zone class, virtual method signatures may change")]
[GameDependency("Direct", "EClass._map.charas", "Medium", "Character lookup in zone")]
[GameDependency("Direct", "EClass.player.dialogFlags", "Medium", "Flag storage for story progression")]
public class Zone_SukutsuArena : Zone_Civilized
{
    /// <summary>
    /// NPC配置座標の定義（1箇所で管理）
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, (int x, int z)> NpcPositions = new()
    {
        { ArenaConfig.NpcIds.Lily, (42, 57) },
        { ArenaConfig.NpcIds.Iris, (55, 57) },
        { ArenaConfig.NpcIds.Balgas, (43, 52) },
        { ArenaConfig.NpcIds.Zek, (40, 45) },
        { ArenaConfig.NpcIds.Astaroth, (48, 41) },
        { ArenaConfig.NpcIds.Nul, (48, 44) },
        { "sukutsu_cain", (45, 52) },
#if DEBUG
        { ArenaConfig.NpcIds.DebugMaster, (45, 50) },
#endif
    };

    /// <summary>
    /// 常駐NPC（ゾーン入場時に必ず存在を確認するNPC）
    /// </summary>
    private static readonly string[] ResidentNpcIds =
    {
        ArenaConfig.NpcIds.Lily,
        ArenaConfig.NpcIds.Iris,
        ArenaConfig.NpcIds.Balgas,
        ArenaConfig.NpcIds.Astaroth,
        ArenaConfig.NpcIds.Nul,
#if DEBUG
        ArenaConfig.NpcIds.DebugMaster,
#endif
    };

    /// <summary>
    /// マップファイルのパス（Mod フォルダ内の Maps/ から読み込む）
    /// StrangeSpellShop と同じパターン
    /// </summary>
    public override string pathExport =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                     "Maps/" + this.idExport + ".z");

    /// <summary>
    /// 建築を明示的に禁止
    /// </summary>
    public override bool RestrictBuild => true;

    /// <summary>
    /// ワールドマップから入場時、マップ中央に配置
    /// </summary>
    public override ZoneTransition.EnterState RegionEnterState => ZoneTransition.EnterState.Center;

    public override void OnGenerateMap()
    {
        base.OnGenerateMap();
        ModLog.Log("[SukutsuArena] OnGenerateMap called. (NPCs should be placed via Map Editor)");
    }

    /// <summary>
    /// ゾーンに入った時に呼ばれる
    /// オープニングイベントをトリガー
    /// </summary>
    public override void OnBeforeSimulate()
    {
        base.OnBeforeSimulate();

        // Ensure zone playlist can take over in the lobby
        LayerDrama.haltPlaylist = false;

        // 時空の裂け目的な背景設定（異次元のアリーナ）
        if (EClass._map != null)
        {
            // 利用可能なLUT一覧をログ出力（デバッグ用）
            var luts = Resources.LoadAll<Texture2D>("Scene/Profile/Lut/");
            ModLog.Log($"[SukutsuArena] Available LUTs: {string.Join(", ", luts.Select(l => l.name))}");

            EClass._map.config.bg = MapBG.Fog;
            EClass._map.config.fog = FogType.Grave;  // 墓地の霧
            EClass._map.config.hour = 0;                 // 常時深夜（昼夜サイクル無効）
            EClass._map.config.indoor = true;            // 室内扱い（外光の影響を抑える）
            // LUT（カラーグレーディング）設定
            EClass._map.config.idLut = "LUT_TonedDown";    // 落ち着いた色合いで見やすい
            EClass._map.config.lutBrightness = 0.7f;       // 照度を下げる
        }

        // BGMはExcelのidPlaylist列で設定（Lobby_Normal）

        // 常駐NPCをアリーナに戻す（somewhereにいる場合）
        EnsureMainNpcsInArena();

        // NPCの初期位置を設定（初回のみ）
        PositionNpcsIfNeeded();

        // アリーナNPCのhomeZoneが正しく設定されているか確認
        EnsureNpcHomeZones();

        // ストーリー進行に応じてNPCの表示/非表示を制御
        HandleNpcVisibility();

        // 初回訪問時のみオープニングドラマを再生
        // dialogFlags でフラグを管理（CWLと同じ）
        bool openingSeen = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.OpeningSeen)
            && EClass.player.dialogFlags[SessionFlagKeys.OpeningSeen] != 0;

        if (!openingSeen)
        {
            ModLog.Log("[SukutsuArena] First visit detected. Triggering opening drama...");
            TriggerOpeningDrama();
        }
    }

    /// <summary>
    /// 常駐NPC（リリィ、バルガス、アスタロト）をアリーナに配置
    /// CWLの「already exists」スキップや初回訪問時の配置漏れを救済
    /// </summary>
    private void EnsureMainNpcsInArena()
    {
        // 常駐NPCをチェック（ストーリー進行で非表示になるNPCはHandleNpcVisibilityで制御）
        foreach (var npcId in ResidentNpcIds)
        {
            // 既にこのゾーンにいる場合はスキップ
            var existingNpc = EClass._map.charas.Find(c => c.id == npcId);
            if (existingNpc != null)
                continue;

            // グローバルキャラから探す
            Chara npc = null;
            foreach (var c in EClass.game.cards.globalCharas.Values)
            {
                if (c.id == npcId)
                {
                    npc = c;
                    break;
                }
            }

            if (npc != null)
            {
                // ペット化されている場合はスキップ
                if (npc.IsPCParty || npc.master == EClass.pc)
                    continue;

                // 別のゾーン（somewhereなど）にいる場合、アリーナに移動
                if (npc.currentZone != this)
                {
                    ModLog.Log($"[SukutsuArena] EnsureMainNpcsInArena: Moving {npcId} from {npc.currentZone?.id ?? "null"} to arena");
                    npc.MoveZone(this);
                }
            }
            else
            {
                // グローバルにも存在しない場合、新規作成してスポーン
                ModLog.Log($"[SukutsuArena] EnsureMainNpcsInArena: Creating and spawning {npcId}");
                var newNpc = CharaGen.Create(npcId);
                if (newNpc != null)
                {
                    var (x, z) = GetNpcPosition(npcId);
                    var point = new Point { x = x, z = z };
                    EClass._zone.AddCard(newNpc, point);
                    newNpc.SetGlobal();
                    newNpc.homeZone = this;
                }
                else
                {
                    Debug.LogWarning($"[SukutsuArena] Failed to create NPC: {npcId}");
                }
            }
        }
    }

    /// <summary>
    /// NPCを指定座標に配置（毎回訪問時）
    /// CWLのaddZone_タグで生成されたNPCを固定位置に移動
    /// </summary>
    private void PositionNpcsIfNeeded()
    {
        foreach (var kvp in NpcPositions)
        {
            PositionNpc(kvp.Key, kvp.Value.x, kvp.Value.z);
        }
    }

    /// <summary>
    /// 指定NPCを定義済み座標に配置
    /// </summary>
    private void PositionNpc(string npcId)
    {
        var (x, z) = GetNpcPosition(npcId);
        PositionNpc(npcId, x, z);
    }

    /// <summary>
    /// NPCの配置座標を取得
    /// </summary>
    private (int x, int z) GetNpcPosition(string npcId)
    {
        if (NpcPositions.TryGetValue(npcId, out var pos))
            return pos;
        return (45, 50); // デフォルト
    }

    /// <summary>
    /// 指定NPCを座標に配置し、動かないように設定
    /// ゾーンにいない場合（somewhereに移動中など）はスキップ
    /// </summary>
    private void PositionNpc(string npcId, int x, int z)
    {
        var npc = EClass._map.charas.Find(c => c.id == npcId);
        if (npc == null || !npc.ExistsOnMap)
        {
            // NPCがこのゾーンにいない場合はスキップ
            return;
        }

        // ペット化されている場合はスキップ
        if (npc.IsPCParty || npc.master == EClass.pc)
            return;

        // NPCを動かないように設定
        npc.AddEditorTag(EditorTag.AINoMove);

        // MoveImmediateで正しく配置
        var point = new Point { x = x, z = z };
        npc.MoveImmediate(point, focus: false, cancelAI: true);
        ModLog.Log($"[SukutsuArena] Positioned {npcId} at ({x}, {z})");
    }

    /// <summary>
    /// アリーナNPCのhomeZoneが正しく設定されているか確認
    /// CWLのaddZone_タグで設定済みだが、古いセーブデータ対策として残す
    /// ペット解放時にhomeZoneへ自動帰還するために必要
    /// </summary>
    private void EnsureNpcHomeZones()
    {
        foreach (var npcId in NpcPositions.Keys)
        {
            var npc = EClass._map.charas.Find(c => c.id == npcId);
            if (npc != null && npc.IsGlobal && npc.homeZone != this)
            {
                ModLog.Log($"[SukutsuArena] Setting homeZone for {npcId}");
                npc.homeZone = this;
            }
        }
    }

    [GameDependency("Direct", "Chara.ShowDialog", "Medium", "CWL drama trigger via Chara.ShowDialog(book, step)")]
    private void TriggerOpeningDrama()
    {
        // リリィ（受付嬢）を探してドラマを開始
        var lily = EClass._map.charas.Find(c => c.id == ArenaConfig.NpcIds.Lily);
        if (lily != null)
        {
            // CWLドラマを開始
            // ShowDialog(book, step) で呼び出し
            // book = "drama_sukutsu_opening" (CWLがマッピング)
            lily.ShowDialog("drama_sukutsu_opening", "main");
        }
        else
        {
            Debug.LogWarning("[SukutsuArena] Lily not found. Cannot trigger opening drama.");
        }
    }

    /// <summary>
    /// ストーリー進行に応じてNPCの表示/非表示を制御
    /// 特定のクエスト完了やストーリーフラグに基づいてNPCを"somewhere"ゾーンに移動
    /// </summary>
    internal void HandleNpcVisibility()
    {
        var qm = ArenaQuestManager.Instance;
        var storage = Core.ArenaContext.I?.Storage;

        // ArenaStorageからフラグを取得するヘルパー
        bool HasArenaFlag(string key) => storage?.GetInt(key, 0) == 1;

        // Zek: 02_rank_up_G完了後に表示、それまでは非表示
        bool rankGCompleted = qm?.IsQuestCompleted("02_rank_up_G") ?? false;
        ModLog.Log($"[SukutsuArena] HandleNpcVisibility: rankGCompleted={rankGCompleted}, qm={qm != null}");

        // dialogFlagsのクエスト完了フラグをダンプ（デバッグ用）
        var questDoneFlags = EClass.player.dialogFlags
            .Where(f => f.Key.Contains("quest_done"))
            .Select(f => $"{f.Key}={f.Value}")
            .ToList();
        ModLog.Log($"[SukutsuArena] Quest done flags: {string.Join(", ", questDoneFlags)}");

        if (rankGCompleted)
        {
            ShowNpcInArena(ArenaConfig.NpcIds.Zek);
        }
        else
        {
            HideNpcToSomewhere(ArenaConfig.NpcIds.Zek);
        }

        // バルガス死亡ルート（ArenaStorageから取得）
        bool balgasDead = HasArenaFlag(ArenaFlagKeys.BalgasKilled);

        // Nul: 最終戦（18_last_battle）完了後は表示（全ルート共通で復活）
        // Bランク試験～最終戦間は非表示
        bool lastBattleCompleted = qm?.IsQuestCompleted("18_last_battle") ?? false;
        bool rankBCompleted = qm?.IsQuestCompleted("12_rank_b_trial") ?? false;
        if (lastBattleCompleted)
        {
            // 最終戦完了後はNulが復活（19_epilogueで描写、全ルート共通）
            ShowNpcInArena(ArenaConfig.NpcIds.Nul);
        }
        else if (balgasDead || rankBCompleted)
        {
            // Bランク試験後～最終戦前は非表示（18_last_battleで「消滅」する演出のため）
            HideNpcToSomewhere(ArenaConfig.NpcIds.Nul);
        }

        // Astaroth: 最終戦完了後は非表示（シナリオで消滅）
        if (lastBattleCompleted)
        {
            HideNpcToSomewhere(ArenaConfig.NpcIds.Astaroth);
        }

        // Cain: 蘇生クエスト完了後に表示。それ以前は常にsomewhereに退避
        bool cainRevived = HasArenaFlag(ArenaFlagKeys.CainRevived);
        if (cainRevived)
        {
            ShowNpcInArena("sukutsu_cain");
        }
        else if (EClass.game?.cards?.globalCharas?.Find("sukutsu_cain") != null)
        {
            HideNpcToSomewhere("sukutsu_cain");
        }

        // Balgas: 復活フラグがあれば表示、それ以外でPOSTGAMEまたは死亡なら非表示
        bool balgasRevived = HasArenaFlag(ArenaFlagKeys.BalgasRevived);
        if (balgasRevived)
        {
            ShowNpcInArena(ArenaConfig.NpcIds.Balgas);
        }
        else if (lastBattleCompleted || balgasDead)
        {
            HideNpcToSomewhere(ArenaConfig.NpcIds.Balgas);
        }
    }

    /// <summary>
    /// NPCを"somewhere"ゾーンに移動して非表示にする
    /// ペット化されている場合はスキップ
    /// </summary>
    private void HideNpcToSomewhere(string npcId)
    {
        var npc = EClass._map.charas.Find(c => c.id == npcId);
        if (npc == null)
        {
            ModLog.Log($"[SukutsuArena] HideNpc: {npcId} not found in map");
            return;
        }

        // ペット化されている場合はスキップ
        if (npc.IsPCParty || npc.master == EClass.pc)
        {
            ModLog.Log($"[SukutsuArena] {npcId} is in party, skipping hide");
            return;
        }

        ModLog.Log($"[SukutsuArena] Hiding {npcId} to somewhere zone (currentZone={npc.currentZone?.id ?? "null"})");
        npc.MoveZone("somewhere");
    }

    /// <summary>
    /// NPCをアリーナに表示する（"somewhere"から戻す）
    /// </summary>
    private void ShowNpcInArena(string npcId)
    {
        // 既にこのゾーンにいる場合は何もしない
        var existingNpc = EClass._map.charas.Find(c => c.id == npcId);
        if (existingNpc != null)
        {
            ModLog.Log($"[SukutsuArena] ShowNpcInArena: {npcId} already in arena");
            return;
        }

        // グローバルキャラから探す
        Chara npc = null;
        foreach (var c in EClass.game.cards.globalCharas.Values)
        {
            if (c.id == npcId)
            {
                npc = c;
                break;
            }
        }
        if (npc == null)
        {
            ModLog.Log($"[SukutsuArena] ShowNpcInArena: {npcId} not found in globalCharas");
            return;
        }

        ModLog.Log($"[SukutsuArena] ShowNpcInArena: {npcId} currentZone={npc.currentZone?.id ?? "null"}");

        // アリーナ以外にいる場合（somewhere, null含む）は移動
        if (npc.currentZone != this)
        {
            ModLog.Log($"[SukutsuArena] Showing {npcId} in arena (moving from {npc.currentZone?.id ?? "null"})");
            npc.MoveZone(this);
            // 移動後に座標を設定
            PositionNpc(npcId);
        }
    }
}

