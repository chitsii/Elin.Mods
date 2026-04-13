using UnityEngine;

namespace Elin_Elinikki.Quest.Placement
{
    /// <summary>
    /// Solid-color placeholder texture for the Elinikki trace
    /// placements. A single 128x128 neutral texture (white fill with
    /// a 55% gray two-pixel border) is shared across every trace;
    /// per-trace color comes from the material tint path that
    /// <see cref="SharedWorldObjectManager.ApplyNormalProxyMaterial"/>
    /// already drives off <c>definition.Color</c>.
    ///
    /// <para>Why neutral, not pre-colored: the normal proxy material
    /// always multiplies the bound texture by <c>_Color</c>, so a
    /// pre-tinted placeholder texture would double-apply the trace
    /// color and render noticeably darker than the authored
    /// <c>ChannelColor</c> / <c>MarkColor</c> / etc. Leaving the fill
    /// white lets the existing tint pipeline compose correctly.</para>
    ///
    /// <para>These are explicit placeholders. Phase 7 / Task 7.1
    /// swaps them out for the real 512x512 photorealistic textures
    /// required by AGENTS.md, at which point per-trace source files
    /// will replace this shared neutral bitmap. Keeping placeholders
    /// in code rather than bundled <c>.png</c> files means the mod
    /// package stays textureless until the real art ships, and
    /// simplifies the Task 3.6 FPS-verify pass: any visible gap in
    /// the placeholder pipeline is a regression, not a missing
    /// asset.</para>
    ///
    /// <para>The texture is created on first access and cached for
    /// the lifetime of the mod. It uses
    /// <c>HideFlags.HideAndDontSave</c> so the editor and save files
    /// never serialize it.</para>
    /// </summary>
    internal static class ElinikkiPlaceholderTextures
    {
        private const int Size = 128;
        private const int BorderWidth = 2;

        private static Texture2D _neutral;

        /// <summary>
        /// Returns the shared neutral placeholder texture, creating
        /// it on the first call. Callers should bind this to the
        /// normal-proxy <c>TextureOverride</c> and leave the
        /// material tint to carry the actual trace color.
        /// </summary>
        public static Texture2D GetOrCreateNeutral()
        {
            if (_neutral != null)
            {
                return _neutral;
            }

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "ElinikkiPlaceholderNeutral",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            Color fill = Color.white;
            Color border = new Color(0.55f, 0.55f, 0.55f, 1f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool onBorder = x < BorderWidth
                                    || y < BorderWidth
                                    || x >= Size - BorderWidth
                                    || y >= Size - BorderWidth;
                    texture.SetPixel(x, y, onBorder ? border : fill);
                }
            }

            texture.Apply(false, false);
            _neutral = texture;
            return _neutral;
        }
    }
}
