using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Elin_ItemRelocator {

    // Discriminator based polymorphism
    public interface ICondition {
        bool IsMatch(Thing t);
        string GetUiLabel();
        ConditionType GetConditionType();
    }

    public abstract class BaseCondition : ICondition {
        public bool Not; // Negation flag common to most
        public abstract bool IsMatch(Thing t);
        public abstract string GetUiLabel();
        public abstract ConditionType GetConditionType();

        public string GetNotPrefix() => Not ? RelocatorLang.GetText(RelocatorLang.LangKey.Not) + " " : "";

        protected bool CheckOp(int value, string op, int target) {
            return op switch {
                ">=" => value >= target,
                ">" => value > target,
                "<=" => value <= target,
                "<" => value < target,
                "==" => value == target,
                "=" => value == target,
                "!=" => value != target,
                _ => value >= target
            };
        }
    }

    public class ConditionCategory : BaseCondition {
        public HashSet<string> CategoryIds = new();

        public override bool IsMatch(Thing t) {
            if (CategoryIds.Count == 0)
                return false;
            foreach (var id in CategoryIds) {
                if (t.category.IsChildOf(id)) {
                    return !Not;
                }
            }
            return Not;
        }

        public override ConditionType GetConditionType() => ConditionType.Category;
        public override string GetUiLabel() {
            List<string> names = [];
            foreach (var id in CategoryIds) {
                var source = EClass.sources.categories.map.TryGetValue(id);
                names.Add(source is not null ? source.GetName() : id);
            }
            // Truncation Logic (Limit 10)
            string display = "";
            int limit = 10;
            if (names.Count <= limit) {
                display = string.Join(", ", names);
            } else {
                display = string.Join(", ", names.Take(limit)) + " ... (+" + (names.Count - limit) + ")";
            }
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Category) + ": " + display;
        }
    }

    public class ConditionRarity : BaseCondition {
        public HashSet<int> Rarities = new();
        public override bool IsMatch(Thing t) {
            if (Rarities.Count == 0)
                return false;
            bool match = Rarities.Contains((int)t.rarity);
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.Rarity;
        public override string GetUiLabel() {
            var qualityNames = Lang.GetList("quality");
            List<string> display = [];
            var sorted = Rarities.ToList();
            sorted.Sort();
            foreach (var rar in sorted) {
                int idx = rar + 1;
                if (idx >= 0 && idx < qualityNames.Length)
                    display.Add(qualityNames[idx]);
                else
                    display.Add(rar.ToString());
            }
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Rarity) + ": " + string.Join(", ", display.ToArray());
        }
    }

    public class ConditionQuality : BaseCondition {
        public string Op = ">=";
        public int Value;

        public override bool IsMatch(Thing t) {
            bool match = CheckOp((int)t.encLV, Op, Value);
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.Quality;
        public override string GetUiLabel() => GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Enhancement) + " " + Op + Value;
    }

    public class ConditionWeight : BaseCondition {
        public string Op = ">=";
        public int Value;
        public override bool IsMatch(Thing t) {
            bool match = CheckOp(t.SelfWeight, Op, Value);
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.Weight;
        public override string GetUiLabel() => GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Weight) + " " + Op + " " + Value;
    }

    public class ConditionGenLvl : BaseCondition {
        public string Op = ">=";
        public int Value;
        public override bool IsMatch(Thing t) {
            bool match = CheckOp(t.genLv, Op, Value);
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.GenLvl;
        public override string GetUiLabel() => GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.GenLvl) + " " + Op + " " + Value;
    }

    public class ConditionDna : BaseCondition {
        public string Op = ">=";
        public int Value;
        public override bool IsMatch(Thing t) {
            if (t.c_DNA == null)
                return Not;
            bool match = CheckOp(t.c_DNA.cost, Op, Value);
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.Dna;
        public override string GetUiLabel() => GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Dna) + " " + Op + " " + Value;
    }

    public class ConditionText : BaseCondition {
        public string Text;
        public override bool IsMatch(Thing t) {
            if (string.IsNullOrEmpty(Text))
                return false;
            string name = t.Name.ToLower();
            string rawName = t.source.GetName().ToLower();
            string[] parts = Text.Split([' ', '　'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts) {
                string q = part.Trim().ToLower();
                if (!name.Contains(q) && !rawName.Contains(q))
                    return Not; // If any part mismatches, fail (AND logic normally). Wait, original was AND.
            }
            return !Not;
        }
        public override ConditionType GetConditionType() => ConditionType.Text;
        public override string GetUiLabel() => GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Text) + ": " + Text;
    }

    public class ConditionMaterial : BaseCondition {
        public HashSet<string> MaterialIds = new(StringComparer.OrdinalIgnoreCase);
        public override bool IsMatch(Thing t) {
            if (t.material == null)
                return Not;
            bool match = MaterialIds.Contains(t.material.alias) || MaterialIds.Contains(t.material.id.ToString());
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.Material;
        public override string GetUiLabel() {
            // Truncation Logic (Limit 10)
            string display = "";
            int limit = 10;
            List<string> names = [];
            foreach (var mid in MaterialIds) {
                var ms = EClass.sources.materials.rows.FirstOrDefault(m => m.alias.Equals(mid, StringComparison.OrdinalIgnoreCase) || m.id.ToString() == mid);
                names.Add(ms is not null ? ms.GetName() : mid);
            }
            if (names.Count <= limit) {
                display = string.Join(", ", names);
            } else {
                display = string.Join(", ", names.Take(limit)) + " ... (+" + (names.Count - limit) + ")";
            }
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Material) + ": " + display;
        }
    }

    public class ConditionBless : BaseCondition {
        public HashSet<int> States = new();
        public override bool IsMatch(Thing t) {
            bool match = States.Contains((int)t.blessedState);
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.Bless;
        public override string GetUiLabel() {
            List<string> sNames = [];
            foreach (var s in States) {
                string key = s switch {
                    1 => RelocatorLang.LangKey.StateBlessed.ToString(),
                    -1 => RelocatorLang.LangKey.StateCursed.ToString(),
                    -2 => RelocatorLang.LangKey.StateDoomed.ToString(),
                    0 => RelocatorLang.LangKey.StateNormal.ToString(),
                    _ => s.ToString()
                };
                sNames.Add(Enum.TryParse(key, out RelocatorLang.LangKey k) ? RelocatorLang.GetText(k) : s.ToString());
            }
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.Bless) + ": " + string.Join(", ", sNames);
        }
    }

    public class ConditionStolen : BaseCondition {
        public bool IsStolen; // Target value
        public override bool IsMatch(Thing t) {
            bool match = t.isStolen == IsStolen;
            return Not ? !match : match; // Usually not negated for boolean flags, but BaseCondition has it.
        }
        public override ConditionType GetConditionType() => ConditionType.Stolen;
        public override string GetUiLabel() {
            string val = IsStolen ? RelocatorLang.GetText(RelocatorLang.LangKey.Stolen) : RelocatorLang.GetText(RelocatorLang.LangKey.StateNormal);
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.StolenState) + ": " + val;
        }
    }

    public class ConditionIdentified : BaseCondition {
        public bool IsIdentified;
        public override bool IsMatch(Thing t) {
            bool match = t.IsIdentified == IsIdentified;
            return Not ? !match : match;
        }
        public override ConditionType GetConditionType() => ConditionType.Identified;
        public override string GetUiLabel() {
            string val = IsIdentified ? RelocatorLang.GetText(RelocatorLang.LangKey.StateIdentified) : RelocatorLang.GetText(RelocatorLang.LangKey.StateUnidentified);
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.IdentifiedState) + ": " + val;
        }
    }

    public class ConditionDnaContent : BaseCondition {
        public HashSet<string> DnaIds = new();
        public bool IsAndMode;

        public override bool IsMatch(Thing t) {
            if (t.c_DNA == null)
                return false;

            if (DnaIds.Count == 0)
                return false;

            bool match;
            if (IsAndMode) {
                // AND: All DnaIds must be present
                match = true;
                foreach (string cond in DnaIds) {
                    if (!CheckSingleDna(t, cond)) {
                        match = false;
                        break;
                    }
                }
            } else {
                // OR: Any DnaId present matches
                match = false;
                foreach (string cond in DnaIds) {
                    if (CheckSingleDna(t, cond)) {
                        match = true;
                        break;
                    }
                }
            }
            return Not ? !match : match;
        }

        private bool CheckSingleDna(Thing t, string cond) {
            ConditionRegistry.ParseKeyOp(cond, out string key, out string op, out int val);

            // Determine if value was explicitly provided or just default
            // ParseKeyOp defaults to ">=1", but if the raw string didn't have operator?
            // ParseKeyOp doesn't return that info.
            // Check raw string for operator presence if needed or just trust defaults.
            // For DNA, we might want ">=1" if no operator matches behavior of enchant?
            // Enchant defaults to val=1.

            // Re-check raw string for operator to know if we skip val check (hasVal logic)
            // Or just always check val? If user selects "Mining", we want ANY mining.
            // "Mining" -> op=">=", val=1.
            // If item has Mining=5, 5>=1 is true. Correct.
            // If item has Mining=0 (not present?), vals lookup fails.

            // Lookup ID
            int elementId = EClass.sources.elements.alias.TryGetValue(key, out var source) ? source.id : -1;
            if (elementId != -1 && t.c_DNA != null && t.c_DNA.vals != null) {
                for (int i = 0; i < t.c_DNA.vals.Count; i += 2) {
                    if (t.c_DNA.vals[i] == elementId) {
                        int curVal = t.c_DNA.vals[i + 1];
                        return ConditionRegistry.CheckOp(curVal, op, val);
                    }
                }
            }
            return false;
        }

        public override ConditionType GetConditionType() => ConditionType.DnaContent;
        public override string GetUiLabel() {
            List<string> names = [];
            foreach (var id in DnaIds) {
                ConditionRegistry.ParseKeyOp(id, out string key, out string op, out int val);
                string suffix = op + val;
                string displayName = key;
                if (EClass.sources.elements.alias.TryGetValue(key, out var source)) {
                    displayName = source.GetName();
                }
                names.Add(displayName + suffix);
            }
            // Truncation Logic (Limit 10)
            string display = "";
            int limit = 10;
            if (names.Count <= limit) {
                display = string.Join(", ", names);
            } else {
                display = string.Join(", ", names.Take(limit)) + " ... (+" + (names.Count - limit) + ")";
            }
            string mode = IsAndMode ? " (AND)" : " (OR)";
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.DnaContent) + mode + ": " + display;
        }
    }



    public class ConditionEnchantOr : BaseCondition {
        public List<string> Runes = new();
        public bool IsAndMode; // If true, all runes must match (AND). If false, any rune matches (OR).

        public override bool IsMatch(Thing t) {
            if (Runes.Count == 0)
                return false;

            bool match;
            if (IsAndMode) {
                match = true;
                foreach (var r in Runes) {
                    if (!ConditionRegistry.CheckEnchantMatch(t, r, CheckOp)) {
                        match = false;
                        break;
                    }
                }
            } else {
                match = false;
                foreach (var r in Runes) {
                    if (ConditionRegistry.CheckEnchantMatch(t, r, CheckOp)) {
                        match = true;
                        break;
                    }
                }
            }
            return Not ? !match : match;
        }

        public override ConditionType GetConditionType() => ConditionType.Enchant;
        public override string GetUiLabel() {
            string mode = IsAndMode ? " (AND)" : " (OR)";
            List<string> displayNames = [];
            foreach (var rune in Runes) {
                ConditionRegistry.ParseKeyOp(rune, out string key, out string op, out int val);
                string suffix = op + val;
                string dName = key;
                if (EClass.sources.elements.alias.TryGetValue(key, out var source)) {
                    dName = source.GetName();
                }
                displayNames.Add(dName + suffix);
            }
            string display = "";
            int limit = 10;
            if (displayNames.Count <= limit) {
                display = string.Join(", ", displayNames);
            } else {
                display = string.Join(", ", displayNames.Take(limit)) + " ... (+" + (displayNames.Count - limit) + ")";
            }
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.EnchantOr) + mode + ": " + display;
        }
    }

    public class ConditionFoodElement : BaseCondition {
        public HashSet<string> ElementIds = new();
        public bool IsAndMode; // Added IsAndMode

        public override bool IsMatch(Thing t) {
            try {
                if (t == null || t.elements == null)
                    return false;

                // Check if it's food or drink (anything that can have food traits)
                if (!t.IsFood && t.trait is not TraitDrink)
                    return false;

                if (ElementIds.Count == 0)
                    return false;

                if (IsAndMode) {
                    foreach (var id in ElementIds) {
                        if (!ConditionRegistry.CheckFoodTraitMatch(t, id, CheckOp))
                            return Not;
                    }
                    // Loop completed = All matched. Match = true.
                    return !Not;
                } else {
                    // OR Mode
                    foreach (var id in ElementIds) {
                        if (ConditionRegistry.CheckFoodTraitMatch(t, id, CheckOp))
                            return !Not; // Match found. If Not=false, true. If Not=true, false.
                    }
                    // None matched. Match = false.
                    return Not;
                }
            } catch (Exception ex) {
                Debug.LogError($"[Relocator] Error in FoodElement.IsMatch: {ex.Message}");
                return false;
            }
        }
        public override ConditionType GetConditionType() => ConditionType.FoodTraits;
        public override string GetUiLabel() {
            List<string> displayNames = [];
            foreach (var id in ElementIds) {
                // ID文字列からキー、演算子、値を解析 (例: "Strength>=10" -> key="Strength", op=">=", val=10)
                ConditionRegistry.ParseKeyOp(id, out string key, out string op, out int val);
                string suffix = op + val;

                // 表示名の解決
                string dName = key;
                if (EClass.sources.elements.alias.TryGetValue(key, out var source)) {
                    dName = source.GetName();
                }
                displayNames.Add(dName + suffix);
            }

            // リストの整形（10件以上は省略）
            string display = "";
            int limit = 10;
            if (displayNames.Count <= limit) {
                display = string.Join(", ", displayNames);
            } else {
                display = string.Join(", ", displayNames.Take(limit)) + " ... (+" + (displayNames.Count - limit) + ")";
            }

            string mode = IsAndMode ? " (AND)" : " (OR)";
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.FoodTraits) + mode + ": " + display;
        }
    }
    public class ConditionSourceContainer : BaseCondition {
        public string ContainerName = "";

        public override bool IsMatch(Thing t) {
            if (string.IsNullOrEmpty(ContainerName))
                return false;

            // Get the parent container of this item
            ICardParent parent = t.parent;
            if (parent == null)
                return Not; // No parent = PC's direct inventory

            // Check if parent is a Thing (container)
            if (parent is not Thing container)
                return Not;

            // Match container name (case-insensitive partial match)
            string containerNameLower = container.Name.ToLower();
            string searchLower = ContainerName.ToLower();
            bool match = containerNameLower.Contains(searchLower);

            return Not ? !match : match;
        }

        public override ConditionType GetConditionType() => ConditionType.SourceContainer;
        public override string GetUiLabel() {
            return GetNotPrefix() + RelocatorLang.GetText(RelocatorLang.LangKey.SourceContainer) + ": " + ContainerName;
        }
    }

    public class ConditionAddButton : BaseCondition {
        public override bool IsMatch(Thing t) => false;
        public override ConditionType GetConditionType() => ConditionType.AddButton;
        public override string GetUiLabel() => " + ";
    }

    public static class ConditionRegistry {
        public static List<(string Key, Func<JObject, ICondition> Loader)> Loaders = new();
        public static Dictionary<Type, Action<JObject, ICondition>> Savers = new();

        static ConditionRegistry() {
            // Register all handlers
            Register<ConditionCategory>("CategoryIds",
                jo => {
                    var ids = jo["CategoryIds"]?.ToObject<List<string>>();
                    var negs = jo["NegatedCategoryIds"]?.ToObject<List<string>>(); // Legacy support

                    if (ids != null && ids.Count > 0)
                        return new ConditionCategory { CategoryIds = new HashSet<string>(ids), Not = (bool?)jo["Not"] ?? false };

                    // [Legacy Support] Remove legacy negation check after 2026/04
                    if (negs != null && negs.Count > 0)
                        return new ConditionCategory { CategoryIds = new HashSet<string>(negs), Not = true };

                    return null;
                },
                (jo, c) => {
                    jo.Add("CategoryIds", JArray.FromObject(c.CategoryIds));
                    if (c.Not)
                        jo.Add("Not", true);
                }
            );

            // [Legacy Support] Remove this loader after 2026/04
            Loaders.Add(("NegatedCategoryIds", jo => {
                var negs = jo["NegatedCategoryIds"]?.ToObject<List<string>>();
                if (negs != null && negs.Count > 0)
                    return new ConditionCategory { CategoryIds = new HashSet<string>(negs), Not = true };
                return null;
            }
            ));

            Register<ConditionWeight>("Weight",
                jo => {
                    // [Legacy Support] Remove null check after 2026/04
                    if (IsJNull(jo["Weight"]))
                        return null;
                    return new ConditionWeight {
                        Value = (int)jo["Weight"],
                        Op = jo["WeightOp"]?.ToString() ?? ">=",
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotWeight"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("Weight", c.Value);
                    jo.Add("WeightOp", c.Op);
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionGenLvl>("GenLvl",
                jo => {
                    // [Legacy Support] Remove null check after 2026/04
                    if (IsJNull(jo["GenLvl"]))
                        return null;
                    return new ConditionGenLvl {
                        Value = (int)jo["GenLvl"],
                        Op = jo["GenLvlOp"]?.ToString() ?? ">=",
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotGenLvl"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("GenLvl", c.Value);
                    jo.Add("GenLvlOp", c.Op);
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionText>("Text",
                jo => {
                    // [Legacy Support] Remove null check after 2026/04
                    if (IsJNull(jo["Text"]))
                        return null;
                    string text = jo["Text"].ToString();
                    if (string.IsNullOrEmpty(text))
                        return null;
                    return new ConditionText {
                        Text = text,
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotText"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("Text", c.Text);
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionQuality>("Quality",
                jo => {
                    // [Legacy Support] Remove null check after 2026/04
                    if (IsJNull(jo["Quality"]))
                        return null;
                    string q = jo["Quality"].ToString();
                    ParseOp(q, out var op, out var val);
                    return new ConditionQuality {
                        Value = val,
                        Op = op,
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotQuality"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("Quality", c.Op + c.Value);
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionRarity>("Rarities",
                jo => {
                    var list = jo["Rarities"]?.ToObject<List<int>>();
                    if (list == null || list.Count == 0)
                        return null;
                    return new ConditionRarity {
                        Rarities = new HashSet<int>(list)
                    };
                },
                (jo, c) => {
                    jo.Add("Rarities", JArray.FromObject(c.Rarities));
                }
            );

            // Enchants / Runes (Ambiguous keys)
            Register<ConditionEnchantOr>("Enchants",
                jo => {
                    var list = jo["Enchants"]?.ToObject<List<string>>();
                    if (list == null || list.Count == 0)
                        return null;
                    return new ConditionEnchantOr {
                        Runes = list,
                        IsAndMode = true,
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotEnchant"] ?? false
                    };
                },
                (jo, c) => {
                    // No serialization needed for legacy key "Enchants" as we want to save in new format "EnchantsOr"
                }
            );

            Register<ConditionEnchantOr>("EnchantsOr",
                jo => {
                    var list = jo["EnchantsOr"]?.ToObject<List<string>>();
                    if (list == null || list.Count == 0)
                        return null;
                    return new ConditionEnchantOr {
                        Runes = list,
                        IsAndMode = (bool?)jo["IsAndMode"] ?? false,
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotEnchantsOr"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("EnchantsOr", JArray.FromObject(c.Runes));
                    jo.Add("IsAndMode", c.IsAndMode);
                    jo.Add("Negate", c.Not);
                }
            );

            // [Legacy Support] Remove this loader after 2026/04
            Loaders.Add(("Runes", jo => {
                var list = jo["Runes"]?.ToObject<List<string>>();
                if (list == null || list.Count == 0)
                    return null;
                // Migration: Runes also maps to ConditionEnchantOr (AND)
                return new ConditionEnchantOr { Runes = list, IsAndMode = true };
            }
            ));

            Register<ConditionBless>("BlessStates",
                jo => {
                    var list = jo["BlessStates"]?.ToObject<List<int>>();
                    if (list == null || list.Count == 0)
                        return null;
                    return new ConditionBless {
                        States = new HashSet<int>(list),
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotBless"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("BlessStates", JArray.FromObject(c.States));
                    jo.Add("Negate", c.Not);
                }
            );
            // [Legacy Support] Remove this loader after 2026/04
            Loaders.Add(("States", jo => {
                var list = jo["States"]?.ToObject<List<int>>();
                if (list == null || list.Count == 0)
                    return null;
                return new ConditionBless {
                    States = new HashSet<int>(list),
                    Not = (bool?)jo["NotBless"] ?? false
                };
            }
            ));

            Register<ConditionFoodElement>("FoodElementIds",
                jo => {
                    var list = jo["FoodElementIds"]?.ToObject<List<string>>();
                    if (list == null || list.Count == 0)
                        return null;
                    return new ConditionFoodElement {
                        ElementIds = new HashSet<string>(list),
                        IsAndMode = (bool?)jo["IsAndMode"] ?? false,
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotFoodElement"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("FoodElementIds", JArray.FromObject(c.ElementIds));
                    jo.Add("IsAndMode", c.IsAndMode);
                    jo.Add("Negate", c.Not);
                }
            );


            Register<ConditionMaterial>("MaterialIds",
                jo => {
                    var list = jo["MaterialIds"]?.ToObject<List<string>>();
                    if (list == null || list.Count == 0)
                        return null;
                    return new ConditionMaterial {
                        MaterialIds = new HashSet<string>(list),
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotMaterial"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("MaterialIds", JArray.FromObject(c.MaterialIds));
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionDna>("Dna",
                jo => {
                    if (IsJNull(jo["Dna"]))
                        return null;
                    return new ConditionDna {
                        Value = (int)jo["Dna"],
                        Op = jo["DnaOp"]?.ToString() ?? ">=",
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotDna"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("Dna", c.Value);
                    jo.Add("DnaOp", c.Op);
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionDnaContent>("DnaIds",
                jo => {
                    var list = jo["DnaIds"]?.ToObject<List<string>>();
                    if (list == null || list.Count == 0)
                        return null;
                    return new ConditionDnaContent {
                        DnaIds = new HashSet<string>(list),
                        IsAndMode = (bool?)jo["IsAndMode"] ?? false,
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotDnaContent"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("DnaIds", JArray.FromObject(c.DnaIds));
                    jo.Add("IsAndMode", c.IsAndMode);
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionStolen>("IsStolen",
                jo => {
                    if (IsJNull(jo["IsStolen"]))
                        return null;
                    return new ConditionStolen {
                        IsStolen = (bool)jo["IsStolen"],
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotStolen"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("IsStolen", c.IsStolen);
                    jo.Add("Negate", c.Not);
                }
            );

            Register<ConditionIdentified>("IsIdentified",
                jo => {
                    if (IsJNull(jo["IsIdentified"]))
                        return null;
                    return new ConditionIdentified {
                        IsIdentified = (bool)jo["IsIdentified"],
                        Not = (bool?)jo["Negate"] ?? (bool?)jo["NotIdentified"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("IsIdentified", c.IsIdentified);
                    if (c.Not)
                        jo.Add("Negate", true);
                }
            );

            Register<ConditionSourceContainer>("SourceContainerName",
                jo => {
                    if (IsJNull(jo["SourceContainerName"]))
                        return null;
                    string name = jo["SourceContainerName"].ToString();
                    if (string.IsNullOrEmpty(name))
                        return null;
                    return new ConditionSourceContainer {
                        ContainerName = name,
                        Not = (bool?)jo["Negate"] ?? false
                    };
                },
                (jo, c) => {
                    jo.Add("SourceContainerName", c.ContainerName);
                    if (c.Not)
                        jo.Add("Negate", true);
                }
            );
        }

        private static bool IsJNull(JToken t) => t == null || t.Type == JTokenType.Null;

        public static void Register<T>(string key, Func<JObject, T> loader, Action<JObject, T> saver) where T : ICondition {
            Loaders.Add((key, jo => loader(jo)));
            Savers[typeof(T)] = (jo, c) => saver(jo, (T)c);
        }

        public static void ParseOp(string raw, out string op, out int val) {
            op = ">=";
            val = 0;
            if (string.IsNullOrEmpty(raw))
                return;
            string[] ops = [">=", "<=", "!=", ">", "<", "="];
            foreach (var o in ops) {
                if (raw.Contains(o)) { // Contains check is safer for "Name>=10"
                    int idx = raw.IndexOf(o);
                    if (idx >= 0) { // Allow operator at start (e.g. ">=10")
                        op = o;
                        int.TryParse(raw.Substring(idx + o.Length), out val);
                        return;
                    }
                }
            }
            // Fallback if starts with op (e.g. ">=10" but should be handled by caller usually passing full string)
            // But here raw likely includes key?
            // My previous ParseOp (line 478 original) assumed raw was VALUE part only for Quality?
            // Wait, ConditionQuality loader passed ">=10".
            // ConditionEnchant logic needs to split Key and Op.
            // Let's make a ParseKeyOp helper or keep specific logic.
            int.TryParse(raw, out val);
        }

        public static void ParseKeyOp(string raw, out string key, out string op, out int val) {
            key = raw.StartsWith("@") ? raw.Substring(1) : raw;
            op = ">=";
            val = 1; // Default existence check

            if (string.IsNullOrEmpty(raw))
                return;

            string[] ops = [">=", "<=", "!=", ">", "<", "="];
            foreach (var o in ops) {
                int idx = raw.IndexOf(o);
                if (idx > 0) {
                    op = o;
                    key = raw.Substring(0, idx).Trim();
                    key = key.StartsWith("@") ? key.Substring(1) : key;
                    int.TryParse(raw.Substring(idx + o.Length), out val);
                    return;
                }
            }
        }

        private static int ResolveElementId(string key) {
            if (EClass.sources.elements.alias.ContainsKey(key))
                return EClass.sources.elements.alias[key].id;

            var match = EClass.sources.elements.rows.FirstOrDefault(e =>
                e != null && (
                e.GetName().Equals(key, StringComparison.OrdinalIgnoreCase) ||
                (e.alias != null && e.alias.Equals(key, StringComparison.OrdinalIgnoreCase)))
            );
            return match?.id ?? -1;
        }

        public static bool CheckEnchantMatch(Thing t, string rune, Func<int, string, int, bool> checkOp) {
            try {
                if (string.IsNullOrEmpty(rune))
                    return false;
                ParseKeyOp(rune, out string key, out string op, out int val);
                if (string.IsNullOrEmpty(key))
                    return false;

                int eleId = ResolveElementId(key);
                if (eleId != -1) {
                    int curVal = t.elements.Value(eleId);

                    // Implicit check: 0 value means enchant/trait is missing.
                    if (curVal == 0)
                        return false;

                    // Enchantment: Raw value comparison
                    return checkOp(curVal, op, val);
                }
            } catch (Exception ex) {
                Debug.LogError($"[Relocator] Error CheckEnchantMatch: {ex.Message}");
            }
            return false;
        }

        public static bool CheckFoodTraitMatch(Thing t, string rune, Func<int, string, int, bool> checkOp) {
            try {
                if (string.IsNullOrEmpty(rune))
                    return false;
                ParseKeyOp(rune, out string key, out string op, out int val);
                if (string.IsNullOrEmpty(key))
                    return false;

                int eleId = ResolveElementId(key);
                if (eleId != -1) {
                    int curVal = t.elements.Value(eleId);

                    // Implicit check: 0 value means trait is missing.
                    if (curVal == 0)
                        return false;

                    // Food Trait: Convert to Level (Lv.X)
                    // Logic based on Card.GetTextTrait: val/10 + (val<0 ? -1 : 1)
                    int lvl = curVal / 10;
                    curVal = (curVal < 0) ? (lvl - 1) : (lvl + 1);

                    return checkOp(curVal, op, val);
                }
            } catch (Exception ex) {
                Debug.LogError($"[Relocator] Error CheckFoodTraitMatch: {ex.Message}");
            }
            return false;
        }


        public static bool CheckOp(int current, string op, int target) {
            switch (op) {
            case ">=":
                return current >= target;
            case "<=":
                return current <= target;
            case "!=":
                return current != target;
            case ">":
                return current > target;
            case "<":
                return current < target;
            case "=":
                return current == target;
            default:
                return current >= target;
            }
        }
    }
}
