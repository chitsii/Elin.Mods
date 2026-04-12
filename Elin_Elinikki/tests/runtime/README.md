# Runtime Test (Elinikki)

Elin_Elinikki のランタイム smoke テスト配置先です。  
共通ランナーはリポジトリ直下 `runtime-test-v2/` を利用します。

## 実行

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag smoke
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId elinikki.shared_world.demo_spawned
```

## 構成

- ケース実装: `tests/runtime/src/cases`
- 実行結果: `tests/runtime/_artifacts`
- 追記ログ: 各 run の `_artifacts/<run_id>/playerlog.diff.log` と `playerlog.diff.meta.json`

## 方針

- 共有ランナー API は変更せず、既存 `smoke` suite の範囲で書ける検証を優先する
- まずは `manager 起動`, `shared object 生成`, `GPU preview 生存`, `reload 後の root 再接続` を確認する
- スクリーンショット比較や見た目の正しさ検証は別途必要になった時点で追加する
