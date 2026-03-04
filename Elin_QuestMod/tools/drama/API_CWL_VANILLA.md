# Drama API List (CWL / Vanilla)

Updated: 2026-03-02

## Source of Truth

### CWL official source

- `C:\Users\tishi\programming\elin_modding\Elin.Plugins\CustomWhateverLoader\API\Drama\DramaExpansion.cs`
- `C:\Users\tishi\programming\elin_modding\Elin.Plugins\CustomWhateverLoader\API\Drama\ActionBuilder.cs`
- `C:\Users\tishi\programming\elin_modding\Elin.Plugins\CustomWhateverLoader\API\Drama\Expansions\*.cs`

### CWL official docs

- `C:\Users\tishi\programming\elin_modding\Elin.Docs\articles\100_Mod Documentation\Custom Whatever Loader\JP\Character\4_drama.md`
- `C:\Users\tishi\programming\elin_modding\Elin.Docs\articles\100_Mod Documentation\Custom Whatever Loader\JP\API\Custom\drama.md`

### Game native reference (decompiled)

- `C:\Users\tishi\programming\elin_modding\Elin-Decompiled\Elin\DramaManager.cs`
- `C:\Users\tishi\programming\elin_modding\Elin-Decompiled\Elin\DramaCustomSequence.cs`
- `C:\Users\tishi\programming\elin_modding\Elin-Decompiled\Elin\DramaEventMethod.cs`

## 1. CWL Drama API

## 1.1 CWL special actions (action column)

- `inject`
- `choice` (CWL拡張条件付き choice)
- `i*`
- `invoke*`
- `eval`

## 1.2 CWL built-in expansion methods (invoke*/i* から呼ぶ)

### Action

- `add_item`
- `apply_condition`
- `cure_condition`
- `equip_item`
- `destroy_item`
- `remove_condition`
- `join_faith`
- `join_party`

### Modification

- `mod_affinity`
- `mod_currency`
- `mod_element`
- `mod_element_exp`
- `mod_fame`
- `mod_flag`
- `mod_keyitem`

### Scene

- `move_next_to`
- `move_tile`
- `move_to`
- `move_zone`
- `play_anime`
- `play_effect`
- `play_emote`
- `play_screen_effect`
- `pop_text`
- `portrait_set`
- `set_portrait`
- `set_sprite`
- `show_book`

### Quest

- `set_quest_state`
- `set_quest_text`

### Dynamic / meta

- `add_temp_talk`
- `build_ext` (deprecated in docs)
- `choice`
- `console_cmd`
- `debug_invoke`
- `emit_call` (deprecated in docs)
- `eval`
- `and`
- `or`
- `not`

### Condition

- `if_affinity`
- `if_condition`
- `if_cint`
- `if_cs_get`
- `if_currency`
- `if_element`
- `if_faith`
- `if_fame`
- `if_flag`
- `if_has_item`
- `if_hostility`
- `if_in_party`
- `if_keyitem`
- `if_lv`
- `if_race`
- `if_stat`
- `if_tag`
- `if_zone`

## 2. Game Native Drama API (Elin core)

`DramaManager.ParseLine()` の `action` 分岐で処理される標準 API。

## 2.1 Core flow / structure

- `choice`
- `cancel`
- `inject`
- `topic`
- `reload`
- `wait`
- `end`

## 2.2 Actors / dialog

- `addActor`
- `addTempActor`
- `setDialog`
- `hideDialog`
- `disableFullPortrait`
- `enableTone`
- `replace`

## 2.3 Visual / camera / effects

- `setBG`
- `setBG2`
- `setAdvBG`
- `glitch`
- `shake`
- `fadeIn`
- `fadeOut`
- `fadeInOut`
- `fadeEnd`
- `alphaIn`
- `alphaOut`
- `alphaInOut`
- `focus`
- `focusChara`
- `focusPC`
- `focusPos`
- `unfocus`
- `setAlwaysVisible`
- `effect`
- `effectEmbarkIn`
- `effectEmbarkOut`
- `propEnter`
- `propLeave`

## 2.4 Audio

- `BGM`
- `BGMStay`
- `Playlist`
- `editPlaylist`
- `lastBGM`
- `stopBGM`
- `sound`
- `haltBGM`
- `haltPlaylist`
- `keepBGM`
- `forceBGM`

## 2.5 Quest / flags / progression

- `setFlag`
- `startQuest`
- `completeQuest`
- `nextPhase`
- `changePhase`
- `setQuestClient`
- `updateJournal`
- `acceptQuest`
- `addKeyItem`
- `addResource`
- `modAffinity`
- `drop`

## 2.6 System / misc

- `save`
- `setHour`
- `hideUI`
- `showSkip`
- `canSkip`
- `canCancel`
- `screenLock`
- `tutorial`
- `slap`
- `removeItem`
- `destroyItem`
- `destroy`
- `moveZone`
- `bossText`
- `refAction1`
- `refAction2`
- `endroll`
- `%worship`

## 2.7 Legacy / alias / special entries in switch

- `new`
- `saveBGM`
- `checkAffinity`
- `buy`
- `sell`
- `give`
- `trade`
- `quest`
- `depart`
- `rumor`
- `bye`
- `bout_win`
- `bout_lose`

## 2.8 choice shorthand mapping (game native)

- `choice/quest` -> `_quest`
- `choice/depart` -> `_depart`
- `choice/rumor` -> `_rumor`
- `choice/buy` -> `_buy`
- `choice/sell` -> `_sell`
- `choice/give` -> `_give`
- `choice/trade` -> `_trade`
- `choice/bye` -> `_bye`

## 2.9 if / if2 native condition system

`CheckIF()` は以下を処理:

- 特殊キーワード (`fromBook`, `hasItem`, `isCompleted`, `hasDLC`, `hasFlag` など)
- 比較演算子ベース (`=`, `!`, `>`, `>=`, `<`, `<=`) のフラグ・クエストphase判定

## 2.10 Common built-in jump targets (from DramaCustomSequence)

- `_trade`
- `_buy`
- `_joinParty`
- `_leaveParty`
- `_train`
- `_heal`
- `_investShop`
- `_sellFame`
- `_copyItem`
- `_give`
- `_whore`
- `_tail`
- `_suck`
- `_bout`
- `_rumor`
- `_news`
- `_bye`
