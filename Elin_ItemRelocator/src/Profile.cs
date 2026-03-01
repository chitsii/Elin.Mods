using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine;

namespace Elin_ItemRelocator {
    [Serializable]
    public class RelocationProfile {
        public string ContainerName; // For display/debug
        public bool Enabled = true;
        public FilterScope Scope = FilterScope.Both;
        public int Version = 0;

        // Main Data
        public List<RelocationRule> Rules = [];

        public ResultSortMode SortMode = ResultSortMode.Default;

        // Scope definition
        public enum FilterScope { Inventory, Both, ZoneOnly, PetsOnly }

        public enum ResultSortMode {
            Default, PriceAsc, PriceDesc, EnchantMagAsc, EnchantMagDesc, TotalEnchantMagDesc,
            TotalWeightAsc, TotalWeightDesc, UnitWeightAsc, UnitWeightDesc, UidAsc, UidDesc, GenLvlAsc, GenLvlDesc, DnaAsc, DnaDesc,
            FoodPowerAsc, FoodPowerDesc,
            TotalFoodPowerAsc, TotalFoodPowerDesc
        }

        public enum RelocatorOp { Ge, Le, Eq, Ne, Gt, Lt }
    }

    [JsonConverter(typeof(RelocationRuleConverter))]
    [Serializable]
    public class RelocationRule {
        public string Name = RelocatorLang.GetText(RelocatorLang.LangKey.NewRuleName);
        public bool Enabled = true;

        [JsonIgnore]
        public bool _migrated = false;

        // Polymorphic Conditions
        public List<ICondition> Conditions = new();

        // Matching Logic (AND within Rule)
        public bool IsMatch(Thing t) {
            if (!Enabled || t.c_lockedHard)
                return false;

            // Safe Default: If no conditions, match nothing (or match all? old logic was match nothing if empty)
            if (Conditions.Count == 0)
                return false;

            foreach (var cond in Conditions) {
                if (!cond.IsMatch(t))
                    return false;
            }
            return true;
        }
    }

    public class RelocationRuleConverter : JsonConverter {
        public override bool CanConvert(Type objectType) => objectType == typeof(RelocationRule);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            JObject jo = JObject.Load(reader);
            var rule = new RelocationRule();

            if (jo["Name"] != null)
                rule.Name = jo["Name"].ToString();
            if (jo["Enabled"] != null)
                rule.Enabled = (bool)jo["Enabled"];

            // Hybrid List Format (New Standard)
            var jArray = jo["Conditions"] as JArray;
            if (jArray != null && jArray.Count > 0) {
                foreach (var token in jArray) {
                    if (token is JObject obj) {
                        foreach (var (key, loader) in ConditionRegistry.Loaders) {
                            if (obj[key] != null) {
                                ICondition c = loader(obj);
                                if (c != null) {
                                    rule.Conditions.Add(c);
                                    break; // Only match one type per object (priority based on registration order)
                                }
                            }
                        }
                    }
                }
                if (rule.Conditions.Count > 0)
                    return rule;
            }

            // Fallback: Legacy Flat Root Loading (Format A compatibility)
            ParseLegacyRootFields(jo, rule);
            return rule;
        }

        // [Legacy Support] Remove this method after 2026/04
        private void ParseLegacyRootFields(JObject jo, RelocationRule rule) {
            foreach (var (key, loader) in ConditionRegistry.Loaders) {
                if (jo[key] != null) {
                    ICondition c = loader(jo);
                    if (c != null)
                        rule.Conditions.Add(c);
                }
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var rule = (RelocationRule)value;
            var jo = new JObject();
            jo.Add("Name", rule.Name);
            jo.Add("Enabled", rule.Enabled);

            var condArray = new JArray();

            foreach (var cond in rule.Conditions) {
                var cObj = new JObject();
                // Find saver
                if (ConditionRegistry.Savers.TryGetValue(cond.GetType(), out var saver)) {
                    saver(cObj, cond);
                    condArray.Add(cObj);
                }
            }

            jo.Add("Conditions", condArray);
            jo.WriteTo(writer);
        }
    }
}
