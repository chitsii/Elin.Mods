# -*- coding: utf-8 -*-
"""Feature showcase drama for QuestMod template."""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_quest_drama_feature_showcase(builder: DramaBuilder) -> None:
    guide = Actors.NARRATOR
    demo_npc = "showcase_chicken"

    start = builder.label("main")
    intro = builder.label("intro")
    route = builder.label("route")

    # Intro and pre-check.
    builder.drama_start(bg_id="bg3", fade_duration=0.15)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.play_bgm_vanilla(41)
    builder.set_flag(FlagKeys.TMP_CAN_START_FEATURE, 0)
    builder.set_flag(FlagKeys.TMP_IS_DONE_FEATURE, 0)
    builder.set_flag(FlagKeys.TMP_BRANCH_FEATURE, 0)
    builder.set_flag(FlagKeys.TMP_COUNT_FEATURE, 0)
    builder.set_flag("yourname.elin_quest_mod.flag.feature_followup.run_count", 0)
    builder.set_flag("yourname.elin_quest_mod.tmp.feature_followup.stage", 0)
    builder.set_flag("yourname.elin_quest_mod.tmp.feature_followup.error_count", 0)
    builder.set_flag("yourname.elin_quest_mod.tmp.can_start.feature_followup", 0)
    builder.set_flag("yourname.elin_quest_mod.tmp.is_done.feature_followup", 0)
    builder.set_flag("yourname.elin_quest_mod.tmp.feature_showcase.done_at_followup_entry", 0)
    builder.set_flag("yourname.elin_quest_mod.tmp.feature_followup.diag.resolve_status", -1)
    builder.set_flag("yourname.elin_quest_mod.tmp.feature_followup.start_cond.all", 0)
    builder.set_flag("yourname.elin_quest_mod.tmp.feature_followup.cooldown_ready", 0)
    builder.set_flag("yourname.elin_quest_mod.flag.feature_followup.last_advance_raw", 0)
    builder.set_flag("yourname.elin_quest_mod.flag.feature.branch.last_choice", 0)

    # Sync quest conditions to temporary flags.
    builder.quest_check("quest_drama_feature_showcase", FlagKeys.TMP_CAN_START_FEATURE)
    builder.resolve_flag(
        "state.quest.is_done.quest_drama_feature_showcase",
        FlagKeys.TMP_IS_DONE_FEATURE,
    )
    builder.jump(intro)

    builder.step(intro)
    builder.conversation(
        [
            (
                "feature_intro_1",
                "案内役: ようこそ。このドラマは QuestMod 機能の総合ショーケースです。",
                "Guide: Welcome. This drama is a full QuestMod showcase.",
                "",
                guide,
            ),
            (
                "feature_intro_2",
                "プレイヤー: 具体的には、何を確認できますか？",
                "Player: What exactly will be demonstrated?",
                "",
                "pc",
            ),
            (
                "feature_intro_3",
                "案内役: クエスト進行、ドラマ接続、分岐、そして演出の実行です。",
                "Guide: Quest flow, drama transitions, branching, and scene performance.",
                "",
                guide,
            ),
            (
                "feature_intro_4",
                "案内役: BGM、背景切替、NPCスポーン・移動、ポートレート変更を順に動かします。",
                "Guide: BGM, background switch, NPC spawn/move, and portrait updates will run.",
                "",
                guide,
            ),
        ]
    )
    builder.say_if(
        "feature_intro_5",
        "案内役: このセーブで開始条件がOFFでも、ショーケース本体は進行可能です。",
        DramaBuilder.cond_flag_equals(FlagKeys.TMP_CAN_START_FEATURE, 0),
        "Guide: Even if can_start is off, showcase flow will continue.",
        actor=guide,
    )
    builder.show_book("questmod_feature_guide")
    builder.jump(route)

    builder.step(route)
    builder.conversation(
        [
            (
                "feature_route_1",
                "案内役: まず背景を切り替え、シーン演出を実行します。",
                "Guide: First we switch background and run scene actions.",
                "",
                guide,
            ),
        ]
    )
    builder.scene_transition(bg_id="bg_labo", fade_duration=0.2)
    builder.say(
        "feature_route_1_2",
        "案内役: ここで背景演出の確認は完了です。これから背景を解除して実マップ上のNPC演出を見せます。",
        "Guide: Background demo complete. Clearing background to show NPC actions on the live map.",
        actor=guide,
    )
    builder.clear_background()
    builder.wait(0.8)
    # Always-on scene action samples so runtime smoke can validate them regardless of branch coverage.
    builder.spawn_npc("chicken", level=1, actor_alias=demo_npc)
    builder.wait(1.2)
    builder.say(
        "feature_route_2",
        "案内役: ニワトリ(chicken)を1体スポーンしました。これから移動と見た目変更を順番に行います。",
        "Guide: Spawned one chicken. Next we run movement and visual updates step by step.",
        actor=guide,
    )
    builder.say(
        "feature_route_move_1",
        "案内役: 手順1。ニワトリを数マス歩かせます。まず3手の相対移動を連続で実行します。",
        "Guide: Step 1. Walk the chicken several tiles with three consecutive relative moves.",
        actor=guide,
    )
    builder.move_tile(1, 0, actor=demo_npc)
    builder.wait(0.5)
    builder.move_tile(1, 0, actor=demo_npc)
    builder.wait(0.5)
    builder.move_tile(0, 1, actor=demo_npc)
    builder.wait(0.8)
    builder.say(
        "feature_route_move_2",
        "案内役: 相対移動が完了しました。次に、比較としてプレイヤーの隣へ直接移動させます。",
        "Guide: Relative movement done. Next, move it directly next to the player for comparison.",
        actor=guide,
    )
    builder.move_next_to("pc", actor=demo_npc)
    builder.wait(0.8)
    builder.say(
        "feature_route_move_3",
        "案内役: 手順2完了。ここから見た目変更に入ります。",
        "Guide: Step 2 done. Now we switch to visual changes.",
        actor=guide,
    )
    builder.say(
        "feature_route_visual_1",
        "案内役: 手順3。エモートを表示します。",
        "Guide: Step 3. Show an emote.",
        actor=guide,
    )
    builder.play_emote("happy", actor=demo_npc)
    builder.wait(0.8)
    builder.say(
        "feature_route_visual_2",
        "案内役: 手順4。ポートレートを happy に変更します。",
        "Guide: Step 4. Change portrait to happy.",
        actor=guide,
    )
    builder.set_portrait("happy", actor=demo_npc)
    builder.wait(0.3)
    builder.say(
        "feature_route_visual_3",
        "対象NPC: いまポートレートが変更された状態です。",
        "Target NPC: This line is shown with the changed portrait.",
        actor=demo_npc,
    )
    builder.wait(0.6)
    builder.say(
        "feature_route_visual_4",
        "案内役: 手順5。ポートレートをデフォルトに戻します。",
        "Guide: Step 5. Reset portrait to default.",
        actor=guide,
    )
    builder.set_portrait(actor=demo_npc)
    builder.wait(0.3)
    builder.say(
        "feature_route_visual_5",
        "対象NPC: ポートレートをデフォルトに戻しました。",
        "Target NPC: Portrait has been reset to default.",
        actor=demo_npc,
    )
    builder.wait(0.6)
    builder.say(
        "feature_route_visual_6",
        "案内役: 手順6。Vile由来の Texture/ars_hecatia.png を使って、set_sprite(ars_hecatia) を実行します。",
        "Guide: Step 6. Apply set_sprite(ars_hecatia) using Vile's Texture/ars_hecatia.png.",
        actor=guide,
    )
    builder.set_sprite("ars_hecatia", actor=demo_npc)
    builder.wait(0.4)
    builder.say(
        "feature_route_visual_6_2",
        "対象NPC: いまカスタムスプライト(ars_hecatia)が適用されています。",
        "Target NPC: Custom sprite (ars_hecatia) is currently applied.",
        actor=demo_npc,
    )
    builder.wait(0.5)
    builder.say(
        "feature_route_visual_6_3",
        "案内役: 手順6-2。set_sprite() でスプライトをリセットします。",
        "Guide: Step 6-2. Reset sprite with set_sprite().",
        actor=guide,
    )
    builder.set_sprite(actor=demo_npc)
    builder.wait(0.6)
    builder.say(
        "feature_route_visual_7",
        "案内役: 手順7。ニワトリに爆発エフェクトを出し、呪いSEを再生します。",
        "Guide: Step 7. Trigger explosion effect on the chicken and play cursed SFX.",
        actor=guide,
    )
    builder.play_effect_ext("explosion", actor=demo_npc)
    builder.play_sound("curse3")
    builder.wait(0.8)
    builder.say(
        "feature_route_visual_8",
        "案内役: 爆発エフェクトと呪いSEのテストが完了しました。",
        "Guide: Explosion effect and cursed SFX test completed.",
        actor=guide,
    )
    builder.wait(1.0)
    builder.conversation(
        [
            (
                "feature_route_3",
                "案内役: ここまでで、NPCの移動と見た目変更が完了しました。次はクエスト関連デモへ進みます。",
                "Guide: NPC movement and visual updates are complete. Next is the quest-flow demo.",
                "",
                guide,
            ),
            (
                "feature_route_4",
                "プレイヤー: 了解です。ここからはクエスト制御を線形フローで確認するんですね。",
                "Player: Understood. From here we verify quest control in one linear flow.",
                "",
                "pc",
            ),
        ]
    )
    builder.say(
        "feature_quest_1",
        "案内役: ここからクエスト制御デモです。キュー実行、演出、完了処理、フォローアップ接続を順に実行します。",
        "Guide: Quest control demo starts now: cue, effects, completion, then follow-up handoff.",
        actor=guide,
    )
    builder.run_cue("questmod.feature_showcase_pulse")
    builder.play_pc_effect_with_sound("teleport", "revive")
    builder.mod_flag(FlagKeys.TMP_COUNT_FEATURE, "+", 1)
    builder.play_sound("base.ok")
    builder.wait(0.6)
    builder.say(
        "feature_quest_2",
        "案内役: showcase の完了フラグを更新し、続けてフォローアップ診断ドラマを起動します。",
        "Guide: Marking showcase complete, then starting the follow-up diagnostic drama.",
        actor=guide,
    )
    builder.stamp_current_raw_time("yourname.elin_quest_mod.flag.feature_followup.last_advance_raw")
    builder.resolve_run("cmd.quest.complete.quest_drama_feature_showcase")
    builder.action("stopBGM")
    builder.wait(0.2)
    builder.quest_try_start_repeatable("quest_drama_feature_followup")
    builder.finish()
