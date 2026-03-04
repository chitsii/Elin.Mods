# Drama v2 移行計画 (ArsMoriendi 限定)

最終更新: 2026-03-02

## 目的

- 対象を `Elin_ArsMoriendi` のみへ限定して、`tools/drama` から `tools/drama_v2` へ段階移行する。
- 大規模な一括置換は避け、互換レイヤー経由で安全に標準化する。

## スコープ

### 対象

- `Elin_ArsMoriendi/tools/drama/scenarios/*.py`
- `Elin_ArsMoriendi/tools/drama_v2/*` (新規)
- `Elin_ArsMoriendi/tools/drama/tests/*` のうち移行ゲートに必要なもの
- 生成物: `LangMod/EN/Dialog/Drama/*.xlsx`

### 非対象

- `Elin_QuestMod` 本体のAPI再設計
- `Elin_SukutsuArena` 向け `ArenaModCommands`
- ゲーム内仕様変更や新演出追加

## 方針

1. 先に API 標準を固定する。  
   - `ModCommands` + `ArsModCommands` を Ars 側の唯一のカスタムコマンド入口にする。
   - `DramaBuilder` は共通ライブラリ（submodule）として運用し、Ars 専用APIは含めない。
2. 互換レイヤーを先に作る。  
   - 既存シナリオを即座に書き換えず、旧 builder 呼び出しを新 API へ委譲する。
3. 変更の安全性はテストゲートで担保する。  
   - build 成功、Excel差分、runtime smoke の3点で判定する。

## Phase 0.5: 共通ライブラリ化 (submodule)

作業:

- `drama_v2` 共通ライブラリを独立repo化し、`Elin_ArsMoriendi` では submodule として導入する。
- 共通ライブラリに含める範囲を固定する。
  - `DramaBuilder` (純CWL)
  - `ModCommands` (base)
- Ars 専用実装は Ars リポジトリ側へ残す。
  - `ArsModCommands`
  - 互換レイヤー (`tools/drama_v2/compat`)

完了条件:

- Ars 側の import は submodule の `drama_v2` を参照する。
- 共通ライブラリ更新は submodule SHA 更新で追跡できる。

## フェーズ計画

## Phase 0: ベースライン固定

作業:

- 現行 `tools/drama` で全シナリオをビルドし、基準Excelを確定。
- 既存 runtime テストの結果を記録。
- 既知の差分許容ルールを明文化。
  - 例: text id 自動化で `id` 列差分が出るケース。

完了条件:

- 現行状態で「壊れていない基準」が再現可能。
- 差分比較対象ファイル一覧が固定されている。

## Phase 1: 基盤導入 (Ars専用)

作業:

- `tools/drama_v2` を Ars 側へ配置。
- `ModCommands` 実装を追加。
  - `resolve_flag / resolve_run / resolve_flags_all / resolve_flags_any`
  - `quest_check / quest_try_start* / quest_complete`
- `ArsModCommands(ModCommands)` 実装を追加。
  - `state.quest.is_complete*`
  - `state.erenos.is_borrowed`
  - `cmd.erenos.* / cmd.hecatia.*`
  - `cue.apotheosis.*`
- `cmd.scene.stop_bgm` は純CWL `stopBGM` 利用に統一。

完了条件:

- Ars resolver のキー群を新コマンドAPIから全て表現できる。
- 低レベルAPIを直接 `eval` 文字列で書かなくても同等操作が可能。

## Phase 2: 互換レイヤー実装

作業:

- 旧 `DramaBuilder` 互換アダプタを `tools/drama_v2/compat` に実装。
- 既存シナリオの import 差し替えのみで動作する最小互換を作る。
- 旧APIから新APIへのマッピング表を作成。
  - `quest_check -> ModCommands.quest_check`
  - `resolve_run -> ModCommands.resolve_run`
  - `play_pc_effect_with_sound -> ModCommands.play_pc_fx_with_sfx`
  - など

完了条件:

- 既存 Ars シナリオ群が、大規模書き換えなしでビルド可能。
- 互換層を通しても機能回帰がない。

## Phase 3: テストゲート構築

作業:

- 単体テスト:
  - `ModCommands` の compile 出力検証
  - `ArsModCommands` のキー検証
- 統合テスト:
  - 全シナリオ一括 build テスト
  - 旧生成物との Excel diff テスト
- runtime smoke:
  - カタログ登録ドラマ起動
  - `quest_check / resolve_run / cue` 実行経路確認

完了条件:

- CI相当のゲートで回帰を検出可能。
- 「移行して良い/戻すべき」が自動判定できる。

## Phase 4: シナリオ段階移行

作業:

- 優先順で `node/story` 記法へ移行。
  - まず小規模シナリオ
  - 次に `ars_apotheosis`
  - 最後に大型 (`ars_hecatia_talk`)
- 各シナリオ移行後に build + diff + runtime smoke を実施。

完了条件:

- 対象シナリオが新記法へ移行済み。
- 演出/分岐/接続が旧版と同等である。

## Phase 5: 収束と削除

作業:

- 互換レイヤーへの依存をゼロにする。
- 旧 `tools/drama` 廃止計画を実行。
- ドキュメント更新:
  - 標準API
  - 移行済み/非移行一覧
  - 運用手順

完了条件:

- Ars 側のドラマ標準は `drama_v2` のみ。
- 旧builderに戻らなくても保守可能。

## 受け入れ基準 (Definition of Done)

- 全 Ars ドラマが `drama_v2` 経由でビルド可能。
- 主要ドラマの runtime smoke が成功。
- カスタムコマンドは `ArsModCommands` に集約され、シナリオ直書きがない。
- `cmd.scene.stop_bgm` 依存が除去され、純CWL `stopBGM` へ統一されている。

## リスクと対策

- リスク: 大型シナリオの移行で差分が膨らむ。  
  - 対策: 小規模から段階移行し、diff許容ルールを先に固定する。
- リスク: 互換層が長期残存して複雑化する。  
  - 対策: Phase 5 の削除条件を先に明示し、期限付きで運用する。
- リスク: ランタイム依存キーの取りこぼし。  
  - 対策: `ArsDramaResolver` と `schema/key_spec.py` を source of truth とし、キー一覧テストを追加する。

## Ars への移行手順 (1シナリオ単位)

1. 対象シナリオを1本だけ選ぶ（同時に複数本は移行しない）。
2. import を共通 `drama_v2` + Ars 専用 `ArsModCommands` 構成へ切り替える。
3. 旧 builder 互換APIは `compat` 経由に限定し、シナリオ直書き `eval` を増やさない。
4. `build.bat` を実行し、fail-fast 検証を通す。
5. 差分が必要な変更のみ `build.bat regen` を実行して生成物を更新する。
6. 生成物差分を確認し、許容差分ルールに照らしてレビューする。
7. runtime smoke で該当ドラマ導線を確認してから次のシナリオへ進む。

## ArsModCommands 実装・確認チェックリスト

- source of truth 突合:
  - `src/Drama/ArsDramaResolver.cs`
  - `tools/drama/schema/key_spec.py`
- 必須実装:
  - `state.quest.is_complete*`
  - `state.erenos.is_borrowed`
  - `cmd.erenos.* / cmd.hecatia.*`
  - `cue.apotheosis.*`
- 除外確認:
  - `cmd.scene.stop_bgm` は実装しない（純CWL `stopBGM` を使う）。
- compile 展開確認:
  - `resolve_*` 系が `eval` へ展開されること
  - actor 指定時に `actor` 列が保持されること

## `build.bat` 実行後の差分確認方法

前提:

- 通常は `build.bat` を実行する（生成物更新は行わない）。
- 生成物更新が必要なときだけ `build.bat regen` を使う。

確認手順:

1. `build.bat` 実行結果で fail-fast（キー/テスト/C#）の通過を確認。
2. 生成物を更新する必要がある変更のみ `build.bat regen` を実行。
3. 差分対象を `LangMod/EN/Dialog/Drama/*.xlsx` に限定して比較。
4. レビュー観点を固定する。
   - 期待差分: text id 自動化（常に発生）、ノード接続の正規化、空セル整理
   - 非期待差分: 分岐先変更、条件式欠落、action/param の意味変更
5. 非期待差分が1件でもあれば移行を止めて修正してから再実行。

補足:

- drama_v2 移行時は text ID 自動生成仕様が旧実装と異なるため、`id` 列差分は必ず発生する前提で判定する。
- 差分レビューは `id` 列の一致ではなく、`jump/if/action/param/text_*` の意味一致を優先する。

## 直近の実行順 (次の3タスク)

1. `drama_v2` 共通ライブラリを submodule 前提で整備し、`ModCommands` (base) を実装する。  
2. Ars 側に `ArsModCommands` と 旧 builder 互換アダプタを追加し、既存シナリオ1本を import 差し替えでビルド確認する。  
3. `build.bat --check` + Excel diff + runtime smoke の自動テストを先に整備する。
