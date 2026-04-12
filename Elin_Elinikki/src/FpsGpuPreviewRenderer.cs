using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BeautifyEffect;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace Elin_Elinikki
{
    internal sealed class FpsGpuPreviewRenderer : IDisposable
    {
        internal const int RenderLayer = 29;
        private const float RenderHeightOffset = 0.01f;
        private const int MaxPreviewRadius = 32;
        private const int TerrainChunkSize = 8;
        private const int TerrainTileTextureSize = 16;
        private const int TerrainTextureSlotCount = 3;
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
        private readonly List<FpsResolvedRoofPlane> _roofPlanes = new List<FpsResolvedRoofPlane>(128);
        private readonly List<FpsResolvedRoofStructure> _roofStructures = new List<FpsResolvedRoofStructure>(32);
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
        private readonly List<RoofVisualState> _roofVisualStates = new List<RoofVisualState>(128);
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

            bool cullWorldBackfaces = Plugin.Settings?.EnableWorldMeshBackfaceCulling?.Value != false;
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
            float terrainMaxDistance = Mathf.Max(1f, Plugin.Settings.MaxDistance.Value * Plugin.Settings.TerrainDistanceMultiplier.Value);
            int radius = Mathf.Min(Mathf.CeilToInt(terrainMaxDistance), MaxPreviewRadius);
            int minX = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.x) - radius);
            int maxX = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.x) + radius);
            int minZ = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.y) - radius);
            int maxZ = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.y) + radius);

            int activeWallIndex = 0;
            bool useHybridWallGeometry = Plugin.Settings?.EnableHybridWallGeometry?.Value == true;
            _fullBlockWallSeeds.Clear();
            _fullBlockWallRuns.Clear();
            _fullBlockWallSurfaceLookup.Clear();
            _panelWallSeeds.Clear();
            _panelWallRuns.Clear();
            _panelDoorFrameRuns.Clear();
            _panelWallSurfaceLookup.Clear();
            _terrainChunkContexts.Clear();
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Cell cell = EClass._map.cells[x, z];
                    Vector3 cellCenter = new Vector3(x + 0.5f, FpsIdealizedWorld.GetCellSurfaceHeight(cell), z + 0.5f);
                    bool terrainVisible = IsTerrainVisibleToCamera(cellCenter, pose, terrainMaxDistance, 0f, 0.35f);
                    bool structureVisible = IsTerrainVisibleToCamera(cellCenter, pose, terrainMaxDistance + 0.25f, 0f, 0.45f);
                    if (!terrainVisible && !structureVisible)
                    {
                        continue;
                    }

                    bool hasSurface = _idealizedWorld.TryResolveFloor(cell, cell?.index ?? -1, out FpsResolvedFloorSurface surface);
                    bool isFullBlock = cell != null && cell.HasFullBlock;
                    FpsResolvedWallSurface blockTopSurface = default;
                    bool hasBlockSurface = isFullBlock && _idealizedWorld.TryResolveWall(cell, out blockTopSurface);

                    if (terrainVisible)
                    {
                        float surfaceHeight = isFullBlock
                            ? FpsIdealizedWorld.GetCellSurfaceHeight(cell) + 1f + RenderHeightOffset
                            : FpsIdealizedWorld.GetCellSurfaceHeight(cell) + RenderHeightOffset;
                        CollectTerrainChunkCell(x, z, surfaceHeight, hasSurface, surface, hasBlockSurface, blockTopSurface);
                    }

                    if (structureVisible && IsSolidWall(cell) && _idealizedWorld.TryResolveWall(cell, out FpsResolvedWallSurface wallSurface))
                    {
                        if (useHybridWallGeometry && cell.HasFullBlock)
                        {
                            CollectFullBlockWallSeeds(x, z, wallSurface);
                        }
                        else if (useHybridWallGeometry && cell.HasWallOrFence)
                        {
                            CollectPanelWallSeeds(x, z, wallSurface);
                        }
                        else
                        {
                            activeWallIndex = AddWallQuads(activeWallIndex, x, z, wallSurface);
                        }
                    }
                }
            }

            if (useHybridWallGeometry && _fullBlockWallSeeds.Count > 0)
            {
                _fullBlockWallRuns.AddRange(FpsWallRunPlanner.BuildRuns(_fullBlockWallSeeds));
                activeWallIndex = RenderHybridFullBlockRuns(activeWallIndex);
            }

            if (useHybridWallGeometry && _panelWallSeeds.Count > 0)
            {
                _panelWallRuns.AddRange(FpsWallRunPlanner.BuildRuns(_panelWallSeeds));
                activeWallIndex = RenderHybridPanelRuns(activeWallIndex);
            }

            int activeTerrainChunks = RenderTerrainChunks(terrainMaxDistance);

            for (int i = activeWallIndex; i < _wallQuads.Count; i++)
            {
                _wallQuads[i].SetActive(false);
            }

            LogDebugFrame(activeTerrainChunks, pose);
        }

        private void CollectTerrainChunkCell(
            int cellX,
            int cellZ,
            float surfaceHeight,
            bool hasFloorSurface,
            FpsResolvedFloorSurface floorSurface,
            bool hasBlockSurface,
            FpsResolvedWallSurface blockTopSurface)
        {
            int chunkX = Mathf.FloorToInt(cellX / (float)TerrainChunkSize);
            int chunkZ = Mathf.FloorToInt(cellZ / (float)TerrainChunkSize);
            int key = GetTerrainChunkKey(chunkX, chunkZ);
            if (!_terrainChunkContexts.TryGetValue(key, out TerrainChunkBuildContext context))
            {
                context = new TerrainChunkBuildContext(chunkX, chunkZ);
                _terrainChunkContexts.Add(key, context);
            }

            Cell cell = EClass._map.cells[cellX, cellZ];
            bool allowRisers = cell != null && !cell.HasFullBlock;
            bool hasRamp = cell != null && cell.HasRamp;
            int rampDir = hasRamp ? cell.blockDir : 0;
            int rampStepCount = 0;
            FpsResolvedFloorSurface baseFloorSurface = default;
            float cellBaseHeight = cell != null ? GetCellBaseHeight(cell) + RenderHeightOffset : 0f;
            float rampBaseHeight = surfaceHeight;
            if (hasRamp && cell.sourceBlock?.tileType != null)
            {
                float rampDrop = cell.sourceBlock.tileType.slopeHeight * FpsIdealizedWorld.GetTerrainHeightScale();
                rampBaseHeight = Mathf.Max(cellBaseHeight, surfaceHeight - rampDrop);
                rampStepCount = Mathf.Clamp(Mathf.RoundToInt(cell.sourceBlock.tileType.slopeHeight / 2f), 2, 6);
            }
            bool hasBridgePillar = cell != null
                && cell.HasBridge
                && cell.bridgeHeight > cell.height
                && cell.sourceBridge?.tileType?.ShowPillar == true;
            float bridgeBaseHeight = hasBridgePillar ? cellBaseHeight : 0f;
            bool hasUndersideDeck = cell != null && surfaceHeight > cellBaseHeight + 0.02f;
            bool hasNorthNeighbor = TryGetTerrainNeighborHeight(cellX, cellZ - 1, out float northNeighborHeight);
            bool hasEastNeighbor = TryGetTerrainNeighborHeight(cellX + 1, cellZ, out float eastNeighborHeight);
            bool hasSouthNeighbor = TryGetTerrainNeighborHeight(cellX, cellZ + 1, out float southNeighborHeight);
            bool hasWestNeighbor = TryGetTerrainNeighborHeight(cellX - 1, cellZ, out float westNeighborHeight);
            FpsResolvedWallSurface riserSurface = default;
            bool hasRiserSurface = allowRisers && _idealizedWorld.TryResolveTerrainRiser(EClass._map.cells[cellX, cellZ], out riserSurface);
            bool hasBaseFloorSurface = cell != null
                && cell.HasBridge
                && surfaceHeight > cellBaseHeight + 0.02f
                && _idealizedWorld.TryResolveBaseFloor(cell, cellZ * EClass._map.Size + cellX, out baseFloorSurface);
            float baseFloorHeight = hasBaseFloorSurface
                ? cell.height * FpsIdealizedWorld.GetTerrainHeightScale() + ((baseFloorSurface.Floor != null ? baseFloorSurface.Floor.tileType.FloorHeight : 0f)) + RenderHeightOffset
                : 0f;
            context.Cells.Add(new TerrainChunkSourceCell(
                cellX - chunkX * TerrainChunkSize,
                cellZ - chunkZ * TerrainChunkSize,
                surfaceHeight,
                hasFloorSurface,
                floorSurface,
                hasBlockSurface,
                blockTopSurface,
                allowRisers,
                hasNorthNeighbor,
                northNeighborHeight + RenderHeightOffset,
                hasEastNeighbor,
                eastNeighborHeight + RenderHeightOffset,
                hasSouthNeighbor,
                southNeighborHeight + RenderHeightOffset,
                hasWestNeighbor,
                westNeighborHeight + RenderHeightOffset,
                hasRiserSurface,
                riserSurface,
                hasBaseFloorSurface,
                baseFloorSurface,
                baseFloorHeight,
                hasRamp,
                rampDir,
                rampStepCount,
                rampBaseHeight,
                hasBridgePillar,
                bridgeBaseHeight,
                hasUndersideDeck,
                cellBaseHeight));
        }

        private int RenderTerrainChunks(float terrainMaxDistance)
        {
            _orderedTerrainChunks.Clear();
            foreach (TerrainChunkBuildContext context in _terrainChunkContexts.Values)
            {
                if (context.Cells.Count > 0)
                {
                    _orderedTerrainChunks.Add(context);
                }
            }

            _orderedTerrainChunks.Sort(static (a, b) =>
            {
                int zCompare = a.ChunkZ.CompareTo(b.ChunkZ);
                return zCompare != 0 ? zCompare : a.ChunkX.CompareTo(b.ChunkX);
            });

            int activeIndex = 0;
            for (int i = 0; i < _orderedTerrainChunks.Count; i++)
            {
                TerrainChunkBuildContext chunk = _orderedTerrainChunks[i];
                EnsureTerrainPool(activeIndex + 1);
                EnsureTerrainChunkStateCount(activeIndex + 1);

                GameObject quad = _terrainQuads[activeIndex];
                MeshRenderer renderer = _terrainRenderers[activeIndex];
                MeshFilter filter = _terrainFilters[activeIndex];
                quad.SetActive(true);
                _terrainOverlayQuads[activeIndex].SetActive(false);
                quad.transform.localPosition = Vector3.zero;
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = Vector3.one;

                int signature = ComputeTerrainChunkSignature(chunk);
                int lightingSignature = ComputeTerrainChunkLightingSignature(chunk);
                TerrainChunkVisualState state = _terrainChunkStates[activeIndex];
                if (!state.Matches(chunk.ChunkX, chunk.ChunkZ, signature))
                {
                    RebuildTerrainChunkMesh(filter.sharedMesh, chunk);
                    Texture2D texture = BuildTerrainChunkTexture(chunk);
                    if (state.Texture != null && state.Texture != texture)
                    {
                        UnityEngine.Object.Destroy(state.Texture);
                    }

                    state = new TerrainChunkVisualState
                    {
                        HasData = true,
                        ChunkX = chunk.ChunkX,
                        ChunkZ = chunk.ChunkZ,
                        Signature = signature,
                        LightingSignature = lightingSignature,
                        Texture = texture
                    };
                    _terrainChunkStates[activeIndex] = state;
                }
                else if (state.LightingSignature != lightingSignature)
                {
                    RebuildTerrainChunkMesh(filter.sharedMesh, chunk);
                    state.LightingSignature = lightingSignature;
                    _terrainChunkStates[activeIndex] = state;
                }

                _propertyBlock.Clear();
                _propertyBlock.SetTexture("_MainTex", state.Texture != null ? state.Texture : Texture2D.whiteTexture);
                _propertyBlock.SetColor("_Color", ApplyAtmosphericFog(Color.white, chunk.CenterWorld, terrainMaxDistance, 0.18f, "terrain-floor"));
                renderer.SetPropertyBlock(_propertyBlock);
                activeIndex++;
            }

            for (int i = activeIndex; i < _terrainQuads.Count; i++)
            {
                _terrainQuads[i].SetActive(false);
                _terrainOverlayQuads[i].SetActive(false);
            }

            return activeIndex;
        }

        private void EnsureTerrainChunkStateCount(int count)
        {
            while (_terrainChunkStates.Count < count)
            {
                _terrainChunkStates.Add(default);
            }
        }

        private static int GetTerrainChunkKey(int chunkX, int chunkZ)
        {
            return (chunkX << 16) ^ (chunkZ & 0xFFFF);
        }

        private static int ComputeTerrainChunkSignature(TerrainChunkBuildContext chunk)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + chunk.ChunkX;
                hash = hash * 31 + chunk.ChunkZ;
                for (int i = 0; i < chunk.Cells.Count; i++)
                {
                    TerrainChunkSourceCell cell = chunk.Cells[i];
                    hash = hash * 31 + cell.LocalX;
                    hash = hash * 31 + cell.LocalZ;
                    hash = hash * 31 + cell.Height.GetHashCode();
                    hash = hash * 31 + (cell.HasFloorSurface ? 1 : 0);
                    hash = hash * 31 + (cell.HasBlockSurface ? 1 : 0);
                    hash = hash * 31 + (cell.AllowRisers ? 1 : 0);
                    hash = hash * 31 + (cell.HasNorthNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.NorthNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasEastNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.EastNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasSouthNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.SouthNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasWestNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.WestNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasRiserSurface ? 1 : 0);
                    hash = hash * 31 + (cell.HasBaseFloorSurface ? 1 : 0);
                    hash = hash * 31 + cell.BaseFloorHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasRamp ? 1 : 0);
                    hash = hash * 31 + cell.RampDir;
                    hash = hash * 31 + cell.RampStepCount;
                    hash = hash * 31 + cell.RampBaseHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasBridgePillar ? 1 : 0);
                    hash = hash * 31 + cell.BridgeBaseHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasUndersideDeck ? 1 : 0);
                    hash = hash * 31 + cell.UndersideDeckBaseHeight.GetHashCode();
                    if (cell.HasBlockSurface)
                    {
                        hash = hash * 31 + cell.BlockTopSurface.Tile;
                        hash = hash * 31 + cell.BlockTopSurface.MaterialColor;
                    }
                    else
                    {
                        hash = hash * 31 + cell.FloorSurface.BaseTile;
                        hash = hash * 31 + cell.FloorSurface.AutoTileOverlay;
                        hash = hash * 31 + (cell.FloorSurface.UseSnowAtlas ? 1 : 0);
                        hash = hash * 31 + (cell.FloorSurface.UseWaterAutoTileAtlas ? 1 : 0);
                        hash = hash * 31 + cell.FloorSurface.MaterialColor;
                    }

                    if (cell.HasRiserSurface)
                    {
                        hash = hash * 31 + cell.RiserSurface.Tile;
                        hash = hash * 31 + cell.RiserSurface.MaterialColor;
                    }

                    if (cell.HasBaseFloorSurface)
                    {
                        hash = hash * 31 + cell.BaseFloorSurface.BaseTile;
                        hash = hash * 31 + cell.BaseFloorSurface.MaterialColor;
                    }
                }

                return hash;
            }
        }

        private static int ComputeTerrainChunkLightingSignature(TerrainChunkBuildContext chunk)
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + chunk.ChunkX;
                hash = hash * 31 + chunk.ChunkZ;
                for (int i = 0; i < chunk.Cells.Count; i++)
                {
                    TerrainChunkSourceCell cell = chunk.Cells[i];
                    hash = hash * 31 + cell.LocalX;
                    hash = hash * 31 + cell.LocalZ;
                    hash = hash * 31 + (cell.HasBlockSurface
                        ? cell.BlockTopSurface.Light.PackedLight
                        : cell.HasFloorSurface
                            ? cell.FloorSurface.Light.PackedLight
                            : 0);
                    hash = hash * 31 + (cell.HasBaseFloorSurface ? cell.BaseFloorSurface.Light.PackedLight : 0);
                }

                return hash;
            }
        }

        private static void RebuildTerrainChunkMesh(Mesh mesh, TerrainChunkBuildContext chunk)
        {
            if (mesh == null)
            {
                return;
            }

            List<FpsTerrainChunkCell> cells = new List<FpsTerrainChunkCell>(chunk.Cells.Count);
            for (int i = 0; i < chunk.Cells.Count; i++)
            {
                TerrainChunkSourceCell cell = chunk.Cells[i];
                cells.Add(new FpsTerrainChunkCell(
                    cell.LocalX,
                    cell.LocalZ,
                    cell.Height,
                    ResolveTerrainChunkTint(cell),
                    cell.HasNorthNeighbor,
                    cell.NorthNeighborHeight,
                    cell.HasEastNeighbor,
                    cell.EastNeighborHeight,
                    cell.HasSouthNeighbor,
                    cell.SouthNeighborHeight,
                    cell.HasWestNeighbor,
                    cell.WestNeighborHeight,
                    cell.AllowRisers && cell.HasRiserSurface,
                    cell.HasRamp,
                    cell.RampDir,
                    cell.RampStepCount,
                    cell.RampBaseHeight,
                    cell.HasBridgePillar,
                    cell.BridgeBaseHeight,
                    cell.HasUndersideDeck,
                    cell.UndersideDeckBaseHeight,
                    cell.HasBaseFloorSurface,
                    cell.BaseFloorHeight));
            }

            FpsTerrainChunkMeshData data = FpsTerrainChunkMeshBuilder.Build(
                chunk.ChunkX * TerrainChunkSize,
                chunk.ChunkZ * TerrainChunkSize,
                TerrainChunkSize,
                cells);

            mesh.Clear();
            mesh.vertices = data.Vertices;
            mesh.uv = data.Uvs;
            mesh.triangles = data.Triangles;
            mesh.colors32 = data.Colors;
            if (data.Vertices.Length > 0)
            {
                Vector3[] normals = new Vector3[data.Vertices.Length];
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = Vector3.up;
                }

                mesh.normals = normals;
            }

            mesh.RecalculateBounds();
        }

        private Texture2D BuildTerrainChunkTexture(TerrainChunkBuildContext chunk)
        {
            int textureWidth = TerrainChunkSize * TerrainTileTextureSize * TerrainTextureSlotCount;
            int textureHeight = TerrainChunkSize * TerrainTileTextureSize;
            Color32[] pixels = new Color32[textureWidth * textureHeight];

            for (int i = 0; i < chunk.Cells.Count; i++)
            {
                TerrainChunkSourceCell cell = chunk.Cells[i];
                if (!TryResolveTerrainChunkSource(cell, out Texture2D sourceTexture))
                {
                    continue;
                }

                BlitTerrainChunkTile(
                    pixels,
                    textureWidth,
                    textureHeight,
                    cell.LocalX * TerrainTileTextureSize * TerrainTextureSlotCount,
                    cell.LocalZ * TerrainTileTextureSize,
                    sourceTexture,
                    ApplySurfaceStyle(Color.white, FpsVisualSurfaceKind.TerrainTop, 0, false));

                if (TryResolveTerrainChunkRiserSource(cell, out Texture2D riserTexture))
                {
                    BlitTerrainChunkTile(
                        pixels,
                        textureWidth,
                        textureHeight,
                        cell.LocalX * TerrainTileTextureSize * TerrainTextureSlotCount + TerrainTileTextureSize,
                        cell.LocalZ * TerrainTileTextureSize,
                        riserTexture,
                        ApplySurfaceStyle(Color.white, FpsVisualSurfaceKind.TerrainSide, 0, false));
                }

                if (TryResolveTerrainChunkBaseFloorSource(cell, out Texture2D baseFloorTexture))
                {
                    BlitTerrainChunkTile(
                        pixels,
                        textureWidth,
                        textureHeight,
                        cell.LocalX * TerrainTileTextureSize * TerrainTextureSlotCount + TerrainTileTextureSize * 2,
                        cell.LocalZ * TerrainTileTextureSize,
                        baseFloorTexture,
                        ApplySurfaceStyle(Color.white, FpsVisualSurfaceKind.TerrainTop, 0, false));
                }
            }

            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"FpsGpuTerrainChunk_{chunk.ChunkX}_{chunk.ChunkZ}"
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private bool TryResolveTerrainChunkSource(TerrainChunkSourceCell cell, out Texture2D texture)
        {
            texture = null;

            if (cell.HasBlockSurface)
            {
                if (_spriteTextureCache.TryGetBlockFaceTexture(cell.BlockTopSurface, FpsAtlasSampler.BlockFaceKind.Top, false, out Texture blockTexture)
                    && blockTexture is Texture2D blockTexture2D)
                {
                    texture = blockTexture2D;
                    return true;
                }

                return false;
            }

            if (!cell.HasFloorSurface)
            {
                return false;
            }

            if (_floorAtlasBaker.TryGetCompositeTexture(cell.FloorSurface, out Texture floorTexture)
                && floorTexture is Texture2D floorTexture2D)
            {
                texture = floorTexture2D;
                return true;
            }

            return false;
        }

        private bool TryResolveTerrainChunkRiserSource(TerrainChunkSourceCell cell, out Texture2D texture)
        {
            texture = null;
            if (!cell.HasRiserSurface)
            {
                return false;
            }

            if (_spriteTextureCache.TryGetBlockFaceTexture(cell.RiserSurface, true, out Texture riserTexture)
                && riserTexture is Texture2D riserTexture2D)
            {
                texture = riserTexture2D;
                return true;
            }

            if (cell.HasFloorSurface
                && _floorAtlasBaker.TryGetCompositeTexture(cell.FloorSurface, out Texture floorTexture)
                && floorTexture is Texture2D floorTexture2D)
            {
                texture = floorTexture2D;
                return true;
            }

            return false;
        }

        private bool TryResolveTerrainChunkBaseFloorSource(TerrainChunkSourceCell cell, out Texture2D texture)
        {
            texture = null;
            if (!cell.HasBaseFloorSurface)
            {
                return false;
            }

            if (_floorAtlasBaker.TryGetCompositeTexture(cell.BaseFloorSurface, out Texture floorTexture)
                && floorTexture is Texture2D floorTexture2D)
            {
                texture = floorTexture2D;
                return true;
            }

            return false;
        }

        private static void BlitTerrainChunkTile(Color32[] destination, int destinationWidth, int destinationHeight, int startX, int startY, Texture2D sourceTexture, Color tint)
        {
            Color32[] source = sourceTexture.GetPixels32();
            int sourceWidth = sourceTexture.width;
            int sourceHeight = sourceTexture.height;

            for (int y = 0; y < TerrainTileTextureSize; y++)
            {
                int destinationY = startY + y;
                if (destinationY < 0 || destinationY >= destinationHeight)
                {
                    continue;
                }

                int sampleY = Mathf.Clamp(Mathf.FloorToInt((y / (float)TerrainTileTextureSize) * sourceHeight), 0, sourceHeight - 1);
                for (int x = 0; x < TerrainTileTextureSize; x++)
                {
                    int destinationX = startX + x;
                    if (destinationX < 0 || destinationX >= destinationWidth)
                    {
                        continue;
                    }

                    int sampleX = Mathf.Clamp(Mathf.FloorToInt((x / (float)TerrainTileTextureSize) * sourceWidth), 0, sourceWidth - 1);
                    Color32 color = source[sampleY * sourceWidth + sampleX];
                    color.a = 255;
                    destination[destinationY * destinationWidth + destinationX] = MultiplyColor(color, tint);
                }
            }
        }

        private static bool TryGetTerrainNeighborHeight(int cellX, int cellZ, out float height)
        {
            height = 0f;
            if (EClass._map == null || cellX < 0 || cellZ < 0 || cellX >= EClass._map.Size || cellZ >= EClass._map.Size)
            {
                return false;
            }

            Cell neighbor = EClass._map.cells[cellX, cellZ];
            if (neighbor == null)
            {
                return false;
            }

            height = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor);
            return true;
        }

        private static float GetCellBaseHeight(Cell cell)
        {
            if (cell == null)
            {
                return 0f;
            }

            // Base height is the structural support level of the cell.
            // Do not include floor/platform thickness here, or elevated decks and ramps lose their underside.
            return cell.height * FpsIdealizedWorld.GetTerrainHeightScale();
        }

        private static Color32 ResolveTerrainChunkTint(TerrainChunkSourceCell cell)
        {
            if (cell.HasBlockSurface)
            {
                return (Color32)ApplySurfaceStyle(
                    FpsLightApplicator.ApplySample(new Color32(255, 255, 255, 255), cell.BlockTopSurface.Light),
                    FpsVisualSurfaceKind.TerrainTop,
                    0,
                    false);
            }

            if (cell.HasFloorSurface)
            {
                return (Color32)ResolveTerrainChunkFloorTint(cell.FloorSurface);
            }

            return new Color32(255, 255, 255, 255);
        }

        private static Color ResolveTerrainChunkFloorTint(FpsResolvedFloorSurface surface)
        {
            Color32 color = new Color32(255, 255, 255, 255);
            return ApplySurfaceStyle(FpsLightApplicator.ApplySample(color, surface.Light), FpsVisualSurfaceKind.TerrainTop, 0, false);
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

            if (hasWallTop)
            {
                return Mathf.Max(maxSurface + 0.4f, maxWallTop - 0.02f);
            }

            return maxSurface + Mathf.Max(0.4f, Plugin.Settings?.IndoorCeilingHeight?.Value ?? 1.1f);
        }

        private void UpdateHybridRoofPreview(GpuViewPose pose, float terrainMaxDistance)
        {
            _idealizedWorld.GatherRoofStructures(pose.PlayerOrigin, terrainMaxDistance + 4f, _roofStructures);

            int activeIndex = 0;
            for (int structureIndex = 0; structureIndex < _roofStructures.Count; structureIndex++)
            {
                FpsResolvedRoofStructure structure = _roofStructures[structureIndex];
                if (structure.Plan?.Quads == null)
                {
                    continue;
                }

                for (int quadIndex = 0; quadIndex < structure.Plan.Quads.Count; quadIndex++)
                {
                    FpsResolvedRoofPlane plane = ConvertStructureQuadToPlane(structure, structure.Plan.Quads[quadIndex]);
                    Vector3 center = plane.Face.Center;
                    if (!IsRoofPlaneVisible(plane, pose, terrainMaxDistance + 4f))
                    {
                        continue;
                    }

                    EnsureRoofPool(activeIndex + 1);
                    GameObject quad = _roofQuads[activeIndex];
                    MeshRenderer renderer = _roofRenderers[activeIndex];
                    MeshFilter filter = _roofFilters[activeIndex];
                    RoofVisualState visualState = _roofVisualStates[activeIndex];
                    quad.SetActive(true);
                    Material rendererMaterial = ResolveWallRendererMaterial(plane.DoubleSided);
                    if (renderer.sharedMaterial != rendererMaterial)
                    {
                        renderer.sharedMaterial = rendererMaterial;
                    }

                    if (!visualState.MatchesGeometry(plane))
                    {
                        ApplyWallQuadGeometry(quad, filter, plane.Face, plane.DoubleSided);
                        visualState.CaptureGeometry(plane);
                    }

                    if (!visualState.MatchesUv(plane))
                    {
                        ApplyRoofTextureUv(filter.sharedMesh, plane);
                        visualState.CaptureUv(plane);
                    }

                    bool solidRoofDebug = UseSolidFaceDebug();
                    if (!TryApplyRoofTexture(plane, _propertyBlock, out bool usesBakedTint, out bool useFallbackGray))
                    {
                        quad.SetActive(false);
                        continue;
                    }

                    string fogCategory = plane.Kind == FpsRoofPlaneKind.InteriorCeiling ? "roof-interior" : "terrain-roof";
                    Color baseColor = solidRoofDebug
                        ? ResolveRoofDebugColor(plane.Kind)
                        : useFallbackGray
                            ? ResolveRoofFallbackColor(plane.Kind)
                        : usesBakedTint || plane.TextureKind == FpsRoofTextureKind.PrimarySource
                            ? Color.white
                            : ResolveSpriteTint(plane.MaterialColor, plane.HasMaterialTint, plane.BlockSurface.Light);
                    _propertyBlock.SetColor("_Color", ApplyAtmosphericFog(baseColor, center, terrainMaxDistance + 4f, 0.12f, fogCategory));
                    renderer.SetPropertyBlock(_propertyBlock);
                    _roofVisualStates[activeIndex] = visualState;
                    activeIndex++;
                }
            }

            for (int i = activeIndex; i < _roofQuads.Count; i++)
            {
                _roofQuads[i].SetActive(false);
            }
        }

        private bool IsRoofPlaneVisible(FpsResolvedRoofPlane plane, GpuViewPose pose, float maxDistance)
        {
            FpsGpuFaceQuad face = plane.Face;
            Vector3 cameraPosition = _camera != null ? _camera.transform.position : Vector3.zero;
            if (IsPointUnderRoofFace(face, cameraPosition, 0.2f))
            {
                return true;
            }

            if (plane.Kind == FpsRoofPlaneKind.InteriorCeiling)
            {
                if (IsVisibleToCamera(face.Center, maxDistance, -1f))
                {
                    return true;
                }

                return IsAnyRoofVertexVisible(face, pose, maxDistance, 0.5f, 1.1f);
            }

            if (IsTerrainVisibleToCamera(face.Center, pose, maxDistance, 0.5f, 0.6f))
            {
                return true;
            }

            return IsAnyRoofVertexVisible(face, pose, maxDistance, 0.75f, 1.2f);
        }

        private bool IsAnyRoofVertexVisible(FpsGpuFaceQuad face, GpuViewPose pose, float maxDistance, float distancePadding, float viewportPadding)
        {
            return IsTerrainVisibleToCamera(face.BottomLeft, pose, maxDistance, distancePadding, viewportPadding)
                || IsTerrainVisibleToCamera(face.BottomRight, pose, maxDistance, distancePadding, viewportPadding)
                || IsTerrainVisibleToCamera(face.TopLeft, pose, maxDistance, distancePadding, viewportPadding)
                || IsTerrainVisibleToCamera(face.TopRight, pose, maxDistance, distancePadding, viewportPadding);
        }

        private static bool IsPointUnderRoofFace(FpsGpuFaceQuad face, Vector3 point, float verticalPadding)
        {
            float minX = Mathf.Min(Mathf.Min(face.BottomLeft.x, face.BottomRight.x), Mathf.Min(face.TopLeft.x, face.TopRight.x));
            float maxX = Mathf.Max(Mathf.Max(face.BottomLeft.x, face.BottomRight.x), Mathf.Max(face.TopLeft.x, face.TopRight.x));
            float minZ = Mathf.Min(Mathf.Min(face.BottomLeft.z, face.BottomRight.z), Mathf.Min(face.TopLeft.z, face.TopRight.z));
            float maxZ = Mathf.Max(Mathf.Max(face.BottomLeft.z, face.BottomRight.z), Mathf.Max(face.TopLeft.z, face.TopRight.z));
            float minY = Mathf.Min(Mathf.Min(face.BottomLeft.y, face.BottomRight.y), Mathf.Min(face.TopLeft.y, face.TopRight.y));
            return point.x >= minX
                && point.x <= maxX
                && point.z >= minZ
                && point.z <= maxZ
                && point.y <= minY + verticalPadding;
        }

        private int AddWallQuads(int activeWallIndex, int cellX, int cellZ, FpsResolvedWallSurface surface)
        {
            if (surface.Cell == null)
            {
                return activeWallIndex;
            }

            float bottom = FpsIdealizedWorld.GetCellSurfaceHeight(surface.Cell);
            float top = ResolveWallTopHeight(surface.Cell, bottom);

            if (surface.Cell.HasWallOrFence && !surface.Cell.HasFullBlock)
            {
                return AddWallFencePanels(activeWallIndex, cellX, cellZ, bottom, top, surface);
            }

            return AddFullBlockCornerQuads(activeWallIndex, cellX, cellZ, bottom, top, surface);
        }

        private int AddFullBlockCornerQuads(int activeWallIndex, int cellX, int cellZ, float bottom, float top, FpsResolvedWallSurface surface)
        {
            float segmentBottom = bottom;
            while (segmentBottom < top - 0.01f)
            {
                float segmentTop = Mathf.Min(segmentBottom + 1f, top);
                for (int dir = 0; dir < 4; dir++)
                {
                    if (!ShouldRenderBlockFace(surface.Cell, dir, segmentTop))
                    {
                        continue;
                    }

                    activeWallIndex = AddBlockFaceQuad(
                        activeWallIndex,
                        cellX,
                        cellZ,
                        segmentBottom,
                        segmentTop,
                        dir,
                        surface);
                }

                segmentBottom = segmentTop;
            }

            return activeWallIndex;
        }

        private void CollectFullBlockWallSeeds(int cellX, int cellZ, FpsResolvedWallSurface surface)
        {
            if (surface.Cell == null)
            {
                return;
            }

            float bottom = FpsIdealizedWorld.GetCellSurfaceHeight(surface.Cell);
            float top = ResolveWallTopHeight(surface.Cell, bottom);
            if (surface.Cell.hasDoor)
            {
                bottom = ResolveDoorOpeningTop(bottom, top, ResolvePreferredDoorHeightWorld(surface.Cell));
                if (bottom >= top - 0.01f)
                {
                    return;
                }
            }

            int surfaceKey = ComputeFullBlockSurfaceKey(surface);
            _fullBlockWallSurfaceLookup[surfaceKey] = surface;

            float segmentBottom = bottom;
            while (segmentBottom < top - 0.01f)
            {
                float segmentTop = Mathf.Min(segmentBottom + 1f, top);
                for (int dir = 0; dir < 4; dir++)
                {
                    if (!ShouldRenderBlockFace(surface.Cell, dir, segmentTop))
                    {
                        continue;
                    }

                    _fullBlockWallSeeds.Add(new FpsWallRunSeed(cellX, cellZ, dir, segmentBottom, segmentTop, surfaceKey));
                }

                segmentBottom = segmentTop;
            }
        }

        private int RenderHybridFullBlockRuns(int activeWallIndex)
        {
            for (int i = 0; i < _fullBlockWallRuns.Count; i++)
            {
                FpsWallRun run = _fullBlockWallRuns[i];
                if (!_fullBlockWallSurfaceLookup.TryGetValue(run.SurfaceKey, out FpsResolvedWallSurface surface))
                {
                    continue;
                }

                FpsWallStructureSpec spec = run.Axis == FpsWallStructureAxis.AlongX
                    ? FpsWallStructureSpec.CreateAlongX(run.MinAlong, run.MaxAlong, run.Constant, run.BottomY, run.TopY, 0.24f)
                    : FpsWallStructureSpec.CreateAlongZ(run.Constant, run.MinAlong, run.MaxAlong, run.BottomY, run.TopY, 0.24f);
                FpsWallStructurePlan plan = FpsWallStructurePlanner.Build(spec);
                for (int quadIndex = 0; quadIndex < plan.Quads.Count; quadIndex++)
                {
                    activeWallIndex = RenderHybridFullBlockQuad(activeWallIndex, run, plan.Quads[quadIndex], surface);
                }
            }

            return activeWallIndex;
        }

        private int RenderHybridFullBlockQuad(int activeWallIndex, FpsWallRun run, FpsWallStructureQuad quadDef, FpsResolvedWallSurface surface)
        {
            EnsureWallPool(activeWallIndex + 1);

            GameObject quad = _wallQuads[activeWallIndex];
            MeshRenderer renderer = _wallRenderers[activeWallIndex];
            MeshFilter filter = _wallFilters[activeWallIndex];
            quad.SetActive(true);
            renderer.sharedMaterial = _spriteMaterial;

            FpsGpuFaceQuad face = new FpsGpuFaceQuad(
                ToVector3(quadDef.BottomLeft),
                ToVector3(quadDef.BottomRight),
                ToVector3(quadDef.TopLeft),
                ToVector3(quadDef.TopRight));
            ApplyWallQuadGeometry(quad, filter, face, false);
            ApplyWallStructureUv(filter.sharedMesh, quadDef);

            _propertyBlock.Clear();
            if (quadDef.Kind == FpsWallStructureQuadKind.TopCap)
            {
                bool textureResolved = _spriteTextureCache.TryGetHybridBlockTexture(surface, run.Dir == 1 || run.Dir == 3, FpsAutoSurfaceMaterialKind.Top, out Texture topTexture);
                _propertyBlock.SetTexture("_MainTex", textureResolved ? topTexture : Texture2D.whiteTexture);
                _propertyBlock.SetColor("_Color", textureResolved
                    ? Color.white
                    : new Color32(255, 0, 255, 255));
            }
            else
            {
                FpsAutoSurfaceMaterialKind materialKind = quadDef.Kind == FpsWallStructureQuadKind.EndCap
                    ? FpsAutoSurfaceMaterialKind.Side
                    : FpsAutoSurfaceMaterialKind.Front;
                bool textureResolved = _spriteTextureCache.TryGetHybridBlockTexture(surface, run.Dir == 1 || run.Dir == 3, materialKind, out Texture wallTexture);
                Texture texture = UseSolidFaceDebug()
                    ? Texture2D.whiteTexture
                    : textureResolved
                        ? wallTexture
                        : Texture2D.whiteTexture;
                _propertyBlock.SetTexture("_MainTex", texture);
                _propertyBlock.SetColor("_Color", UseSolidFaceDebug()
                    ? ResolveDebugFaceColor(run.Dir)
                    : textureResolved
                        ? Color.white
                        : new Color32(255, 0, 255, 255));
            }

            renderer.SetPropertyBlock(_propertyBlock);
            return activeWallIndex + 1;
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
            float segmentBottom = bottom;
            while (segmentBottom < top - 0.01f)
            {
                float segmentTop = Mathf.Min(segmentBottom + 1f, top);
                float segmentHeight = segmentTop - segmentBottom;
                if (wallDir == 0 || wallDir == 2)
                {
                    activeWallIndex = AddWallPanelQuad(activeWallIndex, cellX, cellZ, segmentBottom, segmentHeight, 0, renderData, baseTile, false, surface);
                }

                if (wallDir == 1 || wallDir == 2)
                {
                    activeWallIndex = AddWallPanelQuad(activeWallIndex, cellX, cellZ, segmentBottom, segmentHeight, 1, renderData, baseTile, true, surface);
                }

                segmentBottom = segmentTop;
            }

            return activeWallIndex;
        }

        private void CollectPanelWallSeeds(int cellX, int cellZ, FpsResolvedWallSurface surface)
        {
            RenderData renderData = surface.Cell?.sourceBlock?.renderData;
            int[] tiles = surface.Cell?.sourceBlock?._tiles;
            if (renderData == null || tiles == null || tiles.Length == 0)
            {
                return;
            }

            float bottom = FpsIdealizedWorld.GetCellSurfaceHeight(surface.Cell);
            float top = ResolveWallTopHeight(surface.Cell, bottom);
            bool hasDoor = surface.Cell?.hasDoor == true;
            float openingTop = bottom;
            float mountedHorizontalScale = Plugin.Settings?.WallMountedHorizontalScale?.Value ?? 0.72f;
            if (surface.Cell?.hasDoor == true)
            {
                openingTop = ResolveDoorOpeningTop(bottom, top, ResolvePreferredDoorHeightWorld(surface.Cell));
                if (openingTop <= bottom + 0.01f)
                {
                    return;
                }

                bottom = openingTop;
                if (bottom >= top - 0.01f)
                {
                    bottom = top;
                }
            }

            int baseTile = Mathf.Abs(tiles[0]);
            int wallDir = surface.Cell.blockDir;

            if (wallDir == 0 || wallDir == 2)
            {
                int surfaceKey = ComputePanelWallSurfaceKey(renderData, baseTile, flipX: false, surface.MaterialColor, surface.Light);
                _panelWallSurfaceLookup[surfaceKey] = new HybridPanelWallSurface(renderData, baseTile, false, surface);
                if (hasDoor)
                {
                    AddDoorFrameRuns(_panelDoorFrameRuns, FpsWallStructureAxis.AlongX, 0, cellZ, cellX, FpsIdealizedWorld.GetCellSurfaceHeight(surface.Cell), openingTop, surfaceKey, mountedHorizontalScale);
                }
                _panelWallSeeds.Add(new FpsWallRunSeed(cellX, cellZ, 0, bottom, top, surfaceKey));
            }

            if (wallDir == 1 || wallDir == 2)
            {
                int surfaceKey = ComputePanelWallSurfaceKey(renderData, baseTile, flipX: true, surface.MaterialColor, surface.Light);
                _panelWallSurfaceLookup[surfaceKey] = new HybridPanelWallSurface(renderData, baseTile, true, surface);
                if (hasDoor)
                {
                    AddDoorFrameRuns(_panelDoorFrameRuns, FpsWallStructureAxis.AlongZ, 1, cellX + 1f, cellZ, FpsIdealizedWorld.GetCellSurfaceHeight(surface.Cell), openingTop, surfaceKey, mountedHorizontalScale);
                }
                _panelWallSeeds.Add(new FpsWallRunSeed(cellX, cellZ, 1, bottom, top, surfaceKey));
            }
        }

        private int RenderHybridPanelRuns(int activeWallIndex)
        {
            for (int i = 0; i < _panelWallRuns.Count; i++)
            {
                FpsWallRun run = _panelWallRuns[i];
                if (!_panelWallSurfaceLookup.TryGetValue(run.SurfaceKey, out HybridPanelWallSurface panelSurface))
                {
                    continue;
                }

                FpsWallStructureSpec spec = run.Axis == FpsWallStructureAxis.AlongX
                    ? FpsWallStructureSpec.CreateAlongX(run.MinAlong, run.MaxAlong, run.Constant, run.BottomY, run.TopY, 0.14f)
                    : FpsWallStructureSpec.CreateAlongZ(run.Constant, run.MinAlong, run.MaxAlong, run.BottomY, run.TopY, 0.14f);
                FpsWallStructurePlan plan = FpsWallStructurePlanner.Build(spec);
                for (int quadIndex = 0; quadIndex < plan.Quads.Count; quadIndex++)
                {
                    activeWallIndex = RenderHybridPanelQuad(activeWallIndex, run, plan.Quads[quadIndex], panelSurface);
                }
            }

            for (int i = 0; i < _panelDoorFrameRuns.Count; i++)
            {
                FpsWallRun run = _panelDoorFrameRuns[i];
                if (!_panelWallSurfaceLookup.TryGetValue(run.SurfaceKey, out HybridPanelWallSurface panelSurface))
                {
                    continue;
                }

                FpsWallStructureSpec spec = run.Axis == FpsWallStructureAxis.AlongX
                    ? FpsWallStructureSpec.CreateAlongX(run.MinAlong, run.MaxAlong, run.Constant, run.BottomY, run.TopY, 0.14f)
                    : FpsWallStructureSpec.CreateAlongZ(run.Constant, run.MinAlong, run.MaxAlong, run.BottomY, run.TopY, 0.14f);
                FpsWallStructurePlan plan = FpsWallStructurePlanner.Build(spec);
                for (int quadIndex = 0; quadIndex < plan.Quads.Count; quadIndex++)
                {
                    activeWallIndex = RenderHybridPanelQuad(activeWallIndex, run, plan.Quads[quadIndex], panelSurface);
                }
            }

            return activeWallIndex;
        }

        private int RenderHybridPanelQuad(int activeWallIndex, FpsWallRun run, FpsWallStructureQuad quadDef, HybridPanelWallSurface panelSurface)
        {
            EnsureWallPool(activeWallIndex + 1);

            GameObject quad = _wallQuads[activeWallIndex];
            MeshRenderer renderer = _wallRenderers[activeWallIndex];
            MeshFilter filter = _wallFilters[activeWallIndex];
            quad.SetActive(true);
            renderer.sharedMaterial = _spriteMaterial;

            FpsGpuFaceQuad face = new FpsGpuFaceQuad(
                ToVector3(quadDef.BottomLeft),
                ToVector3(quadDef.BottomRight),
                ToVector3(quadDef.TopLeft),
                ToVector3(quadDef.TopRight));
            ApplyWallQuadGeometry(quad, filter, face, false);
            ApplyWallStructureUv(filter.sharedMesh, quadDef);

            _propertyBlock.Clear();
            if (quadDef.Kind == FpsWallStructureQuadKind.TopCap)
            {
                bool topResolved = _spriteTextureCache.TryGetHybridWallTexture(
                    panelSurface.RenderData,
                    panelSurface.Tile,
                    panelSurface.FlipX,
                    panelSurface.Surface.MaterialColor,
                    true,
                    false,
                    panelSurface.Surface.Light,
                    FpsAutoSurfaceMaterialKind.Top,
                    out Texture topTexture);
                _propertyBlock.SetTexture("_MainTex", topResolved ? topTexture : Texture2D.whiteTexture);
                _propertyBlock.SetColor("_Color", topResolved
                    ? Color.white
                    : new Color32(255, 0, 255, 255));
            }
            else
            {
                FpsAutoSurfaceMaterialKind materialKind = quadDef.Kind == FpsWallStructureQuadKind.EndCap
                    ? FpsAutoSurfaceMaterialKind.Side
                    : FpsAutoSurfaceMaterialKind.Front;
                bool textureResolved = _spriteTextureCache.TryGetHybridWallTexture(
                    panelSurface.RenderData,
                    panelSurface.Tile,
                    panelSurface.FlipX,
                    panelSurface.Surface.MaterialColor,
                    true,
                    false,
                    panelSurface.Surface.Light,
                    materialKind,
                    out Texture panelTexture);
                _propertyBlock.SetTexture("_MainTex", textureResolved ? panelTexture : Texture2D.whiteTexture);
                _propertyBlock.SetColor("_Color", textureResolved
                    ? Color.white
                    : UseSolidFaceDebug()
                        ? ResolveDebugFaceColor(run.Dir)
                        : new Color32(255, 0, 255, 255));
            }

            renderer.SetPropertyBlock(_propertyBlock);
            return activeWallIndex + 1;
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
            renderer.sharedMaterial = _spriteMaterial;
            FpsGpuFaceQuad face = FpsGpuBlockGeometryBuilder.BuildSideQuad(cellX, cellZ, bottom, top, dir);
            ApplyWallQuadGeometry(quad, filter, face, true);

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

        private int AddWallPanelQuad(int activeWallIndex, int cellX, int cellZ, float bottom, float heightWorld, int dir, RenderData renderData, int tile, bool flipX, FpsResolvedWallSurface surface)
        {
            EnsureWallPool(activeWallIndex + 1);

            GameObject quad = _wallQuads[activeWallIndex];
            MeshRenderer renderer = _wallRenderers[activeWallIndex];
            MeshFilter filter = _wallFilters[activeWallIndex];
            quad.SetActive(true);
            renderer.sharedMaterial = _spriteMaterial;

            bool textureResolved = _spriteTextureCache.TryGetWallMountedTexture(
                renderData,
                tile,
                flipX,
                true,
                surface.MaterialColor,
                true,
                false,
                surface.Light,
                out Texture panelTexture);
            Texture texture = UseSolidFaceDebug()
                ? Texture2D.whiteTexture
                : textureResolved
                ? panelTexture
                : Texture2D.whiteTexture;
            FpsGpuFaceQuad face = BuildWallPanelFace(cellX, cellZ, dir, bottom, 1f, heightWorld);
            ApplyWallQuadGeometry(quad, filter, face, true);
            _diagnostics.WallPanels++;
            if (!textureResolved)
            {
                _diagnostics.WallPanelFallbacks++;
            }

            RecordWallDiagnostic("panel", cellX, cellZ, dir, textureResolved, surface, face, renderer, true);
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

        private static float ResolveWallTopHeight(Cell cell, float bottom)
        {
            if (cell?.sourceBlock?.tileType?.RepeatBlock != true)
            {
                return bottom + 1f;
            }

            Room room = ResolveRepeatBlockRoom(cell);
            if (room?.lot == null)
            {
                return bottom + 1f;
            }

            return bottom + Mathf.Max(0.05f, room.lot.realHeight);
        }

        private static float ResolveDoorOpeningTop(float bottom, float top, float preferredDoorHeightWorld)
        {
            return FpsDoorVisualLayout.ResolveOpeningTop(bottom, top, preferredDoorHeightWorld);
        }

        private static void AddDoorFrameRuns(
            List<FpsWallRun> output,
            FpsWallStructureAxis axis,
            int dir,
            float constant,
            float cellAlongStart,
            float bottomY,
            float openingTop,
            int surfaceKey,
            float horizontalScale)
        {
            if (output == null || openingTop <= bottomY + 0.01f)
            {
                return;
            }

            FpsDoorJambSpans spans = FpsDoorVisualLayout.ResolveJambSpans(cellAlongStart, horizontalScale);
            if (spans.LeftEnd > spans.LeftStart + 0.01f)
            {
                output.Add(new FpsWallRun(axis, dir, constant, spans.LeftStart, spans.LeftEnd, bottomY, openingTop, surfaceKey));
            }

            if (spans.RightEnd > spans.RightStart + 0.01f)
            {
                output.Add(new FpsWallRun(axis, dir, constant, spans.RightStart, spans.RightEnd, bottomY, openingTop, surfaceKey));
            }
        }

        private static float ResolvePreferredDoorHeightWorld(Cell cell)
        {
            if (cell?.sourceObj?.tileType?.IsDoor == true)
            {
                return ResolveWallMountedNativeWorldSize(cell.sourceObj.renderData, null).y;
            }

            return 0f;
        }

        private static Room ResolveRepeatBlockRoom(Cell cell)
        {
            if (cell == null)
            {
                return null;
            }

            return cell.room
                ?? cell.Front.room
                ?? cell.Right.room
                ?? cell.FrontRight.room;
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

        private bool TryApplyRoofTexture(FpsResolvedRoofPlane plane, MaterialPropertyBlock block, out bool usesBakedTint, out bool useFallbackGray)
        {
            usesBakedTint = false;
            useFallbackGray = false;
            Texture texture = null;
            FpsRoofTextureProjectionMode roofTextureMode = Plugin.Settings?.RoofTextureProjectionMode?.Value ?? FpsRoofTextureProjectionMode.SolidGray;

            if (UseSolidFaceDebug())
            {
                block.Clear();
                block.SetTexture("_MainTex", Texture2D.whiteTexture);
                return true;
            }

            if (roofTextureMode == FpsRoofTextureProjectionMode.SolidGray)
            {
                block.Clear();
                block.SetTexture("_MainTex", Texture2D.whiteTexture);
                useFallbackGray = true;
                return true;
            }

            switch (plane.TextureKind)
            {
                case FpsRoofTextureKind.BlockFace:
                    usesBakedTint = _spriteTextureCache.TryGetBlockFaceTexture(
                        plane.BlockSurface,
                        plane.BlockFaceKind,
                        plane.FlipX,
                        out texture);
                    break;
                case FpsRoofTextureKind.PrimarySource:
                    if (plane.RenderData != null)
                    {
                        usesBakedTint = _spriteTextureCache.TryGetRoofRenderTileTexture(plane, roofTextureMode, out texture);
                    }
                    break;
            }

            if (texture == null)
            {
                block.Clear();
                block.SetTexture("_MainTex", Texture2D.whiteTexture);
                useFallbackGray = true;
                return true;
            }

            block.Clear();
            block.SetTexture("_MainTex", texture);
            return true;
        }

        private static Color ResolveRoofFallbackColor(FpsRoofPlaneKind kind)
        {
            switch (kind)
            {
                case FpsRoofPlaneKind.Top:
                    return new Color32(156, 156, 156, 255);
                case FpsRoofPlaneKind.SlopeLeft:
                    return new Color32(148, 148, 148, 255);
                case FpsRoofPlaneKind.SlopeRight:
                    return new Color32(140, 140, 140, 255);
                case FpsRoofPlaneKind.Edge:
                    return new Color32(122, 122, 122, 255);
                case FpsRoofPlaneKind.InteriorCeiling:
                    return new Color32(132, 132, 132, 255);
                default:
                    return new Color32(144, 144, 144, 255);
            }
        }

        private static Color ResolveRoofDebugColor(FpsRoofPlaneKind kind)
        {
            switch (kind)
            {
                case FpsRoofPlaneKind.Top:
                    return new Color32(214, 164, 72, 255);
                case FpsRoofPlaneKind.SlopeLeft:
                    return new Color32(196, 92, 92, 255);
                case FpsRoofPlaneKind.SlopeRight:
                    return new Color32(92, 150, 214, 255);
                case FpsRoofPlaneKind.Edge:
                    return new Color32(118, 86, 58, 255);
                case FpsRoofPlaneKind.InteriorCeiling:
                    return new Color32(170, 170, 190, 255);
                default:
                    return new Color32(220, 80, 220, 255);
            }
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

        private Color ApplyAtmosphericFog(Color baseColor, Vector3 worldPosition, float maxDistance, float startRatio, string category)
        {
            if (Plugin.Settings.EnableDistanceFog.Value != true || _camera == null || maxDistance <= 0.01f)
            {
                return ApplySceneTone(baseColor);
            }

            Vector3 cameraSpace = _camera.transform.InverseTransformPoint(worldPosition);
            float depth = cameraSpace.z;
            if (depth <= 0f)
            {
                return baseColor;
            }

            float radialDistance = Vector3.Distance(_camera.transform.position, worldPosition);
            bool terrainLike = category.StartsWith("terrain", StringComparison.Ordinal);
            bool spriteLike = category.StartsWith("ground", StringComparison.Ordinal) || category.StartsWith("upright", StringComparison.Ordinal);
            float configuredStartRatio = Plugin.Settings?.DistanceFogStartRatio?.Value ?? 0.3f;
            float configuredEndRatio = Plugin.Settings?.DistanceFogEndRatio?.Value ?? 0.58f;
            float configuredDensity = Plugin.Settings?.DistanceFogDensity?.Value ?? 1.8f;
            float configuredClearBlend = Plugin.Settings?.DistanceFogClearBlend?.Value ?? 0.7f;
            float effectiveStartRatio = terrainLike
                ? Mathf.Max(startRatio, configuredStartRatio * 0.92f)
                : spriteLike
                    ? Mathf.Max(startRatio, configuredStartRatio * 0.88f)
                    : Mathf.Max(startRatio, configuredStartRatio);
            float effectiveEndRatio = terrainLike
                ? Mathf.Clamp(configuredEndRatio * 0.94f, effectiveStartRatio + 0.05f, 1f)
                : spriteLike
                    ? Mathf.Clamp(configuredEndRatio, effectiveStartRatio + 0.05f, 1f)
                    : Mathf.Clamp(configuredEndRatio * 0.97f, effectiveStartRatio + 0.05f, 1f);
            float fogStart = Mathf.Max(0.1f, maxDistance * effectiveStartRatio);
            float fogEnd = Mathf.Max(fogStart + 0.1f, maxDistance * effectiveEndRatio);
            if (radialDistance <= fogStart)
            {
                return baseColor;
            }

            float fogFactor = FpsDistanceFog.ComputeFogFactor(radialDistance, maxDistance, effectiveStartRatio, effectiveEndRatio, configuredDensity);
            Color desaturated = Desaturate(baseColor, fogFactor * 0.7f);
            Color fogColor = ResolveFogColor();
            fogColor = FpsVisualStyle.GradeFogColor(fogColor, category, (Plugin.Settings?.FogColorGradeStrength?.Value ?? 0.18f) * (0.55f + (fogFactor * 0.45f)));
            fogColor = FpsDistanceFog.BlendTowardClearColor(fogColor, ResolveClearColor(), fogFactor, configuredClearBlend);
            fogColor.a = desaturated.a;
            Color result = Color.Lerp(desaturated, fogColor, fogFactor);
            result = ApplySceneTone(result);
            MaybeRecordFogDiagnostic(category, radialDistance, fogStart, fogEnd, fogFactor, baseColor, result);
            return result;
        }

        private float ResolveSceneTimeRatio()
        {
            if (EMono.scene != null)
            {
                return EMono.scene.timeRatio;
            }

            return 0f;
        }

        private Color ResolveClearColor()
        {
            SceneProfile profile = EMono.scene?.profile;
            SceneColorProfile color = profile?.color;
            if (color == null)
            {
                return DefaultClearColor;
            }

            float timeRatio = ResolveSceneTimeRatio();
            Color sky = color.sky.Evaluate(timeRatio);
            Color skyBg = color.skyBG.Evaluate(timeRatio);
            Color clear = Color.Lerp(skyBg, sky, 0.35f);
            return ApplySceneTone(clear, includeNightBrightness: false);
        }

        private void MaybeRecordFogDiagnostic(string category, float depth, float fogStart, float fogEnd, float fogFactor, Color baseColor, Color result)
        {
            if (!IsGpuDiagnosticsEnabled() || _diagnosticFramesRemaining <= 0 || _fogDiagnosticSamples.Count >= 12)
            {
                return;
            }

            _fogDiagnosticSamples.Add(
                $"GPU fog sample[{_fogDiagnosticSamples.Count}]: kind={category} depth={depth:F3} start={fogStart:F3} end={fogEnd:F3} factor={fogFactor:F3} base=({baseColor.r:F3},{baseColor.g:F3},{baseColor.b:F3}) result=({result.r:F3},{result.g:F3},{result.b:F3})");
        }

        private Color ResolveFogColor()
        {
            SceneProfile profile = EMono.scene?.profile;
            SceneColorProfile color = profile?.color;
            if (color == null)
            {
                Color background = _camera != null ? _camera.backgroundColor : DefaultClearColor;
                Color haze = Color.Lerp(background, Color.white, 0.38f);
                return Color.Lerp(haze, new Color(0.76f, 0.82f, 0.88f, 1f), 0.24f);
            }

            float timeRatio = ResolveSceneTimeRatio();
            Color fog = color.fog.Evaluate(timeRatio);
            Color skyBg = color.skyBG.Evaluate(timeRatio);
            Color hazeColor = Color.Lerp(fog, skyBg, 0.5f);
            return ApplySceneTone(hazeColor, includeNightBrightness: false);
        }

        private Color ApplySceneTone(Color color)
        {
            return ApplySceneTone(color, includeNightBrightness: true);
        }

        private Color ApplySceneTone(Color color, bool includeNightBrightness)
        {
            Color result = color;

            if (EMono.scene?.camSupport?.beautify != null)
            {
                Color tint = EMono.scene.camSupport.beautify.tintColor;
                if (tint.a > 0.001f)
                {
                    Color tinted = new Color(result.r * tint.r, result.g * tint.g, result.b * tint.b, result.a);
                    result = Color.Lerp(result, tinted, Mathf.Clamp01(tint.a * 0.35f));
                }
            }

            SceneProfile profile = EMono.scene?.profile;
            SceneColorProfile colorProfile = profile?.color;
            SceneLightProfile lightProfile = profile?.light;
            if (colorProfile != null && lightProfile != null)
            {
                float timeRatio = ResolveSceneTimeRatio();
                float nightRate = lightProfile.nightRatioCurve.Evaluate(timeRatio);
                Color sky = colorProfile.sky.Evaluate(timeRatio);
                Color fog = colorProfile.fog.Evaluate(timeRatio);
                Color ambientTone = Color.Lerp(sky, fog, 0.45f);
                Color modulated = new Color(result.r * ambientTone.r, result.g * ambientTone.g, result.b * ambientTone.b, result.a);
                result = Color.Lerp(result, modulated, Mathf.Clamp01(nightRate * 0.18f));
            }

            if (includeNightBrightness && EMono.scene?.camSupport?.grading != null)
            {
                float nightBrightness = EMono.scene.camSupport.grading.nightBrightness;
                if (!Mathf.Approximately(nightBrightness, 0f))
                {
                    result.r = Mathf.Clamp01(result.r + nightBrightness);
                    result.g = Mathf.Clamp01(result.g + nightBrightness);
                    result.b = Mathf.Clamp01(result.b + nightBrightness);
                }
            }

            return result;
        }

        private static Color Desaturate(Color color, float amount)
        {
            float gray = color.grayscale;
            return Color.Lerp(color, new Color(gray, gray, gray, color.a), Mathf.Clamp01(amount));
        }

        private static Color ApplySurfaceStyle(Color baseColor, FpsVisualSurfaceKind kind, int dir, bool indoor)
        {
            return FpsVisualStyle.ApplySurfaceShading(
                baseColor,
                kind,
                dir,
                indoor,
                Plugin.Settings?.VisualContrastStrength?.Value ?? 0.12f,
                Plugin.Settings?.IndoorShadowStrength?.Value ?? 0.16f);
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
                renderer.sharedMaterial = _spriteMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                _roofQuads.Add(quad);
                _roofRenderers.Add(renderer);
                _roofFilters.Add(filter);
                _roofVisualStates.Add(default);
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

        private static void ApplyRoofTextureUv(Mesh mesh, FpsResolvedRoofPlane plane)
        {
            if (mesh == null)
            {
                return;
            }

            if (plane.TextureKind == FpsRoofTextureKind.PrimarySource
                && plane.ProjectionKind == FpsRoofTextureProjectionKind.TriSlice)
            {
                mesh.uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f)
                };
                return;
            }

            SetQuadUv(mesh, new Rect(0f, 0f, 1f, 1f));
        }

        private static FpsResolvedRoofPlane ConvertStructureQuadToPlane(FpsResolvedRoofStructure structure, FpsRoofStructureQuad quad)
        {
            FpsRoofPlaneKind planeKind = ResolveHybridRoofPlaneKind(quad.Kind);
            FpsRoofTextureProjectionKind projectionKind =
                quad.Kind == FpsRoofStructureQuadKind.TopSurface || quad.Kind == FpsRoofStructureQuadKind.RidgeCap
                    ? FpsRoofTextureProjectionKind.RawTile
                    : FpsRoofTextureProjectionKind.RectSlice;

            FpsResolvedRoofPlane plane = new FpsResolvedRoofPlane
            {
                Lot = structure.Lot,
                Kind = planeKind,
                Face = new FpsGpuFaceQuad(
                    ToVector3(quad.BottomLeft),
                    ToVector3(quad.BottomRight),
                    ToVector3(quad.TopLeft),
                    ToVector3(quad.TopRight)),
                ProjectionKind = projectionKind,
                DoubleSided = quad.Kind == FpsRoofStructureQuadKind.BottomSurface,
                UsePanelPlacement = false,
                DiagnosticLabel = $"{structure.DiagnosticLabel}:{quad.Kind}"
            };

            if (structure.HasPrimarySource)
            {
                plane.TextureKind = FpsRoofTextureKind.PrimarySource;
                plane.SourceOrigin = structure.PrimarySource.Origin;
                plane.RenderData = structure.PrimarySource.RenderData;
                plane.Tile = structure.PrimarySource.Tile;
                plane.MaterialColor = structure.PrimarySource.MaterialColor == 0 ? 104025 : structure.PrimarySource.MaterialColor;
                plane.HasMaterialTint = true;
                plane.UseSelectiveMaterialTint = false;
                plane.FlipX = false;
                plane.TrimTransparent = false;
                plane.BlockSurface = new FpsResolvedWallSurface
                {
                    Cell = null,
                    Tile = 0,
                    RenderData = null,
                    MaterialColor = plane.MaterialColor,
                    UseSnowAtlas = false,
                    Light = structure.Light
                };
                plane.BlockFaceKind = FpsAtlasSampler.BlockFaceKind.Top;
                plane.HasLotPrimarySource = true;
                plane.LotPrimarySource = structure.PrimarySource;
                return plane;
            }

            plane.TextureKind = FpsRoofTextureKind.BlockFace;
            plane.SourceOrigin = FpsRoofSourceOrigin.None;
            plane.RenderData = structure.FallbackSurface.RenderData;
            plane.Tile = structure.FallbackSurface.Tile;
            plane.MaterialColor = structure.FallbackSurface.MaterialColor == 0 ? 104025 : structure.FallbackSurface.MaterialColor;
            plane.HasMaterialTint = true;
            plane.UseSelectiveMaterialTint = false;
            plane.FlipX = false;
            plane.TrimTransparent = false;
            plane.BlockSurface = structure.FallbackSurface;
            plane.BlockFaceKind = FpsAtlasSampler.BlockFaceKind.Top;
            plane.HasLotPrimarySource = false;
            plane.LotPrimarySource = default;
            return plane;
        }

        private static FpsRoofPlaneKind ResolveHybridRoofPlaneKind(FpsRoofStructureQuadKind kind)
        {
            switch (kind)
            {
                case FpsRoofStructureQuadKind.BottomSurface:
                    return FpsRoofPlaneKind.InteriorCeiling;
                case FpsRoofStructureQuadKind.EdgeBand:
                case FpsRoofStructureQuadKind.GableFace:
                    return FpsRoofPlaneKind.Edge;
                case FpsRoofStructureQuadKind.SlopeSurface:
                    return FpsRoofPlaneKind.SlopeLeft;
                default:
                    return FpsRoofPlaneKind.Top;
            }
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
            bool indoor = IsCurrentIndoorCell(surface.Cell);
            _propertyBlock.SetColor("_Color", UseSolidFaceDebug()
                ? ResolveDebugEdgeColor(edge)
                : textureResolved
                ? ApplySurfaceStyle(Color.white, FpsVisualSurfaceKind.TerrainSide, EdgeToDir(edge), indoor)
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

        private struct RoofVisualState
        {
            public bool HasGeometry;
            public bool HasUv;
            public FpsGpuFaceQuad Face;
            public bool DoubleSided;
            public FpsRoofTextureKind TextureKind;
            public FpsRoofTextureProjectionKind ProjectionKind;

            public bool MatchesGeometry(FpsResolvedRoofPlane plane)
            {
                return HasGeometry
                    && DoubleSided == plane.DoubleSided
                    && Face.BottomLeft == plane.Face.BottomLeft
                    && Face.BottomRight == plane.Face.BottomRight
                    && Face.TopLeft == plane.Face.TopLeft
                    && Face.TopRight == plane.Face.TopRight;
            }

            public void CaptureGeometry(FpsResolvedRoofPlane plane)
            {
                HasGeometry = true;
                Face = plane.Face;
                DoubleSided = plane.DoubleSided;
            }

            public bool MatchesUv(FpsResolvedRoofPlane plane)
            {
                return HasUv
                    && TextureKind == plane.TextureKind
                    && ProjectionKind == plane.ProjectionKind;
            }

            public void CaptureUv(FpsResolvedRoofPlane plane)
            {
                HasUv = true;
                TextureKind = plane.TextureKind;
                ProjectionKind = plane.ProjectionKind;
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
                bool allowRisers,
                bool hasNorthNeighbor,
                float northNeighborHeight,
                bool hasEastNeighbor,
                float eastNeighborHeight,
                bool hasSouthNeighbor,
                float southNeighborHeight,
                bool hasWestNeighbor,
                float westNeighborHeight,
                bool hasRiserSurface,
                FpsResolvedWallSurface riserSurface,
                bool hasBaseFloorSurface,
                FpsResolvedFloorSurface baseFloorSurface,
                float baseFloorHeight,
                bool hasRamp,
                int rampDir,
                int rampStepCount,
                float rampBaseHeight,
                bool hasBridgePillar,
                float bridgeBaseHeight,
                bool hasUndersideDeck,
                float undersideDeckBaseHeight)
            {
                LocalX = localX;
                LocalZ = localZ;
                Height = height;
                HasFloorSurface = hasFloorSurface;
                FloorSurface = floorSurface;
                HasBlockSurface = hasBlockSurface;
                BlockTopSurface = blockTopSurface;
                AllowRisers = allowRisers;
                HasNorthNeighbor = hasNorthNeighbor;
                NorthNeighborHeight = northNeighborHeight;
                HasEastNeighbor = hasEastNeighbor;
                EastNeighborHeight = eastNeighborHeight;
                HasSouthNeighbor = hasSouthNeighbor;
                SouthNeighborHeight = southNeighborHeight;
                HasWestNeighbor = hasWestNeighbor;
                WestNeighborHeight = westNeighborHeight;
                HasRiserSurface = hasRiserSurface;
                RiserSurface = riserSurface;
                HasBaseFloorSurface = hasBaseFloorSurface;
                BaseFloorSurface = baseFloorSurface;
                BaseFloorHeight = baseFloorHeight;
                HasRamp = hasRamp;
                RampDir = rampDir;
                RampStepCount = rampStepCount;
                RampBaseHeight = rampBaseHeight;
                HasBridgePillar = hasBridgePillar;
                BridgeBaseHeight = bridgeBaseHeight;
                HasUndersideDeck = hasUndersideDeck;
                UndersideDeckBaseHeight = undersideDeckBaseHeight;
            }

            public int LocalX { get; }

            public int LocalZ { get; }

            public float Height { get; }

            public bool HasFloorSurface { get; }

            public FpsResolvedFloorSurface FloorSurface { get; }

            public bool HasBlockSurface { get; }

            public FpsResolvedWallSurface BlockTopSurface { get; }

            public bool AllowRisers { get; }

            public bool HasNorthNeighbor { get; }

            public float NorthNeighborHeight { get; }

            public bool HasEastNeighbor { get; }

            public float EastNeighborHeight { get; }

            public bool HasSouthNeighbor { get; }

            public float SouthNeighborHeight { get; }

            public bool HasWestNeighbor { get; }

            public float WestNeighborHeight { get; }

            public bool HasRiserSurface { get; }

            public FpsResolvedWallSurface RiserSurface { get; }

            public bool HasBaseFloorSurface { get; }

            public FpsResolvedFloorSurface BaseFloorSurface { get; }

            public float BaseFloorHeight { get; }

            public bool HasRamp { get; }

            public int RampDir { get; }

            public int RampStepCount { get; }

            public float RampBaseHeight { get; }

            public bool HasBridgePillar { get; }

            public float BridgeBaseHeight { get; }

            public bool HasUndersideDeck { get; }

            public float UndersideDeckBaseHeight { get; }
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
