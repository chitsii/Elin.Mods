namespace Elin_SukutsuArena.Feats;

/// <summary>
/// 闘志（Arena Spirit）フィート
/// 闘技場で戦い抜いた証として得られるフィート。
/// ランクが上がるごとにレベルが上がり、活力が向上する。
/// </summary>
internal class FeatArenaSpirit : Feat
{
    /// <summary>
    /// CWL によって呼び出されるフィート効果適用メソッド
    /// </summary>
    /// <param name="add">フィートのレベル（1-7）</param>
    /// <param name="eleOwner">フィート所有者のElementContainer</param>
    /// <param name="hint">trueの場合はヒント情報収集モード</param>
    internal void _OnApply(int add, ElementContainer eleOwner, bool hint)
    {
        // スタミナ（活力）ボーナス：1レベルにつき +2
        int staminaBonus = add * 2;

        if (hint)
        {
            // hints.Add() で直接表示を制御
            hints.Add($"活力 +{staminaBonus}");
        }
        else
        {
            // 実際の効果適用（hint=false の時のみ）
            eleOwner.ModBase(62, staminaBonus); // vigor = 62
        }
    }
}
