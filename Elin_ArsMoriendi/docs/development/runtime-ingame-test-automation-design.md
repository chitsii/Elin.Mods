# Runtime In-Game Test Automation Design

最終更新: 2026-02-26

## 1. 概要設計

### 1.1 目的

- ゲーム起動中の実挙動に対して自動テストを実行し、回帰を早期検出する。
- CWL の外部コンソール入力機能（`\\.\pipe\Elin\Console`）を利用し、ゲーム外からテストを駆動する。
- テスト基盤を共通化し、Ars Moriendi は「テスト定義のみ」を持つ。
- 本番ビルドにテスト基盤/テストコードを同梱しない。

### 1.2 非目的

- ユニットテスト（`dotnet test`）の置き換え。
- CI 上での完全ヘッドレス実行（初期段階ではローカル実行を前提）。
- 既存 Harmony パッチをテスト目的で増やすこと。

### 1.3 全体アーキテクチャ

1. 外部ランナー（PowerShell 一本化）
- Named Pipe に CWL コンソールコマンドを送信する。
- テスト結果 JSON ファイルを監視し、成功/失敗を判定する。

2. 共通テスト基盤（Script ベース）
- テストケース実行・アサート・結果収集・JSON 出力を提供する。
- 複数 Mod から再利用可能な共通レイヤ。

3. Mod 別スイート（Ars Moriendi 側）
- シナリオ固有の前提作成、検証、後始末のみを記述する。
- 共通基盤 API を呼ぶだけに限定する。

4. 実行経路
- 外部ランナー -> CWL pipe -> `cwl.cs.file`/`cwl.cs.eval` -> 共通基盤 -> Mod スイート -> 結果 JSON。

### 1.4 本番非同梱ポリシー

- 本番パッケージに含めるのは `Elin_ArsMoriendi.dll` と既存リソースのみ。
- テスト資産は以下に分離し、本番 deploy 対象外とする。
  - 共通（MVP は mod 内に配置）: `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_ArsMoriendi\tools\runtime-testkit\`
  - Ars: `tests\runtime\`
- `build.bat` は変更しない。テスト実行は別コマンド（例: `run_runtime_tests.ps1`）で行う。
- 運用簡素化のため、初期実装ではランナー言語を PowerShell のみとする。

## 2. 詳細設計

### 2.1 ディレクトリ設計

1. 共通基盤（リポジトリ共通）
- `tools/runtime-testkit/common/runtime_testkit.csx`
- `tools/runtime-testkit/common/runtime_assert.csx`
- `tools/runtime-testkit/common/runtime_result_writer.csx`
- `tools/runtime-testkit/runner/run_runtime_tests.ps1`

2. Ars Moriendi（Mod 個別）
- `tests/runtime/ars/suites/smoke.csx`
- `tests/runtime/ars/suites/compat.csx`
- `tests/runtime/ars/entry.csx`
- `tests/runtime/ars/runtime_test_manifest.json`

3. 出力
- `tests/runtime/_artifacts/<run_id>/result.json`
- `tests/runtime/_artifacts/<run_id>/console.log`

### 2.2 実行プロトコル

1. 外部ランナー開始
- Pipe `\\.\pipe\Elin\Console` に接続。

2. CWL readiness 確認
- `cwl.cs.is_ready`
- 失敗時は即終了（ゲーム未起動/CWL scripting 無効）。

3. セーブデータ実行ガード（必須）
- テスト開始前に `cwl.cs.eval` で現在セーブの実行許可条件を検証する。
- 既定条件:
  - PC 名が固定プレフィクス `RUNTIME_TEST` で始まること。
- 条件不一致ならテストを開始せず即 fail（exit code 非 0）。
- ガードは fail-close とし、判定不能（例外/値取得不可）も不許可扱いにする。

4. テスト起動
- `cwl.cs.file "<abs_path_to_entry.csx>"`
- `entry.csx` が manifest を読み、suite を順次実行。

5. 結果判定
- `result.json` の生成を待機。
- `summary.failed > 0` なら非 0 exit code で終了。
- タイムアウト既定値は 120 秒。超過時は `timeout` として fail。

### 2.3 共通テスト基盤 API

1. 主要型
- `RuntimeTestCase { string Id; Func<TestContext, TestResult> Run; }`
- `RuntimeTestSuite { string Name; IReadOnlyList<RuntimeTestCase> Cases; }`
- `TestContext { string RunId; string ModId; IDictionary<string, object> Bag; }`

2. 主要関数
- `RunSuite(RuntimeTestSuite suite, TestContext ctx)`
- `Assert.True(bool cond, string msg)`
- `Assert.Equal<T>(T expected, T actual, string msg)`
- `Assert.Fail(string msg)`
- `WriteResultJson(RuntimeRunResult result, string path)`

3. 失敗扱い
- アサート失敗
- 例外
- タイムアウト

### 2.4 Ars スイート設計

1. `smoke`
- Mod 有効化前提の確認。
- 主要 singleton / manager の取得可否。
- 主要パッチ適用状態の簡易確認（存在チェック）。

2. `compat`
- シグネチャ差分に弱い API の反射検証。
- 例: `Chara.Die` / `Chara.Revive` / `Trait.OnBarter` 周辺の前提検証。

3. `behavior`（第2段階）
- 従者生成/解除、SoulBind、PreserveCorpse などのシナリオ検証。
- 初期段階では対象を最小化し、flake を避ける。

### 2.5 汚染防止と後始末

1. 前提
- 専用セーブスロット（runtime test 用）でのみ実行する。
- 専用セーブの PC 名は `RUNTIME_TEST` プレフィクスを必須とする。

2. 各ケースの規約
- `Setup -> Run -> Cleanup` を必須化。
- Cleanup は `try/finally` で必ず実行する。

3. 失敗時
- 後続ケースを続行するが、run 全体は fail。
- 最終結果に失敗ケースと例外を記録。

### 2.6 結果 JSON スキーマ

```json
{
  "run_id": "20260226_231500_local",
  "mod_id": "chitsii.elin.ars_moriendi",
  "summary": { "total": 12, "passed": 11, "failed": 1, "duration_ms": 18234 },
  "suites": [
    {
      "name": "smoke",
      "cases": [
        { "id": "smoke.manager_alive", "status": "passed", "duration_ms": 12, "message": "" }
      ]
    }
  ]
}
```

### 2.7 実行コマンド案

1. ローカル
- `powershell -File Elin_ArsMoriendi\tools\runtime-testkit\runner\run_runtime_tests.ps1`

2. 複数 suite
- `powershell -File Elin_ArsMoriendi\tools\runtime-testkit\runner\run_runtime_tests.ps1`

3. ガード指定（将来拡張）
- `powershell -File Elin_ArsMoriendi\tools\runtime-testkit\runner\run_runtime_tests.ps1 -RequiredNameContains RUNTIME_TEST`

### 2.8 依存・実現可能性

1. 実現根拠
- CWL pipe サーバーは `\\.\pipe\Elin\Console` を公開している。
- 送信コマンドは ReflexCLI 経由で実行される。
- `cwl.cs.file` / `cwl.cs.eval` が利用可能。

2. 前提条件
- CWL の scripting が有効であること。
- ゲームが起動済みで、CWL 読み込み完了であること。

3. 不確実性と対策
- `.csx` の `#load` 動作に依存しすぎない。
- 必要なら runner 側で共通基盤 + suite を結合した一時スクリプトを生成して実行する。
- Nightly 差分は `compat` suite を最優先に更新し、機能テストより先に破断を検知する。

## 3. 再検討（無駄削減・運用最小化）

### 3.1 無駄の見直し

削減方針として、次は採用しない。

1. 専用 TestHost DLL の新規開発（初期段階）
- メリットはあるが、ビルド/配布/依存管理が増える。

2. 双方向 pipe プロトコルの独自実装
- 現段階では結果 JSON 出力で十分。

3. いきなり behavior 全自動化
- まず `smoke + compat` に限定し、運用負荷と flake を抑える。

### 3.2 実現可能性の再評価

- 技術的には実現可能。
- 最大の実務リスクは「ゲーム状態汚染」と「テストの不安定化」。
- これを専用セーブスロット運用と suite 分割で緩和する。

### 3.3 運用の手間をさらに減らす施策

1. 最初のリリースは `smoke` のみ必須、`compat` は任意実行にする。
2. manifest で `enabled: false` を持たせ、重いケースを簡単に無効化する。
3. テスト失敗時の再現コマンドを JSON に書き戻す。
4. 結果保存先を固定し、最新結果へのシンボリックリンク（`latest.json`）を作る。

### 3.4 段階導入計画（最小）

1. Phase 1
- 共通基盤最小 API + `smoke` 3ケース。

2. Phase 2
- `compat` 追加（Nightly/Stable で壊れやすい API 重点）。

3. Phase 3
- 主要 `behavior` を 1-2 シナリオずつ追加。

この順序で、実装量・運用負荷・保守性のバランスを最小コストで取る。

### 3.5 再検討後の採用案（最小構成）

再検討の結果、初回導入は次の 4 点だけを実装対象に固定する。

1. ランナーは `run_runtime_tests.ps1` のみ（多言語対応しない）。
2. suite は `smoke` のみ必須（`compat` は手動実行）。
3. 結果連携は `result.json` のみ（双方向通信を作らない）。
4. テスト資産は `tools/` と `tests/runtime/` のみ（本番 DLL 側にコードを追加しない）。
5. セーブガードは「PC 名プレフィクス（`RUNTIME_TEST`）」のみを強制し、未一致時は実行しない。

この固定により、実現可能性を維持しつつ、運用コストと保守対象を最小化する。

## 4. 実装計画（Runtime Test 基盤）

### 4.1 目標

- 最小コストで回帰検知を開始し、Nightly 追従の保守性を上げる。
- 判定が機械化できるケースのみを先行実装する。

### 4.2 フェーズ計画

1. Phase A: 基盤最小実装
- `run_runtime_tests.ps1` を実装する。
- `RUNTIME_TEST` プレフィクスガードを必須化する（fail-close）。
- `result.json` 出力と exit code 判定を実装する。
- 完了条件: ガード不一致時に必ず非 0 終了、空suite実行で正常終了。

2. Phase B: 静的寄り高価値ケース
- `compat.die_signature`
- `patch.health_chara_die`
- `drama.integrity`
- 完了条件: 3ケースが自動実行され、失敗時にケースID付きで記録される。

3. Phase C: コア挙動ケース（1）
- `behavior.preserve_corpse_basic`
- `behavior.soulbind_basic`
- 完了条件: `Before -> Act -> After` 差分判定が安定し、Cleanupまで通る。

4. Phase D: コア挙動ケース（2）
- `behavior.revive_variant_identity`
- `saveload.servant_persistence_smoke`
- 完了条件: Save/Load を含む再実行で false positive が許容範囲内。

### 4.3 テスト判定ルール（運用固定）

1. ケース fail 条件
- Assert 失敗
- 例外
- タイムアウト

2. ラン fail 条件
- `summary.failed > 0`
- Cleanup 失敗（`dirty fail`）

3. 正式結果の扱い
- `smoke/compat` はそのまま正式結果とする。
- `behavior` は原則クリーンセーブ復元後の実行結果のみ正式扱いとする。

### 4.4 優先順位（最初の 2 週間）

1. 週1
- Phase A 完了
- Phase B の `compat.die_signature` と `patch.health_chara_die` 完了

2. 週2
- Phase B の `drama.integrity` 完了
- Phase C の `behavior.preserve_corpse_basic` 着手

### 4.5 完了判定（MVP）

- ランナー単体で `smoke + compat` を 1 コマンド実行できる。
- Nightly で `Chara.Die` 破断とパッチ不整合を自動検知できる。
- セーブ汚染防止ガードが常時有効で、誤実行時にテストが走らない。
