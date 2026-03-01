# -*- coding: utf-8 -*-
"""Placeholder drama scenario for QuestMod template."""

from tools.drama.data import Actors
from tools.drama.drama_builder import DramaBuilder


def define_quest_drama_placeholder(builder: DramaBuilder) -> None:
    guide = builder.register_actor(Actors.GUIDE, "Guide", "Guide")
    main = builder.label("main")
    end = builder.label("end")

    builder.step(main)
    builder.say(
        "questmod_placeholder_1",
        "QuestMod placeholder drama. Replace this scenario with your own quest flow.",
        "QuestMod placeholder drama. Replace this scenario with your own quest flow.",
        actor=guide,
    )
    builder.choice(
        end,
        "Continue",
        "Continue",
        text_id="questmod_placeholder_choice_continue",
    )

    builder.step(end)
    builder.say(
        "questmod_placeholder_2",
        "Template check complete.",
        "Template check complete.",
        actor=guide,
    )
    builder.drama_end(0.3)
