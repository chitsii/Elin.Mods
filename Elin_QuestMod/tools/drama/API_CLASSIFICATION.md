# Drama Builder API Classification

`tools/drama/drama_builder.py` の API を、`純CWL` と `Mod DLL依存` に分類したメモ。

## Pure CWL / Vanilla Based

- 基本構築: `step`, `say`, `choice`, `jump`, `finish`, `on_cancel`
- 条件・フラグ: `set_flag`, `mod_flag`, `branch_if`, `switch_on_flag`, `check_quests`
- 演出アクション: `fade_in`, `fade_out`, `set_background`, `clear_background`, `shake`, `glitch`, `play_sound`, `wait`
- 組み込みジャンプ: `jump_to_*`, `jump_builtin`, `inject_unique`, `inject_builtin_choices`
- クエスト標準アクション: `start_quest`, `complete_quest`, `next_phase`, `change_phase`, `set_quest_client`, `update_journal`
- `invoke*` ヘルパー: `invoke_expansion`, `move_next_to`, `move_tile`, `move_to`, `play_emote`, `set_portrait`, `set_sprite`, `show_book`
- `eval` ヘルパー（ゲーム標準 API 呼び出し）: `play_bgm*`, `spawn_npc`, `resolve_flags_all/any`, `resolve_cooldown_elapsed_*`, `stamp_current_raw_time`

## Mod DLL Dependent (QuestMod Runtime Bridge)

以下は `Elin_QuestMod.Drama.DramaRuntime` への依存を持つ。

- `cs_call_common_runtime`
- `resolve_flag`
- `resolve_run`
- `play_pc_effect`
- `play_pc_effect_with_sound`
- `run_cue`
- `quest_check`
- `quest_try_start`
- `quest_try_start_repeatable`
- `quest_try_start_until_complete`

コード上の一覧: `DramaBuilder.MOD_DLL_DEPENDENT_APIS`
