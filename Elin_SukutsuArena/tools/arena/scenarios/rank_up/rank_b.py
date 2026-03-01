# -*- coding: utf-8 -*-
"""
11_rank_up_B.md - Rank B 昇格試合『虚無の処刑人』
ヌルとの戦い - 感情と虚無の対決
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, Keys, QuestIds, Rank, SessionKeys


def define_rank_up_B(builder: DramaBuilder):
    """
    Rank B 昇格試合「虚無の処刑人」
    シナリオ: 11_rank_up_B.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_warning")
    choice1 = builder.label("choice1")
    react1_null = builder.label("react1_null")
    react1_fear = builder.label("react1_fear")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_lily_advice")
    choice2 = builder.label("choice2")
    react2_survive = builder.label("react2_survive")
    react2_meaning = builder.label("react2_meaning")
    react2_nod = builder.label("react2_nod")
    battle_start = builder.label("battle_start")

    # ========================================
    # シーン1: 虚無の予兆
    # ========================================
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/Ominous_Suspense_02"
    ).say(
        "narr_1",
        "ロビーの空気が異様に重い。",
        "The air in the lobby is unnaturally heavy.",
        "大厅的气氛异常沉重。",
        actor=narrator,
    ).say(
        "narr_2",
        "バルガスは珍しく、酒瓶を手にせず、険しい表情で闘技場の門を見つめている。",
        "Unusually, Vargus stands without his bottle, his stern gaze fixed on the arena gates.",
        "巴尔加斯罕见地没有拿着酒瓶，用严峻的表情注视着角斗场的大门。",
        actor=narrator,
    ).say(
        "narr_3",
        "リリィも、いつもの事務的な仮面の下に、微かな緊張を滲ませていた。",
        "Even Lily shows a faint trace of tension beneath her usual professional mask.",
        "莉莉也在平日公事公办的面具下，隐隐透露出紧张。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.BALGAS).say(
        "balgas_1",
        "……おい、朱砂食い。\nお前はここまで、よく戦ってきた。だが、次の相手は……今までの敵とは次元が違う。",
        "...Oi, Cinnabar Eater.\nYou've fought well to get this far. But your next opponent... he's on a whole different level.",
        "……喂，朱砂吞噬者。\n你小子一路战斗到这里，很不错。但下一个对手……和之前的敌人完全不是一个档次。",
        actor=balgas,
    ).say(
        "balgas_3",
        "『虚無の処刑人ヌル』。\n\nあいつは、グランドマスター『アスタロト』の直属の『人形』だ。感情も、意志も、魂すらも持たねえ。\n\nただひたすらに、『存在を無に還す』ことだけを目的に動く、生ける屍だ。",
        "'Null, the Executioner of Void.'\n\nHe's the Grand Master Astaroth's personal 'puppet.' No emotions, no will, not even a soul.\n\nA living corpse that exists only to 'return existence to nothing.'",
        "『虚无处刑人·Nul』。\n\n那家伙是大宗师『阿斯塔罗特』直属的『人偶』。没有感情，没有意志，甚至连灵魂都没有。\n\n只为『将存在归于虚无』这一目的而行动的活尸。",
        actor=balgas,
    ).say(
        "narr_4",
        "彼は深く息を吐く。",
        "He exhales deeply.",
        "他深深地叹了口气。",
        actor=narrator,
    ).say(
        "balgas_6",
        "……かつての英雄たち、カインを含めた俺の仲間たちも、あいつに飲み込まれて消えた。\n\n俺たちが持っていた『意志』も、『信念』も、全てあいつの『無』の前では意味を成さなかった。",
        "...The heroes of old, my comrades including Cain... they were all swallowed by him and vanished.\n\nOur 'will,' our 'conviction'... none of it meant a damn thing against his 'nothingness.'",
        "……曾经的英雄们，包括凯恩在内的老子的伙伴们，都被那家伙吞噬消失了。\n\n我们拥有的『意志』也好，『信念』也好，在那家伙的『虚无』面前全都毫无意义。",
        actor=balgas,
    ).say(
        "balgas_7b",
        "……俺が敗北した時の経験から、助言させてもらう。どんなに良い一撃だろうと、ヌルは一撃で殺すことはできねえ。トドメを刺した瞬間に一度は復活しちまう。",
        "...From my own defeat, let me give ya some advice. No matter how good your strike is, you can't kill Null in one blow. The moment you deal the finishing hit, it revives once.",
        "……从老子战败的经验来说一句。不管多好的一击，都无法一击杀死Nul。给予致命一击的瞬间，它会复活一次。",
        actor=balgas,
    ).say(
        "balgas_7c",
        "それから、あいつの体は液体金属の特性を持っている。闇雲に攻撃すると傷口から分裂しちまう。厄介だろ？",
        "Also, its body has liquid metal properties. Attack carelessly and it'll split from the wounds. Pain in the ass, right?",
        "而且，那家伙的身体有液态金属的特性。胡乱攻击的话会从伤口分裂。麻烦吧？",
        actor=balgas,
    ).say(
        "balgas_7d",
        "……対策がわかる訳じゃねえが、昔、分裂体特攻の巻物の噂を聞いたことがある。イルヴァに戻れるお前なら、探してみてもいいかもしれんな。",
        "...I don't know the countermeasure, but I once heard rumors of a scroll effective against split creatures. Since you can return to Ylva, might be worth looking for.",
        "……老子不知道对策，不过以前听说过有一种对分裂体特攻的卷轴。既然你能回到伊尔瓦，找找看也许有用。",
        actor=balgas,
    )

    # プレイヤーの選択肢1
    builder.choice(
        react1_null,
        "……それでも、勝ち目はあるのか？",
        "...Even so, is there a chance of winning?",
        "……即便如此，也有胜算吗？",
        text_id="c1_null",
    ).choice(
        react1_fear,
        "……お前も恐れているのか",
        "...Are you afraid too?",
        "……你也害怕吗",
        text_id="c1_fear",
    ).choice(
        react1_silent,
        "（無言で聞く）",
        "(Listen in silence)",
        "（默默倾听）",
        text_id="c1_silent",
    )

    # 選択肢反応1
    builder.step(react1_null).say(
        "balgas_r1", "……正直、分からねえ。だが、お前ならやれるかもしれん。", "...Honestly, I don't know. But you... you might just pull it off.", "……说实话，老子也不知道。但你小子说不定能行。", actor=balgas
    ).jump(scene2)

    builder.step(react1_fear).say(
        "balgas_r2",
        "……ああ。恐れてる。あいつは、俺が今まで信じてきた全てを否定する存在だ。",
        "...Yeah. I'm afraid. He's the embodiment of everything that denies all I've ever believed in.",
        "……嗯。害怕。那家伙是否定老子至今为止相信的一切的存在。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "balgas_r3",
        "……まあ、黙って聞いててくれ。リリィからも話がある。",
        "...Well, just keep listening. Lily's got something to say too.",
        "……算了，默默听着吧。莉莉也有话要说。",
        actor=balgas,
    ).jump(scene2)

    # ========================================
    # シーン2: リリィの忠告
    # ========================================
    builder.step(scene2).focus_chara(Actors.LILY).say(
        "narr_5",
        "リリィが近づいてくる。その目には、珍しく真剣な光が宿っている。",
        "Lily approaches. An unusually earnest light dwells in her eyes.",
        "莉莉走过来。她的眼中罕见地闪烁着认真的光芒。",
        actor=narrator,
    ).say(
        "lily_1a",
        "……お客様。ヌルは、このアリーナが生み出した『究極の絶望』の結晶です。\n\nあなたがどれほど強くなっても、どれほど技術を磨いても、『虚無』の前では全てが等しく無意味となるかもしれません。",
        "...Dear guest. Null is the crystallization of 'ultimate despair' that this arena has spawned.\n\nNo matter how strong you become, no matter how you hone your skills, before the 'void,' all may prove equally meaningless.",
        "……客人。Nul是这个角斗场所孕育的『究极绝望』的结晶。\n\n无论您变得多么强大，无论您的技术多么精湛，在『虚无』面前，一切可能都同样毫无意义。",
        actor=lily,
    ).say(
        "lily_1b",
        "それでも、あなたには、私たちがいます。",
        "Yet still, you have us.",
        "即便如此，您还有我们。",
        actor=lily,
    ).say(
        "lily_4",
        "バルガスさんの技、あなたが救った魂たち......\n\n虚無に抗う唯一の方法は、『自身』を信じ続けることです。\n\nあなたが今まで積み上げてきた全ての選択、全ての戦い……それが、あなたを虚無から守る盾になります。",
        "Vargus's techniques, the souls you have saved...\n\nThe sole means to resist the void is to continue believing in 'yourself.'\n\nEvery choice you have made, every battle you have fought... these shall become the shield that protects you from the void.",
        "巴尔加斯先生的技艺、您拯救的灵魂们……\n\n抵抗虚无的唯一方法，是持续相信『自己』。\n\n您至今为止积累的所有选择、所有战斗……都将成为保护您免受虚无侵蚀的盾牌。",
        actor=lily,
    ).say(
        "narr_7",
        "リリィは、あなたの額に軽く口づけをする。",
        "Lily places a gentle kiss upon your forehead.",
        "莉莉轻轻地在您额头上落下一吻。",
        actor=narrator,
    ).say(
        "lily_7",
        "……行ってらっしゃい。そして、必ず戻ってきてください。",
        "...Go now. And please, return to me without fail.",
        "……请慢走。然后，请务必回来。",
        actor=lily,
    )

    # プレイヤーの選択肢2
    builder.choice(
        react2_survive,
        "……生き残る。必ず",
        "...I will survive. No matter what.",
        "……我会活下来。一定",
        text_id="c2_survive",
    ).choice(
        react2_meaning,
        "自身を信じる",
        "I'll believe in myself.",
        "我会相信自己",
        text_id="c2_meaning",
    ).choice(
        react2_nod, "（無言で頷く）", "(Nod silently)", "（默默点头）", text_id="c2_nod"
    )

    # 選択肢反応2
    builder.step(react2_survive).say(
        "lily_r4",
        "ええ。その言葉を信じていますよ。",
        "Yes. I shall hold those words dear.",
        "是的。我会铭记这句话的。",
        actor=lily,
    ).jump(battle_start)

    builder.step(react2_meaning).say(
        "lily_r5",
        "……ええ。それがあなたの武器です。",
        "...Indeed. That is your weapon.",
        "……是的。那就是您的武器。",
        actor=lily,
    ).jump(battle_start)

    builder.step(react2_nod).say(
        "lily_r6",
        "……無口ですが、その目は答えを語っていますね。",
        "...You speak few words, yet your eyes convey the answer.",
        "……虽然沉默，但您的眼神已经给出了答案呢。",
        actor=lily,
    ).jump(battle_start)

    # ========================================
    # シーン3: 戦闘開始
    # ========================================
    builder.step(battle_start).play_bgm("BGM/Battle_Null_Assassin").say(
        "narr_8",
        "闘技場の門を潜ると、そこは無音の世界だった。",
        "Passing through the arena gates, you enter a world of absolute silence.",
        "穿过角斗场大门，那里是一个无声的世界。",
        actor=narrator,
    ).say(
        "narr_9",
        "観客の声も、風の音も、自分の心臓の鼓動さえも、全てが消え去っている。",
        "The roar of the crowd, the whisper of wind, even your own heartbeat... all have vanished.",
        "观众的声音、风的声音、甚至自己心脏的跳动，一切都消失了。",
        actor=narrator,
    ).shake().say(
        "narr_10",
        "中央に立つのは、輪郭すら曖昧な、黒い霧のような人型の影。",
        "Standing at the center is a humanoid shadow like black mist, its very outline indistinct.",
        "站在中央的是一个连轮廓都模糊不清的、如黑雾般的人形阴影。",
        actor=narrator,
    ).say(
        "narr_11",
        "それは、こちらを見ているのか、見ていないのかすら分からない。ただ、その存在が放つ『虚無』の圧力が、魂を押し潰そうとしている。",
        "You cannot tell if it sees you or not. Only the crushing pressure of 'void' emanating from its existence threatens to obliterate your very soul.",
        "无法分辨它是否在注视着这边。只有它的存在所散发的『虚无』压力，正试图压碎灵魂。",
        actor=narrator,
    ).shake().start_battle_by_stage(
        "rank_b_trial", master_id="sukutsu_arena_master"
    ).finish()


def add_rank_up_B_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    Rank B 昇格試合の勝利/敗北ステップを arena_master ビルダーに追加する

    Args:
        builder: arena_master の ArenaDramaBuilder インスタンス
        victory_label: 勝利ステップのラベル名
        defeat_label: 敗北ステップのラベル名
        return_label: 結果表示後にジャンプするラベル名
    """
    pc = Actors.PC
    lily = Actors.LILY
    balgas = Actors.BALGAS
    narrator = Actors.NARRATOR

    # ========================================
    # Rank B 昇格試合 勝利
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Emotional_Sacred_Triumph_Special"
    ).say(
        "rb_narr_v1",
        "ヌルの体が霧のように崩れ始めた瞬間--",
        "The moment Null's body begins to crumble like mist--",
        "Nul的身体开始如雾般崩溃的瞬间--",
        actor=narrator,
    ).shake().say(
        "rb_narr_v1b",
        "闘技場の空間が歪み、巨大な『手』が虚空から伸びてきた。",
        "The arena's space warps, and a massive 'hand' reaches out from the void.",
        "角斗场的空间扭曲，一只巨大的『手』从虚空中伸出。",
        actor=narrator,
    ).say(
        "rb_narr_v1c",
        "その手は崩れかけたヌルの体を掴み、次元の裂け目へと引きずり込んでいく。",
        "The hand grasps Null's collapsing form and drags it into a dimensional rift.",
        "那只手抓住正在崩塌的Nul的身体，将其拖入次元裂缝。",
        actor=narrator,
    ).say(
        "rb_narr_v1d",
        "一瞬、ヌルの瞳に……何か、光が宿ったように見えた。",
        "For just an instant, something like light seemed to dwell in Null's eyes...",
        "一瞬间，Nul的眼中……似乎闪过了什么光芒。",
        actor=narrator,
    ).say(
        "rb_narr_v2",
        "それは、観客の歓声でも、嘲笑でもない。ただ、静かな沈黙の中に響く、あなた自身の心臓の鼓動。",
        "No cheering crowd, no mocking laughter. Only your own heartbeat echoing in the quiet silence.",
        "既没有观众的欢呼，也没有嘲笑。只有在寂静中回响的，自己的心跳声。",
        actor=narrator,
    ).say(
        "rb_narr_v3",
        "ロビーに戻ると、リリィが駆け寄ってきた。",
        "Returning to the lobby, Lily rushes toward you.",
        "回到大厅，莉莉跑了过来。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "rb_lily_v1",
        "……おかえりなさい。",
        "...Welcome back.",
        "……欢迎回来。",
        actor=lily,
    ).say(
        "rb_narr_v4",
        "彼女の目には涙。サキュバスが、二度目の涙を流している。",
        "Tears glisten in her eyes. For the second time, the succubus weeps.",
        "她的眼中含着泪水。魅魔第二次流下了眼泪。",
        actor=narrator,
    ).say(
        "rb_lily_v2",
        "あなたは……本当に、虚無を打ち破ったのですね。",
        "You have... truly shattered the void.",
        "您……真的打破了虚无呢。",
        actor=lily,
    ).say(
        "rb_lily_v3",
        "信じられません。このアリーナの歴史で、ヌルを倒した闘士は……あなたが初めてです。",
        "It is beyond belief. In all this arena's history, you are the first gladiator to defeat Null.",
        "难以置信。在这个角斗场的历史上，打倒Nul的角斗士……您是第一个。",
        actor=lily,
    ).focus_chara(Actors.BALGAS).say(
        "rb_narr_v5",
        "バルガスが、珍しく笑顔で近づいてくる。",
        "Vargus approaches with a rare smile on his face.",
        "巴尔加斯罕见地面带微笑走过来。",
        actor=narrator,
    ).say(
        "rb_balgas_v1",
        "……やりやがったな。",
        "...You actually did it.",
        "……你小子还真做到了。",
        actor=balgas,
    ).say(
        "rb_balgas_v2",
        "お前は、カインが持っていた以上の……いや、俺たち全員が持っていなかった『何か』を持っている。",
        "You've got more than what Cain had... no, you've got 'something' none of us ever had.",
        "你小子拥有比凯恩更多的……不，是我们所有人都没有的『某种东西』。",
        actor=balgas,
    ).say(
        "rb_balgas_v3",
        "今日からお前は、ただの『朱砂食い』じゃねえ。",
        "From today, you ain't just some 'Cinnabar Eater' anymore.",
        "从今天起你小子不再只是『朱砂吞噬者』了。",
        actor=balgas,
    ).say(
        "rb_balgas_v4",
        "絶望の空を飛び越え、希望を掴み取る……『銀翼』だ。",
        "Soaring beyond the skies of despair, seizing hope... you're the 'Silver Wing.'",
        "飞越绝望的天空，抓住希望……你是『银翼』。",
        actor=balgas,
    ).focus_chara(Actors.LILY).say(
        "rb_lily_v4",
        "では、報酬の授与です。",
        "Now then, allow me to present your reward.",
        "那么，请接受您的报酬。",
        actor=lily,
    ).complete_quest(QuestIds.RANK_UP_B).grant_rank_reward(
        "B", actor=lily
    ).change_journal_phase("sukutsu_arena", 7).finish()

    # ========================================
    # Rank B 昇格試合 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Lobby_Normal"
    ).focus_chara(Actors.LILY).say(
        "rb_lily_d1",
        "……虚無に、飲み込まれてしまいましたね。",
        "...You were swallowed by the void.",
        "……被虚无吞噬了呢。",
        actor=lily,
    ).say(
        "rb_lily_d2",
        "でも、あなたはまだ生きています。それだけで、十分に奇跡です。",
        "Yet you still live. That alone is miracle enough.",
        "但是，您还活着。仅此一点，就已经是奇迹了。",
        actor=lily,
    ).say(
        "rb_lily_d3",
        "準備が整ったら、また挑戦してください。私たちは、ここで待っていますから。",
        "When you are ready, please challenge him once more. We shall be here, waiting for you.",
        "准备好了请再次挑战。我们会在这里等着您的。",
        actor=lily,
    ).finish()
