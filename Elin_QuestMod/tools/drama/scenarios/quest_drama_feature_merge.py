# -*- coding: utf-8 -*-
"""Merged quest drama for showcase branch convergence demo."""

from tools.drama.data import Actors
from tools.drama.drama_builder import DramaBuilder


FLAG_BRANCH_LAST_CHOICE = "yourname.elin_quest_mod.flag.feature.branch.last_choice"
FLAG_TMP_BRANCH_A_DONE = "yourname.elin_quest_mod.tmp.feature.branch_a.done"
FLAG_TMP_BRANCH_B_DONE = "yourname.elin_quest_mod.tmp.feature.branch_b.done"


def define_quest_drama_feature_merge(builder: DramaBuilder) -> None:
    guide = Actors.NARRATOR

    start = builder.label("main")
    route_check = builder.label("route_check")
    from_a = builder.label("from_a")
    from_b = builder.label("from_b")
    from_unknown = builder.label("from_unknown")
    summary = builder.label("summary")

    builder.drama_start(bg_id="bg3", fade_duration=0.1)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.set_flag(FLAG_TMP_BRANCH_A_DONE, 0)
    builder.set_flag(FLAG_TMP_BRANCH_B_DONE, 0)
    builder.resolve_flag(
        "state.quest.is_done.quest_drama_feature_branch_a",
        FLAG_TMP_BRANCH_A_DONE,
    )
    builder.resolve_flag(
        "state.quest.is_done.quest_drama_feature_branch_b",
        FLAG_TMP_BRANCH_B_DONE,
    )
    builder.say(
        "feature_merge_1",
        "案内役: ここは合流クエストです。分岐A/Bのどちらからでも最終的にここへ集約します。",
        "Guide: This is the merge quest. Both branch A and B converge here.",
        actor=guide,
    )
    builder.jump(route_check)

    builder.step(route_check)
    builder.switch_on_flag(
        FLAG_BRANCH_LAST_CHOICE,
        {
            1: from_a,
            2: from_b,
        },
        fallback=from_unknown,
    )

    builder.step(from_a)
    builder.say(
        "feature_merge_from_a",
        "案内役: 今回は分岐Aから合流しました。選択結果が1本のクエストにマージされています。",
        "Guide: This run merged from branch A. Your choice now converged into one quest.",
        actor=guide,
    )
    builder.jump(summary)

    builder.step(from_b)
    builder.say(
        "feature_merge_from_b",
        "案内役: 今回は分岐Bから合流しました。選択結果が1本のクエストにマージされています。",
        "Guide: This run merged from branch B. Your choice now converged into one quest.",
        actor=guide,
    )
    builder.jump(summary)

    builder.step(from_unknown)
    builder.say(
        "feature_merge_from_unknown",
        "案内役: 分岐経路を特定できませんでした。デバッグ用手動起動の可能性があります。",
        "Guide: Branch route could not be identified. This may be a debug/manual launch.",
        actor=guide,
    )
    builder.jump(summary)

    builder.step(summary)
    builder.say_if(
        "feature_merge_2_a",
        "案内役: 診断: branch_a 完了フラグ = 1",
        DramaBuilder.cond_flag_equals(FLAG_TMP_BRANCH_A_DONE, 1),
        "Guide: Diagnostic: branch_a done flag = 1",
        actor=guide,
    )
    builder.say_if(
        "feature_merge_2_b",
        "案内役: 診断: branch_b 完了フラグ = 1",
        DramaBuilder.cond_flag_equals(FLAG_TMP_BRANCH_B_DONE, 1),
        "Guide: Diagnostic: branch_b done flag = 1",
        actor=guide,
    )
    builder.say(
        "feature_merge_3",
        "案内役: 分岐クエストから単一クエストへのマージデモを完了しました。",
        "Guide: Branch-to-single-quest merge demonstration completed.",
        actor=guide,
    )
    builder.resolve_run("cmd.quest.complete.quest_drama_feature_merge")
    builder.clear_background()
    builder.drama_end(0.15)
