"""
06_2_zek_steal_soulgem.md - 英雄の残響、商人の天秤
ゼクがカインの魂の欠片を狙う重要な道徳的選択イベント
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds


def define_zek_steal_soulgem(builder: DramaBuilder):
    """
    ゼクがカインの魂の欠片を狙う
    シナリオ: 06_2_zek_steal_soulgem.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_whisper")
    choice1 = builder.label("choice1")
    react1_what = builder.label("react1_what")
    react1_friend = builder.label("react1_friend")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_cruel_choice")
    choice2 = builder.label("choice2")
    react2_price = builder.label("react2_price")
    react2_balgas = builder.label("react2_balgas")
    react2_stare = builder.label("react2_stare")
    scene3 = builder.label("scene3_decision")
    final_choice = builder.label("final_choice")
    refuse = builder.label("refuse")
    refuse_balgas = builder.label("refuse_balgas")
    sell = builder.label("sell")
    sell_balgas = builder.label("sell_balgas")
    scene4_lily = builder.label("scene4_lily")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 砂上の囁き
    # ========================================
    builder.step(main).play_bgm("BGM/Ominous_Suspense_01").say(
        "narr_1",
        "闘技場の熱気が冷めやらぬ、薄暗い廊下。\n勝利の余韻に浸るあなたの前に、鎖の音と共にゼクが現れる。\n彼は、あなたの掌にある『カインの魂の欠片』を、まるで極上の宝石を見るような飢えた目で見つめていた。",
        "A dimly lit corridor, still thick with the arena's lingering heat.\nAs you bask in the afterglow of victory, Zek appears before you, chains rattling.\nHe gazes at 'Cain's Soul Fragment' in your palm with hungry eyes, as if beholding a priceless gem.",
        "角斗场的热气尚未散去，昏暗的走廊。\n正当你沉浸在胜利的余韵中时，泽克伴着锁链的声响出现在你面前。\n他用饥渴的目光凝视着你掌中的『凯恩的灵魂碎片』，仿佛在欣赏一颗绝世珍宝。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.ZEK).say(
        "zek_1",
        "……あぁ、なんと美しい。\nこの輝き……永遠に失われるには、あまりに惜しい。",
        "...Ahh, how exquisite.\nThis radiance... far too precious to be lost forever.",
        "……啊啊，多么美丽。\n这光辉……就这样永远失去，实在太过可惜。",
        actor=zek,
    ).say(
        "zek_2",
        "数千回の敗北と、最期の瞬間の安らぎが凝固した、混じり気なしの『純粋な魂』だ。",
        "Thousands of defeats and the peace of his final moment, crystallized into a 'pure soul' without impurity.",
        "数千次败北与临终时刻的安宁凝结而成的，毫无杂质的『纯粹灵魂』。",
        actor=zek,
    ).say(
        "narr_4", "彼は細長い指を伸ばし、魂の欠片を指し示す。", "He extends his slender fingers, pointing at the soul fragment.", "他伸出细长的手指，指向灵魂碎片。", actor=narrator
    ).say(
        "zek_3",
        "バルガスがそれを望んでいるのは分かっています。……ですが、闘士殿。これを返して、一体何になるのですか？\n死者に安らぎを、生者に感傷を。……そんなもの、一文の得にもなりゃしない。",
        "I understand Vargus desires it. ...But tell me, honored gladiator. What profit is there in returning it?\nPeace for the dead, sentiment for the living... Such things yield no return on investment whatsoever.",
        "在下知道巴尔加斯想要它。……但是，阁下。把它还回去，又能得到什么呢？\n让死者安息，让生者伤感。……这种东西，一文钱的好处都没有。",
        actor=zek,
    )

    # プレイヤーの選択肢1
    builder.choice(react1_what, "……何が言いたい", "...What are you getting at?", "……你想说什么", text_id="c1_what").choice(
        react1_friend, "友情に価値がないと？", "Are you saying friendship has no value?", "你是说友情没有价值？", text_id="c1_friend"
    ).choice(react1_silent, "（無言で聞く）", "(Listen in silence)", "（沉默聆听）", text_id="c1_silent")

    # 選択肢反応1
    builder.step(react1_what).say(
        "zek_r1",
        "おや、警戒心がおありで。では、単刀直入に申し上げましょう。",
        "Oh my, such wariness. Very well, allow me to be direct.",
        "哎呀，阁下颇有警惕心呢。那么，请容在下开门见山。",
        actor=zek,
    ).jump(scene2)

    builder.step(react1_friend).say(
        "zek_r2",
        "価値がない、とは言いません。ただ、『足りない』と言っているのです。",
        "I never said it has no value. I merely said it's... insufficient.",
        "在下并非说没有价值。只是说……『不够』罢了。",
        actor=zek,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "zek_r3",
        "……ふふ、沈黙は賢明さの証。では、続けさせていただきましょう。",
        "...Heh heh, silence is the mark of wisdom. Then allow me to continue.",
        "……呵呵呵，沉默是智慧的证明。那么，请容在下继续。",
        actor=zek,
    ).jump(scene2)

    # ========================================
    # シーン2: 残酷な二択
    # ========================================
    builder.step(scene2).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_5",
        "ゼクはさらに一歩踏み込み、フードの下から歪んだ笑みを覗かせる。",
        "Zek steps closer, a twisted smile visible beneath his hood.",
        "泽克又向前迈了一步，兜帽下露出扭曲的笑容。",
        actor=narrator,
    ).say(
        "zek_5",
        "それよりも、私にその欠片を預けてはいただけませんか？私ならば、この英雄の最期を……永遠に保存することができる。\nそしてその見返りに、あなたに私の貴重な賞品を無料でおゆずりしましょう。",
        "Instead, might you entrust that fragment to me? I can preserve this hero's final moment... for eternity.\nAnd in return, I shall offer you one of my prized commodities, free of charge.",
        "与其如此，能否将那碎片托付给在下呢？若是在下的话，可以将这位英雄的最后时刻……永远保存下来。\n作为回报，在下愿将珍藏的宝物免费赠与阁下。",
        actor=zek,
    ).say(
        "zek_6",
        "選ぶのはあなただ。バルガスに返し、友情と名誉という名の、腹の足しにもならない温もりを噛み締めるか。\nそれとも、私に売り払い、この先の地獄を生き抜くための『絶対的な暴力』を手に入れるか。",
        "The choice is yours. Return it to Vargus and savor the warmth of friendship and honor... which won't fill your stomach.\nOr sell it to me and obtain 'absolute power' to survive the hell that awaits you.",
        "选择权在阁下手中。是还给巴尔加斯，细细品味那名为友情与荣誉、却填不饱肚子的温情。\n还是卖给在下，获得在今后的地狱中生存所需的『绝对暴力』。",
        actor=zek,
    ).say(
        "zek_8",
        "バルガスに返せば、彼は救われるでしょう。ですが、あなたは弱いままだ。私に売れば、あなたは英雄の力を食らって強くなる。\n……どちらがこのアリーナの『正解』か、賢明なあなたならお分かりでしょう？",
        "Return it to Vargus, and he will be saved. But you remain weak. Sell it to me, and you devour a hero's power to grow stronger.\n...Which is the 'correct answer' in this arena? I'm sure someone as wise as you understands.",
        "还给巴尔加斯，他便能得救。但阁下依然弱小。卖给在下，阁下便能吞噬英雄之力而变强。\n……哪个才是这角斗场的『正确答案』，聪明的阁下应该明白吧？",
        actor=zek,
    )

    # プレイヤーの選択肢2
    builder.choice(react2_price, "代償は何だ？", "What's the cost?", "代价是什么？", text_id="c2_price").choice(
        react2_balgas, "バルガスが知ったら……", "If Vargus finds out...", "如果巴尔加斯知道了……", text_id="c2_balgas"
    ).choice(react2_stare, "（無言で魂の欠片を見つめる）", "(Silently gaze at the soul fragment)", "（沉默地凝视灵魂碎片）", text_id="c2_stare")

    # 選択肢反応2
    builder.step(react2_price).say(
        "zek_r4",
        "代償は……あなたの『良心』を、少しばかりいただくだけです。ああ、それと友情も。",
        "The cost is... merely a small portion of your 'conscience.' Oh, and your friendship too.",
        "代价是……只需阁下的『良心』一小部分。啊，还有友情。",
        actor=zek,
    ).jump(scene3)

    builder.step(react2_balgas).say(
        "zek_r5",
        "ふふ、知られなければいいのですよ。それとも、正直に告白なさいますか？",
        "Heh heh, what he doesn't know won't hurt him. Unless... you plan to confess honestly?",
        "呵呵呵，不被发现就好了嘛。还是说，阁下打算坦白交代？",
        actor=zek,
    ).jump(scene3)

    builder.step(react2_stare).say(
        "zek_r6", "……悩んでおられる。それが正常です。人間らしいですね。", "...You're hesitating. That's perfectly normal. How very human of you.", "……阁下在犹豫啊。这很正常。真是人性化呢。", actor=zek
    ).jump(scene3)

    # ========================================
    # シーン3: 決断の瞬間
    # ========================================
    builder.step(scene3).play_bgm("BGM/Ominous_Heartbeat").say(
        "narr_6",
        "ゼクはあなたの返答を待つ。その目は、あなたの決断を愉しむように細められた。",
        "Zek awaits your response. His eyes narrow, savoring your moment of decision.",
        "泽克等待着你的回答。他的眼睛微微眯起，仿佛在享受你做决定的这一刻。",
        actor=narrator,
    ).jump(final_choice)

    # ========================================
    # 最終選択
    # ========================================
    builder.step(final_choice).choice(
        refuse, "バルガスとの絆を選ぶ", "Choose the bond with Vargus", "选择与巴尔加斯的羁绊", text_id="c_refuse"
    ).choice(sell, "力を手に入れる。売る", "Obtain power. Sell it", "获得力量。卖掉", text_id="c_sell")

    # ========================================
    # 分岐A: 断る（バルガスに返す）
    # ========================================
    builder.step(refuse).play_bgm("BGM/Lobby_Normal").say(
        "zek_ref1",
        "……残念です。ですが、無理強いはいたしません。あなたが『感傷』を選ばれるというのなら、それもまた一興。",
        "...A pity. But I won't force you. If you choose 'sentiment,' that too has its own... entertainment value.",
        "……真是遗憾。但在下不会强求。阁下若选择『感伤』，那也别有一番趣味。",
        actor=zek,
    ).say(
        "zek_ref2",
        "……いつか、その選択を後悔する日が来るかもしれませんが。その時はまた、お声掛けください。私は常に、影の中におりますから。",
        "...Perhaps one day you'll regret this transaction. When that day comes, do call on me again. I'm always waiting in the shadows.",
        "……也许有一天，阁下会后悔这个选择。届时请再来找在下。鄙人始终在暗影中等候。",
        actor=zek,
    ).say("narr_ref", "ゼクは影の中へと消えていく。", "Zek disappears into the shadows.", "泽克消失在阴影之中。", actor=narrator).jump(
        refuse_balgas
    )

    builder.step(refuse_balgas).play_bgm("BGM/Emotional_Sorrow").say(
        "narr_ref2",
        "あなたはロビーに戻り、バルガスにカインの魂の欠片を渡す。",
        "You return to the lobby and hand Cain's Soul Fragment to Vargus.",
        "你回到大厅，将凯恩的灵魂碎片交给巴尔加斯。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_ref1",
        "……あぁ。これでようやく、あいつもこの錆びた檻から出られる。",
        "...Ahh. Finally, he can escape this rusted cage too.",
        "……啊。这下那家伙终于也能从这生锈的牢笼里出去了。",
        actor=balgas,
    ).say(
        "narr_ref3",
        "彼は震える手で魂の欠片を受け取る。その目には涙。\n彼は魂の欠片を兜の中にそっと収める。",
        "He accepts the soul fragment with trembling hands. Tears well in his eyes.\nHe gently places the soul fragment inside his helmet.",
        "他用颤抖的双手接过灵魂碎片。眼中含着泪水。\n他将灵魂碎片轻轻收入头盔之中。",
        actor=narrator,
    ).say(
        "balgas_ref2",
        "……ありがよ。お前をただの『鉄屑』呼ばわりしたのは取り消してやる。\nお前は……カインが持っていた以上の、本物の『鋼の心』を持った戦士だ。",
        "...Thanks. I take back calling you just 'scrap metal.'\nYou're... a warrior with a true 'heart of steel,' even more than Cain had.",
        "……谢了。老子收回叫你『废铁』的话。\n你是……拥有比凯恩更真正的『钢铁之心』的战士。",
        actor=balgas,
    ).set_flag(
        Keys.KAIN_SOUL_CHOICE, FlagValues.KainSoulChoice.RETURNED
    ).complete_quest(QuestIds.ZEK_STEAL_SOULGEM).complete_quest(
        QuestIds.ZEK_STEAL_SOULGEM_RETURN
    ).finish()

    # ========================================
    # 分岐B: 売る
    # ========================================
    builder.step(sell).say(
        "zek_sell1",
        "ふふ、素晴らしい！ これです、これこそが私が求めていた『合理的かつ冷酷な決断』だ！\n友情を燃料にして、さらなる高みへ昇る……。あなたは、本物の怪物の素質がある。",
        "Heh heh, splendid! This is it, this is precisely the 'rational and ruthless decision' I was hoping for!\nUsing friendship as fuel to ascend to greater heights... You have the makings of a true monster.",
        "呵呵呵，太棒了！就是这个，这正是在下期盼的『理性而冷酷的决断』！\n将友情当作燃料，攀向更高的境界……阁下有成为真正怪物的资质。",
        actor=zek,
    ).say("narr_sell1", "彼は懐から禍々しい錠剤を取り出す。", "He produces an ominous pill from within his robes.", "他从怀中取出一颗邪异的药丸。", actor=narrator).say(
        "zek_sell3",
        "さあ、約束の報酬です。私の店の秘蔵品……『禁断の覚醒剤』を差し上げましょう。\nこれで、あなたは『魂を喰らう者』となりました。……では、良い演技を。彼に気づかれないよう、お気をつけて。",
        "Now then, your promised payment. A prized commodity from my collection... the 'Forbidden Stimulant.'\nWith this, you have become a 'Soul Devourer.' ...Now then, put on a good performance. Do be careful not to let him notice.",
        "来，这是约定的报酬。在下店铺的珍藏……『禁忌兴奋剂』赠与阁下。\n如此一来，阁下便成为了『噬魂者』。……那么，好好表演吧。小心别让他发现。",
        actor=zek,
    ).say("narr_sell2", "ゼクは影の中へと消えていく。", "Zek disappears into the shadows.", "泽克消失在阴影之中。", actor=narrator).cs_eval(
        'EClass.pc.Pick(ThingGen.Create("sukutsu_stimulant"));'
    ).jump(sell_balgas)

    builder.step(sell_balgas).play_bgm("BGM/Lobby_Normal").say(
        "narr_sell3",
        "あなたはロビーに戻る。バルガスがあなたを待っている。",
        "You return to the lobby. Vargus is waiting for you.",
        "你回到大厅。巴尔加斯正在等你。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_sell1", "……おい。カインの魂の欠片は見つかったか？", "...Hey. Did you find Cain's Soul Fragment?", "……喂。凯恩的灵魂碎片找到了吗？", actor=balgas
    ).say("narr_sell4", "あなたは首を横に振る。", "You shake your head.", "你摇了摇头。", actor=narrator).say(
        "balgas_sell2", "……そうか。見つからなかったか。", "...I see. You couldn't find it.", "……是吗。没找到啊。", actor=balgas
    ).say("narr_sell5", "彼は深く息を吐き、酒瓶を手に取る。", "He exhales deeply and reaches for his bottle.", "他深深叹了口气，拿起酒瓶。", actor=narrator).say(
        "balgas_sell3",
        "……まあ、仕方ねえ。お前は十分頑張った。……ありがよ。",
        "...Well, can't be helped. You did your best. ...Thanks.",
        "……算了，没办法。你已经尽力了。……谢了。",
        actor=balgas,
    ).set_flag(Keys.KAIN_SOUL_CHOICE, FlagValues.KainSoulChoice.SOLD).complete_quest(
        QuestIds.ZEK_STEAL_SOULGEM
    ).complete_quest(QuestIds.ZEK_STEAL_SOULGEM_SELL).finish()
