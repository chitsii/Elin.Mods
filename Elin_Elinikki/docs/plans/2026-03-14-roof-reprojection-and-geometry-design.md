# FPS View Roof Reprojection And Geometry Design

## Goal

Elin 本体の lot 単位の屋根描画を、FPS/TPS 側でも 3D 面として再構成する。

屋根は block や wall-mounted object のような単一 panel ではなく、

- lot の大きさ
- `RoofStyle`
- `reverse`
- `wing`
- `flatW`

によって動的に形が変わる。

そのため、FPS 側でも「roof tile を 1 枚正面化する」のではなく、
`Lot + RoofStyle` から roof face 群を生成する必要がある。

## Truth Sources

### Geometry truth

- `BaseTileMap.DrawRoof(Lot lot)`
- `BaseTileMap.SetRoofHeight(...)`
- `RoofStyle`

参照:

- [BaseTileMap.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/BaseTileMap.cs)
- [RoofStyle.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/RoofStyle.cs)

### Visibility truth

- `showRoof`
- `showFullWall`
- `noRoofMode`
- `currentRoom`
- `hideRoomFog`

FPS 側でも、屋根を常に出すのではなく、本体の roof 表示状態に寄せる。

ただし FPS の最終要件は「屋外でも屋内でも、屋根が存在する空間として破綻なく見えること」であり、
2D クォータービューの `showRoof` をそのまま mirror することではない。

### Material / color truth

- roof tile がある場合: `lot.idRoofTile` と `lot.colRoof`
- roof block / ramp: `lot.idBlock`, `lot.idRamp`, `lot.colBlock`
- roof light: `GetRoofLight(lot)`

Phase 1 の既定表示は texture 再現ではなく neutral gray solid とする。
roof texture の reprojection 候補は設定で切り替えて比較し、最終採用を後で決める。

屋根 texture の source tile をどう切り出すかの大方針は、
[2026-03-15-roof-texture-source-slicing-memo.md](C:/Users/tishi/programming/elin_modding/Elin.Mods/Elin_Elinikki/docs/plans/2026-03-15-roof-texture-source-slicing-memo.md)
を参照する。

## Problem

現在の FPS GPU path には roof pass が存在しない。

- `card.isRoofItem` は収集前に除外している
- `cell._roofBlock` / `lot.idRoofStyle` は見ていない

そのため、

- 屋根本体が出ない
- 屋根上の設置物も出ない

状態になっている。

## Design Summary

屋根は `Lot` 単位で face 群へ分解する。

FPS 側では次の 2 段で扱う。

1. `RoofPlane` 群の生成
2. 各 plane に roof tile / roof block / ramp texture を貼る

ただし Phase 1 では shape review を優先し、`SolidGray` を既定にする。
`RenderTile` 屋根は候補モードごとに別の reprojection を掛けて比較できるようにする。

## Core Model

### A. Roof is a lot-level structure

屋根の描画単位は `cell` ではなく `lot`。

1 lot から複数の roof plane を作る。

### B. Roof uses 3D planes, not billboards

屋根は billboard でも wall-mounted でもない。

- `Top`
- `SlopeLeft`
- `SlopeRight`
- `Edge`
- `Ramp`

の plane 群として組む。

### C. Roof texture reprojection reuses block face logic

roof の各面は block と同じ考え方で扱う。

- 上面: roof top / flat top 用 texture を top face に貼る
- 傾斜面 / 側面: block side と同じ「1 面 -> 長方形」逆写像を使う

wall-mounted の shear 補正は使わない。

ただし、Phase 1 では `RenderTile` slope の候補比較用に fixed shear も許可する。
これは最終方式を決めるまでの比較経路であり、既定モードでは使わない。

### D. Indoor and outdoor are rendered differently

FPS 側では roof を 2 種類に分ける。

- `ExteriorRoofPlane`
  - 屋外から見える屋根外殻
  - `Top / Slope / Edge / Ramp`
- `InteriorCeilingPlane`
  - 現在の room / lot に入った時に見える天井表現
  - 最初は flat ceiling として近似してよい

目的は Elin と完全一致ではなく、
「屋外では屋根が建物の上に見え、屋内では頭上に屋根があると分かる」こと。

## Roof texture candidate policy

Phase 1 の roof texture は `RenderTile` slope だけを比較対象にする。

- `SolidGray`
  - 既定
  - roof 全面を灰色ソリッドで表示
- `RawTile`
  - 元 tile をそのまま貼る
- `SlopeShearLow / Medium / High`
  - slope 面だけ fixed shear で再投影する候補
- `SlopeShearHighFlipped`
  - shear 方向違いの比較候補

`BlockFace` 屋根は既存の block face 変換を使い、候補比較の主対象にはしない。

## Data Model

### New intermediate type

```csharp
struct FpsResolvedRoofPlane
{
    public Lot Lot;
    public RoofPlaneKind Kind;
    public Vector3 Bl;
    public Vector3 Br;
    public Vector3 Tl;
    public Vector3 Tr;
    public RenderData RenderData;
    public int Tile;
    public int MaterialColor;
    public FpsResolvedLightSample Light;
}
```

### RoofPlaneKind

- `Top`
- `SlopeLeft`
- `SlopeRight`
- `Edge`
- `Ramp`
- `InteriorCeiling`

## Geometry Strategy

### 1. Flat / FlatFloor

形状:

- 上面を平面 slab として作る
- 必要なら外周だけ `Edge` を出す

利用:

- `RoofStyle.Type.Flat`
- `RoofStyle.Type.FlatFloor`

### 2. Default / DefaultNoTop

形状:

- 棟を中心に左右 2 枚の slope
- `Default` は top cap を持てる
- `DefaultNoTop` は slope のみ

利用:

- `RoofStyle.Type.Default`
- `RoofStyle.Type.DefaultNoTop`

### 3. Triangle

形状:

- 切妻より尖った三角断面
- 左右 slope を基本とする
- top plane は持たない

### 4. Ramp

本体の `lot.idRamp` がある場合は、
屋根面とは別に `Ramp` plane を作る。

### 5. Interior ceiling

屋内では、現在の `currentRoom?.lot` に対して ceiling plane を追加する。

- Phase 1 では slope underside を正確に再現しない
- 代わりに room/lot の矩形に沿った flat ceiling plane を使う
- ceiling 高さは roof band の最低面より少し下に置く

これで屋内でも「屋根がある」表現を維持する。

## Mapping From DrawRoof(lot)

`DrawRoof(lot)` の branch をそのまま写す。

重要な入力:

- `reverse`
- `roofStyle.type`
- `roofStyle.flatW`
- `roofStyle.wing`
- `lot.height`
- `lot.heightFix`
- `lot.fullblock`

重要な範囲変数:

- `num, num2, num3, num4`
  - lot の roof 描画範囲
- `num10, num11`
  - 棟の平坦部 / 傾斜切替位置
- `num13`
  - 高さ段数
- `num14`
  - 左右どちらの slope / edge を使うか

FPS 側では、これらを直接再現するのではなく、次の roof layout へ翻訳する。

### RoofLayout

```csharp
struct RoofLayout
{
    public int MinX;
    public int MinZ;
    public int MaxX;
    public int MaxZ;
    public bool Reverse;
    public RoofStyle.Type Type;
    public int FlatStart;
    public int FlatEnd;
    public int ExtraWing;
}
```

## Coverage Review

`DrawRoof(lot)` と `RoofStyle` を突き合わせた時点で、Phase 1/2 の境界は次のように固定する。

### Phase 1 で扱うもの

- `RoofStyle.Type.Default`
- `RoofStyle.Type.DefaultNoTop`
- `RoofStyle.Type.Flat`
- `RoofStyle.Type.FlatFloor` を `Flat` 近似として扱う
- `reverse`
- `flatW`
- `idRoofTile == 0` と `idRoofTile != 0` の両方
- `idRamp` / `idBlock`
- `lot.fullblock`
- indoor ceiling approximation

### Phase 2 へ回すもの

- `RoofStyle.Type.Triangle`
- `roofStyle.wing`
- `lot.altRoof`
- snow variant
- roof item
- slope underside の忠実再現

### 理由

`DrawRoof(lot)` の主要分岐は `Default / DefaultNoTop / Flat / FlatFloor / Triangle` であり、
一般的な住宅の大半は `Default / DefaultNoTop / Flat` で説明できる。

逆に `FlatFloor / Triangle / wing / altRoof / snow` は、
形状・tile 選択・境界条件の追加分岐が多く、Phase 1 の geometry builder を不必要に複雑にする。

## Geometry Translation Rules

### 1. Layout axis

- `reverse == false`
  - ridge 軸は lot の `x` 側
  - slope は `z` 方向へ下る
- `reverse == true`
  - ridge 軸は lot の `z` 側
  - slope は `x` 方向へ下る

FPS 側では、これは「回転」ではなく `RoofLayout` の主軸を入れ替える処理として扱う。

### 2. Flat section

`num10` と `num11` は「平坦部の開始/終了」であり、FPS 側ではそのまま

- `FlatStart`
- `FlatEnd`

として保持する。`Default` 系では ridge cap、`Flat` 系では上面 slab の範囲になる。

### 3. Slope bands

`num13` と `num14` は、現在の cell が

- slope 上か
- flat 上か
- edge / cap 上か

を切り替える境界として使われている。

FPS 側ではこれを cell ごとの判定へ直接持ち込まず、

- 左 slope band
- 右 slope band
- flat band

の 3 帯へ先に分解し、各帯から `RoofPlane` をまとめて作る。

`DrawRoof(lot)` の更新規則は次の意味で固定する。

- `num14 = 0`: 上り slope
- `num14 = 1`: 平坦部 / 高さ維持
- `num14 = 2`: 下り slope

そして `num13` は現在の band 高さ段であり、行ループの最後に

- 上りでは `+1`
- 平坦では `+0`
- 下りでは `-1`

される。FPS 側ではこれを cell 単位で追従するのではなく、

1. 上り帯
2. 平坦帯
3. 下り帯

へ正規化し、それぞれを大きい `RoofPlane` にまとめる。

### 4. Ramp and side selection

- `lot.idRamp` は roof plane そのものではなく、傾斜部の side/ramp texture source
- `lot.idBlock` は roof tile が無い時の fallback source

つまり geometry は `RoofStyle` で決まり、`idRamp/idBlock` は texture source としてのみ使う。

## Per-Type Builder Rules

### Default

- 左右 2 枚の slope plane を作る
- 端面は lot 外周の長方形 edge ではなく、ridge に収束する 2 枚の `gable face` で閉じる
- `flatW > 0` の場合だけ ridge top plane を追加
- `flatW == 0` の場合は ridge top plane を作らず、2 slope を ridge line で接続する

### DefaultNoTop

- 左右 2 枚の slope plane を作る
- ridge top plane は作らない
- 端面は `gable face` で閉じる
- `flatW` は slope 切替位置だけに使う

### Flat

- lot 全体に top slab を作る
- 外周だけ edge plane を追加する
- slope plane は作らない

### FlatFloor

Phase 1 では `Flat` 近似。

理由:

- `DrawRoof(lot)` 側でも `FlatFloor` は top tile と side tile の選択が通常 `Flat` と異なる
- floor/light/snow ルールも別扱いになる

### Triangle

Phase 2。

理由:

- `DrawRoof(lot)` 側で ridge / flat 判定が `Default` と異なる
- 断面が鋭く、単純な `DefaultNoTop` 近似だと silhouette が変わる

## What Is Still Not Fully Closed

ここまでで「Phase 1 を実装するための骨格」は固まっているが、次は実装前に以下を追加で固定する必要がある。

1. `RoofPlaneKind.Edge` を lot 外周 4 辺へどう分配するか
2. `GetRoofLight(lot)` の light sample を plane 単位へどう適用するか
3. `Flat` 系の `flag2` による cell 単位の side choice を、plane 結合時にどこまで忠実に再現するか

`SetRoofHeight(...)` については、今回の再調査で位置付けを確定できた。

- `DrawRoof(lot)`: roof 面そのものの silhouette truth
- `SetRoofHeight(...)`: roof 面に付随する block / wall / object の anchor truth

なので、Phase 1 の roof geometry 実装では `SetRoofHeight(...)` を plane 生成に直接使わず、
roof 面の高さ決定は `DrawRoof(lot)` 側の段構造を truth にする。

## Phase Plan

### Phase 1

対象:

- `Default`
- `DefaultNoTop`
- `Flat`

除外:

- `Triangle`
- `wing`
- snow variant
- `altRoof`

狙い:

- まず一般的な家屋の屋根を出す
- lot 全体が roof として見える状態を作る
- `DrawRoof(lot)` の主要分岐を、例外の少ない形で first pass 実装する

### Phase 2

対象追加:

- `Triangle`
- `FlatFloor`
- `wing`
- snow
- `altRoof`
- roof item

## Placement Rules

### Base height

roof plane の基準高さは

- `lot.mh`
- `lot.realHeight`
- `roofFix`
- `roofFix2`
- `roofFix3`
- `roofStyle.posFix` / `posFixBlock`
- `roofStyle.lowRoofFix`

を本体どおりに使う。

具体的には、本体 `DrawRoof(lot)` の

```csharp
float baseY =
    (cz - cx) * tileAlign.y
  + lot.mh * _heightMod.y
  + lot.realHeight
  + roofFix.y
  + vector.y;
```

と

```csharp
param.y = baseY + num13 * roofFix2.y;
```

を、FPS 側では次の world-space 意味へ翻訳する。

- `lot.mh + lot.realHeight`: lot 全体の roof 基準高さ
- `vector.y`: `fullblock` に応じた roof style オフセット
- `num13 * roofFix2.y`: 上り/下り帯の段差
- `lowRoofFix.y`: `lot.height == 1` の低屋根補正

ただし FPS 側では、クォータービューの `roofFix2.y` をそのまま world step に使うと
低い視点から屋根勾配が浅すぎて silhouette が潰れる。
このため、Phase 1 は「本体の段構造を truth にするが、step 量は FPS 視認性のために誇張する」。

つまり Phase 1 の roof 面高さ truth は

```text
roofBaseHeight =
    lot.mh
  + lot.realHeight
  + stylePosFixY(fullblock)
  + lowRoofFixY(if lot.height == 1)

roofBandHeight(step) =
    roofBaseHeight + step * exaggeratedRoofStep
```

ここで `exaggeratedRoofStep` は、

- `abs(roofFix2.y) * terrainHeightScale`
- ただし一定の最低勾配を持つ

とする。目的は Elin と完全一致させることではなく、
FPS 視点で「屋根として見える silhouette」を確保すること。

とする。

一方で `SetRoofHeight(...)` は roof 面そのものではなく、

- 屋根上 block
- 屋根縁 wall
- roof 付随 object

の anchor truth としてだけ使う。

### Indoor ceiling height

Phase 1 の indoor ceiling は、現在 lot の roof band 群のうち

- 最低の `Top/Slope` 面

からわずかに下げた高さへ置く。

理由:

- 壁上端と視覚的に噛み合う
- roof 面そのものと z-fighting しない
- 室内から見た時に「頭上に天井がある」ことを保証できる

### Reverse

`reverse` は見た目回転ではなく、ridge 軸そのものを入れ替える。

FPS 側でも plane の向きを切り替える。

### Wing

`wing` は eave / 張り出しとして追加面を作る。

最初は Phase 2 に回す。

## Texture Rules

### Roof tile path

`lot.idRoofTile != 0` の場合:

- roof tile を primary とする
- 上面だけでなく斜面も roof tile 側の slope 用 tile を優先する
- roof tile 側で面を取れない場合だけ `idRamp / idBlock` の block face へ fallback する

### Block fallback path

roof tile が無い場合:

- `lot.idBlock` の block / floor を使う
- block side reprojection を利用する

### Ramp

- `lot.idRamp` の block renderData を使う

## Rendering Order

順序:

1. terrain top
2. riser / wall / block
3. exterior roof
4. interior ceiling
5. wall-mounted object
6. ground sprite
7. upright sprite

理由:

- 屋根は地形 / 壁の上に乗る
- 室内 ceiling は wall-mounted より先に置く
- sprite よりは後ろに置く

## Debug Plan

最初の実装では次をログへ出す。

- `GPU roof lot[...]`
  - `lot id`
  - `roofStyle`
  - `reverse`
  - `flatW`
  - `wing`
  - `idRoofTile`
  - `idBlock`
  - `idRamp`
- `GPU roof plane[...]`
  - `kind`
  - `tile`
  - `bl/br/tl/tr`

必要なら roof plane を単色 debug でも見られるようにする。

## Rejected / Failed Approaches

- `card.isRoofItem` を戻すだけで屋根を出そうとする
  - 屋根本体は出ない
- 本体 `showRoof` をそのまま mirror して、室内では屋根を消す
  - FPS では頭上の閉塞感が失われる
- roof を wall panel の shear 補正で済ませる
  - lot 単位の形状変化を再現できない
- roof を full block の積み重ねで近似する
  - `RoofStyle` の ridge / flat / reverse / wing が失われる

## Next Step

1. `Triangle` / `wing` / `altRoof` を Phase 2 builder として追加
2. roof plane の light / fog / visibility を実機で詰める
3. slope / edge texture の repeat と trim を必要なら見直す
