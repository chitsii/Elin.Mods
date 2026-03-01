namespace Elin_SukutsuArena.MapWeapon
{
    using Elin_SukutsuArena.Localization;

    /// <summary>
    /// 攻撃スクロールTrait
    /// マップ全体の敵に属性ダメージを与える
    /// </summary>
    public class TraitSukutsuScrollAttack : TraitScroll
    {
        /// <summary>
        /// 属性エイリアス（trait パラメータから取得、デフォルトは炎）
        /// </summary>
        public string ElementAlias => GetParam(1) ?? "eleFire";

        /// <summary>
        /// 威力（trait パラメータから取得、デフォルトは500）
        /// </summary>
        public int Power
        {
            get
            {
                string powerParam = GetParam(2);
                if (!string.IsNullOrEmpty(powerParam) && int.TryParse(powerParam, out int p))
                {
                    return p;
                }
                return 500;
            }
        }

        /// <summary>
        /// スクロールを読んだ時の処理
        /// </summary>
        public override void OnRead(Chara c)
        {
            // 混乱チェック
            if ((c.isConfused || c.HasCondition<ConDim>()) && EClass.rnd(4) == 0)
            {
                c.Say("stagger", c);
                return;
            }

            var param = MapWeaponParams.AttackPreset(ElementAlias, Power);

            // 属性名からエフェクトIDを生成
            string eleName = ElementAlias.Replace("ele", "");
            param.EffectId = $"ball_{eleName}";

            var result = MapWeaponEngine.Execute(c, param);

            if (result.Success)
            {
                // メッセージ表示
                Msg.Say(ArenaLocalization.ScrollAttackResult(result.DamageCount));

                // スクロール消費
                owner.ModNum(-1);

                // スキル経験値獲得（リテラシー）
                c.elements.ModExp(285, 50f);
            }
            else
            {
                Msg.Say(ArenaLocalization.NothingHappens);
            }
        }

        /// <summary>
        /// 読めるかどうかの判定
        /// </summary>
        public override bool CanRead(Chara c)
        {
            if (c.IsPC && !c.isBlind)
            {
                if (EClass._zone.IsUserZone)
                {
                    return !owner.isNPCProperty;
                }
                return true;
            }
            return false;
        }
    }
}
