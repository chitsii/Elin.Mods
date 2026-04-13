# -*- coding: utf-8 -*-
"""
Chapter-1 trace examine drama: water channel (水路の溝).

Chapter 1 section 5 (story/chapters/chapter-01.md). The second trace in
the waterstone layer. Sets chitsii.elinikki.quest.event.trace_channel on
completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_channel(builder: DramaBuilder) -> None:
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
                "channel_sora_guide",
                "ソラ:「水を誘導している。この角度、自然の浸食じゃない。直線すぎる」",
                "Sora: 'It's channeling water. This angle—it's not natural erosion. Too straight.'",
                "",
                sora,
            ),
            (
                "channel_mina_natural",
                "ミナ:「たまたま岩の目に沿って流れただけだ」",
                "Mina: 'It just happens to follow the grain of the rock.'",
                "",
                mina,
            ),
            (
                "channel_sora_pileup",
                "ソラ:「たまたまが多すぎるんだよ、ミナ。壁の記号、この水路。二つ揃って偶然か？」",
                "Sora: 'Too many coincidences, Mina. The wall symbols. Now this channel. Still chance?'",
                "",
                sora,
            ),
            (
                "channel_mina_silence",
                "ミナ:「……」",
                "Mina: '...'",
                "",
                mina,
            ),
            (
                "channel_sora_living",
                "ソラ:「何かがここで生活してる。水を引いて、壁に記録を残して」",
                "Sora: 'Something lives here. Drawing water. Marking the walls.'",
                "",
                sora,
            ),
            (
                "channel_mina_living",
                "ミナ:「生活って……」",
                "Mina: 'Lives, you say...'",
                "",
                mina,
            ),
            (
                "channel_sora_same",
                "ソラ:「俺たちと同じだ。生きてるんだ、ここで」",
                "Sora: 'Same as us. Alive. Down here.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_CHANNEL, 1)
    builder.drama_end(0.3)
