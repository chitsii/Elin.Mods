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

### 3) DOOM commercial IWADs

- `doom1.wad` など id Software の商用アセットは本 Mod に同梱しません。
- 利用者が自身で正規に保有するデータのみ利用してください。

## Notes

- この Mod は Elin 本体、BepInEx、および上記サードパーティ資産に依存します。
- ライセンスの最終判断は各プロジェクトの原文ライセンスに従ってください。
- DOOMモード中のBGMは `Sound/BGM/*.ogg` を順番に再生します（ファイル名昇順）。
- FreeDoom 由来の音源（OGG化済み）を `Sound/BGM` に配置してください。
