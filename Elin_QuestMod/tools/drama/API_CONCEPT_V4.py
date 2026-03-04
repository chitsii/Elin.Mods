# API Concept V4
# North-star driven sample:
# - strict mode
# - ID catalog usage
# - reusable Cond values
# - menu prompt as Line (not raw string)
# - safe shortcuts: say()/dialog()

d = DramaDsl(mod_name="QuestMod", strict=True)


class IDs:
    # quest
    QUEST_SCARM = Id("quest", "quest_ff4_scarmiglione_demo")

    # items / feats / conditions
    ITEM_HOLY_WATER = Id("item", "holy_water")
    FEAT_WHITE_MAGIC = Id("feat", "white_magic")
    FEAT_UNDEAD_SLAYER = Id("feat", "undead_slayer")
    COND_CURSE = Id("condition", "curse")
    COND_FEAR = Id("condition", "fear")

    # world / scene
    BG_CAMP = Id("bg", "BG/mt_ordeals_camp")
    BG_PEAK = Id("bg", "BG/mt_ordeals_peak")
    BG_CLIFF = Id("bg", "BG/mt_ordeals_cliff")
    BGM_OMINOUS = Id("bgm", "BGM/ominous_01")
    BGM_BOSS = Id("bgm", "BGM/boss_undead")

    # fx / sound
    FX_HEAL = Id("effect", "heal")
    FX_HOLY = Id("effect", "holy_burst")
    FX_SLASH = Id("effect", "slash")
    FX_EXPLODE = Id("effect", "explode")
    SND_OK = Id("sound", "base.ok")
    SND_HIT = Id("sound", "battle.hit_heavy")
    SND_HOLY = Id("sound", "spell.holy")
    SND_BOSS_DOWN = Id("sound", "battle.boss_down")
    SND_DEFEAT = Id("sound", "battle.defeat")

    # chara ids
    CHARA_SCARM = Id("chara", "scarmiglione")
    CHARA_ZOMBIE = Id("chara", "undead_soldier")


# ===== actors =====
pc = d.chara("pc")
cecil = d.chara("cecil")
kain = d.chara("kain")
rosa = d.chara("rosa")
scarm = d.chara("scarmiglione")
zombie = d.chara("zombie_guard")


# ===== nodes =====
main = d.point("main_menu")
briefing = d.point("briefing")
prayer = d.point("prayer")
battle_entry = d.point("battle_entry")
battle_menu = d.point("battle_menu")
front_attack = d.point("front_attack")
holy_attack = d.point("holy_attack")
scan_enemy = d.point("scan_enemy")
curse_gate = d.point("curse_gate")
purify_menu = d.point("purify_menu")
final_check = d.point("final_check")
victory = d.point("victory")
retreat = d.point("retreat")
bad_end = d.point("bad_end")

d.start(main)


# ===== shared conditions (single source of truth) =====
can_purify = d.any_of(
    d.has_item(IDs.ITEM_HOLY_WATER, expr=">=1", actor=pc),
    d.has_feat(IDs.FEAT_WHITE_MAGIC, expr=">=1", actor=rosa),
)

must_purify = d.any_of(
    d.native_if("survival"),
    d.has_condition(IDs.COND_CURSE, expr=">=1", actor=pc),
)

can_finish_cleanly = d.all_of(
    d.not_(d.has_condition(IDs.COND_FEAR, expr=">=1", actor=pc)),
    d.has_feat(IDs.FEAT_UNDEAD_SLAYER, expr=">=1", actor=cecil),
)


# ===== reusable options =====
opt_briefing = d.option("作戦を確認する", to=briefing)
opt_prayer = d.option("浄化の祈りを捧げる", to=prayer, cond=can_purify)
opt_enter = d.option("山頂へ進む", to=battle_entry)
opt_retreat = d.option("撤退する", to=retreat)

opt_front = d.option("正面から攻める", to=front_attack)
opt_holy = d.option("聖属性で攻める", to=holy_attack, cond=can_purify)
opt_scan = d.option("敵の状態を確認する", to=scan_enemy)

opt_purify_item = d.option(
    "聖水で浄化する",
    to=victory,
    cond=d.has_item(IDs.ITEM_HOLY_WATER, expr=">=1", actor=pc),
)
opt_purify_prayer = d.option(
    "祈りで浄化する",
    to=victory,
    cond=d.has_feat(IDs.FEAT_WHITE_MAGIC, expr=">=1", actor=rosa),
)
opt_force = d.option("強引に押し切る", to=bad_end)


# ===== main =====
d.at(
    main,
    [
        d.transition(bg=IDs.BG_CAMP, bgm=IDs.BGM_OMINOUS, fade=0.6),
        d.quest_start(IDs.QUEST_SCARM),
        d.quest_phase(IDs.QUEST_SCARM, phase=1),
        d.quest_journal(IDs.QUEST_SCARM),
        d.dialog(
            [
                d.line("main_001", "ここから先は強い瘴気を感じる。", actor=cecil),
                d.line("main_002", "浄化手段を用意しておきましょう。", actor=rosa),
                d.line("main_003", "進路は任せる。", actor=kain),
            ]
        ),
        d.menu(
            prompt=d.line("menu_prompt_001", "行動を選択してください。", actor=pc),
            options=[opt_briefing, opt_prayer, opt_enter, opt_retreat],
            cancel=retreat,
        ),
    ],
)


# ===== briefing -> loop =====
d.at(
    briefing,
    [
        d.dialog(
            [
                d.line("brief_001", "敵は不死系だ。聖属性が有効。", actor=cecil),
                d.line("brief_002", "前衛で引きつける。隙を見て突く。", actor=kain),
                d.line("brief_003", "準備が整ったら進みましょう。", actor=rosa),
            ]
        ),
        d.go(main),
    ],
)


# ===== prayer =====
d.at(
    prayer,
    [
        d.effect(IDs.FX_HEAL, actor=rosa),
        d.sound(IDs.SND_OK),
        d.say("pray_001", "浄化の祈りを捧げるわ。", actor=rosa),
        d.when(cond=can_purify, then_to=battle_entry, else_to=main),
    ],
)


# ===== battle entry =====
d.at(
    battle_entry,
    [
        d.transition(bg=IDs.BG_PEAK, bgm=IDs.BGM_BOSS, fade=0.6),
        d.quest_phase(IDs.QUEST_SCARM, phase=2),
        d.quest_journal(IDs.QUEST_SCARM),
        d.spawn(scarm, chara_id=IDs.CHARA_SCARM),
        d.set_portrait(scarm, portrait=Id("portrait", "portrait/scarmiglione_phase1")),
        d.spawn(zombie, chara_id=IDs.CHARA_ZOMBIE),
        d.dialog(
            [
                d.line("entry_001", "よく来たな。ここがお前たちの墓場だ。", actor=scarm),
                d.line("entry_002", "来るぞ、構えろ。", actor=cecil),
            ]
        ),
        d.go(battle_menu),
    ],
)


# ===== battle menu (loop point) =====
d.at(
    battle_menu,
    [
        d.menu(
            prompt=d.line("menu_prompt_002", "戦術を選択してください。", actor=cecil),
            options=[opt_front, opt_holy, opt_scan, opt_retreat],
            cancel=retreat,
        ),
    ],
)


# ===== routes =====
d.at(
    front_attack,
    [
        d.effect(IDs.FX_SLASH, actor=cecil),
        d.sound(IDs.SND_HIT),
        d.say("front_001", "正面から押し込む。", actor=cecil),
        d.go(curse_gate),
    ],
)

d.at(
    holy_attack,
    [
        d.effect(IDs.FX_HOLY, actor=pc),
        d.sound(IDs.SND_HOLY),
        d.say("holy_001", "聖属性で瘴気を削る。", actor=pc),
        d.go(curse_gate),
    ],
)

d.at(
    scan_enemy,
    [
        d.say("scan_001", "呪いの瘴気が強まっている。", actor=rosa),
        d.when(d.native_if("survival"), then_to=purify_menu, else_to=battle_menu),
    ],
)


# ===== gate =====
d.at(
    curse_gate,
    [
        d.when(cond=must_purify, then_to=purify_menu, else_to=final_check),
    ],
)


# ===== final check =====
d.at(
    final_check,
    [
        d.when(cond=can_finish_cleanly, then_to=victory, else_to=battle_menu),
    ],
)


# ===== purify =====
d.at(
    purify_menu,
    [
        d.transition(bg=IDs.BG_CLIFF, fade=0.4),
        d.menu(
            prompt=d.line("menu_prompt_003", "浄化方法を選択してください。", actor=rosa),
            options=[opt_purify_item, opt_purify_prayer, opt_force],
            cancel=bad_end,
        ),
    ],
)


# ===== endings =====
d.at(
    victory,
    [
        d.effect(IDs.FX_EXPLODE, actor=scarm),
        d.sound(IDs.SND_BOSS_DOWN),
        d.dialog(
            [
                d.line("victory_001", "ばかな……この私が……。", actor=scarm),
                d.line("victory_002", "戦闘終了。先へ進もう。", actor=cecil),
            ]
        ),
        d.quest_phase(IDs.QUEST_SCARM, phase=999),
        d.quest_complete(IDs.QUEST_SCARM),
        d.quest_journal(IDs.QUEST_SCARM),
        d.transition(clear_bg=True, stop_bgm=True, fade=0.6),
        d.end(),
    ],
)

d.at(
    retreat,
    [
        d.say("retreat_001", "いったん引いて立て直す。", actor=pc),
        d.quest_phase(IDs.QUEST_SCARM, phase=10),
        d.quest_journal(IDs.QUEST_SCARM),
        d.end(),
    ],
)

d.at(
    bad_end,
    [
        d.sound(IDs.SND_DEFEAT),
        d.say("bad_001", "瘴気に押し切られた。", actor=rosa),
        d.quest_phase(IDs.QUEST_SCARM, phase=20),
        d.quest_journal(IDs.QUEST_SCARM),
        d.end(),
    ],
)

d.save("quest_drama_scarmiglione_v4.xlsx")
