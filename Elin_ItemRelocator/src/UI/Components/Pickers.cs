using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_ItemRelocator {
    public static class RelocatorPickers {


        public static void ShowCategoryPicker(List<string> initialSelection, Action<List<string>> onConfirm) {
            HashSet<string> selected = new HashSet<string>();
            if (initialSelection != null)
                foreach (var s in initialSelection)
                    selected.Add(s);

            var allCats = EClass.sources.categories.rows;
            var roots = allCats.Where(r => r.parent == null).OrderBy(r => r.id).ToList();

            var tree = RelocatorTree<SourceCategory.Row>.Create();

            tree.SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.Category));
            tree.SetRoots(roots);

            tree.SetChildren((SourceCategory.Row cat) => allCats.Where(r => r.parent == cat).OrderBy(r => r.id));
            tree.SetText((SourceCategory.Row cat) => cat.GetName());

            Func<SourceCategory.Row, bool> isParentSelected = null;
            isParentSelected = (SourceCategory.Row c) => {
                if (c.parent == null)
                    return false;
                if (selected.Contains(c.parent.id))
                    return true;
                return isParentSelected(c.parent);
            };

            tree.SetIsSelected((SourceCategory.Row cat) => selected.Contains(cat.id));
            tree.SetIsDisabled((SourceCategory.Row cat) => isParentSelected(cat));
            tree.SetOnSelect((SourceCategory.Row cat) => {
                if (selected.Contains(cat.id))
                    selected.Remove(cat.id);
                else
                    selected.Add(cat.id);
            });

            tree.AddBottomButton("[ OK ]", () => {
                onConfirm(selected.ToList());
                tree.Close();
            }, new Color(0.3f, 0.5f, 0));

            tree.Show();
        }



        public static void ShowPresetPicker(Action<string> onSelect) {
            var layer = EClass.ui.AddLayer<LayerList>();
            layer.SetSize(400, 600);
            layer.windows[0].SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.Presets));
            Action refresh = null;
            refresh = () => {
                if (layer == null || layer.gameObject == null)
                    return;
                var presets = RelocatorManager.Instance.GetPresetList();
                try {
                    layer.SetList2<string>(
                        presets,
                        (n) => n,
                        (n, b) => { onSelect(n); layer.Close(); },
                        (n, b) => {
                            b.SetMainText(n);
                            Transform t = b.transform.Find("BtnPresetMenu");
                            Button btnMenu = null;
                            if (t == null) {
                                var go = new GameObject("BtnPresetMenu");
                                go.transform.SetParent(b.transform, false);
                                var sourceText = b.GetComponentInChildren<Text>();
                                var txt = go.AddComponent<Text>();
                                txt.text = "[▼More]";
                                if (sourceText != null) { txt.font = sourceText.font; txt.fontSize = sourceText.fontSize; } else { txt.font = SkinManager.Instance.fontSet.ui.source.font; txt.fontSize = 14; } // Updated Font
                                txt.alignment = TextAnchor.MiddleCenter;
                                txt.color = Color.white;
                                var rt = go.GetComponent<RectTransform>();
                                rt.anchorMin = new Vector2(1, 0);
                                rt.anchorMax = new Vector2(1, 1);
                                rt.pivot = new Vector2(1, 0.5f);
                                rt.sizeDelta = new Vector2(80, 0);
                                rt.anchoredPosition = new Vector2(-10, 0);
                                btnMenu = go.AddComponent<Button>();
                                btnMenu.targetGraphic = txt;
                                var colors = btnMenu.colors;
                                colors.normalColor = new Color(0.45f, 0.55f, 0.7f, 1f);
                                btnMenu.colors = colors;
                            } else { btnMenu = t.GetComponent<Button>(); }
                            btnMenu.onClick.RemoveAllListeners();
                            btnMenu.onClick.AddListener(() => {
                                RelocatorMenu.Create()
                                    .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Rename), () => {
                                        Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_RenamePrompt), n, (cancel, text) => {
                                            if (!cancel && !string.IsNullOrEmpty(text) && text != n) {
                                                RelocatorManager.Instance.RenamePreset(n, text);
                                                if (layer != null && layer.gameObject != null)
                                                    refresh();
                                            }
                                        });
                                    })
                                    .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Delete), () => {
                                        RelocatorManager.Instance.DeletePreset(n);
                                        if (layer != null && layer.gameObject != null)
                                            refresh();
                                    })
                                    .Show();
                            });
                        }
                    );
                } catch (Exception ex) { Debug.LogError(ex); }
            };
            refresh();
        }

        public static void ShowMaterialPicker(List<string> initialSelection, Action<List<string>> onConfirm) {
            HashSet<string> selected = new HashSet<string>();
            if (initialSelection != null)
                foreach (var s in initialSelection)
                    selected.Add(s);

            // Access materials
            var sources = EClass.sources.materials.rows.Where(r => !string.IsNullOrEmpty(r.alias)).OrderBy(r => r.id).ToList();

            var tree = RelocatorTree<SourceMaterial.Row>.Create();
            tree.SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.Material));
            tree.SetRoots(sources);

            tree.SetText((SourceMaterial.Row m) => m.GetName() + " (" + m.alias + ")");
            tree.SetIsSelected((SourceMaterial.Row m) => {
                return selected.Contains(m.alias) || selected.Contains(m.id.ToString());
            });

            tree.SetOnSelect((SourceMaterial.Row m) => {
                string key = m.alias; // Use alias as primary key
                if (selected.Contains(key))
                    selected.Remove(key);
                else
                    selected.Add(key);
            });

            tree.AddBottomButton("[ OK ]", () => {
                onConfirm(selected.ToList());
                tree.Close();
            }, new Color(0.3f, 0.5f, 0));

            tree.Show();
        }

        public static void ShowBlessPicker(List<int> initialSelection, Action<List<int>> onConfirm) {
            HashSet<int> selected = new HashSet<int>();
            if (initialSelection != null)
                foreach (var s in initialSelection)
                    selected.Add(s);

            // Hardcoded sources
            var sources = new List<int> { 0, 1, -1, -2 }; // Normal, Blessed, Cursed, Doomed

            var tree = RelocatorTree<int>.Create();
            tree.SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.Bless));
            tree.SetRoots(sources);

            tree.SetText((int b) => {
                if (b == 1)
                    return RelocatorLang.GetText(RelocatorLang.LangKey.StateBlessed);
                if (b == -1)
                    return RelocatorLang.GetText(RelocatorLang.LangKey.StateCursed);
                if (b == 0)
                    return RelocatorLang.GetText(RelocatorLang.LangKey.StateNormal);
                if (b == -2)
                    return RelocatorLang.GetText(RelocatorLang.LangKey.StateDoomed);
                return b.ToString();
            });

            tree.SetIsSelected((int b) => selected.Contains(b));

            tree.SetOnSelect((int b) => {
                if (selected.Contains(b))
                    selected.Remove(b);
                else
                    selected.Add(b);
            });

            tree.AddBottomButton("[ OK ]", () => {
                onConfirm(selected.ToList());
                tree.Close();
            }, new Color(0.3f, 0.5f, 0));

            tree.Show();
        }

        public static void ShowRarityPicker(List<int> initialSelection, Action<List<int>> onConfirm) {
            HashSet<int> selected = new HashSet<int>();
            if (initialSelection != null)
                foreach (var s in initialSelection)
                    selected.Add(s);

            // Access Rarity List (Lang "quality"): index 0 = -1 (Crude), 1 = 0 (Normal)...
            var updateList = Lang.GetList("quality");
            List<int> sources = new List<int>();
            for (int i = 0; i < updateList.Length; i++) {
                sources.Add(i - 1);
            }

            var tree = RelocatorTree<int>.Create();
            tree.SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.Rarity));
            tree.SetRoots(sources);

            tree.SetText((int r) => {
                int index = r + 1;
                if (index >= 0 && index < updateList.Length)
                    return updateList[index];
                return r.ToString();
            });

            tree.SetIsSelected((int r) => selected.Contains(r));

            tree.SetOnSelect((int r) => {
                if (selected.Contains(r))
                    selected.Remove(r);
                else
                    selected.Add(r);
            });

            tree.AddBottomButton("[ OK ]", () => {
                onConfirm(selected.ToList());
                tree.Close();
            }, new Color(0.3f, 0.5f, 0));

            tree.Show();
        }

        // General Element Picker (for DNA etc)
        // DNA Content Picker (Tree-based)
        public static void ShowDnaContentPicker(List<string> initialSelection, Action<List<string>, bool> onConfirm, bool initialMode = false) {
            HashSet<string> selected = new HashSet<string>();
            Dictionary<string, string> initialValues = new Dictionary<string, string>();

            if (initialSelection != null)
                foreach (var s in initialSelection) {
                    ConditionRegistry.ParseKeyOp(s, out string id, out string op, out int val);
                    selected.Add(id);
                    initialValues[id] = op + val;
                }

            // Roots: Categories
            var categories = new List<string> { "attribute", "skill", "feat", "ability", "slot" };

            // Map: Category -> List<SourceElement.Row>
            var map = new Dictionary<string, List<SourceElement.Row>>();
            foreach (var cat in categories)
                map[cat] = new List<SourceElement.Row>();

            foreach (var row in EClass.sources.elements.rows) {
                if (string.IsNullOrEmpty(row.alias))
                    continue;
                if (row.tag.Contains("hidden"))
                    continue;
                if (map.ContainsKey(row.category)) {
                    map[row.category].Add(row);
                }
            }

            // Sort
            foreach (var list in map.Values)
                list.Sort((a, b) => a.id - b.id);

            var tree = RelocatorTree<object>.Create();
            tree.SetCaption("Select DNA Content");
            tree.SetRoots(categories.Cast<object>());
            tree.DefaultValue = ">0";

            // Enable Mode Toggle
            tree.ShowModeToggle = true;
            tree.IsAndMode = initialMode;

            // Set Initial Values
            foreach (var list in map.Values) {
                foreach (var row in list) {
                    if (initialValues.ContainsKey(row.alias)) {
                        tree.SetValue(row, initialValues[row.alias]);
                    }
                }
            }

            tree.SetChildren((object obj) => {
                if (obj is string cat && map.ContainsKey(cat)) {
                    return map[cat].Cast<object>();
                }
                return null;
            });

            tree.SetText((object obj) => {
                if (obj is string cat) {
                    if (Enum.TryParse("Cat_" + char.ToUpper(cat[0]) + cat.Substring(1), out RelocatorLang.LangKey key))
                        return RelocatorLang.GetText(key);
                    return cat.ToUpper();
                }
                if (obj is SourceElement.Row row)
                    return row.GetName();
                return obj.ToString();
            });

            tree.SetIsSelected((object obj) => {
                if (obj is SourceElement.Row row)
                    return selected.Contains(row.alias);
                return false;
            });

            tree.SetOnSelect((object obj) => {
                if (obj is SourceElement.Row row) {
                    string key = row.alias;
                    if (selected.Contains(key))
                        selected.Remove(key);
                    else
                        selected.Add(key);
                }
            });

            tree.AddBottomButton("[ OK ]", () => {
                List<string> result = new List<string>();

                foreach (var catList in map.Values) {
                    foreach (var row in catList) {
                        if (selected.Contains(row.alias)) {
                            string val = tree.GetValue(row);
                            // Let's use manual check as before to be safe about InputField content
                            string[] validOps = [">=", "<=", "!=", ">", "<", "="];
                            bool valid = false;
                            foreach (var op in validOps) {
                                if (val.StartsWith(op) && int.TryParse(val.Substring(op.Length), out _)) {
                                    valid = true;
                                    break;
                                }
                            }

                            if (!valid) {
                                EClass.ui.Say("Invalid format for " + row.GetName() + ": " + val);
                                return;
                            }
                            result.Add(row.alias + val);
                        }
                    }
                }
                onConfirm(result, tree.IsAndMode);
                tree.Close();
            }, new Color(0.3f, 0.5f, 0));

            tree.ExpandSelected();
            tree.Show();
        }

        public static void ShowEnchantMultiPicker(List<string> initialSelection, Action<List<string>, bool> onConfirm, bool initialMode = true) {
            HashSet<string> selected = new HashSet<string>();
            Dictionary<string, string> initialValues = new Dictionary<string, string>();

            if (initialSelection != null)
                foreach (var s in initialSelection) {
                    ConditionRegistry.ParseKeyOp(s, out string id, out string op, out int val);
                    selected.Add(id);
                    initialValues[id] = op + val;
                }

            // Gather Valid Enchants
            var map = new Dictionary<string, List<SourceElement.Row>>();
            // Initialize known categories to ensure order
            string[] cats = ["attribute", "resist", "skill", "enchant", "ability"];
            foreach (var c in cats)
                map[c] = new List<SourceElement.Row>();
            map["other"] = new List<SourceElement.Row>();

            foreach (var row in EClass.sources.elements.rows) {
                if (string.IsNullOrEmpty(row.alias))
                    continue;

                // Match game logic: IsEncAppliable requires encSlot to be non-empty
                // This single check covers spells, traits, and other non-enchant elements.
                if (string.IsNullOrEmpty(row.encSlot))
                    continue;

                string cat = row.category;
                if (!map.ContainsKey(cat))
                    cat = "other";

                map[cat].Add(row);
            }

            // Remove empty categories
            var activeCats = map.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList();

            // Sort items
            foreach (var list in map.Values)
                list.Sort((a, b) => a.id - b.id);

            var tree = RelocatorTree<object>.Create();
            tree.SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.SelectEnchant));
            tree.SetRoots(activeCats.Cast<object>());
            tree.DefaultValue = ">0"; // Default for new selections

            // Enable Mode Toggle
            tree.ShowModeToggle = true;
            tree.IsAndMode = initialMode;

            // Set Initial Values
            foreach (var list in map.Values) {
                foreach (var row in list) {
                    if (initialValues.ContainsKey(row.alias)) {
                        tree.SetValue(row, initialValues[row.alias]);
                    }
                }
            }

            tree.SetChildren((object obj) => {
                if (obj is string cat && map.ContainsKey(cat))
                    return map[cat].Cast<object>();
                return null;
            });

            tree.SetText((object obj) => {
                if (obj is string cat) {
                    return char.ToUpper(cat[0]) + cat.Substring(1);
                }
                if (obj is SourceElement.Row row)
                    return row.GetName() + " (" + row.alias + ")";
                return obj.ToString();
            });

            tree.SetIsSelected((object obj) => {
                if (obj is SourceElement.Row row)
                    return selected.Contains(row.alias);
                return false;
            });

            tree.SetOnSelect((object obj) => {
                if (obj is SourceElement.Row row) {
                    string key = row.alias;
                    if (selected.Contains(key))
                        selected.Remove(key);
                    else
                        selected.Add(key);
                }
            });

            tree.AddBottomButton("[ OK ]", () => {
                List<string> result = new List<string>();
                string[] validOps = [">=", "<=", "!=", ">", "<", "="];

                // Validate and Build
                foreach (var catList in map.Values) {
                    foreach (var row in catList) {
                        if (selected.Contains(row.alias)) {
                            string val = tree.GetValue(row);
                            // Validate
                            bool valid = false;
                            foreach (var op in validOps) {
                                if (val.StartsWith(op)) {
                                    if (int.TryParse(val.Substring(op.Length), out _)) {
                                        valid = true;
                                        break;
                                    }
                                }
                            }
                            if (!valid) {
                                EClass.ui.Say("Invalid format for " + row.GetName() + ": " + val);
                                return; // Do not close
                            }
                            result.Add(row.alias + val);
                        }
                    }
                }

                onConfirm(result, tree.IsAndMode);
                tree.Close();
            }, new Color(0.3f, 0.5f, 0));

            tree.ExpandSelected();
            tree.Show();
        }


        public static void ShowFoodElementPicker(List<string> initialSelection, bool initialAndMode, Action<List<string>, bool> onConfirm) {
            HashSet<string> selected = new HashSet<string>();
            if (initialSelection != null)
                foreach (var s in initialSelection)
                    selected.Add(s);

            // Gather elements with foodEffect
            var map = new Dictionary<string, List<SourceElement.Row>>();
            string[] cats = ["attribute", "resist", "skill", "enchant", "ability"];
            foreach (var c in cats)
                map[c] = new List<SourceElement.Row>();
            map["other"] = new List<SourceElement.Row>();

            foreach (var row in EClass.sources.elements.rows) {
                if (string.IsNullOrEmpty(row.alias))
                    continue;

                // Match CraftUtil.IsValidTrait logic for Food
                // Check noInherit
                if (row.tag != null && Array.IndexOf(row.tag, "noInherit") != -1)
                    continue;

                bool isFoodTrait = (row.foodEffect != null && row.foodEffect.Length > 0);
                bool isTrait = (row.tag != null && Array.IndexOf(row.tag, "trait") != -1);

                // Previously hardcoded encSleep exception, but now relying on generic IsTrait check.
                // If encSleep is a trait (which it should be if it appears on food), it will pass.
                // However, user specifically mentioned sleepiness, so we trust IsTrait covers it or it has foodEffect.
                // If encSleep has neither, it wouldn't appear on food via MixIngredients.

                if (!isFoodTrait && !isTrait)
                    continue;

                string cat = row.category;
                if (!map.ContainsKey(cat))
                    cat = "other";

                map[cat].Add(row);
            }

            // Remove empty categories
            var activeCats = map.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList();

            // Sort items
            foreach (var list in map.Values)
                list.Sort((a, b) => a.id - b.id);

            var tree = RelocatorTree<object>.Create();
            tree.SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.FoodTraits));
            tree.SetRoots(activeCats.Cast<object>());
            tree.DefaultValue = ">0"; // Default

            // Add And/Or Toggle
            tree.ShowModeToggle = true;
            tree.IsAndMode = initialAndMode;

            // Set Initial Values (Parse alias>=10 etc)
            Dictionary<string, string> initialValues = new Dictionary<string, string>();
            if (initialSelection != null) {
                foreach (string s in initialSelection) {
                    ConditionRegistry.ParseKeyOp(s, out string key, out string op, out int val);
                    initialValues[key] = op + val;
                    selected.Add(key); // Pre-populate selected set
                }
            }

            foreach (var list in map.Values) {
                foreach (var row in list) {
                    if (initialValues.ContainsKey(row.alias)) {
                        tree.SetValue(row, initialValues[row.alias]);
                    }
                }
            }

            tree.SetChildren((object obj) => {
                if (obj is string cat && map.ContainsKey(cat))
                    return map[cat].Cast<object>();
                return null;
            });

            tree.SetText((object obj) => {
                if (obj is string cat) {
                    return char.ToUpper(cat[0]) + cat.Substring(1);
                }
                if (obj is SourceElement.Row row)
                    return row.GetName() + " (" + row.alias + ")";
                return obj.ToString();
            });

            // Selection is based on toggle
            tree.SetIsSelected((object obj) => {
                if (obj is SourceElement.Row row)
                    return selected.Contains(row.alias);
                return false;
            });

            tree.SetOnSelect((object obj) => {
                if (obj is SourceElement.Row row) {
                    string key = row.alias;
                    if (selected.Contains(key))
                        selected.Remove(key);
                    else
                        selected.Add(key);
                }
            });

            tree.AddBottomButton("[ OK ]", () => {
                List<string> result = new List<string>();
                string[] validOps = [">=", "<=", "!=", ">", "<", "="];

                foreach (var catList in map.Values) {
                    foreach (var row in catList) {
                        if (selected.Contains(row.alias)) {
                            string val = tree.GetValue(row);

                            // Validation
                            bool valid = false;
                            foreach (var op in validOps) {
                                if (val.StartsWith(op)) {
                                    if (int.TryParse(val.Substring(op.Length), out _)) {
                                        valid = true;
                                        break;
                                    }
                                }
                            }
                            if (!valid) {
                                // Fallback to >=1 if invalid? Or strictly enforce?
                                // Enchant picker strictly enforces. Let's do same.
                                EClass.ui.Say("Invalid format for " + row.GetName() + ": " + val);
                                return;
                            }
                            result.Add(row.alias + val);
                        }
                    }
                }
                onConfirm(result, tree.IsAndMode);
                tree.Close();
            }, new Color(0.3f, 0.5f, 0));

            tree.ExpandSelected();
            tree.Show();
        }
    }
}
