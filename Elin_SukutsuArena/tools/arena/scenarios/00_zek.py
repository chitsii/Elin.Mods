"""
00_zek.py - ゼク（怪しい商人）のメインダイアログ
NPCクリック時の会話処理
"""

from arena.builders import ArenaDramaBuilder
from arena.data import (
    Actors,
    DramaNames,
    QUEST_DONE_PREFIX,
    QuestBattleFlags,
    QuestEntry,
    QuestIds,
    SessionKeys,
)


# ゼクのクエストエントリ定義
ZEK_QUESTS = [
    QuestEntry(QuestIds.ZEK_INTRO, 21, "start_zek_intro"),
    QuestEntry(QuestIds.ZEK_STEAL_BOTTLE, 23, "start_zek_steal_bottle"),
    QuestEntry(QuestIds.ZEK_STEAL_SOULGEM, 24, "start_zek_steal_soulgem"),
    QuestEntry(QuestIds.LAST_BATTLE, 33, "start_last_battle"),
]


def define_zek_main_drama(builder: ArenaDramaBuilder):
    """
    ゼクのメインダイアログ
    NPCクリック時に表示される会話
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")

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

    # クエストバトル結果ラベル
    quest_battle_result_check = builder.label("quest_battle_result_check")
    last_battle_victory = builder.label("last_battle_victory")
    last_battle_defeat = builder.label("last_battle_defeat")

    # 商品紹介ラベル
    rec_intro = builder.label("rec_intro")
    rec_menu = builder.label("rec_menu")
    rec_res = builder.label("rec_res")
    rec_boost = builder.label("rec_boost")
    rec_eq = builder.label("rec_eq")
    rec_return = builder.label("rec_return")
    rec_end = builder.label("rec_end")

    # ========================================
    # エントリーポイント（クエストバトル結果 → エピローグ完了チェック）
    # ========================================
    builder.step(main).branch_if(
        SessionKeys.IS_QUEST_BATTLE_RESULT, "==", 1, quest_battle_result_check
    ).branch_if(
        quest_done_last_battle, "==", 1, post_game
    ).jump(greeting)

    # ========================================
    # クエストバトル結果処理
    # ========================================
    builder.step(quest_battle_result_check).set_flag(
        SessionKeys.IS_QUEST_BATTLE_RESULT, 0
    ).branch_if(
        QuestBattleFlags.FLAG_NAME, "==", QuestBattleFlags.LAST_BATTLE, last_battle_victory
    ).jump(greeting)  # last_battle以外の場合は通常の挨拶へ

    # last_battle勝利 → エピローグドラマを開始
    builder.step(last_battle_victory).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).switch_flag(
        SessionKeys.ARENA_RESULT,
        [
            greeting,  # 0: 未設定（通常の挨拶へ）
            builder.label("trigger_epilogue_victory"),  # 1: 勝利
            builder.label("trigger_epilogue_defeat"),  # 2: 敗北
        ],
    )

    # エピローグドラマ開始（勝利）
    builder.step(builder.label("trigger_epilogue_victory")).action(
        "modInvoke",
        param=f"start_drama({DramaNames.EPILOGUE})",
        actor="pc",
    ).finish()

    # 最終決戦 敗北処理
    # 敗北時はフラグをクリアして再挑戦できるようにする
    # 注: エピローグドラマは勝利専用なので呼び出さない
    narrator = Actors.NARRATOR
    astaroth = builder.register_actor(Actors.ASTAROTH, "アスタロト", "Astaroth")
    builder.step(builder.label("trigger_epilogue_defeat")).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).set_flag(
        SessionKeys.ARENA_RESULT, 0
    ).set_flag(
        SessionKeys.IS_QUEST_BATTLE_RESULT, 0
    ).play_bgm("BGM/Lobby_Normal").say(
        "narr_d1",
        "アスタロトの圧倒的な力の前に、あなたは膝をついた。",
        "Before Astaroth's overwhelming power, you fall to your knees.",
        "在阿斯塔罗特压倒性的力量面前，你跪倒了。",
        actor=narrator,
    ).say(
        "astaroth_d1",
        "「……まだ、足りないな。お前の中に宿る可能性は、未だ開花していない。」",
        "\"...Still not enough. The potential dwelling within thee has yet to bloom.\"",
        "「……还不够。栖息于汝身中的可能性，尚未绽放。」",
        actor=astaroth,
    ).say(
        "astaroth_d2",
        "「……出直して来い。私は、お前が『完成形』に至るまで待っていよう。」",
        "\"...Return when thou art ready. I shall wait until thou reachest thy 'completed form.'\"",
        "「……回去再来吧。吾将等待，直到汝达成『完成形态』。」",
        actor=astaroth,
    ).say(
        "narr_d2",
        "あなたは闘技場へと戻された。再び挑戦するには、さらなる鍛錬が必要だ……。",
        "You are returned to the arena. Further training is needed to challenge him again...",
        "你被送回了角斗场。要再次挑战的话，还需要更多的锻炼……",
        actor=narrator,
    ).finish()

    # ========================================
    # 挨拶
    # ========================================
    builder.step(greeting).say(
        "greet", "おや……何か御用でしょうか？", "Oh my... Is there something I can help you with?", "哎呀……有什么需要的吗？", actor=zek
    ).jump(choices)

    # ========================================
    # 選択肢
    # ========================================
    builder.step(choices).choice(
        builder.label("_buy"), "商品を見る", "Browse your wares", "看看商品", text_id="c_buy"
    ).choice(
        rec_intro, "おすすめの商品は？", "Any recommendations?", "有什么推荐の吗？", text_id="c_recommend"
    ).choice(
        check_quests, "（イベントを開始）", "(Start an event)", "（开始事件）", text_id="c_event"
    ).choice(
        end, "また今度", "Perhaps another time", "下次再说", text_id="c_bye"
    ).on_cancel(end)

    # ========================================
    # クエストディスパッチ（高レベルAPI使用）
    # ========================================
    quest_labels = builder.build_quest_dispatcher(
        ZEK_QUESTS,
        entry_step=check_quests,
        fallback_step=quest_none,
        actor=zek,
    )

    # クエストが見つからなかった場合 → 選択肢に戻る
    builder.step(quest_none).say(
        "quest_none", "……おや、今は特にお伝えすることがないようです。", "...Oh my, it seems I have nothing particular to share with you at the moment.", "……哎呀，目前似乎没有什么特别的事情要告诉您呢。", actor=zek
    ).jump(choices)

    # 各クエスト開始 → ドラマ遷移
    builder.step(quest_labels["start_zek_intro"]).start_quest_drama(
        QuestIds.ZEK_INTRO, DramaNames.ZEK_INTRO
    )

    builder.step(quest_labels["start_zek_steal_bottle"]).start_quest_drama(
        QuestIds.ZEK_STEAL_BOTTLE, DramaNames.ZEK_STEAL_BOTTLE
    )

    builder.step(quest_labels["start_zek_steal_soulgem"]).start_quest_drama(
        QuestIds.ZEK_STEAL_SOULGEM, DramaNames.ZEK_STEAL_SOULGEM
    )

    builder.step(quest_labels["start_last_battle"]).start_quest_drama(
        QuestIds.LAST_BATTLE, DramaNames.LAST_BATTLE
    )

    # ========================================
    # 商品紹介: 導入
    # ========================================
    builder.step(rec_intro).say(
        "rec_intro", "おや……品物にご興味が？", "Oh my... Interested in my wares?", "哎呀……对商品感兴趣？", actor=zek
    ).say(
        "rec_intro2",
        "この闘技場で生き残るための『道具』、いくつかご用意がございます。",
        "I have prepared several 'instruments' for surviving this arena, if you would.",
        "为了在这角斗场生存的『道具』，我准备了一些。",
        actor=zek,
    ).jump(rec_menu)

    # ========================================
    # 商品紹介: カテゴリー選択
    # ========================================
    builder.step(rec_menu).say(
        "rec_menu",
        "災厄を退ける薬か、己を駆り立てる薬か、あるいは呪われし遺物か。どれをお聞きになりますか？",
        "Potions to ward off calamity, elixirs to drive yourself beyond limits, or perhaps cursed relics? Which shall I elaborate upon?",
        "祛除灾厄的药剂，激发自身潜能的药剂，还是诅咒的遗物呢？您想听哪个？",
        actor=zek,
    ).choice(rec_res, "耐性薬について", "Tell me about resistance potions", "关于抗性药剂", text_id="c_rec_res").choice(
        rec_boost, "戦闘ブースト薬について", "Tell me about combat boosters", "关于战斗增强药剂", text_id="c_rec_boost"
    ).choice(rec_eq, "装備品について", "Tell me about equipment", "关于装备", text_id="c_rec_eq").choice(
        rec_end, "やっぱりいい", "Never mind", "算了", text_id="c_rec_cancel"
    ).on_cancel(rec_end)

    # ========================================
    # 商品紹介: 耐性薬カテゴリー
    # ========================================
    builder.step(rec_res).say(
        "rec_res_1", "災厄を退ける薬……防御を固める戦術ですね。", "Potions to ward off calamity... A defensive strategy, I see.", "祛除灾厄的药剂……防御型的战术呢。", actor=zek
    ).say(
        "rec_res_2",
        "『万難のエリクサー』。かの異端錬金術師が生涯をかけた遺作です。あらゆる厄災を退ける……ただし、術師自身は完成の日に灰燼と化しました。",
        "The 'Elixir of All-Protection.' The magnum opus of a certain heretical alchemist who dedicated his entire life to its creation. It wards off all calamities... though I'm afraid the alchemist himself turned to ash on the day of its completion.",
        "『万难灵药』。那位异端炼金术师倾尽一生的遗作。能抵御一切灾厄……不过，术师本人在完成之日化为了灰烬。",
        actor=zek,
    ).say(
        "rec_res_3",
        "完璧な守りを得た瞬間、守るべき命が消えた。皮肉な結末ですが、効果は私が保証いたします。",
        "The moment he achieved perfect protection, there was no life left to protect. How delightfully ironic. But I shall personally guarantee its effectiveness.",
        "获得完美守护的瞬间，需要守护的生命却消失了。讽刺的结局，但效果我可以保证。",
        actor=zek,
    ).say(
        "rec_res_4",
        "個別の薬もございます。霜竜の血から精製した冷気の護り、砕けた真実の鏡から作られた精神の護り、狂人が遺した混沌を鎮める虹の薬……",
        "I also carry individual potions. Protection from cold refined from frost dragon's blood, mental protection crafted from a shattered mirror of truth, a rainbow elixir to quell chaos left behind by a madman...",
        "也有单独的药剂。用霜龙之血提炼的寒冷守护，用破碎的真实之镜制作的精神守护，狂人遗留的镇定混沌的彩虹药剂……",
        actor=zek,
    ).say(
        "rec_res_5",
        "轟音を退ける雷神の耳栓、衝撃を弾く鉄巨人の心臓、出血を止める吸血鬼の凝固血。お好みのものをどうぞ。",
        "Earplugs of the thunder god to ward off deafening roars, the iron giant's heart to deflect impact, coagulated vampire blood to stop bleeding. Take your pick, if you would.",
        "抵御轰鸣的雷神耳塞，弹开冲击的铁巨人之心，止血的吸血鬼凝血。请随意选择。",
        actor=zek,
    ).say(
        "rec_res_6",
        "どれも確かな効果がありますが、魂が少々……軽くなります。命あってこそ、些細な代償でしょう？",
        "Each has proven effects, though your soul may feel... somewhat lighter afterward. A trivial cost for keeping your life, wouldn't you agree?",
        "每一种都有确实的效果，不过灵魂会稍微……变轻一些。有命才有一切，小小代价而已对吧？",
        actor=zek,
    ).jump(rec_return)

    # ========================================
    # 商品紹介: 戦闘ブースト薬カテゴリー
    # ========================================
    builder.step(rec_boost).say(
        "rec_boost_1",
        "己を駆り立てる薬ですか。攻めの姿勢、嫌いではありません。",
        "Elixirs to drive yourself beyond limits? An offensive stance. I do find that rather agreeable.",
        "激发自身潜能的药剂吗。进攻型的姿态，我不讨厌。",
        actor=zek,
    ).say(
        "rec_boost_2",
        "『痛覚遮断薬』。かつて拷問官が囚人に与えていた黒い薬です。苦痛を遮断し、肉体の限界を超えて戦える。",
        "The 'Pain Suppressant.' A black elixir once administered by torturers to their prisoners. It blocks all pain, allowing one to fight beyond the body's limits.",
        "『痛觉阻断药』。曾是拷问官给囚犯服用的黑色药剂。阻断痛觉，超越肉体极限战斗。",
        actor=zek,
    ).say(
        "rec_boost_3",
        "囚人たちは痛みを忘れ、己の身体が腐り落ちていることにも気づかなかったそうです。……まあ、あなたはそこまで長く戦わないでしょう。",
        "The prisoners forgot their pain entirely, failing to notice even as their own bodies rotted away. ...Well, I'm afraid you won't be fighting quite that long.",
        "据说囚犯们忘记了痛苦，连自己身体腐烂都没察觉。……嘛，您应该不会战斗那么久。",
        actor=zek,
    ).say(
        "rec_boost_4",
        "『禁断の覚醒剤』は狂戦士御用達。神経が極限まで研ぎ澄まされ、世界がゆっくりと見える。",
        "The 'Forbidden Stimulant' is a berserker's favorite. It sharpens the nerves to their absolute limit, making the world appear to slow down.",
        "『禁忌兴奋剂』是狂战士的最爱。神经被磨砺到极限，世界看起来变得缓慢。",
        actor=zek,
    ).say(
        "rec_boost_5",
        "代わりに血管が内側から破裂することも。どちらも命の蝋燭を早く燃やす道具ですね。それでも、一瞬の輝きを求めるなら。",
        "In exchange, your blood vessels may rupture from within. Both are instruments that burn life's candle faster, I'm afraid. Still, if you seek a moment of brilliance...",
        "作为代价，血管可能会从内部破裂。两者都是加速燃烧生命蜡烛的工具呢。即便如此，若追求刹那的光辉……",
        actor=zek,
    ).jump(rec_return)

    # ========================================
    # 商品紹介: 装備品カテゴリー
    # ========================================
    builder.step(rec_eq).say(
        "rec_eq_1",
        "呪われし遺物ですか。おや、良い趣味をしていらっしゃる。",
        "Cursed relics, you say? Oh my, what exquisite taste you have.",
        "诅咒的遗物吗。哎呀，品味真不错。",
        actor=zek,
    ).say(
        "rec_eq_2",
        "『儚き天禀』と『愚者の平穏』。対照的な二つの指輪です。前者は夭折した天才魔術師の、後者は魔法を恐れた愚かな王の遗品。",
        "The 'Glass Sovereignty' and the 'Fool's Serenity.' Two contrasting rings. The former belonged to a prodigious mage who died young; the latter, to a foolish king who feared magic.",
        "『脆薄天禀』和『愚者之安』。两枚对照的戒指。前者是早逝天才魔术师的，后者是惧怕魔法的愚蠢国王的遗物。",
        actor=zek,
    ).say(
        "rec_eq_3",
        "魔力を極限まで高めて命を削るか、肉体を頑強にして魔力を封じるか。どちらを選んでも、何かを失う定めです。",
        "Maximize your magical power at the cost of your life, or fortify your body while sealing away your magic. Whichever you choose, you're destined to lose something.",
        "将魔力提升到极限削减寿命，还是强化肉体封印魔力。无论选哪个，都注定要失去些什么。",
        actor=zek,
    ).say(
        "rec_eq_4",
        "『虚飾の黄金鎧』。強欲な王の呪われた遺産。傷を受けるたび、血の代わりに金貨が剥がれ落ちます。王は最期、一枚の金貨も残さず骸となりました。",
        "The 'Gilded Vanity Armor.' The cursed legacy of a greedy king. Each wound sheds gold coins instead of blood. In the end, the king became a corpse without a single coin remaining. How delightful.",
        "『虚饰黄金甲』。贪婪国王的诅咒遗产。每受一次伤，不是流血而是掉落金币。国王最终一枚金币都没留下就成了骸骨。",
        actor=zek,
    ).say(
        "rec_eq_5",
        "『双子の鏡』。互いを封じ込めた双子の魔女の呪物です。装備すれば鏡からあなたの分身が這い出し、主に付き従います。外せば影は還る。",
        "The 'Twin Mirror.' A cursed artifact of twin witches who sealed each other within. Equip it, and your doppelganger shall crawl forth from the mirror to serve you. Remove it, and the shadow returns.",
        "『双子之镜』。互相封印的双胞胎女巫的咒物。装备后分身会从镜中爬出追随主人。卸下则影子回归。",
        actor=zek,
    ).say(
        "rec_eq_6",
        "『飢餓の首飾り』。餓鬼道に堕ちた僧侶の遺品。底なしの飢えに苛まれ、いくら食べても満たされない。僧侶は最期、己の腕を喰らったとか。",
        "The 'Amulet of Hunger.' The relic of a monk who fell into the realm of hungry ghosts. Plagued by bottomless hunger, never satisfied no matter how much he ate. They say the monk devoured his own arm in the end.",
        "『饥饿项链』。堕入饿鬼道的僧侣遗物。被无底的饥饿折磨，无论吃多少都无法满足。据说僧侣最后吞食了自己的手臂。",
        actor=zek,
    ).say(
        "rec_eq_7",
        "……どれも持ち主を幸福にはしなかった遺物です。ですが、力は確かにある。それで十分でしょう？",
        "...None of these relics brought happiness to their owners. But they possess undeniable power. That should be sufficient, wouldn't you agree?",
        "……没有一件遗物给主人带来过幸福。但力量是确实存在的。这就足够了吧？",
        actor=zek,
    ).jump(rec_return)

    # ========================================
    # 商品紹介: カテゴリー選択に戻る
    # ========================================
    builder.step(rec_return).say(
        "rec_return", "他にお聞きになりたいことは？", "Is there anything else you wish to know?", "还有其他想了解的吗？", actor=zek
    ).choice(rec_res, "耐性薬について", "Tell me about resistance potions", "关于抗性药剂", text_id="c_rec_res2").choice(
        rec_boost, "戦闘ブースト薬について", "Tell me about combat boosters", "关于战斗增强药剂", text_id="c_rec_boost2"
    ).choice(rec_eq, "装備品について", "Tell me about equipment", "关于装备", text_id="c_rec_eq2").choice(
        rec_end, "もういい", "That's enough", "够了", text_id="c_rec_done"
    ).on_cancel(rec_end)

    # ========================================
    # 商品紹介: 終了
    # ========================================
    builder.step(rec_end).say(
        "rec_end",
        "では、ごゆっくり品定めを。……私は影の中におりますので。",
        "Then please, take your time browsing. ...I shall be waiting in the shadows.",
        "那么，请慢慢挑选。……我会在暗处等候。",
        actor=zek,
    ).jump(choices)

    # ========================================
    # エピローグ後の会話
    # ========================================
    builder.step(post_game).say(
        "pg_greet",
        "クク……外の世界でも商売の機会は尽きませんね。あなたと一緒なら、さらに面白い取引ができそうです。",
        "Heh... Business opportunities never end, even in the outside world. With you, I could make even more interesting deals.",
        "呵呵……外面的世界商机也是无穷无尽呢。和您在一起的话，能做更有趣的交易。",
        actor=zek,
    ).jump(post_game_choices)

    # パーティメンバー用選択肢
    # inject_unique(): バニラの_invite, _joinParty, _leaveParty, _buy, _heal等を追加
    builder.step(post_game_choices).inject_unique().choice(
        rec_intro,
        "おすすめの商品は？",
        "Any recommendations?",
        "有什么推荐的吗？",
        text_id="c_pg_recommend",
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
