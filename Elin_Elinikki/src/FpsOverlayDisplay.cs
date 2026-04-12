using UnityEngine;
using UnityEngine.UI;

namespace Elin_Elinikki
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
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public bool IsVisible => _canvas != null && _canvas.enabled;

        public Texture FrameTexture => _frameTex;

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

        public void SetDisplayTexture(Texture texture, bool flipVertical)
        {
            if (_image == null)
            {
                return;
            }

            _image.texture = texture;
            _image.uvRect = flipVertical
                ? new Rect(0f, 1f, 1f, -1f)
                : new Rect(0f, 0f, 1f, 1f);
            UpdateImageRect(force: true);
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
            if (_canvas != null)
            {
                UpdateImageRect(force: true);
                _canvas.enabled = true;
            }
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
            _image.uvRect = new Rect(0f, 0f, 1f, 1f);

            var rt = _image.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(_width, _height);

            _canvas.enabled = false;
            UpdateImageRect(force: true);
        }

        private void Update()
        {
            UpdateImageRect(force: false);
        }

        private void UpdateImageRect(bool force)
        {
            if (_image == null || _width <= 0 || _height <= 0)
            {
                return;
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (!force && screenWidth == _lastScreenWidth && screenHeight == _lastScreenHeight)
            {
                return;
            }

            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;

            float scaleX = (float)screenWidth / _width;
            float scaleY = (float)screenHeight / _height;
            int integerScale = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(scaleX, scaleY)));

            float targetWidth = _width * integerScale;
            float targetHeight = _height * integerScale;

            if (targetWidth > screenWidth + 0.01f || targetHeight > screenHeight + 0.01f)
            {
                float fitScale = Mathf.Min(scaleX, scaleY);
                targetWidth = _width * fitScale;
                targetHeight = _height * fitScale;
            }

            RectTransform rt = _image.rectTransform;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(targetWidth, targetHeight);
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
