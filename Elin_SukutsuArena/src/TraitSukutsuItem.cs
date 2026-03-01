using UnityEngine;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// 巣窟アリーナ専用カスタムアイテムTrait
    ///
    /// TraitDrinkを継承し、OnDrinkで代償付き効果を発動。
    /// trait列で "Elin_SukutsuArena.TraitSukutsuItem,効果ID" と指定して使用。
    /// </summary>
    public class TraitSukutsuItem : TraitDrink
    {
        /// <summary>
        /// 親クラスの IdEffect をオーバーライド
        /// カスタム効果IDはバニラの EffectId 列挙型に存在しないため、
        /// デフォルト値を返して列挙型変換エラーを回避
        /// </summary>
        public override EffectId IdEffect => global::EffectId.Drink;

        /// <summary>
        /// 効果ID（item_definitions.py の EFFECT_DEFINITIONS キーに対応）
        /// trait列の2番目のパラメータで指定
        /// </summary>
        public string CustomEffectId => GetParam(1) ?? "";

        /// <summary>
        /// 飲んだ時の処理
        /// </summary>
        public override void OnDrink(Chara c)
        {
            switch (CustomEffectId)
            {
                case "kiss_of_inferno":
                    ApplyKissOfInferno(c);
                    break;

                case "frost_ward":
                    ApplyConditionByAlias(c, "ConSukutsuResCold", -15);
                    break;

                case "mind_ward":
                    ApplyConditionByAlias(c, "ConSukutsuResDarkness", -15);
                    break;

                case "chaos_ward":
                    ApplyConditionByAlias(c, "ConSukutsuResChaos", -15);
                    break;

                case "sound_ward":
                    ApplyConditionByAlias(c, "ConSukutsuResSound", -15);
                    break;

                case "impact_ward":
                    ApplyConditionByAlias(c, "ConSukutsuResImpact", -15);
                    break;

                case "bleed_ward":
                    ApplyConditionByAlias(c, "ConSukutsuResCut", -15);
                    break;

                case "painkiller":
                    ApplyPainkiller(c);
                    break;

                case "stimulant":
                    ApplyStimulant(c);
                    break;

                default:
                    Debug.LogWarning($"[SukutsuArena] Unknown effect ID: {CustomEffectId}, falling back to base");
                    base.OnDrink(c);
                    break;
            }
        }

        /// <summary>
        /// 万難のエリクサー効果
        /// - 複数耐性バフ（一時的）
        /// - カルマ-30
        /// </summary>
        private void ApplyKissOfInferno(Chara c)
        {
            // 演出
            c.PlaySound("curse");
            c.PlayEffect("curse");

            // 付与する耐性Condition（alias）
            string[] conditionAliases = new string[]
            {
                "ConSukutsuResCold",
                "ConSukutsuResDarkness",
                "ConSukutsuResChaos",
                "ConSukutsuResSound",
                "ConSukutsuResImpact",
                "ConSukutsuResCut",
            };

            // カスタムConditionはdurationで3000ターン固定なのでpower=100で十分
            foreach (string alias in conditionAliases)
            {
                c.AddCondition(Condition.Create(alias, 100));
            }

            // カルマ減少（PCのみ）
            if (c.IsPC)
            {
                EClass.player.ModKarma(-30);
            }

            // CWL rarity pollution対策: CardBlueprint.currentが残っていればリセット
            if (CardBlueprint.current != null)
            {
                CardBlueprint.current = null;
            }
        }

        /// <summary>
        /// Condition aliasを使ってバフを付与
        /// </summary>
        private void ApplyConditionByAlias(Chara c, string conditionAlias, int karmaChange)
        {
            // 演出
            c.PlaySound("buff");
            c.PlayEffect("buff");

            // Conditionを付与（カスタムConditionはdurationで3000ターン固定）
            c.AddCondition(Condition.Create(conditionAlias, 100));

            // カルマ減少（PCのみ）
            if (c.IsPC && karmaChange != 0)
            {
                EClass.player.ModKarma(karmaChange);
            }

            // CWL rarity pollution対策: CardBlueprint.currentが残っていればリセット
            if (CardBlueprint.current != null)
            {
                CardBlueprint.current = null;
            }
        }

        /// <summary>
        /// 痛覚遮断薬効果
        /// - 物理ダメージ軽減（PVバフ、一時的）
        /// - 薬物中毒状態付与（回復阻止）
        /// </summary>
        private void ApplyPainkiller(Chara c)
        {
            // 演出
            c.PlaySound("curse");
            c.PlayEffect("curse");

            // PVを一時的に上昇（カスタムConditionはdurationで3000ターン固定）
            c.AddCondition(Condition.Create("ConSukutsuPVBuff", 100));

            // 薬物中毒状態付与（カスタムConditionはdurationで3000ターン固定）
            c.AddCondition(Condition.Create("ConSukutsuPoison", 100));

            // カルマ減少
            if (c.IsPC)
            {
                EClass.player.ModKarma(-10);
            }

            // CWL rarity pollution対策: CardBlueprint.currentが残っていればリセット
            if (CardBlueprint.current != null)
            {
                CardBlueprint.current = null;
            }
        }

        /// <summary>
        /// 禁断の覚醒剤効果
        /// - 覚醒状態付与（能力上昇）
        /// - 後遺症として内出血
        /// </summary>
        private void ApplyStimulant(Chara c)
        {
            // 演出
            c.PlaySound("boost2");
            c.PlayEffect("buff");

            // 覚醒状態付与（カスタムConditionはdurationで3000ターン固定）
            c.AddCondition(Condition.Create("ConSukutsuBoost", 100));

            // 内出血状態付与（カスタムConditionはdurationで3000ターン固定）
            c.AddCondition(Condition.Create("ConSukutsuBleed", 100));

            // カルマ減少
            if (c.IsPC)
            {
                EClass.player.ModKarma(-15);
            }

            // CWL rarity pollution対策: CardBlueprint.currentが残っていればリセット
            if (CardBlueprint.current != null)
            {
                CardBlueprint.current = null;
            }
        }

    }
}
