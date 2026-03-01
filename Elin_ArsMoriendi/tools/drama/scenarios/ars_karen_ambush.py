# -*- coding: utf-8 -*-
"""Drama: ars_karen_ambush (Stage 3: Karen re-encounter on zone transition)"""

from drama.data import BGM, Actors
from drama.drama_builder import DramaBuilder


def define_karen_ambush(builder: DramaBuilder):
    karen = builder.register_actor(Actors.KAREN, "カレン", "Karen", name_cn="卡伦")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_karen_ambush"

    builder.step(main)
    builder.quest_check("ars_karen_ambush", can_start_tmp_flag)
    builder.play_bgm(BGM.BATTLE)
    builder.conversation(
        [
            ("ka_1", "……逃がさないわ、死霊術師。", "...You won't escape, necromancer.", "……休想逃走，死灵术士。"),
        ],
        actor=karen,
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.finish()
