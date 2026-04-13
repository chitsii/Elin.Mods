# -*- coding: utf-8 -*-
"""
Chapter-5 silence ending drama (沈黙エンド).

Chapter 5 "沈黙エンド" (story/chapters/chapter-05.md). Plays when
the player finished chapter 4 without hearing any (or with only
partial) truths from Yuu. The named silence ending fires when truth
count is 0; truth counts of 1-7 share this drama under the spec's
"silence, partial" bucket rather than getting their own drama.

On completion:
  - quest.ending = 2 (silence)
  - quest.stage advanced to EndingSeen via
    cmd.elinikki.stage.advance.ending_seen
"""

from tools.drama.data import Actors, CommandKeys, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_ending_silence(builder: DramaBuilder) -> None:
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    epilogue = builder.label("epilogue")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.3)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "ending_silence_yuu_ask",
                "ユウ:「いろいろ聞きたいことあるだろ？」",
                "Yuu: 'You must have a lot of questions, right?'",
                "",
                yuu,
            ),
            (
                "ending_silence_pc_no",
                "「……いや、もういい」",
                "'...No. It's fine.'",
                "",
                Actors.PC,
            ),
            (
                "ending_silence_yuu_ok",
                "ユウ:「そう？ まあいいけど」",
                "Yuu: 'Yeah? Alright then.'",
                "",
                yuu,
            ),
            (
                "ending_silence_mina_sure",
                "ミナ:「聞かなくていいのか？」",
                "Mina: 'You sure you don't want to ask.'",
                "",
                mina,
            ),
            (
                "ending_silence_sora_final",
                "ソラ:「……見たものは、見たままでいい」",
                "Sora: '...What we saw can stay what we saw.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(epilogue)

    # -----------------------------------------------------------------
    # Silence epilogue: Sora starts a paper. Mina pushes back. Sora
    # insists it is "recording," not "believing."
    # -----------------------------------------------------------------
    builder.step(epilogue)
    builder.conversation(
        [
            (
                "ending_silence_epilogue_paper",
                "ソラは記録をまとめ始める。「異常ネフィアにおける非ランダム構造パターンの考察」。",
                "Sora starts writing it up. \"On the non-random structural patterns observed in an anomalous Nefia.\"",
                "",
                Actors.NARRATOR,
            ),
            (
                "ending_silence_epilogue_mina",
                "ミナ:「お前ら、まだ信じてるのか」",
                "Mina: 'You two still believing in it?'",
                "",
                mina,
            ),
            (
                "ending_silence_epilogue_sora",
                "ソラ:「信じてるんじゃない。記録してるんだ。見たものを」",
                "Sora: 'Not believing. Recording. What we saw.'",
                "",
                sora,
            ),
            (
                "ending_silence_epilogue_reach",
                "その記録はいずれ他の冒険者の目に触れるだろう。あのネフィアに向かう者が現れるかもしれない。",
                "The record will reach other adventurers someday. Some of them may decide to go looking for that Nefia.",
                "",
                Actors.NARRATOR,
            ),
            (
                "ending_silence_final_1",
                "答えは分からないままだ。",
                "The answers stay out of reach.",
                "",
                Actors.NARRATOR,
            ),
            (
                "ending_silence_final_2",
                "でも、見たものは見た。",
                "But what was seen was seen.",
                "",
                Actors.NARRATOR,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_QUEST_ENDING, 2)
    builder.resolve_run(CommandKeys.ELINIKKI_STAGE_ADVANCE_ENDING_SEEN)
    builder.drama_end(0.4)
