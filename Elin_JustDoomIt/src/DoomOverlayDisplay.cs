using UnityEngine;
using UnityEngine.UI;

namespace Elin_JustDoomIt
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
        private Text _hudBetText;
        private Text _hudMultiText;
        private Text _hudPoolLabelText;
        private Text _hudPoolValueText;
        private Text _hudPoolCapText;
        private RectTransform _hudPoolBarRoot;
        private Image _hudPoolBarBg;
        private Image _hudPoolBarFill;
        private Image _hudPoolBarFillEdge;
        private Image _hudPoolBarGain;
        private Image _hudPoolBarLag;
        private Image _hudPoolBarRisk;
        private float _hudTargetPool;
        private float _hudCommittedPool;
        private float _hudDisplayedPool;
        private float _hudLagPool;
        private const float HudPoolSegmentSize = 10000f;
        private float _hudRiskLoss;
        private float _hudPendingGain;
        private float _hudPendingLoss;
        private float _hudGainPreviewStartPool;
        private float _hudPreviewStartPool;
        private bool _hudRoundActive;
        private float _hudMotionPauseUntil;
        private float _hudFlashUntil;
        private Color _hudFlashColor = Color.white;
        private float _hudGainPreviewUntil;
        private float _hudGainApplyAt;
        private float _hudDamagePreviewUntil;
        private float _hudDamageApplyAt;
        private Text _hudDeltaText;
        private RectTransform _hudDeltaTextRt;
        private float _hudDeltaTextStart;
        private float _hudDeltaTextUntil;
        private Vector2 _hudDeltaTextFrom;
        private Vector2 _hudDeltaTextTo;
        private Color _hudDeltaTextBaseColor = Color.white;
        private Image _noticeBg;
        private Text _noticeText;
        private RectTransform _noticeRt;
        private Outline _noticeOutline;
        private GameObject _rateSelectionRoot;
        private Text _rateSelectionTitle;
        private Text _rateSelectionHelp;
        private Text[] _rateSelectionRows;
        private Image[] _rateSelectionRowBackgrounds;
        private Outline[] _rateSelectionRowBorders;
        private Texture2D _frameTex;
        private float _noticeUntil;
        private float _noticeStart;
        private float _noticeDuration;
        private Color _noticeBaseColor = new Color(1f, 0.84f, 0.22f, 1f);
        private Vector2 _noticeBasePos;
        private const float NoticeEnterSeconds = 0.22f;
        private const float NoticeExitSeconds = 0.20f;
        private const float NoticeSlidePixels = 22f;
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

        public void ShowNotice(string text, Color color, float durationSeconds = 1.55f)
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
            _noticeDuration = Mathf.Max(0.55f, durationSeconds);
            _noticeUntil = _noticeStart + _noticeDuration;
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null)
            {
                _canvas.enabled = visible;
            }
        }

        public void ShowHudDelta(string text, Color color)
        {
            StartHudDeltaText(text, color);
        }

        public void SetHud(string betText, string rewardText, string poolLabelText, string poolText, int poolValue, int riskLoss, bool roundActive)
        {
            if (_hudBetText != null)
            {
                _hudBetText.text = betText ?? string.Empty;
            }

            if (_hudMultiText != null)
            {
                _hudMultiText.text = rewardText ?? string.Empty;
            }

            if (_hudPoolValueText != null)
            {
                _hudPoolValueText.text = poolText ?? string.Empty;
            }

            if (_hudPoolLabelText != null)
            {
                _hudPoolLabelText.text = poolLabelText ?? string.Empty;
            }

            var clampedPool = Mathf.Max(0, poolValue);
            var clampedRisk = Mathf.Max(0, riskLoss);
            if (clampedPool > _hudCommittedPool)
            {
                BeginGainPreview(clampedPool);
            }
            else if (clampedPool < _hudCommittedPool)
            {
                BeginLossPreview(clampedPool, clampedRisk);
            }
            else
            {
                _hudTargetPool = clampedPool;
            }

            _hudRiskLoss = roundActive ? Mathf.Min(Mathf.Max(clampedPool, _hudCommittedPool), clampedRisk) : 0f;
            _hudRoundActive = roundActive;
            if (!roundActive)
            {
                ResetHudPoolState();
            }
        }

        private void BeginGainPreview(float nextPool)
        {
            var now = Time.unscaledTime;
            CancelPendingLossPreview(now);
            _hudFlashColor = new Color(1f, 0.92f, 0.38f, 1f);
            _hudFlashUntil = now + 0.26f;
            _hudPendingGain = nextPool - _hudCommittedPool;
            _hudGainPreviewStartPool = Mathf.Max(_hudCommittedPool, _hudDisplayedPool, _hudTargetPool);
            _hudGainApplyAt = now + 0.16f;
            _hudGainPreviewUntil = now + 0.24f;
            _hudMotionPauseUntil = now + 0.06f;
            _hudCommittedPool = nextPool;
        }

        private void BeginLossPreview(float nextPool, float clampedRisk)
        {
            var now = Time.unscaledTime;
            _hudFlashColor = new Color(1f, 0.34f, 0.24f, 1f);
            _hudFlashUntil = now + 0.34f;
            _hudPendingLoss = Mathf.Max(_hudCommittedPool - nextPool, clampedRisk);
            _hudPendingLoss = Mathf.Min(_hudCommittedPool, _hudPendingLoss);
            _hudPreviewStartPool = Mathf.Max(_hudCommittedPool, _hudDisplayedPool, _hudTargetPool);
            _hudDamageApplyAt = now + 0.28f;
            _hudDamagePreviewUntil = now + 0.34f;
            _hudMotionPauseUntil = now + 0.06f;
            _hudCommittedPool = nextPool;
        }

        private void CancelPendingLossPreview(float now)
        {
            if (_hudPendingLoss <= 0f || now >= _hudDamageApplyAt)
            {
                return;
            }

            _hudTargetPool = _hudCommittedPool;
            _hudPendingLoss = 0f;
            _hudPreviewStartPool = 0f;
            _hudDamageApplyAt = 0f;
            _hudDamagePreviewUntil = 0f;
        }

        private void ResetHudPoolState()
        {
            _hudCommittedPool = 0f;
            _hudTargetPool = 0f;
            _hudDisplayedPool = 0f;
            _hudLagPool = 0f;
            _hudRiskLoss = 0f;
            _hudPendingGain = 0f;
            _hudPendingLoss = 0f;
            _hudGainPreviewStartPool = 0f;
            _hudPreviewStartPool = 0f;
            _hudGainApplyAt = 0f;
            _hudGainPreviewUntil = 0f;
            _hudDamageApplyAt = 0f;
            _hudDamagePreviewUntil = 0f;
            _hudMotionPauseUntil = 0f;
            _hudDeltaTextUntil = 0f;
            if (_hudDeltaText != null)
            {
                _hudDeltaText.enabled = false;
                _hudDeltaText.text = string.Empty;
            }
        }

        private void StartHudDeltaText(string text, Color color)
        {
            if (_hudDeltaText == null || text.IsEmpty())
            {
                return;
            }

            var now = Time.unscaledTime;
            _hudDeltaText.text = text;
            _hudDeltaTextBaseColor = color;
            _hudDeltaTextStart = now;
            _hudDeltaTextUntil = now + 0.52f;
            _hudDeltaTextFrom = new Vector2(0f, -10f);
            _hudDeltaTextTo = new Vector2(0f, -20f);
            _hudDeltaText.enabled = true;
        }

        public void ShowRateSelection(string title, string helperText, string[] options, int selectedIndex)
        {
            if (_rateSelectionRoot == null)
            {
                return;
            }

            _rateSelectionRoot.SetActive(true);
            if (_rateSelectionTitle != null)
            {
                _rateSelectionTitle.text = title ?? string.Empty;
            }

            if (_rateSelectionHelp != null)
            {
                _rateSelectionHelp.text = helperText ?? string.Empty;
            }

            for (var i = 0; i < _rateSelectionRows.Length; i++)
            {
                if (_rateSelectionRows[i] == null)
                {
                    continue;
                }

                var option = options != null && i < options.Length ? options[i] : string.Empty;
                var selected = i == selectedIndex;
                _rateSelectionRows[i].text = option;
                _rateSelectionRows[i].color = selected
                    ? new Color(1f, 0.96f, 0.72f, 1f)
                    : new Color(0.86f, 0.86f, 0.88f, 1f);
                _rateSelectionRows[i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;

                if (_rateSelectionRowBackgrounds != null && i < _rateSelectionRowBackgrounds.Length && _rateSelectionRowBackgrounds[i] != null)
                {
                    _rateSelectionRowBackgrounds[i].color = selected
                        ? new Color(0.30f, 0.16f, 0.06f, 0.96f)
                        : new Color(0.08f, 0.09f, 0.12f, 0.90f);
                }

                if (_rateSelectionRowBorders != null && i < _rateSelectionRowBorders.Length && _rateSelectionRowBorders[i] != null)
                {
                    _rateSelectionRowBorders[i].effectColor = selected
                        ? new Color(1f, 0.46f, 0.12f, 0.90f)
                        : new Color(0.18f, 0.22f, 0.28f, 0.70f);
                    _rateSelectionRowBorders[i].effectDistance = selected
                        ? new Vector2(2f, -2f)
                        : new Vector2(1f, -1f);
                }
            }
        }

        public void HideRateSelection()
        {
            if (_rateSelectionRoot != null)
            {
                _rateSelectionRoot.SetActive(false);
            }
        }

        public int GetRateSelectionHoverIndex(Vector2 screenPosition)
        {
            if (_rateSelectionRoot == null || !_rateSelectionRoot.activeInHierarchy || _rateSelectionRows == null)
            {
                return -1;
            }

            for (var i = 0; i < _rateSelectionRows.Length; i++)
            {
                var row = _rateSelectionRows[i];
                if (row == null)
                {
                    continue;
                }

                var rowRt = row.GetComponent<RectTransform>();
                if (rowRt != null && RectTransformUtility.RectangleContainsScreenPoint(rowRt, screenPosition, null))
                {
                    return i;
                }
            }

            return -1;
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

            BuildHud();
            BuildRateSelectionPanel();

            var noticeGo = new GameObject("Notice");
            noticeGo.transform.SetParent(transform, false);
            _noticeBg = noticeGo.AddComponent<Image>();
            _noticeBg.raycastTarget = false;
            _noticeBg.color = new Color(0.03f, 0.03f, 0.05f, 0f);
            var noticeBorder = noticeGo.AddComponent<Outline>();
            noticeBorder.effectColor = new Color(1f, 0.28f, 0.08f, 0.50f);
            noticeBorder.effectDistance = new Vector2(2f, -2f);

            var noticeTextGo = new GameObject("Text");
            noticeTextGo.transform.SetParent(noticeGo.transform, false);
            _noticeText = noticeTextGo.AddComponent<Text>();
            _noticeText.raycastTarget = false;
            _noticeText.alignment = TextAnchor.MiddleCenter;
            _noticeText.fontSize = 25;
            _noticeText.fontStyle = FontStyle.Bold;
            _noticeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _noticeText.verticalOverflow = VerticalWrapMode.Overflow;
            _noticeText.font = ResolveElinFont();
            _noticeText.color = _noticeBaseColor;
            _noticeText.enabled = false;
            _noticeOutline = noticeTextGo.AddComponent<Outline>();
            _noticeOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            _noticeOutline.effectDistance = new Vector2(3f, -3f);

            _noticeRt = noticeGo.GetComponent<RectTransform>();
            _noticeRt.anchorMin = new Vector2(0.5f, 0.5f);
            _noticeRt.anchorMax = new Vector2(0.5f, 0.5f);
            _noticeRt.pivot = new Vector2(0.5f, 0.5f);
            _noticeRt.sizeDelta = new Vector2(540f, 70f);
            var noticeTextRt = _noticeText.GetComponent<RectTransform>();
            noticeTextRt.anchorMin = new Vector2(0f, 0f);
            noticeTextRt.anchorMax = new Vector2(1f, 1f);
            noticeTextRt.offsetMin = new Vector2(18f, 8f);
            noticeTextRt.offsetMax = new Vector2(-18f, -8f);
        }

        private void BuildHud()
        {
            var root = new GameObject("HudRoot");
            root.transform.SetParent(transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.sizeDelta = new Vector2(640f, 48f);

            var bg = root.AddComponent<Image>();
            bg.raycastTarget = false;
            bg.color = new Color(0.03f, 0.03f, 0.05f, 0.88f);
            var frameOutline = root.AddComponent<Outline>();
            frameOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            frameOutline.effectDistance = new Vector2(2f, -2f);
            CreateHudLine(root.transform, "HudTopLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f, 0f), new Color(1f, 0.34f, 0.10f, 0.96f));
            CreateHudLine(root.transform, "HudBottomLine", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Color(1f, 0.22f, 0.08f, 0.80f));

            _hudBetText = CreateHudText(root.transform, "HudBet", Vector2.zero, 18, new Color(0.95f, 0.88f, 0.62f, 1f));
            _hudBetText.alignment = TextAnchor.MiddleLeft;
            _hudBetText.resizeTextForBestFit = true;
            _hudBetText.resizeTextMinSize = 14;
            _hudBetText.resizeTextMaxSize = 18;
            var betRt = _hudBetText.GetComponent<RectTransform>();
            betRt.anchorMin = new Vector2(0f, 0.5f);
            betRt.anchorMax = new Vector2(0f, 0.5f);
            betRt.pivot = new Vector2(0f, 0.5f);
            betRt.anchoredPosition = new Vector2(12f, 0f);
            betRt.sizeDelta = new Vector2(220f, 24f);

            _hudMultiText = CreateHudText(root.transform, "HudMulti", Vector2.zero, 18, new Color(1f, 0.92f, 0.45f, 1f));
            _hudMultiText.alignment = TextAnchor.MiddleRight;
            _hudMultiText.resizeTextForBestFit = true;
            _hudMultiText.resizeTextMinSize = 14;
            _hudMultiText.resizeTextMaxSize = 18;
            var multiRt = _hudMultiText.GetComponent<RectTransform>();
            multiRt.anchorMin = new Vector2(1f, 0.5f);
            multiRt.anchorMax = new Vector2(1f, 0.5f);
            multiRt.pivot = new Vector2(1f, 0.5f);
            multiRt.anchoredPosition = new Vector2(-12f, 0f);
            multiRt.sizeDelta = new Vector2(210f, 24f);

            var barRoot = new GameObject("HudPoolBar");
            barRoot.transform.SetParent(root.transform, false);
            _hudPoolBarRoot = barRoot.AddComponent<RectTransform>();
            _hudPoolBarRoot.anchorMin = new Vector2(0f, 0.5f);
            _hudPoolBarRoot.anchorMax = new Vector2(1f, 0.5f);
            _hudPoolBarRoot.pivot = new Vector2(0.5f, 0.5f);
            _hudPoolBarRoot.offsetMin = new Vector2(238f, -11f);
            _hudPoolBarRoot.offsetMax = new Vector2(-228f, 11f);

            _hudPoolBarBg = barRoot.AddComponent<Image>();
            _hudPoolBarBg.raycastTarget = false;
            _hudPoolBarBg.color = new Color(0.05f, 0.05f, 0.07f, 0.78f);
            var bgOutline = barRoot.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            bgOutline.effectDistance = new Vector2(2f, -2f);

            CreateBarTicks(barRoot.transform, 6);
            _hudPoolBarGain = CreateBarRect(barRoot.transform, "Gain", new Color(1f, 0.96f, 0.72f, 0.92f));
            _hudPoolBarLag = CreateBarRect(barRoot.transform, "Lag", new Color(0.90f, 0.26f, 0.18f, 0.64f));
            _hudPoolBarFill = CreateBarRect(barRoot.transform, "Fill", new Color(1f, 0.76f, 0.14f, 0.96f));
            _hudPoolBarFillEdge = CreateBarRect(barRoot.transform, "FillEdge", new Color(1f, 0.96f, 0.72f, 0.96f));
            _hudPoolBarRisk = CreateBarRect(barRoot.transform, "Risk", new Color(1f, 0.14f, 0.14f, 0.82f));
            _hudPoolBarGain.transform.SetAsLastSibling();
            _hudPoolBarFillEdge.transform.SetAsLastSibling();
            _hudPoolBarRisk.transform.SetAsLastSibling();

            _hudPoolLabelText = CreateHudText(barRoot.transform, "HudPoolLabel", Vector2.zero, 13, new Color(0.98f, 0.88f, 0.42f, 1f));
            _hudPoolLabelText.alignment = TextAnchor.MiddleLeft;
            _hudPoolLabelText.text = "-";
            var labelRt = _hudPoolLabelText.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(0f, 0.5f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.anchoredPosition = new Vector2(10f, 0f);
            labelRt.sizeDelta = new Vector2(106f, 18f);

            _hudPoolValueText = CreateHudText(barRoot.transform, "HudPoolValue", Vector2.zero, 18, Color.white);
            _hudPoolValueText.alignment = TextAnchor.MiddleCenter;
            var valueRt = _hudPoolValueText.GetComponent<RectTransform>();
            valueRt.anchorMin = new Vector2(0f, 0.5f);
            valueRt.anchorMax = new Vector2(1f, 0.5f);
            valueRt.pivot = new Vector2(0.5f, 0.5f);
            valueRt.anchoredPosition = Vector2.zero;
            valueRt.offsetMin = new Vector2(116f, -11f);
            valueRt.offsetMax = new Vector2(-10f, 11f);

            _hudPoolCapText = CreateHudText(barRoot.transform, "HudPoolCap", Vector2.zero, 12, new Color(1f, 0.86f, 0.42f, 1f));
            _hudPoolCapText.alignment = TextAnchor.MiddleRight;
            var capRt = _hudPoolCapText.GetComponent<RectTransform>();
            capRt.anchorMin = new Vector2(1f, 0.5f);
            capRt.anchorMax = new Vector2(1f, 0.5f);
            capRt.pivot = new Vector2(1f, 0.5f);
            capRt.anchoredPosition = new Vector2(-10f, 0f);
            capRt.sizeDelta = new Vector2(88f, 18f);
            _hudPoolCapText.text = string.Empty;

            _hudDeltaText = CreateHudText(root.transform, "HudDeltaText", Vector2.zero, 19, Color.white);
            _hudDeltaText.alignment = TextAnchor.MiddleCenter;
            _hudDeltaText.enabled = false;
            _hudDeltaTextRt = _hudDeltaText.GetComponent<RectTransform>();
            _hudDeltaTextRt.anchorMin = new Vector2(0.5f, 0f);
            _hudDeltaTextRt.anchorMax = new Vector2(0.5f, 0f);
            _hudDeltaTextRt.pivot = new Vector2(0.5f, 0.5f);
            _hudDeltaTextRt.anchoredPosition = new Vector2(0f, -20f);
            _hudDeltaTextRt.sizeDelta = new Vector2(280f, 26f);

            SetHud("-", "-", "-", "-", 0, 0, false);
        }

        private Image CreateBarRect(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            return image;
        }

        private static void CreateBarTicks(Transform parent, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var go = new GameObject("Tick_" + i);
                go.transform.SetParent(parent, false);
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.color = new Color(1f, 0.70f, 0.18f, 0.14f);
                var rt = go.GetComponent<RectTransform>();
                var x = i / (float)count;
                rt.anchorMin = new Vector2(x, 0.5f);
                rt.anchorMax = new Vector2(x, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(1.5f, 18f);
            }
        }

        private static void CreateHudLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private Text CreateHudText(Transform parent, string name, Vector2 anchoredPosition, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = ResolveElinFont();
            text.color = color;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(172f, 28f);
            return text;
        }

        private void BuildRateSelectionPanel()
        {
            _rateSelectionRoot = new GameObject("RateSelection");
            _rateSelectionRoot.transform.SetParent(transform, false);
            var rootRt = _rateSelectionRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(980f, 430f);

            var bg = _rateSelectionRoot.AddComponent<Image>();
            bg.raycastTarget = true;
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.94f);

            var border = _rateSelectionRoot.AddComponent<Outline>();
            border.effectColor = new Color(1f, 0.45f, 0.12f, 0.85f);
            border.effectDistance = new Vector2(2f, -2f);

            CreateHudLine(_rateSelectionRoot.transform, "RateTopLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -4f), new Vector2(-18f, -2f), new Color(1f, 0.34f, 0.10f, 0.92f));
            CreateHudLine(_rateSelectionRoot.transform, "RateBottomLine", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 2f), new Vector2(-18f, 4f), new Color(1f, 0.22f, 0.08f, 0.72f));

            _rateSelectionTitle = CreatePanelText(_rateSelectionRoot.transform, "RateTitle", 28, FontStyle.Bold, new Color(1f, 0.88f, 0.35f, 1f));
            var titleRt = _rateSelectionTitle.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -24f);
            titleRt.sizeDelta = new Vector2(-40f, 38f);
            _rateSelectionTitle.alignment = TextAnchor.MiddleCenter;

            _rateSelectionHelp = CreatePanelText(_rateSelectionRoot.transform, "RateHelp", 19, FontStyle.Normal, new Color(0.82f, 0.90f, 1f, 1f));
            var helpRt = _rateSelectionHelp.GetComponent<RectTransform>();
            helpRt.anchorMin = new Vector2(0f, 1f);
            helpRt.anchorMax = new Vector2(1f, 1f);
            helpRt.pivot = new Vector2(0.5f, 1f);
            helpRt.anchoredPosition = new Vector2(0f, -72f);
            helpRt.sizeDelta = new Vector2(-84f, 52f);
            _rateSelectionHelp.alignment = TextAnchor.MiddleCenter;
            _rateSelectionHelp.horizontalOverflow = HorizontalWrapMode.Wrap;
            _rateSelectionHelp.verticalOverflow = VerticalWrapMode.Overflow;

            _rateSelectionRows = new Text[3];
            _rateSelectionRowBackgrounds = new Image[3];
            _rateSelectionRowBorders = new Outline[3];
            for (var i = 0; i < _rateSelectionRows.Length; i++)
            {
                var card = new GameObject("RateCard_" + i);
                card.transform.SetParent(_rateSelectionRoot.transform, false);
                var cardRt = card.AddComponent<RectTransform>();
                cardRt.anchorMin = new Vector2(0f, 1f);
                cardRt.anchorMax = new Vector2(1f, 1f);
                cardRt.pivot = new Vector2(0.5f, 1f);
                cardRt.anchoredPosition = new Vector2(0f, -138f - i * 86f);
                cardRt.sizeDelta = new Vector2(-56f, 72f);

                var cardBg = card.AddComponent<Image>();
                cardBg.raycastTarget = false;
                cardBg.color = new Color(0.08f, 0.09f, 0.12f, 0.90f);
                var cardBorder = card.AddComponent<Outline>();
                cardBorder.effectColor = new Color(0.18f, 0.22f, 0.28f, 0.70f);
                cardBorder.effectDistance = new Vector2(1f, -1f);

                var accent = new GameObject("Accent");
                accent.transform.SetParent(card.transform, false);
                var accentImage = accent.AddComponent<Image>();
                accentImage.raycastTarget = false;
                accentImage.color = new Color(1f, 0.35f, 0.10f, 0.88f);
                var accentRt = accent.GetComponent<RectTransform>();
                accentRt.anchorMin = new Vector2(0f, 0f);
                accentRt.anchorMax = new Vector2(0f, 1f);
                accentRt.pivot = new Vector2(0f, 0.5f);
                accentRt.anchoredPosition = Vector2.zero;
                accentRt.sizeDelta = new Vector2(8f, 0f);

                var row = CreatePanelText(card.transform, "RateRow_" + i, 23, FontStyle.Normal, Color.white);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0f, 0f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 0.5f);
                rowRt.offsetMin = new Vector2(22f, 10f);
                rowRt.offsetMax = new Vector2(-18f, -10f);
                row.alignment = TextAnchor.MiddleLeft;
                row.horizontalOverflow = HorizontalWrapMode.Wrap;
                row.verticalOverflow = VerticalWrapMode.Overflow;
                _rateSelectionRows[i] = row;
                _rateSelectionRowBackgrounds[i] = cardBg;
                _rateSelectionRowBorders[i] = cardBorder;
            }

            _rateSelectionRoot.SetActive(false);
        }

        private Text CreatePanelText(Transform parent, string name, int fontSize, FontStyle fontStyle, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.raycastTarget = false;
            text.font = ResolveElinFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
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
                _noticeRt.sizeDelta = new Vector2(Mathf.Min(rt.sizeDelta.x * 0.62f, 640f), 70f);
                _noticeBasePos = new Vector2(0f, rt.sizeDelta.y * 0.5f - 82f);
                _noticeRt.anchoredPosition = _noticeBasePos;
            }

            UpdateHudLayout(rt);

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
                if (_noticeBg != null)
                {
                    var bg = _noticeBg.color;
                    bg.a = alpha * 0.58f;
                    _noticeBg.color = bg;
                }

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
                    if (_noticeBg != null)
                    {
                        var bg = _noticeBg.color;
                        bg.a = 0f;
                        _noticeBg.color = bg;
                    }
                }
            }
        }

        private void UpdateHudLayout(RectTransform frameRt)
        {
            if (_hudBetText == null)
            {
                return;
            }

            var hudRoot = _hudBetText.transform.parent as RectTransform;
            if (hudRoot != null)
            {
                var hudWidth = Mathf.Clamp(frameRt.sizeDelta.x * 0.98f, 520f, 900f);
                hudRoot.sizeDelta = new Vector2(hudWidth, 48f);
                hudRoot.anchoredPosition = new Vector2(0f, frameRt.sizeDelta.y * 0.5f + 10f);
            }

            if (_rateSelectionRoot != null)
            {
                var rateRt = _rateSelectionRoot.GetComponent<RectTransform>();
                rateRt.anchoredPosition = new Vector2(0f, 0f);
            }

            var now = Time.unscaledTime;
            ApplyPendingHudTransitions(now);
            UpdateHudScale(now);
            UpdateHudPoolMotion(now);
            UpdateHudFillVisual(now);
            UpdateHudLagVisual();
            UpdateHudGainVisual(now);
            UpdateHudLossVisual(now);
            UpdateHudDeltaTextVisual(now);
        }

        private void ApplyPendingHudTransitions(float now)
        {
            if (_hudPendingGain > 0f && now >= _hudGainApplyAt)
            {
                _hudTargetPool = _hudCommittedPool;
                _hudPendingGain = 0f;
                _hudGainPreviewStartPool = 0f;
            }

            if (_hudPendingLoss > 0f && now >= _hudDamageApplyAt)
            {
                _hudTargetPool = _hudCommittedPool;
                _hudLagPool = Mathf.Max(_hudLagPool, _hudDisplayedPool, _hudPreviewStartPool);
                _hudPendingLoss = 0f;
                _hudPreviewStartPool = 0f;
            }
        }

        private void UpdateHudScale(float now)
        {
            _ = now;
        }

        private void UpdateHudPoolMotion(float now)
        {
            if (now < _hudMotionPauseUntil)
            {
                return;
            }

            var riseSpeed = 12000f * Time.unscaledDeltaTime;
            var fallSpeed = 8200f * Time.unscaledDeltaTime;
            _hudDisplayedPool = _hudDisplayedPool < _hudTargetPool
                ? Mathf.Min(_hudDisplayedPool + riseSpeed, _hudTargetPool)
                : Mathf.Max(_hudDisplayedPool - fallSpeed, _hudTargetPool);
            _hudLagPool = Mathf.Max(_hudDisplayedPool, Mathf.Lerp(_hudLagPool, _hudTargetPool, Time.unscaledDeltaTime * 2.4f));
        }

        private void UpdateHudFillVisual(float now)
        {
            if (_hudPoolBarFill == null)
            {
                return;
            }

            var fillWidth = GetBarWidth(_hudDisplayedPool);
            SetBarWidth(_hudPoolBarFill, 2f, fillWidth, 18f);
            var baseFill = new Color(1f, 0.76f, 0.14f, _hudRoundActive ? 0.96f : 0.18f);
            if (now < _hudFlashUntil)
            {
                var pulse = Mathf.Clamp01((_hudFlashUntil - now) / 0.22f);
                _hudPoolBarFill.color = Color.Lerp(baseFill, _hudFlashColor, pulse);
            }
            else
            {
                _hudPoolBarFill.color = baseFill;
            }

            if (_hudPoolBarFillEdge == null)
            {
                return;
            }

            var edgeBoost = now < _hudFlashUntil ? 8f : 4f;
            SetBarWidth(_hudPoolBarFillEdge, Mathf.Max(2f, fillWidth - 2f), fillWidth > 8f ? edgeBoost : 0f, 18f);
            _hudPoolBarFillEdge.enabled = fillWidth > 8f;
            var edgeColor = _hudPoolBarFill.color;
            edgeColor = Color.Lerp(edgeColor, Color.white, 0.65f);
            edgeColor.a = _hudPoolBarFill.color.a;
            _hudPoolBarFillEdge.color = edgeColor;

            if (_hudPoolCapText != null)
            {
                var completed = GetCompletedSegments(_hudDisplayedPool);
                _hudPoolCapText.text = completed > 0 ? "MAX x" + completed : string.Empty;
            }
        }

        private void UpdateHudLagVisual()
        {
            if (_hudPoolBarLag == null)
            {
                return;
            }

            var lagWidth = GetBarWidth(_hudLagPool);
            SetBarWidth(_hudPoolBarLag, 2f, lagWidth, 18f);
            _hudPoolBarLag.enabled = _hudLagPool > _hudDisplayedPool + 2f && lagWidth > 2f;
        }

        private void UpdateHudGainVisual(float now)
        {
            if (_hudPoolBarGain == null)
            {
                return;
            }

            if (_hudRoundActive && _hudPendingGain > 0f && now < _hudGainPreviewUntil)
            {
                var start = GetBarAnchor(_hudGainPreviewStartPool);
                var width = Mathf.Min(GetBarWidth(_hudPendingGain), GetAvailableBarWidth(start));
                SetBarAnchorWidth(_hudPoolBarGain, start, width, 18f);
                var blink = 0.55f + Mathf.PingPong(now * 12f, 0.35f);
                var c = _hudPoolBarGain.color;
                c.a = blink;
                _hudPoolBarGain.color = c;
                _hudPoolBarGain.enabled = true;
            }
            else
            {
                _hudPoolBarGain.enabled = false;
            }
        }

        private void UpdateHudLossVisual(float now)
        {
            if (_hudPoolBarRisk == null)
            {
                return;
            }

            if (_hudRoundActive && _hudPendingLoss > 0f && now < _hudDamagePreviewUntil && _hudPreviewStartPool > 0f)
            {
                var start = Mathf.Clamp01(GetBarAnchor(_hudPreviewStartPool) - (_hudPendingLoss / HudPoolSegmentSize));
                var width = Mathf.Min(GetBarWidth(_hudPendingLoss), GetAvailableBarWidth(start));
                SetBarAnchorWidth(_hudPoolBarRisk, start, width, 18f);
                var blink = 0.45f + Mathf.PingPong(now * 10f, 0.55f);
                var c = _hudPoolBarRisk.color;
                c.a = blink;
                _hudPoolBarRisk.color = c;
                _hudPoolBarRisk.enabled = true;
            }
            else
            {
                _hudPoolBarRisk.enabled = false;
            }
        }

        private void UpdateHudDeltaTextVisual(float now)
        {
            if (_hudDeltaText == null || _hudDeltaTextRt == null)
            {
                return;
            }

            if (!_hudDeltaText.enabled)
            {
                return;
            }

            if (now >= _hudDeltaTextUntil)
            {
                _hudDeltaText.enabled = false;
                _hudDeltaText.text = string.Empty;
                return;
            }

            var t = Mathf.Clamp01((now - _hudDeltaTextStart) / Mathf.Max(0.01f, _hudDeltaTextUntil - _hudDeltaTextStart));
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            _hudDeltaTextRt.anchoredPosition = Vector2.Lerp(_hudDeltaTextFrom, _hudDeltaTextTo, eased);
            var c = Color.Lerp(_hudDeltaTextBaseColor, Color.white, 0.18f);
            c.a = 1f - t * 0.18f;
            _hudDeltaText.color = c;
        }

        private float GetBarWidth(float value)
        {
            return Mathf.Max(0f, Mathf.Clamp01(GetSegmentDisplayValue(value) / HudPoolSegmentSize) * (_hudPoolBarRoot?.rect.width ?? 0f) - 4f);
        }

        private float GetBarAnchor(float value)
        {
            return Mathf.Clamp01(GetSegmentDisplayValue(value) / HudPoolSegmentSize);
        }

        private float GetAvailableBarWidth(float anchor)
        {
            return Mathf.Max(0f, ((_hudPoolBarRoot?.rect.width ?? 0f) - 4f) * (1f - Mathf.Clamp01(anchor)));
        }

        private static float GetSegmentDisplayValue(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            var rem = value % HudPoolSegmentSize;
            return rem <= 0.001f ? HudPoolSegmentSize : rem;
        }

        private static int GetCompletedSegments(float value)
        {
            return value <= 0f ? 0 : Mathf.FloorToInt(value / HudPoolSegmentSize);
        }

        private static void SetBarWidth(Image image, float x, float width, float height)
        {
            if (image == null)
            {
                return;
            }

            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void SetBarAnchorWidth(Image image, float anchor, float width, float height)
        {
            if (image == null)
            {
                return;
            }

            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(anchor, 0.5f);
            rt.anchorMax = new Vector2(anchor, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, height);
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

