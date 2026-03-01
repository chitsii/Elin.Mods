# フェーズ運用ルール

このドキュメントはフェーズ値の管理と同期の方針を定義します。

## ソース・オブ・トゥルース

- クエスト側フェーズは `Elin_SukutsuArena.Quests.StoryPhase`（Generated）を使用する。
- フラグ側フェーズは `Elin_SukutsuArena.Flags.Phase`（Generated）を使用する。
- 両者は **ordinal と順序が完全一致**していることが前提。

## 利用ルール

- クエストの可否判定・進行・フェーズ遷移は `StoryPhase` を使う。
- 永続フラグや dialogFlags の読み書きは `Phase` を使う。
- `PlayerState.CurrentStoryPhase` は `CurrentPhase` と同値のビューとして扱う。

## 一致チェック

- プラグイン起動時に `ValidatePhaseEnums()` が **長さと ordinal の差分**を警告ログに出す。
- 不一致が出た場合は Generated を再生成するか、生成元を修正して整合させる。

## 追加フェーズ時の注意

- 新しいフェーズを追加する際は **両方の Generated enum を同時に更新**する。
- Generated 以外にローカルな `StoryPhase` を定義しない。
