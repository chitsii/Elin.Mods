using UnityEngine;

namespace Elin_Elinikki
{
    internal readonly struct FpsTerrainArchetypeResolution
    {
        public FpsTerrainArchetypeResolution(
            FpsTerrainChunkArchetype archetype,
            float surfaceHeight,
            float cellBaseHeight,
            float shapeBaseHeight,
            float supportBaseHeight,
            int rampDir,
            int rampStepCount,
            bool hasBridgePillar)
        {
            Archetype = archetype;
            SurfaceHeight = surfaceHeight;
            CellBaseHeight = cellBaseHeight;
            ShapeBaseHeight = shapeBaseHeight;
            SupportBaseHeight = supportBaseHeight;
            RampDir = rampDir;
            RampStepCount = rampStepCount;
            HasBridgePillar = hasBridgePillar;
        }

        public FpsTerrainChunkArchetype Archetype { get; }

        public float SurfaceHeight { get; }

        public float CellBaseHeight { get; }

        public float ShapeBaseHeight { get; }

        public float SupportBaseHeight { get; }

        public int RampDir { get; }

        public int RampStepCount { get; }

        public bool HasBridgePillar { get; }

        public bool IsSpecialTerrainCell =>
            Archetype != FpsTerrainChunkArchetype.Flat
            || SurfaceHeight > CellBaseHeight + 0.02f;
    }

    internal static class FpsTerrainArchetypeResolver
    {
        public static FpsTerrainArchetypeResolution Resolve(Cell cell, float surfaceHeight, float renderHeightOffset)
        {
            float cellBaseHeight = cell != null ? GetCellBaseHeight(cell) + renderHeightOffset : 0f;
            float shapeBaseHeight = cellBaseHeight;
            float supportBaseHeight = 0f;
            int rampStepCount = 0;
            int rampDir = IsStairTerrainCell(cell) ? cell.blockDir : 0;
            FpsTerrainChunkArchetype archetype = FpsTerrainChunkArchetype.Flat;

            if (IsStairTerrainCell(cell) && cell?.sourceBlock?.tileType != null)
            {
                float rampDrop = cell.sourceBlock.tileType.slopeHeight * FpsIdealizedWorld.GetTerrainHeightScale();
                surfaceHeight = Mathf.Max(surfaceHeight, cellBaseHeight + rampDrop);
                shapeBaseHeight = cellBaseHeight;
                rampStepCount = Mathf.Clamp(Mathf.RoundToInt(cell.sourceBlock.tileType.slopeHeight / 2f), 2, 6);
                archetype = FpsTerrainChunkArchetype.Stair;
            }

            bool hasBridgePillar = cell != null
                && cell.HasBridge
                && cell.bridgeHeight > cell.height
                && cell.sourceBridge?.tileType?.ShowPillar == true;

            if (archetype == FpsTerrainChunkArchetype.Flat && cell != null && surfaceHeight > cellBaseHeight + 0.02f)
            {
                archetype = hasBridgePillar || cell.HasBridge
                    ? FpsTerrainChunkArchetype.Bridge
                    : FpsTerrainChunkArchetype.RaisedPlatform;
            }

            if (archetype == FpsTerrainChunkArchetype.RaisedPlatform || archetype == FpsTerrainChunkArchetype.Bridge)
            {
                shapeBaseHeight = GetPlatformDeckBaseHeight(cell, surfaceHeight) + renderHeightOffset;
                supportBaseHeight = GetPlatformSupportBaseHeight(cell) + renderHeightOffset;
                if (archetype == FpsTerrainChunkArchetype.RaisedPlatform && shapeBaseHeight > supportBaseHeight + 0.12f)
                {
                    hasBridgePillar = true;
                }
            }

            return new FpsTerrainArchetypeResolution(
                archetype,
                surfaceHeight,
                cellBaseHeight,
                shapeBaseHeight,
                supportBaseHeight,
                rampDir,
                rampStepCount,
                hasBridgePillar);
        }

        public static bool IsStairTerrainCell(Cell cell)
        {
            if (cell == null)
            {
                return false;
            }

            if (cell.HasRamp || cell.HasStairs || cell.HasSlope)
            {
                return true;
            }

            return cell.sourceBlock?.tileType?.IsRamp == true;
        }

        public static float GetCellBaseHeight(Cell cell)
        {
            if (cell == null)
            {
                return 0f;
            }

            return cell.height * FpsIdealizedWorld.GetTerrainHeightScale();
        }

        public static float GetCellDeckBaseHeight(Cell cell)
        {
            if (cell == null)
            {
                return 0f;
            }

            byte deckHeight = cell.bridgeHeight != 0 ? cell.bridgeHeight : cell.height;
            return deckHeight * FpsIdealizedWorld.GetTerrainHeightScale();
        }

        public static float GetPlatformDeckBaseHeight(Cell cell, float surfaceHeight)
        {
            float supportTop = GetPlatformSupportBaseHeight(cell);
            float minThickness = 0.16f;
            if (cell?.sourceBridge?.tileType != null)
            {
                minThickness = Mathf.Max(minThickness, cell.sourceBridge.tileType.FloorHeight);
            }
            else if (cell?.sourceFloor?.tileType != null)
            {
                minThickness = Mathf.Max(minThickness, cell.sourceFloor.tileType.FloorHeight);
            }

            return Mathf.Max(supportTop, surfaceHeight - minThickness);
        }

        public static float GetPlatformSupportBaseHeight(Cell cell)
        {
            if (cell == null)
            {
                return 0f;
            }

            if (!TryGetPlatformSupportCell(cell, out Cell supportCell))
            {
                return GetCellBaseHeight(cell);
            }

            return FpsIdealizedWorld.GetCellSurfaceHeight(supportCell);
        }

        private static bool TryGetPlatformSupportCell(Cell cell, out Cell supportCell)
        {
            supportCell = cell;
            if (cell == null)
            {
                return false;
            }

            if (cell.Front != null && !cell.Front.HasBridge && !cell.Front.HasRamp && !cell.Front.HasFullBlock)
            {
                supportCell = cell.Front;
                return true;
            }

            if (cell.Right != null && !cell.Right.HasBridge && !cell.Right.HasRamp && !cell.Right.HasFullBlock)
            {
                supportCell = cell.Right;
                return true;
            }

            if (cell.Back != null && !cell.Back.HasBridge && !cell.Back.HasRamp && !cell.Back.HasFullBlock)
            {
                supportCell = cell.Back;
                return true;
            }

            if (cell.Left != null && !cell.Left.HasBridge && !cell.Left.HasRamp && !cell.Left.HasFullBlock)
            {
                supportCell = cell.Left;
                return true;
            }

            return true;
        }
    }
}
