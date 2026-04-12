# Elin Elinikki Mod - 実装計画（2026-03-12 改訂）

## Context

Elin の 2D クォータービューマップを、CPU ソフトウェアレイキャスターで疑似 3D FPS 視点表示する Mod。ゲーム本体の進行・入力・戦闘・UI はできるだけそのまま維持し、表示だけを FPS オーバーレイへ切り替える。

## 現時点の結論

実現は可能。ただし、初稿にあった次の前提は修正が必要。

- 壁判定を `cell._block != 0` に単純化する
- `cell._blockMat` / `cell._floorMat` をテクスチャ選択の主キーにする
- `Texture2D.GetPixels32(x, y, w, h)` でタイル矩形を直接抜けるとみなす
- MVP からマウスルック前提で入力を横取りする

採用方針自体は維持する。

- Elin のランタイムデータを直接参照する
- Unity 3D カメラやメッシュ生成は使わない
- 2026-03-13 時点ではこの制約は撤回。性能上の理由がある場合は Unity の offscreen camera / mesh / billboard pool を使う。
- `Color32[]` を `Texture2D` へアップロードする全画面オーバーレイで表示する

## 確認済みの事実

### 1. マップとセル

```csharp
Map map = EClass._map;
Cell cell = map.cells[x, z];
Chara pc = EClass.pc;

cell._block;
cell._floor;
cell.height;
cell.HasFullBlock;
cell.HasWallOrFence;
cell.blockDir;
cell.floorDir;
cell.autotile;
cell.autotileBridge;
cell.shore;
cell.Charas;
cell.Things;
```

- `EClass._map.cells[x, z]` は直接参照できる。
- FPS の「遮蔽壁」判定は `_block != 0` では粗すぎる。
- MVP の衝突/遮蔽判定は `HasFullBlock || HasWallOrFence` を基準にする。
- `ramp` / `ladder` / `half-block` / `pillar` は別扱いが必要。
- 座標系は `x, z` を平面として扱えばよい。

### 2. テクスチャとタイル

- Elin の atlas は `IO.LoadPNG()` 経由でロードされ、実コード上は `GetPixels32()` 利用実績がある。
- ただし `Texture2D.GetPixels32()` に矩形切り出しオーバーロードはない。
- したがって atlas は「全体を 1 回だけ `GetPixels32()` で読む」前提でキャッシュし、タイル矩形はマネージド側で切り出す。
- CSV 上の `dir * 100 + position` 形式タイル値は、`RenderRow.SetTiles()` / `RenderData.ConvertTile()` で atlas index に正規化される。
- `SourceFloor.Row.GetTile()` / `SourceBlock.Row.GetTile()` は material ではなく `_tiles` を返す。
- `cell._blockMat` / `cell._floorMat` は主に `matColor` 用で、基本タイル選択そのものではない。
- `MeshPass` / `RenderData` には次の表現差分がある。
  - `TokenLiquid = 10000`
  - `TokenLowWall = 1000000`
  - `TokenLowWallDefault = 3000000`
  - `multiSize`
  - `snowPass`

### 3. 床の再現は壁より難しい

- 床描画は `cell._floor` だけで決まらない。
- 実描画では `floorDir` / `autotile` / `shore` / `bridge` / `snow` / `water` が関与する。
- 橋は `sourceBridge` / `autotileBridge` / `bridgeHeight` まで別系統で持っている。
- したがって、Phase 2 で壁と床を同時に「正確再現」する計画は分離する。

### 4. キャラ・アイテム・オブジェクト

- `Thing.SetRenderParam(RenderParam)` と `Chara.SetRenderParam(RenderParam)` は `p.tile` / `p.mat` / `p.matColor` を実ランタイム表現へ解決する。
- ビルボード描画用の取得経路は 2 本ある。
  - `Card.GetSprite()` + `Sprite.textureRect`
  - `SetRenderParam()` で得た tile/material 情報を atlas キャッシュへ通す
- `Chara.GetSprite(int dir)` は見た目上の「方向差分取得 API」とは言いづらい。
- 特に chara は `skin` / `uid` / `spriteReplacer` / `PCC` の分岐を持つため、Phase 3 以降で慎重に扱う。

### 5. 入力とオーバーレイ

- 全画面 `RawImage` は `raycastTarget = false` にしないと UI / クリックを阻害する。
- `F7` は Elin 側の debug/minigame 経路と衝突するためデフォルトキーに不向き。
- 「ゲームは通常通り動作し、表示だけ切り替える」を守るなら、MVP は入力横取りを避けるべき。

## 採用アーキテクチャ

### 基本方針

1. `Plugin` が永続 `GameObject` と `FpsViewManager` を生成する。
2. `FpsViewManager` はシーン常駐し、表示状態・解像度・FOV・カメラ状態を管理する。
3. `FpsRenderer` は CPU レイキャスターとして `Color32[]` を生成する。
4. `FpsOverlayDisplay` は `Texture2D.SetPixels32()` + `Apply()` で画面更新する。
5. MVP では Elin 本体の移動・戦闘・UI 操作を横取りしない。

### カメラ方針

- MVP のカメラ位置は `EClass.pc.pos` に追従する。
- MVP の yaw は `EClass.pc.dir` ベースの 8 方向で開始する。
- マウスルックは後段のオプション機能として分離する。
- まず「ゲーム入力を壊さない」「表示だけ FPS に変わる」を成立条件にする。

### Harmony 方針

- 可能なら常駐 `MonoBehaviour` とポーリングで済ませる。
- Harmony はライフサイクル検知や描画同期で必要になった時点で導入する。
- 導入時は `TargetMethod` 明示、fail-soft、`Postfix` 優先。

## 実装方針の修正

### 壁判定

- 初稿の `_block != 0` を廃止する。
- Phase 1 の壁ヒット条件は `HasFullBlock || HasWallOrFence`。
- `ramp` / `ladder` / `half-block` は当面「非完全壁」として除外または簡略表現へ倒す。

### タイルキャッシュ

- 初稿の `(sourceId, materialId) -> Color32[]` キャッシュは採用しない。
- 基本単位は「atlas 全体」と「正規化済み tile index」。
- 壁/床/橋/オブジェクト/キャラで参照元 pass が異なるため、キャッシュキーは少なくとも次を含む。
  - atlas 種別
  - tile index
  - snow 使用有無
  - multiSize 有無
  - 反転有無
- material はタイル選択キーではなく tint 計算へ使う。

### 床テクスチャ

- 壁と同じ難易度ではないのでフェーズを分離する。
- まずは `sourceFloor._tiles[floorDir]` / `sourceBridge._tiles[floorDir]` を使った単純床から始める。
- `autotile` / `shore` / `water` の完全再現は後段に回す。

### ビルボード

- chara は `GetSprite()` 直接利用を第一候補にする。
- item / installed object は `Card.GetSprite()` か `SetRenderParam()` ベースで実装する。
- `stack` / `mount height` / `hang` / `PCC` / `spriteReplacer` は段階導入する。

## 段階的実装プラン

### Phase 0: 土台の補強

- `src/FpsViewManager.cs` を追加
  - 常駐 manager の生成
  - 表示状態管理
  - zone / scene ready 判定
- `src/FpsOverlayDisplay.cs` を補強
  - `RawImage.raycastTarget = false`
  - 必要なら UI 透過設定整理
- config 項目を追加
  - トグルキー
  - 内部解像度
  - FOV
  - 目線高さ
- デフォルトトグルキーは `F9` 以降または設定式にする

検証:

1. `build.bat` 成功
2. ゲーム起動後、表示切替しても UI クリック・通常操作が阻害されない
3. zone 切替やタイトル復帰で null 参照を出さない

### Phase 1: 壁だけの表示安全 MVP

- `src/FpsRenderer.cs`
  - DDA レイキャスター
  - 壁ヒット条件は `HasFullBlock || HasWallOrFence`
  - 色付き壁 + 空 + 床のベタ塗り
  - 距離減衰と Z バッファ
- `FpsViewManager`
  - カメラ位置 = `EClass.pc.pos`
  - yaw = `EClass.pc.dir`
  - 可視状態の時だけ描画更新
- 入力横取りなし
  - Elin の移動・戦闘・UI は既存入力のまま

検証:

1. 拠点やダンジョンでトグル可能
2. プレイヤー移動に追従して壁シルエットが見える
3. UI 操作、クリック、通常プレイが継続できる

### Phase 2: 壁テクスチャ + material tint

- `src/AtlasPixelCache.cs`
  - atlas 全体の `Color32[]` を 1 回だけ読む
  - pass ごとの tile size を持つ
- `src/TileSampler.cs`
  - 正規化済み tile index からタイル矩形を切り出す
  - 反転、`multiSize`、token 正規化を扱う
- `FpsRenderer`
  - 壁 UV 計算
  - `cell.sourceBlock` + `cell.blockDir` から tile 選択
  - `cell.matBlock` と `colorMod` に応じた tint
- 初期対応対象
  - full block
  - wall / fence
  - low-wall 簡略版
- 後回し対象
  - pillar
  - half-block
  - ramp / stairs
  - block render mode 完全一致

検証:

1. 壁が Elin atlas の見た目になる
2. material 違いで tint が変わる
3. 低解像度でも破綻せず動く

### Phase 3: 床・橋・雪面

- まず単純床を追加
  - `sourceFloor._tiles[floorDir]`
  - `sourceBridge._tiles[floorDir]`
  - `FloorHeight` を反映
- 次に必要に応じて拡張
  - `autotile`
  - `autotileBridge`
  - `shore`
  - 水面/氷面/雪面差分

検証:

1. 基本床と橋がテクスチャ付きで見える
2. 雪マップで通常床と雪面が破綻しない
3. 水辺や橋で最低限の視覚破綻に収まる

### Phase 4: キャラ・アイテムのビルボード

- chara billboard
  - `Map.charas` 走査
  - 距離ソート
  - `Card.GetSprite()` または `SetRenderParam()` 利用
- thing billboard
  - installed / dropped item を段階対応
- Z バッファで壁の後ろをクリップ
- 簡易 vertical placement
  - `Pref.height`
  - `FloorHeight`
  - 基本的な設置高さ

後回し:

- stack 完全再現
- mount height
- hang object
- PCC / spriteReplacer の完全一致

検証:

1. NPC が壁の前後関係を保って描画される
2. 足元アイテムや設置物が最低限識別できる
3. 特殊スプライトで落ちても fail-soft に継続する

### Phase 5: 忠実度と操作性の向上

- 高低差 `cell.height` 反映
- ramp / stairs / half-block 表現
- block render mode 精度向上
- 自由視点 look mode
- FPS 中央レイと Elin インタラクションの対応付け
- ミニマップや視線補助 UI

## 追加調査が必要な項目

### 1. 自由視点の入力設計

- 表示だけ差し替える要件と、マウスルックは競合しやすい。
- 実装するなら「押している間だけ look mode」など、既存 UI と共存する仕様にする。

### 2. 低壁・柱・半ブロック

- Elin 側は token と render mode で表現している。
- FPS 側は専用高さ計算が要るため、MVP 範囲から外す。

### 3. 床の完全再現

- shoreline / autotile / water overlay まで合わせると BaseTileMap 相当のロジックが必要。
- 見た目要求に応じて「簡易再現で十分か」を見極める。

### 4. パフォーマンス実測

- DDA だけなら軽い見込みだが、支配コストは `SetPixels32` + `Apply` 側になりやすい。
- Phase 1 完了後に 320x180, 400x225, 640x360 で実測する。

## パフォーマンス前提

- レイ走査自体は現代 CPU で十分現実的。
- まずは `320x180` か `320x200` を初期解像度にする。
- 非表示時は描画しない。
- 必要なら将来次を検討する。
  - 変更がないフレームの再描画抑制
  - 内部解像度の設定化
  - 壁/床/ビルボードの段階描画

## 重要な参照ファイル

| ファイル | 役割 |
|---------|------|
| `Elin-Decompiled/Elin/Cell.cs` | `HasFullBlock`, `HasWallOrFence`, `blockDir`, `floorDir`, `autotile`, `shore` |
| `Elin-Decompiled/Elin/Map.cs` | `cells[,]`, `charas`, `things`, bounds |
| `Elin-Decompiled/Elin/RenderRow.cs` | `SetTiles()`, `GetSprite()` |
| `Elin-Decompiled/Elin/RenderData.cs` | `ConvertTile()`, `multiSize` |
| `Elin-Decompiled/Elin/RenderDataTile.cs` | floor/object の `snowPass`, `multiSize` |
| `Elin-Decompiled/Elin/BaseTileMap.cs` | floor / bridge / autotile / shore の実描画 |
| `Elin-Decompiled/Elin/TileMapElona.cs` | block render mode, thing/chara 描画位置 |
| `Elin-Decompiled/Elin/TextureManager.cs` | atlas と MeshPass の対応 |
| `Elin-Decompiled/Elin/Thing.cs` | `SetRenderParam()` for things |
| `Elin-Decompiled/Elin/Chara.cs` | `GetSprite()`, `SetRenderParam()` for charas |
| `Elin-Decompiled/Elin/CoreDebug.cs` | `F7` 競合確認 |
| `Elin.Mods/Elin_JustDoomIt/src/DoomOverlayDisplay.cs` | UI オーバーレイ実装参考 |

## 検証手順

1. `build.bat` でビルド
2. ゲーム内で対象 Phase の動作確認
3. 公開前は `build.bat` で Release ビルド

Phase ごとの最低確認項目:

1. 表示切替しても通常プレイが壊れない
2. zone 切替・ロード・タイトル復帰で落ちない
3. Player.log に致命例外を出さない
4. fps view 非表示時の通常描画へ副作用を残さない

## 関連文書

- 3D 単独プレイ化の残ギャップと段階導入は `docs/plans/2026-03-12-fps-view-3d-only-roadmap.md` を参照。
