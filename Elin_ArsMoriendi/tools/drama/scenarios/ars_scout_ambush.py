# -*- coding: utf-8 -*-
"""Drama: ars_scout_ambush (Stage 4: Scout re-encounter on zone transition)"""

from drama.data import BGM, Actors
from drama.drama_builder import DramaBuilder


def define_scout_ambush(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_scout_ambush"

    builder.step(main)
    builder.quest_check("ars_scout_ambush", can_start_tmp_flag)
    builder.play_bgm(BGM.BATTLE)
    builder.conversation(
        [
            (
                "sa_1",
                "……また、気配がする。\n\n騎士団の偵察兵が追跡を続けている。",
                "...That presence again.\n\nThe order's scouts continue their pursuit.",
                "……又是那股气息。\n\n骑士团的侦察兵仍在追踪。",
            ),
        ],
        actor=narrator,
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.finish()
