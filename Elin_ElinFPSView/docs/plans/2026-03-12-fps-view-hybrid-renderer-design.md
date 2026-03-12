# FPS View Hybrid Renderer Design

## Goal

Elin の 2D クォータービュー資産を使いながら、Daggerfall 系の「3D terrain + 2D impostor」を破綻なく描画する。

この設計では、Elin の 2D 描画結果を world 座標へ逆変換しない。world 空間の truth は `Cell` / `Card` / `Thing` のゲーム状態に限定し、2D 側からは sprite と最小限の anchor 情報だけを借りる。

## Root Cause Of Current Failures

- `renderer.position` や `BaseTileMap.GetThingPosition()` はクォータービュー描画のための 2D オフセットを含む
- それを FPS 側の world anchor に流用すると、camera yaw に対して billboard が横滑りする
- loose item, chara, furniture, tall object を同じ upright billboard で描くと、接地とサイズ規則が破綻する
- terrain height difference を wall hit として扱うと、存在しない柱や invisible ridge が出る

## Non-Goals

- Elin の 2D タイルマップ描画順をそのまま再現する
- すべての sprite に対して本来のクォータービュー offset を維持する
- 初期段階で PCC / 装備差分 / effect animation の完全一致を狙う

## Core Principles

1. terrain と object は別レイヤーとして扱う
2. world anchor は game state から作る
3. 2D render offset は見た目用情報としてのみ扱う
4. upright billboard と ground sprite を分離する
5. terrain height は height field として扱い、wall hit に潰さない

## Data Model

### 1. Terrain Layer

`FpsResolvedTerrainCell`

- `Cell Cell`
- `float FloorHeightWorld`
- `float CeilingHeightWorld`
- `bool HasSolidWall`
- `bool HasFenceLikeWall`
- `bool HasBridge`
- `bool IsWater`
- `FpsResolvedFloorSurface FloorSurface`
- `FpsResolvedWallSurface WallSurface`

Rules:

- `FloorHeightWorld` は `cell.height/bridgeHeight + tileType.FloorHeight` を基準にする
- slope / ramp / bridge の補正は terrain 側で吸収する
- 高低差は `neighbor.FloorHeightWorld - current.FloorHeightWorld` として持つ

### 2. Object Layer

`FpsResolvedSpriteInstance`

- `SpriteRenderKind Kind`
- `Vector3 AnchorWorld`
- `Vector2 FootprintWorld`
- `float HeightWorld`
- `float PivotX`
- `float PivotY`
- `Sprite Sprite`
- `RenderData RenderData`
- `int Tile`
- `BillboardFacingRule FacingRule`
- `int FacingIndex`
- `bool CastsShadow`

Kinds:

- `UprightBillboard`
- `GroundBillboard`
- `EffectBillboard`

Rules:

- `AnchorWorld` は `card.pos`, `altitude`, `stackOrder`, `freePos` から作る
- `renderer.position` は anchor source に使わない
- `GetThingPosition()` は「高度の参考」には使えても「world position の truth」には使わない

### 3. Classification Layer

`SpriteRenderKind` の分類規則:

- `Chara` -> `UprightBillboard`
- `Installed furniture / tree / plant / tall prop` -> `UprightBillboard`
- `Loose item / bag / material / dropped tool` -> `GroundBillboard`
- `Smoke / fire / mist` -> `EffectBillboard`

## Coordinate System

### World XY

FPS renderer 側の ground plane は tile 座標系そのものを使う。

- `world.x = cell x + local offset`
- `world.z = cell z + local offset`
- `world.y = floor height + extra elevation`

### Allowed Inputs For Anchor

- `card.pos.x`, `card.pos.z`
- `card.altitude`
- `Thing.stackOrder`
- `card.freePos`, `card.fx`, `card.fy` を最小限の local offset として使う
- `cell.height`, `bridgeHeight`, `tileType.FloorHeight`

### Forbidden Inputs For Anchor

- `card.renderer.position`
- `CardRenderer.PositionCenter()`
- `BaseTileMap.GetThingPosition()` の screen-space X/Y をそのまま world へ逆投影すること

## Facing Rules

### Upright Billboard

- camera-facing を基本とする
- chara は次段階で `camera-relative dir selection` を追加できる
- 初期版では sprite の左右反転と 4 方向選択までで止める

### Ground Billboard

- camera-facing でも高さを極小に保つ
- 実質的には floor-aligned impostor とみなす
- 足元が地形から離れないことを優先する

### Effect Billboard

- camera-facing
- 半透明 / 加算寄り
- floor から一定高さだけ持ち上げる

## Sizing Rules

pixel ratio だけで size を決めない。カテゴリ別の world height baseline が必要。

### Base Heights

- chara: species-independent base height
- installed prop: medium height
- tree / tall prop: tall height
- loose item: very low height
- smoke/fire: effect-specific height

### Adjustment Inputs

- sprite aspect ratio
- `sourceCard.multisize`
- optional authored overrides later

### Explicitly Avoid

- `renderData.size` をそのまま world height に使う
- `imagePivot` をそのまま接地位置に使う

## Terrain Rendering Model

terrain は 3D 的に持つが、Unity mesh は使わない。CPU raycaster の height field として扱う。

### Required Passes

1. floor pass
2. wall pass
3. step/riser pass
4. billboard pass
5. effect pass

### Step Rendering

- terrain step は wall hit に変換しない
- ray が floor intersection を進む中で、隣接セルの `FloorHeightWorld` 差分を riser として描画する
- これは `solid wall` と別の hit type にする

`HitType`

- `SolidWall`
- `TerrainRiser`
- `BridgeSide`

## Occlusion And Sorting

### Terrain

- depth buffer により隠す

### Upright Billboard

- world depth で terrain に隠れる
- billboard 同士は far-to-near sort

### Ground Billboard

- terrain の直後、upright billboard の前に描く
- footprint が terrain depth より奥なら描かない

### Effect Billboard

- 基本は最終段
- ただし wall の裏には出さない

## What To Keep From Current Implementation

- floor / wall atlas sampling
- material tint
- snow / bridge / autotile 解決
- software renderer / depth buffer / overlay 基盤

## What To Discard

- `renderer.position` 由来の billboard anchor
- loose item を upright billboard として描く前提
- terrain height difference を wall hit で代用する前提
- 2D `imagePivot` を FPS 接地の truth とみなす前提

## Implementation Milestones

### M1. Data Model Refactor

- `FpsResolvedBillboard` を廃止
- `FpsResolvedUprightSprite`
- `FpsResolvedGroundSprite`
- `FpsResolvedEffectSprite`
- `FpsResolvedTerrainCell`
  を追加

Success criteria:

- renderer が「terrain / upright / ground / effect」の 4 種だけを見る

### M2. Ground Sprite Pass

- loose item を ground sprite 化
- height は極小
- tile center + deterministic local scatter のみ

Success criteria:

- dropped item が横滑りせず、地面から浮かない

### M3. Upright Billboard Rebuild

- chara / furniture / tree を新 anchor 規則へ移行
- facing rule を camera-facing に統一

Success criteria:

- camera yaw で billboard が不自然に drift しない

### M4. Terrain Riser Pass

- step difference を separate pass にする
- invisible wall/pillar を消す

Success criteria:

- 起伏が見えるが、存在しない柱は出ない

### M5. Directional Sprite Selection

- chara に対して camera-relative dir を導入
- 4 or 8 方向 sprite を選択

Success criteria:

- chara の見た目が正面固定ではなくなる

## Verification Scenarios

1. 草原に dropped item を 1 個置く
   - 地面に貼り付いて見える
   - yaw で横滑りしない

2. 犬や NPC を近距離で観察する
   - 足元が地面から浮かない
   - near wall で前後関係が壊れない

3. 家具と木を同じ cell 周辺に置く
   - tall prop は upright
   - loose item は ground

4. 小段差のある地形を見る
   - 地形の起伏は見える
   - 柱状 artifact が出ない

5. 火や煙のある cell を見る
   - effect は wall の裏抜けしない

## Design Decision

この Mod の billboard は「2D描画結果を再現するもの」ではなく、「3D world に置く 2D impostor」として設計する。

そのため truth は world 側に置き、2D 側から受け取るのは sprite 資産だけに制限する。
