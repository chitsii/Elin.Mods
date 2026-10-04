# Runtime Test (SukutsuArena)

SukutsuArena のランタイムテスト配置先です。  
共通ランナーはリポジトリ直下 `runtime-test-v2/` を利用します。

## 実行

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId <case_id>
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId patch.compat.cwl_incompatible_scan
```

## 構成

- ケース実装: `tests/runtime/src/cases`
- 実行結果: `tests/runtime/_artifacts`
- 追記ログ: 各 run の `_artifacts/<run_id>/playerlog.diff.log` と `playerlog.diff.meta.json`

## 実装済みケース

- `patch.targets.core_methods`
- `quest.chain.main_and_postgame`
- `quest.manager.phase_contract`
- `quest.dispatch.selection_contract`
- `drama.modinvoke_registry_contract`
- `arena.battle_pipeline_contract`
- `arena.zoneinstance.result_routing_flags`
- `arena.zoneinstance.direct_drama_schedule`
- `patch.compat.cwl_incompatible_scan` (能動互換チェック、既定 smoke から除外)

## 運用

- `-Suite smoke` の既定実行は `-Tag smoke` 扱い（`-Tag` 未指定かつ `-CaseId` 未指定時）
- 重要機能の回帰確認は `-Tag critical` を必須運用とする
- `summary.failed > 0` は失敗扱い

## 互換性能動チェック

- `patch.compat.cwl_incompatible_scan` は CWL の `TestIncompatibleIl` を能動実行する
- 実行時負荷/不安定化リスクがあるため既定 smoke には含めない
- 実行する場合は `-CaseId patch.compat.cwl_incompatible_scan` を明示する

## PR3 Native Integration

以下は実APIケースです。`patch.targets.core_methods` / `arena.battle_pipeline_contract` の存在・契約確認とは別分類です。
既定 smoke には含めません。前提不足は失敗になり、未検証のままpassedにしません。

| CaseId | 操作と期待値 |
| --- | --- |
| `pr3.arena.heal_long` | native `CharaGen.Create` の隔離キャラへ `HealHP(long, None)`。実NoHealingイベント＋arena instanceで2とInt32最大超は引数0・HP不増。イベント解除／外部zoneでは回復。0／負量は引数を維持し、負量の標準HP挙動を観測。 |
| `pr3.arena.damage_long` | 製品 `CardDamageHpPatchTarget.Apply` から9引数native DamageHPへInt32最大超を渡す。観測引数が一致し、HPは実DLLの99,999,999 capに従って減る。fixtureだけをovercap HPにし、死亡を避ける。 |
| `pr3.arena.factory_return_coordinates` | 実 `ArenaManager.StartBattleByStage("rank_g_trial", master)` にPCと異なる位置のnative masterを渡す。基底x/z=PC入口、独自returnX/Z=NPC位置、帰還zoneUIDとNPC UID保持をassert。一致を要求せず、factory後の同期もしない。 |
| `pr3.arena.return_serialization` | 実 `ZoneInstanceArenaBattle` を `GameIO.jsWriteGame/jsReadGame` で正常保存してroundtrip。PC入口48,49に対してNPC return49,49／masterなしreturn0,0の両方で、基底48,49と独自fields／UIDを維持。実save/reloadの代用ではない。 |
| `pr3.arena.return_victory` | native instance作成・MoveZone・製品pre-enterで敵を生成。実DamageHPで敵を倒し、製品OnTickの勝利待機・退出へ到達。帰還zoneUIDとPC座標、OnLeaveZone 1回、報酬plat 3、クエスト不変をassert。 |
| `pr3.arena.return_retreat` | 同じ実入場後、製品 `LeaveZone()` から退出。実結果2と帰還位置をassert。 |
| `pr3.arena.return_vanilla` | 同じ実入場後、native `MoveZone(..., Return)` が基底x/zを読む経路を確認。実結果2と帰還位置をassert。 |
| `pr3.arena.return_save_prepare` | PCの隣の空きtileにsleep中の固有NPCを配置し、native factory＋製品pre-enterで実入場。基底=PC入口／独自return=NPC位置の正常保存fixtureを改変せずmanifest出力。prepare-only。 |
| `pr3.arena.return_save_prepare_without_master` | 同じ実入場でNPC UID0／独自return0,0／基底=PC入口の正常保存fixtureを作る。prepare-only。 |
| `pr3.arena.return_save_verify` | 各prepareの外部save/reload後、manifest schema2とUIDでnative instanceを再取得。保存済み基底・独自fields・UIDを手動修復なしでassertし、実退出→PC入口到着とNPC UIDによる会話予約を確認。NPCと同tile到着は要求しない。 |
| `pr3.arena.return_save_abort` | 保持中のsave fixtureを退出・削除するcleanup-onlyケース。reload前でも使用可能。統合成功の証拠にはしない。 |

HPケースは現在zoneのinstanceとNoHealingイベントを同一フレーム内でfixtureとして設定し、実ゲームCardと製品Harmonyを通します。
遷移ケースは実native zone/製品pre-enter/戦闘イベントを使います。勝利OnTickはフレームごとにテストから駆動するため、通常操作だけでの自然tick/UI確認は別途必要です。
Observerはfixture UID/instanceだけを対象にし、引数・結果を変更せず、固有Harmony ownerを `UnpatchSelf()` で解除します。
報酬は実製品 `GiveReward` 内の2つの `ThingGen.Create` 呼出し直後をtest-owned transpilerで観測し、fixture instance参照・生成オブジェクト参照・UID・ID・予定数量を記録します。元のSetNum/Pick処理はそのまま実行します。cleanupはこの所有証拠が現在の品と一致するものだけを削除します。snapshot差分だけでは所有扱いせず、別callbackのplat、数量変更／merge、UID再利用などの証明不能な品は残留UIDをログ・失敗結果に記録し、専用baselineをreloadします。

### 実行担当の前提

- `RUNTIME_TEST` をPC名に含む、コピー済みの使い捨てsaveを使用。ケース側にも名前guardを置き、wrapper設定で解除できないようにしています。
- 通常saveには実行しない。遷移／保存ケースは `-CaseId` 単独実行必須。同行者なし・PC条件なし・pending drama/return spellなし・PC所持plat/lovepotionなしのfixtureを用意します。
- 入口は安全な非0座標。Mod本体・本物のfield source・putit source・製品Harmony patchがロード済みであること。
- 各fixtureケースの終了後、成功・失敗を問わず実行前baseline saveをreloadします（save_prepare成功時は専用handoffを先に完了）。UID採番、ゾーン移動、敵の死、地図生成、world統計などの全副作用をメモリrollbackだけで完全復旧したとは扱いません。
- cleanup失敗／外部timeout時は次ケースを実行せずbaselineをreload。外部timeoutはコルーチン停止を保証しません。

```powershell
# 実機担当だけが実行。ここでは生成・compileのみで未実行。
.\tests\runtime\run.ps1 -CaseId pr3.arena.heal_long -KeepGeneratedSource
.\tests\runtime\run.ps1 -CaseId pr3.arena.damage_long -KeepGeneratedSource
.\tests\runtime\run.ps1 -CaseId pr3.arena.return_vanilla -KeepGeneratedSource
```

### Save/Reload Handoff

1. baselineをコピーし、`pr3.arena.return_save_prepare` を単独実行します。fixture token、zoneUID、PC/NPC UID、入口と独自return座標、元flags/quests/inventory/cacheをschema2の `tests/runtime/_artifacts/pr3-return-<Game.id>.json` に出力します。NPCと敵fixtureにはnative ConSleepを付けます。NPC用にPC右隣の空きtileが必要です。
2. prepare成功時だけfixtureを保持します。実行担当が専用saveを保存・reloadし、保存時刻、reload、loaded DLL hashとゲームbuildを外部manifestに記録します。ケース自身はGame.Save/Loadを呼びません。
3. `pr3.arena.return_save_verify` を単独実行します。Gameオブジェクトのidentityが変わったことを補助guardで確認しますが、hashだけを保存・reloadの証明にせず、前項の外部記録を併用してください。
4. verifyの成功・失敗・例外時に退出、識別token一致のfixture zone/reward、manifestを除去し、元flags/quests/HP/入口位置/cache/drama/event状態をassertします。保存準備を中止する場合は `pr3.arena.return_save_abort`。元baseline saveのreloadを最後に実施します。
5. 別の専用baselineコピーで `pr3.arena.return_save_prepare_without_master` → 外部save/reload → 同じverifyを実行します。両scenarioの外部記録を残し、同座標fixtureだけでPC≠NPC／masterなしを検証済みとは扱いません。

### 座標同期追加の撤回

基底x/zはnative factoryがPC入口として設定・保存し、独自returnX/ZはNPC位置（masterなしでは0）を保持する元の仕様です。会話はNPC UIDで予約し、NPCと同tile到着は要求しません。今回追加したproperty/helper/OnDeserialized同期は正常保存の基底座標を上書きするため撤回しました。根拠のない旧保存migrationを当然視した `legacy_return_json` と、baseを消す／0へ変える保存fixtureは廃止しました。旧manifestはnormal-save証拠として受け付けません。

rollbackはmutation前に登録しています。非同期ケースは子IEnumeratorをyieldせず、同じenumerator内で期限付きframe待機を行います。
Scoped cleanupは元inventoryのUID/数量、全dialogFlags、クエストJSON、NoHealingイベント／instance、HP、pending drama、BGM halt、cache、autosave switchを確認します。
save準備成功時のfixture保持は明示的な引継ぎ例外で、cleanup結果にもbaseline未復元と記録します。
