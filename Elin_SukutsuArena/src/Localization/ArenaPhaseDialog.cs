namespace Elin_SukutsuArena.Localization
{
    /// <summary>
    /// ボスフェーズ移行時の台詞（汎用/アスタロト/影の自己）
    /// </summary>
    public static class ArenaPhaseDialog
    {
        public enum Speaker
        {
            Generic,
            Astaroth,
            ShadowSelf
        }

        private static bool IsJP => Lang.isJP;
        private static bool IsCN => !Lang.isJP && Lang.langCode == "CN";

        private static string L(string jp, string en, string cn) =>
            IsJP ? jp : (IsCN ? cn : en);

        public static string GetLine(Speaker speaker, int nextPhase)
        {
            return speaker switch
            {
                Speaker.Astaroth => GetAstarothLine(nextPhase),
                Speaker.ShadowSelf => GetShadowSelfLine(nextPhase),
                _ => GetGenericLine(nextPhase)
            };
        }

        private static string GetGenericLine(int nextPhase) => nextPhase switch
        {
            1 => L("正念場だ。", "The real fight begins.", "真正的战斗开始了。"),
            2 => L("引くな。", "Do not falter.", "别退缩。"),
            3 => L("終わらせる。", "I will end this.", "我将结束这一切。"),
            _ => ""
        };

        private static string GetAstarothLine(int nextPhase) => nextPhase switch
        {
            1 => L("ここまで本気になるのは久しぶりだ。", "It has been a while since I fought seriously.", "好久没这么认真了。"),
            2 => L("フッ、油断大敵！", "Hmph, carelessness is fatal!", "哼，疏忽大意会致命！"),
            3 => L("これで最後だ！", "This is the end!", "到此为止！"),
            _ => ""
        };

        private static string GetShadowSelfLine(int nextPhase) => nextPhase switch
        {
            1 => L("まだ立ってるの？ しぶといね。", "Still standing? How persistent.", "还站着？真顽强。"),
            2 => L("ほらほら、足が止まってるぞ。", "Come on, your feet are slowing.", "怎么了，脚都慢了。"),
            3 => L("終幕だ。泣き顔を見せろ。", "Curtain call. Let me see you break.", "终幕了，让我看看你崩溃。"),
            _ => ""
        };
    }
}
