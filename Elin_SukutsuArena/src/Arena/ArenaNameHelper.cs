namespace Elin_SukutsuArena.Arena;

/// <summary>
/// 名前表示の補助ユーティリティ
/// </summary>
public static class ArenaNameHelper
{
    /// <summary>
    /// 影の自己の名前をPC名ベースで組み立てる。
    /// 例: "影の自己・召喚形態" -> "{PC名}・召喚形態"
    /// </summary>
    public static string GetShadowSelfAltName(Chara shadow, Chara pc)
    {
        if (pc == null)
            return shadow?.Name ?? "";

        string postfix = shadow?.source?.GetDetail() ?? "";
        return string.IsNullOrEmpty(postfix) ? pc.Name : pc.Name + postfix;
    }
}
