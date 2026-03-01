using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// 敵配置パターン
    /// </summary>
    public enum SpawnPattern
    {
        Random,          // ランダム配置
        Horde,           // 群れ（同種の敵を多数）
        Mixed,           // 混成（複数種類の敵）
        BossWithMinions, // ボス1体 + 取り巻き
        BossRush,        // ボスラッシュ（複数のボス級が出現）
        // === 出現方式変更系 ===
        FreeForAll,      // バトルロイヤル（敵同士も敵対）
        Apocalypse,      // 終末（5Wave制、巨人/ドラゴン連続召喚）
        // === 種族ピックアップ系 ===
        UndeadArmy,      // アンデッド軍団
        SeaCreatures,    // 海洋生物
        CosmicHorror,    // 宇宙的恐怖（イス系）
        Kamikazes        // 自爆生物
    }

    /// <summary>
    /// ランダムバトル用の敵生成システム
    /// </summary>
    public static class RandomEnemyGenerator
    {
        /// <summary>
        /// 最深層に応じた敵の数を計算
        /// </summary>
        /// <param name="deepest">最深層到達記録</param>
        /// <returns>敵の数（5-100体）</returns>
        public static int CalculateEnemyCount(int deepest)
        {
            // 基本: 5体から開始
            // スケーリング: 5階層ごとに+1体
            // 最大: 100体
            int baseCount = 5;
            int scaledCount = baseCount + (deepest / 5);  // 5階ごとに+1体

            return Math.Min(100, Math.Max(baseCount, scaledCount));
        }

        /// <summary>
        /// 配置パターンを選択
        /// </summary>
        /// <param name="deepest">最深層到達記録</param>
        /// <returns>選択されたパターン</returns>
        public static SpawnPattern SelectSpawnPattern(int deepest)
        {
            // デバッグオーバーライドを確認
            var forcePattern = Elin_SukutsuArena.Debugging.RandomBattleDebug.GetForcePattern();
            if (forcePattern.HasValue)
            {
                ModLog.Log($"[RandomEnemyGenerator] Using forced pattern: {forcePattern.Value}");
                return forcePattern.Value;
            }

            // === 特殊パターンの抽選（深層条件あり） ===

            // Apocalypse（終末）: 未実装のため無効化
            // TODO: 5Wave制の実装が必要
            // if (deepest >= 60 && EClass.rnd(100) < 5)
            // {
            //     ModLog.Log("[RandomEnemyGenerator] Selected special pattern: Apocalypse");
            //     return SpawnPattern.Apocalypse;
            // }

            // FreeForAll（バトルロイヤル）: 未実装のため無効化
            // TODO: 敵同士の敵対設定の実装が必要
            // if (deepest >= 30 && EClass.rnd(100) < 5)
            // {
            //     ModLog.Log("[RandomEnemyGenerator] Selected special pattern: FreeForAll");
            //     return SpawnPattern.FreeForAll;
            // }

            // CosmicHorror（宇宙的恐怖）: 50層以上、7%の確率
            if (deepest >= 50 && EClass.rnd(100) < 7)
            {
                ModLog.Log("[RandomEnemyGenerator] Selected special pattern: CosmicHorror");
                return SpawnPattern.CosmicHorror;
            }

            // Kamikazes（自爆軍団）: 30層以上、7%の確率
            if (deepest >= 30 && EClass.rnd(100) < 7)
            {
                ModLog.Log("[RandomEnemyGenerator] Selected special pattern: Kamikazes");
                return SpawnPattern.Kamikazes;
            }

            // UndeadArmy（アンデッド軍団）: 20層以上、10%の確率
            if (deepest >= 20 && EClass.rnd(100) < 10)
            {
                ModLog.Log("[RandomEnemyGenerator] Selected special pattern: UndeadArmy");
                return SpawnPattern.UndeadArmy;
            }

            // SeaCreatures（海洋生物）: 20層以上、10%の確率
            if (deepest >= 20 && EClass.rnd(100) < 10)
            {
                ModLog.Log("[RandomEnemyGenerator] Selected special pattern: SeaCreatures");
                return SpawnPattern.SeaCreatures;
            }

            // === 既存パターン ===

            // 序盤は群れが多い
            if (deepest < 20)
            {
                return SpawnPattern.Horde;
            }

            // 中盤は群れか混成
            if (deepest < 50)
            {
                return EClass.rnd(2) == 0 ? SpawnPattern.Horde : SpawnPattern.Mixed;
            }

            // 終盤（80層以上）はボスラッシュも出現（10%の確率）
            if (deepest >= 80 && EClass.rnd(10) == 0)
            {
                return SpawnPattern.BossRush;
            }

            // 後半は通常パターン（Random, Horde, Mixed, BossWithMinions）
            return (SpawnPattern)EClass.rnd(4);
        }

        /// <summary>
        /// ゲーム本来のレベルカーブに従って敵を生成
        /// </summary>
        /// <param name="gimmicks">適用するギミック（地獄門で敵フィルタリングに使用）</param>
        /// <returns>生成されたBattleStageDataとスポーンパターン</returns>
        public static (BattleStageData stageData, SpawnPattern pattern) GenerateRandomBattle(List<ArenaGimmickType> gimmicks = null)
        {
            // Step 1: 生成レベル = 最深層到達記録（デバッグオーバーライド対応）
            int deepest = Elin_SukutsuArena.Debugging.RandomBattleDebug.GetEffectiveDeepest();
            deepest = Math.Max(1, deepest);
            int totalCount = CalculateEnemyCount(deepest);
            SpawnPattern pattern = SelectSpawnPattern(deepest);

            ModLog.Log($"[RandomEnemyGenerator] Generating battle: deepest={deepest}, count={totalCount}, pattern={pattern}");

            // Step 2: 敵構成を決定（ギミック情報も渡す）
            var enemies = GenerateEnemyConfigs(deepest, totalCount, pattern, gimmicks);

            // Step 3: BattleStageDataを構築
            var stageData = new BattleStageData
            {
                StageId = "random_battle",
                DisplayNameJp = "ランダムバトル",
                DisplayNameEn = "Random Battle",
                ZoneType = "field_fine",
                Biome = "",  // デフォルト
                BgmBattle = "",  // デフォルト戦闘BGM
                BgmVictory = "",
                RewardPlat = CalculateReward(deepest),
                Enemies = enemies,
                Gimmicks = new List<GimmickConfig>()  // ギミックは別途追加
            };

            return (stageData, pattern);
        }

        /// <summary>
        /// スポーンパターンの表示名を取得（ローカライズ対応）
        /// </summary>
        public static string GetSpawnPatternDisplayName(SpawnPattern pattern, bool _ = true)
        {
            // 言語判定
            bool isJp = Lang.isJP;
            bool isCn = !Lang.isJP && Lang.langCode == "CN";

            switch (pattern)
            {
                case SpawnPattern.Horde:
                    return isJp ? "群れ（同種の敵が多数出現）" :
                           isCn ? "群体（大量同种敌人出现）" :
                           "Horde (many of the same enemy)";
                case SpawnPattern.Mixed:
                    return isJp ? "混成（複数種類の敵が出現）" :
                           isCn ? "混合（多种敌人出现）" :
                           "Mixed (multiple enemy types)";
                case SpawnPattern.BossWithMinions:
                    return isJp ? "ボス戦（ボス1体 + 取り巻き）" :
                           isCn ? "Boss战（1个Boss + 小兵）" :
                           "Boss Battle (1 boss + minions)";
                case SpawnPattern.BossRush:
                    return isJp ? "ボスラッシュ（複数のボス級が連続出現）" :
                           isCn ? "Boss连战（多个Boss级敌人）" :
                           "Boss Rush (multiple boss-tier enemies)";
                case SpawnPattern.FreeForAll:
                    return isJp ? "バトルロイヤル（敵同士も敵対）" :
                           isCn ? "大乱斗（敌人互相敌对）" :
                           "Battle Royale (enemies fight each other)";
                case SpawnPattern.Apocalypse:
                    return isJp ? "終末（5Wave耐久戦）" :
                           isCn ? "末日（5波生存战）" :
                           "Apocalypse (5 Wave survival)";
                case SpawnPattern.UndeadArmy:
                    return isJp ? "アンデッド軍団" :
                           isCn ? "亡灵军团" :
                           "Undead Army";
                case SpawnPattern.SeaCreatures:
                    return isJp ? "海洋生物の群れ" :
                           isCn ? "海洋生物群" :
                           "Sea Creatures";
                case SpawnPattern.CosmicHorror:
                    return isJp ? "宇宙的恐怖" :
                           isCn ? "宇宙恐怖" :
                           "Cosmic Horror";
                case SpawnPattern.Kamikazes:
                    return isJp ? "自爆軍団" :
                           isCn ? "自爆军团" :
                           "Kamikaze Squad";
                case SpawnPattern.Random:
                default:
                    return isJp ? "ランダム" :
                           isCn ? "随机" :
                           "Random";
            }
        }

        /// <summary>
        /// パターンに応じて敵構成を生成
        /// </summary>
        private static List<EnemyConfig> GenerateEnemyConfigs(
            int deepest,
            int totalCount,
            SpawnPattern pattern,
            List<ArenaGimmickType> gimmicks)
        {
            var configs = new List<EnemyConfig>();

            switch (pattern)
            {
                case SpawnPattern.Horde:
                    // 同種の敵を全数配置
                    var hordeEnemy = SelectEnemy(deepest, gimmicks);
                    if (hordeEnemy != null)
                    {
                        configs.Add(new EnemyConfig
                        {
                            CharaId = hordeEnemy.id,
                            Count = totalCount,
                            Rarity = DetermineRarity(),
                            Position = "random"
                        });
                    }
                    break;

                case SpawnPattern.Mixed:
                    // 2-4種類の敵を混成
                    int types = Math.Min(4, 2 + deepest / 50);
                    int perType = totalCount / types;
                    for (int i = 0; i < types; i++)
                    {
                        var enemy = SelectEnemy(deepest, gimmicks);
                        if (enemy != null)
                        {
                            int count = (i == types - 1) ? totalCount - (perType * i) : perType;
                            configs.Add(new EnemyConfig
                            {
                                CharaId = enemy.id,
                                Count = count,
                                Rarity = DetermineRarity(),
                                Position = "random"
                            });
                        }
                    }
                    break;

                case SpawnPattern.BossWithMinions:
                    // ボス1体 + 残りは取り巻き
                    var boss = SelectEnemy(deepest, gimmicks);
                    if (boss != null)
                    {
                        configs.Add(new EnemyConfig
                        {
                            CharaId = boss.id,
                            Count = 1,
                            IsBoss = true,
                            Rarity = "Legendary",
                            Position = "center"
                        });
                    }

                    if (totalCount > 1)
                    {
                        var minion = SelectEnemy(deepest / 2, gimmicks);  // 取り巻きは弱め
                        if (minion != null)
                        {
                            configs.Add(new EnemyConfig
                            {
                                CharaId = minion.id,
                                Count = totalCount - 1,
                                Rarity = "Normal",
                                Position = "random"
                                // Level = 0 → CalculateMobLevel(deepest) で計算
                            });
                        }
                    }
                    break;

                case SpawnPattern.BossRush:
                    // 複数のボス級が出現（3-5体）
                    int bossCount = 3 + EClass.rnd(3);  // 3-5体
                    for (int i = 0; i < bossCount; i++)
                    {
                        var rushBoss = SelectEnemy(deepest, gimmicks);
                        if (rushBoss != null)
                        {
                            configs.Add(new EnemyConfig
                            {
                                CharaId = rushBoss.id,
                                Count = 1,
                                IsBoss = true,
                                Rarity = i == bossCount - 1 ? "Legendary" : "Superior",  // 最後の1体はレジェンダリー
                                Position = "random"
                            });
                        }
                    }
                    break;

                // === 出現方式変更系 ===
                case SpawnPattern.FreeForAll:
                    // バトルロイヤル: 通常と同じ敵生成だが、敵同士も敵対
                    // 敵対設定はZonePreEnterArenaBattleで行う
                    GenerateMixedEnemies(configs, deepest, totalCount, gimmicks);
                    break;

                case SpawnPattern.Apocalypse:
                    // 終末: 5Wave制（Wave情報はメタデータとして保持）
                    // Wave 1-3: 巨人 3-5体
                    // Wave 4: ドラゴン 3-5体
                    // Wave 5: 巨人+ドラゴン 5-8体
                    // 初回は全Waveの敵を一度に生成（合計20-30体程度）
                    GenerateApocalypseEnemies(configs, deepest);
                    break;

                // === 種族ピックアップ系 ===
                case SpawnPattern.UndeadArmy:
                    // アンデッド軍団
                    GenerateFilteredEnemies(configs, deepest, totalCount, IsUndeadCreature, "lich", "zombie");
                    break;

                case SpawnPattern.SeaCreatures:
                    // 海洋生物
                    GenerateFilteredEnemies(configs, deepest, totalCount, IsAquaticCreature, "fish", "crab");
                    break;

                case SpawnPattern.CosmicHorror:
                    // 宇宙的恐怖
                    GenerateFilteredEnemies(configs, deepest, totalCount, IsCosmicHorror, "yeek", "shub_niggurath");
                    break;

                case SpawnPattern.Kamikazes:
                    // 自爆軍団
                    GenerateFilteredEnemies(configs, deepest, totalCount, IsSuicideCreature, "putty", "putty_");
                    break;

                default:  // Random
                    for (int i = 0; i < totalCount; i++)
                    {
                        var enemy = SelectEnemy(deepest, gimmicks);
                        if (enemy != null)
                        {
                            configs.Add(new EnemyConfig
                            {
                                CharaId = enemy.id,
                                Count = 1,
                                Rarity = DetermineRarity(),
                                Position = "random"
                            });
                        }
                    }
                    break;
            }

            // フォールバック: 敵が一体もいない場合
            if (configs.Count == 0)
            {
                configs.Add(new EnemyConfig
                {
                    CharaId = "putty",
                    Count = totalCount,
                    Rarity = "Normal",
                    Position = "random"
                });
            }

            // レベルスケーリング: Level未設定（0）の敵にボス/モブ別のレベルを適用
            // 個別設定済みのものはスキップ
            foreach (var config in configs)
            {
                if (config.Level == 0)
                {
                    int sourceLV = 1;
                    if (EClass.sources.charas.map.TryGetValue(config.CharaId, out var row))
                        sourceLV = row.LV;
                    config.Level = CalculateVoidScaledLevel(deepest, sourceLV, config.IsBoss);
                }
            }

            return configs;
        }

        /// <summary>
        /// 敵を選定（純粋なランダム選択、ギミック対応）
        /// </summary>
        private static SourceChara.Row SelectEnemy(int genLv, List<ArenaGimmickType> gimmicks)
        {
            try
            {
                // 地獄門ギミック: ドラゴン/デーモン/ジャイアントのみ出現
                bool hellishActive = gimmicks?.Contains(ArenaGimmickType.Hellish) ?? false;
                if (hellishActive)
                {
                    var hellishEnemy = SelectHellishEnemy(genLv);
                    if (hellishEnemy != null)
                    {
                        ModLog.Log($"[RandomEnemyGenerator] Hellish gimmick: selected {hellishEnemy.id}");
                        return hellishEnemy;
                    }
                }

                // SpawnListを使ってゲーム本来のレベルカーブで敵を選定（純粋なランダム）
                SpawnList spawnList = SpawnList.Get("chara");
                var selected = spawnList.Select(genLv, 10);
                if (selected is SourceChara.Row charaRow)
                {
                    return charaRow;
                }

                // CardRowをSourceChara.Rowに変換
                if (selected != null)
                {
                    return EClass.sources.charas.map.TryGetValue(selected.id, out var row) ? row : null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RandomEnemyGenerator] Error selecting enemy: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 地獄門ギミック用: ドラゴン/デーモン/ジャイアント系の敵を選定
        /// </summary>
        private static SourceChara.Row SelectHellishEnemy(int genLv)
        {
            try
            {
                SpawnList spawnList = SpawnList.Get("chara");
                var filtered = spawnList.Filter(genLv, 15);
                if (filtered?.rows == null || filtered.rows.Count == 0)
                {
                    return GetFallbackHellishEnemy();
                }

                var hellishCandidates = new List<SourceChara.Row>();
                foreach (var row in filtered.rows)
                {
                    SourceChara.Row charaRow = row as SourceChara.Row;
                    if (charaRow == null && row != null)
                    {
                        EClass.sources.charas.map.TryGetValue(row.id, out charaRow);
                    }
                    if (charaRow == null) continue;

                    // 種族またはタグでフィルタリング
                    if (IsHellishCreature(charaRow))
                    {
                        hellishCandidates.Add(charaRow);
                    }
                }

                if (hellishCandidates.Count > 0)
                {
                    var selected = hellishCandidates[EClass.rnd(hellishCandidates.Count)];
                    ModLog.Log($"[RandomEnemyGenerator] Hellish candidates: {hellishCandidates.Count}, selected: {selected.id}");
                    return selected;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RandomEnemyGenerator] Error selecting hellish enemy: {ex.Message}");
            }

            return GetFallbackHellishEnemy();
        }

        /// <summary>
        /// 地獄門対象のクリーチャーかどうか判定
        /// </summary>
        private static bool IsHellishCreature(SourceChara.Row charaRow)
        {
            string charaId = charaRow.id ?? "unknown";
            string raceId = charaRow.race ?? "unknown";

            // 種族チェック
            var race = charaRow.race_row;
            if (race != null)
            {
                // ドラゴン
                if (race.IsDragon)
                {
                    ModLog.Log($"[Hellish] {charaId} is dragon (race={raceId})");
                    return true;
                }

                // 種族タグでデーモン/ジャイアントをチェック（tagはstring[]）
                if (race.tag != null)
                {
                    string tagStr = string.Join(",", race.tag);
                    if (race.tag.Contains("demon") || race.tag.Contains("giant"))
                    {
                        ModLog.Log($"[Hellish] {charaId} matched race.tag (race={raceId}, tags={tagStr})");
                        return true;
                    }
                }
            }

            // 特定の種族IDでもチェック（フォールバック）
            if (raceId == "dragon" || raceId == "demon" || raceId == "giant" ||
                raceId == "imp" || raceId == "drake" || raceId == "cyclops" ||
                raceId == "titan" || raceId == "ogre")
            {
                ModLog.Log($"[Hellish] {charaId} matched raceId={raceId}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// アンデッドクリーチャーかどうか判定
        /// </summary>
        private static bool IsUndeadCreature(SourceChara.Row charaRow)
        {
            var race = charaRow.race_row;
            if (race != null && race.IsUndead) return true;
            return charaRow.HasTag(CTAG.undead);
        }

        /// <summary>
        /// 水棲生物かどうか判定
        /// </summary>
        private static bool IsAquaticCreature(SourceChara.Row charaRow)
        {
            var race = charaRow.race_row;
            if (race != null && race.IsFish) return true;
            if (race?.tag != null && race.tag.Contains("water")) return true;
            return charaRow.race == "octopus";
        }

        /// <summary>
        /// 宇宙的恐怖かどうか判定（イス系・ホラー）
        /// </summary>
        private static bool IsCosmicHorror(SourceChara.Row charaRow)
        {
            var race = charaRow.race_row;
            if (race != null && race.IsHorror) return true;
            return charaRow.race == "yeek";
        }

        /// <summary>
        /// 自爆生物かどうか判定
        /// </summary>
        private static bool IsSuicideCreature(SourceChara.Row charaRow)
        {
            if (charaRow.HasTag(CTAG.suicide)) return true;
            if (charaRow.HasTag(CTAG.kamikaze)) return true;
            // フォールバック: putty系
            return charaRow.id.Contains("putty") || charaRow.id.Contains("puti");
        }

        /// <summary>
        /// 巨人かどうか判定（Apocalypse用）
        /// </summary>
        private static bool IsGiantCreature(SourceChara.Row charaRow)
        {
            string raceId = charaRow.race ?? "";
            var race = charaRow.race_row;

            if (race?.tag != null && race.tag.Contains("giant")) return true;
            if (raceId == "giant" || raceId == "cyclops" || raceId == "titan" || raceId == "ogre") return true;
            return false;
        }

        /// <summary>
        /// ドラゴンかどうか判定（Apocalypse用）
        /// </summary>
        private static bool IsDragonCreature(SourceChara.Row charaRow)
        {
            var race = charaRow.race_row;
            if (race != null && race.IsDragon) return true;
            string raceId = charaRow.race ?? "";
            return raceId == "dragon" || raceId == "drake";
        }

        /// <summary>
        /// 混成敵を生成（FreeForAll用）
        /// </summary>
        private static void GenerateMixedEnemies(
            List<EnemyConfig> configs,
            int deepest,
            int totalCount,
            List<ArenaGimmickType> gimmicks)
        {
            // 2-4種類の敵を混成（Mixedと同じロジック）
            int types = Math.Min(4, 2 + deepest / 50);
            int perType = totalCount / types;

            for (int i = 0; i < types; i++)
            {
                var enemy = SelectEnemy(deepest, gimmicks);
                if (enemy != null)
                {
                    int count = (i == types - 1) ? totalCount - (perType * i) : perType;
                    configs.Add(new EnemyConfig
                    {
                        CharaId = enemy.id,
                        Count = count,
                        Rarity = DetermineRarity(),
                        Position = "random"
                    });
                }
            }
        }

        /// <summary>
        /// 終末モードの敵を生成（Apocalypse用）
        /// Wave 1-3: 巨人、Wave 4: ドラゴン、Wave 5: 混成
        /// </summary>
        private static void GenerateApocalypseEnemies(List<EnemyConfig> configs, int deepest)
        {
            // 巨人を選定（Wave 1-3用）
            var giants = SelectFilteredEnemies(deepest, IsGiantCreature, 5);
            // ドラゴンを選定（Wave 4-5用）
            var dragons = SelectFilteredEnemies(deepest, IsDragonCreature, 3);

            // Wave 1-3: 巨人 各3-5体
            int giantCount = 3 + EClass.rnd(3);  // 3-5体
            foreach (var giant in giants.Take(3))
            {
                configs.Add(new EnemyConfig
                {
                    CharaId = giant.id,
                    Count = giantCount,
                    Rarity = "Superior",
                    Position = "random"
                });
            }

            // Wave 4: ドラゴン 3-5体
            int dragonCount = 3 + EClass.rnd(3);
            foreach (var dragon in dragons.Take(2))
            {
                configs.Add(new EnemyConfig
                {
                    CharaId = dragon.id,
                    Count = dragonCount,
                    Rarity = "Legendary",
                    Position = "random"
                });
            }

            // Wave 5: 混成 5-8体
            int finalCount = 5 + EClass.rnd(4);
            if (giants.Count > 0)
            {
                configs.Add(new EnemyConfig
                {
                    CharaId = giants[0].id,
                    Count = finalCount / 2,
                    IsBoss = true,
                    Rarity = "Legendary",
                    Position = "random"
                });
            }
            if (dragons.Count > 0)
            {
                configs.Add(new EnemyConfig
                {
                    CharaId = dragons[0].id,
                    Count = finalCount - (finalCount / 2),
                    IsBoss = true,
                    Rarity = "Legendary",
                    Position = "random"
                });
            }

            ModLog.Log($"[RandomEnemyGenerator] Apocalypse: {giants.Count} giant types, {dragons.Count} dragon types");
        }

        /// <summary>
        /// フィルタ条件に合致する敵を生成（種族ピックアップ系用）
        /// </summary>
        private static void GenerateFilteredEnemies(
            List<EnemyConfig> configs,
            int deepest,
            int totalCount,
            Func<SourceChara.Row, bool> filter,
            string fallbackId1,
            string fallbackId2)
        {
            // フィルタ条件に合う敵を複数選定
            var candidates = SelectFilteredEnemies(deepest, filter, 5);

            if (candidates.Count > 0)
            {
                // 2-3種類を混成
                int types = Math.Min(3, candidates.Count);
                int perType = totalCount / types;

                for (int i = 0; i < types; i++)
                {
                    var enemy = candidates[i];
                    int count = (i == types - 1) ? totalCount - (perType * i) : perType;
                    configs.Add(new EnemyConfig
                    {
                        CharaId = enemy.id,
                        Count = count,
                        Rarity = DetermineRarity(),
                        Position = "random"
                    });
                }

                ModLog.Log($"[RandomEnemyGenerator] Generated {types} types from {candidates.Count} candidates");
            }
            else
            {
                // フォールバック
                string fallbackId = EClass.sources.charas.map.ContainsKey(fallbackId1) ? fallbackId1 : fallbackId2;
                configs.Add(new EnemyConfig
                {
                    CharaId = fallbackId,
                    Count = totalCount,
                    Rarity = "Normal",
                    Position = "random"
                });
                ModLog.Log($"[RandomEnemyGenerator] Used fallback: {fallbackId}");
            }
        }

        /// <summary>
        /// フィルタ条件に合致する敵を複数選定
        /// </summary>
        private static List<SourceChara.Row> SelectFilteredEnemies(
            int genLv,
            Func<SourceChara.Row, bool> filter,
            int maxCount)
        {
            var result = new List<SourceChara.Row>();

            try
            {
                SpawnList spawnList = SpawnList.Get("chara");
                var filtered = spawnList.Filter(genLv, 15);
                if (filtered?.rows == null || filtered.rows.Count == 0)
                {
                    return result;
                }

                var candidates = new List<SourceChara.Row>();
                foreach (var row in filtered.rows)
                {
                    SourceChara.Row charaRow = row as SourceChara.Row;
                    if (charaRow == null && row != null)
                    {
                        EClass.sources.charas.map.TryGetValue(row.id, out charaRow);
                    }
                    if (charaRow == null) continue;

                    if (filter(charaRow))
                    {
                        candidates.Add(charaRow);
                    }
                }

                // ランダムに選択
                while (result.Count < maxCount && candidates.Count > 0)
                {
                    int idx = EClass.rnd(candidates.Count);
                    result.Add(candidates[idx]);
                    candidates.RemoveAt(idx);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RandomEnemyGenerator] Error selecting filtered enemies: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 地獄門のフォールバック敵
        /// </summary>
        private static SourceChara.Row GetFallbackHellishEnemy()
        {
            // インプをデフォルトとして返す
            if (EClass.sources.charas.map.TryGetValue("imp", out var imp))
            {
                ModLog.Log("[RandomEnemyGenerator] Hellish fallback: imp");
                return imp;
            }
            // 見つからない場合はputty
            if (EClass.sources.charas.map.TryGetValue("putty", out var putty))
            {
                ModLog.Log("[RandomEnemyGenerator] Hellish fallback: putty");
                return putty;
            }
            return null;
        }

        /// <summary>
        /// レアリティを決定
        /// </summary>
        private static string DetermineRarity()
        {
            // 1%の確率で伝説級
            if (EClass.rnd(100) == 0)
            {
                return "Legendary";
            }
            // 5%の確率で上位
            if (EClass.rnd(100) < 5)
            {
                return "Superior";
            }
            return "Normal";
        }

        /// <summary>
        /// Void式のスケーリングLV（キャラのsourceLVを考慮）
        /// Zone.SpawnMob の Void スケーリングと同じ式
        /// </summary>
        private static int CalculateVoidScaledLevel(int deepest, int sourceLV, bool isBoss)
        {
            if (deepest <= 50)
            {
                int lv = sourceLV;
                if (isBoss) lv = lv * 150 / 100;
                return Math.Max(1, lv);
            }
            long scaled = (50L + sourceLV) * Math.Max(1, (deepest - 1) / 50);
            if (isBoss) scaled = scaled * 150 / 100;
            return (int)Math.Min(Math.Max(1L, scaled), 100000000L);
        }

        /// <summary>
        /// 報酬を計算
        /// </summary>
        /// <param name="deepest">最深層到達記録</param>
        /// <returns>プラチナコイン数</returns>
        private static int CalculateReward(int deepest)
        {
            // 基本報酬 + 深層ボーナス
            int baseReward = 5;
            int depthBonus = deepest / 3;
            return baseReward + depthBonus;
        }

        /// <summary>
        /// ランク名を取得
        /// </summary>
        public static string GetRankName(int rank)
        {
            return rank switch
            {
                0 => "G",
                1 => "F",
                2 => "E",
                3 => "D",
                4 => "C",
                5 => "B",
                6 => "A",
                7 => "S",
                _ => "G"
            };
        }

        /// <summary>
        /// ランクに基づく基本報酬を取得（逓減設計）
        /// </summary>
        public static (int plat, int potion) GetRankBaseReward(int rank)
        {
            return rank switch
            {
                0 => (1, 1),   // G
                1 => (1, 1),   // F
                2 => (1, 1),   // E
                3 => (1, 1),   // D
                4 => (2, 2),   // C
                5 => (2, 2),   // B
                6 => (2, 2),   // A
                7 => (2, 2),   // S
                _ => (1, 1)
            };
        }

        /// <summary>
        /// スポーンパターンに基づく報酬補正を取得
        /// </summary>
        public static float GetSpawnPatternRewardModifier(SpawnPattern pattern)
        {
            return pattern switch
            {
                // 出現方式変更系
                SpawnPattern.FreeForAll => 0.7f,   // 敵同士で削り合うため減少
                SpawnPattern.Apocalypse => 1.5f,   // 大量の強敵で増加

                // 種族ピックアップ系
                SpawnPattern.UndeadArmy => 1.1f,   // やや危険
                SpawnPattern.SeaCreatures => 1.0f, // 標準
                SpawnPattern.CosmicHorror => 1.3f, // 精神攻撃が厄介
                SpawnPattern.Kamikazes => 1.2f,    // 事故死リスク

                // 既存パターン
                SpawnPattern.BossRush => 1.2f,     // 高難度
                SpawnPattern.BossWithMinions => 1.1f,
                _ => 1.0f
            };
        }
    }
}

