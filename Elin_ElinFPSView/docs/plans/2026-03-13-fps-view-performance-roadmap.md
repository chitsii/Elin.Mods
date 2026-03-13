# FPS View Performance Roadmap

## Context

- 2026-03-13 時点の renderer は `640x400` 既定値で重さが目立つ。
- 現行の `FpsRenderer` は wall raycast に加えて、terrain top/riser を screen-space triangle rasterization で重ね、さらに upright/ground/effect sprite を別 pass で描いている。
- この延長で full-screen 的な見た目改善を積み続けると、解像度を上げるほどコストが素直に増え、将来の機能追加余地も減る。

## Current Cost Structure

### 1. Resolution-linear cost

- `RenderFrame()` は毎フレーム `Color32[]`、column depth、scene depth を全消去する。
- wall column pass、sprite pass、terrain triangle pass はいずれも内部解像度にほぼ比例して重くなる。
- `320x200 -> 640x400` は画素数 4 倍なので、今の構造では単純に重くなりやすい。

### 2. Terrain pass is the likely dominant hotspot

- `RenderTerrainSurfaces()` は視界前方だけでなく、`maxDistance` ベースの square 範囲を毎フレーム走査する。
- 各 cell について top quad と riser quad を三角形へ分解して rasterize している。
- 近距離では screen-space bounding box が大きく、同じ画素を terrain / wall / sprite が何度も上書きする。

### 3. Sprite pass still scales with overdraw

- upright / ground / effect は毎フレーム sort し、screen rect を走査して alpha test している。
- 自キャラを含む TPS では近距離 sprite の screen 占有率が大きくなり、解像度を上げるほどコストが増える。

### 4. World idealization is cheaper than rasterization, but still not free

- `FpsIdealizedWorld.GatherSprites()` は可視半径の square を毎フレーム走査する。
- lighting 解決は cache 化済みだが、cell / object / chara の収集と分類は依然フレーム依存。

## Roadmap

### Milestone 0: Add measurement before further visuals

Goal:
- 「どの pass が何 ms 使っているか」を毎回見えるようにする。

Changes:
- `RenderFrame()` に lightweight profiler を追加する。
- 最低でも次を計測する:
  - wall raycast
  - terrain pass
  - sprite gather
  - ground sprite pass
  - upright sprite pass
  - effect pass
  - upload
- `cells visited`, `terrain tris rasterized`, `sprites drawn`, `pixels touched` も取る。

Why first:
- 今の議論で最も怪しいのは terrain pass だが、実測なしに最適化順を決めると外す可能性がある。

### Milestone 1: Cheap wins without changing visual model

Goal:
- 見た目をほぼ変えずに無駄な仕事を減らす。

Changes:
- `RenderTerrainSurfaces()` の走査を square から frustum-aware にする。
- camera 前方と screen bounds に入らない cell は top/riser 生成前に落とす。
- sprite gather も forward cone と距離で事前 cull する。
- 近距離 TPS で自キャラが巨大になるケースは near sprite rect を早期 clip する。
- 解像度は固定 default 引き上げではなく preset / dynamic scale へ寄せる。

Expected impact:
- 中。構造を変えずに overdraw と不要 cell 走査を削れる。

### Milestone 2: Migrate terrain to GPU hybrid chunks

Goal:
- 現行の最重 pass を構造から軽くする。

Changes:
- terrain top/riser を「cell ごとの三角形 rasterizer」から外す。
- `docs/plans/2026-03-13-fps-view-gpu-hybrid-design.md` に沿って chunk mesh 化する。
- wall raycast と terrain height の責務を整理し、地形側面を per-cell triangle で塗り潰さない。

Why this is the pivot:
- 今の renderer で最も高コストになりやすいのは terrain の screen-space overdraw。
- ここを放置したまま解像度や見た目を上げるのは効率が悪い。
- 現時点では CPU 内での terrain rewrite より、GPU hybrid への移行を優先する。

Expected impact:
- 大。解像度上昇時の鈍化を抑えやすい。

### Milestone 3: Make sprite rendering cheaper and more predictable

Goal:
- TPS を含む billboard pass のコストを制御しやすくする。

Changes:
- upright / ground / effect の pass を前方可視だけに限定する。
- sprite metrics と render rect を frame 内再計算しないよう整理する。
- 可能なら front-to-back で描き、scene depth による早期 skip を増やす。
- near full-screen sprite のときは細かい per-pixel sample を減らす LOD を検討する。

Expected impact:
- 中。terrain rewrite 後の次の支配項になりやすい部分を抑える。

### Milestone 4: Re-introduce richer visuals under a budget

Goal:
- 見た目改善を再開しても、frame budget を超えない状態を保つ。

Rules:
- 新しい見た目機能は profiler 付きで入れる。
- 解像度 default の変更は pass 計測とセットで判断する。
- まず terrain と sprite の予算を削ってから、追加演出へ戻る。

## Recommended Near-term Direction

1. まず profiler を入れる。
2. terrain pass の visited cell / rasterized tri / ms を見る。
3. terrain が支配的なら、次の実装は frustum culling ではなく terrain rewrite を優先する。
4. その間の既定解像度は保守的に扱う。

## Non-goals For Now

- full-screen post effect の追加
- 高価な per-pixel lighting の追加
- screen-space shadow や SSAO 的な演出
- さらなる高解像度 default への引き上げ
