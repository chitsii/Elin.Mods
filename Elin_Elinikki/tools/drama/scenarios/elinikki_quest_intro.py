# -*- coding: utf-8 -*-
"""
Chapter-0 intro drama for 「帰らなかった遠足」.

Mina visits the player's home to deliver the quest. Ends by advancing
quest.stage from NotStarted to Accepted via
`cmd.elinikki.stage.advance.accepted`.

Script structure mirrors story/chapters/chapter-00.md. Actor aliases
are still pointed at the built-in NARRATOR until real CWL character
rows for Mina / Sora are registered. The text is deliberately written
so it reads naturally with or without portraits.
"""

from tools.drama.data import Actors, CommandKeys, DramaIds  # noqa: F401
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_quest_intro(builder: DramaBuilder) -> None:
    # Aliases. All route to the NARRATOR until real characters exist.
    pc = Actors.PC
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")

    start = builder.label("main")
    sora_join = builder.label("sora_join")
    accept = builder.label("accept")
    end = builder.label("end")

    # -----------------------------------------------------------------
    # Opening: Mina arrives at the player's home with the request.
    # -----------------------------------------------------------------
    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "intro_knock",
                "ミナ:「……久しぶりだな。仕事の話だ。中に入れてくれ」",
                "Mina: '...Been a while. I've got a job for you. Let me in.'",
                "",
                mina,
            ),
            (
                "intro_mina_greet",
                "ミナ:「急に悪いな。あんたの評判は聞いてる。この手のネフィアに慣れてるだろ」",
                "Mina: 'Sorry to show up like this. I've heard of you. You've handled this kind of Nefia before, haven't you?'",
                "",
                mina,
            ),
            (
                "intro_mina_request",
                "ミナ:「人探しだ。エーテル濃度異常のネフィアに入った冒険者が戻ってこない。数ヶ月前からだ」",
                "Mina: 'It's a missing-person job. An adventurer went into an anomalous-ether Nefia months ago and never came back.'",
                "",
                mina,
            ),
            (
                "intro_mina_name",
                "ミナ:「名前はユウ。昔、同じパーティにいた。一年前に解散してからは、一人でネフィアを回ってたらしい」",
                "Mina: 'Name's Yuu. We used to be in the same party. After we broke up a year ago, Yuu kept running Nefias solo.'",
                "",
                mina,
            ),
            (
                "intro_mina_regret",
                "ミナ:「……もっと早く気づくべきだった。それはあんたには関係ない話だ。報酬は出す」",
                "Mina: '...I should've noticed sooner. That's not your problem. I'll pay.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(sora_join)

    # -----------------------------------------------------------------
    # Sora joins — introduces the second companion and the "record"
    # motif that later drives the knowledge-collapse beat.
    # -----------------------------------------------------------------
    builder.step(sora_join)
    builder.conversation(
        [
            (
                "intro_sora_arrive",
                "ソラ:「……ミナが腕の良い冒険者を雇ったって聞いて。同行させてほしい」",
                "Sora: '...I heard Mina hired a good one. Let me come along.'",
                "",
                sora,
            ),
            (
                "intro_sora_pitch",
                "ソラ:「学術目的、と言いたいところだけど……正直、ユウのことが気になってる」",
                "Sora: 'I could say academic curiosity. Honestly, though, I'm just worried about Yuu.'",
                "",
                sora,
            ),
            (
                "intro_sora_confession",
                "ソラ:「あの子がいなくなったって聞いても、最初ピンと来なかったんだ。それがちょっと……引っかかってる」",
                "Sora: 'When I heard Yuu was gone, it didn't hit me at first. That bothers me a little.'",
                "",
                sora,
            ),
            (
                "intro_mina_terse",
                "ミナ:「こいつは記録魔だ。邪魔はしない」",
                "Mina: 'She's obsessive about records. She won't get in your way.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(accept)

    # -----------------------------------------------------------------
    # Player's acceptance. The PC is silent in-game; the drama stays
    # short here and lets Mina carry the commitment beat.
    # -----------------------------------------------------------------
    builder.step(accept)
    builder.say(
        "intro_mina_final",
        "ミナ:「……引き受けてくれるか。エーテルが濃い。気をつけろ。行くぞ」",
        "Mina: '...Will you take it? The ether's thick. Be careful. Let's move.'",
        actor=mina,
    )
    builder.jump(end)

    # -----------------------------------------------------------------
    # Exit: advance the quest stage and close the drama layer. This is
    # the commit point that moves quest.stage from NotStarted (0) to
    # Accepted (1), which also stops ElinikkiQuestFlow.TryStartIntroQuest
    # from re-opening this drama on later pulses.
    # -----------------------------------------------------------------
    builder.step(end)
    builder.resolve_run(CommandKeys.ELINIKKI_STAGE_ADVANCE_ACCEPTED)
    builder.drama_end(0.3)
