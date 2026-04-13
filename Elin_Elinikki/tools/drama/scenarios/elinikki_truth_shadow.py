# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: wall shadow.

Chapter 4 "真相会話: 壁の人影" (story/chapters/chapter-04.md).
The "ether dissolution" silhouette is soot from Yuu's cooking fire —
his own shadow, which he also mistook for something else the first
time he saw it.

Sets chitsii.elinikki.quest.event.truth_shadow = 1 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_shadow(builder: DramaBuilder) -> None:
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "truth_shadow_yuu_fire",
                "ユウ:「焚き火。ずっと同じ場所で焚いてたら煤で焼きついた。ちょっと面白いよな」",
                "Yuu: 'Campfire. Same spot every day. Soot burned my outline into the wall.'",
                "",
                yuu,
            ),
            (
                "truth_shadow_yuu_mine",
                "ユウ:「自分の影だよ、あれ。最初見たとき自分でもびっくりした」",
                "Yuu: 'It's my own shadow. Spooked myself the first time I noticed.'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_SHADOW, 1)
    builder.drama_end(0.3)
