# -*- coding: utf-8 -*-
"""
Chapter-1 trace examine drama: sorted stones (石の配置).

Chapter 1 section 6 (story/chapters/chapter-01.md). The third waterstone
trace. Mina breaks for the first time — "too tidy" — and Sora calls the
system of traces a single body of evidence. Sets
chitsii.elinikki.quest.event.trace_stones on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_stones(builder: DramaBuilder) -> None:
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
                "stones_sora_sorted",
                "ソラ:「やっぱりだ。大きさと色で分類されてる」",
                "Sora: 'Knew it. Sorted by size and color.'",
                "",
                sora,
            ),
            (
                "stones_sora_system",
                "ソラ:「壁の記号、水路、石の配置。三つ揃った。これは偶然じゃない。体系だ」",
                "Sora: 'Symbols. Channels. Sorted stones. Three in a row. This isn't coincidence—it's a system.'",
                "",
                sora,
            ),
            (
                "stones_mina_tidy",
                "ミナ:「……整理整頓にしては丁寧すぎないか？」",
                "Mina: '...Little too tidy for tidying up, isn't it.'",
                "",
                mina,
            ),
            (
                "stones_sora_gotcha",
                "ソラ:「ミナ、今お前も思っただろ」",
                "Sora: 'Mina. You felt it just now.'",
                "",
                sora,
            ),
            (
                "stones_mina_denial",
                "ミナ:「思ってない。先に行くぞ」",
                "Mina: 'I did not. Keep moving.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_STONES, 1)
    builder.drama_end(0.3)
