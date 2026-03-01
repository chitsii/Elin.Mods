# 更新時チェックリスト（ゲーム版アップデート時）

1. `python tools/builder/validate_source_headers.py sync` でバニラキャッシュ更新
2. `python ..\tools\elin_channel_tracker\run.py track-channels --report-json tools/elin_channel_tracker/reports/channel_snapshot.json --report-md tools/elin_channel_tracker/reports/channel_snapshot.md --no-legacy-shim` を実行し、stable/nightly のヘッドを記録
3. Stable チャンネルで `python ..\tools\elin_channel_tracker\run.py collect-signatures --targets tools/elin_channel_tracker/config/compat_targets.json --output tools/elin_channel_tracker/config/stable_signatures.json` を実行
4. Nightly チャンネルで `python ..\tools\elin_channel_tracker\run.py collect-signatures --targets tools/elin_channel_tracker/config/compat_targets.json --output tools/elin_channel_tracker/config/nightly_signatures.json` を実行
5. `python ..\tools\elin_channel_tracker\run.py verify-compat --targets tools/elin_channel_tracker/config/compat_targets.json --stable-signatures tools/elin_channel_tracker/config/stable_signatures.json --nightly-signatures tools/elin_channel_tracker/config/nightly_signatures.json --report-json tools/elin_channel_tracker/reports/compat_report.json --report-md tools/elin_channel_tracker/reports/compat_report.md --no-legacy-shim` を実行し、`broken` が 0 であることを確認（`risky` は通知扱い）
6. `python ..\tools\elin_channel_tracker\run.py detect-target-gaps --targets tools/elin_channel_tracker/config/compat_targets.json --src-root src --report-json tools/elin_channel_tracker/reports/target_gap_report.json --report-md tools/elin_channel_tracker/reports/target_gap_report.md --fail-on-missing` を実行し、`missing_targets` が 0 であることを確認
7. `build.bat debug` でビルド（検証ステップで差異があれば停止）
8. `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical` を実行し、`failed` が 0 であることを確認
9. ゲームを起動し、`Compat warmup` / `Compat fallback match` / Harmony パッチ失敗ログ / 例外ログを確認
10. パッチ対象のメソッドが存在するか確認（特に `ActPlan._Update`）
11. ゲーム内で機能が正しく動作するか確認（特に召喚/復活時の `GetNearestPointCompat` ルート）
12. リリース前に `build.bat` で Release ビルドを実行

## 月次互換メンテ運用（毎月1回）

1. `python ..\tools\elin_channel_tracker\run.py track-channels --report-json tools/elin_channel_tracker/reports/channel_snapshot.json --report-md tools/elin_channel_tracker/reports/channel_snapshot.md --no-legacy-shim` を実行してヘッド更新
2. Stable チャンネルで `python ..\tools\elin_channel_tracker\run.py collect-signatures --targets tools/elin_channel_tracker/config/compat_targets.json --output tools/elin_channel_tracker/config/stable_signatures.json` を実行
3. Nightly チャンネルで `python ..\tools\elin_channel_tracker\run.py collect-signatures --targets tools/elin_channel_tracker/config/compat_targets.json --output tools/elin_channel_tracker/config/nightly_signatures.json` を実行
4. `python ..\tools\elin_channel_tracker\run.py verify-compat --targets tools/elin_channel_tracker/config/compat_targets.json --stable-signatures tools/elin_channel_tracker/config/stable_signatures.json --nightly-signatures tools/elin_channel_tracker/config/nightly_signatures.json --report-json tools/elin_channel_tracker/reports/compat_report.json --report-md tools/elin_channel_tracker/reports/compat_report.md --no-legacy-shim` を実行して判定確認
5. `python ..\tools\elin_channel_tracker\run.py detect-target-gaps --targets tools/elin_channel_tracker/config/compat_targets.json --src-root src --report-json tools/elin_channel_tracker/reports/target_gap_report.json --report-md tools/elin_channel_tracker/reports/target_gap_report.md --fail-on-missing` を実行し、`missing_targets` が 0 であることを確認
6. `tools/elin_channel_tracker/reports/compat_report.md` を確認し、`broken` を 0 にする（`risky` は内容別に対応判断）
7. `tools/elin_channel_tracker/reports/target_gap_report.md` を確認し、`configured_not_detected` と `check_kind_mismatches` を棚卸しする
8. レポートに応じて Compat を整備（例: `tools/elin_channel_tracker/config/compat_targets.json`, `src/Compat/*.cs`, `src/Patches.cs`）
9. `build.bat debug` でビルド
10. `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke` を実行し、`failed` が 0 であることを確認
11. 必要時のみ能動互換チェックを個別実行: `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId patch.compat.cwl_incompatible_scan`
12. 能動互換チェックを実行した場合はゲーム再起動後に通常プレイ/再テストを行う

最終確認: 2026-02-28
