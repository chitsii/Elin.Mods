using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsGpuPreviewRenderer : IDisposable
    {
        private const int RenderLayer = 29;
        private const float RenderHeightOffset = 0.01f;
        private const int MaxPreviewRadius = 8;
        private const float GpuEyeHeightScale = 1.0f;
        private static readonly Color ClearColor = new Color32(34, 40, 52, 255);
        private static Mesh _terrainPlaneMesh;

        private readonly FpsIdealizedWorld _idealizedWorld = new FpsIdealizedWorld();
        private readonly FpsGpuFloorAtlasBaker _floorAtlasBaker = new FpsGpuFloorAtlasBaker();
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private readonly List<GameObject> _terrainQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _terrainRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _terrainFilters = new List<MeshFilter>(256);
        private readonly List<GameObject> _terrainOverlayQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _terrainOverlayRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _terrainOverlayFilters = new List<MeshFilter>(256);
        private GameObject _root;
        private GameObject _terrainRoot;
        private GameObject _terrainOverlayRoot;
        private Camera _camera;
        private RenderTexture _renderTexture;
        private Material _terrainMaterial;
        private Material _terrainOverlayMaterial;
        private int _width;
        private int _height;
        private bool _loggedDebugFrame;

        public Texture OutputTexture => _renderTexture;

        public void Initialize(int width, int height)
        {
            _width = Mathf.Max(1, width);
            _height = Mathf.Max(1, height);

            CreateRenderTexture();
            CreateCamera();
            CreateRoot();
            EnsureTerrainPool(GetPreviewDiameter() * GetPreviewDiameter());
        }

        public void RenderFrame(FpsViewState viewState)
        {
            if (_camera == null || _root == null || !TryResolveViewPose(viewState, out GpuViewPose pose))
            {
                return;
            }

            _idealizedWorld.PrepareFrame();
            UpdateCamera(pose);
            UpdateTerrainPreview(pose);

            _root.SetActive(true);
            _camera.Render();
            _root.SetActive(false);
        }

        public void Dispose()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
                _terrainRoot = null;
                _terrainOverlayRoot = null;
            }

            if (_camera != null)
            {
                UnityEngine.Object.Destroy(_camera.gameObject);
                _camera = null;
            }

            if (_terrainMaterial != null)
            {
                UnityEngine.Object.Destroy(_terrainMaterial);
                _terrainMaterial = null;
            }

            if (_terrainOverlayMaterial != null)
            {
                UnityEngine.Object.Destroy(_terrainOverlayMaterial);
                _terrainOverlayMaterial = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }

            _terrainQuads.Clear();
            _terrainRenderers.Clear();
            _terrainOverlayQuads.Clear();
            _terrainOverlayRenderers.Clear();
        }

        private void CreateRenderTexture()
        {
            _renderTexture = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                name = "ElinFPSViewGpuPreview"
            };
            _renderTexture.Create();
        }

        private void CreateCamera()
        {
            GameObject cameraGo = new GameObject("ElinFPSViewGpuCamera");
            cameraGo.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(cameraGo);

            _camera = cameraGo.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = ClearColor;
            _camera.nearClipPlane = 0.03f;
            _camera.farClipPlane = Mathf.Max(32f, Plugin.Settings.MaxDistance.Value + 12f);
            _camera.cullingMask = 1 << RenderLayer;
            _camera.targetTexture = _renderTexture;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.orthographic = false;
        }

        private void CreateRoot()
        {
            _root = new GameObject("ElinFPSViewGpuRoot");
            _root.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(_root);
            SetLayerRecursively(_root, RenderLayer);

            _terrainRoot = new GameObject("TerrainPreview");
            _terrainRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_terrainRoot, RenderLayer);

            _terrainOverlayRoot = new GameObject("TerrainOverlayPreview");
            _terrainOverlayRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_terrainOverlayRoot, RenderLayer);

            Shader terrainShader = Shader.Find("Sprites/Default");
            if (terrainShader == null)
            {
                terrainShader = Shader.Find("Unlit/Transparent");
            }
            if (terrainShader == null)
            {
                terrainShader = Shader.Find("Unlit/Texture");
            }

            _terrainMaterial = terrainShader != null
                ? new Material(terrainShader) { hideFlags = HideFlags.HideAndDontSave }
                : null;
            if (_terrainMaterial != null)
            {
                _terrainMaterial.mainTexture = Texture2D.whiteTexture;
                if (_terrainMaterial.HasProperty("_Color"))
                {
                    _terrainMaterial.SetColor("_Color", Color.white);
                }

                if (_terrainMaterial.HasProperty("_Cull"))
                {
                    _terrainMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                }
            }

            _terrainOverlayMaterial = terrainShader != null
                ? new Material(terrainShader) { hideFlags = HideFlags.HideAndDontSave }
                : null;
            if (_terrainOverlayMaterial != null)
            {
                _terrainOverlayMaterial.mainTexture = Texture2D.whiteTexture;
                if (_terrainOverlayMaterial.HasProperty("_Color"))
                {
                    _terrainOverlayMaterial.SetColor("_Color", Color.white);
                }

                if (_terrainOverlayMaterial.HasProperty("_Cull"))
                {
                    _terrainOverlayMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                }
            }

            _root.SetActive(false);
        }

        private void UpdateCamera(GpuViewPose pose)
        {
            float eyeHeight = Mathf.Max(0.05f, Plugin.Settings.EyeHeight.Value * GpuEyeHeightScale);
            float pitchDegrees = pose.PitchOffset * 90f;
            float yawDegrees = Mathf.Atan2(pose.Forward.y, pose.Forward.x) * Mathf.Rad2Deg;

            _camera.fieldOfView = Mathf.Clamp(Plugin.Settings.FieldOfViewDegrees.Value, 30f, 120f);
            _camera.aspect = _width / (float)_height;
            _camera.transform.position = new Vector3(
                pose.CameraOrigin.x,
                pose.CameraGroundHeight + eyeHeight,
                pose.CameraOrigin.y);
            Vector3 lookDirection = new Vector3(
                pose.Forward.x,
                Mathf.Sin(pose.PitchOffset * Mathf.PI * 0.5f),
                pose.Forward.y).normalized;
            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = Vector3.forward;
            }

            _camera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        private void UpdateTerrainPreview(GpuViewPose pose)
        {
            int radius = Mathf.Min(Mathf.CeilToInt(Mathf.Max(1f, Plugin.Settings.MaxDistance.Value)), MaxPreviewRadius);
            int minX = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.x) - radius);
            int maxX = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.x) + radius);
            int minZ = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.y) - radius);
            int maxZ = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.y) + radius);

            int activeIndex = 0;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Cell cell = EClass._map.cells[x, z];
                    bool hasSurface = _idealizedWorld.TryResolveFloor(cell, cell?.index ?? -1, out FpsResolvedFloorSurface surface);

                    EnsureTerrainPool(activeIndex + 1);
                    GameObject quad = _terrainQuads[activeIndex];
                    MeshRenderer renderer = _terrainRenderers[activeIndex];
                    quad.SetActive(true);
                    _terrainOverlayQuads[activeIndex].SetActive(false);

                    float surfaceHeight = FpsIdealizedWorld.GetCellSurfaceHeight(cell) + RenderHeightOffset;
                    quad.transform.localPosition = new Vector3(x + 0.5f, surfaceHeight, z + 0.5f);
                    quad.transform.localRotation = Quaternion.identity;
                    quad.transform.localScale = Vector3.one;

                    _propertyBlock.Clear();
                    if (hasSurface)
                    {
                        ApplyTerrainTexture(_terrainFilters[activeIndex], surface);
                        _propertyBlock.SetColor("_Color", ResolvePreviewTint(surface));
                    }
                    else
                    {
                        _propertyBlock.SetTexture("_MainTex", Texture2D.whiteTexture);
                        _propertyBlock.SetColor("_Color", new Color32(200, 80, 200, 255));
                    }
                    renderer.SetPropertyBlock(_propertyBlock);
                    activeIndex++;
                }
            }

            for (int i = activeIndex; i < _terrainQuads.Count; i++)
            {
                _terrainQuads[i].SetActive(false);
                _terrainOverlayQuads[i].SetActive(false);
            }

            LogDebugFrame(activeIndex, pose);
        }

        private Color ResolvePreviewTint(FpsResolvedFloorSurface surface)
        {
            Color32 baseColor;
            if (surface.UseSnowAtlas)
            {
                baseColor = new Color32(255, 255, 255, 255);
            }
            else if (surface.Floor?.tileType.IsWater == true)
            {
                baseColor = new Color32(236, 244, 255, 255);
            }
            else
            {
                baseColor = new Color32(255, 255, 255, 255);
            }

            Color32 color = FpsIdealizedWorld.ApplyMatTint(baseColor, surface.MaterialColor);
            Color32 lit = FpsLightApplicator.ApplySample(color, surface.Light);
            return new Color32(lit.r, lit.g, lit.b, 255);
        }

        private void ApplyTerrainTexture(MeshFilter filter, FpsResolvedFloorSurface surface)
        {
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            if (_floorAtlasBaker.TryGetCompositeTexture(surface, out Texture texture))
            {
                _propertyBlock.SetTexture("_MainTex", texture);
                SetQuadUv(filter.sharedMesh, new Rect(0f, 0f, 1f, 1f));
                return;
            }

            _propertyBlock.SetTexture("_MainTex", Texture2D.whiteTexture);
            SetQuadUv(filter.sharedMesh, new Rect(0f, 0f, 1f, 1f));
        }

        private void EnsureTerrainPool(int count)
        {
            while (_terrainQuads.Count < count)
            {
                GameObject quad = new GameObject($"GpuTerrainQuad_{_terrainQuads.Count}");
                quad.name = $"GpuTerrainQuad_{_terrainQuads.Count}";
                quad.hideFlags = HideFlags.HideAndDontSave;
                quad.transform.SetParent(_terrainRoot.transform, false);
                SetLayerRecursively(quad, RenderLayer);

                MeshFilter filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = GetTerrainPlaneMesh();

                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _terrainMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                if (filter != null && filter.sharedMesh != null)
                {
                    filter.sharedMesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                }

                _terrainQuads.Add(quad);
                _terrainRenderers.Add(renderer);
                _terrainFilters.Add(filter);
                GameObject overlayQuad = new GameObject($"GpuTerrainOverlayQuad_{_terrainOverlayQuads.Count}");
                overlayQuad.hideFlags = HideFlags.HideAndDontSave;
                overlayQuad.transform.SetParent(_terrainOverlayRoot.transform, false);
                SetLayerRecursively(overlayQuad, RenderLayer);

                MeshFilter overlayFilter = overlayQuad.AddComponent<MeshFilter>();
                overlayFilter.sharedMesh = GetTerrainPlaneMesh();
                MeshRenderer overlayRenderer = overlayQuad.AddComponent<MeshRenderer>();
                overlayRenderer.sharedMaterial = _terrainOverlayMaterial;
                overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                overlayRenderer.receiveShadows = false;
                if (overlayFilter.sharedMesh != null)
                {
                    overlayFilter.sharedMesh = UnityEngine.Object.Instantiate(overlayFilter.sharedMesh);
                }

                _terrainOverlayQuads.Add(overlayQuad);
                _terrainOverlayRenderers.Add(overlayRenderer);
                _terrainOverlayFilters.Add(overlayFilter);
                quad.SetActive(false);
                overlayQuad.SetActive(false);
            }
        }

        private static Mesh GetTerrainPlaneMesh()
        {
            if (_terrainPlaneMesh != null)
            {
                return _terrainPlaneMesh;
            }

            Mesh mesh = new Mesh
            {
                name = "FpsGpuTerrainPlane"
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            mesh.normals = new[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1,
                2, 3, 1
            };
            mesh.RecalculateBounds();
            _terrainPlaneMesh = mesh;
            return _terrainPlaneMesh;
        }

        private void LogDebugFrame(int activeTerrainQuads, GpuViewPose pose)
        {
            if (_loggedDebugFrame || Plugin.Log == null)
            {
                return;
            }

            _loggedDebugFrame = true;
            Vector3 camPos = _camera != null ? _camera.transform.position : Vector3.zero;
            string firstQuad = activeTerrainQuads > 0 && _terrainQuads.Count > 0
                ? _terrainQuads[0].transform.position.ToString("F3")
                : "none";
            Plugin.Log.LogInfo(
                $"GPU preview debug: cam={camPos:F3}, player={pose.PlayerOrigin:F3}, ground={pose.CameraGroundHeight:F3}, " +
                $"forward={pose.Forward:F3}, terrainQuads={activeTerrainQuads}, firstTerrain={firstQuad}");
        }

        private static void SetQuadUv(Mesh mesh, Rect uvRect)
        {
            if (mesh == null)
            {
                return;
            }

            mesh.uv = new[]
            {
                new Vector2(uvRect.xMin, uvRect.yMin),
                new Vector2(uvRect.xMax, uvRect.yMin),
                new Vector2(uvRect.xMin, uvRect.yMax),
                new Vector2(uvRect.xMax, uvRect.yMax)
            };
        }

        private static int GetPreviewDiameter()
        {
            return MaxPreviewRadius * 2 + 1;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
            }
        }

        private static bool TryResolveViewPose(FpsViewState viewState, out GpuViewPose pose)
        {
            pose = default;
            if (EClass.core == null || !EClass.core.IsGameStarted || EClass.pc == null || EClass._map == null || EClass._map.Size <= 0)
            {
                return false;
            }

            Vector2 playerOrigin = new Vector2(EClass.pc.pos.x + 0.5f, EClass.pc.pos.z + 0.5f);
            Vector2 forward = viewState.HasCustomYaw
                ? new Vector2(Mathf.Cos(viewState.YawRadians), Mathf.Sin(viewState.YawRadians))
                : DirToVector(EClass.pc.dir);
            Vector2 cameraOrigin = ResolveCameraOrigin(playerOrigin, forward, EClass._map.Size, Mathf.Max(0f, viewState.CameraDistance));
            float pitchOffset = Mathf.Clamp(viewState.PitchOffset, -0.65f, 0.55f);
            float cameraGroundHeight = viewState.CameraDistance > 0.01f
                ? FpsIdealizedWorld.GetSurfaceHeightAt(playerOrigin) + viewState.CameraHeightOffset
                : FpsIdealizedWorld.GetSurfaceHeightAt(cameraOrigin) + viewState.CameraHeightOffset;

            pose = new GpuViewPose
            {
                PlayerOrigin = playerOrigin,
                CameraOrigin = cameraOrigin,
                Forward = forward.normalized,
                PitchOffset = pitchOffset,
                CameraGroundHeight = cameraGroundHeight
            };
            return true;
        }

        private static Vector2 ResolveCameraOrigin(Vector2 playerOrigin, Vector2 forward, int mapSize, float cameraDistance)
        {
            if (cameraDistance <= 0.01f)
            {
                return playerOrigin;
            }

            Vector2 lastValid = playerOrigin;
            int steps = Mathf.Max(1, Mathf.CeilToInt(cameraDistance / 0.1f));
            for (int i = 1; i <= steps; i++)
            {
                float distance = cameraDistance * (i / (float)steps);
                Vector2 candidate = playerOrigin - forward * distance;
                if (!IsWalkableCameraPoint(candidate, mapSize))
                {
                    break;
                }

                lastValid = candidate;
            }

            return lastValid;
        }

        private static bool IsWalkableCameraPoint(Vector2 point, int mapSize)
        {
            int x = Mathf.FloorToInt(point.x);
            int z = Mathf.FloorToInt(point.y);
            if (x < 0 || z < 0 || x >= mapSize || z >= mapSize)
            {
                return false;
            }

            Cell cell = EClass._map.cells[x, z];
            return cell != null && !(cell.HasFullBlock || cell.HasWallOrFence);
        }

        private static Vector2 DirToVector(int dir)
        {
            switch (dir)
            {
                case 0:
                    return Vector2.left;
                case 1:
                    return Vector2.down;
                case 2:
                    return Vector2.right;
                case 3:
                    return Vector2.up;
                default:
                    return Vector2.right;
            }
        }

        private struct GpuViewPose
        {
            public Vector2 PlayerOrigin;
            public Vector2 CameraOrigin;
            public Vector2 Forward;
            public float PitchOffset;
            public float CameraGroundHeight;
        }
    }
}
