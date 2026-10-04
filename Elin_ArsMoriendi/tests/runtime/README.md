# Runtime Test (ArsMoriendi)

ArsMoriendi のランタイムテスト配置先です。  
共通ランナーはリポジトリ直下 `runtime-test-v2/` を利用します。

## 実行

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId <case_id>
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId patch.compat.cwl_incompatible_scan
```

## 構成

- ケース実装: `tests/runtime/src/cases`
- ドラマ定義: `tests/runtime/src/drama`
- 実行結果: `tests/runtime/_artifacts`
- 追記ログ: 各 run の `_artifacts/<run_id>/playerlog.diff.log` と `playerlog.diff.meta.json`

## オプション

- `-KeepGeneratedSource`: 既定では保存しない `runtime_suite_v2_generated.csx` を `_artifacts` に残す
- `-Suite smoke` の既定実行は `-Tag smoke` 扱い（`-Tag` 未指定かつ `-CaseId` 未指定時）

## 実装済みケース

- `patch.targets.spawnloot_and_oncharadie`
- `patch.compat.cwl_incompatible_scan`
- `spell.soultrap_drop_pipeline`
- `spell.preservecorpse_guaranteed_drop`
- `spell.soulbind_substitution`
- `servant.ritual_create_and_track`
- `servant.release_detach_and_cleanup`
- `servant.revive_master_persistence`
- `quest.stage0_to1_first_soul`
- `quest.stage1_to2_tome_open`
- `quest.stage2_knight_spawn_once`
- `quest.stage7_erenos_defeat_advance`
- `quest.branch_contract_rule_presence`
- `quest.branch_runtime_flag_select`
- `quest.branch_converge_common_successor`
- `ars_apotheosis` ほか `ArsDramaRuntimeCatalog` に定義された drama ケース

## クリティカル運用

- `-Tag critical` は呪文/従者/クエスト進行の回帰を検知する必須ゲート。
- `summary.failed > 0` は即失敗扱い（再実行しない）。

## 互換性能動チェック

- `patch.compat.cwl_incompatible_scan` は CWL の `TestIncompatibleIl` を能動実行するため、既定 smoke からは除外。
- 実行する場合は `-CaseId patch.compat.cwl_incompatible_scan` を明示する。

## PR4 実ゲーム統合ケース

実装と静的コンパイルの証拠を、ゲーム内でpassedになった証拠と混同しない。
起動・deploy・save/load・zone操作はruntime実行担当だけが行う。
全ケースは追加の `RUNTIME_TEST` 名前guardを持ち、作成名は
`RUNTIME_TEST_PR4_ARS_<nonce>_<role>`、後始末はそのrunのUIDだけに限定する。

| CaseId | 実操作とassert |
|---|---|
| `pr4.ars.temporary_lifecycle` | 実召喚呪文2種のPerform。non-global、PC master、正の寿命、map/carryoverでUID一意、GetAlive/GetAll各1件、sv.flag不在、stash拒否。fixtureの寿命を1にしてnative TickConditions -> Die -> Destroy、もう一方はnative DamageHP(Euthanasia)で死。queryによるprune前にuntrack、FX/flags/map/global/carryover/deadCharas不在とAlive/All/Combat検索不在をassert。破棄済みinstance上のpresence markerは実値を診断ログへ記録し、不在assertはしない。live個体のmarker要件は維持。通常召喚・敵主従(Ars marker付き)は除外、永久従者は保持。 |
| `pr4.ars.temporary_destroy` | 実召喚 -> native Destroy -> 通常GetAll query。UID/FX/flags/remnants/deadCharasとAlive/All/Combat検索不在を別検証し、破棄済みinstance上のmarkerは実値診断のみ。Dieの代用ではない。 |
| `pr4.ars.permanent_control` | 公開RegisterRitualServantで登録、global/home/trait/masterとsv.flag=1。永久stash/recall、native Dieでglobal死体保持、native Revive後の追跡保持。home branch必須。復活後のmap配置はfixture補助であり、標準zone自動復活の証明ではない。 |
| `pr4.ars.temporary_save_prepare` | 実召喚2種とcontrolsを保持し、外部保存完了を最大120秒待つ。UID/名前/残寿命/master/zone/Game identityと元状態をhandoffへ記録。 |
| `pr4.ars.temporary_save_verify` | 外部reload後、新Game・同PC/zone・同UID/名前を確認。古い参照を使わずmap/global/carryoverから再取得、通常manager queryだけで自動追跡をassert。手動Register/Reconcile補修なし。controlsの除外と永久保持も再検証。 |
| `pr4.ars.carryover_cleanup` | 実呪文2種を保持し、外部zone移動を最大120秒待つ。native AddGlobalCharasOnActivate前のcarryoverをpass-through observerで観測。新zoneの同UID一意/non-global/追跡とnative寿命tickで掃除をassert。 |
| `pr4.ars.dice_damage_heal` | native fixture死体1個と敵を配置してActCorpseChainBurst.Perform。pass-through observerで実Dice.Roll(long)、9引数DamageHP(long)の量と60%cap、HealHP(long)を記録し、実HP減少/回復と死体消費をassert。乱数の厳密値を固定しない。 |

新規ケースには `smoke` / `critical` を付けていない。二段階の保存ケースを含むため、
`-Tag pr4` 一括実行ではなく、必ず個別CaseIdを指定する。
従来 `servant.ritual_create_and_track` / `servant.release_detach_and_cleanup` は
AddServant(一時登録)ではなく公開RegisterRitualServantでfixtureを作る。
既存SoulBindケース/設計は変更しない。

### 実行前提と後始末

- 通常saveは使用しない。専用の使い捨てsaveを実行前にコピーし、autosaveを止める。
  清浄なhomeと隣接する非quest zone、未騎乗PC、召喚枠2つ以上を用意する。
- Diceケースは既存従者0、可視の既存死体0、fixture以外の敵0、爆心4マスのblock/object0が必須。
  本物spellが周囲の死体を消費し壁を掘るので、前提違反はprepare failedとする。
- mutation前にrollbackを登録。UID別に追跡・presence・FX registry/lease・party・home/reserve・global・carryover・
  fixture inventory/dropを掃除し、元の従者/party/home UID集合とArs dialog flags(quest含む、VFX一度限りmigrationを除く)をassertする。
  複数rollbackは1件失敗してもhostが残りを試行する。
- これは全世界の完全復元ではない。死亡の標準統計/ログ、branch再計算、zone/PC移動、他Mod副作用は
  捨てsaveで隔離し、各ケース後に実行前saveをreloadする。cleanup失敗時は次ケースへ進まずreloadする。
- waitは外側IEnumeratorで期限付き `yield return null` のみ。子IEnumeratorの例外をhostに委ねない。
  外部timeoutを内部120秒より短くしない。ゲームをreload/終了してコルーチンを中断する前にprepare run完了を待つ。
- observerはfixture UID/実castに限定して引数・戻り値・元処理を変更しない。
  後始末はtest専用ownerのUnpatchSelfだけ。製品Harmony ownerは外さない。
- 召喚fixtureの所有はmap差分では判定しない。既存global/map/carryover UID(PC含む)を保護し、
  native `CharaGen._Create` の `new Chara` と対象Performインスタンスの生成呼出の戻り値の同一参照を照合する。
  一時test-only transpilerは元の命令・引数・戻り値を保ち、`dup`で受動記録を挟むだけ。
  生成siteの形が変わった場合や証明できない戻り値はfailed。未知のmap追加を改名/Own/Destroyせず、専用saveの手動復元を要求する。
  cleanupは先頭のUID解決だけに頼らず、global/map/carryover内の全同UID参照をnative mutation前に検査する。
  別instanceが1件でもあればfixture本体・所持品のcleanupを拒否する。carryover削除はReferenceEquals一致だけ。
  旧handoffはownershipVersionで拒否する。

### save/reload 二段階手順

1. 元の捨てsaveをバックアップし、古いhandoff/receiptを証拠フォルダへ移す。
   Mod rootで次を実行し、別のruntime操作窓からhandoff生成を待つ。

   ```powershell
   .\tests\runtime\run.ps1 -Suite smoke -CaseId pr4.ars.temporary_save_prepare -TimeoutSeconds 180 -KeepGeneratedSource
   ```

2. `tests/runtime/_artifacts/pr4-save-handoff.json` がreadyの証拠。
   **fixtureがまだゲーム内にいる間に**別の専用slotへ実際にsaveし、save成功・slot/path/hashを外部manifestへ記録する。
   保存に成功した場合だけ、handoffのtokenを同じpathの `.saved` ファイルへ書く。

   ```powershell
   $p = '.\tests\runtime\_artifacts\pr4-save-handoff.json'
   $handoff = Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
   Set-Content -LiteralPath ($p + '.saved.tmp') -Value $handoff.token -Encoding UTF8
   Move-Item -LiteralPath ($p + '.saved.tmp') -Destination ($p + '.saved')
   ```

3. prepareの結果JSONとrollback完了を待つ。live fixtureはここで削除されるが、保存済みsnapshotには残る。
   保存先slotを実reloadし、元Gameと違うinstanceになってから次を実行する。

   ```powershell
   .\tests\runtime\run.ps1 -Suite smoke -CaseId pr4.ars.temporary_save_verify -KeepGeneratedSource
   ```

4. verify後に結果JSON/Player.log差分/handoff/receipt/slotの証拠を保存し、元の清浄な捨てsaveへreloadする。
   handoffは証拠として自動削除しない。次prepare前にarchiveする。
   prepareがtimeout/failedならverifyしない。途中reloadや手動登録で合格にしない。

### zone移動と単発実行

```powershell
.\tests\runtime\run.ps1 -Suite smoke -CaseId pr4.ars.carryover_cleanup -TimeoutSeconds 180 -KeepGeneratedSource
```

fixture生成後、120秒以内に別の非quest zoneへ本物の移動を行う。
carryover observerが両UIDを観測し、新zoneが有効になるまでcaseが待つ。
caseの終了後、元saveから戻す。PC/zone移動はrollbackで書き戻さない。
その他単発ケースは同じcommandのCaseIdを差し替える。

### 証拠と未実施範囲

型/patch存在だけの既存 `servant.revive_master_persistence`、`spell.*_pipeline` 等はstructural契約チェック。
新規ケースのHP/UID/寿命の統合assertと区別する。
ゲーム内結果にはbuild/channel、loaded DLL hash/assembly、observer/製品Harmony owner、fixture UID、
native call数/引数、結果JSON、Player.log差分、cleanup結果を添える。
DiceのInt32超返値stress、UnholyVigor/DeathZone/PlagueTouchの追加long経路、
hearth reserve UI、Zone.OnVisitからの自然永久復活、視覚FX/audioの目視は本ケース群だけでは証明しない。

### 所有判定のオフライン回帰

```powershell
dotnet test .\tests\runtime\offline\ArsPr4FixtureOwnership.Tests.csproj
```

game型なしのtest-helper policyを直接テストする。既存global/carryover/map UIDの拒否、
生成証拠なし・別spell instance・無関係な生成・同UID別参照の拒否、native birthと正しい生成戻り値の受入れを検証する。
これはゲーム内Harmony計測の成功証拠ではない。出生/生成siteの実機計測はruntime担当が別途確認する。
cleanup反例は実runtime cleanupが使うArsPr4CleanupBoundary.Runを直接実行する。
mapの所有Aに加えてglobal/map/carryoverの未所有Bが同UIDでも、mutation callbackが0回でBが残ることを検証する。
またguard後に同UIDのBが追加されても、参照限定carryover削除がBを削除しないことを確認する。
