"""
05_2_zek_steal_lily.md - ゼクの囁き『偽りの器、真実の対価』
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds


def define_zek_steal_bottle(builder: DramaBuilder):
    """
    ゼクの囁き「偽りの器、真実の対価」
    シナリオ: 05_2_zek_steal_lily.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You", "你")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily", "莉莉")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek", "泽克")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_shadow_call")
    choice1 = builder.label("choice1")
    react1_what = builder.label("react1_what")
    react1_manage = builder.label("react1_manage")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_betrayal_offer")
    choice2 = builder.label("choice2")
    react2_forbidden = builder.label("react2_forbidden")
    react2_betray = builder.label("react2_betray")
    react2_price = builder.label("react2_price")
    scene3 = builder.label("scene3_choice")
    refuse = builder.label("refuse")
    accept = builder.label("accept")
    scene4_aftermath = builder.label("scene4_aftermath")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 影からの招き
    # ========================================
    builder.step(main).play_bgm("BGM/Ominous_Suspense_01").say(
        "narr_1",
        "リリィが作った『共鳴瓶』。彼女は満足げに、その器を受付の棚に飾っていた。\n\nーー廊下を歩いていると、灯火が不自然に揺らぎ、あなたの足元の影が、まるで意思を持った沼のように長く伸びる。",
        "The 'Resonance Vessel' that Lily had crafted. She had displayed it on the reception shelf with evident satisfaction.\n\nAs you walked down the corridor, the torches flickered unnaturally, and your shadow stretched long like a sentient swamp.",
        "莉莉制作的「共鸣瓶」。她满意地将这件器皿摆放在前台的架子上。\n\n--走在走廊上时，火光不自然地摇曳，你脚下的影子如同拥有意识的沼泽般不断延伸。",
        actor=narrator,
    ).shake().say(
        "narr_3",
        "そこから、鎖の擦れる音と共に、ゼクが音もなく這い出してきた。",
        "From within, accompanied by the scraping of chains, Zek emerged soundlessly.",
        "伴随着锁链摩擦的声响，泽克悄无声息地从中爬出。",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "zek_1",
        "……素晴らしい。実に、溜息が出るほどに。\nあの器に溜まる残響……闘士たちの魂が刻んだ、消えゆく瞬間の記録。",
        "...Magnificent. Truly, it takes one's breath away.\nThe echoes pooling in that vessel... records of fleeting moments carved by gladiators' souls.",
        "……精彩绝伦。实在是，令人叹为观止。\n那器皿中积蓄的回响……是斗士们的灵魂所铭刻的、消逝瞬间的记录。",
        actor=zek,
    ).say(
        "zek_2",
        "あのサキュバスが作った『共鳴瓶』……実に惜しい傑作ですね、闘士殿。",
        "That 'Resonance Vessel' the succubus crafted... such a regrettable masterpiece, if you would, gladiator.",
        "那位魅魔制作的「共鸣瓶」……实在是令人惋惜的杰作呢，斗士阁下。",
        actor=zek,
    ).say(
        "narr_4",
        "彼は細長い指で、受付の方角を指し示す。",
        "He pointed toward the reception with his long, slender fingers.",
        "他用细长的手指指向前台的方向。",
        actor=narrator,
    ).say(
        "zek_3",
        "彼女が何と言ったかは知りませんが……彼女がその器で集めようとしているのは、単なる魔力ではありません。",
        "I'm afraid I don't know what she told you... but what she intends to collect with that vessel is not mere magical power.",
        "鄙人不知她对您说了什么……但她打算用那器皿收集的，绝非单纯的魔力。",
        actor=zek,
    ).say(
        "zek_4",
        "そこに溜まるのは、あなたがた闘士の戦いが生み出した、魂の残滓……。彼女はそれを啜り、あなたをより効率的に『管理』しようとしているのですよ。……ふふ、お気づきでしたか？",
        "What accumulates there is the residue of souls... born from your battles as gladiators. She sips it to 'manage' you more efficiently. Heh heh... were you aware of this investment she's making?",
        "积蓄其中的，是诸位斗士的战斗所产生的灵魂残渣……她吸食这些，以便更有效地「管理」您。……呵呵呵，您注意到了吗？",
        actor=zek,
    )

    # プレイヤーの選択肢
    builder.choice(
        react1_what, "……何が言いたい", "...What are you getting at?", "……你想说什么", text_id="c1_what"
    ).choice(
        react1_manage,
        "リリィに管理されているって？",
        "You're saying Lily is managing me?",
        "你说莉莉在管理我？",
        text_id="c1_manage",
    ).choice(
        react1_silent, "（無言で聞く）", "(Listen in silence)", "（沉默聆听）", text_id="c1_silent"
    )

    # 選択肢反応
    builder.step(react1_what).say(
        "zek_r1",
        "おや、警戒心がおありで。では、単刀直入に申し上げましょう。",
        "Oh my, such wariness. Very well, I shall be direct with you.",
        "哎呀，阁下颇有戒心呢。那么，容鄙人开门见山。",
        actor=zek,
    ).jump(scene2)

    builder.step(react1_manage).say(
        "zek_r2",
        "ええ、そうです。彼女はサキュバス。獲物を支配し、魂をすすり喰らう存在なのですよ。",
        "Indeed, precisely so. She is a succubus. A being that dominates her prey and sips upon their souls.",
        "是的，正是如此。她是魅魔。是支配猎物、吸食灵魂的存在呢。",
        actor=zek,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "zek_r3",
        "……ふふ、沈黙は賢明さの証。では、続けさせていただきましょう。",
        "...Heh heh, silence is proof of wisdom. Then, if you would allow me to continue.",
        "……呵呵呵，沉默是睿智的证明。那么，请容鄙人继续。",
        actor=zek,
    ).jump(scene2)

    # ========================================
    # シーン2: 裏切りの提案
    # ========================================
    builder.step(scene2).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_5",
        "ゼクは長い指で、懐から一つの『偽物』を取り出した。\n見た目はリリィが作った共鳴瓶と瓜二つだが、そのガラスの奥には、澱んだどす黒い霧が渦巻いている。",
        "With his long fingers, Zek produced a 'counterfeit' from his robes.\nIt looked identical to the Resonance Vessel Lily had crafted, but within its glass depths, a stagnant, pitch-black mist swirled.",
        "泽克用细长的手指从怀中取出一件「赝品」。\n外观与莉莉制作的共鸣瓶一模一样，但在玻璃深处，却有一团浑浊的漆黑迷雾在翻涌。",
        actor=narrator,
    ).say(
        "zek_5",
        "どうでしょう、一つ提案があります。\nあの器に溜まった記録は、彼女の手には余る。消費されるだけでは、あまりに惜しい。",
        "I have a proposition for you, if you would hear it.\nThe records accumulating in that vessel are beyond her capacity to properly utilize. It would be such a waste for them to merely be consumed.",
        "阁下意下如何，鄙人有一个提议。\n那器皿中积蓄的记录，她根本无法驾驭。仅仅被消耗掉的话，未免太过可惜。",
        actor=zek,
    ).say(
        "zek_6",
        "あの本物の器を盗み出し、私に譲ってはいただけませんか？代わりに、この『模造品』を棚に戻しておくのです。",
        "Would you be willing to steal the genuine vessel and transfer it to me? In exchange, you shall place this 'imitation' back on the shelf.",
        "能否请您将那真品盗出，转让给鄙人呢？作为交换，将这件「仿制品」放回架子上即可。",
        actor=zek,
    ).say(
        "narr_7",
        "彼は偽物の器をあなたの前に差し出す。",
        "He held out the counterfeit vessel before you.",
        "他将假的器皿递到你面前。",
        actor=narrator,
    ).say(
        "zek_7",
        "彼女は気づかないでしょう……。いえ、気づいた時には、彼女の『研究』はあなたの支配ではなく、私への捧げ物へとすり替わっている。",
        "She shall not notice... or rather, by the time she does, her 'research' will have transformed from your subjugation into an offering to me.",
        "她不会察觉的……不，等她发觉时，她的「研究」早已从支配您，变成了献给鄙人的供物。",
        actor=zek,
    ).say(
        "zek_8",
        "そしてあなたには、その誠実な裏切りの対価として、私が異次元のゴミ捨て場で拾い上げた珍品を差し上げましょう。……それと、特別な報酬も、ね。",
        "And as the cost of your sincere betrayal, I shall grant you a curiosity I salvaged from an interdimensional refuse heap. ...Along with a special bonus, of course.",
        "而作为您真诚背叛的代价，鄙人将赠予您一件从异次元垃圾场捡来的珍品。……当然，还有特别的报酬呢。",
        actor=zek,
    )

    # プレイヤーの選択肢
    builder.choice(
        react2_forbidden,
        "その珍品とは何だ？",
        "What is this curiosity?",
        "那珍品是什么？",
        text_id="c2_forbidden",
    ).choice(
        react2_betray,
        "リリィを裏切れと？",
        "You want me to betray Lily?",
        "你要我背叛莉莉？",
        text_id="c2_betray",
    ).choice(
        react2_price,
        "……代償は何だ",
        "...What's the cost?",
        "……代价是什么",
        text_id="c2_price",
    )

    # 選択肢反応
    builder.step(react2_forbidden).say(
        "zek_r4",
        "ふふ、興味を持たれましたか。では、お見せいたしましょう。",
        "Heh heh, you're interested? Then allow me to present it.",
        "呵呵呵，阁下有兴趣了吗。那么，请容鄙人为您展示。",
        actor=zek,
    ).jump(scene3)

    builder.step(react2_betray).say(
        "zek_r5",
        "裏切りとは聞こえが悪い。……『戦略的な選択』と呼びましょう。",
        "Betrayal has such an unpleasant ring to it. ...Let us call it a 'strategic investment.'",
        "背叛这个词未免太难听了。……就叫「战略性选择」吧。",
        actor=zek,
    ).jump(scene3)

    builder.step(react2_price).say(
        "zek_r6",
        "おや、慎重ですね。代償は……あなたの『良心』を、少しばかりいただくだけです。",
        "Oh my, how cautious. The cost is... merely a small portion of your 'conscience.'",
        "哎呀，真谨慎呢。代价嘛……只是稍微取用一点您的「良心」罢了。",
        actor=zek,
    ).jump(scene3)

    # ========================================
    # シーン3: 天秤にかけられる魂
    # ========================================
    builder.step(scene3).play_bgm("BGM/Ominous_Heartbeat").say(
        "narr_8",
        "ゼクは「偽物の器」と報酬となる「禍々しい品」を並べ、あなたの返答を待つ。\n彼のフードの奥にある瞳が、あなたの決断を愉しむように細められた。",
        "Zek arranged the 'counterfeit vessel' and the 'ominous reward' before you, awaiting your response.\nThe eyes beneath his hood narrowed, savoring your deliberation.",
        "泽克将「赝品器皿」和作为报酬的「不祥之物」并排摆好，等待着你的答复。\n他兜帽深处的双眸微微眯起，仿佛在玩味着你的抉择。",
        actor=narrator,
    ).say(
        "zek_9",
        "リリィに従い、大人しく彼女の飼い犬となるか。それとも、私と手を組み、彼女を欺いて『真なる力』を手にするか。",
        "Shall you obey Lily and remain her obedient lapdog? Or shall you ally with me, deceive her, and seize 'true power'?",
        "是顺从莉莉，乖乖做她的走狗呢？还是与鄙人联手，欺骗她并获得「真正的力量」？",
        actor=zek,
    ).say(
        "zek_10",
        "……ふふ、どちらを選んでも、このアリーナに刻まれるあなたの物語は、より残酷で美しいものになるでしょう。さあ、お選びください。",
        "...Heh heh, whichever you choose, the tale carved into this arena shall become all the more cruel and beautiful. Now then, if you would make your selection.",
        "……呵呵呵，无论您选择哪一边，镌刻在这角斗场的您的故事，都将变得更加残酷而美丽吧。好了，请做出您的选择。",
        actor=zek,
    )

    # 重要な選択
    builder.choice(
        refuse,
        "断る。リリィを裏切るつもりはない",
        "I refuse. I have no intention of betraying Lily.",
        "我拒绝。我没有背叛莉莉的打算",
        text_id="c_refuse",
    ).choice(
        accept,
        "……分かった。取引に応じる",
        "...Very well. I accept your transaction.",
        "……好吧。我接受这笔交易",
        text_id="c_accept",
    )

    # 選択肢: 断る
    builder.step(refuse).say(
        "zek_ref1",
        "……残念です。ですが、無理強いはいたしません。\n\nあなたが『忠犬』の道を選ばれるというのなら、それもまた一興。……いつか、その選択を後悔する日が来るかもしれませんが。",
        "...A pity. However, I shall not force the matter.\n\nIf you choose the path of the 'loyal hound,' that too has its own entertainment value. ...Though the day may come when you regret that investment.",
        "……真遗憾。不过，鄙人不会强人所难。\n\n您若选择做「忠犬」，那也别有一番趣味。……只是，也许有一天您会后悔这个选择呢。",
        actor=zek,
    ).say(
        "narr_ref",
        "ゼクは影の中へと消えていく。",
        "Zek faded into the shadows.",
        "泽克消失在阴影之中。",
        actor=narrator,
    ).set_flag(
        Keys.BOTTLE_CHOICE, FlagValues.BottleChoice.REFUSED
    ).complete_quest(QuestIds.ZEK_STEAL_BOTTLE).complete_quest(
        QuestIds.ZEK_STEAL_BOTTLE_REFUSE
    ).finish()

    # 選択肢: 受諾
    builder.step(accept).say(
        "zek_acc1",
        "ふふ、賢明な判断です。",
        "Heh heh, a wise decision.",
        "呵呵呵，明智的决定。",
        actor=zek,
    ).say(
        "narr_acc1",
        "ゼクは満足げに偽物の器をあなたの手に握らせる。",
        "With evident satisfaction, Zek pressed the counterfeit vessel into your hands.",
        "泽克满意地将赝品器皿塞进你的手中。",
        actor=narrator,
    ).say(
        "zek_acc2",
        "これがあなたへの報酬……私の店の品を、ひとつお分けしましょう。",
        "This shall be your compensation... I shall share one item from my inventory.",
        "这是给您的报酬……鄙人店里的货物，分您一件吧。",
        actor=zek,
    ).say(
        "narr_acc2",
        "ゼクは禍々しい瓶を取り出し、あなたの手に押し付けた。",
        "Zek produced an ominous vial and pressed it into your hand.",
        "泽克取出一个不祥的瓶子，塞进你手中。",
        actor=narrator,
    ).say(
        "zek_acc3",
        "『痛覚遮断薬』。次の戦いで、役に立つでしょう。……では、良い仕事を。彼女が寝静まった頃に、すり替えてきてくださいな。",
        "'Pain Suppressant.' It shall prove useful in your next battle. ...Now then, I wish you profitable work. If you would make the exchange once she retires for the night.",
        "「痛觉阻断药」。下次战斗时会派上用场的。……那么，祝您顺利。待她入睡后，请去完成调包吧。",
        actor=zek,
    ).say(
        "narr_acc3",
        "ゼクは影の中へと消えていく。",
        "Zek faded into the shadows.",
        "泽克消失在阴影之中。",
        actor=narrator,
    ).cs_eval(
        'EClass.pc.Pick(ThingGen.Create("sukutsu_painkiller"));'
    ).jump(scene4_aftermath)

    # ========================================
    # シーン4: 事後の静寂（受諾した場合のみ）
    # ========================================
    builder.step(scene4_aftermath).play_bgm("BGM/Lobby_Normal").say(
        "narr_aft1",
        "深夜ーーリリィが休んでいる隙に、あなたは受付に忍び込み、棚の『共鳴瓶』を偽物とすり替えた。\n本物の器はゼクの元へ。あなたの手には、澱んだ霧を宿す模造品だけが残った。",
        "Late at night -- while Lily rested, you slipped into the reception and swapped the 'Resonance Vessel' on the shelf with the counterfeit.\nThe genuine vessel went to Zek. In your hands remained only the imitation harboring stagnant mist.",
        "深夜--趁着莉莉休息的空隙，你潜入前台，将架子上的「共鸣瓶」换成了赝品。\n真品器皿送到了泽克手中。你手中只剩下那件蕴含着浑浊迷雾的仿制品。",
        actor=narrator,
    ).say(
        "narr_aft3",
        "翌朝ーー",
        "The next morning --",
        "翌日清晨--",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_a1",
        "……あら、なんだか少し、器の感触が変わったかしら？",
        "...Oh my, does the vessel feel slightly different somehow?",
        "……哎呀，总觉得这器皿的手感有点变了呢？",
        actor=lily,
    ).say(
        "narr_aft4",
        "彼女は棚の器を手に取り、軽く傾ける。一瞬、その瞳があなたを鋭く見つめるが、すぐに笑顔に戻る。",
        "She picked up the vessel from the shelf and tilted it gently. For an instant, her eyes fixed sharply on you, but her smile quickly returned.",
        "她从架子上拿起器皿，轻轻倾斜。刹那间，她的目光锐利地扫向你，但很快又恢复了笑容。",
        actor=narrator,
    ).say(
        "lily_a2",
        "……まぁいいわ。これで私の研究は飛躍的に進みます。ふふ、感謝してくださいね。あなたが『使い物』にならなくなった後も、その響きだけは私の手元に残るのですから。",
        "...Well, never mind. With this, my research shall advance dramatically. Hehe, do be grateful. Even after you're no longer 'useful,' at least the echoes will remain in my possession.",
        "……算了。有了这个，我的研究将会突飞猛进。呵呵，要感谢我哦。即使您变得「没用」了，那回响也会留在我手中呢。",
        actor=lily,
    ).say(
        "narr_aft5",
        "彼女は気づいていないのか、それともーー",
        "Has she not noticed, or perhaps --",
        "她是没有察觉，还是说--",
        actor=narrator,
    ).jump(ending)

    # ========================================
    # 終了処理（受諾した場合のみここに到達）
    # ========================================
    builder.step(ending).set_flag(
        Keys.BOTTLE_CHOICE, FlagValues.BottleChoice.SWAPPED
    ).complete_quest(QuestIds.ZEK_STEAL_BOTTLE).complete_quest(
        QuestIds.ZEK_STEAL_BOTTLE_ACCEPT
    ).finish()
