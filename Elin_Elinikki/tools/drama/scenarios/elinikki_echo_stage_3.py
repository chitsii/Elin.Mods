# -*- coding: utf-8 -*-
"""
Chapter-2 echo experiment stage 3: count verification (地点B).

Chapter 2 section 4 (story/chapters/chapter-02.md). Triggered at echo
point B. Sora relocates and re-runs the test with three claps. Three
claps come back. Mina starts to lose the fallback "it's just reflection"
explanation — the story beat is Mina tapping out ("...好きにしろ").

Sets chitsii.elinikki.quest.state.echo_experiment = 3 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder
from tools.drama.scenarios.elinikki_echo_stage_1 import (
    AUDIO_SE_CLAP,
    AUDIO_SE_CLAP_RETURN,
)


def define_elinikki_echo_stage_3(builder: DramaBuilder) -> None:
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
                "echo3_sora_relocate",
                "ソラ:「場所を変える。科学的にやろう」",
                "Sora: 'Changing position. Let's do this properly.'",
                "",
                sora,
            ),
            (
                "echo3_sora_three_claps",
                "ソラ:「手を三回。間を空けずに」",
                "Sora: 'Three claps. No pause.'",
                "",
                sora,
            ),
        ]
    )
    # Sora claps three times, then the 3-second echo plays three
    # claps back. The built-in SE plays once per resolve_run, so
    # schedule three clap SE plays and three return SE plays to
    # match the "回数まで合ってる" line below.
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP_RETURN)
    builder.resolve_run(AUDIO_SE_CLAP_RETURN)
    builder.resolve_run(AUDIO_SE_CLAP_RETURN)
    builder.conversation(
        [
            (
                "echo3_sora_count",
                "ソラ:「三秒後……三回、返ってきた」",
                "Sora: 'Three seconds later... three claps came back.'",
                "",
                sora,
            ),
            (
                "echo3_sora_count_preserved",
                "ソラ:「回数まで合ってる」",
                "Sora: 'The count matches.'",
                "",
                sora,
            ),
            (
                "echo3_mina_reflection",
                "ミナ:「反射で回数が保存されるのは……」",
                "Mina: 'Reflection preserving the count...'",
                "",
                mina,
            ),
            (
                "echo3_sora_one_more",
                "ソラ:「ありえなくはない。だがもう一つ試す」",
                "Sora: 'Possible. I'm running one more test.'",
                "",
                sora,
            ),
            (
                "echo3_mina_resign",
                "ミナ:「……好きにしろ」",
                "Mina: '...Do what you want.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_ECHO_EXPERIMENT, 3)
    builder.drama_end(0.3)
