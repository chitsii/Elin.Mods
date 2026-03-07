using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_ModTemplate
{
    public sealed class DoomArcadeMenuUI : MonoBehaviour
    {
        private enum MenuState { Main, IwadPicker, PwadPicker, SkillPicker, ModRuleConfirm, ExitConfirm }
        private enum RowKind { Setting, Action, Separator }
        private enum RowTag
        {
            None,
            Back,
            Play,
            Continue,
            Iwad,
            Mods,
            OpenModFolder,
            Close,
            NewRun,
            Skill,
            RuleUseAuto,
            RuleDoom1,
            RuleDoom2,
            RuleAny,
            RulePending,
            RuleSkip
        }

        private struct RowEntry
        {
            public GameObject Go;
            public Text CursorText;
            public Text LabelText;
            public RowKind Kind;
            public bool Enabled;
            public RectTransform Rect;
            public RowTag Tag;
        }

        // ── Palette ──
        private static readonly Color ColBgPanel = new Color(10 / 255f, 10 / 255f, 15 / 255f, 0.97f);
        private static readonly Color ColDim = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color ColAccent = new Color(1f, 0.27f, 0f); // #FF4500
        private static readonly Color ColAccentDim = new Color(1f, 0.27f, 0f, 0.80f);
        private static readonly Color ColValue = new Color(1f, 0.80f, 0f); // #FFCC00
        private static readonly Color ColNormal = new Color(0.87f, 0.87f, 0.87f); // #DDDDDD
        private static readonly Color ColSelected = Color.white;
        private static readonly Color ColCursor = new Color(1f, 0.40f, 0f); // #FF6600
        private static readonly Color ColDisabled = new Color(0.33f, 0.33f, 0.33f); // #555555
        private static readonly Color ColModOn = new Color(0.27f, 1f, 0.53f); // #44FF88
        private static readonly Color ColModOff = new Color(0.67f, 0.67f, 0.67f); // #AAAAAA
        private static readonly Color ColIwadSel = new Color(1f, 1f, 0f); // #FFFF00
        private static readonly Color ColMeter = new Color(1f, 0.53f, 0f); // #FF8800
        private static readonly Color ColMeterBg = new Color(0.20f, 0.20f, 0.20f);
        private static readonly Color ColFooter = new Color(0.40f, 0.40f, 0.40f); // #666666
        private static readonly Color ColBorder = new Color(1f, 0.27f, 0f, 0.60f);
        private static readonly Color ColOverlayBg = new Color(5 / 255f, 5 / 255f, 8 / 255f, 0.98f);
        private static readonly Color ColFlashRed = new Color(1f, 0.2f, 0.2f);
        private static readonly Color ColAmber = new Color(1f, 0.75f, 0.15f);

        private const float PanelWidth = 680f;
        private const float PanelHeight = 520f;
        private const float RowHeight = 28f;
        private const float RowSpacing = 4f;
        private const int FontSizeTitle = 28;
        private const int FontSizeSection = 18;
        private const int FontSizeRow = 20;
        private const int FontSizeFooter = 12;
        private const int FontSizeOverlay = 20;

        // ── State ──
        private MenuState _state = MenuState.Main;
        private int _cursor;
        private readonly List<RowEntry> _rows = new List<RowEntry>();
        private DoomRuntimeLoadout _loadout;
        private List<DoomWadEntry> _iwads;
        private List<DoomWadEntry> _pwads;
        private Chara _user;
        private Action<bool> _onPlay;
        private Action _onClose;
        private int _overlayCursor;
        private bool _hasSave;
        private DoomSaveSummary _saveSummary;
        private bool _hasSaveSummary;
        private readonly Queue<DoomModRulePrompt> _pendingRulePrompts = new Queue<DoomModRulePrompt>();
        private readonly Dictionary<int, int> _pwadRowToIndex = new Dictionary<int, int>();
        private DoomModRulePrompt _activeRulePrompt;
        private bool _hasActiveRulePrompt;
        private MenuState _modRuleReturnState = MenuState.Main;

        // ── UI refs ──
        private Canvas _canvas;
        private Image _dimImage;
        private Image _panelImage;
        private Text _titleText;
        private Text _sectionHeader;
        private Text _statusLine;
        private Text _footerText;
        private RectTransform _listRoot;
        private Image _slotMeter;
        private Image _slotMeterBg;
        private Text _slotMeterText;
        private GameObject _overlayPanel;
        private Text _overlayTitle;
        private Text _overlayInfo;
        private Text _overlayRow0;
        private Text _overlayRow1;
        private Text _overlayCursorText0;
        private Text _overlayCursorText1;
        private Image _crtScanlines;
        private RawImage _crtNoise;
        private Texture2D _crtNoiseTex;
        private Texture2D _crtScanlineTex;
        private Font _font;
        private float _bootTime;

        // ── Menu BGM ──
        private bool _menuBgmActive;
        private int _menuBgmIndex;
        private float _nextMenuBgmRetryAt;
        private static readonly string[] MenuBgmIds =
        {
            "BGM/doom_themed_alien",
            "BGM/doom_themed_boss",
            "BGM/doom_themed_hell",
            "BGM/doom_themed_industrial",
            "BGM/doom_themed_labo"
        };

        public static DoomArcadeMenuUI Create()
        {
            var go = new GameObject("JustDoomIt_ArcadeMenu");
            DontDestroyOnLoad(go);
            return go.AddComponent<DoomArcadeMenuUI>();
        }

        public void Show(DoomRuntimeLoadout loadout, Chara user,
                         Action<bool> onPlay, Action onClose)
        {
            _loadout = loadout;
            _user = user;
            _onPlay = onPlay;
            _onClose = onClose;
            DoomModRuleStore.EnsureLoaded();
            _iwads = DoomWadLocator.FindIwads();
            _pwads = DoomWadLocator.FindPwads();
            NormalizeSingleActiveMod(_loadout);
            _bootTime = Time.unscaledTime;
            _canvas.enabled = true;
            EInput.Consume(consumeAxis: true, _skipFrame: 2);
            StartMenuBgm();
            TransitionTo(MenuState.Main);
        }

        private bool RecomputeHasSave()
        {
            try
            {
                var launch = DoomWadLocator.BuildLaunchConfig(_loadout);
                _hasSaveSummary = false;
                _saveSummary = default;
                if (string.IsNullOrWhiteSpace(launch.SaveSlotKey))
                {
                    return false;
                }

                var hasSave = DoomPersistentSaveStore.HasSave(launch.SaveSlotKey);
                if (hasSave)
                {
                    _hasSaveSummary = DoomPersistentSaveStore.TryLoadSummary(launch.SaveSlotKey, out _saveSummary);
                }

                return hasSave;
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] RecomputeHasSave failed: " + ex.Message);
                _hasSaveSummary = false;
                _saveSummary = default;
                return false;
            }
        }

        public void Close()
        {
            StopMenuBgm();
            _canvas.enabled = false;
            _onClose?.Invoke();
            Destroy(gameObject);
        }

        private void Awake()
        {
            _font = ResolveElinFont();
            BuildUi();
            _canvas.enabled = false;
        }

        private void Update()
        {
            if (!_canvas.enabled) return;

            // Block all game input while menu is open
            EInput.Consume(consumeAxis: true, _skipFrame: 1);

            AnimateCursorPulse();
            AnimateNoiseScroll();
            HandleInput();
            HandleMouseInput();
            UpdateMenuBgmPlayback();
        }

        private void OnDestroy()
        {
            StopMenuBgm();
            if (_crtNoiseTex != null) Destroy(_crtNoiseTex);
            if (_crtScanlineTex != null) Destroy(_crtScanlineTex);
        }

        // ═══════════════════════════════════════════════
        //  UI Construction
        // ═══════════════════════════════════════════════

        private void BuildUi()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 4999;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            BuildBackground();
            BuildHeader();
            BuildListArea();
            BuildSlotMeter();
            BuildFooter();
            BuildOverlayPanel();
        }

        private void BuildBackground()
        {
            // Fullscreen dim
            var dimGo = new GameObject("FullscreenDim");
            dimGo.transform.SetParent(transform, false);
            _dimImage = dimGo.AddComponent<Image>();
            _dimImage.color = ColDim;
            _dimImage.raycastTarget = true;
            Stretch(dimGo);

            // Panel
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            _panelImage = panelGo.AddComponent<Image>();
            _panelImage.color = ColBgPanel;
            _panelImage.raycastTarget = true;
            var prt = panelGo.GetComponent<RectTransform>();
            CenterFixed(prt, PanelWidth, PanelHeight);

            // CRT Scanlines
            var scanGo = new GameObject("CrtScanlines");
            scanGo.transform.SetParent(panelGo.transform, false);
            _crtScanlines = scanGo.AddComponent<Image>();
            _crtScanlineTex = BuildScanlineTexture(4, 4);
            _crtScanlines.sprite = Sprite.Create(_crtScanlineTex,
                new Rect(0, 0, _crtScanlineTex.width, _crtScanlineTex.height),
                new Vector2(0.5f, 0.5f));
            _crtScanlines.type = Image.Type.Tiled;
            _crtScanlines.color = new Color(1f, 1f, 1f, 0.22f);
            _crtScanlines.raycastTarget = false;
            Stretch(scanGo);

            // CRT Noise
            var noiseGo = new GameObject("CrtNoise");
            noiseGo.transform.SetParent(panelGo.transform, false);
            _crtNoise = noiseGo.AddComponent<RawImage>();
            _crtNoiseTex = BuildNoiseTexture(128, 72);
            _crtNoise.texture = _crtNoiseTex;
            _crtNoise.color = new Color(1f, 1f, 1f, 0.03f);
            _crtNoise.raycastTarget = false;
            Stretch(noiseGo);

            // Border top
            MakeBorder(panelGo.transform, "BorderTop", new Vector2(0, 1), new Vector2(1, 1), 1f);
            // Border bottom
            MakeBorder(panelGo.transform, "BorderBottom", new Vector2(0, 0), new Vector2(1, 0), 1f);
        }

        private void MakeBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = ColBorder;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, anchorMin.y > 0.5f ? 1f : 0f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        private void BuildHeader()
        {
            var panelTr = _panelImage.transform;

            // Title
            _titleText = MakeText(panelTr, "TitleText", FontSizeTitle, FontStyle.Bold, ColAccent);
            var trt = _titleText.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, -16);
            trt.sizeDelta = new Vector2(-40, 40);
            _titleText.text = "J U S T   D O O M   I T";
            _titleText.alignment = TextAnchor.MiddleCenter;

            // Section header
            _sectionHeader = MakeText(panelTr, "SectionHeader", FontSizeSection, FontStyle.Normal, ColAccentDim);
            var srt = _sectionHeader.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0f, 1f);
            srt.anchoredPosition = new Vector2(30, -62);
            srt.sizeDelta = new Vector2(-60, 24);
            _sectionHeader.alignment = TextAnchor.MiddleLeft;

            // Divider
            var divGo = new GameObject("Divider");
            divGo.transform.SetParent(panelTr, false);
            var divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(1f, 1f, 1f, 0.12f);
            divImg.raycastTarget = false;
            var drt = divGo.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 1);
            drt.anchorMax = new Vector2(1, 1);
            drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(0, -88);
            drt.sizeDelta = new Vector2(-40, 1);

            // Status line
            _statusLine = MakeText(panelTr, "StatusLine", 16, FontStyle.Normal, ColAmber);
            var strt = _statusLine.GetComponent<RectTransform>();
            strt.anchorMin = new Vector2(0, 1);
            strt.anchorMax = new Vector2(1, 1);
            strt.pivot = new Vector2(0f, 1f);
            strt.anchoredPosition = new Vector2(30, -90);
            strt.sizeDelta = new Vector2(-60, 22);
            _statusLine.alignment = TextAnchor.MiddleLeft;
            _statusLine.text = "";
        }

        private void BuildListArea()
        {
            var panelTr = _panelImage.transform;
            var listGo = new GameObject("ListRoot");
            listGo.transform.SetParent(panelTr, false);
            _listRoot = listGo.AddComponent<RectTransform>();
            _listRoot.anchorMin = new Vector2(0, 1);
            _listRoot.anchorMax = new Vector2(1, 1);
            _listRoot.pivot = new Vector2(0f, 1f);
            _listRoot.anchoredPosition = new Vector2(30, -114);
            _listRoot.sizeDelta = new Vector2(-60, 340);
        }

        private void BuildSlotMeter()
        {
            var panelTr = _panelImage.transform;

            // Meter background
            var bgGo = new GameObject("SlotMeterBg");
            bgGo.transform.SetParent(panelTr, false);
            _slotMeterBg = bgGo.AddComponent<Image>();
            _slotMeterBg.color = ColMeterBg;
            _slotMeterBg.raycastTarget = false;
            var bgrt = bgGo.GetComponent<RectTransform>();
            bgrt.anchorMin = new Vector2(0, 0);
            bgrt.anchorMax = new Vector2(1, 0);
            bgrt.pivot = new Vector2(0f, 0f);
            bgrt.anchoredPosition = new Vector2(30, 52);
            bgrt.sizeDelta = new Vector2(-200, 14);

            // Meter fill
            var fillGo = new GameObject("SlotMeter");
            fillGo.transform.SetParent(bgGo.transform, false);
            _slotMeter = fillGo.AddComponent<Image>();
            _slotMeter.color = ColMeter;
            _slotMeter.raycastTarget = false;
            _slotMeter.type = Image.Type.Filled;
            _slotMeter.fillMethod = Image.FillMethod.Horizontal;
            Stretch(fillGo);

            // Meter text
            _slotMeterText = MakeText(panelTr, "SlotMeterText", 14, FontStyle.Normal, ColMeter);
            var mtrt = _slotMeterText.GetComponent<RectTransform>();
            mtrt.anchorMin = new Vector2(1, 0);
            mtrt.anchorMax = new Vector2(1, 0);
            mtrt.pivot = new Vector2(1f, 0f);
            mtrt.anchoredPosition = new Vector2(-30, 50);
            mtrt.sizeDelta = new Vector2(140, 18);
            _slotMeterText.alignment = TextAnchor.MiddleRight;

            SetSlotMeterVisible(false);
        }

        private void BuildFooter()
        {
            var panelTr = _panelImage.transform;
            _footerText = MakeText(panelTr, "Footer", FontSizeFooter, FontStyle.Normal, ColFooter);
            var frt = _footerText.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0);
            frt.anchorMax = new Vector2(1, 0);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.anchoredPosition = new Vector2(0, 10);
            frt.sizeDelta = new Vector2(-40, 30);
            _footerText.alignment = TextAnchor.MiddleCenter;
        }

        private void BuildOverlayPanel()
        {
            var panelTr = _panelImage.transform;
            _overlayPanel = new GameObject("OverlayPanel");
            _overlayPanel.transform.SetParent(panelTr, false);
            var oBg = _overlayPanel.AddComponent<Image>();
            oBg.color = ColOverlayBg;
            oBg.raycastTarget = true;
            var ort = _overlayPanel.GetComponent<RectTransform>();
            CenterFixed(ort, 400, 220);

            // Border for overlay
            MakeBorder(_overlayPanel.transform, "OvBorderTop", new Vector2(0, 1), new Vector2(1, 1), 1f);
            MakeBorder(_overlayPanel.transform, "OvBorderBottom", new Vector2(0, 0), new Vector2(1, 0), 1f);

            _overlayTitle = MakeText(_overlayPanel.transform, "OvTitle", 24, FontStyle.Bold, ColAccent);
            var otrt = _overlayTitle.GetComponent<RectTransform>();
            otrt.anchorMin = new Vector2(0, 1);
            otrt.anchorMax = new Vector2(1, 1);
            otrt.pivot = new Vector2(0.5f, 1f);
            otrt.anchoredPosition = new Vector2(0, -16);
            otrt.sizeDelta = new Vector2(-30, 32);
            _overlayTitle.alignment = TextAnchor.MiddleCenter;

            _overlayInfo = MakeText(_overlayPanel.transform, "OvInfo", 16, FontStyle.Normal, ColValue);
            var oirt = _overlayInfo.GetComponent<RectTransform>();
            oirt.anchorMin = new Vector2(0, 1);
            oirt.anchorMax = new Vector2(1, 1);
            oirt.pivot = new Vector2(0.5f, 1f);
            oirt.anchoredPosition = new Vector2(0, -52);
            oirt.sizeDelta = new Vector2(-30, 50);
            _overlayInfo.alignment = TextAnchor.MiddleCenter;

            // Row 0 (YES)
            var row0Go = new GameObject("OvRow0");
            row0Go.transform.SetParent(_overlayPanel.transform, false);
            var r0rt = row0Go.AddComponent<RectTransform>();
            r0rt.anchorMin = new Vector2(0, 1);
            r0rt.anchorMax = new Vector2(1, 1);
            r0rt.pivot = new Vector2(0.5f, 1f);
            r0rt.anchoredPosition = new Vector2(0, -120);
            r0rt.sizeDelta = new Vector2(-30, 28);

            _overlayCursorText0 = MakeText(row0Go.transform, "OvCur0", FontSizeOverlay, FontStyle.Bold, ColCursor);
            var c0rt = _overlayCursorText0.GetComponent<RectTransform>();
            c0rt.anchorMin = new Vector2(0, 0);
            c0rt.anchorMax = new Vector2(0, 1);
            c0rt.pivot = new Vector2(0f, 0.5f);
            c0rt.anchoredPosition = new Vector2(50, 0);
            c0rt.sizeDelta = new Vector2(24, 0);
            _overlayCursorText0.alignment = TextAnchor.MiddleLeft;

            _overlayRow0 = MakeText(row0Go.transform, "OvLabel0", FontSizeOverlay, FontStyle.Bold, ColSelected);
            var l0rt = _overlayRow0.GetComponent<RectTransform>();
            l0rt.anchorMin = new Vector2(0, 0);
            l0rt.anchorMax = new Vector2(1, 1);
            l0rt.pivot = new Vector2(0f, 0.5f);
            l0rt.anchoredPosition = new Vector2(74, 0);
            l0rt.sizeDelta = new Vector2(-100, 0);
            _overlayRow0.alignment = TextAnchor.MiddleLeft;

            // Row 1 (NO)
            var row1Go = new GameObject("OvRow1");
            row1Go.transform.SetParent(_overlayPanel.transform, false);
            var r1rt = row1Go.AddComponent<RectTransform>();
            r1rt.anchorMin = new Vector2(0, 1);
            r1rt.anchorMax = new Vector2(1, 1);
            r1rt.pivot = new Vector2(0.5f, 1f);
            r1rt.anchoredPosition = new Vector2(0, -152);
            r1rt.sizeDelta = new Vector2(-30, 28);

            _overlayCursorText1 = MakeText(row1Go.transform, "OvCur1", FontSizeOverlay, FontStyle.Bold, ColCursor);
            var c1rt = _overlayCursorText1.GetComponent<RectTransform>();
            c1rt.anchorMin = new Vector2(0, 0);
            c1rt.anchorMax = new Vector2(0, 1);
            c1rt.pivot = new Vector2(0f, 0.5f);
            c1rt.anchoredPosition = new Vector2(50, 0);
            c1rt.sizeDelta = new Vector2(24, 0);
            _overlayCursorText1.alignment = TextAnchor.MiddleLeft;

            _overlayRow1 = MakeText(row1Go.transform, "OvLabel1", FontSizeOverlay, FontStyle.Bold, ColNormal);
            var l1rt = _overlayRow1.GetComponent<RectTransform>();
            l1rt.anchorMin = new Vector2(0, 0);
            l1rt.anchorMax = new Vector2(1, 1);
            l1rt.pivot = new Vector2(0f, 0.5f);
            l1rt.anchoredPosition = new Vector2(74, 0);
            l1rt.sizeDelta = new Vector2(-100, 0);
            _overlayRow1.alignment = TextAnchor.MiddleLeft;

            _overlayPanel.SetActive(false);
        }

        // ═══════════════════════════════════════════════
        //  State Transitions
        // ═══════════════════════════════════════════════

        private void TransitionTo(MenuState next)
        {
            _state = next;
            _overlayPanel.SetActive(false);
            SetSlotMeterVisible(false);

            switch (next)
            {
                case MenuState.Main:
                    _hasSave = RecomputeHasSave();
                    RefreshMain();
                    break;
                case MenuState.IwadPicker:
                    RefreshIwadPicker();
                    break;
                case MenuState.PwadPicker:
                    RefreshPwadPicker();
                    break;
                case MenuState.SkillPicker:
                    RefreshSkillPicker();
                    break;
                case MenuState.ModRuleConfirm:
                    RefreshModRuleConfirm();
                    break;
                case MenuState.ExitConfirm:
                    ShowOverlay(next);
                    break;
            }

            UpdateFooterForCurrentRow();
        }

        private static readonly string[] SkillNames =
        {
            "1 - I'M TOO YOUNG TO DIE",
            "2 - HEY, NOT TOO ROUGH",
            "3 - HURT ME PLENTY",
            "4 - ULTRA-VIOLENCE",
            "5 - NIGHTMARE!"
        };

        private void RefreshMain()
        {
            ClearRows();

            // ── CURRENT LOADOUT (display-only block) ──
            _sectionHeader.text = L("現在の構成", "CURRENT LOADOUT", "当前配置");

            var iwadName = DoomWadLocator.GetIwadDisplayName(_loadout.selectedIwadFile);
            AddRow("IWAD     " + iwadName, RowKind.Setting, ColValue, enabled: false);

            var skillIdx = Mathf.Clamp(_loadout.selectedSkill, 1, 5) - 1;
            AddRow("SKILL    " + SkillNames[skillIdx], RowKind.Setting, ColValue, enabled: false);

            AddRow("MODS     " + GetSelectedModSummary(), RowKind.Setting, ColValue, enabled: false);

            if (_hasSave)
            {
                AddRow("SAVE     ● exists", RowKind.Setting, ColModOn, enabled: false);
                if (_hasSaveSummary)
                {
                    AddRow("         " + BuildSaveSummaryLine(_saveSummary), RowKind.Setting, ColNormal, enabled: false, fontSizeOverride: 16);
                }
            }
            else
                AddRow("SAVE     - none", RowKind.Setting, ColDisabled, enabled: false);

            // Separator
            AddRow("", RowKind.Separator, ColDisabled, false);

            if (_hasSave)
            {
                AddRow(L("再開", "CONTINUE", "继续"), RowKind.Action, ColAccent, tag: RowTag.Continue, fontSizeOverride: 24);
            }

            AddRow(L("最初から", "START OVER", "从头开始"), RowKind.Action, _hasSave ? ColNormal : ColAccent, tag: RowTag.NewRun, fontSizeOverride: 24);

            AddRow(L("ゲーム切替", "CHANGE GAME", "切换游戏"), RowKind.Action, ColNormal, tag: RowTag.Iwad);
            AddRow(L("難易度変更", "CHANGE SKILL", "更换难度"), RowKind.Action, ColNormal, tag: RowTag.Skill);
            AddRow(L("MOD設定", "CONFIGURE MODS", "配置MOD"), RowKind.Action, ColNormal, tag: RowTag.Mods);

            AddRow(L("閉じる", "CLOSE", "关闭"), RowKind.Action, ColNormal, tag: RowTag.Close);

            SetCursor(0);
        }

        private void RefreshIwadPicker()
        {
            ClearRows();
            _sectionHeader.text = L("<< IWADを選択 >>", "<< SELECT IWAD >>", "<< 选择IWAD >>");

            AddRow(L("戻る", "BACK", "返回"), RowKind.Action, ColNormal, tag: RowTag.Back);
            AddRow("", RowKind.Separator, ColDisabled, false);

            _iwads = DoomWadLocator.FindIwads();
            var selectedIdx = 2;
            for (var i = 0; i < _iwads.Count; i++)
            {
                var iw = _iwads[i];
                var isCurrent = string.Equals(iw.FileName, _loadout.selectedIwadFile, StringComparison.OrdinalIgnoreCase);
                var marker = isCurrent ? "(*) " : "( ) ";
                var color = isCurrent ? ColIwadSel : ColNormal;
                AddRow(marker + DoomWadLocator.GetIwadDisplayName(iw.FileName), RowKind.Action, color);
                if (isCurrent) selectedIdx = i + 2;
            }

            SetCursor(selectedIdx);
        }

        private void RefreshPwadPicker()
        {
            ClearRows();
            var hasEnabledMod = _loadout.enabledModFiles != null && _loadout.enabledModFiles.Count > 0;
            var iwadShort = DoomWadLocator.GetIwadDisplayName(_loadout.selectedIwadFile);
            _sectionHeader.text = L(
                "<< MOD設定: " + (hasEnabledMod ? "ON" : "OFF") + "  IWAD: " + iwadShort + " >>",
                "<< MOD CONFIG: " + (hasEnabledMod ? "ON" : "OFF") + "  IWAD: " + iwadShort + " >>",
                "<< MOD配置: " + (hasEnabledMod ? "ON" : "OFF") + "  IWAD: " + iwadShort + " >>");

            AddRow(L("戻る", "BACK", "返回"), RowKind.Action, ColNormal, tag: RowTag.Back);
            AddRow("", RowKind.Separator, ColDisabled, false);
            AddRow(L("MODフォルダを開く", "OPEN MOD FOLDER", "打开MOD文件夹"), RowKind.Action, ColNormal, tag: RowTag.OpenModFolder);
            AddRow("", RowKind.Separator, ColDisabled, false);

            _pwads = DoomWadLocator.FindPwads();
            if (_pwads.Count == 0)
            {
                AddRow(
                    L(
                        "MODが見つかりません (wad/mods)。先に「MODフォルダを開く」。",
                        "No mods found (wad/mods). Use OPEN MOD FOLDER first.",
                        "未找到MOD（wad/mods）。请先打开MOD文件夹。"),
                    RowKind.Setting,
                    ColDisabled,
                    enabled: false);
            }

            for (var i = 0; i < _pwads.Count; i++)
            {
                var p = _pwads[i];
                var on = _loadout.enabledModFiles != null &&
                         _loadout.enabledModFiles.Any(f => string.Equals(f, p.FileName, StringComparison.OrdinalIgnoreCase));
                var info = DoomModRuleStore.GetRuleInfo(p.FileName);
                var family = info.Exists ? info.Family : "unknown";
                var mismatch = IsIwadMismatch(_loadout.selectedIwadFile, p.FileName);
                var unknown = string.Equals(family, "unknown", StringComparison.OrdinalIgnoreCase) && !mismatch;

                if (mismatch)
                {
                    var rowIndex = AddRow("[---] " + p.FileName, RowKind.Action, ColDisabled, enabled: false);
                    _pwadRowToIndex[rowIndex] = i;
                }
                else if (unknown)
                {
                    var prefix = on ? "[?ON] " : "[?] ";
                    var rowIndex = AddRow(prefix + p.FileName, RowKind.Action, ColAmber);
                    _pwadRowToIndex[rowIndex] = i;
                }
                else if (on)
                {
                    var rowIndex = AddRow("[ON ] " + p.FileName, RowKind.Action, ColModOn);
                    _pwadRowToIndex[rowIndex] = i;
                }
                else
                {
                    var rowIndex = AddRow("[OFF] " + p.FileName, RowKind.Action, ColModOff);
                    _pwadRowToIndex[rowIndex] = i;
                }
            }

            // Slot meter
            SetSlotMeterVisible(true);
            UpdateSlotMeter();

            var firstModRow = _pwadRowToIndex.Count > 0 ? _pwadRowToIndex.Keys.Min() : 0;
            SetCursor(firstModRow);
        }

        private void RefreshModRuleConfirm()
        {
            ClearRows();
            _sectionHeader.text = L("<< MOD依存を確認 >>", "<< CONFIRM MOD DEPENDENCY >>", "<< 确认MOD依赖 >>");

            if (!_hasActiveRulePrompt)
            {
                AddRow(
                    L("確認対象のMODはありません。", "No pending mod checks.", "没有待确认的MOD。"),
                    RowKind.Setting,
                    ColDisabled,
                    enabled: false);
                AddRow(L("戻る", "BACK", "返回"), RowKind.Action, ColNormal, tag: RowTag.RuleSkip);
                SetCursor(0);
                return;
            }

            var familyLabel = FamilyLabel(_activeRulePrompt.SuggestedFamily);
            var reason = DoomModRuleStore.GetReasonText(_activeRulePrompt.ReasonCode, _activeRulePrompt.SuggestedFamily);
            AddRow("FILE     " + _activeRulePrompt.FileName, RowKind.Setting, ColValue, enabled: false);
            AddRow("AUTO     " + familyLabel, RowKind.Setting, ColValue, enabled: false);
            AddRow("CONF     " + _activeRulePrompt.Confidence.ToUpperInvariant(), RowKind.Setting, ColNormal, enabled: false);
            AddRow("REASON   " + reason, RowKind.Setting, ColNormal, enabled: false, fontSizeOverride: 16);
            AddRow("", RowKind.Separator, ColDisabled, false);

            AddRow(L("自動判定を使う", "USE AUTO RESULT", "使用自动判定"), RowKind.Action, ColAccent, tag: RowTag.RuleUseAuto);
            AddRow(L("手動: DOOM1系", "MANUAL: DOOM1", "手动: DOOM1"), RowKind.Action, ColNormal, tag: RowTag.RuleDoom1);
            AddRow(L("手動: DOOM2系", "MANUAL: DOOM2", "手动: DOOM2"), RowKind.Action, ColNormal, tag: RowTag.RuleDoom2);
            AddRow(L("手動: 両対応", "MANUAL: DUAL", "手动: 双兼容"), RowKind.Action, ColNormal, tag: RowTag.RuleAny);
            AddRow(L("手動: 保留(不明)", "MANUAL: PENDING", "手动: 待定"), RowKind.Action, ColNormal, tag: RowTag.RulePending);
            AddRow(L("残りを後で確認", "SKIP REMAINING", "稍后处理剩余项"), RowKind.Action, ColDisabled, tag: RowTag.RuleSkip);
            SetCursor(0);
        }

        private void ShowOverlay(MenuState which)
        {
            _overlayPanel.SetActive(true);
            _overlayCursor = 0;

            _overlayTitle.text = L("設定を閉じますか？", "REALLY QUIT SETUP?", "确定退出设置？");
            _overlayInfo.text = "";
            _overlayRow0.text = L("[はい、閉じる]", "[YES, CLOSE]", "[是，关闭]");
            _overlayRow1.text = L("[いいえ、戻る]", "[NO, GO BACK]", "[否，返回]");

            UpdateOverlayCursor();
        }

        // ═══════════════════════════════════════════════
        //  Row Management
        // ═══════════════════════════════════════════════

        private void ClearRows()
        {
            for (var i = _rows.Count - 1; i >= 0; i--)
            {
                Destroy(_rows[i].Go);
            }
            _rows.Clear();
            _pwadRowToIndex.Clear();
            _cursor = 0;
        }

        private int AddRow(string label, RowKind kind, Color color, bool enabled = true, RowTag tag = RowTag.None, int fontSizeOverride = 0)
        {
            var idx = _rows.Count;
            var y = -(idx * (RowHeight + RowSpacing));
            var fontSize = fontSizeOverride > 0 ? fontSizeOverride : FontSizeRow;

            var go = new GameObject("Row_" + idx);
            go.transform.SetParent(_listRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(0, RowHeight);

            // Cursor ">"
            var curText = MakeText(go.transform, "Cursor", fontSize, FontStyle.Bold, ColCursor);
            var crt = curText.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0);
            crt.anchorMax = new Vector2(0, 1);
            crt.pivot = new Vector2(0f, 0.5f);
            crt.anchoredPosition = new Vector2(0, 0);
            crt.sizeDelta = new Vector2(20, 0);
            curText.alignment = TextAnchor.MiddleLeft;
            curText.text = "";

            // Label
            var labelText = MakeText(go.transform, "Label", fontSize, FontStyle.Normal, color);
            var lrt = labelText.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.anchoredPosition = new Vector2(22, 0);
            lrt.sizeDelta = new Vector2(-22, 0);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;

            if (kind == RowKind.Separator)
            {
                curText.text = "";
                labelText.text = "─────────────────────────";
                labelText.color = new Color(1f, 1f, 1f, 0.12f);
            }

            _rows.Add(new RowEntry
            {
                Go = go,
                CursorText = curText,
                LabelText = labelText,
                Kind = kind,
                Enabled = enabled,
                Rect = rt,
                Tag = tag
            });

            return idx;
        }

        private void SetCursor(int index)
        {
            if (_rows.Count == 0)
            {
                _cursor = 0;
                return;
            }

            var hasSelectable = false;
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Kind != RowKind.Separator && _rows[i].Enabled)
                {
                    hasSelectable = true;
                    break;
                }
            }

            if (!hasSelectable)
            {
                for (var i = 0; i < _rows.Count; i++)
                {
                    _rows[i].CursorText.text = "";
                    if (_rows[i].Kind != RowKind.Separator)
                    {
                        _rows[i].LabelText.fontStyle = FontStyle.Normal;
                    }
                }

                _cursor = -1;
                return;
            }

            // Clamp and skip separators
            index = Mathf.Clamp(index, 0, _rows.Count - 1);
            while (index < _rows.Count && (_rows[index].Kind == RowKind.Separator || !_rows[index].Enabled))
            {
                index++;
            }
            if (index >= _rows.Count)
            {
                index = _rows.Count - 1;
                while (index >= 0 && (_rows[index].Kind == RowKind.Separator || !_rows[index].Enabled))
                {
                    index--;
                }
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.CursorText.text = i == index ? ">" : "";
                if (row.Kind != RowKind.Separator)
                {
                    row.LabelText.fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            _cursor = index;
            UpdateFooterForCurrentRow();
            UpdatePwadInlineDetailForCursor();
        }

        private string GetSelectedModSummary()
        {
            if (_loadout?.enabledModFiles == null || _loadout.enabledModFiles.Count == 0)
            {
                return "- none";
            }

            var selected = _loadout.enabledModFiles[0];
            return string.IsNullOrWhiteSpace(selected) ? "- none" : selected.Trim();
        }

        private void UpdateSlotMeter()
        {
            if (_slotMeter == null) return;
            var enabled = (_loadout.enabledModFiles != null && _loadout.enabledModFiles.Count > 0) ? 1 : 0;
            var max = 1;
            _slotMeter.fillAmount = max > 0 ? (float)enabled / max : 0f;
            _slotMeterText.text = L(
                "MOD状態: " + (enabled > 0 ? "ON" : "OFF"),
                "MOD state: " + (enabled > 0 ? "ON" : "OFF"),
                "MOD状态: " + (enabled > 0 ? "ON" : "OFF"));
        }

        private void UpdateFooterForCurrentRow()
        {
            if (_state == MenuState.Main)
            {
                _footerText.text = L(
                    "ENTER: 選択  ESC: 閉じる",
                    "ENTER: select  ESC: close",
                    "ENTER: 选择  ESC: 关闭");
                _statusLine.text = "";
            }
            else if (_state == MenuState.PwadPicker)
            {
                _footerText.text = L(
                    "ENTER: 選択/切り替え  ESC: 戻る",
                    "ENTER: select/toggle  ESC: back",
                    "ENTER: 选择/切换  ESC: 返回");
                _statusLine.text = "";
            }
            else if (_state == MenuState.IwadPicker || _state == MenuState.SkillPicker)
            {
                _footerText.text = L(
                    "ENTER: 選択  ESC: 戻る",
                    "ENTER: select  ESC: back",
                    "ENTER: 选择  ESC: 返回");
            }
            else if (_state == MenuState.ModRuleConfirm)
            {
                _footerText.text = L(
                    "ENTER: 決定  ESC: 後で",
                    "ENTER: apply  ESC: later",
                    "ENTER: 应用  ESC: 稍后");
            }
            else if (_state == MenuState.ExitConfirm)
            {
                _footerText.text = L(
                    "ENTER: 確定  ESC: 戻る",
                    "ENTER: confirm  ESC: back",
                    "ENTER: 确认  ESC: 返回");
            }
        }

        private void SetSlotMeterVisible(bool visible)
        {
            if (_slotMeter != null) _slotMeter.gameObject.SetActive(visible);
            if (_slotMeterBg != null) _slotMeterBg.gameObject.SetActive(visible);
            if (_slotMeterText != null) _slotMeterText.gameObject.SetActive(visible);
        }

        // ═══════════════════════════════════════════════
        //  Input Handling
        // ═══════════════════════════════════════════════

        private void HandleInput()
        {
            if (_state == MenuState.ExitConfirm)
            {
                HandleOverlayInput();
                return;
            }

            if (_state == MenuState.Main)
            {
                HandleMainInput();
                return;
            }

            if (_state == MenuState.ModRuleConfirm)
            {
                HandleModRuleConfirmInput();
                return;
            }

            // IwadPicker / PwadPicker / SkillPicker
            HandlePickerInput();
        }

        private void HandleMainInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EInput.Consume(consumeAxis: true, _skipFrame: 1);
                TransitionTo(MenuState.ExitConfirm);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow)) { MoveSelection(-1); return; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { MoveSelection(1); return; }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnMainSelect();
            }
        }

        private void InvokePlayAndClose(bool loadExisting)
        {
            StopMenuBgm();
            Close();
            try
            {
                _onPlay?.Invoke(loadExisting);
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] onPlay callback failed.", ex);
            }
        }

        private void OnMainSelect()
        {
            if (_cursor < 0 || _cursor >= _rows.Count) return;
            var tag = _rows[_cursor].Tag;
            switch (tag)
            {
                case RowTag.Continue:
                    SE.Click();
                    InvokePlayAndClose(true);
                    break;
                case RowTag.NewRun:
                    SE.Click();
                    InvokePlayAndClose(false);
                    break;
                case RowTag.Iwad:
                    SE.Tab();
                    TransitionTo(MenuState.IwadPicker);
                    break;
                case RowTag.Skill:
                    SE.Tab();
                    TransitionTo(MenuState.SkillPicker);
                    break;
                case RowTag.Mods:
                    SE.Tab();
                    if (DoomWadLocator.ReconcileRuntimeLoadout(_loadout))
                    {
                        DoomWadLocator.SaveRuntimeLoadout(_loadout);
                    }

                    RefreshModRules(MenuState.PwadPicker);
                    break;
                case RowTag.Close:
                    SE.Click();
                    Close();
                    break;
            }
        }

        private void HandlePickerInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SE.Tab();
                EInput.Consume(consumeAxis: true, _skipFrame: 1);
                TransitionTo(MenuState.Main);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow)) { MoveSelection(-1); return; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { MoveSelection(1); return; }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_state == MenuState.IwadPicker)
                    OnIwadConfirm();
                else if (_state == MenuState.SkillPicker)
                    OnSkillConfirm();
                else
                    OnPwadPickerSelect();
            }
        }

        private void HandleModRuleConfirmInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SE.Tab();
                _pendingRulePrompts.Clear();
                _hasActiveRulePrompt = false;
                EInput.Consume(consumeAxis: true, _skipFrame: 1);
                TransitionTo(_modRuleReturnState);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow)) { MoveSelection(-1); return; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { MoveSelection(1); return; }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnModRuleConfirmSelect();
            }
        }

        private void OnIwadConfirm()
        {
            if (_cursor < 0 || _cursor >= _rows.Count) return;
            if (_rows[_cursor].Tag == RowTag.Back)
            {
                SE.Tab();
                TransitionTo(MenuState.Main);
                return;
            }

            var selectedIndex = _cursor - 2;
            if (selectedIndex < 0 || _iwads == null || selectedIndex >= _iwads.Count) return;
            var selected = _iwads[selectedIndex];
            _loadout.selectedIwadFile = selected.FileName;
            var removed = RemoveIncompatibleModsForIwad(_loadout, selected.FileName);
            DoomWadLocator.SaveRuntimeLoadout(_loadout);
            SE.Click();

            if (removed > 0)
            {
                StartCoroutine(ShowTempStatus(
                    L(removed + "件のMODを無効化", removed + " MOD(S) DISABLED", removed + "个MOD已禁用"),
                    ColAmber, 2f));
            }

            TransitionTo(MenuState.Main);
        }

        private void RefreshSkillPicker()
        {
            ClearRows();
            _sectionHeader.text = L("<< 難易度を選択 >>", "<< SELECT SKILL >>", "<< 选择难度 >>");

            AddRow(L("戻る", "BACK", "返回"), RowKind.Action, ColNormal, tag: RowTag.Back);
            AddRow("", RowKind.Separator, ColDisabled, false);

            var selectedIdx = Mathf.Clamp(_loadout.selectedSkill, 1, 5) + 1;
            for (var i = 0; i < SkillNames.Length; i++)
            {
                var isCurrent = i == selectedIdx - 2;
                var marker = isCurrent ? "(*) " : "( ) ";
                var color = isCurrent ? ColIwadSel : ColNormal;
                AddRow(marker + SkillNames[i], RowKind.Action, color);
            }

            SetCursor(selectedIdx);
        }

        private void OnSkillConfirm()
        {
            if (_cursor < 0 || _cursor >= _rows.Count) return;
            if (_rows[_cursor].Tag == RowTag.Back)
            {
                SE.Tab();
                TransitionTo(MenuState.Main);
                return;
            }

            var selectedIndex = _cursor - 2;
            if (selectedIndex < 0 || selectedIndex >= SkillNames.Length) return;
            _loadout.selectedSkill = selectedIndex + 1;
            DoomWadLocator.SaveRuntimeLoadout(_loadout);
            SE.Click();
            TransitionTo(MenuState.Main);
        }

        private void OnPwadPickerSelect()
        {
            if (_cursor < 0 || _cursor >= _rows.Count)
            {
                return;
            }

            var tag = _rows[_cursor].Tag;
            if (tag == RowTag.Back)
            {
                SE.Tab();
                TransitionTo(MenuState.Main);
                return;
            }

            if (tag == RowTag.OpenModFolder)
            {
                SE.Click();
                OpenModsFolder();
                return;
            }

            if (!_pwadRowToIndex.TryGetValue(_cursor, out var pwadIndex))
            {
                return;
            }

            if (pwadIndex < 0 || _pwads == null || pwadIndex >= _pwads.Count)
            {
                return;
            }

            var p = _pwads[pwadIndex];

            // Incompatible check
            if (IsIwadMismatch(_loadout.selectedIwadFile, p.FileName))
            {
                SE.Beep();
                var required = GetRequiredIwadFamilyForPwad(p.FileName);
                StartCoroutine(ShowTempStatus(
                    L("NEEDS " + required?.ToUpperInvariant(), "NEEDS " + required?.ToUpperInvariant(), "需要 " + required?.ToUpperInvariant()),
                    ColFlashRed, 1.5f));
                StartCoroutine(FlashRow(_cursor, ColFlashRed, 0.4f));
                return;
            }

            var enabled = _loadout.enabledModFiles ?? new List<string>();
            var existing = enabled.FindIndex(f => string.Equals(f, p.FileName, StringComparison.OrdinalIgnoreCase));

            if (existing >= 0)
            {
                enabled.RemoveAt(existing);
                SE.Click();
            }
            else
            {
                enabled.Clear();
                enabled.Add(p.FileName);
                SE.Click();
            }

            _loadout.enabledModFiles = enabled;
            DoomWadLocator.SaveRuntimeLoadout(_loadout);
            var savedCursor = _cursor;
            RefreshPwadPicker();
            SetCursor(Mathf.Min(savedCursor, _rows.Count - 1));
        }

        private void OpenModsFolder()
        {
            var path = Path.Combine(DoomWadLocator.GetWadRoot(), "mods");
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var quotedPath = "\"" + path + "\"";
                var process = System.Diagnostics.Process.Start("explorer.exe", quotedPath);
                if (process == null)
                {
                    throw new InvalidOperationException("explorer.exe returned null process.");
                }

                StartCoroutine(ShowTempStatus(
                    L("MODフォルダを開きました。", "Opened MOD folder.", "已打开MOD文件夹。"),
                    ColModOn,
                    2.0f));
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] OPEN MOD FOLDER failed: " + ex.Message);
                _statusLine.text = L(
                    "開けませんでした: " + path,
                    "Open failed: " + path,
                    "打开失败: " + path);
                _statusLine.color = ColFlashRed;
                Dialog.YesNo(
                    L(
                        "MODフォルダを開けませんでした。\nパスをコピーしますか？\n" + path,
                        "Failed to open MOD folder.\nCopy path?\n" + path,
                        "无法打开MOD文件夹。\n是否复制路径？\n" + path),
                    () =>
                    {
                        var copied = TryCopyPathToClipboard(path);
                        Msg.SayRaw(copied
                            ? L("パスをコピーしました。", "Path copied.", "路径已复制。")
                            : L("コピーに失敗しました。手動で開いてください。", "Copy failed. Open it manually.", "复制失败，请手动打开。"));
                    },
                    () => { },
                    L("コピー", "Copy", "复制"),
                    L("閉じる", "Close", "关闭"));
            }
        }

        private void RefreshModRules(MenuState returnState)
        {
            _modRuleReturnState = returnState;
            _pendingRulePrompts.Clear();
            _hasActiveRulePrompt = false;
            _activeRulePrompt = null;
            try
            {
                var report = DoomWadLocator.RefreshPwadRules();
                for (var i = 0; i < report.Prompts.Count; i++)
                {
                    _pendingRulePrompts.Enqueue(report.Prompts[i]);
                }

                var summary = L(
                    "再判定完了: " + report.ProcessedCount + "件 / 自動確定 " + report.AutoAcceptedCount + "件 / 要確認 " + report.Prompts.Count + "件",
                    "Refresh done: " + report.ProcessedCount + " files / auto " + report.AutoAcceptedCount + " / review " + report.Prompts.Count,
                    "刷新完成: " + report.ProcessedCount + "个文件 / 自动 " + report.AutoAcceptedCount + " / 待确认 " + report.Prompts.Count);
                StartCoroutine(ShowTempStatus(summary, ColAmber, 3.5f));
                _pwads = DoomWadLocator.FindPwads();

                if (_pendingRulePrompts.Count > 0)
                {
                    _activeRulePrompt = _pendingRulePrompts.Dequeue();
                    _hasActiveRulePrompt = true;
                    TransitionTo(MenuState.ModRuleConfirm);
                }
                else
                {
                    TransitionTo(_modRuleReturnState);
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] RefreshModRules failed.", ex);
                StartCoroutine(ShowTempStatus(
                    L("再判定に失敗しました。", "Refresh failed.", "刷新失败。"),
                    ColFlashRed,
                    2.5f));
            }
        }

        private void OnModRuleConfirmSelect()
        {
            if (!_hasActiveRulePrompt)
            {
                TransitionTo(_modRuleReturnState);
                return;
            }

            if (_cursor < 0 || _cursor >= _rows.Count)
            {
                return;
            }

            var tag = _rows[_cursor].Tag;
            var file = _activeRulePrompt.FileName;
            switch (tag)
            {
                case RowTag.RuleUseAuto:
                    SE.Click();
                    break;
                case RowTag.RuleDoom1:
                    SE.Click();
                    DoomModRuleStore.SetManualFamily(file, "doom1");
                    break;
                case RowTag.RuleDoom2:
                    SE.Click();
                    DoomModRuleStore.SetManualFamily(file, "doom2");
                    break;
                case RowTag.RuleAny:
                    SE.Click();
                    DoomModRuleStore.SetManualFamily(file, "any");
                    break;
                case RowTag.RulePending:
                    SE.Click();
                    DoomModRuleStore.SetManualFamily(file, "unknown");
                    break;
                case RowTag.RuleSkip:
                    SE.Tab();
                    _pendingRulePrompts.Clear();
                    _hasActiveRulePrompt = false;
                    TransitionTo(_modRuleReturnState);
                    return;
                default:
                    return;
            }

            if (_pendingRulePrompts.Count > 0)
            {
                _activeRulePrompt = _pendingRulePrompts.Dequeue();
                _hasActiveRulePrompt = true;
                TransitionTo(MenuState.ModRuleConfirm);
            }
            else
            {
                _hasActiveRulePrompt = false;
                TransitionTo(_modRuleReturnState);
                StartCoroutine(ShowTempStatus(
                    L("MOD依存の確認が完了しました。", "MOD dependency review complete.", "MOD依赖确认完成。"),
                    ColModOn,
                    2.0f));
            }
        }

        private void HandleOverlayInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EInput.Consume(consumeAxis: true, _skipFrame: 1);
                SE.Tab();
                TransitionTo(MenuState.Main);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                _overlayCursor = 1 - _overlayCursor;
                UpdateOverlayCursor();
                SE.Tab();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_overlayCursor == 0) // YES
                {
                    SE.Click();
                    // ExitConfirm
                    {
                        Close();
                    }
                }
                else // NO
                {
                    SE.Tab();
                    TransitionTo(MenuState.Main);
                }
            }
        }

        private void UpdateOverlayCursor()
        {
            _overlayCursorText0.text = _overlayCursor == 0 ? ">" : "";
            _overlayCursorText1.text = _overlayCursor == 1 ? ">" : "";
            _overlayRow0.fontStyle = _overlayCursor == 0 ? FontStyle.Bold : FontStyle.Normal;
            _overlayRow1.fontStyle = _overlayCursor == 1 ? FontStyle.Bold : FontStyle.Normal;
        }

        private void MoveSelection(int delta)
        {
            if (_rows.Count == 0)
            {
                return;
            }

            if (_cursor < 0)
            {
                SetCursor(0);
                return;
            }

            var next = _cursor + delta;
            // Skip separators/disabled rows
            while (next >= 0 && next < _rows.Count && (_rows[next].Kind == RowKind.Separator || !_rows[next].Enabled))
            {
                next += delta;
            }

            if (next < 0 || next >= _rows.Count) return;
            SetCursor(next);
            SE.Tab();
        }

        // ═══════════════════════════════════════════════
        //  Mouse Input
        // ═══════════════════════════════════════════════

        private void HandleMouseInput()
        {
            if (_state == MenuState.ExitConfirm)
            {
                HandleOverlayMouse();
                return;
            }

            // Hover detection for list rows
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Kind == RowKind.Separator || !row.Enabled) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(row.Rect, Input.mousePosition, null))
                {
                    if (_cursor != i)
                    {
                        SetCursor(i);
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (_state == MenuState.Main)
                            OnMainSelect();
                        else if (_state == MenuState.IwadPicker)
                            OnIwadConfirm();
                        else if (_state == MenuState.SkillPicker)
                            OnSkillConfirm();
                        else if (_state == MenuState.ModRuleConfirm)
                            OnModRuleConfirmSelect();
                        else if (_state == MenuState.PwadPicker)
                            OnPwadPickerSelect();
                    }
                    break;
                }
            }

        }

        private void UpdatePwadInlineDetailForCursor()
        {
            if (_state != MenuState.PwadPicker || _pwads == null || _pwads.Count == 0)
            {
                return;
            }

            foreach (var kv in _pwadRowToIndex)
            {
                var rowIndex = kv.Key;
                var pwadIndex = kv.Value;
                if (rowIndex < 0 || rowIndex >= _rows.Count || pwadIndex < 0 || pwadIndex >= _pwads.Count)
                {
                    continue;
                }

                var p = _pwads[pwadIndex];
                var on = _loadout.enabledModFiles != null &&
                         _loadout.enabledModFiles.Any(f => string.Equals(f, p.FileName, StringComparison.OrdinalIgnoreCase));
                var mismatch = IsIwadMismatch(_loadout.selectedIwadFile, p.FileName);
                var required = GetRequiredIwadFamilyForPwad(p.FileName);
                var info = DoomModRuleStore.GetRuleInfo(p.FileName);
                var unknown = string.Equals(required, "unknown", StringComparison.OrdinalIgnoreCase) && !mismatch;

                var prefix = mismatch ? "[---] " : (unknown ? (on ? "[?ON] " : "[?] ") : (on ? "[ON ] " : "[OFF] "));
                var text = prefix + p.FileName;
                if (rowIndex == _cursor)
                {
                    if (mismatch)
                    {
                        text += "  " + L(
                            "※ 要求: " + required?.ToUpperInvariant() + "（不一致）",
                            "* Requires: " + required?.ToUpperInvariant() + " (incompatible)",
                            "※ 需要: " + required?.ToUpperInvariant() + "（不兼容）");
                    }
                    else if (unknown)
                    {
                        var reason = info.Exists
                            ? DoomModRuleStore.GetReasonText(info.ReasonCode, info.Family)
                            : L("未分類", "Unclassified", "未分类");
                        text += "  " + L(
                            "※ 依存不明: 手動設定推奨 (" + reason + ")",
                            "* Unknown dependency: set manually (" + reason + ")",
                            "※ 依赖不明: 建议手动设置 (" + reason + ")");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(required) && !string.Equals(required, "any", StringComparison.OrdinalIgnoreCase))
                        {
                            text += "  " + L(
                                "※ 要求: " + required?.ToUpperInvariant(),
                                "* Requires: " + required?.ToUpperInvariant(),
                                "※ 需要: " + required?.ToUpperInvariant());
                        }
                    }
                }

                _rows[rowIndex].LabelText.text = text;
                _rows[rowIndex].LabelText.color = mismatch ? ColDisabled : (unknown ? ColAmber : (on ? ColModOn : ColModOff));
            }
        }

        private void HandleOverlayMouse()
        {
            var r0 = _overlayRow0.GetComponent<RectTransform>();
            var r1 = _overlayRow1.GetComponent<RectTransform>();

            if (r0 != null && RectTransformUtility.RectangleContainsScreenPoint(r0, Input.mousePosition, null))
            {
                if (_overlayCursor != 0)
                {
                    _overlayCursor = 0;
                    UpdateOverlayCursor();
                }

                if (Input.GetMouseButtonDown(0))
                {
                    SE.Click();
                    if (_state == MenuState.ExitConfirm)
                    {
                        Close();
                    }
                }
            }
            else if (r1 != null && RectTransformUtility.RectangleContainsScreenPoint(r1, Input.mousePosition, null))
            {
                if (_overlayCursor != 1)
                {
                    _overlayCursor = 1;
                    UpdateOverlayCursor();
                }

                if (Input.GetMouseButtonDown(0))
                {
                    SE.Tab();
                    TransitionTo(MenuState.Main);
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  Visual Animation
        // ═══════════════════════════════════════════════

        private void AnimateCursorPulse()
        {
            var alpha = 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f * Mathf.PI * 2f));
            var pulseColor = ColCursor;
            pulseColor.a = alpha;

            // Main rows cursor
            if (_cursor >= 0 && _cursor < _rows.Count)
            {
                _rows[_cursor].CursorText.color = pulseColor;
            }

            // Overlay cursor
            if (_overlayPanel.activeSelf)
            {
                if (_overlayCursor == 0) _overlayCursorText0.color = pulseColor;
                else _overlayCursorText1.color = pulseColor;
            }
        }

        private void AnimateNoiseScroll()
        {
            if (_crtNoise == null) return;

            var elapsed = Time.unscaledTime - _bootTime;

            // Boot flicker for first 0.3s
            if (elapsed < 0.3f)
            {
                var t = elapsed / 0.3f;
                _crtNoise.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 0.03f, t));
            }
            else
            {
                _crtNoise.color = new Color(1f, 1f, 1f, 0.03f);
            }

            _crtNoise.uvRect = new Rect(0f, Time.unscaledTime * 1.2f, 2f, 2f);
        }

        private IEnumerator FlashRow(int index, Color flash, float sec)
        {
            if (index < 0 || index >= _rows.Count) yield break;
            var row = _rows[index];
            var original = row.LabelText.color;
            row.LabelText.color = flash;
            yield return new WaitForSecondsRealtime(sec);
            if (index < _rows.Count && _rows[index].LabelText != null)
                _rows[index].LabelText.color = original;
        }

        private IEnumerator ShowTempStatus(string msg, Color color, float sec)
        {
            if (_statusLine == null) yield break;
            _statusLine.text = msg;
            _statusLine.color = color;
            yield return new WaitForSecondsRealtime(sec);
            if (_statusLine != null) _statusLine.text = "";
        }

        // ═══════════════════════════════════════════════
        //  Menu BGM
        // ═══════════════════════════════════════════════

        private void StartMenuBgm()
        {
            try
            {
                _menuBgmActive = true;
                _menuBgmIndex = UnityEngine.Random.Range(0, MenuBgmIds.Length);
                _nextMenuBgmRetryAt = 0f;
                if (EClass.Sound == null) return;
                LayerDrama.haltPlaylist = true;
                EClass.Sound.StopBGM();
                PlayNextMenuBgm();
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Menu BGM start failed: " + ex.Message);
            }
        }

        private void UpdateMenuBgmPlayback()
        {
            if (!_menuBgmActive || EClass.Sound == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextMenuBgmRetryAt)
            {
                return;
            }

            var hasPlayingBgm = EClass.Sound.sourceBGM != null && EClass.Sound.sourceBGM.isPlaying;
            var currentId = EClass.Sound.currentBGM != null ? EClass.Sound.currentBGM.id : string.Empty;
            var isMenuBgm = IsMenuBgmId(currentId);
            if (hasPlayingBgm && isMenuBgm)
            {
                return;
            }

            PlayNextMenuBgm();
        }

        private void PlayNextMenuBgm()
        {
            if (!_menuBgmActive || EClass.Sound == null || MenuBgmIds.Length == 0)
            {
                return;
            }

            var id = MenuBgmIds[_menuBgmIndex % MenuBgmIds.Length];
            _menuBgmIndex = (_menuBgmIndex + 1) % MenuBgmIds.Length;
            var bgm = EClass.Sound.PlayBGM(id);
            _nextMenuBgmRetryAt = bgm == null ? Time.unscaledTime + 1.0f : Time.unscaledTime + 0.5f;
        }

        private static bool IsMenuBgmId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            var normalized = NormalizeBgmId(id);
            for (var i = 0; i < MenuBgmIds.Length; i++)
            {
                if (string.Equals(normalized, NormalizeBgmId(MenuBgmIds[i]), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeBgmId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            var s = id.Trim().Replace("\\", "/");
            const string bgmPrefix = "BGM/";
            if (s.StartsWith(bgmPrefix, StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(bgmPrefix.Length);
            }

            if (s.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 4);
            }

            return s;
        }

        private void StopMenuBgm()
        {
            if (!_menuBgmActive) return;
            _menuBgmActive = false;
            try
            {
                EClass.Sound?.StopBGM();
                LayerDrama.haltPlaylist = false;
                EClass._zone?.RefreshBGM();
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Menu BGM stop failed: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════
        //  Utilities
        // ═══════════════════════════════════════════════

        private Text MakeText(Transform parent, string name, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = _font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void CenterFixed(RectTransform rt, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        private static Font ResolveElinFont()
        {
            try
            {
                var skin = SkinManager.Instance;
                var font = skin?.fontSet?.widget?.source?.font;
                if (font != null) return font;
            }
            catch { }
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Texture2D BuildNoiseTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                var v = (byte)UnityEngine.Random.Range(24, 255);
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

        private static string L(string jp, string en, string cn)
        {
            if (Lang.langCode == "CN") return cn;
            return Lang.isJP ? jp : en;
        }

        private static bool TryCopyPathToClipboard(string path)
        {
            try
            {
                var guiType = Type.GetType("UnityEngine.GUIUtility, UnityEngine.IMGUIModule");
                var prop = guiType?.GetProperty("systemCopyBuffer", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (prop != null)
                {
                    prop.SetValue(null, path, null);
                    return true;
                }
            }
            catch
            {
                // no-op
            }

            return false;
        }

        private static string FamilyLabel(string family)
        {
            switch (DoomModRuleStore.NormalizeFamily(family))
            {
                case "doom1":
                    return "DOOM1";
                case "doom2":
                    return "DOOM2";
                case "any":
                    return "DUAL";
                default:
                    return "UNKNOWN";
            }
        }

        private static string BuildSaveSummaryLine(DoomSaveSummary summary)
        {
            var playtimePart = L(
                "合計 " + FormatDuration(summary.TotalPlaySeconds),
                "Playtime " + FormatDuration(summary.TotalPlaySeconds),
                "总时长 " + FormatDuration(summary.TotalPlaySeconds));

            string timePart;
            if (summary.SavedUtcTicks > 0)
            {
                try
                {
                    timePart = new DateTime(summary.SavedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                }
                catch
                {
                    timePart = "-";
                }
            }
            else
            {
                timePart = "-";
            }

            return playtimePart + "  @" + timePart;
        }

        private static string FormatDuration(int totalSeconds)
        {
            var sec = Mathf.Max(0, totalSeconds);
            var hours = sec / 3600;
            var minutes = (sec % 3600) / 60;
            var seconds = sec % 60;

            if (hours > 0)
            {
                return hours + "h " + minutes.ToString("00") + "m";
            }

            if (minutes > 0)
            {
                return minutes + "m " + seconds.ToString("00") + "s";
            }

            return seconds + "s";
        }

        private static void NormalizeSingleActiveMod(DoomRuntimeLoadout loadout)
        {
            if (loadout?.enabledModFiles == null || loadout.enabledModFiles.Count <= 1)
            {
                return;
            }

            loadout.enabledModFiles = new List<string> { loadout.enabledModFiles[0] };
            DoomWadLocator.SaveRuntimeLoadout(loadout);
        }

        // ── Forwarded from DoomSessionManager (static helpers) ──

        internal static int RemoveIncompatibleModsForIwad(DoomRuntimeLoadout loadout, string iwadFile)
        {
            if (loadout?.enabledModFiles == null || loadout.enabledModFiles.Count == 0) return 0;
            var before = loadout.enabledModFiles.Count;
            loadout.enabledModFiles = loadout.enabledModFiles
                .Where(f => !IsIwadMismatch(iwadFile, f))
                .ToList();
            return before - loadout.enabledModFiles.Count;
        }

        internal static bool IsIwadMismatch(string selectedIwadFile, string pwadFile)
        {
            var required = GetRequiredIwadFamilyForPwad(pwadFile);
            if (string.IsNullOrEmpty(required) ||
                string.Equals(required, "any", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(required, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var selectedFamily = GetIwadFamily(selectedIwadFile);
            return !string.Equals(required, selectedFamily, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetRequiredIwadFamilyForPwad(string pwadFile)
        {
            if (string.IsNullOrWhiteSpace(pwadFile))
            {
                return "unknown";
            }

            var info = DoomModRuleStore.GetRuleInfo(Path.GetFileName(pwadFile));
            if (!info.Exists)
            {
                return "unknown";
            }

            return DoomModRuleStore.NormalizeFamily(info.Family);
        }

        private static string GetIwadFamily(string iwadFile)
        {
            if (string.IsNullOrWhiteSpace(iwadFile)) return "doom1";
            var n = Path.GetFileName(iwadFile).ToLowerInvariant();
            if (n.Contains("freedoom2") || n.Contains("doom2") || n.Contains("freedm")) return "doom2";
            return "doom1";
        }
    }
}

