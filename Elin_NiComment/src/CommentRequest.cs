using UnityEngine;

namespace Elin_NiComment
{
    /// <summary>
    /// Data carrier for a comment to be displayed on screen.
    /// Sources create these and pass them to <see cref="NiCommentAPI.Send"/>.
    /// </summary>
    public struct CommentRequest
    {
        public string Text;
        public Color Color;

        /// <summary>
        /// Override font size for this comment. 0 or negative = use config default.
        /// </summary>
        public int FontSizeOverride;

        public CommentRequest(string text)
        {
            Text = text;
            Color = Color.white;
            FontSizeOverride = 0;
        }

        public CommentRequest(string text, Color color)
        {
            Text = text;
            Color = color;
            FontSizeOverride = 0;
        }

        public int EffectiveFontSize =>
            FontSizeOverride > 0 ? FontSizeOverride : ModConfig.FontSize.Value;
    }
}
