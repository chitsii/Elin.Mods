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

## Fail-fast 検証順序

`build.bat` 先頭で以下を実行:

1. キー生成チェック（または regen）
2. Python ドラマツールテスト
3. C# ユニットテスト

これらに通ってからデータ生成・C#本ビルドへ進む。
