# -*- coding: utf-8 -*-
"""Follow-up drama used by feature showcase branch handoff.

Quest chain overview::

  showcase (complete) -> [handoff] -> followup (choice) -> branch_a or branch_b (complete) -> merge (complete)
                                       ^^^ this file

This drama demonstrates:
  - Quest-to-quest handoff verification
  - Flag-based start conditions (resolve_flag / resolve_flags_all)
  - Time-based cooldown conditions (resolve_cooldown_elapsed_days)
  - Actual save-state branching using live values from current save
  - Choice-based quest branching with merge convergence
"""

from tools.drama.data import Actors, ResolveKeys
from tools.drama.drama_builder import DramaBuilder


FLAG_FOLLOWUP_RUN_COUNT = "yourname.elin_quest_mod.flag.feature_followup.run_count"
FLAG_FOLLOWUP_STAGE = "yourname.elin_quest_mod.tmp.feature_followup.stage"
FLAG_FOLLOWUP_CAN_START = "yourname.elin_quest_mod.tmp.can_start.feature_followup"
FLAG_FOLLOWUP_IS_DONE = "yourname.elin_quest_mod.tmp.is_done.feature_followup"
FLAG_SHOWCASE_DONE_AT_ENTRY = "yourname.elin_quest_mod.tmp.feature_showcase.done_at_followup_entry"
FLAG_FOLLOWUP_START_COND_ALL = "yourname.elin_quest_mod.tmp.feature_followup.start_cond.all"
FLAG_FOLLOWUP_COOLDOWN_READY = "yourname.elin_quest_mod.tmp.feature_followup.cooldown_ready"
FLAG_FOLLOWUP_LAST_ADVANCE_RAW = "yourname.elin_quest_mod.flag.feature_followup.last_advance_raw"
FLAG_FOLLOWUP_DEMO_START_COND = "yourname.elin_quest_mod.tmp.feature_followup.demo.start_cond"
FLAG_FOLLOWUP_DEMO_COOLDOWN = "yourname.elin_quest_mod.tmp.feature_followup.demo.cooldown"
FLAG_BRANCH_LAST_CHOICE = "yourname.elin_quest_mod.flag.feature.branch.last_choice"


def define_quest_drama_feature_showcase_followup(builder: DramaBuilder) -> None:
    guide = Actors.NARRATOR

    # --- Handoff verification ---
    start = builder.label("main")
    handoff_check = builder.label("handoff_check")
    handoff_confirmed = builder.label("handoff_confirmed")
    handoff_unexpected = builder.label("handoff_unexpected")

    # --- Overview ---
    overview = builder.label("overview")

    # --- Demo: condition A (flag-based start condition) ---
    demo_start_intro = builder.label("demo_start_intro")
    demo_start_true_check = builder.label("demo_start_true_check")
    demo_start_true_ok = builder.label("demo_start_true_ok")
    demo_start_true_ng = builder.label("demo_start_true_ng")
    demo_start_false_prep = builder.label("demo_start_false_prep")
    demo_start_false_check = builder.label("demo_start_false_check")
    demo_start_false_ok = builder.label("demo_start_false_ok")
    demo_start_false_ng = builder.label("demo_start_false_ng")

    # --- Demo: condition B (cooldown elapsed) ---
    demo_cd_intro = builder.label("demo_cd_intro")
    demo_cd_true_check = builder.label("demo_cd_true_check")
    demo_cd_true_ok = builder.label("demo_cd_true_ok")
    demo_cd_true_ng = builder.label("demo_cd_true_ng")
    demo_cd_false_prep = builder.label("demo_cd_false_prep")
    demo_cd_false_check = builder.label("demo_cd_false_check")
    demo_cd_false_ok = builder.label("demo_cd_false_ok")
    demo_cd_false_ng = builder.label("demo_cd_false_ng")

    # --- Actual branching (live save values) ---
    actual_intro = builder.label("actual_intro")
    actual_start_check = builder.label("actual_start_check")
    actual_start_ok = builder.label("actual_start_ok")
    actual_start_ng = builder.label("actual_start_ng")
    actual_start_force_intro = builder.label("actual_start_force_intro")
    actual_start_force_check = builder.label("actual_start_force_check")
    actual_start_force_ok = builder.label("actual_start_force_ok")
    actual_start_force_ng = builder.label("actual_start_force_ng")
    actual_cd_check = builder.label("actual_cd_check")
    actual_cd_ok = builder.label("actual_cd_ok")
    actual_cd_ng = builder.label("actual_cd_ng")

    # --- Summary and branch choice ---
    summary = builder.label("summary")
    branch_intro = builder.label("branch_intro")
    branch_menu = builder.label("branch_menu")
    branch_route_a = builder.label("branch_route_a")
    branch_route_b = builder.label("branch_route_b")

    builder.drama_start(bg_id="bg3", fade_duration=0.12)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.set_flag(FLAG_FOLLOWUP_STAGE, 0)
    builder.set_flag(FLAG_FOLLOWUP_CAN_START, 0)
    builder.set_flag(FLAG_FOLLOWUP_IS_DONE, 0)
    builder.set_flag(FLAG_SHOWCASE_DONE_AT_ENTRY, 0)
    builder.set_flag(FLAG_FOLLOWUP_START_COND_ALL, 0)
    builder.set_flag(FLAG_FOLLOWUP_COOLDOWN_READY, 0)
    builder.set_flag(FLAG_FOLLOWUP_DEMO_START_COND, 0)
    builder.set_flag(FLAG_FOLLOWUP_DEMO_COOLDOWN, 0)
    builder.mod_flag(FLAG_FOLLOWUP_RUN_COUNT, "+", 1)

    builder.resolve_flag(
        ResolveKeys.QUEST_CAN_START_FEATURE_FOLLOWUP,
        FLAG_FOLLOWUP_CAN_START,
    )
    builder.resolve_flag(
        ResolveKeys.QUEST_DONE_FEATURE_FOLLOWUP,
        FLAG_FOLLOWUP_IS_DONE,
    )
    builder.resolve_flag(
        ResolveKeys.QUEST_DONE_FEATURE,
        FLAG_SHOWCASE_DONE_AT_ENTRY,
    )
    builder.resolve_flags_all(
        [FLAG_SHOWCASE_DONE_AT_ENTRY, FLAG_FOLLOWUP_CAN_START],
        FLAG_FOLLOWUP_START_COND_ALL,
    )
    builder.resolve_cooldown_elapsed_days(
        FLAG_FOLLOWUP_LAST_ADVANCE_RAW,
        FLAG_FOLLOWUP_COOLDOWN_READY,
        cooldown_days=1,
    )
    builder.conversation(
        [
            (
                "feature_followup_1",
                "案内役: ここはフォローアップドラマです。ここに来た時点で、前クエストからの接続が発生しています。",
                "Guide: This is the follow-up drama. Reaching here means handoff from the previous quest already occurred.",
                "",
                guide,
            ),
            (
                "feature_followup_2",
                "案内役: この後は、接続確認 -> 全体像 -> 条件バリアント体験 -> 実測分岐 -> まとめ、の順で進めます。",
                "Guide: We proceed in order: handoff check -> overview -> condition variants -> actual branching -> summary.",
                "",
                guide,
            ),
        ]
    )
    builder.jump(handoff_check)

    builder.step(handoff_check)
    builder.switch_on_flag(
        FLAG_SHOWCASE_DONE_AT_ENTRY,
        {
            1: handoff_confirmed,
        },
        fallback=handoff_unexpected,
    )

    builder.step(handoff_confirmed)
    builder.conversation(
        [
            (
                "feature_followup_handoff_1",
                "案内役: showcase 完了フラグ=1 を検出しました。前クエスト完了後に自動でここへ接続されています。",
                "Guide: Detected showcase complete flag=1. This was auto-connected after previous quest completion.",
                "",
                guide,
            ),
            (
                "feature_followup_handoff_2",
                "プレイヤー: クエスト間の接続が、実際の進行として確認できました。",
                "Player: I confirmed quest-to-quest connection in an actual progression flow.",
                "",
                "pc",
            ),
        ]
    )
    builder.mod_flag(FLAG_FOLLOWUP_STAGE, "+", 1)
    builder.jump(overview)

    builder.step(handoff_unexpected)
    builder.conversation(
        [
            (
                "feature_followup_handoff_warn_1",
                "案内役: showcase 完了フラグ=1 を検出できませんでした。手動起動の可能性があります。",
                "Guide: showcase complete flag=1 was not detected. This may be a manual launch.",
                "",
                guide,
            ),
            (
                "feature_followup_handoff_warn_2",
                "プレイヤー: 了解です。接続確認は参考扱いにして、条件デモを続けます。",
                "Player: Understood. We'll treat handoff as reference and continue with condition demos.",
                "",
                "pc",
            ),
        ]
    )
    builder.mod_flag(FLAG_FOLLOWUP_STAGE, "+", 1)
    builder.jump(overview)

    builder.step(overview)
    builder.conversation(
        [
            (
                "feature_followup_overview_1",
                "案内役: 全体像です。ここでは2種類の「クエスト開始条件」を見ます。",
                "Guide: High-level view: we cover two types of quest start conditions here.",
                "",
                guide,
            ),
            (
                "feature_followup_overview_2",
                "案内役: 条件Aはフラグ条件です。resolve_flag でランタイムからクエスト状態（完了済みか、開始可能か等）を一時フラグに同期し、resolve_flags_all で AND 判定します。",
                "Guide: Condition A is flag-based. resolve_flag syncs quest state (done, can_start, etc.) from runtime into temporary flags; resolve_flags_all combines them with AND logic.",
                "",
                guide,
            ),
            (
                "feature_followup_overview_3",
                "案内役: 条件Bは時間経過です。前クエスト完了時に stamp_current_raw_time で記録したゲーム内時刻を基準に、resolve_cooldown_elapsed_days が経過日数を判定します。",
                "Guide: Condition B is time-based. stamp_current_raw_time records in-game time at previous quest completion; resolve_cooldown_elapsed_days checks if enough days have passed.",
                "",
                guide,
            ),
            (
                "feature_followup_overview_4",
                "案内役: まず教育用に成立/不成立の両方を体験し、その後に今のセーブ実測値で分岐します。",
                "Guide: We first replay both satisfied/unsatisfied variants, then branch by actual values in this save.",
                "",
                guide,
            ),
            (
                "feature_followup_overview_5",
                "案内役: フラグ運用の規約です。tmp.* はドラマ中だけの一時判定用、flag.* はクエスト間で保持する永続状態です。",
                "Guide: Flag naming convention: tmp.* is temporary per-drama evaluation; flag.* is persistent state across quests.",
                "",
                guide,
            ),
            (
                "feature_followup_overview_6",
                "案内役: 例: tmp.start_cond.all は毎回再計算する分岐判定用、flag.last_advance_raw はクールダウン基準時刻として永続保持します。",
                "Guide: Example: tmp.start_cond.all is recomputed each time for branching; flag.last_advance_raw persists as cooldown baseline timestamp.",
                "",
                guide,
            ),
        ]
    )
    builder.jump(demo_start_intro)

    builder.step(demo_start_intro)
    builder.say(
        "feature_followup_demo_intro_1",
        "案内役: まず条件A(フラグ条件)の成立/不成立を、1回の導線で連続再生します。",
        "Guide: First, for condition A (flag condition), we replay satisfied/unsatisfied variants in one run.",
        actor=guide,
    )
    builder.say(
        "feature_followup_demo_intro_2",
        "案内役: 注意: 以下はデモ用フラグを手動で1/0に切り替えて両パスを体験する教育用の特殊構造です。実際のクエストではランタイムが条件を自動評価するため、このパターンは使いません。",
        "Guide: Note: The following manually toggles demo flags between 1/0 to walk both paths. This is an educational pattern only; in real quests, the runtime evaluates conditions automatically.",
        actor=guide,
    )
    builder.set_flag(FLAG_FOLLOWUP_DEMO_START_COND, 1)
    builder.jump(demo_start_true_check)

    builder.step(demo_start_true_check)
    builder.switch_on_flag(
        FLAG_FOLLOWUP_DEMO_START_COND,
        {
            1: demo_start_true_ok,
        },
        fallback=demo_start_true_ng,
    )

    builder.step(demo_start_true_ok)
    builder.say(
        "feature_followup_demo_start_true_ok",
        "案内役: 条件A 成立ルート: showcase_done && can_start = 1。",
        "Guide: Condition A satisfied route: showcase_done && can_start = 1.",
        actor=guide,
    )
    builder.jump(demo_start_false_prep)

    builder.step(demo_start_true_ng)
    builder.say(
        "feature_followup_demo_start_true_ng",
        "案内役: 条件A成立デモで想定外分岐に入りました。Player.log でフラグ値を確認するか、switch_on_flag の直前に set_flag で期待値が設定されているか見直してください。",
        "Guide: Unexpected branch during condition A satisfied demo. Check flag values in Player.log, or verify set_flag sets the expected value before switch_on_flag.",
        actor=guide,
    )
    builder.jump(demo_start_false_prep)

    builder.step(demo_start_false_prep)
    builder.set_flag(FLAG_FOLLOWUP_DEMO_START_COND, 0)
    builder.jump(demo_start_false_check)

    builder.step(demo_start_false_check)
    builder.switch_on_flag(
        FLAG_FOLLOWUP_DEMO_START_COND,
        {
            1: demo_start_false_ok,
        },
        fallback=demo_start_false_ng,
    )

    builder.step(demo_start_false_ok)
    builder.say(
        "feature_followup_demo_start_false_ok",
        "案内役: 条件A不成立デモで想定外分岐に入りました。set_flag で0に設定した直後なのに1と判定されています。Player.log でフラグ値を確認してください。",
        "Guide: Unexpected branch during condition A unsatisfied demo. Flag was just set to 0 but evaluated as 1. Check flag values in Player.log.",
        actor=guide,
    )
    builder.jump(demo_cd_intro)

    builder.step(demo_start_false_ng)
    builder.say(
        "feature_followup_demo_start_false_ng",
        "案内役: 条件A 不成立ルート: showcase_done && can_start = 0。",
        "Guide: Condition A unsatisfied route: showcase_done && can_start = 0.",
        actor=guide,
    )
    builder.jump(demo_cd_intro)

    builder.step(demo_cd_intro)
    builder.say(
        "feature_followup_demo_cd_intro",
        "案内役: 次に条件B(時間経過)の成立/不成立を連続再生します。仕組み: showcase完了時に stamp_current_raw_time でゲーム内時刻を記録し、ここで resolve_cooldown_elapsed_days が「記録時刻から1日経過したか」を判定します。",
        "Guide: Next, we replay condition B (elapsed time). Mechanism: stamp_current_raw_time records in-game time at showcase completion; here resolve_cooldown_elapsed_days checks if 1 day has passed since that timestamp.",
        actor=guide,
    )
    builder.set_flag(FLAG_FOLLOWUP_DEMO_COOLDOWN, 1)
    builder.jump(demo_cd_true_check)

    builder.step(demo_cd_true_check)
    builder.switch_on_flag(
        FLAG_FOLLOWUP_DEMO_COOLDOWN,
        {
            1: demo_cd_true_ok,
        },
        fallback=demo_cd_true_ng,
    )

    builder.step(demo_cd_true_ok)
    builder.say(
        "feature_followup_demo_cd_true_ok",
        "案内役: 条件B 成立ルート: 前クエスト完了から1日経過 = 1。",
        "Guide: Condition B satisfied route: 1 day elapsed since previous quest completion = 1.",
        actor=guide,
    )
    builder.jump(demo_cd_false_prep)

    builder.step(demo_cd_true_ng)
    builder.say(
        "feature_followup_demo_cd_true_ng",
        "案内役: 条件B成立デモで想定外分岐に入りました。Player.log でフラグ値を確認するか、switch_on_flag の直前に set_flag で期待値が設定されているか見直してください。",
        "Guide: Unexpected branch during condition B satisfied demo. Check flag values in Player.log, or verify set_flag sets the expected value before switch_on_flag.",
        actor=guide,
    )
    builder.jump(demo_cd_false_prep)

    builder.step(demo_cd_false_prep)
    builder.set_flag(FLAG_FOLLOWUP_DEMO_COOLDOWN, 0)
    builder.jump(demo_cd_false_check)

    builder.step(demo_cd_false_check)
    builder.switch_on_flag(
        FLAG_FOLLOWUP_DEMO_COOLDOWN,
        {
            1: demo_cd_false_ok,
        },
        fallback=demo_cd_false_ng,
    )

    builder.step(demo_cd_false_ok)
    builder.say(
        "feature_followup_demo_cd_false_ok",
        "案内役: 条件B不成立デモで想定外分岐に入りました。set_flag で0に設定した直後なのに1と判定されています。Player.log でフラグ値を確認してください。",
        "Guide: Unexpected branch during condition B unsatisfied demo. Flag was just set to 0 but evaluated as 1. Check flag values in Player.log.",
        actor=guide,
    )
    builder.mod_flag(FLAG_FOLLOWUP_STAGE, "+", 1)
    builder.jump(actual_intro)

    builder.step(demo_cd_false_ng)
    builder.say(
        "feature_followup_demo_cd_false_ng",
        "案内役: 条件B 不成立ルート: 前クエスト完了から1日経過 = 0。",
        "Guide: Condition B unsatisfied route: 1 day elapsed since previous quest completion = 0.",
        actor=guide,
    )
    builder.mod_flag(FLAG_FOLLOWUP_STAGE, "+", 1)
    builder.jump(actual_intro)

    builder.step(actual_intro)
    builder.conversation(
        [
            (
                "feature_followup_actual_1",
                "案内役: ここから実測分岐です。あなたの現在セーブの値で分岐します。",
                "Guide: Actual branching starts here using current values in your save.",
                "",
                guide,
            ),
            (
                "feature_followup_actual_2",
                "案内役: 先に条件Aを判定します。ここで重要なのは、showcase_done は既に1でも can_start は実行中状態で0になり得る点です。",
                "Guide: We check condition A first. Important point: showcase_done can already be 1 while can_start becomes 0 during active/running state.",
                "",
                guide,
            ),
        ]
    )
    builder.jump(actual_start_check)

    builder.step(actual_start_check)
    builder.switch_on_flag(
        FLAG_FOLLOWUP_START_COND_ALL,
        {
            1: actual_start_ok,
        },
        fallback=actual_start_ng,
    )

    builder.step(actual_start_ok)
    builder.say(
        "feature_followup_actual_start_ok",
        "案内役: 実測結果A: showcase_done=1 かつ can_start=1。A条件は成立です。",
        "Guide: Actual result A: showcase_done=1 and can_start=1. Condition A is satisfied.",
        actor=guide,
    )
    builder.jump(actual_start_force_intro)

    builder.step(actual_start_ng)
    builder.say(
        "feature_followup_actual_start_ng",
        "案内役: 実測結果A: showcase_done は1ですが can_start は0です。このためA条件は不成立です。",
        "Guide: Actual result A: showcase_done is 1, but can_start is 0. Therefore condition A is unsatisfied.",
        actor=guide,
    )
    builder.say(
        "feature_followup_actual_start_ng_2",
        "案内役: つまり『showcase完了済み』と『can_start可能』は別条件です。ここでは後者が落ちています。",
        "Guide: In short, 'showcase completed' and 'can_start available' are separate conditions. The latter is failing here.",
        actor=guide,
    )
    builder.jump(actual_start_force_intro)

    builder.step(actual_start_force_intro)
    builder.say(
        "feature_followup_actual_start_force_intro_1",
        "案内役: 続けて診断モードです。can_start を1へ上書きし、A条件を再同期して再判定します。",
        "Guide: Next is diagnostic mode. We overwrite can_start to 1, resync condition A, and check again.",
        actor=guide,
    )
    builder.set_flag(FLAG_FOLLOWUP_CAN_START, 1)
    builder.resolve_flags_all(
        [FLAG_SHOWCASE_DONE_AT_ENTRY, FLAG_FOLLOWUP_CAN_START],
        FLAG_FOLLOWUP_START_COND_ALL,
    )
    builder.jump(actual_start_force_check)

    builder.step(actual_start_force_check)
    builder.switch_on_flag(
        FLAG_FOLLOWUP_START_COND_ALL,
        {
            1: actual_start_force_ok,
        },
        fallback=actual_start_force_ng,
    )

    builder.step(actual_start_force_ok)
    builder.say(
        "feature_followup_actual_start_force_ok",
        "案内役: 再診断結果A: can_start=1へ上書き後、A条件は成立しました。",
        "Guide: Re-diagnosis A: after forcing can_start=1, condition A is now satisfied.",
        actor=guide,
    )
    builder.jump(actual_cd_check)

    builder.step(actual_start_force_ng)
    builder.say(
        "feature_followup_actual_start_force_ng",
        "案内役: 再診断結果A: can_start=1でも不成立です。showcase_done 側の値を確認してください。",
        "Guide: Re-diagnosis A: still unsatisfied even with can_start=1. Check showcase_done value.",
        actor=guide,
    )
    builder.jump(actual_cd_check)

    builder.step(actual_cd_check)
    builder.switch_on_flag(
        FLAG_FOLLOWUP_COOLDOWN_READY,
        {
            1: actual_cd_ok,
        },
        fallback=actual_cd_ng,
    )

    builder.step(actual_cd_ok)
    builder.say(
        "feature_followup_actual_cd_ok",
        "案内役: 実測結果B: 前クエスト完了から1日経過 = 1。時間条件は成立です。",
        "Guide: Actual result B: 1 day elapsed since previous quest completion = 1. Time condition is satisfied.",
        actor=guide,
    )
    builder.jump(summary)

    builder.step(actual_cd_ng)
    builder.say(
        "feature_followup_actual_cd_ng",
        "案内役: 実測結果B: 前クエスト完了から1日経過 = 0。時間条件は不成立です。",
        "Guide: Actual result B: 1 day elapsed since previous quest completion = 0. Time condition is unsatisfied.",
        actor=guide,
    )
    builder.jump(summary)

    builder.step(summary)
    builder.conversation(
        [
            (
                "feature_followup_summary_1",
                "案内役: まとめです。前クエストからの接続 -> 条件バリアント体験 -> 実測分岐、を完了しました。",
                "Guide: Summary: completed previous-quest handoff, condition variants, and actual branching.",
                "",
                guide,
            ),
            (
                "feature_followup_summary_2",
                "案内役: クエスト起動には3つのバリアントがあります。try_start は冪等で1回だけ起動、try_start_repeatable は完了済みでも再起動可能、try_start_until_complete は完了まで何度でも再試行します。",
                "Guide: Three quest start variants: try_start is idempotent (fires once), try_start_repeatable can restart even after completion, try_start_until_complete retries until the quest completes.",
                "",
                guide,
            ),
            (
                "feature_followup_summary_3",
                "案内役: 遷移条件は、クエスト完了コマンド、フラグ条件、時間経過条件を組み合わせて設計します。",
                "Guide: Transition conditions combine quest-complete commands, flag conditions, and elapsed-time checks.",
                "",
                guide,
            ),
            (
                "feature_followup_summary_4",
                "案内役: フラグ運用の原則は、tmp.* を診断・分岐、flag.* を履歴・時刻の保存に使うことです。",
                "Guide: Flag rule of thumb: use tmp.* for diagnostics/branching and flag.* for history/timestamp persistence.",
                "",
                guide,
            ),
            (
                "feature_followup_summary_5",
                "プレイヤー: フラグが分岐にどう反映されるか、会話の変化で確認できました。",
                "Player: I could verify how flags affect branching through dialogue changes.",
                "",
                "pc",
            ),
        ]
    )
    builder.jump(branch_intro)

    builder.step(branch_intro)
    builder.say(
        "feature_followup_branch_intro_1",
        "案内役: 最後に、選択肢によるクエスト分岐と、分岐先から単一クエストへのマージを実体験します。",
        "Guide: Finally, you will experience choice-based quest branching and branch-to-single-quest merge.",
        actor=guide,
    )
    builder.say(
        "feature_followup_branch_intro_2",
        "案内役: ここでA/Bを選ぶと、それぞれ別クエストへ進み、最終的に共通のmergeクエストへ合流します。",
        "Guide: Choosing A/B here moves to separate quests, then both converge into the shared merge quest.",
        actor=guide,
    )
    builder.say(
        "feature_followup_branch_intro_3",
        "案内役: ここでは quest_try_start_repeatable を使います。ショーケースは繰り返し実行可能にするためです。1回限りのクエストなら try_start を使います。",
        "Guide: We use quest_try_start_repeatable here so the showcase can be re-run. For one-time quests, use try_start instead.",
        actor=guide,
    )
    builder.jump(branch_menu)

    builder.step(branch_menu)
    builder.say(
        "feature_followup_branch_menu_1",
        "案内役: 分岐ルートを選択してください。",
        "Guide: Select your branch route.",
        actor=guide,
    )
    builder.choice(
        branch_route_a,
        "ルートAへ進む（branch_a を起動）",
        "Go to route A (start branch_a)",
        text_id="feature_followup_branch_choice_a",
    )
    builder.choice(
        branch_route_b,
        "ルートBへ進む（branch_b を起動）",
        "Go to route B (start branch_b)",
        text_id="feature_followup_branch_choice_b",
    )

    builder.step(branch_route_a)
    builder.say(
        "feature_followup_branch_route_a_1",
        "案内役: ルートAを選択しました。followupを完了し、branch_aを起動します。",
        "Guide: Route A selected. Completing followup and starting branch_a.",
        actor=guide,
    )
    builder.set_flag(FLAG_BRANCH_LAST_CHOICE, 1)
    builder.resolve_run("cmd.quest.complete.quest_drama_feature_followup")
    builder.quest_try_start_repeatable("quest_drama_feature_branch_a")
    builder.clear_background()
    builder.drama_end(0.2)

    builder.step(branch_route_b)
    builder.say(
        "feature_followup_branch_route_b_1",
        "案内役: ルートBを選択しました。followupを完了し、branch_bを起動します。",
        "Guide: Route B selected. Completing followup and starting branch_b.",
        actor=guide,
    )
    builder.set_flag(FLAG_BRANCH_LAST_CHOICE, 2)
    builder.resolve_run("cmd.quest.complete.quest_drama_feature_followup")
    builder.quest_try_start_repeatable("quest_drama_feature_branch_b")
    builder.clear_background()
    builder.drama_end(0.2)
