# FPS View Sprite Sizing And Color Design

## Goal

GPU preview path の sprite / object 表示について、次の 2 点を事実ベースで固定する。

1. world size の truth を何に置くか
2. sprite color をどこまで元画像そのまま扱うか

今回の結論は A 案を採用すること、つまり `Sprite` と `RenderData` の両方で「元画像相対サイズ」を size truth にすることだ。

## Question

検討した案は次の 2 つだった。

- A: すべて「元画像ピクセルサイズ」を相対的な truth にする
- B: `RenderData` 系だけ `renderData.size` を truth にする

論点は `renderData.size` が本当に「見た目サイズ」の truth か、それとも別用途の論理値か、だった。

## Decompiled Findings

### 1. `RenderData.size` は serialized field だが、描画サイズそのものには使われていない

`RenderData` には `Vector2 size` が serialized field として保持されている。[RenderData.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/RenderData.cs#L30)

しかし `RenderData.Draw()` は `size` を見ず、`offset`、`pass`、`tile`、`matrices` を使って描画している。[RenderData.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/RenderData.cs#L116)

つまり `size` は draw call 上の見た目ピクセルサイズではない。

### 2. UI / sprite の見た目サイズは `SetNativeSize()` と `imageScale` が担っている

`RenderRow.SetImage()` は `image.SetNativeSize()` のあと、必要に応じて `imageScale` と `pref.scaleIcon` を掛ける。[RenderRow.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/RenderRow.cs#L383)

ここでも `renderData.size` は使われていない。

つまり UI / sprite 表示における見た目サイズの truth は `Sprite.rect` と `imageScale` であり、`renderData.size` ではない。

### 3. `renderData.size` は logical center / top / head-height 用の論理寸法である

`renderData.size` の参照先は主に次だった。

- 中心点計算: [CardRenderer.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/CardRenderer.cs#L33)
- 頭上アイコン高さ: [TCOrbitThing.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/TCOrbitThing.cs#L16), [TCOrbitChara.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/TCOrbitChara.cs#L34)
- target marker 高さ: [Scene.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/Scene.cs#L528)
- 倒れた PCC の積み上げ量: [BaseTileMap.cs](C:/Users/tishi/programming/elin_modding/Elin-Decompiled/Elin/BaseTileMap.cs#L1712)

これは `size` が「元画像サイズ」ではなく、「論理的な中心・頭頂・高さ」のために設計された値であることを示す。

### 4. `RenderData` 系でも size truth を `renderData.size` にする根拠は弱い

`RenderData` 系 object は atlas tile から切り出すが、その切り出しサイズや bake サイズは実装依存で変わりうる。一方で、今回ユーザが求めているのは「元画像相対サイズをそのまま使う」ことであり、これは authored logical size ではなく visual source size を truth にする方針である。

したがって、`renderData.size` を scale truth に採用する理由は薄い。`renderData.size` は scale ではなく anchor 補助へ限定して使うのが調査結果と整合する。

## Design Decision

結論は A 案を全面採用する。

### Rule 1: world size truth は source image pixel size で統一する

対象:

- `Sprite` source
- `RenderData` source

size rule:

- `Sprite` source:
  - `sprite.textureRect.width/height`
  - `imageScale`
- `RenderData` source:
  - atlas から切り出した元画像相当の pixel size
  - `renderData.imageScale`

を使う。

重要なのは、transparent pixel を含んだ元画像サイズをそのまま保持することだ。opaque bounds で自動縮小しない。固定 `64x64` / `64x128` への bake は texture cache の都合として許容しても、world size truth に使ってはいけない。

### Rule 2: `renderData.size` は scale truth に使わない

`renderData.size` は次の補助用途に限定する。

- grounding の論理補助
- center / top の補助
- head icon / target marker の高さ補助

world billboard の縦横 size を決める truth と混同しない。

### Rule 3: 色は raw atlas color を first truth にする

初期方針:

- sprite / render tile とも raw atlas color を優先する
- `colorMod` / `useAltColor` は明示的に必要な asset だけ opt-in で使う
- 木・草・植物など、元画像の色が見た目の主体であるものには無条件 tint を掛けない

lighting の最終明るさモデルは別問題として切り分ける。

### Rule 4: source type の違いは size truth ではなく extraction method の違いとして扱う

`Sprite` と `RenderData` の違いは「どこから pixel size を取るか」であって、「size truth を変える理由」ではない。

- `Sprite`:
  - `textureRect` から pixel size を取得
- `RenderData`:
  - atlas tile / extracted texture の native pixel size を取得

どちらも「元画像相対サイズ」を返す helper を用意し、その上で共通の `pixels -> world units` 変換を使う。

### Rule 5: grounding truth は opaque bottom を共通採用する

- upright object は source 種別に関係なく、不透明部分の下端を地面へ合わせる
- `renderData.imagePivot` や UI 用 pivot は world grounding の truth にしない
- `pivotY = 0` は opaque bottom へ接地するための初期値として扱う

## Why A Wins

A を採る理由は 3 つある。

1. ユーザ要求と一致する  
   透明部分を排除せず、元画像サイズのまま world に置きたい、という要求に直接対応する。

2. `renderData.size` の実際の用途と整合する  
   Decompiled code 上、`renderData.size` は見た目幅高さではなく logical anchor 用である。

3. source type 間の規則が一貫する  
   `Sprite` だけ A、`RenderData` だけ B にすると、asset 種別で size policy が変わり、調査・デバッグが難しくなる。

## Implementation Policy

### Size policy helper

`ResolveSourcePixelSize()` を source type ごとに分ける。

- `Sprite` source:
  - `textureRect.width/height`
- `RenderData` source:
  - atlas から切り出した native pixel size

その結果を共通の `PixelsToWorldUnits()` へ流す。

### Extraction policy helper

texture cache は「表示色」と「size truth」を分離する。

- texture extraction:
  - 色を取る責務
- source pixel size:
  - 元画像相対サイズを返す責務

texture cache が内部で fixed-size bake を持っても、別途 native source size を保持して world size を決める。

### Diagnostics to keep

size 問題の再発防止のため、次のログは維持する。

- `sourcePx`
- `renderData.size`
- `imageScale`
- `worldSize`
- `anchorY`

ただし `renderData.size` は anchor 補助値として読むのであって、scale truth と誤読しない。

## Immediate Changes Implied By This Design

1. `RenderData` 系 object の world size truth を `renderData.size * imageScale` から外す
2. `RenderData` texture cache は fixed-size bake と native source size を分離する
3. opaque bounds による自動拡大縮小をやめる
4. raw atlas color を優先し、tint は explicit opt-in にする
5. `renderData.size` は grounding / center / head-height の補助に限定する
6. upright object の grounding は opaque bottom を共通採用する

## Rejected / Failed Approaches

この問題では、過去に次の経路が実際にデグレを起こした。今後は再採用しないか、再採用する場合は明確な根拠が必要。

### 1. `RenderData.size * imageScale` を world size truth にする

結果:

- 木や植物が著しく小さくなる
- `Sprite` source と `RenderData` source で size policy が分裂する
- source type ごとに見た目の縮尺感がばらつく

理由:

`renderData.size` は visual source size ではなく logical anchor 用であり、見た目サイズの truth ではない。

### 2. opaque bounds を使って visible 部分だけで自動縮小/拡大する

結果:

- 小物が過大に見える
- 透明余白を含んだ authored size が失われる
- 「元画像の相対サイズをそのまま使う」という要求と衝突する

理由:

FPS 側の見やすさ補正としては機能しても、asset の authored size を壊す。

### 3. `RenderData` cache の fixed `64x64` / `64x128` bake を size truth に使う

結果:

- multiSize や atlas 実寸と無関係な world size になる
- cache 実装都合が見た目サイズへ漏れる

理由:

cache の内部表現と world size truth を混同している。

### 4. `cell.obj` で `colorMod` / `useAltColor` を一律で切る

結果:

- 木や object が白黒寄りになる
- 本体では material tint 前提の asset まで raw atlas 色だけで出てしまう

理由:

`cell.obj` の中には raw atlas 色主体の asset と material tint 前提の asset が混在している。一律無効化は成立しない。

### 5. UI 用の `imagePivot` を world grounding にそのまま流用する

結果:

- 木や草が浮く、または逆に沈む
- UI icon 用 pivot と world 接地が混ざって不安定になる

理由:

`imagePivot` は UI 表示用の pivot であり、world billboard の接地 truth ではない。grounding は bottom-anchor を基準に別途決める必要がある。

### 6. opaque bottom grounding を source type ごとに分ける

結果:

- `cell.obj` だけ直って `Sprite` / `RenderData` の他系統で浮きやめり込みが残る
- grounding ルールが分散して再発しやすい

理由:

接地 truth は `cell.obj` 固有ではなく、すべての upright object で共通化するべきである。

### 7. `HasGrowth` object の material tint を一律で切る

結果:

- 一部の木や植物で葉が白黒になる
- 成長段階の tile は合っていても、Elin 本体の葉色と一致しなくなる

理由:

Decompiled code 上、`TileMapElona` / `BaseTileMap` は `sourceObj.HasGrowth` でも先に `param.matColor` を設定し、その後 `cell.growth.OnRenderTileMap(param)` を呼ぶ。`GrowSystem.OnRenderTileMap()` は stage の `tile/renderData` と `pref.y/z`、shadow を変えるだけで、色は変えていない。つまり growth object も通常の `matColor` 経路に乗っており、`HasGrowth` を理由に tint を一律無効化してはいけない。

## Out Of Scope

この設計書は次を扱わない。

- lighting の最終明度モデル
- shadow / AO
- billboard の支持面解決
- camera-relative movement

これらは別設計で扱う。
