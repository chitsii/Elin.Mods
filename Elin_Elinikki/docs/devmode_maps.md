# Elinikki Quest — Devmode Map Specification

**Status:** Phase 5 — awaiting user action. Maps must be created through
Elin devmode before Phase 6 (end-to-end verification) can start.

This document is the bridge between the Elinikki quest code
(`src/Quest/Placement/*.cs`) and the Elin devmode map editor. Every
mapping here is already wired up on the code side — creating a devmode
zone with the matching content id is the only step that still blocks
the Phase 6 playthrough.

---

## Summary table

| # | Content id                  | Chapter       | Atmosphere                | BGM                           | Placement count |
|---|-----------------------------|---------------|---------------------------|-------------------------------|------------------|
| 1 | `elinikki_nefia_entrance`   | 0 + 5         | scene default             | scene playlist (no override)  | 0                |
| 2 | `elinikki_layer_waterstone` | 1             | blue-green fog, cool LUT  | `BGM/Cave_Quiet_Exploration`* | 4                |
| 3 | `elinikki_layer_echo`       | 2             | near-black fog, warm LUT  | `BGM/Dungeon_Dark_Echo`*      | 5                |
| 4 | `elinikki_layer_bloom`      | 3             | warm magenta fog, warm LUT| `BGM/Bloom_Warm_Dream`*       | 2                |
| 5 | `elinikki_yuu_camp`         | 4             | scene default (演出なし)   | silent (drama halt)           | 1                |

`*` BGM ids are provisional placeholders pending Task 6.1 in-game
verification. `PlayBgm` already logs a Warn when the asset does not
resolve, so a missing placeholder fails loud without breaking the
zone transition. See `src/Quest/Placement/ElinikkiBgmMap.cs` for the
complete wiring.

---

## Shared conventions

* **Provisional map size:** every placement in
  `src/Quest/Placement/ElinikkiPlacementData.cs` sits inside a ~40x40
  tile bounding box, centred around `(20.5, y, 20.5)`. A devmode map
  of at least 40x40 cells will fit the placeholder layout; Task 3.6
  will tune coordinates once the real map geometry exists.
* **Cell centre convention:** all placement coordinates end in `.5`
  (e.g. `20.5, 0.03, 24.5`) because
  `FpsIdealizedWorld.GetSurfaceHeightAt` subtracts `0.5` before
  sampling. Raw integer coordinates land on cell corners and pick
  surface height from the wrong four cells.
* **Entry/exit hookup:** `ElinikkiQuestFlow.RegisterDefaultZoneRules`
  (called from `QuestBootstrap.Initialize`) already wires the
  chapter progression, so the only thing a devmode zone has to do is
  live at the right content id. The map exits can use normal Elin
  zone links; the quest flow reacts to `Zone.Activate`.
* **Playlist override:** leaving `IDPlaylistOverwrite` empty in
  devmode lets Elin's zone playlist run; the Elinikki placement
  manager takes over only when an `ElinikkiBgmMap` entry exists and
  resolves to a real asset id.

---

## 1. `elinikki_nefia_entrance` — shared entrance

The standard Nefia entrance map visited twice: once in chapter 0
(Mina + Sora are still lingering at the player's home, the player
physically travels to the entrance but the intro drama runs at
home) and once in chapter 5 (after the reunion, the return-journey
drama plays on re-entry).

* **Size:** no Elinikki-specific requirement. Any vanilla-looking
  Nefia entrance is fine; the zone rule engine uses
  `RequirePreviousZoneId = elinikki_yuu_camp` to distinguish the
  chapter 5 return from any other entry, so reuse with other
  quests is safe.
* **Fog/LUT:** no override. `ElinikkiAtmosphereData` returns null
  for this zone, so the scene profile runs unchanged.
* **BGM:** not in `ElinikkiBgmMap`. Scene playlist controls it.
* **Placements:** none. Nothing to place.
* **Quest stage rule:**
  `RegisterZoneStage(NefiaEntrance, YuuFound → Returned,
  requirePreviousZoneId: YuuCamp)`.

---

## 2. `elinikki_layer_waterstone` — 水石の層 (Chapter 1)

Water and moss cave, low blue light, steady drip. This is the
reader's first chapter-zone atmosphere shift.

* **Size:** 40x40 or larger. 4 placements, spread across the map.
* **Fog/LUT:** blue-green fog strength 0.55, cool LUT strength 0.35.
  See `ElinikkiAtmosphereData.Profiles[LayerWaterstone]`.
* **BGM:** `BGM/Cave_Quiet_Exploration` (provisional). Intent: quiet
  water/cavern ambience bed.
* **Playlist override:** clear the devmode playlist field so the
  Elinikki BGM override is authoritative.
* **Placements** (from `ElinikkiPlacementData.BuildWaterstone`):

  | Id                                    | Primitive | Tile centre        | Notes                                   |
  |---------------------------------------|-----------|--------------------|-----------------------------------------|
  | `elinikki/waterstone/trace_marks`     | Wall quad | `(20.5, 1.3, 28.5)`| North wall at z≈28. Yaw 0° (face -Z).  |
  | `elinikki/waterstone/trace_channel`   | Floor quad| `(20.5, 0.02, 24.5)`| Thin strip 0.5 × 3.0 tiles, north-south.|
  | `elinikki/waterstone/trace_stones`    | Cube       | `(16.5, 0.12, 20.5)`| Cluster placeholder, south-west corner. |
  | `elinikki/waterstone/trace_journal`   | Cube       | `(24.5, 0.06, 16.5)`| Small journal dropped by Yuu.           |

* **Devmode checklist:** wall tile at `(20, 28)` must be a solid
  north wall (the trace_marks quad projects onto it). Floor at
  `(20, 24)` and the 3-tile strip south of it should be walkable.

---

## 3. `elinikki_layer_echo` — 反響の層 (Chapter 2)

Giant dark cavern. The three echo experiment points need physical
spacing so the player actually walks between them.

* **Size:** 40x40 minimum. 5 placements plus open space for the
  three echo points (x≈12.5, 20.5, 28.5).
* **Fog/LUT:** near-black fog 0.70, warm residual LUT 0.25.
* **BGM:** `BGM/Dungeon_Dark_Echo` (provisional). Intent: slow
  dark-cavern drone.
* **Placements** (from `ElinikkiPlacementData.BuildEcho`):

  | Id                                 | Primitive  | Tile centre        | Notes                                    |
  |------------------------------------|------------|--------------------|------------------------------------------|
  | `elinikki/echo/point_a`            | Floor quad | `(12.5, 0.03, 24.5)`| 1.4 × 1.4 marker for handclap test #1.  |
  | `elinikki/echo/point_b`            | Floor quad | `(20.5, 0.03, 24.5)`| Handclap test #2.                       |
  | `elinikki/echo/point_c`            | Floor quad | `(28.5, 0.03, 24.5)`| Handclap test #3 (PHM climax).          |
  | `elinikki/echo/trace_map`          | Floor quad | `(20.5, 0.03, 16.5)`| 2.2 × 2.2 etched map south of the line. |
  | `elinikki/echo/trace_shadow`       | Wall quad  | `(30.5, 1.2, 20.5)`| East wall, yaw 90° (face -X).           |

* **Devmode checklist:** the three echo points sit on the same z
  row (`z=24.5`) 8 tiles apart. Make sure nothing occludes the
  walking path between them. Wall tile at `(30, 20)` must be a
  solid east wall (trace_shadow projects onto it).

---

## 4. `elinikki_layer_bloom` — 花の層 (Chapter 3)

Glowing flower field. The visual atmosphere carries most of this
chapter; only two placements are needed, both near the map centre.

* **Size:** 40x40 minimum. Task 3.4 atmosphere + custom floor tiles
  do the heavy lifting.
* **Fog/LUT:** warm magenta fog 0.45, warm LUT 0.40.
* **BGM:** `BGM/Bloom_Warm_Dream` (provisional). Intent: dreamy
  warm-major cue for the ゆめにっき peak.
* **Placements** (from `ElinikkiPlacementData.BuildBloom`):

  | Id                             | Primitive | Tile centre        | Notes                                      |
  |--------------------------------|-----------|--------------------|--------------------------------------------|
  | `elinikki/bloom/trace_flowers` | Cube      | `(20.5, 0.375, 20.5)`| Central flower root cluster.             |
  | `elinikki/bloom/trace_weave`   | Cube      | `(26.5, 0.05, 20.5)` | Abandoned woven cord, east of flowers.  |

* **Devmode checklist:** open walkable terrain around `(20, 20)`
  and `(26, 20)` so the player can approach both traces.

---

## 5. `elinikki_yuu_camp` — ユウの野営地 (Chapter 4)

Ordinary cave with Yuu sitting by his campfire. The story spec is
explicit about this zone: **no atmospheric override** and **no BGM**
(BGM map returns `SilentSentinel`, triggering `StopBgm` in the
placement manager). The drama layer handles the reunion + 8 truth
conversations.

* **Size:** ~40x40 or smaller is fine. Only one placeholder object
  is required (the campfire).
* **Fog/LUT:** none. `ElinikkiAtmosphereData` returns null here.
* **BGM:** `SilentSentinel`. Leave Elin's playlist field empty or
  set to a neutral cave track — the placement manager will stop
  audio on zone entry regardless.
* **Placements** (from `ElinikkiPlacementData.BuildYuuCamp`):

  | Id                            | Primitive | Tile centre         | Notes                                   |
  |-------------------------------|-----------|---------------------|-----------------------------------------|
  | `elinikki/yuu_camp/campfire`  | Cylinder  | `(20.5, 0.175, 20.5)`| 0.35-tile visible height (Unity cylinder is 2 units tall, so `scale.y = 0.175`). |

* **Devmode checklist:** leave central walkable space around
  `(20, 20)` for Yuu's spawn, the reunion dialogue, and the eight
  truth conversations. The chapter 5 return drama fires via zone
  transition back to `nefia_entrance`, so make sure the map exit
  points back there.

---

## Zone registration summary

All of these zones are already referenced in code; the only step
blocking Phase 6 is creating the actual devmode zones with the
matching `source.id`. No C# changes are required.

| Source | Zones it touches |
|--------|------------------|
| `src/Quest/Quest/ElinikkiZoneIds.cs` | All five content-id constants |
| `src/Quest/Quest/ElinikkiQuestFlow.cs::RegisterDefaultZoneRules` | layer_echo / layer_bloom / yuu_camp / nefia_entrance (stage transitions) |
| `src/Quest/Placement/ElinikkiZonePlacementManager.cs` | All five zones (placement refresh, atmosphere sync, BGM sync) |
| `src/Quest/Placement/ElinikkiPlacementData.cs` | layer_waterstone / layer_echo / layer_bloom / yuu_camp |
| `src/Quest/Placement/ElinikkiAtmosphereData.cs` | layer_waterstone / layer_echo / layer_bloom |
| `src/Quest/Placement/ElinikkiBgmMap.cs` | layer_waterstone / layer_echo / layer_bloom / yuu_camp |

Once the devmode zones exist and `Zone.source.id` resolves to the
expected constants, the quest flow, placements, atmosphere, and
BGM all take effect on the next `Zone.Activate`.

---

## Next step

1. Create the five devmode zones with the content ids listed above.
2. Run the game with an Elinikki-enabled save, enter each zone, and
   check `Player.log` for:
   - `Zone placement refresh. zone=elinikki_layer_*` (placement
     manager hook firing)
   - `QuestBridge.PlayBgm: started` / `QuestBridge.PlayBgm: BGM not
     found` (BGM mapping hit, or missing asset fail-loud)
   - `Placement verify finished. errors=0` at mod load
3. Report any mismatches and tune the provisional coordinates in
   `ElinikkiPlacementData` per Task 3.6 / Phase 6.
4. Swap provisional BGM/SE ids in `ElinikkiBgmMap` and
   `elinikki_echo_stage_1.py` for the real vanilla Elin asset ids
   (Task 6.1).
