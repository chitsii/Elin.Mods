using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_ItemRelocator {
    public class RelocatorSidebar {
        private LayerList _layer;
        private string caption = "";
        private List<string> items = new List<string>();
        private Action<string> onClickItem;
        private Action<string> onRightClickItem;
        private Action onClickAdd;
        public float? PreferredWidth;

        private RelocatorSidebar() { }

        public static RelocatorSidebar Create() {
            return new RelocatorSidebar();
        }

        public RelocatorSidebar SetCaption(string text) {
            this.caption = text;
            return this;
        }

        public RelocatorSidebar SetPreferredWidth(float w) {
            this.PreferredWidth = w;
            return this;
        }

        public RelocatorSidebar SetItems(IEnumerable<string> items) {
            this.items = items.ToList();
            return this;
        }

        public RelocatorSidebar SetOnSelect(Action<string> onClick) {
            this.onClickItem = onClick;
            return this;
        }

        public RelocatorSidebar SetOnRightClick(Action<string> onRightClick) {
            this.onRightClickItem = onRightClick;
            return this;
        }

        public RelocatorSidebar SetOnAdd(Action onAdd) {
            this.onClickAdd = onAdd;
            return this;
        }

        public void Refresh() {
            if (_layer && _layer.gameObject.activeInHierarchy) {
                Show();
            }
        }

        public void Close() {
            if (_layer)
                _layer.Close();
        }

        public LayerList Show() {
            if (_layer == null || _layer.gameObject == null) {
                _layer = EClass.ui.AddLayer<LayerList>();
                _layer.gameObject.AddComponent<RelocatorLayerMarker>();
            }
            var layer = _layer;

            var win = layer.windows[0];
            win.SetCaption(caption);
            win.setting.allowResize = false;

            float width = PreferredWidth ?? 160f;
            layer.SetSize(width, 800);

            var listImg = layer.list.GetComponent<Image>();

            // 1. Hide window components EXCEPT the title/caption
            foreach (var g in win.GetComponentsInChildren<Graphic>(true)) {
                if (g.transform.IsChildOf(layer.list.transform))
                    continue;

                // Allow Title/Caption elements to stay visible check REMOVED
                // if (g.gameObject == win.textCaption.gameObject || g.transform.IsChildOf(win.textCaption.transform))
                //    continue;

                // Also keep the caption background if it's a specific object (usually named "Caption" or similar) check REMOVED
                // if (g.name.Contains("Caption"))
                //    continue;

                g.enabled = false;
                g.raycastTarget = false;
            }

            if (listImg) {
                listImg.enabled = false;
                listImg.raycastTarget = false;
            }

            // Populate Items
            List<string> displayItems = new List<string>();
            displayItems.AddRange(items);
            displayItems.Add("+");

            layer.SetList2(displayItems,
                (s) => s,
                (s, b) => {
                    if (s == "+") {
                        if (onClickAdd != null)
                            onClickAdd();
                    } else {
                        if (onClickItem != null)
                            onClickItem(s);
                    }
                },
                (s, b) => {
                    var btn = b.button1;

                    // 2. Clear previous layout overrides
                    foreach (var layout in b.GetComponentsInChildren<LayoutGroup>(true)) {
                        layout.enabled = false;
                    }

                    // 3. Container Size
                    var rectB = b.GetComponent<RectTransform>();
                    if (rectB) {
                        rectB.sizeDelta = new Vector2(width, 42);
                        rectB.anchoredPosition = new Vector2(0, rectB.anchoredPosition.y);
                    }

                    // 5. RESTORE DEFAULT BUTTON LOOK
                    // 5. RESTORE DEFAULT BUTTON LOOK
                    // Text Color (Game Standard)
                    var t = btn.mainText;
                    if (t) {
                        // Use standard text color. Selection highlight is handled by the game button logic usually, or we can add it later.
                        t.color = (SkinManager.CurrentColors != null ? SkinManager.CurrentColors.textDefault : Color.black);
                    }

                    // Tint the button with the game's default button color (buttonSide for Sidebar)
                    var btnImg = btn.image;
                    if (btnImg) {
                        btnImg.enabled = true;

                        // Force solid block to ensure opacity
                        btnImg.sprite = null;

                        // Set Colors using ColorBlock to handle Highlight/Press states correctly
                        Color baseColor = new Color(0.8f, 0.7f, 0.5f);
                        if (SkinManager.CurrentColors != null) {
                            baseColor = SkinManager.CurrentColors.buttonSide; // Use Side button color
                        }
                        baseColor.a = 1.0f; // Force opaque

                        btn.transition = Selectable.Transition.ColorTint;
                        var colors = btn.colors;
                        colors.normalColor = baseColor;
                        // Brighten for highlight (1.2x)
                        colors.highlightedColor = new Color(Mathf.Min(baseColor.r * 1.2f, 1f), Mathf.Min(baseColor.g * 1.2f, 1f), Mathf.Min(baseColor.b * 1.2f, 1f), 1f);
                        colors.pressedColor = new Color(baseColor.r * 0.8f, baseColor.g * 0.8f, baseColor.b * 0.8f, 1f);
                        colors.selectedColor = baseColor;
                        colors.disabledColor = new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, baseColor.b * 0.5f, 0.5f);
                        colors.colorMultiplier = 1f;
                        btn.colors = colors;

                        // Reset Image color to white so ColorTint works properly (it multiplies)
                        btnImg.color = Color.white;

                        btnImg.raycastTarget = true;

                        var btnRect = btn.GetComponent<RectTransform>();
                        if (btnRect) {
                            btnRect.anchorMin = Vector2.zero;
                            btnRect.anchorMax = Vector2.one;
                            btnRect.pivot = new Vector2(0.5f, 0.5f);
                            btnRect.sizeDelta = new Vector2(-6, -6);
                            btnRect.anchoredPosition = Vector2.zero;
                        }
                    }

                    // 5. Hide Shortcut Keys and Center Text
                    if (btn.icon)
                        btn.icon.transform.parent.gameObject.SetActive(false);
                    if (btn.keyText)
                        btn.keyText.transform.parent.gameObject.SetActive(false);

                    if (btn.mainText) {
                        btn.mainText.alignment = TextAnchor.MiddleCenter;
                        // Use default text colors if not + button
                        btn.mainText.text = s;

                        var txtRect = btn.mainText.rectTransform;
                        if (txtRect) {
                            txtRect.anchorMin = Vector2.zero;
                            txtRect.anchorMax = Vector2.one;
                            txtRect.pivot = new Vector2(0.5f, 0.5f);
                            txtRect.sizeDelta = Vector2.zero;
                            txtRect.anchoredPosition = Vector2.zero;
                        }
                    }

                    // 6. Right click
                    if (s != "+") {
                        btn.onRightClick = () => { if (onRightClickItem != null) onRightClickItem(s); };
                    }
                },
                false
            );

            // Cleanup Layer Blockers
            foreach (var g in layer.GetComponentsInChildren<Graphic>(true)) {
                if (g.transform.IsChildOf(win.transform) || g.gameObject == win.gameObject)
                    continue;
                g.raycastTarget = false;
            }

            return layer;
        }
    }
}
