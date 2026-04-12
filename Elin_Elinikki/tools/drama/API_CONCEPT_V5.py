from drama_dsl_v5 import (
    all_of,
    any_of,
    chara,
    compile_xlsx,
    dialog,
    effect,
    end,
    go,
    has_condition,
    has_feat,
    has_item,
    id,
    line,
    native_if,
    node,
    not_,
    option,
    quest_begin,
    quest_finish,
    quest_update,
    ref,
    save_xlsx,
    say,
    sound,
    spawn,
    story,
    transition,
    when,
)


class IDs:
    QUEST_SCARM = id("quest", "quest_ff4_scarmiglione_demo")

    ITEM_HOLY_WATER = id("item", "holy_water")
    FEAT_WHITE_MAGIC = id("feat", "white_magic")
    FEAT_UNDEAD_SLAYER = id("feat", "undead_slayer")
    COND_CURSE = id("condition", "curse")
    COND_FEAR = id("condition", "fear")

    BG_CAMP = id("bg", "BG/mt_ordeals_camp")
    BG_PEAK = id("bg", "BG/mt_ordeals_peak")
    BG_CLIFF = id("bg", "BG/mt_ordeals_cliff")

    BGM_OMINOUS = id("bgm", "BGM/ominous_01")
    BGM_BOSS = id("bgm", "BGM/boss_undead")

    FX_HEAL = id("effect", "heal")
    FX_HOLY = id("effect", "holy_burst")
    FX_SLASH = id("effect", "slash")
    FX_EXPLODE = id("effect", "explode")

    SND_OK = id("sound", "base.ok")
    SND_HIT = id("sound", "battle.hit_heavy")
    SND_HOLY = id("sound", "spell.holy")
    SND_BOSS_DOWN = id("sound", "battle.boss_down")
    SND_DEFEAT = id("sound", "battle.defeat")

    CHARA_SCARM = id("chara", "scarmiglione")
    CHARA_ZOMBIE = id("chara", "undead_soldier")


pc = chara("pc")
cecil = chara("cecil")
kain = chara("kain")
rosa = chara("rosa")
scarm = chara("scarmiglione")
zombie = chara("zombie_guard")

# Cond is defined once and reused in option/dialog/branch.
can_purify = any_of(
    has_item(IDs.ITEM_HOLY_WATER, expr=">=1", actor=pc),
    has_feat(IDs.FEAT_WHITE_MAGIC, expr=">=1", actor=rosa),
)

must_purify = any_of(
    native_if("survival"),
    has_condition(IDs.COND_CURSE, expr=">=1", actor=pc),
)

can_finish_cleanly = all_of(
    not_(has_condition(IDs.COND_FEAR, expr=">=1", actor=pc)),
    has_feat(IDs.FEAT_UNDEAD_SLAYER, expr=">=1", actor=cecil),
)

# First define terminal/simple nodes so their returned NodeSpec can be reused directly.
retreat = node(
    "retreat",
    say("いったん引いて立て直す。", actor=pc),
    quest_update(IDs.QUEST_SCARM, phase=10, journal=True),
    end(),
)

bad_end = node(
    "bad_end",
    sound(IDs.SND_DEFEAT),
    say("瘴気に押し切られた。", actor=rosa),
    quest_update(IDs.QUEST_SCARM, phase=20, journal=True),
    end(),
)

victory = node(
    "victory",
    effect(IDs.FX_EXPLODE, actor=scarm),
    sound(IDs.SND_BOSS_DOWN),
    dialog(
        [
            line("ばかな……この私が……。", actor=scarm),
            line("戦闘終了。先へ進もう。", actor=cecil),
        ]
    ),
    quest_finish(IDs.QUEST_SCARM, phase=999, journal=True),
    transition(clear_bg=True, stop_bgm=True, fade=0.6),
    end(),
)

# Uses one forward ref because battle_menu is defined later (cycle path).
final_check = node(
    "final_check",
    when(can_finish_cleanly, then_to=victory, else_to=ref("battle_menu")),
)

# Purify options can now target concrete NodeSpec values.
opt_purify_item = option(
    "聖水で浄化する",
    to=victory,
    cond=has_item(IDs.ITEM_HOLY_WATER, expr=">=1", actor=pc),
)
opt_purify_prayer = option(
    "祈りで浄化する",
    to=victory,
    cond=has_feat(IDs.FEAT_WHITE_MAGIC, expr=">=1", actor=rosa),
)
opt_force = option("強引に押し切る", to=bad_end)

purify_menu = node(
    "purify_menu",
    transition(bg=IDs.BG_CLIFF, fade=0.4),
    dialog(
        prompt=line("浄化方法を選択してください。", actor=rosa),
        choices=[opt_purify_item, opt_purify_prayer, opt_force],
        cancel=bad_end,
    ),
)

curse_gate = node(
    "curse_gate",
    when(must_purify, then_to=purify_menu, else_to=final_check),
)

front_attack = node(
    "front_attack",
    effect(IDs.FX_SLASH, actor=cecil),
    sound(IDs.SND_HIT),
    say("正面から押し込む。", actor=cecil),
    go(curse_gate),
)

holy_attack = node(
    "holy_attack",
    effect(IDs.FX_HOLY, actor=pc),
    sound(IDs.SND_HOLY),
    say("聖属性で瘴気を削る。", actor=pc),
    go(curse_gate),
)

# Uses one forward ref because battle_menu loops back from scan route.
scan_enemy = node(
    "scan_enemy",
    say("呪いの瘴気が強まっている。", actor=rosa),
    when(native_if("survival"), then_to=purify_menu, else_to=ref("battle_menu")),
)

opt_front = option("正面から攻める", to=front_attack)
opt_holy = option("聖属性で攻める", to=holy_attack, cond=can_purify)
opt_scan = option("敵の状態を確認する", to=scan_enemy)
opt_retreat = option("撤退する", to=retreat)

battle_menu = node(
    "battle_menu",
    dialog(
        prompt=line("戦術を選択してください。", actor=cecil),
        choices=[opt_front, opt_holy, opt_scan, opt_retreat],
        cancel=retreat,
    ),
)

battle_entry = node(
    "battle_entry",
    transition(bg=IDs.BG_PEAK, bgm=IDs.BGM_BOSS, fade=0.6),
    quest_update(IDs.QUEST_SCARM, phase=2, journal=True),
    spawn(scarm, chara_id=IDs.CHARA_SCARM),
    spawn(zombie, chara_id=IDs.CHARA_ZOMBIE),
    dialog(
        [
            line("よく来たな。ここがお前たちの墓場だ。", actor=scarm),
            line("来るぞ、構えろ。", actor=cecil),
        ]
    ),
    go(battle_menu),
)

# main is referenced by earlier nodes via forward ref (minimal, exception case).
prayer = node(
    "prayer",
    effect(IDs.FX_HEAL, actor=rosa),
    sound(IDs.SND_OK),
    say("浄化の祈りを捧げるわ。", actor=rosa),
    when(can_purify, then_to=battle_entry, else_to=ref("main_menu")),
)

briefing = node(
    "briefing",
    dialog(
        [
            line("敵は不死系だ。聖属性が有効。", actor=cecil),
            line("前衛で引きつける。隙を見て突く。", actor=kain),
            line("準備が整ったら進みましょう。", actor=rosa),
        ]
    ),
    go(ref("main_menu")),
)

opt_briefing = option("作戦を確認する", to=briefing)
opt_prayer = option("浄化の祈りを捧げる", to=prayer, cond=can_purify)
opt_enter = option("山頂へ進む", to=battle_entry)

main = node(
    "main_menu",
    transition(bg=IDs.BG_CAMP, bgm=IDs.BGM_OMINOUS, fade=0.6),
    quest_begin(IDs.QUEST_SCARM, phase=1, journal=True),
    dialog(
        [
            line("ここから先は強い瘴気を感じる。", actor=cecil),
            line("浄化手段を用意しておきましょう。", actor=rosa),
            line("進路は任せる。", actor=kain),
        ]
    ),
    dialog(
        prompt=line("行動を選択してください。", actor=pc),
        choices=[opt_briefing, opt_prayer, opt_enter, opt_retreat],
        cancel=retreat,
    ),
)

spec = story(
    start=main,
    meta={"mod_name": "QuestMod", "strict": True},
    nodes=[
        main,
        briefing,
        prayer,
        battle_entry,
        battle_menu,
        front_attack,
        holy_attack,
        scan_enemy,
        curse_gate,
        purify_menu,
        final_check,
        victory,
        retreat,
        bad_end,
    ],
)

wb = compile_xlsx(spec, strict=True)
save_xlsx(wb, "quest_drama_scarmiglione_v5.xlsx")
