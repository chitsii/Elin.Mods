"""
08_lily_private.md - 紫煙と観察者の深淵
リリィの私室での親密なイベント
"""

from arena.builders import DramaBuilder
from arena.data import Actors, Keys, QuestIds, SessionKeys


def define_lily_private(builder: DramaBuilder):
    """
    リリィの私室
    シナリオ: 08_lily_private.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_invitation")
    choice1 = builder.label("choice1")
    react1_accept = builder.label("react1_accept")
    react1_scheme = builder.label("react1_scheme")
    react1_nod = builder.label("react1_nod")
    scene2 = builder.label("scene2_private_room")
    choice2 = builder.label("choice2")
    react2_no = builder.label("react2_no")
    react2_tasty = builder.label("react2_tasty")
    react2_silent = builder.label("react2_silent")
    scene3 = builder.label("scene3_observation")
    choice3 = builder.label("choice3")
    react3_ok = builder.label("react3_ok")
    react3_rest = builder.label("react3_rest")
    react3_watch = builder.label("react3_watch")
    scene4 = builder.label("scene4_farewell")
    choice4 = builder.label("choice4")
    react4_thanks = builder.label("react4_thanks")
    react4_again = builder.label("react4_again")
    react4_nod = builder.label("react4_nod")
    # 契約タイプ別の説明分岐
    contract_branch = builder.label("contract_branch")
    contract_mana_meat = builder.label("contract_mana_meat")
    contract_mana_bond = builder.label("contract_mana_bond")
    choice4_entry = builder.label("choice4_entry")
    ending = builder.label("ending")

    # ========================================
    # シーン1: カウンター越しの視線
    # ========================================
    builder.step(main).play_bgm("BGM/Lobby_Normal").say(
        "narr_1",
        "戦いの傷を癒し、戦果を報告しに受付へ向かったあなた。\nリリィはいつものように書類を整理しているが、その動きはどこか緩慢だ。\n彼女の細長い瞳が、あなたの全身を這うように舐め回す。",
        "Having tended to your wounds from battle, you head to the reception desk to report your results.\nLily is sorting through documents as usual, but her movements seem somehow languid.\nHer slender eyes crawl across your entire body, drinking in every detail.",
        "疗愈了战斗的伤势后，您前往接待处汇报战果。\n莉莉一如往常地整理着文件，但她的动作显得有些慵懒。\n她细长的眼眸如同舔舐般缓缓扫过您的全身。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.LILY).say(
        "lily_1",
        "……おかえりなさい。\n観客席からの『供物』で、少しばかり身だしなみが乱れているようですね。ふふ、でも……今のあなたからは、とても複雑で、芳醇な香りがします。",
        "...Welcome back.\nIt seems the 'offerings' from the spectators have left you a bit disheveled. Hehe, but... right now, you exude such a complex, intoxicating aroma.",
        "……您回来了呢。\n观众席上的'贡品'似乎让您的仪容有些凌乱呢。呵呵，不过……现在的您，散发着如此复杂而醇厚的香气。",
        actor=lily,
    ).say("narr_4", "彼女は羽根ペンを置き、あなたに近づく。", "She sets down her quill and approaches you.", "她放下羽毛笔，向您走近。", actor=narrator).say(
        "lily_3",
        "絶望と、不屈。そして、ほんの少しの『殺意』が混じり合った、熟成した魂の匂い。\n……バルガスの酒臭い怒声が響くこの場所では、その香りを十分に楽しめません。少し、私の『私室』へいらっしゃいませんか？",
        "Despair and determination. And just a hint of 'killing intent' mingled together... the scent of a well-aged soul.\n...I cannot fully savor that fragrance in this place, where Balgas's alcohol-soaked bellowing echoes constantly. Won't you come to my private chambers for a moment?",
        "绝望与不屈。还有一丝'杀意'交织其中……这是陈酿灵魂的气息。\n……在这个回荡着巴尔加斯酒气熏天怒吼的地方，我无法尽情品味那香气。要不要来我的'私室'坐坐呢？",
        actor=lily,
    ).say(
        "lily_5",
        "仕事の話ではありません。ただ、観察者として……あなたのその魂を、もう少し近くで愛でたいのです。",
        "This isn't about work. I simply wish, as an observer... to admire that soul of yours from a little closer.",
        "这不是工作上的事。只是作为一个观察者……我想更近一些欣赏您的灵魂呢。",
        actor=lily,
    )

    # プレイヤーの選択肢1
    builder.choice(react1_accept, "……分かった。行こう", "...Alright. Let's go.", "……好吧。走吧。", text_id="c1_accept").choice(
        react1_scheme, "何か企んでいるのか？", "Are you scheming something?", "你在打什么主意？", text_id="c1_scheme"
    ).choice(react1_nod, "（無言で頷く）", "(Nod silently)", "（无言地点头）", text_id="c1_nod")

    # 選択肢反応1
    builder.step(react1_accept).say(
        "lily_r1", "ふふ、素直ですこと。では、こちらへどうぞ。", "Hehe, how obedient. This way, please.", "呵呵，真是坦率呢。那么，请这边走。", actor=lily
    ).jump(scene2)

    builder.step(react1_scheme).say(
        "lily_r2",
        "まあ、警戒心がおありで。ご心配なく、ただの社交ですよ。……多分。",
        "My my, such caution. Don't worry, it's merely social pleasantries. ...Probably.",
        "哎呀，您真是警觉呢。请放心，只是单纯的社交而已哦。……大概吧。",
        actor=lily,
    ).jump(scene2)

    builder.step(react1_nod).say(
        "lily_r3",
        "……無口ですが、了承はしていただけたようですね。どうぞ、ついてきてください。",
        "...Taciturn, but it seems you've agreed. Please, follow me.",
        "……虽然不善言辞，但看来您同意了呢。请跟我来吧。",
        actor=lily,
    ).jump(scene2)

    # ========================================
    # シーン2: サキュバスの私室
    # ========================================
    builder.step(scene2).play_bgm("BGM/Lily_Private_Room").say(
        "narr_5",
        "ロビーの裏手にある重厚な黒檀の扉。そこを潜った瞬間、大気の重さが変わる。\n部屋全体を支配するのは、甘美でありながら肺の奥を痺れさせるような「紫煙」の香。",
        "A heavy ebony door behind the lobby. The moment you pass through, the very weight of the air changes.\nThe entire room is dominated by the scent of 'purple haze' - sweet yet numbing to the depths of your lungs.",
        "大厅后方有一扇厚重的乌木门。穿过的瞬间，空气的重量似乎都变了。\n整个房间弥漫着'紫烟'的气息--甜美却让人的肺腑深处都感到麻痹。",
        actor=narrator,
    ).say(
        "narr_7",
        "床には柔らかな魔獣の毛皮が敷き詰められ、壁には異次元の珍奇な書物や、何かの魂が閉じ込められたと思わしき脈打つ宝石が飾られている。\nリリィは、毒々しいほど鮮やかなソファに深く腰掛け、事務服の襟元をわずかに緩めた。",
        "Soft magical beast furs carpet the floor, while the walls display bizarre tomes from other dimensions and pulsating gems that seem to contain trapped souls.\nLily sinks deeply into a garishly vibrant sofa and loosens the collar of her office attire ever so slightly.",
        "地上铺满了柔软的魔兽皮毛，墙上装饰着来自异次元的奇异书籍，以及似乎封印着某些灵魂的跳动宝石。\n莉莉深深地陷入一张艳丽得有些妖冶的沙发中，微微松开了制服的领口。",
        actor=narrator,
    ).say(
        "lily_6",
        "さあ、遠慮なさらず。\nここはアリーナの法則からも、神々の視線からも隔絶された場所。",
        "Now then, don't be shy.\nThis place is isolated from both the laws of the Arena and the gaze of the gods.",
        "来吧，请不要客气。\n这里是与角斗场的法则、与神明的目光都隔绝的地方。",
        actor=lily,
    ).say(
        "lily_8",
        "……今のあなたの戦い、実に滑稽で、そして美しかったわ。落下物に翻弄されながらも、その瞳から光が消えなかった。\n……あの瞬間、あなたの魂がどれほど甘く弾けたか、自覚はありますか？",
        "...Your battle just now was truly amusing, and beautiful. Even while being tossed about by falling debris, the light never left your eyes.\n...Do you realize how deliciously your soul burst forth in that moment?",
        "……您刚才的战斗，真是既滑稽又美丽呢。即使被落下的杂物摆布，您眼中的光芒也从未消失。\n……那一刻，您可知道自己的灵魂绽放得有多甜美吗？",
        actor=lily,
    )

    # プレイヤーの選択肢2
    builder.choice(react2_no, "……自覚はない", "...I wasn't aware.", "……我并不知道。", text_id="c2_no").choice(
        react2_tasty, "俺の魂がそんなに美味しそうか？", "Is my soul really that delicious?", "我的灵魂真有那么美味吗？", text_id="c2_tasty"
    ).choice(react2_silent, "（無言で聞く）", "(Listen silently)", "（沉默倾听）", text_id="c2_silent")

    # 選択肢反応2
    builder.step(react2_no).say(
        "lily_r4", "ふふ、そうでしょうね。当事者は気づかないものです。", "Hehe, I thought as much. Those in the moment rarely notice.", "呵呵，我想也是呢。当事人往往是察觉不到的。", actor=lily
    ).jump(scene3)

    builder.step(react2_tasty).say(
        "lily_r5", "ええ。……とても。食べてしまいたいくらいに。", "Yes. ...Very much so. Enough to make me want to devour you.", "是的。……非常美味。美味到让我想要将您吞噬呢。", actor=lily
    ).jump(scene3)

    builder.step(react2_silent).say(
        "lily_r6", "……無口ですが、その瞳は饒舌ですね。", "...Taciturn, but your eyes speak volumes.", "……虽然沉默寡言，但您的眼眸却很健谈呢。", actor=lily
    ).jump(scene3)

    # ========================================
    # シーン3: 観察と誘惑（捕食者の本能）
    # ========================================
    builder.step(scene3).say(
        "narr_9",
        "リリィは指先を唇に当て、クスクスと喉を鳴らす。\n彼女の尻尾が、まるで生き物のようにあなたの足首に絡みついた。",
        "Lily presses her fingertips to her lips, a soft chuckle escaping her throat.\nHer tail coils around your ankle like a living creature.",
        "莉莉将指尖抵在唇边，喉咙里发出轻柔的笑声。\n她的尾巴如同活物一般缠上了您的脚踝。",
        actor=narrator,
    ).say(
        "lily_10",
        "……あなたは不思議な人。\n多くの闘士は、ランクDに上がる頃には心が擦り切れて、ただの戦う機械になる。けれど、あなたからは『意志』の拍動が聞こえるの。",
        "...You are a curious one.\nMost gladiators have their hearts worn down to nothing by the time they reach Rank D, becoming mere fighting machines. But from you... I can hear the pulse of 'will.'",
        "……您真是个不可思议的人呢。\n大多数斗士在升到D级的时候，心灵就已经磨损殆尽，沦为单纯的战斗机器。但是从您身上……我能听到'意志'的脉动。",
        actor=lily,
    ).say(
        "narr_11", "サキュバスの本能が、抑えきれずに漏れ出す。", "The instincts of a succubus begin to leak through, no longer fully contained.", "魅魔的本能开始无法抑制地流露出来。", actor=narrator
    ).say(
        "lily_12",
        "……ねえ、一つ教えて。あなたは、どこまで登るつもり？その魂が完全に磨き上げられ、最高級の宝石になった時……\nそれを最初に手にするのは、私であってもいいかしら？",
        "...Tell me one thing. How high do you intend to climb? When that soul of yours has been polished to perfection, becoming the finest of gems...\n...would it be alright if I were the first to claim it?",
        "……呐，告诉我一件事。您打算攀登到什么高度呢？当您的灵魂被完全打磨，成为最高级的宝石时……\n第一个将它收入囊中的……可以是我吗？",
        actor=lily,
    ).say(
        "narr_12",
        "あなたは彼女の吐息に、甘美でありながら危険な「何か」を感じ取る。ーーそれは、捕食者の本能。",
        "You sense something in her breath - sweet yet dangerous. The instincts of a predator.",
        "您从她的呼吸中感受到某种甜美却危险的东西--那是捕食者的本能。",
        actor=narrator,
    ).shake().say("lily_14", "……ふふ、冗談よ。", "...Hehe, I'm joking.", "……呵呵，开玩笑的啦。", actor=lily).say(
        "narr_13", "彼女は一歩下がるが、その手は微かに震えている。", "She steps back, but her hands tremble ever so slightly.", "她后退一步，但她的手微微颤抖着。", actor=narrator
    ).say(
        "lily_mono1",
        "（……だめよ。この人に手をだしてはいけない。）\n（でも……この香り。この、熟した魂の甘い香り。……ああ、食べたい。喰らいたい。あなたの全てを、この牙で噛み砕いて、永遠に私の中に……）",
        "(...No. I mustn't lay a hand on this one.)\n(...But... this fragrance. This sweet scent of a ripened soul. ...Ah, I want to taste it. To devour it. To crush everything you are with these fangs, and keep you inside me forever...)",
        "（……不行。不能对这个人出手。）\n（但是……这香气。这成熟灵魂的甜美香气。……啊啊，好想品尝。好想吞噬。用这獠牙咬碎您的一切，永远留在我的体内……）",
        actor=lily,
    ).say(
        "narr_14",
        "リリィは自分の唇を噛み、その衝動を必死に抑え込む。彼女の尻尾が、苦しげに床を叩いた。",
        "Lily bites her own lip, desperately suppressing the urge. Her tail thrashes against the floor in anguish.",
        "莉莉咬住自己的嘴唇，拼命压抑着那股冲动。她的尾巴痛苦地拍打着地板。",
        actor=narrator,
    ).say(
        "lily_15",
        "……ごめんなさい。少し、気分が……。\nサキュバスにとって、『美味しそうな魂』を前にして我慢するのは……拷問に近いの。でも、あなたを傷つけたくない。……これは、初めての感覚。",
        "...I'm sorry. I feel a bit...\nFor a succubus, restraining oneself before a 'delicious-looking soul'... is akin to torture. But I don't want to hurt you. ...This is a new sensation for me.",
        "……对不起。我有点……\n对魅魔来说，在'看起来很美味的灵魂'面前忍耐……简直是一种酷刑。但是，我不想伤害您。……这是第一次有这种感觉呢。",
        actor=lily,
    )

    # プレイヤーの選択肢3
    builder.choice(react3_ok, "大丈夫か？", "Are you alright?", "你还好吗？", text_id="c3_ok").choice(
        react3_rest, "無理をするな", "Don't push yourself.", "不要勉强自己。", text_id="c3_rest"
    ).choice(react3_watch, "（静かに見守る）", "(Watch quietly)", "（静静地注视）", text_id="c3_watch")

    # 選択肢反応3
    builder.step(react3_ok).say(
        "lily_r7", "……ええ、大丈夫です。少し、驚いただけ。", "...Yes, I'm fine. Just a bit... startled.", "……是的，我没事。只是有点……吃惊而已。", actor=lily
    ).jump(scene4)

    builder.step(react3_rest).say(
        "lily_r8", "……ありがとう。でも、私は大丈夫。……多分。", "...Thank you. But I'm alright. ...Probably.", "……谢谢您。但我没事的。……大概吧。", actor=lily
    ).jump(scene4)

    builder.step(react3_watch).say(
        "lily_r9", "……あなた、本当に優しいのですね。", "...You really are kind, aren't you?", "……您真的很温柔呢。", actor=lily
    ).jump(scene4)

    # ========================================
    # シーン4: 別れの接吻（契約フィート付与）
    # ========================================
    builder.step(scene4).play_bgm("BGM/Lily_Tranquil").say(
        "narr_15",
        "彼女は立ち上がり、至近距離まで顔を近づける。\nサキュバス特有の、抗いがたい魔力の波動があなたを包み込んだ。",
        "She rises and brings her face close to yours.\nA wave of irresistible magical energy, unique to succubi, envelops you.",
        "她站起身来，将脸凑近到咫尺之距。\n魅魔特有的、令人无法抗拒的魔力波动将您包围。",
        actor=narrator,
    ).say(
        "lily_17",
        "……ふふ、先程の質問の答えは、またどこかで聞かせてもらうことにしましょう。\nこれは、私のお気に入りの『観察対象』への投資です。次からも、もっと美味しい絶望と勝利を私に見せてくださいね。",
        "...Hehe, I'll have you answer my earlier question another time.\nConsider this an investment in my favorite 'subject of observation.' From now on, please continue to show me more delicious despair and victory.",
        "……呵呵，刚才那个问题的答案，改天再让您告诉我吧。\n这是我对心爱的'观察对象'的投资。今后也请继续向我展示更多美味的绝望与胜利吧。",
        actor=lily,
    ).say(
        "narr_17",
        "リリィがあなたの額に指先を触れる。瞬間、全身の疲労が霧散し、魔力が底から溢れ出すような感覚に陥る。",
        "Lily touches her fingertips to your forehead. In that instant, all fatigue dissipates from your body, and you feel magical power welling up from within.",
        "莉莉将指尖轻触您的额头。刹那间，全身的疲劳烟消云散，您感觉魔力从体内深处涌出。",
        actor=narrator,
    ).shake().action(
        "eval", param="Elin_SukutsuArena.ArenaManager.GrantLilyContract();"
    ).jump(contract_branch)

    # 契約タイプ分岐
    # sukutsu_lily_contract_type: 1=マナの体（エレア）, 2=マナとの絆（非エレア）
    builder.step(contract_branch).switch_flag(
        SessionKeys.LILY_CONTRACT_TYPE,
        [
            contract_mana_bond,  # 0: fallback（通常はここには来ない）
            contract_mana_meat,  # 1: マナの体（エレア）
            contract_mana_bond,  # 2: マナとの絆（非エレア）
        ],
    )

    # マナの体（エレア用）
    builder.step(contract_mana_meat).say(
        "lily_contract_1a",
        "……あら、あなたはエレアなのですね。ふふ、私たちサキュバスと相性が良い種族。\nあなたには『マナの体』を授けましょう。魂とマナが深く結びつき、体が傷ついても魔力で補えるようになります。",
        "...Oh my, you're an Elea. Hehe, a race quite compatible with us succubi.\nI shall bestow upon you the 'Mana Body.' Your soul and mana will be deeply intertwined, allowing magical power to compensate even when your body is wounded.",
        "……哎呀，您原来是艾利亚人呢。呵呵，和我们魅魔很相配的种族。\n我将赐予您'魔力之躯'。灵魂与魔力将深深交融，即使身体受伤也能用魔力来弥补。",
        actor=lily,
    ).say(
        "lily_contract_3a",
        "……ふふ、これであなたは少し死ににくくなりましたね。",
        "...Hehe, with this, you've become a little harder to kill.",
        "……呵呵，这样一来您就更难被杀死了呢。",
        actor=lily,
    ).say("contract_sys_a", "【マナの体】を獲得した。", "Acquired [Mana Body].", "获得了【魔力之躯】。", actor=narrator).jump(
        choice4_entry
    )

    # マナとの絆（非エレア用）
    builder.step(contract_mana_bond).say(
        "lily_contract_1b",
        "……あなたには『マナとの絆』を授けましょう。私の魔力の一部があなたの中に宿ります。\nマナが尽きた状態で無理に魔法を使うと、体に反動が来るでしょう？　このフィートがあれば、その反動ダメージが大幅に軽減されます。",
        "...I shall bestow upon you the 'Mana Bond.' A portion of my magical power will reside within you.\nWhen you force yourself to cast spells with depleted mana, your body suffers backlash, does it not? With this feat, that recoil damage will be greatly reduced.",
        "……我将赐予您'魔力之绊'。我的一部分魔力将寄宿在您体内。\n在魔力耗尽的状态下强行使用魔法，身体会承受反噬吧？有了这个技能，那反噬伤害将会大幅减轻。",
        actor=lily,
    ).say(
        "lily_contract_3b",
        "……ふふ、これで少しは無茶ができるようになりましたね。",
        "...Hehe, with this, you can push yourself a little more recklessly.",
        "……呵呵，这样您就能稍微放肆一点了呢。",
        actor=lily,
    ).say("contract_sys_b", "【マナとの絆】を獲得した。", "Acquired [Mana Bond].", "获得了【魔力之绊】。", actor=narrator).jump(
        choice4_entry
    )

    # プレイヤーの選択肢4
    builder.step(choice4_entry).say(
        "lily_19",
        "……これは、私からのささやかな贈り物。どうか受け取ってくださいね。",
        "...This is a modest gift from me. Please accept it.",
        "……这是我送给您的小小礼物。请务必收下呢。",
        actor=lily,
    ).choice(react4_thanks, "……ありがとう", "...Thank you.", "……谢谢。", text_id="c4_thanks").choice(
        react4_again, "次も来てもいいか？", "May I come again?", "下次还可以来吗？", text_id="c4_again"
    ).choice(react4_nod, "（無言で頷く）", "(Nod silently)", "（无言地点头）", text_id="c4_nod")

    # 選択肢反応4
    builder.step(react4_thanks).say(
        "lily_r10", "ふふ、どういたしまして。……またいらしてくださいね。", "Hehe, you're welcome. ...Do come again.", "呵呵，不客气。……请再来哦。", actor=lily
    ).jump(ending)

    builder.step(react4_again).say(
        "lily_r11",
        "……ええ。もちろん。あなたなら、いつでも歓迎いたします。",
        "...Yes. Of course. You are always welcome.",
        "……是的。当然可以。如果是您的话，随时欢迎。",
        actor=lily,
    ).jump(ending)

    builder.step(react4_nod).say(
        "lily_r12", "……無口ですが、気持ちは伝わりましたよ。", "...Taciturn as ever, but I understand your feelings.", "……虽然沉默寡言，但您的心意我收到了哦。", actor=lily
    ).jump(ending)

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending).complete_quest(QuestIds.LILY_PRIVATE).finish()
