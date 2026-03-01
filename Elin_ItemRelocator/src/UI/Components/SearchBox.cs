using System;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_ItemRelocator {
    public class RelocatorSearchBox {
        public GameObject GameObject { get; private set; }
        public InputField Input { get; private set; }
        public Button ModeButton { get; private set; }

        private Action<string> onSearch;
        private Action<bool> onModeChanged;
        private bool isAndMode;
        private Text txtMode;
        private Image imgMode;

        public static RelocatorSearchBox Create(Transform parent, Font font, bool showModeToggle, bool initialAndMode, Action<string> onSearch, Action<bool> onModeChanged = null) {
            var box = new RelocatorSearchBox();
            box.Build(parent, font, showModeToggle, initialAndMode, onSearch, onModeChanged);
            return box;
        }

        private void Build(Transform parent, Font font, bool showModeToggle, bool initialAndMode, Action<string> onSearch, Action<bool> onModeChanged) {
            this.onSearch = onSearch;
            this.onModeChanged = onModeChanged;
            this.isAndMode = initialAndMode;

            // Container
            var inputs = new GameObject("SearchInput");
            GameObject = inputs;
            inputs.transform.SetParent(parent, false);

            // Fix Parent Layout if needed (handled by caller or helper?)
            // The caller (Tree.cs) currently handles parent layout fixes.
            // Ideally this component just enables itself to play nice.

            var le = inputs.AddComponent<LayoutElement>();
            le.minHeight = 24;
            le.preferredHeight = 24;
            le.flexibleWidth = 1;
            le.flexibleHeight = 0;

            var hlg = inputs.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false; // Prevent forcing equal width
            hlg.spacing = 5;
            hlg.padding = new RectOffset(5, 5, 2, 2);

            // --- Input Field ---
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(inputs.transform, false);

            // Add LayoutElement to take remaining space
            var leInput = inputGO.AddComponent<LayoutElement>();
            leInput.flexibleWidth = 1;

            var imgInput = inputGO.AddComponent<Image>();
            imgInput.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Input = inputGO.AddComponent<InputField>();
            Input.textComponent = new GameObject("Text", typeof(Text)).GetComponent<Text>();
            Input.textComponent.transform.SetParent(inputGO.transform, false);
            Input.textComponent.font = font;
            Input.textComponent.fontSize = 12;
            Input.textComponent.color = Color.white;
            Input.textComponent.alignment = TextAnchor.MiddleLeft;
            var rtText = Input.textComponent.GetComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.offsetMin = new Vector2(5, 0);
            rtText.offsetMax = new Vector2(-5, 0);

            Input.placeholder = new GameObject("Placeholder", typeof(Text)).GetComponent<Text>();
            Input.placeholder.transform.SetParent(inputGO.transform, false);
            var txtPlace = (Text)Input.placeholder;
            txtPlace.font = font;
            txtPlace.fontSize = 12;
            txtPlace.text = "Search...";
            txtPlace.color = new Color(0.5f, 0.5f, 0.5f);
            txtPlace.fontStyle = FontStyle.Italic;
            txtPlace.alignment = TextAnchor.MiddleLeft;
            var rtPlace = txtPlace.GetComponent<RectTransform>();
            rtPlace.anchorMin = Vector2.zero;
            rtPlace.anchorMax = Vector2.one;
            rtPlace.offsetMin = new Vector2(5, 0);
            rtPlace.offsetMax = new Vector2(-5, 0);

            Input.onValueChanged.AddListener((val) => {
                onSearch?.Invoke(val);
            });

            // --- Mode Toggle Button (Right Side) ---
            if (showModeToggle) {
                var btnMode = new GameObject("BtnMode");
                btnMode.transform.SetParent(inputs.transform, false);

                var leMode = btnMode.AddComponent<LayoutElement>();
                leMode.minWidth = 70; // Wide enough for "Mode: AND"
                leMode.preferredWidth = 70;
                leMode.flexibleWidth = 0;

                imgMode = btnMode.AddComponent<Image>();
                // Make it look like a button (rounded if possible, but flat color is fine)

                ModeButton = btnMode.AddComponent<Button>();
                var colors = ModeButton.colors;
                colors.normalColor = Color.white;
                colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
                ModeButton.colors = colors;

                txtMode = new GameObject("Text", typeof(Text)).GetComponent<Text>();
                txtMode.transform.SetParent(btnMode.transform, false);
                txtMode.font = font;
                txtMode.fontSize = 10;
                txtMode.alignment = TextAnchor.MiddleCenter;
                txtMode.color = Color.white;
                txtMode.rectTransform.anchorMin = Vector2.zero;
                txtMode.rectTransform.anchorMax = Vector2.one;

                UpdateModeUI();

                ModeButton.onClick.AddListener(() => {
                    isAndMode = !isAndMode;
                    UpdateModeUI();
                    onModeChanged?.Invoke(isAndMode);
                });
            }
        }

        private void UpdateModeUI() {
            if (txtMode != null)
                txtMode.text = isAndMode ? "AND" : "OR";
            // Color feedback: Blue/Green for AND, Red/Orange for OR
            if (imgMode != null)
                imgMode.color = isAndMode ? new Color(0.3f, 0.6f, 0.8f) : new Color(0.8f, 0.4f, 0.3f);
        }

        public void SetSiblingIndex(int index) {
            GameObject.transform.SetSiblingIndex(index);
        }
    }
}
