using UnityEngine;
using UnityEngine.UI;

namespace Elin_NiComment
{
    public class CommentElement : MonoBehaviour
    {
        private RectTransform _rt;
        private Text _text;
        private float _speed;
        private CommentPool _pool;
        private int _laneIndex;

        public RectTransform RectTransform => _rt;
        public float Width => _rt != null ? _rt.sizeDelta.x : 0f;
        public int LaneIndex => _laneIndex;

        public void Setup(RectTransform rt, Text text)
        {
            _rt = rt;
            _text = text;
        }

        public void Initialize(string content, float yPos, float speed, Color color, int fontSize, CommentPool pool, int laneIndex)
        {
            _speed = speed;
            _pool = pool;
            _laneIndex = laneIndex;

            _text.text = content;
            _text.fontSize = fontSize;
            _text.color = color;

            // Force layout to calculate preferred width
            var preferredWidth = _text.preferredWidth;
            _rt.sizeDelta = new Vector2(preferredWidth, _text.preferredHeight);

            // Start just off the right edge of the screen (reference resolution 1920)
            _rt.anchoredPosition = new Vector2(1920f, -yPos);

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_rt == null) return;

            var pos = _rt.anchoredPosition;
            pos.x -= _speed * Time.unscaledDeltaTime;
            _rt.anchoredPosition = pos;

            // Return to pool when fully off-screen left
            if (pos.x < -Width)
            {
                _pool.Return(this);
            }
        }

        public float RightEdgeX()
        {
            if (_rt == null) return -9999f;
            return _rt.anchoredPosition.x + Width;
        }
    }
}
