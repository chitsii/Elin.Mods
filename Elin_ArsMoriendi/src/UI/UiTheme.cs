using UnityEngine;

namespace Elin_ArsMoriendi
{
    public readonly struct ArsUiTheme
    {
        public ArsUiTheme(
            bool isCompatibilityMode,
            bool useInkColorsInChapterFour,
            bool useSimpleConfirmOverlay,
            bool useSimpleDivider,
            bool useSimpleStockBar,
            bool useSimpleResultCard,
            Color compatWindowBg,
            Color compatScrollBg)
        {
            IsCompatibilityMode = isCompatibilityMode;
            UseInkColorsInChapterFour = useInkColorsInChapterFour;
            UseSimpleConfirmOverlay = useSimpleConfirmOverlay;
            UseSimpleDivider = useSimpleDivider;
            UseSimpleStockBar = useSimpleStockBar;
            UseSimpleResultCard = useSimpleResultCard;
            CompatWindowBg = compatWindowBg;
            CompatScrollBg = compatScrollBg;
        }

        public bool IsCompatibilityMode { get; }
        public bool UseInkColorsInChapterFour { get; }
        public bool UseSimpleConfirmOverlay { get; }
        public bool UseSimpleDivider { get; }
        public bool UseSimpleStockBar { get; }
        public bool UseSimpleResultCard { get; }
        public Color CompatWindowBg { get; }
        public Color CompatScrollBg { get; }
    }

    public readonly struct ServantWidgetTheme
    {
        public ServantWidgetTheme(
            bool isCompatibilityMode,
            bool useMonochromeBars,
            Color bgColor,
            Color barBgColor,
            Color barOutlineColor,
            Color separatorColor,
            Color borderColor,
            Color nameColor,
            Color statTextColor,
            Color barFillColor)
        {
            IsCompatibilityMode = isCompatibilityMode;
            UseMonochromeBars = useMonochromeBars;
            BgColor = bgColor;
            BarBgColor = barBgColor;
            BarOutlineColor = barOutlineColor;
            SeparatorColor = separatorColor;
            BorderColor = borderColor;
            NameColor = nameColor;
            StatTextColor = statTextColor;
            BarFillColor = barFillColor;
        }

        public bool IsCompatibilityMode { get; }
        public bool UseMonochromeBars { get; }
        public Color BgColor { get; }
        public Color BarBgColor { get; }
        public Color BarOutlineColor { get; }
        public Color SeparatorColor { get; }
        public Color BorderColor { get; }
        public Color NameColor { get; }
        public Color StatTextColor { get; }
        public Color BarFillColor { get; }
    }

    public static class UiTheme
    {
        public static bool IsCompatibilityMode => ModConfig.EnableUiCompatibilityMode.Value;

        public static ArsUiTheme Ars =>
            IsCompatibilityMode ? ArsCompat : ArsNormal;

        public static ServantWidgetTheme ServantWidget =>
            IsCompatibilityMode ? WidgetCompat : WidgetNormal;

        private static readonly ArsUiTheme ArsNormal = new(
            isCompatibilityMode: false,
            useInkColorsInChapterFour: true,
            useSimpleConfirmOverlay: false,
            useSimpleDivider: false,
            useSimpleStockBar: false,
            useSimpleResultCard: false,
            compatWindowBg: Color.clear,
            compatScrollBg: Color.clear);

        private static readonly ArsUiTheme ArsCompat = new(
            isCompatibilityMode: true,
            useInkColorsInChapterFour: false,
            useSimpleConfirmOverlay: true,
            useSimpleDivider: true,
            useSimpleStockBar: true,
            useSimpleResultCard: true,
            compatWindowBg: new Color(0.96f, 0.95f, 0.94f, 1f),
            compatScrollBg: new Color(0.93f, 0.92f, 0.91f, 1f));

        private static readonly ServantWidgetTheme WidgetNormal = new(
            isCompatibilityMode: false,
            useMonochromeBars: false,
            bgColor: new Color(0.06f, 0.05f, 0.04f, 0.70f),
            barBgColor: new Color(0.12f, 0.10f, 0.07f, 0.90f),
            barOutlineColor: new Color(0.25f, 0.20f, 0.14f, 0.60f),
            separatorColor: new Color(0.30f, 0.25f, 0.15f, 0.25f),
            borderColor: new Color(0.40f, 0.32f, 0.18f, 0.50f),
            nameColor: new Color(0.88f, 0.82f, 0.68f),
            statTextColor: new Color(0.70f, 0.65f, 0.55f),
            barFillColor: Color.clear);

        private static readonly ServantWidgetTheme WidgetCompat = new(
            isCompatibilityMode: true,
            useMonochromeBars: true,
            bgColor: new Color(0.93f, 0.93f, 0.93f, 0.80f),
            barBgColor: new Color(0.82f, 0.80f, 0.78f, 0.90f),
            barOutlineColor: new Color(0.55f, 0.53f, 0.50f, 0.70f),
            separatorColor: new Color(0.70f, 0.68f, 0.65f, 0.40f),
            borderColor: new Color(0.45f, 0.42f, 0.38f, 0.60f),
            nameColor: new Color(0.08f, 0.08f, 0.08f, 1f),
            statTextColor: new Color(0.22f, 0.22f, 0.22f, 1f),
            barFillColor: new Color(0.28f, 0.28f, 0.28f, 1f));
    }
}
