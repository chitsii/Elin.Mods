# FPS View Idealization Model

> Note
>
> この文書は floor/wall 中心の初期 idealization モデル。billboard まわりの方針は後続の
> `docs/plans/2026-03-12-fps-view-hybrid-renderer-design.md`
> で置き換えた。

## Goal

Elin の 2D クォータービュー描画ロジックをそのまま FPS に移すのではなく、FPS レンダラが扱いやすい「理想化済み状態」に変換してから描画する。

## Layering

1. Elin runtime state
   - `Cell`
   - `SourceFloor/SourceBlock`
   - `SourceMaterial`
   - `CellDetail.things/charas`
2. Idealized FPS state
   - `FpsResolvedFloorSurface`
   - `FpsResolvedWallSurface`
   - `FpsResolvedBillboard`
3. Renderer
   - raycast wall columns
   - floor projection
   - billboard projection

## Why this split

- Elin 側は `snow`, `bridge`, `floorDir`, `autotile`, `blockDir`, `matColor`, `sprite replacer` など決定要素が多い。
- FPS レンダラ側は「どの atlas を使うか」「どの tile index を使うか」「どの tint を掛けるか」だけ分かればよい。
- そのため renderer が `Cell` の複雑な分岐を直接持つのをやめ、`FpsIdealizedWorld` が責務を吸収する。

## Current idealized states

### Floor

- base floor tile
- material tint
- snow/ice/bridge 解決後の atlas 選択
- autotile overlay tile
- water autotile atlas 使用フラグ

### Wall

- block tile
- material tint
- snow atlas 使用フラグ

### Billboard

- 初期版では `FpsResolvedBillboard`
- ただしこの設計は `renderer.position` / 2D offset の混入で破綻しやすく、後続設計で `upright billboard` / `ground sprite` / `effect billboard` に分離する

## Known gaps

- `shore` overlay は未反映
- `passEdge` 系の ambient shadow/transition は未反映
- half block / repeat block / room height は未理想化
- billboard の dir 選択・PCC・装備差分は未理想化
- loose item / chara / furniture を同じ billboard 規則で扱うのは破綻するため、hybrid renderer 設計へ移行する
