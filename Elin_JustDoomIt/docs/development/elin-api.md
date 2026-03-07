# Elin APIメモ

最終確認: 2026-03-07

## Thing の `render_data` と mod sprite 差し替え

前提:
- `Thing` の inventory icon は `Card.GetSprite()` を通る。
- 設置済み `Thing` のマップ描画は `Thing.SetRenderParam()` と `CardRenderer` の描画モードに依存する。

挙動:
- `render_data=obj` のように `RenderData.pass` が存在する設定では、設置物はメッシュ+`tiles` ベースで描画される。
- この経路では `Texture/<id>.png` などの mod sprite 差し替えが inventory には効いても、マップ上の設置物表示には反映されないことがある。
- `render_data=@obj` のように `@` 付きにすると、`RenderRow.SetRenderData()` が `RenderData` を複製した上で `pass = null` にする。
- `pass = null` の場合、`CardRenderer` は actor/sprite 描画へ落ち、`owner.GetSprite()` 経由で `SpriteReplacer` の mod texture が使われる。

注意点:
- `@obj` は「`obj` と同じ見た目設定をベースにしつつ、pass を切って sprite 描画に寄せる」用途で使う。
- 既存の tile atlas を使い続けたい設置物には向かない。
- 配置後の見た目が inventory と一致してほしい custom furniture/arcade 系では有効。

確認根拠:
- `Elin-Decompiled/Elin/RenderRow.cs`
- `Elin-Decompiled/Elin/CardRenderer.cs`
- `Elin-Decompiled/Elin/Thing.cs`
