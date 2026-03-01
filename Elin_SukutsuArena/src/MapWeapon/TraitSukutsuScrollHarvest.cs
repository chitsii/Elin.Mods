namespace Elin_SukutsuArena.MapWeapon
{
    using Elin_SukutsuArena.Localization;

    /// <summary>
    /// 収穫スクロールTrait
    /// マップ全体の壁を採掘し、素材を収穫する
    /// </summary>
    public class TraitSukutsuScrollHarvest : TraitScroll
    {
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

            var param = MapWeaponParams.HarvestPreset();
            var result = MapWeaponEngine.Execute(c, param);

            if (result.Success)
            {
                // メッセージ表示
                Msg.Say(ArenaLocalization.ScrollHarvestResult(result.HarvestCount));

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
