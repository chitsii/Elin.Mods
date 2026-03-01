using System;

namespace Elin_QuestMod.CommonQuest
{
    public static class QuestCondition
    {
        public static Func<bool> All(params Func<bool>[] conditions)
        {
            if (conditions == null) throw new ArgumentNullException(nameof(conditions));
            return () =>
            {
                for (int i = 0; i < conditions.Length; i++)
                {
                    var condition = conditions[i];
                    if (condition == null || !condition()) return false;
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
                    var condition = conditions[i];
                    if (condition != null && condition()) return true;
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
