using System;
using System.Collections.Generic;

namespace Elin_JustDoomIt
{
    internal static class DoomBindingParser
    {
        public static IReadOnlyList<DoomBindingToken> ParseCsv(string csv)
        {
            var list = new List<DoomBindingToken>();
            if (string.IsNullOrWhiteSpace(csv))
            {
                return list;
            }

            var parts = csv.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var token = parts[i]?.Trim();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (TryParseSpecial(token, out var parsed))
                {
                    list.Add(parsed);
                    continue;
                }

                list.Add(DoomBindingToken.ForKey(token));
            }

            return list;
        }

        private static bool TryParseSpecial(string token, out DoomBindingToken parsed)
        {
            if (token.Equals("WheelUp", StringComparison.OrdinalIgnoreCase))
            {
                parsed = DoomBindingToken.ForWheelUp();
                return true;
            }

            if (token.Equals("WheelDown", StringComparison.OrdinalIgnoreCase))
            {
                parsed = DoomBindingToken.ForWheelDown();
                return true;
            }

            if (token.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase) &&
                token.Length > "Mouse".Length &&
                int.TryParse(token.Substring("Mouse".Length), out var mouseButton))
            {
                parsed = DoomBindingToken.ForMouseButton(mouseButton);
                return true;
            }

            parsed = default;
            return false;
        }
    }
}
