# Elin_JustDoomIt

Elin のカスタムアーケード筐体（CWLで追加）から、オーバーレイ表示で DOOM 互換ゲームプレイ（FreeDoom IWAD）を起動する Mod です。

## CWL Data

- `LangMod/EN/Thing.xlsx`
- `LangMod/JP/Thing.xlsx`
- `LangMod/EN/Thing.tsv`
- `LangMod/JP/Thing.tsv`
- Generator: `tools/builder/create_thing_excel.py`
- Definitions: `tools/builder/thing_definitions.py`

`id: justdoomit_arcade` の専用筐体を追加します。バニラTV/BGM機能には干渉しません。

## Credits

- Original DOOM game concept and IP: id Software
- DOOM-compatible engine library used by this mod:
  - `DoomNetFrameworkEngine` (author: `mahach`)
  - Repository: <https://github.com/mahach666/DoomNetFrameworkEngine>
- Game data (IWAD) used by this mod:
  - `FreeDoom` project (`freedoom1.wad`)
  - Project site: <https://freedoom.github.io/>

## License / Redistribution

この Mod の再配布時は、以下のライセンス条件を満たしてください。

### 1) DoomNetFrameworkEngine (binary dependency)

- License: `MIT`
- Source: `_ext/DoomNetFrameworkEngine/DoomNetFrameworkEngine.nuspec`
- License URL: <https://licenses.nuget.org/MIT>

再配布時は MIT ライセンスの条件（著作権表示とライセンス文の保持）を遵守してください。

### 2) FreeDoom WAD (game data)

- Asset: `freedoom1.wad`
- License: `BSD 3-Clause` (FreeDoom project)
- Distributed license text: `LICENSES/FreeDoom-BSD-3-Clause.txt`
- Distributed credits list: `LICENSES/FreeDoom-CREDITS.txt`

再配布時は BSD 3-Clause の条件に従い、著作権表示・条件文・免責事項を同梱資料に保持してください。

### 2.5) Third-party PWAD policy

- This package does not redistribute third-party PWAD map packs.
- External PWADs are user-supplied only.
- We provide installation guidance and tested examples, but users must download those files from the original distribution pages.

### 3) DOOM commercial IWADs

- `doom1.wad` など id Software の商用アセットは本 Mod に同梱しません。
- 利用者が自身で正規に保有するデータのみ利用してください。

## 設定と補足

- この Mod は Elin 本体、BepInEx、および上記サードパーティ資産に依存します。
- ライセンスの最終判断は各プロジェクトの原文ライセンスに従ってください。
- DOOMモード中のBGMは `Sound/BGM/*.ogg` を順番に再生します（ファイル名昇順）。
- FreeDoom 由来の音源（OGG化済み）を `Sound/BGM` に配置してください。

## 操作方法

デフォルト設定では以下の操作です。

- 移動: `W / A / S / D`（矢印キーでも可）
- エイム / 射撃: `マウス移動` / `左クリック`
- 使う/開ける: `E` / `Space`
- ダッシュ: `Shift`
- 武器切替: `1 - 7` / `マウスホイール`
- 終了: `ESC`

キーバインドを変更している場合は、実際の操作は `BepInEx/config/chitsii.elin_justdoomit.cfg` の `Input.*` に従います。

## 報酬ルール

- 1マップごとに `LOW / MID / HIGH` の RATE を使い、参加コストとしてカジノチップを支払います。
- `LOW` は安定、`MID` は標準、`HIGH` は長マップ無被弾で大きく狙う夢枠です。
- 現在の RATE 値は `LOW: entry 100 / base 30 / hit loss 18%`、`MID: entry 500 / base 70 / hit loss 24%`、`HIGH: entry 1000 / base 110 / hit loss 30%` です。
- `GENERAL SETTINGS` で `RATE` を `ASK EVERY MAP / FIX LOW / FIX MID / FIX HIGH` から選べます。
- `FIX *` を選んだ場合は毎マップの `SELECT RATE` を省略し、設定した RATE で自動参加します。チップ不足時だけ手動選択に戻ります。
- 撃破報酬は即時支給されず、そのマップ専用の未確定プールへ加算されます。
- シークレット発見でも、そのマップの未確定プールへ固定 `+500` が加算されます。
- 連続ボーナスは `LOW +400% / MID +600% / HIGH +999%` まで伸び、被弾で初期値に戻ります。
- 被弾するとプールの一部を失い、連続ボーナスは初期値に戻ります。
- マップクリア時または死亡時に `CASH OUT` され、未確定プールがカジノチップに変換されます。
- `ESC` 退出では、そのマップの未確定プールを失います。
- ボスマップクリアの追加 `+10000` は初版で維持しています。
- HUD は `賭け / 未精算チップ / 1キル報酬` を表示し、未精算チップは増減が分かるゲージで見せます。

## Notes

- DOOM の内部解像度と明るさは `BepInEx/config/chitsii.elin_justdoomit.cfg` の `DOOM.ScreenWidth` / `DOOM.ScreenHeight` / `DOOM.Brightness` で調整できます。
- DOOM のキーバインドは `BepInEx/config/chitsii.elin_justdoomit.cfg` の `Input.*` で調整できます。値は `W,UpArrow` のようなカンマ区切りです。`Mouse0` / `Mouse1` / `Mouse2` も使えます。`WheelUp` / `WheelDown` は `NextWeapon` / `PreviousWeapon` 専用です。キー名は Unity 公式 `KeyCode` 一覧を参照してください: <https://docs.unity3d.com/ScriptReference/KeyCode.html>
- `cfg` を編集した後は Elin 再起動ではなく、次に DOOM を起動した時点で再読込されます。
- `OpenMenu` の入力項目はありません。メニュー操作は Elin 側 UI が直接処理します。
- 外部PWADは `OPEN MOD FOLDER` で案内を確認し、`CHANGE GAME` -> `CONFIGURE MODS` の順で導入します。`CONFIGURE MODS` を開くと自動で再判定されます。
- PWADは v1 仕様で1つだけ有効化できます（`unknown` は起動前警告つきで選択可）。
- Mod導入手引き: `docs/doom_mod_install_guide.md`
