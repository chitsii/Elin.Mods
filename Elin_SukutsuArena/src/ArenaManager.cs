using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using DG.Tweening;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using Elin_SukutsuArena.Localization;
using Elin_SukutsuArena.RandomBattle;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// アリーナ管理クラス
    /// CWL eval から呼び出すためにstatic メソッドを提供
    /// </summary>
    public static class ArenaManager
    {
        /// <summary>
        /// ランク情報をログに表示（CWL evalから呼び出し）
        /// </summary>
        public static void ShowRankInfoLog()
        {
            var storage = ArenaContext.I?.Storage;
            if (storage != null)
            {
                int currentRank = storage.GetInt(ArenaFlagKeys.Rank, 0);
                int inferredRank = InferRankFromQuestCompletion();
                if (inferredRank > currentRank)
                {
                    storage.SetInt(ArenaFlagKeys.Rank, inferredRank);
                    currentRank = inferredRank;
                }
                int rankValue = currentRank;
                string rankName = ArenaLocalization.GetRankDisplayName(rankValue);
                Msg.Say(ArenaLocalization.CurrentRankMessage(rankName));
                return;
            }

            int rank = (int)ArenaContext.I.Player.Rank;
            string rankDisplayName = ArenaLocalization.GetRankDisplayName(rank);
            Msg.Say(ArenaLocalization.CurrentRankMessage(rankDisplayName));
        }

        private static int InferRankFromQuestCompletion()
        {
            var qm = ArenaQuestManager.Instance;
            if (qm == null) return 0;

            // Highest completed rank-up quest determines rank
            var rankUpMapping = new Dictionary<string, int>
            {
                { "02_rank_up_G", 1 },
                { "04_rank_up_F", 2 },
                { "06_rank_up_E", 3 },
                { "10_rank_up_D", 4 },
                { "09_rank_up_C", 5 },
                { "11_rank_up_B", 6 },
                { "12_rank_up_A", 7 },
                { "15_vs_balgas", 8 },
            };

            int inferredRank = 0;
            foreach (var kvp in rankUpMapping.OrderByDescending(x => x.Value))
            {
                if (qm.IsQuestCompleted(kvp.Key))
                {
                    inferredRank = kvp.Value;
                    break;
                }
            }

            return inferredRank;
        }

        /// <summary>
        /// 指定したドラマを開始する（CWL evalから呼び出し）
        /// </summary>
        public static void StartDrama(string dramaName)
        {
            ModLog.Log($"[SukutsuArena] StartDrama called with: {dramaName}");

            if (LayerDrama.Instance == null)
            {
                Debug.LogError($"[SukutsuArena] LayerDrama.Instance is null! Cannot schedule next drama.");
                return;
            }

            // 現在のドラマが終了した後に、次のドラマを開始する
            LayerDrama.Instance.SetOnKill(() =>
            {
                ModLog.Log($"[SukutsuArena] SetOnKill callback triggered for drama: {dramaName}. Scheduling delayed activation.");

                // 即座ではなく少し待つ（UIの破棄タイミング回避）
                DOVirtual.DelayedCall(0.05f, () =>
                {
                    ModLog.Log($"[SukutsuArena] DelayedCall executed. Attempting to activate drama: {dramaName}");
                    try
                    {
                        // targetにPCを指定することで、CWLのif_flag等がEClass.player.dialogFlagsを参照できるようにする
                        LayerDrama.Activate(dramaName, null, null, EClass.pc, null, null);
                        ModLog.Log($"[SukutsuArena] LayerDrama.Activate returned successfully.");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SukutsuArena] Failed to activate drama '{dramaName}': {ex}");
                    }
                });
            });
            ModLog.Log($"[SukutsuArena] SetOnKill registered successfully.");
        }

        /// <summary>
        /// メッセージをログに表示してからドラマを開始する
        /// CWLのsayアクション後にevalが実行されない問題を回避するための一括処理
        /// Msg.Sayでログに表示し、ドラマを開始する
        /// </summary>
        /// <param name="actorId">発言者のキャラクターID</param>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="dramaName">開始するドラマ名</param>
        public static void SayAndStartDrama(string actorId, string message, string dramaName)
        {
            ModLog.Log($"[SukutsuArena] SayAndStartDrama called: actor={actorId}, drama={dramaName}");

            // アクター名を取得してログに表示
            var actor = EClass._zone.FindChara(actorId);
            if (actor != null)
            {
                Msg.Say($"{actor.Name}: {message}");
            }
            else
            {
                Msg.Say(message);
            }

            // ドラマを開始
            StartDrama(dramaName);
        }

        /// <summary>
        /// ステージIDを指定して戦闘を開始（新API）
        /// JSONファイルからステージ設定を読み込んで戦闘を開始する
        /// </summary>
        /// <param name="stageId">ステージID</param>
        /// <param name="master">アリーナマスター（戻り先）</param>
        public static void StartBattleByStage(string stageId, Chara master)
        {
            ModLog.Log($"[SukutsuArena] StartBattleByStage called: stageId={stageId}");

            if (master == null)
            {
                Debug.LogError("[SukutsuArena] Master is null!");
                return;
            }

            // パッケージパスを取得
            string packagePath = GetPackagePath();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogError("[SukutsuArena] Could not determine package path!");
                return;
            }

            // ステージ設定を取得
            var stageData = BattleStageLoader.GetStage(stageId, packagePath);
            if (stageData == null)
            {
                Debug.LogError($"[SukutsuArena] Stage not found: {stageId}");
                return;
            }

            // 一時戦闘マップを作成
            Zone battleZone = SpatialGen.CreateInstance(stageData.ZoneType, new ZoneInstanceArenaBattle
            {
                uidMaster = master.uid,
                returnX = master.pos.x,
                returnZ = master.pos.z,
                uidZone = EClass._zone.uid,
                rewardPlat = stageData.RewardPlat,
                isRankUp = stageId.StartsWith("rank_"),
                stageId = stageId,
                bgmBattle = stageData.BgmBattle,
                bgmVictory = stageData.BgmVictory
            });

            // 敵配置イベントを追加
            battleZone.events.AddPreEnter(new ZonePreEnterArenaBattle
            {
                stageId = stageId,
                stageData = stageData
            });

            // 戦闘監視イベントを追加
            battleZone.events.Add(new ZoneEventArenaBattle());

            ModLog.Log($"[SukutsuArena] Created battle zone for stage: {stageId}");

            // ダイアログ終了後にゾーン移動
            LayerDrama.Instance?.SetOnKill(() =>
            {
                ModLog.Log($"[SukutsuArena] Moving to battle zone: {stageId}");
                EClass.pc.MoveZone(battleZone, ZoneTransition.EnterState.Center);
            });
        }

        /// <summary>
        /// ステージIDを指定して戦闘を開始（マスターIDで検索）
        /// マスターが見つからない場合は、戻り先なしで戦闘を開始
        /// </summary>
        public static void StartBattleByStage(string stageId, string masterId)
        {
            var master = EClass._zone.FindChara(masterId);
            if (master == null)
            {
                Debug.LogWarning($"[SukutsuArena] Master not found: {masterId}, starting battle without return point");
                StartBattleByStageWithoutMaster(stageId);
                return;
            }
            StartBattleByStage(stageId, master);
        }

        /// <summary>
        /// マスターなしで戦闘を開始（最終決戦など、ドラマ空間から直接戦闘に入る場合）
        /// 戦闘後は直接エピローグドラマを開始（NPCに依存しない）
        /// </summary>
        public static void StartBattleByStageWithoutMaster(string stageId)
        {
            StartBattleByStageWithoutMaster(stageId, null, null);
        }

        /// <summary>
        /// マスターなしで戦闘を開始（直接ドラマ指定版）
        /// </summary>
        /// <param name="stageId">ステージID</param>
        /// <param name="victoryDramaId">勝利時に開始するドラマID（nullの場合は自動ダイアログ）</param>
        /// <param name="defeatDramaId">敗北時に開始するドラマID（nullの場合は自動ダイアログ）</param>
        public static void StartBattleByStageWithoutMaster(string stageId, string victoryDramaId, string defeatDramaId)
        {
            ModLog.Log($"[SukutsuArena] StartBattleByStageWithoutMaster called: stageId={stageId}, victoryDrama={victoryDramaId}, defeatDrama={defeatDramaId}");

            string packagePath = GetPackagePath();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogError("[SukutsuArena] Could not determine package path!");
                return;
            }

            var stageData = BattleStageLoader.GetStage(stageId, packagePath);
            if (stageData == null)
            {
                Debug.LogError($"[SukutsuArena] Stage not found: {stageId}");
                return;
            }

            // アリーナゾーンを取得
            Zone arenaZone = EClass.game.spatials.Find(ArenaConfig.ZoneId) as Zone;
            if (arenaZone == null)
            {
                Debug.LogError("[SukutsuArena] Arena zone not found!");
                return;
            }

            // 一時戦闘マップを作成（戻り先はアリーナゾーン、直接ドラマ開始）
            Zone battleZone = SpatialGen.CreateInstance(stageData.ZoneType, new ZoneInstanceArenaBattle
            {
                uidMaster = 0,  // NPCダイアログは使用しない
                returnX = 0,
                returnZ = 0,
                uidZone = arenaZone.uid,
                rewardPlat = stageData.RewardPlat,
                isRankUp = stageId.StartsWith("rank_"),
                stageId = stageId,
                bgmBattle = stageData.BgmBattle,
                bgmVictory = stageData.BgmVictory,
                victoryDramaId = victoryDramaId ?? "",
                defeatDramaId = defeatDramaId ?? ""
            });

            battleZone.events.AddPreEnter(new ZonePreEnterArenaBattle
            {
                stageId = stageId,
                stageData = stageData
            });

            battleZone.events.Add(new ZoneEventArenaBattle());

            ModLog.Log($"[SukutsuArena] Created battle zone for stage: {stageId}, return to arena UID: {arenaZone.uid}");

            LayerDrama.Instance?.SetOnKill(() =>
            {
                ModLog.Log($"[SukutsuArena] Moving to battle zone: {stageId}");
                EClass.pc.MoveZone(battleZone, ZoneTransition.EnterState.Center);
            });
        }

        /// <summary>
        /// パッケージパスを取得
        /// </summary>
        private static string GetPackagePath()
        {
            // Pluginアセンブリの場所からパスを取得
            var modPath = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
            if (!string.IsNullOrEmpty(modPath))
            {
                var packagePath = Path.Combine(modPath, "Package");
                if (Directory.Exists(packagePath))
                {
                    return packagePath;
                }
            }

            // フォールバック: 既知のパスを試す
            string[] possiblePaths = new[]
            {
                Path.Combine(Application.dataPath, "..", "Package", "Elin_SukutsuArena", "Package"),
                Path.Combine(Application.dataPath, "..", "Mods", "Elin_SukutsuArena", "Package"),
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        // ============================================================
        // ランダムバトルシステム（1日1回制限・リロール機能付き）
        // ============================================================

        /// <summary>
        /// ランダムバトルを開始（本日のバトルを使用）
        /// パーティの弱点を分析し、それに対応する敵を生成して戦闘を開始する
        /// </summary>
        /// <param name="master">アリーナマスター（戻り先）</param>
        public static void StartRandomBattle(Chara master)
        {
            ModLog.Log("[SukutsuArena] StartRandomBattle called");

            if (master == null)
            {
                Debug.LogError("[SukutsuArena] Master is null for random battle!");
                return;
            }

            try
            {
                // 本日のバトルを取得（キャッシュから）
                var todaysBattle = TodaysBattleCache.GetOrGenerateTodaysBattle();
                if (todaysBattle == null)
                {
                    Debug.LogError("[SukutsuArena] Failed to get today's battle!");
                    return;
                }

                var stageData = todaysBattle.StageData;

                ModLog.Log($"[SukutsuArena] Random battle reward: {todaysBattle.RewardPlat} plat, {todaysBattle.RewardPotion} potion");

                // 一時戦闘マップを作成
                Zone battleZone = SpatialGen.CreateInstance(stageData.ZoneType, new ZoneInstanceArenaBattle
                {
                    uidMaster = master.uid,
                    returnX = master.pos.x,
                    returnZ = master.pos.z,
                    uidZone = EClass._zone.uid,
                    rewardPlat = todaysBattle.RewardPlat,
                    rewardPotion = todaysBattle.RewardPotion,
                    isRankUp = false,
                    stageId = "random_battle",
                    bgmBattle = stageData.BgmBattle,
                    bgmVictory = stageData.BgmVictory
                });

                // 敵配置イベントを追加
                battleZone.events.AddPreEnter(new ZonePreEnterArenaBattle
                {
                    stageId = "random_battle",
                    stageData = stageData
                });

                // 戦闘監視イベントを追加
                battleZone.events.Add(new ZoneEventArenaBattle());

                ModLog.Log($"[SukutsuArena] Created random battle zone with {stageData.TotalEnemyCount} enemies");

                // ダイアログ終了後にゾーン移動
                LayerDrama.Instance?.SetOnKill(() =>
                {
                    ModLog.Log("[SukutsuArena] Moving to random battle zone");
                    EClass.pc.MoveZone(battleZone, ZoneTransition.EnterState.Center);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SukutsuArena] Error starting random battle: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// ランダムバトルを開始（マスターIDで検索）
        /// </summary>
        public static void StartRandomBattle(string masterId)
        {
            var master = EClass._zone.FindChara(masterId);
            if (master == null)
            {
                Debug.LogError($"[SukutsuArena] Master not found: {masterId}");
                return;
            }
            StartRandomBattle(master);
        }

        /// <summary>
        /// 本日のランダムバトルをリロール（プラチナコイン消費）
        /// CWL evalから呼び出される
        /// </summary>
        /// <returns>リロール成功したらtrue</returns>
        public static bool RerollTodaysBattle()
        {
            return TodaysBattleCache.RerollTodaysBattle();
        }

        /// <summary>
        /// ランダムバトルのプレビュー情報を取得（本日のバトル）
        /// </summary>
        public static (string enemyInfo, string spawnInfo, string gimmickInfo, int rewardPlat, int rewardPotion) GetRandomBattlePreview()
        {
            return TodaysBattleCache.GetTodaysBattlePreview();
        }

        /// <summary>
        /// ランダムバトルのプレビューダイアログを表示（ローカライズ対応）
        /// CWL evalから呼び出される
        /// </summary>
        public static void ShowRandomBattlePreviewDialog()
        {
            var preview = GetRandomBattlePreview();
            var text = Localization.ArenaLocalization.FormatBattlePreview(
                preview.enemyInfo,
                preview.spawnInfo,
                preview.gimmickInfo,
                preview.rewardPlat,
                preview.rewardPotion
            );
            Dialog.Ok(text);
        }

        /// <summary>
        /// リロールに必要なプラチナコインを所持しているか確認
        /// </summary>
        public static bool CanAffordReroll()
        {
            return EClass.pc.GetCurrency("plat") >= TodaysBattleCache.REROLL_COST;
        }

        /// <summary>
        /// リロールコストを取得
        /// </summary>
        public static int GetRerollCost()
        {
            return TodaysBattleCache.REROLL_COST;
        }

        // ============================================================
        // 永久バフ付与システム（ランク昇格報酬）
        // ============================================================

        /// <summary>
        /// 闘志フィート付与/レベルアップ
        /// ランク昇格時にフィート「闘志（Arena Spirit）」を付与またはレベルアップする。
        /// </summary>
        /// <param name="level">フィートレベル (1-7)</param>
        public static void GrantArenaFeat(int level)
        {
            var pc = EClass.pc;
            if (pc == null || pc.elements == null)
            {
                Debug.LogWarning("[SukutsuArena] GrantArenaFeat: pc or elements is null, skipping");
                return;
            }

            try
            {
                const int featId = 10001; // featArenaSpirit

                // フィートを設定（既存の場合はレベルアップ）
                // SetFeatを使用することでfeat.Apply()が呼ばれ、活力ボーナスが適用される
                pc.SetFeat(featId, level, msg: false);

                // スタミナ（活力）ボーナスを計算（1レベルにつき +2）
                int staminaBonus = level * 2;

                Msg.Say(ArenaLocalization.ArenaFeatGained(level, staminaBonus));
                ModLog.Log($"[SukutsuArena] Granted Arena Spirit feat Lv{level} (vigor+{staminaBonus})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SukutsuArena] GrantArenaFeat failed: {ex.Message}");
            }
        }


        // ============================================================
        // リリィとの契約（私室イベント報酬）
        // ============================================================

        /// <summary>
        /// リリィとの契約フィートを付与
        /// - エレア種族: featManaMeat (1421) - マナの体
        /// - それ以外: featManaBond (1201) - マナとの絆
        /// フラグ SessionFlagKeys.LilyContractType に結果を保存:
        ///   1 = マナの体（エレア）
        ///   2 = マナとの絆（エレア以外）
        /// </summary>
        public static void GrantLilyContract()
        {
            var pc = EClass.pc;
            if (pc == null || pc.elements == null)
            {
                Debug.LogWarning("[SukutsuArena] GrantLilyContract: pc or elements is null");
                return;
            }

            var storage = Core.ArenaContext.I?.Storage;
            if (storage == null)
            {
                Debug.LogError("[SukutsuArena] GrantLilyContract: Storage is null");
                return;
            }

            try
            {
                bool isElea = pc.race?.id == "elea";

                if (isElea)
                {
                    // エレア: マナの体 (featManaMeat = 1421)
                    // 既に持っている場合はレベルアップ
                    int currentLevel = pc.Evalue(1421);
                    int newLevel = currentLevel + 1;
                    pc.SetFeat(1421, newLevel, msg: false);

                    storage.SetInt(SessionFlagKeys.LilyContractType, 1);
                    ModLog.Log($"[SukutsuArena] Granted Lily Contract: Mana Meat Lv{newLevel} (Elea)");
                }
                else
                {
                    // エレア以外: マナとの絆 (featManaBond = 1201)
                    // HasElementで有無チェック、なければ付与
                    if (!pc.HasElement(1201))
                    {
                        pc.SetFeat(1201, 1, msg: false);
                        ModLog.Log("[SukutsuArena] Granted Lily Contract: Mana Bond (non-Elea)");
                    }
                    else
                    {
                        ModLog.Log("[SukutsuArena] Lily Contract: Mana Bond already present (non-Elea)");
                    }

                    storage.SetInt(SessionFlagKeys.LilyContractType, 2);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SukutsuArena] GrantLilyContract failed: {ex.Message}");
            }
        }

        /// <summary>
        /// マクマ（ランクB後イベント）報酬
        /// </summary>
        public static void GrantMakumaReward()
        {
            ModLog.Log("[SukutsuArena] Granted Makuma reward");
        }

        /// <summary>
        /// マクマ2（ランクA前イベント）報酬: コインとプラチナ
        /// </summary>
        public static void GrantMakuma2Reward()
        {
            var pc = EClass.pc;
            if (pc == null) return;

            // 小さなコイン30枚
            for (int i = 0; i < 30; i++)
            {
                pc.Pick(ThingGen.Create("medal"));
            }

            // プラチナコイン15枚
            for (int i = 0; i < 15; i++)
            {
                pc.Pick(ThingGen.Create("plat"));
            }

            Msg.Say(ArenaLocalization.Makuma2Reward);
            ModLog.Log("[SukutsuArena] Granted Makuma2 reward: 30 coins, 15 plat");
        }

        /// <summary>
        /// ゼクの共鳴瓶すり替えイベント完了処理
        /// </summary>
        public static void CompleteZekStealBottleQuest()
        {
            try
            {
                ArenaQuestManager.Instance.CompleteQuest("05_2_zek_steal_bottle");
                ModLog.Log("[SukutsuArena] Completed Zek Steal Bottle quest");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SukutsuArena] Error completing Zek Steal Bottle quest: {ex.Message}");
            }
        }

        /// <summary>
        /// バルガス訓練クエスト完了処理
        /// </summary>
        public static void CompleteBalgasTrainingQuest()
        {
            try
            {
                ArenaQuestManager.Instance.CompleteQuest("09_balgas_training");

                // [REMOVED v1.1] ステータスボーナス付与を削除
                // GrantBalgasTrainingBonus();

                ModLog.Log("[SukutsuArena] Completed Balgas Training quest");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SukutsuArena] Error completing Balgas Training quest: {ex.Message}");
            }
        }

        /// <summary>
        /// マクマ2: 共鳴瓶告白 - リリィに正直に話した
        /// </summary>
        public static void Makuma2ConfessToLily()
        {
            // カルマ+5
            EClass.player.ModKarma(5);
            ModLog.Log("[SukutsuArena] Makuma2: Confessed to Lily (Karma+5)");
        }

        /// <summary>
        /// マクマ2: 共鳴瓶告白 - ゼクのせいにした
        /// </summary>
        public static void Makuma2BlameZek()
        {
            // 関係値変更なし
            ModLog.Log("[SukutsuArena] Makuma2: Blamed Zek");
        }

        /// <summary>
        /// マクマ2: 共鳴瓶告白 - 関与を否定した
        /// </summary>
        public static void Makuma2DenyInvolvement()
        {
            // カルマ-30
            EClass.player.ModKarma(-30);

            // リリィをアリーナから退場させる（シナリオ演出と連動）
            HideLilyFromArena();

            ModLog.Log("[SukutsuArena] Makuma2: Denied involvement (Karma-30, hostile, Lily departed)");
        }

        /// <summary>
        /// リリィをアリーナから退場させる
        /// </summary>
        private static void HideLilyFromArena()
        {
            var lily = EClass._map?.charas?.Find(c => c.id == ArenaConfig.NpcIds.Lily);
            if (lily == null)
            {
                ModLog.Log("[SukutsuArena] HideLilyFromArena: Lily not found");
                return;
            }

            // ペット化されている場合はスキップ
            if (lily.IsPCParty || lily.master == EClass.pc)
            {
                ModLog.Log("[SukutsuArena] HideLilyFromArena: Lily is in party, skipping");
                return;
            }

            ModLog.Log("[SukutsuArena] HideLilyFromArena: Moving Lily to somewhere zone");
            lily.MoveZone("somewhere");
        }

        /// <summary>
        /// マクマ2: カインの魂告白 - 正直に話した
        /// </summary>
        public static void Makuma2ConfessAboutKain()
        {
            // カルマ-20
            EClass.player.ModKarma(-20);
            ModLog.Log("[SukutsuArena] Makuma2: Confessed about Kain (Karma-20)");
        }

        /// <summary>
        /// マクマ2: カインの魂告白 - 嘘をついた
        /// </summary>
        public static void Makuma2LieAboutKain()
        {
            ModLog.Log("[SukutsuArena] Makuma2: Lied about Kain");
        }

        /// <summary>
        /// マクマ2: 最終選択 - 信頼を選んだ
        /// </summary>
        public static void Makuma2ChooseTrust()
        {
            ModLog.Log("[SukutsuArena] Makuma2: Chose trust");
        }

        /// <summary>
        /// マクマ2: 最終選択 - 知識を選んだ
        /// </summary>
        public static void Makuma2ChooseKnowledge()
        {
            ModLog.Log("[SukutsuArena] Makuma2: Chose knowledge");
        }

        /// <summary>
        /// エンディングクレジットを表示
        /// Dialog.Confettiを使用した華やかなダイアログ
        /// </summary>
        public static void ShowEndingCredit()
        {
            var storage = Core.ArenaContext.I?.Storage;
            if (storage == null)
            {
                Debug.LogError("[SukutsuArena] ShowEndingCredit: Storage is null");
                return;
            }

            // エンディングタイプを取得
            int endingType = storage.GetInt(ArenaFlagKeys.Ending, -1);
            bool balgasKilled = storage.GetInt(ArenaFlagKeys.BalgasKilled, 0) == 1;
            bool lilyHostile = false;

            // エンド名を決定
            string endingName = ArenaLocalization.GetEndingName(endingType, balgasKilled, lilyHostile);
            string title = ArenaLocalization.EndingCreditTitle(endingName);
            string detail = ArenaLocalization.EndingCreditDetail;

            // ドラマ終了後に遅延してダイアログを表示（レイヤー競合回避）
            // 1.5秒待機: finish() + fade_in(0.5s) + GraphicRaycaster有効化(0.3s)を確実に待つ
            DOVirtual.DelayedCall(1.5f, () =>
            {
                Dialog.Ok($"{title}\n\n{detail}");
            });
            ModLog.Log($"[SukutsuArena] ShowEndingCredit: {endingName}");
        }

        // ============================================================
        // クエストディスパッチャー用
        // ============================================================

        /// <summary>
        /// クエストディスパッチ用のチェック
        /// 指定されたクエストIDリストの中で、利用可能な最初のクエストのインデックスをフラグに設定
        /// </summary>
        /// <param name="flagName">設定するフラグ名</param>
        /// <param name="questIds">クエストIDのカンマ区切りリスト</param>
        public static void CheckQuestsForDispatch(string flagName, string questIds)
        {
            var storage = Core.ArenaContext.I?.Storage;
            if (storage == null)
            {
                Debug.LogError("[SukutsuArena] CheckQuestsForDispatch: Storage is null");
                return;
            }

            // クエストIDリストをパース
            var questIdList = questIds.Split(',');
            for (int i = 0; i < questIdList.Length; i++)
            {
                questIdList[i] = questIdList[i].Trim();
            }

            // 利用可能なクエストを取得
            var availableQuests = ArenaQuestManager.Instance.GetAvailableQuests();
            var availableIds = new System.Collections.Generic.HashSet<string>();
            foreach (var q in availableQuests)
            {
                availableIds.Add(q.QuestId);
            }

            // 最初に見つかった利用可能なクエストのインデックスを設定
            int selectedIndex = 0;  // 0はfallback
            for (int i = 0; i < questIdList.Length; i++)
            {
                if (availableIds.Contains(questIdList[i]))
                {
                    selectedIndex = i + 1;  // 1から開始（0はfallback）
                    ModLog.Log($"[CheckQuestsForDispatch] Found available quest: {questIdList[i]} at index {selectedIndex}");
                    break;
                }
            }

            storage.SetInt(flagName, selectedIndex);
            ModLog.Log($"[CheckQuestsForDispatch] {flagName} = {selectedIndex} (checked {questIdList.Length} quests)");
        }
    }
}

