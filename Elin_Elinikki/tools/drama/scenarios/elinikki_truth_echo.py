# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: echo response.

Chapter 4 "真相会話: 反響音" + "手帳の『伝わった』" combined
(story/chapters/chapter-04.md). The journal beat is folded in here
because both reveals hinge on the same fact — Yuu had the same fake
epiphany Sora did, and in Yuu's mouth it is explicitly deflated to
"it's just your own voice coming back."

Sets chitsii.elinikki.quest.event.truth_echo = 1 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_echo(builder: DramaBuilder) -> None:
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    journal = builder.label("journal")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "truth_echo_yuu_fear",
                "ユウ:「エコーすごいよな、あの場所。最初めちゃくちゃ怖かった。自分の足音が追いかけてくるみたいで」",
                "Yuu: 'The echoes in that place are wild. Scared me at first. My own footsteps chasing me.'",
                "",
                yuu,
            ),
            (
                "truth_echo_yuu_play",
                "ユウ:「慣れたら面白くなって、手叩いたり声出したりしてた。暇だったし」",
                "Yuu: 'Once I got used to it I started clapping. Shouting. Nothing else to do.'",
                "",
                yuu,
            ),
            (
                "truth_echo_sora_pattern",
                "ソラ:「パターンを送ったら返事が来た。あれもエコーか？」",
                "Sora: 'I sent a pattern. A pattern came back. Was that just echo too?'",
                "",
                sora,
            ),
            (
                "truth_echo_yuu_odd_shape",
                "ユウ:「たぶん。あそこ、変な反射するんだよ。壁の形が複雑で。俺も最初は返事来たと思った」",
                "Yuu: 'Probably. The walls over there are weird—bounces everywhere. I thought it was a reply too, at first.'",
                "",
                yuu,
            ),
            (
                "truth_echo_sora_you_too",
                "ソラ:「……お前も思ったのか」",
                "Sora: '...You thought so too.'",
                "",
                sora,
            ),
            (
                "truth_echo_yuu_glad",
                "ユウ:「うん。嬉しかったよ、あの瞬間。誰かいるって思えて」",
                "Yuu: 'Yeah. Happy for a second there. Thought I wasn't alone.'",
                "",
                yuu,
            ),
            (
                "truth_echo_yuu_voice",
                "ユウ:「でもまあ、自分の声だよな。何回やっても同じパターンしか返ってこないし」",
                "Yuu: 'But it's just my own voice. Same pattern every time.'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(journal)

    # -----------------------------------------------------------------
    # Folded-in "手帳の『伝わった』" beat. It has no dedicated drama id
    # in the spec, but lives thematically with the echo truth.
    # -----------------------------------------------------------------
    builder.step(journal)
    builder.conversation(
        [
            (
                "truth_echo_sora_journal",
                "ソラ:「……手帳に『伝わった』って書いてあった」",
                "Sora: '...The journal. \"It got through.\" You wrote that.'",
                "",
                sora,
            ),
            (
                "truth_echo_yuu_journal",
                "ユウ:「ああ、あれか。エコーの場所で叫んだら、返事が来た気がしてさ。嬉しくて書いた」",
                "Yuu: 'Oh, yeah. Yelled into that echo spot and something came back. Got excited and wrote it down.'",
                "",
                yuu,
            ),
            (
                "truth_echo_yuu_only_me",
                "ユウ:「まあ、自分の声が返ってきただけなんだけど」",
                "Yuu: 'My own voice, though. That's all.'",
                "",
                yuu,
            ),
            (
                "truth_echo_yuu_alone",
                "ユウ:「でもあの瞬間は本当に通じたと思った。一人じゃないって」",
                "Yuu: 'But in that moment I thought it got through. That I wasn't alone.'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_ECHO, 1)
    builder.drama_end(0.3)
