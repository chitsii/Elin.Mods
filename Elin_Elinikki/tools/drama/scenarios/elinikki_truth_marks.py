# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: wall markings.

Chapter 4 "真相会話: 壁の刻み目" (story/chapters/chapter-04.md).
Yuu reveals the wall markings were day-count tallies that got bored
and turned into a failed flower doodle. Sora tries one last time to
save his "geometric progression" theory and fails.

Sets chitsii.elinikki.quest.event.truth_marks = 1 on completion. The
chapter-4 dialogue menu (Phase 3) is responsible for only offering
this conversation when trace_marks == 1 and truth_marks == 0.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_marks(builder: DramaBuilder) -> None:
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "truth_marks_yuu_days",
                "ユウ:「ああ、あれ。日数数えてたんだけどさ。正の字ばっかりで飽きて、途中から適当に模様描いた」",
                "Yuu: 'Oh, those. I was tallying days. Got bored of straight lines and started drawing shapes.'",
                "",
                yuu,
            ),
            (
                "truth_marks_sora_series",
                "ソラ:「……等比数列に見えたんだが」",
                "Sora: '...Looked like a geometric progression to me.'",
                "",
                sora,
            ),
            (
                "truth_marks_yuu_what",
                "ユウ:「何それ」",
                "Yuu: 'A what?'",
                "",
                yuu,
            ),
            (
                "truth_marks_sora_try_again",
                "ソラ:「間隔に法則が……いや、本当に適当か？ 途中からの模様、何か考えて描いた？」",
                "Sora: 'The spacing—no, are you sure it was random? Those later shapes, were you thinking of anything?'",
                "",
                sora,
            ),
            (
                "truth_marks_yuu_flower",
                "ユウ:「花の形描こうとして失敗した。それだけ」",
                "Yuu: 'Tried to draw a flower. Failed. That's it.'",
                "",
                yuu,
            ),
            (
                "truth_marks_sora_silent",
                "ソラ:「……」",
                "Sora: '...'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_MARKS, 1)
    builder.drama_end(0.3)
