using System;
using System.Collections.Generic;

namespace Elin_Elinikki
{
    internal static class FpsSpriteBudgetAllocator
    {
        public static void ApplyBudget<T>(
            List<T> items,
            int maxCount,
            Func<T, int> prioritySelector,
            Func<T, float> distanceSelector)
        {
            if (items == null || items.Count <= maxCount || maxCount < 0)
            {
                return;
            }

            if (maxCount == 0)
            {
                items.Clear();
                return;
            }

            items.Sort((a, b) =>
            {
                int priorityCompare = prioritySelector(a).CompareTo(prioritySelector(b));
                if (priorityCompare != 0)
                {
                    return priorityCompare;
                }

                return distanceSelector(a).CompareTo(distanceSelector(b));
            });

            if (items.Count > maxCount)
            {
                items.RemoveRange(maxCount, items.Count - maxCount);
            }
        }
    }
}
