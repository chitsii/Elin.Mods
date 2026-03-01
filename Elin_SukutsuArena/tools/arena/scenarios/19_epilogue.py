"""
19_epilogue.py - 最終決戦後のエピローグ

アスタロト撃破後のエンディング選択と帰還シーン。
勝利後にarena_masterドラマから呼び出される。
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds, SessionKeys


def define_epilogue(builder: DramaBuilder):
    """
    エピローグ：エンディング選択と帰還
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    astaroth = builder.register_actor(Actors.ASTAROTH, "アスタロト", "Astaroth")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    nul = builder.register_actor(Actors.NUL, "ヌル", "Nul")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    act5 = builder.label("act5_victory")
    act6 = builder.label("act6_return")
    ending_choice = builder.label("ending_choice")
    ending_a_rescue = builder.label("ending_a_rescue")  # 連れ出し
    ending_b_inherit = builder.label("ending_b_inherit")  # 継承
    epilogue = builder.label("epilogue")
    nul_return = builder.label("nul_return")
    finale = builder.label("finale")

    # バルガス死亡版ラベル
    act5_dead = builder.label("act5_balgas_dead")
    act6_dead = builder.label("act6_balgas_dead")
    ending_choice_dead = builder.label("ending_choice_balgas_dead")
    ending_a_dead = builder.label("ending_a_balgas_dead")
    ending_b_dead = builder.label("ending_b_balgas_dead")
    finale_dead = builder.label("finale_balgas_dead")

    # ========================================
    # メイン（act5から開始）
    # ========================================
    builder.step(main).jump(act5)

    # ========================================
    # 第5幕: 終焉と、はじまり
    # ========================================
    builder.step(act5).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, act5_dead
    ).play_bgm(
        "BGM/Emotional_Hope"
    ).say(
        "narr_14",
        "激闘の末、アスタロトの身体が崩れ始める。王座は砕け、アリーナの外壁は剥がれ落ち、そこから美しい『本当の空』が姿を現した。",
        "After the fierce battle, Astaroth's body begins to crumble. The throne shatters, the arena's outer walls peel away, revealing the beautiful 'true sky' beyond.",
        "激战之后，阿斯塔罗特的身躯开始崩解。王座碎裂，角斗场的外墙剥落，那美丽的『真正的天空』终于显现。",
        actor=narrator,
    ).say(
        "astaroth_6",
        "「……見事だ。私は……ただ、この閉じられた孵化器を守るだけの、古い部品に過ぎなかったのかもしれないな。」",
        "\"...Magnificent. Perhaps I was... merely an obsolete component, guarding this sealed incubator and nothing more.\"",
        "「……精彩。吾或许……不过是守护这封闭孵化器的，陈旧零件罢了。」",
        actor=astaroth,
    ).say(
        "astaroth_7",
        "「……リリィ、バルガス、ゼク。……そして新しき王よ。この世界の重さを、お前たちが分かち合うというのなら……私は、安心して土へ還ろう。」",
        "\"...Lily, Vargus, Zek. ...And thou, the new sovereign. If ye shall share the burden of this world... then I may return to the earth in peace.\"",
        "「……莉莉、巴尔加斯、泽克。……以及新王啊。若尔等愿共同承担这世界之重……吾便可安心归于尘土。」",
        actor=astaroth,
    ).say(
        "narr_15", "アスタロトが柔らかな光となって霧散していく。", "Astaroth dissolves into a gentle light, fading away.", "阿斯塔罗特化作柔和的光芒，逐渐消散。", actor=narrator
    ).focus_chara(Actors.ZEK).say(
        "zek_12",
        "……やれやれ。これほどの損失（コスト）を出して、得られた利益は『自由』だけですか。……悪くない取引でしたね。",
        "Heh heh... My, my. After all that cost, all we gained was 'freedom.' ...Not a bad deal, I'd say.",
        "……呵呵。付出如此代价，所得不过『自由』而已吗。……这笔交易，倒也不差呢。",
        actor=zek,
    ).jump(act6)

    # ========================================
    # 第6幕: 帰還の道
    # ========================================
    builder.step(act6).say(
        "narr_16",
        "アスタロトが完全に消えると、アリーナ全体を覆っていた次元の壁が、ガラスのように砕け散り始める。",
        "As Astaroth vanishes completely, the dimensional walls that shrouded the entire arena begin to shatter like glass.",
        "当阿斯塔罗特彻底消失，笼罩整个角斗场的次元之壁开始如玻璃般碎裂。",
        actor=narrator,
    ).say(
        "narr_17",
        "紫紺の虚無が晴れ、その向こうに久しく忘れていた『青空』が広がっていた。",
        "The violet void clears, revealing the 'blue sky' long forgotten beyond.",
        "紫黑的虚无散去，那久违的『蓝天』在彼方展开。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_6", "……おい、見ろ。あれは……", "...Hey, look. That's...", "……喂，快看。那是……", actor=balgas
    ).say(
        "narr_18",
        "崩れゆく次元の境界線の中に、一筋の光の階段が浮かび上がる。それはイルヴァへと続く、帰還の道だ。",
        "Within the crumbling dimensional boundaries, a stairway of light emerges. It is the path home, leading to Ylva.",
        "在崩塌的次元边界之中，一道光之阶梯浮现。那是通往伊尔瓦的归途之路。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_7",
        "この道は、地上と、かつてのアリーナ跡を繋ぐ『自由な通路』になるでしょう。行きたい者は行き、留まりたい者は留まる。……それが、あなたが勝ち取った『解放』なのですね。",
        "This path shall become a 'free passage' connecting the surface to the remnants of the arena. Those who wish to leave may leave, those who wish to stay may stay. ...That is the 'liberation' you have won.",
        "这条路将成为连接地面与昔日角斗场遗迹的『自由通道』。想走的人可以离开，想留的人可以留下。……这就是您争取到的『解放』呢。",
        actor=lily,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_7",
        "……おい、戦鬼。いや、竜断ち。お前、この先どうする？ 地上に戻るか？ それとも……",
        "...Hey, War Demon. No, Dragon Slayer. What're you gonna do now? Head back to the surface? Or...",
        "……喂，战鬼。不，屠龙者。你小子接下来打算怎么办？回地面吗？还是说……",
        actor=balgas,
    ).jump(ending_choice)

    # ========================================
    # エンディング選択
    # ========================================
    builder.step(ending_choice).choice(
        ending_a_rescue, "皆を連れて、イルヴァへ行こう", "Let's take everyone to Ylva", "带着大家，前往伊尔瓦吧", text_id="ending_a_rescue"
    ).choice(
        ending_b_inherit, "新しいアリーナを作り直したい", "I want to rebuild a new arena", "我想重建一座新的角斗场", text_id="ending_b_inherit"
    )

    # エンディングA: 連れ出し（皆を連れてイルヴァへ）
    builder.step(ending_a_rescue).focus_chara(Actors.LILY).say(
        "lily_ea2",
        "あなたとの契約を通じて、私も……初めて『帰る場所』を持てるのね。",
        "Through our contract, I too... shall finally have a 'place to return to.'",
        "通过与您的契约，我也……终于有了『可以回去的地方』呢。",
        actor=lily,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_ea1",
        "……35年ぶりの故郷か。カインのやつにも見せてやりたかった。",
        "...Home after 35 years. Wish I could've shown Cain.",
        "……35年没回故乡了啊。真想让凯恩那家伙也看看。",
        actor=balgas,
    ).focus_chara(Actors.ZEK).say(
        "zek_ea1",
        "ふむ、私は……もう少しここに残りましょう。次元構造の変化が安定化するまで、見届ける者も必要ですからね。",
        "Hm, I shall... stay here a while longer. Someone must observe until the dimensional structure stabilizes.",
        "嗯，在下……还要在这里多留一会。在次元结构稳定之前，总需要有人看守呢。",
        actor=zek,
    ).say(
        "zek_ea2",
        "……また会いましょう。私の最高傑作が、どんな人生を歩むのか、記録させてもらいますよ。",
        "...Until we meet again. I shall document what life my greatest masterpiece leads.",
        "……后会有期。在下会记录下，我最得意的杰作将会走上怎样的人生呢。",
        actor=zek,
    ).set_flag(Keys.ENDING, FlagValues.Ending.RESCUE).jump(epilogue)

    # エンディングB: 継承（アリーナを純粋な闘技場として再建）
    builder.step(ending_b_inherit).focus_chara(Actors.LILY).say(
        "lily_eb1",
        "……ふふ、あなたらしいですね。この場所にも、居場所を必要とする者がいますから。",
        "Hehe... How like you. There are those who need a place to belong here as well.",
        "……呵呵，真像您的风格呢。这里也有需要容身之所的人们。",
        actor=lily,
    ).say(
        "lily_eb2",
        "私も残ります。あなたが新しいグランドマスターなら、私は……管理者ではなく、『伴侶』として支えさせてください。",
        "I shall stay as well. If you are to be the new Grand Master, then let me support you... not as an administrator, but as a 'companion.'",
        "我也会留下。如果您是新的大角斗主，那请让我……不是以管理者，而是以『伴侣』的身份支持您。",
        actor=lily,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_eb1",
        "ハッ、そうこなくちゃな。『神の孵化場』はもう終わりだ。これからは、自分の意志で戦いたい奴だけが来る場所にする。",
        "Hah! That's more like it. The 'God's Hatchery' is over. From now on, this'll be a place only for those who fight by their own will.",
        "哈！这才对嘛。『神的孵化场』已经结束了。从今往后，这里只属于那些凭自己意志战斗的家伙。",
        actor=balgas,
    ).say(
        "balgas_eb2",
        "俺は引退済みだが……まあ、若い奴らの相談役くらいはやってやるさ。",
        "I'm retired, but... well, I can at least be an advisor to the young ones.",
        "老子已经退休了……不过嘛，给年轻人当个顾问倒是可以的。",
        actor=balgas,
    ).focus_chara(Actors.ZEK).say(
        "zek_eb1",
        "ふむ、それなら私が商店街を取り仕切りましょう。アルカディアの技術と、各次元の品物……ククッ、繁盛しそうですね。",
        "Hm, then I shall manage the market district. Arcadian technology and goods from various dimensions... Heh heh, business should be booming.",
        "嗯，那在下来管理商业街吧。阿卡迪亚的技术，各个次元的商品……呵呵，生意应该会很兴隆呢。",
        actor=zek,
    ).set_flag(Keys.ENDING, FlagValues.Ending.INHERIT).jump(epilogue)

    # ========================================
    # エピローグ
    # ========================================
    builder.step(epilogue).play_bgm("BGM/ProgressiveDance").say(
        "narr_21",
        "かつて、異次元の闘技場に迷い込んだ一人の冒険者がいた。そこで絶望の底で友を得て、魂を賭けて戦い、ついには、『うつろいし神』をも超える存在となった。そして今、解放された魂は、新たな物語を紡ぎ始めるーー",
        "Once, there was an adventurer who wandered into an interdimensional arena. In the depths of despair, they found friends, fought with their soul at stake, and ultimately transcended even the 'Hollow God.' And now, the liberated soul begins to weave a new tale--",
        "曾经，有一位冒险者误入了异次元的角斗场。在那绝望的深渊中结识了伙伴，以灵魂为赌注战斗，最终成为了超越『虚空之神』的存在。而如今，被解放的灵魂开始编织新的故事--",
        actor=narrator,
    ).jump(nul_return)

    # ========================================
    # ヌル再登場（全ルート共通）
    # ========================================
    builder.step(nul_return).say(
        "narr_nul_r1",
        "帰還の道へ向かおうとした時、崩れかけた回廊から、ゆっくりと歩いてくる人影がある。",
        "As you turn toward the path home, a figure slowly emerges from the crumbling corridor.",
        "正当你转向归途之时，一个身影从崩塌的回廊中缓缓走来。",
        actor=narrator,
    ).shake().say(
        "narr_nul_r2", "その姿を見た瞬間、あなたは目を疑った。", "The moment you see who it is, you doubt your eyes.", "看到那个身影的瞬间，你不敢相信自己的眼睛。", actor=narrator
    ).focus_chara(Actors.NUL).say(
        "nul_r1", "……私も、驚いている。消えかけた……でも、消えなかった。", "...I am... surprised too. I was fading... but... I didn't fade.", "……我也……很惊讶。本以为会消失……但是……没有消失。", actor=nul
    ).focus_chara(Actors.ZEK).say(
        "zek_nul_r1",
        "ふむ……なるほど。アスタロトの『削除命令』は、完全に執行される前に、あの方の消滅と共に無効化されたのでしょう。",
        "Hm... I see. Astaroth's 'deletion order' must have been nullified along with their demise, before it could be fully executed.",
        "嗯……原来如此。阿斯塔罗特的『删除指令』，在完全执行之前，就随着那位的消亡而失效了吧。",
        actor=zek,
    ).focus_chara(Actors.NUL).say(
        "nul_r2",
        "……あの方が消えた瞬間、私の中で何かが止まった。削除ではなく……解放。",
        "...The moment they vanished... something inside me stopped. Not deletion... liberation.",
        "……那位消失的瞬间……我体内的什么……停止了。不是删除……是解放。",
        actor=nul,
    ).say("nul_r3", "……私の名前は、ヌル。……それでいい。", "...My name is... Nul. ...That is enough.", "……我的名字……是努尔。……这样就好。", actor=nul).say(
        "nul_r4", "……これからは、あなたたちと一緒に……生きていきたい。", "...From now on... I want to live... with all of you.", "……从今以后……我想和你们一起……活下去。", actor=nul
    ).focus_chara(Actors.ZEK).say(
        "zek_nul_r2",
        "クク……また一人、このアリーナから解放された魂が生まれましたね。",
        "Heh heh... Another liberated soul has been born from this arena.",
        "呵呵……又有一个被解放的灵魂从这角斗场中诞生了呢。",
        actor=zek,
    ).jump(finale)

    # ========================================
    # フィナーレ
    # ========================================
    builder.step(finale).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, finale_dead
    ).say(
        "balgas_f1",
        "……おい、いつまで感傷に浸ってんだ。次は俺の奢りで、地上で一番うまい酒を飲みに行くぞ！",
        "...Hey, how long you gonna wallow in sentiment? Next round's on me--we're getting the best drinks on the surface!",
        "……喂，你还要沉浸在感伤里多久。下次老子请客，去地面上喝最好的酒！",
        actor=balgas,
    ).say(
        "lily_f1",
        "ふふ、楽しみです。……バルガスさん、まだイルヴァのお金は残ってあるの？",
        "Hehe, I look forward to it. ...Vargus, do you still have any Ylvan currency left?",
        "呵呵，真令人期待。……巴尔加斯先生，伊尔瓦的钱还有剩吗？",
        actor=lily,
    ).say("zek_f1", "おや、私も混ぜてくださいよ？", "Oh my, might I join you?", "哎呀，也让在下加入吧？", actor=zek).set_flag(
        Keys.RANK, FlagValues.Rank.SS
    ).set_flag(
        SessionKeys.IS_QUEST_BATTLE_RESULT, 0
    ).set_flag(
        SessionKeys.ARENA_RESULT, 0
    ).set_flag(
        SessionKeys.QUEST_BATTLE, 0
    ).complete_quest(QuestIds.LAST_BATTLE).change_journal_phase("sukutsu_arena", 10).eval("Elin_SukutsuArena.ArenaManager.ShowEndingCredit()").unfocus().fade_in(
        duration=0.5, color="black"
    ).finish()

    # ========================================
    # バルガス死亡版: 第5幕
    # ========================================
    builder.step(act5_dead).play_bgm("BGM/Emotional_Hope").say(
        "narr_14_d",
        "激闘の末、アスタロトの身体が崩れ始める。王座は砕け、アリーナの外壁は剥がれ落ち、そこから美しい『本当の空』が姿を現した。",
        "After the fierce battle, Astaroth's body begins to crumble. The throne shatters, the arena's outer walls peel away, revealing the beautiful 'true sky' beyond.",
        "激战之后，阿斯塔罗特的身躯开始崩解。王座碎裂，角斗场的外墙剥落，那美丽的『真正的天空』终于显现。",
        actor=narrator,
    ).say(
        "astaroth_6d",
        "「……見事だ。私は……ただ、この閉じられた孵化器を守るだけの、古い部品に過ぎなかったのかもしれないな。」",
        "\"...Magnificent. Perhaps I was... merely an obsolete component, guarding this sealed incubator and nothing more.\"",
        "「……精彩。吾或许……不过是守护这封闭孵化器的，陈旧零件罢了。」",
        actor=astaroth,
    ).say(
        "astaroth_7d",
        "「……リリィ、ゼク。……そして新しき王よ。この世界の重さを、お前たちが分かち合うというのなら……私は、安心して土へ還ろう。」",
        "\"...Lily, Zek. ...And thou, the new sovereign. If ye shall share the burden of this world... then I may return to the earth in peace.\"",
        "「……莉莉、泽克。……以及新王啊。若尔等愿共同承担这世界之重……吾便可安心归于尘土。」",
        actor=astaroth,
    ).say(
        "narr_15_d", "アスタロトが柔らかな光となって霧散していく。", "Astaroth dissolves into a gentle light, fading away.", "阿斯塔罗特化作柔和的光芒，逐渐消散。", actor=narrator
    ).focus_chara(Actors.ZEK).say(
        "zek_12_d",
        "……やれやれ。これほどの損失（コスト）を出して、得られた利益は『自由』だけですか。……悪くない取引でしたね。",
        "Heh heh... My, my. After all that cost, all we gained was 'freedom.' ...Not a bad deal, I'd say.",
        "……呵呵。付出如此代价，所得不过『自由』而已吗。……这笔交易，倒也不差呢。",
        actor=zek,
    ).focus_chara(Actors.LILY).say(
        "lily_d5",
        "……バルガスさんも、きっと喜んでくれているわ。彼の分まで、私たちは生きていかなきゃね。",
        "...Vargus would surely be pleased. We must live on for his sake as well.",
        "……巴尔加斯先生一定也会高兴的。我们要连他的份一起，好好活下去呢。",
        actor=lily,
    ).jump(act6_dead)

    # バルガス死亡版: 第6幕
    builder.step(act6_dead).say(
        "narr_16_d",
        "アスタロトが完全に消えると、アリーナ全体を覆っていた次元の壁が、ガラスのように砕け散り始める。",
        "As Astaroth vanishes completely, the dimensional walls that shrouded the entire arena begin to shatter like glass.",
        "当阿斯塔罗特彻底消失，笼罩整个角斗场的次元之壁开始如玻璃般碎裂。",
        actor=narrator,
    ).say(
        "narr_17_d",
        "紫紺の虚無が晴れ、その向こうに久しく忘れていた『青空』が広がっていた。",
        "The violet void clears, revealing the 'blue sky' long forgotten beyond.",
        "紫黑的虚无散去，那久违的『蓝天』在彼方展开。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_d6", "……見て。あれは……帰り道。", "...Look. That is... the way home.", "……看。那是……回去的路。", actor=lily
    ).say(
        "narr_18_d",
        "崩れゆく次元の境界線の中に、一筋の光の階段が浮かび上がる。それはイルヴァへと続く、帰還の道だ。",
        "Within the crumbling dimensional boundaries, a stairway of light emerges. It is the path home, leading to Ylva.",
        "在崩塌的次元边界之中，一道光之阶梯浮现。那是通往伊尔瓦的归途之路。",
        actor=narrator,
    ).say(
        "lily_7_d",
        "この道は、地上と、かつてのアリーナ跡を繋ぐ『自由な通路』になるでしょう。行きたい者は行き、留まりたい者は留まる。……それが、あなたが勝ち取った『解放』なのですね。",
        "This path shall become a 'free passage' connecting the surface to the remnants of the arena. Those who wish to leave may leave, those who wish to stay may stay. ...That is the 'liberation' you have won.",
        "这条路将成为连接地面与昔日角斗场遗迹的『自由通道』。想走的人可以离开，想留的人可以留下。……这就是您争取到的『解放』呢。",
        actor=lily,
    ).focus_chara(Actors.ZEK).say(
        "zek_d7",
        "……おい、竜断ち。この先どうする？ 地上に戻るか？ それとも……",
        "...Hey, Dragon Slayer. What will you do now? Return to the surface? Or...",
        "……喂，屠龙者。接下来打算怎么办？回地面吗？还是说……",
        actor=zek,
    ).jump(ending_choice_dead)

    # バルガス死亡版: エンディング選択
    builder.step(ending_choice_dead).choice(
        ending_a_dead,
        "皆を連れて、イルヴァへ行こう",
        "Let's take everyone to Ylva",
        "带着大家，前往伊尔瓦吧",
        text_id="ending_a_rescue_dead",
    ).choice(
        ending_b_dead,
        "新しいアリーナを作り直したい",
        "I want to rebuild a new arena",
        "我想重建一座新的角斗场",
        text_id="ending_b_inherit_dead",
    )

    # バルガス死亡版: エンディングA
    builder.step(ending_a_dead).focus_chara(Actors.LILY).say(
        "lily_ea2_d",
        "あなたとの契約を通じて、私も……初めて『帰る場所』を持てるのね。",
        "Through our contract, I too... shall finally have a 'place to return to.'",
        "通过与您的契约，我也……终于有了『可以回去的地方』呢。",
        actor=lily,
    ).say(
        "lily_ead", "……バルガスさんの分まで、私たちは生きていくわ。", "...We shall live on, for Vargus's sake as well.", "……我们会连巴尔加斯先生的份一起，好好活下去。", actor=lily
    ).focus_chara(Actors.ZEK).say(
        "zek_ea1_d",
        "ふむ、私は……もう少しここに残りましょう。次元構造の変化が安定化するまで、見届ける者も必要ですからね。",
        "Hm, I shall... stay here a while longer. Someone must observe until the dimensional structure stabilizes.",
        "嗯，在下……还要在这里多留一会。在次元结构稳定之前，总需要有人看守呢。",
        actor=zek,
    ).say(
        "zek_ea2_d",
        "……また会いましょう。私の最高傑作が、どんな人生を歩むのか、記録させてもらいますよ。",
        "...Until we meet again. I shall document what life my greatest masterpiece leads.",
        "……后会有期。在下会记录下，我最得意的杰作将会走上怎样的人生呢。",
        actor=zek,
    ).set_flag(Keys.ENDING, FlagValues.Ending.RESCUE).jump(epilogue)

    # バルガス死亡版: エンディングB
    builder.step(ending_b_dead).focus_chara(Actors.LILY).say(
        "lily_eb1_d",
        "……ふふ、あなたらしいですね。この場所にも、居場所を必要とする者がいますから。",
        "Hehe... How like you. There are those who need a place to belong here as well.",
        "……呵呵，真像您的风格呢。这里也有需要容身之所的人们。",
        actor=lily,
    ).say(
        "lily_eb2_d",
        "私も残ります。あなたが新しいグランドマスターなら、私は……管理者ではなく、『伴侶』として支えさせてください。",
        "I shall stay as well. If you are to be the new Grand Master, then let me support you... not as an administrator, but as a 'companion.'",
        "我也会留下。如果您是新的大角斗主，那请让我……不是以管理者，而是以『伴侣』的身份支持您。",
        actor=lily,
    ).say(
        "lily_ebd",
        "バルガスさんの意志も、きっとこのアリーナに宿っているはず。私たちで受け継ぎましょう。",
        "Vargus's spirit surely dwells within this arena. Let us carry on his legacy together.",
        "巴尔加斯先生的意志，一定也寄宿在这角斗场中。让我们一起继承吧。",
        actor=lily,
    ).focus_chara(Actors.ZEK).say(
        "zek_eb1_d",
        "ふむ、それなら私が商店街を取り仕切りましょう。アルカディアの技術と、各次元の品物……ククッ、繁盛しそうですね。",
        "Hm, then I shall manage the market district. Arcadian technology and goods from various dimensions... Heh heh, business should be booming.",
        "嗯，那在下来管理商业街吧。阿卡迪亚的技术，各个次元的商品……呵呵，生意应该会很兴隆呢。",
        actor=zek,
    ).set_flag(Keys.ENDING, FlagValues.Ending.INHERIT).jump(epilogue)

    # バルガス死亡版: フィナーレ
    builder.step(finale_dead).say(
        "lily_f1d",
        "……さあ、行きましょう。私たちの新しい物語が、始まるのですから。",
        "...Come now, let us go. Our new story is about to begin.",
        "……来吧，走吧。我们新的故事，就要开始了。",
        actor=lily,
    ).say("zek_f1_d", "おや、私も混ぜてくださいよ？", "Oh my, might I join you?", "哎呀，也让在下加入吧？", actor=zek).set_flag(
        Keys.RANK, FlagValues.Rank.SS
    ).set_flag(
        SessionKeys.IS_QUEST_BATTLE_RESULT, 0
    ).set_flag(
        SessionKeys.ARENA_RESULT, 0
    ).set_flag(
        SessionKeys.QUEST_BATTLE, 0
    ).complete_quest(QuestIds.LAST_BATTLE).change_journal_phase("sukutsu_arena", 10).eval("Elin_SukutsuArena.ArenaManager.ShowEndingCredit()").unfocus().fade_in(
        duration=0.5, color="black"
    ).finish()
