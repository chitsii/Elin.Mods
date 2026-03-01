"""
99_debug_menu.py - 自動生成デバッグメニュー

全てのドラマとバトルステージへのアクセスを提供。
新しいドラマ/バトルを追加すると自動的にメニューに反映される。

自動化の仕組み:
- ドラマ: ALL_DRAMA_IDS + get_drama_category() で動的カテゴリ分け
- バトル: battle_stages.py の辞書から自動取得
"""

from arena.builders import DramaBuilder
from arena.data import (
    ALL_DRAMA_IDS,
    DEBUG_STAGES,
    DRAMA_DISPLAY_NAMES,
    NORMAL_STAGES,
    RANK_UP_STAGES,
    Actors,
    DramaIds,
    FlagValues,
    Keys,
    get_drama_category,
)


def _get_dramas_by_category():
    """
    ALL_DRAMA_IDSをカテゴリ別に分類して返す

    Returns:
        dict: {'story': [...], 'rank': [...], 'character': [...]}
    """
    categorized = {"story": [], "rank": [], "character": []}

    for drama_id in ALL_DRAMA_IDS:
        # デバッグメニュー自体とメインドラマは除外
        if drama_id in (DramaIds.DEBUG_MENU, DramaIds.SUKUTSU_ARENA_MASTER):
            continue
        category = get_drama_category(drama_id)
        categorized[category].append(drama_id)

    return categorized


def define_debug_menu(builder: DramaBuilder):
    """
    デバッグメニュー定義

    全ドラマ/バトルへのアクセスを自動生成
    """
    # アクター
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    debug_master = builder.register_actor("sukutsu_debug_master", "観測者", "Observer")

    # メインラベル
    main = builder.label("main")
    main_menu = builder.label("main_menu")
    drama_menu = builder.label("drama_menu")
    battle_menu = builder.label("battle_menu")
    quest_menu = builder.label("quest_menu")
    items_menu = builder.label("items_menu")
    npc_menu = builder.label("npc_menu")
    flags_menu = builder.label("flags_menu")
    end = builder.label("end")

    # ドラマサブメニュー
    drama_story = builder.label("drama_story")
    drama_rank = builder.label("drama_rank")
    drama_char = builder.label("drama_char")

    # バトルサブメニュー
    battle_rank = builder.label("battle_rank")
    battle_normal = builder.label("battle_normal")
    battle_debug = builder.label("battle_debug")

    # ========================================
    # メイン
    # ========================================
    builder.step(main).say(
        "debug_welcome",
        "デバッグメニューへようこそ。何をテストしますか？",
        "Welcome to the debug menu. What would you like to test?",
        actor=debug_master,
    )

    builder.choice(drama_menu, "ドラマを再生", "Play Drama", text_id="c_drama").choice(
        battle_menu, "バトルを開始", "Start Battle", text_id="c_battle"
    ).choice(quest_menu, "クエスト操作", "Quest Operations", text_id="c_quest").choice(
        items_menu, "アイテム取得", "Get Items", text_id="c_items"
    ).choice(npc_menu, "NPC操作", "NPC Operations", text_id="c_npc").choice(
        flags_menu, "フラグ操作", "Flag Operations", text_id="c_flags"
    ).choice(end, "終了", "Exit", text_id="c_end")

    # main_menu ステップ（戻る先）
    builder.step(main_menu).say(
        "debug_main", "何をテストしますか？", "What would you like to test?", actor=debug_master
    )

    builder.choice(drama_menu, "ドラマを再生", "Play Drama", text_id="c_drama_2").choice(
        battle_menu, "バトルを開始", "Start Battle", text_id="c_battle_2"
    ).choice(quest_menu, "クエスト操作", "Quest Operations", text_id="c_quest_2").choice(
        items_menu, "アイテム取得", "Get Items", text_id="c_items_2"
    ).choice(npc_menu, "NPC操作", "NPC Operations", text_id="c_npc_2").choice(
        flags_menu, "フラグ操作", "Flag Operations", text_id="c_flags_2"
    ).choice(end, "終了", "Exit", text_id="c_end_2")

    # ========================================
    # ドラマメニュー（動的カテゴリ分け）
    # ========================================
    categorized_dramas = _get_dramas_by_category()

    builder.step(drama_menu).say(
        "drama_cat", "ドラマカテゴリを選択してください。", "Select a drama category.", actor=debug_master
    )

    builder.choice(drama_story, "ストーリー", "Story", text_id="c_drama_story").choice(
        drama_rank, "ランクアップ", "Rank Up", text_id="c_drama_rank"
    ).choice(drama_char, "キャラクター", "Character", text_id="c_drama_char").choice(
        main_menu, "戻る", "Back", text_id="c_drama_back"
    )

    # ストーリードラマ（自動取得）
    _build_drama_submenu(
        builder, drama_story, categorized_dramas["story"], drama_menu, debug_master
    )

    # ランクアップドラマ（自動取得）
    _build_drama_submenu(
        builder, drama_rank, categorized_dramas["rank"], drama_menu, debug_master
    )

    # キャラクタードラマ（自動取得）
    _build_drama_submenu(
        builder, drama_char, categorized_dramas["character"], drama_menu, debug_master
    )

    # ========================================
    # バトルメニュー（自動取得）
    # ========================================
    random_battle_menu = builder.label("random_battle_menu")

    builder.step(battle_menu).say(
        "battle_cat", "バトルカテゴリを選択してください。", "Select a battle category.", actor=debug_master
    )

    builder.choice(random_battle_menu, "ランダムバトル", "Random Battle", text_id="c_battle_random").choice(
        battle_rank, "ランクアップ試練", "Rank Up Trial", text_id="c_battle_rank"
    ).choice(
        battle_normal, "通常バトル", "Normal Battle", text_id="c_battle_normal"
    ).choice(battle_debug, "デバッグバトル", "Debug Battle", text_id="c_battle_debug").choice(
        main_menu, "戻る", "Back", text_id="c_battle_back"
    )

    # ========================================
    # ランダムバトルメニュー
    # ========================================
    random_preview = builder.label("random_preview")
    random_start = builder.label("random_start")
    random_set_deepest = builder.label("random_set_deepest")
    random_set_deepest_10 = builder.label("random_set_deepest_10")
    random_set_deepest_50 = builder.label("random_set_deepest_50")
    random_set_deepest_100 = builder.label("random_set_deepest_100")
    random_clear_override = builder.label("random_clear_override")
    random_set_gimmick = builder.label("random_set_gimmick")
    random_set_pattern = builder.label("random_set_pattern")

    builder.step(random_battle_menu).say(
        "random_menu_msg", "ランダムバトルテスト", "Random Battle Test", actor=debug_master
    )

    builder.choice(random_preview, "プレビュー（開始しない）", "Preview (no start)", text_id="c_random_preview").choice(
        random_start, "即時開始", "Start Now", text_id="c_random_start_debug"
    ).choice(random_set_deepest, "最深層オーバーライド", "Override Deepest Floor", text_id="c_random_deepest").choice(
        random_set_gimmick, "ギミック指定", "Set Gimmick", text_id="c_random_gimmick"
    ).choice(random_set_pattern, "パターン指定", "Set Pattern", text_id="c_random_pattern"
    ).choice(random_clear_override, "オーバーライドクリア", "Clear Overrides", text_id="c_random_clear").choice(
        battle_menu, "戻る", "Back", text_id="c_random_back"
    )

    # プレビュー
    builder.step(random_preview).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.PreviewRandomBattle();"
    ).say(
        "random_preview_done", "プレビュー情報をログに表示しました。", "Preview info written to log.", actor=debug_master
    ).jump(random_battle_menu)

    # 即時開始
    builder.step(random_start).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.StartDebugBattle();"
    ).finish()

    # 最深層オーバーライドサブメニュー
    builder.step(random_set_deepest).say(
        "random_deepest_msg", "テスト用の最深層を選択", "Select deepest floor for testing", actor=debug_master
    )

    builder.choice(random_set_deepest_10, "最深層=10", "Deepest=10", text_id="c_deepest_10").choice(
        random_set_deepest_50, "最深層=50", "Deepest=50", text_id="c_deepest_50"
    ).choice(random_set_deepest_100, "最深層=100", "Deepest=100", text_id="c_deepest_100").choice(
        random_battle_menu, "戻る", "Back", text_id="c_deepest_back"
    )

    builder.step(random_set_deepest_10).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetDeepestOverride(10);"
    ).say("deepest_10_set", "最深層を10に設定しました。", "Deepest floor set to 10.", actor=debug_master).jump(random_battle_menu)

    builder.step(random_set_deepest_50).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetDeepestOverride(50);"
    ).say("deepest_50_set", "最深層を50に設定しました。", "Deepest floor set to 50.", actor=debug_master).jump(random_battle_menu)

    builder.step(random_set_deepest_100).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetDeepestOverride(100);"
    ).say("deepest_100_set", "最深層を100に設定しました。", "Deepest floor set to 100.", actor=debug_master).jump(random_battle_menu)

    # オーバーライドクリア
    builder.step(random_clear_override).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.ClearAllOverrides();"
    ).say(
        "override_cleared", "全オーバーライドをクリアしました。", "All overrides cleared.", actor=debug_master
    ).jump(random_battle_menu)

    # ========================================
    # ギミック指定サブメニュー
    # ========================================
    gimmick_none = builder.label("gimmick_none")
    gimmick_antimagic = builder.label("gimmick_antimagic")
    gimmick_critical = builder.label("gimmick_critical")
    gimmick_hellish = builder.label("gimmick_hellish")
    gimmick_empathetic = builder.label("gimmick_empathetic")
    gimmick_chaos = builder.label("gimmick_chaos")
    gimmick_elemental = builder.label("gimmick_elemental")
    gimmick_nohealing = builder.label("gimmick_nohealing")
    gimmick_magicaffinity = builder.label("gimmick_magicaffinity")

    builder.step(random_set_gimmick).say(
        "gimmick_menu_msg", "強制するギミックを選択", "Select gimmick to force", actor=debug_master
    )

    builder.choice(gimmick_none, "なし（ランダム）", "None (Random)", text_id="c_gimmick_none").choice(
        gimmick_antimagic, "無法地帯 (AntiMagic)", "Lawless Zone (AntiMagic)", text_id="c_gimmick_antimagic"
    ).choice(gimmick_critical, "臨死の闘技場 (Critical)", "Near-Death Arena (Critical)", text_id="c_gimmick_critical"
    ).choice(gimmick_hellish, "地獄門 (Hellish)", "Hell Gate (Hellish)", text_id="c_gimmick_hellish"
    ).choice(gimmick_empathetic, "共感の場 (Empathetic)", "Empathy Field (Empathetic)", text_id="c_gimmick_empathetic"
    ).choice(gimmick_chaos, "混沌の爆発 (Chaos)", "Chaos Burst (Chaos)", text_id="c_gimmick_chaos"
    ).choice(gimmick_elemental, "属性傷 (ElementalScar)", "Elemental Scar (ElementalScar)", text_id="c_gimmick_elemental"
    ).choice(gimmick_nohealing, "禁忌の癒し (NoHealing)", "Forbidden Healing (NoHealing)", text_id="c_gimmick_nohealing"
    ).choice(gimmick_magicaffinity, "魔法親和 (MagicAffinity)", "Magic Affinity (MagicAffinity)", text_id="c_gimmick_magicaffinity"
    ).choice(random_battle_menu, "戻る", "Back", text_id="c_gimmick_back")

    # ギミック設定ステップ (ArenaGimmickType: 0=None, 1=AntiMagic, 2=Critical, 3=Hellish, 4=Empathetic, 5=Chaos, 6=ElementalScar, 7=NoHealing, 8=MagicAffinity)
    builder.step(gimmick_none).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(0);"
    ).say("gimmick_set_none", "ギミック強制を解除しました。", "Gimmick forcing disabled.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_antimagic).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(1);"
    ).say("gimmick_set_antimagic", "無法地帯を強制設定しました。", "Lawless Zone forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_critical).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(2);"
    ).say("gimmick_set_critical", "臨死の闘技場を強制設定しました。", "Near-Death Arena forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_hellish).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(3);"
    ).say("gimmick_set_hellish", "地獄門を強制設定しました。", "Hell Gate forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_empathetic).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(4);"
    ).say("gimmick_set_empathetic", "共感の場を強制設定しました。", "Empathy Field forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_chaos).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(5);"
    ).say("gimmick_set_chaos", "混沌の爆発を強制設定しました。", "Chaos Burst forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_elemental).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(6);"
    ).say("gimmick_set_elemental", "属性傷を強制設定しました。", "Elemental Scar forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_nohealing).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(7);"
    ).say("gimmick_set_nohealing", "禁忌の癒しを強制設定しました。", "Forbidden Healing forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(gimmick_magicaffinity).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForceGimmickByIndex(8);"
    ).say("gimmick_set_magicaffinity", "魔法親和を強制設定しました。", "Magic Affinity forced.", actor=debug_master).jump(random_battle_menu)

    # ========================================
    # パターン指定サブメニュー
    # ========================================
    pattern_random = builder.label("pattern_random")
    pattern_horde = builder.label("pattern_horde")
    pattern_mixed = builder.label("pattern_mixed")
    pattern_boss = builder.label("pattern_boss")
    pattern_clear = builder.label("pattern_clear")

    builder.step(random_set_pattern).say(
        "pattern_menu_msg", "強制する配置パターンを選択", "Select spawn pattern to force", actor=debug_master
    )

    builder.choice(pattern_clear, "なし（自動選択）", "None (Auto)", text_id="c_pattern_clear").choice(
        pattern_random, "Random（毎回異なる敵）", "Random (Varied enemies)", text_id="c_pattern_random"
    ).choice(pattern_horde, "Horde（群れ）", "Horde (Swarm)", text_id="c_pattern_horde"
    ).choice(pattern_mixed, "Mixed（混成）", "Mixed (Varied)", text_id="c_pattern_mixed"
    ).choice(pattern_boss, "BossWithMinions（ボス+取り巻き）", "BossWithMinions (Boss + adds)", text_id="c_pattern_boss"
    ).choice(random_battle_menu, "戻る", "Back", text_id="c_pattern_back")

    # パターン設定ステップ (SpawnPattern: 0=Random, 1=Horde, 2=Mixed, 3=BossWithMinions)
    builder.step(pattern_clear).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForcePatternByIndex(-1);"
    ).say("pattern_set_clear", "パターン強制を解除しました。", "Pattern forcing disabled.", actor=debug_master).jump(random_battle_menu)

    builder.step(pattern_random).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForcePatternByIndex(0);"
    ).say("pattern_set_random", "Randomパターンを強制設定しました。", "Random pattern forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(pattern_horde).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForcePatternByIndex(1);"
    ).say("pattern_set_horde", "Hordeパターンを強制設定しました。", "Horde pattern forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(pattern_mixed).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForcePatternByIndex(2);"
    ).say("pattern_set_mixed", "Mixedパターンを強制設定しました。", "Mixed pattern forced.", actor=debug_master).jump(random_battle_menu)

    builder.step(pattern_boss).action(
        "eval", param="Elin_SukutsuArena.Debugging.RandomBattleDebug.SetForcePatternByIndex(3);"
    ).say("pattern_set_boss", "BossWithMinionsパターンを強制設定しました。", "BossWithMinions pattern forced.", actor=debug_master).jump(random_battle_menu)

    # ランクアップバトル
    _build_battle_submenu(
        builder, battle_rank, RANK_UP_STAGES, battle_menu, debug_master
    )

    # 通常バトル
    _build_battle_submenu(
        builder, battle_normal, NORMAL_STAGES, battle_menu, debug_master
    )

    # デバッグバトル
    debug_only = {k: v for k, v in DEBUG_STAGES.items() if k.startswith("debug_")}
    _build_battle_submenu(builder, battle_debug, debug_only, battle_menu, debug_master)

    # ========================================
    # クエスト操作メニュー（新規）
    # ========================================
    quest_pet_unlock = builder.label("quest_pet_unlock")
    quest_rank_b = builder.label("quest_rank_b")
    quest_rank_a = builder.label("quest_rank_a")
    quest_all = builder.label("quest_all")
    quest_reset = builder.label("quest_reset")

    builder.step(quest_menu).say(
        "quest_info", "クエスト進行を操作します。", "Manage quest progression.", actor=debug_master
    )

    builder.choice(
        quest_pet_unlock, "ペット解禁のみ", "Pet Unlock Only", text_id="c_quest_pet"
    ).choice(quest_rank_b, "Bランク到達", "Reach Rank B", text_id="c_quest_b").choice(
        quest_rank_a, "Aランク到達", "Reach Rank A", text_id="c_quest_a"
    ).choice(quest_all, "全クエスト完了", "Complete All Quests", text_id="c_quest_all").choice(
        quest_reset, "クエストリセット", "Reset Quests", text_id="c_quest_reset"
    ).choice(main_menu, "戻る", "Back", text_id="c_quest_back")

    builder.step(quest_pet_unlock).action(
        "eval",
        param='Elin_SukutsuArena.DebugHelper.CompleteQuestsUpTo("18_last_battle");',
    ).say(
        "quest_pet_done",
        "18_last_battleまで完了しました。ペット化が解禁されました。",
        "Completed up to 18_last_battle. Pet recruitment unlocked.",
        actor=debug_master,
    ).jump(quest_menu)

    builder.step(quest_rank_b).action(
        "eval",
        param='Elin_SukutsuArena.DebugHelper.CompleteQuestsUpTo("12_rank_b_trial");',
    ).jump(quest_menu)

    builder.step(quest_rank_a).action(
        "eval",
        param='Elin_SukutsuArena.DebugHelper.CompleteQuestsUpTo("14_rank_a_trial");',
    ).say("quest_a_done", "Aランク到達まで完了しました。", "Completed up to Rank A.", actor=debug_master).jump(
        quest_menu
    )

    builder.step(quest_all).action(
        "eval",
        param="Elin_SukutsuArena.ArenaQuestManager.Instance.DebugCompleteAllQuests();",
    ).say("quest_all_done", "全クエストを完了しました。", "All quests completed.", actor=debug_master).jump(
        quest_menu
    )

    builder.step(quest_reset).action(
        "eval", param="Elin_SukutsuArena.DebugHelper.ResetAllQuests();"
    ).say(
        "quest_reset_done", "全クエストをリセットしました。", "All quests reset.", actor=debug_master
    ).jump(quest_menu)

    # ========================================
    # アイテム取得メニュー
    # ========================================
    item_makuma2 = builder.label("item_makuma2")
    item_lily_exp = builder.label("item_lily_exp")
    item_plat = builder.label("item_plat")
    item_resurrection = builder.label("item_resurrection")
    item_chickens = builder.label("item_chickens")

    builder.step(items_menu).say(
        "items_info", "クエスト用素材を取得します。", "Get quest materials.", actor=debug_master
    )

    builder.choice(
        item_resurrection,
        "蘇りの儀式素材（エリクシル×2+産卵薬×2）",
        "Resurrection Ritual Materials (Elixir x2 + Loveplus x2)",
        text_id="c_item_resurrection",
    ).choice(
        item_chickens,
        "鶏×2をパーティに追加",
        "Add Chicken x2 to Party",
        text_id="c_item_chickens",
    ).choice(
        item_makuma2,
        "虚空の心臓素材（心臓+ルーンモールド）",
        "Void Heart Materials (Heart + Rune Mold)",
        text_id="c_item_makuma2",
    ).choice(item_lily_exp, "残響の器素材（骨）", "Echo Vessel Materials (Bone)", text_id="c_item_lily_exp").choice(
        item_plat, "プラチナコイン×10", "Platinum Coin x10", text_id="c_item_plat"
    ).choice(main_menu, "戻る", "Back", text_id="c_items_back")

    # 蘇りの儀式素材: 不老長寿のエリクシル×2 + 産卵薬×2
    # アイテムID: 1264 / 1254 (バニラ)
    builder.step(item_resurrection).cs_eval(
        'for(int i=0; i<2; i++) { EClass.pc.Pick(ThingGen.Create("1264")); }'
    ).cs_eval(
        'for(int i=0; i<2; i++) { EClass.pc.Pick(ThingGen.Create("1254")); }'
    ).say(
        "item_resurrection_got",
        "不老長寿のエリクシル×2と産卵薬×2を取得しました。",
        "Obtained Elixir of Eternal Youth x2 and Love Plus Potion x2.",
        actor=debug_master,
    ).jump(items_menu)

    # 鶏×2をパーティに追加
    builder.step(item_chickens).action(
        "modInvoke", param="add_chickens_to_party(2)", actor="pc"
    ).say(
        "item_chickens_got",
        "鶏×2をパーティに追加しました。",
        "Added Chicken x2 to party.",
        actor=debug_master,
    ).jump(items_menu)

    # makuma2素材: 心臓×1 + ルーンモールド×1
    builder.step(item_makuma2).cs_eval(
        'EClass.pc.Pick(ThingGen.Create("heart"));'
    ).cs_eval('EClass.pc.Pick(ThingGen.Create("rune_mold_earth"));').say(
        "item_makuma2_got",
        "心臓×1とルーンモールド（大地）×1を取得しました。",
        "Obtained Heart x1 and Rune Mold (Earth) x1.",
        actor=debug_master,
    ).jump(items_menu)

    # lily_experiment素材: 骨×1
    builder.step(item_lily_exp).cs_eval('EClass.pc.Pick(ThingGen.Create("bone"));').say(
        "item_lily_exp_got", "骨×1を取得しました。", "Obtained Bone x1.", actor=debug_master
    ).jump(items_menu)

    # プラチナコイン×10
    builder.step(item_plat).cs_eval(
        'for(int i=0; i<10; i++) { EClass.pc.Pick(ThingGen.Create("plat")); }'
    ).say(
        "item_plat_got", "プラチナコイン×10を取得しました。", "Obtained Platinum Coin x10.", actor=debug_master
    ).jump(items_menu)

    # ========================================
    # NPC操作メニュー（新規）
    # ========================================
    npc_status = builder.label("npc_status")
    npc_hide_nul = builder.label("npc_hide_nul")
    npc_hide_astaroth = builder.label("npc_hide_astaroth")
    npc_restore_all = builder.label("npc_restore_all")
    npc_bad_end = builder.label("npc_bad_end")
    npc_flag_status = builder.label("npc_flag_status")

    builder.step(npc_menu).say(
        "npc_info", "NPC状態を操作します。", "Manage NPC states.", actor=debug_master
    )

    builder.choice(npc_status, "NPC状態確認", "Check NPC Status", text_id="c_npc_status").choice(
        npc_hide_nul, "Nul非表示", "Hide Nul", text_id="c_npc_hide_nul"
    ).choice(npc_hide_astaroth, "Astaroth非表示", "Hide Astaroth", text_id="c_npc_hide_ast").choice(
        npc_restore_all, "全NPC再表示", "Restore All NPCs", text_id="c_npc_restore"
    ).choice(npc_bad_end, "バッドエンドシミュ", "Bad Ending Simulation", text_id="c_npc_bad").choice(
        npc_flag_status, "フラグ状態確認", "Check Flag Status", text_id="c_npc_flags"
    ).choice(main_menu, "戻る", "Back", text_id="c_npc_back")

    builder.step(npc_status).action(
        "eval", param="Elin_SukutsuArena.DebugHelper.ShowNpcStatus();"
    ).say(
        "npc_status_done", "NPC状態をログに出力しました。", "NPC status written to log.", actor=debug_master
    ).jump(npc_menu)

    builder.step(npc_hide_nul).action(
        "modInvoke", param="hide_npc(sukutsu_null)", actor="pc"
    ).say("npc_hide_nul_done", "Nulを非表示にしました。", "Nul hidden.", actor=debug_master).jump(
        npc_menu
    )

    builder.step(npc_hide_astaroth).action(
        "modInvoke", param="hide_npc(sukutsu_astaroth)", actor="pc"
    ).say(
        "npc_hide_ast_done", "Astarothを非表示にしました。", "Astaroth hidden.", actor=debug_master
    ).jump(npc_menu)

    builder.step(npc_restore_all).action(
        "eval", param="Elin_SukutsuArena.DebugHelper.RestoreAllNpcs();"
    ).say(
        "npc_restore_done", "全NPCをアリーナに再表示しました。", "All NPCs restored to arena.", actor=debug_master
    ).jump(npc_menu)

    builder.step(npc_bad_end).set_flag(
        Keys.BALGAS_KILLED, FlagValues.BalgasChoice.KILLED
    ).say(
        "npc_bad_done",
        "バッドエンドフラグを設定しました（バルガス殺害）",
        "Bad ending flags set (Vargus killed).",
        actor=debug_master,
    ).jump(npc_menu)

    builder.step(npc_flag_status).action(
        "eval", param="Elin_SukutsuArena.DebugHelper.ShowFlagStatus();"
    ).say(
        "npc_flag_done", "フラグ状態をログに出力しました。", "Flag status written to log.", actor=debug_master
    ).jump(npc_menu)

    # ========================================
    # フラグ操作メニュー
    # ========================================
    set_rank_s = builder.label("set_rank_s")
    set_all_quests = builder.label("set_all_quests")
    scenario_flags = builder.label("scenario_flags")

    # リセットメニュー用ラベル
    reset_menu = builder.label("reset_menu")
    reset_newgame = builder.label("reset_newgame")
    reset_newgame_confirm = builder.label("reset_newgame_confirm")
    reset_uninstall = builder.label("reset_uninstall")
    reset_uninstall_confirm = builder.label("reset_uninstall_confirm")

    set_postgame = builder.label("set_postgame")

    builder.step(flags_menu).say(
        "flags_info", "フラグ操作を選択してください。", "Select flag operation.", actor=debug_master
    )

    builder.choice(set_rank_s, "ランクSに設定", "Set Rank S", text_id="c_set_rank_s").choice(
        set_postgame, "Postgame状態に設定", "Set Postgame State", text_id="c_set_postgame"
    ).choice(
        scenario_flags, "シナリオ分岐フラグ", "Scenario Branch Flags", text_id="c_scenario_flags"
    ).choice(
        reset_menu, "データリセット", "Data Reset", text_id="c_reset_menu"
    ).choice(main_menu, "戻る", "Back", text_id="c_flags_back")

    # Postgame状態に設定（アスタロト打倒後、クリア後クエストテスト用）
    builder.step(set_postgame).action(
        "eval", param="Elin_SukutsuArena.DebugHelper.SetPostgame();"
    ).say(
        "postgame_set",
        "Postgame状態に設定しました（全メインクエスト完了、ランクSS）",
        "Set to Postgame state (all main quests completed, Rank SS).",
        actor=debug_master,
    ).jump(flags_menu)

    # ランクSに設定するには、全ランクアップクエストを完了させる
    builder.step(set_rank_s).action(
        "eval", param="Elin_SukutsuArena.DebugHelper.SetRankS();"
    ).say(
        "rank_set",
        "ランクをSに設定しました（全ランクアップクエスト完了）",
        "Rank set to S (all rank-up quests completed).",
        actor=debug_master,
    ).jump(flags_menu)

    # ========================================
    # シナリオ分岐フラグメニュー
    # ========================================
    set_balgas_killed = builder.label("set_balgas_killed")
    set_balgas_alive = builder.label("set_balgas_alive")
    reset_all_flags = builder.label("reset_all_flags")

    builder.step(scenario_flags).say(
        "scenario_info", "シナリオ分岐フラグを設定します。", "Set scenario branch flags.", actor=debug_master
    )

    builder.choice(
        set_balgas_killed, "バルガス殺害ON", "Vargus Killed ON", text_id="c_balgas_killed"
    ).choice(set_balgas_alive, "バルガス殺害OFF", "Vargus Killed OFF", text_id="c_balgas_alive").choice(
        reset_all_flags, "全てリセット", "Reset All", text_id="c_reset_flags"
    ).choice(flags_menu, "戻る", "Back", text_id="c_scenario_back")

    builder.step(set_balgas_killed).set_flag(
        Keys.BALGAS_KILLED, FlagValues.BalgasChoice.KILLED
    ).say(
        "balgas_killed_set",
        "バルガス殺害フラグをONにしました。",
        "Vargus killed flag set to ON.",
        actor=debug_master,
    ).jump(scenario_flags)

    builder.step(set_balgas_alive).set_flag(
        Keys.BALGAS_KILLED, FlagValues.BalgasChoice.SPARED
    ).say(
        "balgas_alive_set",
        "バルガス殺害フラグをOFFにしました。",
        "Vargus killed flag set to OFF.",
        actor=debug_master,
    ).jump(scenario_flags)

    builder.step(reset_all_flags).set_flag(
        Keys.BALGAS_KILLED, FlagValues.BalgasChoice.SPARED
    ).say(
        "flags_reset", "全フラグをリセットしました。", "All flags reset.", actor=debug_master
    ).jump(scenario_flags)

    # ========================================
    # データリセットメニュー
    # ========================================
    builder.step(reset_menu).say(
        "reset_info",
        "リセット操作を選択してください。この操作は取り消せません。",
        "Select reset operation. This cannot be undone.",
        actor=debug_master,
    )

    builder.choice(
        reset_newgame, "周回リセット（NewGame+）", "New Game+ Reset", text_id="c_reset_ng"
    ).choice(
        reset_uninstall, "Mod削除準備（Uninstall）", "Uninstall Preparation", text_id="c_reset_un"
    ).choice(flags_menu, "戻る", "Back", text_id="c_reset_back")

    # NewGame+ 確認
    builder.step(reset_newgame).say(
        "reset_ng_warn",
        "ストーリー・クエストをリセットします。ランクは保持されます。ペット化NPCは離脱します。実行しますか？",
        "Reset story and quests. Rank is preserved. Pet NPCs will leave. Proceed?",
        actor=debug_master,
    )

    builder.choice(
        reset_newgame_confirm, "実行する", "Execute", text_id="c_reset_ng_yes"
    ).choice(reset_menu, "キャンセル", "Cancel", text_id="c_reset_ng_no")

    builder.step(reset_newgame_confirm).action(
        "eval", param="Elin_SukutsuArena.Reset.ArenaResetManager.ExecuteNewGamePlus();"
    ).say(
        "reset_ng_done",
        "周回リセットが完了しました。",
        "New Game+ reset complete.",
        actor=debug_master,
    ).jump(flags_menu)

    # Uninstall 確認（アリーナ内チェックはC#側で実行）
    builder.step(reset_uninstall).say(
        "reset_un_warn",
        "全てのModデータを削除します。フィート・ゾーンも削除されます。Mod削除前にのみ使用してください。実行しますか？",
        "Delete all mod data including feats and zone. Use only before uninstalling mod. Proceed?",
        actor=debug_master,
    )

    builder.choice(
        reset_uninstall_confirm, "実行する", "Execute", text_id="c_reset_un_yes"
    ).choice(reset_menu, "キャンセル", "Cancel", text_id="c_reset_un_no")

    builder.step(reset_uninstall_confirm).action(
        "eval", param="Elin_SukutsuArena.Reset.ArenaResetManager.ExecuteUninstall();"
    ).say(
        "reset_un_done",
        "Mod削除準備が完了しました。ゲームを再起動してください。",
        "Uninstall preparation complete. Please restart the game.",
        actor=debug_master,
    ).finish()

    # ========================================
    # 終了
    # ========================================
    builder.step(end).say(
        "debug_bye", "デバッグメニューを終了します。", "Closing debug menu.", actor=debug_master
    ).finish()


def _build_drama_submenu(
    builder: DramaBuilder, entry_label: str, drama_ids: list, back_label: str, actor
):
    """ドラマサブメニューを構築"""
    menu_label = f"{entry_label}_choice"

    builder.step(entry_label).say(
        f"{entry_label}_msg", "再生するドラマを選択してください。", "Select a drama to play.", actor=actor
    ).jump(menu_label)

    # 選択肢を構築
    choice_labels = []
    for drama_id in drama_ids:
        label = f"play_{drama_id}"
        jp_name, en_name = DRAMA_DISPLAY_NAMES.get(drama_id, (drama_id, drama_id))
        choice_labels.append((label, jp_name, en_name))

    # 最初の選択肢
    if choice_labels:
        first = choice_labels[0]
        b = builder.choice(first[0], first[1], first[2], text_id=f"c_{first[0]}")

        # 残りの選択肢
        for label, jp, en in choice_labels[1:]:
            b.choice(label, jp, en, text_id=f"c_{label}")

        # 戻るボタン
        b.choice(back_label, "戻る", "Back", text_id=f"c_back_{entry_label}")

    # 各ドラマ再生ステップ
    for drama_id in drama_ids:
        label = f"play_{drama_id}"
        drama_name = f"drama_{drama_id}"
        builder.step(label)._start_drama(drama_name).finish()


def _build_battle_submenu(
    builder: DramaBuilder, entry_label: str, stages: dict, back_label: str, actor
):
    """バトルサブメニューを構築"""
    menu_label = f"{entry_label}_choice"

    builder.step(entry_label).say(
        f"{entry_label}_msg", "バトルを選択してください。", "Select a battle.", actor=actor
    ).jump(menu_label)

    # 選択肢を構築
    choice_labels = []
    for stage_id, stage in stages.items():
        label = f"fight_{stage_id}"
        choice_labels.append(
            (label, stage.display_name_jp, stage.display_name_en, stage_id)
        )

    # 最初の選択肢
    if choice_labels:
        first = choice_labels[0]
        b = builder.choice(first[0], first[1], first[2], text_id=f"c_{first[0]}")

        # 残りの選択肢
        for label, jp, en, _ in choice_labels[1:]:
            b.choice(label, jp, en, text_id=f"c_{label}")

        # 戻るボタン
        b.choice(back_label, "戻る", "Back", text_id=f"c_back_{entry_label}")

    # 各バトル開始ステップ
    for label, _, _, stage_id in choice_labels:
        builder.step(label).start_battle_by_stage(stage_id).finish()
