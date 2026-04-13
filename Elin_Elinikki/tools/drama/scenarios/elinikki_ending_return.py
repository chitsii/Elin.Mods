# -*- coding: utf-8 -*-
"""
Chapter-5 return ending drama (帰還エンド).

Chapter 5 "帰還エンド" (story/chapters/chapter-05.md). Plays when the
player has collected all 8 truth flags in chapter 4. Yuu casually
walks off to go eat, Sora says "welcome back," and the three short
epilogue beats (Sora closes the notebook without throwing it away,
Mina drinks alone at the bar remembering the flowers, a single bloom
glows in the player's room) play as a narrator voice-over. The final
line is the story bible's "全部くだらなかった。たぶん。"

On completion:
  - quest.ending = 1 (return)
  - quest.stage advanced to EndingSeen via
    cmd.elinikki.stage.advance.ending_seen
"""

from tools.drama.data import Actors, CommandKeys, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_ending_return(builder: DramaBuilder) -> None:
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
                "ending_return_yuu_leave",
                "ユウ:「じゃ、俺帰るわ。飯食いたい。ちゃんとした飯」",
                "Yuu: 'Alright, I'm heading home. I want food. Real food.'",
                "",
                yuu,
            ),
            (
                "ending_return_mina_yeah",
                "ミナ:「……ああ」",
                "Mina: '...Yeah.'",
                "",
                mina,
            ),
            (
                "ending_return_sora_call",
                "ソラ:「ユウ」",
                "Sora: 'Yuu.'",
                "",
                sora,
            ),
            (
                "ending_return_yuu_hmm",
                "ユウ:「ん？」",
                "Yuu: 'Hm?'",
                "",
                yuu,
            ),
            (
                "ending_return_sora_welcome",
                "ソラ:「……おかえり」",
                "Sora: '...Welcome back.'",
                "",
                sora,
            ),
            (
                "ending_return_yuu_back",
                "ユウ:「おう。ただいま」",
                "Yuu: 'Yeah. I'm home.'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(epilogue)

    # -----------------------------------------------------------------
    # Epilogue voice-over. The story bible specifies three images; we
    # present them as narrator (neutral actor) text so they float
    # outside the party dialogue frame.
    # -----------------------------------------------------------------
    builder.step(epilogue)
    builder.conversation(
        [
            (
                "ending_return_epilogue_sora",
                "ソラは手帳を開く。等比数列の計算。通信パターンの分析。知性体の行動予測。全部、間違いだった。しばらく眺めて、閉じる。捨てはしなかった。",
                "Sora opens the notebook. Geometric progressions. Signal analysis. Predictions of an intelligence. All of it wrong. He looks at the pages for a while. Closes it. Doesn't throw it away.",
                "",
                Actors.NARRATOR,
            ),
            (
                "ending_return_epilogue_mina",
                "ミナが酒場で一人で飲んでいる。ふと花の層を思い出す。ゴミから生えた花。でも、あの瞬間は綺麗だった。ミナはもう一杯頼む。",
                "Mina drinks alone at the tavern. The bloom layer comes back to her. Flowers that grew from garbage. But in that moment, they were beautiful. She orders another.",
                "",
                Actors.NARRATOR,
            ),
            (
                "ending_return_epilogue_flower",
                "あの花畑から一輪、持ち帰っていた。ゴミから生えた、エーテル変異の花。でも、光っている。",
                "A single bloom, carried back from the garden. Born from garbage. An ether mutation. Still glowing.",
                "",
                Actors.NARRATOR,
            ),
            (
                "ending_return_final_1",
                "全部くだらなかった。",
                "All of it was nothing.",
                "",
                Actors.NARRATOR,
            ),
            (
                "ending_return_final_2",
                "たぶん。",
                "Probably.",
                "",
                Actors.NARRATOR,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_QUEST_ENDING, 1)
    builder.resolve_run(CommandKeys.ELINIKKI_STAGE_ADVANCE_ENDING_SEEN)
    builder.drama_end(0.4)
