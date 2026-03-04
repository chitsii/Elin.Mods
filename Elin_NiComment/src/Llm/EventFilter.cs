using System.Text.RegularExpressions;

namespace Elin_NiComment.Llm
{
    public static class EventFilter
    {
        // <color=#FF0000>text</color>, <i>text</i>, etc.
        private static readonly Regex HtmlTagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);

        // [ 浮遊 ], [ 1 回復 ], etc.
        private static readonly Regex BracketStatusRegex =
            new Regex(@"\[\s*[^\]]*\s*\]", RegexOptions.Compiled);

        // 「X」where X is 8 chars or fewer (animal sounds, short exclamations)
        private static readonly Regex ShortSpeechRegex =
            new Regex(@"「.{1,8}」", RegexOptions.Compiled);

        // Baby talk: 「ばぶ...」「バブ...」「(ばぶ...)」「(オギャ...)」「(あうあう...)」etc.
        private static readonly Regex BabyTalkRegex =
            new Regex(@"「[\(（]?(?:ばぶ|バブ|オギャ|ぁぅ|あうあう|ママ)", RegexOptions.Compiled);

        // Animal sounds: elongated kana in quotes — 「メェエェェェ」「フーーーー」etc.
        private static readonly Regex AnimalSoundRegex =
            new Regex(@"^「[ァ-ヶー～♪ｰ]+」$", RegexOptions.Compiled);

        private static readonly string[] BlacklistSuffixes =
        {
            "を押しのけた",
            "と入れ替わった",
        };

        /// <summary>
        /// Returns true if the event should be kept. Cleaned text is output via <paramref name="cleaned"/>.
        /// </summary>
        public static bool TryFilter(string raw, out string cleaned)
        {
            cleaned = null;
            if (string.IsNullOrEmpty(raw))
                return false;

            // 1. Strip HTML tags
            var text = HtmlTagRegex.Replace(raw, "");

            // 2. Strip bracket statuses
            text = BracketStatusRegex.Replace(text, "");

            // 3. Blacklist patterns — strip trailing punctuation then suffix match
            var stripped = text.TrimEnd('。', '！', '？', '…', '♪', ' ', '　');
            foreach (var suffix in BlacklistSuffixes)
            {
                if (stripped.EndsWith(suffix))
                    return false;
            }

            // 4a. Baby talk pattern (any length)
            if (BabyTalkRegex.IsMatch(text))
                return false;

            // 4b. Pure katakana animal sounds in quotes
            var trimmed = text.Trim();
            if (AnimalSoundRegex.IsMatch(trimmed))
                return false;

            // 4c. Short speech exclusion (≤5 chars inside quotes)
            text = ShortSpeechRegex.Replace(text, "");

            // 5. If nothing meaningful remains, discard
            cleaned = text.Trim();
            return cleaned.Length > 0;
        }
    }
}
