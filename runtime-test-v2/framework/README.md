# runtime-test-v2/framework

共有の Runtime Test V2 基盤コードです。

## 含まれるもの
- `src/core`: テストホスト、ケース契約、アサート、実行ランナー
- `src/compat`: Harmony/CWL/Reflection 互換層
- `src/drama`: drama ランナー、drama ケース定義契約
- `suites`: 共通 csx テンプレート

## 実行時の注意（PowerShell）
PowerShell で `run.ps1` / runner を直接実行する場合、実行ポリシー制限で失敗することがあります。

- 推奨:
  - `powershell -ExecutionPolicy Bypass -File .\tests\runtime_v2\run.ps1 -Suite smoke -Tag smoke`
- またはセッション限定で許可:
  - `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`

