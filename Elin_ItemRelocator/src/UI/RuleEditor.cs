using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Elin_ItemRelocator {
    public static class RuleEditor {



        public static void ShowAddConditionMenu(RelocationRule rule, Action refresh) {
            // Mutual Exclusivity
            bool hasEnchantOr = rule.Conditions.Any(c => c is ConditionEnchantOr);

            var menu = RelocatorMenu.Create();

            menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Text), () => {
                Dialog.InputName("Enter Text/Tag", "", (c, text) => {
                    if (!c && !string.IsNullOrEmpty(text)) {
                        rule.Conditions.Add(new ConditionText { Text = text });
                        refresh();
                    }
                }, (Dialog.InputType)0);
            });

            // "Enchant" (Single Button for OR-list creation)
            // Logic: Creates a ConditionEnchantOr with picked items.
            // If user wants AND logic, they add *another* rule instance.
            menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Enchant), () => {
                RelocatorPickers.ShowEnchantMultiPicker(null, (selected, isAndMode) => {
                    if (selected.Count > 0) {
                        rule.Conditions.Add(new ConditionEnchantOr { Runes = selected, IsAndMode = isAndMode });
                        refresh();
                    }
                }, initialMode: true);
            });

            menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Category), () => {
                RelocatorPickers.ShowCategoryPicker([], (selectedIds) => {
                    if (selectedIds.Count > 0) {
                        rule.Conditions.Add(new ConditionCategory { CategoryIds = new HashSet<string>(selectedIds) });
                        refresh();
                    }
                });
            })
               .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Rarity), () => {
                   RelocatorPickers.ShowRarityPicker(null, (selected) => {
                       rule.Conditions.Add(new ConditionRarity { Rarities = new HashSet<int>(selected) });
                       refresh();
                   });
               })
               .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Enhancement), () => {
                   Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.EditEnhancement), "", (c, text) => {
                       if (!c && !string.IsNullOrEmpty(text)) {
                           string op = ">=";
                           int val = 0;
                           if (text.StartsWith(">=") || text.StartsWith("<=") || text.StartsWith("==") || text.StartsWith("!=")) { op = text.Substring(0, 2); int.TryParse(text.Substring(2), out val); } else if (text.StartsWith(">") || text.StartsWith("<") || text.StartsWith("=")) { op = text.Substring(0, 1); int.TryParse(text.Substring(1), out val); } else { int.TryParse(text, out val); }

                           rule.Conditions.Add(new ConditionQuality { Op = op, Value = val });
                           refresh();
                       }
                   }, (Dialog.InputType)0);
               })
                .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Weight), () => {
                    Dialog.InputName("Enter Weight (e.g. >=10)", "", (c, text) => {
                        if (!c && !string.IsNullOrEmpty(text)) {
                            if (char.IsDigit(text[0]))
                                text = ">=" + text;
                            string op = ">=";
                            int val = 0;
                            if (text.StartsWith(">=") || text.StartsWith("<=") || text.StartsWith("==") || text.StartsWith("!=")) { op = text.Substring(0, 2); int.TryParse(text.Substring(2), out val); } else if (text.StartsWith(">") || text.StartsWith("<") || text.StartsWith("=")) { op = text.Substring(0, 1); int.TryParse(text.Substring(1), out val); } else { int.TryParse(text, out val); }

                            rule.Conditions.Add(new ConditionWeight { Op = op, Value = val });
                            refresh();
                        }
                    }, (Dialog.InputType)0);
                })
                 .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Material), () => {
                     RelocatorPickers.ShowMaterialPicker(null, (aliases) => {
                         rule.Conditions.Add(new ConditionMaterial { MaterialIds = new HashSet<string>(aliases) });
                         refresh();
                     });
                 })
                 .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Bless), () => {
                     RelocatorPickers.ShowBlessPicker(null, (states) => {
                         rule.Conditions.Add(new ConditionBless { States = new HashSet<int>(states) });
                         refresh();
                     });
                 })
                 .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.Stolen), (child) => {
                     child
                          .AddButton("Yes (Is Stolen)", () => { rule.Conditions.Add(new ConditionStolen { IsStolen = true }); refresh(); })
                          .AddButton("No (Not Stolen)", () => { rule.Conditions.Add(new ConditionStolen { IsStolen = false }); refresh(); });
                 })
                 .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.Identified), (child) => {
                     child
                          .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.StateIdentified), () => { rule.Conditions.Add(new ConditionIdentified { IsIdentified = true }); refresh(); })
                          .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.StateUnidentified), () => { rule.Conditions.Add(new ConditionIdentified { IsIdentified = false }); refresh(); });
                 })
                 .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.GenLvl), () => {
                     Dialog.InputName("Enter GenLvl (e.g. >=10)", "", (c, text) => {
                         if (!c && !string.IsNullOrEmpty(text)) {
                             if (char.IsDigit(text[0]))
                                 text = ">=" + text;
                             string op = ">=";
                             int val = 0;
                             if (text.StartsWith(">=") || text.StartsWith("<=") || text.StartsWith("==") || text.StartsWith("!=")) { op = text.Substring(0, 2); int.TryParse(text.Substring(2), out val); } else if (text.StartsWith(">") || text.StartsWith("<") || text.StartsWith("=")) { op = text.Substring(0, 1); int.TryParse(text.Substring(1), out val); } else { int.TryParse(text, out val); }

                             rule.Conditions.Add(new ConditionGenLvl { Op = op, Value = val });
                             refresh();
                         }
                     }, (Dialog.InputType)0);
                 })
                 .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Dna), () => {
                     Dialog.InputName("Enter DNA Cost (e.g. >=100)", "", (c, text) => {
                         if (!c && !string.IsNullOrEmpty(text)) {
                             if (char.IsDigit(text[0]))
                                 text = ">=" + text;
                             string op = ">=";
                             int val = 0;
                             if (text.StartsWith(">=") || text.StartsWith("<=") || text.StartsWith("==") || text.StartsWith("!=")) { op = text.Substring(0, 2); int.TryParse(text.Substring(2), out val); } else if (text.StartsWith(">") || text.StartsWith("<") || text.StartsWith("=")) { op = text.Substring(0, 1); int.TryParse(text.Substring(1), out val); } else { int.TryParse(text, out val); }

                             rule.Conditions.Add(new ConditionDna { Op = op, Value = val });
                             refresh();
                         }
                     }, (Dialog.InputType)0);
                 })
                 .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.DnaContent), () => {
                     RelocatorPickers.ShowDnaContentPicker(null, (selectedList, isAndMode) => {
                         rule.Conditions.Add(new ConditionDnaContent { DnaIds = new HashSet<string>(selectedList), IsAndMode = isAndMode });
                         refresh();
                     });
                 })
                 .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.FoodTraits), () => {
                     RelocatorPickers.ShowFoodElementPicker(null, false, (selectedList, isAndMode) => {
                         if (selectedList.Count > 0) {
                             rule.Conditions.Add(new ConditionFoodElement { ElementIds = new HashSet<string>(selectedList), IsAndMode = isAndMode });
                             refresh();
                         }
                     });
                 })
                 .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SourceContainer), () => {
                     Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.SourceContainer), "", (c, text) => {
                         if (!c && !string.IsNullOrEmpty(text)) {
                             rule.Conditions.Add(new ConditionSourceContainer { ContainerName = text });
                             refresh();
                         }
                     }, (Dialog.InputType)0);
                 })
                 .Show();
        }

        public static void EditRuleCondition(FilterNode node, Action refresh) {
            var rule = node.Rule;
            var cond = node.ConditionRef;
            if (cond == null)
                return;

            Action showMenu = () => {
                var menu = RelocatorMenu.Create();
                menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Remove), () => {
                    rule.Conditions.Remove(cond);
                    refresh();
                });

                if (cond is ConditionText t) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => Dialog.InputName("Edit Text", t.Text, (c, val) => { if (!c) { t.Text = val; refresh(); } }, (Dialog.InputType)0));
                } else if (cond is ConditionWeight w) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => Dialog.InputName("Weight", w.Op + w.Value, (c, val) => {
                        if (!c && !string.IsNullOrEmpty(val)) {
                            string op = ">=";
                            int v = 0;
                            if (char.IsDigit(val[0]))
                                val = ">=" + val;
                            if (val.StartsWith(">=") || val.StartsWith("<=") || val.StartsWith("==") || val.StartsWith("!=")) { op = val.Substring(0, 2); int.TryParse(val.Substring(2), out v); } else if (val.StartsWith(">") || val.StartsWith("<") || val.StartsWith("=")) { op = val.Substring(0, 1); int.TryParse(val.Substring(1), out v); } else { int.TryParse(val, out v); }
                            w.Op = op;
                            w.Value = v;
                            refresh();
                        }
                    }, (Dialog.InputType)0));
                } else if (cond is ConditionGenLvl g) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => Dialog.InputName("Gen Level", g.Op + g.Value, (c, val) => {
                        if (!c && !string.IsNullOrEmpty(val)) {
                            string op = ">=";
                            int v = 0;
                            if (char.IsDigit(val[0]))
                                val = ">=" + val;
                            if (val.StartsWith(">=") || val.StartsWith("<=") || val.StartsWith("==") || val.StartsWith("!=")) { op = val.Substring(0, 2); int.TryParse(val.Substring(2), out v); } else if (val.StartsWith(">") || val.StartsWith("<") || val.StartsWith("=")) { op = val.Substring(0, 1); int.TryParse(val.Substring(1), out v); } else { int.TryParse(val, out v); }
                            g.Op = op;
                            g.Value = v;
                            refresh();
                        }
                    }, (Dialog.InputType)0));
                } else if (cond is ConditionDna d) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => Dialog.InputName("DNA Cost", d.Op + d.Value, (c, val) => {
                        if (!c && !string.IsNullOrEmpty(val)) {
                            string op = ">=";
                            int v = 0;
                            if (char.IsDigit(val[0]))
                                val = ">=" + val;
                            if (val.StartsWith(">=") || val.StartsWith("<=") || val.StartsWith("==") || val.StartsWith("!=")) { op = val.Substring(0, 2); int.TryParse(val.Substring(2), out v); } else if (val.StartsWith(">") || val.StartsWith("<") || val.StartsWith("=")) { op = val.Substring(0, 1); int.TryParse(val.Substring(1), out v); } else { int.TryParse(val, out v); }
                            d.Op = op;
                            d.Value = v;
                            refresh();
                        }
                    }, (Dialog.InputType)0));
                } else if (cond is ConditionCategory c) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => RelocatorPickers.ShowCategoryPicker(c.CategoryIds.ToList(), (ids) => {
                        c.CategoryIds = new HashSet<string>(ids);
                        refresh();
                    }));
                } else if (cond is ConditionRarity r) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => RelocatorPickers.ShowRarityPicker(r.Rarities.ToList(), (ids) => {
                        r.Rarities = new HashSet<int>(ids);
                        refresh();
                    }));
                } else if (cond is ConditionQuality q) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => Dialog.InputName("Quality", q.Op + q.Value, (c, val) => {
                        if (!c && !string.IsNullOrEmpty(val)) {
                            string op = ">=";
                            int v = 0;
                            if (char.IsDigit(val[0]))
                                val = ">=" + val;
                            if (val.StartsWith(">=")) { op = ">="; int.TryParse(val.Substring(2), out v); } else { int.TryParse(val, out v); }
                            q.Op = op;
                            q.Value = v;
                            refresh();
                        }
                    }, (Dialog.InputType)0));
                } else if (cond is ConditionDnaContent dc) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => RelocatorPickers.ShowDnaContentPicker(dc.DnaIds.ToList(), (ids, isAndMode) => {
                        dc.DnaIds = new HashSet<string>(ids);
                        dc.IsAndMode = isAndMode;
                        refresh();
                    }, initialMode: dc.IsAndMode));
                } else if (cond is ConditionMaterial m) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => RelocatorPickers.ShowMaterialPicker(m.MaterialIds.ToList(), (ids) => {
                        m.MaterialIds = new HashSet<string>(ids);
                        refresh();
                    }));
                } else if (cond is ConditionBless b) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => RelocatorPickers.ShowBlessPicker(b.States.ToList(), (ids) => {
                        b.States = new HashSet<int>(ids);
                        refresh();
                    }));
                } else if (cond is ConditionStolen s) {
                    menu.AddButton("Set 'Is Stolen'", () => { s.IsStolen = true; refresh(); });
                    menu.AddButton("Set 'Not Stolen'", () => { s.IsStolen = false; refresh(); });
                } else if (cond is ConditionIdentified i) {
                    menu.AddButton("Set 'Identified'", () => { i.IsIdentified = true; refresh(); });
                    menu.AddButton("Set 'Unidentified'", () => { i.IsIdentified = false; refresh(); });

                } else if (cond is ConditionEnchantOr eo) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => {
                        RelocatorPickers.ShowEnchantMultiPicker(eo.Runes.ToList(), (selected, isAndMode) => {
                            eo.Runes = selected;
                            eo.IsAndMode = isAndMode;
                            refresh();
                        }, initialMode: eo.IsAndMode);
                    });

                } else if (cond is ConditionFoodElement fe) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => {
                        RelocatorPickers.ShowFoodElementPicker(fe.ElementIds.ToList(), fe.IsAndMode, (selected, isAndMode) => {
                            fe.ElementIds = new HashSet<string>(selected);
                            fe.IsAndMode = isAndMode;
                            refresh();
                        });
                    });
                } else if (cond is ConditionSourceContainer sc) {
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Edit), () => {
                        Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.SourceContainer), sc.ContainerName, (c, val) => {
                            if (!c && !string.IsNullOrEmpty(val)) {
                                sc.ContainerName = val;
                                refresh();
                            }
                        }, (Dialog.InputType)0);
                    });
                }
                menu.Show();
            };

            showMenu();
        }
    }
}
