# -*- coding: utf-8 -*-
"""Branch A quest drama for showcase branching demo."""

from tools.drama.data import Actors
from tools.drama.drama_builder import DramaBuilder


FLAG_BRANCH_LAST_CHOICE = "yourname.elin_quest_mod.flag.feature.branch.last_choice"


def define_quest_drama_feature_branch_a(builder: DramaBuilder) -> None:
    guide = Actors.NARRATOR

    start = builder.label("main")

    builder.drama_start(bg_id="bg_road", fade_duration=0.1)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.set_flag(FLAG_BRANCH_LAST_CHOICE, 1)
    builder.conversation(
        [
            (
                "feature_branch_a_1",
                "案内役: 分岐クエストAが開始されました。選択肢Aの結果としてこのクエストへ到達しています。",
                "Guide: Branch quest A has started. You reached this quest from choice A.",
                "",
                guide,
            ),
            (
                "feature_branch_a_2",
                "案内役: ここでAを完了し、次に合流クエストを起動します。",
                "Guide: We complete route A here, then start the merge quest.",
                "",
                guide,
            ),
        ]
    )
    builder.resolve_run("cmd.quest.complete.quest_drama_feature_branch_a")
    builder.quest_try_start_repeatable("quest_drama_feature_merge")
    builder.clear_background()
    builder.drama_end(0.15)
