# Roof Texture Source Slicing Memo

## Purpose

屋根テクスチャの扱いを、当面の大方針として整理する。

これは詳細実装仕様ではなく、

- source tile を何として見るか
- どの面へどう使い回すか
- どこを比較対象にするか

を固定するためのメモである。

## Core Idea

単純化した三角柱ベースの屋根形状に対して、
`block tiles` の source 画像をそのまま貼るのではなく、
1 枚の source tile から複数の屋根面用 texture を切り出して使う。

例:

- `lot_0_kind_Top_tile_41_face_Top.png`

この source を、少なくとも次の 2 種類へ分けて使う。

1. 傾斜面用 texture
   - 最終的には長方形面へ貼る
   - 切妻屋根の左右の slope 用
2. 妻面用 texture
   - 最終的には三角形面へ貼る
   - 切妻屋根の前後 end cap 用
3. 屋内側面用 texture
   - 当面は傾斜面用の長方形 texture を流用する
   - 専用の underside/ceiling source はまだ要求しない

要するに、
`Top` source tile を「屋根全体の元画像」とみなし、
そこから「長方形面用」と「三角面用」の 2 系統を切り出す。

## What This Replaces

次の考え方は当面やめる。

- source tile 全体をそのまま slope 面へ貼る
- wall-mounted 用の shear を屋根へ流用する
- `Edge` 用 tile を先に決めてから屋根面を組む

まずは屋根の 3D 形状を単純な三角柱/平屋根として安定させ、
その形状に対して source tile のどこを使うかを後から決める。

屋内に面する roof face は、専用 texture が無い限り
「傾斜面用の長方形 texture を流用する」を暫定ルールとする。

## Source Hierarchy

当面の優先順位は次の通り。

1. `kind=Top` の source tile
   - 傾斜面用 texture
   - 妻面用 texture
2. 必要なら `kind=Edge` の source tile
   - 補助比較用
   - primary にはしない

つまり、屋根 texture の primary source はまず `Top` とする。

## Primary Source Decision

Phase 1 では、primary source は面ごとに変えず、
`lot` ごとに 1 つ決める。

判定順は次の通り。

1. `lot.idRoofTile` が有効
2. 対応する `SourceObj.Row` の `renderData` が有効
3. `reverse` を考慮して代表となる roof tile を 1 枚選ぶ
4. その 1 枚を `RoofTopPrimary` とする

この `RoofTopPrimary` から、

- 傾斜面用の長方形 texture
- 妻面用の三角形 texture
- 屋内側面用の長方形 texture

を切り出す。

水平な `Top` 面や小さい ridge cap は、
Phase 1 では source tile 全体をそのまま使ってよい。

つまり、Phase 1 では
`Top` / `Edge` / `Slope` の geometry kind に応じて source tile を切り替えない。
先に primary source を 1 枚決めて、それを各面へ使い回す。

`lot.idRoofTile` が無効な場合だけ、primary source なしとして
block fallback を使う。

## Slice And Projection Are Separate Steps

屋根 texture は、1 回で最終形へ作るのではなく、
次の 2 段で扱う。

1. `slice`
   - source tile のどこを使うか
2. `projection`
   - 切り出した patch を、最終的に貼る正面 shape へ起こす

この 2 つは別の調整軸とする。

## Slice Step

`RoofTopPrimary` の source tile 上に、共有境界を持つ 2 patch を定義する。

- `RectSlice`
  - 傾斜面用
  - source 上の四角 patch
- `TriSlice`
  - 妻面用
  - source 上の三角 patch

両者は source 上の同じ境界線を共有する。

屋内側面は、当面 `RectSlice` を流用する。

## Projection Step

切り出した patch は、そのまま貼らず正面化する。

- `RectSlice`
  - 正面長方形へ逆写像する
  - 最終的に slope 面へ貼る
- `TriSlice`
  - 正面三角形へ逆写像する
  - texture 自体は四角 canvas に保持し、
    三角形外側は透明にする

つまり、比較対象は

- source のどこを切るか
- 切った patch をどう正面化するか

の 2 つに分かれる。

## Comparison Workflow

最初に詰めるべきなのは変換式そのものではなく、切り出し位置である。

そのため比較は次の順で行う。

1. source tile を dump する
2. 傾斜面用の切り出し候補を複数作る
3. 妻面用の切り出し候補を複数作る
4. ゲーム内で一番 fit する組み合わせを選ぶ
5. その後で必要なら shear / scale / trim を詰める

## Non-Goals

このメモの段階では、次は決めない。

- 最終的な UV 数式
- trim の閾値
- lighting bake の方法
- snow / altRoof の扱い
- `RenderTile` 屋根との統一ルール

それらは別の詳細設計または実装中レビューで詰める。

## Initial Scope

この方針をまず適用するのは、Phase 1 の単純屋根だけ。

- `Default`
- `DefaultNoTop`
- `Flat`

`Triangle`、`wing`、`altRoof`、`snow` は後段で扱う。
