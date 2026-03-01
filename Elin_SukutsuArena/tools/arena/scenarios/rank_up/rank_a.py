# -*- coding: utf-8 -*-
"""
12_rank_up_A.md - Rank A 昇格試合『戦鬼』
影の自己との戦い - 自分自身を超える
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds, Rank, SessionKeys


def define_rank_up_A(builder: DramaBuilder):
    """
    Rank A 昇格試合「戦鬼」
    シナリオ: 12_rank_up_A.md

    観客の「注目」が生み出した影の自己と戦う
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_shadow_warning")
    choice1 = builder.label("choice1")
    react1_shadow = builder.label("react1_shadow")
    react1_ready = builder.label("react1_ready")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_zek_revelation")
    choice2 = builder.label("choice2")
    react2_understand = builder.label("react2_understand")
    react2_fear = builder.label("react2_fear")
    react2_nod = builder.label("react2_nod")
    scene3 = builder.label("scene3_lily_blessing")
    choice3 = builder.label("choice3")
    react3_promise = builder.label("react3_promise")
    react3_confident = builder.label("react3_confident")
    react3_hold = builder.label("react3_hold")
    battle_start = builder.label("battle_start")

    # ========================================
    # シーン1: 影の予兆
    # ========================================
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/Ominous_Suspense_02"
    ).say(
        "narr_1",
        "ロビーに足を踏み入れた瞬間、あなたは自分の影が妙に濃いことに気づく。",
        "The moment you step into the lobby, you notice your shadow is unusually dark.",
        "踏入大厅的瞬间，你注意到自己的影子异常浓重。",
        actor=narrator,
    ).say(
        "narr_2",
        "それは、松明の光に関係なく、まるで意志を持つかのように蠢いている。",
        "Regardless of the torchlight, it writhes as if possessing a will of its own.",
        "无论火把的光如何，它都像拥有意志一般蠕动着。",
        actor=narrator,
    ).say(
        "narr_3",
        "バルガスが厳しい表情で近づいてくる。",
        "Vargus approaches with a stern expression.",
        "巴尔加斯带着严峻的表情走过来。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.BALGAS).say(
        "balgas_1",
        "……おい、銀翼。お前、自分の影をよく見てみろ。\n\n観客どもの『注目』が、お前に集まりすぎたんだ。その結果、お前の影から……『もう一人のお前』が生まれようとしている。",
        "...Hey, Silver Wing. Take a good look at your shadow.\n\nToo much 'attention' from those spectators has gathered on ya. As a result, from your shadow... 'another you' is tryin' to be born.",
        "……喂，银翼。你小子好好看看自己的影子。\n\n那些观众的『注目』太过集中在你小子身上了。结果，从你的影子里……『另一个你』正在诞生。",
        actor=balgas,
    ).say(
        "balgas_3",
        "『戦鬼』ーーそれが、次の試練の名前だ。\n\nお前が積み上げてきた全ての技術、全ての経験、全ての殺意……それを完璧にコピーした存在と戦うことになる。",
        "'War Demon'--that's the name of your next trial.\n\nAll the skills you've built up, all the experience, all the killing intent... you'll be fightin' something that's copied all of it perfectly.",
        "『战鬼』--那就是下一个试炼的名字。\n\n你小子积累的所有技术、所有经验、所有杀意……你要跟一个完美复制了这一切的存在战斗。",
        actor=balgas,
    ).say(
        "narr_4",
        "バルガスは苦々しげに首を振る。",
        "Vargus shakes his head bitterly.",
        "巴尔加斯苦涩地摇了摇头。",
        actor=narrator,
    ).say(
        "balgas_5",
        "こいつは、ヌルとは違う意味で厄介だ。虚無には『意味』で対抗できた。だが、影はお前自身だ。\n\nお前の強さも、弱さも、全て知っている。お前が次に何をするか、完璧に読んでくる。",
        "This one's troublesome in a different way than Null. Ya could counter the void with 'meaning.' But the shadow is you.\n\nIt knows all your strengths, all your weaknesses. It'll read exactly what you're gonna do next.",
        "这家伙跟Nul是另一种意义上的棘手。虚无可以用『意义』来对抗。但影子就是你自己。\n\n它知道你所有的强项和弱点。会完美预判你小子接下来要做什么。",
        actor=balgas,
    )

    # プレイヤーの選択肢1
    builder.choice(
        react1_shadow,
        "自分自身と戦う……どうすれば勝てる？",
        "Fighting myself... how can I win?",
        "跟自己战斗……怎样才能赢？",
        text_id="c1_shadow",
    ).choice(
        react1_ready,
        "覚悟はできている",
        "I'm ready.",
        "我已经准备好了",
        text_id="c1_ready",
    ).choice(
        react1_silent,
        "（無言で影を見つめる）",
        "(Silently stare at the shadow)",
        "（默默凝视影子）",
        text_id="c1_silent",
    )

    # 選択肢反応1
    builder.step(react1_shadow).say(
        "balgas_r1",
        "……それは、俺にも分からねえ。だが、お前は今まで、いつも予想を裏切ってきた。それが、答えかもしれねえな。",
        "...I dunno either. But you've always defied expectations. Maybe that's the answer.",
        "……这个老子也不知道。但你小子一直都在打破预期。也许那就是答案。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_ready).say(
        "balgas_r2",
        "……ハッ、その顔だ。その目を忘れるなよ。",
        "...Hah! That's the face. Don't forget those eyes.",
        "……哈！就是这表情。别忘了这眼神。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "balgas_r3",
        "……そうだ。よく見ておけ。あれが、お前の『可能性』の一つだ。",
        "...That's right. Look closely. That's one of your 'possibilities.'",
        "……没错。好好看看。那是你的『可能性』之一。",
        actor=balgas,
    ).jump(scene2)

    # ========================================
    # シーン2: ゼクの洞察
    # ========================================
    builder.step(scene2).play_bgm("BGM/Zek_Merchant").focus_chara(Actors.ZEK).say(
        "narr_5",
        "影の中から、ゼクが姿を現す。",
        "Zek emerges from the shadows.",
        "泽克从阴影中现身。",
        actor=narrator,
    ).say(
        "zek_5b",
        "影はあなたの姿、武器、防具……全てをそっくりそのままコピーしてくる。つまり、あなたが強ければ強いほど、奴も強くなる。",
        "The shadow copies your appearance, weapons, armor... everything exactly. In other words, the stronger you are, the stronger it becomes.",
        "影子会完全复制您的姿态、武器、防具……一切。也就是说，您越强，它就越强。",
        actor=zek,
    ).say(
        "zek_5c",
        "ですから、こう考えてみてはいかがでしょう。あなたの防具に穴はないか？ あなた自身を倒す方法を、今のうちに考えておくことですね。",
        "So, consider this: are there any gaps in your armor? Think now about how you would defeat yourself.",
        "所以，不妨这样想想：您的防具有没有漏洞？现在就考虑好如何击败自己吧。",
        actor=zek,
    ).say(
        "zek_6",
        "……まあ、私としては、どちらが勝っても『最高の瞬間』を記録できるので、気楽に見物させてもらいますよ。",
        "...Well, as for me, I can record the 'finest moment' regardless of who wins, so I'll enjoy watching at my leisure.",
        "……嗯，对我来说，不管谁赢都能记录『最精彩的瞬间』，所以我会悠闲地观赏的。",
        actor=zek,
    )

    # プレイヤーの選択肢2
    builder.choice(
        react2_understand,
        "……自分を倒す方法、か。心当たりはある",
        "...How to defeat myself, huh. I have some ideas.",
        "……击败自己的方法吗。我有些想法",
        text_id="c2_understand",
    ).choice(
        react2_fear,
        "困ったな……俺が完璧すぎて、つけ入る隙がない",
        "That's a problem. You can't find flaws in perfection.",
        "这可难办了。想在完美中找到缺陷可不容易",
        text_id="c2_fear",
    ).choice(
        react2_nod,
        "（無言で頷く）",
        "(Nod silently)",
        "（默默点头）",
        text_id="c2_nod",
    )

    # 選択肢反応2
    builder.step(react2_understand).say(
        "zek_r1",
        "ほう……それは頼もしい。では、その『心当たり』を試す場をお膳立てしましょう。",
        "Oh...? How reassuring. Then let me arrange a stage to test that 'idea' of yours.",
        "哦……那可真令人安心。那么，让我来安排一个验证那个『想法』的舞台吧。",
        actor=zek,
    ).jump(scene3)

    builder.step(react2_fear).say(
        "zek_r2",
        "クク……自信過剰は嫌いではありませんよ。その慢心が命取りにならなければ、ですが。",
        "Hehe... I don't dislike overconfidence. So long as that hubris doesn't prove fatal, that is.",
        "呵呵……我不讨厌过度自信哦。只要那份自负不会致命的话。",
        actor=zek,
    ).jump(scene3)

    builder.step(react2_nod).say(
        "zek_r3",
        "……無口ですが、覚悟は決まったようですね。期待していますよ。",
        "...Taciturn, but it seems your resolve is set. I look forward to it.",
        "……虽然沉默，但似乎已下定决心了呢。我很期待哦。",
        actor=zek,
    ).jump(scene3)

    # ========================================
    # シーン3: リリィの祝福
    # ========================================
    builder.step(scene3).focus_chara(Actors.LILY).say(
        "narr_7",
        "リリィが、いつもの微笑みで近づいてくる。",
        "Lily approaches with her usual smile.",
        "莉莉带着一如既往的微笑走过来。",
        actor=narrator,
    ).say(
        "lily_1",
        "ところで、お客様。影があなたの『完璧なコピー』だとしたら、一つ気になることがありまして。",
        "By the way, dear guest. If the shadow is a 'perfect copy' of you, there is one thing that concerns me.",
        "话说回来，客人。如果影子是您的『完美复制品』的话，有一件事让我很在意。",
        actor=lily,
    ).say(
        "lily_4",
        "……あなた、サキュバスの前でいつも隙だらけですよね？\n影もそうなら、少し話しかけてみましょうか。案外あっさり戦意喪失するかもしれません。",
        "...You always leave yourself wide open around a succubus, don't you?\nIf the shadow is the same way, perhaps I should chat it up. It might lose its fighting spirit surprisingly easily.",
        "……您在魅魔面前总是破绽百出呢？\n如果影子也是那样的话，要不要我去跟它聊聊？说不定它会出乎意料地轻易丧失战意。",
        actor=lily,
    ).say(
        "lily_6",
        "……冗談です。行ってらっしゃい。影には真似できないものを、見せてあげてください。",
        "...Just kidding. Off you go. Show it something it can never imitate.",
        "……开玩笑的。去吧。让它看看它永远无法模仿的东西。",
        actor=lily,
    )

    # プレイヤーの選択肢3
    builder.choice(
        react3_promise,
        "……隙だらけ？ そんなことないと思うけど",
        "...Wide open? I don't think so.",
        "……破绽百出？我觉得没有吧",
        text_id="c3_promise",
    ).choice(
        react3_confident,
        "じゃあ、影がリリィに見惚れた隙に斬る",
        "Then I'll cut it down while it's distracted by you.",
        "那我就趁影子看你看呆的时候砍了它",
        text_id="c3_confident",
    ).choice(
        react3_hold,
        "……冗談はいい。行ってくる",
        "...Enough joking. I'm heading out.",
        "……别开玩笑了。我去了",
        text_id="c3_hold",
    )

    # 選択肢反応3
    builder.step(react3_promise).say(
        "lily_r4",
        "ふふ、自覚がないのが一番危ないんですよ？ ……まあ、それも含めてあなたらしいですけど。",
        "Hehe, being unaware is the most dangerous part, you know? ...Well, that's just like you, I suppose.",
        "呵呵，没有自觉才是最危险的哦？……不过，这也是您的风格就是了。",
        actor=lily,
    ).jump(battle_start)

    builder.step(react3_confident).say(
        "lily_r5",
        "……それ、作戦としては最低ですが、嫌いじゃないです。",
        "...As a strategy, that's the worst. But I don't hate it.",
        "……作为战术来说这是最烂的，但我不讨厌。",
        actor=lily,
    ).jump(battle_start)

    builder.step(react3_hold).say(
        "lily_r6",
        "あら、つれない。……ご武運を。",
        "My, how cold. ...Good luck out there.",
        "哎呀，真冷淡。……祝您武运昌隆。",
        actor=lily,
    ).jump(battle_start)

    # ========================================
    # シーン4: 戦闘開始
    # ========================================
    builder.step(battle_start).play_bgm("BGM/Battle_Shadow_Self").say(
        "narr_9",
        "闘技場の中央に足を踏み入れた瞬間、空気が凍りつく。",
        "The moment you step into the center of the arena, the air freezes.",
        "踏入角斗场中央的瞬间，空气凝固了。",
        actor=narrator,
    ).say(
        "narr_10",
        "そこに立っていたのは、まさに鏡像。あなたと寸分違わぬ姿をした、黄金色に輝く戦士。",
        "Standing there was a perfect mirror image. A warrior shining golden, identical to you in every way.",
        "站在那里的正是镜像。一个与你一模一样、闪耀着金色光芒的战士。",
        actor=narrator,
    ).shake().say(
        "narr_11",
        "その瞳は、あなたと同じ光を湛えながらも、どこか虚ろだ。",
        "Its eyes hold the same light as yours, yet somehow appear hollow.",
        "它的眼中蕴含着与你相同的光芒，却又显得有些空洞。",
        actor=narrator,
    ).say(
        "narr_12",
        "影が剣を構える。その動作は、あなたがこれから行おうとした動作と、寸分違わず一致していた。",
        "The shadow raises its sword. The motion perfectly matches what you were about to do.",
        "影子举起剑。那个动作与你即将做的动作分毫不差。",
        actor=narrator,
    ).shake().say(
        "narr_13",
        "観客席から、異様な熱狂が渦巻くーー自分自身との決闘。これほど『面白い』見世物は、アリーナの歴史にも稀だろう。",
        "An eerie frenzy swirls from the spectator seats--a duel against oneself. Such an 'entertaining' spectacle is rare even in the arena's history.",
        "观众席上涌动着异样的狂热--与自己的决斗。如此『有趣』的表演，即使在角斗场的历史上也是罕见的。",
        actor=narrator,
    ).start_battle_by_stage("rank_a_trial", master_id="sukutsu_arena_master").finish()


def add_rank_up_A_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    Rank A 昇格試合の勝利/敗北ステップを arena_master ビルダーに追加する

    Args:
        builder: arena_master の ArenaDramaBuilder インスタンス
        victory_label: 勝利ステップのラベル名
        defeat_label: 敗北ステップのラベル名
        return_label: 結果表示後にジャンプするラベル名
    """
    pc = Actors.PC
    lily = Actors.LILY
    balgas = Actors.BALGAS
    zek = Actors.ZEK
    narrator = Actors.NARRATOR

    # ========================================
    # Rank A 昇格試合 勝利
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Emotional_Sacred_Triumph_Special"
    ).say(
        "ra_narr_v1",
        "影が砕け散る瞬間、その破片はあなたの体に吸い込まれていった。",
        "The moment the shadow shatters, its fragments are absorbed into your body.",
        "影子碎裂的瞬间，它的碎片被吸入你的身体。",
        actor=narrator,
    ).say(
        "ra_narr_v2",
        "それは、あなたが自分自身を超えた証。過去の限界を打ち破り、新たな高みへ到達した瞬間だった。",
        "It was proof that you had surpassed yourself. The moment you broke through past limits and reached new heights.",
        "这是你超越自己的证明。是你打破过去的极限、到达新高度的瞬间。",
        actor=narrator,
    ).say(
        "ra_narr_v3",
        "ロビーに戻ると、仲間たちがあなたを待っていた。",
        "Returning to the lobby, your companions await you.",
        "回到大厅，伙伴们正在等着你。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "ra_balgas_v1",
        "……やりやがったな。自分自身を超えるってのは、口で言うほど簡単じゃねえ。",
        "...Ya did it. Surpassin' yourself ain't as easy as it sounds.",
        "……你小子还真做到了。超越自己可不是说说那么简单。",
        actor=balgas,
    ).say(
        "ra_balgas_v2",
        "だが、お前はやってのけた。……カインが生きていたら、きっと喜んだだろうよ。",
        "But you pulled it off. ...If Cain were alive, he'd be happy for sure.",
        "但你小子做到了。……凯恩要是还活着，一定会很高兴吧。",
        actor=balgas,
    ).focus_chara(Actors.ZEK).say(
        "zek_v1",
        "クク……素晴らしい。影を超えた瞬間、あなたの魂は一段と輝きを増しました。",
        "Hehe... Magnificent. The moment you surpassed your shadow, your soul shone even brighter.",
        "呵呵……太棒了。超越影子的瞬间，您的灵魂变得更加耀眼了。",
        actor=zek,
    ).say(
        "zek_v2",
        "私のコレクションに加えるのが惜しくなってきましたね。……冗談ですよ。",
        "I'm starting to feel reluctant to add you to my collection. ...Just joking.",
        "开始舍不得把您加入我的收藏了呢。……开玩笑的。",
        actor=zek,
    ).focus_chara(Actors.LILY).say(
        "ra_lily_v1",
        "……おかえりなさい。",
        "...Welcome back.",
        "……欢迎回来。",
        actor=lily,
    ).say(
        "ra_lily_v2",
        "あなたは今、このアリーナで最も輝く存在になりました。",
        "You have now become the most radiant presence in this arena.",
        "您现在已经成为这个角斗场中最耀眼的存在了。",
        actor=lily,
    ).say(
        "ra_lily_v3",
        "今日からあなたは、ランクA……『戦鬼』です。",
        "From this day forward, you are Rank A... the 'War Demon.'",
        "从今天起您就是A级……『战鬼』了。",
        actor=lily,
    ).complete_quest(QuestIds.RANK_UP_A).grant_rank_reward(
        "A", actor=lily
    ).change_journal_phase("sukutsu_arena", 8).finish()

    # ========================================
    # Rank A 昇格試合 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Lobby_Normal"
    ).focus_chara(Actors.LILY).say(
        "ra_lily_d1",
        "……影に、飲み込まれてしまいましたね。",
        "...You were consumed by the shadow.",
        "……被影子吞噬了呢。",
        actor=lily,
    ).say(
        "ra_lily_d2",
        "でも、大丈夫です。影はあなた自身……いつか必ず、超えられます。",
        "But it is all right. The shadow is yourself... someday, you will surely surpass it.",
        "但没关系。影子就是您自己……总有一天一定能超越的。",
        actor=lily,
    ).say(
        "ra_lily_d3",
        "準備が整ったら、また挑戦してください。私たちは、いつでもここにいます。",
        "When you are ready, please challenge it again. We shall always be here.",
        "准备好了请再次挑战。我们随时都在这里。",
        actor=lily,
    ).finish()
