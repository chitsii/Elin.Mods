# Drama DSL API Design V5 (Pure Spec + Compiler Split)

Updated: 2026-03-02

この設計は、宣言と副作用を明確に分離する。

- 宣言層: すべて pure（値を返すだけ）
- 生成層: compile/save のみ impure（副作用あり）

## Source of Truth

- CWL source: `C:\Users\tishi\programming\elin_modding\Elin.Plugins\CustomWhateverLoader\API\Drama\*`
- CWL docs: `C:\Users\tishi\programming\elin_modding\Elin.Docs\articles\100_Mod Documentation\Custom Whatever Loader\JP\*`
- Game native drama: `C:\Users\tishi\programming\elin_modding\Elin-Decompiled\Elin\DramaManager.cs`

## 1. North Star

- 読みやすい: 物語フローがコード順に読める
- 壊れにくい: 未定義遷移や typo を compile 前に落とせる
- 運用しやすい: strict のオンオフを CI とローカルで切り替えられる

## 2. Architecture

## 2.1 Spec Layer (pure)

宣言層は一切状態を持たない。すべて `Spec` を返す。

- Graph: `node`, `ref`, `story`
- Entity: `chara`, `id`
- Dialogue/Flow: `line`, `say`, `dialog`, `option`, `when`, `go`, `end`
- Scene: `transition`, `wait`, `sound`, `effect`, `shake`, `glitch`
- Staging: `spawn`, `move_to`, `move_tile`, `move_next_to`, `emote`, `set_portrait`, `set_sprite`
- Quest: `quest_begin`, `quest_update`, `quest_finish`
- Condition: `has_flag`, `has_item`, `has_condition`, `has_feat`, `has_keyitem`, `in_zone`, `native_if`, `all_of`, `any_of`, `not_`

## 2.2 Compile Layer (impure)

- `compile_xlsx(spec: StorySpec, strict: bool = True) -> Workbook`
- `save_xlsx(workbook: Workbook, path: str, sheet: str = "main") -> None`

`save` は compile 結果だけを受け取り、DSLオブジェクトを直接保存しない。

## 3. Type Model

- `Id(kind, value)`
- `Chara(alias)`
- `NodeRef(name)`
- `Line(jp, en, cn, actor)`  # text_id はコンパイラが自動生成
- `Cond(op, args...)`
- `OptionSpec(label, to, cond?)`
- `StepSpec(kind, payload...)`
- `NodeSpec(ref, steps)`
- `NodeTarget = NodeRef | NodeSpec`
- `StorySpec(start, nodes, meta)`

## 4. Public API (V5)

## 4.1 Graph

- `node(name: str, *steps: StepSpec) -> NodeSpec`
- `ref(name: str) -> NodeRef`  # 前方参照が必要な場合のみ使用
- `story(start: NodeTarget, nodes: list[NodeSpec], meta: dict | None = None) -> StorySpec`

## 4.2 Entities / IDs

- `chara(alias: str) -> Chara`
- `id(kind: str, value: str) -> Id`

## 4.3 Dialogue / Branch / Flow

- `line(jp: str, actor: Chara | str | None = None, en: str = "", cn: str = "") -> Line`
- `say(jp: str, actor: Chara | str, en: str = "", cn: str = "") -> StepSpec`
- `dialog(lines: list[Line | tuple] | None = None, prompt: Line | None = None, choices: list[OptionSpec] | None = None, cancel: NodeRef | None = None) -> StepSpec`
  - 会話のみ、会話+選択肢、選択肢のみを1APIで表現する。
- `option(label: str, to: NodeTarget, cond: Cond | None = None) -> OptionSpec`
- `when(cond: Cond, then_to: NodeTarget, else_to: NodeTarget | None = None) -> StepSpec`
- `go(to: NodeTarget) -> StepSpec`
- `end() -> StepSpec`

## 4.4 Scene / Effect

- `transition(bg: Id | None = None, bgm: Id | None = None, fade: float = 0.5, clear_bg: bool = False, stop_bgm: bool = False) -> StepSpec`
- `wait(seconds: float) -> StepSpec`
- `sound(snd: Id) -> StepSpec`
- `effect(fx: Id, actor: Chara | str | None = None) -> StepSpec`
- `shake() -> StepSpec`
- `glitch() -> StepSpec`

## 4.5 Staging

- `spawn(actor: Chara | str, chara_id: Id, level: int | None = None) -> StepSpec`
- `move_to(actor: Chara | str, x: int, y: int) -> StepSpec`
- `move_tile(actor: Chara | str, dx: int, dy: int) -> StepSpec`
- `move_next_to(actor: Chara | str, target: Chara | str) -> StepSpec`
- `emote(actor: Chara | str, emote_id: Id, duration: float | None = None) -> StepSpec`
- `set_portrait(actor: Chara | str, portrait_id: Id | None) -> StepSpec`
- `set_sprite(actor: Chara | str, sprite_id: Id | None) -> StepSpec`

## 4.6 Quest

- `quest_begin(quest_id: Id, phase: int = 1, journal: bool = True) -> StepSpec`
  - `start + phase + journal` を1ステップ化。
- `quest_update(quest_id: Id, phase: int | None = None, journal: bool = False) -> StepSpec`
  - 進行中クエストの状態更新。
- `quest_finish(quest_id: Id, phase: int | None = 999, journal: bool = True) -> StepSpec`
  - `complete + (optional) phase + journal` を1ステップ化。

## 4.7 Conditions

- `has_flag(flag_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `has_item(item_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `has_condition(cond_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `has_feat(feat_id: Id, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `has_keyitem(key_id: Id, expr: str = ">0") -> Cond`
- `in_zone(zone_id: Id, level: int | None = None, actor: Chara | str = "pc") -> Cond`
- `native_if(raw_if_expr: str) -> Cond`
- `all_of(*conds: Cond) -> Cond`
- `any_of(*conds: Cond) -> Cond`
- `not_(cond: Cond) -> Cond`

## 5. Validation

## 5.1 Always

- `StorySpec.start` が nodes 内に存在しない場合はエラー
- `go/to/cancel` の遷移先未定義はエラー
- 同一 `NodeRef` の重複定義はエラー
- `dialog(choices=...)` で choices が空はエラー

## 5.2 strict=True

- `Id.kind` とAPI用途が一致しない場合はエラー
- `dialog(choices=...)` 使用時に `prompt` 未指定はエラー
- `say/dialog/line` 本文に `"<name>:"` 形式が含まれる場合はエラー
- 条件式 `expr` が不正ならエラー

## 5.3 Auto Text ID Generation

- `line/say/dialog(prompt)` のテキストIDはユーザーが指定しない。
- `compile_xlsx()` が短い安定ハッシュを自動生成する。
- 生成キーの推奨材料: `node_ref`, `step_index`, `line_index`, `actor_alias`, `jp`.
- 生成方式の推奨: `blake2s` などでハッシュし、先頭8〜10桁を使用。
- 同一入力に対して同一IDを再現し、出力差分が安定することを要件とする。

## 6. Migration from V4

- `d.start(node)` -> `story(start=node, nodes=[...])`
- `d.at(node, [...])` -> `node(node, ...steps...)`
- `d.save(path)` -> `save_xlsx(compile_xlsx(spec, strict=True), path)`
- `menu(prompt, options, cancel)` -> `dialog(prompt=prompt, choices=options, cancel=cancel)`
- `line(text_id, jp, ...)` -> `line(jp, ...)`
- `say(text_id, jp, ...)` -> `say(jp, ...)`
- `quest_start(q) + quest_phase(q, p) + quest_journal(q)` -> `quest_begin(q, phase=p, journal=True)`
- `quest_phase(q, p) + quest_journal(q)` -> `quest_update(q, phase=p, journal=True)`
- `quest_phase(q, 999) + quest_complete(q) + quest_journal(q)` -> `quest_finish(q, phase=999, journal=True)`

## 7. Example Skeleton

```python
ending = node("ending", end())
main = node(
    "main",
    transition(bg=id("bg", "BG/town"), bgm=id("bgm", "BGM/theme")),
    dialog(
        prompt=line("行動を選択", actor=chara("pc")),
        choices=[option("終了", to=ending)],
        cancel=ending,
    ),
)

spec = story(start=main, nodes=[main, ending])

wb = compile_xlsx(spec, strict=True)
save_xlsx(wb, "quest_drama_v5.xlsx")
```
