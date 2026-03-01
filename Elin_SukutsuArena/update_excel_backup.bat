@echo off
REM Excel比較用バックアップを更新
REM ビルド後の差分確認が完了した後、次回のベースラインとして現在のExcelを保存

pushd tools
uv run python builder/excel_diff_manager.py backup --force
popd

echo.
echo バックアップを更新しました。次回ビルド時はこの状態との差分が表示されます。
