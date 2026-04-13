# -*- coding: utf-8 -*-
"""
Chapter-1 trace examine drama: wall markings (刻み目).

Triggered once in chapter 1 (水石の層) when the player first examines the
wall-markings trace object. Corresponds to story/chapters/chapter-01.md
section 3 "痕跡1: 壁の刻み目".

Sets chitsii.elinikki.quest.event.trace_marks = 1 on completion so chapter 4
can unlock the matching truth conversation with Yuu. Replay suppression is
handled by the C# examine trigger (Phase 3), not by this drama.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_marks(builder: DramaBuilder) -> None:
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
                "marks_sora_stop",
                "ソラ:「待って。これ……記号だ。間隔を見て。最初は等間隔だけど、途中から変わる」",
                "Sora: 'Wait. These... they're symbols. Look at the spacing. Even at first, but it changes partway.'",
                "",
                sora,
            ),
            (
                "marks_sora_series",
                "ソラ:「等比数列に近い。偶然こうはならない」",
                "Sora: 'It's close to a geometric progression. That doesn't happen by chance.'",
                "",
                sora,
            ),
            (
                "marks_mina_cavein",
                "ミナ:「落盤の亀裂だろ」",
                "Mina: 'A crack from a cave-in.'",
                "",
                mina,
            ),
            (
                "marks_sora_refute",
                "ソラ:「亀裂は等比数列にならない」",
                "Sora: 'Cracks don't form geometric progressions.'",
                "",
                sora,
            ),
            (
                "marks_sora_shavings",
                "ソラ:「……削りかすが落ちてる。新しい。最近削られたものだ」",
                "Sora: '...There's fresh shavings. New. Something carved this recently.'",
                "",
                sora,
            ),
            (
                "marks_sora_alive",
                "ソラ:「ここに何かが今もいる」",
                "Sora: 'Whatever made this is still here.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_MARKS, 1)
    builder.drama_end(0.3)
