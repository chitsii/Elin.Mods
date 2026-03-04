# Drama DSL API Design V4 (North-Star Driven)

Updated: 2026-03-02

この設計は「指摘に対処する」だけでなく、長期運用できる DSL の最終像を定義する。

## Source of Truth

- CWL source: `C:\Users\tishi\programming\elin_modding\Elin.Plugins\CustomWhateverLoader\API\Drama\*`
- CWL docs: `C:\Users\tishi\programming\elin_modding\Elin.Docs\articles\100_Mod Documentation\Custom Whatever Loader\JP\*`
- Game native drama: `C:\Users\tishi\programming\elin_modding\Elin-Decompiled\Elin\DramaManager.cs`

## 1. North Star

- 可読性: シナリオ作者が「物語の流れ」をコード順に追える。
- 安全性: タイポ・未定義遷移・条件ズレをビルド前に検出できる。
- 実用性: 安全を維持したまま、日常記述は短く書ける。

## 2. Operation Modes

`DramaDsl(mod_name=..., strict=...)` の2モードを持つ。

- `strict=False` (authoring default)
  - 迅速な試作向け。
  - `str` 引数を許容する（警告は出す）。
- `strict=True` (CI/release)
  - 配布前検証向け。
  - 重要IDは `Id` 型必須。
  - 非推奨ショートハンドをエラー化。

## 3. Type Model (6 types)

- `NodeRef`: 遷移先参照
- `Chara`: PC/NPC 参照
- `Line`: 会話1行 (`text_id`, `jp`, `en`, `cn`, `actor`)
- `Cond`: 条件式
- `OptionSpec`: 選択肢 (`label`, `to`, `cond`)
- `Id`: 各種ID (`kind`, `value`)

## 4. Authoring Rules

- エントリは `d.start(node)` で必ず明示する。
- ノード本体は `d.at(node, [...])` で定義する。
- 選択肢は `d.option(...)` を値として作り `d.menu(...)` に渡す。
- 分岐条件は `Cond` を変数化して再利用する。
- 会話本文に話者名は書かない。話者は `actor` のみ。
- `menu` の prompt は `Line` を正規とする（`str` は緩和モードのみ許可）。

## 5. Public API

## 5.1 Graph / Lifecycle

- `d.chara(alias: str) -> Chara`
- `d.point(name: str) -> NodeRef`
- `d.at(node: NodeRef, steps: list[StepSpec]) -> DramaDsl`
- `d.start(node: NodeRef) -> DramaDsl`
- `d.save(path: str, sheet: str = "main") -> None`

## 5.2 Dialogue / Choice / Flow

Core:
- `d.line(text_id: str, jp: str, en: str = "", cn: str = "", actor: Chara | str | None = None) -> Line`
- `d.speak(*lines: Line) -> StepSpec`
- `d.option(label: str, to: NodeRef, cond: Cond | None = None) -> OptionSpec`
- `d.menu(prompt: Line | str, options: list[OptionSpec], cancel: NodeRef | None = None) -> StepSpec`
- `d.when(cond: Cond, then_to: NodeRef, else_to: NodeRef | None = None) -> StepSpec`
- `d.go(to: NodeRef) -> StepSpec`
- `d.end() -> StepSpec`

Ergonomics (safe shortcuts):
- `d.say(text_id: str, jp: str, actor: Chara | str, en: str = "", cn: str = "") -> StepSpec`
  - `d.speak(d.line(...))` の短縮。
- `d.dialog(lines: list[Line | tuple]) -> StepSpec`
  - 複数行会話の短縮。

## 5.3 Scene / Effects

- `d.transition(bg: Id | str | None = None, bgm: Id | str | None = None, fade: float = 0.5, clear_bg: bool = False, stop_bgm: bool = False) -> StepSpec`
- `d.wait(seconds: float) -> StepSpec`
- `d.sound(sound: Id | str) -> StepSpec`
- `d.effect(effect: Id | str, actor: Chara | str | None = None) -> StepSpec`
- `d.shake() -> StepSpec`
- `d.glitch() -> StepSpec`

## 5.4 Actor Staging

- `d.spawn(actor: Chara | str, chara_id: Id | str, level: int | None = None) -> StepSpec`
- `d.move_to(actor: Chara | str, x: int, y: int) -> StepSpec`
- `d.move_tile(actor: Chara | str, dx: int, dy: int) -> StepSpec`
- `d.move_next_to(actor: Chara | str, target: Chara | str) -> StepSpec`
- `d.emote(actor: Chara | str, emote: Id | str, duration: float | None = None) -> StepSpec`
- `d.set_portrait(actor: Chara | str, portrait: Id | str | None) -> StepSpec`
- `d.set_sprite(actor: Chara | str, sprite: Id | str | None) -> StepSpec`

## 5.5 Quest

- `d.quest_start(quest: Id | str) -> StepSpec`
- `d.quest_phase(quest: Id | str, phase: int) -> StepSpec`
- `d.quest_next(quest: Id | str) -> StepSpec`
- `d.quest_complete(quest: Id | str) -> StepSpec`
- `d.quest_journal(quest: Id | str | None = None) -> StepSpec`

## 5.6 Conditions

- `d.has_flag(flag_key: Id | str, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_item(item_id: Id | str, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_condition(alias: Id | str, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_feat(alias: Id | str, expr: str = ">=1", actor: Chara | str = "pc") -> Cond`
- `d.has_keyitem(alias: Id | str, expr: str = ">0") -> Cond`
- `d.in_zone(zone_id: Id | str, level: int | None = None, actor: Chara | str = "pc") -> Cond`
- `d.native_if(raw_if_expr: str) -> Cond`
- `d.all_of(*conds: Cond) -> Cond`
- `d.any_of(*conds: Cond) -> Cond`
- `d.not_(cond: Cond) -> Cond`

## 6. Catalog Pattern (Recommended)

ID直書きを減らすため、`ids.py` 的なカタログを推奨。

```python
class IDs:
    QUEST_SCARM = Id("quest", "quest_ff4_scarmiglione_demo")
    ITEM_HOLY_WATER = Id("item", "holy_water")
    FEAT_WHITE_MAGIC = Id("feat", "white_magic")
    BG_CAMP = Id("bg", "BG/mt_ordeals_camp")
    BGM_BOSS = Id("bgm", "BGM/boss_undead")
```

## 7. Removed / Not Exposed

- `choose(at, ...)`
- `direct(action, ...)`, `play(action, ...)`, `advance(action, ...)`
- 生API (`action`, `invoke_expansion`, `eval`, `cs_*`)
- QuestMod DLL依存 (`resolve_*`, `run_cue`, `quest_try_start*`, `quest_check`)

## 8. Validation Rules

Always:
- `d.start(...)` 未設定はエラー。
- `NodeRef` 未定義参照 (`go`, `option.to`, `menu.cancel`) はエラー。
- `d.at()` の同一 `NodeRef` 二重定義はエラー。
- `menu.options` 空はエラー。
- `Line.text_id` 重複はエラー。

Warnings (`strict=False`):
- `menu(prompt="...")` の `str` 使用。
- 重要IDの生文字列使用（`quest/item/bg/bgm/effect/sound`）。
- `Line.jp` が `"<name>:"` で始まる（話者重複）。

Errors (`strict=True`):
- 上記 warning 項目をエラー化。
- 条件式 (`expr`) 構文不正。
- `Id.kind` とAPI引数用途の不一致。
