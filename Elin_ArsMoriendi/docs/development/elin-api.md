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

## 種族由来Featの個体単位打ち消し（実装確認済み）

- 最終確認: 2026-03-07
- `Chara.SetFeat(id, 0)` は現在値をいったん逆適用してから base 値を再設定するため、`race` や `elements` 由来の feat でも個体単位で無効化できる。
- 単純に `elements.Remove(id)` すると feat の適用/逆適用を通らないため、種族由来 feat の打ち消しには使わない。
- 日光弱点は `Chara.Refresh()` 内で `HasElement(featAshborn) && !HasElement(431)` により再計算されるため、個体から日光体質を外したい場合は `featAshborn` を `SetFeat(..., 0)` で消す。

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

## `MakeMinion(EClass.pc)` 従者のバニラ制限（実装確認済み）

- 最終確認: 2026-03-09
- バニラは召喚系 Effect 実行時に `EClass._zone.CountMinions(CC) >= CC.MaxSummon || CC.c_uidMaster != 0` を見ており、`c_uidMaster != 0` のキャラは召喚系能力を使えない。
- そのため、`source.actCombat` に召喚能力を持つ個体でも、`MakeMinion(EClass.pc)` 後は召喚に失敗する。これは Trait 交換の有無ではなく、minion 化そのものによる制限。
- 例: シュブ＝ニグラスは `SpSummonShubKid` を持つが、従者化後は `c_uidMaster != 0` により落とし子召喚が失敗する。
- 現行の Ars Moriendi 従者モデルでは `MakeMinion(EClass.pc)` を使っているため、この制限を受ける。

### 通常ペットとの違い

- 通常のペット加入は主に `Party.AddMemeber` で処理され、party 参加だけでは `c_uidMaster` は立たない。
- したがって、通常ペット枠のキャラは原則として上記の「minion の召喚禁止」条件には引っかからない。
- 一方で Ars Moriendi の従者は `Party` ではなく `minion` として管理されるため、以下の差分が出る:
  - 召喚系能力を使えない
  - `WidgetRoster` / `maxAlly` など party ベース UI に出ない
  - ゾーン遷移時に PC 近傍 spawn ではなく、minion/carryover 側の経路で追従する
  - `AI_Idle` の一部 party 専用挙動（party 全体回復、共有コンテナ取得、読書、釣り追従など）に入らない
  - `CanJoinParty == false` の場合、`GetRevived()` はバニラで `homeZone` へ戻そうとする

### 設計上の含意

- 「召喚能力を持つ敵/ボスを従者化しても召喚能力を維持したい」場合、現行の `MakeMinion(EClass.pc)` モデルのままではバニラ制限に止められる。
- 回避策は実質的に次のどちらか:
  - 召喚系 Effect の `c_uidMaster != 0` 制限を個別パッチで緩和する
  - 従者の管理モデルを minion 以外（party / 独自追従管理など）へ再設計する
- Trait 差し替えや tactic 調整だけでは、この召喚禁止は解除できない。

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

## 固定マップの保存・再生成・差分適用（実装確認済み）

- 最終確認: 2026-03-05

### 保存/ロードの実体

- ゾーン実マップは `Spatial.pathSave`（`<save>/<gameId>/<zoneUid>/`）配下に保存される。
- `Map.Save(path)` は `map` 本体 + セル配列ファイル（`blocks`, `floors`, `flags` など）を保存する。
- `Zone.Activate()` は通常、`base.pathSave + "map"` を `GameIO.LoadFile<Map>` して `map.Load(...)` でセル配列を復元する。

### 固定マップ（.z）の読み込み

- 固定マップは `Zone.pathExport`（`CorePath.ZoneSave + idExport + ".z"`）から `Zone.Import(pathExport)` で展開される。
- インポート時は `pathTemp` に展開後、`pathTemp + "map"` を読み込む。
- その後 `map.OnImport(zoneExportData)` で `ZoneExportData.serializedCards` が復元される。

### 再生成時の既存データ引き継ぎ

- `Zone.Activate()` の再生成分岐（`flag3`）では `zoneExportData.orgMap = GameIO.LoadFile<Map>(base.pathSave + "map")` を保持し、読み込み後に一部マージする。
- 既存セーブからは主に以下が引き継がれる:
  - `charas / serializedCharas / deadCharas`
  - 視界フラグ（`flags` の bit1）
  - 一部の特殊 Thing（`TraitNewZone`, `TraitPowerStatue`, `TraitTent` など）
- 任意の設置物（通常 Thing）を完全保持する仕組みではないため、ゲーム更新や再生成で消えるケースがある。

### PartialMap の性質（注意）

- `PartialMap.Apply(..., ApplyMode.Apply)` は対象範囲タイルを書き換えた後、範囲内の `trait.CanBeDestroyed` な Thing を削除する。
- その後 `exportData.serializedCards.Restore(..., addToZone: true, partial)` で Partial 側カードを再配置する。
- したがって `PartialMap` は「差分追加」より「局所上書き」に近い。

### Mod実装指針（壊れにくい固定マップ拡張）

- 元 `.z` を直接上書きせず、Mod側で「非破壊の差分レイヤ」を持つ。
- 適用タイミングは `Zone.Activate` Postfix か、`OnVisitNewMapOrRegenerate` 相当の初回/再生成フローに同期する。
- 差分は `zoneId + anchor + relative placements` で定義し、絶対座標依存を下げる。
- 各配置に一意キーを持たせて冪等化し、重複配置を防ぐ。
- 競合セル（既存カードあり・通行不可など）は fail-soft で skip + ログ。

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
