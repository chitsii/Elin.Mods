# ログ方針

- `[Info]`: 正常動作の確認ログ
- `[Warning]`: 想定外だが動作継続可能
- `[Error]`: 機能が無効化された

## DEBUGログ（クエスト基盤）

- `DramaRuntime.ResolveFlag`: `quest_check` を含む依存条件評価の結果を出力する。
- `DramaRuntime.ResolveRun`: `quest_try_start` を含む依存コマンド実行を出力する。
- `QuestBridge.CanStartDrama`: ドラマ開始可否判定（開始済みフラグと結果）を出力する。
- `QuestBridge.TryStartDrama`: 開始成功/既開始スキップ/失敗を出力する。

最終確認: 2026-02-17
