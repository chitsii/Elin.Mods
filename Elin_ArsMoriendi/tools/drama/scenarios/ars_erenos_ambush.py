# -*- coding: utf-8 -*-
"""Drama: ars_erenos_ambush (Stage 7: Erenos re-encounter on zone transition)"""

from drama.data import BGM, Actors
from drama.drama_builder import DramaBuilder


def define_erenos_ambush(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_erenos_ambush"

    builder.step(main)
    builder.quest_check("ars_erenos_ambush", can_start_tmp_flag)
    builder.play_bgm(BGM.BATTLE)
    builder.conversation(
        [
            (
                "ea_1",
                "……影が、追ってきた。\n\nエレノスの残滓が再び形を成す。",
                "...The shadow has followed you.\n\nThe remnant of Erenos takes form once more.",
                "……影，追上来了。\n\n艾雷诺斯的残滓再度成形。",
            ),
        ],
        actor=narrator,
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.finish()
