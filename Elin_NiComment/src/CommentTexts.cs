using System.Collections.Generic;

namespace Elin_NiComment
{
    public static class CommentTexts
    {
        // Event IDs
        public const string PcDeath = "pc_death";
        public const string QuestComplete = "quest_complete";
        public const string StrongKill = "strong_kill";
        public const string LevelUp = "level_up";
        public const string RareItem = "rare_item";
        public const string EtherWind = "ether_wind";
        public const string NewZone = "new_zone";
        public const string Weather = "weather";
        public const string Hunger = "hunger";
        public const string Tired = "tired";
        public const string BigDamage = "big_damage";
        public const string NpcEat = "npc_eat";
        public const string NpcMusic = "npc_music";

        private static readonly Dictionary<string, string[]> Reactions = new Dictionary<string, string[]>
        {
            // Tier S
            [PcDeath] = new[] { "え", "死んだwww", "ちょwww", "嘘だろ", "マジか", "おいおい", "あー", "終わった", "ドンマイ", "RIP" },
            [QuestComplete] = new[] { "おめ！", "88888", "クリアきた", "GJ", "すごい", "おつ", "やったね", "ナイス", "おめでとう", "完了！" },
            [StrongKill] = new[] { "つよい", "神", "倒したのか…", "つえー", "ナイス", "GJ", "すご", "やるやん", "さすが", "勝った！" },

            // Tier A
            [LevelUp] = new[] { "おめ！", "つよくなった", "レベル上がった", "きたきた", "成長してる", "いいぞ", "おめ" },
            [RareItem] = new[] { "！？", "神アイテム", "いいなー", "うらやま", "それ強い", "マジか", "当たりだ", "すげー" },
            [EtherWind] = new[] { "エーテルやば", "逃げろ", "マジかよ", "やばい", "うわ", "早く避難", "あかん" },
            [NewZone] = new[] { "ここどこ", "新エリア", "わくわく", "探索開始", "おお", "どんなとこ", "きた" },
            [Weather] = new[] { "雨か…", "晴れた", "雪だ", "いい天気", "天気変わった", "おお" },
            [Hunger] = new[] { "おなかすいた", "ごはん…", "腹減ったな", "何か食べて", "空腹やば" },
            [Tired] = new[] { "疲れてる", "休め", "寝ろ", "無理すんな", "大丈夫？" },

            // Tier B
            [BigDamage] = new[] { "痛そう", "つよ", "やば", "えぐ", "いたた", "ダメージでか" },
            [NpcEat] = new[] { "飯テロ", "おいしそう", "腹減った", "いいなー", "もぐもぐ" },
            [NpcMusic] = new[] { "いい曲", "888", "♪", "上手い", "いいね" },
        };

        public static string[] Get(string eventId)
        {
            return Reactions.TryGetValue(eventId, out var texts) ? texts : null;
        }

        public static string GetRandom(string eventId)
        {
            var texts = Get(eventId);
            if (texts == null || texts.Length == 0) return null;
            return texts[UnityEngine.Random.Range(0, texts.Length)];
        }
    }
}
