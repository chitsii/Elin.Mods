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

        private static readonly Color32 CeilingColor = new Color32(34, 40, 52, 255);
        private static readonly Color32 FloorFallbackColor = new Color32(60, 52, 40, 255);
        private static readonly Color32 WallLightColor = new Color32(174, 152, 124, 255);
        private static readonly Color32 WallDarkColor = new Color32(140, 120, 96, 255);

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

            if (!TryGetViewState(viewState, out Vector2 origin, out Vector2 forward, out float pitchOffset, out int mapSize))
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
            float cameraGroundHeight = FpsIdealizedWorld.GetCellSurfaceHeight(EClass.pc?.pos?.cell);
            Vector2 plane = new Vector2(-forward.y, forward.x) * planeLength;

            DrawFloor(origin, forward, plane, halfHeight, mapSize);

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

                for (int y = drawStart; y <= drawEnd; y++)
                {
                    float v = (y - drawStart) / (float)Mathf.Max(1, drawEnd - drawStart);
                    int index = y * _width + screenX;
                    _pixels[index] = SampleWallColor(hit, v);
                    _sceneDepthBuffer[index] = perpendicularDistance;
                }

                _depthBuffer[screenX] = perpendicularDistance;
            }

            _idealizedWorld.GatherSprites(origin, maxDistance, _uprightSprites, _groundSprites, _effectSprites);
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

        private void DrawFloor(Vector2 origin, Vector2 forward, Vector2 plane, float halfHeight, int mapSize)
        {
            Vector2 rayDirLeft = forward - plane;
            Vector2 rayDirRight = forward + plane;
            float cameraHeight = halfHeight;

            for (int y = Mathf.Max(1, Mathf.CeilToInt(halfHeight)); y < _height; y++)
            {
                float rowOffset = y - halfHeight;
                if (Mathf.Abs(rowOffset) < 0.0001f)
                {
                    continue;
                }

                float rowDistance = cameraHeight / rowOffset;
                Vector2 worldStep = rowDistance * (rayDirRight - rayDirLeft) / _width;
                Vector2 world = origin + rowDistance * rayDirLeft;
                int rowIndex = y * _width;

                for (int x = 0; x < _width; x++)
                {
                    int cellX = Mathf.FloorToInt(world.x);
                    int cellZ = Mathf.FloorToInt(world.y);
                    if (cellX >= 0 && cellZ >= 0 && cellX < mapSize && cellZ < mapSize)
                    {
                        Cell cell = EClass._map.cells[cellX, cellZ];
                        if (TrySampleFloorComposite(cell, cellX + cellZ * mapSize, world.x, world.y, out Color32 floorColor))
                        {
                            _pixels[rowIndex + x] = ApplyDistanceShading(floorColor, rowDistance, 0.75f);
                        }
                    }

                    world += worldStep;
                }
            }
        }

        private static bool TryGetViewState(FpsViewState viewState, out Vector2 origin, out Vector2 forward, out float pitchOffset, out int mapSize)
        {
            origin = default;
            forward = Vector2.right;
            pitchOffset = 0f;
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

            origin = TryGetSmoothedOrigin();
            forward = viewState.HasCustomYaw
                ? new Vector2(Mathf.Cos(viewState.YawRadians), Mathf.Sin(viewState.YawRadians))
                : DirToVector(EClass.pc.dir);
            pitchOffset = Mathf.Clamp(viewState.PitchOffset, -0.35f, 0.35f);
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
                    origin,
                    forward,
                    plane,
                    halfHeight,
                    eyeHeight,
                    cameraGroundHeight);
            }
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

            if (sprite != null && _atlasSampler.TryGetSpriteMetrics(sprite, out FpsAtlasSampler.SpriteMetrics metrics))
            {
                pivotY = Mathf.Max(pivotY, metrics.BottomV);
            }

            int screenX = Mathf.RoundToInt(projected.ScreenX);
            float spriteDistance = Mathf.Max(0.05f, projected.RadialDistance);
            int spriteHeight = Mathf.Max(1, Mathf.Abs(Mathf.RoundToInt((_height * heightWorld) / spriteDistance)));
            int spriteWidth = Mathf.Max(1, Mathf.Abs(Mathf.RoundToInt((_height * widthWorld) / spriteDistance)));
            int groundY = Mathf.RoundToInt(projected.ScreenY);
            int rawStartX = Mathf.RoundToInt(screenX - spriteWidth * pivotX);
            int rawEndX = rawStartX + spriteWidth - 1;
            int rawStartY = Mathf.RoundToInt(groundY - spriteHeight * (1f - pivotY));
            int rawEndY = rawStartY + spriteHeight - 1;
            int drawStartX = Mathf.Max(0, rawStartX);
            int drawEndX = Mathf.Min(_width - 1, rawEndX);
            int drawStartY = Mathf.Max(0, rawStartY);
            int drawEndY = Mathf.Min(_height - 1, rawEndY);
            if (drawStartX > drawEndX || drawStartY > drawEndY)
            {
                return;
            }

            if (castsShadow)
            {
                DrawGroundShadow(spriteDistance, screenX, groundY, shadowSizeWorld);
            }

            for (int stripe = drawStartX; stripe <= drawEndX; stripe++)
            {
                float u = (stripe - rawStartX) / (float)Mathf.Max(1, rawEndX - rawStartX);
                for (int y = drawStartY; y <= drawEndY; y++)
                {
                    float v = 1f - ((y - rawStartY) / (float)Mathf.Max(1, rawEndY - rawStartY));
                    if (!TrySampleSpriteInstance(sprite, renderData, tile, u, v, out Color32 color))
                    {
                        continue;
                    }

                    int index = y * _width + stripe;
                    if (projected.Depth >= _sceneDepthBuffer[index])
                    {
                        continue;
                    }

                    _pixels[index] = ApplyDistanceShading(color, projected.Depth, 1f);
                    _sceneDepthBuffer[index] = projected.Depth;
                }
            }
        }

        private bool TrySampleSpriteInstance(Sprite sprite, RenderData renderData, int tile, float u, float v, out Color32 color)
        {
            if (sprite != null)
            {
                return _atlasSampler.TrySampleSprite(sprite, u, v, out color);
            }

            if (renderData != null)
            {
                return _atlasSampler.TrySampleRenderTile(renderData, tile, u, v, out color);
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
                    if (!TrySampleSpriteInstance(sprite.Sprite, null, 0, u, v, out Color32 color))
                    {
                        continue;
                    }

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

        private static bool IsInsideTriangle(float w0, float w1, float w2, float area)
        {
            if (area < 0f)
            {
                return w0 <= 0f && w1 <= 0f && w2 <= 0f;
            }

            return w0 >= 0f && w1 >= 0f && w2 >= 0f;
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

        private Color32 SampleWallColor(RayHit hit, float v)
        {
            if (_idealizedWorld.TryResolveWall(hit.Cell, out FpsResolvedWallSurface surface)
                && _atlasSampler.TrySampleBlock(surface, hit.TextureU, v, hit.HitVertical, out Color32 sampled))
            {
                sampled = FpsIdealizedWorld.ApplyMatTint(sampled, surface.MaterialColor);
                return ApplyDistanceShading(sampled, hit.Distance, hit.HitVertical ? 1f : 0.82f);
            }

            Color32 fallback = hit.HitVertical ? WallLightColor : WallDarkColor;
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

            return true;
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

    }

    internal struct FpsViewState
    {
        public static readonly FpsViewState Default = new FpsViewState
        {
            HasCustomYaw = false,
            YawRadians = 0f,
            PitchOffset = 0f
        };

        public bool HasCustomYaw;
        public float YawRadians;
        public float PitchOffset;
    }
}
