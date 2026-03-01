# Servant Visual Indicator Ideas

## Status: Investigation

## Idea A: TCコンポーネント追加（グローマーカー方式）

- `CardRenderer.AddExtra()` の仕組みを流用
- キャラの背面に光るマーカーやグロー効果を配置
- バニラの伝説キャラが使う `c_unique` / `c_unique_evolved` マーカーと同じアーキテクチャ
- 利点: Elin既存のTC系システムに乗るため安定性が高い
- 課題: プレハブ or 動的生成が必要

### 参考: バニラの実装
- `CardRenderer.RefreshExtra()` で `AddExtra("c_unique")` を呼び出し
- プレハブパス: `Scene/Render/Actor/Component/Extra/{id}`
- `TC` コンポーネントとして管理される

## Idea B: 足元カラーサークル

- キャラの足元に半透明の紫/緑の円を常時表示
- RTS風の選択マーカー的な見た目
- 利点: シンプル、視認性が高い
- 課題: 他のエフェクトと干渉する可能性、パフォーマンス影響

### 実装案
- `CardRenderer.Draw()` のタイミングで足元にスプライトを描画
- または `MeshPass.AddShadow()` 相当の仕組みで円を描画
- 半透明スプライトを `passShadow` の上のレイヤーに配置
