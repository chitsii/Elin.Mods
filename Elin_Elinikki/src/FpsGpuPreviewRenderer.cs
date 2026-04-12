using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BeautifyEffect;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace Elin_Elinikki
{
    internal sealed partial class FpsGpuPreviewRenderer : IDisposable
    {
        internal const int RenderLayer = 29;
        private const float RenderHeightOffset = 0.01f;
        private const int MaxPreviewRadius = 32;
        private const int TerrainChunkSize = 8;
        private const int TerrainTileTextureSize = 16;
        private const int TerrainTextureSlotCount = 2;
        private const float MaxProjectedOriginDeviation = 0.35f;
        private const float MaxDungeonCrawlerAxisDeviation = 1.5f;
        private const float GpuEyeHeightScale = 1.0f;
        private static readonly Color DefaultClearColor = new Color32(34, 40, 52, 255);
        private static Mesh _terrainPlaneMesh;
        private static Mesh _uprightPlaneMesh;

        private readonly FpsIdealizedWorld _idealizedWorld = new FpsIdealizedWorld();
        private readonly FpsGpuFloorAtlasBaker _floorAtlasBaker = new FpsGpuFloorAtlasBaker();
        private readonly FpsGpuSpriteTextureCache _spriteTextureCache = new FpsGpuSpriteTextureCache();
        private readonly FpsAtlasSampler _atlasSampler = new FpsAtlasSampler();
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private readonly List<FpsResolvedWallMountedSprite> _wallMountedSprites = new List<FpsResolvedWallMountedSprite>(128);
        private readonly List<FpsResolvedUprightSprite> _uprightSprites = new List<FpsResolvedUprightSprite>(256);
        private readonly List<FpsResolvedGroundSprite> _groundSprites = new List<FpsResolvedGroundSprite>(256);
        private readonly List<FpsResolvedEffectSprite> _effectSprites = new List<FpsResolvedEffectSprite>(64);
        private readonly List<FpsWallRunSeed> _fullBlockWallSeeds = new List<FpsWallRunSeed>(256);
        private readonly List<FpsWallRun> _fullBlockWallRuns = new List<FpsWallRun>(128);
        private readonly Dictionary<int, FpsResolvedWallSurface> _fullBlockWallSurfaceLookup = new Dictionary<int, FpsResolvedWallSurface>();
        private readonly List<FpsWallRunSeed> _panelWallSeeds = new List<FpsWallRunSeed>(256);
        private readonly List<FpsWallRun> _panelWallRuns = new List<FpsWallRun>(128);
        private readonly List<FpsWallRun> _panelDoorFrameRuns = new List<FpsWallRun>(64);
        private readonly Dictionary<int, HybridPanelWallSurface> _panelWallSurfaceLookup = new Dictionary<int, HybridPanelWallSurface>();
        private readonly Dictionary<int, TerrainChunkBuildContext> _terrainChunkContexts = new Dictionary<int, TerrainChunkBuildContext>(64);
        private readonly List<TerrainChunkBuildContext> _orderedTerrainChunks = new List<TerrainChunkBuildContext>(64);
        private readonly Plane[] _cameraFrustumPlanes = new Plane[6];
        private readonly List<GameObject> _terrainQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _terrainRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _terrainFilters = new List<MeshFilter>(256);
        private readonly List<GameObject> _terrainOverlayQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _terrainOverlayRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _terrainOverlayFilters = new List<MeshFilter>(256);
        private readonly List<TerrainChunkVisualState> _terrainChunkStates = new List<TerrainChunkVisualState>(64);
        private readonly List<GameObject> _wallQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _wallRenderers = new List<MeshRenderer>(256);
        private readonly List<MeshFilter> _wallFilters = new List<MeshFilter>(256);
        private readonly List<GameObject> _roofQuads = new List<GameObject>(128);
        private readonly List<MeshRenderer> _roofRenderers = new List<MeshRenderer>(128);
        private readonly List<MeshFilter> _roofFilters = new List<MeshFilter>(128);
        private readonly List<GameObject> _wallMountedQuads = new List<GameObject>(128);
        private readonly List<MeshRenderer> _wallMountedRenderers = new List<MeshRenderer>(128);
        private readonly List<MeshFilter> _wallMountedFilters = new List<MeshFilter>(128);
        private readonly List<GameObject> _uprightQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _uprightRenderers = new List<MeshRenderer>(256);
        private readonly List<GameObject> _groundQuads = new List<GameObject>(256);
        private readonly List<MeshRenderer> _groundRenderers = new List<MeshRenderer>(256);
        private GameObject _root;
        private GameObject _terrainRoot;
        private GameObject _terrainOverlayRoot;
        private GameObject _wallRoot;
        private GameObject _roofRoot;
        private GameObject _wallMountedRoot;
        private GameObject _uprightRoot;
        private GameObject _groundRoot;
        private GameObject _sharedWorldRoot;
        private Camera _camera;
        private RenderTexture _renderTexture;
        private Material _terrainMaterial;
        private Material _terrainOverlayMaterial;
        private Material _wallMaterial;
        private Material _wallDoubleSidedMaterial;
        private Material _wallDebugMaterial;
        private Material _spriteMaterial;
        private Material _groundMaterial;
        private BloomOptimized _previewBloom;
        private Beautify _previewBeautify;
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
        private readonly List<string> _fogDiagnosticSamples = new List<string>(12);

        public Texture OutputTexture => _renderTexture;

        public Transform SharedWorldRoot => _sharedWorldRoot != null ? _sharedWorldRoot.transform : null;

        internal Camera PreviewCameraForTests => _camera;

        public void ResetDiagnostics()
        {
            _diagnosticFramesRemaining = 3;
            _diagnostics = default;
            _spriteDiagnosticSamples.Clear();
            _fogDiagnosticSamples.Clear();
        }

        public void Initialize(int width, int height)
        {
            _width = Mathf.Max(1, width);
            _height = Mathf.Max(1, height);

            CreateRenderTexture();
            CreateCamera();
            CreateRoot();
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
            ConfigurePreviewPostEffects();
            UpdateCamera(pose);
            GeometryUtility.CalculateFrustumPlanes(_camera, _cameraFrustumPlanes);
            UpdateTerrainPreview(pose);
            UpdateRoofPreview(pose);
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
                _roofRoot = null;
                _wallMountedRoot = null;
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

            if (_groundMaterial != null)
            {
                UnityEngine.Object.Destroy(_groundMaterial);
                _groundMaterial = null;
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

            for (int i = 0; i < _terrainChunkStates.Count; i++)
            {
                if (_terrainChunkStates[i].Texture != null)
                {
                    UnityEngine.Object.Destroy(_terrainChunkStates[i].Texture);
                }
            }

            _spriteTextureCache.Dispose();
            _terrainQuads.Clear();
            _terrainRenderers.Clear();
            _terrainOverlayQuads.Clear();
            _terrainOverlayRenderers.Clear();
            _wallQuads.Clear();
            _wallRenderers.Clear();
            _wallFilters.Clear();
            _roofQuads.Clear();
            _roofRenderers.Clear();
            _roofFilters.Clear();
            _wallMountedQuads.Clear();
            _wallMountedRenderers.Clear();
            _wallMountedFilters.Clear();
            _uprightQuads.Clear();
            _uprightRenderers.Clear();
            _groundQuads.Clear();
            _groundRenderers.Clear();
        }

        private void ConfigurePreviewPostEffects()
        {
            bool enabled = false;

            if (_previewBloom != null)
            {
                _previewBloom.enabled = enabled;
            }

            if (_previewBeautify != null)
            {
                _previewBeautify.enabled = enabled;
            }
        }

        private static void SetComponentMember(Component component, string memberName, object value)
        {
            if (component == null)
            {
                return;
            }

            Type type = component.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value, null);
                return;
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(component, value);
            }
        }

        private static void SetEnumMemberByName(Component component, string memberName, string enumName)
        {
            if (component == null)
            {
                return;
            }

            Type type = component.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Type enumType = property?.PropertyType;
            FieldInfo field = null;
            if (enumType == null)
            {
                field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                enumType = field?.FieldType;
            }

            if (enumType == null || !enumType.IsEnum)
            {
                return;
            }

            object parsed = Enum.Parse(enumType, enumName, true);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, parsed, null);
                return;
            }

            field?.SetValue(component, parsed);
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
                name = "ElinikkiGpuPreview"
            };
            _renderTexture.Create();
        }

        private void CreateCamera()
        {
            GameObject cameraGo = new GameObject("ElinikkiGpuCamera");
            cameraGo.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(cameraGo);

            _camera = cameraGo.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = DefaultClearColor;
            _camera.nearClipPlane = 0.03f;
            _camera.farClipPlane = Mathf.Max(32f, Plugin.Settings.MaxDistance.Value + 12f);
            _camera.cullingMask = 1 << RenderLayer;
            _camera.targetTexture = _renderTexture;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.orthographic = false;
            _previewBloom = cameraGo.AddComponent<BloomOptimized>();
            _previewBeautify = cameraGo.AddComponent<Beautify>();
            ConfigurePreviewPostEffects();
        }

        private void CreateRoot()
        {
            _root = new GameObject("ElinikkiGpuRoot");
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

            _roofRoot = new GameObject("Roofs");
            _roofRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_roofRoot, RenderLayer);

            _wallMountedRoot = new GameObject("WallMountedSprites");
            _wallMountedRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_wallMountedRoot, RenderLayer);

            _groundRoot = new GameObject("GroundSprites");
            _groundRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_groundRoot, RenderLayer);

            _sharedWorldRoot = new GameObject("SharedWorld");
            _sharedWorldRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_sharedWorldRoot, RenderLayer);

            _uprightRoot = new GameObject("UprightSprites");
            _uprightRoot.transform.SetParent(_root.transform, false);
            SetLayerRecursively(_uprightRoot, RenderLayer);

            Shader terrainShader = FindPreferredShader(
                "Unlit/Texture",
                "Unlit/Transparent Cutout",
                "Legacy Shaders/Transparent/Cutout/Diffuse",
                "Legacy Shaders/Transparent/Cutout/VertexLit",
                "Sprites/Default",
                "Unlit/Transparent");
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
                "Unlit/Texture",
                "Unlit/Transparent Cutout",
                "Legacy Shaders/Transparent/Cutout/Diffuse",
                "Legacy Shaders/Transparent/Cutout/VertexLit",
                "Sprites/Default",
                "Unlit/Transparent");
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

            bool cullWorldBackfaces = Plugin.Settings?.EnableWorldMeshBackfaceCulling?.Value != false;
            _terrainMaterial = CreateTexturedMaterial(terrainShader, cullOff: true, preferAlphaTestQueue: false);
            _terrainOverlayMaterial = CreateTexturedMaterial(terrainShader, cullOff: true, preferAlphaTestQueue: false);
            _spriteMaterial = CreateTexturedMaterial(spriteShader, cullOff: true, preferAlphaTestQueue: true);
            _groundMaterial = CreateTexturedMaterial(spriteShader, cullOff: false, preferAlphaTestQueue: true);
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
            _camera.backgroundColor = ResolveClearColor();
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
            bool cullWorldBackfaces = Plugin.Settings?.EnableWorldMeshBackfaceCulling?.Value != false;
            updated |= EnsureMaterialFromPass(ref _wallMaterial, tileMap.passBlock, cullOff: false, preferAlphaTestQueue: true);
            updated |= EnsureMaterialFromPass(ref _wallDoubleSidedMaterial, tileMap.passBlock, cullOff: true, preferAlphaTestQueue: true);
            updated |= EnsureMaterialFromPass(ref _spriteMaterial, tileMap.passObj ?? tileMap.passChara, cullOff: true, preferAlphaTestQueue: true);

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

            for (int i = 0; i < _wallMountedRenderers.Count; i++)
            {
                _wallMountedRenderers[i].sharedMaterial = ResolveWallRendererMaterial(false);
            }

            for (int i = 0; i < _uprightRenderers.Count; i++)
            {
                _uprightRenderers[i].sharedMaterial = _spriteMaterial;
            }

            for (int i = 0; i < _groundRenderers.Count; i++)
            {
                _groundRenderers[i].sharedMaterial = _groundMaterial ?? _spriteMaterial;
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

        private void UpdateRoofPreview(GpuViewPose pose)
        {
            if (Plugin.Settings?.EnableIndoorCeiling?.Value != true)
            {
                DisableRoofQuads();
                return;
            }

            Room currentRoom = EClass.pc?.pos?.cell?.room;
            Lot currentLot = currentRoom?.lot;
            if (currentRoom == null || currentLot == null || !currentRoom.HasRoof || (currentRoom.data?.atrium ?? false))
            {
                DisableRoofQuads();
                return;
            }

            float ceilingY = ResolveIndoorCeilingTop(currentRoom, currentLot);
            FpsGpuFaceQuad face = new FpsGpuFaceQuad(
                new Vector3(currentLot.x, ceilingY, currentLot.z),
                new Vector3(currentLot.mx + 1f, ceilingY, currentLot.z),
                new Vector3(currentLot.x, ceilingY, currentLot.mz + 1f),
                new Vector3(currentLot.mx + 1f, ceilingY, currentLot.mz + 1f));

            EnsureRoofPool(1);
            GameObject quad = _roofQuads[0];
            MeshRenderer renderer = _roofRenderers[0];
            MeshFilter filter = _roofFilters[0];
            quad.SetActive(true);
            Material rendererMaterial = ResolveWallRendererMaterial(true);
            if (renderer.sharedMaterial != rendererMaterial)
            {
                renderer.sharedMaterial = rendererMaterial;
            }

            ApplyWallQuadGeometry(quad, filter, face, true);
            SetQuadUv(filter.sharedMesh, new Rect(0f, 0f, 1f, 1f));

            Texture ceilingTexture = Texture2D.whiteTexture;
            Color baseColor = Color.white;
            if (_idealizedWorld.TryResolveFloor(EClass.pc?.pos?.cell, EClass.pc?.pos?.cell?.index ?? -1, out FpsResolvedFloorSurface surface)
                && _floorAtlasBaker.TryGetCompositeTexture(surface, out Texture floorTexture))
            {
                ceilingTexture = floorTexture;
            }

            _propertyBlock.Clear();
            _propertyBlock.SetTexture("_MainTex", ceilingTexture);
            Color styledCeiling = ApplySurfaceStyle(baseColor, FpsVisualSurfaceKind.Ceiling, 0, true);
            _propertyBlock.SetColor("_Color", ApplyAtmosphericFog(styledCeiling, face.Center, Mathf.Max(1f, Plugin.Settings.MaxDistance.Value), 0.08f, "roof-interior"));
            renderer.SetPropertyBlock(_propertyBlock);

            for (int i = 1; i < _roofQuads.Count; i++)
            {
                _roofQuads[i].SetActive(false);
            }
        }

        private void DisableRoofQuads()
        {
            for (int i = 0; i < _roofQuads.Count; i++)
            {
                _roofQuads[i].SetActive(false);
            }
        }

        private static float ResolveIndoorCeilingTop(Room room, Lot lot)
        {
            float maxSurface = 0f;
            bool hasSurface = false;
            float maxWallTop = 0f;
            bool hasWallTop = false;
            int minX = Mathf.Max(0, lot.x);
            int maxX = Mathf.Min(EClass._map.Size - 1, lot.mx);
            int minZ = Mathf.Max(0, lot.z);
            int maxZ = Mathf.Min(EClass._map.Size - 1, lot.mz);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Cell cell = EClass._map.cells[x, z];
                    if (cell?.room != room)
                    {
                        continue;
                    }

                    float surfaceHeight = FpsIdealizedWorld.GetCellSurfaceHeight(cell);
                    maxSurface = Mathf.Max(maxSurface, surfaceHeight);
                    hasSurface = true;

                    if (cell.HasWallOrFence || cell.HasFullBlock || cell?.sourceBlock?.tileType?.RepeatBlock == true)
                    {
                        maxWallTop = Mathf.Max(maxWallTop, ResolveWallTopHeight(cell, surfaceHeight));
                        hasWallTop = true;
                    }
                }
            }

            if (!hasSurface)
            {
                maxSurface = FpsIdealizedWorld.GetCellSurfaceHeight(EClass.pc?.pos?.cell);
            }

            return FpsIndoorCeilingResolver.Resolve(
                maxSurface,
                hasWallTop,
                maxWallTop,
                Plugin.Settings?.IndoorCeilingHeight?.Value ?? 1.1f);
        }

        private void UpdateSpritePreview(GpuViewPose pose, bool includePlayerSelf)
        {
            float maxDistance = Mathf.Max(1f, Plugin.Settings.MaxDistance.Value);
            _idealizedWorld.GatherSprites(pose.PlayerOrigin, maxDistance, includePlayerSelf, _wallMountedSprites, _uprightSprites, _groundSprites, _effectSprites);
            ApplySpriteBudgets();
            _wallMountedSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            _uprightSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            _groundSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            _effectSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            RecordSpriteDiagnostics();

            EnsureWallMountedPool(_wallMountedSprites.Count);
            EnsureGroundPool(_groundSprites.Count);
            EnsureUprightPool(_uprightSprites.Count + _effectSprites.Count);
            UpdateWallMountedSprites();
            UpdateGroundSprites();
            UpdateUprightSprites();
        }

        private void ApplySpriteBudgets()
        {
            FpsSpriteBudgetAllocator.ApplyBudget(
                _wallMountedSprites,
                Mathf.Max(0, Plugin.Settings.MaxWallMountedSprites.Value),
                static sprite => sprite.RenderPriority,
                static sprite => sprite.Distance);
            FpsSpriteBudgetAllocator.ApplyBudget(
                _uprightSprites,
                Mathf.Max(0, Plugin.Settings.MaxUprightSprites.Value),
                static sprite => sprite.RenderPriority,
                static sprite => sprite.Distance);
            FpsSpriteBudgetAllocator.ApplyBudget(
                _groundSprites,
                Mathf.Max(0, Plugin.Settings.MaxGroundSprites.Value),
                static sprite => sprite.RenderPriority,
                static sprite => sprite.Distance);
            FpsSpriteBudgetAllocator.ApplyBudget(
                _effectSprites,
                Mathf.Max(0, Plugin.Settings.MaxEffectSprites.Value),
                static sprite => sprite.RenderPriority,
                static sprite => sprite.Distance);
        }

        private void UpdateWallMountedSprites()
        {
            float maxDistance = Mathf.Max(1f, Plugin.Settings.MaxDistance.Value);
            int loggedSamples = 0;
            for (int i = 0; i < _wallMountedSprites.Count; i++)
            {
                FpsResolvedWallMountedSprite sprite = _wallMountedSprites[i];
                GameObject quad = _wallMountedQuads[i];
                MeshRenderer renderer = _wallMountedRenderers[i];
                MeshFilter filter = _wallMountedFilters[i];
                if (!IsVisibleToCamera(sprite.CellCenter, maxDistance, 0.17364818f))
                {
                    quad.SetActive(false);
                    continue;
                }

                Texture texture;
                bool usesDoorFallback = false;
                bool resolved = sprite.Sprite != null
                    ? _spriteTextureCache.TryGetWallMountedTexture(
                        sprite.Sprite,
                        sprite.FlipX,
                        sprite.TrimTransparent,
                        sprite.Light,
                        out texture)
                    : _spriteTextureCache.TryGetWallMountedTexture(
                        sprite.RenderData,
                        sprite.Tile,
                        sprite.FlipX,
                        sprite.TrimTransparent,
                        sprite.MaterialColor,
                        sprite.HasMaterialTint,
                        sprite.UseSelectiveMaterialTint,
                        sprite.Light,
                        out texture);
                if (!resolved && sprite.IsDoor)
                {
                    texture = Texture2D.whiteTexture;
                    resolved = true;
                    usesDoorFallback = true;
                }

                if (!resolved)
                {
                    quad.SetActive(false);
                    continue;
                }

                int dir = ResolveWallMountedDir(sprite);
                float textureWidthWorld = texture.width / 64f;
                float textureHeightWorld = texture.height / 64f;
                float panelWidthWorld = 1f;
                float panelHeightWorld = textureHeightWorld;
                float mountedHorizontalScale = Plugin.Settings?.WallMountedHorizontalScale?.Value ?? 0.72f;
                float wallBottom = 0f;
                float wallTop = 0f;
                Vector2 nativeDoorWorldSize = Vector2.zero;
                if (sprite.IsDoor)
                {
                    wallBottom = FpsIdealizedWorld.GetCellSurfaceHeight(sprite.Cell);
                    wallTop = ResolveWallTopHeight(sprite.Cell, wallBottom);
                    nativeDoorWorldSize = ResolveWallMountedNativeWorldSize(sprite.RenderData, texture);
                    float fallbackAspect = texture.height > 1 ? texture.width / (float)texture.height : 0.5f;
                    FpsDoorPanelDimensions dims = FpsDoorVisualLayout.ResolvePanelDimensions(
                        nativeDoorWorldSize.x,
                        nativeDoorWorldSize.y,
                        wallBottom,
                        wallTop,
                        fallbackAspect);
                    panelWidthWorld = FpsWallMountedAspect.ApplyHorizontalScale(dims.WidthWorld, mountedHorizontalScale);
                    panelHeightWorld = dims.HeightWorld;
                }
                FpsGpuFaceQuad face = sprite.UseWallPanelPlacement
                    ? (sprite.HasCustomAnchor
                        ? BuildWallPanelFaceAt(
                            sprite.MountWorld,
                            dir,
                            sprite.IsDoor ? panelWidthWorld : FpsWallMountedAspect.ApplyHorizontalScale(1f, mountedHorizontalScale),
                            sprite.IsDoor ? panelHeightWorld : textureHeightWorld,
                            sprite.IsDoor ? 0.06f : 0.03f)
                        : BuildWallPanelFace(
                            Mathf.FloorToInt(sprite.CellCenter.x - 0.5f),
                            Mathf.FloorToInt(sprite.CellCenter.z - 0.5f),
                            dir,
                            FpsIdealizedWorld.GetCellSurfaceHeight(sprite.Cell),
                            sprite.IsDoor ? panelWidthWorld : FpsWallMountedAspect.ApplyHorizontalScale(1f, mountedHorizontalScale),
                            sprite.IsDoor ? panelHeightWorld : textureHeightWorld,
                            sprite.IsDoor ? 0.06f : 0.03f))
                    : BuildWallMountedFace(sprite, dir, textureWidthWorld, textureHeightWorld);
                quad.SetActive(true);
                renderer.sharedMaterial = _spriteMaterial;
                ApplyWallQuadGeometry(quad, filter, face, true);

                _propertyBlock.Clear();
                _propertyBlock.SetTexture("_MainTex", texture);
                _propertyBlock.SetColor(
                    "_Color",
                    usesDoorFallback ? new Color32(150, 108, 60, 255) : Color.white);
                renderer.SetPropertyBlock(_propertyBlock);

                if (Plugin.Log != null && loggedSamples < 8)
                {
                    Plugin.Log.LogInfo(
                        $"GPU wall-mounted sample[{loggedSamples}]: label={sprite.DiagnosticLabel} cell=({sprite.Cell?.x},{sprite.Cell?.z}) dir={dir} " +
                        $"trim={sprite.TrimTransparent} door={sprite.IsDoor} panelPlacement={sprite.UseWallPanelPlacement} size=({texture.width},{texture.height}) " +
                        $"panel=({panelWidthWorld:0.###},{panelHeightWorld:0.###}) native=({nativeDoorWorldSize.x:0.###},{nativeDoorWorldSize.y:0.###}) " +
                        $"wall=({wallBottom:0.###},{wallTop:0.###}) opaque={DescribeOpaqueBounds(texture)} materialTint={sprite.HasMaterialTint} selectiveTint={sprite.UseSelectiveMaterialTint}");
                    loggedSamples++;
                }
            }

            for (int i = _wallMountedSprites.Count; i < _wallMountedQuads.Count; i++)
            {
                _wallMountedQuads[i].SetActive(false);
            }
        }

        private void UpdateGroundSprites()
        {
            for (int i = 0; i < _groundSprites.Count; i++)
            {
                FpsResolvedGroundSprite sprite = _groundSprites[i];
                GameObject quad = _groundQuads[i];
                MeshRenderer renderer = _groundRenderers[i];
                if (!IsVisibleToCamera(sprite.CenterWorld, sprite.VisibilityDistance, sprite.VisibilityConeDot))
                {
                    quad.SetActive(false);
                    continue;
                }

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
                    ? ApplyAtmosphericFog(Color.white, sprite.CenterWorld, Plugin.Settings.MaxDistance.Value, 0.12f, "ground-baked")
                    : ApplyAtmosphericFog(
                        ApplySurfaceStyle(ResolveSpriteDisplayColor(sprite, sprite.MaterialColor, sprite.HasMaterialTint, sprite.Light), FpsVisualSurfaceKind.GroundSprite, 0, false),
                        sprite.CenterWorld,
                        Plugin.Settings.MaxDistance.Value,
                        0.12f,
                        "ground"));
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
                    VisibilityDistance = effect.VisibilityDistance,
                    VisibilityConeDot = effect.VisibilityConeDot,
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
            if (!IsVisibleToCamera(sprite.AnchorWorld, sprite.VisibilityDistance, sprite.VisibilityConeDot))
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
                ? ApplyAtmosphericFog(Color.white, center, Plugin.Settings.MaxDistance.Value, 0.12f, "upright-baked")
                : ApplyAtmosphericFog(
                    ApplySurfaceStyle(ResolveSpriteDisplayColor(sprite, sprite.MaterialColor, sprite.HasMaterialTint, sprite.Light), FpsVisualSurfaceKind.UprightSprite, 0, false),
                    center,
                    Plugin.Settings.MaxDistance.Value,
                    0.12f,
                    "upright"));
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

        private bool IsVisibleToCamera(Vector3 position, float maxDistance, float minConeDot)
        {
            if (_camera == null)
            {
                return false;
            }

            Vector3 delta = position - _camera.transform.position;
            float distance = delta.magnitude;
            if (distance > maxDistance)
            {
                return false;
            }

            if (distance <= 0.05f)
            {
                return true;
            }

            Vector3 horizontalDelta = delta;
            horizontalDelta.y = 0f;
            float horizontalDistance = horizontalDelta.magnitude;
            if (horizontalDistance > 0.05f)
            {
                Vector3 horizontalDir = horizontalDelta / horizontalDistance;
                Vector3 horizontalForward = _camera.transform.forward;
                horizontalForward.y = 0f;
                if (horizontalForward.sqrMagnitude > 0.0001f)
                {
                    horizontalForward.Normalize();
                    float dot = Vector3.Dot(horizontalForward, horizontalDir);
                    if (dot < minConeDot)
                    {
                        return false;
                    }
                }
            }

            Vector3 viewport = _camera.WorldToViewportPoint(position);
            return viewport.z > 0f && viewport.x >= -0.2f && viewport.x <= 1.2f && viewport.y >= -0.2f && viewport.y <= 1.2f;
        }

        private int ResolveWallMountedDir(FpsResolvedWallMountedSprite sprite)
        {
            return ((sprite.FaceDir % 4) + 4) % 4;
        }

        private static FpsGpuFaceQuad BuildWallMountedFace(FpsResolvedWallMountedSprite sprite, int dir, float widthWorld, float heightWorld)
        {
            float halfWidth = Mathf.Max(0.05f, widthWorld) * 0.5f;
            float height = Mathf.Max(0.05f, heightWorld);
            float baseY = FpsIdealizedWorld.GetCellSurfaceHeight(sprite.Cell);
            float topY = baseY + height;
            float inset = 0.015f;
            float cellX = sprite.CellCenter.x - 0.5f;
            float cellZ = sprite.CellCenter.z - 0.5f;
            float centerX = sprite.CellCenter.x;
            float centerZ = sprite.CellCenter.z;

            switch ((dir % 4 + 4) % 4)
            {
                case 0:
                    return new FpsGpuFaceQuad(
                        new Vector3(centerX - halfWidth, baseY, cellZ + inset),
                        new Vector3(centerX + halfWidth, baseY, cellZ + inset),
                        new Vector3(centerX - halfWidth, topY, cellZ + inset),
                        new Vector3(centerX + halfWidth, topY, cellZ + inset));
                case 1:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + 1f - inset, baseY, centerZ - halfWidth),
                        new Vector3(cellX + 1f - inset, baseY, centerZ + halfWidth),
                        new Vector3(cellX + 1f - inset, topY, centerZ - halfWidth),
                        new Vector3(cellX + 1f - inset, topY, centerZ + halfWidth));
                case 2:
                    return new FpsGpuFaceQuad(
                        new Vector3(centerX + halfWidth, baseY, cellZ + 1f - inset),
                        new Vector3(centerX - halfWidth, baseY, cellZ + 1f - inset),
                        new Vector3(centerX + halfWidth, topY, cellZ + 1f - inset),
                        new Vector3(centerX - halfWidth, topY, cellZ + 1f - inset));
                default:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + inset, baseY, centerZ + halfWidth),
                        new Vector3(cellX + inset, baseY, centerZ - halfWidth),
                        new Vector3(cellX + inset, topY, centerZ + halfWidth),
                        new Vector3(cellX + inset, topY, centerZ - halfWidth));
            }
        }

        private static FpsGpuFaceQuad BuildWallPanelFace(int cellX, int cellZ, int dir, float baseY, float widthWorld, float heightWorld, float inset = 0.015f)
        {
            float halfWidth = Mathf.Max(0.05f, widthWorld) * 0.5f;
            float height = Mathf.Max(0.05f, heightWorld);
            float topY = baseY + height;
            float centerX = cellX + 0.5f;
            float centerZ = cellZ + 0.5f;

            switch ((dir % 4 + 4) % 4)
            {
                case 0:
                    return new FpsGpuFaceQuad(
                        new Vector3(centerX - halfWidth, baseY, cellZ + inset),
                        new Vector3(centerX + halfWidth, baseY, cellZ + inset),
                        new Vector3(centerX - halfWidth, topY, cellZ + inset),
                        new Vector3(centerX + halfWidth, topY, cellZ + inset));
                case 1:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + 1f - inset, baseY, centerZ - halfWidth),
                        new Vector3(cellX + 1f - inset, baseY, centerZ + halfWidth),
                        new Vector3(cellX + 1f - inset, topY, centerZ - halfWidth),
                        new Vector3(cellX + 1f - inset, topY, centerZ + halfWidth));
                case 2:
                    return new FpsGpuFaceQuad(
                        new Vector3(centerX + halfWidth, baseY, cellZ + 1f - inset),
                        new Vector3(centerX - halfWidth, baseY, cellZ + 1f - inset),
                        new Vector3(centerX + halfWidth, topY, cellZ + 1f - inset),
                        new Vector3(centerX - halfWidth, topY, cellZ + 1f - inset));
                default:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + inset, baseY, centerZ + halfWidth),
                        new Vector3(cellX + inset, baseY, centerZ - halfWidth),
                        new Vector3(cellX + inset, topY, centerZ + halfWidth),
                        new Vector3(cellX + inset, topY, centerZ - halfWidth));
            }
        }

        private static FpsGpuFaceQuad BuildWallPanelFaceAt(Vector3 anchorWorld, int dir, float widthWorld, float heightWorld, float inset = 0.015f)
        {
            float halfWidth = Mathf.Max(0.05f, widthWorld) * 0.5f;
            float height = Mathf.Max(0.05f, heightWorld);
            float topY = anchorWorld.y + height;

            switch ((dir % 4 + 4) % 4)
            {
                case 0:
                    return new FpsGpuFaceQuad(
                        new Vector3(anchorWorld.x - halfWidth, anchorWorld.y, anchorWorld.z + inset),
                        new Vector3(anchorWorld.x + halfWidth, anchorWorld.y, anchorWorld.z + inset),
                        new Vector3(anchorWorld.x - halfWidth, topY, anchorWorld.z + inset),
                        new Vector3(anchorWorld.x + halfWidth, topY, anchorWorld.z + inset));
                case 1:
                    return new FpsGpuFaceQuad(
                        new Vector3(anchorWorld.x + inset, anchorWorld.y, anchorWorld.z - halfWidth),
                        new Vector3(anchorWorld.x + inset, anchorWorld.y, anchorWorld.z + halfWidth),
                        new Vector3(anchorWorld.x + inset, topY, anchorWorld.z - halfWidth),
                        new Vector3(anchorWorld.x + inset, topY, anchorWorld.z + halfWidth));
                case 2:
                    return new FpsGpuFaceQuad(
                        new Vector3(anchorWorld.x + halfWidth, anchorWorld.y, anchorWorld.z - inset),
                        new Vector3(anchorWorld.x - halfWidth, anchorWorld.y, anchorWorld.z - inset),
                        new Vector3(anchorWorld.x + halfWidth, topY, anchorWorld.z - inset),
                        new Vector3(anchorWorld.x - halfWidth, topY, anchorWorld.z - inset));
                default:
                    return new FpsGpuFaceQuad(
                        new Vector3(anchorWorld.x - inset, anchorWorld.y, anchorWorld.z + halfWidth),
                        new Vector3(anchorWorld.x - inset, anchorWorld.y, anchorWorld.z - halfWidth),
                        new Vector3(anchorWorld.x - inset, topY, anchorWorld.z + halfWidth),
                        new Vector3(anchorWorld.x - inset, topY, anchorWorld.z - halfWidth));
            }
        }

        private static Vector2 ResolveWallMountedNativeWorldSize(RenderData renderData, Texture texture)
        {
            const float spritePixelsPerTile = 64f;
            if (renderData?.pass?.pmesh != null && renderData.pass.pmesh.tiling.x > 0f && renderData.pass.pmesh.tiling.y > 0f)
            {
                Texture sourceTexture = renderData.pass.mat?.GetTexture("_MainTex");
                if (sourceTexture != null)
                {
                    float sourceWidth = sourceTexture.width / renderData.pass.pmesh.tiling.x;
                    float sourceHeight = sourceTexture.height / renderData.pass.pmesh.tiling.y;
                    if (renderData.multiSize)
                    {
                        sourceHeight *= 2f;
                    }

                    float scaleX = Mathf.Max(0.1f, renderData.imageScale.x);
                    float scaleY = Mathf.Max(0.1f, renderData.imageScale.y);
                    return new Vector2(
                        Mathf.Max(0.02f, sourceWidth * scaleX / spritePixelsPerTile),
                        Mathf.Max(0.02f, sourceHeight * scaleY / spritePixelsPerTile));
                }
            }

            if (texture != null)
            {
                return new Vector2(
                    Mathf.Max(0.02f, texture.width / spritePixelsPerTile),
                    Mathf.Max(0.02f, texture.height / spritePixelsPerTile));
            }

            return Vector2.one;
        }

        private static string DescribeOpaqueBounds(Texture texture)
        {
            if (!(texture is Texture2D texture2D))
            {
                return "n/a";
            }

            try
            {
                Color32[] pixels = texture2D.GetPixels32();
                int width = texture2D.width;
                int height = texture2D.height;
                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[rowOffset + x].a <= 8)
                        {
                            continue;
                        }

                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }

                return maxX < minX || maxY < minY
                    ? "empty"
                    : $"({minX},{minY})-({maxX},{maxY})/{width}x{height}";
            }
            catch
            {
                return "unreadable";
            }
        }

        private bool IsTerrainVisibleToCamera(Vector3 position, GpuViewPose pose, float maxDistance, float distancePadding, float viewportPadding)
        {
            if (_camera == null)
            {
                return false;
            }

            Vector3 delta = position - _camera.transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > maxDistance + distancePadding)
            {
                return false;
            }

            if (distance <= 2.5f)
            {
                return true;
            }

            if (distance > 0.05f)
            {
                Vector2 dir = new Vector2(delta.x, delta.z) / distance;
                if (Vector2.Dot(pose.Forward, dir) < 0.17364818f)
                {
                    return false;
                }
            }

            Vector3 viewport = _camera.WorldToViewportPoint(position);
            return viewport.z > 0f
                && viewport.x >= -viewportPadding
                && viewport.x <= 1f + viewportPadding
                && viewport.y >= -viewportPadding
                && viewport.y <= 1f + viewportPadding;
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

        private static bool IsCurrentIndoorCell(Cell cell)
        {
            Room currentRoom = EClass.pc?.pos?.cell?.room;
            return currentRoom != null && cell?.room == currentRoom;
        }

        private static Color32 MultiplyColor(Color32 source, Color tint)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.r * tint.r), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.g * tint.g), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.b * tint.b), 0, 255),
                source.a);
        }

        private Color ResolvePreviewTint(FpsResolvedFloorSurface surface)
        {
            Color32 color = new Color32(255, 255, 255, 255);
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
            _fogDiagnosticSamples.Clear();
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
                $"wallPanels={_diagnostics.WallPanels} wallPanelFallbacks={_diagnostics.WallPanelFallbacks} panelFront={_diagnostics.PanelFrontFacing} panelBack={_diagnostics.PanelBackFacing}");

            for (int i = 0; i < _diagnosticSamples.Count; i++)
            {
                Plugin.Log.LogInfo($"GPU wall sample[{i}]: {_diagnosticSamples[i]}");
            }

            for (int i = 0; i < _spriteDiagnosticSamples.Count; i++)
            {
                Plugin.Log.LogInfo($"GPU sprite sample[{i}]: {_spriteDiagnosticSamples[i]}");
            }

            for (int i = 0; i < _fogDiagnosticSamples.Count; i++)
            {
                Plugin.Log.LogInfo(_fogDiagnosticSamples[i]);
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

        private void EnsureRoofPool(int count)
        {
            while (_roofQuads.Count < count)
            {
                GameObject quad = new GameObject($"GpuRoofQuad_{_roofQuads.Count}");
                quad.hideFlags = HideFlags.HideAndDontSave;
                quad.transform.SetParent(_roofRoot.transform, false);
                SetLayerRecursively(quad, RenderLayer);

                MeshFilter filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = CreateWallQuadMesh($"FpsGpuRoofQuad_{_roofQuads.Count}");

                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _groundMaterial ?? _spriteMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                _roofQuads.Add(quad);
                _roofRenderers.Add(renderer);
                _roofFilters.Add(filter);
                quad.SetActive(false);
            }
        }

        private void EnsureWallMountedPool(int count)
        {
            while (_wallMountedQuads.Count < count)
            {
                GameObject quad = new GameObject($"GpuWallMountedQuad_{_wallMountedQuads.Count}");
                quad.hideFlags = HideFlags.HideAndDontSave;
                quad.transform.SetParent(_wallMountedRoot.transform, false);
                SetLayerRecursively(quad, RenderLayer);

                MeshFilter filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = CreateWallQuadMesh($"FpsGpuWallMountedQuad_{_wallMountedQuads.Count}");

                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _wallMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                _wallMountedQuads.Add(quad);
                _wallMountedRenderers.Add(renderer);
                _wallMountedFilters.Add(filter);
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

        private static void ApplyWallStructureUv(Mesh mesh, FpsWallStructureQuad quad)
        {
            FpsUvRect uv = FpsWallTextureUv.ComputeUvRect(quad);
            SetQuadUv(mesh, new Rect(uv.XMin, uv.YMin, uv.Width, uv.Height));
        }

        private static Vector3 ToVector3(FpsStructurePoint3 point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }

        private static int GetPreviewDiameter()
        {
            return MaxPreviewRadius * 2 + 1;
        }

        private static bool IsSolidWall(Cell cell)
        {
            return cell != null && (cell.HasFullBlock || cell.HasWallOrFence);
        }

        private static int ComputeFullBlockSurfaceKey(FpsResolvedWallSurface surface)
        {
            unchecked
            {
                int renderDataId = surface.RenderData != null ? surface.RenderData.GetInstanceID() : 0;
                int hash = 17;
                hash = hash * 31 + renderDataId;
                hash = hash * 31 + surface.Tile;
                hash = hash * 31 + surface.MaterialColor;
                return hash;
            }
        }

        private static int ComputePanelWallSurfaceKey(RenderData renderData, int tile, bool flipX, int materialColor, FpsResolvedLightSample light)
        {
            unchecked
            {
                int renderDataId = renderData != null ? renderData.GetInstanceID() : 0;
                int hash = 17;
                hash = hash * 31 + renderDataId;
                hash = hash * 31 + tile;
                hash = hash * 31 + (flipX ? 1 : 0);
                hash = hash * 31 + materialColor;
                hash = hash * 31 + light.PackedLight;
                return hash;
            }
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

            float neighborBottom = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor);
            float neighborTop = ResolveWallTopHeight(neighbor, neighborBottom);
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
            Vector2 logicalOrigin = new Vector2(EClass.pc.pos.x + 0.5f, EClass.pc.pos.z + 0.5f);
            if (EClass.pc?.renderer == null || EClass.screen == null || EClass.screen.tileMap == null || EClass._zone.IsRegion)
            {
                return logicalOrigin;
            }

            float alignX = EClass.screen.tileAlign.x;
            float alignY = EClass.screen.tileAlign.y;
            if (Mathf.Abs(alignX) < 0.0001f || Mathf.Abs(alignY) < 0.0001f)
            {
                return logicalOrigin;
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
                return logicalOrigin;
            }

            Vector2 projectedOrigin = new Vector2(x + 0.5f, z + 0.5f);
            if (FpsViewManager.IsDungeonCrawlerModeActive)
            {
                return FpsViewOriginResolver.ResolveAxisLocked(
                    logicalOrigin,
                    projectedOrigin,
                    EClass.pc != null ? EClass.pc.dir : 0,
                    MaxDungeonCrawlerAxisDeviation);
            }

            return FpsViewOriginResolver.Resolve(logicalOrigin, projectedOrigin, false, MaxProjectedOriginDeviation);
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

        private sealed class TerrainChunkBuildContext
        {
            public TerrainChunkBuildContext(int chunkX, int chunkZ)
            {
                ChunkX = chunkX;
                ChunkZ = chunkZ;
            }

            public int ChunkX { get; }

            public int ChunkZ { get; }

            public List<TerrainChunkSourceCell> Cells { get; } = new List<TerrainChunkSourceCell>(TerrainChunkSize * TerrainChunkSize);

            public Vector3 CenterWorld
            {
                get
                {
                    float centerX = ChunkX * TerrainChunkSize + TerrainChunkSize * 0.5f;
                    float centerZ = ChunkZ * TerrainChunkSize + TerrainChunkSize * 0.5f;
                    if (Cells.Count == 0)
                    {
                        return new Vector3(centerX, 0f, centerZ);
                    }

                    float totalHeight = 0f;
                    for (int i = 0; i < Cells.Count; i++)
                    {
                        totalHeight += Cells[i].Height;
                    }

                    return new Vector3(centerX, totalHeight / Cells.Count, centerZ);
                }
            }
        }

        private readonly struct TerrainChunkSourceCell
        {
            public TerrainChunkSourceCell(
                int localX,
                int localZ,
                float height,
                bool hasFloorSurface,
                FpsResolvedFloorSurface floorSurface,
                bool hasBlockSurface,
                FpsResolvedWallSurface blockTopSurface,
                bool emitFlatCliffSides,
                FpsTerrainChunkArchetype archetype,
                float baseHeight,
                bool hasNorthNeighbor,
                float northNeighborHeight,
                bool hasEastNeighbor,
                float eastNeighborHeight,
                bool hasSouthNeighbor,
                float southNeighborHeight,
                bool hasWestNeighbor,
                float westNeighborHeight,
                bool hasSideSurface,
                FpsResolvedWallSurface sideSurface,
                int rampDir,
                int rampStepCount,
                bool hasBridgePillar,
                float bridgeBaseHeight)
            {
                LocalX = localX;
                LocalZ = localZ;
                Height = height;
                HasFloorSurface = hasFloorSurface;
                FloorSurface = floorSurface;
                HasBlockSurface = hasBlockSurface;
                BlockTopSurface = blockTopSurface;
                EmitFlatCliffSides = emitFlatCliffSides;
                Archetype = archetype;
                BaseHeight = baseHeight;
                HasNorthNeighbor = hasNorthNeighbor;
                NorthNeighborHeight = northNeighborHeight;
                HasEastNeighbor = hasEastNeighbor;
                EastNeighborHeight = eastNeighborHeight;
                HasSouthNeighbor = hasSouthNeighbor;
                SouthNeighborHeight = southNeighborHeight;
                HasWestNeighbor = hasWestNeighbor;
                WestNeighborHeight = westNeighborHeight;
                HasSideSurface = hasSideSurface;
                SideSurface = sideSurface;
                RampDir = rampDir;
                RampStepCount = rampStepCount;
                HasBridgePillar = hasBridgePillar;
                BridgeBaseHeight = bridgeBaseHeight;
            }

            public int LocalX { get; }

            public int LocalZ { get; }

            public float Height { get; }

            public bool HasFloorSurface { get; }

            public FpsResolvedFloorSurface FloorSurface { get; }

            public bool HasBlockSurface { get; }

            public FpsResolvedWallSurface BlockTopSurface { get; }

            public bool EmitFlatCliffSides { get; }

            public FpsTerrainChunkArchetype Archetype { get; }

            public float BaseHeight { get; }

            public bool HasNorthNeighbor { get; }

            public float NorthNeighborHeight { get; }

            public bool HasEastNeighbor { get; }

            public float EastNeighborHeight { get; }

            public bool HasSouthNeighbor { get; }

            public float SouthNeighborHeight { get; }

            public bool HasWestNeighbor { get; }

            public float WestNeighborHeight { get; }

            public bool HasSideSurface { get; }

            public FpsResolvedWallSurface SideSurface { get; }

            public int RampDir { get; }

            public int RampStepCount { get; }

            public bool HasBridgePillar { get; }

            public float BridgeBaseHeight { get; }
        }

        private struct TerrainChunkVisualState
        {
            public bool HasData;
            public int ChunkX;
            public int ChunkZ;
            public int Signature;
            public int LightingSignature;
            public Texture2D Texture;

            public bool Matches(int chunkX, int chunkZ, int signature)
            {
                return HasData
                    && ChunkX == chunkX
                    && ChunkZ == chunkZ
                    && Signature == signature
                    && Texture != null;
            }
        }

        private readonly struct HybridPanelWallSurface
        {
            public HybridPanelWallSurface(RenderData renderData, int tile, bool flipX, FpsResolvedWallSurface surface)
            {
                RenderData = renderData;
                Tile = tile;
                FlipX = flipX;
                Surface = surface;
            }

            public RenderData RenderData { get; }

            public int Tile { get; }

            public bool FlipX { get; }

            public FpsResolvedWallSurface Surface { get; }
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
            public int WallPanels;
            public int WallPanelFallbacks;
            public int PanelFrontFacing;
            public int PanelBackFacing;
        }
    }
}
