# Elinikki FPS Refactor Plan

## Current complexity hotspots

### 1. `FpsGpuPreviewRenderer`
- Too many responsibilities:
  - terrain collection/classification
  - terrain mesh and texture build
  - wall hybrid rendering
  - sprite rendering
  - fog / tone
  - camera / visibility
  - indoor ceiling
- Result:
  - bug fixes require cross-cutting edits
  - terrain and wall regressions are hard to isolate

### 2. `FpsIdealizedWorld`
- Mixed responsibilities:
  - source resolution
  - visibility heuristics
  - size/elevation rules
  - stack/support rules
  - roof leftovers
- Result:
  - gameplay rules and visual rules are coupled

### 3. Roof leftovers
- Roof has already been simplified to indoor ceiling only in runtime flow.
- But `FpsIdealizedWorld` still contains large roof-generation logic and caches.
- Result:
  - dead complexity remains in the codebase

## Refactor principles

1. Prefer explicit archetypes over inference-heavy generic logic.
2. Separate:
   - source resolution
   - archetype classification
   - mesh generation
   - rendering/material application
3. Keep runtime-safe changes small and testable.
4. Delete dead code once the replacement path is stable.

## Planned stages

### Stage 1: Terrain seams
- Extract terrain classification from `FpsGpuPreviewRenderer`
- Keep `FpsTerrainChunkMeshBuilder` pure
- Add tests around terrain archetype resolution

Status:
- `FpsTerrainArchetypeResolver` added
- `FpsGpuPreviewRenderer` now delegates terrain archetype decisions

### Stage 2: Sprite pipeline seams
- Split `FpsIdealizedWorld` output rules into:
  - size resolver
  - elevation/support resolver
  - visibility resolver
- Goal:
  - stop visual-size changes from affecting stacking rules

Status:
- `FpsSpriteVisibilityResolver` added
- `FpsStackSupportResolver` added
- `FpsIdealizedWorld` now delegates visibility and elevation/support decisions

### Stage 3: Remove dead roof complexity
- Delete or isolate old roof-generation paths from `FpsIdealizedWorld`
- Keep only:
  - indoor ceiling resolution
  - minimal runtime ceiling rendering

### Stage 4: Renderer decomposition
- Break `FpsGpuPreviewRenderer` into focused modules:
  - terrain pass
  - wall pass
  - sprite pass
  - atmosphere/fog pass

## Remaining tasks

### A. Roof dead-code removal
1. Remove unused roof caches from `FpsIdealizedWorld`
2. Remove `GatherRoofPlanes(...)` and `GatherRoofStructures(...)`
3. Remove old roof planning helpers:
   - `TryResolveRoofStyle`
   - `TryBuildRoofStructure`
   - `TryBuildRoofLayout`
   - `AddExteriorRoofPlanes`
   - `AddFlatRoofPlanes`
   - `AddRidgeRoofPlanes`
   - `AddInteriorCeilingPlane`
   - related roof plane builders and source helpers
4. Remove dead roof structs/enums if no longer referenced
5. Remove dead roof texture helpers from `FpsGpuSpriteTextureCache`

### B. Renderer decomposition
1. Extract terrain pass from `FpsGpuPreviewRenderer`
2. Extract wall pass from `FpsGpuPreviewRenderer`
3. Extract sprite pass from `FpsGpuPreviewRenderer`
4. Extract atmosphere/fog pass from `FpsGpuPreviewRenderer`
5. Leave only orchestration and shared caches in `FpsGpuPreviewRenderer`

### C. Sprite pipeline cleanup
1. Add direct tests for `FpsSpriteVisibilityResolver`
2. Add direct tests for stack/elevation resolver where feasible
3. Move remaining sprite-specific heuristics out of `FpsIdealizedWorld`
4. Re-check that gameplay stacking rules and visual support rules stay separated

### D. Terrain simplification cleanup
1. Keep only `Flat / RaisedPlatform / Stair / Bridge` archetype rules
2. Audit remaining inference-heavy terrain branches inside renderer
3. Remove leftover generic riser/base-floor assumptions that archetypes replaced

### E. Ceiling cleanup
1. Keep only indoor ceiling math and rendering
2. Add tests for indoor ceiling height rules
3. Remove any remaining roof-specific naming that now means ceiling only

## Immediate next targets

1. Add direct tests for `FpsSpriteVisibilityResolver`
2. Add direct tests for indoor ceiling resolution
3. Start deleting dead roof runtime paths from `FpsGpuPreviewRenderer`
