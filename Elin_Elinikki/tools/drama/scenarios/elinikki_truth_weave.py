# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: woven cord.

Chapter 4 "真相会話: 編み物" (story/chapters/chapter-04.md).
The "tool-using intelligence that weaves cord" was Yuu trying to make
a rope, getting halfway through, realizing he had no use for it, and
giving up out of boredom. The smallest reveal, the most pathetic.

Sets chitsii.elinikki.quest.event.truth_weave = 1 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_weave(builder: DramaBuilder) -> None:
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "truth_weave_yuu_rope",
                "ユウ:「ロープ作ろうと思ったんだけどさ。何に使うか思いつかなくてやめた」",
                "Yuu: 'I was going to make a rope. Then I couldn't think what for, so I stopped.'",
                "",
                yuu,
            ),
            (
                "truth_weave_yuu_bored",
                "ユウ:「暇だったんだよ。本当に暇だった」",
                "Yuu: 'I was bored. Really, really bored.'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_WEAVE, 1)
    builder.drama_end(0.3)
