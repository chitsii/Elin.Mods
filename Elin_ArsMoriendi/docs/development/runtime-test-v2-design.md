# Runtime Test V2 Design (Code-First)

## 目的
- Runtime テストを「コードファースト」で再構築する。
- 共通基盤 `runtime-test-v2` を利用し、Mod 側はケース実装に集中する。
- テストケース側の保守コストを最小化する。

## 非目的
- 既存 Runtime テスト基盤の即時置換。
- 既存ケースの一括移植。

## 新規フォルダ構成（V2）
```text
Elin_ArsMoriendi/
  tests/
    runtime/
      README.md
      src/
        cases/
          PatchTargetsCase.cs
        drama/
          ArsDramaRuntimeCatalog.cs
      _artifacts/
runtime-test-v2/
  README.md
  framework/
    suites/
      runtime_suite_v2_template.csx
    src/
      core/
      compat/
      drama/
  runner/
    run_runtime_suite_v2.ps1
    build_runtime_suite_v2.ps1
```

## 責務分離
### `runtime-test-v2/framework/src/core`
- `IRuntimeCase`: ケースの契約 (`Prepare/Execute/Verify/Cleanup`)。
- `RuntimeCaseBase`: 共通ログ・例外整形の基底実装。
- `RuntimeTestContext`: ケースごとの隔離コンテキスト。
- `RuntimeTestHost`: 実行順序、タイムアウト、cleanup 保証、結果集計。
- `RuntimeAssertions`: アサート関数群。
- `RuntimeTestRunnerV2`: ケース自動検出（`IRuntimeCase` 実装を反射で収集）と実行入口。

### `runtime-test-v2/framework/src/compat`
- `HarmonyCompatFacade`: `PatchInfo` の差異を吸収して Prefix/Postfix を列挙。
- `CwlCompatFacade`: `TestIncompatibleIl` など CWL API の差異を吸収。
- `ReflectionCompat`: バージョン差を吸収する反射ユーティリティ。

### `runtime-test-v2/framework/src/drama`
- `DramaCaseDefinition`: drama ケース定義と provider 契約（`IDramaCaseProvider`）。
- `DramaRuntimeRunnerV2`: provider 自動検出と drama 実行入口。

### `Elin_*/tests/runtime/src/cases`
- 1クラス1ケースで実装。
- ケースは「意図」と「検証対象」だけ記述し、反射・互換処理は `compat` に委譲。

### `Elin_*/tests/runtime/src/drama`
- `IDramaCaseProvider` 実装のみ配置（ケース定義データに集中）。

## 実行フロー
1. `run_runtime_suite_v2.ps1` が run id を生成。
2. `build_runtime_suite_v2.ps1` が `framework/src` + `ModRoot/tests/runtime/src`（互換: `tests/runtime_v2/ars/src`）を結合し `runtime_suite_v2_generated.csx` を生成。
3. CWL の `cwl.cs.file` で generated csx を実行。
4. `tests/runtime/_artifacts/<run_id>/result.json` を出力。

## Suite
- `Suite=drama`:
  - ドラマ再生専用ランナー（`DramaRuntimeRunnerV2`）を起動。
  - ケースごとに `Game.Load(基準セーブ)` を実行して分離。
  - quiet error（Error/Exception/Assert）と再生停止を検知。
- `Suite=smoke`:
  - 既存のパッチ形状スモーク（`RuntimeTestRunnerV2`）を起動。

## セーブ汚染ガード
- `PC.Name` に `RUNTIME_TEST` を含まない場合は即失敗。
- 破壊系ケースはタグ `destructive` を必須にし、デフォルト実行対象から除外。

## 再実行性
- `-CaseId` 単体実行を標準化。
- `-Tag` 指定（例: `smoke`, `compat`, `destructive`）を標準化。
- 失敗時は `case_id`, `step`, `exception`, `logs` を固定 JSON スキーマで記録。

## 移行手順（段階的）
1. V2 基盤の最小起動 (`RuntimeTestHost` + 1ケース)。
2. 既存 patch smoke から 1ケースずつ移植。
3. `tests/runtime` へ統合し、`tests/runtime_v2` は互換ラッパ化する。

## 実装ステータス
- `M1` 完了:
  - `RuntimeTestRunnerV2` / `RuntimeTestHost` / `RuntimeTestContext` を実装。
  - `HarmonyCompatFacade` / `CwlCompatFacade` / `ReflectionCompat` を追加。
  - `PatchTargetsCase` を V2 の初期ケースとして実装。
  - `runtime-test-v2/runner/run_runtime_suite_v2.ps1` で実行可能。
- `M2` 完了:
  - `Suite=drama` を追加し、ドラマ再生ランナーを実装。
  - `ArsDramaRuntimeCatalog` にドラマケース定義を実装。
  - ケースごとのセーブ再読込分離（`Game.Load`）を実装。
  - NPC前準備（`RequiredNpcIds`）とケース別フラグ前提（`SetupFlags`）を実装。
- `M3` 完了:
  - `core/compat/drama runner/template` を `runtime-test-v2/framework` へ共通化。
  - smoke/drama ともケース自動検出に移行し、Mod 側はケース実装中心へ簡素化。

## 設計上の重要ルール
- ケースで Harmony/CWL 生 API を直接触らない。
- ケースで LINQ 依存を増やさない（互換判定ノイズ低減）。
- すべてのケースで `cleanup` を `finally` で保証する。
