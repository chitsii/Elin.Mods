# -*- coding: utf-8 -*-
"""Drama A-4: ars_dormant_flavor (3+ dormant servants flavor text)

Lightweight narration. No fade -- minimal disruption to gameplay.
"""

from drama.drama_builder import DramaBuilder
from drama.data import Actors, BGM


def define_dormant_flavor(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_dormant_flavor"

    builder.step(main)
    builder.quest_check("ars_dormant_flavor", can_start_tmp_flag)
    builder.wait(0.3)
    builder.play_bgm(BGM.REVELATION)
    builder.conversation([
        ("df_1", "動かぬ従者たちが並んでいる。整列し、沈黙し、命令を待っている。",
                 "The motionless servants stand in a row. Aligned, silent, awaiting orders.",
                 "不动的仆从们排成一列。整齐、沉默，等待命令。"),
        ("df_2", "コレクション。...その言葉がどこから来たのか。自分の語彙か、禁書の語彙か。",
                 "Collection. -- Where did that word come from? Your vocabulary? Or the tome's?",
                 "收藏品。……这个词从何而来？自己的用语？还是禁书的？"),
        ("df_3", "生前、彼らには名前があった。家族があった。夢があったかもしれない。今は...番号がある。",
                 "In life, they had names. Families. Perhaps dreams. Now -- they have numbers.",
                 "生前，他们有名字。有家人。或许还有梦想。如今……只剩编号。"),
        ("df_4", "禁書が静かに満足している。そう感じる。\n"
                 "禁書に感情があるのか、それとも自分が禁書に感情を投影しているのか。",
                 "The tome is quietly satisfied. You sense it. \n"
                 "Does the tome have emotions, or are you projecting your own onto it?",
                 "禁书在静静地满足着。你能感知到。\n"
                 "禁书有感情吗？还是你在将自己的感情投射于它？"),
        ("df_5", "……エレノスも同じ光景を見たのだろうか。同じことを考えたのだろうか。\n"
                 "同じ結論に達したのだろうか。...やめよう。",
                 "...Did Erenos see the same sight? Think the same thoughts? \n"
                 "Reach the same conclusion? -- Stop.",
                 "……艾雷诺斯也曾见过同样的光景吗？想过同样的事吗？\n"
                 "得出过同样的结论吗？……算了。"),
    ], actor=narrator)
    builder.set_flag(can_start_tmp_flag, 0)
    builder.finish()
