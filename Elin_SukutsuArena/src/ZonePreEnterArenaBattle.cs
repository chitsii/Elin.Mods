using System.Collections.Generic;
using UnityEngine;
using Elin_SukutsuArena;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.RandomBattle;
using Elin_SukutsuArena.Localization;

/// <summary>
/// アリーナ戦闘前のボス配置イベント
/// 戦闘マップに入った時にボスをスポーンさせる
/// </summary>
[GameDependency("Inheritance", "ZonePreEnterEvent", "High", "Base class for pre-enter events, Execute() signature may change")]
[GameDependency("Direct", "CharaGen.Create", "Medium", "Enemy character generation")]
[GameDependency("Direct", "EClass._zone.AddCard", "Medium", "Adding spawned enemies to zone")]
[GameDependency("Direct", "SoundManager.current", "Low", "BGM playback")]
public class ZonePreEnterArenaBattle : ZonePreEnterEvent
{
    // ステージ設定
    public string stageId = "";
    public BattleStageData stageData = null;

    public override void Execute()
    {
        ModLog.Log("[SukutsuArena] ZonePreEnterArenaBattle.Execute()");

        if (stageData == null)
        {
            Debug.LogError($"[SukutsuArena] stageData is null for stage: {stageId}");
            return;
        }

        // Biome設定（雪原など）
        if (!string.IsNullOrEmpty(stageData.Biome) && EClass._map != null)
        {
            EClass._map.config.idBiome = stageData.Biome;
            ModLog.Log($"[SukutsuArena] Set biome to: {stageData.Biome}");
        }

        // 既存のモブを掃除（プレイヤーと仲間以外）
        ClearExistingCharas();

        // マップ全体を開示（闘技場は探索不要）
        EClass._map.RevealAll();

        // プレイヤーの位置（中心のはず）
        Point centerPos = EClass._map.GetCenterPos();

        List<Chara> enemies = new List<Chara>();

        ModLog.Log($"[SukutsuArena] Using stageData for stage: {stageId}");
        SpawnEnemiesFromStageData(centerPos, enemies);

        // FreeForAllモードかどうか確認
        bool isFreeForAll = TodaysBattleCache.GetOrGenerateTodaysBattle()?.IsFreeForAll ?? false;

        // 全ての敵を敵対化
        foreach (Chara enemy in enemies)
        {
            enemy.hostility = Hostility.Enemy;
            enemy.c_originalHostility = Hostility.Enemy;
            enemy.SetEnemy(EClass.pc);
            enemy.HealAll();
            // #if DEBUG
            //             // DEBUGビルド: HPを1に設定（HealAllの後に実行）
            //             enemy.hp = 1;
            // #endif
        }

        // FreeForAll: 敵同士も敵対させる
        if (isFreeForAll && enemies.Count > 1)
        {
            ModLog.Log($"[SukutsuArena] FreeForAll mode: Setting enemies hostile to each other");
            SetEnemiesHostileToEachOther(enemies);
        }

        // 睡眠禁止のためにZoneEventQuestを追加
        // Chara.CanSleep()はZoneEventQuestがあるとfalseを返す
        if (EClass._zone.events.GetEvent<ZoneEventQuest>() == null)
        {
            EClass._zone.events.Add(new ZoneEventQuest());
        }

        // 勝利判定イベントを追加
        EClass._zone.events.Add(new ZoneEventArenaBattle());

        // 開始メッセージ
        bool isJP = EClass.core.config.lang == "JP";
        string displayName = isJP ? stageData.DisplayNameJp : stageData.DisplayNameEn;
        Msg.Say(ArenaLocalization.BattleStart(displayName));
    }

    /// <summary>
    /// 新API: stageDataから敵を生成
    /// </summary>
    private void SpawnEnemiesFromStageData(Point centerPos, List<Chara> enemies)
    {
        // BGM設定（stageDataから直接取得）
        string bgmPath = stageData?.BgmBattle;
        if (!string.IsNullOrEmpty(bgmPath))
        {
            ModLog.Log($"[SukutsuArena] Setting custom BGM from stageData: {bgmPath}");
            PlayCustomBGM(bgmPath);
        }
        else
        {
            // arenaInstanceからも試す（フォールバック）
            var arenaInstance = EClass._zone.instance as ZoneInstanceArenaBattle;
            if (arenaInstance != null && !string.IsNullOrEmpty(arenaInstance.bgmBattle))
            {
                ModLog.Log($"[SukutsuArena] Setting custom BGM from arenaInstance: {arenaInstance.bgmBattle}");
                PlayCustomBGM(arenaInstance.bgmBattle);
            }
            else
            {
                ModLog.Log($"[SukutsuArena] No custom BGM, using default (102)");
                EClass._zone.SetBGM(102);  // デフォルト戦闘BGM
            }
        }

        int enemyIndex = 0;
        foreach (var enemyConfig in stageData.Enemies)
        {
            for (int i = 0; i < enemyConfig.Count; i++)
            {
                Chara enemy = SpawnSingleEnemy(enemyConfig, centerPos, enemyIndex);
                if (enemy != null)
                {
                    enemies.Add(enemy);
                    enemyIndex++;
                }
            }
        }

        ModLog.Log($"[SukutsuArena] Spawned {enemies.Count} enemies from stageData");

        // ボス登場エフェクトの制御
        // 複数ボスがいても1回だけ表示されるように調整
        HandleBossTextDisplay(enemies);

        // ギミックの追加
        if (stageData.Gimmicks != null && stageData.Gimmicks.Count > 0)
        {
            foreach (var gimmickConfig in stageData.Gimmicks)
            {
                AddGimmickEvent(gimmickConfig);
            }
        }
    }

    /// <summary>
    /// ギミックイベントを追加
    /// </summary>
    private void AddGimmickEvent(GimmickConfig config)
    {
        if (string.IsNullOrEmpty(config.GimmickType)) return;

        switch (config.GimmickType)
        {
            case "audience_interference":
                var audienceEvent = new ZoneEventAudienceInterference
                {
                    // 基本設定
                    interval = config.Interval,
                    damage = config.Damage,
                    radius = config.Radius,
                    startDelay = config.StartDelay,
                    effectName = config.EffectName,
                    soundName = config.SoundName,
                    // エスカレーション設定
                    enableEscalation = config.EnableEscalation,
                    escalationRate = config.EscalationRate,
                    minInterval = config.MinInterval,
                    maxRadius = config.MaxRadius,
                    radiusGrowthInterval = config.RadiusGrowthInterval,
                    // アイテムドロップ設定
                    enableItemDrop = config.EnableItemDrop,
                    itemDropChance = config.ItemDropChance,
                    // 命中率設定
                    blastRadius = config.BlastRadius,
                    directHitChance = config.DirectHitChance,
                    explosionCount = config.ExplosionCount
                };
                EClass._zone.events.Add(audienceEvent);
                ModLog.Log($"[SukutsuArena] Added gimmick: {config.GimmickType} (interval={config.Interval}, damage={config.Damage}, directHit={config.DirectHitChance}, blastRadius={config.BlastRadius})");
                break;

            case "elemental_scar":
                var scarEvent = new ZoneEventElementalScar
                {
                    interval = config.Interval,
                    startDelay = config.StartDelay,
                    enableEscalation = config.EnableEscalation,
                    escalationRate = config.EscalationRate,
                    minInterval = config.MinInterval
                };
                EClass._zone.events.Add(scarEvent);
                ModLog.Log($"[SukutsuArena] Added gimmick: {config.GimmickType} (interval={config.Interval}, minInterval={config.MinInterval})");
                break;

            case "anti_magic":
                var antiMagicEvent = new ZoneEventAntiMagic();
                EClass._zone.events.Add(antiMagicEvent);
                ModLog.Log($"[SukutsuArena] Added gimmick: anti_magic");
                break;

            case "critical_damage":
                var criticalEvent = new ZoneEventCriticalDamage();
                EClass._zone.events.Add(criticalEvent);
                ModLog.Log($"[SukutsuArena] Added gimmick: critical_damage");
                break;

            case "empathetic":
                var empatheticEvent = new ZoneEventEmpathetic();
                EClass._zone.events.Add(empatheticEvent);
                ModLog.Log($"[SukutsuArena] Added gimmick: empathetic");
                break;

            case "no_healing":
                var noHealingEvent = new ZoneEventNoHealing();
                EClass._zone.events.Add(noHealingEvent);
                ModLog.Log($"[SukutsuArena] Added gimmick: no_healing");
                break;

            case "magic_affinity":
                var magicAffinityEvent = new ZoneEventMagicAffinity();
                EClass._zone.events.Add(magicAffinityEvent);
                ModLog.Log($"[SukutsuArena] Added gimmick: magic_affinity");
                break;

            case "hellish":
                // 地獄門は敵生成時に処理するため、ここでは何もしない
                ModLog.Log($"[SukutsuArena] Gimmick 'hellish' is handled during enemy generation");
                break;

            default:
                Debug.LogWarning($"[SukutsuArena] Unknown gimmick type: {config.GimmickType}");
                break;
        }
    }

    /// <summary>
    /// ボス登場エフェクトの表示を制御
    /// 複数ボスがいても1回だけエフェクトが表示されるようにする
    /// </summary>
    private void HandleBossTextDisplay(List<Chara> enemies)
    {
        bool hasShownBoss = false;

        foreach (var enemy in enemies)
        {
            if (enemy.bossText)
            {
                if (!hasShownBoss)
                {
                    // 最初のボスのみエフェクト表示を許可
                    hasShownBoss = true;
                    ModLog.Log($"[SukutsuArena] Boss text enabled for: {enemy.Name}");
                }
                else
                {
                    // 2体目以降はエフェクトを無効化
                    enemy.bossText = false;
                    ModLog.Log($"[SukutsuArena] Boss text disabled for: {enemy.Name}");
                }
            }
        }
    }

    /// <summary>
    /// 単体の敵を生成
    /// </summary>
    private Chara SpawnSingleEnemy(EnemyConfig config, Point centerPos, int index)
    {
        Chara enemy = null;

        // キャラ生成
        try
        {
            enemy = CharaGen.Create(config.CharaId);
        }
        catch
        {
            Debug.LogWarning($"[SukutsuArena] Failed to create enemy: {config.CharaId}");
        }

        // 生成失敗時のフォールバック
        if (enemy == null || enemy.id == "chicken")
        {
            if (enemy != null) enemy.Destroy();

            string fallbackId = config.IsBoss ? "orc_warrior" : "putty";
            try
            {
                enemy = CharaGen.Create(fallbackId);
                Debug.LogWarning($"[SukutsuArena] Used fallback: {fallbackId}");
            }
            catch
            {
                enemy = CharaGen.CreateFromFilter("c_neutral", 1);
            }
        }

        if (enemy == null) return null;

        // レアリティ設定（Zone追加前でもOK）
        switch (config.Rarity)
        {
            case "Superior":
                enemy.ChangeRarity(Rarity.Superior);
                break;
            case "Legendary":
                enemy.ChangeRarity(Rarity.Legendary);
                break;
            case "Mythical":
                enemy.ChangeRarity(Rarity.Mythical);
                break;
        }

        // ========================================
        // 特殊キャラクター処理
        // ========================================

        // 影の自己: プレイヤーのステータスをコピー
        if (config.CharaId == ArenaConfig.ItemIds.ShadowSelf)
        {
            ApplyShadowSelfStats(enemy);
        }

        // ボスキャラクター: 耐久（END）を10倍に設定してHP増加
        if (config.IsBoss)
        {
            ApplyBossEnduranceBoost(enemy);
            ArenaBalance.ApplyBossDamageRate(enemy);
        }

        // 位置決定
        Point pos;
        if (config.Position == "fixed" && config.PositionX != 0 && config.PositionZ != 0)
        {
            pos = new Point(config.PositionX, config.PositionZ);
        }
        else if (config.Position == "center")
        {
            pos = centerPos;
        }
        else
        {
            pos = GetSpawnPos(centerPos, 3 + (index % 3));
        }

        // マップに追加
        EClass._zone.AddCard(enemy, pos);

        // レベルスケーリング（Zone追加後に行う - SetLv内部でFeat.Applyが呼ばれるため）
        // Level=0 はランクアップ試練等でSourceChara既定LVを使うケース
        if (config.Level > 0 && config.Level > enemy.LV)
        {
            enemy.SetLv(config.Level);
            enemy.hp = enemy.MaxHP;
        }

        ModLog.Log($"[SukutsuArena] Spawned: {enemy.Name} (Lv.{enemy.LV}, {config.Rarity}, Boss={config.IsBoss}) at {pos}");

        return enemy;
    }

    /// <summary>
    /// 影の自己にプレイヤーのステータスをコピー
    /// </summary>
    private void ApplyShadowSelfStats(Chara shadow)
    {
        var pc = EClass.pc;
        if (pc == null) return;

        ModLog.Log($"[SukutsuArena] Applying Shadow Self stats from player...");

        // プレイヤーの主要ステータスをコピー
        // SKILL.cs: STR=70, END=71, DEX=72, PER=73, LER=74, WIL=75, MAG=76, CHA=77, LUC=78, SPD=79
        int[] mainStats = { 70, 71, 72, 73, 74, 75, 76, 77, 78, 79 };
        foreach (int statId in mainStats)
        {
            var pcElement = pc.elements.GetElement(statId);
            if (pcElement != null)
            {
                int pcValue = pcElement.ValueWithoutLink;
                shadow.elements.SetBase(statId, pcValue);
            }
        }

        // 戦闘関連ステータスもコピー
        // SKILL.cs: DV=64, PV=65
        int[] combatStats = { 64, 65 };
        foreach (int statId in combatStats)
        {
            var pcElement = pc.elements.GetElement(statId);
            if (pcElement != null)
            {
                shadow.elements.SetBase(statId, pcElement.ValueWithoutLink);
            }
        }

        // ========================================
        // プレイヤーの外見（PCC）をコピー
        // ========================================
        CopyPlayerAppearance(shadow, pc);

        // ========================================
        // プレイヤーの装備をコピー
        // ========================================
        CopyPlayerEquipment(shadow, pc);

        // ========================================
        // バフ効果の補正（Condition適用済みの値に合わせる）
        // ========================================
        // バフ効果の補正（Condition適用済みの値に合わせる）
        // mainStats + combatStats
        int[] buffTargetStats = { 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 64, 65 };
        int syncCount = 0;
        foreach (int statId in buffTargetStats)
        {
            var pcElem = pc.elements.GetElement(statId);
            var shadowElem = shadow.elements.GetElement(statId);
            if (pcElem == null || shadowElem == null) continue;
            int diff = pcElem.Value - shadowElem.Value;
            if (diff != 0)
            {
                shadow.elements.ModBase(statId, diff);
                syncCount++;
            }
        }
        ModLog.Log($"[SukutsuArena] Shadow: Buff synced {syncCount} stat(s)");

        // ========================================
        // アイテムドロップ無効化
        // ========================================
        shadow.noMove = false;  // 動けるようにはしておく
        // isCopy=true で SpawnLoot をスキップ（装備解除時のインデックスエラー防止）
        shadow.isCopy = true;
        ModLog.Log($"[SukutsuArena] Shadow: Item drops disabled (via isCopy=true)");

        // 影の名前をカスタマイズ（プレイヤー名を含める）
        shadow.c_altName = ArenaNameHelper.GetShadowSelfAltName(shadow, pc);

        ModLog.Log($"[SukutsuArena] Shadow Self stats applied. Name: {shadow.c_altName}");
    }

    /// <summary>
    /// プレイヤーの外見（PCC）を影にコピー
    /// </summary>
    private void CopyPlayerAppearance(Chara shadow, Chara pc)
    {
        ModLog.Log($"[SukutsuArena] Copying player appearance to shadow...");

        try
        {
            // PCCデータをコピー（キャラチップの外見）
            if (pc.pccData != null)
            {
                shadow.pccData = pc.pccData;
                ModLog.Log($"[SukutsuArena] Shadow: Copied pccData");

                // レンダラーのpccDataも更新してRenderDataを再選択
                // これにより正しいサイズ・位置で表示される
                if (shadow.renderer is CharaRenderer charaRenderer)
                {
                    charaRenderer.pccData = pc.pccData;
                    charaRenderer.SetOwner(shadow);
                    ModLog.Log($"[SukutsuArena] Shadow: Renderer pccData updated and SetOwner called");
                }
            }

            // 生物学的情報をコピー（性別のみ確実にアクセス可能）
            if (pc.bio != null && shadow.bio != null)
            {
                shadow.bio.SetGender(pc.bio.gender);
                shadow.bio.height = pc.bio.height;
                shadow.bio.weight = pc.bio.weight;
                ModLog.Log($"[SukutsuArena] Shadow: Copied bio data (gender={pc.bio.gender})");
            }

            // スキンIDをコピー
            shadow.idSkin = pc.idSkin;
            ModLog.Log($"[SukutsuArena] Shadow: Copied idSkin={pc.idSkin}");

            // レンダラーをリフレッシュ
            shadow.Refresh();
            ModLog.Log($"[SukutsuArena] Shadow appearance copy complete");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SukutsuArena] Failed to copy appearance: {ex.Message}");
        }
    }

    /// <summary>
    /// プレイヤーの装備を影にコピー
    /// </summary>
    private void CopyPlayerEquipment(Chara shadow, Chara pc)
    {
        ModLog.Log($"[SukutsuArena] Copying player equipment to shadow...");

        // 影の既存アイテムを全削除
        shadow.things.DestroyAll();

        // プレイヤーの装備スロットをイテレート（武器・防具のみ）
        // コンテナスロット(elementId=44)は除外
        foreach (var slot in pc.body.slots)
        {
            if (slot.thing == null) continue;

            // コンテナスロット(44)をスキップ
            if (slot.elementId == 44) continue;

            try
            {
                // 装備のコピーを作成
                Thing copy = slot.thing.Duplicate(1);
                if (copy != null)
                {
                    // コピーした装備はNPCプロパティとしてマーク（ドロップ防止）
                    copy.isNPCProperty = true;
                    // スロット番号をリセット（Duplicateで元のスロット番号が残る場合があるため）
                    // Equip時に影のスロット番号に更新される
                    copy.c_equippedSlot = 0;

                    // 影に装備させる
                    shadow.AddThing(copy);
                    shadow.body.Equip(copy);

                    ModLog.Log($"[SukutsuArena] Shadow equipped: {copy.Name} in slot {slot.element?.alias ?? "unknown"}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SukutsuArena] Failed to copy equipment: {slot.thing.Name} - {ex.Message}");
            }
        }

        // 装備後にステータスを再計算
        shadow.Refresh();
        ModLog.Log($"[SukutsuArena] Shadow equipment copy complete");
    }

    /// <summary>
    /// ボスキャラクターの耐久を10倍に設定
    /// </summary>
    private void ApplyBossEnduranceBoost(Chara boss)
    {
        // Element ID: END = 71
        const int END_ELEMENT_ID = 71;
        const int BOSS_ENDURANCE_MULTIPLIER = 3;  // LV調整に伴い10→3に変更

        var endElement = boss.elements.GetElement(END_ELEMENT_ID);
        int currentEnd = endElement?.ValueWithoutLink ?? 10;
        int boostedEnd = currentEnd * BOSS_ENDURANCE_MULTIPLIER;

        boss.elements.SetBase(END_ELEMENT_ID, boostedEnd);

        // HPを新しいMaxHPに合わせてフル回復
        boss.hp = boss.MaxHP;

        ModLog.Log($"[SukutsuArena] Boss Endurance Boost: {boss.Name} END {currentEnd} -> {boostedEnd} (x{BOSS_ENDURANCE_MULTIPLIER}), HP={boss.hp}");
    }

    /// <summary>
    /// 中心から指定距離離れた有効な座標を取得
    /// </summary>
    private Point GetSpawnPos(Point center, int distance)
    {
        for (int i = 0; i < 20; i++)
        {
            int dx = EClass.rnd(distance * 2 + 1) - distance;
            int dy = EClass.rnd(distance * 2 + 1) - distance;
            // 距離制限
            if (Mathf.Abs(dx) + Mathf.Abs(dy) < 2) continue; // 近すぎない

            Point p = new Point(center.x + dx, center.z + dy);
            if (p.IsValid && !p.IsBlocked && !p.HasChara)
            {
                return p;
            }
        }
        return center; // 失敗時は中心
    }

    /// <summary>
    /// FreeForAllモード: 敵同士を敵対させる
    /// </summary>
    private void SetEnemiesHostileToEachOther(List<Chara> enemies)
    {
        // 各敵に対して、他の全ての敵を敵として設定
        foreach (var enemy in enemies)
        {
            foreach (var other in enemies)
            {
                if (enemy == other) continue;

                // 互いに敵として認識させる
                enemy.SetEnemy(other);
            }
        }

        ModLog.Log($"[SukutsuArena] FreeForAll: {enemies.Count} enemies are now hostile to each other");
    }

    /// <summary>
    /// 既存のキャラを削除（プレイヤー・仲間・特殊NPC以外）
    /// </summary>
    private void ClearExistingCharas()
    {
        List<Chara> toRemove = new List<Chara>();
        foreach (Chara c in EClass._map.charas)
        {
            if (c.IsPC || c.IsPCFaction) continue;
            toRemove.Add(c);
        }

        foreach (Chara c in toRemove)
        {
            c.Destroy();
        }
    }

    /// <summary>
    /// カスタムBGMを再生（ドラマと同じ方式）
    /// </summary>
    /// <param name="bgmPath">BGMパス（例: "BGM/Battle_Kain_Requiem"）</param>
    private void PlayCustomBGM(string bgmPath)
    {
        try
        {
            ModLog.Log($"[SukutsuArena] Attempting to play BGM: {bgmPath}");
            var data = SoundManager.current.GetData(bgmPath);
            if (data != null)
            {
                ModLog.Log($"[SukutsuArena] Found BGM data, type: {data.GetType().Name}");
                if (data is BGMData bgm)
                {
                    ModLog.Log($"[SukutsuArena] Playing as BGM with haltPlaylist");
                    LayerDrama.haltPlaylist = true;  // ゾーンBGMによる上書きを防止
                    SoundManager.current.PlayBGM(bgm);
                }
                else
                {
                    ModLog.Log($"[SukutsuArena] Data is not BGMData, playing as sound");
                    SoundManager.current.Play(data);
                }
            }
            else
            {
                Debug.LogWarning($"[SukutsuArena] BGM not found: {bgmPath}, using default");
                EClass._zone.SetBGM(102);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SukutsuArena] Error playing BGM: {ex.Message}\n{ex.StackTrace}");
            EClass._zone.SetBGM(102);
        }
    }

}

