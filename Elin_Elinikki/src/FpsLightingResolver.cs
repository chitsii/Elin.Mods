using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class FpsLightingResolver
    {
        private static readonly FieldInfo ShadowStrengthField = AccessTools.Field(typeof(BaseTileMap), "shadowStrength");
        private static readonly FieldInfo ShadowStrengthCurrentField = AccessTools.Field(typeof(BaseTileMap), "_shadowStrength");
        private static readonly FieldInfo LightLimitField = AccessTools.Field(typeof(BaseTileMap), "lightLimit");
        private static readonly FieldInfo SnowLightField = AccessTools.Field(typeof(BaseTileMap), "snowLight");
        private static readonly FieldInfo SnowColorField = AccessTools.Field(typeof(BaseTileMap), "snowColor");
        private static readonly FieldInfo SnowColor2Field = AccessTools.Field(typeof(BaseTileMap), "snowColor2");
        private static readonly FieldInfo SnowLimitField = AccessTools.Field(typeof(BaseTileMap), "snowLimit");
        private static readonly FieldInfo SnowColorTokenField = AccessTools.Field(typeof(BaseTileMap), "snowColorToken");
        private static readonly FieldInfo CurrentHeightField = AccessTools.Field(typeof(BaseTileMap), "currentHeight");
        private static readonly FieldInfo CurrentLotField = AccessTools.Field(typeof(BaseTileMap), "currentLot");
        private static readonly FieldInfo CurrentRoomField = AccessTools.Field(typeof(BaseTileMap), "currentRoom");
        private static readonly FieldInfo IsSnowCoveredField = AccessTools.Field(typeof(BaseTileMap), "isSnowCovered");
        private static readonly FieldInfo FogBoundsField = AccessTools.Field(typeof(BaseTileMap), "fogBounds");
        private static readonly FieldInfo DarkenOuterField = AccessTools.Field(typeof(BaseTileMap), "darkenOuter");
        private static readonly FieldInfo BuildModeField = AccessTools.Field(typeof(BaseTileMap), "buildMode");

        private static bool _loggedCaptureFailure;
        private static bool _loggedApproximateFailure;

        private BaseTileMap _tileMap;
        private FpsLightingContext _context;
        private FpsResolvedCellLighting[] _cache = Array.Empty<FpsResolvedCellLighting>();
        private int[] _cacheStamp = Array.Empty<int>();
        private int _frameStamp;
        private bool _hasContext;

        public void CaptureFrame(BaseTileMap tileMap, bool enableWorldLighting)
        {
            AdvanceFrameStamp();
            _tileMap = tileMap;
            _context = default;
            _hasContext = false;

            if (!enableWorldLighting || tileMap == null || EClass._map == null || EClass._map.Size <= 0)
            {
                return;
            }

            try
            {
                _context = FpsLightingContext.Create(tileMap);
                EnsureCacheCapacity(EClass._map.Size * EClass._map.Size);
                _hasContext = true;
            }
            catch (Exception ex)
            {
                if (!_loggedCaptureFailure)
                {
                    _loggedCaptureFailure = true;
                    Plugin.Log?.LogWarning($"Failed to capture BaseTileMap lighting context. Falling back to unlit FPS rendering. First failure: {ex}");
                }
            }
        }

        public FpsResolvedCellLighting ResolveCellLighting(Cell cell)
        {
            if (!_hasContext || cell == null)
            {
                return FpsResolvedCellLighting.Unlit;
            }

            int index = cell.index;
            if (index < 0 || index >= _cache.Length)
            {
                return ResolveCellLightingCore(cell);
            }

            if (_cacheStamp[index] == _frameStamp)
            {
                return _cache[index];
            }

            FpsResolvedCellLighting resolved = ResolveCellLightingCore(cell);
            _cache[index] = resolved;
            _cacheStamp[index] = _frameStamp;
            return resolved;
        }

        public FpsResolvedLightSample ResolveRoofLight(Lot lot)
        {
            if (!_hasContext || lot == null || _tileMap == null)
            {
                return FpsResolvedLightSample.Unlit;
            }

            try
            {
                return FpsLightApplicator.DecodeSample(_tileMap.GetRoofLight(lot));
            }
            catch (Exception ex)
            {
                if (!_loggedApproximateFailure)
                {
                    _loggedApproximateFailure = true;
                    Plugin.Log?.LogWarning($"Failed to query BaseTileMap.GetRoofLight(). Falling back to unlit roof sample. First failure: {ex}");
                }

                return FpsResolvedLightSample.Unlit;
            }
        }

        public Room GetCurrentRoom()
        {
            return _hasContext ? _context.CurrentRoom : null;
        }

        public Lot GetCurrentLot()
        {
            return _hasContext ? _context.CurrentLot : null;
        }

        public bool GetShowRoof()
        {
            return _hasContext && _context.ShowRoof;
        }

        public bool GetHideRoomFog()
        {
            return _hasContext && _context.HideRoomFog;
        }

        public bool GetNoRoofMode()
        {
            return _hasContext && _context.NoRoofMode;
        }

        private FpsResolvedCellLighting ResolveCellLightingCore(Cell cell)
        {
            bool roof = cell.HasRoof;
            bool snowed = _context.IsSnowCovered && !roof && !cell.isClearSnow && !cell.isDeck && !cell.isFloating;
            float shadowStrength = snowed ? _context.ShadowStrength * 0.4f : _context.ShadowStrength;
            int lightIndex = cell.pcSync ? cell.light : cell.light / 3 * 2;
            float lookup = ResolveLightLookup(lightIndex);
            float blockLight = _context.LightMod * lookup + _context.BaseBrightness + ((roof || cell.isShadowed) ? shadowStrength : 0f);

            if (cell.HasBridge)
            {
                if (cell.bridgeHeight < _context.CurrentHeight)
                {
                    blockLight -= _context.HeightLightMod * (_context.CurrentHeight - cell.bridgeHeight);
                }
            }
            else if (cell.height < _context.CurrentHeight)
            {
                blockLight -= _context.HeightLightMod * (_context.CurrentHeight - cell.height);
            }

            if (blockLight < 0f)
            {
                blockLight = 0f;
            }

            if (cell.effect != null && cell.effect.IsFire)
            {
                blockLight += 0.2f;
            }

            if (blockLight > _context.LightLimit)
            {
                blockLight = _context.LightLimit;
            }

            blockLight += _context.ShadowModStrength * cell.shadowMod * _context.HeightShadowScale * shadowStrength;

            if (_context.CurrentLot != null && _context.CurrentLot.idRoofStyle != 0 && !_context.BuildMode)
            {
                bool roomEdgeTouchesCurrentLot = cell.IsRoomEdge
                    && (cell.Front.room?.lot == _context.CurrentLot
                        || cell.Right.room?.lot == _context.CurrentLot
                        || cell.FrontRight.room?.lot == _context.CurrentLot);
                if ((cell.room != null && cell.room.lot == _context.CurrentLot) || roomEdgeTouchesCurrentLot)
                {
                    blockLight += _context.LotLight;
                }
                else
                {
                    blockLight += _context.IsSnowCovered ? -0.02f : _context.LotLight2;
                }
            }

            bool isSeen = cell.isSeen;
            if (cell.outOfBounds && _context.DarkenOuter)
            {
                blockLight -= 0.1f;
                if (_context.FogBounds)
                {
                    isSeen = false;
                }
            }

            float floorLight = blockLight;
            int packedBlockLight = EncodeBlockLight(blockLight, cell.lightR, cell.lightG, cell.lightB);
            int packedFloorLight;
            if (snowed)
            {
                floorLight += _context.SnowLight;
                if (floorLight > _context.LightLimit * _context.SnowLimit)
                {
                    floorLight = _context.LightLimit * _context.SnowLimit;
                }

                packedFloorLight = EncodeSnowLight(floorLight, cell.lightR, cell.lightG, cell.lightB, _context.SnowColor, _context.SnowColorToken);
            }
            else if (_context.IsSnowCovered && !roof)
            {
                packedFloorLight = EncodeSnowLight(floorLight, cell.lightR, cell.lightG, cell.lightB, _context.SnowColor2, _context.SnowColorToken);
            }
            else
            {
                packedFloorLight = packedBlockLight;
            }

            SourceBlock.Row sourceBlock = cell.sourceBlock;
            bool selfShadowFloor = cell.room != null && sourceBlock != null && sourceBlock.tileType.CastShadowSelf && !cell.hasDoor;
            bool roofEdgeDarkening = cell.room != null
                && _context.ShowRoof
                && cell.room.lot.idRoofStyle != 0
                && !cell.room.data.atrium
                && sourceBlock != null
                && !sourceBlock.tileType.Invisible;
            if (roofEdgeDarkening && cell.hasDoor && cell.IsLotEdge)
            {
                roofEdgeDarkening = false;
            }

            if (selfShadowFloor || !isSeen || roofEdgeDarkening)
            {
                packedFloorLight -= 3145728;
            }

            if (cell.isWatered && !snowed)
            {
                packedFloorLight -= 2359296;
            }

            int packedApproxBlockLight = ResolveApproximateBlockLight(cell, packedBlockLight);
            return new FpsResolvedCellLighting
            {
                Valid = true,
                BlockLight = FpsLightApplicator.DecodeSample(packedBlockLight),
                FloorLight = FpsLightApplicator.DecodeSample(packedFloorLight),
                ApproxBlockLight = FpsLightApplicator.DecodeSample(packedApproxBlockLight)
            };
        }

        private int ResolveApproximateBlockLight(Cell cell, int fallback)
        {
            if (_tileMap == null)
            {
                return fallback;
            }

            try
            {
                return _tileMap.GetApproximateBlocklight(cell);
            }
            catch (Exception ex)
            {
                if (!_loggedApproximateFailure)
                {
                    _loggedApproximateFailure = true;
                    Plugin.Log?.LogWarning($"Failed to query BaseTileMap.GetApproximateBlocklight(). Falling back to block light. First failure: {ex}");
                }

                return fallback;
            }
        }

        private float ResolveLightLookup(int index)
        {
            if (_context.LightLookup != null && _context.LightLookup.Length > 0)
            {
                return _context.LightLookup[Mathf.Clamp(index, 0, _context.LightLookup.Length - 1)];
            }

            return Mathf.Clamp01(index / 255f);
        }

        private static int EncodeBlockLight(float brightness, int lightR, int lightG, int lightB)
        {
            int baseToken = (int)(brightness * 50f) * BaseTileMap.BlocklightToken;
            return baseToken
                + Mathf.Min(63, lightR) * 4096
                + Mathf.Min(63, lightG) * 64
                + Mathf.Min(63, lightB);
        }

        private static int EncodeSnowLight(float brightness, int lightR, int lightG, int lightB, float snowColor, int snowColorToken)
        {
            int baseToken = (int)(brightness * 50f) * BaseTileMap.BlocklightToken;
            return baseToken
                + (int)(Mathf.Min(50, lightR) * snowColor) * 4096
                + (int)(Mathf.Min(50, lightG) * snowColor) * 64
                + (int)(Mathf.Min(50, lightB) * snowColor)
                + snowColorToken;
        }

        private void AdvanceFrameStamp()
        {
            _frameStamp++;
            if (_frameStamp == int.MaxValue)
            {
                Array.Clear(_cacheStamp, 0, _cacheStamp.Length);
                _frameStamp = 1;
            }
        }

        private void EnsureCacheCapacity(int size)
        {
            if (_cache.Length == size && _cacheStamp.Length == size)
            {
                return;
            }

            _cache = new FpsResolvedCellLighting[size];
            _cacheStamp = new int[size];
        }

        private static float GetFloat(FieldInfo field, BaseTileMap tileMap, float fallback = 0f)
        {
            if (field == null || tileMap == null)
            {
                return fallback;
            }

            object value = field.GetValue(tileMap);
            return value is float result ? result : fallback;
        }

        private static int GetInt(FieldInfo field, BaseTileMap tileMap, int fallback = 0)
        {
            if (field == null || tileMap == null)
            {
                return fallback;
            }

            object value = field.GetValue(tileMap);
            return value is int result ? result : fallback;
        }

        private static bool GetBool(FieldInfo field, BaseTileMap tileMap, bool fallback = false)
        {
            if (field == null || tileMap == null)
            {
                return fallback;
            }

            object value = field.GetValue(tileMap);
            return value is bool result ? result : fallback;
        }

        private static Lot GetLot(FieldInfo field, BaseTileMap tileMap)
        {
            return field?.GetValue(tileMap) as Lot;
        }

        private static Room GetRoom(FieldInfo field, BaseTileMap tileMap)
        {
            return field?.GetValue(tileMap) as Room;
        }

        private struct FpsLightingContext
        {
            public float LightMod;
            public float BaseBrightness;
            public float[] LightLookup;
            public float ShadowStrength;
            public float HeightLightMod;
            public float HeightShadowScale;
            public float LightLimit;
            public float SnowLight;
            public float SnowColor;
            public float SnowColor2;
            public float SnowLimit;
            public float ShadowModStrength;
            public float LotLight;
            public float LotLight2;
            public int SnowColorToken;
            public int CurrentHeight;
            public bool ShowRoof;
            public bool HideRoomFog;
            public bool NoRoofMode;
            public bool DarkenOuter;
            public bool FogBounds;
            public bool IsSnowCovered;
            public bool BuildMode;
            public Lot CurrentLot;
            public Room CurrentRoom;

            public static FpsLightingContext Create(BaseTileMap tileMap)
            {
                return new FpsLightingContext
                {
                    LightMod = tileMap._lightMod,
                    BaseBrightness = tileMap._baseBrightness,
                    LightLookup = tileMap.lightLookUp,
                    ShadowStrength = ShadowStrengthField != null ? GetFloat(ShadowStrengthField, tileMap, GetFloat(ShadowStrengthCurrentField, tileMap)) : GetFloat(ShadowStrengthCurrentField, tileMap),
                    HeightLightMod = tileMap.heightLightMod,
                    HeightShadowScale = tileMap._heightMod.x,
                    LightLimit = GetFloat(LightLimitField, tileMap, 1f),
                    SnowLight = GetFloat(SnowLightField, tileMap),
                    SnowColor = GetFloat(SnowColorField, tileMap, 1f),
                    SnowColor2 = GetFloat(SnowColor2Field, tileMap, 1f),
                    SnowLimit = GetFloat(SnowLimitField, tileMap, 1f),
                    ShadowModStrength = tileMap.shadowModStrength,
                    LotLight = tileMap.lotLight,
                    LotLight2 = tileMap.lotLight2,
                    SnowColorToken = GetInt(SnowColorTokenField, tileMap),
                    CurrentHeight = GetInt(CurrentHeightField, tileMap),
                    ShowRoof = tileMap.showRoof,
                    HideRoomFog = tileMap.hideRoomFog,
                    NoRoofMode = tileMap.noRoofMode,
                    DarkenOuter = GetBool(DarkenOuterField, tileMap),
                    FogBounds = GetBool(FogBoundsField, tileMap),
                    IsSnowCovered = GetBool(IsSnowCoveredField, tileMap),
                    BuildMode = GetBool(BuildModeField, tileMap),
                    CurrentLot = GetLot(CurrentLotField, tileMap),
                    CurrentRoom = GetRoom(CurrentRoomField, tileMap)
                };
            }
        }
    }

    internal static class FpsLightApplicator
    {
        public static FpsResolvedLightSample DecodeSample(int packedLight)
        {
            return new FpsResolvedLightSample
            {
                PackedLight = packedLight,
                Multiplier = DecodeMultiplier(packedLight)
            };
        }

        public static Color32 ApplySample(Color32 color, FpsResolvedLightSample lightSample)
        {
            return ApplyMultiplier(color, lightSample.Multiplier);
        }

        public static Color32 ApplyMultiplier(Color32 color, Vector3 multiplier)
        {
            return new Color32(
                ClampToByte(color.r * multiplier.x),
                ClampToByte(color.g * multiplier.y),
                ClampToByte(color.b * multiplier.z),
                color.a);
        }

        private static Vector3 DecodeMultiplier(int packedLight)
        {
            const float colorScale = 1f / 126f;
            int baseSteps = Mathf.FloorToInt(packedLight / (float)BaseTileMap.BlocklightToken);
            int colorBits = packedLight - baseSteps * BaseTileMap.BlocklightToken;
            if (colorBits < 0)
            {
                colorBits %= BaseTileMap.BlocklightToken;
                if (colorBits < 0)
                {
                    colorBits += BaseTileMap.BlocklightToken;
                }
            }

            int lightR = Mathf.Clamp(colorBits / 4096, 0, 63);
            int lightG = Mathf.Clamp((colorBits % 4096) / 64, 0, 63);
            int lightB = Mathf.Clamp(colorBits % 64, 0, 63);
            float brightness = Mathf.Max(0f, baseSteps * 0.02f);
            return new Vector3(
                Mathf.Clamp(brightness + lightR * colorScale, 0f, 2f),
                Mathf.Clamp(brightness + lightG * colorScale, 0f, 2f),
                Mathf.Clamp(brightness + lightB * colorScale, 0f, 2f));
        }

        private static byte ClampToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);
        }
    }

    internal struct FpsResolvedLightSample
    {
        public static readonly FpsResolvedLightSample Unlit = new FpsResolvedLightSample
        {
            PackedLight = 0,
            Multiplier = Vector3.one
        };

        public int PackedLight;
        public Vector3 Multiplier;
    }

    internal struct FpsResolvedCellLighting
    {
        public static readonly FpsResolvedCellLighting Unlit = new FpsResolvedCellLighting
        {
            Valid = true,
            BlockLight = FpsResolvedLightSample.Unlit,
            FloorLight = FpsResolvedLightSample.Unlit,
            ApproxBlockLight = FpsResolvedLightSample.Unlit
        };

        public bool Valid;
        public FpsResolvedLightSample BlockLight;
        public FpsResolvedLightSample FloorLight;
        public FpsResolvedLightSample ApproxBlockLight;
    }
}
