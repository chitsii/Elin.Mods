"""
13_makuma2.md - 虚空を繋ぎ止める心臓、そして清算の時
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds


def define_makuma2(builder: DramaBuilder):
    """
    ランクA前夜：虚空の心臓製作と過去の清算
    シナリオ: 13_makuma2.md

    Note: This scenario has complex conditional branches based on previous choices:
    - If bottle_choice == SWAPPED: bottle malfunction event occurs
    - If kain_soul_choice == SOLD: Vargus confrontation occurs
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_void_core_request")
    choice1 = builder.label("choice1")
    react1_accept = builder.label("react1_accept")
    react1_soul = builder.label("react1_soul")
    react1_silent = builder.label("react1_silent")

    # 条件分岐: 瓶の暴走イベント
    check_bottle_seen = builder.label("check_bottle_seen")
    check_bottle = builder.label("check_bottle")
    bottle_event = builder.label("bottle_event")
    bottle_battle = builder.label("bottle_battle")
    bottle_choice = builder.label("bottle_choice")
    bottle_confess = builder.label("bottle_confess")
    bottle_blame = builder.label("bottle_blame")
    bottle_deny = builder.label("bottle_deny")
    lily_departure = builder.label("lily_departure")
    after_bottle = builder.label("after_bottle")

    # シーン2: 製作
    scene2 = builder.label("scene2_crafting")
    check_materials = builder.label("check_materials")
    has_materials = builder.label("has_materials")
    no_materials = builder.label("no_materials")
    crafting_complete = builder.label("crafting_complete")
    scene3 = builder.label("scene3_balgas_warning")

    # 条件分岐: カインの魂
    check_kain = builder.label("check_kain")
    kain_event = builder.label("kain_event")
    kain_choice = builder.label("kain_choice")
    kain_confess = builder.label("kain_confess")
    kain_lie = builder.label("kain_lie")
    after_kain = builder.label("after_kain")

    final_choice = builder.label("final_choice")
    final_thanks = builder.label("final_thanks")
    final_trust = builder.label("final_trust")
    final_knowledge = builder.label("final_knowledge")
    scene4 = builder.label("scene4_completion")
    ending = builder.label("ending")

    # ========================================
    # シーン1: リリィの虚空の心臓依頼
    # ========================================
    builder.step(main).play_bgm("BGM/Ominous_Heartbeat").focus_chara(Actors.LILY).say(
        "narr_1",
        "最近、アリーナ全体を断続的な震動が襲っている。",
        "Recently, intermittent tremors have been shaking the entire Arena.",
        "最近，整个角斗场不断遭受间歇性的震动。",
        actor=narrator,
    ).shake().say(
        "narr_2",
        "それは観客の喝采によるものではなく、あなたの強大すぎる存在感に、異次元の構造自体が悲鳴を上げているのだ。",
        "It is not from the audience's cheers, but from the interdimensional structure itself groaning under the weight of your overwhelming presence.",
        "这并非源自观众的欢呼，而是异次元的结构本身正因您过于强大的存在而发出悲鸣。",
        actor=narrator,
    ).say(
        "narr_3",
        "リリィはカウンターの奥で、幾重にも重なった複雑な魔法幾何学の設計図と格闘していた。",
        "Lily was wrestling with complex magical geometric blueprints layered upon one another behind the counter.",
        "莉莉正在柜台深处，与层层叠叠的复杂魔法几何设计图奋战着。",
        actor=narrator,
    ).say(
        "narr_4",
        "彼女の瞳には、これまでにない焦燥と、それ以上の『愉悦』が宿っている。",
        "In her eyes dwelt an unprecedented anxiety, and even greater 'delight.'",
        "她的眼眸中蕴含着前所未有的焦躁，以及更胜一筹的「愉悦」。",
        actor=narrator,
    ).say(
        "lily_1",
        "……あぁ、困りました。あなたの魂が放つオーラが、このアリーナの空間構造を揺るがしています。",
        "...Oh my, this is troublesome. The aura emanating from your soul is destabilizing this Arena's spatial structure.",
        "……哎呀，真是困扰呢。您灵魂散发的气场正在动摇这座角斗场的空间结构。",
        actor=lily,
    ).say(
        "lily_2",
        "このままでは、次の昇格試合を迎える前に、この空間が壊れて、私たちは虚無に投げ出されてしまうわ。",
        "At this rate, before your next promotion match, this space will collapse and we shall all be cast into the void.",
        "照这样下去，在迎来下一场晋级赛之前，这个空间就会崩塌，我们都会被抛入虚无之中。",
        actor=lily,
    ).say(
        "narr_5",
        "彼女は設計図をあなたの前に広げる。",
        "She spreads the blueprints before you.",
        "她在您面前展开设计图。",
        actor=narrator,
    ).say(
        "lily_3",
        "だから、補修に協力して頂けますか？アリーナを安定させるための楔……『虚空の心臓』と呼んでいます。",
        "Therefore, would you assist me with the repairs? A wedge to stabilize the Arena... I call it the 'Heart of the Void.'",
        "所以，您能协助我进行修补吗？用于稳定角斗场的楔子……我称之为「虚空之心」。",
        actor=lily,
    ).jump(choice1)

    # プレイヤーの選択肢
    builder.choice(
        react1_accept,
        "分かった。一緒に作ろう",
        "Understood. Let's make it together.",
        "明白了。一起制作吧。",
        text_id="c1_accept",
    ).choice(
        react1_soul,
        "俺の魂がアリーナを壊すのか……？",
        "My soul is destroying the Arena...?",
        "我的灵魂会毁掉角斗场……？",
        text_id="c1_soul",
    ).choice(
        react1_silent,
        "（無言で設計図を見つめる）",
        "(Silently stare at the blueprints)",
        "（默默凝视设计图）",
        text_id="c1_silent",
    )

    # 選択肢反応
    builder.step(react1_accept).say(
        "lily_r1",
        "ふふ、素直ですこと。では、材料を集めてきてくださいね。",
        "Hehe, how obedient of you. Now then, please gather the materials for me.",
        "呵呵，真是听话呢。那么，请去收集材料吧。",
        actor=lily,
    ).jump(check_bottle_seen)

    builder.step(react1_soul).say(
        "lily_r2",
        "ええ。あなたの力は既に、この異次元の限界を超え始めています。",
        "Indeed. Your power has already begun to exceed the limits of this dimension.",
        "是的。您的力量已经开始超越这个异次元的极限了。",
        actor=lily,
    ).jump(check_bottle_seen)

    builder.step(react1_silent).say(
        "lily_r3",
        "……難しそうに見えますが、私が作りますから、大丈夫ですよ。",
        "...It may look difficult, but I shall craft it, so worry not.",
        "……看起来似乎很难，但由我来制作，所以不必担心。",
        actor=lily,
    ).jump(check_bottle_seen)

    # ========================================
    # 条件分岐: 瓶イベント済みチェック
    # ========================================
    # LILY_BOTTLE_CONFESSION が設定済み（>= 0）なら瓶イベントをスキップ
    builder.step(check_bottle_seen).branch_if(
        Keys.LILY_BOTTLE_CONFESSION,
        ">=",
        FlagValues.LilyBottleConfession.CONFESSED,
        scene2,
    ).jump(check_bottle)

    # ========================================
    # 条件分岐: 瓶の暴走イベント (bottle_choice == SWAPPED)
    # ========================================
    # BottleChoice: 0=REFUSED, 1=SWAPPED
    builder.step(check_bottle).switch_flag(
        Keys.BOTTLE_CHOICE,
        [
            scene2,  # 0: 拒否した場合
            bottle_event,  # 1: すり替えた場合
            scene2,  # fallback
        ],
    )

    builder.step(bottle_event).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_6",
        "リリィが設計図を広げる中、彼女は棚の奥から、以前あなたが製作した『死の共鳴瓶』を取り出した。",
        "As Lily spread out the blueprints, she retrieved the 'Resonance Bottle of Death' you had crafted before from the back of the shelf.",
        "就在莉莉展开设计图时，她从架子深处取出了您之前制作的「死亡共鸣瓶」。",
        actor=narrator,
    ).say(
        "lily_7",
        "……虚空の心臓を起動させる前に、この共鳴瓶で次元の『周波数』を測定する必要があります。",
        "...Before activating the Heart of the Void, we need to measure this dimension's 'frequency' with this resonance bottle.",
        "……在启动虚空之心之前，需要用这个共鸣瓶来测量次元的「频率」。",
        actor=lily,
    ).say(
        "lily_8",
        "あなたが作ってくれたこの器……今まで完璧に機能していたのだけれど。",
        "This vessel you crafted for me... it has functioned perfectly until now.",
        "您为我制作的这个器皿……到目前为止一直运作得很完美。",
        actor=lily,
    ).say(
        "narr_7",
        "リリィが瓶に魔力を流し込むと、瓶の表面に不吉な亀裂が走る。",
        "As Lily channels magical power into the bottle, ominous cracks spread across its surface.",
        "当莉莉将魔力注入瓶中时，瓶子表面浮现出不祥的裂纹。",
        actor=narrator,
    ).shake().say(
        "lily_9",
        "……！？ なに、これ……内部構造が崩壊している……！？",
        "...!? What is this... the internal structure is collapsing...!?",
        "……！？这、这是……内部结构在崩溃……！？",
        actor=lily,
    ).say(
        "narr_8",
        "瓶から黒い霧が噴き出し、ロビー全体を覆い始める。",
        "Black mist erupts from the bottle and begins to engulf the entire lobby.",
        "黑色雾气从瓶中喷涌而出，开始笼罩整个大厅。",
        actor=narrator,
    ).say(
        "narr_9",
        "霧の中から、これまでアリーナで死んでいった闘士たちの怨念が、おぞましい人型の影となって実体化していく。",
        "From within the mist, the grudges of gladiators who died in the Arena materialize as grotesque humanoid shadows.",
        "从雾中，曾在角斗场死去的斗士们的怨念化作狰狞的人形阴影逐渐实体化。",
        actor=narrator,
    ).say(
        "lily_10",
        "くっ……この瓶、まさか……！ 誰かが構造を改竄（かいざん）している……！",
        "Kh...! This bottle, could it be...! Someone has tampered with its structure...!",
        "呃……这个瓶子，难道……！有人篡改了它的结构……！",
        actor=lily,
    ).say(
        "lily_11",
        "このままでは、溜め込んだ死者の残響が暴走して、アリーナごと呑み込まれるわ！",
        "At this rate, the accumulated echoes of the dead will run rampant and consume the entire Arena!",
        "照这样下去，积累的死者残响会失控暴走，把整个角斗场都吞噬掉！",
        actor=lily,
    ).jump(bottle_battle)

    # 戦闘イベント
    builder.step(bottle_battle).shake().say(
        "narr_10",
        "黒い霧から形成された怨念の影が、悲鳴のような咆哮を上げてあなたに襲いかかる！",
        "The shadow of grudges formed from black mist lets out a scream-like roar and lunges at you!",
        "由黑雾形成的怨念之影发出尖啸般的咆哮，向您扑来！",
        actor=narrator,
    ).say(
        "narr_10_1",
        "リリィが叫ぶ。「私が抑え込む！ 今のうちに核を砕いて！」",
        "Lily shouts. 'I'll hold it back! Shatter the core while you can!'",
        "莉莉高喊道：「我来压制住它！趁现在击碎核心！」",
        actor=narrator,
    ).shake().say(
        "narr_10_2",
        "あなたは怨念の中心、脈動する黒い結晶を砕き、影を霧散させた。",
        "You shatter the pulsating black crystal at the heart of the grudge, dispersing the shadows.",
        "您击碎了怨念中心那颗脉动的黑色结晶，将阴影驱散。",
        actor=narrator,
    ).say(
        "narr_11",
        "リリィは息を切らしながら、砕け散った偽物の瓶の破片を拾い上げる。",
        "Lily, breathing heavily, picks up the shattered fragments of the fake bottle.",
        "莉莉气喘吁吁地捡起破碎的假瓶子碎片。",
        actor=narrator,
    ).say(
        "narr_12",
        "その断面には、禍々しい形状の刻印が刻まれていた。",
        "On its cross-section, a sinister marking was carved.",
        "在断面上刻有不祥的印记。",
        actor=narrator,
    ).say(
        "lily_12",
        "……これは、ゼクの細工ね。間違いないわ。",
        "...This is Zek's handiwork. There is no doubt.",
        "……这是泽克的手法。毫无疑问。",
        actor=lily,
    ).say(
        "narr_13",
        "リリィはゆっくりとあなたを振り返る。その瞳には、怒りと悲しみ、そして『裏切られたかもしれない』という疑念が混じり合っていた。",
        "Lily slowly turns to face you. In her eyes, anger and sorrow mingle with the suspicion that she may have been betrayed.",
        "莉莉缓缓转身面向您。她的眼眸中交织着愤怒、悲伤，以及「或许遭到背叛」的疑虑。",
        actor=narrator,
    ).say(
        "lily_13",
        "……答えて。あなたは、あの時私に渡した瓶が『偽物』だと知っていたの？ それとも、ゼクに騙されていたの？",
        "...Answer me. Did you know the bottle you gave me that time was a 'fake'? Or were you deceived by Zek?",
        "……回答我。您当时交给我的瓶子，您知道那是「假货」吗？还是说，您也被泽克欺骗了？",
        actor=lily,
    ).jump(bottle_choice)

    # 瓶の真実についての選択肢
    builder.choice(
        bottle_confess,
        "……すまない。ゼクに唆されて、本物と偽物をすり替えた。君を裏切ってしまった",
        "...I'm sorry. Zek tempted me, and I swapped the real one with a fake. I betrayed you.",
        "……对不起。我被泽克唆使，把真品和假货调换了。我背叛了你。",
        text_id="c_bottle_confess",
    ).choice(
        bottle_blame,
        "ゼクが勝手に細工したのだろう……",
        "Zek must have tampered with it on his own...",
        "应该是泽克擅自动了手脚……",
        text_id="c_bottle_blame",
    )
    # NOTE: bottle_deny（リリィ離反ルート）は invoke* + LayerDrama.Activate の
    # NullReferenceException が解消されるまで一時的に無効化
    # .choice(
    #     bottle_deny,
    #     "何も知らない。君の管理ミスじゃないか？",
    #     "I know nothing. Isn't this your management error?",
    #     "我什么都不知道。这不是你的管理失误吗？",
    #     text_id="c_bottle_deny",
    # )

    # 瓶の選択肢反応
    builder.step(bottle_confess).say(
        "lily_r4",
        "……そう。",
        "...I see.",
        "……是吗。",
        actor=lily,
    ).say(
        "narr_14",
        "リリィの肩が小刻みに震えている。",
        "Lily's shoulders tremble slightly.",
        "莉莉的肩膀微微颤抖着。",
        actor=narrator,
    ).say(
        "lily_15",
        "……ふふ、サキュバスが人間に裏切られるなんて、滑稽な話だわ。",
        "...Hehe, a succubus being betrayed by a human... how absurd.",
        "……呵呵，魅魔被人类背叛，真是滑稽的故事呢。",
        actor=lily,
    ).say(
        "narr_15",
        "彼女は深く息を吐き、再びあなたを見つめる。",
        "She exhales deeply and gazes at you once more.",
        "她深深叹了口气，再次注视着您。",
        actor=narrator,
    ).say(
        "lily_16",
        "でも……でもね。あなたが今、正直に話してくれたこと……それだけは、評価します。",
        "But... but you see. The fact that you spoke honestly to me just now... that alone, I appreciate.",
        "但是……但是呢。您现在能对我坦诚相告……仅凭这一点，我还是认可的。",
        actor=lily,
    ).say(
        "lily_17",
        "『嘘つき』よりは、まだ救いがある。",
        "There is still redemption for you, compared to a 'liar.'",
        "比起「说谎者」，您还有救。",
        actor=lily,
    ).say(
        "lily_18",
        "……私は、あなたを許すわ。ただし、二度目はない。次にあなたが私を欺いたら……その時は、この爪であなたの喉を裂きます。約束よ。",
        "...I shall forgive you. However, there will be no second time. If you deceive me again... then I will tear out your throat with these claws. That is a promise.",
        "……我原谅您。但不会有第二次。下次您再欺骗我的话……届时，我会用这双利爪撕裂您的喉咙。这是约定。",
        actor=lily,
    ).set_flag(
        Keys.LILY_BOTTLE_CONFESSION, FlagValues.LilyBottleConfession.CONFESSED
    ).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.Makuma2ConfessToLily();"
    ).jump(after_bottle)

    builder.step(bottle_blame).say(
        "lily_r5",
        "……そう、ですか。",
        "...Is that so.",
        "……是这样吗。",
        actor=lily,
    ).say(
        "narr_16",
        "リリィが破片を丁寧に片付け、いつもの事務的な表情に戻る。",
        "Lily carefully cleans up the fragments and returns to her usual businesslike expression.",
        "莉莉仔细地收拾好碎片，恢复了平时公事公办的表情。",
        actor=narrator,
    ).say(
        "lily_19",
        "それなら仕方ありませんね。ゼクという男は、そういう生き物ですから。",
        "Then it cannot be helped. That man Zek is simply that kind of creature.",
        "那也是没办法的事呢。泽克那个人，就是那种生物。",
        actor=lily,
    ).say(
        "lily_20",
        "……さあ、虚空の心臓の製作に取り掛かりましょう。時間がありません。",
        "...Now then, let us proceed with crafting the Heart of the Void. We have no time to waste.",
        "……好了，让我们开始制作虚空之心吧。时间紧迫。",
        actor=lily,
    ).say(
        "narr_17",
        "リリィの尻尾だけが、不機嫌そうに床を叩いている。",
        "Only Lily's tail betrays her displeasure, tapping irritably against the floor.",
        "只有莉莉的尾巴不满地拍打着地板。",
        actor=narrator,
    ).set_flag(
        Keys.LILY_BOTTLE_CONFESSION, FlagValues.LilyBottleConfession.BLAMED_ZEK
    ).action("eval", param="Elin_SukutsuArena.ArenaManager.Makuma2BlameZek();").jump(
        after_bottle
    )

    builder.step(bottle_deny).say(
        "narr_18",
        "リリィの表情が凍りつく。",
        "Lily's expression freezes.",
        "莉莉的表情僵住了。",
        actor=narrator,
    ).say(
        "narr_19",
        "リリィの周囲に氷のような魔力の波動が広がる。",
        "An icy wave of magical power spreads around Lily.",
        "冰冷的魔力波动在莉莉周围扩散开来。",
        actor=narrator,
    ).say(
        "lily_21",
        "……そうですか。私の、管理ミス。",
        "...I see. My management error.",
        "……是这样啊。我的管理失误。",
        actor=lily,
    ).say(
        "lily_22",
        "ふふふ……ええ、私が、あなたという『獣』を『人間』だと勘違いしていた。それが最大のミスでした。",
        "Hehe... Yes, my greatest mistake was mistaking a 'beast' like you for a 'human.'",
        "呵呵呵……是啊，我把您这头「野兽」误认为是「人类」了。那才是最大的失误。",
        actor=lily,
    ).say(
        "lily_23",
        "結構です。どうぞ、虚空の心臓でも何でも作って、アスタロト様に挑んでください。……私は、もうあなたに期待しません。",
        "Very well. Please, go ahead and craft the Heart of the Void or whatever you wish, and challenge Lord Astaroth. ...I no longer have any expectations of you.",
        "好的。请随便制作虚空之心还是什么，然后去挑战阿斯塔罗特大人吧。……我对您已经不抱任何期望了。",
        actor=lily,
    ).set_flag(
        Keys.LILY_BOTTLE_CONFESSION, FlagValues.LilyBottleConfession.DENIED
    ).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.Makuma2DenyInvolvement();"
    ).jump(lily_departure)

    # リリィが去る描写
    builder.step(lily_departure).say(
        "narr_departure1",
        "リリィは羽根ペンを机に置き、ゆっくりと立ち上がった。",
        "Lily places her quill pen on the desk and slowly rises.",
        "莉莉将羽毛笔放在桌上，缓缓站起身来。",
        actor=narrator,
    ).say(
        "lily_departure1",
        "……もう、あなたの顔を見たくありません。",
        "...I don't want to see your face anymore.",
        "……我再也不想见到您的脸了。",
        actor=lily,
    ).say(
        "lily_departure2",
        "探さないでください。……私はもう、あなたの味方ではない。",
        "Don't look for me. ...I am no longer your ally.",
        "请不要找我。……我已经不再是您的伙伴了。",
        actor=lily,
    ).say(
        "narr_departure2",
        "リリィは振り返ることなく、受付の奥へと姿を消した。",
        "Lily disappears into the back of the reception without looking back.",
        "莉莉头也不回地消失在接待处深处。",
        actor=narrator,
    ).shake().say(
        "narr_departure3",
        "あなたは、静まり返ったロビーに取り残された。",
        "You are left alone in the silent lobby.",
        "您被留在了寂静的大厅中。",
        actor=narrator,
    ).complete_quest(QuestIds.MAKUMA2).finish()

    builder.step(after_bottle).jump(scene2)

    # ========================================
    # シーン2: 虚空の心臓の製作
    # ========================================
    builder.step(scene2).play_bgm("BGM/Mystical_Ritual").say(
        "lily_24",
        "必要な素材を言い渡すわ。",
        "I shall inform you of the required materials.",
        "我来告诉您所需的材料。",
        actor=lily,
    ).say(
        "lily_25",
        "「心臓」を１つ、それから「大地のルーンモールド」を１つ。これらを私に渡してください。",
        "One 'heart,' and one 'rune mold of earth.' Please deliver these to me.",
        "「心脏」一颗，然后「大地的符文模具」一个。请把这些交给我。",
        actor=lily,
    ).say(
        "lily_26",
        "……素材が揃ったら、私が『虚空の心臓』を組み上げます。",
        "...Once the materials are gathered, I shall assemble the 'Heart of the Void.'",
        "……材料齐备之后，由我来组装「虚空之心」。",
        actor=lily,
    ).jump(check_materials)

    # 素材チェック
    builder.step(check_materials).say(
        "lily_check",
        "……さて、素材はお持ちですか？",
        "...Now then, do you have the materials?",
        "……那么，材料带来了吗？",
        actor=lily,
    )

    # 条件付き選択肢: 両方の素材を持っている場合のみ「渡す」が表示される
    # Note: CWLでは & での条件結合をサポートしていないため、if と if2 を別々に使用
    builder.choice_if2(
        has_materials,
        "素材を渡す（心臓×1、ルーンモールド×1）",
        "hasItem,heart",
        "hasItem,rune_mold_earth",
        text_en="Hand over the materials (heart x1, rune mold x1)",
        text_cn="交付材料（心脏×1、符文模具×1）",
        text_id="c_give_materials",
    ).choice(
        no_materials,
        "まだ揃っていない",
        "Not yet gathered.",
        "还没有凑齐。",
        text_id="c_no_materials",
    )

    # 素材あり → 消費して製作へ
    builder.step(has_materials).cs_eval(
        'var heart = EClass.pc.things.Find(t => t.id == "heart"); if(heart != null) heart.Destroy();'
    ).cs_eval(
        'var mold = EClass.pc.things.Find(t => t.id == "rune_mold_earth"); if(mold != null) mold.Destroy();'
    ).say(
        "lily_take",
        "……ありがとうございます。優秀ですこと。",
        "...Thank you. How very capable of you.",
        "……谢谢您。真是能干呢。",
        actor=lily,
    ).jump(crafting_complete)

    # 素材なし → 会話終了（再度話しかけで再試行可能）
    builder.step(no_materials).say(
        "lily_no_mat",
        "そうですか。素材がまだ揃っていないのですね。",
        "I see. The materials are not yet gathered.",
        "是吗。材料还没有凑齐呢。",
        actor=lily,
    ).say(
        "lily_no_mat2",
        "心臓は簡単でしょう。ルーンモールドは、ご自身で魔法石から磨き上げる必要があります。",
        "The heart should be simple enough. As for the rune mold, you will need to craft it yourself from a magic stone.",
        "心脏应该很简单。符文模具需要您自己用魔法石打磨制作。",
        actor=lily,
    ).say(
        "lily_no_mat3",
        "……揃ったらまた声をかけてくださいな。",
        "...Please speak to me again once you have gathered them.",
        "……凑齐之后请再来找我。",
        actor=lily,
    ).finish()

    # 製作完了
    builder.step(crafting_complete).play_bgm("BGM/Lily_Tranquil").say(
        "narr_20",
        "リリィは素材を受け取ると、それらを机の上に並べた。",
        "Lily receives the materials and arranges them on the desk.",
        "莉莉收下材料后，将它们排列在桌上。",
        actor=narrator,
    ).say(
        "narr_20_1",
        "彼女は素材に指先を当て、何やら呪文を唱え始める。",
        "She places her fingertips on the materials and begins chanting an incantation.",
        "她将指尖触碰材料，开始念诵某种咒语。",
        actor=narrator,
    ).say(
        "narr_20_2",
        "心臓とルーンの鋳型が淡く光り、徐々に融合していく。",
        "The heart and rune mold glow faintly and gradually begin to fuse.",
        "心脏和符文模具发出微光，逐渐融合在一起。",
        actor=narrator,
    ).say(
        "narr_20_3",
        "数分後、淡い青白い光を放つ、拳大の結晶が完成した。",
        "A few minutes later, a fist-sized crystal emitting a pale bluish-white light is completed.",
        "几分钟后，一颗散发着淡蓝白光芒、拳头大小的结晶完成了。",
        actor=narrator,
    ).say(
        "lily_craft",
        "……これが『虚空の心臓』。稼働させるには、まだ調整が必要ですが。私が調整する間、少しお待ち下さいね。",
        "...This is the 'Heart of the Void.' It still requires calibration to activate. Please wait while I make the adjustments.",
        "……这就是「虚空之心」。要让它运作还需要进行调整。在我调整期间，请稍等一下。",
        actor=lily,
    ).jump(scene3)

    # ========================================
    # シーン3: バルガスの忠告
    # ========================================
    builder.step(scene3).play_bgm("BGM/Ominous_Suspense_01").focus_chara(
        Actors.BALGAS
    ).say(
        "narr_21",
        "リリィの調整を待つ間、あなたがアリーナのロビーを歩いていると、酒臭い、しかし岩のように力強い手があなたの腕を掴んだ。",
        "While waiting for Lily's adjustments, as you walk through the Arena lobby, a hand reeking of alcohol yet strong as rock grabs your arm.",
        "在等待莉莉调整的期间，当您在角斗场大厅走动时，一只带着酒气却坚如磐石的手抓住了您的手臂。",
        actor=narrator,
    ).say(
        "narr_22",
        "バルガスだ。彼はあなたを人気の無い柱の影へと引きずり込み、周囲を警戒しながら低く、掠れた声で話し始めた。",
        "It's Vargus. He drags you into the shadow of an empty pillar and begins speaking in a low, raspy voice while watching his surroundings.",
        "是巴尔加斯。他把您拖到无人的柱子阴影处，警惕着周围，用低沉沙哑的声音说道。",
        actor=narrator,
    ).say(
        "balgas_1",
        "……おい、待て。最近、ゼクの野郎とよく話してるようだな。",
        "...Hey, hold up. You've been talking to that bastard Zek a lot lately.",
        "……喂，等等。你小子最近好像经常和泽克那家伙聊天啊。",
        actor=balgas,
    ).say(
        "balgas_2",
        "お前は、あいつの吐く『毒』を飲みすぎだ。ヌルの記憶チップだの、世界のバグだの……ゼクが語る『真実』ってのはな、お前の足を止めるための泥濘（ぬかるみ）なんだよ。",
        "You've been drinking too much of his 'poison.' Memory chips of Null, bugs in the world... The 'truth' Zek speaks of is nothing but a quagmire meant to stop you in your tracks.",
        "你小子喝了太多那家伙吐出的「毒药」了。什么Nul的记忆芯片，什么世界的漏洞……泽克说的那些「真相」，不过是让你停下脚步的泥潭罢了。",
        actor=balgas,
    ).say(
        "balgas_3",
        "あいつはな、ただの商人じゃねえ。……『剥製師（はくせいし）』なんだ。",
        "He ain't just a merchant. ...He's a 'taxidermist.'",
        "那家伙可不只是个商人。……他是「标本师」。",
        actor=balgas,
    ).say(
        "balgas_4",
        "英雄が絶望し、魂が折れる瞬間を待っている。そして、その『最も美しい瞬間』を切り取って、永遠にコレクションしやがる。",
        "He waits for the moment heroes despair and their souls break. Then he captures that 'most beautiful moment' and adds it to his eternal collection.",
        "他在等待英雄绝望、灵魂崩溃的瞬间。然后把那「最美的瞬间」切割下来，永远收藏起来。",
        actor=balgas,
    ).say(
        "balgas_5",
        "カインの時もそうだ……あいつはただ、お前が友を裏切るかどうかをニヤニヤしながら見てたんだよ。",
        "It was the same with Cain... That bastard was just watching with a smirk, seeing whether you'd betray your friend or not.",
        "凯恩那次也是……那家伙只是皮笑肉不笑地看着，看你会不会背叛朋友。",
        actor=balgas,
    ).jump(check_kain)

    # ========================================
    # 条件分岐: カインの魂について (kain_soul_choice == SOLD)
    # ========================================
    # KainSoulChoice: 0=RETURNED, 1=SOLD
    builder.step(check_kain).switch_flag(
        Keys.KAIN_SOUL_CHOICE,
        [
            after_kain,  # 0: バルガスに返した場合
            kain_event,  # 1: ゼクに売った場合
            after_kain,  # fallback
        ],
    )

    builder.step(kain_event).say(
        "narr_23",
        "バルガスは一瞬、言葉を止め、あなたを鋭く見つめる。",
        "Vargus pauses for a moment and stares at you sharply.",
        "巴尔加斯停顿了一下，锐利地盯着您。",
        actor=narrator,
    ).say(
        "balgas_6",
        "……おい。一つ、聞いていいか。",
        "...Hey. Mind if I ask you something?",
        "……喂。能问你一件事吗。",
        actor=balgas,
    ).say(
        "balgas_7",
        "あの時……カインの魂の欠片。本当に、見つからなかったのか？",
        "Back then... Cain's soul fragment. Did you really not find it?",
        "那时候……凯恩的灵魂碎片。真的没找到吗？",
        actor=balgas,
    ).say(
        "narr_24",
        "バルガスの手が微かに震えている。",
        "Vargus's hands are trembling slightly.",
        "巴尔加斯的手在微微颤抖。",
        actor=narrator,
    ).say(
        "balgas_8",
        "いや……俺の勘違いかもしれねえ。だが、ゼクの野郎がやたらと上機嫌だった時期があってな。……まるで、『最高の獲物』を手に入れたような顔をしてやがった。",
        "No... maybe I'm just imagining things. But there was a time when that bastard Zek was in an unusually good mood. ...Like he'd gotten his hands on the 'finest prey.'",
        "不……也许是老子想多了。但有段时间泽克那家伙异常高兴……一副得到了「最上等猎物」的嘴脸。",
        actor=balgas,
    ).say(
        "balgas_9",
        "……お前が、あいつに何か『売った』なんてことは……ないよな？",
        "...You didn't 'sell' anything to him... did you?",
        "……你不会把什么东西「卖」给那家伙了吧……？",
        actor=balgas,
    ).jump(kain_choice)

    # カインの魂についての選択肢
    builder.choice(
        kain_confess,
        "……すまない。カインの魂を、ゼクに売った",
        "...I'm sorry. I sold Cain's soul to Zek.",
        "……抱歉。我把凯恩的灵魂卖给泽克了。",
        text_id="c_kain_confess",
    ).choice(
        kain_lie,
        "見つからなかった。ゼクとは関係ない",
        "I couldn't find it. It has nothing to do with Zek.",
        "没找到。和泽克没关系。",
        text_id="c_kain_lie",
    )

    # カインの選択肢反応
    builder.step(kain_confess).say(
        "narr_25",
        "バルガスは深く息を吐き、拳を握りしめる。",
        "Vargus exhales deeply and clenches his fists.",
        "巴尔加斯深深叹了口气，握紧拳头。",
        actor=narrator,
    ).say(
        "narr_26",
        "バルガスの目に、怒りと失望、そして深い悲しみが宿る。",
        "Anger, disappointment, and deep sorrow dwell in Vargus's eyes.",
        "巴尔加斯的眼中浮现出愤怒、失望，以及深深的悲伤。",
        actor=narrator,
    ).say(
        "balgas_10",
        "……そうか。お前は、俺の相棒を……カインを、二度殺したんだな。",
        "...I see. So you killed my partner... you killed Cain twice.",
        "……是吗。你小子把老子的搭档……凯恩，杀了两次啊。",
        actor=balgas,
    ).say(
        "balgas_11",
        "一度目は、異次元の錆に魂を食われて。二度目は、お前に売り飛ばされて。",
        "The first time, his soul was devoured by the rust of another dimension. The second time, you sold him off.",
        "第一次是灵魂被异次元的锈蚀吞噬。第二次是被你卖掉。",
        actor=balgas,
    ).say(
        "balgas_12",
        "……ハッ、俺はなんてマヌケだ。お前を『カイン以上の戦士』だと思っちまってた。",
        "...Hah, what a fool I am. I thought you were 'a warrior greater than Cain.'",
        "……哈，老子真是个蠢货。竟然以为你是「超越凯恩的战士」。",
        actor=balgas,
    ).say(
        "balgas_13",
        "……俺はこれでも、色々と経験してきてる身だ。だから、お前を殺したりはしない。",
        "...I've been through a lot in my time. So I won't kill you.",
        "……老子也算是经历过不少事的人了。所以不会杀你。",
        actor=balgas,
    ).say(
        "balgas_14",
        "だが……もう二度と、俺に『友』として話しかけるな。お前は今日から、ただの『契約闘士』だ。それ以上でも、それ以下でもねえ。",
        "But... never speak to me as a 'friend' again. From today, you're just a 'contracted gladiator.' Nothing more, nothing less.",
        "但是……以后别再以「朋友」的身份跟老子说话了。从今天起，你只是个「签约斗士」。不多也不少。",
        actor=balgas,
    ).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.Makuma2ConfessAboutKain();"
    ).jump(after_kain)

    builder.step(kain_lie).say(
        "narr_27",
        "バルガスは視線を逸らし、深く息を吐く。",
        "Vargus averts his gaze and exhales deeply.",
        "巴尔加斯移开视线，深深叹了口气。",
        actor=narrator,
    ).say(
        "narr_28",
        "バルガスが信じたいが、心の奥では真実を察している。",
        "Vargus wants to believe, but deep down he senses the truth.",
        "巴尔加斯想要相信，但内心深处已经察觉到真相。",
        actor=narrator,
    ).say(
        "balgas_15",
        "……そうか。",
        "...I see.",
        "……是吗。",
        actor=balgas,
    ).say(
        "balgas_16",
        "……なら、いい。……いや、よかねえな。俺の勘が外れてることを祈るよ。",
        "...Then that's fine. ...No, it ain't fine. I pray my gut is wrong.",
        "……那就算了。……不，算个屁。希望老子的直觉是错的。",
        actor=balgas,
    ).say(
        "narr_29",
        "バルガスがあなたの肩を叩くが、その手には以前のような力強さがない。",
        "Vargus pats your shoulder, but his hand lacks the strength it once had.",
        "巴尔加斯拍了拍您的肩膀，但那只手已没有了以前的力道。",
        actor=narrator,
    ).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.Makuma2LieAboutKain();"
    ).jump(after_kain)

    # ========================================
    # 共通後続
    # ========================================
    builder.step(after_kain).say(
        "balgas_17",
        "いいか……あいつの言葉に耳を貸すな。真実なんてのは、アスタロトの首を獲った後に、自分の目で見りゃいい。",
        "Listen... don't lend an ear to that guy's words. The truth? You can see it with your own eyes after you take Astaroth's head.",
        "听好了……别听那家伙的话。什么真相，取下阿斯塔罗特的首级之后，自己亲眼去看就行了。",
        actor=balgas,
    ).say(
        "balgas_18",
        "影に潜む奴に背中を見せるなよ。……お前まで『物言わぬコレクション』にされるのは、俺の酒が不味くなるからな。",
        "Don't show your back to those who lurk in the shadows. ...If you become another 'silent collection piece,' my drinks will taste sour.",
        "别把后背露给潜藏在暗处的家伙。……你要是也变成「沉默的收藏品」的话，老子的酒喝起来会很难受的。",
        actor=balgas,
    ).jump(final_choice)

    # プレイヤーの選択肢
    builder.choice(
        final_thanks,
        "……ありがとう",
        "...Thank you.",
        "……谢谢。",
        text_id="c_final_thanks",
    ).choice(
        final_trust,
        "バルガスの言葉を信じる",
        "I believe Vargus's words.",
        "我相信巴尔加斯的话。",
        text_id="c_final_trust",
    ).choice(
        final_knowledge,
        "ゼクの知識をさらに求める",
        "I seek more of Zek's knowledge.",
        "我还想获取更多泽克的知识。",
        text_id="c_final_knowledge",
    )

    # 選択肢反応
    builder.step(final_thanks).say(
        "balgas_r1",
        "……気をつけろ。生きて戻ってこい。",
        "...Be careful. Come back alive.",
        "……小心点。活着回来。",
        actor=balgas,
    ).jump(scene4)

    builder.step(final_trust).say(
        "balgas_r2",
        "……よし。それでいい。",
        "...Good. That's the way.",
        "……很好。这样就对了。",
        actor=balgas,
    ).action("eval", param="Elin_SukutsuArena.ArenaManager.Makuma2ChooseTrust();").jump(
        scene4
    )

    builder.step(final_knowledge).say(
        "balgas_r3",
        "……お前の好きにしろ。だが、後悔するなよ。",
        "...Do as you like. But don't come crying to me later.",
        "……随你便。但别后悔。",
        actor=balgas,
    ).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.Makuma2ChooseKnowledge();"
    ).jump(scene4)

    # ========================================
    # シーン4: 虚空の心臓の設置
    # ========================================
    builder.step(scene4).play_bgm("BGM/Lily_Tranquil").focus_chara(Actors.LILY).say(
        "narr_30",
        "バルガスとの会話を終え、あなたはリリィのもとへ戻った。",
        "Having finished your conversation with Vargus, you return to Lily.",
        "结束与巴尔加斯的对话后，您回到了莉莉身边。",
        actor=narrator,
    ).say(
        "lily_27",
        "お戻りになりましたか。……さて、虚空の心臓の設置を始めましょう。",
        "You have returned. ...Now then, let us begin the installation of the Heart of the Void.",
        "您回来了。……那么，让我们开始安装虚空之心吧。",
        actor=lily,
    ).say(
        "narr_31",
        "リリィが虚空の心臓をアリーナの中心へ運び、設置の儀式を始める。",
        "Lily carries the Heart of the Void to the center of the Arena and begins the installation ritual.",
        "莉莉将虚空之心运到角斗场中央，开始进行安装仪式。",
        actor=narrator,
    ).say(
        "narr_31_2",
        "淡い青白い光がアリーナ全体を包み込み、空間の震動が徐々に収まっていく。",
        "A pale bluish-white light envelops the entire Arena, and the spatial tremors gradually subside.",
        "淡蓝白色的光芒笼罩了整个角斗场，空间的震动逐渐平息下来。",
        actor=narrator,
    ).say(
        "lily_28",
        "……完璧です。あなたの協力のおかげで、この異次元の構造が安定しました。",
        "...Perfect. Thanks to your cooperation, the structure of this dimension has been stabilized.",
        "……完美。多亏您的协助，这个异次元的结构已经稳定下来了。",
        actor=lily,
    ).say(
        "lily_30",
        "報酬をお渡ししましょう。",
        "Allow me to present your reward.",
        "让我把报酬交给您吧。",
        actor=lily,
    ).action("eval", param="Elin_SukutsuArena.ArenaManager.GrantMakuma2Reward();").jump(
        ending
    )

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending).complete_quest(QuestIds.MAKUMA2).finish()
