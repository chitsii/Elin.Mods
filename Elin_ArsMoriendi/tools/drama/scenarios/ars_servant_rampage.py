# -*- coding: utf-8 -*-
"""Drama A-2: ars_servant_rampage (Servant rampage event with 4-way branch)

Branch is determined by C# flag set before drama invocation:
  chitsii.ars.drama.rampage_type = 1(dark), 2(berserk), 3(destruct), 4(mutate)
"""

from drama.drama_builder import DramaBuilder
from drama.data import Actors, BGM

RAMPAGE_TYPE_FLAG = "chitsii.ars.drama.rampage_type"


def define_servant_rampage(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    dark = builder.label("dark")
    berserk = builder.label("berserk")
    destruct = builder.label("destruct")
    mutate = builder.label("mutate")
    done = builder.label("done")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_servant_rampage"

    # ── main: common introduction ──
    builder.step(main)
    builder.quest_check("ars_servant_rampage", can_start_tmp_flag)
    builder.shake()
    builder.play_bgm(BGM.BATTLE)
    builder.conversation([
        ("sr_1", "従者の体が痙攣する。突然に。警告もなく。",
                 "The servant's body convulses. Suddenly. Without warning.",
                 "仆从的身体痉挛起来。毫无预兆。毫无征兆。"),
        ("sr_2", "魂が器を拒絶している...あるいは、器が魂を超えようとしている。\n"
                 "どちらにせよ、制御が外れた。",
                 "The soul rejects the vessel -- or the vessel tries to exceed the soul. \n"
                 "Either way, control is lost.",
                 "灵魂在排斥容器……或者说，容器试图超越灵魂。\n"
                 "无论如何，控制已经脱离。"),
        ("sr_3", "禁書が脈打っている。警告か。……いや、観察だ。禁書は警告しない。記録するだけだ。",
                 "The tome pulses. A warning? ...No, observation. The tome doesn't warn. It only records.",
                 "禁书在搏动。警告吗？……不，是观察。禁书不会警告。它只做记录。"),
        ("sr_4", "...結果が出る。",
                 "-- The result manifests.",
                 "……结果显现了。"),
    ], actor=narrator)

    # Branch based on rampage type flag
    builder.switch_on_flag(RAMPAGE_TYPE_FLAG, {
        1: dark,
        2: berserk,
        3: destruct,
        4: mutate,
    }, fallback=dark)

    # ── dark: DarkAwakening ──
    builder.step(dark)
    builder.conversation([
        ("sr_dark_1", "……闇の中から、別の何かが目覚めた。制御を超えた力が、従者に宿る。",
                      "...From the darkness, something else awakens. Power beyond control settles into the servant.",
                      "……从黑暗之中，另一种存在觉醒了。超越控制的力量寄宿于仆从体内。"),
        ("sr_dark_2", "従者の目に知性が灯った。だが、それは...以前の主の知性ではない。",
                      "Intelligence lights in the servant's eyes. But it is -- not the former master's intelligence.",
                      "仆从眼中点燃了智慧之光。然而那——并非旧主的智慧。"),
    ], actor=narrator)
    builder.jump(done)

    # ── berserk: Berserk ──
    builder.step(berserk)
    builder.conversation([
        ("sr_berserk_1", "理性が消えた。だが、肉体は覚えている。殺すための動きを。",
                         "Reason is gone. But the body remembers. How to kill.",
                         "理性消失了。然而身体还记得。杀戮的方式。"),
        ("sr_berserk_2", "……これが代償だ。強化には限界がある。限界を超えた先にあるのは、破壊だけだ。",
                         "...This is the price. Enhancement has limits. Beyond those limits, only destruction.",
                         "……这就是代价。强化有其极限。超越极限之后，只剩毁灭。"),
    ], actor=narrator)
    builder.jump(done)

    # ── destruct: SelfDestruct ──
    builder.step(destruct)
    builder.conversation([
        ("sr_destruct_1", "器が砕けた。魂が爆ぜ、周囲を巻き込む。",
                          "The vessel shatters. The soul detonates, engulfing the surroundings.",
                          "容器碎裂了。灵魂爆裂，波及周遭。"),
        ("sr_destruct_2", "禁書から、従者の名が消えていく。インクが乾くように。...痛みはない。禁書にとっては。",
                          "The servant's name fades from the tome. Like ink drying. -- There is no pain. Not for the tome.",
                          "禁书上，仆从的名字正在消退。如墨迹干涸。……没有痛感。对禁书而言。"),
    ], actor=narrator)
    builder.jump(done)

    # ── mutate: Mutation ──
    builder.step(mutate)
    builder.conversation([
        ("sr_mutate_1", "骨が伸びる。肉が裂ける。新しい部位が這い出す。",
                        "Bones stretch. Flesh tears. New appendages crawl forth.",
                        "骨骼伸长。血肉撕裂。新的肢体蠕动而出。"),
        ("sr_mutate_2", "……進化か、崩壊か。禁書はこれを「適応」と記録している。\n"
                        "人間なら「怪物」と呼ぶだろう。",
                        '...Evolution or collapse. The tome records this as "adaptation." '
                        'A human would call it "monster."',
                        "……进化，还是崩溃？禁书将此记录为「适应」。\n"
                        "人类大概会称之为「怪物」吧。"),
    ], actor=narrator)
    builder.jump(done)

    # ── done ──
    builder.step(done)
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.3)
