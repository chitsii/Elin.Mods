using UnityEngine;
using UnityEngine.UI;

namespace Elin_ModTemplate
{
    public sealed class DoomOverlayDisplay : MonoBehaviour
    {
        private Canvas _canvas;
        private RawImage _image;
        private Image _backdrop;
        private Text _noticeText;
        private Texture2D _frameTex;
        private RenderTexture _target;
        private float _noticeUntil;

        private float CurrentScale => Mathf.Clamp(ModConfig.OverlayScale?.Value ?? 0.60f, 0.30f, 0.95f);
        private float CurrentBackdropAlpha => Mathf.Clamp01(ModConfig.BackdropAlpha?.Value ?? 0.55f);

        public static DoomOverlayDisplay Create()
        {
            var go = new GameObject("JustDoomIt_Overlay");
            DontDestroyOnLoad(go);
            return go.AddComponent<DoomOverlayDisplay>();
        }

        public void Initialize(int width, int height)
        {
            _frameTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _frameTex.filterMode = FilterMode.Point;
            _frameTex.wrapMode = TextureWrapMode.Clamp;

            _target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _target.filterMode = FilterMode.Point;
            _target.wrapMode = TextureWrapMode.Clamp;
            _target.Create();

            BuildUi();
            _image.texture = _target;
        }

        public void Upload(Color32[] pixels)
        {
            if (_frameTex == null || pixels == null || pixels.Length == 0) return;
            _frameTex.SetPixels32(pixels);
            _frameTex.Apply(false, false);
            Graphics.Blit(_frameTex, _target);
        }

        public void ShowNotice(string text, Color color, float durationSeconds = 1.25f)
        {
            if (_noticeText == null || text.IsEmpty())
            {
                return;
            }

            _noticeText.text = text;
            _noticeText.color = color;
            _noticeText.enabled = true;
            _noticeUntil = Time.unscaledTime + Mathf.Max(0.2f, durationSeconds);
        }

        private void BuildUi()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var backdropGo = new GameObject("Backdrop");
            backdropGo.transform.SetParent(transform, false);
            _backdrop = backdropGo.AddComponent<Image>();
            _backdrop.color = new Color(0f, 0f, 0f, CurrentBackdropAlpha);
            var backdropRt = backdropGo.GetComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;

            var frameGo = new GameObject("Screen");
            frameGo.transform.SetParent(transform, false);
            _image = frameGo.AddComponent<RawImage>();
            _image.raycastTarget = false;
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            var scale = CurrentScale;
            frameRt.sizeDelta = new Vector2(Screen.width * scale, Screen.height * scale);

            var noticeGo = new GameObject("Notice");
            noticeGo.transform.SetParent(frameGo.transform, false);
            _noticeText = noticeGo.AddComponent<Text>();
            _noticeText.raycastTarget = false;
            _noticeText.alignment = TextAnchor.UpperCenter;
            _noticeText.fontSize = 24;
            _noticeText.fontStyle = FontStyle.Bold;
            _noticeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _noticeText.verticalOverflow = VerticalWrapMode.Overflow;
            _noticeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _noticeText.color = Color.white;
            _noticeText.enabled = false;

            var noticeRt = noticeGo.GetComponent<RectTransform>();
            noticeRt.anchorMin = new Vector2(0.05f, 0.72f);
            noticeRt.anchorMax = new Vector2(0.95f, 0.98f);
            noticeRt.offsetMin = Vector2.zero;
            noticeRt.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (_image == null || _target == null) return;

            if (_backdrop != null)
            {
                var c = _backdrop.color;
                var a = CurrentBackdropAlpha;
                if (!Mathf.Approximately(c.a, a))
                {
                    _backdrop.color = new Color(c.r, c.g, c.b, a);
                }
            }

            var rt = _image.rectTransform;
            var scale = CurrentScale;
            var maxW = Screen.width * scale;
            var maxH = Screen.height * scale;
            var texAspect = _target.width / (float)_target.height;
            var maxAspect = maxW / maxH;

            if (maxAspect > texAspect)
            {
                rt.sizeDelta = new Vector2(maxH * texAspect, maxH);
            }
            else
            {
                rt.sizeDelta = new Vector2(maxW, maxW / texAspect);
            }

            if (_noticeText != null && _noticeText.enabled && Time.unscaledTime >= _noticeUntil)
            {
                _noticeText.enabled = false;
                _noticeText.text = string.Empty;
            }
        }

        private void OnDestroy()
        {
            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }
            if (_frameTex != null) Destroy(_frameTex);
        }
    }
}
