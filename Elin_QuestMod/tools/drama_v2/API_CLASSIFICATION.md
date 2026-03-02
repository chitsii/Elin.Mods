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
