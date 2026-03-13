using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsRenderer
    {
        private Color32[] _pixels;
        private float[] _depthBuffer;
        private float[] _sceneDepthBuffer;
        private int _width;
        private int _height;
        private readonly FpsAtlasSampler _atlasSampler = new FpsAtlasSampler();
        private readonly FpsIdealizedWorld _idealizedWorld = new FpsIdealizedWorld();
        private readonly List<FpsResolvedUprightSprite> _uprightSprites = new List<FpsResolvedUprightSprite>(64);
        private readonly List<FpsResolvedGroundSprite> _groundSprites = new List<FpsResolvedGroundSprite>(64);
        private readonly List<FpsResolvedEffectSprite> _effectSprites = new List<FpsResolvedEffectSprite>(32);
        private readonly TerrainVertex[] _terrainClipInput = new TerrainVertex[4];
        private readonly TerrainVertex[] _terrainClipOutput = new TerrainVertex[4];

        private static readonly Color32 CeilingColor = new Color32(34, 40, 52, 255);
        private static readonly Color32 FloorFallbackColor = new Color32(60, 52, 40, 255);
        private static readonly Color32 WallLightColor = new Color32(174, 152, 124, 255);
        private static readonly Color32 WallDarkColor = new Color32(140, 120, 96, 255);
        private const float NearPlaneDepth = 0.05f;
        private const float TerrainCoverageEpsilon = 0.01f;
        private const float PitchMin = -0.65f;
        private const float PitchMax = 0.55f;

        public int Width => _width;

        public int Height => _height;

        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;
            _pixels = new Color32[width * height];
            _depthBuffer = new float[width];
            _sceneDepthBuffer = new float[width * height];
        }

        public Color32[] RenderFrame()
        {
            return RenderFrame(FpsViewState.Default);
        }

        public Color32[] RenderFrame(FpsViewState viewState)
        {
            if (_pixels == null || _pixels.Length != _width * _height)
            {
                Initialize(Math.Max(1, _width), Math.Max(1, _height));
            }

            bool includePlayerSelf = viewState.CameraDistance > 0.2f;
            if (!TryGetViewState(viewState, out Vector2 origin, out Vector2 forward, out float pitchOffset, out float cameraGroundHeight, out int mapSize))
            {
                FillBackground(_height * 0.5f);
                return _pixels;
            }

            float halfHeight = _height * (0.5f + pitchOffset);
            FillBackground(halfHeight);
            ClearDepthBuffer(float.MaxValue);
            ClearSceneDepthBuffer(float.MaxValue);
            float fovRadians = Mathf.Clamp(Plugin.Settings.FieldOfViewDegrees.Value, 30f, 120f) * Mathf.Deg2Rad;
            float planeLength = Mathf.Tan(fovRadians * 0.5f);
            float maxDistance = Mathf.Max(1f, Plugin.Settings.MaxDistance.Value);
            float eyeHeight = Mathf.Max(0.05f, Plugin.Settings.EyeHeight.Value);
            Vector2 plane = new Vector2(-forward.y, forward.x) * planeLength;
            _idealizedWorld.PrepareFrame();

            for (int screenX = 0; screenX < _width; screenX++)
            {
                float cameraX = 2f * screenX / (float)_width - 1f;
                Vector2 rayDirection = forward + plane * cameraX;

                if (!CastRay(origin, rayDirection, mapSize, maxDistance, out RayHit hit))
                {
                    continue;
                }

                float perpendicularDistance = Mathf.Max(hit.Distance, 0.0001f);
                int drawStart;
                int drawEnd;
                int lineHeight = Mathf.Clamp(Mathf.RoundToInt(_height / perpendicularDistance), 1, _height);
                drawStart = Mathf.RoundToInt(halfHeight - lineHeight * 0.5f);
                drawEnd = Mathf.RoundToInt(halfHeight + lineHeight * 0.5f);

                if (drawStart > drawEnd)
                {
                    int temp = drawStart;
                    drawStart = drawEnd;
                    drawEnd = temp;
                }

                drawStart = Mathf.Max(0, drawStart);
                drawEnd = Mathf.Min(_height - 1, drawEnd);
                if (drawStart > drawEnd)
                {
                    continue;
                }

                bool hasWallSurface = _idealizedWorld.TryResolveWall(hit.Cell, out FpsResolvedWallSurface wallSurface);

                for (int y = drawStart; y <= drawEnd; y++)
                {
                    float v = (y - drawStart) / (float)Mathf.Max(1, drawEnd - drawStart);
                    int index = y * _width + screenX;
                    _pixels[index] = SampleWallColor(hit, hasWallSurface, wallSurface, v);
                    _sceneDepthBuffer[index] = perpendicularDistance;
                }

                _depthBuffer[screenX] = perpendicularDistance;
            }

            RenderTerrainSurfaces(origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight, mapSize, maxDistance);
            _idealizedWorld.GatherSprites(origin, maxDistance, includePlayerSelf, _uprightSprites, _groundSprites, _effectSprites);
            RenderGroundSprites(origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight);
            RenderUprightSprites(origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight);
            RenderEffectSprites(origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight);

            return _pixels;
        }

        private void FillBackground(float halfHeight)
        {
            int horizon = Mathf.Clamp(Mathf.RoundToInt(halfHeight), 0, _height);
            for (int y = 0; y < _height; y++)
            {
                Color32 color = y < horizon ? CeilingColor : FloorFallbackColor;
                int rowOffset = y * _width;
                for (int x = 0; x < _width; x++)
                {
                    _pixels[rowOffset + x] = color;
                }
            }
        }

        private void ClearDepthBuffer(float value)
        {
            if (_depthBuffer == null || _depthBuffer.Length != _width)
            {
                _depthBuffer = new float[_width];
            }

            for (int i = 0; i < _depthBuffer.Length; i++)
            {
                _depthBuffer[i] = value;
            }
        }

        private void ClearSceneDepthBuffer(float value)
        {
            if (_sceneDepthBuffer == null || _sceneDepthBuffer.Length != _width * _height)
            {
                _sceneDepthBuffer = new float[_width * _height];
            }

            for (int i = 0; i < _sceneDepthBuffer.Length; i++)
            {
                _sceneDepthBuffer[i] = value;
            }
        }

        private static bool TryGetViewState(
            FpsViewState viewState,
            out Vector2 origin,
            out Vector2 forward,
            out float pitchOffset,
            out float cameraGroundHeight,
            out int mapSize)
        {
            origin = default;
            forward = Vector2.right;
            pitchOffset = 0f;
            cameraGroundHeight = 0f;
            mapSize = 0;

            if (EClass.core == null || !EClass.core.IsGameStarted || EClass.pc == null || EClass._map == null)
            {
                return false;
            }

            mapSize = EClass._map.Size;
            if (mapSize <= 0)
            {
                return false;
            }

            Vector2 playerOrigin = TryGetSmoothedOrigin();
            forward = viewState.HasCustomYaw
                ? new Vector2(Mathf.Cos(viewState.YawRadians), Mathf.Sin(viewState.YawRadians))
                : DirToVector(EClass.pc.dir);
            pitchOffset = Mathf.Clamp(viewState.PitchOffset, PitchMin, PitchMax);
            origin = ResolveCameraOrigin(playerOrigin, forward, mapSize, Mathf.Max(0f, viewState.CameraDistance));
            if (viewState.CameraDistance > 0.01f)
            {
                cameraGroundHeight = FpsIdealizedWorld.GetSurfaceHeightAt(playerOrigin) + viewState.CameraHeightOffset;
            }
            else
            {
                cameraGroundHeight = FpsIdealizedWorld.GetSurfaceHeightAt(origin) + viewState.CameraHeightOffset;
            }
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
            return cell != null && !IsSolidWall(cell);
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

        private void RenderGroundSprites(
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight)
        {
            _groundSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));

            for (int i = 0; i < _groundSprites.Count; i++)
            {
                FpsResolvedGroundSprite sprite = _groundSprites[i];
                float halfWidth = sprite.SizeWorld.x * 0.5f;
                float halfDepth = sprite.SizeWorld.y * 0.5f;

                Vector3 corner0 = new Vector3(
                    sprite.CenterWorld.x - halfWidth,
                    sprite.CenterWorld.y,
                    sprite.CenterWorld.z + halfDepth);
                Vector3 corner1 = new Vector3(
                    sprite.CenterWorld.x + halfWidth,
                    sprite.CenterWorld.y,
                    sprite.CenterWorld.z + halfDepth);
                Vector3 corner2 = new Vector3(
                    sprite.CenterWorld.x + halfWidth,
                    sprite.CenterWorld.y,
                    sprite.CenterWorld.z - halfDepth);
                Vector3 corner3 = new Vector3(
                    sprite.CenterWorld.x - halfWidth,
                    sprite.CenterWorld.y,
                    sprite.CenterWorld.z - halfDepth);

                if (!TryProjectWorld(corner0, origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint p0)
                    || !TryProjectWorld(corner1, origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint p1)
                    || !TryProjectWorld(corner2, origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint p2)
                    || !TryProjectWorld(corner3, origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint p3))
                {
                    continue;
                }

                DrawGroundTriangle(sprite, p0, p1, p2, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f));
                DrawGroundTriangle(sprite, p0, p2, p3, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            }
        }

        private void RenderTerrainSurfaces(
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight,
            int mapSize,
            float maxDistance)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(origin.x - maxDistance - 2f));
            int maxX = Mathf.Min(mapSize - 1, Mathf.CeilToInt(origin.x + maxDistance + 2f));
            int minZ = Mathf.Max(0, Mathf.FloorToInt(origin.y - maxDistance - 2f));
            int maxZ = Mathf.Min(mapSize - 1, Mathf.CeilToInt(origin.y + maxDistance + 2f));

            for (int cellZ = minZ; cellZ <= maxZ; cellZ++)
            {
                for (int cellX = minX; cellX <= maxX; cellX++)
                {
                    Cell cell = EClass._map.cells[cellX, cellZ];
                    if (cell == null || IsSolidWall(cell))
                    {
                        continue;
                    }

                    float cellHeight = FpsIdealizedWorld.GetCellSurfaceHeight(cell);
                    Vector2 center = new Vector2(cellX + 0.5f, cellZ + 0.5f);
                    if (Vector2.Distance(center, origin) > maxDistance + 1.5f)
                    {
                        continue;
                    }

                    if (!_idealizedWorld.TryResolveFloor(cell, cellX + cellZ * mapSize, out FpsResolvedFloorSurface surface))
                    {
                        continue;
                    }

                    RenderTerrainTop(cellX, cellZ, cellHeight, surface, origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight);

                    if (cellX + 1 < mapSize)
                    {
                        Cell neighbor = EClass._map.cells[cellX + 1, cellZ];
                        if (neighbor != null && !IsSolidWall(neighbor))
                        {
                            float neighborHeight = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor);
                            if (cellHeight > neighborHeight + 0.02f)
                            {
                                RenderTerrainRiser(
                                    cellX,
                                    cellZ,
                                    TerrainEdge.East,
                                    cellHeight,
                                    neighborHeight,
                                    surface,
                                    origin,
                                    forward,
                                    plane,
                                    halfHeight,
                                    eyeHeight,
                                    cameraGroundHeight,
                                    0.74f);
                            }
                            else if (neighborHeight > cellHeight + 0.02f
                                && _idealizedWorld.TryResolveFloor(neighbor, (cellX + 1) + cellZ * mapSize, out FpsResolvedFloorSurface neighborSurface))
                            {
                                RenderTerrainRiser(
                                    cellX + 1,
                                    cellZ,
                                    TerrainEdge.West,
                                    neighborHeight,
                                    cellHeight,
                                    neighborSurface,
                                    origin,
                                    forward,
                                    plane,
                                    halfHeight,
                                    eyeHeight,
                                    cameraGroundHeight,
                                    0.74f);
                            }
                        }
                    }

                    if (cellZ + 1 < mapSize)
                    {
                        Cell neighbor = EClass._map.cells[cellX, cellZ + 1];
                        if (neighbor != null && !IsSolidWall(neighbor))
                        {
                            float neighborHeight = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor);
                            if (cellHeight > neighborHeight + 0.02f)
                            {
                                RenderTerrainRiser(
                                    cellX,
                                    cellZ,
                                    TerrainEdge.South,
                                    cellHeight,
                                    neighborHeight,
                                    surface,
                                    origin,
                                    forward,
                                    plane,
                                    halfHeight,
                                    eyeHeight,
                                    cameraGroundHeight,
                                    0.64f);
                            }
                            else if (neighborHeight > cellHeight + 0.02f
                                && _idealizedWorld.TryResolveFloor(neighbor, cellX + (cellZ + 1) * mapSize, out FpsResolvedFloorSurface neighborSurface))
                            {
                                RenderTerrainRiser(
                                    cellX,
                                    cellZ + 1,
                                    TerrainEdge.North,
                                    neighborHeight,
                                    cellHeight,
                                    neighborSurface,
                                    origin,
                                    forward,
                                    plane,
                                    halfHeight,
                                    eyeHeight,
                                    cameraGroundHeight,
                                    0.64f);
                            }
                        }
                    }
                }
            }
        }

        private void RenderTerrainTop(
            int cellX,
            int cellZ,
            float surfaceHeight,
            FpsResolvedFloorSurface surface,
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight)
        {
            RenderTerrainTriangleClipped(
                surface,
                new Vector3(cellX, surfaceHeight, cellZ + 1f),
                new Vector2(0f, 1f),
                new Vector3(cellX + 1f, surfaceHeight, cellZ + 1f),
                new Vector2(1f, 1f),
                new Vector3(cellX + 1f, surfaceHeight, cellZ),
                new Vector2(1f, 0f),
                origin,
                forward,
                plane,
                halfHeight,
                eyeHeight,
                cameraGroundHeight,
                0.95f,
                false);
            RenderTerrainTriangleClipped(
                surface,
                new Vector3(cellX, surfaceHeight, cellZ + 1f),
                new Vector2(0f, 1f),
                new Vector3(cellX + 1f, surfaceHeight, cellZ),
                new Vector2(1f, 0f),
                new Vector3(cellX, surfaceHeight, cellZ),
                new Vector2(0f, 0f),
                origin,
                forward,
                plane,
                halfHeight,
                eyeHeight,
                cameraGroundHeight,
                0.95f,
                false);
        }

        private void RenderTerrainRiser(
            int cellX,
            int cellZ,
            TerrainEdge edge,
            float topHeight,
            float bottomHeight,
            FpsResolvedFloorSurface surface,
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight,
            float shade)
        {
            Vector3 corner0;
            Vector3 corner1;
            Vector3 corner2;
            Vector3 corner3;

            switch (edge)
            {
                case TerrainEdge.East:
                    corner0 = new Vector3(cellX + 1f, topHeight, cellZ + 1f);
                    corner1 = new Vector3(cellX + 1f, topHeight, cellZ);
                    corner2 = new Vector3(cellX + 1f, bottomHeight, cellZ);
                    corner3 = new Vector3(cellX + 1f, bottomHeight, cellZ + 1f);
                    break;
                case TerrainEdge.South:
                    corner0 = new Vector3(cellX + 1f, topHeight, cellZ + 1f);
                    corner1 = new Vector3(cellX, topHeight, cellZ + 1f);
                    corner2 = new Vector3(cellX, bottomHeight, cellZ + 1f);
                    corner3 = new Vector3(cellX + 1f, bottomHeight, cellZ + 1f);
                    break;
                case TerrainEdge.West:
                    corner0 = new Vector3(cellX, topHeight, cellZ);
                    corner1 = new Vector3(cellX, topHeight, cellZ + 1f);
                    corner2 = new Vector3(cellX, bottomHeight, cellZ + 1f);
                    corner3 = new Vector3(cellX, bottomHeight, cellZ);
                    break;
                case TerrainEdge.North:
                    corner0 = new Vector3(cellX, topHeight, cellZ);
                    corner1 = new Vector3(cellX + 1f, topHeight, cellZ);
                    corner2 = new Vector3(cellX + 1f, bottomHeight, cellZ);
                    corner3 = new Vector3(cellX, bottomHeight, cellZ);
                    break;
                default:
                    return;
            }

            RenderTerrainTriangleClipped(
                surface,
                corner0,
                new Vector2(0f, 1f),
                corner1,
                new Vector2(1f, 1f),
                corner2,
                new Vector2(1f, 0f),
                origin,
                forward,
                plane,
                halfHeight,
                eyeHeight,
                cameraGroundHeight,
                shade,
                true);
            RenderTerrainTriangleClipped(
                surface,
                corner0,
                new Vector2(0f, 1f),
                corner2,
                new Vector2(1f, 0f),
                corner3,
                new Vector2(0f, 0f),
                origin,
                forward,
                plane,
                halfHeight,
                eyeHeight,
                cameraGroundHeight,
                shade,
                true);
        }

        private void RenderTerrainTriangleClipped(
            FpsResolvedFloorSurface surface,
            Vector3 worldA,
            Vector2 uvA,
            Vector3 worldB,
            Vector2 uvB,
            Vector3 worldC,
            Vector2 uvC,
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight,
            float shade,
            bool riser)
        {
            _terrainClipInput[0] = CreateTerrainVertex(worldA, uvA, origin, forward, plane);
            _terrainClipInput[1] = CreateTerrainVertex(worldB, uvB, origin, forward, plane);
            _terrainClipInput[2] = CreateTerrainVertex(worldC, uvC, origin, forward, plane);

            int clippedCount = ClipTerrainTriangleToNearPlane(_terrainClipInput, 3, _terrainClipOutput);
            if (clippedCount < 3)
            {
                return;
            }

            TerrainVertex first = _terrainClipOutput[0];
            for (int i = 1; i < clippedCount - 1; i++)
            {
                if (!TryProjectTerrainVertex(first, halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint p0)
                    || !TryProjectTerrainVertex(_terrainClipOutput[i], halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint p1)
                    || !TryProjectTerrainVertex(_terrainClipOutput[i + 1], halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint p2))
                {
                    continue;
                }

                FpsResolvedWallSurface riserSurface = default;
                bool hasRiserSurface = riser && _idealizedWorld.TryResolveTerrainRiser(surface.Cell, out riserSurface);

                DrawTerrainTriangle(
                    surface,
                    p0,
                    p1,
                    p2,
                    first.Uv,
                    _terrainClipOutput[i].Uv,
                    _terrainClipOutput[i + 1].Uv,
                    shade,
                    riser,
                    riser && IsVerticalTerrainEdge(worldA, worldB, worldC),
                    hasRiserSurface,
                    riserSurface);
            }
        }

        private void RenderUprightSprites(
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight)
        {
            _uprightSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            for (int i = 0; i < _uprightSprites.Count; i++)
            {
                FpsResolvedUprightSprite sprite = _uprightSprites[i];
                RenderVerticalSprite(
                    sprite.AnchorWorld,
                    sprite.Sprite,
                    sprite.RenderData,
                    sprite.Tile,
                    sprite.WidthWorld,
                    sprite.HeightWorld,
                    sprite.PivotX,
                    sprite.PivotY,
                    sprite.CastsShadow,
                    sprite.ShadowSizeWorld,
                    sprite.MaterialColor,
                    sprite.HasMaterialTint,
                    sprite.Light,
                    origin,
                    forward,
                    plane,
                    halfHeight,
                    eyeHeight,
                    cameraGroundHeight);
            }
        }

        private void DrawTerrainTriangle(
            FpsResolvedFloorSurface surface,
            ProjectedPoint a,
            ProjectedPoint b,
            ProjectedPoint c,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC,
            float shade,
            bool riser,
            bool riserHitVertical,
            bool hasRiserSurface,
            FpsResolvedWallSurface riserSurface)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.ScreenX, Mathf.Min(b.ScreenX, c.ScreenX))));
            int maxX = Mathf.Min(_width - 1, Mathf.CeilToInt(Mathf.Max(a.ScreenX, Mathf.Max(b.ScreenX, c.ScreenX))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.ScreenY, Mathf.Min(b.ScreenY, c.ScreenY))));
            int maxY = Mathf.Min(_height - 1, Mathf.CeilToInt(Mathf.Max(a.ScreenY, Mathf.Max(b.ScreenY, c.ScreenY))));
            float area = EdgeFunction(a.ScreenX, a.ScreenY, b.ScreenX, b.ScreenY, c.ScreenX, c.ScreenY);
            if (Mathf.Abs(area) < 0.0001f)
            {
                return;
            }

            float invDepthA = 1f / Mathf.Max(0.0001f, a.Depth);
            float invDepthB = 1f / Mathf.Max(0.0001f, b.Depth);
            float invDepthC = 1f / Mathf.Max(0.0001f, c.Depth);

            for (int y = minY; y <= maxY; y++)
            {
                float py = y + 0.5f;
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float w0 = EdgeFunction(b.ScreenX, b.ScreenY, c.ScreenX, c.ScreenY, px, py);
                    float w1 = EdgeFunction(c.ScreenX, c.ScreenY, a.ScreenX, a.ScreenY, px, py);
                    float w2 = EdgeFunction(a.ScreenX, a.ScreenY, b.ScreenX, b.ScreenY, px, py);
                    if (!IsInsideTriangle(w0, w1, w2, area, TerrainCoverageEpsilon))
                    {
                        continue;
                    }

                    w0 /= area;
                    w1 /= area;
                    w2 /= area;

                    float invDepth = w0 * invDepthA + w1 * invDepthB + w2 * invDepthC;
                    if (invDepth <= 0.0001f)
                    {
                        continue;
                    }

                    float depth = 1f / invDepth;
                    int index = y * _width + x;
                    if (depth >= _sceneDepthBuffer[index])
                    {
                        continue;
                    }

                    float u = (uvA.x * w0 * invDepthA + uvB.x * w1 * invDepthB + uvC.x * w2 * invDepthC) / invDepth;
                    float v = (uvA.y * w0 * invDepthA + uvB.y * w1 * invDepthB + uvC.y * w2 * invDepthC) / invDepth;
                    if (!TrySampleTerrainSurface(surface, u, v, riser, riserHitVertical, hasRiserSurface, riserSurface, out Color32 color))
                    {
                        continue;
                    }

                    color = ApplyDistanceShading(color, depth, shade);
                    _pixels[index] = AlphaBlend(_pixels[index], color);
                    _sceneDepthBuffer[index] = depth;
                }
            }
        }

        private TerrainVertex CreateTerrainVertex(Vector3 world, Vector2 uv, Vector2 origin, Vector2 forward, Vector2 plane)
        {
            float invDet = 1f / (plane.x * forward.y - forward.x * plane.y);
            Vector2 relative = new Vector2(world.x - origin.x, world.z - origin.y);
            return new TerrainVertex
            {
                CameraX = invDet * (forward.y * relative.x - forward.x * relative.y),
                Depth = invDet * (-plane.y * relative.x + plane.x * relative.y),
                WorldY = world.y,
                Uv = uv
            };
        }

        private static TerrainVertex IntersectTerrainEdge(TerrainVertex from, TerrainVertex to, float clipDepth)
        {
            float denom = to.Depth - from.Depth;
            float t = Mathf.Abs(denom) < 0.0001f ? 0f : (clipDepth - from.Depth) / denom;
            t = Mathf.Clamp01(t);
            return new TerrainVertex
            {
                CameraX = Mathf.Lerp(from.CameraX, to.CameraX, t),
                Depth = clipDepth,
                WorldY = Mathf.Lerp(from.WorldY, to.WorldY, t),
                Uv = Vector2.Lerp(from.Uv, to.Uv, t)
            };
        }

        private static int ClipTerrainTriangleToNearPlane(TerrainVertex[] input, int inputCount, TerrainVertex[] output)
        {
            int outputCount = 0;
            TerrainVertex previous = input[inputCount - 1];
            bool previousInside = previous.Depth >= NearPlaneDepth;

            for (int i = 0; i < inputCount; i++)
            {
                TerrainVertex current = input[i];
                bool currentInside = current.Depth >= NearPlaneDepth;

                if (currentInside != previousInside)
                {
                    output[outputCount++] = IntersectTerrainEdge(previous, current, NearPlaneDepth);
                }

                if (currentInside)
                {
                    output[outputCount++] = current;
                }

                previous = current;
                previousInside = currentInside;
            }

            return outputCount;
        }

        private bool TryProjectTerrainVertex(
            TerrainVertex vertex,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight,
            out ProjectedPoint projected)
        {
            projected = default;
            if (vertex.Depth < NearPlaneDepth)
            {
                return false;
            }

            projected = new ProjectedPoint
            {
                ScreenX = (_width * 0.5f) * (1f + vertex.CameraX / vertex.Depth),
                ScreenY = halfHeight + (_height * (eyeHeight + cameraGroundHeight - vertex.WorldY)) / vertex.Depth,
                Depth = vertex.Depth,
                RadialDistance = vertex.Depth
            };
            return true;
        }

        private void RenderEffectSprites(
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight)
        {
            _effectSprites.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            for (int i = 0; i < _effectSprites.Count; i++)
            {
                FpsResolvedEffectSprite sprite = _effectSprites[i];
                RenderVerticalSprite(
                    sprite.AnchorWorld,
                    null,
                    sprite.RenderData,
                    sprite.Tile,
                    sprite.WidthWorld,
                    sprite.HeightWorld,
                    sprite.PivotX,
                    sprite.PivotY,
                    false,
                    0f,
                    0,
                    false,
                    sprite.Light,
                    origin,
                    forward,
                    plane,
                    halfHeight,
                    eyeHeight,
                    cameraGroundHeight);
            }
        }

        private void RenderVerticalSprite(
            Vector3 anchorWorld,
            Sprite sprite,
            RenderData renderData,
            int tile,
            float widthWorld,
            float heightWorld,
            float pivotX,
            float pivotY,
            bool castsShadow,
            float shadowSizeWorld,
            int materialColor,
            bool hasMaterialTint,
            FpsResolvedLightSample light,
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight)
        {
            if (!TryProjectWorld(anchorWorld, origin, forward, plane, halfHeight, eyeHeight, cameraGroundHeight, out ProjectedPoint projected))
            {
                return;
            }

            FpsAtlasSampler.SpriteMetrics metrics;
            float minU = 0f;
            float maxU = 1f;
            float minV = 0f;
            float maxV = 1f;
            float effectiveWidthWorld = widthWorld;
            float effectiveHeightWorld = heightWorld;
            float effectivePivotX = pivotX;
            float effectivePivotY = pivotY;
            if (sprite != null && _atlasSampler.TryGetSpriteMetrics(sprite, out metrics))
            {
                minU = metrics.MinU;
                maxU = metrics.MaxU;
                minV = metrics.BottomV;
                maxV = metrics.TopV;
            }
            else if (sprite == null && renderData != null && _atlasSampler.TryGetRenderTileMetrics(renderData, tile, out metrics))
            {
                minU = metrics.MinU;
                maxU = metrics.MaxU;
                minV = metrics.BottomV;
                maxV = metrics.TopV;
            }

            float visibleWidth = Mathf.Max(0.02f, maxU - minU);
            float visibleHeight = Mathf.Max(0.02f, maxV - minV);
            effectiveWidthWorld *= visibleWidth;
            effectiveHeightWorld *= visibleHeight;
            effectivePivotX = Mathf.Clamp01((pivotX - minU) / visibleWidth);
            effectivePivotY = Mathf.Clamp01((pivotY - minV) / visibleHeight);
            if (effectivePivotY < 0.001f)
            {
                effectivePivotY = 0f;
            }

            int screenX = Mathf.RoundToInt(projected.ScreenX);
            float spriteDistance = Mathf.Max(0.05f, projected.RadialDistance);
            int spriteHeight = Mathf.Max(1, Mathf.Abs(Mathf.RoundToInt((_height * effectiveHeightWorld) / spriteDistance)));
            int spriteWidth = Mathf.Max(1, Mathf.Abs(Mathf.RoundToInt((_height * effectiveWidthWorld) / spriteDistance)));
            int groundY = Mathf.RoundToInt(projected.ScreenY);
            int rawStartX = Mathf.RoundToInt(screenX - spriteWidth * effectivePivotX);
            int rawEndX = rawStartX + spriteWidth - 1;
            int rawStartY = Mathf.RoundToInt(groundY - spriteHeight * (1f - effectivePivotY));
            int rawEndY = rawStartY + spriteHeight - 1;
            int drawStartX = Mathf.Max(0, rawStartX);
            int drawEndX = Mathf.Min(_width - 1, rawEndX);
            int drawStartY = Mathf.Max(0, rawStartY);
            int drawEndY = Mathf.Min(_height - 1, rawEndY);
            if (drawStartX > drawEndX || drawStartY > drawEndY)
            {
                return;
            }

            float occlusionDepth = Mathf.Max(0.0001f, projected.Depth - 0.01f);
            if (castsShadow)
            {
                DrawGroundShadow(spriteDistance, screenX, groundY, shadowSizeWorld);
            }

            for (int stripe = drawStartX; stripe <= drawEndX; stripe++)
            {
                float u = (stripe - rawStartX) / (float)Mathf.Max(1, rawEndX - rawStartX);
                u = Mathf.Lerp(minU, maxU, u);
                for (int y = drawStartY; y <= drawEndY; y++)
                {
                    float v = 1f - ((y - rawStartY) / (float)Mathf.Max(1, rawEndY - rawStartY));
                    v = Mathf.Lerp(minV, maxV, v);
                    if (sprite == null && renderData != null)
                    {
                        v = 1f - v;
                    }

                    if (!TrySampleSpriteInstance(sprite, renderData, tile, u, v, materialColor, hasMaterialTint, out Color32 color))
                    {
                        continue;
                    }

                    color = FpsLightApplicator.ApplySample(color, light);
                    int index = y * _width + stripe;
                    if (occlusionDepth >= _sceneDepthBuffer[index])
                    {
                        continue;
                    }

                    _pixels[index] = ApplyDistanceShading(color, projected.Depth, 1f);
                    _sceneDepthBuffer[index] = occlusionDepth;
                }
            }
        }

        private bool TrySampleSpriteInstance(
            Sprite sprite,
            RenderData renderData,
            int tile,
            float u,
            float v,
            int materialColor,
            bool hasMaterialTint,
            out Color32 color)
        {
            if (sprite != null)
            {
                if (!_atlasSampler.TrySampleSprite(sprite, u, v, out color))
                {
                    return false;
                }

                return true;
            }

            if (renderData != null)
            {
                if (!_atlasSampler.TrySampleRenderTile(renderData, tile, u, v, out color))
                {
                    return false;
                }

                if (hasMaterialTint)
                {
                    color = FpsIdealizedWorld.ApplyMatTint(color, materialColor);
                }

                return true;
            }

            color = default;
            return false;
        }

        private void DrawGroundTriangle(
            FpsResolvedGroundSprite sprite,
            ProjectedPoint a,
            ProjectedPoint b,
            ProjectedPoint c,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.ScreenX, Mathf.Min(b.ScreenX, c.ScreenX))));
            int maxX = Mathf.Min(_width - 1, Mathf.CeilToInt(Mathf.Max(a.ScreenX, Mathf.Max(b.ScreenX, c.ScreenX))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.ScreenY, Mathf.Min(b.ScreenY, c.ScreenY))));
            int maxY = Mathf.Min(_height - 1, Mathf.CeilToInt(Mathf.Max(a.ScreenY, Mathf.Max(b.ScreenY, c.ScreenY))));
            float area = EdgeFunction(a.ScreenX, a.ScreenY, b.ScreenX, b.ScreenY, c.ScreenX, c.ScreenY);
            if (Mathf.Abs(area) < 0.0001f)
            {
                return;
            }

            for (int y = minY; y <= maxY; y++)
            {
                float py = y + 0.5f;
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float w0 = EdgeFunction(b.ScreenX, b.ScreenY, c.ScreenX, c.ScreenY, px, py);
                    float w1 = EdgeFunction(c.ScreenX, c.ScreenY, a.ScreenX, a.ScreenY, px, py);
                    float w2 = EdgeFunction(a.ScreenX, a.ScreenY, b.ScreenX, b.ScreenY, px, py);
                    if (!IsInsideTriangle(w0, w1, w2, area))
                    {
                        continue;
                    }

                    w0 /= area;
                    w1 /= area;
                    w2 /= area;

                    float depth = a.Depth * w0 + b.Depth * w1 + c.Depth * w2;
                    int index = y * _width + x;
                    if (depth >= _sceneDepthBuffer[index])
                    {
                        continue;
                    }

                    float u = uvA.x * w0 + uvB.x * w1 + uvC.x * w2;
                    float v = uvA.y * w0 + uvB.y * w1 + uvC.y * w2;
                    if (!TrySampleSpriteInstance(
                        sprite.Sprite,
                        sprite.RenderData,
                        sprite.Tile,
                        u,
                        v,
                        sprite.MaterialColor,
                        sprite.HasMaterialTint,
                        out Color32 color))
                    {
                        continue;
                    }

                    color = FpsLightApplicator.ApplySample(color, sprite.Light);
                    _pixels[index] = AlphaBlend(_pixels[index], ApplyDistanceShading(color, depth, 1f));
                    _sceneDepthBuffer[index] = depth;
                }
            }
        }

        private void DrawGroundShadow(float depth, int screenX, int groundY, float shadowSizeWorld)
        {
            int radiusX = Mathf.Max(1, Mathf.RoundToInt((_height * shadowSizeWorld) / depth));
            int radiusY = Mathf.Max(1, Mathf.RoundToInt(radiusX * 0.35f));
            int centerY = Mathf.Clamp(groundY + 1, 0, _height - 1);
            int startX = Mathf.Max(0, screenX - radiusX);
            int endX = Mathf.Min(_width - 1, screenX + radiusX);
            int startY = Mathf.Max(0, centerY - radiusY);
            int endY = Mathf.Min(_height - 1, centerY + radiusY);

            for (int y = startY; y <= endY; y++)
            {
                float dy = radiusY <= 0 ? 0f : (y - centerY) / (float)radiusY;
                for (int x = startX; x <= endX; x++)
                {
                    float dx = radiusX <= 0 ? 0f : (x - screenX) / (float)radiusX;
                    float dist = dx * dx + dy * dy;
                    if (dist > 1f)
                    {
                        continue;
                    }

                    int index = y * _width + x;
                    if (depth >= _sceneDepthBuffer[index])
                    {
                        continue;
                    }

                    float alpha = (1f - dist) * 0.28f;
                    _pixels[index] = AlphaBlend(_pixels[index], new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f)));
                }
            }
        }

        private bool TryProjectWorld(
            Vector3 world,
            Vector2 origin,
            Vector2 forward,
            Vector2 plane,
            float halfHeight,
            float eyeHeight,
            float cameraGroundHeight,
            out ProjectedPoint projected)
        {
            projected = default;
            float invDet = 1f / (plane.x * forward.y - forward.x * plane.y);
            Vector2 relative = new Vector2(world.x - origin.x, world.z - origin.y);
            float transformX = invDet * (forward.y * relative.x - forward.x * relative.y);
            float transformY = invDet * (-plane.y * relative.x + plane.x * relative.y);
            if (transformY <= 0.05f)
            {
                return false;
            }

            projected = new ProjectedPoint
            {
                ScreenX = (_width * 0.5f) * (1f + transformX / transformY),
                ScreenY = halfHeight + (_height * (eyeHeight + cameraGroundHeight - world.y)) / transformY,
                Depth = transformY,
                RadialDistance = Mathf.Max(0.05f, Mathf.Sqrt(
                    relative.x * relative.x
                    + relative.y * relative.y
                    + (world.y - (cameraGroundHeight + eyeHeight)) * (world.y - (cameraGroundHeight + eyeHeight))))
            };
            return true;
        }

        private static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py)
        {
            return (px - ax) * (by - ay) - (py - ay) * (bx - ax);
        }

        private static bool IsInsideTriangle(float w0, float w1, float w2, float area, float epsilon = 0f)
        {
            if (area < 0f)
            {
                return w0 <= epsilon && w1 <= epsilon && w2 <= epsilon;
            }

            return w0 >= -epsilon && w1 >= -epsilon && w2 >= -epsilon;
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

        private static bool CastRay(Vector2 origin, Vector2 rayDirection, int mapSize, float maxDistance, out RayHit hit)
        {
            hit = default;

            int mapX = Mathf.FloorToInt(origin.x);
            int mapZ = Mathf.FloorToInt(origin.y);

            float deltaDistanceX = rayDirection.x == 0f ? float.MaxValue : Mathf.Abs(1f / rayDirection.x);
            float deltaDistanceZ = rayDirection.y == 0f ? float.MaxValue : Mathf.Abs(1f / rayDirection.y);

            int stepX;
            int stepZ;
            float sideDistanceX;
            float sideDistanceZ;
            float travelDistance = 0f;
            bool hitVertical = false;

            if (rayDirection.x < 0f)
            {
                stepX = -1;
                sideDistanceX = (origin.x - mapX) * deltaDistanceX;
            }
            else
            {
                stepX = 1;
                sideDistanceX = (mapX + 1f - origin.x) * deltaDistanceX;
            }

            if (rayDirection.y < 0f)
            {
                stepZ = -1;
                sideDistanceZ = (origin.y - mapZ) * deltaDistanceZ;
            }
            else
            {
                stepZ = 1;
                sideDistanceZ = (mapZ + 1f - origin.y) * deltaDistanceZ;
            }

            while (travelDistance < maxDistance || (sideDistanceX < maxDistance || sideDistanceZ < maxDistance))
            {
                if (sideDistanceX < sideDistanceZ)
                {
                    mapX += stepX;
                    travelDistance = sideDistanceX;
                    sideDistanceX += deltaDistanceX;
                    hitVertical = true;
                }
                else
                {
                    mapZ += stepZ;
                    travelDistance = sideDistanceZ;
                    sideDistanceZ += deltaDistanceZ;
                    hitVertical = false;
                }

                if (mapX < 0 || mapZ < 0 || mapX >= mapSize || mapZ >= mapSize)
                {
                    return false;
                }

                Cell cell = EClass._map.cells[mapX, mapZ];
                if (!IsSolidWall(cell))
                {
                    continue;
                }

                float perpendicularDistance = hitVertical
                    ? (mapX - origin.x + (1 - stepX) * 0.5f) / rayDirection.x
                    : (mapZ - origin.y + (1 - stepZ) * 0.5f) / rayDirection.y;
                if (perpendicularDistance <= 0f || perpendicularDistance > maxDistance)
                {
                    return false;
                }

                float hitX = origin.x + rayDirection.x * perpendicularDistance;
                float hitZ = origin.y + rayDirection.y * perpendicularDistance;
                float textureU = hitVertical ? hitZ - Mathf.Floor(hitZ) : hitX - Mathf.Floor(hitX);
                if ((hitVertical && rayDirection.x > 0f) || (!hitVertical && rayDirection.y < 0f))
                {
                    textureU = 1f - textureU;
                }

                hit = new RayHit
                {
                    Cell = cell,
                    Distance = perpendicularDistance,
                    HitVertical = hitVertical,
                    TextureU = textureU,
                    WorldX = hitX,
                    WorldZ = hitZ
                };
                return true;
            }

            return false;
        }

        private static bool IsSolidWall(Cell cell)
        {
            return cell != null && (cell.HasFullBlock || cell.HasWallOrFence);
        }

        private Color32 SampleWallColor(RayHit hit, bool hasWallSurface, FpsResolvedWallSurface surface, float v)
        {
            if (hasWallSurface && _atlasSampler.TrySampleBlock(surface, hit.TextureU, v, hit.HitVertical, out Color32 sampled))
            {
                sampled = FpsIdealizedWorld.ApplyMatTint(sampled, surface.MaterialColor);
                sampled = FpsLightApplicator.ApplySample(sampled, surface.Light);
                return ApplyDistanceShading(sampled, hit.Distance, hit.HitVertical ? 1f : 0.82f);
            }

            Color32 fallback = hit.HitVertical ? WallLightColor : WallDarkColor;
            if (hasWallSurface)
            {
                fallback = FpsLightApplicator.ApplySample(fallback, surface.Light);
            }

            return ApplyDistanceShading(fallback, hit.Distance, 1f);
        }

        private static Color32 ApplyDistanceShading(Color32 color, float distance, float sideMultiplier)
        {
            if (!Plugin.Settings.EnableDistanceShading.Value)
            {
                return color;
            }

            float normalized = Mathf.Clamp01(distance / Mathf.Max(1f, Plugin.Settings.MaxDistance.Value));
            float multiplier = Mathf.Lerp(1f, 0.3f, normalized) * sideMultiplier;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * multiplier), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * multiplier), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * multiplier), 0, 255),
                color.a);
        }

        private bool TrySampleFloorComposite(Cell cell, int index, float worldX, float worldZ, out Color32 color)
        {
            color = FloorFallbackColor;
            if (!_idealizedWorld.TryResolveFloor(cell, index, out FpsResolvedFloorSurface surface))
            {
                return false;
            }

            return TrySampleFloorComposite(surface, worldX, worldZ, out color);
        }

        private bool TrySampleFloorComposite(FpsResolvedFloorSurface surface, float worldX, float worldZ, out Color32 color)
        {
            color = FloorFallbackColor;
            if (!_atlasSampler.TrySampleFloorState(surface, worldX, worldZ, out Color32 baseColor))
            {
                return false;
            }

            color = FpsIdealizedWorld.ApplyMatTint(baseColor, surface.MaterialColor);

            if (surface.AutoTileOverlay >= 0)
            {
                if (_atlasSampler.TrySampleAutoTile(surface.UseWaterAutoTileAtlas, surface.AutoTileOverlay, worldX, worldZ, out Color32 overlayColor))
                {
                    overlayColor = FpsIdealizedWorld.ApplyMatTint(overlayColor, surface.MaterialColor);
                    color = AlphaBlend(color, overlayColor);
                }
            }

            color = FpsLightApplicator.ApplySample(color, surface.Light);

            return true;
        }

        private bool TrySampleTerrainSurface(
            FpsResolvedFloorSurface surface,
            float u,
            float v,
            bool riser,
            bool riserHitVertical,
            bool hasRiserSurface,
            FpsResolvedWallSurface riserSurface,
            out Color32 color)
        {
            float sampleX = Mathf.Clamp01(u);
            float sampleZ = riser ? 1f - Mathf.Clamp01(v) : Mathf.Clamp01(v);
            if (riser && hasRiserSurface)
            {
                if (_atlasSampler.TrySampleBlock(riserSurface, sampleX, sampleZ, riserHitVertical, out Color32 blockColor))
                {
                    color = FpsIdealizedWorld.ApplyMatTint(blockColor, riserSurface.MaterialColor);
                    color = FpsLightApplicator.ApplySample(color, riserSurface.Light);
                    return true;
                }
            }

            if (!_atlasSampler.TrySampleFloorSurface(surface, sampleX, sampleZ, out Color32 baseColor))
            {
                color = FloorFallbackColor;
                return false;
            }

            color = FpsIdealizedWorld.ApplyMatTint(baseColor, surface.MaterialColor);
            if (surface.AutoTileOverlay >= 0
                && _atlasSampler.TrySampleAutoTileSurface(surface.UseWaterAutoTileAtlas, surface.AutoTileOverlay, sampleX, sampleZ, out Color32 overlayColor))
            {
                overlayColor = FpsIdealizedWorld.ApplyMatTint(overlayColor, surface.MaterialColor);
                color = AlphaBlend(color, overlayColor);
            }

            color = FpsLightApplicator.ApplySample(color, surface.Light);

            return true;
        }

        private static bool IsVerticalTerrainEdge(Vector3 worldA, Vector3 worldB, Vector3 worldC)
        {
            float dx = Mathf.Abs(worldA.x - worldB.x) + Mathf.Abs(worldB.x - worldC.x) + Mathf.Abs(worldC.x - worldA.x);
            float dz = Mathf.Abs(worldA.z - worldB.z) + Mathf.Abs(worldB.z - worldC.z) + Mathf.Abs(worldC.z - worldA.z);
            return dx < dz;
        }

        private static Color32 AlphaBlend(Color32 under, Color32 over)
        {
            float alpha = over.a / 255f;
            float inv = 1f - alpha;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(under.r * inv + over.r * alpha), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(under.g * inv + over.g * alpha), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(under.b * inv + over.b * alpha), 0, 255),
                255);
        }

        private struct RayHit
        {
            public Cell Cell;
            public float Distance;
            public bool HitVertical;
            public float TextureU;
            public float WorldX;
            public float WorldZ;
        }

        private struct ProjectedPoint
        {
            public float ScreenX;
            public float ScreenY;
            public float Depth;
            public float RadialDistance;
        }

        private enum TerrainEdge
        {
            East,
            South,
            West,
            North
        }

        private struct TerrainVertex
        {
            public float CameraX;
            public float Depth;
            public float WorldY;
            public Vector2 Uv;
        }

    }

    internal struct FpsViewState
    {
        public static readonly FpsViewState Default = new FpsViewState
        {
            HasCustomYaw = false,
            YawRadians = 0f,
            PitchOffset = 0f,
            CameraDistance = 0f,
            CameraHeightOffset = 0f
        };

        public bool HasCustomYaw;
        public float YawRadians;
        public float PitchOffset;
        public float CameraDistance;
        public float CameraHeightOffset;
    }
}
