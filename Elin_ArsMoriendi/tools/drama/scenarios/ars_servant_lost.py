# -*- coding: utf-8 -*-
"""Drama A-3: ars_servant_lost (Servant release or SoulBind sacrifice)

Branch: if SoulBind sacrifice flag is set, use sl_bind_1/sl_bind_2 instead of sl_1/sl_2.
"""

from drama.drama_builder import DramaBuilder
from drama.data import Actors, BGM

SOULBIND_FLAG = "chitsii.ars.drama.soulbind_sacrifice"


def define_servant_lost(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    soulbind_start = builder.label("soulbind_start")
    common = builder.label("common")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_servant_lost"

    # ── main: check for soulbind sacrifice ──
    builder.step(main)
    builder.quest_check("ars_servant_lost", can_start_tmp_flag)
    builder.fade_out(0.3)
    builder.fade_in(0.3)
    builder.play_bgm_with_fallback(BGM.SORROW, BGM.REQUIEM)
    builder.wait(0.3)

    # Branch: SoulBind sacrifice → soulbind_start, else → release path
    builder.branch_if(SOULBIND_FLAG, "==", 1, soulbind_start)

    # Release path (default)
    builder.conversation([
        ("sl_1", "従者の名が禁書から消えていく。インクが乾くように、ゆっくりと。",
                 "The servant's name fades from the tome. Slowly, like ink drying.",
                 "仆从的名字从禁书上消退。如墨迹干涸般，缓缓地。"),
        ("sl_2", "魂が器から離れる。目から光が消え、身体が崩れ落ちる。もう...ただの死体だ。",
                 "The soul leaves the vessel. Light fades from the eyes, the body crumbles. Now -- just a corpse.",
                 "灵魂脱离容器。双目失去光泽，躯体轰然倒塌。如今……不过是具尸体。"),
    ], actor=narrator)
    builder.jump(common)

    # ── soulbind_start: SoulBind sacrifice ──
    builder.step(soulbind_start)
    builder.conversation([
        ("sl_bind_1", "身代わりとなった従者の名が禁書から消える。魂が燃え尽きるように、一瞬で。",
                      "The name of the servant who took your place vanishes from the tome. The soul burns out in an instant.",
                      "替身仆从的名字从禁书上消失。灵魂如燃尽般，转瞬即逝。"),
        ("sl_bind_2", "従者は最後まで命令に従った。……感謝すべきだろうか。それとも...いや。考えるな。",
                      "The servant obeyed to the very end. ...Should you be grateful? Or -- no. Don't think about it.",
                      "仆从至死服从了命令。……应该感激吗？还是……不。别想了。"),
    ], actor=narrator)
    builder.jump(common)

    # ── common: shared reflection ──
    builder.step(common)
    builder.wait(0.5)
    builder.conversation([
        ("sl_3", "……名前があったことすら、やがて忘れるのだろう。\n"
                 "禁書は覚えているかもしれない。だが、教えてはくれない。",
                 "...Eventually, you'll forget there was even a name. \n"
                 "The tome may remember. But it won't tell you.",
                 "……终有一天，连曾有名字这件事都会忘却。\n"
                 "禁书或许还记得。但它不会告诉你。"),
        ("sl_4", "エレノスは従者を何人失っただろう。カレンの手帳には「少なくとも十二人」と書かれていた。",
                 "How many servants did Erenos lose? Karen's journal says 'at least twelve.'",
                 "艾雷诺斯失去过多少仆从？卡伦的手账上写着「至少十二人」。"),
        ("sl_5", "十二人。それぞれに名前があった。それぞれに...生前があった。",
                 "Twelve. Each had a name. Each had -- a life before.",
                 "十二人。每一个都有名字。每一个都有……生前。"),
        ("sl_6", "……道具か、仲間か。その問いに答えを出す必要はない。結果だけが残る。\n"
                 "...エレノスの口癖だ。いつの間にか、自分の口癖になっていた。",
                 "...Tool or companion. You need not answer that question. Only results remain. \n"
                 "-- Erenos's favorite phrase. Somehow, it became yours.",
                 "……道具，还是同伴？无须回答这个问题。只有结果留存。\n"
                 "……这是艾雷诺斯的口头禅。不知何时，也成了你的口头禅。"),
    ], actor=narrator)
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.5)
