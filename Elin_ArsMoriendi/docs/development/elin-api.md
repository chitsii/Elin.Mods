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

## `ActEffect.GetTeleportPos` の互換呼び出し

- ソース確認: Stable EA23.338.2 (`35fac67c8cd3adbc145f37a55b0f04b1347dbb73`) / Nightly EA23.351.2 (`38a69bd9e976e46d5a512bb4811d586b96e70b3c`) の `Elin/ActEffect.cs`。
- Stable は `Point GetTeleportPos(Point org, int radius = 6)`、Nightly は末尾に `Chara target = null` を追加。追加条件は `(target == null || i >= 100 || Los.IsVisible(point, target.pos))` なので、`null` なら Stable の探索条件・最終 fallback を維持する。
- `ActGraveExile` の2箇所と `ActSoulRecall` の1箇所は `TeleportPosCompat.GetTeleportPos` を使用する。`CompatSymbol` の既知2署名を `MethodResolver` で warmup / キャッシュし、初回使用時に型付き delegate を1回だけ bind する。Nightly には明示的に `null` を渡す。
- 未知署名への緩い fallback は行わない。解決失敗は resolver が1回ログし、binding 失敗は `Lazy` がキャッシュする。呼出し時は例外で既存の spell `Perform()` の catch に戻す。安全性不明の `origin` 等を成功位置として返さない。ゲーム側の例外は delegate から元の例外のまま伝播し、binding を失敗扱いにしない。
- オフライン contract テスト: `dotnet test tests/Elin_ArsMoriendi.Compat.Tests/Elin_ArsMoriendi.Compat.Tests.csproj -p:TeleportContract=Stable`。`Nightly` / `Unsupported` も個別実行する。fixture のメソッド本体は上記実ソースから抽出し、map / random / LOS 境界のみ制御している。実 Nightly DLL / ゲーム内検証の代替にはならない。
- tracker の対象には追加済み。既存の `stable_signatures.json` / `nightly_signatures.json` は過去の実DLL収集結果なので、現在版の実DLLで再収集するまで更新済みとは扱わない。ソース由来 fixture と実DLL catalog を混ぜない。

## `Card.DamageHP` の互換呼び出し（実装確認済み）

- 最終確認: 2026-05-10
- Elin 23.304 Nightly で `Card.DamageHP(long, int, int, AttackSource, Card, bool, Thing, Chara)` に `resistPenetrationLevel` 引数が追加され、9引数版になった。
- C# の optional 引数はコンパイル時に呼び出しシグネチャが焼き込まれるため、安定版DLLでビルドした直接呼び出しは Nightly で `MissingMethodException` や効果不発の原因になる。
- Ars Moriendi 内で `DamageHP` を使う場合は直接呼び出さず、`CardDamageHpCompat.Apply(...)` を使う。
- `CardDamageHpCompat` は実行時に `Card.DamageHP` の8引数版/9引数版を解決し、Nightly では `resistPenetrationLevel` まで渡す。

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

## 従者の faction 判定（実装確認済み）

- 最終確認: 2026-03-25

従者は `MakeMinion(EClass.pc)` で作成されるため、faction 判定に注意:

| プロパティ | 従者での値 | 説明 |
|---|---|---|
| `IsPCFaction` | **false** | `faction == EClass.pc.faction` - 直接のファクションメンバーのみ |
| `IsPCFactionMinion` | **true** | master が `IsPCFaction` or `IsPCFactionMinion` |
| `IsPCFactionOrMinion` | **true** | `IsPCFaction || IsPCFactionMinion` |

従者の生存・所属チェックには **`IsPCFactionOrMinion`** を使うこと。
`IsPCFaction` だと従者（ミニオン）が全て除外される。

- ただし `IsPCFactionMinion` は `c_uidMaster != 0` ではなく **`master` 参照が生きているか** を見ている。
- `master == null` でも `IsMinion` 自体は `c_uidMaster != 0` で true になりうる。
- そのため、死亡済み/オフマップ/banish 済み minion は `c_uidMaster` が残っていても、`master` 未解決の間は `IsPCFactionOrMinion` が false になりうる。
- オフマップ死体やロード直後の minion を判定する場合は、`FindMaster()` か `c_uidMaster` を補助条件として併用する。

## 従者の resident カウントと `homeBranch.members` の罠（実装確認済み）

- 最終確認: 2026-03-28
- `TraitUndeadServant.IsCountAsResident == false` のため、`FactionBranch.CountMembers(FactionMemberType.Default)` を使う UI や人口上限判定には従者が入らない。
- そのため、住民数 UI、最大人口、過密系の基本判定は追加 patch なしでも従者を除外できる。
- ただし、バニラには `EClass.pc.homeBranch.members.Count` をそのまま人数扱いする処理がある。
- 確認した具体例は `QuestCompanion.CanUpdateOnTalk()` と `QuestCompanion.GetTextProgress()`。
- 「バニラ勧誘 NPC の人数だけを見たい」用途では、`CountMembers(Default)` へ単純置換せず、`members.Count - Ars従者数` のように mod 管理個体だけを差し引く方が安全。

## 盟約の石 reserve と従者の衝突（実装確認済み）

- 最終確認: 2026-03-29
- `FACTION.AddReserve(Chara)` は Hearth Stone の「盟約の石に移す」で使われる。
- この処理は `IsHomeMember()` なら `homeBranch.RemoveMemeber(c)` を呼び、その後 `currentZone.RemoveCard(c)` して `listReserve` へ積む。
- しかし minion の detach は行わないため、従者に対して使うと `homeBranch` と `currentZone` を失ったまま `c_uidMaster` だけ残る中途半端な状態になりうる。
- Ars Moriendi の従者はこの reserve に直接入れず、`退避` へ振り替える方が安全。
- 既に reserve に入っている従者を扱う場合は、整合性チェックや remnant purge から除外し、呼び戻し時に従者追跡を再同期する。

## 遠征 (`Expedition`) と従者の衝突（実装確認済み）

- 最終確認: 2026-03-29
- `ExpeditionManager.Add(Expedition)` は `Expedition.Start()` を呼び、対象キャラを `MoveZone("somewhere")` する。
- `ListPeopleExpedition.OnList()` は `Branch.members` の非パーティメンバーをそのまま遠征候補に入れる。
- バニラは `homeBranch.members` に残ったキャラが `currentZone == null/somewhere` の場合、ロード時に拠点へ戻す補正を持つ。
- そのため、従者を遠征へ送ると `somewhere` 管理と従者 runtime/state 管理が衝突しやすい。
- Ars Moriendi の従者は遠征候補から除外し、backend 側でも `ExpeditionManager.Add` を拒否する方が安全。

## 屠殺 (`AI_Slaughter`) 後の banish / revive（実装確認済み）

- 最終確認: 2026-03-25
- `AI_Slaughter` は完了時に `target.Die()` を呼んだ後、unique Trait でない相手には `target.Chara.homeBranch.BanishMember(target.Chara, skipMsg: true)` を実行する。
- `FactionBranch.RemoveMemeber` はメンバー一覧から外した上で `SetFaction(Wilds)` を行うが、`homeZone` と `c_uidMaster` はここでは消さない。
- `big_daddy` は SourceChara 上 `quality=4`（Artifact 扱い）なので、`RemoveMemeber` でも `RemoveGlobal()` されず、global chara のまま banish されうる。
- その結果、屠殺済みでも `homeZone` に紐づく dead global chara として残り、後日 `Zone.Revive()` の `value.isDead && value.CanRevive() && value.homeZone == this` に拾われて復活しうる。
- ただし banish 後は `IsPCFaction` を失っているため、復活後は「従者リストにいない / minion の痕跡はある / 肉切り包丁対象にならない / big_daddy 死亡時の `littleOne` pop 条件 (`!IsPCFaction`) は満たす」という破綻状態が起こりうる。

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

## Drama の `inject/Unique` 挙動（実装確認済み）

- 最終確認: 2026-03-12
- `action=inject`, `param=Unique` は `DramaCustomSequence` の Unique 会話ビルダーを現在の talk に差し込み、NPC 状態に応じてバニラの `_invite`, `_joinParty`, `_leaveParty`, `_buy`, `_heal` などを追加する。
- 追加候補は `Trait`, `IsHomeMember`, `affinity.CanInvite()`, `CanJoinParty` などで変わるため、「仲間化の導線だけ欲しい」場合でも表示内容は NPC の状態依存になる。
- merchant 系ドラマで mod 側が手動で `_buy` を置いている場合、`inject/Unique` と同居させると取引導線が重複しうる。解禁後専用メニューへ分離するか、手動 `_buy` を片側だけに寄せる。
- `_choices` は `inject` で蓄積したバニラ選択肢を同一会話内の後続 prompt に再注入するためのアクション。勧誘や join/leave 後に会話が終了する設計なら、`inject/Unique` 単独で十分なことが多い。
