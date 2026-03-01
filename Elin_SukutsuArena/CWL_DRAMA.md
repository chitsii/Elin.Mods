# CWLドラマ仕様書

CWL（Custom Whatever Loader）のドラマシステムに関する技術的な仕様をまとめたドキュメント。

## バニラNPC会話オプションの挿入（inject/Unique）

バニラゲームの標準NPC会話オプション（招待、パーティ参加、決闘など）をModのカスタムドラマに挿入する方法。

### 基本構文

```
action=inject | param=Unique
```

Pythonビルダー:
```python
builder.inject_unique()
```

### 動作条件

`inject/Unique`が選択肢を追加するには、以下の条件を**すべて**満たす必要がある：

1. **idDefaultPassed = true**: ドラマの`main`ステップを通過している
2. **tg.hasChara = true**: ターゲットがキャラクターである

### 挿入される選択肢の条件

#### `_invite`（招待）の表示条件

```csharp
if (!c.IsPCFaction
    && c.affinity.CanInvite()      // 好感度がRespected以上
    && !EClass._zone.IsInstance    // インスタンスゾーンではない
    && c.c_bossType == BossType.none)
{
    // さらに分岐
    if ((c.trait.IsUnique || c.IsGlobal) && c.GetInt(111) == 0)
    {
        // _bout（決闘）が表示される
    }
    else
    {
        // _invite（招待）が表示される
    }
}
```

#### `_joinParty`（パーティ参加）の表示条件

```csharp
if (c.IsHomeMember())  // PC派閥のホームメンバーである
{
    if (!c.IsPCParty
        && c.memberType != FactionMemberType.Livestock
        && c.trait.CanJoinParty)
    {
        // _joinParty が表示される
    }
}
```

### 重要：配置位置

`inject/Unique`は**sayの直後、同じステップ内**に配置する必要がある。

#### 正しい配置（動作する）

```python
builder.step(greeting).say(
    "greet", "こんにちは", "Hello", actor=npc
).choice(
    custom_step, "独自選択肢", "Custom choice"
).inject_unique().choice(  # sayと同じステップ内
    another_step, "別の選択肢", "Another choice"
).on_cancel(end)
```

生成されるExcel:
```
step     | action  | param
---------|---------|-------
greeting |         |
         |         |           <- say (text)
         | choice  |           <- 独自選択肢
         | inject  | Unique    <- バニラ選択肢挿入
         | choice  |           <- 別の選択肢
         | cancel  |
```

#### 誤った配置（動作しない）

```python
# NG: inject後にjumpすると、挿入された選択肢が失われる
builder.step(greeting).say(...).inject_unique().jump(choices)
builder.step(choices).choice(...)  # inject_uniqueの選択肢は表示されない
```

### 好感度の操作

`inject/Unique`の`_invite`を表示するには、好感度がRespected以上必要。ドラマ内で好感度を操作するには：

```python
builder.mod_affinity(100)  # 好感度を100上げる
```

生成されるExcel:
```
action=modAffinity | param=100
```

### Traitの設定

カスタムTraitで`CanJoinParty`、`CanInvite`を制御できる：

```csharp
public class TraitSukutsuNPC : TraitUniqueChara
{
    // バニラの好感度判定 OR クエスト完了で招待可能
    public override bool CanJoinParty =>
        base.CanJoinParty ||
        (ArenaQuestManager.Instance?.IsQuestCompleted("quest_id") ?? false);

    public override bool CanInvite =>
        base.CanInvite ||
        (ArenaQuestManager.Instance?.IsQuestCompleted("quest_id") ?? false);
}
```

### 参考：UltraChanModの構造

動作確認済みの構造例（UltraChanMod）:
```
Row 61: main1 step
Row 62: say (text)
Row 63: choice (独自選択肢)
Row 64: inject | Unique
Row 65: choice (条件付き独自選択肢)
Row 66: choice/bye
Row 67: cancel
```

### トラブルシューティング

| 症状 | 原因 | 解決策 |
|------|------|--------|
| 選択肢が表示されない | inject後にjumpしている | 同じステップ内で選択肢を定義 |
| `_invite`が表示されない | 好感度不足 | `mod_affinity`で好感度を上げる |
| `_invite`が表示されない | ゾーンがインスタンス | カスタムゾーンの設定を確認 |
| `_bout`が表示される | IsUnique/IsGlobalがtrue | Traitの設定を確認 |
| `_joinParty`が表示されない | ホームメンバーでない | PC派閥に参加後のみ表示される |
