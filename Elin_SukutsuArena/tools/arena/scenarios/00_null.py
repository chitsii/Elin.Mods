# -*- coding: utf-8 -*-
"""
00_null.py - ヌル（暗殺人形）のメインダイアログ

NPCクリック時の会話処理
- メインクエスト完了前（VS_ASTAROTH前）のみ会話可能
- FUGITIVE状態後はロビーから不在（会話不可）
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QUEST_DONE_PREFIX, QuestIds


def define_null_main_drama(builder: DramaBuilder):
    """
    ヌルのメインダイアログ
    NPCクリック時に表示される会話
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    nul = builder.register_actor(Actors.NUL, "ヌル", "Nul")

    # ========================================
    # ラベル定義
    # ========================================
    main = builder.label("main")
    end = builder.label("end")
    greeting = builder.label("greeting")
    choices = builder.label("choices")
    ask_role = builder.label("ask_role")
    ask_self = builder.label("ask_self")
    faint_flicker = builder.label("faint_flicker")

    # エピローグ後ラベル
    post_game = builder.label("post_game")
    post_game_choices = builder.label("post_game_choices")
    quest_done_last_battle = f"{QUEST_DONE_PREFIX}{QuestIds.LAST_BATTLE}"

    # ========================================
    # エントリーポイント
    # ========================================
    # エピローグ完了後は専用フローへ（FUGITIVEチェックより優先）
    # FUGITIVE状態（VS_ASTAROTH後、エピローグ前）は会話不可
    builder.step(main).branch_if(
        quest_done_last_battle, "==", 1, post_game
    ).branch_if(Keys.FUGITIVE, "==", FlagValues.TRUE, end).jump(greeting)

    # ========================================
    # 挨拶（固定・ランク変化なし）
    # ========================================
    builder.step(greeting).say("greet_1", "……。", "...", "……。", actor=nul).say(
        "greet_2", "……何か、用ですか。", "...Do you... need something?", "……有什么……事吗。", actor=nul
    ).jump(choices)

    # ========================================
    # 選択肢（最小限）
    # ========================================
    builder.step(choices).choice(
        ask_role, "お前の役割は？", "What is your role?", "你的职责是什么？", text_id="c_role"
    ).choice(ask_self, "お前は何者だ？", "Who are you?", "你是什么人？", text_id="c_self").choice(
        end, "（立ち去る）", "(Leave)", "（离开）", text_id="c_leave"
    ).on_cancel(end)

    # ========================================
    # 役割について
    # ========================================
    builder.step(ask_role).say("role_1", "……私の……機能。", "...My... function.", "……我的……功能。", actor=nul).say(
        "role_2", "敗北した闘士を……回収すること。", "...To collect... defeated fighters.", "……回收……战败的斗士。", actor=nul
    ).say("role_3", "死体の……処理。……それだけ。", "...Disposing of... corpses. ...That's all.", "……处理……尸体。……仅此而已。", actor=nul).say(
        "role_4", "……それ以上は……ない。", "...Nothing... more.", "……没有……其他的了。", actor=nul
    ).jump(faint_flicker)

    # ========================================
    # 自身について
    # ========================================
    builder.step(ask_self).say("self_1", "……私が……何者か。", "...Who... I am.", "……我是……什么人。", actor=nul).say(
        "self_2", "……分からない。", "...I don't know.", "……不知道。", actor=nul
    ).say("self_3", "『ヌル』……『無』という意味……らしい。", "...'Nul'... It means... 'nothing'... apparently.", "……『Nul』……好像是……『空无』的意思。", actor=nul).say(
        "self_4", "アスタロト様が……そう呼んだ。", "...Lord Astaroth... called me that.", "……阿斯塔罗特大人……这样叫我。", actor=nul
    ).say("self_5", "……私には……名前がない。……思い出せない。", "...I have... no name. ...I can't remember.", "……我没有……名字。……想不起来。", actor=nul).say(
        "self_6", "……以前は……あったかもしれない。", "...Maybe... I had one... before.", "……以前……也许有过。", actor=nul
    ).say("self_7", "……でも……関係ない。", "...But... it doesn't matter.", "……但是……无所谓了。", actor=nul).jump(faint_flicker)

    # ========================================
    # 微かな揺らぎ（伏線）
    # ========================================
    # ランクC以上（ある程度進んだプレイヤー）に対してのみ表示
    # player.rank: 0=UNRANKED, 1=G, 2=F, 3=E, 4=D, 5=C, 6=B, ...
    flicker_show = builder.label("flicker_show")
    flicker_skip = builder.label("flicker_skip")

    builder.step(faint_flicker).branch_if(
        "player.rank",
        ">=",
        5,
        flicker_show,  # ランクC以上
    ).jump(flicker_skip)

    builder.step(flicker_show).say("flicker_1", "……あなたは……。", "...You...", "……你……。", actor=nul).say(
        "flicker_2", "……いえ。……何でもない。", "...No. ...It's nothing.", "……不。……没什么。", actor=nul
    ).say("flicker_3", "……帰って……ください。", "...Please... go.", "……请……回去吧。", actor=nul).jump(end)

    builder.step(flicker_skip).say("flicker_skip", "……。", "...", "……。", actor=nul).jump(choices)

    # ========================================
    # エピローグ後の会話
    # ========================================
    builder.step(post_game).say(
        "pg_greet_1",
        "……あなた。",
        "...You.",
        "……你。",
        actor=nul,
    ).say(
        "pg_greet_2",
        "……また……会えた。",
        "...We meet... again.",
        "……又……见面了。",
        actor=nul,
    ).jump(post_game_choices)

    # パーティメンバー用選択肢
    # inject_unique(): バニラの_invite, _joinParty, _leaveParty, _buy, _heal等を追加
    builder.step(post_game_choices).inject_unique().choice(
        end,
        "また今度",
        "Maybe next time",
        "下次吧",
        text_id="c_pg_bye",
    ).on_cancel(end)

    # ========================================
    # 終了
    # ========================================
    builder.step(end).finish()
