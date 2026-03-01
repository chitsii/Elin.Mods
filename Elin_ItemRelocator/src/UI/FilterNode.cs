using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_ItemRelocator {
    public enum ConditionType { None, Category, Rarity, Quality, Text, Id, AddButton, Settings, Weight, Material, Bless, Stolen, Enchant, Identified, GenLvl, Dna, DnaContent, FoodTraits, SourceContainer }

    public class FilterNode {
        public RelocationRule Rule;     // Level 1: Rule

        // Level 2: Condition Details
        public ConditionType CondType;
        public string CondValue; // ID or Text
        public ICondition ConditionRef; // New Ref
        public string DisplayText = "";

        public bool IsRule => Rule is not null && CondType is ConditionType.None;
        public bool IsCondition => CondType is not ConditionType.None and not ConditionType.AddButton and not ConditionType.Settings;
        public bool IsAddButton => CondType is ConditionType.AddButton;
        public bool IsSettings => CondType is ConditionType.Settings;

        // Override Equals/GetHashCode for Persistence (Expansion State)
        public override bool Equals(object obj) {
            if (obj is not FilterNode other)
                return false;
            if (IsRule && other.IsRule)
                return Rule == other.Rule; // Identity based on Rule instance
            return base.Equals(obj);
        }
        public override int GetHashCode() {
            if (IsRule)
                return Rule.GetHashCode();
            return base.GetHashCode();
        }
    }
}
