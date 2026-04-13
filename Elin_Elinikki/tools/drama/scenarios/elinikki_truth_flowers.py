# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: flower field.

Chapter 4 "真相会話: 花畑" (story/chapters/chapter-04.md).
The "intelligence creating life from garbage" was Yuu's dump. He
threw out fruit pits and vegetable scraps; something sprouted. This
is the quietest and most vicious of the reveals — Mina is still
thinking about her own "綺麗だな" from chapter 3.

Sets chitsii.elinikki.quest.event.truth_flowers = 1 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_flowers(builder: DramaBuilder) -> None:
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "truth_flowers_yuu_trash",
                "ユウ:「ゴミ捨て場。干し果物の種とか野菜くずとか捨ててたら、なんか生えてきた」",
                "Yuu: 'Trash pile. Dried fruit seeds, vegetable scraps. Something sprouted.'",
                "",
                yuu,
            ),
            (
                "truth_flowers_yuu_bad",
                "ユウ:「食えるかと思って試したけど不味かった」",
                "Yuu: 'Tried eating some. Tasted terrible.'",
                "",
                yuu,
            ),
            (
                "truth_flowers_sora_creation",
                "ソラ:「生命の創造だと思った……」",
                "Sora: 'I thought it was... the creation of life.'",
                "",
                sora,
            ),
            (
                "truth_flowers_yuu_garbage",
                "ユウ:「え、あれ創造？ ゴミだよ」",
                "Yuu: 'Creation? It's garbage.'",
                "",
                yuu,
            ),
            (
                "truth_flowers_mina_silent",
                "ミナ:「……」",
                "Mina: '...'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_FLOWERS, 1)
    builder.drama_end(0.3)
