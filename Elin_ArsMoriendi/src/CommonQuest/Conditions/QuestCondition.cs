using System;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Small condition-composition helpers for declarative quest transition rules.
    /// </summary>
    public static class QuestCondition
    {
        public static Func<bool> All(params Func<bool>[] conditions)
        {
            if (conditions == null) throw new ArgumentNullException(nameof(conditions));
            return () =>
            {
                for (int i = 0; i < conditions.Length; i++)
                {
                    var c = conditions[i];
                    if (c == null || !c()) return false;
                }
                return true;
            };
        }

        public static Func<bool> Any(params Func<bool>[] conditions)
        {
            if (conditions == null) throw new ArgumentNullException(nameof(conditions));
            return () =>
            {
                for (int i = 0; i < conditions.Length; i++)
                {
                    var c = conditions[i];
                    if (c != null && c()) return true;
                }
                return false;
            };
        }

        public static Func<bool> Not(Func<bool> condition)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return () => !condition();
        }
    }
}
