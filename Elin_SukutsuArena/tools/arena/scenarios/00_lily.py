"""
00_lily.py - リリィ（受付嬢）のメインダイアログ
NPCクリック時の会話処理
"""

from arena.builders import ArenaDramaBuilder
from arena.data import (
    Actors,
    DramaNames,
    QUEST_DONE_PREFIX,
    QuestEntry,
    QuestIds,
)


# リリィのクエストエントリ定義
# 順序: 前提クエストの進行度順（早い方が先）
LILY_QUESTS = [
    QuestEntry(QuestIds.LILY_EXPERIMENT, 22, "start_lily_experiment"),  # RANK_UP_F?
    QuestEntry(QuestIds.LILY_PRIVATE, 26, "start_lily_private"),  # RANK_UP_D?
    QuestEntry(QuestIds.MAKUMA, 28, "start_makuma"),  # RANK_UP_B???????
    QuestEntry(QuestIds.MAKUMA2, 29, "start_makuma2"),  # MAKUMA?
    QuestEntry(QuestIds.LILY_REAL_NAME, 31, "start_lily_real_name"),  # RANK_UP_S_BALGAS_SPARED?
    # Postgame
    QuestEntry(
        QuestIds.PG_02A_RESURRECTION_INTRO, 40, "start_resurrection_intro"
    ),  # LAST_BATTLE?
    QuestEntry(
        QuestIds.PG_02B_RESURRECTION_EXECUTION, 42, "start_resurrection_execution"
    ),  # PG_02A?
    QuestEntry(QuestIds.PG_03_SCROLL_SHOWCASE, 43, "start_scroll_showcase"),
]


def define_lily_main_drama(builder: ArenaDramaBuilder):
    """
    リリィのメインダイアログ
    NPCクリック時に表示される会話
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")

    # ラベル定義
    main = builder.label("main")
    greeting = builder.label("greeting")
    choices = builder.label("choices")
    check_quests = builder.label("check_quests")
    quest_none = builder.label("quest_none")
    end = builder.label("end")

    # エピローグ後ラベル
    post_game = builder.label("post_game")
    post_game_choices = builder.label("post_game_choices")
    quest_done_last_battle = f"{QUEST_DONE_PREFIX}{QuestIds.LAST_BATTLE}"

    # ========================================
    # エントリーポイント（エピローグ完了チェック）
    # ========================================
    builder.step(main).branch_if(
        quest_done_last_battle, "==", 1, post_game
    ).jump(greeting)

    # ========================================
    # 挨拶
    # ========================================
    builder.step(greeting).action(
        "modInvoke", param="update_lily_shop_stock()", actor="pc"
    ).say(
        "greet",
        "いらっしゃいませ。何かお手伝いできることはありますか？",
        "Welcome. How may I assist you today?",
        "欢迎光临。有什么可以为您效劳的吗？",
        actor=lily,
    ).jump(choices)

    # ========================================
    # 選択肢
    # ========================================
    builder.step(choices).choice(
        builder.label("_buy"), "商品を見る", "Browse your wares", "看看商品", text_id="c_buy"
    ).choice(
        builder.label("_identifyAll"), "全て鑑定", "Identify all", "全部鉴定", text_id="c_identify"
    ).choice(
        check_quests, "（イベントを開始）", "(Start an event)", "（开始事件）", text_id="c_event"
    ).choice(
        end, "また今度", "Perhaps another time", "下次再说", text_id="c_bye"
    ).on_cancel(end)

    # ========================================
    # クエストディスパッチ（高レベルAPI使用）
    # ========================================
    quest_labels = builder.build_quest_dispatcher(
        LILY_QUESTS,
        entry_step=check_quests,
        fallback_step=quest_none,
        actor=lily,
    )

    # クエストが見つからなかった場合 → 選択肢に戻る（クリア前/後で分岐）
    builder.step(quest_none).say(
        "quest_none", "……あら、今は特にお伝えすることがないみたいです。", "...Oh my, it seems there's nothing particular to share at the moment.", "……哎呀，目前似乎没有什么特别的事情要告诉您呢。", actor=lily
    ).branch_if(
        quest_done_last_battle, "==", 1, post_game_choices
    ).jump(choices)

    # 各クエスト開始 → ドラマ遷移
    builder.step(quest_labels["start_lily_experiment"]).start_quest_drama(
        QuestIds.LILY_EXPERIMENT, DramaNames.LILY_EXPERIMENT
    )

    builder.step(quest_labels["start_lily_private"]).start_quest_drama(
        QuestIds.LILY_PRIVATE, DramaNames.LILY_PRIVATE
    )

    builder.step(quest_labels["start_makuma"]).start_quest_drama(
        QuestIds.MAKUMA, DramaNames.MAKUMA
    )

    builder.step(quest_labels["start_makuma2"]).start_quest_drama(
        QuestIds.MAKUMA2, DramaNames.MAKUMA2
    )

    builder.step(quest_labels["start_lily_real_name"]).start_quest_drama(
        QuestIds.LILY_REAL_NAME, DramaNames.LILY_REAL_NAME
    )

    # Postgame クエスト
    builder.step(quest_labels["start_resurrection_intro"]).start_quest_drama(
        QuestIds.PG_02A_RESURRECTION_INTRO, DramaNames.RESURRECTION_INTRO
    )

    builder.step(quest_labels["start_resurrection_execution"]).start_quest_drama(
        QuestIds.PG_02B_RESURRECTION_EXECUTION, DramaNames.RESURRECTION_EXECUTION
    )

    builder.step(quest_labels["start_scroll_showcase"]).start_quest_drama(
        QuestIds.PG_03_SCROLL_SHOWCASE, DramaNames.P2_03_SCROLL_SHOWCASE
    )

    # ========================================
    # エピローグ後の会話
    # ========================================
    # フェーズ復元とクエスト利用可能性チェック（既存セーブ救済用）
    builder.step(post_game).action(
        "modInvoke", param="update_lily_shop_stock()", actor="pc"
    ).check_available_quests_for_npc(
        "sukutsu_receptionist"
    ).say(
        "pg_greet",
        "自由な世界……まだ慣れないけれど、あなたと一緒なら、どこへでも行けそうな気がします。",
        "A free world... I'm still not used to it, but with you, I feel like I could go anywhere.",
        "自由的世界……虽然还不太习惯，但和您在一起的话，感觉哪里都可以去。",
        actor=lily,
    ).jump(post_game_choices)

    # パーティメンバー用選択肢
    # inject_unique(): バニラの_invite, _joinParty, _leaveParty, _buy, _heal等を追加
    builder.step(post_game_choices).inject_unique().choice(
        check_quests,
        "（イベントを開始）",
        "(Start an event)",
        "（开始事件）",
        text_id="c_pg_event",
    ).choice(
        end,
        "また話そう",
        "Let's talk again later",
        "下次再聊",
        text_id="c_pg_bye",
    ).on_cancel(end)

    # ========================================
    # 終了
    # ========================================
    builder.step(end).finish()
