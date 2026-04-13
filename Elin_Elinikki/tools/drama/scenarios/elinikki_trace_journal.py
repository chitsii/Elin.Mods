# -*- coding: utf-8 -*-
"""
Chapter-1 journal discovery drama: Yuu's notebook.

Chapter 1 section 7 (story/chapters/chapter-01.md). Unlike the other
chapter-1 traces, the journal does NOT set a quest.event.trace_* flag
— instead it sets quest.state.journal_found, the spec's pure story
marker. There is no gameplay gate on the journal flag in the current
design, but the flag is authored so the story bible's 21-flag count
stays complete and future hooks (e.g. chapter-4 dialogue branching
on whether the player actually read the journal) have a hook to read.
Replay suppression is handled by the C# examine trigger.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_journal(builder: DramaBuilder) -> None:
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
                "journal_sora_find",
                "ソラ:「……ユウの字だ。手帳、まだ書き足されてる」",
                "Sora: '...Yuu's handwriting. The journal—still being written in.'",
                "",
                sora,
            ),
            (
                "journal_sora_last_page",
                "ソラ:「最後のページ……『伝わった』、とだけ書いてある」",
                "Sora: 'Last page... just one line. \"It got through.\"'",
                "",
                sora,
            ),
            (
                "journal_sora_puzzled",
                "ソラ:「『伝わった』？ 何が伝わったんだ？ 誰に?」",
                "Sora: '\"Got through\"? Got through what? To whom?'",
                "",
                sora,
            ),
            (
                "journal_mina_alive",
                "ミナ:「……あいつ、まだ生きてるのか」",
                "Mina: '...So the idiot's still alive.'",
                "",
                mina,
            ),
            (
                "journal_sora_bridge",
                "ソラ:「生きてる。ここにいる何かと、通じたのかもしれない」",
                "Sora: 'Alive. And maybe—reached something down here.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_JOURNAL_FOUND, 1)
    builder.drama_end(0.3)
