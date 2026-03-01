# Elin API メモ

## よく使うAPI

| API | 用途 |
|-----|------|
| `CardManager.globalCharas` | `Dictionary<int, Chara>` - 全グローバルキャラ。UID で TryGetValue |
| `TraitFoodMeat` | 死体アイテムの Trait クラス（`TraitCorpse` は存在しない） |
| `ElementContainer.ModBase(id, v)` | `vBase += v`（**加算**）。Refresh Postfix で使う場合はデルタ追跡が必要 |
| `chara.AddCondition<T>(power, force)` | コンディション付与 |
| `chara.MakeMinion(master)` | ミニオン化 |
| `chara.SetSummon(duration)` | 一時召喚（ターン経過で消滅） |
| `SpawnListChara.Get(id, filter)` | フィルタ付きスポーンリスト取得 |

## Act クラスのパターン

- `Spell` を継承（`Ability` → `Act` の階層）
- `Act.CC` = 詠唱者, `Act.TC` = 対象, `Act.TP` = 対象地点
- `GetPower(Act.CC)` でスペルパワー取得
- 敵対スペルには `override bool IsHostileAct => true`

## ターン終了フック（実装確認済み）

- 最終確認: 2026-02-23
- `Player.EndTurn(bool)` は「ターン終了の予約」であり、実ターン処理本体ではない（`GoalEndTurn` をセットする）。
- 実際のプレイヤー1ターン終端は `Chara.Tick()` の末尾で `EClass.screen.OnEndPlayerTurn()` が呼ばれる箇所。
- 1ターンごとに1回実行したい処理（軽量メンテナンス等）は、`BaseGameScreen.OnEndPlayerTurn` の `Postfix` を優先する。
- ターン進行値を参照する場合は `EClass.pc.turn` を使う（`TickConditions()` で進む）。

## ロード時の「無効キャラ移動」補正（実装確認済み）

- 最終確認: 2026-02-24
- `Game.OnLoad` は拠点メンバーを走査し、`!isDead && (currentZone == null || currentZone.id == "somewhere")` を満たすと `MoveZone(child.owner, RandomVisit)` で拠点に戻す。
- このときログに `Moving invalid chara` が出る。
- `Zone.RemoveCard` は `Chara.currentZone = null` をセットするため、`homeBranch.members` に残したまま `RemoveCard` するスタッシュはこの補正対象になる。
- スタッシュ実装の基本方針:
  - `members` から外す場合は、`Reserve` 相当の永続強参照を必ず持つ。
  - `members` に残す場合は、`currentZone` を `null/somewhere` にしない（例: 拠点ゾーンへ移動）。

## 従者の faction 判定

従者は `MakeMinion(EClass.pc)` で作成されるため、faction 判定に注意:

| プロパティ | 従者での値 | 説明 |
|---|---|---|
| `IsPCFaction` | **false** | `faction == EClass.pc.faction` - 直接のファクションメンバーのみ |
| `IsPCFactionMinion` | **true** | master が `IsPCFaction` or `IsPCFactionMinion` |
| `IsPCFactionOrMinion` | **true** | `IsPCFaction || IsPCFactionMinion` |

従者の生存・所属チェックには **`IsPCFactionOrMinion`** を使うこと。
`IsPCFaction` だと従者（ミニオン）が全て除外される。

追放時: `FactionBranch.BanishMember` -> `RemoveMemeber` -> `SetFaction(Wilds)` -> `IsPCFactionOrMinion == false`

## SourceExcel データの探索

ゲーム内の全アイテム・キャラ・カテゴリ等は SourceExcel（xlsx）で定義されている。

### ソースファイル

- **場所**: `../SourceExcels/` （= `Elin.Mods/SourceExcels/`）
- **主要ファイルとシート**:

| ファイル | 主なシート |
|---|---|
| `SourceCard.xlsx` | Thing, ThingV, Food, Category, SpawnList, Recipe, Collectible, KeyItem |
| `SourceChara.xlsx` | Chara, CharaText, Race, Job, Hobby, Tactics |
| `SourceGame.xlsx` | Element, Zone, Quest, Religion, Faction, Research, Person |
| `SourceBlock.xlsx` | Block, Floor, Obj, Material, CellEffect |
| `Lang.xlsx` | General, Game, Note, List, Word |

### CSV変換済みデータ

`../SourceExcels/csv/`（= `Elin.Mods/SourceExcels/csv/`）に全シートをCSV化してある。grep/Python で即検索可能。

```bash
# 再生成（ゲーム更新時）
python ../SourceExcels/convert_source_excel.py

# 使用例: カテゴリ "booze" に属するアイテムを探す
grep "booze" ../SourceExcels/csv/SourceCard_ThingV.csv

# 使用例: アイテムIDで逆引き
grep "^crimAle," ../SourceExcels/csv/SourceCard_ThingV.csv

# 使用例: カテゴリ階層を確認
grep "drink" ../SourceExcels/csv/SourceCard_Category.csv
```

### シート間の関係

- **Thing**: ベースアイテム定義（id, category, trait, elements 等）
- **ThingV**: 派生アイテム（`_origin` で Thing の行を継承し、差分だけ上書き）
- **Category**: カテゴリ階層（`_parent` で親子関係。例: `booze` → 親 `drink`）
- **Food**: 食品の追加属性（食事効果等）

アイテムの正式なカテゴリは Thing/ThingV の `category` 列で確認する。

## Drama Runtime 連携キー（Quest Bridge）

- 最終確認: 2026-02-17
- `state.quest.can_start.<drama_id>`
  - `ArsDramaResolver.TryResolveBool` で解決。
  - `GameArsDramaRuntimeContext.CanStartDrama` は `chitsii.ars.drama.started.<drama_id>` を見て未開始なら true。
- `cmd.quest.try_start.<drama_id>`
  - `ArsDramaResolver.TryExecute` で解決。
  - `GameArsDramaRuntimeContext.TryStartDrama` は idempotent（既開始なら false）。
  - 開始時に `chitsii.ars.drama.started.<drama_id> = 1` を保存して `QuestDrama.PlayDeferred(drama_id)` を呼ぶ。
  - 既開始時は `QuestBridge.TryStartDrama: skipped already started (...)` をログ出力。
