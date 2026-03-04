# ビルド・バリデーションパイプライン

## TSV → XLSX 変換

- LibreOffice headless で変換。**必ず `--infilter="CSV:9,34,76"` を指定**する
  - `9` = タブ区切り, `34` = ダブルクォート, `76` = UTF-8
  - これがないと全カラムが1列にマージされる

## バニラExcelヘッダー検証

- `tools/builder/validate_source_headers.py` でバニラの先頭3行と厳密比較
- JSONキャッシュ (`vanilla_defaults.json`) でExcel直読みを回避
- ゲーム更新後:
  - `python tools/builder/validate_source_headers.py sync` でキャッシュ再生成

## ドラマキー生成（Single Source）

- 正ファイル:
  - `tools/drama/schema/key_spec.py`
- 生成先:
  - Python: `tools/drama/data_generated.py`
  - C#: `src/Drama/Generated/DramaKeys.g.cs`
- 生成コマンド:
  - `python tools/drama/schema/generate_keys.py --write`
- 検証コマンド:
  - `python tools/drama/schema/generate_keys.py --check`

## build.bat のルール

- 通常:
  - `build.bat` は `--check` で生成物差分を検出し、差分があれば失敗
- 明示更新時のみ:
  - `build.bat regen` で `--write` を実行して生成物更新
- debug 併用:
  - `build.bat debug`
  - `build.bat debug regen`

## Drama v2 移行時のチェック運用 (Ars)

- 1シナリオ移行ごとに `build.bat` を実行して fail-fast を通す
- 生成物更新が必要なケースのみ `build.bat regen` を実行する
- 差分確認対象は `LangMod/EN/Dialog/Drama/*.xlsx` を基本とする
- 差分レビューでは以下を分離して判定する
  - 許容: text id 自動化（移行時は常時発生）、整形由来の空セル整理
  - 不許容: 分岐先、条件式、action/param の意味変更
- `id` 列は一致判定に使わず、`jump/if/action/param/text_*` を主判定にする

## Fail-fast 検証順序

`build.bat` 先頭で以下を実行:

1. キー生成チェック（または regen）
2. Python ドラマツールテスト
3. C# ユニットテスト

これらに通ってからデータ生成・C#本ビルドへ進む。
