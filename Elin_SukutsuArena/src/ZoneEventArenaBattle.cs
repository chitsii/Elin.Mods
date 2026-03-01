using UnityEngine;
using Elin_SukutsuArena;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Flags;
using Elin_SukutsuArena.Localization;

/// <summary>
/// アリーナ戦闘進行監視イベント
/// 敵の全滅を監視し、勝利時にゾーン移動を行う
/// </summary>
[GameDependency("Inheritance", "ZoneEvent", "High", "Base class for zone events, OnTick/OnCharaDie may change")]
[GameDependency("Direct", "EClass._map.charas", "Medium", "Character enumeration for victory check")]
[GameDependency("Direct", "Chara.IsHostile", "Medium", "Hostility check for enemy detection")]
[GameDependency("Direct", "Chara.Revive", "Medium", "Player revival on defeat")]
public class ZoneEventArenaBattle : ZoneEvent
{
    private const int BossPhaseStateKey = 9910;
    private float checkTimer;
    private bool isGameFinished;
    private float victoryTimer = -1f;
    private float phaseTransitionCooldown;

    public override void OnTick()
    {
        // 戦闘中の帰還を禁止
        if (EClass.player.returnInfo != null)
        {
            EClass.player.returnInfo = null;
            Msg.Say(ArenaLocalization.CannotReturnDuringBattle);
        }

        // 勝利後の待機処理
        if (victoryTimer >= 0f)
        {
            victoryTimer -= Core.delta;
            if (victoryTimer <= 0f)
            {
                var arenaInstance = EClass._zone.instance as ZoneInstanceArenaBattle;
                if (arenaInstance != null)
                {
                    arenaInstance.LeaveZone();
                }
                else
                {
                    // フォールバック（通常ありえない）
                    Zone zone = EClass.game.spatials.Find(ArenaConfig.ZoneId) as Zone;
                    if (zone != null)
                    {
                        EClass.pc.MoveZone(zone, new ZoneTransition
                        {
                            state = ZoneTransition.EnterState.Center
                        });
                    }
                }
                victoryTimer = -1f; // 完了
            }
            return;
        }

        if (isGameFinished) return;

        if (phaseTransitionCooldown > 0f)
        {
            phaseTransitionCooldown -= Core.delta;
            if (phaseTransitionCooldown > 0f)
            {
                return;
            }
        }

        checkTimer += Core.delta;
        if (checkTimer < 1.0f) return;
        checkTimer = 0f;

        // ボスのフェーズ切替監視（HP閾値）
        TryHandleBossPhases();

        // 敵の生存確認
        bool enemyExists = false;
        foreach (Chara c in EClass._map.charas)
        {
            if (c != null && c.IsHostile() && !c.isDead && !c.IsPC && !c.IsPCFaction)
            {
                enemyExists = true;
                break;
            }
        }

        if (!enemyExists)
        {
            // 勝利処理
            isGameFinished = true;

            // ゾーンインスタンスを取得してフラグ設定
            var arenaInstance = EClass._zone.instance as ZoneInstanceArenaBattle;
            if (arenaInstance != null)
            {
                EClass.player.dialogFlags[SessionFlagKeys.ArenaResult] = 1; // 勝利フラグ

                // 貢献度システムは廃止 - クエストシステムで進行を管理

                // ログとエフェクト
                Msg.Say(ArenaLocalization.EnemiesDefeated);

                EClass.Sound.Play("questComplete");
                PlayVictoryBGM(arenaInstance);

                // 少し待ってから帰還 (3秒後にタイマーで実行)
                victoryTimer = 3.0f;
            }
        }
    }

    public override void OnCharaDie(Chara c)
    {
        ModLog.Log($"[SukutsuArena] OnCharaDie called: chara={c?.id}, IsPC={c?.IsPC}");

        if (c == null) return;

        if (!c.IsPC && !c.IsPCFactionOrMinion && TryGetBossPhaseConfig(c, out var config))
        {
            int currentPhase = c.GetInt(BossPhaseStateKey);
            if (currentPhase < 1 && config.Phase2Id != null)
            {
                TransformBossPhase(c, config.Phase2Id, 1, config.PhaseLineSpeaker);
                return;
            }
            if (currentPhase < 2 && config.Phase3Id != null)
            {
                TransformBossPhase(c, config.Phase3Id, 2, config.PhaseLineSpeaker);
                return;
            }
            if (currentPhase < 3 && config.Phase4Id != null)
            {
                TransformBossPhase(c, config.Phase4Id, 3, config.PhaseLineSpeaker);
                return;
            }
        }

        if (c.IsPC)
        {
            // 敗北処理
            var arenaInstance = EClass._zone.instance as ZoneInstanceArenaBattle;
            ModLog.Log($"[SukutsuArena] OnCharaDie PC died, arenaInstance={arenaInstance != null}, defeatDramaId={arenaInstance?.defeatDramaId}");
            if (arenaInstance != null)
            {
                // 敗北フラグを設定
                EClass.player.dialogFlags[SessionFlagKeys.ArenaResult] = 2; // 敗北

                // defeatDramaIdが設定されている場合は直接ドラマを開始
                // OnLeaveZoneより前にフラグを設定する必要がある（Zone.Activateのタイミングの問題）
                if (!string.IsNullOrEmpty(arenaInstance.defeatDramaId))
                {
                    ZoneInstanceArenaBattle.PendingDirectDrama = arenaInstance.defeatDramaId;
                    EClass.player.dialogFlags[SessionFlagKeys.DirectDrama] = 1;
                    ModLog.Log($"[SukutsuArena] OnCharaDie: Direct drama scheduled: {arenaInstance.defeatDramaId}");
                }
                else
                {
                    // 通常の自動会話フラグをセット
                    EClass.player.dialogFlags[SessionFlagKeys.AutoDialog] = arenaInstance.uidMaster;
                }

                Msg.Say(ArenaLocalization.ForcedRetreat);

                // アリーナ戦闘での死亡ペナルティを無効化
                // バニラの preventDeathPenalty フラグを使用（遺書と同じ仕組み）
                // Revive() 内で消費されるワンショットフラグ
                EClass.player.preventDeathPenalty = true;

                // 強制復活 (HP全快、メッセージなし)
                c.Revive(null, false);

                // 退出
                arenaInstance.LeaveZone();
            }
        }
    }

    private void TryHandleBossPhases()
    {
        var pending = new System.Collections.Generic.List<(Chara c, string nextId, int nextPhase, ArenaPhaseDialog.Speaker speaker)>();

        foreach (Chara c in EClass._map.charas)
        {
            if (c == null || c.isDead || c.IsPC || c.IsPCFactionOrMinion) continue;

            if (!TryGetBossPhaseConfig(c, out var config)) continue;

            int currentPhase = c.GetInt(BossPhaseStateKey);
            int hpPct = (int)(c.hp * 100 / Mathf.Max(1, c.MaxHP));

            if (currentPhase < 1 && config.Phase2Id != null && hpPct <= config.Phase2Hp)
            {
                pending.Add((c, config.Phase2Id, 1, config.PhaseLineSpeaker));
                continue;
            }

            if (currentPhase < 2 && config.Phase3Id != null && hpPct <= config.Phase3Hp)
            {
                pending.Add((c, config.Phase3Id, 2, config.PhaseLineSpeaker));
                continue;
            }

            if (currentPhase < 3 && config.Phase4Id != null && hpPct <= config.Phase4Hp)
            {
                pending.Add((c, config.Phase4Id, 3, config.PhaseLineSpeaker));
                continue;
            }
        }

        foreach (var item in pending)
        {
            if (item.c == null || item.c.isDead) continue;
            TransformBossPhase(item.c, item.nextId, item.nextPhase, item.speaker);
        }
    }

    private void TransformBossPhase(Chara oldBoss, string nextId, int nextPhase, ArenaPhaseDialog.Speaker speaker)
    {
        if (oldBoss == null || EClass._zone == null) return;

        Chara newBoss = CharaGen.Create(nextId);
        if (newBoss == null)
        {
            Debug.LogWarning($"[SukutsuArena] Phase change failed: could not create '{nextId}'");
            return;
        }

        newBoss.SetLv(oldBoss.LV);
        newBoss.c_altName = oldBoss.c_altName;
        ArenaBalance.ApplyBossDamageRate(newBoss);

        Point spawnPoint = oldBoss.pos;
        EClass._zone.AddCard(newBoss, spawnPoint);

        // 影の自己: フェーズ変化後もプレイヤー外見を維持
        if (IsShadowSelfId(nextId) || IsShadowSelfId(oldBoss.id))
        {
            ApplyShadowSelfStatsFromPlayer(newBoss);
        }

        newBoss.SetHostility(oldBoss.OriginalHostility);
        if (oldBoss.IsHostile())
        {
            newBoss.SetHostility(Hostility.Enemy);
        }

        if (oldBoss.enemy != null && !oldBoss.enemy.isDead)
        {
            newBoss.SetEnemy(oldBoss.enemy);
        }

        newBoss.SetInt(BossPhaseStateKey, nextPhase);

        string line = ArenaPhaseDialog.GetLine(speaker, nextPhase);
        if (!string.IsNullOrEmpty(line))
        {
            if (newBoss.renderer != null)
            {
                newBoss.renderer.Say(line, default(Color), 0f);
            }
            else
            {
                Msg.Say(line, newBoss);
            }
        }

        newBoss.PlayEffect("buff");
        newBoss.PlaySound("boost2");

        oldBoss.PlayEffect("vanish");
        oldBoss.PlaySound("identify");

        // フェーズ遷移時は手持ちアイテムをドロップさせない
        if (oldBoss.held != null)
        {
            oldBoss.held.Destroy();
        }
        oldBoss.Destroy();
        ModLog.Log($"[SukutsuArena] Boss phase changed: {oldBoss.id} -> {nextId} (phase={nextPhase})");
        phaseTransitionCooldown = 0.5f;
    }

    private static bool IsShadowSelfId(string charaId)
    {
        if (string.IsNullOrEmpty(charaId)) return false;
        string baseId = ArenaConfig.ItemIds.ShadowSelf;
        return charaId.Equals(baseId, System.StringComparison.OrdinalIgnoreCase)
            || charaId.StartsWith(baseId + "_", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 影の自己: プレイヤー外見・装備・主要能力をコピー（初期生成と同等）
    /// </summary>
    private void ApplyShadowSelfStatsFromPlayer(Chara shadow)
    {
        var pc = EClass.pc;
        if (pc == null || shadow == null) return;

        ModLog.Log($"[SukutsuArena] Shadow Self phase: Applying player copy...");

        // プレイヤーの主要ステータスをコピー
        int[] mainStats = { 70, 71, 73, 74, 75, 76, 77, 79 };
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
        int[] combatStats = { 152, 153 };
        foreach (int statId in combatStats)
        {
            var pcElement = pc.elements.GetElement(statId);
            if (pcElement != null)
            {
                int pcValue = pcElement.ValueWithoutLink;
                shadow.elements.SetBase(statId, pcValue);
            }
        }

        CopyPlayerAppearance(shadow, pc);
        CopyPlayerEquipment(shadow, pc);

        // アイテムドロップ無効化
        shadow.noMove = false;
        shadow.isCopy = true;

        // 影の名前をカスタマイズ（プレイヤー名を含める）
        shadow.c_altName = ArenaNameHelper.GetShadowSelfAltName(shadow, pc);

        ModLog.Log($"[SukutsuArena] Shadow Self phase: Player copy applied.");
    }

    /// <summary>
    /// プレイヤーの外見（PCC）を影にコピー
    /// </summary>
    private void CopyPlayerAppearance(Chara shadow, Chara pc)
    {
        try
        {
            if (pc.pccData != null)
            {
                shadow.pccData = pc.pccData;
                if (shadow.renderer is CharaRenderer charaRenderer)
                {
                    charaRenderer.pccData = pc.pccData;
                    charaRenderer.SetOwner(shadow);
                }
            }

            if (pc.bio != null && shadow.bio != null)
            {
                shadow.bio.SetGender(pc.bio.gender);
                shadow.bio.height = pc.bio.height;
                shadow.bio.weight = pc.bio.weight;
            }

            shadow.idSkin = pc.idSkin;
            shadow.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SukutsuArena] Failed to copy appearance (phase): {ex.Message}");
        }
    }

    /// <summary>
    /// プレイヤーの装備を影にコピー
    /// </summary>
    private void CopyPlayerEquipment(Chara shadow, Chara pc)
    {
        // 影の既存アイテムを全削除
        shadow.things.DestroyAll();

        foreach (var slot in pc.body.slots)
        {
            if (slot.thing == null) continue;
            if (slot.elementId == 44) continue; // コンテナスロット除外

            try
            {
                Thing copy = slot.thing.Duplicate(1);
                if (copy != null)
                {
                    copy.isNPCProperty = true;
                    copy.c_equippedSlot = 0;

                    shadow.AddThing(copy);
                    shadow.body.Equip(copy);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SukutsuArena] Failed to copy equipment (phase): {slot.thing.Name} - {ex.Message}");
            }
        }

        shadow.Refresh();
    }

    private static bool TryGetBossPhaseConfig(Chara c, out BossPhaseConfig config)
    {
        config = default;
        if (c == null) return false;

        string tagRaw = string.Join(",", c.source?.tag ?? System.Array.Empty<string>());
        if (string.IsNullOrEmpty(tagRaw)) return false;

        var tags = tagRaw.Split(',');
        bool isBoss = false;
        var map = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (string t in tags)
        {
            string s = t.Trim();
            if (s.Length == 0) continue;
            if (s.Equals("boss", System.StringComparison.OrdinalIgnoreCase))
            {
                isBoss = true;
                continue;
            }
            int eq = s.IndexOf('=');
            if (eq <= 0 || eq >= s.Length - 1) continue;
            string key = s.Substring(0, eq).Trim();
            string val = s.Substring(eq + 1).Trim();
            if (key.Length == 0 || val.Length == 0) continue;
            map[key] = val;
        }

        if (!isBoss) return false;

        config = new BossPhaseConfig
        {
            Phase2Id = map.TryGetValue("phase2", out var p2) ? p2 : null,
            Phase3Id = map.TryGetValue("phase3", out var p3) ? p3 : null,
            Phase4Id = map.TryGetValue("phase4", out var p4) ? p4 : null,
            Phase2Hp = map.TryGetValue("phase2_hp", out var h2) ? h2.ToInt() : 70,
            Phase3Hp = map.TryGetValue("phase3_hp", out var h3) ? h3.ToInt() : 40,
            Phase4Hp = map.TryGetValue("phase4_hp", out var h4) ? h4.ToInt() : 20,
            PhaseLineSpeaker = ParsePhaseLineSpeaker(map.TryGetValue("phase_line", out var phaseLineKey) ? phaseLineKey : null),
        };

        if (config.Phase2Id == null && config.Phase3Id == null && config.Phase4Id == null) return false;
        return true;
    }

    private static ArenaPhaseDialog.Speaker ParsePhaseLineSpeaker(string key)
    {
        if (string.IsNullOrEmpty(key)) return ArenaPhaseDialog.Speaker.Generic;
        switch (key.Trim().ToLowerInvariant())
        {
            case "astaroth":
                return ArenaPhaseDialog.Speaker.Astaroth;
            case "shadowself":
            case "shadow_self":
            case "shadow":
                return ArenaPhaseDialog.Speaker.ShadowSelf;
            case "generic":
            default:
                return ArenaPhaseDialog.Speaker.Generic;
        }
    }

    private struct BossPhaseConfig
    {
        public string Phase2Id;
        public string Phase3Id;
        public string Phase4Id;
        public int Phase2Hp;
        public int Phase3Hp;
        public int Phase4Hp;
        public ArenaPhaseDialog.Speaker PhaseLineSpeaker;
    }

    /// <summary>
    /// 勝利BGMを再生（カスタムBGM対応）
    /// </summary>
    private void PlayVictoryBGM(ZoneInstanceArenaBattle arenaInstance)
    {
        if (arenaInstance != null && !string.IsNullOrEmpty(arenaInstance.bgmVictory))
        {
            try
            {
                ModLog.Log($"[SukutsuArena] Playing victory BGM: {arenaInstance.bgmVictory}");
                var data = SoundManager.current.GetData(arenaInstance.bgmVictory);
                if (data != null && data is BGMData bgm)
                {
                    LayerDrama.haltPlaylist = true;  // ゾーンBGMによる上書きを防止
                    SoundManager.current.PlayBGM(bgm);
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SukutsuArena] Failed to play victory BGM: {ex.Message}");
            }
        }
        // フォールバック
        EClass._zone.SetBGM(106);
    }
}

