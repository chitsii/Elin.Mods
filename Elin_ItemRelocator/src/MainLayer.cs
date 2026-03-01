using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Elin_ItemRelocator {
    // Helper component to make sidebar follow main window
    public class RelocatorSidebarSync : MonoBehaviour {
        public RectTransform target;
        public RectTransform sidebar;
        public Vector2 offset;

        private void LateUpdate() {
            if (target == null || sidebar == null)
                return;
            sidebar.anchoredPosition = target.anchoredPosition + offset;
        }
    }
    public class LayerItemRelocator : ELayer {
        // Wrappers Moved to UI/FilterNode.cs
        // FilterNode and ConditionType are now external

        // ====================================================================
        // Fields
        // ====================================================================
        private static RelocatorTable<Thing> previewTable;
        private static RelocatorAccordion<FilterNode> mainAccordion;
        private static RelocatorSidebar presetSidebar;
        private static Thing currentContainer;

        // ====================================================================
        // Main Entry Point
        // ====================================================================
        public static void Open(Thing container) {
            currentContainer = container;

            // Setup Accordion Logic FIRST
            mainAccordion = RelocatorAccordion<FilterNode>.Create();
            var profile = RelocatorManager.Instance.GetProfile(container);
            // Build Cache on Open
            RelocatorManager.Instance.BuildCache(profile, container);

            // UX: Default Empty Rule for new/empty profiles
            if (profile.Rules.Count == 0) {
                profile.Rules.Add(new RelocationRule { Name = RelocatorLang.GetText(RelocatorLang.LangKey.NewRuleName) });
            }

            Action refresh = null;
            // Define Refresh Logic
            refresh = () => {
                // Rebuild Data
                List<FilterNode> roots = new List<FilterNode>();


                if (profile.Rules is not null) {
                    foreach (var r in profile.Rules) {
                        FilterNode node = new() { Rule = r, CondType = ConditionType.None };
                        roots.Add(node);
                        // Default Open: Ensure rule is expanded regardless of state
                        // But we want persistence. Since we override Equals based on Rule,
                        // previously expanded rules remain expanded.
                        // But for FIRST load or New Rule, we should expand it.
                        // 'Expand' method relies on 'Equals'.
                        // If we call Expand(node) it will add it to the set.
                        // Persistence is handled by SetRoots not clearing 'expanded'.
                        // WE WANT TO FORCE EXPAND ALWAYS according to user request?
                        // "Default to open" implies new ones open.
                        // User said: "Adding condition closes rule is bad". Override Equals fixes that.
                        // "Rule default open" -> Let's force expand all on refresh? No, that prevents closing.
                        // But for UX, let's Expand if not already tracked (aka new).
                        // However, since we recreate nodes, we can't easily distinguish 'new' from 'closed'.
                        // Use 'mainAccordion.Expand(node)' which checks 'contains'.
                        mainAccordion.Expand(node);
                    }
                }
                mainAccordion.SetRoots(roots);
                mainAccordion.Refresh();

                RefreshPreview(null, container, profile);
            };

            // Configure Accordion
            mainAccordion.SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.Title));

            mainAccordion.SetOnBuildRow((trans, node) => {
                // Settings row logic removed
            });

            mainAccordion.SetChildren((node) => {
                if (node.IsRule) {
                    var list = new List<FilterNode>();
                    var r = node.Rule;

                    foreach (var cond in r.Conditions) {
                        var fNode = new FilterNode() { Rule = r, ConditionRef = cond };
                        fNode.CondType = cond.GetConditionType();
                        fNode.DisplayText = cond.GetUiLabel();
                        list.Add(fNode);
                    }

                    // Add Button
                    list.Add(new() { Rule = r, CondType = ConditionType.AddButton, DisplayText = " + " });
                    return list;
                }
                return null;
            });

            mainAccordion.SetText((node) => {
                if (node.IsRule) {
                    string status = node.Rule.Enabled ? "" : " (Disabled)";
                    return node.Rule.Name + " (" + RulePresenter.GetConditionList(node.Rule).Count + ")" + status;
                }
                return node.DisplayText;
            });

            mainAccordion.SetGetBackgroundColor((node) => {
                if (node.IsRule) {
                    return new(0.85f, 0.82f, 0.75f, 1f);
                }
                if (node.IsAddButton) {
                    return Color.clear;
                }
                return Color.clear;
            });

            mainAccordion.SetOnSelect((node) => {
                if (node.IsAddButton) {
                    RuleEditor.ShowAddConditionMenu(node.Rule, refresh);
                } else if (node.IsCondition) {
                    RuleEditor.EditRuleCondition(node, refresh);
                }
            });

            mainAccordion.SetOnRightClick((node) => {
                if (node.IsRule) {
                    var menu = RelocatorMenu.Create();
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Rename), () => {
                        Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.NewRuleName), node.Rule.Name, (c, text) => {
                            if (!c && !string.IsNullOrEmpty(text)) { node.Rule.Name = text; refresh(); }
                        }, (Dialog.InputType)0);
                    })
                        .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Remove), () => {
                            Dialog.YesNo(RelocatorLang.GetText(RelocatorLang.LangKey.Delete) + "?", () => { profile.Rules.Remove(node.Rule); refresh(); });
                        })
                        .AddButton(node.Rule.Enabled ? RelocatorLang.GetText(RelocatorLang.LangKey.Disable) : RelocatorLang.GetText(RelocatorLang.LangKey.Enable), () => { node.Rule.Enabled = !node.Rule.Enabled; refresh(); })
                        .Show();
                }
            });



            mainAccordion.SetOnBuildRowExtra((node, rowGO) => {
                if (node.IsCondition) {
                    var cond = node.ConditionRef;

                    // Negation Button
                    bool isNegated = false;
                    Action toggleNegation = null;

                    if (cond is BaseCondition baseCond) {
                        // Most conditions support negation via BaseCondition.Not
                        // Exceptions: Maybe 'Enchant' if we didn't implement Not for it (BaseCondition has it, but logic needs support)
                        // ConditionEnchant overrides IsMatch and uses Not.

                        // Check implies logic support
                        if (cond is ConditionAddButton) { } // Skip
                        else {
                            isNegated = baseCond.Not;
                            toggleNegation = () => { baseCond.Not = !baseCond.Not; refresh(); };
                        }
                    }

                    if (toggleNegation != null) {
                        var btnNeg = CreateTinyButton(rowGO.transform, RelocatorLang.GetText(RelocatorLang.LangKey.Not), toggleNegation);
                        var txtNeg = btnNeg.GetComponentInChildren<Text>();
                        var imgNeg = btnNeg.GetComponent<Image>();
                        if (isNegated) {
                            if (txtNeg)
                                txtNeg.color = Color.red;
                            if (imgNeg)
                                imgNeg.color = new Color(1f, 0.8f, 0.8f);
                        } else {
                            if (txtNeg)
                                txtNeg.color = SkinManager.CurrentColors.textDefault;
                            if (imgNeg)
                                imgNeg.color = new Color(0.9f, 0.9f, 0.9f);
                        }
                    }

                    // Remove Button
                    var btnRemove = CreateTinyButton(rowGO.transform, "x", () => {
                        Dialog.YesNo(RelocatorLang.GetText(RelocatorLang.LangKey.Delete) + "?", () => {
                            // Polymorphic Removal is simple: Remove the object from the list.
                            if (node.Rule.Conditions.Contains(cond)) {
                                node.Rule.Conditions.Remove(cond);
                            }
                            refresh();
                        });
                    });
                }
            });

            // 1. Setup Sidebar
            presetSidebar = RelocatorSidebar.Create()
                .SetCaption(RelocatorLang.GetText(RelocatorLang.LangKey.Presets))
                .SetOnSelect((name) => {
                    var loaded = RelocatorManager.Instance.LoadPreset(name);
                    if (loaded is not null) {
                        profile.Rules = loaded.Rules;
                        profile.Scope = loaded.Scope;
                        profile.SortMode = loaded.SortMode;
                        refresh();
                        Msg.Say(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_Loaded));
                    }
                })
                .SetOnRightClick((name) => {
                    RelocatorMenu.Create()
                        .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Rename), () => {
                            Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_RenamePrompt), name, (c, text) => {
                                if (!c && !string.IsNullOrEmpty(text)) {
                                    RelocatorManager.Instance.RenamePreset(name, text);
                                    refresh();
                                }
                            }, (Dialog.InputType)0);
                        })
                        .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Overwrite), () => {
                            Dialog.YesNo(RelocatorLang.GetText(RelocatorLang.LangKey.Overwrite) + " " + name + "?", () => {
                                RelocatorManager.Instance.SavePreset(name, profile);
                                refresh();
                            });
                        })
                        .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Delete), () => {
                            Dialog.YesNo(RelocatorLang.GetText(RelocatorLang.LangKey.Delete) + "?", () => {
                                RelocatorManager.Instance.DeletePreset(name);
                                refresh();
                            });
                        })
                        .Show();
                })
                .SetOnAdd(() => {
                    Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.PresetName), "", (c, text) => {
                        if (!c && !string.IsNullOrEmpty(text)) {
                            RelocatorManager.Instance.SavePreset(text, profile);
                            refresh();
                        }
                    }, (Dialog.InputType)0);
                });

            // 2. Setup Preview Table
            previewTable = RelocatorTable<Thing>.Create()
                .SetShowHeader(true)
                .SetPreferredHeight(800)
                .SetOnSelect((t) => {
                    var menu = RelocatorMenu.Create();
                    menu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Move), () => {
                        if (currentContainer is not null) {
                            RelocatorManager.Instance.RelocateSingleThing(t, currentContainer);
                            if (previewTable is not null)
                                previewTable.Refresh();
                        }
                    });
                    menu.Show();
                })
                .AddColumn(RelocatorLang.GetText(RelocatorLang.LangKey.Text), 350, (t, cell) => {
                    var txt = cell.GetComponentInChildren<Text>();
                    if (!txt) {
                        GameObject g = new("Text");
                        g.transform.SetParent(cell.transform, false);
                        txt = g.AddComponent<Text>();
                        txt.font = SkinManager.Instance.fontSet.ui.source.font;
                        txt.color = SkinManager.CurrentColors.textDefault;
                        txt.alignment = TextAnchor.MiddleLeft;
                        txt.resizeTextForBestFit = true;
                        txt.resizeTextMinSize = 10;
                        txt.resizeTextMaxSize = 13;
                        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                        txt.verticalOverflow = VerticalWrapMode.Truncate;
                        txt.raycastTarget = false;
                        var rt = txt.rectTransform;
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.sizeDelta = Vector2.zero;
                    }
                    txt.text = t?.Name ?? "";
                    var rowBtn = cell.transform.parent.GetComponentInChildren<UIButton>();
                    if (rowBtn) {
                        var field = typeof(UIButton).GetField("tooltip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (field != null)
                            field.SetValue(rowBtn, null);
                        rowBtn.SetTooltip((tooltip) => {
                            if (t is not null && tooltip.note is not null)
                                t.WriteNote(tooltip.note);
                        }, true);
                    }
                })
                .AddTextColumn(RelocatorLang.GetText(RelocatorLang.LangKey.Category), 100, t => t is { category: not null } ? t.category.GetName() : "")
                .AddTextColumn(RelocatorLang.GetText(RelocatorLang.LangKey.Parent), 100, t => {
                    if (t is null || t.parent is null)
                        return "";
                    if (t.parent == EClass.pc.things)
                        return RelocatorLang.GetText(RelocatorLang.LangKey.Inventory);
                    if (t.parent == EClass._map.things)
                        return RelocatorLang.GetText(RelocatorLang.LangKey.Zone);
                    return t.parent is Card c ? c.Name : t.parent.ToString();
                })
                .AddTextColumn(RelocatorLang.GetText(RelocatorLang.LangKey.Details), 250, t => {
                    if (t == null)
                        return "";

                    // Show value based on current sort mode
                    // Show value based on current sort mode
                    return RelocatorManager.Instance.GetDisplayValue(t, profile);
                });

            // Initialize Layers (Main and Preview)
            LayerList layerMain, layerPreview, layerSidebar;
            InitLayers(out layerMain, out layerPreview, out layerSidebar);

            // Refine refresh to update sidebar
            var oldRefresh = refresh;
            refresh = () => {
                oldRefresh();
                if (presetSidebar != null) {
                    presetSidebar.SetItems(RelocatorManager.Instance.GetPresetList());
                    presetSidebar.Refresh();
                }
                // Update Preview Header with Sort Mode
                string sortText = GetSortText(profile.SortMode);
                // "Details (Default)" looks weird if Default has no text? Default returns "".
                if (!string.IsNullOrEmpty(sortText))
                    previewTable.UpdateHeader(3, RelocatorLang.GetText(RelocatorLang.LangKey.Details) + " - " + sortText);
                else
                    previewTable.UpdateHeader(3, RelocatorLang.GetText(RelocatorLang.LangKey.Details));
            };

            // Restore Footer Buttons ("Settings", "Execute", "Add Rule")
            // "Add Rule" stays in Accordion? No, user said "Except Add Filter".
            // "Add Filter" is gone. We have "Add Rule".
            // Let's put ALL main actions in Footer for consistency.
            var win = layerMain.windows[0];
            win.AddBottomButton(RelocatorLang.GetText(RelocatorLang.LangKey.AddRule), () => {
                Dialog.InputName(RelocatorLang.GetText(RelocatorLang.LangKey.NewRuleName), RelocatorLang.GetText(RelocatorLang.LangKey.NewRuleName), (c, text) => {
                    if (!c && !string.IsNullOrEmpty(text)) {
                        profile.Rules.Add(new() { Name = text });
                        refresh();
                    }
                }, (Dialog.InputType)0);
            });
            // Reset Button (Clear current rule conditions)
            win.AddBottomButton(RelocatorLang.GetText(RelocatorLang.LangKey.ResetRules), () => {
                Dialog.YesNo(RelocatorLang.GetText(RelocatorLang.LangKey.ResetRulesConfirm), () => {
                    // Logic: "Erase editing search rule/condition (Initialize All)"
                    // If this means "Reset Profile Rules to Empty", use:
                    // profile.Rules.Clear();
                    // But user said "editing...". If there's a selected rule, maybe just that one?
                    // "すべて初期化する" (Initialize All) usually implies a full reset of the current context.
                    // Given the "Relocator" context, I'll reset the entire profile rules list to be safe/thorough,
                    // OR just the conditions of the rules? No, deleting rules is "Reset".
                    // Actually, "Rules & Conditions".
                    // I will Clear profile.Rules.
                    profile.Rules.Clear();
                    profile.Rules.Add(new RelocationRule()); // Add default rule
                    refresh();
                    Msg.Say(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_RulesCleared));
                });
            });
            win.AddBottomButton(RelocatorLang.GetText(RelocatorLang.LangKey.Settings), () => ShowSettingsMenu(profile, refresh));
            win.AddBottomButton(RelocatorLang.GetText(RelocatorLang.LangKey.Execute), () => {
                RelocatorManager.Instance.ExecuteRelocation(container);
                RelocatorManager.Instance.ClearCache();
                if (mainAccordion != null)
                    mainAccordion.Close();
            });

            // Initial Data Load
            refresh();
        }

        // ====================================================================
        // Layer Initialization & Setup
        // ====================================================================
        private static void InitLayers(out LayerList main, out LayerList preview, out LayerList sidebar) {
            // 1. Create Main Layer via Accordion
            var _main = mainAccordion.Show();
            var winMain = _main.windows[0];

            // 2. Create Preview Layer (Table)
            var _preview = previewTable.Show();
            var winPreview = _preview.windows[0];

            // 3. Create Sidebar (Last = Topmost)
            var _sidebar = presetSidebar.Show();
            var winSidebar = _sidebar.windows[0];

            // Disable full-screen raycast blocking for all layers to allow clicking through to others
            DisableLayerBlocker(_main, winMain);
            DisableLayerBlocker(_preview, winPreview);
            DisableLayerBlocker(_sidebar, winSidebar);

            // =================================================================================
            // DYNAMIC LAYOUT CALCULATION (Center Group)
            // =================================================================================
            // Define Component Widths
            float widthSidebar = 200f; // Increased from 160f
            float widthMain = 580f;
            float widthPreview = 840f; // Increased to accommodate new columns (was 640f)
            float gap = 10f;

            // Calculate Total Width and Starting X for centering
            // Group: [Sidebar] [Gap] [Main] [Gap] [Preview]
            float totalWidth = widthSidebar + gap + widthMain + gap + widthPreview;
            float startX = -(totalWidth / 2f); // Left edge of the group

            // Calculate Center X for each component
            // 1. Sidebar Center: StartX + (W/2)
            float xSidebar = startX + (widthSidebar / 2f);

            // 2. Main Center: StartX + SidebarW + Gap + (W/2)
            float xMain = startX + widthSidebar + gap + (widthMain / 2f);

            // 3. Preview Center: StartX + SidebarW + Gap + MainW + Gap + (W/2)
            float xPreview = startX + widthSidebar + gap + widthMain + gap + (widthPreview / 2f);

            // Apply Widths to Components (via new APIs)
            presetSidebar.SetPreferredWidth(widthSidebar);
            // mainAccordion.SetPreferredWidth(widthMain); // Accordion relies on MainLayer setting size below
            previewTable.SetPreferredWidth(widthPreview);

            // Apply Sizes & Positions
            winMain.setting.allowResize = true;
            winPreview.setting.allowResize = true;

            _main.SetSize(widthMain, 800);

            var rectMain = winMain.GetComponent<RectTransform>();
            var rectPreview = winPreview.GetComponent<RectTransform>();
            var rectSidebar = winSidebar.GetComponent<RectTransform>();

            rectMain.anchoredPosition = new Vector2(xMain, 0);
            rectPreview.anchoredPosition = new Vector2(xPreview, 0);
            rectSidebar.anchoredPosition = new Vector2(xSidebar, 0);

            // Implementation of Sidebar Following Main Window
            var sync = _sidebar.gameObject.AddComponent<RelocatorSidebarSync>();
            sync.target = winMain.GetComponent<RectTransform>();
            sync.sidebar = winSidebar.GetComponent<RectTransform>();
            sync.offset = rectSidebar.anchoredPosition - rectMain.anchoredPosition;

            // Synced Closing Logic
            bool closing = false;
            OnKill(_main, () => {
                if (closing)
                    return;
                closing = true;
                if (mainAccordion != null)
                    mainAccordion.Close();
                if (_preview != null)
                    _preview.Close();
                if (_sidebar != null)
                    _sidebar.Close();
                RelocatorManager.Instance.ClearCache();
            });
            OnKill(_preview, () => {
                if (closing)
                    return;
                closing = true;
                if (_main != null)
                    _main.Close();
                if (_sidebar != null)
                    _sidebar.Close();
                RelocatorManager.Instance.ClearCache();
            });
            OnKill(_sidebar, () => {
                if (closing)
                    return;
                closing = true;
                if (_main != null)
                    _main.Close();
                if (_preview != null)
                    _preview.Close();
                RelocatorManager.Instance.ClearCache();
            });

            main = _main;
            preview = _preview;
            sidebar = _sidebar;
        }

        private static void DisableLayerBlocker(ELayer layer, Window win) {
            foreach (var g in layer.GetComponentsInChildren<Graphic>(true)) {
                if (g.transform.IsChildOf(win.transform) || g.gameObject == win.gameObject)
                    continue;
                g.raycastTarget = false;
            }
        }

        private static void OnKill(ELayer layer, Action action) {
            layer.onKill.AddListener(() => action());
        }

        public override void OnRightClick() {
            // This is actually not called since we use LayerList prefab
            // Handled via Harmony patch in Mod.cs
        }

        // ====================================================================
        // Refresh Logic
        // ====================================================================
        private static void RefreshPreview(LayerList layer, Thing container, RelocationProfile profile) {
            if (container == null)
                return;

            // Limit preview search to 2000
            List<Thing> matches = RelocatorManager.Instance.GetMatches(container, 2000).ToList();

            // Sorting is handled by Manager.GetMatches

            int count = matches.Count;
            string countStr = count >= 2000 ? "2000+" : count.ToString();

            if (previewTable != null) {
                string caption = RelocatorLang.GetText(RelocatorLang.LangKey.Preview) + " (" + countStr + ")";
                // Limit display to 200 items to avoid UI lag (Optimized: 100 -> 200)
                if (count > 200) {
                    caption += " [Display Limit: 200]";
                    previewTable.SetDataSource(matches.Take(200).ToList());
                } else {
                    previewTable.SetDataSource(matches);
                }
                previewTable.SetCaption(caption);
                previewTable.Refresh();
            }
        }

        private static void ShowSettingsMenu(RelocationProfile profile, Action refresh) {
            string currentScope = RelocatorLang.GetText(RelocatorLang.LangKey.Inventory); // Default
            if (profile.Scope == RelocationProfile.FilterScope.Both)
                currentScope = RelocatorLang.GetText(RelocatorLang.LangKey.ScopeBoth);
            else if (profile.Scope == RelocationProfile.FilterScope.ZoneOnly)
                currentScope = RelocatorLang.GetText(RelocatorLang.LangKey.Zone);
            else if (profile.Scope == RelocationProfile.FilterScope.PetsOnly)
                currentScope = RelocatorLang.GetText(RelocatorLang.LangKey.ScopePets);

            // Local Helper for Cache Updates
            void UpdateProfile(Action<RelocationProfile> action) {
                // With Master Cache, we only rebuild if game state changes (not scope/hotbar)
                // However, if we CHANGE THE SORT? No, sort is handled dynamically.
                // If we load PRESET? Preset might change rules.
                // MainLayer calls BuildCache on Open.
                // Scope/Hotbar toggles just need refresh.

                // But wait, if we switch TO a scope that wasn't gathered?
                // Master Cache gathers BOTH. So we have everything.

                // So triggers are NOT needed for Scope/Exclusion changes.
                // Just execute action and Refresh.

                // If Rules change? Rules are filtered dynamically.
                // So BuildCache is ONLY needed on:
                // 1. Initial Open
                // 2. Relocation Execution (items moved/destroyed)
                // 3. User Explicit Refresh (if added)
                // 4. Reset Rules? No, rules are dynamic.

                // So UpdateProfile just becomes:
                action(profile);
                refresh();
            }

            RelocatorMenu.Create()
                .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.Scope) + ": " + currentScope, (child) => {
                    child
                         .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Inventory), () => {
                             UpdateProfile(p => p.Scope = RelocationProfile.FilterScope.Inventory);
                         })
                         .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Zone), () => { // Zone Only
                             UpdateProfile(p => p.Scope = RelocationProfile.FilterScope.ZoneOnly);
                         })
                         .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.ScopeBoth), () => { // Both
                             UpdateProfile(p => p.Scope = RelocationProfile.FilterScope.Both);
                         })
                         .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.ScopePets), () => { // Pets Only
                             UpdateProfile(p => p.Scope = RelocationProfile.FilterScope.PetsOnly);
                         });
                })
                .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.SortLabel) + GetSortText(profile.SortMode), (sortRoot) => {
                    sortRoot
                        // Default
                        .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortDefault), () => { profile.SortMode = RelocationProfile.ResultSortMode.Default; refresh(); })

                        // Price Group
                        .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.LabelPrice), (g) => {
                            g.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.PriceAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.PriceDesc; refresh(); });
                        })

                        // Enchant Group
                        .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.Enchant), (g) => {
                            g.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortMagAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.EnchantMagAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortMagDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.EnchantMagDesc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortTotalEnchantMagDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.TotalEnchantMagDesc; refresh(); });
                        })

                        // Weight Group
                        .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.Weight), (g) => {
                            g.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortWeightAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.TotalWeightAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortWeightDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.TotalWeightDesc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortUnitWeightAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.UnitWeightAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortUnitWeightDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.UnitWeightDesc; refresh(); });
                        })

                        // Gen Level
                        .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.GenLvl), (g) => {
                            g.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.GenLvlAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.GenLvlDesc; refresh(); });
                        })

                        // DNA
                        .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.Dna), (g) => {
                            g.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.DnaAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.DnaDesc; refresh(); });
                        })

                        // Food Power
                        .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.LabelFoodPower), (g) => {
                            g.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.FoodPowerAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.FoodPowerDesc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortTotalFoodPowerAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.TotalFoodPowerAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortTotalFoodPowerDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.TotalFoodPowerDesc; refresh(); });
                        })

                        // UID
                        .AddChild(RelocatorLang.GetText(RelocatorLang.LangKey.LabelUid), (g) => {
                            g.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortAsc), () => { profile.SortMode = RelocationProfile.ResultSortMode.UidAsc; refresh(); })
                             .AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.SortDesc), () => { profile.SortMode = RelocationProfile.ResultSortMode.UidDesc; refresh(); });
                        });
                })
                .Show();
        }

        private static void ShowOperatorMenu(Action<string> onSelect) {
            var opMenu = RelocatorMenu.Create();
            string[] ops = [">=", "=", "<=", ">", "<", "!="];
            foreach (var op in ops) {
                opMenu.AddButton(RelocatorLang.GetText(RelocatorLang.LangKey.Operator) + ": " + op, () => onSelect(op));
            }
            opMenu.Show();
        }


        private static Button CreateTinyButton(Transform parent, string label, Action onClick) {
            var btnGO = new GameObject("Btn_" + label);
            btnGO.transform.SetParent(parent, false);
            var le = btnGO.AddComponent<LayoutElement>();
            le.minWidth = 24;
            le.preferredWidth = 24;
            le.minHeight = 24;
            le.preferredHeight = 24;

            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.8f, 0.8f, 0.8f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txt = txtGO.AddComponent<Text>();
            Font f = null;
            if (SkinManager.Instance is { fontSet.ui.source: not null }) {
                f = SkinManager.Instance.fontSet.ui.source.font;
            }
            if (f is null)
                f = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf"); // Fallback

            txt.font = f;
            txt.fontSize = 12;
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;

            Color c = Color.black;
            if (SkinManager.CurrentColors is not null)
                c = SkinManager.CurrentColors.textDefault;
            txt.color = c;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta = Vector2.zero;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;

            return btn;
        }



        private static string GetSortText(RelocationProfile.ResultSortMode mode) => mode switch {
            RelocationProfile.ResultSortMode.Default => RelocatorLang.GetText(RelocatorLang.LangKey.SortDefault),
            RelocationProfile.ResultSortMode.PriceAsc => RelocatorLang.GetText(RelocatorLang.LangKey.SortPriceAsc),
            RelocationProfile.ResultSortMode.PriceDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortPriceDesc),
            RelocationProfile.ResultSortMode.EnchantMagAsc => RelocatorLang.GetText(RelocatorLang.LangKey.SortMagAsc),
            RelocationProfile.ResultSortMode.EnchantMagDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortMagDesc),
            RelocationProfile.ResultSortMode.TotalEnchantMagDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortTotalEnchantMagDesc),
            RelocationProfile.ResultSortMode.TotalWeightAsc => RelocatorLang.GetText(RelocatorLang.LangKey.SortWeightAsc),
            RelocationProfile.ResultSortMode.TotalWeightDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortWeightDesc),
            RelocationProfile.ResultSortMode.UnitWeightAsc => RelocatorLang.GetText(RelocatorLang.LangKey.SortUnitWeightAsc),
            RelocationProfile.ResultSortMode.UnitWeightDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortUnitWeightDesc),
            RelocationProfile.ResultSortMode.UidAsc => RelocatorLang.GetText(RelocatorLang.LangKey.SortUidAsc),
            RelocationProfile.ResultSortMode.UidDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortUidDesc),
            RelocationProfile.ResultSortMode.GenLvlAsc => RelocatorLang.GetText(RelocatorLang.LangKey.GenLvl) + " - " + RelocatorLang.GetText(RelocatorLang.LangKey.SortAsc),
            RelocationProfile.ResultSortMode.GenLvlDesc => RelocatorLang.GetText(RelocatorLang.LangKey.GenLvl) + " - " + RelocatorLang.GetText(RelocatorLang.LangKey.SortDesc),
            RelocationProfile.ResultSortMode.DnaAsc => RelocatorLang.GetText(RelocatorLang.LangKey.Dna) + " - " + RelocatorLang.GetText(RelocatorLang.LangKey.SortAsc),
            RelocationProfile.ResultSortMode.DnaDesc => RelocatorLang.GetText(RelocatorLang.LangKey.Dna) + " - " + RelocatorLang.GetText(RelocatorLang.LangKey.SortDesc),
            RelocationProfile.ResultSortMode.FoodPowerAsc => RelocatorLang.GetText(RelocatorLang.LangKey.SortFoodPowerAsc),
            RelocationProfile.ResultSortMode.FoodPowerDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortFoodPowerDesc),
            RelocationProfile.ResultSortMode.TotalFoodPowerAsc => RelocatorLang.GetText(RelocatorLang.LangKey.SortTotalFoodPowerAsc),
            RelocationProfile.ResultSortMode.TotalFoodPowerDesc => RelocatorLang.GetText(RelocatorLang.LangKey.SortTotalFoodPowerDesc),
            _ => mode.ToString()
        };
    }
}
