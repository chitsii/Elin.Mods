using System.Collections.Generic;
using UnityEngine;
namespace Elin_Elinikki
{
    internal sealed partial class FpsGpuPreviewRenderer
    {

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
            renderer.sharedMaterial = ResolveWallRendererMaterial(false);

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
            renderer.sharedMaterial = ResolveWallRendererMaterial(false);

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
            renderer.sharedMaterial = ResolveWallRendererMaterial(true);
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
            renderer.sharedMaterial = ResolveWallRendererMaterial(true);

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

    }
}
