# Runtime Test (SukutsuArena)

SukutsuArena のランタイムテスト配置先です。  
共通ランナーはリポジトリ直下 `runtime-test-v2/` を利用します。

## 実行

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId <case_id>
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId patch.compat.cwl_incompatible_scan
```

## 構成

- ケース実装: `tests/runtime/src/cases`
- 実行結果: `tests/runtime/_artifacts`
- 追記ログ: 各 run の `_artifacts/<run_id>/playerlog.diff.log` と `playerlog.diff.meta.json`

## 実装済みケース

- `patch.targets.core_methods`
- `quest.chain.main_and_postgame`
- `quest.manager.phase_contract`
- `quest.dispatch.selection_contract`
- `drama.modinvoke_registry_contract`
- `arena.battle_pipeline_contract`
- `arena.zoneinstance.result_routing_flags`
- `arena.zoneinstance.direct_drama_schedule`
- `patch.compat.cwl_incompatible_scan` (能動互換チェック、既定 smoke から除外)

## 運用

- `-Suite smoke` の既定実行は `-Tag smoke` 扱い（`-Tag` 未指定かつ `-CaseId` 未指定時）
- 重要機能の回帰確認は `-Tag critical` を必須運用とする
- `summary.failed > 0` は失敗扱い

## 互換性能動チェック

- `patch.compat.cwl_incompatible_scan` は CWL の `TestIncompatibleIl` を能動実行する
- 実行時負荷/不安定化リスクがあるため既定 smoke には含めない
- 実行する場合は `-CaseId patch.compat.cwl_incompatible_scan` を明示する
