using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Elin_Elinikki
{
    [System.Flags]
    internal enum SharedWorldVisibility
    {
        NormalView = 1,
        FpsView = 2,
        Both = NormalView | FpsView
    }

    internal enum SharedWorldPrimitiveKind
    {
        Cube,
        Sphere,
        Cylinder,
        Quad
    }

    internal enum SharedWorldNormalProxyMode
    {
        AutoBake,
        ExplicitTexture,
        Hidden
    }

    internal enum SharedWorldNormalProxyHeightMode
    {
        MaxSemanticAndTextureAspect,
        SemanticOnly,
        TextureAspectOnly
    }

    internal enum SharedWorldNormalProxyFacingMode
    {
        QuantizedElin,
        DefinitionEuler,
        FrontFacing
    }

    internal sealed class SharedWorldNormalProxyDefinition
    {
        public SharedWorldNormalProxyMode Mode = SharedWorldNormalProxyMode.AutoBake;
        public SharedWorldNormalProxyHeightMode HeightMode = SharedWorldNormalProxyHeightMode.MaxSemanticAndTextureAspect;
        public SharedWorldNormalProxyFacingMode FacingMode = SharedWorldNormalProxyFacingMode.QuantizedElin;
        public Vector2 Footprint = Vector2.zero;
        public float HeightUnits;
        public Vector2 DisplayFootprint = Vector2.zero;
        public float DisplayHeightUnits;
        public float TextureAspectHeightScale = 1f;
        public float TextureAspectOverride;
        public Vector2 ScreenOffset = Vector2.zero;
        public Vector2 Pivot = new Vector2(0.5f, 0f);
        public float SortPivotY;
        public int SliceCount = 1;
        public float SliceStepHeight;
        public float SliceStepZ;
        public Texture2D TextureOverride;
    }

    internal sealed class SharedWorldPrimitiveDefinition
    {
        public string Id;
        public SharedWorldPrimitiveKind Primitive;
        public SharedWorldVisibility Visibility = SharedWorldVisibility.Both;
        public Vector3 TilePosition;
        public Vector3 Scale = Vector3.one;
        public Vector3 EulerAngles = Vector3.zero;
        public Color Color = Color.white;
        public bool AttachToSurface = true;
        public string CustomGeometryKey;
        public Action<GameObject> BuildCustomGeometry;
        public SharedWorldNormalProxyDefinition NormalProxy = new SharedWorldNormalProxyDefinition();
    }

    internal sealed class SharedWorldObjectManager : MonoBehaviour
    {
        private const string DemoPrefix = "demo/";
        private static readonly Dictionary<string, Texture2D> ImpostorTextureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, SharedWorldGeometryMetrics> GeometryMetricsCache = new Dictionary<string, SharedWorldGeometryMetrics>();
        private static SharedWorldObjectManager _instance;

        private readonly Dictionary<string, SharedWorldPrimitiveDefinition> _definitions = new Dictionary<string, SharedWorldPrimitiveDefinition>();
        private readonly Dictionary<string, SharedWorldObjectHandle> _handles = new Dictionary<string, SharedWorldObjectHandle>();
        private readonly List<string> _scratchIds = new List<string>();
        private GameObject _normalRoot;
        private GameObject _previewRoot;
        private Map _demoMap;

        internal static SharedWorldObjectManager Instance => _instance;

        public static void EnsureCreated()
        {
            if (_instance != null)
            {
                return;
            }

            SharedWorldObjectManager existing = FindObjectOfType<SharedWorldObjectManager>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            GameObject managerObject = new GameObject("ElinikkiSharedWorldManager");
            DontDestroyOnLoad(managerObject);
            managerObject.hideFlags = HideFlags.HideAndDontSave;
            _instance = managerObject.AddComponent<SharedWorldObjectManager>();
        }

        public static void Upsert(SharedWorldPrimitiveDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id))
            {
                return;
            }

            EnsureCreated();
            _instance._definitions[definition.Id] = definition;
        }

        public static void Remove(string id)
        {
            if (_instance == null || string.IsNullOrEmpty(id))
            {
                return;
            }

            _instance._definitions.Remove(id);
            _instance.DestroyHandle(id);
        }

        public static void Clear()
        {
            if (_instance == null)
            {
                return;
            }

            _instance._definitions.Clear();
            _instance.ClearHandles();
        }

        /// <summary>
        /// Removes every definition whose id starts with
        /// <paramref name="prefix"/>. Public entry point for external
        /// subsystems (notably <c>ElinikkiZonePlacementManager</c>)
        /// that own a subset of the definition table and need to wipe
        /// it atomically on state changes such as zone transitions.
        /// A null or empty prefix is treated as a no-op to prevent
        /// accidental blanket wipes of the whole table — call
        /// <see cref="Clear"/> explicitly if that is what you want.
        /// </summary>
        public static void RemoveDefinitionsByPrefix(string prefix)
        {
            if (_instance == null || string.IsNullOrEmpty(prefix))
            {
                return;
            }

            _instance.RemoveDefinitionsByPrefixInstance(prefix);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Update()
        {
            if (!CanSyncScene())
            {
                SetHandleVisibility(false);
                return;
            }

            EnsureRoots();
            EnsureDreamTestSet();
            RefreshHandles();
        }

        internal int DefinitionCountForTests => _definitions.Count;

        internal int HandleCountForTests => _handles.Count;

        internal GameObject NormalRootForTests => _normalRoot;

        internal GameObject PreviewRootForTests => _previewRoot;

        internal bool TryGetDefinitionCenterForTests(out Vector3 centerTile)
        {
            centerTile = Vector3.zero;
            if (_definitions.Count == 0)
            {
                return false;
            }

            int count = 0;
            foreach (KeyValuePair<string, SharedWorldPrimitiveDefinition> pair in _definitions)
            {
                centerTile += pair.Value.TilePosition;
                count++;
            }

            if (count <= 0)
            {
                return false;
            }

            centerTile /= count;
            return true;
        }

        internal string BuildDebugMetricsJsonForTests(Camera normalCamera, Camera previewCamera)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append('[');
            bool first = true;
            foreach (KeyValuePair<string, SharedWorldPrimitiveDefinition> pair in _definitions)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                AppendDebugMetricObject(sb, pair.Value, normalCamera, previewCamera);
            }

            sb.Append(']');
            return sb.ToString();
        }

        internal void ForceUpdateForTests()
        {
            Update();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            ClearHandles();

            if (_normalRoot != null)
            {
                Destroy(_normalRoot);
                _normalRoot = null;
            }

            if (_previewRoot != null)
            {
                Destroy(_previewRoot);
                _previewRoot = null;
            }
        }

        private void EnsureDreamTestSet()
        {
            if (Plugin.Settings?.EnableDreamTestSet?.Value != true)
            {
                RemoveDefinitionsByPrefixInstance(DemoPrefix);
                _demoMap = null;
                return;
            }

            if (ReferenceEquals(_demoMap, EClass._map))
            {
                return;
            }

            _demoMap = EClass._map;
            RemoveDefinitionsByPrefixInstance(DemoPrefix);

            int size = EClass._map.Size;
            float centerX = Mathf.Clamp(EClass.pc.pos.x + 1.35f, 1.5f, size - 2.5f);
            float centerZ = Mathf.Clamp(EClass.pc.pos.z + 1.1f, 1.5f, size - 2.5f);

            UpsertInternal(new SharedWorldPrimitiveDefinition
            {
                Id = DemoPrefix + "monolith",
                Primitive = SharedWorldPrimitiveKind.Cube,
                TilePosition = new Vector3(
                    Mathf.Clamp(EClass.pc.pos.x - 1f, 1.0f, size - 1.0f),
                    0f,
                    Mathf.Clamp(EClass.pc.pos.z + 1f, 1.0f, size - 1.0f)),
                Scale = new Vector3(0.45f, 1.8f, 0.45f),
                EulerAngles = new Vector3(0f, 18f, 0f),
                Color = new Color32(18, 18, 24, 255),
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    Mode = SharedWorldNormalProxyMode.ExplicitTexture,
                    HeightMode = SharedWorldNormalProxyHeightMode.MaxSemanticAndTextureAspect,
                    FacingMode = SharedWorldNormalProxyFacingMode.QuantizedElin,
                    Footprint = new Vector2(0.5f, 0.5f),
                    HeightUnits = 1.8f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f,
                    TextureOverride = GetOrCreateDebugMonolithProxyTexture()
                }
            });
            UpsertInternal(new SharedWorldPrimitiveDefinition
            {
                Id = DemoPrefix + "moon",
                Primitive = SharedWorldPrimitiveKind.Sphere,
                TilePosition = new Vector3(centerX + 1.05f, 3.2f, centerZ + 0.45f),
                Scale = new Vector3(1.35f, 1.35f, 1.35f),
                Color = new Color32(208, 88, 96, 255),
                AttachToSurface = false,
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    HeightMode = SharedWorldNormalProxyHeightMode.MaxSemanticAndTextureAspect,
                    FacingMode = SharedWorldNormalProxyFacingMode.DefinitionEuler,
                    Footprint = new Vector2(1.35f, 1.35f),
                    HeightUnits = 1.35f,
                    DisplayFootprint = new Vector2(1.35f, 1.35f),
                    DisplayHeightUnits = 1.35f,
                    TextureAspectOverride = 1f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f
                }
            });
            UpsertInternal(new SharedWorldPrimitiveDefinition
            {
                Id = DemoPrefix + "screen",
                Primitive = SharedWorldPrimitiveKind.Quad,
                TilePosition = new Vector3(centerX - 0.55f, 1.45f, centerZ + 0.2f),
                Scale = new Vector3(2.4f, 2.8f, 1f),
                EulerAngles = new Vector3(0f, 28f, 0f),
                Color = new Color32(214, 238, 241, 255),
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    Mode = SharedWorldNormalProxyMode.AutoBake,
                    HeightMode = SharedWorldNormalProxyHeightMode.MaxSemanticAndTextureAspect,
                    FacingMode = SharedWorldNormalProxyFacingMode.QuantizedElin,
                    Footprint = new Vector2(2.4f, 0.2f),
                    HeightUnits = 2.8f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f
                }
            });
            UpsertInternal(new SharedWorldPrimitiveDefinition
            {
                Id = DemoPrefix + "altar",
                Primitive = SharedWorldPrimitiveKind.Cylinder,
                TilePosition = new Vector3(centerX + 0.05f, 0.35f, centerZ + 1.05f),
                Scale = new Vector3(1.25f, 0.42f, 1.25f),
                Color = new Color32(168, 128, 94, 255),
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    HeightMode = SharedWorldNormalProxyHeightMode.SemanticOnly,
                    FacingMode = SharedWorldNormalProxyFacingMode.DefinitionEuler,
                    Footprint = new Vector2(1.25f, 1.25f),
                    HeightUnits = 0.42f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f
                }
            });
            UpsertInternal(new SharedWorldPrimitiveDefinition
            {
                Id = DemoPrefix + "statue",
                Primitive = SharedWorldPrimitiveKind.Cube,
                CustomGeometryKey = "statue_v1",
                BuildCustomGeometry = BuildDemoStatueGeometry,
                TilePosition = new Vector3(centerX + 1.25f, 0f, centerZ + 0.85f),
                Scale = Vector3.one,
                EulerAngles = new Vector3(0f, -16f, 0f),
                Color = new Color32(140, 180, 210, 255),
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    HeightMode = SharedWorldNormalProxyHeightMode.MaxSemanticAndTextureAspect,
                    FacingMode = SharedWorldNormalProxyFacingMode.DefinitionEuler,
                    Footprint = new Vector2(0.82f, 0.72f),
                    HeightUnits = 1.62f,
                    DisplayFootprint = new Vector2(0.9f, 0.8f),
                    DisplayHeightUnits = 1.9f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0.08f
                }
            });
            UpsertInternal(new SharedWorldPrimitiveDefinition
            {
                Id = DemoPrefix + "arch",
                Primitive = SharedWorldPrimitiveKind.Cube,
                CustomGeometryKey = "arch_v1",
                BuildCustomGeometry = BuildDemoArchGeometry,
                TilePosition = new Vector3(centerX - 1.15f, 0f, centerZ + 0.85f),
                Scale = Vector3.one,
                EulerAngles = new Vector3(0f, 14f, 0f),
                Color = new Color32(196, 210, 170, 255),
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    HeightMode = SharedWorldNormalProxyHeightMode.SemanticOnly,
                    FacingMode = SharedWorldNormalProxyFacingMode.QuantizedElin,
                    Footprint = new Vector2(1.86f, 0.34f),
                    HeightUnits = 1.92f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0.1f
                }
            });
        }

        private void RefreshHandles()
        {
            _scratchIds.Clear();
            foreach (KeyValuePair<string, SharedWorldObjectHandle> pair in _handles)
            {
                if (!_definitions.ContainsKey(pair.Key))
                {
                    _scratchIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < _scratchIds.Count; i++)
            {
                DestroyHandle(_scratchIds[i]);
            }

            foreach (KeyValuePair<string, SharedWorldPrimitiveDefinition> pair in _definitions)
            {
                SharedWorldObjectHandle handle = EnsureHandle(pair.Value);
                ApplyDefinition(pair.Value, handle);
            }
        }

        private SharedWorldObjectHandle EnsureHandle(SharedWorldPrimitiveDefinition definition)
        {
            SharedWorldObjectHandle handle;
            if (!_handles.TryGetValue(definition.Id, out handle))
            {
                handle = new SharedWorldObjectHandle();
                _handles.Add(definition.Id, handle);
            }

            string geometryKey = ResolveGeometryKey(definition);
            if (handle.Primitive != definition.Primitive || handle.GeometryKey != geometryKey)
            {
                DestroyHandleObjects(handle);
                handle.Primitive = definition.Primitive;
                handle.GeometryKey = geometryKey;
            }

            if (ShouldRenderInNormalView(definition) && handle.NormalObject == null)
            {
                handle.NormalObject = CreateNormalProxyRoot(definition);
            }

            if (ShouldRenderInNormalView(definition))
            {
                EnsureNormalProxySlices(handle, definition);
            }

            if ((definition.Visibility & SharedWorldVisibility.FpsView) != 0 && handle.PreviewObject == null)
            {
                handle.PreviewObject = CreatePreviewObject(definition, handle);
            }

            if (!ShouldRenderInNormalView(definition) && handle.NormalObject != null)
            {
                DestroyNormalProxySlices(handle);
                DestroyUnityObject(handle.NormalObject);
                handle.NormalObject = null;
            }

            if ((definition.Visibility & SharedWorldVisibility.FpsView) == 0 && handle.PreviewObject != null)
            {
                DestroyPreviewObject(handle);
                handle.PreviewObject = null;
                handle.PreviewRenderer = null;
                handle.PreviewMaterial = null;
            }

            return handle;
        }

        private static string ResolveGeometryKey(SharedWorldPrimitiveDefinition definition)
        {
            return string.IsNullOrEmpty(definition.CustomGeometryKey)
                ? "primitive:" + definition.Primitive
                : "custom:" + definition.CustomGeometryKey;
        }

        private static bool TryGetGeometryMetrics(SharedWorldPrimitiveDefinition definition, out SharedWorldGeometryMetrics metrics)
        {
            metrics = default;
            string key = ResolveGeometryKey(definition);
            if (GeometryMetricsCache.TryGetValue(key, out metrics))
            {
                return metrics.IsValid;
            }

            metrics = BuildGeometryMetrics(definition);
            GeometryMetricsCache[key] = metrics;
            return metrics.IsValid;
        }

        private static SharedWorldGeometryMetrics BuildGeometryMetrics(SharedWorldPrimitiveDefinition definition)
        {
            GameObject root = null;
            try
            {
                root = new GameObject("ElinikkiGeometryMetrics_" + definition.Id);
                root.hideFlags = HideFlags.HideAndDontSave;

                if (definition.BuildCustomGeometry != null)
                {
                    GameObject geometryRoot = new GameObject("Geometry");
                    geometryRoot.hideFlags = HideFlags.HideAndDontSave;
                    geometryRoot.transform.SetParent(root.transform, false);
                    definition.BuildCustomGeometry(geometryRoot);
                }
                else
                {
                    GameObject primitive = GameObject.CreatePrimitive(ToNormalProxyPrimitiveType(definition.Primitive));
                    primitive.hideFlags = HideFlags.HideAndDontSave;
                    primitive.transform.SetParent(root.transform, false);
                    primitive.transform.localPosition = Vector3.zero;
                    primitive.transform.localRotation = ResolveNormalProxyPrimitiveRotation(
                        definition,
                        new SharedWorldNormalProxyDefinition
                        {
                            FacingMode = SharedWorldNormalProxyFacingMode.DefinitionEuler
                        });
                    primitive.transform.localScale = ResolveNormalProxyPrimitiveScale(definition, new SharedWorldNormalProxyDefinition());
                    Component collider = primitive.GetComponent("Collider");
                    if (collider != null)
                    {
                        UnityEngine.Object.DestroyImmediate(collider);
                    }
                }

                Bounds localBounds;
                if (!TryCalculateLocalBounds(root.transform, out localBounds))
                {
                    return default;
                }

                return new SharedWorldGeometryMetrics
                {
                    BaseFootprint = CalculateBaseFootprint(localBounds, root.transform),
                    Size = localBounds.size,
                    Center = localBounds.center
                };
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static bool TryCalculateLocalBounds(Transform root, out Bounds localBounds)
        {
            localBounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;
                for (int ix = -1; ix <= 1; ix += 2)
                {
                    for (int iy = -1; iy <= 1; iy += 2)
                    {
                        for (int iz = -1; iz <= 1; iz += 2)
                        {
                            Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(ix, iy, iz));
                            Vector3 localCorner = root.InverseTransformPoint(worldCorner);
                            if (!hasBounds)
                            {
                                localBounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        private static Vector2 CalculateBaseFootprint(Bounds localBounds, Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            float thresholdY = localBounds.min.y + Mathf.Max(0.08f, localBounds.size.y * 0.28f);
            bool hasFootprint = false;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;
                for (int ix = -1; ix <= 1; ix += 2)
                {
                    for (int iy = -1; iy <= 1; iy += 2)
                    {
                        for (int iz = -1; iz <= 1; iz += 2)
                        {
                            Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(ix, iy, iz));
                            Vector3 localCorner = root.InverseTransformPoint(worldCorner);
                            if (localCorner.y > thresholdY)
                            {
                                continue;
                            }

                            Vector2 footprintPoint = new Vector2(localCorner.x, localCorner.z);
                            if (!hasFootprint)
                            {
                                min = footprintPoint;
                                max = footprintPoint;
                                hasFootprint = true;
                            }
                            else
                            {
                                min = Vector2.Min(min, footprintPoint);
                                max = Vector2.Max(max, footprintPoint);
                            }
                        }
                    }
                }
            }

            if (!hasFootprint)
            {
                return new Vector2(localBounds.size.x, localBounds.size.z);
            }

            return new Vector2(Mathf.Max(0.01f, max.x - min.x), Mathf.Max(0.01f, max.y - min.y));
        }

        private void ApplyDefinition(SharedWorldPrimitiveDefinition definition, SharedWorldObjectHandle handle)
        {
            if (handle.NormalObject != null)
            {
                handle.NormalObject.transform.SetParent(_normalRoot != null ? _normalRoot.transform : transform, false);
                ApplyNormalProxyTransform(definition, handle);
                RefreshNormalProxyTextures(handle, definition);
                ApplyNormalProxyMaterials(handle, definition.Color);
                handle.NormalObject.SetActive(true);
            }

            if (handle.PreviewObject != null)
            {
                Transform previewParent = FpsViewManager.SharedWorldPreviewRoot;
                if (_previewRoot != null && previewParent != null && _previewRoot.transform.parent != previewParent)
                {
                    _previewRoot.transform.SetParent(previewParent, false);
                }

                handle.PreviewObject.transform.SetParent(_previewRoot != null ? _previewRoot.transform : transform, false);
                ApplyPreviewTransform(definition, handle.PreviewObject.transform);
                for (int i = 0; i < handle.PreviewMaterials.Count; i++)
                {
                    ApplyMaterial(handle.PreviewMaterials[i], definition.Color);
                }
                handle.PreviewObject.SetActive(previewParent != null);
            }
        }

        private static void ApplyPreviewTransform(SharedWorldPrimitiveDefinition definition, Transform target)
        {
            float baseHeight = definition.AttachToSurface
                ? FpsIdealizedWorld.GetSurfaceHeightAt(new Vector2(definition.TilePosition.x, definition.TilePosition.z))
                : 0f;
            target.localPosition = new Vector3(
                definition.TilePosition.x,
                baseHeight + definition.TilePosition.y,
                definition.TilePosition.z);
            target.localRotation = Quaternion.Euler(definition.EulerAngles);
            target.localScale = definition.Scale;
        }

        private void EnsureRoots()
        {
            if (_normalRoot == null)
            {
                _normalRoot = new GameObject("ElinikkiSharedWorld_Normal");
                _normalRoot.hideFlags = HideFlags.HideAndDontSave;
            }

            if (EClass.scene != null && _normalRoot.transform.parent != EClass.scene.transform)
            {
                _normalRoot.transform.SetParent(EClass.scene.transform, false);
            }

            Transform previewParent = FpsViewManager.SharedWorldPreviewRoot;
            if (_previewRoot == null)
            {
                _previewRoot = new GameObject("ElinikkiSharedWorld_Preview");
                _previewRoot.hideFlags = HideFlags.HideAndDontSave;
                SetLayerRecursively(_previewRoot, FpsGpuPreviewRenderer.RenderLayer);
            }

            if (previewParent != null && _previewRoot.transform.parent != previewParent)
            {
                _previewRoot.transform.SetParent(previewParent, false);
            }
        }

        private void SetHandleVisibility(bool visible)
        {
            foreach (KeyValuePair<string, SharedWorldObjectHandle> pair in _handles)
            {
                if (pair.Value.NormalObject != null)
                {
                    pair.Value.NormalObject.SetActive(visible);
                }

                if (pair.Value.PreviewObject != null)
                {
                    pair.Value.PreviewObject.SetActive(visible && FpsViewManager.SharedWorldPreviewRoot != null);
                }
            }
        }

        private void RemoveDefinitionsByPrefixInstance(string prefix)
        {
            _scratchIds.Clear();
            foreach (KeyValuePair<string, SharedWorldPrimitiveDefinition> pair in _definitions)
            {
                if (pair.Key.StartsWith(prefix))
                {
                    _scratchIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < _scratchIds.Count; i++)
            {
                Remove(_scratchIds[i]);
            }
        }

        private void UpsertInternal(SharedWorldPrimitiveDefinition definition)
        {
            _definitions[definition.Id] = definition;
        }

        private static bool ShouldRenderInNormalView(SharedWorldPrimitiveDefinition definition)
        {
            if ((definition.Visibility & SharedWorldVisibility.NormalView) == 0)
            {
                return false;
            }

            return ResolveNormalProxy(definition).Mode != SharedWorldNormalProxyMode.Hidden;
        }

        private static SharedWorldNormalProxyDefinition ResolveNormalProxy(SharedWorldPrimitiveDefinition definition)
        {
            SharedWorldNormalProxyDefinition source = definition.NormalProxy ?? new SharedWorldNormalProxyDefinition();
            SharedWorldNormalProxyDefinition proxy = new SharedWorldNormalProxyDefinition
            {
                Mode = source.Mode,
                HeightMode = source.HeightMode,
                FacingMode = source.FacingMode,
                DisplayFootprint = source.DisplayFootprint,
                DisplayHeightUnits = source.DisplayHeightUnits,
                TextureAspectHeightScale = source.TextureAspectHeightScale,
                TextureAspectOverride = source.TextureAspectOverride,
                ScreenOffset = source.ScreenOffset,
                Pivot = source.Pivot,
                SortPivotY = source.SortPivotY,
                SliceCount = source.SliceCount,
                SliceStepHeight = source.SliceStepHeight,
                SliceStepZ = source.SliceStepZ,
                TextureOverride = source.TextureOverride
            };

            Vector2 defaultFootprint;
            float defaultHeightUnits;
            SharedWorldGeometryMetrics metrics;
            if (TryGetGeometryMetrics(definition, out metrics))
            {
                defaultFootprint = metrics.BaseFootprint == Vector2.zero
                    ? new Vector2(metrics.Size.x, metrics.Size.z)
                    : metrics.BaseFootprint;
                defaultHeightUnits = metrics.Size.y;
            }
            else
            {
                switch (definition.Primitive)
                {
                    case SharedWorldPrimitiveKind.Sphere:
                    {
                        float diameter = Mathf.Max(definition.Scale.x, definition.Scale.y, definition.Scale.z);
                        defaultFootprint = new Vector2(diameter, diameter);
                        defaultHeightUnits = diameter;
                        break;
                    }
                    case SharedWorldPrimitiveKind.Cylinder:
                        defaultFootprint = new Vector2(definition.Scale.x, definition.Scale.z);
                        defaultHeightUnits = definition.Scale.y;
                        break;
                    case SharedWorldPrimitiveKind.Quad:
                        defaultFootprint = new Vector2(definition.Scale.x, Mathf.Max(0.15f, definition.Scale.z));
                        defaultHeightUnits = definition.Scale.y;
                        break;
                    default:
                        defaultFootprint = new Vector2(definition.Scale.x, definition.Scale.z);
                        defaultHeightUnits = definition.Scale.y;
                        break;
                }
            }

            proxy.Footprint = source.Footprint == Vector2.zero ? defaultFootprint : source.Footprint;
            proxy.HeightUnits = source.HeightUnits <= 0f ? defaultHeightUnits : source.HeightUnits;
            proxy.DisplayFootprint = source.DisplayFootprint == Vector2.zero ? proxy.Footprint : source.DisplayFootprint;
            proxy.DisplayHeightUnits = source.DisplayHeightUnits <= 0f ? proxy.HeightUnits : source.DisplayHeightUnits;
            proxy.SliceCount = Mathf.Max(1, proxy.SliceCount);
            return proxy;
        }

        private Vector3 ResolveNormalOrigin(SharedWorldPrimitiveDefinition definition)
        {
            int mapSize = EClass._map.Size;
            int baseCellX = Mathf.Clamp(Mathf.FloorToInt(definition.TilePosition.x), 0, mapSize - 1);
            int baseCellZ = Mathf.Clamp(Mathf.FloorToInt(definition.TilePosition.z), 0, mapSize - 1);
            Vector3 basePosition = ResolveCellCenterPosition(baseCellX, baseCellZ);
            Vector3 eastStep = baseCellX + 1 < mapSize
                ? ResolveCellCenterPosition(baseCellX + 1, baseCellZ) - basePosition
                : ResolveFallbackEastStep();
            Vector3 southStep = baseCellZ + 1 < mapSize
                ? ResolveCellCenterPosition(baseCellX, baseCellZ + 1) - basePosition
                : ResolveFallbackSouthStep();
            float centerX = baseCellX + 0.5f;
            float centerZ = baseCellZ + 0.5f;
            float fracX = definition.TilePosition.x - centerX;
            float fracZ = definition.TilePosition.z - centerZ;

            return basePosition
                + eastStep * fracX
                + southStep * fracZ
                + ResolveNormalAltitudeStep() * definition.TilePosition.y;
        }

        private void ApplyNormalProxyTransform(SharedWorldPrimitiveDefinition definition, SharedWorldObjectHandle handle)
        {
            SharedWorldNormalProxyDefinition proxy = ResolveNormalProxy(definition);
            Vector3 basePosition = ResolveNormalBasePosition(definition);
            Vector3 origin = ResolveNormalOrigin(definition);
            Rect bounds = ResolveNormalProxyScreenBounds(definition, proxy);
            float width = Mathf.Max(0.01f, bounds.width);
            float height = Mathf.Max(0.01f, bounds.height);
            Vector2 pivot = proxy.Pivot;
            float pivotY = bounds.yMin + height * Mathf.Clamp01(pivot.y);
            int sliceCount = Mathf.Max(1, handle.NormalSliceObjects.Count);
            float sliceHeight = height / sliceCount;
            float sliceUnitHeight = proxy.SliceStepHeight > 0f ? proxy.SliceStepHeight : (proxy.HeightUnits / sliceCount);
            float sliceStepHeight = sliceUnitHeight * ResolveNormalObjectHeightStep().y;
            float sliceStepZ = ResolveDefaultSliceStepZ(sliceStepHeight);
            if (proxy.SliceStepZ != 0f)
            {
                sliceStepZ = proxy.SliceStepZ;
            }

            float sortFactor = EClass.setting.render.zSetting.mod1;

            handle.NormalObject.transform.localPosition = Vector3.zero;
            handle.NormalObject.transform.localRotation = Quaternion.identity;
            handle.NormalObject.transform.localScale = Vector3.one;

            for (int i = 0; i < handle.NormalSliceObjects.Count; i++)
            {
                Transform slice = handle.NormalSliceObjects[i].transform;
                SpriteRenderer spriteRenderer = i < handle.NormalSliceSpriteRenderers.Count
                    ? handle.NormalSliceSpriteRenderers[i]
                    : null;
                float segmentMinY = bounds.yMin + sliceHeight * i;
                float slicePivotY = ResolveSlicePivotY(proxy, i, sliceCount);
                float worldY = origin.y + segmentMinY + sliceHeight * slicePivotY - pivotY + i * (sliceStepHeight - sliceHeight);
                float sortLocalY = segmentMinY + sliceHeight * Mathf.Clamp01(proxy.SortPivotY);
                float sortYOffset = (origin.y + sortLocalY - pivotY) - basePosition.y;
                float extraSortZ = EClass.setting.render.thingZ + sortFactor * sortYOffset;
                float customSliceZ = i * sliceStepZ;
                float worldX = origin.x + bounds.xMin + proxy.ScreenOffset.x;

                slice.localPosition = new Vector3(
                    worldX,
                    worldY + proxy.ScreenOffset.y,
                    origin.z + extraSortZ + customSliceZ);
                slice.localRotation = Quaternion.identity;
                slice.localScale = ResolveNormalSpriteScale(spriteRenderer, width, sliceHeight);
            }
        }

        private static float ResolveSlicePivotY(SharedWorldNormalProxyDefinition proxy, int sliceIndex, int sliceCount)
        {
            float sliceStart = sliceIndex / (float)Mathf.Max(1, sliceCount);
            float sliceSize = 1f / Mathf.Max(1, sliceCount);
            float pivot = Mathf.Clamp01(proxy.Pivot.y);
            return Mathf.Clamp01((pivot - sliceStart) / Mathf.Max(0.0001f, sliceSize));
        }

        private static Vector3 ResolveNormalSpriteScale(SpriteRenderer spriteRenderer, float targetWidth, float targetHeight)
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
                float scaleX = targetWidth / Mathf.Max(0.0001f, spriteSize.x);
                float scaleY = targetHeight / Mathf.Max(0.0001f, spriteSize.y);
                return new Vector3(scaleX, scaleY, 1f);
            }

            return new Vector3(targetWidth, targetHeight, 1f);
        }

        private static Rect ResolveNormalProxyScreenBounds(SharedWorldPrimitiveDefinition definition, SharedWorldNormalProxyDefinition proxy)
        {
            Vector2 east = new Vector2(ResolveFallbackEastStep().x, ResolveFallbackEastStep().y);
            Vector2 south = new Vector2(ResolveFallbackSouthStep().x, ResolveFallbackSouthStep().y);
            Vector2 displayFootprint = proxy.DisplayFootprint == Vector2.zero ? proxy.Footprint : proxy.DisplayFootprint;
            float halfX = displayFootprint.x * 0.5f;
            float halfZ = displayFootprint.y * 0.5f;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    Vector2 baseCorner = east * (halfX * ix) + south * (halfZ * iz);
                    AddToBounds(ref min, ref max, baseCorner);
                }
            }

            if (min.x > max.x || min.y > max.y)
            {
                return new Rect(-0.5f, 0f, 1f, 1f);
            }

            float width = Mathf.Max(0.01f, max.x - min.x);
            float height = ResolveNormalProxyDisplayHeight(definition, proxy, width);
            return new Rect(min.x, 0f, width, height);
        }

        private static void AddToBounds(ref Vector2 min, ref Vector2 max, Vector2 value)
        {
            min.x = Mathf.Min(min.x, value.x);
            min.y = Mathf.Min(min.y, value.y);
            max.x = Mathf.Max(max.x, value.x);
            max.y = Mathf.Max(max.y, value.y);
        }

        private Vector3 ResolveNormalBasePosition(SharedWorldPrimitiveDefinition definition)
        {
            int mapSize = EClass._map.Size;
            int baseCellX = Mathf.Clamp(Mathf.FloorToInt(definition.TilePosition.x), 0, mapSize - 1);
            int baseCellZ = Mathf.Clamp(Mathf.FloorToInt(definition.TilePosition.z), 0, mapSize - 1);
            return Point.shared.Set(baseCellX, baseCellZ).Position();
        }

        private static Vector3 ResolveCellCenterPosition(int cellX, int cellZ)
        {
            Cell cell = EClass._map.cells[cellX, cellZ];
            byte cellHeight = cell != null && cell.bridgeHeight != 0 ? cell.bridgeHeight : cell != null ? cell.height : (byte)0;
            float baseX = (cellX + cellZ) * EClass.screen.tileAlign.x;
            float baseY = (cellZ - cellX) * EClass.screen.tileAlign.y + cellHeight * EClass.screen.tileMap._heightMod.y;
            float baseZ = 1000f + baseX * EClass.screen.tileWeight.x + baseY * EClass.screen.tileWeight.z + cellHeight * EClass.screen.tileMap._heightMod.z;
            return new Vector3(
                baseX + EClass.screen.tileWorldSize.x * 0.5f,
                baseY + EClass.screen.tileWorldSize.y * 0.75f,
                baseZ);
        }

        private static Vector3 ResolveFallbackEastStep()
        {
            float baseX = EClass.screen.tileAlign.x;
            float baseY = -EClass.screen.tileAlign.y;
            float baseZ = baseX * EClass.screen.tileWeight.x + baseY * EClass.screen.tileWeight.z;
            return new Vector3(baseX, baseY, baseZ);
        }

        private static Vector3 ResolveFallbackSouthStep()
        {
            float baseX = EClass.screen.tileAlign.x;
            float baseY = EClass.screen.tileAlign.y;
            float baseZ = baseX * EClass.screen.tileWeight.x + baseY * EClass.screen.tileWeight.z;
            return new Vector3(baseX, baseY, baseZ);
        }

        private static Vector3 ResolveNormalHeightStep()
        {
            return new Vector3(0f, EClass.screen.tileMap._heightMod.y, EClass.screen.tileMap._heightMod.z);
        }

        private static Vector3 ResolveNormalAltitudeStep()
        {
            if (EClass.screen != null && EClass.screen.tileMap != null)
            {
                Vector3 altitudeFix = EClass.screen.tileMap.altitudeFix;
                if (Mathf.Abs(altitudeFix.y) > 0.0001f || Mathf.Abs(altitudeFix.z) > 0.0001f)
                {
                    return altitudeFix;
                }
            }

            return ResolveNormalHeightStep();
        }

        private static Vector3 ResolveNormalObjectHeightStep()
        {
            Vector3 peakFix = EClass.setting?.render?.peakFix ?? Vector3.zero;
            if (Mathf.Abs(peakFix.y) > 0.0001f || Mathf.Abs(peakFix.z) > 0.0001f)
            {
                return new Vector3(0f, peakFix.y, peakFix.z);
            }

            return ResolveNormalHeightStep();
        }

        private static float ResolveNormalProxyDisplayHeight(
            SharedWorldPrimitiveDefinition definition,
            SharedWorldNormalProxyDefinition proxy,
            float projectedWidth)
        {
            Texture2D texture = GetOrCreateNormalProxyTexture(definition);
            float aspectHeight = projectedWidth;
            float aspectOverride = proxy.TextureAspectOverride;
            if (aspectOverride > 0f)
            {
                aspectHeight = projectedWidth * aspectOverride * Mathf.Max(0.01f, proxy.TextureAspectHeightScale);
            }
            else if (texture != null && texture.width > 0 && texture.height > 0)
            {
                aspectHeight = projectedWidth * (texture.height / (float)texture.width) * Mathf.Max(0.01f, proxy.TextureAspectHeightScale);
            }

            float semanticHeight = Mathf.Max(0.01f, proxy.DisplayHeightUnits) * ResolveNormalObjectHeightStep().y;
            switch (proxy.HeightMode)
            {
                case SharedWorldNormalProxyHeightMode.SemanticOnly:
                    return semanticHeight;
                case SharedWorldNormalProxyHeightMode.TextureAspectOnly:
                    return aspectHeight;
                default:
                    return Mathf.Max(aspectHeight, semanticHeight);
            }
        }

        private static GameObject CreatePrimitiveObject(SharedWorldPrimitiveDefinition definition, bool previewObject)
        {
            GameObject obj = GameObject.CreatePrimitive(ToPrimitiveType(definition.Primitive));
            obj.name = definition.Id + (previewObject ? "_Preview" : "_Normal");
            obj.hideFlags = HideFlags.HideAndDontSave;
            Component collider = obj.GetComponent("Collider");
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            if (previewObject)
            {
                SetLayerRecursively(obj, FpsGpuPreviewRenderer.RenderLayer);
            }

            return obj;
        }

        private static GameObject CreatePreviewObject(SharedWorldPrimitiveDefinition definition, SharedWorldObjectHandle handle)
        {
            GameObject root;
            if (definition.BuildCustomGeometry != null)
            {
                root = new GameObject(definition.Id + "_PreviewRoot");
                root.hideFlags = HideFlags.HideAndDontSave;
                definition.BuildCustomGeometry(root);
                SetLayerRecursively(root, FpsGpuPreviewRenderer.RenderLayer);
            }
            else
            {
                root = CreatePrimitiveObject(definition, true);
            }

            handle.PreviewRenderers.Clear();
            handle.PreviewMaterials.Clear();
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material material = CreateObjectMaterial(definition.Id + "_Preview_" + i);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                handle.PreviewRenderers.Add(renderer);
                handle.PreviewMaterials.Add(material);
            }

            handle.PreviewRenderer = handle.PreviewRenderers.Count > 0 ? handle.PreviewRenderers[0] : null;
            handle.PreviewMaterial = handle.PreviewMaterials.Count > 0 ? handle.PreviewMaterials[0] : null;
            return root;
        }

        private static void BuildDemoStatueGeometry(GameObject root)
        {
            CreateGeometryPart(root, PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.2f, 0f), new Vector3(0.75f, 0.4f, 0.75f), Vector3.zero);
            CreateGeometryPart(root, PrimitiveType.Cube, "Torso", new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 1.0f, 0.42f), new Vector3(0f, 12f, 0f));
            CreateGeometryPart(root, PrimitiveType.Sphere, "Head", new Vector3(0.02f, 1.65f, 0.02f), new Vector3(0.38f, 0.38f, 0.38f), Vector3.zero);
            CreateGeometryPart(root, PrimitiveType.Cube, "Arm", new Vector3(0.34f, 1.05f, -0.03f), new Vector3(0.18f, 0.78f, 0.18f), new Vector3(18f, 0f, -32f));
            CreateGeometryPart(root, PrimitiveType.Cylinder, "Halo", new Vector3(-0.04f, 1.92f, 0f), new Vector3(0.52f, 0.06f, 0.52f), new Vector3(90f, 0f, 0f));
        }

        private static void BuildDemoArchGeometry(GameObject root)
        {
            CreateGeometryPart(root, PrimitiveType.Cube, "ColumnL", new Vector3(-0.72f, 0.9f, 0f), new Vector3(0.34f, 1.8f, 0.3f), Vector3.zero);
            CreateGeometryPart(root, PrimitiveType.Cube, "ColumnR", new Vector3(0.72f, 0.9f, 0f), new Vector3(0.34f, 1.8f, 0.3f), Vector3.zero);
            CreateGeometryPart(root, PrimitiveType.Cube, "Lintel", new Vector3(0f, 1.95f, 0f), new Vector3(1.84f, 0.36f, 0.38f), new Vector3(0f, 0f, 2f));
            CreateGeometryPart(root, PrimitiveType.Cube, "CapL", new Vector3(-0.72f, 1.78f, 0f), new Vector3(0.46f, 0.14f, 0.42f), Vector3.zero);
            CreateGeometryPart(root, PrimitiveType.Cube, "CapR", new Vector3(0.72f, 1.78f, 0f), new Vector3(0.46f, 0.14f, 0.42f), Vector3.zero);
        }

        private static GameObject CreateGeometryPart(GameObject parent, PrimitiveType primitiveType, string name, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.hideFlags = HideFlags.HideAndDontSave;
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEulerAngles);
            part.transform.localScale = localScale;
            Component collider = part.GetComponent("Collider");
            if (collider != null)
            {
                    UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return part;
        }

        private static void DestroyPreviewObject(SharedWorldObjectHandle handle)
        {
            for (int i = 0; i < handle.PreviewMaterials.Count; i++)
            {
                DestroyMaterial(handle.PreviewMaterials[i]);
            }

            handle.PreviewMaterials.Clear();
            handle.PreviewRenderers.Clear();
            DestroyUnityObject(handle.PreviewObject);
        }

        private static GameObject CreateNormalProxyRoot(SharedWorldPrimitiveDefinition definition)
        {
            GameObject obj = new GameObject(definition.Id + "_NormalRoot");
            obj.hideFlags = HideFlags.HideAndDontSave;
            return obj;
        }

        private static GameObject CreateNormalProxySliceObject(string name)
        {
            GameObject obj = new GameObject(name);
            obj.name = name;
            obj.hideFlags = HideFlags.HideAndDontSave;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return obj;
        }

        private static void EnsureNormalProxySlices(SharedWorldObjectHandle handle, SharedWorldPrimitiveDefinition definition)
        {
            int sliceCount = ResolveNormalProxy(definition).SliceCount;
            while (handle.NormalSliceObjects.Count < sliceCount)
            {
                int index = handle.NormalSliceObjects.Count;
                GameObject slice = CreateNormalProxySliceObject(definition.Id + "_Normal_" + index);
                slice.transform.SetParent(handle.NormalObject.transform, false);
                SpriteRenderer renderer = slice.GetComponent<SpriteRenderer>();
                ApplyNativeSpriteRendererSettings(renderer);
                Material material = CreateNormalProxyMaterial(definition.Id + "_Normal_" + index, definition);
                renderer.sharedMaterial = material;
                handle.NormalSliceObjects.Add(slice);
                handle.NormalSliceRenderers.Add(renderer);
                handle.NormalSliceSpriteRenderers.Add(renderer);
                handle.NormalSliceMaterials.Add(material);
            }

            while (handle.NormalSliceObjects.Count > sliceCount)
            {
                int last = handle.NormalSliceObjects.Count - 1;
                DestroyUnityObject(handle.NormalSliceObjects[last]);
                DestroyMaterial(handle.NormalSliceMaterials[last]);
                handle.NormalSliceObjects.RemoveAt(last);
                handle.NormalSliceRenderers.RemoveAt(last);
                handle.NormalSliceSpriteRenderers.RemoveAt(last);
                handle.NormalSliceMaterials.RemoveAt(last);
            }

            for (int i = 0; i < handle.NormalSliceObjects.Count; i++)
            {
                handle.NormalSliceObjects[i].name = definition.Id + "_Normal_" + i;
                handle.NormalSliceObjects[i].transform.SetParent(handle.NormalObject.transform, false);
                ApplyNativeSpriteRendererSettings(handle.NormalSliceSpriteRenderers[i]);
            }
        }

        private static void ApplyNativeSpriteRendererSettings(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.spriteSortPoint = SpriteSortPoint.Pivot;

            SpriteRenderer native = EClass.pc?.renderer?.actor?.sr;
            if (native == null)
            {
                return;
            }

            renderer.sortingLayerID = native.sortingLayerID;
            renderer.sortingOrder = native.sortingOrder;
            renderer.maskInteraction = native.maskInteraction;
            renderer.renderingLayerMask = native.renderingLayerMask;
        }

        private static void ApplyMaterial(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.35f);
            }
        }

        private static void ApplyNormalProxyMaterial(Material material, Color color)
        {
            ApplyMaterial(material, LiftNormalProxyColor(color));
        }

        private static void ApplyNormalProxyMaterials(SharedWorldObjectHandle handle, Color color)
        {
            for (int i = 0; i < handle.NormalSliceMaterials.Count; i++)
            {
                ApplyNormalProxyMaterial(handle.NormalSliceMaterials[i], color);
            }
        }

        private static Color LiftNormalProxyColor(Color color)
        {
            float max = Mathf.Max(color.r, color.g, color.b);
            if (max >= 0.18f)
            {
                return color;
            }

            float lift = 0.18f - max;
            return new Color(
                Mathf.Clamp01(color.r + lift),
                Mathf.Clamp01(color.g + lift),
                Mathf.Clamp01(color.b + lift),
                color.a);
        }

        private static Material CreateObjectMaterial(string name)
        {
            Shader shader = Shader.Find("Unlit/Color")
                ?? Shader.Find("Legacy Shaders/Diffuse")
                ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_Color"))
            {
                material.color = Color.white;
            }

            return material;
        }

        private static Material CreateNormalProxyMaterial(string name, SharedWorldPrimitiveDefinition definition)
        {
            Shader shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse");
            Material material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = GetOrCreateNormalProxyTexture(definition)
            };
            if (material.HasProperty("_Color"))
            {
                material.color = Color.white;
            }

            return material;
        }

        private static void RefreshNormalProxyTextures(SharedWorldObjectHandle handle, SharedWorldPrimitiveDefinition definition)
        {
            Texture2D texture = GetOrCreateNormalProxyTexture(definition);
            SharedWorldNormalProxyDefinition proxy = ResolveNormalProxy(definition);
            int sliceCount = Mathf.Max(1, handle.NormalSliceMaterials.Count);
            for (int i = 0; i < handle.NormalSliceSprites.Count; i++)
            {
                DestroySprite(handle.NormalSliceSprites[i]);
            }

            handle.NormalSliceSprites.Clear();
            for (int i = 0; i < handle.NormalSliceMaterials.Count; i++)
            {
                Material material = handle.NormalSliceMaterials[i];
                if (material == null)
                {
                    continue;
                }

                SpriteRenderer spriteRenderer = i < handle.NormalSliceSpriteRenderers.Count
                    ? handle.NormalSliceSpriteRenderers[i]
                    : null;
                if (material.mainTexture != texture)
                {
                    material.mainTexture = texture;
                }

                material.mainTextureScale = Vector2.one;
                material.mainTextureOffset = Vector2.zero;

                if (spriteRenderer == null || texture == null)
                {
                    continue;
                }

                int pixelYMin = Mathf.RoundToInt(texture.height * (i / (float)sliceCount));
                int pixelYMax = Mathf.RoundToInt(texture.height * ((i + 1) / (float)sliceCount));
                int pixelHeight = Mathf.Max(1, pixelYMax - pixelYMin);
                Rect rect = new Rect(0f, pixelYMin, texture.width, pixelHeight);
                float pivotY = ResolveSlicePivotY(proxy, i, sliceCount);
                Sprite sprite = Sprite.Create(
                    texture,
                    rect,
                    new Vector2(0f, pivotY),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
                sprite.name = definition.Id + "_NormalSprite_" + i;
                handle.NormalSliceSprites.Add(sprite);
                spriteRenderer.sprite = sprite;
            }
        }

        private static Texture2D GetOrCreateNormalProxyTexture(SharedWorldPrimitiveDefinition definition)
        {
            SharedWorldNormalProxyDefinition proxy = ResolveNormalProxy(definition);
            if (proxy.Mode == SharedWorldNormalProxyMode.ExplicitTexture && proxy.TextureOverride != null)
            {
                return proxy.TextureOverride;
            }

            string cacheKey = GetImpostorCacheKey(definition);
            Texture2D texture;
            if (ImpostorTextureCache.TryGetValue(cacheKey, out texture) && texture != null)
            {
                return texture;
            }

            texture = BuildNormalProxyTexture(definition);
            ImpostorTextureCache[cacheKey] = texture;
            return texture;
        }

        private static Texture2D GetOrCreateDebugMonolithProxyTexture()
        {
            const string cacheKey = "debug:monolith";
            Texture2D texture;
            if (ImpostorTextureCache.TryGetValue(cacheKey, out texture) && texture != null)
            {
                return texture;
            }

            const int width = 64;
            const int height = 128;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "ElinikkiDebugMonolithProxy",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = Color.white;
            Color bevel = new Color(0.82f, 0.82f, 0.82f, 1f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            int minX = 10;
            int maxX = 54;
            int minY = 4;
            int maxY = 123;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool edge = x <= minX + 1 || x >= maxX - 1 || y >= maxY - 2;
                    texture.SetPixel(x, y, edge ? bevel : fill);
                }
            }

            texture.Apply(false, false);
            texture = TrimTransparentTexture(texture, "ElinikkiDebugMonolithProxyTrimmed");
            ImpostorTextureCache[cacheKey] = texture;
            return texture;
        }

        private static Texture2D GetOrCreateDebugScreenProxyTexture()
        {
            const string cacheKey = "debug:screen";
            Texture2D texture;
            if (ImpostorTextureCache.TryGetValue(cacheKey, out texture) && texture != null)
            {
                return texture;
            }

            const int width = 128;
            const int height = 160;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "ElinikkiDebugScreenProxy",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = Color.white;
            Color edge = new Color(0.84f, 0.9f, 0.92f, 1f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            int left = 16;
            int right = 114;
            int bottom = 6;
            int top = 151;
            for (int y = bottom; y <= top; y++)
            {
                int skew = Mathf.RoundToInt((y - bottom) * 0.08f);
                int rowLeft = left + skew;
                int rowRight = right + skew;
                for (int x = rowLeft; x <= rowRight; x++)
                {
                    bool isBorder = x <= rowLeft + 2 || x >= rowRight - 2 || y <= bottom + 2 || y >= top - 2;
                    texture.SetPixel(x, y, isBorder ? edge : fill);
                }
            }

            texture.Apply(false, false);
            texture = TrimTransparentTexture(texture, "ElinikkiDebugScreenProxyTrimmed");
            ImpostorTextureCache[cacheKey] = texture;
            return texture;
        }

        private static string GetImpostorCacheKey(SharedWorldPrimitiveDefinition definition)
        {
            SharedWorldNormalProxyDefinition proxy = ResolveNormalProxy(definition);
            Vector3 euler = ResolveNormalProxyBakeEulerAngles(definition, proxy);
            int pitch = Mathf.RoundToInt(Mathf.Repeat(euler.x, 360f) / 5f);
            int yaw = Mathf.RoundToInt(Mathf.Repeat(euler.y, 360f) / 5f);
            int roll = Mathf.RoundToInt(Mathf.Repeat(euler.z, 360f) / 5f);
            return ResolveGeometryKey(definition) + ":" + proxy.FacingMode + ":" + pitch + ":" + yaw + ":" + roll;
        }

        private static Texture2D BuildNormalProxyTexture(SharedWorldPrimitiveDefinition definition)
        {
            const int size = 128;
            GameObject root = null;
            Camera camera = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            SharedWorldNormalProxyDefinition proxy = ResolveNormalProxy(definition);

            try
            {
                root = new GameObject("ElinikkiNormalProxyRoot_" + definition.Primitive);
                root.hideFlags = HideFlags.HideAndDontSave;

                if (definition.BuildCustomGeometry != null)
                {
                    GameObject geometryRoot = new GameObject("Geometry");
                    geometryRoot.hideFlags = HideFlags.HideAndDontSave;
                    geometryRoot.transform.SetParent(root.transform, false);
                    geometryRoot.transform.localRotation = ResolveNormalProxyBakeRotation(definition, proxy);
                    definition.BuildCustomGeometry(geometryRoot);
                    Renderer[] customRenderers = geometryRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
                    for (int i = 0; i < customRenderers.Length; i++)
                    {
                        Renderer customRenderer = customRenderers[i];
                        if (customRenderer == null)
                        {
                            continue;
                        }

                        Material previewMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Legacy Shaders/Diffuse"))
                        {
                            color = Color.white,
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        customRenderer.sharedMaterial = previewMaterial;
                        customRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        customRenderer.receiveShadows = false;
                    }
                }
                else
                {
                    GameObject primitiveObject = GameObject.CreatePrimitive(ToNormalProxyPrimitiveType(definition.Primitive));
                    primitiveObject.name = "Primitive";
                    primitiveObject.hideFlags = HideFlags.HideAndDontSave;
                    primitiveObject.transform.SetParent(root.transform, false);
                    primitiveObject.transform.localPosition = Vector3.zero;
                    primitiveObject.transform.localRotation = ResolveNormalProxyPrimitiveRotation(definition, proxy);
                    primitiveObject.transform.localScale = ResolveNormalProxyPrimitiveScale(definition, proxy);
                    Component collider = primitiveObject.GetComponent("Collider");
                    if (collider != null)
                    {
                        UnityEngine.Object.DestroyImmediate(collider);
                    }

                    Renderer primitiveRenderer = primitiveObject.GetComponent<Renderer>();
                    if (primitiveRenderer != null)
                    {
                        Material previewMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Legacy Shaders/Diffuse"))
                        {
                            color = Color.white,
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        primitiveRenderer.sharedMaterial = previewMaterial;
                        primitiveRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        primitiveRenderer.receiveShadows = false;
                    }
                }

                ResolveNormalProxyBakeFraming(definition, proxy, out float lookAtY, out float cameraSize);

                GameObject cameraObject = new GameObject("ElinikkiNormalProxyCamera_" + definition.Primitive);
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.position = new Vector3(2.35f, 2.4f, -2.35f);
                cameraObject.transform.LookAt(Vector3.up * lookAtY);

                camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = cameraSize;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.enabled = false;

                renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "ElinikkiNormalProxy_" + definition.Primitive,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.ReadPixels(new Rect(0f, 0f, size, size), 0, 0, false);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                return TrimTransparentTexture(texture, "ElinikkiNormalProxyTrimmed_" + definition.Primitive);
            }
            finally
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static PrimitiveType ToNormalProxyPrimitiveType(SharedWorldPrimitiveKind primitive)
        {
            return ToPrimitiveType(primitive);
        }

        private static Quaternion ResolveNormalProxyPrimitiveRotation(SharedWorldPrimitiveDefinition definition, SharedWorldNormalProxyDefinition proxy)
        {
            return ResolveNormalProxyBakeRotation(definition, proxy);
        }

        private static Quaternion ResolveNormalProxyBakeRotation(SharedWorldPrimitiveDefinition definition, SharedWorldNormalProxyDefinition proxy)
        {
            return Quaternion.Euler(ResolveNormalProxyBakeEulerAngles(definition, proxy));
        }

        private static Vector3 ResolveNormalProxyBakeEulerAngles(SharedWorldPrimitiveDefinition definition, SharedWorldNormalProxyDefinition proxy)
        {
            switch (proxy.FacingMode)
            {
                case SharedWorldNormalProxyFacingMode.DefinitionEuler:
                    return definition.EulerAngles;
                case SharedWorldNormalProxyFacingMode.FrontFacing:
                {
                    Vector3 cameraDirection = new Vector3(2.35f, 2.4f, -2.35f).normalized;
                    return Quaternion.LookRotation(cameraDirection, Vector3.up).eulerAngles;
                }
                default:
                {
                    Vector3 euler = definition.EulerAngles;
                    euler.x = 0f;
                    euler.z = 0f;
                    euler.y = Mathf.Round(Mathf.Repeat(euler.y, 360f) / 90f) * 90f;
                    return euler;
                }
            }
        }

        private static Vector3 ResolveNormalProxyPrimitiveScale(SharedWorldPrimitiveDefinition definition, SharedWorldNormalProxyDefinition proxy)
        {
            Vector3 normalizedScale = NormalizeScale(definition.Scale);
            switch (definition.Primitive)
            {
                case SharedWorldPrimitiveKind.Cube:
                    return normalizedScale;
                case SharedWorldPrimitiveKind.Sphere:
                    return normalizedScale;
                case SharedWorldPrimitiveKind.Cylinder:
                    return new Vector3(normalizedScale.x, normalizedScale.y, normalizedScale.z);
                case SharedWorldPrimitiveKind.Quad:
                    return new Vector3(
                        Mathf.Max(0.2f, normalizedScale.x),
                        Mathf.Max(0.2f, normalizedScale.y),
                        Mathf.Max(0.04f, normalizedScale.z * 0.08f));
                default:
                    return normalizedScale;
            }
        }

        private static float ResolveNormalProxyCameraSize(SharedWorldPrimitiveDefinition definition, SharedWorldNormalProxyDefinition proxy)
        {
            float horizontal = Mathf.Max(proxy.Footprint.x, proxy.Footprint.y);
            float vertical = Mathf.Max(proxy.HeightUnits, horizontal);
            return Mathf.Clamp(vertical * 0.55f, 0.8f, 2.5f);
        }

        private static void ResolveNormalProxyBakeFraming(
            SharedWorldPrimitiveDefinition definition,
            SharedWorldNormalProxyDefinition proxy,
            out float lookAtY,
            out float cameraSize)
        {
            lookAtY = Mathf.Max(0.25f, proxy.HeightUnits * 0.3f);
            cameraSize = ResolveNormalProxyCameraSize(definition, proxy);

            SharedWorldGeometryMetrics metrics;
            if (!TryGetGeometryMetrics(definition, out metrics))
            {
                return;
            }

            lookAtY = Mathf.Max(0.25f, metrics.Center.y);
            float horizontal = Mathf.Max(metrics.Size.x, metrics.Size.z, proxy.Footprint.x, proxy.Footprint.y);
            float vertical = Mathf.Max(0.01f, metrics.Size.y);
            cameraSize = Mathf.Clamp(Mathf.Max(vertical * 0.6f, horizontal * 0.6f), 0.8f, 3.2f);
        }

        private static Vector3 NormalizeScale(Vector3 scale)
        {
            float max = Mathf.Max(0.01f, scale.x, scale.y, scale.z);
            return new Vector3(scale.x / max, scale.y / max, scale.z / max);
        }

        private static float ResolveDefaultSliceStepZ(float sliceStepHeight)
        {
            Vector3 peakFix = EClass.setting?.render?.peakFix ?? Vector3.zero;
            if (Mathf.Abs(peakFix.y) > 0.0001f)
            {
                return sliceStepHeight * (peakFix.z / peakFix.y);
            }

            return EClass.setting.render.zSetting.mod1 * sliceStepHeight;
        }

        private static Texture2D TrimTransparentTexture(Texture2D source, string trimmedName)
        {
            if (source == null)
            {
                return null;
            }

            Color32[] pixels = source.GetPixels32();
            int width = source.width;
            int height = source.height;
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

            if (maxX < minX || maxY < minY)
            {
                source.name = trimmedName;
                source.Apply(false, true);
                return source;
            }

            const int padding = 2;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(width - 1, maxX + padding);
            maxY = Mathf.Min(height - 1, maxY + padding);
            int trimmedWidth = maxX - minX + 1;
            int trimmedHeight = maxY - minY + 1;

            if (trimmedWidth == width && trimmedHeight == height)
            {
                source.name = trimmedName;
                source.Apply(false, true);
                return source;
            }

            Texture2D trimmed = new Texture2D(trimmedWidth, trimmedHeight, TextureFormat.RGBA32, false)
            {
                name = trimmedName,
                filterMode = source.filterMode,
                wrapMode = source.wrapMode,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < trimmedHeight; y++)
            {
                for (int x = 0; x < trimmedWidth; x++)
                {
                    trimmed.SetPixel(x, y, source.GetPixel(minX + x, minY + y));
                }
            }

            trimmed.Apply(false, true);
            UnityEngine.Object.DestroyImmediate(source);
            return trimmed;
        }

        private static PrimitiveType ToPrimitiveType(SharedWorldPrimitiveKind kind)
        {
            switch (kind)
            {
                case SharedWorldPrimitiveKind.Sphere:
                    return PrimitiveType.Sphere;
                case SharedWorldPrimitiveKind.Cylinder:
                    return PrimitiveType.Cylinder;
                case SharedWorldPrimitiveKind.Quad:
                    return PrimitiveType.Quad;
                default:
                    return PrimitiveType.Cube;
            }
        }

        private void AppendDebugMetricObject(
            StringBuilder sb,
            SharedWorldPrimitiveDefinition definition,
            Camera normalCamera,
            Camera previewCamera)
        {
            SharedWorldObjectHandle handle;
            _handles.TryGetValue(definition.Id, out handle);
            SharedWorldNormalProxyDefinition proxy = ResolveNormalProxy(definition);
            Vector3 normalOrigin = ResolveNormalOrigin(definition);
            Vector3 normalBase = ResolveNormalBasePosition(definition);
            Rect modelRect = ResolveNormalProxyScreenBounds(definition, proxy);
            Rect projectedNormalRect;
            bool hasNormalRect = TryProjectRenderersToScreenRect(normalCamera, handle != null ? handle.NormalSliceRenderers : null, out projectedNormalRect);
            Rect projectedPreviewRect;
            bool hasPreviewRect = TryProjectRenderersToScreenRect(previewCamera, handle != null ? handle.PreviewRenderers : null, out projectedPreviewRect);
            Vector3 previewWorldPosition = Vector3.zero;
            Vector3 previewViewportPoint = Vector3.zero;
            Vector3 normalSpriteWorldPosition = Vector3.zero;
            Vector3 normalSpriteLocalPosition = Vector3.zero;
            Vector3 normalSpriteBoundsSize = Vector3.zero;
            if (handle != null && handle.PreviewObject != null)
            {
                previewWorldPosition = handle.PreviewObject.transform.position;
                if (previewCamera != null)
                {
                    previewViewportPoint = previewCamera.WorldToViewportPoint(previewWorldPosition);
                }
            }

            if (handle != null && handle.NormalSliceSpriteRenderers.Count > 0 && handle.NormalSliceSpriteRenderers[0] != null)
            {
                SpriteRenderer normalRenderer = handle.NormalSliceSpriteRenderers[0];
                normalSpriteWorldPosition = normalRenderer.transform.position;
                normalSpriteLocalPosition = normalRenderer.transform.localPosition;
                normalSpriteBoundsSize = normalRenderer.bounds.size;
            }

            sb.Append('{');
            AppendJsonProp(sb, "id", definition.Id);
            sb.Append(',');
            AppendJsonProp(sb, "primitive", definition.Primitive.ToString());
            sb.Append(',');
            AppendJsonVector3Prop(sb, "tile_position", definition.TilePosition);
            sb.Append(',');
            AppendJsonVector3Prop(sb, "scale", definition.Scale);
            sb.Append(',');
            AppendJsonVector3Prop(sb, "normal_origin", normalOrigin);
            sb.Append(',');
            AppendJsonVector3Prop(sb, "normal_base", normalBase);
            sb.Append(',');
            AppendJsonRectProp(sb, "normal_model_rect", modelRect);
            sb.Append(',');
            AppendJsonProp(sb, "normal_slice_count", proxy.SliceCount.ToString());
            sb.Append(',');
            AppendJsonVector2Prop(sb, "proxy_footprint", proxy.Footprint);
            sb.Append(',');
            AppendJsonProp(sb, "proxy_height_units", proxy.HeightUnits.ToString("F4"));
            sb.Append(',');
            AppendJsonVector3Prop(sb, "normal_sprite_world_position", normalSpriteWorldPosition);
            sb.Append(',');
            AppendJsonVector3Prop(sb, "normal_sprite_local_position", normalSpriteLocalPosition);
            sb.Append(',');
            AppendJsonVector3Prop(sb, "normal_sprite_bounds_size", normalSpriteBoundsSize);
            sb.Append(',');
            AppendJsonProp(sb, "has_normal_projected_rect", hasNormalRect ? "true" : "false");
            sb.Append(',');
            AppendJsonRectProp(sb, "normal_projected_rect", projectedNormalRect);
            sb.Append(',');
            AppendJsonProp(sb, "has_preview_projected_rect", hasPreviewRect ? "true" : "false");
            sb.Append(',');
            AppendJsonRectProp(sb, "preview_projected_rect", projectedPreviewRect);
            sb.Append(',');
            AppendJsonVector3Prop(sb, "preview_world_position", previewWorldPosition);
            sb.Append(',');
            AppendJsonVector3Prop(sb, "preview_viewport_point", previewViewportPoint);
            sb.Append('}');
        }

        private static bool TryProjectRenderersToScreenRect(Camera camera, List<Renderer> renderers, out Rect rect)
        {
            rect = default;
            if (camera == null || renderers == null || renderers.Count == 0)
            {
                return false;
            }

            bool hasRect = false;
            Rect combined = default;
            for (int i = 0; i < renderers.Count; i++)
            {
                Rect current;
                if (!TryProjectRendererToScreenRect(camera, renderers[i], out current))
                {
                    continue;
                }

                if (!hasRect)
                {
                    combined = current;
                    hasRect = true;
                    continue;
                }

                combined = Rect.MinMaxRect(
                    Mathf.Min(combined.xMin, current.xMin),
                    Mathf.Min(combined.yMin, current.yMin),
                    Mathf.Max(combined.xMax, current.xMax),
                    Mathf.Max(combined.yMax, current.yMax));
            }

            rect = combined;
            return hasRect;
        }

        private static bool TryProjectRendererToScreenRect(Camera camera, Renderer renderer, out Rect rect)
        {
            rect = default;
            if (camera == null || renderer == null)
            {
                return false;
            }

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool foundVisiblePoint = false;

            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iy = -1; iy <= 1; iy += 2)
                {
                    for (int iz = -1; iz <= 1; iz += 2)
                    {
                        Vector3 world = center + Vector3.Scale(extents, new Vector3(ix, iy, iz));
                        Vector3 screen = camera.WorldToScreenPoint(world);
                        if (screen.z <= 0f)
                        {
                            continue;
                        }

                        foundVisiblePoint = true;
                        minX = Mathf.Min(minX, screen.x);
                        minY = Mathf.Min(minY, screen.y);
                        maxX = Mathf.Max(maxX, screen.x);
                        maxY = Mathf.Max(maxY, screen.y);
                    }
                }
            }

            if (!foundVisiblePoint)
            {
                return false;
            }

            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private static void AppendJsonProp(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":\"");
            sb.Append(JsonEscape(value ?? string.Empty)).Append('"');
        }

        private static void AppendJsonVector2Prop(StringBuilder sb, string key, Vector2 value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":{");
            AppendJsonFloatProp(sb, "x", value.x);
            sb.Append(',');
            AppendJsonFloatProp(sb, "y", value.y);
            sb.Append('}');
        }

        private static void AppendJsonVector3Prop(StringBuilder sb, string key, Vector3 value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":{");
            AppendJsonFloatProp(sb, "x", value.x);
            sb.Append(',');
            AppendJsonFloatProp(sb, "y", value.y);
            sb.Append(',');
            AppendJsonFloatProp(sb, "z", value.z);
            sb.Append('}');
        }

        private static void AppendJsonRectProp(StringBuilder sb, string key, Rect value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":{");
            AppendJsonFloatProp(sb, "x", value.x);
            sb.Append(',');
            AppendJsonFloatProp(sb, "y", value.y);
            sb.Append(',');
            AppendJsonFloatProp(sb, "width", value.width);
            sb.Append(',');
            AppendJsonFloatProp(sb, "height", value.height);
            sb.Append('}');
        }

        private static void AppendJsonFloatProp(StringBuilder sb, string key, float value)
        {
            sb.Append('"').Append(JsonEscape(key)).Append("\":");
            sb.Append(value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(value.Length + 16);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private void ClearHandles()
        {
            _scratchIds.Clear();
            foreach (KeyValuePair<string, SharedWorldObjectHandle> pair in _handles)
            {
                _scratchIds.Add(pair.Key);
            }

            for (int i = 0; i < _scratchIds.Count; i++)
            {
                DestroyHandle(_scratchIds[i]);
            }
        }

        private void DestroyHandle(string id)
        {
            SharedWorldObjectHandle handle;
            if (!_handles.TryGetValue(id, out handle))
            {
                return;
            }

            DestroyHandleObjects(handle);
            _handles.Remove(id);
        }

        private static void DestroyHandleObjects(SharedWorldObjectHandle handle)
        {
            DestroyNormalProxySlices(handle);
            DestroyUnityObject(handle.NormalObject);
            DestroyPreviewObject(handle);
            handle.NormalObject = null;
            handle.PreviewObject = null;
            handle.PreviewRenderer = null;
            handle.PreviewMaterial = null;
        }

        private static void DestroyNormalProxySlices(SharedWorldObjectHandle handle)
        {
            for (int i = 0; i < handle.NormalSliceObjects.Count; i++)
            {
                DestroyUnityObject(handle.NormalSliceObjects[i]);
            }

            for (int i = 0; i < handle.NormalSliceSprites.Count; i++)
            {
                DestroySprite(handle.NormalSliceSprites[i]);
            }

            for (int i = 0; i < handle.NormalSliceMaterials.Count; i++)
            {
                DestroyMaterial(handle.NormalSliceMaterials[i]);
            }

            handle.NormalSliceObjects.Clear();
            handle.NormalSliceRenderers.Clear();
            handle.NormalSliceSpriteRenderers.Clear();
            handle.NormalSliceSprites.Clear();
            handle.NormalSliceMaterials.Clear();
        }

        private static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
            }
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite != null)
            {
                UnityEngine.Object.Destroy(sprite);
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

        private static bool CanSyncScene()
        {
            return EClass.core != null
                && EClass.core.IsGameStarted
                && EClass.pc != null
                && EClass.scene != null
                && EClass.screen != null
                && EClass.screen.tileMap != null
                && EClass._map != null
                && EClass._map.Size > 0
                && EClass._map.cells != null;
        }

        private sealed class SharedWorldObjectHandle
        {
            public SharedWorldPrimitiveKind Primitive;
            public string GeometryKey;
            public GameObject NormalObject;
            public List<GameObject> NormalSliceObjects = new List<GameObject>();
            public List<Renderer> NormalSliceRenderers = new List<Renderer>();
            public List<SpriteRenderer> NormalSliceSpriteRenderers = new List<SpriteRenderer>();
            public List<Sprite> NormalSliceSprites = new List<Sprite>();
            public List<Material> NormalSliceMaterials = new List<Material>();
            public GameObject PreviewObject;
            public Renderer PreviewRenderer;
            public Material PreviewMaterial;
            public List<Renderer> PreviewRenderers = new List<Renderer>();
            public List<Material> PreviewMaterials = new List<Material>();
        }

        private struct SharedWorldGeometryMetrics
        {
            public Vector2 BaseFootprint;
            public Vector3 Size;
            public Vector3 Center;

            public bool IsValid => Size.x > 0.001f || Size.y > 0.001f || Size.z > 0.001f;
        }
    }
}
