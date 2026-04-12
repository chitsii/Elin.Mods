---
type: chapter-registry
story: kaeranakata-ensoku
---

# Chapters

## Registry

| # | Title | POV | Status | Est. Play Time | File |
|---|-------|-----|--------|----------------|------|
| 0 | 依頼 | protagonist | outline | 20min | [chapter-00.md](chapter-00.md) |
| 1 | 水石の層 | protagonist | outline | 45min | [chapter-01.md](chapter-01.md) |
| 2 | 反響の層 | protagonist | outline | 50min | [chapter-02.md](chapter-02.md) |
| 3 | 花の層 | protagonist | outline | 40min | [chapter-03.md](chapter-03.md) |
| 4 | 再会 | protagonist | outline | 35min | [chapter-04.md](chapter-04.md) |
| 5 | 帰還と選択 | protagonist | outline | 30min | [chapter-05.md](chapter-05.md) |

## Total Estimated Play Time: 3.5-4.5 hours

## Flag Design

Ars Moriendi の `EClass.player.dialogFlags` 規則に準拠。

### Naming Convention

```
chitsii.elinikki.[category].[subcategory].[flag_name]
```

プレフィックス: `chitsii.elinikki.`
格納先: `EClass.player.dialogFlags` (IDictionary<string, int>)
アクセス: `DialogFlagStore.GetInt`, `SetInt`, `IsTrue`, `SetBool`

### quest.stage ── メイン進行（int enum）

| Value | Stage | Description |
|-------|-------|-------------|
| 0 | not_started | 未開始 |
| 1 | accepted | 依頼受諾。ミナ/ソラ合流 |
| 2 | layer1_clear | 水石の層通過 |
| 3 | layer2_clear | 反響の層通過 |
| 4 | layer3_clear | 花の層通過 |
| 5 | yuu_found | ユウ発見・再会 |
| 6 | returned | 帰還完了 |
| 7 | ending_seen | エンディング到達 |

```
chitsii.elinikki.quest.stage = 0..7
```

### quest.event.* ── 一回性イベント（bool: 0/1）

痕跡を調べた記録。truth は帰還後にユウから真相を聞いた記録。

```
# 痕跡発見（調べたか）
chitsii.elinikki.quest.event.trace_marks        # 壁の刻み目
chitsii.elinikki.quest.event.trace_channel       # 水路の溝
chitsii.elinikki.quest.event.trace_stones        # 石の配置
chitsii.elinikki.quest.event.trace_echo          # 反響音（段階4到達）
chitsii.elinikki.quest.event.trace_map           # 床の地図
chitsii.elinikki.quest.event.trace_shadow        # 壁の人影
chitsii.elinikki.quest.event.trace_flowers       # 花畑
chitsii.elinikki.quest.event.trace_weave         # 編み物

# 真相を聞いた（帰還後のユウとの会話）
chitsii.elinikki.quest.event.truth_marks
chitsii.elinikki.quest.event.truth_channel
chitsii.elinikki.quest.event.truth_stones
chitsii.elinikki.quest.event.truth_echo
chitsii.elinikki.quest.event.truth_map
chitsii.elinikki.quest.event.truth_shadow
chitsii.elinikki.quest.event.truth_flowers
chitsii.elinikki.quest.event.truth_weave
```

### quest.state.* ── 状態条件（bool: 0/1）

```
chitsii.elinikki.quest.state.journal_found       # ユウの手帳を拾った
chitsii.elinikki.quest.state.echo_experiment     # 反響実験の段階（int: 0-4）
chitsii.elinikki.quest.state.revisit             # 再訪エンド用。帰還エンド到達済み
```

### quest.ending ── エンディング到達（int enum）

| Value | Ending | Condition |
|-------|--------|-----------|
| 0 | none | 未到達 |
| 1 | return | 帰還エンド（truth 全取得） |
| 2 | silence | 沈黙エンド（truth 0） |
| 3 | revisit | 再訪エンド（帰還後に再入場） |

```
chitsii.elinikki.quest.ending = 0..3
```

### Flag Count Summary

| Category | Count | Notes |
|----------|-------|-------|
| quest.stage | 1 (int 0-7) | メイン進行 |
| quest.event.trace_* | 8 | 痕跡発見 |
| quest.event.truth_* | 8 | 真相を聞いた |
| quest.state.* | 3 | 状態条件 |
| quest.ending | 1 (int 0-3) | エンド到達 |
| **Total keys** | **21** | |

### Ending Resolution Logic

エンディングは truth フラグの取得数で3種類に分岐する。中間状態（1-7）は「沈黙エンドの変種」として扱い、専用のテキスト差分は作らない。

```
truth_count = sum of all quest.event.truth_* flags

if truth_count == 8:
    quest.ending = 1 (return)     # 全部聞いた
elif truth_count == 0:
    quest.ending = 2 (silence)     # 何も聞かなかった
else:
    # 1-7個聞いた: 沈黙エンドと同じ扱い
    # 「聞きたかったが全ては聞かなかった」状態
    # 沈黙エンドの演出/テキストを流用
    quest.ending = 2 (silence, partial)

# 再訪エンド (隠し): 帰還エンド後にネフィア再入場
if quest.ending == 1 AND player re-enters nefia:
    quest.ending = 3 (revisit)
```

**設計判断**: 中間状態に専用テキストを用意すると実装・翻訳コストが膨らむ。truth 1-7 は「結局踏み込めなかった」状態として沈黙エンドに吸収する。ただし帰還道中の会話は truth_count で段階的に変化する（chapter-05参照）。

### Quest Stage Transitions

`quest.stage` は以下のタイミングで更新される。

| Stage | Value | Trigger | 設定場所 |
|-------|-------|---------|---------|
| not_started | 0 | 初期値 | - |
| accepted | 1 | chapter-00: ミナの依頼受諾drama完了時 | 0-1 依頼受諾drama末尾 |
| layer1_clear | 2 | chapter-01: 水石の層から反響の層への zone 遷移時 | zone transition hook |
| layer2_clear | 3 | chapter-02: 反響の層から花の層への zone 遷移時 | zone transition hook |
| layer3_clear | 4 | chapter-03: 花の層からユウの野営地への zone 遷移時 | zone transition hook |
| yuu_found | 5 | chapter-04: ユウとの再会drama完了時 | 再会drama末尾 |
| returned | 6 | chapter-05: ネフィア入口マップへの zone 遷移時 | zone transition hook |
| ending_seen | 7 | エンディング演出完了時 | ending drama末尾 |

### Drama Trigger Mapping

| Trigger Type | Flag Check | Drama Action |
|---|---|---|
| 調べる: 壁の刻み目 | `trace_marks == 0` | ソラ推理会話 → set trace_marks = 1 |
| 入場: 反響の層 | `echo_experiment == 0` | 反響段階1（気づき） → set echo_experiment = 1 |
| 地点に立つ: 反響地点A | `echo_experiment == 1` | 反響段階2（最初の実験） → set echo_experiment = 2 |
| 地点に立つ: 反響地点B | `echo_experiment == 2` | 反響段階3（検証） → set echo_experiment = 3 |
| 地点に立つ: 反響地点C | `echo_experiment == 3` | 反響段階4（確信） → set echo_experiment = 4, trace_echo = 1 |
| 調べる: 花畑中央 | `trace_flowers == 0` | ソラ推理会話 → set trace_flowers = 1 |
| 会話: ユウに壁の真相を聞く | `trace_marks == 1 AND truth_marks == 0` | 真相会話 → set truth_marks = 1 |
| zone遷移: 各層のクリア | `quest.stage` ごとに更新 | 次stageへ進行 |
| zone遷移: 帰還後にネフィア再入場 | `quest.ending == 1` | 再訪エンド開始 → set quest.ending = 3 |

### Drama Trigger Mapping

| Trigger Type | Flag Check | Drama Action |
|---|---|---|
| 調べる: 壁の刻み目 | `trace_marks == 0` | ソラ推理会話 → set trace_marks = 1 |
| 地点に立つ: 反響地点A | `echo_experiment < 2` | 反響段階2 → set echo_experiment = 2 |
| 地点に立つ: 反響地点B | `echo_experiment == 2` | 反響段階3 → set echo_experiment = 3 |
| 地点に立つ: 反響地点C | `echo_experiment == 3` | 反響段階4 → set echo_experiment = 4, trace_echo = 1 |
| 調べる: 花畑中央 | `trace_flowers == 0` | ソラ推理会話 → set trace_flowers = 1 |
| 会話: ユウに壁の真相を聞く | `trace_marks == 1 AND truth_marks == 0` | 真相会話 → set truth_marks = 1 |
| zone遷移: 帰還後にネフィア再入場 | `quest.ending == 1` | 再訪エンド開始 → set quest.ending = 3 |

## Content Volume

| Chapter | Trace Points | Dialogue Triggers | Walk Sections |
|---------|-------------|-------------------|---------------|
| 0 | 0 | 3 (ミナ依頼, ソラ合流, 入口) | 1 |
| 1 | 3 + 手帳 | 4 + 議論 | 2 |
| 2 | 3 (反響3地点) + 地図 + 人影 | 6 (段階1-4, 地図, 人影) | 1 |
| 3 | 2 (花畑, 編み物) | 4 (開花, 満開, 根元, 編み物) | 3 (薄闇, 満開無言, 自由探索) |
| 4 | 0 | 8 (真相×8) + 再会 + 帰還提案 | 0 |
| 5 | 0 | 4 (帰還道中4段階) + エンド | 1 |

## Endings

| Name | Condition | Tone |
|------|-----------|------|
| 帰還エンド | truth 全取得 (8/8) | 全部くだらなかった。たぶん。 |
| 沈黙エンド | truth 0/8 | 答えは分からないまま。でも見たものは見た。 |
| 再訪エンド（隠し） | 帰還エンド後に再入場 | テキストなし。何も起きない。たぶん。 |
