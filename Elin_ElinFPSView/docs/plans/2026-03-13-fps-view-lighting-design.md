# FPS View Lighting Design

## Goal

Elin 本体で計算されている fire / torch / lot light / shadow による色変化を、FPS 視点でも terrain / wall / sprite に反映する。

今回の目的は「新しい照明モデルを発明する」ことではなく、`BaseTileMap` が毎フレーム解いている light state を CPU レンダラへ移すこと。

## Investigation Summary

### 1. 現在の FPS 側は world lighting を持っていない

- `FpsRenderer` は atlas sample のあとに `ApplyMatTint()` と `ApplyDistanceShading()` を掛けているだけ
- `material tint` は再現しているが、cell light / fire light / shadow / lot light は未反映
- そのため、2D 側で赤く照らされる床や壁も FPS 側では同じ色のままになる

### 2. Elin 本体の lighting truth は既に `Cell` と `BaseTileMap` にある

`BaseTileMap` の main lighting path では、各 cell ごとに次を使って `blockLight` と `floorLight` を作っている。

- `Cell.light`
- `Cell.lightR`, `Cell.lightG`, `Cell.lightB`
- `Cell.pcSync`
- `Cell.shadowMod`
- `Cell.isShadowed`
- `Cell.effect.IsFire`
- `BaseTileMap._lightMod`
- `BaseTileMap._baseBrightness`
- `BaseTileMap.lightLookUp`
- `BaseTileMap._shadowStrength`
- `BaseTileMap.heightLightMod`
- `BaseTileMap.lightLimit`
- `BaseTileMap.currentHeight`
- `BaseTileMap.showRoof`
- `BaseTileMap.currentLot`
- `BaseTileMap.lotLight`, `BaseTileMap.lotLight2`
- `BaseTileMap.darkenOuter`, `BaseTileMap.fogBounds`
- `BaseTileMap.snowLight`, `snowColor`, `snowColor2`, `snowLimit`

これらの多くは `BaseTileMap` の public field か `Cell` の public state として既に読める。

### 3. Elin 本体は wall/object と floor で light を分けている

`BaseTileMap` は `param.color` に相当する `blockLight` と、床用の `floorLight` を別に持つ。

- `blockLight`: 壁、object、多くの upright 表現の基本
- `floorLight`: 床、雪床、watered floor、未視認床、roof edge の darkening を含む

つまり FPS 側も「1 個の light で全部塗る」では足りない。

### 4. effect の近似光は既に本体 API がある

`EffectIRenderer.OnUpdate()` は `EMono.scene.screenElin.tileMap.GetApproximateBlocklight(from.cell)` を使って effect の色を決めている。

これは FPS 側でも effect / transient sprite に使うべき公式寄りの近似経路。

### 5. packed light の decode は shader 依存なので別責務に切るべき

本体は `param.color` を packed int として `_Color` へ渡し、shader 側で最終色を作る。

- `BaseTileMap`: packed light を作る
- `RenderRow.SetImage()`: `matColor` 用の簡易 decode はある
- `_Color` の最終 decode は decompiled C# だけでは完全には追えていない

したがって FPS 側では、

1. `どの packed light を使うか`
2. `packed light を sampled color へどう適用するか`

を分離して設計する必要がある。

## Design Principles

1. light の truth は `Cell` と `BaseTileMap` から取る
2. terrain / wall / sprite で使う light 種別を分ける
3. `light resolution` と `light application` を分離する
4. 実装初期は per-cell cache を使い、per-pixel で `BaseTileMap` の式を回さない
5. effect は `GetApproximateBlocklight()` を優先し、独自 heuristic を避ける

## Proposed Model

### 1. New Lighting Context

`FpsLightingContext`

- `BaseTileMap TileMap`
- `float LightMod`
- `float BaseBrightness`
- `float[] LightLookup`
- `float ShadowStrength`
- `float HeightLightMod`
- `float LightLimit`
- `float SnowLight`
- `float SnowColor`
- `float SnowColor2`
- `float SnowLimit`
- `float ShadowModStrength`
- `float LotLight`
- `float LotLight2`
- `int CurrentHeight`
- `bool ShowRoof`
- `bool DarkenOuter`
- `bool FogBounds`
- `bool IsSnowCovered`
- `Lot CurrentLot`
- `Room CurrentRoom`

Rules:

- `FpsRenderer.RenderFrame()` の冒頭で 1 回だけ current `screenElin.tileMap` から snapshot を取る
- protected field は `AccessTools.FieldRef` で読む
- current frame 中は immutable として扱う

### 2. Per-Cell Light Cache

`FpsResolvedCellLighting`

- `int PackedBlockLight`
- `int PackedFloorLight`
- `int PackedApproxBlockLight`
- `bool Valid`

Cache key:

- `cell.index`
- 1 frame 単位で再構築

Rationale:

- lighting inputs は cell 単位で変わる
- terrain top / riser / wall / sprite のすべてが同じ cell light を再利用できる
- packed light を 1 回解いておけば、wall raycast でも terrain triangle でも sprite pass でも再利用できる

### 3. Surface To Light Mapping

#### Terrain top

- use `PackedFloorLight`

理由:

- snow floor
- watered floor
- unseen floor
- lot roof edge darkening

は `blockLight` ではなく `floorLight` 側に入っている。

#### Terrain riser

- use `PackedBlockLight`

理由:

- riser は terrain の side だが、見た目としては block side に近い
- `sourceFloor._defBlock` / `sourceBridge._bridgeBlock` を使う current 設計とも一致する

#### Solid wall / fence

- use `PackedBlockLight`
- fence は後で本体同様に `floorLight - shadow` へ分岐可能

#### Upright billboard

Phase 1:

- use `PackedApproxBlockLight`

Phase 2:

- tall object / installed object は `PackedBlockLight`
- chara / mobile card は anchor cell の `PackedApproxBlockLight`

理由:

- object 系は wall/object 側に寄る
- chara や移動 effect は本体でも `GetApproximateBlocklight()` が使われる経路がある
- まずは接地と遮蔽を壊さず light を載せることを優先する

#### Ground sprite

- use `PackedFloorLight`

理由:

- dropped item / low prop は terrain の上に置かれており、床と同じ light を受ける方が自然

#### Effect billboard

- use `TileMap.GetApproximateBlocklight(cell)`

理由:

- 既に `EffectIRenderer` がそうしている

### 4. CPU Lighting Resolver

新クラス:

- `FpsLightingResolver`

主要メソッド:

- `CaptureContext(BaseTileMap tileMap)`
- `ResolveCellLighting(Cell cell, in FpsLightingContext context)`
- `ResolveApproxBlockLight(Cell cell, BaseTileMap tileMap)`

### Phase 1 Resolution Strategy

#### A. Approx block light

- `tileMap.GetApproximateBlocklight(cell)` をそのまま使う

#### B. Exact block / floor light

`BaseTileMap` 972-1088 付近の式を CPU 側へ port する。

対象ロジック:

- `pcSync` による `light` 切替
- `lightLookUp` と `_lightMod`
- `currentHeight` による高低差 penalty
- `Cell.effect.IsFire`
- `lightLimit`
- `shadowModStrength * cell.shadowMod`
- `lotLight / lotLight2`
- `outOfBounds + darkenOuter`
- `snow / snow-covered`
- `self-shadow / unseen / roof edge`
- `isWatered`

非対象:

- `floorLight2`
- liquid 専用 pass
- roof 専用 pass

これで「火に照らされる地面」と「雪床/水やり床の別扱い」までは揃う。

### 5. CPU Light Application

新 helper:

- `FpsLightApplicator`

主要メソッド:

- `ApplyPackedLight(Color32 sampled, int packedLight)`

### Important separation

- `FpsLightingResolver`: packed int を決める
- `FpsLightApplicator`: packed int を sampled color に適用する

この分離により、shader との差分が出ても decode 側だけ調整できる。

### Initial application order

1. atlas sample
2. material tint
3. packed light application
4. distance shading

`distance shading` は本体由来ではないため、light 適用後の最後に残す。

## Integration Plan

### Step 1. Add packed light fields to resolved surfaces

- `FpsResolvedFloorSurface.PackedLight`
- `FpsResolvedWallSurface.PackedLight`
- `FpsResolvedUprightSprite.PackedLight`
- `FpsResolvedGroundSprite.PackedLight`
- `FpsResolvedEffectSprite.PackedLight`

### Step 2. Resolve lighting in `FpsIdealizedWorld`

- `TryResolveFloor()` で `PackedFloorLight`
- `TryResolveWall()` / `TryResolveTerrainRiser()` で `PackedBlockLight`
- sprite gather 時に surface kind に応じた packed light を付与

### Step 3. Apply light in `FpsRenderer`

- `SampleWallColor()`
- `TrySampleFloorComposite()`
- `TrySampleTerrainSurface()`
- `RenderGroundSprites()`
- `RenderUprightSprites()`
- `RenderEffectSprites()`

すべて `ApplyMatTint()` の直後に `ApplyPackedLight()` を通す。

### Step 4. Remove current ad-hoc distance-only look from validation path

debug option:

- `EnableDistanceShading`
- `EnableWorldLighting`

これを分離して、まず world lighting だけの比較ができるようにする。

## Validation Plan

### Scene cases to verify

1. campfire の近くで床が赤くなる
2. torch / lamp の近くで wall と terrain riser が暖色になる
3. snow field で floor と wall の色味差が出る
4. watered farm tile が周囲より暗くなる
5. room edge / roof edge で床が暗くなる
6. smoke / fire effect が本体 2D 側と近い明るさになる

### Comparison method

- 同じ場所で 2D view と FPS view を交互に切り替える
- first pass は hue / brightness の傾向一致を確認
- exact shader match は第二段階

## Risks And Open Questions

### 1. `_Color` の exact decode はまだ不明

現時点では packed light の encode 側は確定できているが、shader 側の final mix は未確認。

対策:

- resolver と applicator を分離する
- first pass は視覚一致優先で実装し、必要なら shader 資産を追加調査する

### 2. FPS camera と 2D tileMap の `currentHeight/currentLot/showRoof` がずれる可能性

本体の `BaseTileMap` は player-centric な screen state を持つ。FPS も現状は player 視点 overlay なので、まずは tileMap state をそのまま truth として使う。

### 3. sprite は multi-cell light interpolation しない

初期版では anchor cell の light のみ使う。大きい tree や長い wall decoration で light の切れ目が出る可能性はあるが、まずは単純化を優先する。

## Recommended Implementation Order

1. `FpsLightingResolver` と `FpsLightApplicator` を追加
2. `GetApproximateBlocklight()` を effect / upright sprite に適用
3. terrain top / wall / riser に packed light を追加
4. `floorLight` の watered / unseen / roof edge 差分を port
5. 必要なら `_Color` decode を追加調査して tune
