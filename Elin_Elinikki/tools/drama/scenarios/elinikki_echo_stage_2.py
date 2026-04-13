# -*- coding: utf-8 -*-
"""
Chapter-2 echo experiment stage 2: first handclap test (地点A).

Chapter 2 section 3 (story/chapters/chapter-02.md). Triggered at echo
point A. Sora claps once, listens, claps again — and the interval is
different. Mina calls the obvious explanation; Sora rejects it. This is
the first crack in the ordinary-cave framing.

Sets chitsii.elinikki.quest.state.echo_experiment = 2 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder
from tools.drama.scenarios.elinikki_echo_stage_1 import (
    AUDIO_SE_CLAP,
    AUDIO_SE_CLAP_RETURN,
)


def define_elinikki_echo_stage_2(builder: DramaBuilder) -> None:
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
                "echo2_sora_first_clap",
                "ソラ:「……手を叩く。聞いててくれ」",
                "Sora: '...Clapping. Listen.'",
                "",
                sora,
            ),
        ]
    )
    # Sora's first clap + the 3-second echo return.
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP_RETURN)
    builder.conversation(
        [
            (
                "echo2_sora_result_a",
                "ソラ:「三秒。返ってきた」",
                "Sora: 'Three seconds to come back.'",
                "",
                sora,
            ),
        ]
    )
    # Second clap + the mismatched 2-second return.
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP_RETURN)
    builder.conversation(
        [
            (
                "echo2_sora_result_b",
                "ソラ:「もう一度……今度は二秒だ」",
                "Sora: 'Again... two seconds this time.'",
                "",
                sora,
            ),
            (
                "echo2_sora_logic",
                "ソラ:「間隔が変わった。反射なら距離で決まるから、同じタイミングのはずだ」",
                "Sora: 'The interval changed. If it's reflection, distance decides—and distance doesn't change.'",
                "",
                sora,
            ),
            (
                "echo2_mina_moved",
                "ミナ:「お前が動いたんだろ」",
                "Mina: 'You moved.'",
                "",
                mina,
            ),
            (
                "echo2_sora_didnt",
                "ソラ:「動いてない！ ……もう一回やる」",
                "Sora: 'I did not! ...I'm running it again.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_ECHO_EXPERIMENT, 2)
    builder.drama_end(0.3)
