using UnityEngine;
using UnityEngine.UI;

namespace Elin_ModTemplate
{
    public sealed class DoomOverlayDisplay : MonoBehaviour
    {
        private Canvas _canvas;
        private RawImage _image;
        private Image[] _shadowLayers;
        private readonly float[] _shadowSpreads = { 72f, 140f, 220f };
        private readonly float[] _shadowStrengths = { 0.75f, 0.45f, 0.25f };
        private Text _noticeText;
        private RectTransform _noticeRt;
        private Texture2D _frameTex;
        private RenderTexture _target;
        private float _noticeUntil;

        private float CurrentScale => Mathf.Clamp(ModConfig.OverlayScale?.Value ?? 0.60f, 0.30f, 0.95f);
        private float CurrentShadowAlpha => Mathf.Clamp01(ModConfig.BackdropAlpha?.Value ?? 0.55f);

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
            _noticeText.color = Color.white;
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

            var frameGo = new GameObject("Screen");
            frameGo.transform.SetParent(transform, false);
            _image = frameGo.AddComponent<RawImage>();
            _image.raycastTarget = false;
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);

            _shadowLayers = new Image[_shadowSpreads.Length];
            for (var i = 0; i < _shadowLayers.Length; i++)
            {
                var shadowGo = new GameObject("Shadow_" + i);
                shadowGo.transform.SetParent(transform, false);
                shadowGo.transform.SetSiblingIndex(frameGo.transform.GetSiblingIndex());
                var shadow = shadowGo.AddComponent<Image>();
                shadow.raycastTarget = false;
                shadow.color = new Color(0f, 0f, 0f, CurrentShadowAlpha * _shadowStrengths[i]);
                var srt = shadow.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f);
                srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                _shadowLayers[i] = shadow;
            }

            var noticeGo = new GameObject("Notice");
            noticeGo.transform.SetParent(transform, false);
            _noticeText = noticeGo.AddComponent<Text>();
            _noticeText.raycastTarget = false;
            _noticeText.alignment = TextAnchor.MiddleCenter;
            _noticeText.fontSize = 25;
            _noticeText.fontStyle = FontStyle.Bold;
            _noticeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _noticeText.verticalOverflow = VerticalWrapMode.Overflow;
            _noticeText.font = ResolveElinFont();
            _noticeText.color = Color.white;
            _noticeText.enabled = false;
            var outline = noticeGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(1f, -1f);

            _noticeRt = noticeGo.GetComponent<RectTransform>();
            _noticeRt.anchorMin = new Vector2(0.5f, 0.5f);
            _noticeRt.anchorMax = new Vector2(0.5f, 0.5f);
            _noticeRt.pivot = new Vector2(0.5f, 0.5f);
            _noticeRt.sizeDelta = new Vector2(700f, 72f);
        }

        private void Update()
        {
            if (_image == null || _target == null) return;

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

            if (_noticeRt != null)
            {
                _noticeRt.sizeDelta = new Vector2(rt.sizeDelta.x + 180f, 78f);
                _noticeRt.anchoredPosition = new Vector2(0f, -rt.sizeDelta.y * 0.5f - 34f);
            }

            if (_shadowLayers != null)
            {
                var shadowAlpha = CurrentShadowAlpha;
                for (var i = 0; i < _shadowLayers.Length; i++)
                {
                    var shadow = _shadowLayers[i];
                    if (shadow == null)
                    {
                        continue;
                    }

                    var c = shadow.color;
                    var targetA = shadowAlpha * _shadowStrengths[i];
                    if (!Mathf.Approximately(c.a, targetA))
                    {
                        shadow.color = new Color(0f, 0f, 0f, targetA);
                    }

                    var srt = shadow.rectTransform;
                    var spread = _shadowSpreads[i];
                    srt.sizeDelta = rt.sizeDelta + new Vector2(spread, spread);
                }
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

        private static Font ResolveElinFont()
        {
            try
            {
                var skin = SkinManager.Instance;
                var font = skin?.fontSet?.widget?.source?.font;
                if (font != null)
                {
                    return font;
                }
            }
            catch
            {
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
