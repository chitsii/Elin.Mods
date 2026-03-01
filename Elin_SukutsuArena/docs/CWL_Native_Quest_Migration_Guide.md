# CWLネイティブクエスト移行ガイド

Modのカスタムクエスト管理をCWLネイティブ機能に移行するためのガイド。
GoldenSoupでの実証済みパターンをSukutsuArenaに適用する。

## 概要

### 移行前（JSON/C#ベース）
```
quest_definitions.json → QuestManager.cs → modInvoke complete_quest()
```
- カスタムQuestManager実装が必要
- JSON定義ファイルの管理
- C#で依存関係・条件チェック

### 移行後（CWLネイティブ）
```
SourceQuest.xlsx → Elin標準Quest → CWLドラマ startQuest/completeQuest
```
- CWLがクエストをElinネイティブシステムに登録
- ドラマアクションで直接操作
- C#コード大幅削減

## Step 1: SourceQuest.xlsx の作成

### 1.1 TSV/Excel構造

```
| id          | name_JP      | name         | type  | drama                        | idZone     | detail_JP              |
|-------------|--------------|--------------|-------|------------------------------|------------|------------------------|
| my_quest    | クエスト名   | Quest Name   | Quest | drama_npc_name,main          | zone_id    | クエストの説明文       |
| my_quest1   | クエスト名   | Quest Name   | Quest |                              |            | フェーズ1の説明        |
| my_quest2   | クエスト名   | Quest Name   | Quest |                              |            | フェーズ2の説明        |
```

### 1.2 カラム説明

| カラム | 必須 | 説明 |
|--------|------|------|
| `id` | ◯ | クエストID。フェーズは `{base_id}{phase_num}` |
| `name_JP` | ◯ | 日本語名 |
| `name` | ◯ | 英語名 |
| `type` | ◯ | 常に `Quest` |
| `drama` | △ | 関連ドラマ（`drama_name,step`形式）。メインのみ指定 |
| `idZone` | - | 発生ゾーン |
| `detail_JP` | ◯ | ジャーナル表示テキスト |
| `detail` | ◯ | 英語ジャーナルテキスト |
| `talkProgress_JP` | - | 進行中の会話テキスト |
| `talkComplete_JP` | - | 完了時の会話テキスト |

### 1.3 Pythonでの生成（推奨）

```python
# models/quest.py
from pydantic import BaseModel, Field
from typing import ClassVar, Any

class QuestRow(BaseModel):
    id: str
    name_JP: str = ""
    name: str = ""
    type: str = "Quest"
    drama: list[str] = Field(default_factory=list)
    idZone: str = ""
    detail_JP: str = ""
    detail: str = ""
    # ... 他のカラム

QUEST_DATA = [
    QuestRow(
        id="arena_rank_g",
        name_JP="闘技場ランクG",
        name="Arena Rank G",
        drama=["drama_arena_master", "main"],
        detail_JP="Gランクの試練に挑戦する。",
        detail="Challenge the G rank trial.",
    ),
    # フェーズ定義（ジャーナル表示用）
    QuestRow(
        id="arena_rank_g1",
        name_JP="闘技場ランクG",
        name="Arena Rank G",
        detail_JP="マスターに話しかけて試練を開始する。",
        detail="Talk to the master to start the trial.",
    ),
]
```

## Step 2: ドラマでの使用

### 2.1 クエスト開始

```python
# ドラマビルダー
builder.action("startQuest", param="arena_rank_g")
```

**生成されるExcel:**
```
| action     | param        |
|------------|--------------|
| startQuest | arena_rank_g |
```

### 2.2 クエスト完了

```python
builder.action("completeQuest", param="arena_rank_g")
```

### 2.3 フェーズ進行

```python
# 次のフェーズへ
builder.action("nextPhase", param="arena_rank_g")

# 特定フェーズに変更
builder.action("changePhase", param="arena_rank_g,2")
```

### 2.4 DramaBuilderへのメソッド追加（オプション）

```python
# cwl_quest_lib/builders/drama_builder.py に追加

def start_quest_native(self, quest_id: str) -> "DramaBuilder":
    """CWLネイティブのstartQuestアクション"""
    self.entries.append({"action": "startQuest", "param": quest_id})
    return self

def complete_quest_native(self, quest_id: str) -> "DramaBuilder":
    """CWLネイティブのcompleteQuestアクション"""
    self.entries.append({"action": "completeQuest", "param": quest_id})
    return self
```

## Step 3: C#コードの削減

### 3.1 削除可能なファイル

| ファイル | 理由 |
|----------|------|
| `QuestManager.cs` | CWLネイティブで代替 |
| `QuestDefinition.cs` | SourceQuest.xlsxで代替 |
| `CompleteQuestCommand.cs` | CWL completeQuestで代替 |
| `StartQuestCommand.cs` | CWL startQuestで代替 |
| `quest_definitions.json` | SourceQuest.xlsxで代替 |

### 3.2 残す可能性があるもの

| ファイル | 理由 |
|----------|------|
| `QuestMarkerManager.cs` | NPCマーカー表示（カスタム機能） |
| `CheckQuestAvailableCommand.cs` | 複雑な条件チェック（必要なら） |

### 3.3 SukutsuArena固有の考慮事項

SukutsuArenaは以下の追加機能を持つため、完全移行には注意が必要:

1. **ランク進行システム** - フラグベースで管理可能
2. **バトル結果連動** - 戦闘結果でクエスト完了
3. **NPCマーカー** - カスタム実装維持

## Step 4: build.bat更新

```batch
REM Quest.tsv -> SourceQuest.xlsx 変換を追加
if exist "%~dp0LangMod\JP\Quest.tsv" (
    call :convert_tsv "%~dp0LangMod\JP\Quest.tsv" "%~dp0LangMod\JP" "SourceQuest.xlsx"
    if !ERRORLEVEL! NEQ 0 set SOFFICE_ERROR=1
)
```

## Step 5: テスト

### 5.1 テスト用ドラマ選択肢

```python
# テスト用選択肢を追加
builder.choice(start_quest_test, "Start Quest Test", text_id="c_test_start")
builder.choice(complete_quest_test, "Complete Quest Test", text_id="c_test_complete")

# Start Quest Test ステップ
builder.step(start_quest_test).say(
    "test_start", "Starting quest...", actor=npc
).action(
    "startQuest", param="arena_rank_g"
).say(
    "test_done", "Check your journal!", actor=npc
).jump(end)
```

### 5.2 確認項目

- [ ] SourceQuest.xlsxがCWLに読み込まれる
- [ ] startQuestでジャーナルにクエスト追加
- [ ] completeQuestでクエスト完了
- [ ] nextPhaseでフェーズ進行
- [ ] detail_JPがジャーナルに正しく表示

### 5.3 デバッグ

```
# コンソールでクエスト確認
cwl.cs.eval EMono.game.quests.list.Select(q => q.id)

# dialogFlagsでフェーズ確認
cwl.cs.eval EClass.player.dialogFlags.Where(x => x.Key.Contains("quest"))
```

## 移行チェックリスト

### 準備
- [ ] SourceQuest用pydanticモデル作成
- [ ] QUEST_DATAにクエスト定義追加
- [ ] create_source_tsv.pyにQuest生成追加
- [ ] build.batにQuest.tsv変換追加

### ドラマ更新
- [ ] startQuest使用箇所を特定
- [ ] modInvoke start_quest → action startQuest に変更
- [ ] completeQuest使用箇所を特定
- [ ] modInvoke complete_quest → action completeQuest に変更

### C#削除
- [ ] 使用箇所を検索（grep QuestManager等）
- [ ] 依存を解消
- [ ] ファイル削除
- [ ] ビルド確認

### テスト
- [ ] ゲーム内でクエスト開始テスト
- [ ] クエスト完了テスト
- [ ] ジャーナル表示確認
- [ ] エラーログ確認

## 参考: GoldenSoup移行結果

| 指標 | 移行前 | 移行後 | 削減 |
|------|--------|--------|------|
| C#行数 | ~2,100行 | ~1,200行 | -900行 |
| ファイル数 | 15+ | 9 | -6 |
| クエスト管理 | カスタム | CWLネイティブ | 簡素化 |

## トラブルシューティング

### startQuestでエラー
```
The given key 'quest_id' was not present in the dictionary.
```
→ SourceQuest.xlsxのidが正しく登録されているか確認

### ジャーナルに表示されない
→ type列が `Quest` になっているか確認

### フェーズが切り替わらない
→ `{base_id}{phase_num}` 形式でサブクエスト定義があるか確認
