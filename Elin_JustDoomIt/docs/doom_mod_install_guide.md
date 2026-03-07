# JustDoomIt: DOOM Mod導入手引き

## 目的
この手引きは `Elin_JustDoomIt` で IWAD/PWAD を導入し、依存判定つきで安全に起動するためのガイドです。

## 1. フォルダ構成

```text
Package/Elin_JustDoomIt/
  wad/
    freedoom1.wad
    freedoom2.wad
    mods/
      00_HOW_TO_ADD_MODS_*.txt
      <mod_folder>/
        *.wad
BepInEx/config/chitsii.elin_justdoomit/profiles/
  runtime_loadout.json
```

補足:
- `freedoom1.wad` / `freedoom2.wad` は Mod に同梱されています。
- 互換のため `wad/iwads` 配置の IWAD も検出します。
- 旧構成の `StreamingAssets/doom` も探索対象です。
- 可変設定ファイルは `wad` 配下ではなく `BepInEx/config/chitsii.elin_justdoomit/profiles` に保存されます。

## 2. 入れるファイル
- IWAD
  - `freedoom1.wad`（DOOM1系, 同梱済み）
  - `freedoom2.wad`（DOOM2系, 同梱済み）
- PWAD（任意）
  - `wad/mods/<mod_folder>/` を作成して配置
  - 第三者 PWAD は同梱しません。ユーザーが配布元から取得してください。
  - 複数 `.wad` を使う Mod は、ゲーム内で初回セットアップ確認が出ます。
  - v1 の対応対象は `.wad` のみです。`.deh` / `.bex` / `.pk3` / `.pk7` は対象外です。

## 3. ゲーム内の操作手順
アーケード筐体 `justdoomit_arcade` を使用するとメニューが開きます。

1. `OPEN MOD FOLDER` で `wad/mods` を開き、同梱の案内テキストを読む
2. 案内先の配布元から欲しい PWAD をダウンロードする
3. `wad/mods/<mod_folder>/` を作る
4. 必要な `.wad` をそのフォルダへ配置する
5. `CHANGE GAME` で IWAD を選択する
6. `CONFIGURE MODS` を開く
7. 複数 `.wad` の Mod なら、検出ファイル一覧を確認し、必要なら順序や対象を調整してセットアップを承認する
8. セットアップ承認後、その場で一覧が更新され、すぐ選択可能になります
9. `CONFIGURE MODS` で 1つだけ ON
10. `START OVER` で最初から起動
11. `CONTINUE` が出ない時は、詳細欄にどの GAME / MOD に戻せば再開できるかが表示されます

## 4. 依存判定の仕様
- 依存ファミリー: `doom1 | doom2 | any | unknown`
- 判定根拠:
  - `MAPxx` のみ -> `doom2`
  - `E#M#` のみ -> `doom1`
  - 両方 -> `any`
  - なし/失敗 -> `unknown`
- 安全制限:
  - 解析対象は `.wad` のみ
  - 128MB を超えるファイルは `unknown`
  - 解析タイムアウトは 2 秒

## 5. メニュー表示の意味
- `[ON ]` / `[OFF]`: 有効/無効
- `[---]`: 選択中 IWAD と不一致（選択不可）
- `[?]`: 依存不明（選択可、起動時に警告）
- `[SETUP]`: 複数WADの初回セットアップが必要
- `[ERR ]`: 内部設定や参照ファイルに不整合あり
- `[UNSUPPORTED]`: `.deh` / `.bex` / `.pk3` / `.pk7` を含むため v1 では非対応
- `[LAYOUT]`: フォルダ構成が認識できない
- `CONTINUE`: 今の IWAD / Mod 構成と一致するセーブがある時だけ表示
- `SCANNING MODS...`: 初回走査やハッシュ計算中。フリーズではありません

## 6. トラブルシュート
1. MODフォルダが開かない
- メニューに表示された絶対パスを手動で開く
- `Copy` を押してパスをコピー

2. PWADが出ない
- `wad/mods/<mod_folder>/` になっているか確認
- zip のまま置いていないか確認
- `CONFIGURE MODS` を一度開いて再判定を走らせる
- 初回は `SCANNING MODS...` がしばらく出ることがあります
- `[LAYOUT]` と表示される場合は、`.wad` が深いサブフォルダに入っていないか確認
- `.deh` / `.bex` / `.pk3` / `.pk7` を含む Mod は v1 では対象外
- `[UNSUPPORTED]` と表示される場合は、その Mod は v1 では使えません
- 複数WAD Mod の設定をやり直したい時は、`CONFIGURE MODS` の `RESET SETUP` を使って下さい
- `RESET SETUP` は確認ダイアログ付きです

3. 起動時に無効扱いになる
- `CONFIGURE MODS` で `[---]` になっていないか確認
- IWAD と依存ファミリー（DOOM1/DOOM2）を一致させる

4. `CONTINUE` が出ない
- セーブが消えたとは限りません
- 詳細欄に表示された GAME / MOD に戻すと再開できます

5. 複数WAD Mod の設定を初期化したい
- `CONFIGURE MODS` で対象 Mod の `RESET SETUP` を実行して下さい
- 確認ダイアログで承認すると、その Mod のファイル順設定が初期化されます
- フォルダ名を変えた場合は別 Mod として扱われます

## 7. ライセンス運用
- 同梱は `FreeDoom/FreeDM` のみ
- 第三者 PWAD は非同梱とし、手動導入案内に留める
- 監査メモは「将来同梱を再検討する場合の一次ソース整理」として保持する

詳細:
- `docs/wad_redistribution_audit.md`
