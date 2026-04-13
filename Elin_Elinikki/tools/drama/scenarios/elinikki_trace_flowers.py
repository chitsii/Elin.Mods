# -*- coding: utf-8 -*-
"""
Chapter-3 trace examine drama: flower roots (花の根元).

Chapter 3 section 5 (story/chapters/chapter-03.md). Hidden underneath
the "pretty" bloom layer, the roots turn out to be compost — kitchen
refuse: fruit peels, fish bones. Sora reframes even this as evidence of
creation-by-intelligence.

Sets chitsii.elinikki.quest.event.trace_flowers on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_flowers(builder: DramaBuilder) -> None:
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
                "flowers_sora_roots",
                "ソラ:「根元を見ろ。朽ちた有機物だ。果物の皮、魚の骨……これが苗床になってる」",
                "Sora: 'Look at the roots. Decayed matter. Fruit rind. Fish bones. That's the bed.'",
                "",
                sora,
            ),
            (
                "flowers_sora_nursery",
                "ソラ:「有機物を基質にしている。何かがここに有機物を集めて花を育てている」",
                "Sora: 'Using organics as the substrate. Something's gathering them to grow the flowers.'",
                "",
                sora,
            ),
            (
                "flowers_mina_trash",
                "ミナ:「ゴミに見えるが」",
                "Mina: 'Looks like garbage to me.'",
                "",
                mina,
            ),
            (
                "flowers_sora_create",
                "ソラ:「ゴミを使って生命を創造するなら、それこそ知性だろう」",
                "Sora: 'Making life out of garbage—that's intelligence if anything is.'",
                "",
                sora,
            ),
            (
                "flowers_mina_silent",
                "ミナ:「……」",
                "Mina: '...'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_FLOWERS, 1)
    builder.drama_end(0.3)
