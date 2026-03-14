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
        private static Mesh _uprightPlaneMesh;

        private readonly FpsIdealizedWorld _idealizedWorld = new FpsIdealizedWorld();
        private readonly FpsGpuFloorAtlasBaker _floorAtlasBaker = new FpsGpuFloorAtlasBaker();
        private readonly FpsGpuSpriteTextureCache _spriteTextureCache = new FpsGpuSpriteTextureCache();
        private readonly FpsAtlasSampler _atlasSampler = new FpsAtlasSampler();
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private readonly List<FpsResolvedUprightSprite> _uprightSprites = new List<FpsResolvedUprightSprite>(256);
        private readonly List<FpsResolvedGroundSprite> _groundSprites = new List<FpsResolvedGroundSprite>(256);
        private readonly List<FpsResolvedEffectSprite> _effectSprites = new List<FpsResolvedEffectSprite>(64);
        private readonly Plane[] _cameraFrustumPlanes = new Plane[6];
        private readonly List<GameObject> _terrainQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _terrainRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _terrainFilters = new List<MeshFilter>(256);
        private readonly List<GameObject> _terrainOverlayQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _terrainOverlayRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _terrainOverlayFilters = new List<MeshFilter>(256);
        private readonly List<GameObject> _wallQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _wallRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _wallFilters = new List<MeshFilter>(256);
        private readonly List<GameObject> _uprightQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _uprightRenderers = new List<MeshRenderer>(256);
        private readonly List<GameObject> _groundQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _groundRenderers = new List<MeshRenderer>(256);
        private GameObject _root;
        private GameObject _terrainRoot;
        private GameObject _terrainOverlayRoot;
        private GameObject _wallRoot;
        private GameObject _uprightRoot;
        private GameObject _groundRoot;
        private Camera _camera;
        private RenderTexture _renderTexture;
        private Material _terrainMaterial;
        private Material _terrainOverlayMaterial;
        private Material _wallMaterial;
        private Material _wallDoubleSidedMaterial;
        private Material _wallDebugMaterial;
        private Material _spriteMaterial;
        private bool _runtimeMaterialsInitialized;
        private bool _loggedSkippedPassMaterials;
        private bool _loggedShaderResolution;
        private int _width;
        private int _height;
        private bool _loggedDebugFrame;
        private int _diagnosticFramesRemaining = 3;
        private GpuDiagnosticCounters _diagnostics;
        private readonly List<string> _diagnosticSamples = new List<string>(24);
        private readonly List<string> _spriteDiagnosticSamples = new List<string>(16);

        public Texture OutputTexture => _renderTexture;

        public void ResetDiagnostics()
        {
            _diagnosticFramesRemaining = 3;
            _diagnostics = default;
            _spriteDiagnosticSamples.Clear();
        }

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
            BeginDiagnosticsFrame();
            EnsureRuntimeMaterials();
            UpdateCamera(pose);
            GeometryUtility.CalculateFrustumPlanes(_camera, _cameraFrustumPlanes);
            UpdateTerrainPreview(pose);
            UpdateSpritePreview(pose, viewState.CameraDistance > 0.2f);
            LogDiagnosticsFrame(pose);

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
                _wallRoot = null;
                _uprightRoot = null;
                _groundRoot = null;
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

            if (_spriteMaterial != null)
            {
                UnityEngine.Object.Destroy(_spriteMaterial);
                _spriteMaterial = null;
            }

            if (_wallMaterial != null)
            {
                UnityEngine.Object.Destroy(_wallMaterial);
                _wallMaterial = null;
            }

            if (_wallDoubleSidedMaterial != null)
            {
                UnityEngine.Object.Destroy(_wallDoubleSidedMaterial);
                _wallDoubleSidedMaterial = null;
            }

            if (_wallDebugMaterial != null)
            {
                UnityEngine.Object.Destroy(_wallDebugMaterial);
                _wallDebugMaterial = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }

            _spriteTextureCache.Dispose();
            _terrainQuads.Clear();
            _terrainRenderers.Clear();
            _terrainOverlayQuads.Clear();
            _terrainOverlayRenderers.Clear();
            _wallQuads.Clear();
            _wallRenderers.Clear();
            _wallFilters.Clear();
            _uprightQuads.Clear();
            _uprightRenderers.Clear();
            _groundQuads.Clear();
            _groundRenderers.Clear();
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

            _wallRoot = new GameObject("Walls");
            _wallRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_wallRoot, RenderLayer);

            _groundRoot = new GameObject("GroundSprites");
            _groundRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_groundRoot, RenderLayer);

            _uprightRoot = new GameObject("UprightSprites");
            _uprightRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_uprightRoot, RenderLayer);

            Shader terrainShader = FindPreferredShader(
                "Sprites/Default",
                "Unlit/Transparent",
                "Unlit/Transparent Cutout",
                "Legacy Shaders/Transparent/Cutout/Diffuse",
                "Legacy Shaders/Transparent/Cutout/VertexLit",
                "Unlit/Texture");
            Shader wallShader = FindPreferredShader(
                "Unlit/Transparent Cutout",
                "Legacy Shaders/Transparent/Cutout/Diffuse",
                "Legacy Shaders/Transparent/Cutout/VertexLit",
                "Unlit/Texture",
                "Sprites/Default",
                "Unlit/Transparent");
            Shader spriteShader = FindPreferredShader(
                "Sprites/Default",
                "Unlit/Transparent",
                "Unlit/Texture");

            LogShaderResolution("terrain", terrainShader,
                "Sprites/Default",
                "Unlit/Transparent",
                "Unlit/Transparent Cutout",
                "Legacy Shaders/Transparent/Cutout/Diffuse",
                "Legacy Shaders/Transparent/Cutout/VertexLit",
                "Unlit/Texture");
            LogShaderResolution("wall", wallShader,
                "Unlit/Transparent Cutout",
                "Legacy Shaders/Transparent/Cutout/Diffuse",
                "Legacy Shaders/Transparent/Cutout/VertexLit",
                "Unlit/Texture",
                "Sprites/Default",
                "Unlit/Transparent");
            LogShaderResolution("sprite", spriteShader,
                "Sprites/Default",
                "Unlit/Transparent",
                "Unlit/Texture");
            _loggedShaderResolution = true;

            _terrainMaterial = CreateTexturedMaterial(terrainShader, cullOff: true, preferAlphaTestQueue: true);
            _terrainOverlayMaterial = CreateTexturedMaterial(terrainShader, cullOff: true, preferAlphaTestQueue: true);
            _spriteMaterial = CreateTexturedMaterial(spriteShader, cullOff: true, preferAlphaTestQueue: false);
            _wallMaterial = CreateTexturedMaterial(wallShader, cullOff: false, preferAlphaTestQueue: true);
            _wallDoubleSidedMaterial = CreateTexturedMaterial(wallShader, cullOff: true, preferAlphaTestQueue: true);
            _wallDebugMaterial = CreateColorMaterial(
                FindPreferredShader("Unlit/Color", "Legacy Shaders/Diffuse"),
                cullOff: true);

            _root.SetActive(false);
        }

        private void LogShaderResolution(string label, Shader resolvedShader, params string[] candidates)
        {
            if (_loggedShaderResolution || Plugin.Log == null)
            {
                return;
            }

            List<string> statuses = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                Shader shader = Shader.Find(candidates[i]);
                statuses.Add($"{candidates[i]}={(shader != null ? "found" : "missing")}");
            }

            Plugin.Log.LogInfo(
                $"GPU shader resolution [{label}]: resolved={resolvedShader?.name ?? "null"}; {string.Join(", ", statuses)}");
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

        private void EnsureRuntimeMaterials()
        {
            BaseTileMap tileMap = EClass.scene?.screenElin?.tileMap ?? EClass.screen?.tileMap;
            if (tileMap == null)
            {
                return;
            }

            LogIncompatiblePassMaterials(tileMap);

            bool updated = false;
            updated |= EnsureMaterialFromPass(ref _terrainMaterial, tileMap.passFloor, cullOff: true, preferAlphaTestQueue: true);
            updated |= EnsureMaterialFromPass(ref _terrainOverlayMaterial, tileMap.passFloor, cullOff: true, preferAlphaTestQueue: true);
            updated |= EnsureMaterialFromPass(ref _wallMaterial, tileMap.passBlock, cullOff: false, preferAlphaTestQueue: true);
            updated |= EnsureMaterialFromPass(ref _wallDoubleSidedMaterial, tileMap.passBlock, cullOff: true, preferAlphaTestQueue: true);
            updated |= EnsureMaterialFromPass(ref _spriteMaterial, tileMap.passObj ?? tileMap.passChara, cullOff: true, preferAlphaTestQueue: false);

            if (!updated && _runtimeMaterialsInitialized)
            {
                return;
            }

            _runtimeMaterialsInitialized = true;

            for (int i = 0; i < _terrainRenderers.Count; i++)
            {
                _terrainRenderers[i].sharedMaterial = _terrainMaterial;
            }

            for (int i = 0; i < _terrainOverlayRenderers.Count; i++)
            {
                _terrainOverlayRenderers[i].sharedMaterial = _terrainOverlayMaterial;
            }

            for (int i = 0; i < _wallRenderers.Count; i++)
            {
                _wallRenderers[i].sharedMaterial = ResolveWallRendererMaterial(false);
            }

            for (int i = 0; i < _uprightRenderers.Count; i++)
            {
                _uprightRenderers[i].sharedMaterial = _spriteMaterial;
            }

            for (int i = 0; i < _groundRenderers.Count; i++)
            {
                _groundRenderers[i].sharedMaterial = _spriteMaterial;
            }
        }

        private Material ResolveWallRendererMaterial(bool doubleSided)
        {
            if (UseSolidFaceDebug() && _wallDebugMaterial != null)
            {
                return _wallDebugMaterial;
            }

            return doubleSided ? _wallDoubleSidedMaterial ?? _wallMaterial : _wallMaterial;
        }

        private static Shader FindPreferredShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }

        private static Material CreateTexturedMaterial(Shader shader, bool cullOff, bool preferAlphaTestQueue)
        {
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = Texture2D.whiteTexture
            };

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)(cullOff ? UnityEngine.Rendering.CullMode.Off : UnityEngine.Rendering.CullMode.Back));
            }

            if (material.HasProperty("_Cutoff"))
            {
                material.SetFloat("_Cutoff", 0.1f);
            }

            material.renderQueue = preferAlphaTestQueue
                ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
                : material.renderQueue;
            return material;
        }

        private static Material CreateColorMaterial(Shader shader, bool cullOff)
        {
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)(cullOff ? UnityEngine.Rendering.CullMode.Off : UnityEngine.Rendering.CullMode.Back));
            }

            return material;
        }

        private static bool EnsureMaterialFromPass(ref Material target, MeshPass pass, bool cullOff, bool preferAlphaTestQueue)
        {
            Material source = pass?.mat;
            if (source == null)
            {
                return false;
            }

            if (!CanUsePassMaterial(pass))
            {
                return false;
            }

            if (target != null && target.shader == source.shader)
            {
                return false;
            }

            if (target != null)
            {
                UnityEngine.Object.Destroy(target);
            }

            target = new Material(source)
            {
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = Texture2D.whiteTexture
            };

            if (target.HasProperty("_Color"))
            {
                target.SetColor("_Color", Color.white);
            }

            if (target.HasProperty("_Cull"))
            {
                target.SetInt("_Cull", (int)(cullOff ? UnityEngine.Rendering.CullMode.Off : UnityEngine.Rendering.CullMode.Back));
            }

            if (target.HasProperty("_Cutoff"))
            {
                target.SetFloat("_Cutoff", 0.1f);
            }

            if (preferAlphaTestQueue)
            {
                target.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            return true;
        }

        private static bool CanUsePassMaterial(MeshPass pass)
        {
            return pass != null && !pass.setTile && !pass.setColor && !pass.setMatColor;
        }

        private void LogIncompatiblePassMaterials(BaseTileMap tileMap)
        {
            if (_loggedSkippedPassMaterials || Plugin.Log == null)
            {
                return;
            }

            LogSkippedPassMaterial("passFloor", tileMap.passFloor);
            LogSkippedPassMaterial("passBlock", tileMap.passBlock);
            LogSkippedPassMaterial("passObj", tileMap.passObj);
            LogSkippedPassMaterial("passChara", tileMap.passChara);
            LogPassMaterialDetails("passObj", tileMap.passObj);
            LogPassMaterialDetails("passChara", tileMap.passChara);
            _loggedSkippedPassMaterials = true;
        }

        private static void LogSkippedPassMaterial(string label, MeshPass pass)
        {
            if (pass == null || CanUsePassMaterial(pass))
            {
                return;
            }

            Plugin.Log.LogInfo(
                $"Skipping runtime GPU material clone for {label}: shader={pass.mat?.shader?.name ?? "null"} " +
                $"setTile={pass.setTile} setColor={pass.setColor} setMatColor={pass.setMatColor}");
        }

        private static void LogPassMaterialDetails(string label, MeshPass pass)
        {
            if (Plugin.Log == null || pass?.mat == null)
            {
                return;
            }

            Material mat = pass.mat;
            Shader shader = mat.shader;
            string keywords = (mat.shaderKeywords == null || mat.shaderKeywords.Length == 0)
                ? "none"
                : string.Join(",", mat.shaderKeywords);

            Plugin.Log.LogInfo(
                $"GPU pass material [{label}]: shader={shader?.name ?? "null"} queue={mat.renderQueue} " +
                $"setTile={pass.setTile} setColor={pass.setColor} setMatColor={pass.setMatColor} " +
                $"keywords={keywords} " +
                $"_MainTex={DescribeTexture(mat, "_MainTex")} " +
                $"_MaskTex={DescribeTexture(mat, "_MaskTex")} " +
                $"_Color={DescribeColor(mat, "_Color")} " +
                $"_Mat={DescribeColor(mat, "_Mat")} " +
                $"_Cutoff={DescribeFloat(mat, "_Cutoff")} " +
                $"_MatColorProp={DescribeFloat(mat, "_MatColor")}");
        }

        private static string DescribeTexture(Material mat, string propertyName)
        {
            if (!mat.HasProperty(propertyName))
            {
                return "missing";
            }

            Texture texture = mat.GetTexture(propertyName);
            return texture == null ? "null" : $"{texture.name}({texture.width}x{texture.height})";
        }

        private static string DescribeColor(Material mat, string propertyName)
        {
            if (!mat.HasProperty(propertyName))
            {
                return "missing";
            }

            Color color = mat.GetColor(propertyName);
            return $"({color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3})";
        }

        private static string DescribeFloat(Material mat, string propertyName)
        {
            if (!mat.HasProperty(propertyName))
            {
                return "missing";
            }

            return mat.GetFloat(propertyName).ToString("F3");
        }

        private void UpdateTerrainPreview(GpuViewPose pose)
        {
            int radius = Mathf.Min(Mathf.CeilToInt(Mathf.Max(1f, Plugin.Settings.MaxDistance.Value)), MaxPreviewRadius);
            int minX = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.x) - radius);
            int maxX = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.x) + radius);
            int minZ = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.y) - radius);
            int maxZ = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.y) + radius);

            int activeIndex = 0;
            int activeWallIndex = 0;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Cell cell = EClass._map.cells[x, z];
                    bool hasSurface = _idealizedWorld.TryResolveFloor(cell, cell?.index ?? -1, out FpsResolvedFloorSurface surface);
                    bool isFullBlock = cell != null && cell.HasFullBlock;
                    FpsResolvedWallSurface blockTopSurface = default;
                    bool hasBlockSurface = isFullBlock && _idealizedWorld.TryResolveWall(cell, out blockTopSurface);

                    EnsureTerrainPool(activeIndex + 1);
                    GameObject quad = _terrainQuads[activeIndex];
                    MeshRenderer renderer = _terrainRenderers[activeIndex];
                    quad.SetActive(true);
                    _terrainOverlayQuads[activeIndex].SetActive(false);

                    float surfaceHeight = isFullBlock
                        ? FpsIdealizedWorld.GetCellSurfaceHeight(cell) + 1f + RenderHeightOffset
                        : FpsIdealizedWorld.GetCellSurfaceHeight(cell) + RenderHeightOffset;
                    quad.transform.localPosition = new Vector3(x + 0.5f, surfaceHeight, z + 0.5f);
                    quad.transform.localRotation = Quaternion.identity;
                    quad.transform.localScale = Vector3.one;

                    _propertyBlock.Clear();
                    if (hasBlockSurface)
                    {
                        ApplyBlockTopTexture(_terrainFilters[activeIndex], blockTopSurface);
                        _propertyBlock.SetColor("_Color", ResolveSpriteTint(blockTopSurface.MaterialColor, true, blockTopSurface.Light));
                    }
                    else if (hasSurface)
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

                    if (!IsSolidWall(cell))
                    {
                        activeWallIndex = AddTerrainRisers(activeWallIndex, x, z, cell, EClass._map.Size);
                    }

                    if (IsSolidWall(cell) && _idealizedWorld.TryResolveWall(cell, out FpsResolvedWallSurface wallSurface))
                    {
                        activeWallIndex = AddWallQuads(activeWallIndex, x, z, wallSurface);
                    }
                    activeIndex++;
                }
            }

            for (int i = activeIndex; i < _terrainQuads.Count; i++)
            {
                _terrainQuads[i].SetActive(false);
                _terrainOverlayQuads[i].SetActive(false);
            }

            for (int i = activeWallIndex; i < _wallQuads.Count; i++)
            {
                _wallQuads[i].SetActive(false);
            }

            LogDebugFrame(activeIndex, pose);
        }

        private int AddWallQuads(int activeWallIndex, int cellX, int cellZ, FpsResolvedWallSurface surface)
        {
            if (surface.Cell == null)
            {
                return activeWallIndex;
            }

            float bottom = FpsIdealizedWorld.GetCellSurfaceHeight(surface.Cell);
            float top = bottom + 1f;

            if (surface.Cell.HasWallOrFence && !surface.Cell.HasFullBlock)
            {
                return AddWallFencePanels(activeWallIndex, cellX, cellZ, bottom, top, surface);
            }

            return AddFullBlockCornerQuads(activeWallIndex, cellX, cellZ, bottom, top, surface);
        }

        private int AddFullBlockCornerQuads(int activeWallIndex, int cellX, int cellZ, float bottom, float top, FpsResolvedWallSurface surface)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                if (!ShouldRenderBlockFace(surface.Cell, dir, top))
                {
                    continue;
                }

                activeWallIndex = AddBlockFaceQuad(
                    activeWallIndex,
                    cellX,
                    cellZ,
                    bottom,
                    top,
                    dir,
                    surface);
            }

            return activeWallIndex;
        }

        private int AddWallFencePanels(int activeWallIndex, int cellX, int cellZ, float bottom, float top, FpsResolvedWallSurface surface)
        {
            RenderData renderData = surface.Cell?.sourceBlock?.renderData;
            int[] tiles = surface.Cell?.sourceBlock?._tiles;
            if (renderData == null || tiles == null || tiles.Length == 0)
            {
                return activeWallIndex;
            }

            int baseTile = Mathf.Abs(tiles[0]);
            int wallDir = surface.Cell.blockDir;
            if (wallDir == 0 || wallDir == 2)
            {
                activeWallIndex = AddWallPanelQuad(activeWallIndex, cellX, cellZ, bottom, top, 0, renderData, baseTile, false, surface);
            }

            if (wallDir == 1 || wallDir == 2)
            {
                activeWallIndex = AddWallPanelQuad(activeWallIndex, cellX, cellZ, bottom, top, 1, renderData, baseTile, true, surface);
            }

            return activeWallIndex;
        }

        private int AddBlockFaceQuad(
            int activeWallIndex,
            int cellX,
            int cellZ,
            float bottom,
            float top,
            int dir,
            FpsResolvedWallSurface surface)
        {
            EnsureWallPool(activeWallIndex + 1);

            GameObject quad = _wallQuads[activeWallIndex];
            MeshRenderer renderer = _wallRenderers[activeWallIndex];
            MeshFilter filter = _wallFilters[activeWallIndex];
            quad.SetActive(true);
            renderer.sharedMaterial = ResolveWallRendererMaterial(false);
            FpsGpuFaceQuad face = FpsGpuBlockGeometryBuilder.BuildSideQuad(cellX, cellZ, bottom, top, dir);
            ApplyWallQuadGeometry(quad, filter, face, false);

            bool hitVertical = dir == 1 || dir == 3;
            bool textureResolved = _spriteTextureCache.TryGetBlockFaceTexture(surface, hitVertical, out Texture wallTexture);
            Texture texture = UseSolidFaceDebug()
                ? Texture2D.whiteTexture
                : textureResolved
                ? wallTexture
                : Texture2D.whiteTexture;
            _diagnostics.BlockFaces++;
            if (!textureResolved)
            {
                _diagnostics.BlockFallbacks++;
            }

            RecordWallDiagnostic("block", cellX, cellZ, dir, textureResolved, surface, face, renderer, false);
            _propertyBlock.Clear();
            _propertyBlock.SetTexture("_MainTex", texture);
            _propertyBlock.SetColor("_Color", UseSolidFaceDebug()
                ? ResolveDebugFaceColor(dir)
                : textureResolved
                ? Color.white
                : new Color32(255, 0, 255, 255));
            renderer.SetPropertyBlock(_propertyBlock);
            return activeWallIndex + 1;
        }

        private int AddWallPanelQuad(int activeWallIndex, int cellX, int cellZ, float bottom, float top, int dir, RenderData renderData, int tile, bool flipX, FpsResolvedWallSurface surface)
        {
            EnsureWallPool(activeWallIndex + 1);

            GameObject quad = _wallQuads[activeWallIndex];
            MeshRenderer renderer = _wallRenderers[activeWallIndex];
            MeshFilter filter = _wallFilters[activeWallIndex];
            quad.SetActive(true);
            renderer.sharedMaterial = ResolveWallRendererMaterial(false);
            FpsGpuFaceQuad face = FpsGpuBlockGeometryBuilder.BuildSideQuad(cellX, cellZ, bottom, top, dir);
            ApplyWallQuadGeometry(quad, filter, face, false);

            bool textureResolved = _spriteTextureCache.TryGetTintedRenderTileTexture(
                renderData,
                tile,
                flipX,
                surface.MaterialColor,
                surface.Light,
                out Texture panelTexture);
            Texture texture = UseSolidFaceDebug()
                ? Texture2D.whiteTexture
                : textureResolved
                ? panelTexture
                : Texture2D.whiteTexture;
            _diagnostics.WallPanels++;
            if (!textureResolved)
            {
                _diagnostics.WallPanelFallbacks++;
            }

            RecordWallDiagnostic("panel", cellX, cellZ, dir, textureResolved, surface, face, renderer, false);
            _propertyBlock.Clear();
            _propertyBlock.SetTexture("_MainTex", texture);
            _propertyBlock.SetColor("_Color", UseSolidFaceDebug()
                ? ResolveDebugFaceColor(dir)
                : textureResolved
                ? Color.white
                : new Color32(255, 0, 255, 255));
            renderer.SetPropertyBlock(_propertyBlock);
            return activeWallIndex + 1;
        }

        private void UpdateSpritePreview(GpuViewPose pose, bool includePlayerSelf)
        {
            float maxDistance = Mathf.Max(1f, Plugin.Settings.MaxDistance.Value);
            _idealizedWorld.GatherSprites(pose.PlayerOrigin, maxDistance, includePlayerSelf, _uprightSprites, _groundSprites, _effectSprites);
            _uprightSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            _groundSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            _effectSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            RecordSpriteDiagnostics();

            EnsureGroundPool(_groundSprites.Count);
            EnsureUprightPool(_uprightSprites.Count + _effectSprites.Count);
            UpdateGroundSprites();
            UpdateUprightSprites();
        }

        private void UpdateGroundSprites()
        {
            for (int i = 0; i < _groundSprites.Count; i++)
            {
                FpsResolvedGroundSprite sprite = _groundSprites[i];
                GameObject quad = _groundQuads[i];
                MeshRenderer renderer = _groundRenderers[i];
                quad.SetActive(true);
                quad.transform.localPosition = sprite.CenterWorld;
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = new Vector3(sprite.SizeWorld.x, 1f, sprite.SizeWorld.y);

                _propertyBlock.Clear();
                bool usesBakedTint;
                if (!TryApplySpriteTexture(sprite.Sprite, sprite.RenderData, sprite.Tile, sprite.MaterialColor, sprite.HasMaterialTint, sprite.UseSelectiveMaterialTint, sprite.Light, _propertyBlock, out usesBakedTint))
                {
                    quad.SetActive(false);
                    continue;
                }

                _propertyBlock.SetColor("_Color", usesBakedTint
                    ? Color.white
                    : ResolveSpriteDisplayColor(sprite, sprite.MaterialColor, sprite.HasMaterialTint, sprite.Light));
                renderer.SetPropertyBlock(_propertyBlock);
            }

            for (int i = _groundSprites.Count; i < _groundQuads.Count; i++)
            {
                _groundQuads[i].SetActive(false);
            }
        }

        private void UpdateUprightSprites()
        {
            int index = 0;
            for (int i = 0; i < _uprightSprites.Count; i++, index++)
            {
                ApplyUprightSprite(index, _uprightSprites[i]);
            }

            for (int i = 0; i < _effectSprites.Count; i++, index++)
            {
                FpsResolvedEffectSprite effect = _effectSprites[i];
                ApplyUprightSprite(index, new FpsResolvedUprightSprite
                {
                    AnchorWorld = effect.AnchorWorld,
                    Sprite = null,
                    RenderData = effect.RenderData,
                    Tile = effect.Tile,
                    Distance = effect.Distance,
                    WidthWorld = effect.WidthWorld,
                    HeightWorld = effect.HeightWorld,
                    PivotX = effect.PivotX,
                    PivotY = effect.PivotY,
                    ShadowSizeWorld = 0f,
                    CastsShadow = false,
                    FacingRule = BillboardFacingRule.CameraFacing,
                    MaterialColor = 0,
                    HasMaterialTint = false,
                    UseSelectiveMaterialTint = false,
                    Light = effect.Light
                });
            }

            for (int i = index; i < _uprightQuads.Count; i++)
            {
                _uprightQuads[i].SetActive(false);
            }
        }

        private void ApplyUprightSprite(int index, FpsResolvedUprightSprite sprite)
        {
            GameObject quad = _uprightQuads[index];
            MeshRenderer renderer = _uprightRenderers[index];
            if (!IsVisibleToCamera(sprite.AnchorWorld))
            {
                quad.SetActive(false);
                return;
            }

            quad.SetActive(true);

            Vector3 toCamera = _camera.transform.position - sprite.AnchorWorld;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f)
            {
                toCamera = _camera.transform.forward;
                toCamera.y = 0f;
            }

            Quaternion rotation = toCamera.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : Quaternion.identity;
            float effectivePivotY = ResolveEffectivePivotY(sprite);
            Vector3 center = sprite.AnchorWorld
                + rotation * Vector3.right * ((0.5f - sprite.PivotX) * sprite.WidthWorld)
                + Vector3.up * ((0.5f - effectivePivotY) * sprite.HeightWorld);
            quad.transform.localPosition = center;
            quad.transform.localRotation = rotation;
            quad.transform.localScale = new Vector3(sprite.WidthWorld, sprite.HeightWorld, 1f);

            _propertyBlock.Clear();
            bool usesBakedTint;
            if (!TryApplySpriteTexture(sprite.Sprite, sprite.RenderData, sprite.Tile, sprite.MaterialColor, sprite.HasMaterialTint, sprite.UseSelectiveMaterialTint, sprite.Light, _propertyBlock, out usesBakedTint))
            {
                quad.SetActive(false);
                return;
            }

            _propertyBlock.SetColor("_Color", usesBakedTint
                ? Color.white
                : ResolveSpriteDisplayColor(sprite, sprite.MaterialColor, sprite.HasMaterialTint, sprite.Light));
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private float ResolveEffectivePivotY(FpsResolvedUprightSprite sprite)
        {
            float pivotY = sprite.PivotY;
            FpsAtlasSampler.SpriteMetrics metrics;
            float minV;
            float maxV;
            if (sprite.Sprite != null && _atlasSampler.TryGetSpriteMetrics(sprite.Sprite, out metrics))
            {
                minV = metrics.BottomV;
                maxV = metrics.TopV;
            }
            else if (sprite.Sprite == null && sprite.RenderData != null && _atlasSampler.TryGetRenderTileMetrics(sprite.RenderData, sprite.Tile, out metrics))
            {
                minV = metrics.BottomV;
                maxV = metrics.TopV;
            }
            else
            {
                return pivotY;
            }

            // Keep authored image size, but always ground upright sprites on the visible opaque bottom.
            return minV;
        }

        private bool TryApplySpriteTexture(
            Sprite sprite,
            RenderData renderData,
            int tile,
            int materialColor,
            bool hasMaterialTint,
            bool useSelectiveMaterialTint,
            FpsResolvedLightSample light,
            MaterialPropertyBlock block,
            out bool usesBakedTint)
        {
            usesBakedTint = false;
            Texture texture = null;
            if (sprite != null)
            {
                _spriteTextureCache.TryGetTexture(sprite, out texture);
            }
            else if (renderData != null)
            {
                if (hasMaterialTint)
                {
                    usesBakedTint = useSelectiveMaterialTint
                        ? _spriteTextureCache.TryGetSelectiveTintRenderTileTexture(renderData, tile, false, materialColor, light, out texture)
                        : _spriteTextureCache.TryGetTintedRenderTileTexture(renderData, tile, false, materialColor, light, out texture);
                }

                if (texture == null)
                {
                    _spriteTextureCache.TryGetTexture(renderData, tile, out texture);
                }
            }

            if (texture == null)
            {
                return false;
            }

            block.SetTexture("_MainTex", texture);
            return true;
        }

        private bool IsVisibleToCamera(Vector3 position)
        {
            if (_camera == null)
            {
                return false;
            }

            Vector3 delta = position - _camera.transform.position;
            float distance = delta.magnitude;
            if (distance <= 0.05f)
            {
                return true;
            }

            Vector3 dir = delta / distance;
            float dot = Vector3.Dot(_camera.transform.forward, dir);
            if (dot <= -0.15f)
            {
                return false;
            }

            Vector3 viewport = _camera.WorldToViewportPoint(position);
            return viewport.z > 0f && viewport.x >= -0.2f && viewport.x <= 1.2f && viewport.y >= -0.2f && viewport.y <= 1.2f;
        }

        private static Color ResolveSpriteTint(int materialColor, bool hasMaterialTint, FpsResolvedLightSample light)
        {
            Color32 baseColor = hasMaterialTint
                ? FpsIdealizedWorld.ApplyMatTint(new Color32(255, 255, 255, 255), materialColor)
                : new Color32(255, 255, 255, 255);
            Color32 lit = FpsLightApplicator.ApplySample(baseColor, light);
            return new Color32(lit.r, lit.g, lit.b, 255);
        }

        private static Color ResolveSpriteDisplayColor(FpsResolvedUprightSprite sprite, int materialColor, bool hasMaterialTint, FpsResolvedLightSample light)
        {
            if (sprite.Sprite == null && sprite.RenderData != null)
            {
                Color32 baseColor = hasMaterialTint
                    ? FpsIdealizedWorld.ApplyMatTint(new Color32(255, 255, 255, 255), materialColor)
                    : new Color32(255, 255, 255, 255);
                return new Color32(baseColor.r, baseColor.g, baseColor.b, 255);
            }

            return ResolveSpriteTint(materialColor, hasMaterialTint, light);
        }

        private static Color ResolveSpriteDisplayColor(FpsResolvedGroundSprite sprite, int materialColor, bool hasMaterialTint, FpsResolvedLightSample light)
        {
            if (sprite.Sprite == null && sprite.RenderData != null)
            {
                Color32 baseColor = hasMaterialTint
                    ? FpsIdealizedWorld.ApplyMatTint(new Color32(255, 255, 255, 255), materialColor)
                    : new Color32(255, 255, 255, 255);
                return new Color32(baseColor.r, baseColor.g, baseColor.b, 255);
            }

            return ResolveSpriteTint(materialColor, hasMaterialTint, light);
        }

        private static Color ResolveDebugFaceColor(int dir)
        {
            switch (NormalizeDir(dir))
            {
                case 0:
                    return new Color32(255, 80, 80, 255);
                case 1:
                    return new Color32(80, 255, 80, 255);
                case 2:
                    return new Color32(80, 160, 255, 255);
                default:
                    return new Color32(255, 220, 80, 255);
            }
        }

        private static Color ResolveDebugEdgeColor(FpsGpuTerrainEdge edge)
        {
            switch (edge)
            {
                case FpsGpuTerrainEdge.East:
                    return new Color32(80, 255, 80, 255);
                case FpsGpuTerrainEdge.South:
                    return new Color32(80, 160, 255, 255);
                case FpsGpuTerrainEdge.West:
                    return new Color32(255, 220, 80, 255);
                default:
                    return new Color32(255, 80, 80, 255);
            }
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

        private void BeginDiagnosticsFrame()
        {
            if (!IsGpuDiagnosticsEnabled())
            {
                return;
            }

            _diagnostics = default;
            _diagnosticSamples.Clear();
            _spriteDiagnosticSamples.Clear();
        }

        private void RecordWallDiagnostic(string kind, int cellX, int cellZ, int dir, bool textureResolved, FpsResolvedWallSurface surface, FpsGpuFaceQuad face, MeshRenderer renderer, bool doubleSided)
        {
            if (!IsGpuDiagnosticsEnabled())
            {
                return;
            }

            if (_diagnosticSamples.Count >= 16)
            {
                return;
            }

            Bounds bounds = renderer != null ? renderer.bounds : default;
            bool frustumVisible = renderer != null && GeometryUtility.TestPlanesAABB(_cameraFrustumPlanes, bounds);
            Vector3 normal = Vector3.Cross(face.TopLeft - face.BottomLeft, face.BottomRight - face.BottomLeft).normalized;
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.forward;
            }

            Vector3 toCamera = _camera != null ? (_camera.transform.position - face.Center) : Vector3.zero;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                toCamera.Normalize();
            }

            float facingDot = Vector3.Dot(normal, toCamera);
            bool frontFacing = facingDot > 0f;
            int cullMode = -1;
            string materialShader = "null";
            if (renderer?.sharedMaterial != null)
            {
                materialShader = renderer.sharedMaterial.shader != null ? renderer.sharedMaterial.shader.name : "null";
                if (renderer.sharedMaterial.HasProperty("_Cull"))
                {
                    cullMode = renderer.sharedMaterial.GetInt("_Cull");
                }
            }

            AccumulateFacingDiagnostic(kind, frontFacing);
            _diagnosticSamples.Add(
                $"{kind} cell=({cellX},{cellZ}) dir={dir} blockDir={surface.Cell?.blockDir ?? -1} tile={surface.Tile} " +
                $"renderData={(surface.RenderData != null ? surface.RenderData.GetType().Name : "null")} textureResolved={textureResolved} " +
                $"frustumVisible={frustumVisible} frontFacing={frontFacing} facingDot={facingDot:F3} doubleSided={doubleSided} cull={cullMode} shader={materialShader} " +
                $"boundsMin={bounds.min:F3} boundsMax={bounds.max:F3} normal={normal:F3} " +
                $"bl={face.BottomLeft:F3} br={face.BottomRight:F3} tl={face.TopLeft:F3} tr={face.TopRight:F3}");
        }

        private void AccumulateFacingDiagnostic(string kind, bool frontFacing)
        {
            switch (kind)
            {
                case "block":
                    if (frontFacing)
                    {
                        _diagnostics.BlockFrontFacing++;
                    }
                    else
                    {
                        _diagnostics.BlockBackFacing++;
                    }
                    break;
                case "riser":
                    if (frontFacing)
                    {
                        _diagnostics.RiserFrontFacing++;
                    }
                    else
                    {
                        _diagnostics.RiserBackFacing++;
                    }
                    break;
                case "panel":
                    if (frontFacing)
                    {
                        _diagnostics.PanelFrontFacing++;
                    }
                    else
                    {
                        _diagnostics.PanelBackFacing++;
                    }
                    break;
            }
        }

        private void LogDiagnosticsFrame(GpuViewPose pose)
        {
            if (!IsGpuDiagnosticsEnabled() || _diagnosticFramesRemaining <= 0 || Plugin.Log == null)
            {
                return;
            }

            _diagnosticFramesRemaining--;
            string shaderName = _wallMaterial?.shader != null ? _wallMaterial.shader.name : "null";
            string doubleSidedWallShaderName = _wallDoubleSidedMaterial?.shader != null ? _wallDoubleSidedMaterial.shader.name : "null";
            string terrainShaderName = _terrainMaterial?.shader != null ? _terrainMaterial.shader.name : "null";
            Material activeWallMaterial = ResolveWallRendererMaterial(false);
            string activeWallShaderName = activeWallMaterial?.shader != null ? activeWallMaterial.shader.name : "null";
            Plugin.Log.LogInfo(
                $"GPU wall diagnostics: cam={_camera.transform.position:F3} player={pose.PlayerOrigin:F3} wallShader={shaderName} wallDoubleSidedShader={doubleSidedWallShaderName} activeWallShader={activeWallShaderName} terrainShader={terrainShaderName} " +
                $"queue={_wallMaterial?.renderQueue ?? -1} solidDebug={UseSolidFaceDebug()} " +
                $"blockFaces={_diagnostics.BlockFaces} blockFallbacks={_diagnostics.BlockFallbacks} blockFront={_diagnostics.BlockFrontFacing} blockBack={_diagnostics.BlockBackFacing} " +
                $"riserFaces={_diagnostics.RiserFaces} riserFallbacks={_diagnostics.RiserFallbacks} riserFront={_diagnostics.RiserFrontFacing} riserBack={_diagnostics.RiserBackFacing} " +
                $"wallPanels={_diagnostics.WallPanels} wallPanelFallbacks={_diagnostics.WallPanelFallbacks} panelFront={_diagnostics.PanelFrontFacing} panelBack={_diagnostics.PanelBackFacing}");

            for (int i = 0; i < _diagnosticSamples.Count; i++)
            {
                Plugin.Log.LogInfo($"GPU wall sample[{i}]: {_diagnosticSamples[i]}");
            }

            for (int i = 0; i < _spriteDiagnosticSamples.Count; i++)
            {
                Plugin.Log.LogInfo($"GPU sprite sample[{i}]: {_spriteDiagnosticSamples[i]}");
            }
        }

        private void RecordSpriteDiagnostics()
        {
            if (!IsGpuDiagnosticsEnabled())
            {
                return;
            }

            HashSet<string> seenCategories = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _uprightSprites.Count; i++)
            {
                FpsResolvedUprightSprite sprite = _uprightSprites[i];
                if (TryRecordSpriteDiagnostic(
                    sprite.DiagnosticCategory,
                    sprite.DiagnosticLabel,
                    sprite.SourcePixelSize,
                    sprite.RenderDataSize,
                    sprite.ImageScale,
                    new Vector2(sprite.WidthWorld, sprite.HeightWorld),
                    sprite.AnchorWorld.y,
                    seenCategories) && seenCategories.Count >= 4)
                {
                    return;
                }
            }

            for (int i = 0; i < _groundSprites.Count; i++)
            {
                FpsResolvedGroundSprite sprite = _groundSprites[i];
                if (TryRecordSpriteDiagnostic(
                    sprite.DiagnosticCategory,
                    sprite.DiagnosticLabel,
                    sprite.SourcePixelSize,
                    sprite.RenderDataSize,
                    sprite.ImageScale,
                    sprite.SizeWorld,
                    sprite.CenterWorld.y,
                    seenCategories) && seenCategories.Count >= 4)
                {
                    return;
                }
            }
        }

        private bool TryRecordSpriteDiagnostic(
            string category,
            string label,
            Vector2 sourcePixelSize,
            Vector2 renderDataSize,
            Vector2 imageScale,
            Vector2 worldSize,
            float anchorY,
            HashSet<string> seenCategories)
        {
            if (string.IsNullOrEmpty(category) || seenCategories.Contains(category))
            {
                return false;
            }

            seenCategories.Add(category);
            _spriteDiagnosticSamples.Add(
                $"category={category} label={label} sourcePx={sourcePixelSize:F1} renderSize={renderDataSize:F3} imageScale={imageScale:F3} worldSize={worldSize:F3} anchorY={anchorY:F3}");
            return true;
        }

        private static bool IsGpuDiagnosticsEnabled()
        {
            return Plugin.Settings?.EnableGpuDiagnostics?.Value == true;
        }

        private static bool UseSolidFaceDebug()
        {
            return Plugin.Settings?.GpuDebugSolidFaces?.Value == true;
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

        private void EnsureWallPool(int count)
        {
            while (_wallQuads.Count < count)
            {
                GameObject quad = new GameObject($"GpuWallQuad_{_wallQuads.Count}");
                quad.hideFlags = HideFlags.HideAndDontSave;
                quad.transform.SetParent(_wallRoot.transform, false);
                SetLayerRecursively(quad, RenderLayer);

                MeshFilter filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = CreateWallQuadMesh($"FpsGpuWallQuad_{_wallQuads.Count}");

                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _wallMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                _wallQuads.Add(quad);
                _wallRenderers.Add(renderer);
                _wallFilters.Add(filter);
                quad.SetActive(false);
            }
        }

        private void EnsureUprightPool(int count)
        {
            while (_uprightQuads.Count < count)
            {
                GameObject quad = new GameObject($"GpuUprightQuad_{_uprightQuads.Count}");
                quad.hideFlags = HideFlags.HideAndDontSave;
                quad.transform.SetParent(_uprightRoot.transform, false);
                SetLayerRecursively(quad, RenderLayer);

                MeshFilter filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = GetUprightPlaneMesh();

                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _spriteMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                _uprightQuads.Add(quad);
                _uprightRenderers.Add(renderer);
                quad.SetActive(false);
            }
        }

        private void EnsureGroundPool(int count)
        {
            while (_groundQuads.Count < count)
            {
                GameObject quad = new GameObject($"GpuGroundQuad_{_groundQuads.Count}");
                quad.hideFlags = HideFlags.HideAndDontSave;
                quad.transform.SetParent(_groundRoot.transform, false);
                SetLayerRecursively(quad, RenderLayer);

                MeshFilter filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = GetTerrainPlaneMesh();

                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _spriteMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                _groundQuads.Add(quad);
                _groundRenderers.Add(renderer);
                quad.SetActive(false);
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

        private static Mesh GetUprightPlaneMesh()
        {
            if (_uprightPlaneMesh != null)
            {
                return _uprightPlaneMesh;
            }

            Mesh mesh = new Mesh
            {
                name = "FpsGpuUprightPlane"
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.normals = new[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
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
            _uprightPlaneMesh = mesh;
            return _uprightPlaneMesh;
        }

        private static Mesh CreateWallQuadMesh(string name)
        {
            Mesh mesh = new Mesh
            {
                name = name
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.normals = new[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
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
            return mesh;
        }

        private static void ApplyWallQuadGeometry(GameObject quad, MeshFilter filter, FpsGpuFaceQuad face, bool doubleSided)
        {
            Vector3 center = face.Center;
            quad.transform.localPosition = center;
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = Vector3.one;

            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = new[]
            {
                face.BottomLeft - center,
                face.BottomRight - center,
                face.TopLeft - center,
                face.TopRight - center
            };
            Vector3 normal = Vector3.Cross(vertices[2] - vertices[0], vertices[1] - vertices[0]).normalized;
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.forward;
            }

            mesh.vertices = vertices;
            mesh.normals = new[]
            {
                normal,
                normal,
                normal,
                normal
            };
            mesh.triangles = doubleSided
                ? new[]
                {
                    0, 2, 1,
                    2, 3, 1,
                    0, 1, 2,
                    2, 1, 3
                }
                : new[]
                {
                    0, 2, 1,
                    2, 3, 1
                };
            mesh.RecalculateBounds();
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

        private static bool IsSolidWall(Cell cell)
        {
            return cell != null && (cell.HasFullBlock || cell.HasWallOrFence);
        }

        private int AddTerrainRisers(int activeWallIndex, int cellX, int cellZ, Cell cell, int mapSize)
        {
            float cellHeight = FpsIdealizedWorld.GetCellSurfaceHeight(cell);
            if (cellX + 1 < mapSize)
            {
                Cell neighbor = EClass._map.cells[cellX + 1, cellZ];
                if (neighbor != null && !IsSolidWall(neighbor))
                {
                    float neighborHeight = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor);
                    if (cellHeight > neighborHeight + 0.02f
                        && _idealizedWorld.TryResolveTerrainRiser(cell, out FpsResolvedWallSurface riserSurface))
                    {
                        activeWallIndex = AddTerrainRiserQuad(activeWallIndex, cellX, cellZ, FpsGpuTerrainEdge.East, cellHeight, neighborHeight, riserSurface);
                    }
                    else if (neighborHeight > cellHeight + 0.02f
                        && _idealizedWorld.TryResolveTerrainRiser(neighbor, out FpsResolvedWallSurface neighborRiserSurface))
                    {
                        activeWallIndex = AddTerrainRiserQuad(activeWallIndex, cellX + 1, cellZ, FpsGpuTerrainEdge.West, neighborHeight, cellHeight, neighborRiserSurface);
                    }
                }
            }

            if (cellZ + 1 < mapSize)
            {
                Cell neighbor = EClass._map.cells[cellX, cellZ + 1];
                if (neighbor != null && !IsSolidWall(neighbor))
                {
                    float neighborHeight = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor);
                    if (cellHeight > neighborHeight + 0.02f
                        && _idealizedWorld.TryResolveTerrainRiser(cell, out FpsResolvedWallSurface riserSurface))
                    {
                        activeWallIndex = AddTerrainRiserQuad(activeWallIndex, cellX, cellZ, FpsGpuTerrainEdge.South, cellHeight, neighborHeight, riserSurface);
                    }
                    else if (neighborHeight > cellHeight + 0.02f
                        && _idealizedWorld.TryResolveTerrainRiser(neighbor, out FpsResolvedWallSurface neighborRiserSurface))
                    {
                        activeWallIndex = AddTerrainRiserQuad(activeWallIndex, cellX, cellZ + 1, FpsGpuTerrainEdge.North, neighborHeight, cellHeight, neighborRiserSurface);
                    }
                }
            }

            return activeWallIndex;
        }

        private int AddTerrainRiserQuad(int activeWallIndex, int cellX, int cellZ, FpsGpuTerrainEdge edge, float topHeight, float bottomHeight, FpsResolvedWallSurface surface)
        {
            if (topHeight - bottomHeight <= 0.01f)
            {
                return activeWallIndex;
            }

            EnsureWallPool(activeWallIndex + 1);

            GameObject quad = _wallQuads[activeWallIndex];
            MeshRenderer renderer = _wallRenderers[activeWallIndex];
            MeshFilter filter = _wallFilters[activeWallIndex];
            quad.SetActive(true);
            renderer.sharedMaterial = ResolveWallRendererMaterial(false);
            FpsGpuFaceQuad face = FpsGpuRiserGeometryBuilder.BuildEdgeQuad(cellX, cellZ, topHeight, bottomHeight, edge);
            ApplyWallQuadGeometry(quad, filter, face, false);

            bool hitVertical = edge == FpsGpuTerrainEdge.East || edge == FpsGpuTerrainEdge.West;
            bool textureResolved = _spriteTextureCache.TryGetBlockFaceTexture(
                    surface,
                    hitVertical,
                    out Texture wallTexture);
            Texture texture = UseSolidFaceDebug()
                ? Texture2D.whiteTexture
                : textureResolved
                ? wallTexture
                : Texture2D.whiteTexture;
            _diagnostics.RiserFaces++;
            if (!textureResolved)
            {
                _diagnostics.RiserFallbacks++;
            }

            RecordWallDiagnostic("riser", cellX, cellZ, EdgeToDir(edge), textureResolved, surface, face, renderer, false);
            _propertyBlock.Clear();
            _propertyBlock.SetTexture("_MainTex", texture);
            _propertyBlock.SetColor("_Color", UseSolidFaceDebug()
                ? ResolveDebugEdgeColor(edge)
                : textureResolved
                ? Color.white
                : new Color32(255, 0, 255, 255));
            renderer.SetPropertyBlock(_propertyBlock);
            return activeWallIndex + 1;
        }

        private void ApplyBlockTopTexture(MeshFilter filter, FpsResolvedWallSurface surface)
        {
            if (filter == null)
            {
                return;
            }

            filter.sharedMesh = GetTerrainPlaneMesh();
            Texture texture = _spriteTextureCache.TryGetBlockFaceTexture(surface, FpsAtlasSampler.BlockFaceKind.Top, false, out Texture topTexture)
                ? topTexture
                : Texture2D.whiteTexture;
            _propertyBlock.SetTexture("_MainTex", texture);
        }

        private static bool ShouldRenderBlockFace(Cell cell, int dir, float top)
        {
            Cell neighbor = GetNeighbor(cell, dir);
            if (neighbor == null)
            {
                return true;
            }

            if (!(neighbor.HasFullBlock || neighbor.HasWallOrFence))
            {
                return true;
            }

            float neighborTop = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor) + (neighbor.HasFullBlock ? 1f : 0f);
            return neighborTop + 0.01f < top;
        }

        private static Cell GetNeighbor(Cell cell, int dir)
        {
            switch (NormalizeDir(dir))
            {
                case 0:
                    return cell.Front;
                case 1:
                    return cell.Right;
                case 2:
                    return cell.Back;
                default:
                    return cell.Left;
            }
        }

        private static int NormalizeDir(int dir)
        {
            int normalized = dir % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }

        private static int EdgeToDir(FpsGpuTerrainEdge edge)
        {
            switch (edge)
            {
                case FpsGpuTerrainEdge.East:
                    return 1;
                case FpsGpuTerrainEdge.South:
                    return 2;
                case FpsGpuTerrainEdge.West:
                    return 3;
                default:
                    return 0;
            }
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

            Vector2 playerOrigin = TryGetSmoothedOrigin();
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

        private static Vector2 TryGetSmoothedOrigin()
        {
            if (EClass.pc?.renderer == null || EClass.screen == null || EClass.screen.tileMap == null || EClass._zone.IsRegion)
            {
                return new Vector2(EClass.pc.pos.x + 0.5f, EClass.pc.pos.z + 0.5f);
            }

            float alignX = EClass.screen.tileAlign.x;
            float alignY = EClass.screen.tileAlign.y;
            if (Mathf.Abs(alignX) < 0.0001f || Mathf.Abs(alignY) < 0.0001f)
            {
                return new Vector2(EClass.pc.pos.x + 0.5f, EClass.pc.pos.z + 0.5f);
            }

            byte height = EClass.pc.pos.cell.bridgeHeight == 0 ? EClass.pc.pos.cell.height : EClass.pc.pos.cell.bridgeHeight;
            float renderX = EClass.pc.renderer.position.x;
            float renderY = EClass.pc.renderer.position.y - height * EClass.screen.tileMap._heightMod.y;
            float sum = renderX / alignX;
            float diff = renderY / alignY;
            float x = (sum - diff) * 0.5f;
            float z = (sum + diff) * 0.5f;
            if (float.IsNaN(x) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(z))
            {
                return new Vector2(EClass.pc.pos.x + 0.5f, EClass.pc.pos.z + 0.5f);
            }

            return new Vector2(x + 0.5f, z + 0.5f);
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

        private struct GpuDiagnosticCounters
        {
            public int BlockFaces;
            public int BlockFallbacks;
            public int BlockFrontFacing;
            public int BlockBackFacing;
            public int RiserFaces;
            public int RiserFallbacks;
            public int RiserFrontFacing;
            public int RiserBackFacing;
            public int WallPanels;
            public int WallPanelFallbacks;
            public int PanelFrontFacing;
            public int PanelBackFacing;
        }
    }
}
