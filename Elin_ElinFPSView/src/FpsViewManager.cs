using System;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsViewManager : MonoBehaviour
    {
        private FpsOverlayDisplay _overlay;
        private FpsRenderer _renderer;
        private float _nextRenderTime;
        private float _yawRadians;
        private float _pitchOffset;

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
                _renderer = new FpsRenderer();
                _renderer.Initialize(
                    Math.Max(1, Plugin.Settings.RenderWidth.Value),
                    Math.Max(1, Plugin.Settings.RenderHeight.Value));

                _overlay = gameObject.AddComponent<FpsOverlayDisplay>();
                _overlay.Initialize(_renderer.Width, _renderer.Height);
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
                _nextRenderTime = Time.unscaledTime + (1f / 60f);
                _overlay.Upload(_renderer.RenderFrame(new FpsViewState
                {
                    HasCustomYaw = true,
                    YawRadians = _yawRadians,
                    PitchOffset = _pitchOffset
                }));
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
            ReleaseCursor();
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
            _yawRadians += Input.GetAxisRaw("Mouse X") * Plugin.Settings.LookSensitivity.Value;
            _pitchOffset = Mathf.Clamp(
                _pitchOffset + Input.GetAxisRaw("Mouse Y") * Plugin.Settings.PitchSensitivity.Value,
                -0.35f,
                0.35f);
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
