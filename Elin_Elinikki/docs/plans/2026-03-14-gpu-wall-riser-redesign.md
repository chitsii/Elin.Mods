# GPU Wall / Riser Redesign

## 背景

現状の GPU path の壁・full block・段差側面は、次の 3 つが混ざっている。

1. world face の向き
2. `blockDir` による tile の向き
3. isometric block tile 内の `top / left / right` face の意味

この 3 つを renderer 側で都度 `dir -> faceKind -> flipX` に変換しているため、修正箇所が増えるたびに別の面が壊れる。

## 現状の主問題

### 1. face の意味と world 方向が密結合

- `ResolveDirectionalQuadTransform()`
- `ResolveBlockFaceSelection()`
- `AddTerrainRiserQuad()`
- `AddFullBlockCornerQuads()`

がそれぞれ別の前提で方向を解釈している。

### 2. top / side が別 mesh で組まれている

top と side が shared edge を持っておらず、`epsilon` や center のズレで隙間が出やすい。

### 3. riser と full block が同じ face 選択ロジックを共有している

両者は見た目の起源は似ていても、geometry の責務が違う。

- full block: 「箱の visible corner を作る」
- riser: 「地形 edge の 1 面だけを作る」

ここを同じ `worldDir -> faceKind` で済ませると破綻しやすい。

## 新設計の方針

### 原則 1: renderer は face 種別を推測しない

renderer 側では `left/right/top` を推測しない。

代わりに idealization 層で

- どの geometry を出すか
- その geometry の各面が `top / left / right / panel` のどれか

を確定して渡す。

### 原則 2: geometry は edge / corner から直接組む

`dir + center + rotation + scale` ではなく、面を world vertex 4 点で定義する。

これにより:

- top と side が同じ edge を共有できる
- riser は CPU の `RenderTerrainRiser()` と同じ corner 定義をそのまま使える
- `0.49` や `epsilon` のような ad-hoc center 調整が不要になる

### 原則 3: full block と riser を分ける

#### Full block

- 1 cell = 1 local cuboid
- camera に対して visible corner を 1 つ選ぶ
- その corner に接する 2 side + top の 3 面だけを出す
- geometry は 1 つの shared-vertex mesh で持つ

#### Riser

- 1 edge = 1 vertical quad
- CPU の `RenderTerrainRiser()` と同じ 4 corner を使う
- `top / left / right` のうちどの face texture を貼るかだけを決める
- full block 用の visible-corner ロジックは使わない

### 原則 4: tile orientation と face extraction を分離する

1. `FpsIdealizedWorld` で `cell.blockDir` 反映済みの oriented tile を解決する
2. `FpsGpuSpriteTextureCache` で oriented tile から `Top/Left/Right` を抽出する
3. renderer は face texture を geometry face に貼るだけにする

つまり renderer は `flipX` や `relativeDir` を持たない。

## 推奨データモデル

### `FpsResolvedBlockFaces`

```csharp
internal readonly struct FpsResolvedBlockFaces
{
    public Texture Top;
    public Texture Left;
    public Texture Right;
    public Color Tint;
}
```

### `GpuFaceQuad`

```csharp
internal readonly struct GpuFaceQuad
{
    public Vector3 V0;
    public Vector3 V1;
    public Vector3 V2;
    public Vector3 V3;
    public Texture Texture;
    public Color Tint;
}
```

### `GpuBlockMeshPlan`

```csharp
internal readonly struct GpuBlockMeshPlan
{
    public GpuFaceQuad Top;
    public GpuFaceQuad SideA;
    public GpuFaceQuad SideB;
}
```

## 実装方針

### Step 1. face texture の責務を cache 層へ寄せる

`FpsGpuSpriteTextureCache`

- 入力: `FpsResolvedWallSurface`
- 出力: `FpsResolvedBlockFaces`

ここで `Top/Left/Right` を確定する。
renderer 側で `flipX` は持たない。

### Step 2. full block の geometry builder を新設

`FpsGpuBlockGeometryBuilder`

- 入力:
  - `cellX`, `cellZ`
  - `bottom`, `top`
  - `visibleCorner`
- 出力:
  - `GpuBlockMeshPlan`

corner は次の 4 値だけにする。

- `SouthWest`
- `SouthEast`
- `NorthWest`
- `NorthEast`

top / side の頂点はこの corner と cell bounds から直接作る。

### Step 3. riser の geometry builder を新設

`FpsGpuRiserGeometryBuilder`

- 入力:
  - `cellX`, `cellZ`
  - `edge`
  - `bottomHeight`, `topHeight`
- 出力:
  - `GpuFaceQuad`

CPU の `RenderTerrainRiser()` と同じ 4 corner をそのまま使う。

### Step 4. wall/fence は panel builder として分離

`TileMapElona` の `WallOrFence` 分岐に近づける。

- `blockDir == 0 || 2` の panel
- `blockDir == 1 || 2` の panel

corner wall の追加 panel も、この builder の責務に入れる。

### Step 5. renderer から次を削除

- `ResolveDirectionalQuadTransform()`
- `ResolveBlockFaceSelection()`
- `flipX` による面合わせ
- `center + rotation + scale` から面を作る処理

## 禁止事項

- worldDir から `left/right` をその場で推測しない
- full block と riser で同じ face selection helper を使わない
- top と side を別々の non-shared geometry として扱わない
- `0.49f` / `0.001f` のような center 補正で面合わせしない

## 実装順

1. `FpsGpuSpriteTextureCache` に `ResolveBlockFaces()` を追加
2. `FpsGpuBlockGeometryBuilder` を追加
3. full block を shared-vertex 3-face mesh に置換
4. `FpsGpuRiserGeometryBuilder` を追加
5. riser を edge quad へ置換
6. wall/fence panel builder を分離
7. 旧 `dir/flipX/rotation` ベース経路を削除

## 完了条件

- 木箱の top / 2 side が同じ角で接続する
- 段差側面が全方角で消えない
- `ResolveDirectionalQuadTransform()` 系の推測ロジックが削除される
- `riser` と `full block` が別 builder を使う
