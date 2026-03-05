using UnityEngine;
using UnityEngine.UI;

namespace Elin_ModTemplate
{
    public sealed class DoomOverlayDisplay : MonoBehaviour
    {
        private Canvas _canvas;
        private RawImage _image;
        private RawImage _crtNoise;
        private Image _crtFlash;
        private Image[] _shadowLayers;
        private readonly float[] _shadowSpreads = { 72f, 140f, 220f };
        private readonly float[] _shadowStrengths = { 0.75f, 0.45f, 0.25f };
        private Text _noticeText;
        private RectTransform _noticeRt;
        private Outline _noticeOutline;
        private Texture2D _frameTex;
        private float _noticeUntil;
        private float _noticeStart;
        private float _noticeDuration;
        private Color _noticeBaseColor = new Color(1f, 0.84f, 0.22f, 1f);
        private Vector2 _noticeBasePos;
        private const float NoticeEnterSeconds = 0.22f;
        private const float NoticeExitSeconds = 0.20f;
        private const float NoticeSlidePixels = 16f;
        private Texture2D _crtNoiseTex;
        private Texture2D _crtScanlineTex;
        private float _bootFxStart;
        private float _bootFxUntil;
        private const float BootFxDuration = 0.72f;

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

            BuildUi();
            _image.texture = _frameTex;
            StartBootEffect();
        }

        public void Upload(Color32[] pixels)
        {
            if (_frameTex == null || pixels == null || pixels.Length == 0) return;
            _frameTex.SetPixels32(pixels);
            _frameTex.Apply(false, false);
        }

        public void ShowNotice(string text, Color color, float durationSeconds = 1.25f)
        {
            if (_noticeText == null || text.IsEmpty())
            {
                return;
            }

            _noticeText.text = text;
            var baseColor = color.a > 0f ? color : _noticeBaseColor;
            var goldTint = new Color(1f, 0.82f, 0.20f, 1f);
            baseColor = Color.Lerp(baseColor, goldTint, 0.72f);
            baseColor = Color.Lerp(baseColor, Color.white, 0.20f);
            baseColor.a = 1f;
            _noticeBaseColor = baseColor;
            _noticeText.color = _noticeBaseColor;
            _noticeText.enabled = true;
            _noticeStart = Time.unscaledTime;
            _noticeDuration = Mathf.Max(0.30f, durationSeconds);
            _noticeUntil = _noticeStart + _noticeDuration;
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null)
            {
                _canvas.enabled = visible;
            }
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

            var noiseGo = new GameObject("CrtNoise");
            noiseGo.transform.SetParent(frameGo.transform, false);
            _crtNoise = noiseGo.AddComponent<RawImage>();
            _crtNoise.raycastTarget = false;
            _crtNoise.color = new Color(1f, 1f, 1f, 0f);
            _crtNoiseTex = BuildNoiseTexture(128, 72);
            _crtNoise.texture = _crtNoiseTex;
            var noiseRt = noiseGo.GetComponent<RectTransform>();
            noiseRt.anchorMin = Vector2.zero;
            noiseRt.anchorMax = Vector2.one;
            noiseRt.offsetMin = Vector2.zero;
            noiseRt.offsetMax = Vector2.zero;

            var flashGo = new GameObject("CrtFlash");
            flashGo.transform.SetParent(frameGo.transform, false);
            _crtFlash = flashGo.AddComponent<Image>();
            _crtFlash.raycastTarget = false;
            _crtScanlineTex = BuildScanlineTexture(4, 4);
            _crtFlash.sprite = Sprite.Create(_crtScanlineTex, new Rect(0, 0, _crtScanlineTex.width, _crtScanlineTex.height), new Vector2(0.5f, 0.5f));
            _crtFlash.type = Image.Type.Tiled;
            _crtFlash.color = new Color(1f, 0.95f, 0.75f, 0f);
            var flashRt = flashGo.GetComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;

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
            _noticeText.color = _noticeBaseColor;
            _noticeText.enabled = false;
            _noticeOutline = noticeGo.AddComponent<Outline>();
            _noticeOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            _noticeOutline.effectDistance = new Vector2(2f, -2f);

            _noticeRt = noticeGo.GetComponent<RectTransform>();
            _noticeRt.anchorMin = new Vector2(0.5f, 0.5f);
            _noticeRt.anchorMax = new Vector2(0.5f, 0.5f);
            _noticeRt.pivot = new Vector2(0.5f, 0.5f);
            _noticeRt.sizeDelta = new Vector2(700f, 72f);
        }

        private void Update()
        {
            if (_image == null || _frameTex == null) return;

            var rt = _image.rectTransform;
            var scale = CurrentScale;
            var maxW = Screen.width * scale;
            var maxH = Screen.height * scale;
            var texAspect = _frameTex.width / (float)_frameTex.height;
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
                _noticeBasePos = new Vector2(0f, -rt.sizeDelta.y * 0.5f - 34f);
                _noticeRt.anchoredPosition = _noticeBasePos;
            }

            UpdateBootEffect(rt);

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

            if (_noticeText != null && _noticeText.enabled)
            {
                var now = Time.unscaledTime;
                var elapsed = now - _noticeStart;
                var total = Mathf.Max(0.01f, _noticeDuration);
                var alpha = 1f;

                if (elapsed <= NoticeEnterSeconds)
                {
                    var t = Mathf.Clamp01(elapsed / NoticeEnterSeconds);
                    t = 1f - Mathf.Pow(1f - t, 3f); // ease-out
                    alpha = Mathf.Lerp(0f, 1f, t);
                    if (_noticeRt != null)
                    {
                        _noticeRt.anchoredPosition = _noticeBasePos + new Vector2(0f, (1f - t) * -NoticeSlidePixels);
                    }
                }
                else
                {
                    if (_noticeRt != null)
                    {
                        _noticeRt.anchoredPosition = _noticeBasePos;
                    }

                    var remaining = _noticeUntil - now;
                    if (remaining <= NoticeExitSeconds)
                    {
                        alpha = Mathf.Clamp01(remaining / NoticeExitSeconds);
                    }
                }

                var pulse = 1f + Mathf.Sin(now * 16f) * 0.06f;
                var display = _noticeBaseColor * pulse;
                display.a = alpha;
                _noticeText.color = display;

                if (_noticeOutline != null)
                {
                    var oc = _noticeOutline.effectColor;
                    oc.a = Mathf.Lerp(0.70f, 0.95f, alpha);
                    _noticeOutline.effectColor = oc;
                }

                if (now >= _noticeUntil)
                {
                    _noticeText.enabled = false;
                    _noticeText.text = string.Empty;
                }
            }
        }

        private void OnDestroy()
        {
            if (_frameTex != null) Destroy(_frameTex);
            if (_crtNoiseTex != null) Destroy(_crtNoiseTex);
            if (_crtScanlineTex != null) Destroy(_crtScanlineTex);
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

        private void StartBootEffect()
        {
            _bootFxStart = Time.unscaledTime;
            _bootFxUntil = _bootFxStart + BootFxDuration;
        }

        private void UpdateBootEffect(RectTransform frameRt)
        {
            if (_crtNoise == null || _crtFlash == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now >= _bootFxUntil)
            {
                _crtNoise.color = new Color(1f, 1f, 1f, 0f);
                _crtFlash.color = new Color(1f, 0.95f, 0.75f, 0f);
                _crtNoise.uvRect = new Rect(0f, 0f, 1f, 1f);
                frameRt.anchoredPosition = Vector2.zero;
                frameRt.localScale = Vector3.one;
                return;
            }

            var t = Mathf.Clamp01((now - _bootFxStart) / BootFxDuration);
            var alpha = (1f - t) * (1f - t);
            var jitter = (1f - t) * 2.4f;
            frameRt.anchoredPosition = new Vector2(Random.Range(-jitter, jitter), Random.Range(-jitter * 0.5f, jitter * 0.5f));

            // Soft CRT-like bloom/pop: expand quickly, then settle.
            var popPhase = Mathf.Clamp01(t / 0.56f);
            var settlePhase = Mathf.Clamp01((t - 0.56f) / 0.44f);
            var popEase = 1f - Mathf.Pow(1f - popPhase, 3f);
            var settleEase = settlePhase * settlePhase * (3f - 2f * settlePhase);
            var popScale = Mathf.Lerp(0.88f, 1.035f, popEase);
            var finalScale = Mathf.Lerp(popScale, 1f, settleEase);
            frameRt.localScale = new Vector3(finalScale, finalScale, 1f);

            _crtNoise.uvRect = new Rect(0f, now * 2.6f, 3f + (1f - t), 2f);
            _crtNoise.color = new Color(1f, 1f, 1f, 0.55f * alpha);
            _crtFlash.color = new Color(1f, 0.95f, 0.75f, 0.40f * alpha);
        }

        private static Texture2D BuildNoiseTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                var v = (byte)Random.Range(24, 255);
                pixels[i] = new Color32(v, v, v, 255);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D BuildScanlineTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var dark = (y % 2) == 0;
                var a = dark ? (byte)56 : (byte)4;
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color32(16, 12, 6, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }
    }
}
