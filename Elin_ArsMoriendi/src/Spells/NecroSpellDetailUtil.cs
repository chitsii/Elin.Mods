using System;

namespace Elin_ArsMoriendi
{
    public static class NecroSpellDetailUtil
    {
        public static int GetCurrentPower(Act act)
        {
            var caster = act.owner?.Card ?? EClass.pc;
            if (caster == null) return 100;
            return Math.Max(1, act.GetPower(caster));
        }

        public static int EvaluateTurns(string conditionAlias, int power, int fallbackDivisor)
        {
            try
            {
                var con = Condition.Create(conditionAlias, power);
                if (con != null)
                    return Math.Max(1, con.EvaluateTurn(power));
            }
            catch
            {
                // Fallback below.
            }

            return Math.Max(1, power / Math.Max(1, fallbackDivisor));
        }

        public static string AppendLine(string baseDetail, string dynamicLine)
        {
            if (string.IsNullOrEmpty(dynamicLine))
                return baseDetail ?? string.Empty;
            if (string.IsNullOrEmpty(baseDetail))
                return dynamicLine;
            return baseDetail + Environment.NewLine + dynamicLine;
        }

        public static string L(string jp, string en, string cn)
        {
            if (Lang.langCode == "CN") return cn;
            return Lang.isJP ? jp : en;
        }
    }
}
