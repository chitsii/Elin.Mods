using UnityEngine;
using UnityEngine.UI;

namespace Elin_ElinFPSView
{
    /// <summary>
    /// Minimal full-screen overlay that displays a Color32[] pixel buffer
    /// as a Texture2D on a ScreenSpaceOverlay Canvas via RawImage.
    /// Based on DoomOverlayDisplay from JustDoomIt (stripped to essentials).
    /// </summary>
    public sealed class FpsOverlayDisplay : MonoBehaviour
    {
        private Canvas _canvas;
        private RawImage _image;
        private Texture2D _frameTex;
        private Color32[] _uploadBuffer;
        private int _width;
        private int _height;

        public bool IsVisible => _canvas != null && _canvas.enabled;

        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;

            _frameTex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _uploadBuffer = new Color32[width * height];

            CreateCanvas();
        }

        public void Upload(Color32[] pixels)
        {
            if (_frameTex == null || pixels == null) return;
            FlipRows(pixels);
            _frameTex.SetPixels32(_uploadBuffer);
            _frameTex.Apply(false, false);
        }

        private void FlipRows(Color32[] pixels)
        {
            if (_uploadBuffer == null || _uploadBuffer.Length != pixels.Length)
            {
                _uploadBuffer = new Color32[pixels.Length];
            }

            for (int y = 0; y < _height; y++)
            {
                int srcRow = y * _width;
                int dstRow = (_height - 1 - y) * _width;
                for (int x = 0; x < _width; x++)
                {
                    _uploadBuffer[dstRow + x] = pixels[srcRow + x];
                }
            }
        }

        public void Show()
        {
            if (_canvas != null) _canvas.enabled = true;
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.enabled = false;
        }

        public void Toggle()
        {
            if (IsVisible) Hide(); else Show();
        }

        private void CreateCanvas()
        {
            var canvasGo = new GameObject("FpsViewCanvas");
            canvasGo.transform.SetParent(transform, false);
            DontDestroyOnLoad(canvasGo);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("FpsViewImage");
            imageGo.transform.SetParent(canvasGo.transform, false);

            _image = imageGo.AddComponent<RawImage>();
            _image.texture = _frameTex;
            _image.color = Color.white;
            _image.raycastTarget = false;

            var rt = _image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _canvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (_frameTex != null)
            {
                Destroy(_frameTex);
                _frameTex = null;
            }
        }
    }
}
