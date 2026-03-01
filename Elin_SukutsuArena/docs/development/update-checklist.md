# 更新時チェックリスト（ゲーム版アップデート時）

前提: tracker 実体は monorepo 共通の `..\tools\elin_channel_tracker\run.py` を使う。

1. `cd tools && uv run python builder/extract_dependencies.py -q` を実行して依存マップを更新
2. `cd tools && uv run python builder/verify_game_api.py --ci` を実行して High リスク依存の破損を確認
3. `python ..\tools\elin_channel_tracker\run.py track-channels --report-json tools/elin_channel_tracker/reports/channel_snapshot.json --report-md tools/elin_channel_tracker/reports/channel_snapshot.md --no-legacy-shim` を実行し、stable/nightly のヘッドを記録
   - `origin/stable` / `origin/nightly` が無い環境では `HEAD` へ自動フォールバックされる
4. `python ..\tools\elin_channel_tracker\run.py verify-compat --targets tools/elin_channel_tracker/config/compat_targets.json --stable-signatures tools/elin_channel_tracker/config/stable_signatures.json --nightly-signatures tools/elin_channel_tracker/config/nightly_signatures.json --report-json tools/elin_channel_tracker/reports/compat_report.json --report-md tools/elin_channel_tracker/reports/compat_report.md --no-legacy-shim` を実行し、`broken` が 0 であることを確認（`risky` は通知扱い）
5. `python ..\tools\elin_channel_tracker\run.py detect-target-gaps --targets tools/elin_channel_tracker/config/compat_targets.json --src-root src --report-json tools/elin_channel_tracker/reports/target_gap_report.json --report-md tools/elin_channel_tracker/reports/target_gap_report.md --fail-on-missing` を実行し、`missing_targets` が 0 であることを確認
6. `build.bat debug` を実行してビルド
7. ゲーム内で最小導線を 1 回実行（セーブロード -> アリーナ入場 -> 戦闘開始/終了 -> ロビー復帰）
8. `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical` を実行し、`failed` が 0 であることを確認
9. 必要時のみ能動互換チェックを個別実行: `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId patch.compat.cwl_incompatible_scan`
10. リリース前に `build.bat` で Release ビルドを実行

## 月次互換メンテ運用（毎月1回）

1. `python ..\tools\elin_channel_tracker\run.py track-channels --report-json tools/elin_channel_tracker/reports/channel_snapshot.json --report-md tools/elin_channel_tracker/reports/channel_snapshot.md --no-legacy-shim` を実行して channel ヘッドを更新
   - `origin/stable` / `origin/nightly` が無い環境では `HEAD` へ自動フォールバックされる
2. `python ..\tools\elin_channel_tracker\run.py verify-compat --targets tools/elin_channel_tracker/config/compat_targets.json --stable-signatures tools/elin_channel_tracker/config/stable_signatures.json --nightly-signatures tools/elin_channel_tracker/config/nightly_signatures.json --report-json tools/elin_channel_tracker/reports/compat_report.json --report-md tools/elin_channel_tracker/reports/compat_report.md --no-legacy-shim` を実行して判定確認
3. `python ..\tools\elin_channel_tracker\run.py detect-target-gaps --targets tools/elin_channel_tracker/config/compat_targets.json --src-root src --report-json tools/elin_channel_tracker/reports/target_gap_report.json --report-md tools/elin_channel_tracker/reports/target_gap_report.md --fail-on-missing` を実行して `missing_targets` が 0 であることを確認
4. `tools/elin_channel_tracker/reports/compat_report.md` を確認し、`broken` を 0 にする（`risky` は内容別に対応判断）
5. `tools/elin_channel_tracker/reports/target_gap_report.md` を確認し、`configured_not_detected` と `check_kind_mismatches` を棚卸しする
6. レポートに応じて Compat を整備（例: `tools/elin_channel_tracker/config/compat_targets.json`, パッチ対象定義, CWL反射フォールバック）
7. `build.bat debug` を実行
8. ゲーム内で最小導線を 1 回実行
9. `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical` を実行し、`failed` が 0 であることを確認

最終確認: 2026-02-28
