# -*- coding: utf-8 -*-
"""
Chapter-4 reunion drama: entry into Yuu's camp and the first meeting.

Chapter 4 sections 1-2 (story/chapters/chapter-04.md). The entire
atmospheric stack drops away as the party reaches Yuu's camp — no fog,
no LUT, no BGM, just a cooking fire and water drops. Yuu is alive,
casually chewing on jerky, asking how many days it's been.

Ends by advancing quest.stage from Layer3Clear to YuuFound via
cmd.elinikki.stage.advance.yuu_found (registered in the C# drama
resolver). Individual truth conversations live in separate dramas
(elinikki_truth_*) and are invoked by the Phase 3 chapter-4 dialogue
menu.
"""

from tools.drama.data import Actors, CommandKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_reunion(builder: DramaBuilder) -> None:
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    meet = builder.label("meet")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "reunion_sora_plain",
                "ソラ:「ここ……普通の洞窟だ」",
                "Sora: 'This place... it's just a cave.'",
                "",
                sora,
            ),
            (
                "reunion_sora_where",
                "ソラ:「花も、反響も、記号もない。焚き火の音と水滴だけだ」",
                "Sora: 'No flowers. No echo. No symbols. Just a campfire and water drops.'",
                "",
                sora,
            ),
            (
                "reunion_mina_look",
                "ミナ:「……あれは、焚き火だな。人が起こしたやつだ」",
                "Mina: '...That's a fire. Someone built that.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(meet)

    builder.step(meet)
    builder.conversation(
        [
            (
                "reunion_yuu_greet",
                "ユウ:「お、誰か来た。……ミナ？ ソラ？ マジか」",
                "Yuu: 'Huh, visitors. ...Mina? Sora? Seriously?'",
                "",
                yuu,
            ),
            (
                "reunion_yuu_lost",
                "ユウ:「出口分からなくてさ。何ヶ月だろ、これ。途中で日数数えるの飽きた」",
                "Yuu: 'I couldn't find the way out. How many months? I got bored of counting.'",
                "",
                yuu,
            ),
            (
                "reunion_mina_alive",
                "ミナ:「……生きてたのか」",
                "Mina: '...You're alive.'",
                "",
                mina,
            ),
            (
                "reunion_sora_what",
                "ソラ:「ユウ、ここで何があった？ 何を見た？」",
                "Sora: 'Yuu—what happened down here? What did you see?'",
                "",
                sora,
            ),
            (
                "reunion_yuu_casual",
                "ユウ:「何って……洞窟だよ。広いのと、音が響くのと、花が生えてるの」",
                "Yuu: 'What do you mean? It's a cave. Big. Echoes. Flowers growing.'",
                "",
                yuu,
            ),
            (
                "reunion_sora_response",
                "ソラ:「花だけじゃない。壁の記号は？ 応答は？」",
                "Sora: 'Not just flowers. The symbols. The response. Those.'",
                "",
                sora,
            ),
            (
                "reunion_yuu_puzzle",
                "ユウ:「……応答？」",
                "Yuu: '...Response?'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.resolve_run(CommandKeys.ELINIKKI_STAGE_ADVANCE_YUU_FOUND)
    builder.drama_end(0.3)
