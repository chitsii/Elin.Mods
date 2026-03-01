# デフォルト値検証ツール

`extract_source_defaults.py` - MODビルダーのカラム定義がバニラExcelと一致しているか検証する。

## 前提

`../SourceExcels/` にバニラのExcelファイル（SourceCard.xlsx, SourceGame.xlsx 等）が配置されていること。

## 基本操作

```bash
cd tools

# 全MODビルダーを一括比較（推奨）
uv run python builder/extract_source_defaults.py compare

# 特定シートのみ比較
uv run python builder/extract_source_defaults.py compare --sheets Thing,Zone
```

## 出力の読み方

```
--- Thing (create_thing_excel.py) ---
  バニラ: 52 カラム, MOD: 52 カラム
  OK                          ← 完全一致
```

差異がある場合:

```
  [カラム数] バニラ: 52, MOD: 54
  [カラム名の差異]
    Col 3: バニラ='unknown_JP', MOD='name_CN'
  [デフォルト値の差異]
    Col 22 (factory): バニラ='', MOD='log'
```

## 差異が出たときの対処

1. 該当ビルダー（例: `create_thing_excel.py`）の `HEADERS` / `TYPES` / `DEFAULTS` を修正
2. 再度 `compare` を実行して OK を確認
3. `build.bat debug` でビルド確認

## 意図的な差異

以下のシートはMOD独自のカラムサブセットを使うため、差異が出るのは正常:

- **Element** - MODは14カラム（バニラは57）
- **Quest** - MODは35カラム（バニラは17）
- **Chara** - バニラExcelから動的読み込み（末尾カラムに差異が出る場合あり）

## サブコマンド: extract

バニラデータをJSONに保存する（参照用）:

```bash
uv run python builder/extract_source_defaults.py extract
# → builder/vanilla_defaults.json に出力
```
