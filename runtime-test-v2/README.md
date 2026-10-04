# runtime-test-v2

リポジトリ共通の Runtime Test V2 ランナーです。  
各 Mod は `tests/runtime/`（旧: `tests/runtime_v2/`）にケース実装を置き、この共有ランナーから実行します。

## 役割
- `runner/build_runtime_suite_v2.ps1`:
  `framework/src`（共通基盤）と指定 Mod (`-ModRoot`) のケース実装（`tests/runtime/src` 優先、次点で `tests/runtime_v2/ars/src`）を結合し、実行用 .cs を生成。
- `runner/run_runtime_suite_v2.ps1`:
  CWL pipe 経由で .cs を実行し、結果 JSON を待機・集計。実行ごとに `Player.log` の追記分を `_artifacts/<run_id>/playerlog.diff.log` へ保存。

## 共有対象
- `framework/src/core`: ホスト・アサート・コンテキスト・smoke ランナー
- `framework/src/core`: ホスト・アサート・コンテキスト・smoke ランナー
  - `IRuntimeCoroutineCase` を実装すると、smoke suite でも `yield return null` を使う frame-based case を追加できる
- `framework/src/compat`: Harmony/CWL/Reflection 互換層
- `framework/src/drama`: drama ランナーと `DramaCaseDefinition` 契約
- `framework/suites`: 共通 csx テンプレート

## 実行例（リポジトリルートから）
```powershell
powershell -ExecutionPolicy Bypass -File .\runtime-test-v2\runner\run_runtime_suite_v2.ps1 `
  -ModRoot .\Elin_ArsMoriendi `
  -Suite drama
```

```powershell
powershell -ExecutionPolicy Bypass -File .\runtime-test-v2\runner\run_runtime_suite_v2.ps1 `
  -ModRoot .\Elin_ArsMoriendi `
  -Suite drama `
  -CaseId ars_first_soul
```

```powershell
powershell -ExecutionPolicy Bypass -File .\runtime-test-v2\runner\run_runtime_suite_v2.ps1 `
  -ModRoot .\Elin_ArsMoriendi `
  -Suite smoke `
  -CaseId patch.targets.spawnloot_and_oncharadie
```

## 備考
- `-ModRoot` を省略した場合はカレントディレクトリを ModRoot とみなします。
- 各 Mod 側に `tests/runtime/run.ps1` の薄いラッパを置く運用を推奨します（旧 `tests/runtime_v2/run.ps1` 互換でも可）。
- PowerShell 実行ポリシー制限がある環境では `-ExecutionPolicy Bypass` 付きで実行してください。
  - 例: `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag smoke`
  - もしくはセッション限定で `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`
- `Player.log` の既定パスは `C:\Users\<User>\AppData\LocalLow\Lafrontier\Elin\Player.log` です。  
  別環境では `-PlayerLogPath` で明示指定できます。追記ログ保存が不要な場合は `-DisablePlayerLogDiff` を指定してください。
- `runtime_suite_v2_generated.cs` は既定で `_artifacts` に保存しません（実行後に一時ファイルを削除）。必要時のみ `-KeepGeneratedSource` を指定してください。
- `playerlog.diff.log` にはその run で `Player.log` に追記された行を保存します。

## スクリプトAPIと一時ハーネス

- 共通ランナーの `-ScriptBackend Auto` は `cs.version` の期限付き応答から現行APIを判定します。明示的なunknown-command応答がある場合だけ `cwl.cs.is_ready` へ切り替えます。接続/応答タイムアウトで再送やfallbackはしません。
- 現行APIは `cs.file`、旧APIは `cwl.cs.file` を使い、生成ファイルはどちらも `.cs`。ファイル実行要求は一回だけです。`-CommandTimeoutSeconds` は実行要求の応答期限、`-TimeoutSeconds` はsuite JSONの待機期限です。
- `transport.json` と失敗時 `runner-error.json`、Player.log差分を保持します。acknowledgementはsuite成功を意味しません。
- 現環境の空キューで終了するEPipeを検証する一時ハーネスは [harness/README.md](harness/README.md)。導入/撤去はゲーム停止後に限定します。ゲーム稼働中のqueue復活、製品DLL変更、セキュリティ設定変更は行いません。
