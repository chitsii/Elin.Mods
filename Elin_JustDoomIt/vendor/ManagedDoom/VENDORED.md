# ManagedDoom Vendor Notes

- Upstream: <https://github.com/sinshu/managed-doom>
- Commit: `9365696eb44326a3aab72c4bab217f7db8a87c96`
- Imported on: `2026-03-28`
- Local changes:
  - Added `src/Net48Compat.cs`
  - Replaced a small set of `net10`-era APIs so the core builds under `net48`
  - Kept `src/Silk/` out of the mod build because Unity provides the host audio/video/input bridge
