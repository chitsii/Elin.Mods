# FPS View Visibility And Fog Design

## Goal

GPU preview path の表示範囲を「何を見せるべき情報か」で分ける。

- terrain / wall / riser は空間把握用なので広く出す
- tree / furniture / large plant は空間把握寄りなので中距離まで出す
- npc / item / effect は gameplay 情報なので Elin 本体の可視判定を優先する
- 遠景は fog で整理し、急に消さない

## Truth Sources

### Gameplay visibility

- `EClass.pc.CanSee(card)`
- `cell.isSeen`

これは card / object の「本体ゲーム上で見えるか」の truth とする。

### Spatial visibility

- camera forward cone
- distance budget
- viewport/frustum

これは terrain / wall / riser の「空間として見せるべきか」の truth とする。

## Visibility Tiers

### Tier 1: Terrain

対象:

- floor
- wall
- riser
- full block top

ルール:

- distance: `MaxDistance * 1.4`
- cone: 前方 160 度
- `cell.isSeen` には縛らない
- fog: 強くかける

理由:

地形は空間認識のために gameplay visibility より広く出す必要がある。

### Tier 2: Large world objects

対象:

- tree
- furniture
- tall plant
- installed object

ルール:

- distance: `MaxDistance * 1.0`
- cone: 前方 160 度
- `cell.isSeen` は必須
- fog: terrain より弱いが明確にかける

理由:

障害物や景観として意味はあるが、最広レンジは terrain / wall / riser に譲る。空間の骨格は地形が担う。

### Tier 3: Gameplay sprites

対象:

- npc
- loose item
- effect

ルール:

- npc distance: `MaxDistance * 1.0`
- item distance: `MaxDistance * 0.85`
- effect distance: `MaxDistance * 0.9`
- `EClass.pc.CanSee(card)` または `cell.isSeen` を必須
- cone: camera frustum に入ること
- fog: 軽め

理由:

これらは情報量が多いため、本体 gameplay visibility を優先する。

## Fog Design

### Terrain fog

- source: camera-space depth
- start: terrain draw distance の 18%
- end: terrain draw distance の 42%
- apply:
  - slight desaturation
  - haze color への lerp

### Large object fog

- source: camera-space depth
- start: object draw distance の 12%
- end: object draw distance の 42%

### Gameplay sprite fog

- source: camera-space depth
- start/end は large object と同じ
- terrain より弱く、見失わない程度にする

## Implementation Notes

1. `GatherSprites()` はカテゴリ別 visibility distance を持つ
2. GPU renderer は sprite ごとに cone threshold を持つ
3. terrain top と wall/riser は同じ tier に属する。ただし wall/riser は half-cell 程度の margin を許し、床より先に切れないようにする
4. fog は shader がなくても `_Color` 経由で同じルールを使う

## Rejected / Failed Approaches

- 全カテゴリを `MaxDistance` ひとつで切る
  - 地形が狭くなり空間把握が崩れる
- terrain まで `CanSee` / `isSeen` に揃える
  - 近代的な 3D 表示としては視界が狭すぎる
- fog を「背景色へ少し寄せるだけ」で済ませる
  - Elin の低彩度パレットでは差が見えにくい
