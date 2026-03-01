using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Elin_SukutsuArena.Core;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// アリーナギミックの種類
    /// </summary>
    public enum ArenaGimmickType
    {
        None,
        AntiMagic,       // 無法地帯: アンチマジック+50
        Critical,        // 臨死の闘技場: 耐性なしで受けるダメージ2倍
        Hellish,         // 地獄門: ドラゴン/デーモン/ジャイアントのみ出現
        Empathetic,      // 共感の場: 全員にテレパシー&透視付与
        Chaos,           // 混沌の爆発: audience_interference
        ElementalScar,   // 属性傷: elemental_scar
        NoHealing,       // 禁忌の癒し: 回復禁止
        MagicAffinity    // 魔法親和: 物理0、魔法2倍
    }

    /// <summary>
    /// ギミック定義
    /// </summary>
    public class ArenaGimmickDefinition
    {
        public ArenaGimmickType Type { get; set; }
        public string NameJp { get; set; }
        public string NameEn { get; set; }
        public string NameCn { get; set; }
        public string DescriptionJp { get; set; }
        public string DescriptionEn { get; set; }
        public string DescriptionCn { get; set; }
        public float DifficultyModifier { get; set; }  // 報酬補正（1.2 = +20%）
        public List<ArenaGimmickType> ExclusiveWith { get; set; } = new List<ArenaGimmickType>();

        /// <summary>
        /// 現在の言語に応じた名前を取得
        /// </summary>
        public string GetLocalizedName()
        {
            if (Lang.isJP) return NameJp;
            if (Lang.langCode == "CN") return NameCn ?? NameEn;
            return NameEn;
        }

        /// <summary>
        /// 現在の言語に応じた説明を取得
        /// </summary>
        public string GetLocalizedDescription()
        {
            if (Lang.isJP) return DescriptionJp;
            if (Lang.langCode == "CN") return DescriptionCn ?? DescriptionEn;
            return DescriptionEn;
        }
    }

    /// <summary>
    /// アリーナギミックシステム
    /// </summary>
    public static class ArenaGimmickSystem
    {
        // ギミック定義
        public static readonly Dictionary<ArenaGimmickType, ArenaGimmickDefinition> Definitions =
            new Dictionary<ArenaGimmickType, ArenaGimmickDefinition>
        {
            {
                ArenaGimmickType.AntiMagic, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.AntiMagic,
                    NameJp = "無法地帯",
                    NameEn = "Anti-Magic Zone",
                    NameCn = "反魔法区域",
                    DescriptionJp = "魔法ダメージ50%軽減",
                    DescriptionEn = "Magic damage reduced by 50%",
                    DescriptionCn = "魔法伤害减少50%",
                    DifficultyModifier = 1.20f,
                    ExclusiveWith = new List<ArenaGimmickType> { ArenaGimmickType.MagicAffinity }
                }
            },
            {
                ArenaGimmickType.Critical, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.Critical,
                    NameJp = "臨死の闘技場",
                    NameEn = "Critical Arena",
                    NameCn = "临死竞技场",
                    DescriptionJp = "物理ダメージ2倍",
                    DescriptionEn = "Physical damage is doubled",
                    DescriptionCn = "物理伤害翻倍",
                    DifficultyModifier = 1.30f
                }
            },
            {
                ArenaGimmickType.Hellish, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.Hellish,
                    NameJp = "地獄門",
                    NameEn = "Hellgate",
                    NameCn = "地狱之门",
                    DescriptionJp = "ドラゴン/デーモン/ジャイアントのみ出現",
                    DescriptionEn = "Only dragons, demons, and giants appear",
                    DescriptionCn = "只出现龙/恶魔/巨人",
                    DifficultyModifier = 1.25f
                }
            },
            {
                ArenaGimmickType.Empathetic, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.Empathetic,
                    NameJp = "共感の場",
                    NameEn = "Empathic Field",
                    NameCn = "共感之场",
                    DescriptionJp = "全員にテレパシー・透視付与",
                    DescriptionEn = "All combatants gain telepathy and see invisible",
                    DescriptionCn = "所有战斗者获得心灵感应和透视",
                    DifficultyModifier = 1.10f
                }
            },
            {
                ArenaGimmickType.Chaos, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.Chaos,
                    NameJp = "混沌の爆発",
                    NameEn = "Chaos Explosions",
                    NameCn = "混沌爆炸",
                    DescriptionJp = "観客からの妨害が発生する",
                    DescriptionEn = "Audience interference occurs",
                    DescriptionCn = "观众会进行干扰",
                    DifficultyModifier = 1.15f
                }
            },
            {
                ArenaGimmickType.ElementalScar, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.ElementalScar,
                    NameJp = "属性傷",
                    NameEn = "Elemental Scars",
                    NameCn = "元素之伤",
                    DescriptionJp = "周期的に属性ダメージを受ける",
                    DescriptionEn = "Periodic elemental damage",
                    DescriptionCn = "周期性受到属性伤害",
                    DifficultyModifier = 1.15f
                }
            },
            {
                ArenaGimmickType.NoHealing, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.NoHealing,
                    NameJp = "禁忌の癒し",
                    NameEn = "Forbidden Healing",
                    NameCn = "禁忌治愈",
                    DescriptionJp = "回復アイテム・回復魔法使用不可",
                    DescriptionEn = "Healing items and spells are disabled",
                    DescriptionCn = "无法使用治疗道具和治疗魔法",
                    DifficultyModifier = 1.35f
                }
            },
            {
                ArenaGimmickType.MagicAffinity, new ArenaGimmickDefinition
                {
                    Type = ArenaGimmickType.MagicAffinity,
                    NameJp = "魔法親和",
                    NameEn = "Magic Affinity",
                    NameCn = "魔法亲和",
                    DescriptionJp = "物理ダメージ0、魔法ダメージ2倍",
                    DescriptionEn = "Physical damage is nullified, magic damage is doubled",
                    DescriptionCn = "物理伤害无效，魔法伤害翻倍",
                    DifficultyModifier = 1.40f,
                    ExclusiveWith = new List<ArenaGimmickType> { ArenaGimmickType.AntiMagic }
                }
            }
        };

        // ランクごとの最大ギミック数
        private static readonly Dictionary<string, int> MaxGimmicksByRank = new Dictionary<string, int>
        {
            { "G", 0 }, { "F", 1 }, { "E", 1 }, { "D", 1 },
            { "C", 2 }, { "B", 2 }, { "A", 2 }, { "S", 3 }
        };

        /// <summary>
        /// ランクに応じてギミックを選択
        /// </summary>
        /// <param name="rank">プレイヤーランク（0-7）</param>
        /// <returns>選択されたギミックのリスト</returns>
        public static List<ArenaGimmickType> SelectGimmicks(int rank)
        {
            string rankName = RandomEnemyGenerator.GetRankName(rank);
            int maxGimmicks = MaxGimmicksByRank.TryGetValue(rankName, out var max) ? max : 0;

            if (maxGimmicks == 0)
            {
                return new List<ArenaGimmickType>();
            }

            int numGimmicks = EClass.rnd(maxGimmicks + 1);  // 0 to maxGimmicks
            if (numGimmicks == 0)
            {
                return new List<ArenaGimmickType>();
            }

            // 利用可能なギミックをリストアップ
            var available = Definitions.Keys.Where(k => k != ArenaGimmickType.None).ToList();
            var selected = new List<ArenaGimmickType>();

            for (int i = 0; i < numGimmicks && available.Count > 0; i++)
            {
                int idx = EClass.rnd(available.Count);
                var gimmick = available[idx];
                selected.Add(gimmick);
                available.RemoveAt(idx);

                // 排他的なギミックを除外
                var def = Definitions[gimmick];
                if (def.ExclusiveWith != null)
                {
                    foreach (var exclusive in def.ExclusiveWith)
                    {
                        available.Remove(exclusive);
                    }
                }
            }

            ModLog.Log($"[ArenaGimmickSystem] Selected {selected.Count} gimmicks for rank {rankName}: {string.Join(", ", selected)}");
            return selected;
        }

        /// <summary>
        /// ギミックを報酬計算用の補正値に変換
        /// </summary>
        public static float CalculateDifficultyModifier(List<ArenaGimmickType> gimmicks)
        {
            if (gimmicks == null || gimmicks.Count == 0)
            {
                return 1.0f;
            }

            float modifier = 1.0f;
            foreach (var gimmick in gimmicks)
            {
                if (Definitions.TryGetValue(gimmick, out var def))
                {
                    // 補正値を加算（1.2 → +0.2を加算）
                    modifier += (def.DifficultyModifier - 1.0f);
                }
            }

            return modifier;
        }

        /// <summary>
        /// ギミックをGimmickConfigリストに変換
        /// </summary>
        public static List<GimmickConfig> ConvertToConfigs(List<ArenaGimmickType> gimmicks)
        {
            var configs = new List<GimmickConfig>();

            foreach (var gimmick in gimmicks)
            {
                var config = CreateGimmickConfig(gimmick);
                if (config != null)
                {
                    configs.Add(config);
                }
            }

            return configs;
        }

        /// <summary>
        /// ギミックタイプからGimmickConfigを生成
        /// </summary>
        private static GimmickConfig CreateGimmickConfig(ArenaGimmickType type)
        {
            switch (type)
            {
                case ArenaGimmickType.Chaos:
                    return new GimmickConfig
                    {
                        GimmickType = "audience_interference",
                        Interval = 3.5f,
                        Damage = 20,
                        Radius = 4,
                        StartDelay = 0.5f,
                        EnableEscalation = true,
                        EscalationRate = 0.85f,
                        MinInterval = 0.7f,
                        DirectHitChance = 0.5f
                    };

                case ArenaGimmickType.ElementalScar:
                    return new GimmickConfig
                    {
                        GimmickType = "elemental_scar",
                        Interval = 8.0f,
                        StartDelay = 3.0f,
                        EnableEscalation = true,
                        EscalationRate = 0.85f,
                        MinInterval = 2.0f
                    };

                case ArenaGimmickType.AntiMagic:
                    return new GimmickConfig
                    {
                        GimmickType = "anti_magic"
                    };

                case ArenaGimmickType.Critical:
                    return new GimmickConfig
                    {
                        GimmickType = "critical_damage"
                    };

                case ArenaGimmickType.Empathetic:
                    return new GimmickConfig
                    {
                        GimmickType = "empathetic"
                    };

                case ArenaGimmickType.NoHealing:
                    return new GimmickConfig
                    {
                        GimmickType = "no_healing"
                    };

                case ArenaGimmickType.MagicAffinity:
                    return new GimmickConfig
                    {
                        GimmickType = "magic_affinity"
                    };

                case ArenaGimmickType.Hellish:
                    return new GimmickConfig
                    {
                        GimmickType = "hellish"
                    };

                default:
                    return null;
            }
        }

        /// <summary>
        /// ギミック情報を表示用文字列に変換（名前のみ）
        /// </summary>
        public static string GetGimmickDisplayText(List<ArenaGimmickType> gimmicks, bool isJp = true)
        {
            if (gimmicks == null || gimmicks.Count == 0)
            {
                return Localization.ArenaLocalization.NoGimmicksShort;
            }

            var names = new List<string>();
            foreach (var gimmick in gimmicks)
            {
                if (Definitions.TryGetValue(gimmick, out var def))
                {
                    names.Add(def.GetLocalizedName());
                }
            }

            return string.Join(", ", names);
        }

        /// <summary>
        /// ギミック情報を表示用文字列に変換（名前と詳細説明を含む、ネスト表示用）
        /// </summary>
        public static string GetGimmickDisplayTextWithDetails(List<ArenaGimmickType> gimmicks, bool isJp = true)
        {
            if (gimmicks == null || gimmicks.Count == 0)
            {
                return Localization.ArenaLocalization.NoGimmicks;
            }

            var lines = new List<string>();
            foreach (var gimmick in gimmicks)
            {
                if (Definitions.TryGetValue(gimmick, out var def))
                {
                    string name = def.GetLocalizedName();
                    string desc = def.GetLocalizedDescription();
                    lines.Add($"  - 【{name}】{desc}");
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// 全ギミックの説明を取得（ダイアログ表示用）
        /// </summary>
        public static string GetAllGimmickDescriptions(bool _ = true)
        {
            bool isJp = Lang.isJP;
            bool isCn = !Lang.isJP && Lang.langCode == "CN";

            var lines = new List<string>();
            string title = isJp ? "=== 特殊ルール一覧 ===" :
                           isCn ? "=== 特殊规则一览 ===" :
                           "=== Special Rules ===";
            string rewardBonusLabel = isJp ? "報酬ボーナス" :
                                      isCn ? "奖励加成" :
                                      "Reward Bonus";
            lines.Add(title);
            lines.Add("");

            foreach (var def in Definitions.Values)
            {
                if (def.Type == ArenaGimmickType.None) continue;

                string name = def.GetLocalizedName();
                string desc = def.GetLocalizedDescription();
                int bonus = (int)((def.DifficultyModifier - 1.0f) * 100);

                lines.Add($"【{name}】");
                lines.Add($"  {desc}");
                lines.Add($"  {rewardBonusLabel}: +{bonus}%");
                lines.Add("");
            }

            return string.Join("\n", lines);
        }
    }
}

