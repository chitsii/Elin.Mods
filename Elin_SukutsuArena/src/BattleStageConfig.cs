using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// ギミック設定
    /// </summary>
    [Serializable]
    public class GimmickConfig
    {
        [JsonProperty("gimmickType")]
        public string GimmickType { get; set; }

        [JsonProperty("interval")]
        public float Interval { get; set; } = 5.0f;

        [JsonProperty("damage")]
        public int Damage { get; set; } = 15;

        [JsonProperty("radius")]
        public int Radius { get; set; } = 3;

        [JsonProperty("startDelay")]
        public float StartDelay { get; set; } = 3.0f;

        [JsonProperty("effectName")]
        public string EffectName { get; set; } = "explosion";

        [JsonProperty("soundName")]
        public string SoundName { get; set; } = "explosion";

        // === エスカレーション設定 ===
        [JsonProperty("enableEscalation")]
        public bool EnableEscalation { get; set; } = true;

        [JsonProperty("escalationRate")]
        public float EscalationRate { get; set; } = 0.9f;

        [JsonProperty("minInterval")]
        public float MinInterval { get; set; } = 0.5f;

        [JsonProperty("maxRadius")]
        public int MaxRadius { get; set; } = 8;

        [JsonProperty("radiusGrowthInterval")]
        public float RadiusGrowthInterval { get; set; } = 30.0f;

        // === アイテムドロップ設定 ===
        [JsonProperty("enableItemDrop")]
        public bool EnableItemDrop { get; set; } = true;

        [JsonProperty("itemDropChance")]
        public float ItemDropChance { get; set; } = 0.15f;

        // === 命中率設定 ===
        [JsonProperty("blastRadius")]
        public int BlastRadius { get; set; } = 2;

        [JsonProperty("directHitChance")]
        public float DirectHitChance { get; set; } = 0.4f;

        [JsonProperty("explosionCount")]
        public int ExplosionCount { get; set; } = 1;
    }

    /// <summary>
    /// 敵スポーン設定
    /// </summary>
    [Serializable]
    public class EnemyConfig
    {
        [JsonProperty("charaId")]
        public string CharaId { get; set; }

        [JsonProperty("rarity")]
        public string Rarity { get; set; } = "Normal";

        [JsonProperty("position")]
        public string Position { get; set; } = "random";

        [JsonProperty("positionX")]
        public int PositionX { get; set; } = 0;

        [JsonProperty("positionZ")]
        public int PositionZ { get; set; } = 0;

        [JsonProperty("isBoss")]
        public bool IsBoss { get; set; } = false;

        [JsonProperty("count")]
        public int Count { get; set; } = 1;

        /// <summary>
        /// 敵のレベル（0 = SourceChara既定LVを使用、>0 = SetLvでスケーリング）
        /// ランダムバトルではdeepestに合わせて設定される
        /// </summary>
        [JsonProperty("level")]
        public int Level { get; set; } = 0;
    }

    /// <summary>
    /// バトルステージ設定
    /// </summary>
    [Serializable]
    public class BattleStageData
    {
        [JsonProperty("stageId")]
        public string StageId { get; set; }

        [JsonProperty("displayNameJp")]
        public string DisplayNameJp { get; set; }

        [JsonProperty("displayNameEn")]
        public string DisplayNameEn { get; set; }

        [JsonProperty("zoneType")]
        public string ZoneType { get; set; } = "field";

        [JsonProperty("biome")]
        public string Biome { get; set; } = "";  // Snow, Sand, etc.

        [JsonProperty("bgmBattle")]
        public string BgmBattle { get; set; } = "";

        [JsonProperty("bgmVictory")]
        public string BgmVictory { get; set; } = "";

        [JsonProperty("rewardPlat")]
        public int RewardPlat { get; set; } = 10;

        [JsonProperty("enemies")]
        public List<EnemyConfig> Enemies { get; set; } = new List<EnemyConfig>();

        [JsonProperty("gimmicks")]
        public List<GimmickConfig> Gimmicks { get; set; } = new List<GimmickConfig>();

        [JsonProperty("descriptionJp")]
        public string DescriptionJp { get; set; } = "";

        [JsonProperty("descriptionEn")]
        public string DescriptionEn { get; set; } = "";

        /// <summary>
        /// 敵の総数を取得
        /// </summary>
        public int TotalEnemyCount
        {
            get
            {
                int count = 0;
                foreach (var enemy in Enemies)
                {
                    count += enemy.Count;
                }
                return count;
            }
        }
    }

    /// <summary>
    /// バトルステージ設定ファイル全体
    /// </summary>
    [Serializable]
    public class BattleStagesFile
    {
        [JsonProperty("rankUpStages")]
        public Dictionary<string, BattleStageData> RankUpStages { get; set; }

        [JsonProperty("normalStages")]
        public Dictionary<string, BattleStageData> NormalStages { get; set; }

        [JsonProperty("debugStages")]
        public Dictionary<string, BattleStageData> DebugStages { get; set; }
    }

    /// <summary>
    /// バトルステージ設定のローダー
    /// </summary>
    public static class BattleStageLoader
    {
        private static BattleStagesFile _cachedStages;
        private static string _lastLoadedPath;

        /// <summary>
        /// ステージ設定を読み込む
        /// </summary>
        public static BattleStagesFile LoadStages(string packagePath)
        {
            string jsonPath = Path.Combine(packagePath, "battle_stages.json");

            if (_cachedStages != null && _lastLoadedPath == jsonPath)
            {
                return _cachedStages;
            }

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[SukutsuArena] battle_stages.json not found at: {jsonPath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                _cachedStages = JsonConvert.DeserializeObject<BattleStagesFile>(json);
                _lastLoadedPath = jsonPath;
                ModLog.Log($"[SukutsuArena] Loaded battle stages from: {jsonPath}");
                return _cachedStages;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SukutsuArena] Failed to load battle_stages.json: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ステージIDからステージ設定を取得
        /// </summary>
        public static BattleStageData GetStage(string stageId, string packagePath)
        {
            var stages = LoadStages(packagePath);
            if (stages == null) return null;

            // debugStagesから検索（全ステージが含まれている）
            if (stages.DebugStages != null && stages.DebugStages.TryGetValue(stageId, out var stage))
            {
                return stage;
            }

            // 念のため他のカテゴリも検索
            if (stages.RankUpStages != null && stages.RankUpStages.TryGetValue(stageId, out stage))
            {
                return stage;
            }

            if (stages.NormalStages != null && stages.NormalStages.TryGetValue(stageId, out stage))
            {
                return stage;
            }

            Debug.LogWarning($"[SukutsuArena] Stage not found: {stageId}");
            return null;
        }

        /// <summary>
        /// キャッシュをクリア
        /// </summary>
        public static void ClearCache()
        {
            _cachedStages = null;
            _lastLoadedPath = null;
        }
    }
}

