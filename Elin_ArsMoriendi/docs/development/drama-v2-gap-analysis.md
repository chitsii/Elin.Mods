# Drama v2 ギャップ分析 (ArsMoriendi)

最終確認: 2026-03-02

## 目的

`Elin_QuestMod/tools/drama_v2` を `Elin_ArsMoriendi/tools/drama_v2` にコピーして、既存 Ars ドラマを再現できるかを事前評価する。

## 比較対象

1. 既存 Ars ビルダー  
   `Elin_ArsMoriendi/tools/drama/drama_builder.py`
2. 既存 Ars シナリオ群  
   `Elin_ArsMoriendi/tools/drama/scenarios/*.py`
3. 新 DSL 実装  
   `Elin_QuestMod/tools/drama_v2/drama_dsl.py`

## 比較方法

1. Ars シナリオで使われている `builder.*` 呼び出しを AST 抽出
2. `DramaDsl` の公開メソッドを抽出
3. 名前一致で機械比較し、さらに仕様差（依存ランタイム、条件式、選択肢構造）を手作業で確認

## 定量結果

- Ars シナリオの `builder.*` 呼び出し: **35種類 / 826回**
- `drama_v2` と同名でそのまま使えるもの: **4種類**
  - `say`, `wait`, `shake`, `glitch`
- 直接不足（同名なし）: **31種類**

## 不足 API 一覧（Ars 既存シナリオ実使用）

| API | 呼び出し回数 |
|---|---:|
| action | 3 |
| add_actor | 1 |
| branch_if | 6 |
| choice | 44 |
| choice_block | 4 |
| choice_dynamic | 5 |
| choice_if | 6 |
| cond_flag_equals | 2 |
| cond_has_flag | 2 |
| cond_no_flag | 4 |
| conversation | 101 |
| drama_end | 14 |
| drama_start | 5 |
| fade_in | 10 |
| fade_out | 9 |
| finish | 6 |
| jump | 68 |
| label | 112 |
| on_cancel | 17 |
| play_bgm | 17 |
| play_bgm_with_fallback | 8 |
| play_sound | 1 |
| quest_check | 19 |
| register_actor | 29 |
| resolve_flag | 2 |
| resolve_run | 11 |
| say_if | 2 |
| set_flag | 27 |
| show_book | 1 |
| step | 112 |
| switch_on_flag | 2 |

## 再現上の主要ブロッカー

### 1) ランタイム依存キー解決

- 既存 Ars は `quest_check / resolve_flag / resolve_run` を多用
- 実装は `Elin_CommonDrama.DramaRuntime` を呼ぶ
- 例:
  - `ars_hecatia_talk.py` の `resolve_flag`, `resolve_run`
  - `ars_apotheosis.py` の `resolve_run(CueKeys.*)`

この層がないと、開始条件判定・キュー実行・依存コマンド実行が落ちる。

### 2) 既存 builder の Step/Label 型フロー

- Ars シナリオは `label -> step -> jump/choice` を前提に広範囲で記述済み
- `drama_v2` は `node/story` 方式で別モデル
- 既存コードを無改修で動かすには互換レイヤーが必須

### 3) 条件付き選択肢と条件付き行

- `choice_if`, `choice_dynamic`, `say_if`, `cond_*` ヘルパー使用あり
- `drama_v2` 側へ同等の条件表現変換が必要

### 4) ChoiceReaction ベース分岐

- `choice_block + ChoiceReaction` を 3 シナリオで利用
- 互換 API なしでは移植できない

### 5) BGM フォールバック挙動

- `play_bgm_with_fallback` 使用 8 回
- `transition(bgm=...)` 単体では同等のフォールバック仕様を満たせない

## 名前は違うが変換可能な領域

以下はシナリオ書き換え前提なら置換可能:

- `label/step/jump/finish` -> `node/go/end`
- `choice/on_cancel` -> `dialog(...choices..., cancel=...)`
- `conversation` -> `dialog(lines=...)` か `say` 連結
- `branch_if/switch_on_flag` -> `when + has_flag`

## 追加差分（移行時に必ず出る）

1. テキストID
   - 既存: 手動 `text_id`
   - v2: 自動生成
   - Excel 差分で `id` 列は不一致になる前提
2. 既存テスト
   - `tools/drama/tests/test_common_runtime_api.py` は `quest_check` 前提
   - v2 移行時はテスト更新か互換層提供が必要

## 結論

`drama_v2` の単純コピーでは既存 Ars ドラマは再現不可。  
先に **Ars 互換レイヤー**（不足 31 API のうち実使用分）を作る必要がある。

推奨順:

1. `tools/drama_v2` に互換 API 層を追加（既存シナリオが import 差し替えだけで動く段階）
2. 既存 20 シナリオを一括ビルド
3. 旧 builder 生成物との Excel 差分テストを追加
4. その後に段階的に `node/story` へシナリオ書き換え

