# Runtime Test (Quest Mod Skeleton)

Quest Mod Skeleton の runtime test 仕様です。  
共通ランナーはリポジトリ直下 `runtime-test-v2/` を利用します。

## 1. 前提条件

- ゲーム起動済み + テスト用セーブ読込済み
- CWL の named pipe (`Elin\Console`) が有効
- プレイヤー名に `RUNTIME_TEST` を含む（既定のセーブガード）
- Mod DLL が `build.bat` で最新化されている

## 2. 実行コマンド

```powershell
# 既定: smoke + Tag smoke
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1
```

```powershell
# smoke: 単体ケース
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId <case_id>
```

```powershell
# smoke: タグ実行
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical
```

```powershell
# smoke: 能動互換チェック（通常 smoke から除外）
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId questmod.patch.compat.cwl_incompatible_scan
```

```powershell
# debug: showcase コンソールコマンド検証（DEBUGビルド時）
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId questmod.debug.console.showcase_command
```

```powershell
# drama: 全ケース実行（成功必須）
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama
```

```powershell
# drama: 単体ケース実行（CaseId は DramaId）
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId <drama_id>
```

```powershell
# drama: feature showcase 単体実行
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId quest_drama_feature_showcase
```

```powershell
# drama: feature showcase follow-up 単体実行
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId quest_drama_feature_followup
```

## 3. smoke/drama の選択仕様

- `-Suite smoke`
  - `tests/runtime/src/cases` の `IRuntimeCase` を実行
  - `-Tag` はケース側 `Tags` と照合
  - `-CaseId` は `IRuntimeCase.Id` と完全一致で絞り込み
  - `-Tag` と `-CaseId` が未指定なら自動で `Tag=smoke`
- `-Suite drama`
  - `tests/runtime/src/drama` の `IDramaCaseProvider.BuildCases()` を実行
  - `-CaseId` は `DramaCaseDefinition.DramaId` と完全一致で絞り込み
  - `-Tag` は空または `drama` のみ有効（それ以外は `no_cases_selected` になり得る）

## 4. Drama Test の仕様（契約）

`tests/runtime/src/drama/QuestDramaCatalog.cs` で `IDramaCaseProvider` を実装し、`DramaCaseDefinition` を返します。

- `DramaId`
  - 実行対象ドラマID（`-CaseId` の照合キー）
- `TimeoutSeconds`
  - 1ケースあたりの最大実行秒数
- `MaxBranchRuns`
  - 分岐探索の最大反復回数
- `MaxChoiceStepsPerRun`
  - 1反復内で処理する選択ステップ上限
- `MaxQueuedPlans`
  - 分岐候補キュー上限（暴走防止）
- `TargetCoverageRatio`
  - 目標分岐カバレッジ（到達で早期終了可）
- `RequiredNpcIds`
  - ケース前に近傍へ準備するNPC ID一覧
- `SetupFlags`
  - 実行前に設定する `dialogFlags` のキー/値

テンプレート初期値は `quest_drama_replace_me` なので、実運用では実際の drama ID に置換してください。

## 5. 実行ライフサイクル（drama）

1. スイート開始時にベースラインセーブIDを記録
2. 各ケース前にベースラインセーブを再ロード
3. `SetupFlags` / `RequiredNpcIds` を適用
4. drama を実行し、分岐を探索
5. ケース後に設定をクリーンアップ
6. スイート終了後、ベースラインセーブを再ロード

このため、drama suite はセーブを跨いで副作用を残しにくい設計です。

## 6. 運用レイヤ（drama）

- 通常リリース判定:
  - `-Suite drama`
  - カタログ登録済みケース（placeholder + feature showcase + followup）を実行
- 調査/デバッグ:
  - `-Suite drama -CaseId <drama_id>`
  - 問題ケースを単体で再現確認

## 7. 結果ファイルとログ

- 出力先: `tests/runtime/_artifacts/<run_id>/`
- 主ファイル
  - `result.json`: 実行結果（suite/status/summary/cases）
  - `playerlog.diff.log`: この run で `Player.log` に追記された差分
  - `playerlog.diff.meta.json`: 差分抽出メタ情報
- `-KeepGeneratedSource` 指定時のみ
  - `runtime_suite_v2_generated.csx` を artifacts に保存

## 8. よくある失敗理由

- `save_guard_rejected`
  - セーブ名が `RequiredNameContains`（既定: `RUNTIME_TEST`）を満たしていない
- `no_cases_selected`
  - `-CaseId`/`-Tag` の絞り込みで対象0件
- `case_failed`
  - 個別ケース失敗（`result.json` の `cases[*].reason` を確認）
- `cleanup_reload_failed`
  - drama suite 終了後のベースライン再ロード失敗

## 9. 実装済みケース（smoke）

- `runtime.boot.pc_available`
- `questmod.plugin.contract.guid_and_package`
- `questmod.patch.targets.zone_activate_postfix`
- `questmod.compat.point_wrapper_default_allow_installed`
- `questmod.quest_flow.bootstrap_defaults`
- `questmod.quest_state.lifecycle_roundtrip`
- `questmod.quest_state.dispatch_priority`
- `questmod.drama.catalog.placeholder_contract`
- `questmod.drama.feature_showcase.activate_smoke`
- `questmod.drama.feature_showcase_followup.activate_smoke`
- `questmod.debug.console.showcase_command`（DEBUGビルド時の手動検証ケース）
- `questmod.patch.compat.cwl_incompatible_scan`（能動チェック、既定 smoke 外）
