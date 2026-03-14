using System;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsViewManager : MonoBehaviour
    {
        private static FpsViewManager _instance;

        private FpsOverlayDisplay _overlay;
        private FpsRenderer _renderer;
        private FpsGpuPreviewRenderer _gpuPreviewRenderer;
        private float _nextRenderTime;
        private float _yawRadians;
        private float _pitchOffset;
        private float _cameraDistance;

        private const float CameraDistanceStep = 0.75f;
        private const float CameraDistanceMin = 0f;
        private const float CameraDistanceMax = 4.5f;
        private const float ThirdPersonHeightOffset = 0.35f;
        private const float PitchMin = -0.65f;
        private const float PitchMax = 0.55f;

        public static bool IsFpsViewActive => _instance != null && _instance._overlay != null && _instance._overlay.IsVisible;

        public static float CurrentYawRadians => _instance?._yawRadians ?? 0f;

        public static void EnsureCreated()
        {
            if (FindObjectOfType<FpsViewManager>() != null)
            {
                return;
            }

            var managerObject = new GameObject("ElinFPSViewManager");
            DontDestroyOnLoad(managerObject);
            managerObject.hideFlags = HideFlags.HideAndDontSave;
            managerObject.AddComponent<FpsViewManager>();
        }

        private void Awake()
        {
            try
            {
                _instance = this;
                _renderer = new FpsRenderer();
                _renderer.Initialize(
                    Math.Max(1, Plugin.Settings.RenderWidth.Value),
                    Math.Max(1, Plugin.Settings.RenderHeight.Value));

                _overlay = gameObject.AddComponent<FpsOverlayDisplay>();
                _overlay.Initialize(_renderer.Width, _renderer.Height);

                if (UseGpuPreviewBackend())
                {
                    _gpuPreviewRenderer = new FpsGpuPreviewRenderer();
                    _gpuPreviewRenderer.Initialize(_renderer.Width, _renderer.Height);
                    _overlay.SetDisplayTexture(_gpuPreviewRenderer.OutputTexture, flipVertical: false);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to initialize FPS view manager: {ex}");
                enabled = false;
            }
        }

        private void Update()
        {
            try
            {
                if (Input.GetKeyDown(Plugin.Settings.ToggleKey.Value))
                {
                    _overlay.Toggle();
                    if (_overlay.IsVisible)
                    {
                        SyncViewToPlayer();
                        CaptureCursor();
                    }
                    else
                    {
                        ReleaseCursor();
                    }
                }

                if (_overlay == null || !_overlay.IsVisible)
                {
                    return;
                }

                if (!CanRenderFrame())
                {
                    return;
                }

                if (Time.unscaledTime < _nextRenderTime)
                {
                    UpdateMouseLook();
                    return;
                }

                UpdateMouseLook();
                UpdateCameraDistance();
                _nextRenderTime = Time.unscaledTime + (1f / 60f);
                FpsViewState viewState = new FpsViewState
                {
                    HasCustomYaw = true,
                    YawRadians = _yawRadians,
                    PitchOffset = _pitchOffset,
                    CameraDistance = _cameraDistance,
                    CameraHeightOffset = _cameraDistance > 0.01f ? ThirdPersonHeightOffset : 0f
                };

                if (UseGpuPreviewBackend())
                {
                    _gpuPreviewRenderer?.RenderFrame(viewState);
                }
                else
                {
                    _overlay.Upload(_renderer.RenderFrame(viewState));
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"FPS view update failed: {ex}");
            }
        }

        private void OnDisable()
        {
            ReleaseCursor();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            _gpuPreviewRenderer?.Dispose();
            _gpuPreviewRenderer = null;
            ReleaseCursor();
        }

        private static bool UseGpuPreviewBackend()
        {
            return Plugin.Settings != null && Plugin.Settings.RenderBackend.Value == FpsRenderBackend.GpuPreview;
        }

        private static bool CanRenderFrame()
        {
            return EClass.core != null
                && EClass.core.IsGameStarted
                && EClass.pc != null
                && EClass.pc.pos != null
                && EClass._map != null
                && EClass._map.cells != null;
        }

        private void SyncViewToPlayer()
        {
            _yawRadians = DirToRadians(EClass.pc != null ? EClass.pc.dir : 2);
            _pitchOffset = 0f;
        }

        private void UpdateMouseLook()
        {
            _yawRadians -= Input.GetAxisRaw("Mouse X") * Plugin.Settings.LookSensitivity.Value;
            _pitchOffset = Mathf.Clamp(
                _pitchOffset + Input.GetAxisRaw("Mouse Y") * Plugin.Settings.PitchSensitivity.Value,
                PitchMin,
                PitchMax);
        }

        private void UpdateCameraDistance()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) <= 0.001f)
            {
                return;
            }

            _cameraDistance = Mathf.Clamp(
                _cameraDistance + Mathf.Sign(scroll) * CameraDistanceStep,
                CameraDistanceMin,
                CameraDistanceMax);
        }

        private static float DirToRadians(int dir)
        {
            switch (dir)
            {
                case 0:
                    return Mathf.PI;
                case 1:
                    return Mathf.PI * 1.5f;
                case 2:
                    return 0f;
                case 3:
                    return Mathf.PI * 0.5f;
                default:
                    return 0f;
            }
        }

        private static void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
