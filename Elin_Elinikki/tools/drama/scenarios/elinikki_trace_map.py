# -*- coding: utf-8 -*-
"""
Chapter-2 trace examine drama: floor map (床の地図).

Chapter 2 section 7 (story/chapters/chapter-02.md). Examined in the echo
layer after the echo experiment has concluded. Sora insists the same
"intelligence" drew both the wall symbols and this map — the reader will
later learn (chapter 4) the truth: Yuu drew them all.

Sets chitsii.elinikki.quest.event.trace_map on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_map(builder: DramaBuilder) -> None:
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
                "map_sora_not_map",
                "ソラ:「地図じゃない。これは通信図だ」",
                "Sora: 'This isn't a map. It's a signal diagram.'",
                "",
                sora,
            ),
            (
                "map_sora_same_system",
                "ソラ:「壁の記号と同じ体系で描かれてる」",
                "Sora: 'Drawn with the same system as the wall symbols.'",
                "",
                sora,
            ),
            (
                "map_mina_human",
                "ミナ:「これは人間が描いたんじゃないのか？ ユウの」",
                "Mina: 'Couldn't a person have drawn it? Yuu, maybe.'",
                "",
                mina,
            ),
            (
                "map_sora_brushwork",
                "ソラ:「いや、壁の記号と同じ筆致だ。同じ存在が描いてる」",
                "Sora: 'No. Same touch as the wall symbols. Same hand behind both.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_MAP, 1)
    builder.drama_end(0.3)
