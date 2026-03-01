using Newtonsoft.Json;
using UnityEngine;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Flags;
using Elin_SukutsuArena.Localization;
using Elin_SukutsuArena.RandomBattle;

/// <summary>
/// アリーナ戦闘用のゾーンインスタンス
/// 戦闘終了後にプレイヤーを元の場所に戻し、報酬を付与する
/// </summary>
[GameDependency("Inheritance", "ZoneInstance", "High", "Base class for zone instances, serialization may change")]
[GameDependency("Direct", "EClass.player.dialogFlags", "Medium", "Flag storage for battle results")]
[GameDependency("Direct", "ThingGen.Create", "Medium", "Item generation for rewards")]
public class ZoneInstanceArenaBattle : ZoneInstance
{
    [JsonProperty]
    public int uidMaster;

    [JsonProperty]
    public int returnX;

    [JsonProperty]
    public int returnZ;

    [JsonProperty]
    public int rewardPlat = 10;

    [JsonProperty]
    public int rewardPotion = 0;

    [JsonProperty]
    public bool isRankUp = false;

    [JsonProperty]
    public string stageId = "";

    [JsonProperty]
    public string bgmBattle = "";

    [JsonProperty]
    public string bgmVictory = "";

    /// <summary>
    /// 勝利時に直接開始するドラマID（設定時は自動ダイアログをスキップ）
    /// </summary>
    [JsonProperty]
    public string victoryDramaId = "";

    /// <summary>
    /// 敗北時に直接開始するドラマID（設定時は自動ダイアログをスキップ）
    /// </summary>
    [JsonProperty]
    public string defeatDramaId = "";

    public override ZoneTransition.EnterState ReturnState => ZoneTransition.EnterState.Exact;

    /// <summary>
    /// ゾーン離脱時の処理
    /// </summary>
    public override void OnLeaveZone()
    {
        ModLog.Log($"[SukutsuArena] OnLeaveZone called, stageId={stageId}");
        // Allow zone BGM to resume after leaving the arena battle
        LayerDrama.haltPlaylist = false;

        // 勝利判定はZoneEventArenaBattleで行われ、フラグが立っているはず
        int result = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.ArenaResult)
            ? EClass.player.dialogFlags[SessionFlagKeys.ArenaResult] : 0;

        if (result == 1) // 勝利
        {
            ModLog.Log("[SukutsuArena] Victory processed");

            GiveReward();
            ScheduleAutoDialog();
        }
        else
        {
            // 敗北（死亡または逃走）
            // 既に勝利フラグがない場合のみ敗北とする
            ModLog.Log("[SukutsuArena] Defeat or flee");
            EClass.player.dialogFlags[SessionFlagKeys.ArenaResult] = 2;  // 敗北
            Msg.Say(ArenaLocalization.Retreated);

            // 帰還後に自動会話
            ScheduleAutoDialog();
        }

        // クエストバトル（sukutsu_quest_battle != 0）が優先
        // isRankUpよりも明示的に設定されたsukutsu_quest_battleを優先する
        // 注意: フラグのクリアはドラマ側で行う（quest_battle_result_checkの後）
        int questBattle = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.QuestBattle)
            ? EClass.player.dialogFlags[SessionFlagKeys.QuestBattle] : 0;

        if (questBattle != 0)
        {
            // クエストバトル結果として処理
            EClass.player.dialogFlags[SessionFlagKeys.IsQuestBattleResult] = 1;
            EClass.player.dialogFlags[SessionFlagKeys.IsRankUpResult] = 0;
            ModLog.Log($"[SukutsuArena] Quest battle result flag set, questBattle={questBattle}");
        }
        else if (isRankUp)
        {
            // ランクアップ戦結果として処理
            EClass.player.dialogFlags[SessionFlagKeys.IsRankUpResult] = 1;
            EClass.player.dialogFlags[SessionFlagKeys.IsQuestBattleResult] = 0;
        }
        else
        {
            // 通常バトル
            EClass.player.dialogFlags[SessionFlagKeys.IsRankUpResult] = 0;
            EClass.player.dialogFlags[SessionFlagKeys.IsQuestBattleResult] = 0;

            // 通常戦闘で勝利した場合、キャッシュをクリア（同日に何度も同じ敵と戦えないように）
            if (result == 1)
            {
                TodaysBattleCache.ClearCache();
                ModLog.Log("[SukutsuArena] Random battle cache cleared on victory");
            }
        }

        // マスターIDを使って後処理などがあればここで行う
    }

    public void LeaveZone()
    {
        Zone zone = EClass.game.spatials.Find(uidZone) as Zone;
        if (zone != null)
        {
            EClass.pc.MoveZone(zone, new ZoneTransition
            {
                state = ZoneTransition.EnterState.Exact,
                x = returnX,
                z = returnZ
            });
        }
        else
        {
            // フォールバック
            Zone fallbackZone = EClass.game.spatials.Find(ArenaConfig.ZoneId) as Zone;
            if (fallbackZone != null)
            {
                EClass.pc.MoveZone(fallbackZone, ZoneTransition.EnterState.Center);
            }
        }
    }

    /// <summary>
    /// 直接ドラマ開始用の静的変数（dialogFlagsはintのみなので別途保存）
    /// </summary>
    public static string PendingDirectDrama { get; set; } = "";

    /// <summary>
    /// 帰還後にアリーナマスターとの会話を自動開始するよう予約
    /// または、直接ドラマを開始するよう予約
    /// </summary>
    private void ScheduleAutoDialog()
    {
        // 戦闘結果を確認
        int battleResult = EClass.player.dialogFlags.ContainsKey(SessionFlagKeys.ArenaResult)
            ? EClass.player.dialogFlags[SessionFlagKeys.ArenaResult] : 0;
        bool isVictory = battleResult == 1;

        // 直接ドラマが設定されている場合はそちらを優先
        string directDrama = isVictory ? victoryDramaId : defeatDramaId;
        if (!string.IsNullOrEmpty(directDrama))
        {
            // 直接ドラマを開始するフラグを設定（静的変数に保存）
            PendingDirectDrama = directDrama;
            EClass.player.dialogFlags[SessionFlagKeys.DirectDrama] = 1;  // フラグとして1をセット
            ModLog.Log($"[SukutsuArena] Scheduled direct drama: {directDrama} (victory={isVictory})");
            return;
        }

        // 通常の自動ダイアログ
        EClass.player.dialogFlags[SessionFlagKeys.AutoDialog] = uidMaster;
        ModLog.Log($"[SukutsuArena] Scheduled auto-dialog flag for master UID: {uidMaster}");
    }

    /// <summary>
    /// 報酬付与
    /// </summary>
    private void GiveReward()
    {
        // 結果フラグはドラマ側でクリアするのでここではクリアしない
        // EClass.player.dialogFlags["sukutsu_arena_result"] = 0;

        // ステージクリア報酬: プラチナコイン
        Thing plat = ThingGen.Create("plat");
        plat.SetNum(rewardPlat);
        EClass.pc.Pick(plat);

        // ステージクリア報酬: 媚薬
        if (rewardPotion > 0)
        {
            Thing potion = ThingGen.Create("lovepotion");
            potion.SetNum(rewardPotion);
            EClass.pc.Pick(potion);
            Msg.Say(ArenaLocalization.VictoryRewardWithPotion(rewardPlat, rewardPotion));
        }
        else
        {
            Msg.Say(ArenaLocalization.VictoryReward(rewardPlat));
        }

        // ステージ進行は廃止 - クエストシステムで管理
        // ランクアップはクエスト完了時にのみ発生
    }
}

