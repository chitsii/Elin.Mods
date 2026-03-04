using System.Collections.Generic;
using UnityEngine;

namespace Elin_NiComment.Llm
{
    public static class KotehanRegistry
    {
        public static readonly Dictionary<string, Color> Characters = new()
        {
            ["エヘカトル"] = new Color(1f, 0.6f, 0.8f),      // pink
            ["ジュア"] = new Color(1f, 0.95f, 0.7f),          // warm gold
            ["ルルウィ"] = new Color(0.6f, 0.85f, 1f),        // sky blue
            ["オパートス"] = new Color(1f, 0.75f, 0.3f),      // amber
            ["クミロミ"] = new Color(0.5f, 0.9f, 0.5f),       // green
            ["マニ"] = new Color(0.7f, 0.8f, 0.95f),          // steel blue
            ["イツパロトル"] = new Color(1f, 0.5f, 0.2f),     // fiery
            ["ラーネイレ"] = new Color(1f, 0.7f, 0.75f),      // rose
            ["ロミアス"] = new Color(0.5f, 0.9f, 0.85f),      // teal
        };

        public static CommentRequest ToRequest(string name, string text)
        {
            var displayText = string.IsNullOrEmpty(name) ? text : $"{name}: {text}";
            var color = !string.IsNullOrEmpty(name) && Characters.TryGetValue(name, out var c)
                ? c
                : Color.white;
            return new CommentRequest(displayText, color);
        }
    }
}
