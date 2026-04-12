# Normal Proxy Design

## Goal

Elin の通常ビューで shared 3D object を自然に見せる。
特に重要なのは次の 2 点。

- 重なり
- スケール感

FPS 側は true 3D のままにし、通常ビューは Elin の 2.5D 描画規約に沿った proxy 表現にする。

## Facts From Elin

### Base position

- `Point.Position()` はタイル座標を通常ビュー用の `x/y/z` に変換する。
- `Point.PositionCenter()` はその中心補正を足す。

Refs:

- `Elin-Decompiled/Elin/Point.cs`

### Thing placement

- `BaseTileMap.GetThingPosition()` は floor height, bridge, ramp, stack, altitude を足した thing offset を返す。
- 前後関係は `renderSetting.thingZ + zSetting.mod1 * thingPos.y` で付く。
- つまり、通常ビューの前後は「物体ごとの 1 本の z」が基本。

Refs:

- `Elin-Decompiled/Elin/BaseTileMap.cs:3235`
- `Elin-Decompiled/Elin/BaseTileMap.cs:3330`

### Large-height rendering

- Elin 本体は大きい壁や柱を「巨大 1 sprite の縮尺」だけで見せていない。
- `RenderData.DrawRepeatTo()` / `DrawRepeat()` は `p.y` と `p.z` を段ごとに増やして繰り返し描く。
- つまり高さ感は `y` の積み上げだけでなく、`z` の積み上げでも作っている。

Refs:

- `Elin-Decompiled/Elin/RenderData.cs:168`
- `Elin-Decompiled/Elin/RenderData.cs:214`
- `Elin-Decompiled/Elin/BaseTileMap.cs:2842`
- `Elin-Decompiled/Elin/BaseTileMap.cs:3002`

## Implications

### Overlap

- true 3D の局所的な遮蔽は通常ビューでは表現しにくい。
- Elin が破綻していないのは、複雑な遮蔽を解いているからではなく、大きいスプライトを正しい足元と sort で置いているから。
- 手前セルのスプライトが後ろセルのスプライトを隠す、という Elin 本来のルールに寄せるべき。
- つまり通常ビュー proxy の基本は「1 枚の大きい proxy を正しく置く」こと。

### Scale feel

- proxy の大きさは sprite の見た目合わせではなく、タイル単位で定義するべき。
- つまり `footprint` と `height_units` が基準。
- large object は FPS mesh の実寸と通常ビュー proxy の footprint/height を同じ semantic size で持つ必要がある。

## Proposed Model

### Dual representation

- FPS: true 3D mesh/prefab
- Normal: quarter-view proxy

### Normal proxy parameters

- `Footprint`
  - タイル単位の占有幅。`x/z`
- `HeightUnits`
  - 通常ビューで何段の高さとして見せるか
- `HeightMode`
  - `SemanticOnly | TextureAspectOnly | MaxSemanticAndTextureAspect`
  - primitive ではなく object/profile ごとに通常ビュー高さの意味づけを切り替える
- `TextureAspectHeightScale`
  - baked/authored proxy texture の縦横比をどの程度採用するか
- `Pivot`
  - 足元基準
- `SortPivotY`
  - `thingZ + mod1 * y` に食わせる基準高さ
- `Mode`
  - `AutoBake | ExplicitTexture | Hidden`

### Authored complex object

- complex mesh は auto bounds だけで semantic size を決めない
- `Footprint` と `HeightUnits` は authored profile で明示する
- `HeightMode` は原則 `SemanticOnly`
- bake texture は silhouette を作るために使い、最終的な大きさは profile が決める

### Next parameter to add

- `SliceCount`
  - 1 なら現在方式
  - 2 以上なら vertical slice proxy に分割
- `SliceStepHeight`
  - slice ごとの `y` 増分
- `SliceStepZ`
  - slice ごとの `z` 増分

Default は Elin の `peakFix` / `heightMod` から導く。

## Rendering Strategy

### Small object

- 1 slice
- `AutoBake` か `ExplicitTexture`
- 高さは primitive 別ではなく profile 別に選ぶ
  - 球体や低い台座: `SemanticOnly`
  - 板や複雑シルエット: `MaxSemanticAndTextureAspect`

### Large object

- `ExplicitTexture` 推奨
- 基本は 1 slice
- `Footprint`, `HeightUnits`, `Pivot`, `SortPivotY` を profile で合わせる
- 大型像や複雑 mesh でも、必要なのは `profile` の調整であって primitive 分岐ではない
- `multi-slice` は基本戦略ではなく、単純な縦長塊で 1 枚 proxy だと前後感が弱すぎる場合の補助手段

### Very large symbolic object

- authored quarter-view atlas
- 原則は 1 枚 proxy
- それでも足りない場合だけ front/mid/back の 2-3 層に分ける

## Immediate Next Step

1. `SharedWorldNormalProxyDefinition` を profile 主導にする
2. bake 済み complex mesh に対して authored profile を持てるようにする
3. complex mesh ごとに semantic size と sort pivot を profile へ持たせる
4. `large statue` 用の explicit proxy texture + profile 実例を追加する
