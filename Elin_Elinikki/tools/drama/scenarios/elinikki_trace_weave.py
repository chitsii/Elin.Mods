# -*- coding: utf-8 -*-
"""
Chapter-3 trace examine drama: woven cord (編み物).

Chapter 3 section 6 (story/chapters/chapter-03.md). The final trace
before the reunion. Sora's theory completes here: every piece of
evidence fuses into one intelligent presence that uses hands. This is
the climax of Sora's "inferential high" before chapter 4 tears it down.

Sets chitsii.elinikki.quest.event.trace_weave on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_weave(builder: DramaBuilder) -> None:
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "weave_sora_tool",
                "ソラ:「道具だ。紐を編む知性。この空間の主は、手を使える存在だ」",
                "Sora: 'A tool. A mind that weaves cord. Whatever rules this place has hands.'",
                "",
                sora,
            ),
            (
                "weave_sora_full_picture",
                "ソラ:「壁に記号を刻み、水路を引き、応答を返し、花を育て、紐を編む」",
                "Sora: 'Carves symbols. Channels water. Answers signals. Grows flowers. Weaves cord.'",
                "",
                sora,
            ),
            (
                "weave_sora_exists",
                "ソラ:「これだけのことをする存在が、ここにいる」",
                "Sora: 'Something that does all of that—is right here with us.'",
                "",
                sora,
            ),
            (
                "weave_mina_afraid",
                "ミナ:「……お前は、それが怖くないのか」",
                "Mina: '...Aren't you afraid of it?'",
                "",
                mina,
            ),
            (
                "weave_sora_meet",
                "ソラ:「怖い。でも、会いたい」",
                "Sora: 'I am. But I want to meet it.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_WEAVE, 1)
    builder.drama_end(0.3)
