using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_ItemRelocator {
    public class RelocatorTree<T> {
        private List<T> roots = new List<T>();
        private Func<T, IEnumerable<T>> getChildren;
        private Func<T, string> getText;
        private Func<T, string> getDebugText;
        private Func<T, Color> getBackgroundColor;
        private Action<T> onSelect;
        private Action<Transform, T> onBuildRow;
        private Func<T, bool> isSelected;
        private Func<T, bool> isDisabled; // e.g., if parent is selected
        private string caption = "Tree";
        private LayerList _layer;
        private HashSet<T> expanded = new HashSet<T>();
        private string _filterText = ""; // Search filter
        private List<BottomButtonDef> bottomButtons = new List<BottomButtonDef>();

        // Mode Controls
        public bool ShowModeToggle = false;
        public bool IsAndMode = true; // Default to AND

        // Cache the content transform to avoid accessing destroyed components
        private Transform _contentTrans;

        // Font customization
        private Font uiFont;

        public class BottomButtonDef {
            public string Label;
            public Action OnClick;
            public Color? Color;
        }

        private RelocatorTree() {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static RelocatorTree<T> Create() {
            return new RelocatorTree<T>();
        }

        public RelocatorTree<T> SetCaption(string text) {
            this.caption = text;
            return this;
        }

        public RelocatorTree<T> SetRoots(IEnumerable<T> roots) {
            this.roots = roots.ToList();
            return this;
        }

        public RelocatorTree<T> SetChildren(Func<T, IEnumerable<T>> accessor) {
            this.getChildren = accessor;
            return this;
        }

        public RelocatorTree<T> SetText(Func<T, string> selector) {
            this.getText = selector;
            return this;
        }

        public RelocatorTree<T> SetDebugText(Func<T, string> selector) {
            this.getDebugText = selector;
            return this;
        }

        public RelocatorTree<T> SetGetBackgroundColor(Func<T, Color> selector) {
            this.getBackgroundColor = selector;
            return this;
        }

        public RelocatorTree<T> SetOnBuildRow(Action<Transform, T> builder) {
            this.onBuildRow = builder;
            return this;
        }

        public RelocatorTree<T> SetOnSelect(Action<T> action) {
            this.onSelect = action;
            return this;
        }

        public RelocatorTree<T> SetIsSelected(Func<T, bool> predicate) {
            this.isSelected = predicate;
            return this;
        }

        public RelocatorTree<T> SetIsDisabled(Func<T, bool> predicate) {
            this.isDisabled = predicate;
            return this;
        }

        public RelocatorTree<T> AddBottomButton(string label, Action onClick, Color? color = null) {
            bottomButtons.Add(new BottomButtonDef { Label = label, OnClick = onClick, Color = color });
            return this;
        }

        public void ExpandSelected() {
            if (isSelected == null)
                return;

            bool Check(T item) {
                var children = getChildren != null ? getChildren(item) : null;
                bool anyChildHit = false;
                if (children != null) {
                    foreach (var kid in children) {
                        if (Check(kid))
                            anyChildHit = true;
                    }
                }

                bool hit = isSelected(item) || anyChildHit;
                if (anyChildHit)
                    expanded.Add(item);
                return hit;
            }

            foreach (var root in roots)
                Check(root);
        }

        public LayerList Show() {
            bool isNew = false;
            // Robust Layer Creation
            if (_layer == null || _layer.gameObject == null) {
                _layer = EClass.ui.AddLayer<LayerList>();
                if (_layer == null) {
                    Debug.LogError("[RelocatorTree] Failed to create LayerList!");
                    return null;
                }
                isNew = true;
            }
            var layer = _layer;

            if (isNew) {
                if (layer.windows.Count > 0) {
                    layer.windows[0].SetCaption(caption ?? "Tree");
                    layer.windows[0].setting.allowResize = true;

                    // --- Search Bar Injection ---
                    try {
                        var searchBox = RelocatorSearchBox.Create(
                            layer.list.transform.parent,
                            uiFont,
                            ShowModeToggle,
                            IsAndMode,
                            (val) => { _filterText = val; Refresh(); },
                            (mode) => { IsAndMode = mode; }
                        );

                        searchBox.SetSiblingIndex(layer.list.transform.GetSiblingIndex());

                        // Fix Parent Layout to prevent forcing height
                        var parentGroup = layer.list.transform.parent.GetComponent<VerticalLayoutGroup>();
                        if (parentGroup) {
                            parentGroup.childForceExpandHeight = false;
                            parentGroup.childControlHeight = true;
                        }

                        // Ensure List takes remaining space
                        var listLE = layer.list.GetComponent<LayoutElement>();
                        if (!listLE)
                            listLE = layer.list.gameObject.AddComponent<LayoutElement>();
                        listLE.flexibleHeight = 1;

                    } catch (Exception ex) { Debug.LogError("Search UI Init Failed: " + ex); }

                } else {
                    Debug.LogWarning("[RelocatorTree] LayerList has no windows!");
                }

                try { layer.SetSize(650, 700); } catch { }

                // --- MANUAL SETUP ---
                if (layer.list != null) {
                    // Cache Transform BEFORE destroying the UIList component
                    _contentTrans = layer.list.transform;
                    var contentTrans = _contentTrans;

                    // 1. Destroy native UIList (if exists)
                    var nativeList = layer.list.GetComponent<UIList>();
                    if (nativeList)
                        UnityEngine.Object.DestroyImmediate(nativeList);

                    // 2. Setup VLG
                    if (contentTrans != null) {
                        var vlg = contentTrans.GetComponent<VerticalLayoutGroup>();
                        if (!vlg)
                            vlg = contentTrans.gameObject.AddComponent<VerticalLayoutGroup>();
                        vlg.childControlWidth = true;
                        vlg.childControlHeight = true;
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;
                        vlg.spacing = 0;
                        vlg.padding = new RectOffset(0, 0, 0, 0);

                        var csf = contentTrans.GetComponent<ContentSizeFitter>();
                        if (!csf)
                            csf = contentTrans.gameObject.AddComponent<ContentSizeFitter>();
                        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    }
                } else {
                    Debug.LogError("[RelocatorTree] layer.list is NULL!");
                }
            }

            Refresh();
            return layer;
        }

        public void Refresh() {
            if (_layer == null || _layer.gameObject == null)
                return;
            if (_contentTrans == null)
                return;

            var contentTrans = _contentTrans;

            // 1. Flatten Data
            // 1. Flatten Data
            var flattened = new List<Tuple<T, int>>(); // Item, Depth

            // Helper to check match
            bool IsMatch(T item) {
                if (string.IsNullOrEmpty(_filterText))
                    return true;
                string t = (getText != null ? getText(item) : item.ToString());
                return t.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // Recursive checker for filtering
            // Returns true if item or any descendant matches filter
            Func<T, bool> checkFilter = null;
            var visibleItems = new HashSet<T>();
            checkFilter = (item) => {
                bool selfMatch = IsMatch(item);
                bool childMatch = false;
                var kids = getChildren != null ? getChildren(item) : null;
                if (kids != null) {
                    foreach (var k in kids) {
                        if (checkFilter(k))
                            childMatch = true;
                    }
                }
                bool visible = selfMatch || childMatch;
                if (visible)
                    visibleItems.Add(item);
                return visible;
            };

            if (!string.IsNullOrEmpty(_filterText)) {
                foreach (var root in roots)
                    checkFilter(root);
            }

            Action<IEnumerable<T>, int> traverse = null;
            traverse = (items, depth) => {
                if (items == null)
                    return;
                foreach (var item in items) {
                    // Filter Check
                    if (!string.IsNullOrEmpty(_filterText) && !visibleItems.Contains(item))
                        continue;

                    flattened.Add(Tuple.Create(item, depth));

                    // Check expansion
                    // If filtering, auto-expand if children are visible
                    bool isFiltering = !string.IsNullOrEmpty(_filterText);
                    bool shouldExpand = expanded.Contains(item) || (isFiltering && visibleItems.Contains(item)); // Simplification: actually if it has visible children we expand.

                    // Optimization: check if any child is visible?
                    var kids = getChildren != null ? getChildren(item) : null;
                    if (kids != null && kids.Any()) {
                        bool anyChildVisible = isFiltering ? kids.Any(k => visibleItems.Contains(k)) : true;

                        if (shouldExpand && anyChildVisible) {
                            traverse(kids, depth + 1);
                        }
                    }
                }
            };
            traverse(roots, 0);

            // 2. Clear Visuals
            foreach (Transform child in contentTrans) {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            contentTrans.DetachChildren();

            // 3. Rebuild
            foreach (var tuple in flattened) {
                T item = tuple.Item1;
                int depth = tuple.Item2;

                BuildRow(contentTrans, item, depth);
            }

            // 4. Bottom Buttons
            foreach (var btnDef in bottomButtons) {
                BuildBottomButton(contentTrans, btnDef);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTrans as RectTransform);
        }

        private void BuildRow(Transform parent, T item, int depth) {
            if (onBuildRow != null) {
                // Create a container for the row but let the user populate it?
                // Or let user take the parent and do whatever?
                // Ideally, we want a row object.
                var cRow = new GameObject("CustomRow");
                cRow.transform.SetParent(parent, false);
                onBuildRow(cRow.transform, item);
                // If the builder populated it, great.
                // We need to check if we should proceed with default building?
                // Let's assume onBuildRow handles EVERYTHING for this item if it does anything?
                // No, we need a way to say "Skip Default".
                // Let's check child count.
                if (cRow.transform.childCount > 0)
                    return;
                // If empty, destroy and continue default?
                UnityEngine.Object.DestroyImmediate(cRow);
            }

            var rowGO = new GameObject("Row");
            rowGO.transform.SetParent(parent, false);

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false; // Allow children to have different heights
            hlg.childAlignment = TextAnchor.MiddleLeft; // Center vertically
            hlg.spacing = 0;
            hlg.padding = new RectOffset(5, 5, 0, 0);

            var imgRow = rowGO.AddComponent<Image>();
            imgRow.color = Color.clear;

            var leRow = rowGO.AddComponent<LayoutElement>();
            leRow.minHeight = 28;
            // leRow.preferredHeight = 28; // Allow expansion
            leRow.flexibleHeight = 0;

            // Clicker
            var clickerGO = new GameObject("Clicker");
            clickerGO.transform.SetParent(rowGO.transform, false);
            var leClicker = clickerGO.AddComponent<LayoutElement>();
            leClicker.ignoreLayout = true;
            var imgClicker = clickerGO.AddComponent<Image>();
            imgClicker.color = Color.clear;
            var btnClicker = clickerGO.AddComponent<Button>();
            btnClicker.targetGraphic = imgClicker;
            var rtClicker = clickerGO.GetComponent<RectTransform>();
            rtClicker.anchorMin = Vector2.zero;
            rtClicker.anchorMax = Vector2.one;
            rtClicker.offsetMin = Vector2.zero;
            rtClicker.offsetMax = Vector2.zero;
            clickerGO.transform.SetSiblingIndex(0);

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(rowGO.transform, false);
            var leSpacer = spacer.AddComponent<LayoutElement>();
            float indent = depth * 15;
            leSpacer.minWidth = indent;
            leSpacer.preferredWidth = indent;
            spacer.SetActive(indent > 0);

            // Expand
            var expandGO = new GameObject("Expand");
            expandGO.transform.SetParent(rowGO.transform, false);
            var leExpand = expandGO.AddComponent<LayoutElement>();
            leExpand.minWidth = 24;
            leExpand.preferredWidth = 24;
            leExpand.minHeight = 24;
            leExpand.preferredHeight = 24;

            var txtExpand = new GameObject("Text").AddComponent<Text>();
            txtExpand.transform.SetParent(expandGO.transform, false);
            txtExpand.rectTransform.anchorMin = Vector2.zero;
            txtExpand.rectTransform.anchorMax = Vector2.one;
            txtExpand.font = uiFont;
            txtExpand.fontSize = 14;
            txtExpand.alignment = TextAnchor.MiddleCenter;
            txtExpand.color = Color.black;
            txtExpand.fontStyle = FontStyle.Bold;
            txtExpand.raycastTarget = false;

            // Expand Button
            var imgExpand = expandGO.AddComponent<Image>();
            imgExpand.color = Color.white; // Base White for Tint
            var btnExpand = expandGO.AddComponent<Button>();
            btnExpand.targetGraphic = imgExpand;

            // Subtle Scale Highlight Logic
            ColorBlock cb = btnExpand.colors;
            cb.normalColor = Color.clear; // Invisible normally (White * Clear = Clear)
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f); // Faint White Highlight
            cb.pressedColor = new Color(1f, 1f, 1f, 0.25f);
            cb.selectedColor = Color.clear;
            cb.disabledColor = Color.clear;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0f; // No flicker
            btnExpand.colors = cb;


            // Label Container
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(rowGO.transform, false);
            var leLbl = lblGO.AddComponent<LayoutElement>();
            leLbl.flexibleWidth = 1;
            leLbl.minWidth = 50; // Ensure it can shrink but not too much

            // Allow height expansion based on Content
            var csfLbl = lblGO.AddComponent<ContentSizeFitter>();
            csfLbl.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csfLbl.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Internal Layout for padding/control
            var vlgLbl = lblGO.AddComponent<VerticalLayoutGroup>();
            vlgLbl.childControlHeight = true;
            vlgLbl.childControlWidth = true;
            vlgLbl.childForceExpandHeight = false;
            vlgLbl.padding = new RectOffset(0, 0, 5, 5); // Add vertical padding

            // Background Image for Hit Test
            var imgLbl = lblGO.AddComponent<Image>();
            imgLbl.color = Color.white; // Base White for Tint

            var btnLbl = lblGO.AddComponent<Button>();
            btnLbl.targetGraphic = imgLbl;
            btnLbl.colors = cb; // Use same highlight style

            // Actual Text
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(lblGO.transform, false);
            var lbl = txtGO.AddComponent<Text>();
            lbl.font = uiFont;
            lbl.fontSize = 14;
            lbl.alignment = TextAnchor.MiddleLeft;

            // Wrapping Settings
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow = VerticalWrapMode.Overflow;
            lbl.resizeTextForBestFit = false;

            // Stretch Text to fill Label Container
            var rtTxt = txtGO.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = Vector2.zero;
            rtTxt.offsetMax = Vector2.zero;



            // Highlight Check
            bool sel = isSelected != null && isSelected(item);
            if (sel) {
                imgRow.color = new Color(0.9f, 0.9f, 1f, 0.5f); // Selected Background
            }

            // Input Field for Selected Items (Leaf nodes only)
            // Need to determine if leaf using getChildren
            var kids = getChildren != null ? getChildren(item) : null;
            bool hasKids = kids != null && kids.Any();
            bool isLeaf = !hasKids;

            if (sel && isLeaf) {
                // Input Field Container
                var inputGO = new GameObject("InputVal");
                inputGO.transform.SetParent(rowGO.transform, false);
                var leInput = inputGO.AddComponent<LayoutElement>();
                leInput.minWidth = 60;
                leInput.preferredWidth = 60;
                leInput.preferredHeight = 24;

                var imgInput = inputGO.AddComponent<Image>();
                imgInput.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

                var inputField = inputGO.AddComponent<InputField>();

                // Text Component
                var txtInputGO = new GameObject("Text");
                txtInputGO.transform.SetParent(inputGO.transform, false);
                var rtTxtInput = txtInputGO.AddComponent<RectTransform>();
                rtTxtInput.anchorMin = Vector2.zero;
                rtTxtInput.anchorMax = Vector2.one;
                rtTxtInput.offsetMin = new Vector2(5, 0);
                rtTxtInput.offsetMax = new Vector2(-5, 0);

                var txtInput = txtInputGO.AddComponent<Text>();
                txtInput.font = uiFont;
                txtInput.fontSize = 14;
                txtInput.color = Color.white;
                txtInput.alignment = TextAnchor.MiddleLeft;
                inputField.textComponent = txtInput;

                // Init Value
                string currentVal = GetValue(item);
                if (string.IsNullOrEmpty(currentVal))
                    currentVal = DefaultValue;
                inputField.text = currentVal;

                // Listener
                inputField.onValueChanged.AddListener((val) => {
                    SetValue(item, val);
                });
            }

            // Logic: Expand Button
            if (hasKids) {
                bool isExp = expanded.Contains(item);
                txtExpand.text = isExp ? "-" : "+";
                btnExpand.onClick.AddListener(() => {
                    if (expanded.Contains(item))
                        expanded.Remove(item);
                    else
                        expanded.Add(item);
                    EClass.Sound.Play("click");
                    Refresh();
                });
            } else {
                imgExpand.color = Color.clear;
                txtExpand.text = "";
                btnExpand.interactable = false;
            }

            bool disabled = isDisabled != null && isDisabled(item);

            string extra = (getDebugText != null) ? getDebugText(item) : "";
            string name = (getText != null) ? getText(item) : item.ToString();

            if (getBackgroundColor != null) {
                imgRow.color = getBackgroundColor(item);
            }

            if (disabled) {
                lbl.text = "[P] " + name + extra;
                lbl.color = Color.gray;
                btnClicker.enabled = false;
                btnLbl.enabled = false;
            } else {
                lbl.text = (sel ? "[v] " : "[  ] ") + name + extra;
                lbl.color = sel ? new Color(0, 0, 0.8f) : new Color(0.1f, 0.1f, 0.1f);
                btnClicker.enabled = true;

                Action doSelect = () => {
                    if (onSelect != null) {
                        onSelect(item);
                        EClass.Sound.Play("click");
                        Refresh();
                    }
                };

                btnClicker.onClick.AddListener(() => doSelect());
                btnLbl.onClick.AddListener(() => doSelect());
            }
        }

        private void BuildBottomButton(Transform parent, BottomButtonDef def) {
            var btnGO = new GameObject("Btn_" + def.Label);
            btnGO.transform.SetParent(parent, false);

            var hlg = btnGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.padding = new RectOffset(10, 10, 5, 5);

            var le = btnGO.AddComponent<LayoutElement>();
            le.minHeight = 40;
            le.preferredHeight = 40;
            le.flexibleHeight = 0;

            var bg = btnGO.AddComponent<Image>();
            bg.color = Color.clear; // Default button bg

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => {
                if (def.OnClick != null)
                    def.OnClick();
            });

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = uiFont;
            txt.fontSize = 16;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = "<b>" + def.Label + "</b>";
            txt.color = def.Color ?? new Color(0.1f, 0.1f, 0.1f);
        }

        // Value Storage
        private Dictionary<T, string> valueMap = new Dictionary<T, string>();
        public string DefaultValue = ">0";

        public RelocatorTree<T> SetValue(T item, string val) {
            valueMap[item] = val;
            return this;
        }

        public string GetValue(T item) {
            return valueMap.TryGetValue(item, out var v) ? v : DefaultValue;
        }

        public void Close() {
            if (_layer != null)
                _layer.Close();
        }
    }
}
