# FPS View GPU Hybrid Design

## Goal

現行の CPU ソフトウェア renderer を描画の中心から外し、Unity の GPU パイプラインを使って FPS/TPS 表示を軽くする。

対象は次の 3 層:

- terrain / riser を Unity の quad / mesh で描く
- object / chara / effect を billboard quad で描く
- CPU raycast は interaction / hit logic / fallback visibility に縮退する

## Why Change

現行の `FpsRenderer` は次の性質を持つ:

- wall raycast
- terrain top/riser の screen-space triangle rasterization
- upright / ground / effect sprite の per-pixel sampling
- `Color32[]` -> `Texture2D.SetPixels32()` upload

この構造では、内部解像度を上げるほど `terrain + sprite overdraw + upload` がそのまま重くなる。

問題の中心は「Elin 資産」ではなく「CPU で全部塗っていること」なので、改善の主軸は GPU への責務移管になる。

## New Rendering Truth

### CPU side responsibilities

- Elin runtime state を読む
- visible cell / visible object を解決する
- terrain chunk mesh の更新判定
- billboard instance の anchor / size / light を解決する
- `Scene.HitPoint` / `Scene.mouseTarget` 向けの interaction raycast

### GPU / Unity side responsibilities

- terrain top
- terrain riser
- wall block
- upright billboard
- ground billboard
- effect billboard
- camera projection
- depth test / transparency sort の大半

## High-level Architecture

### 1. View Layer

`FpsViewManager`

- F9 トグル
- yaw / pitch / camera distance
- camera-relative move
- FPS / TPS 切替

`FpsCameraController`

- hidden layer 用の専用 `Camera`
- offscreen `RenderTexture`
- overlay `RawImage` に表示

Notes:

- main game camera は置き換えない
- FPS/TPS camera は dedicated layer だけを見る
- 既存 UI との衝突を避ける

### 2. Scene Root

`FpsRenderSceneRoot`

- hidden root `GameObject`
- dedicated layer
- terrain chunk parent
- billboard parent
- effect parent
- pooled quad/mesh renderer 群

### 3. Terrain System

`FpsTerrainChunkManager`

- visible 範囲を chunk 単位で管理
- dirty chunk のみ mesh rebuild
- chunk size は 8x8 または 16x16 cell を想定

`FpsTerrainChunkBuilder`

- top surface mesh
- riser mesh
- wall mesh
- UV / color / tangent / bounds 設定

### 4. Billboard System

`FpsBillboardManager`

- upright / ground / effect の instance pool
- visible object の割当
- camera-facing 更新
- `MaterialPropertyBlock` 更新

### 5. Asset Baking Layer

`FpsAtlasBaker`

- floor top 用 square atlas
- autotile overlay atlas
- riser / wall 用 block atlas face
- 必要なら bridge side atlas

ここで quarter-view tile を GPU 向け texture に一度変換する。
毎フレーム diamond -> square を計算しない。

### 6. Interaction Layer

`FpsInteractionRaycaster`

- center reticle / mouse look から world ray を作る
- CPU 側の cell / card hit を返す
- `Scene.HitPoint` / `Scene.mouseTarget` を更新する

## Camera Model

### Offscreen camera

- `Camera.clearFlags = SolidColor`
- `Camera.orthographic = false`
- `Camera.fieldOfView = config`
- `Camera.targetTexture = RenderTexture`
- `Camera.cullingMask = FpsViewLayer`

### FPS

- camera position = player anchor + eye height
- rotation = yaw + pitch

### TPS

- pivot = player anchor
- camera position = pivot - forward * distance + vertical offset
- collision:
  - pivot -> desired camera の sweep
  - terrain / wall mesh にめり込む場合は距離を詰める

Important:

- TPS の回転軸は常に player anchor
- terrain 高さ基準は camera cell ではなく player anchor 基準

## Terrain Design

### Chunked mesh instead of per-frame triangle rasterization

各 chunk は少なくとも 3 mesh に分ける:

- top mesh
- riser mesh
- solid wall mesh

理由:

- material / lighting / transparency ルールが違う
- dirty 範囲だけ rebuild しやすい
- top と side を分離すると UV と light が素直になる

### Geometry rules

#### Top mesh

- 各 cell の上面を 1 quad
- 4 corner 高さは cell surface 高さから作る
- bridge は surface source を bridge へ切替

#### Riser mesh

- 隣接 cell より高い辺だけ生成
- side texture は `sourceFloor._defBlock` または `sourceBridge._bridgeBlock`
- UV は block face atlas に合わせる

#### Solid wall mesh

- full block / wall / fence / bridge side を separate rule で生成
- current `IsSolidWall()` 判定を再利用しつつ、mesh face 単位へ落とす

### Height source

- `cell.height`
- `cell.bridgeHeight`
- `tileType.FloorHeight`
- current `GetTerrainHeightScale()`

### Dirty triggers

- player movement では rebuild しない
- cell content / height / floor / bridge / obj change 時だけ chunk dirty
- 初期版では visible chunks を数 frame に分けて rebuild でもよい

## Billboard Design

### Core rule

billboard は 3D world anchor を持つ。
2D offset の逆算はしない。

### Billboard kinds

#### Upright billboard

- chara
- tree
- furniture
- tall plant
- installed object

#### Ground billboard

- dropped item
- low grass / low prop
- bag / small resource

#### Effect billboard

- fire
- smoke
- mist

### Representation choices

初期版の推奨は `MeshRenderer + quad mesh + MaterialPropertyBlock`。

理由:

- `SpriteRenderer` より制御しやすい
- same material batching を狙いやすい
- per-instance texture rect / tint / light を MPB に載せやすい

ただし実装速度優先なら:

- upright/effect は `SpriteRenderer` pool
- ground は quad mesh

の混在でもよい。

### Facing rules

- upright / effect: camera-facing
- ground: floor-aligned quad
- chara は次段階で camera-relative dir selection を追加可能

### Size rules

- world size の truth は current resolved size rules を維持
- sprite の可視 alpha bounds は余白削減にだけ使う
- raw `renderer.position` や 2D screen offset は使わない

## Texture / Material Strategy

### Terrain

GPU 化するなら terrain texture は事前 bake が前提。

#### Floor top atlas

- `floors.png` を square top atlas に変換して保持
- autotile / water autotile も同様に square atlas 化
- terrain mesh はその UV をそのまま使う

#### Riser / wall atlas

- `blocks.png` face をそのまま使う
- 必要なら visible face 向けの inset atlas を別途 bake

### Billboard

- sprite は original atlas / sprite texture をそのまま使う
- PCC は `card.renderer.actor.sr.sprite` を優先

### Materials

- top / riser / wall / billboard / effect で material を分ける
- shader は unlit ベースで十分
- world lighting は vertex color または MPB color で掛ける

## Lighting Model

### Terrain

- top mesh: `floorLight`
- riser / wall mesh: `blockLight`
- 1 cell 1 color を基本に vertex color へ焼く

### Billboard

- upright: `blockLight` または `ApproxBlockLight`
- ground: `floorLight`
- effect: `ApproxBlockLight`

### Why not dynamic point lights first

- Elin 本体の truth は packed cell lighting にある
- 初期版はそれを color に落とし込む方が一致しやすく、軽い

## Visibility / Culling

### Terrain

- chunk 単位で frustum culling
- chunk bounds は camera から見る

### Billboard

- distance + frustum で CPU cull
- renderers は pool の active 切替

### CPU raycast

- render 用ではなく interaction 用に使う
- center ray から最初の cell / card hit を返す
- GPU depth readback は使わない

## What Happens To Current CPU Renderer

### Keep

- `FpsIdealizedWorld`
- lighting resolver
- terrain / object classification
- atlas / tile 解決ロジック
- interaction raycast の基礎

### Remove from hot path

- `Color32[]` full-screen rasterization
- terrain top/riser の per-frame triangle fill
- billboard の per-pixel CPU sampling
- `Texture2D.SetPixels32()` upload

### Possible retained role

`FpsRenderer` は完全廃止でなく、次の用途へ縮退できる:

- logic raycast
- debug fallback renderer
- screenshot / comparison mode

## Migration Plan

### Phase 1: Camera + RenderTexture + Scene Root

- dedicated camera を作る
- current overlay に RenderTexture を表示する
- terrain/object をまだ何も描かない skeleton を作る

Success:

- F9 で offscreen camera の出力が出る

### Phase 2: Terrain chunks

- top mesh
- riser mesh
- wall mesh
- floor/block baked atlas

Success:

- current CPU renderer を切っても地形が成立する

### Phase 3: Upright / ground / effect billboard pools

- chara / object / item / effect を pool 化
- TPS self avatar を含めて表示

Success:

- CPU sprite pass を切っても object が表示される

### Phase 4: Lighting + shadow parity

- terrain vertex color
- billboard color / MPB light
- optional fake ground shadow

Success:

- torch / fire / night lighting が current world truth に追従する

### Phase 5: Interaction bridge

- center reticle raycast
- `Scene.HitPoint` / `mouseTarget`
- talk / pickup / attack / inspect

Success:

- 3D view 単独プレイへ近づく

### Phase 6: Remove CPU renderer from default path

- current `FpsRenderer` を debug / fallback へ
- config で legacy path だけ残す

## Main Risks

### 1. Too many GameObjects

対策:

- chunk mesh 化
- billboard pool
- visible object だけ active

### 2. Atlas mismatch

対策:

- bake layer を独立させる
- runtime tile 解決と atlas bake を混ぜない

### 3. Transparency sorting for billboards

対策:

- depth test を terrain に任せる
- billboard は far-to-near sort
- ground billboard と upright billboard を pass 分離

### 4. Main game scene interference

対策:

- dedicated layer
- offscreen camera
- root を hidden / dontdestroy

## Recommended Immediate Next Step

1. `AGENTS.md` と旧 CPU-only 設計の前提を更新する
2. `FpsCameraController` + `RenderTexture` path を先に作る
3. terrain atlas bake の PoC を作る
4. chunked top/riser mesh を先に移植する

terrain が最も重いため、最初の実装対象も terrain にする。

## Related Design Notes

- sprite / object の大きさと raw color の truth は [2026-03-14-sprite-sizing-and-color-design.md](C:/Users/tishi/programming/elin_modding/Elin.Mods/Elin_Elinikki/docs/plans/2026-03-14-sprite-sizing-and-color-design.md) を参照。
