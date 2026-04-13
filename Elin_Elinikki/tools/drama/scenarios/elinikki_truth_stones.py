# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: sorted stones.

Chapter 4 "真相会話: 石の配置" (story/chapters/chapter-04.md).
Yuu sorted stones by category so he could find things. Mina
immediately accepts it — she's the same kind of person — while Sora
loses the "coordinate system" interpretation.

Sets chitsii.elinikki.quest.event.truth_stones = 1 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_stones(builder: DramaBuilder) -> None:
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
                "truth_stones_yuu_organize",
                "ユウ:「食い物と水と道具を分けて置いてた。散らかすと何がどこにあるか分からなくなるから」",
                "Yuu: 'Food, water, tools—separate piles. Otherwise I'd lose track.'",
                "",
                yuu,
            ),
            (
                "truth_stones_sora_coord",
                "ソラ:「座標系かと……」",
                "Sora: 'I thought it was a coordinate system...'",
                "",
                sora,
            ),
            (
                "truth_stones_yuu_tidy",
                "ユウ:「整理整頓だよ」",
                "Yuu: 'Tidying up.'",
                "",
                yuu,
            ),
            (
                "truth_stones_mina_same",
                "ミナ:「……それは分かる」",
                "Mina: '...That I understand.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_STONES, 1)
    builder.drama_end(0.3)
