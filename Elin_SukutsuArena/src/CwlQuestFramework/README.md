# CwlQuestFramework

Elin Mod向けの汎用クエスト管理フレームワーク。フェーズベースのストーリー進行、フラグ条件、クエスト依存関係を統一的に管理する。

## 設計思想

### 1. 依存性逆転の原則 (DIP)

フレームワークは具体的なゲーム実装に依存せず、抽象（インターフェース）に依存する。

```
┌─────────────────────────────────────────────────────────────┐
│  Mod Layer (Elin_SukutsuArena)                              │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ ArenaQuestManager│  │ FlagStorageAdapter│                 │
│  │ (uses framework)│  │ (implements)      │                 │
│  └────────┬────────┘  └────────┬─────────┘                  │
└───────────│─────────────────────│────────────────────────────┘
            │ uses                │ implements
            ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│  CwlQuestFramework (Abstractions)                           │
│  ┌────────────────┐  ┌────────────────┐  ┌───────────────┐  │
│  │ QuestManager<T>│  │ IFlagSetter    │  │ IPhaseManager │  │
│  │ (orchestrator) │  │ (interface)    │  │ (interface)   │  │
│  └────────────────┘  └────────────────┘  └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 2. フェーズベースのストーリー進行

クエストをストーリーフェーズで分類し、プレイヤーの進行に応じて利用可能なクエストを制御する。

```
Phase 0 (Prologue)     Phase 1 (Initiation)     Phase 2 (Rising)
┌──────────────────┐   ┌──────────────────┐     ┌──────────────────┐
│ opening_quest    │──▶│ rank_up_G        │────▶│ rank_up_F        │
│                  │   │ side_quest_1     │     │ side_quest_2     │
└──────────────────┘   └──────────────────┘     └──────────────────┘
```

### 3. 宣言的なクエスト定義

クエストの依存関係・条件を宣言的に定義し、評価ロジックをフレームワークに委譲する。

```json
{
  "questId": "rank_up_F",
  "phase": 1,
  "requiredQuests": ["rank_up_G"],
  "requiredFlags": [
    { "flagKey": "player.level", "operator": ">=", "value": 10 }
  ],
  "completionFlags": { "player.rank": "F" },
  "advancesPhaseOrdinal": 2
}
```

### 4. オブザーバーパターン

クエスト完了・フェーズ進行などのイベントを監視し、Mod固有の処理（UI更新、実績解除等）を実行可能。

---

## アーキテクチャ概要

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           CwlQuestFramework                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                      QuestManager<TPhase>                         │   │
│  │  ┌─────────────────────────────────────────────────────────────┐ │   │
│  │  │ - GetAvailableQuests()    - CompleteQuest()                 │ │   │
│  │  │ - IsQuestAvailable()      - IsQuestCompleted()              │ │   │
│  │  │ - GetAutoTriggerQuests()  - GetQuestsForNpc()               │ │   │
│  │  └─────────────────────────────────────────────────────────────┘ │   │
│  │                              │                                    │   │
│  │          ┌───────────────────┼───────────────────┐               │   │
│  │          ▼                   ▼                   ▼               │   │
│  │  ┌──────────────┐  ┌─────────────────┐  ┌──────────────────┐    │   │
│  │  │ IFlagSetter  │  │ IPhaseManager   │  │ IQuestDefinition │    │   │
│  │  │              │  │ <TPhase>        │  │ (list)           │    │   │
│  │  └──────────────┘  └─────────────────┘  └──────────────────┘    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                              │                                           │
│          ┌───────────────────┴───────────────────┐                      │
│          ▼                                       ▼                      │
│  ┌─────────────────────┐              ┌─────────────────────────┐       │
│  │ QuestConditionChecker│              │ QuestAvailabilityEvaluator│     │
│  │ - CheckCondition()  │              │ - IsAvailable()          │      │
│  │ - CheckAllConditions│              │ - Evaluate phase, flags, │      │
│  └─────────────────────┘              │   prerequisites, blocks  │      │
│                                        └─────────────────────────┘       │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    IQuestStateObserver                            │   │
│  │  - OnQuestCompleted()  - OnPhaseAdvanced()  - OnFlagSet()        │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 公開API

### Core Interfaces

#### `IQuestManager`
クエスト管理の主要インターフェース。

```csharp
public interface IQuestManager
{
    bool IsQuestCompleted(string questId);
    bool IsQuestAvailable(string questId);
    void CompleteQuest(string questId);
    IReadOnlyList<IQuestDefinition> GetAvailableQuests();
    IReadOnlyList<IQuestDefinition> GetQuestsForNpc(string npcId);
    IReadOnlyList<IQuestDefinition> GetAutoTriggerQuests();
    IReadOnlyList<string> GetNpcsWithQuests();
    event Action OnQuestStateChanged;
}
```

#### `IQuestDefinition`
クエスト定義のインターフェース。

```csharp
public interface IQuestDefinition
{
    string QuestId { get; }
    string QuestType { get; }
    string DramaId { get; }
    string DisplayNameJP { get; }
    string DisplayNameEN { get; }
    string Description { get; }
    int Phase { get; }
    string QuestGiver { get; }
    bool AutoTrigger { get; }
    int? AdvancesPhase { get; }
    IReadOnlyList<IFlagCondition> RequiredFlags { get; }
    IReadOnlyList<string> RequiredQuests { get; }
    IReadOnlyDictionary<string, object> CompletionFlags { get; }
    IReadOnlyList<string> BlocksQuests { get; }
    int Priority { get; }
}
```

#### `IFlagCondition`
フラグ条件のインターフェース。

```csharp
public interface IFlagCondition
{
    string FlagKey { get; }      // e.g., "player.rank"
    string Operator { get; }     // "==", "!=", ">=", ">", "<=", "<"
    object Value { get; }        // int, bool, or string (enum)
}
```

#### `IPhaseManager<TPhase>`
フェーズ管理のインターフェース。

```csharp
public interface IPhaseManager<TPhase> where TPhase : struct, Enum
{
    TPhase CurrentPhase { get; }
    void SetPhase(TPhase phase);
    int GetPhaseOrdinal();
    event Action<TPhase, TPhase> OnPhaseChanged;
}
```

#### `IFlagSetter` / `IFlagValueProvider`
フラグ読み書きのインターフェース。

```csharp
public interface IFlagValueProvider
{
    int GetInt(string key, int defaultValue = 0);
    bool HasKey(string key);
}

public interface IFlagSetter : IFlagValueProvider
{
    void SetInt(string key, int value);
    void SetFromJsonValue(string key, object value);
}
```

#### `IEnumMappingProvider`
文字列Enum値から整数への変換を提供。

```csharp
public interface IEnumMappingProvider
{
    bool TryGetMapping(string flagKey, out IDictionary<string, int> mapping);
}
```

#### `IQuestStateObserver`
クエスト状態変更の監視。

```csharp
public interface IQuestStateObserver
{
    void OnQuestCompleted(string questId, IQuestDefinition quest);
    void OnPhaseAdvanced(int oldPhase, int newPhase);
    void OnFlagSet(string key, object value);
}
```

---

### Main Classes

#### `QuestManager<TPhase>`
メインのオーケストレーター。

```csharp
public class QuestManager<TPhase> : IQuestManager
    where TPhase : struct, Enum
{
    public QuestManager(
        IFlagSetter flagSetter,
        IPhaseManager<TPhase> phaseManager,
        IEnumerable<IQuestDefinition> quests,
        string questCompletedPrefix);

    public IPhaseManager<TPhase> PhaseManager { get; }

    public void AddObserver(IQuestStateObserver observer);
    public void RemoveObserver(IQuestStateObserver observer);

    // IQuestManager implementation...
}
```

#### `QuestConditionChecker`
フラグ条件の評価ロジック。

```csharp
public class QuestConditionChecker
{
    public QuestConditionChecker(
        IFlagValueProvider flagProvider,
        IEnumMappingProvider enumMappingProvider = null);

    public bool CheckCondition(IFlagCondition condition);
    public bool CheckAllConditions(IEnumerable<IFlagCondition> conditions);
    public bool CheckAnyCondition(IEnumerable<IFlagCondition> conditions);
}
```

#### `QuestAvailabilityEvaluator`
クエスト利用可能性の評価。

```csharp
public class QuestAvailabilityEvaluator
{
    public QuestAvailabilityEvaluator(
        QuestConditionChecker conditionChecker,
        Func<string, bool> isQuestCompleted);

    public bool IsAvailable(
        IQuestDefinition quest,
        int currentPhase,
        IEnumerable<IQuestDefinition> allQuests,
        out string reason);
}
```

---

### Standard Implementations

#### `GenericQuestDefinition`
`IQuestDefinition`の標準実装。

```csharp
var quest = new GenericQuestDefinition("my_quest")
{
    Phase = 1,
    QuestType = "main",
    DisplayNameJP = "クエスト名",
    AutoTrigger = false,
    Priority = 500
};
quest.AddRequiredQuest("prerequisite_quest");
quest.AddCompletionFlag("player.rank", "B");
```

#### `GenericFlagCondition`
`IFlagCondition`の標準実装。

```csharp
var condition = new GenericFlagCondition("player.level", ">=", 10);
```

#### `StandardPhaseManager<TPhase>`
`IPhaseManager<TPhase>`の標準実装。

```csharp
var phaseManager = new StandardPhaseManager<StoryPhase>(storage, "phase_key");
phaseManager.SetPhase(StoryPhase.Rising);
```

#### `FlagStorageAdapter`
`IFlagSetter`の標準実装（IFlagStorageをアダプト）。

```csharp
var adapter = new FlagStorageAdapter(storage, enumMappingProvider);
adapter.SetFromJsonValue("player.rank", "A");  // Enum mapping applied
```

---

## クエスト評価フロー

```
GetAvailableQuests()
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│ QuestAvailabilityEvaluator.IsAvailable()                  │
│                                                           │
│  1. IsQuestCompleted?  ──────────────────▶ Skip if true   │
│           │                                               │
│           ▼                                               │
│  2. Phase Check        ──────────────────▶ Skip if future │
│     (quest.Phase <= currentPhase)                         │
│           │                                               │
│           ▼                                               │
│  3. RequiredQuests     ──────────────────▶ Skip if unmet  │
│     (all must be completed)                               │
│           │                                               │
│           ▼                                               │
│  4. RequiredFlags      ──────────────────▶ Skip if unmet  │
│     (QuestConditionChecker)                               │
│           │                                               │
│           ▼                                               │
│  5. BlocksQuests       ──────────────────▶ Skip if blocked│
│     (check if blocking quest is completed)                │
│           │                                               │
│           ▼                                               │
│      AVAILABLE                                            │
└───────────────────────────────────────────────────────────┘
```

---

## 使用例

### 基本的な使用法

```csharp
// 1. Define your phase enum
public enum StoryPhase
{
    Prologue = 0,
    Initiation = 1,
    Rising = 2,
    Climax = 3
}

// 2. Create adapters
var enumMapping = new MyEnumMappingAdapter();
var flagSetter = new FlagStorageAdapter(storage, enumMapping);
var phaseManager = new StandardPhaseManager<StoryPhase>(storage, "story.phase");

// 3. Load quest definitions
var quests = LoadQuestsFromJson();

// 4. Create QuestManager
var questManager = new QuestManager<StoryPhase>(
    flagSetter,
    phaseManager,
    quests,
    "quest.done."
);

// 5. Add observer for custom handling
questManager.AddObserver(new MyQuestObserver());

// 6. Use the manager
var available = questManager.GetAvailableQuests();
questManager.CompleteQuest("opening_quest");
```

### オブザーバーの実装

```csharp
public class MyQuestObserver : IQuestStateObserver
{
    public void OnQuestCompleted(string questId, IQuestDefinition quest)
    {
        Debug.Log($"Quest completed: {questId}");
        UpdateQuestMarkers();
    }

    public void OnPhaseAdvanced(int oldPhase, int newPhase)
    {
        Debug.Log($"Story progressed: {oldPhase} -> {newPhase}");
        UnlockNewAreas();
    }

    public void OnFlagSet(string key, object value)
    {
        Debug.Log($"Flag set: {key} = {value}");
    }
}
```

---

## ファイル構成

```
CwlQuestFramework/
├── README.md                    # このファイル
├── IQuestManager.cs             # IQuestManager, IQuestDefinition, IFlagCondition
├── QuestManager.cs              # QuestManager<TPhase>
├── QuestDefinition.cs           # GenericQuestDefinition
├── FlagCondition.cs             # GenericFlagCondition, FlagOperators
├── QuestConditionChecker.cs     # QuestConditionChecker, QuestAvailabilityEvaluator,
│                                # IFlagValueProvider, IEnumMappingProvider
├── IPhaseManager.cs             # IPhaseManager<TPhase>
├── StandardPhaseManager.cs      # StandardPhaseManager<TPhase>
├── IFlagSetter.cs               # IFlagSetter
├── FlagStorageAdapter.cs        # FlagStorageAdapter
└── IQuestStateObserver.cs       # IQuestStateObserver
```

---

## 設計上の決定事項

### フラグ値は整数で統一

すべてのフラグ値は内部的に`int`として管理される。文字列Enum（例: `"rank": "A"`）は`IEnumMappingProvider`を通じて整数に変換される。

**理由:**
- CWLのdialogFlagsは整数ベース
- 比較演算が単純化
- 型の一貫性を保証

### フェーズはジェネリックEnum

`QuestManager<TPhase>`はフェーズ型をジェネリックにすることで、Modごとに異なるフェーズ定義を可能にする。

```csharp
// Arena Mod
public enum StoryPhase { Prologue, Initiation, Rising, Awakening, Confrontation, Climax }

// Another Mod
public enum ChapterPhase { Chapter1, Chapter2, Chapter3, Epilogue }
```

### クエスト定義はイミュータブル

`IQuestDefinition`は読み取り専用プロパティのみを公開。実行時の変更は想定せず、すべての定義はJSONから読み込まれる。

---

## 今後の拡張ポイント

1. **永続化の抽象化**: 現在は`IFlagStorage`（Elin固有）に依存。完全な汎用化には独自のストレージインターフェースが必要。

2. **クエストグループ**: 関連クエストのグルーピング（例: サイドクエストチェーン）

3. **条件の拡張**: OR条件、NOT条件、時間ベース条件など

4. **進行状態の可視化**: クエストツリーのグラフ出力
