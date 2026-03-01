# CWL ドラマファイルフォーマット仕様

このドキュメントは、Elin CWL (Custom Whatever Loader) のドラマファイル形式について、
TinyMita サンプルの分析結果に基づいてまとめたものです。

## ファイル配置

- **パス**: `LangMod/**/Dialog/Drama/<ドラマ名>.xlsx`
- **命名規則**: NPCの `tag` に `addDrama_<ドラマ名>` を設定し、ファイル名を `<ドラマ名>.xlsx` にする
- **例**: `addDrama_drama_sukutsu_arena_master` → `Dialog/Drama/drama_sukutsu_arena_master.xlsx`

## Excelファイル構造

### シート名

- シート名は **任意** だが、キャラクターIDやアクターIDを使うのが推奨
- 例: TinyMitaでは `mutsumi` (キャラクターの名前)

### ヘッダー列 (Row 1)

以下の列が必要：

| 列名 | 説明 |
|------|------|
| `step` | ステップ名。ジャンプ先として使用 |
| `jump` | ジャンプ先のステップ名 |
| `if` | 実行条件 |
| `action` | 実行するアクション |
| `param` | アクションのパラメータ |
| `actor` | 発言者のアクターID |
| `version` | (オプション) バージョン管理用 |
| `id` | テキスト行の一意識別子 |
| `text_JP` | 日本語テキスト |
| `text_EN` | 英語テキスト |
| `text` | フォールバックテキスト |

> **重要**: `if2` 列は使用しない

### データ開始位置

- **Row 1**: ヘッダー行
- **Row 2-5**: 空行（メタデータ用スペース）
- **Row 6以降**: 実際のデータ

### ステップ定義

ステップ行と内容行は **分離** する：

```
Row 6: step=main (step列のみ、他は空)
Row 7: action=sound, param=... (内容行)
Row 8: id=1, text_JP=... (テキスト行)
```

## Excel生成時の注意

### openpyxl使用を推奨

soffice (LibreOffice) による TSV → XLSX 変換は以下の問題がある：
- シート名がファイル名から自動生成される
- フォーマットの制御が難しい

**推奨**: `openpyxl` で直接 XLSX を生成する
