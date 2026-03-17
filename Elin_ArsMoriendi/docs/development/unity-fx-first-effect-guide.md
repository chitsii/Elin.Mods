# Unity FX 導入ガイド

最終確認: 2026-03-14

## 対象

- 外部 Unity プロジェクト `C:\Users\tishi\Elin_Mods`
- mod 側の `CustomAssetFx` / `ars_spell_particle` bundle

## 基本フロー

1. Unity に入れる元素材を `PNG` で用意する。
2. 必要なら sprite sheet を作る。
3. `CreateNecroFx.cs` のメニューで prefab/material を再生成する。
4. `Tools/Build Ars Spell Bundle` で repo 側 `Asset` を更新する。
5. mod repo で `build.bat` を実行する。

## 今回の Unholy Vigor 魔法陣

- 受領素材:
  - `C:\Users\tishi\programming\generative_art\output\sprites`
  - `24枚 / 256x256 / RGBA PNG`
- Unity 取り込み先:
  - `Assets/Textures/ArsMoriendi/UnholyVigorCastCircleSheet.png`
- prefab 名:
  - `FxUnholyVigorCastCircle`
- 生成メニュー:
  - `Tools/Create Necro FX/FxUnholyVigorCastCircle`
- 現在の扱い:
  - Unity asset と bundle には残しているが、mod の C# からは未使用

## sprite sheet の前提

- 24 コマは `6x4` に並べる。
- 各フレームはクロップしない。
- 透過 PNG を維持し、動画再エンコードは挟まない。

## バンドル更新

- Unity 側で prefab を再生成した後に `Tools/Build Ars Spell Bundle` を実行する。
- 出力先は mod repo の `Asset` ディレクトリ。
- `build.bat` は bundle を再生成しないため、Unity asset を変えた作業ではこの手順が必須。
