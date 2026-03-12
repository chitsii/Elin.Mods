using System;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsRenderer
    {
        private Color32[] _pixels;
        private int _width;
        private int _height;
        private readonly FpsAtlasSampler _atlasSampler = new FpsAtlasSampler();

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
            float fovRadians = Mathf.Clamp(Plugin.Settings.FieldOfViewDegrees.Value, 30f, 120f) * Mathf.Deg2Rad;
            float planeLength = Mathf.Tan(fovRadians * 0.5f);
            float maxDistance = Mathf.Max(1f, Plugin.Settings.MaxDistance.Value);
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
                int lineHeight = Mathf.Clamp(Mathf.RoundToInt(_height / perpendicularDistance), 1, _height);
                int drawStart = Mathf.Max(0, Mathf.RoundToInt(halfHeight - lineHeight * 0.5f));
                int drawEnd = Mathf.Min(_height - 1, Mathf.RoundToInt(halfHeight + lineHeight * 0.5f));

                for (int y = drawStart; y <= drawEnd; y++)
                {
                    float v = (y - drawStart) / (float)Mathf.Max(1, drawEnd - drawStart);
                    _pixels[y * _width + screenX] = SampleWallColor(hit, v);
                }
            }

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
                        if (_atlasSampler.TrySampleFloor(cell, world.x, world.y, out Color32 floorColor))
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
                    TextureU = textureU
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
            if (_atlasSampler.TrySampleBlock(hit.Cell, hit.TextureU, v, hit.HitVertical, out Color32 sampled))
            {
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

        private struct RayHit
        {
            public Cell Cell;
            public float Distance;
            public bool HitVertical;
            public float TextureU;
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
