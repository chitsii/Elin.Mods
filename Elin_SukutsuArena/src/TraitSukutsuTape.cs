namespace Elin_SukutsuArena
{
    /// <summary>
    /// 巣窟アリーナ用カセットテープTrait
    ///
    /// バニラのTraitTapeはOnCreateでrefValをランダムに上書きしてしまうため、
    /// traitパラメータからBGM IDを取得してrefValを設定するカスタム版。
    ///
    /// trait列で "Elin_SukutsuArena.TraitSukutsuTape,{bgm_id}" と指定して使用。
    /// </summary>
    public class TraitSukutsuTape : TraitTape
    {
        /// <summary>
        /// アイテム作成時の処理
        /// traitパラメータからBGM IDを取得してrefValに設定
        /// </summary>
        public override void OnCreate(int lv)
        {
            // traitパラメータからBGM IDを取得（Tape,{bgm_id}）
            string bgmIdParam = GetParam(1);
            if (!string.IsNullOrEmpty(bgmIdParam))
            {
                int bgmId;
                if (int.TryParse(bgmIdParam, out bgmId))
                {
                    owner.refVal = bgmId;
                    return;
                }
            }

            // フォールバック: バニラの動作（ランダムBGM）
            base.OnCreate(lv);
        }
    }
}
