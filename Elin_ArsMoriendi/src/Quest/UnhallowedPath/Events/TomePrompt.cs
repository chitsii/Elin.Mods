namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Helper for grimoire-related notifications at different importance levels.
    /// High: Dialog.Ok (modal), Medium: PC bubble, Low: Msg.Say (log).
    /// </summary>
    public static class TomePrompt
    {
        /// <summary>Show a PC speech bubble (medium importance).</summary>
        public static void ShowBubble(string langKey)
        {
            try
            {
                var text = LangHelper.Get(langKey);
                EClass.pc?.TalkRaw(text);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn($"TomePrompt.ShowBubble error: {ex.Message}");
            }
        }

        /// <summary>Show a modal dialog (high importance, one-time events).</summary>
        public static void ShowDialog(string langKey)
        {
            try
            {
                var text = LangHelper.Get(langKey);
                Dialog.Ok(text);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn($"TomePrompt.ShowDialog error: {ex.Message}");
            }
        }
    }
}
