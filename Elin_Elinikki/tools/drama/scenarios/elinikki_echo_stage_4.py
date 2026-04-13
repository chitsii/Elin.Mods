# -*- coding: utf-8 -*-
"""
Chapter-2 echo experiment stage 4: pattern recognition (地点C) — PHM climax.

Chapter 2 section 5 (story/chapters/chapter-02.md). This is the PHM
(post-human-mind) climax beat. Sora sends a compound pattern (2 claps,
pause, 3 claps) and the full pattern returns. Sora calls it a response,
not a reflection. Mina goes silent for the first time — she does not
contradict him.

Sets:
  - chitsii.elinikki.quest.state.echo_experiment = 4
  - chitsii.elinikki.quest.event.trace_echo = 1

trace_echo is what unlocks the corresponding chapter-4 truth
conversation with Yuu.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder
from tools.drama.scenarios.elinikki_echo_stage_1 import (
    AUDIO_SE_CLAP,
    AUDIO_SE_PATTERN_RETURN,
)


def define_elinikki_echo_stage_4(builder: DramaBuilder) -> None:
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
                "echo4_sora_pattern_setup",
                "ソラ:「今度はパターンを送る。二回叩いて、間を空けて、三回」",
                "Sora: 'This time I'm sending a pattern. Two claps. Pause. Three claps.'",
                "",
                sora,
            ),
            (
                "echo4_sora_send",
                "ソラ:「……行くぞ」",
                "Sora: '...Here it goes.'",
                "",
                sora,
            ),
        ]
    )
    # The PHM climax clap pattern: two claps, pause, three claps.
    # Task 6.1 in-game tuning will verify the SE timing; for now
    # issue five clap plays in sequence and one pattern return
    # to cover the "2→pause→3" cadence the story describes.
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_CLAP)
    builder.resolve_run(AUDIO_SE_PATTERN_RETURN)
    builder.conversation(
        [
            (
                "echo4_sora_return",
                "ソラ:「三秒──二回返ってきた。間。三回返ってきた」",
                "Sora: 'Three seconds—two claps. Pause. Three claps.'",
                "",
                sora,
            ),
            (
                "echo4_sora_shock",
                "ソラ:「パターンを送ったら、パターンが返ってきた」",
                "Sora: 'I sent a pattern. A pattern came back.'",
                "",
                sora,
            ),
            (
                "echo4_sora_verdict",
                "ソラ:「これは反射じゃない。応答だ。何かがこちらを認識して返事をしている」",
                "Sora: 'This isn't reflection. It's response. Something is hearing us and replying.'",
                "",
                sora,
            ),
            (
                "echo4_mina_quiet",
                "ミナ:「……」",
                "Mina: '...'",
                "",
                mina,
            ),
            (
                "echo4_sora_confirm",
                "ソラ:「ミナ。聞いたよな？」",
                "Sora: 'Mina. You heard that, right?'",
                "",
                sora,
            ),
            (
                "echo4_mina_heard",
                "ミナ:「……聞いた」",
                "Mina: '...I heard it.'",
                "",
                mina,
            ),
            (
                "echo4_sora_conclusion",
                "ソラ:「ここに何かいる。知性がある。伝えようとしてる」",
                "Sora: 'Something is here. It's intelligent. And it's trying to talk.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_ECHO_EXPERIMENT, 4)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_ECHO, 1)
    builder.drama_end(0.3)
