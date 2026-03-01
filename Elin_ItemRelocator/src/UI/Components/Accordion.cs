using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_ItemRelocator {
    public class RelocatorAccordion<T> {
        private List<T> roots = new List<T>();
        private Func<T, IEnumerable<T>> getChildren;
        private Func<T, string> getText;
        private Func<T, string> getDebugText;
        private Action<T> onSelect;
        private Func<T, bool> isSelected;
        private Func<T, bool> isDisabled; // e.g., if parent is selected
        private string caption = "Tree";
        private LayerList _layer;
        private HashSet<T> expanded = new HashSet<T>();
        private List<BottomButtonDef> bottomButtons = new List<BottomButtonDef>();
        public float? PreferredWidth;

        // New Callbacks
        private Action<T> onRightClick;
        private Action<T, GameObject> onBuildRowExtra;
        private Action<Transform, T> onBuildRow; // Custom Builder
        private Func<T, Color> getBackgroundColor;

        private Transform _contentTrans;
        private Font uiFont;

        public class BottomButtonDef {
            public string Label;
            public Action OnClick;
            public Color? Color;
        }

        private RelocatorAccordion() {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static RelocatorAccordion<T> Create() {
            return new RelocatorAccordion<T>();
        }

        public RelocatorAccordion<T> SetCaption(string text) {
            this.caption = text;
            return this;
        }

        public RelocatorAccordion<T> SetPreferredWidth(float w) {
            this.PreferredWidth = w;
            return this;
        }

        public RelocatorAccordion<T> SetRoots(IEnumerable<T> roots) {
            this.roots = roots.ToList();
            return this;
        }

        public RelocatorAccordion<T> SetChildren(Func<T, IEnumerable<T>> accessor) {
            this.getChildren = accessor;
            return this;
        }

        public RelocatorAccordion<T> SetText(Func<T, string> selector) {
            this.getText = selector;
            return this;
        }

        public RelocatorAccordion<T> SetDebugText(Func<T, string> selector) {
            this.getDebugText = selector;
            return this;
        }

        public RelocatorAccordion<T> SetOnSelect(Action<T> action) {
            this.onSelect = action;
            return this;
        }

        public RelocatorAccordion<T> SetIsSelected(Func<T, bool> predicate) {
            this.isSelected = predicate;
            return this;
        }

        public RelocatorAccordion<T> SetIsDisabled(Func<T, bool> predicate) {
            this.isDisabled = predicate;
            return this;
        }

        public RelocatorAccordion<T> SetOnRightClick(Action<T> action) {
            this.onRightClick = action;
            return this;
        }

        public RelocatorAccordion<T> SetOnBuildRowExtra(Action<T, GameObject> action) {
            this.onBuildRowExtra = action;
            return this;
        }

        public RelocatorAccordion<T> SetOnBuildRow(Action<Transform, T> action) {
            this.onBuildRow = action;
            return this;
        }

        public RelocatorAccordion<T> SetGetBackgroundColor(Func<T, Color> selector) {
            this.getBackgroundColor = selector;
            return this;
        }

        public void Expand(T item) {
            if (!expanded.Contains(item))
                expanded.Add(item);
        }

        public RelocatorAccordion<T> AddBottomButton(string label, Action onClick, Color? color = null) {
            bottomButtons.Add(new BottomButtonDef { Label = label, OnClick = onClick, Color = color });
            return this;
        }

        public LayerList Show() {
            bool isNew = false;
            // Robust Layer Creation
            if (_layer == null || _layer.gameObject == null) {
                _layer = EClass.ui.AddLayer<LayerList>();
                if (_layer == null) {
                    Debug.LogError("[RelocatorAccordion] Failed to create LayerList!");
                    return null;
                }
                _layer.gameObject.AddComponent<RelocatorLayerMarker>();
                isNew = true;
            }
            var layer = _layer;

            if (isNew) {
                if (layer.windows.Count > 0) {
                    layer.windows[0].SetCaption(caption ?? "Tree");
                    layer.windows[0].setting.allowResize = true;
                }

                try { layer.SetSize(500, 700); } catch { }

                // --- MANUAL SETUP ---
                if (layer.list != null) {
                    _contentTrans = layer.list.transform;
                    var contentTrans = _contentTrans;

                    var nativeList = layer.list.GetComponent<UIList>();
                    if (nativeList)
                        UnityEngine.Object.DestroyImmediate(nativeList);

                    if (contentTrans != null) {
                        var vlg = contentTrans.GetComponent<VerticalLayoutGroup>();
                        if (!vlg)
                            vlg = contentTrans.gameObject.AddComponent<VerticalLayoutGroup>();
                        vlg.childControlWidth = true;
                        vlg.childControlHeight = true;
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;
                        vlg.spacing = 2;
                        vlg.padding = new RectOffset(0, 0, 0, 0);

                        var csf = contentTrans.GetComponent<ContentSizeFitter>();
                        if (!csf)
                            csf = contentTrans.gameObject.AddComponent<ContentSizeFitter>();
                        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    }
                }
            }

            Refresh();
            return layer;
        }

        public void Close() {
            if (_layer != null)
                _layer.Close();
        }

        public void Refresh() {
            if (_layer == null || _layer.gameObject == null)
                return;
            if (_contentTrans == null)
                return;

            var contentTrans = _contentTrans;

            // 1. Flatten Data
            var flattened = new List<Tuple<T, int>>(); // Item, Depth

            Action<IEnumerable<T>, int> traverse = null;
            traverse = (items, depth) => {
                if (items == null)
                    return;
                foreach (var item in items) {
                    flattened.Add(Tuple.Create(item, depth));
                    if (expanded.Contains(item)) {
                        var kids = getChildren != null ? getChildren(item) : null;
                        if (kids != null && kids.Any()) {
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
                var cRow = new GameObject("CustomRow");
                cRow.transform.SetParent(parent, false);
                try {
                    onBuildRow(cRow.transform, item);
                } catch (Exception ex) {
                    Debug.LogError("[RelocatorAccordion] Custom Row Build Error: " + ex.ToString());
                }
                if (cRow.transform.childCount > 0)
                    return;
                UnityEngine.Object.DestroyImmediate(cRow);
            }

            var rowGO = new GameObject("Row");
            rowGO.transform.SetParent(parent, false);

            // Row Styling
            var imgRow = rowGO.AddComponent<Image>();
            Color baseColor = (getBackgroundColor != null) ? getBackgroundColor(item) : Color.clear;
            if (baseColor == Color.clear && depth == 0)
                baseColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
            imgRow.color = baseColor;
            imgRow.raycastTarget = true;

            // Layout
            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false; // Allow variable height
            hlg.childAlignment = TextAnchor.MiddleLeft; // Fix Vertical Alignment
            hlg.spacing = 5;
            hlg.padding = new RectOffset(5 + (depth * 20), 5, 2, 2);

            var leRow = rowGO.AddComponent<LayoutElement>();
            leRow.minHeight = 32;
            // leRow.preferredHeight = 32; // Allow expansion
            leRow.flexibleHeight = 0;

            // Content
            string name = (getText != null) ? getText(item) : item.ToString();

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(rowGO.transform, false);
            var leLbl = lblGO.AddComponent<LayoutElement>();
            leLbl.flexibleWidth = 1;
            leLbl.minWidth = 50; // Prevent collapse

            // Allow height expansion
            var csfLbl = lblGO.AddComponent<ContentSizeFitter>();
            csfLbl.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csfLbl.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var vlgLbl = lblGO.AddComponent<VerticalLayoutGroup>();
            vlgLbl.childControlHeight = true;
            vlgLbl.childControlWidth = true;
            vlgLbl.childForceExpandHeight = false;
            vlgLbl.padding = new RectOffset(0, 0, 2, 2);

            var lbl = lblGO.AddComponent<Text>();
            // Use Game Default Font
            // Use Game Default Font
            Font f = null;
            if (SkinManager.Instance != null && SkinManager.Instance.fontSet != null && SkinManager.Instance.fontSet.ui != null && SkinManager.Instance.fontSet.ui.source != null) {
                f = SkinManager.Instance.fontSet.ui.source.font;
            }
            if (f == null)
                f = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");

            lbl.font = f;
            lbl.fontSize = 14;
            lbl.alignment = TextAnchor.MiddleLeft;

            // Wrapping
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow = VerticalWrapMode.Truncate;
            lbl.resizeTextForBestFit = false;
            lbl.text = name;

            Color c = Color.black;
            if (SkinManager.CurrentColors != null)
                c = SkinManager.CurrentColors.textDefault;
            lbl.color = c;
            lbl.raycastTarget = false;

            // Interaction logic
            var kids = getChildren != null ? getChildren(item) : null;
            bool hasKids = kids != null && kids.Any();

            // Button covering the whole row
            var btnRow = rowGO.AddComponent<Button>();
            btnRow.targetGraphic = imgRow;
            btnRow.onClick.AddListener(() => {
                if (hasKids) {
                    if (expanded.Contains(item))
                        expanded.Remove(item);
                    else
                        expanded.Add(item);
                } else {
                    if (onSelect != null)
                        onSelect(item);
                }
                EClass.Sound.Play("click");
                Refresh();
            });

            // Hover
            var trigger = rowGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => {
                if (baseColor.a < 0.1f)
                    imgRow.color = new Color(1f, 1f, 1f, 0.2f); // Light highlight for transparent
                else
                    imgRow.color = baseColor + new Color(0.1f, 0.1f, 0.1f, 0f); // Lighten opque
            });
            trigger.triggers.Add(entryEnter);

            var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { imgRow.color = baseColor; });
            trigger.triggers.Add(entryExit);

            // Right Click
            var entryClick = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryClick.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entryClick.callback.AddListener((data) => {
                var pData = data as UnityEngine.EventSystems.PointerEventData;
                if (pData != null && pData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right) {
                    EInput.rightMouse.Consume();
                    if (onRightClick != null)
                        onRightClick(item);
                }
            });
            trigger.triggers.Add(entryClick);

            // Extra Buttons
            if (onBuildRowExtra != null) {
                onBuildRowExtra(item, rowGO);
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
            bg.color = Color.clear;

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
    }
}
