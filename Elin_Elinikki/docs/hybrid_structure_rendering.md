# Hybrid Structure Rendering

## Goal

Replace the current roof and wall `quad projection` path with a richer near-range structure path that:

- keeps the renderer cache-friendly
- removes paper-thin walls
- reduces alpha-cut holes and roof projection artifacts
- remains testable with pure geometry planners

## Current seam

- `FpsIdealizedWorld` already resolves roof lots into `FpsRoofLayout`
- `FpsGpuPreviewRenderer` currently turns that into `FpsResolvedRoofPlane` quads
- walls currently go through `AddFullBlockCornerQuads()` and `AddWallFencePanels()`

## New seam

Pure planning:

- `FpsRoofStructurePlanner`
- future `FpsWallStructurePlanner`

Renderer integration:

- convert `FpsRoofLayout` -> `FpsRoofStructureSpec`
- build a persistent mesh/object per lot
- rebuild only when the lot/layout changes

## Why this is more testable

The planner layer is pure data:

- input: bounds, heights, ridge span, thickness, eave depth
- output: a list of labeled quads

That allows unit tests to lock:

- face counts
- bounds
- ridge direction
- thickness and overhang behavior

## Planned phases

1. Roof structure planner
2. Roof lot cache with persistent mesh objects
3. Wall structure planner
4. Wall run cache with persistent mesh objects
5. Material and UV pass for the richer geometry

## Practical rendering rule

No LOD tiers for now.

- keep view distance short
- use fog aggressively
- render only the near range well
