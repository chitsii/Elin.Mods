from drama_dsl_v5_1 import DramaDsl, compile_xlsx, save_xlsx

d = DramaDsl(mod_name="QuestMod")


class IDs:
    QUEST_SHOWCASE = d.id("quest", "quest_drama_v51_showcase")

    FLAG_CAN_START_FOLLOWUP = d.id("flag", "yourname.questmod.can_start.followup")
    FLAG_DAY_PASSED = d.id("flag", "yourname.questmod.elapsed.day")

    BG_HALL = d.id("bg", "BG/hall")
    BG_FIELD = d.id("bg", "BG/field")
    BG_GATE = d.id("bg", "BG/gate")
    BGM_GUIDE = d.id("bgm", "BGM/ManuscriptByCandlelight")
    BGM_TENSE = d.id("bgm", "BGM/Ominous_Suspense_01")

    CHARA_CHICKEN = d.id("chara", "chicken")
    EMOTE_NOTE = d.id("emote", "note")
    FX_TELEPORT = d.id("effect", "teleport")
    FX_EXPLODE = d.id("effect", "explode")
    SND_REVIVE = d.id("sound", "revive")
    SND_CURSE = d.id("sound", "curse")

    PORTRAIT_HECATIA = d.id("portrait", "Portrait/ars_hecatia")
    SPRITE_HECATIA = d.id("sprite", "Texture/ars_hecatia")


pc = d.chara("pc")
guide = d.chara("guide")
chicken = d.chara("demo_chicken")

can_start_followup = d.has_flag(IDs.FLAG_CAN_START_FOLLOWUP, expr=">=1", actor=pc)
day_elapsed = d.has_flag(IDs.FLAG_DAY_PASSED, expr=">=1", actor=pc)

d.node(
    "intro",
    d.transition(bg=IDs.BG_HALL, bgm=IDs.BGM_GUIDE, fade=0.6),
    d.quest_begin(IDs.QUEST_SHOWCASE, phase=1, journal=True),
    d.dialog(
        [
            d.line("案内役: ようこそ。V5.1のドラマデモを開始します。", actor=guide),
            d.line("案内役: 演出、NPC操作、クエスト分岐と合流を順に確認します。", actor=guide),
        ]
    ),
    d.go("main_menu"),
)

d.node(
    "main_menu",
    d.dialog(
        prompt=d.line("案内役: 見たいデモを選んでください。", actor=guide),
        choices=[
            d.option("演出デモ", to="fx_demo"),
            d.option("NPCデモ", to="npc_demo"),
            d.option("クエスト分岐デモ", to="branch_menu"),
            d.option("終了", to="ending"),
        ],
        cancel="ending",
    ),
)

d.node(
    "fx_demo",
    d.transition(bg=IDs.BG_GATE, bgm=IDs.BGM_TENSE, fade=0.5),
    d.say("案内役: まずPC演出を表示します。", actor=guide),
    d.effect(IDs.FX_TELEPORT, actor=pc),
    d.sound(IDs.SND_REVIVE),
    d.wait(2.0),
    d.say("案内役: 2秒待機したので演出確認がしやすくなります。", actor=guide),
    d.go("main_menu"),
)

d.node(
    "npc_demo",
    d.transition(bg=IDs.BG_FIELD, fade=0.5),
    d.say("案内役: 鶏をスポーンし、移動・見た目変更・効果を順に確認します。", actor=guide),
    d.spawn(chicken, chara_id=IDs.CHARA_CHICKEN),
    d.wait(1.0),
    d.say("案内役: 鶏を右へ3マス移動させます。", actor=guide),
    d.move_tile(chicken, dx=3, dy=0),
    d.wait(1.0),
    d.say("案内役: 次に鶏をプレイヤーの隣まで移動させます。", actor=guide),
    d.move_next_to(chicken, target=pc),
    d.wait(1.0),
    d.say("案内役: 鶏の見た目を変更します。", actor=guide),
    d.set_portrait(chicken, IDs.PORTRAIT_HECATIA),
    d.set_sprite(chicken, IDs.SPRITE_HECATIA),
    d.wait(1.0),
    d.say("案内役: 鶏からエモート・爆発エフェクト・呪いSEを再生します。", actor=guide),
    d.emote(chicken, emote_id=IDs.EMOTE_NOTE, duration=1.2),
    d.effect(IDs.FX_EXPLODE, actor=chicken),
    d.sound(IDs.SND_CURSE),
    d.wait(1.0),
    d.go("main_menu"),
)

d.node(
    "branch_menu",
    d.dialog(
        [
            d.line("案内役: ここでA/B分岐を選び、後段で1本に合流させます。", actor=guide),
            d.line("案内役: 経路差分はクエストphaseで可視化します。", actor=guide),
        ]
    ),
    d.dialog(
        prompt=d.line("案内役: ルートを選択してください。", actor=guide),
        choices=[
            d.option("ルートA（phase=10）", to="route_a"),
            d.option("ルートB（phase=20）", to="route_b"),
            d.option("メニューへ戻る", to="main_menu"),
        ],
        cancel="main_menu",
    ),
)

d.node(
    "route_a",
    d.quest_update(IDs.QUEST_SHOWCASE, phase=10, journal=True),
    d.say("案内役: ルートAを通過しました。", actor=guide),
    d.go("branch_merge"),
)

d.node(
    "route_b",
    d.quest_update(IDs.QUEST_SHOWCASE, phase=20, journal=True),
    d.say("案内役: ルートBを通過しました。", actor=guide),
    d.go("branch_merge"),
)

d.node(
    "branch_merge",
    d.dialog(
        [
            d.line("案内役: A/Bはここで合流します。", actor=guide),
            d.line("案内役: 次にフォローアップ開始条件を診断します。", actor=guide),
        ]
    ),
    d.when(
        d.all_of(can_start_followup, day_elapsed),
        then_to="followup_ready",
        else_to="followup_blocked",
    ),
)

d.node(
    "followup_blocked",
    d.dialog(
        [
            d.line("診断結果: フォローアップ開始条件は未成立です。", actor=guide),
            d.line("要件: can_start=1 かつ day_elapsed=1。", actor=guide),
            d.line("要件が揃ったら、再度この分岐に入ると成立側へ進みます。", actor=guide),
        ]
    ),
    d.go("main_menu"),
)

d.node(
    "followup_ready",
    d.dialog(
        [
            d.line("診断結果: フォローアップ開始条件は成立です。", actor=guide),
            d.line("前クエストの分岐経路から次クエストへの接続を確認できました。", actor=guide),
        ]
    ),
    d.quest_finish(IDs.QUEST_SHOWCASE, phase=999, journal=True),
    d.go("ending"),
)

d.node(
    "ending",
    d.transition(clear_bg=True, stop_bgm=True, fade=0.5),
    d.end(),
)

spec = d.story(
    start="intro",
    meta={
        "mod_name": "QuestMod",
        "title": "QuestMod Drama DSL V5.1 Concept",
        "strict": True,
    },
)

wb = compile_xlsx(spec, strict=True)
save_xlsx(wb, "quest_drama_feature_showcase_v5_1.xlsx")
