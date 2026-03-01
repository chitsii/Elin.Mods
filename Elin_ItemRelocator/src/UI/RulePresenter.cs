using System;
using System.Collections.Generic;
using System.Linq;

namespace Elin_ItemRelocator {
    public static class RulePresenter {
        // Helper to get active conditions for UI
        public static List<string> GetConditionList(RelocationRule rule) {
            List<string> parts = [];

            // Iterate Order? Group by type?
            // Existing UI order: Category, Rarity, Quality, Text, Weight, GenLvl, Dna...
            // We can iterate the list and map to string.
            // Or prioritize types for consistent display order.

            foreach (var cond in rule.Conditions) {
                if (cond is ConditionAddButton)
                    continue; // Skip add button in summary
                parts.Add(cond.GetUiLabel());
            }
            return parts;
        }
    }
}
