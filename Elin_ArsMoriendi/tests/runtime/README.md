# Runtime Test (ArsMoriendi)

ArsMoriendi のランタイムテスト配置先です。  
共通ランナーはリポジトリ直下 `runtime-test-v2/` を利用します。

## 実行

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId <case_id>
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId patch.compat.cwl_incompatible_scan
```

## 構成

- ケース実装: `tests/runtime/src/cases`
- ドラマ定義: `tests/runtime/src/drama`
- 実行結果: `tests/runtime/_artifacts`
- 追記ログ: 各 run の `_artifacts/<run_id>/playerlog.diff.log` と `playerlog.diff.meta.json`

## オプション

- `-KeepGeneratedSource`: 既定では保存しない `runtime_suite_v2_generated.csx` を `_artifacts` に残す
- `-Suite smoke` の既定実行は `-Tag smoke` 扱い（`-Tag` 未指定かつ `-CaseId` 未指定時）

## 実装済みケース

- `patch.targets.spawnloot_and_oncharadie`
- `patch.compat.cwl_incompatible_scan`
- `spell.soultrap_drop_pipeline`
- `spell.preservecorpse_guaranteed_drop`
- `spell.soulbind_substitution`
- `servant.ritual_create_and_track`
- `servant.release_detach_and_cleanup`
- `servant.revive_master_persistence`
- `quest.stage0_to1_first_soul`
- `quest.stage1_to2_tome_open`
- `quest.stage2_knight_spawn_once`
- `quest.stage7_erenos_defeat_advance`
- `quest.branch_contract_rule_presence`
- `quest.branch_runtime_flag_select`
- `quest.branch_converge_common_successor`
- `ars_apotheosis` ほか `ArsDramaRuntimeCatalog` に定義された drama ケース

## クリティカル運用

- `-Tag critical` は呪文/従者/クエスト進行の回帰を検知する必須ゲート。
- `summary.failed > 0` は即失敗扱い（再実行しない）。

## 互換性能動チェック

- `patch.compat.cwl_incompatible_scan` は CWL の `TestIncompatibleIl` を能動実行するため、既定 smoke からは除外。
- 実行する場合は `-CaseId patch.compat.cwl_incompatible_scan` を明示する。
