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


# Elinikki audio command prefixes. Resolved by QuestDramaResolver
# into SoundManager / SE playback at drama execution time. The id
# segment is the Elin sound asset name (currently placeholder —
# verified and tuned in Task 6.1 playthrough).
AUDIO_SE_FOOTSTEP_ECHO = "cmd.elinikki.audio.se.play.elinikki_echo_footstep"
AUDIO_SE_CLAP = "cmd.elinikki.audio.se.play.elinikki_echo_clap"
AUDIO_SE_CLAP_RETURN = "cmd.elinikki.audio.se.play.elinikki_echo_return"
AUDIO_SE_PATTERN_RETURN = "cmd.elinikki.audio.se.play.elinikki_echo_pattern"


def define_elinikki_echo_stage_1(builder: DramaBuilder) -> None:
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    # Stage 1 is the awareness beat: the player's own footsteps
    # come back a beat too late. Trigger the footstep echo SE once
    # as the drama opens so the conversation below can react to
    # what the player "just heard" in-fiction.
    builder.resolve_run(AUDIO_SE_FOOTSTEP_ECHO)
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
