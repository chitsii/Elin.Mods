# FPS View Day-Night And Sky Design

## Goal

Elin 本体の昼・夕方・夜の豊かな見え方を、FPS/GPU preview path に近い形で取り込む。

対象:

- 空色 / 背景色
- 画面全体の空気色
- 夜間の暗さとコントラスト
- weather / snow / rain による色寄り

## Decompiled Findings

### 1. 時間帯の主入力は `Scene.timeRatio`

- `Scene.UpdateTimeRatio()` が `world.date.hour/min` または `map.config.hour` から `timeRatio` を算出する
- 正午を 1.0 にするのではなく、昼夜の対称カーブへ正規化している

参照:

- `Elin-Decompiled/Elin/Scene.cs`

### 2. 空・太陽・海・fog 色は `BaseGameScreen.UpdateShaders()` が決める

本体は `SceneProfile` の gradient / curve を `timeRatio` で評価し、global shader 値へ流している。

主要入力:

- `profile.color.fog`
- `profile.color.sun`
- `profile.color.sunSnow`
- `profile.color.sea`
- `profile.color.sky`
- `profile.color.skyBG`
- `profile.light.nightRatioCurve`
- `profile.light.lightPower`
- `profile.light.vignetteCurve`
- `profile.light.bloomCurve`

主要出力:

- `_FogColor`
- `_FogStrength`
- `_SunColor`
- `_SeaColor`
- `_SkyColor`
- `_SkyBGColor`
- `_NightRate`
- `_LightPower`

参照:

- `Elin-Decompiled/Elin/BaseGameScreen.cs`

### 3. 実タイルの明るさは `BaseTileMap` が別で持つ

本体 2D タイル描画は `BaseTileMap` で次を計算している。

- `nightRatio`
- `fogBrightness`
- `lightLimit`
- `_lightMod`
- `destBrightness`
- `_baseBrightness`
- `shadowStrength`
- `floorShadowStrength`

これが `blockLight` / `floorLight` の packed 値へ流れ、tile ごとの明暗になる。

参照:

- `Elin-Decompiled/Elin/BaseTileMap.cs`

### 4. 画面全体の補正は `RefreshWeather()` / `RefreshGrading()` にある

本体は weather と scene template から次を追加で触っている。

- fog profile
- scene brightness
- `beautify.tintColor`
- `grading.nightBrightness`
- overlay profile

参照:

- `Elin-Decompiled/Elin/BaseGameScreen.cs`

## What We Can Reuse In FPS View

### Reuse directly

- `EMono.scene.timeRatio`
- `EMono.scene.profile.color`
- `EMono.scene.profile.light`
- `EMono._zone.IsSnowCovered`
- `EMono.world.weather.CurrentCondition`
- `EMono.scene.camSupport.beautify.tintColor`
- `EMono.scene.camSupport.grading.nightBrightness`

### Reuse indirectly

- tile/object local light:
  - already approximated via `FpsLightingResolver`
- screen-level grading:
  - must be reapplied in FPS overlay / GPU material path, not copied automatically

## Recommended FPS Design

### Layer 1: Sky / clear color

FPS background should stop using a fixed dark blue clear color.

Use:

- base sky = `profile.color.sky.Evaluate(timeRatio)`
- background sky = `profile.color.skyBG.Evaluate(timeRatio)`

Recommended mapping:

- clear color = lerp(`skyBG`, `sky`, 0.35)
- fog haze color = lerp(`fog`, `skyBG`, 0.5)

### Layer 2: Distance fog / haze

Current FPS fog is local and synthetic.
It should be re-based on the same time-driven colors:

- fog hue from `profile.color.fog`
- night weight from `profile.light.nightRatioCurve`
- snow/weather adjustments from `IsSnowCovered` / `CurrentCondition`

### Layer 3: Screen-wide grading

Approximate these from the live scene:

- `beautify.tintColor`
- `grading.nightBrightness`

In FPS path this should become:

- final color multiply / add on terrain and sprites
- optional full-screen overlay tint in `FpsOverlayDisplay`

### Layer 4: Keep tile lighting separate

Do not replace local `blockLight` / `floorLight`.
Screen-level day/night grading should sit on top of per-tile lighting.

## Minimal Implementation Order

1. Replace fixed `ClearColor` with time-driven sky clear color
2. Re-base fog color on `profile.color.fog/skyBG`
3. Add screen-wide night/tint modulation from live grading state
4. Add optional weather adjustments

## Rejected / Failed Directions

- Treating day-night only as stronger distance fog
  - misses sky hue, screen tint, and night brightness
- Reusing only tile `blockLight/floorLight`
  - local light changes, but the whole scene still lacks the rich sky/screen mood shift
