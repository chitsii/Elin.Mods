# Drama DSL API Design V5.1 (Top-Down Authoring)

Updated: 2026-03-02

- 逆順定義を強制しない（トップダウン記述可能）
- `story(nodes=[...])` の二重管理をなくす
- フリー関数の名前衝突を避ける（`d.*` 名前空間）

## 1. North Star

- 物語順に読めるコード
- 接続ミスは compile 時に確実に検出
- 大規模シナリオでも編集コストが増えにくい

## 2. Architecture

## 2.1 Authoring Layer (stateful, minimal)

`DramaDsl` はノード登録レジストリだけを持つ。

- `d.node(name, *steps)` でノードを登録
- 戻り値は `NodeRef`（接続に使う）
- 遷移先は `NodeRef` / `str` のどちらでも指定可能

## 2.2 Spec Layer (pure value)

`StepSpec`, `Cond`, `OptionSpec`, `StorySpec` は純粋データとして扱う。

## 2.3 Compile Layer (impure)

- `compile_xlsx(spec, strict=True) -> Workbook`
- `save_xlsx(workbook, path, sheet="main") -> None`

## 3. Type Model (5 kinds)

- `Id(kind, value)`
- `Chara(alias)`
- `NodeRef(name)`
- `Cond(op, args...)`
- `StepSpec(kind, payload...)`

補助構造:

- `OptionSpec(label, to, cond?)`
- `StorySpec(start, nodes, meta)`

## 4. Public API (V5.1)

## 4.1 Root

- `DramaDsl(mod_name: str, default_lang: str = "jp")`
- `d.reset() -> None`  # 登録済みノードを破棄（同一プロセスで複数specを作る時）

## 4.2 Graph

- `d.node(name: str, *steps: StepSpec) -> NodeRef`
- `d.ref(name: str) -> NodeRef`  # 文字列より明示したい時のみ
- `d.story(start: NodeRef | str, meta: dict | None = None, nodes: list | None = None) -> StorySpec`
  - `nodes=None` のときは `d.node()` 登録順を自動収集する
  - `nodes=[...]` を指定すると明示セットを優先（テスト・分割用）

## 4.3 Entity / ID

- `d.id(kind: str, value: str) -> Id`
- `d.chara(alias: str) -> Chara`

## 4.4 Dialogue / Flow

- `d.line(jp: str, actor: Chara | str | None = None, en: str = "", cn: str = "") -> StepSpec`
- `d.say(jp: str, actor: Chara | str, en: str = "", cn: str = "") -> StepSpec`
- `d.dialog(lines: list[StepSpec | tuple] | None = None, prompt: StepSpec | None = None, choices: list | None = None, cancel: NodeRef | str | None = None) -> StepSpec`
- `d.option(label: str, to: NodeRef | str, cond: Cond | None = None) -> OptionSpec`
- `d.when(cond: Cond, then_to: NodeRef | str, else_to: NodeRef | str | None = None) -> StepSpec`
- `d.go(to: NodeRef | str) -> StepSpec`
- `d.end() -> StepSpec`

## 4.5 Scene / Staging / Quest

- `d.transition(bg: Id | None = None, bgm: Id | None = None, fade: float = 0.5, clear_bg: bool = False, stop_bgm: bool = False) -> StepSpec`
- `d.wait(seconds: float) -> StepSpec`
- `d.sound(snd: Id) -> StepSpec`
- `d.effect(fx: Id, actor: Chara | str | None = None) -> StepSpec`
- `d.shake() -> StepSpec`
- `d.glitch() -> StepSpec`
- `d.spawn(actor: Chara | str, chara_id: Id, level: int | None = None) -> StepSpec`
- `d.move_to(actor: Chara | str, x: int, y: int) -> StepSpec`
- `d.move_tile(actor: Chara | str, dx: int, dy: int) -> StepSpec`
- `d.move_next_to(actor: Chara | str, target: Chara | str) -> StepSpec`
- `d.emote(actor: Chara | str, emote_id: Id, duration: float | None = None) -> StepSpec`
- `d.set_portrait(actor: Chara | str, portrait_id: Id | None) -> StepSpec`
- `d.set_sprite(actor: Chara | str, sprite_id: Id | None) -> StepSpec`
- `d.quest_begin(quest_id: Id, phase: int = 1, journal: bool = True) -> StepSpec`
- `d.quest_update(quest_id: Id, phase: int | None = None, journal: bool = False) -> StepSpec`
- `d.quest_finish(quest_id: Id, phase: int | None = 999, journal: bool = True) -> StepSpec`

## 4.6 Conditions

- `d.has_flag(flag_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_item(item_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_condition(cond_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_feat(feat_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_keyitem(key_id: Id, expr: str = ">0") -> Cond`
- `d.in_zone(zone_id: Id, level: int | None = None, actor: Chara | str = "pc") -> Cond`
- `d.native_if(raw_if_expr: str) -> Cond`
- `d.all_of(*conds: Cond) -> Cond`
- `d.any_of(*conds: Cond) -> Cond`
- `d.not_(cond: Cond) -> Cond`

## 4.7 Mod Command Layer (3-class split)

純CWL API とは分離して、Modカスタムコマンド層を3クラスで管理する。

- `ModCommands`: 共通（キー解決ベース）
- `ArenaModCommands(ModCommands)`: SukutsuArena 専用 `modInvoke` コマンド
- `ArsModCommands(ModCommands)`: Ars Moriendi 専用キー/キュー

`DramaBuilder`（純CWL層）は純CWLに留める。  
`setFlag/startQuest/completeQuest/changePhase/updateJournal` などの純CWL/標準action APIはこの層に置かない。

### 4.7.1 ModCommands (base)

目的:

- `state.*` / `cmd.quest.*` / `cue.*` / `fx.pc.*` の依存キー解決を共通化する
- シナリオ側から `eval` 文字列を隠蔽する

クラス:

- `ModCommands(runtime_type: str = "Elin_CommonDrama.DramaRuntime")`

API:

- `r.resolve_flag(dep_key: str, out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
- `r.resolve_flags_all(dep_keys: list[str], out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
- `r.resolve_flags_any(dep_keys: list[str], out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
- `r.resolve_run(command_key: str, actor: Chara | str | None = None) -> StepSpec`
- `r.quest_check(drama_id: str, out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
  - `state.quest.can_start.<drama_id>`
- `r.quest_can_start(drama_id: str, out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
  - `state.quest.can_start.<drama_id>`
- `r.quest_is_done(drama_id: str, out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
  - `state.quest.is_done.<drama_id>`
- `r.quest_try_start(drama_id: str, actor: Chara | str | None = None) -> StepSpec`
  - `cmd.quest.try_start.<drama_id>`
- `r.quest_try_start_repeatable(drama_id: str, actor: Chara | str | None = None) -> StepSpec`
  - `cmd.quest.try_start_repeatable.<drama_id>`
- `r.quest_try_start_until_complete(drama_id: str, actor: Chara | str | None = None) -> StepSpec`
  - `cmd.quest.try_start_until_complete.<drama_id>`
- `r.quest_complete(drama_id: str, actor: Chara | str | None = None) -> StepSpec`
  - `cmd.quest.complete.<drama_id>`
- `r.run_cue(cue_key: str, actor: Chara | str | None = None) -> StepSpec`
  - `cue.<cue_key>`
- `r.play_pc_fx(effect_id: str, actor: Chara | str | None = None) -> StepSpec`
  - `fx.pc.<effect_id>`
- `r.play_pc_fx_with_sfx(effect_id: str, sfx_id: str, actor: Chara | str | None = None) -> StepSpec`
  - `fx.pc.<effect_id>+sfx.pc.<sfx_id>`

### 4.7.2 ArenaModCommands(ModCommands)

目的:

- SukutsuArena の `modInvoke(method(args...))` を型付きAPIで隠蔽する
- 既存 `ArenaCommandRegistry` の登録コマンドを1:1でラップする

低レベル:

- `a.mod_invoke(method: str, *args: str | int | float, actor: Chara | str = "pc") -> StepSpec`

クエスト/分岐:

- `a.check_quest_available(quest_id: str, jump_to: NodeRef | str, actor: Chara | str = "pc") -> StepSpec`
- `a.check_available_quests(npc_id: str = "", actor: Chara | str = "pc") -> StepSpec`
- `a.check_quests_for_dispatch(out_flag: Id, quest_ids: list[str], actor: Chara | str = "pc") -> StepSpec`
- `a.start_quest(quest_id: str, actor: Chara | str = "pc") -> StepSpec`
- `a.complete_quest(quest_id: str, actor: Chara | str = "pc") -> StepSpec`
- `a.start_drama(drama_id: str, actor: Chara | str = "pc") -> StepSpec`
- `a.if_flag(flag: Id, op_expr: str, jump_to: NodeRef | str, actor: Chara | str = "pc") -> StepSpec`
  - 例: `op_expr=">=1"`
- `a.switch_flag(flag: Id, cases: list[NodeRef | str], fallback: NodeRef | str | None = None, actor: Chara | str = "pc") -> StepSpec`

デバッグ:

- `a.debug_log_flags(actor: Chara | str = "pc") -> StepSpec`
- `a.debug_log_quests(actor: Chara | str = "pc") -> StepSpec`

Arena専用操作:

- `a.leave_party_to_arena(actor: Chara | str = "pc") -> StepSpec`
- `a.apply_iris_training_buff(buff_type: str, actor: Chara | str = "pc") -> StepSpec`
- `a.apply_iris_training_punish(punish_type: str, actor: Chara | str = "pc") -> StepSpec`
- `a.apply_deep_ecstasy(actor: Chara | str = "pc") -> StepSpec`
- `a.start_newgame(actor: Chara | str = "pc") -> StepSpec`
- `a.start_uninstall(actor: Chara | str = "pc") -> StepSpec`
- `a.check_resurrection_materials(actor: Chara | str = "pc") -> StepSpec`
- `a.consume_resurrection_materials(actor: Chara | str = "pc") -> StepSpec`
- `a.spawn_cain(actor: Chara | str = "pc") -> StepSpec`
- `a.add_chickens_to_party(count: int = 1, actor: Chara | str = "pc") -> StepSpec`
- `a.update_lily_shop_stock(actor: Chara | str = "pc") -> StepSpec`
- `a.invoke_oheya_scroll(radius: float, actor: Chara | str = "pc") -> StepSpec`
- `a.hide_npc(npc_id: str, actor: Chara | str = "pc") -> StepSpec`
- `a.full_recovery(actor: Chara | str = "pc") -> StepSpec`
- `a.check_has_wrath(actor: Chara | str = "pc") -> StepSpec`
- `a.pay_wrath_fee(actor: Chara | str = "pc") -> StepSpec`

### 4.7.3 ArsModCommands(ModCommands)

目的:

- Ars 固有の `state.*` / `cmd.*` / `cue.*` キーを型付きで扱う
- `resolve_run("...")` 直書きを減らす

状態解決:

- `x.quest_is_complete(drama_id: str, out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
  - `state.quest.is_complete.<drama_id>`
- `x.current_quest_is_complete(out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
  - `state.quest.is_complete`
- `x.erenos_is_borrowed(out_flag: Id, actor: Chara | str = "pc") -> StepSpec`
  - `state.erenos.is_borrowed`

Ars専用コマンド:

- `x.erenos_ensure_near_player(actor: Chara | str | None = None) -> StepSpec`
  - `cmd.erenos.ensure_near_player`
- `x.erenos_borrow(actor: Chara | str | None = None) -> StepSpec`
  - `cmd.erenos.borrow`
- `x.hecatia_party_show(actor: Chara | str | None = None) -> StepSpec`
  - `cmd.hecatia.party_show`
- `x.hecatia_set_party_portrait(actor: Chara | str | None = None) -> StepSpec`
  - `cmd.hecatia.set_party_portrait`

Ars専用キュー:

- `x.apotheosis_silence(actor: Chara | str | None = None) -> StepSpec`
  - `cue.apotheosis.silence`
- `x.apotheosis_darkwomb(actor: Chara | str | None = None) -> StepSpec`
  - `cue.apotheosis.darkwomb`
- `x.apotheosis_curse_burst(actor: Chara | str | None = None) -> StepSpec`
  - `cue.apotheosis.curse_burst`
- `x.apotheosis_revive(actor: Chara | str | None = None) -> StepSpec`
  - `cue.apotheosis.revive`
- `x.apotheosis_mutation(actor: Chara | str | None = None) -> StepSpec`
  - `cue.apotheosis.mutation`
- `x.apotheosis_teleport_rebirth(actor: Chara | str | None = None) -> StepSpec`
  - `cue.apotheosis.teleport_rebirth`

除外（純CWLへ寄せる）:

- `cmd.scene.stop_bgm`
  - これは `ModCommands` には置かず、純CWL側の `stopBGM` action（`d.transition(stop_bgm=True)` または `d.action("stopBGM")`）を使う。

### 4.7.4 Compile Rule

- `ModCommands` / `ArsModCommands` の `resolve_*` 系 StepSpec は `eval` へ展開する。
- `ArenaModCommands` は `modInvoke` action に展開する。
- `resolve_flags_all` / `resolve_flags_any` は `dialogFlags` を参照するインライン評価で展開する。

### 4.7.5 Source of Truth

- 共通キー解決（`state.quest.can_start.*`, `state.quest.is_done.*`, `cmd.quest.*`, `cue.*`, `fx.pc.*`）  
  - `Elin_QuestMod/src/Drama/QuestDramaResolver.cs`
- Ars 固有キー（`state.quest.is_complete`, `state.erenos.*`, `cmd.erenos.*`, `cmd.hecatia.*`, `cue.apotheosis.*`）  
  - `Elin_ArsMoriendi/src/Drama/ArsDramaResolver.cs`
  - `Elin_ArsMoriendi/tools/drama/schema/key_spec.py`
- Arena コマンド（26件）  
  - `Elin_SukutsuArena/src/Commands/ArenaCommandRegistry.cs`

## 5. Resolution / Validation

## 5.1 Two-pass target resolution

1. 収集パス: `d.node()` で定義されたノード名を収集  
2. 解決パス: `go/option/when/cancel` の `str` を `NodeRef` に解決

解決不可の名前は compile エラーにする。

## 5.2 Required checks

- `start` が未定義ならエラー
- 同名ノードの重複定義はエラー
- `dialog(choices=[])` はエラー
- `strict=True` では ID kind ミスマッチ・不正 expr をエラー

## 5.3 Auto text ID

- `line/say/dialog(prompt)` の text_id は自動生成
- 入力が同じなら同じIDを再現（差分安定）

## 6. Example Skeleton

```python
from drama_dsl_v5_1 import DramaDsl, compile_xlsx, save_xlsx

d = DramaDsl(mod_name="QuestMod")
pc = d.chara("pc")

d.node(
    "main",
    d.dialog(
        prompt=d.line("行動を選択してください。", actor=pc),
        choices=[d.option("終了", to="ending")],
        cancel="ending",
    ),
)
d.node("ending", d.end())

spec = d.story(start="main")
wb = compile_xlsx(spec, strict=True)
save_xlsx(wb, "quest_drama_v5_1.xlsx")
```
