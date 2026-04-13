# -*- coding: utf-8 -*-
"""
Chapter-2 echo experiment stage 1: awareness (気づき).

Chapter 2 section 2 (story/chapters/chapter-02.md). Triggered on entry to
the echo layer. Sora notices the return timing of footsteps is wrong;
Mina brushes it off as ordinary cave echo. No handclap yet — this is the
setup before the three real experiment stages.

Sets chitsii.elinikki.quest.state.echo_experiment = 1 on completion so
the point-A trigger (stage 2) can be gated.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_echo_stage_1(builder: DramaBuilder) -> None:
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
                "echo1_sora_ceiling",
                "ソラ:「天井が……見えない。とんでもない規模だ」",
                "Sora: 'Can't see the ceiling. The scale of this place...'",
                "",
                sora,
            ),
            (
                "echo1_mina_watch",
                "ミナ:「足元見ろ。上向いてると落ちる」",
                "Mina: 'Eyes down. You'll fall if you keep looking up.'",
                "",
                mina,
            ),
            (
                "echo1_sora_timing",
                "ソラ:「……足音の返り、距離と合わないな」",
                "Sora: '...The echo timing. It doesn't match the distance.'",
                "",
                sora,
            ),
            (
                "echo1_mina_normal",
                "ミナ:「洞窟なんだからエコーぐらいあるだろ」",
                "Mina: 'It's a cave. Echoes happen.'",
                "",
                mina,
            ),
            (
                "echo1_sora_not_normal",
                "ソラ:「そういう話じゃない。……進もう」",
                "Sora: 'Not that kind of echo. ...Let's keep moving.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_ECHO_EXPERIMENT, 1)
    builder.drama_end(0.3)
