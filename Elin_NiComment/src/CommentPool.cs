using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_NiComment
{
    public class CommentPool
    {
        private readonly Stack<CommentElement> _available;
        private readonly List<CommentElement> _all;
        private readonly Transform _parent;

        public CommentPool(Transform parent, int capacity, Font font)
        {
            _available = new Stack<CommentElement>(capacity);
            _all = new List<CommentElement>(capacity);
            _parent = parent;

            for (int i = 0; i < capacity; i++)
            {
                var element = CreateElement(i, font);
                element.gameObject.SetActive(false);
                _available.Push(element);
                _all.Add(element);
            }
        }

        public CommentElement Get()
        {
            if (_available.Count == 0) return null;
            return _available.Pop();
        }

        public void Return(CommentElement element)
        {
            element.gameObject.SetActive(false);
            _available.Push(element);
        }

        public IReadOnlyList<CommentElement> All => _all;

        private CommentElement CreateElement(int index, Font font)
        {
            var go = new GameObject($"Comment_{index}", typeof(RectTransform));
            go.transform.SetParent(_parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); // top-left
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = ModConfig.FontSize.Value;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var element = go.AddComponent<CommentElement>();
            element.Setup(rt, text);

            return element;
        }
    }
}
