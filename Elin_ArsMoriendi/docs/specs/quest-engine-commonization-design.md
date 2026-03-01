# Quest Engine Commonization Design

最終確認: 2026-02-17

## 1. 目的

- Mod固有シナリオ（文言・演出・敵構成）を維持したまま、クエスト依存性解決の共通基盤を抽出する。
- 抽出対象は「進行判定」「状態遷移」「副作用実行」「永続フラグ同期」。
- Harmonyパッチは各Modに残し、パッチ内部から共通エンジンを呼び出す。

## 2. スコープ

### 共通化する

- ステージ状態機械（state machine）
- 依存条件評価（bool condition）
- 遷移トリガー処理（OnTomeOpen/OnSoulDrop/OnZoneActivated/OnEnemyDefeated など）
- フラグ永続化抽象（dialogFlags）
- ジャーナルフェーズ同期の共通手順
- fail-soft 実行ガード（例外時ログ + 継続）
- ドラマ開始可否の判定と開始コマンド実行（重複起動防止含む）

### 共通化しない

- Harmony patchクラス本体
- 具体NPC ID、具体item ID、具体drama ID
- 物語テキスト、UI文言、演出の具体内容

## 2.1 設計制約（重要）

- 依存関係・遷移ルールの**実行時登録は行わない**。
- すべてのキー/ルールはビルド時定義（コードまたは生成物）とする。
- 不整合は `build.bat` の検証で失敗させる（ランタイムで吸収しない）。

## 3. レイヤ設計

1. Core (`Elin.CommonQuest`)
- 純粋な進行エンジン。Mod固有識別子を持たない。

2. Adapter (`<Mod>.QuestAdapter`)
- Core が要求する依存を実ゲームAPIへ橋渡し。

3. Content (`<Mod>.QuestContent`)
- ステージ定義、依存キー、アクション配線、drama ID。

## 4. コア型定義（C#）

```csharp
public readonly record struct QuestStateId(string Value);
public readonly record struct QuestStage(int Value);
public readonly record struct TriggerId(string Value);
public readonly record struct DependencyKey(string Value);
public readonly record struct ActionKey(string Value);

public sealed record TransitionRule(
    QuestStage From,
    QuestStage To,
    TriggerId Trigger,
    IReadOnlyList<DependencyKey> Conditions,
    IReadOnlyList<ActionKey> Actions,
    bool EnforceCooldown = false
);

public sealed record QuestDefinition(
    QuestStateId QuestId,
    QuestStage StartStage,
    QuestStage CompleteStage,
    IReadOnlyList<TransitionRule> Rules
);
```

補足:
- `QuestDefinition` はコンパイル時に固定される。
- 外部から `Register*` で動的追加する API は提供しない。

## 5. 依存抽象インターフェース

```csharp
public interface IQuestFlagStore
{
    int GetInt(string key);
    void SetInt(string key, int value);
}

public interface IQuestClock
{
    int GetRawMinutes();
}

public interface IQuestJournalSync
{
    void SyncPhase(string questId, int phase);
}

public interface IQuestDependencyResolver
{
    bool TryResolveBool(string key, out bool value);
}

public interface IQuestActionExecutor
{
    bool TryExecute(string key);
}

public interface IQuestLog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}
```

補足:
- `IQuestDependencyResolver` と `IQuestActionExecutor` は、現在の `IDramaDependencyResolver` と同じキー方式を採用可能。
- これにより drama 側と quest 側で dependency key を共有できる。

## 5.1 状態参照 API（ドラマ公開用）

ドラマ側が条件分岐しやすいよう、Quest Runtime facade で次を公開する。

```csharp
public interface IQuestStateReader
{
    int GetInt(string key);              // 例: chitsii.ars.quest.stage
    bool Check(string conditionKey);     // 例: state.quest.can_start.ars_karen_shadow
}
```

方針:
- ドラマ側は `builder.cs_call_common_runtime(...)` 経由で `GetInt/Check` を使う。
- `eval` 直書きは禁止（既存移行対象を除く）。

## 6. エンジン責務

`QuestEngine` は次を保証する。

- 現在ステージ取得/設定
- trigger 単位で遷移候補を検索
- conditions 全成立時のみ遷移
- cooldown ルール適用（`last_advance_raw`）
- actions 実行（失敗は warn で継続）
- phase 同期
- 遷移イベント通知（`OnStageChanged`）

```csharp
public sealed class QuestEngine
{
    public QuestStage CurrentStage { get; }
    public bool IsComplete { get; }

    public bool TryHandle(TriggerId trigger);
    public void AdvanceTo(QuestStage stage);

    public event Action<QuestStage>? OnStageChanged;
}
```

### 6.1 実装済み最小コア（2026-02-17）

段階移行のため、先行して次を実装済み:

- `QuestTransitionRule<TState>`
  - `From`, `To`, `CanAdvance`, `OnBlocked`, `BlocksGuiOnAdvance`
- `QuestStateMachine<TState>`
  - `TryAdvanceFromCurrent(out bool blocksGui)`
- `QuestTriggeredTransitionRule<TState, TTrigger>`
  - `From`, `To`, `Trigger`, `CanAdvance`, `OnBlocked`, `BlocksGuiOnAdvance`
- `QuestTriggeredStateMachine<TState, TTrigger>`
  - `TryHandle(trigger, out bool blocksGui)`
  - `(From, Trigger)` の重複定義をコンストラクタで拒否（例外）

目的:
- Mod側が `switch(stage)` と個別フラグPrefix処理を直接書かず、
  「遷移ルール定義」に寄せる。
- 既存 `UnhallowedPath.TryAdvanceOnTomeOpen()` の条件分岐を
  共通ルールに置換して同値動作を維持する。
- `Stage7 -> Stage8` は `trigger.enemy_defeated.erenos`、
  `Stage8 -> Stage9` は `trigger.apotheosis_apply` で
  共通ルール遷移へ移行済み。
- `NotStarted -> Stage1` は `trigger.first_soul_drop`、
  `Stage2 -> Stage3` は `trigger.karen_encounter.hostile` で
  共通ルール遷移へ移行済み。

## 7. 永続キー規約

共通エンジンが予約する管理キー:
- `<ns>.quest.stage`
- `<ns>.quest.last_advance_raw`

Mod側が使うイベントキー:
- `<ns>.quest.event.*`
- `<ns>.quest.state.*`

`<ns>` は `chitsii.ars` のように Mod 固有 prefix を必須化。

## 8. トリガー設計

トリガーは enum 固定でなく、文字列キーで運用。

推奨キー:
- `trigger.first_soul_drop`
- `trigger.tome_open`
- `trigger.zone_activated`
- `trigger.enemy_defeated.karen`
- `trigger.enemy_defeated.erenos`
- `trigger.apotheosis_apply`

理由:
- Mod間で追加トリガーを破壊的変更なしで拡張できる。

## 8.1 条件プリミティブ（共通化対象）

以下はライブラリ側で evaluator を持つ。

- `flag.*`
  - 例: `flag.eq(chitsii.ars.quest.stage,4)`
- `drama.*`
  - 例: `drama.seen(ars_karen_encounter)`
- `npc.*`
  - 例: `npc.talked(ars_karen)`
- `zone.*`
  - 例: `zone.entered(palmia)`
- `time.*`
  - 例: `time.elapsed_raw(chitsii.ars.quest.last_advance_raw,1440)`
- `item.*`
  - 例: `item.has(ars_karen_journal,1)`
- `logic.*`
  - 例: `logic.all(condA,condB)`, `logic.any(...)`, `logic.not(...)`

注記:
- 条件の評価器を一般化する。条件そのもの（NPC/zone/item ID）は Mod 側データ。

## 9. UnhallowedPath への移行マッピング

### 現状
- `UnhallowedPath` が state + conditions + actions を全て内包。
- `KnightEncounter` / `ErenosBattle` が直接フラグ更新と遷移を呼ぶ。

### 移行後

1. `UnhallowedPathDefinition`（新規）
- ステージ遷移ルールのみ記述。

2. `ArsQuestDependencyResolver`（新規）
- `servant.count >= N` 等の条件を解決。

3. `ArsQuestActionExecutor`（新規）
- `action.play_drama.ars_karen_shadow` などを実行。

4. `UnhallowedQuestFacade`（新規）
- 旧 `UnhallowedPath` API 互換窓口。
- 内部で `QuestEngine` 呼び出し。

5. `KnightEncounter` / `ErenosBattle` 更新
- `quest.MarkXxx()` 直接呼び出しを action key 実行へ置換。

## 10. データ定義方式

### Phase 1
- C# の `QuestDefinition` をコードで記述（型安全優先）。

### Phase 2
- Python 生成で `QuestDefinition.g.cs` を出力可能にする。
- `tools/data/quests.py` と同じパイプラインで扱う。

## 10.1 ドラマ開始判定/開始実行キー

開始条件と開始実行はキーを分離する。

- 判定キー: `state.quest.can_start.<drama_id>`
- 完了判定キー: `state.quest.is_complete.<drama_id>`
- 実行キー: `cmd.quest.try_start.<drama_id>`
- 反復実行キー: `cmd.quest.try_start_repeatable.<drama_id>`
- 完了まで反復実行キー: `cmd.quest.try_start_until_complete.<drama_id>`

`cmd.quest.try_start.*` の契約:
1. 開始条件を再評価
2. 未開始時のみ `QuestDrama.TryPlay(...)` を実行
3. **開始成功時のみ** started/seen フラグ更新（失敗時は未更新）
4. 二重起動を抑止
5. 失敗時は次トリガーで再試行可能（進行不能回避）

`cmd.quest.try_start_repeatable.*` の契約:
1. 開始条件評価は呼び出し側（トリガー側）で担保する
2. started フラグを見ずに開始を試行する
3. 開始失敗時は次トリガーで再試行可能
4. ambush のような「完了まで再発」導線で使う

`cmd.quest.try_start_until_complete.*` の契約:
1. `state.quest.is_complete.<drama_id>` を評価
2. 未完了なら開始を試行する
3. 完了済みなら開始しない
4. 開始失敗時は次トリガーで再試行可能

## 11. fail-soft 方針

- 依存未解決: warn して遷移中断（クラッシュしない）
- action 未解決: warn して次 action 継続
- phase sync 失敗: warn のみ（ゲーム継続）

原則:
- エンジンは例外を外へ投げず、`Try*` API で結果を返す。

## 12. テスト設計

### Core 単体テスト
- rule selection
- cooldown gating
- idempotent advance（同ステージ再遷移なし）
- unresolved dependency handling
- unresolved action handling

### Adapter 契約テスト
- key prefix 検証 (`state.*`, `cmd.*`, `cue.*`, `trigger.*`)
- UnhallowedPath 相当シナリオの遷移一致
- `state.quest.can_start.*` と `cmd.quest.try_start.*` の対応存在チェック

### ビルド時静的検証
- 参照キーの存在チェック（definition/resolver/builder の突合）
- 未実装キーがあれば `build.bat` で失敗

### 回帰テスト
- 既存 `ArsDramaResolverTests` に準じたキー実装網羅検証を追加。

## 13. 実装ステップ

1. `src/CommonQuest/` 追加
- `QuestEngine.cs`, `QuestDefinition.cs`, interface群

2. `UnhallowedPath` を Facade 化
- 外部API互換を維持しつつ内部を `QuestEngine` へ差替え

3. `KnightEncounter` / `ErenosBattle` の直接更新を削減
- action key 経由に置換

4. Python 側の builder/data 連携
- 依存キーの単一情報源を `schema/key_spec.py` に統合

5. 既存挙動一致確認
- build.bat
- ユニットテスト
- ゲーム内確認（既存チェックリストに沿う）

## 13.1 移行実行ルール（TDD）

- 詳細手順は `docs/plans/2026-02-17-quest-engine-tdd-migration-plan.md` を正とする。
- 置換はトリガー単位で進める（全量一括置換は禁止）。
- 各作業単位は `RED -> GREEN -> REFACTOR` を必須化する。
- ロジック同値はログ比較で確認し、手動確認は演出系に限定する。

## 14. 見積り（このMod単体）

- 抽出対象（現実的）
  - `tools/builder` + `tools/data`: 2,737行
  - 既存 drama/runtime 共通化対象: 2,264行
  - `src/Quest` の共通化可能部分: 約670〜1,170行

- 合計削減想定
  - 約5,671〜6,171行（全体の約39〜42%）

注記:
- 50%削減は、UIやより多くのシナリオ固有コードまで共通化しない限り到達しにくい。
- 本設計の主価値は、行数より「依存性解決基盤の再利用」と「変更耐性」。

## 15. 反対条件（Go/No-Go）

No-Go:
- 2本目Modの予定がない
- 既存Modだけで機能凍結予定

Go:
- 2本目以降でも「ステージ遷移 + 依存条件 + drama実行」を再利用する見込みがある
- キー契約テストを運用できる
