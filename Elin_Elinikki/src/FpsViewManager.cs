using System;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class FpsViewManager : MonoBehaviour
    {
        private static FpsViewManager _instance;

        private FpsOverlayDisplay _overlay;
        private FpsGpuPreviewRenderer _gpuPreviewRenderer;
        private float _nextRenderTime;
        private float _yawRadians;
        private float _targetYawRadians;
        private float _yawVelocityDegreesPerSecond;
        private float _pitchOffset;
        private float _cameraDistance;
        private int _renderWidth;
        private int _renderHeight;
        private float _nextDungeonMoveTime;
        private int _lastDungeonMoveDirection;

        private const float CameraDistanceStep = 0.75f;
        private const float CameraDistanceMin = 0f;
        private const float CameraDistanceMax = 4.5f;
        private const float ThirdPersonHeightOffset = 0.35f;
        private const float PitchMin = -0.65f;
        private const float PitchMax = 0.55f;
        private const float DungeonMoveInitialRepeat = 0.28f;
        private const float DungeonMoveRepeat = 0.12f;
        private const float DungeonTurnSmoothTime = 0.15f;
        private const float DungeonMoveSettleThreshold = 0.1f;

        public static bool IsFpsViewActive => _instance != null && _instance._overlay != null && _instance._overlay.IsVisible;

        public static float CurrentYawRadians => _instance?._yawRadians ?? 0f;

        internal static bool IsDungeonCrawlerModeActive =>
            IsFpsViewActive && Plugin.Settings?.EnableDungeonCrawlerControls?.Value == true;

        internal static FpsViewManager Instance => _instance;

        internal static Transform SharedWorldPreviewRoot => _instance?._gpuPreviewRenderer?.SharedWorldRoot;

        public static void EnsureCreated()
        {
            if (FindObjectOfType<FpsViewManager>() != null)
            {
                return;
            }

            var managerObject = new GameObject("ElinikkiManager");
            DontDestroyOnLoad(managerObject);
            managerObject.hideFlags = HideFlags.HideAndDontSave;
            managerObject.AddComponent<FpsViewManager>();
        }

        private void Awake()
        {
            try
            {
                _instance = this;
                _renderWidth = Math.Max(1, Plugin.Settings.RenderWidth.Value);
                _renderHeight = Math.Max(1, Plugin.Settings.RenderHeight.Value);

                _overlay = gameObject.AddComponent<FpsOverlayDisplay>();
                _overlay.Initialize(_renderWidth, _renderHeight);

                _gpuPreviewRenderer = new FpsGpuPreviewRenderer();
                _gpuPreviewRenderer.Initialize(_renderWidth, _renderHeight);
                _overlay.SetDisplayTexture(_gpuPreviewRenderer.OutputTexture, flipVertical: false);
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
                        _gpuPreviewRenderer?.ResetDiagnostics();
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
                    UpdateActiveViewInput();
                    return;
                }

                UpdateActiveViewInput();
                _nextRenderTime = Time.unscaledTime + (1f / 60f);
                FpsViewState viewState = new FpsViewState
                {
                    HasCustomYaw = true,
                    YawRadians = _yawRadians,
                    PitchOffset = _pitchOffset,
                    CameraDistance = _cameraDistance,
                    CameraHeightOffset = _cameraDistance > 0.01f ? ThirdPersonHeightOffset : 0f
                };

                _gpuPreviewRenderer?.RenderFrame(viewState);
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

        private static bool CanRenderFrame()
        {
            return EClass.core != null
                && EClass.core.IsGameStarted
                && EClass.pc != null
                && EClass.pc.pos != null
                && EClass._map != null
                && EClass._map.cells != null;
        }

        internal bool IsOverlayVisibleForTests => _overlay != null && _overlay.IsVisible;

        internal FpsGpuPreviewRenderer GpuPreviewRendererForTests => _gpuPreviewRenderer;

        internal FpsViewState CaptureViewStateForTests()
        {
            return new FpsViewState
            {
                HasCustomYaw = true,
                YawRadians = _yawRadians,
                PitchOffset = _pitchOffset,
                CameraDistance = _cameraDistance,
                CameraHeightOffset = _cameraDistance > 0.01f ? ThirdPersonHeightOffset : 0f
            };
        }

        internal void ApplyViewStateForTests(FpsViewState state)
        {
            _yawRadians = state.YawRadians;
            _targetYawRadians = state.YawRadians;
            _yawVelocityDegreesPerSecond = 0f;
            _pitchOffset = Mathf.Clamp(state.PitchOffset, PitchMin, PitchMax);
            _cameraDistance = Mathf.Clamp(state.CameraDistance, CameraDistanceMin, CameraDistanceMax);
        }

        internal void AimAtTileForTests(Vector3 tilePosition, float cameraDistance = 0f, float pitchOffset = 0f)
        {
            Vector3 pcTile = EClass.pc != null
                ? new Vector3(EClass.pc.pos.x + 0.5f, 0f, EClass.pc.pos.z + 0.5f)
                : Vector3.zero;
            Vector2 delta = new Vector2(tilePosition.x - pcTile.x, tilePosition.z - pcTile.z);
            if (delta.sqrMagnitude <= 0.0001f)
            {
                SyncViewToPlayer();
            }
            else
            {
                _yawRadians = Mathf.Atan2(delta.y, delta.x) + Mathf.PI;
            }

            _targetYawRadians = _yawRadians;
            _yawVelocityDegreesPerSecond = 0f;
            _pitchOffset = Mathf.Clamp(pitchOffset, PitchMin, PitchMax);
            _cameraDistance = Mathf.Clamp(cameraDistance, CameraDistanceMin, CameraDistanceMax);
        }

        internal void ShowOverlayForTests()
        {
            if (_overlay == null)
            {
                return;
            }

            if (!_overlay.IsVisible)
            {
                _overlay.Show();
            }

            SyncViewToPlayer();
            _gpuPreviewRenderer?.ResetDiagnostics();
        }

        internal void HideOverlayForTests()
        {
            if (_overlay != null && _overlay.IsVisible)
            {
                _overlay.Hide();
            }
        }

        internal void ForceRenderForTests()
        {
            if (!CanRenderFrame())
            {
                return;
            }

            FpsViewState viewState = new FpsViewState
            {
                HasCustomYaw = true,
                YawRadians = _yawRadians,
                PitchOffset = _pitchOffset,
                CameraDistance = _cameraDistance,
                CameraHeightOffset = _cameraDistance > 0.01f ? ThirdPersonHeightOffset : 0f
            };

            _gpuPreviewRenderer?.RenderFrame(viewState);
        }

        internal static void SyncViewToPlayerForDungeonCrawler()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.SetViewYawImmediate(DirToRadians(EClass.pc != null ? EClass.pc.dir : 2));
            _instance._cameraDistance = 0f;
            _instance._nextRenderTime = 0f;
        }

        private void SyncViewToPlayer()
        {
            SetViewYawImmediate(DirToRadians(EClass.pc != null ? EClass.pc.dir : 2));
            _pitchOffset = 0f;
        }

        private void UpdateActiveViewInput()
        {
            if (IsDungeonCrawlerModeActive)
            {
                HandleDungeonCrawlerControls();
                _cameraDistance = 0f;
                UpdateDungeonCrawlerPitch();
                UpdateDungeonCrawlerYaw();
                return;
            }

            UpdateMouseLook();
            UpdateCameraDistance();
        }

        private void HandleDungeonCrawlerControls()
        {
            if (EClass.pc == null || (EClass.ui != null && EClass.ui.BlockActions))
            {
                _lastDungeonMoveDirection = 0;
                return;
            }

            if (IsTurnLeftPressedDown())
            {
                EClass.pc.Rotate(reverse: false);
                SetDungeonCrawlerYawTargetToPlayer();
                _lastDungeonMoveDirection = 0;
                return;
            }

            if (IsTurnRightPressedDown())
            {
                EClass.pc.Rotate(reverse: true);
                SetDungeonCrawlerYawTargetToPlayer();
                _lastDungeonMoveDirection = 0;
                return;
            }

            int moveDirection = 0;
            if (IsForwardPressed())
            {
                moveDirection = 1;
            }
            else if (IsBackwardPressed())
            {
                moveDirection = -1;
            }

            if (moveDirection == 0)
            {
                _lastDungeonMoveDirection = 0;
                return;
            }

            bool pressedDown = moveDirection > 0 ? IsForwardPressedDown() : IsBackwardPressedDown();
            bool directionChanged = moveDirection != _lastDungeonMoveDirection;
            bool shouldStep = pressedDown || directionChanged || Time.unscaledTime >= _nextDungeonMoveTime;
            if (!shouldStep)
            {
                return;
            }

            if (!pressedDown && !directionChanged && !IsDungeonCrawlerMovementSettled())
            {
                return;
            }

            TryDungeonCrawlerStep(moveDirection);
            _lastDungeonMoveDirection = moveDirection;
            _nextDungeonMoveTime = Time.unscaledTime + (pressedDown || directionChanged
                ? DungeonMoveInitialRepeat
                : DungeonMoveRepeat);
        }

        private static void TryDungeonCrawlerStep(int moveDirection)
        {
            if (EClass.pc == null || EClass.pc.pos == null)
            {
                return;
            }

            Point destination = ResolvePointInFacingDirection(EClass.pc.pos, EClass.pc.dir, moveDirection);
            if (destination == null)
            {
                return;
            }

            int originalDir = EClass.pc.dir;
            Chara.MoveResult result = EClass.pc.TryMove(destination, allowDestroyPath: true);
            if (moveDirection < 0 && EClass.pc.dir != originalDir)
            {
                EClass.pc.SetDir(originalDir);
            }

            if (result != Chara.MoveResult.Fail)
            {
                SyncViewToPlayerForDungeonCrawler();
            }
        }

        private static Point ResolvePointInFacingDirection(Point origin, int dir, int moveDirection)
        {
            if (origin == null || moveDirection == 0)
            {
                return null;
            }

            int dx = 0;
            int dz = 0;
            switch (dir)
            {
                case 0:
                    dz = -1;
                    break;
                case 1:
                    dx = 1;
                    break;
                case 2:
                    dz = 1;
                    break;
                case 3:
                    dx = -1;
                    break;
                default:
                    dx = 0;
                    break;
            }

            dx *= moveDirection;
            dz *= moveDirection;
            Point point = origin.Copy();
            point.x += dx;
            point.z += dz;
            return point;
        }

        private static bool IsForwardPressed()
        {
            return IsKeyPressed(EInput.keys.axisUp.key) || Input.GetKey(KeyCode.UpArrow);
        }

        private static bool IsBackwardPressed()
        {
            return IsKeyPressed(EInput.keys.axisDown.key) || Input.GetKey(KeyCode.DownArrow);
        }

        private static bool IsTurnLeftPressedDown()
        {
            return IsKeyPressedDown(EInput.keys.axisLeft.key) || Input.GetKeyDown(KeyCode.LeftArrow);
        }

        private static bool IsTurnRightPressedDown()
        {
            return IsKeyPressedDown(EInput.keys.axisRight.key) || Input.GetKeyDown(KeyCode.RightArrow);
        }

        private static bool IsForwardPressedDown()
        {
            return IsKeyPressedDown(EInput.keys.axisUp.key) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        private static bool IsBackwardPressedDown()
        {
            return IsKeyPressedDown(EInput.keys.axisDown.key) || Input.GetKeyDown(KeyCode.DownArrow);
        }

        private static bool IsDungeonCrawlerMovementSettled()
        {
            if (EClass.pc?.renderer == null || EClass.screen == null || EClass.screen.tileMap == null || EClass._zone.IsRegion)
            {
                return true;
            }

            float alignX = EClass.screen.tileAlign.x;
            float alignY = EClass.screen.tileAlign.y;
            if (Mathf.Abs(alignX) < 0.0001f || Mathf.Abs(alignY) < 0.0001f)
            {
                return true;
            }

            Vector2 logicalOrigin = new Vector2(EClass.pc.pos.x + 0.5f, EClass.pc.pos.z + 0.5f);
            byte height = EClass.pc.pos.cell.bridgeHeight == 0 ? EClass.pc.pos.cell.height : EClass.pc.pos.cell.bridgeHeight;
            float renderX = EClass.pc.renderer.position.x;
            float renderY = EClass.pc.renderer.position.y - height * EClass.screen.tileMap._heightMod.y;
            float sum = renderX / alignX;
            float diff = renderY / alignY;
            float x = (sum - diff) * 0.5f;
            float z = (sum + diff) * 0.5f;
            if (float.IsNaN(x) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(z))
            {
                return true;
            }

            Vector2 projectedOrigin = new Vector2(x + 0.5f, z + 0.5f);
            return FpsViewOriginResolver.IsAxisSettled(
                logicalOrigin,
                projectedOrigin,
                EClass.pc.dir,
                DungeonMoveSettleThreshold);
        }

        private static bool IsKeyPressed(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKey(key);
        }

        private static bool IsKeyPressedDown(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        private void UpdateDungeonCrawlerYaw()
        {
            if (EClass.pc == null)
            {
                return;
            }

            float targetDegrees = _targetYawRadians * Mathf.Rad2Deg;
            float currentDegrees = _yawRadians * Mathf.Rad2Deg;
            float smoothedDegrees = Mathf.SmoothDampAngle(
                currentDegrees,
                targetDegrees,
                ref _yawVelocityDegreesPerSecond,
                DungeonTurnSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            _yawRadians = smoothedDegrees * Mathf.Deg2Rad;
        }

        private void UpdateDungeonCrawlerPitch()
        {
            _pitchOffset = Mathf.Clamp(
                _pitchOffset + Input.GetAxisRaw("Mouse Y") * Plugin.Settings.PitchSensitivity.Value,
                PitchMin,
                PitchMax);
        }

        private void SetDungeonCrawlerYawTargetToPlayer()
        {
            _targetYawRadians = DirToRadians(EClass.pc != null ? EClass.pc.dir : 2);
            _nextRenderTime = 0f;
        }

        private void SetViewYawImmediate(float yawRadians)
        {
            _yawRadians = yawRadians;
            _targetYawRadians = yawRadians;
            _yawVelocityDegreesPerSecond = 0f;
        }

        private void UpdateMouseLook()
        {
            _yawRadians -= Input.GetAxisRaw("Mouse X") * Plugin.Settings.LookSensitivity.Value;
            _targetYawRadians = _yawRadians;
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
                    return -Mathf.PI * 0.5f;
                case 1:
                    return 0f;
                case 2:
                    return Mathf.PI * 0.5f;
                case 3:
                    return Mathf.PI;
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
