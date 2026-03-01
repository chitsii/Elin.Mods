# -*- coding: utf-8 -*-
"""Drama 4: ars_karen_retreat (Stage 3: Karen retreats after battle)"""

from drama.drama_builder import DramaBuilder
from drama.data import Actors, BGM


def define_karen_retreat(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    karen = builder.register_actor(Actors.KAREN, "カレン", "Karen", name_cn="卡伦")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_karen_retreat"

    builder.step(main)
    builder.quest_check("ars_karen_retreat", can_start_tmp_flag)
    builder.play_bgm_with_fallback(BGM.SORROW, BGM.REQUIEM)
    builder.wait(0.5)
    builder.conversation([
        ("kr_1", "……ッ！",
                 "...Tch!",
                 "……啧！"),
        ("kr_2", "この力……\nあの書の力か。\nまだ半年も経っていないのに\nここまで……。",
                 "This power...\nthe tome's power?\nIt hasn't even been half a year,\nand already...",
                 "这股力量……\n是那本书的力量吗。\n还不到半年\n就已经……"),
        ("kr_3", "エレノスが同じ段階に\n達するまで三年かかった。\n\nあなたは……速すぎる。",
                 "It took Erenos three years\nto reach this stage.\n\nYou... are too fast.",
                 "艾雷诺斯花了三年\n才达到同样的阶段。\n\n你……太快了。"),
    ], actor=karen)
    builder.wait(0.3)
    builder.conversation([
        ("kr_4", "……一つだけ、渡しておくものがある。",
                 "...There is one thing I will leave you.",
                 "……有一样东西，要交给你。"),
        ("kr_5", "この手帳には、\n初代から五代目……\nエレノスまでの記録がある。\n\n私が個人的に調べたものよ。",
                 "This journal contains records\nfrom the first to the fifth...\nup to Erenos.\n\nMy own personal research.",
                 "这本手账记录了\n从初代到第五代……\n直到艾雷诺斯的资料。\n\n是我个人调查所得。"),
        ("kr_6", "騎士団の公式記録ではない。\n公式記録は「全員邪悪だった」\nと書いている。\n\nだが、それは嘘よ。",
                 "Not the order's official records.\nThe official records say\n\"all successors were evil.\"\n\nBut that is a lie.",
                 "这不是骑士团的官方记录。\n官方记录只写着\n「全员皆为邪恶之徒」。\n\n但那是谎言。"),
        ("kr_7", "全員、善意で始めた。\n全員、自分だけは違うと\n信じていた。\n\n……結末だけが、\n同じだった。",
                 "They all began with good intentions.\nThey all believed\nthey were different.\n\n...Only the ending\nwas the same.",
                 "所有人都始于善意。\n所有人都相信\n自己会不同。\n\n……唯有结局，\n如出一辙。"),
    ], actor=karen)
    builder.wait(0.5)
    builder.conversation([
        ("kr_8", "これを読みなさい。\n\nそれでもまだ続けるなら……\nあなたの選択よ。",
                 "Read this.\n\nIf you still continue\nafter that...\nit is your choice.",
                 "读一读吧。\n\n读完之后若仍要继续……\n那便是你的选择。"),
        ("kr_9", "だが次に会うときは\n容赦しない。\n……覚えておきなさい。",
                 "But when we next meet,\nI will show no mercy.\n...Remember that.",
                 "但下次再见面时\n我绝不留情。\n……记住。"),
        ("kr_10", "……一つだけ。\n禁書の声が聞こえるように\nなったら……\nそれは末期症状よ。",
                  "...One more thing.\nWhen you start hearing\nthe tome's voice...\nthat is a terminal symptom.",
                  "……还有一件事。\n当你开始听到\n禁书的声音时……\n那便是末期症状了。"),
    ], actor=karen)
    builder.wait(0.8)
    builder.action("drop", "ars_karen_journal")
    builder.conversation([
        ("kr_11", "カレンの姿が遠ざかっていく。\n騎士たちが続く。\n……禁書の震えが、止まった。",
                  "Karen's figure recedes.\nThe knights follow.\n...The tome's trembling stops.",
                  "卡伦的身影渐行渐远。\n骑士们随之而去。\n……禁书的颤抖，停止了。"),
    ], actor=narrator)
    builder.set_flag(can_start_tmp_flag, 0)
    builder.finish()
