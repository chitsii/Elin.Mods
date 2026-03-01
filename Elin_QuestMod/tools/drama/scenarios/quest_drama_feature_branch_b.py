# -*- coding: utf-8 -*-
"""Branch B quest drama for showcase branching demo."""

from tools.drama.data import Actors
from tools.drama.drama_builder import DramaBuilder


FLAG_BRANCH_LAST_CHOICE = "yourname.elin_quest_mod.flag.feature.branch.last_choice"


def define_quest_drama_feature_branch_b(builder: DramaBuilder) -> None:
    guide = Actors.NARRATOR

    start = builder.label("main")

    builder.drama_start(bg_id="bg_labo", fade_duration=0.1)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.set_flag(FLAG_BRANCH_LAST_CHOICE, 2)
    builder.conversation(
        [
            (
                "feature_branch_b_1",
                "案内役: 分岐クエストBが開始されました。選択肢Bの結果としてこのクエストへ到達しています。",
                "Guide: Branch quest B has started. You reached this quest from choice B.",
                "",
                guide,
            ),
            (
                "feature_branch_b_2",
                "案内役: ここでBを完了し、次に合流クエストを起動します。",
                "Guide: We complete route B here, then start the merge quest.",
                "",
                guide,
            ),
        ]
    )
    builder.resolve_run("cmd.quest.complete.quest_drama_feature_branch_b")
    builder.quest_try_start_repeatable("quest_drama_feature_merge")
    builder.clear_background()
    builder.drama_end(0.15)
