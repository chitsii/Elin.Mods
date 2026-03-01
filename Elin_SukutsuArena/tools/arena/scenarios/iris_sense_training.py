# -*- coding: utf-8 -*-
"""
iris_sense_training.py - 闇で当てろ、息と匂い
アイリスの感覚遮断訓練シナリオ

理不尽クイズ方式で、試行錯誤を楽しむシナリオ。
正解するまでループし、全問正解でバフ付与。
"""

from arena.builders import DramaBuilder
from arena.data import Actors


def define_iris_sense_training(builder: DramaBuilder):
    """
    アイリスの感覚遮断訓練
    シナリオ: 00_iris_trainer.md クエスト1
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    iris = builder.register_actor("sukutsu_trainer", "アイリス", "Iris")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")

    # シーン1: 導入
    scene1 = builder.label("scene1_intro")
    choice1 = builder.label("choice1")
    react1_scared = builder.label("react1_scared")
    react1_excited = builder.label("react1_excited")
    react1_silent = builder.label("react1_silent")

    # シーン2: 目隠し装着
    scene2 = builder.label("scene2_blindfold")

    # ツッコミ分岐
    tsukkomi_choice = builder.label("tsukkomi_choice")
    tsukkomi_yith = builder.label("tsukkomi_yith")

    # クイズ1: 色当て
    quiz1 = builder.label("quiz1")
    quiz1_wrong = builder.label("quiz1_wrong")
    quiz1_correct = builder.label("quiz1_correct")

    # クイズ2: 数字当て
    quiz2 = builder.label("quiz2")
    quiz2_wrong = builder.label("quiz2_wrong")
    quiz2_correct = builder.label("quiz2_correct")

    # クイズ3: 感情読み
    quiz3 = builder.label("quiz3")
    quiz3_wrong = builder.label("quiz3_wrong")
    quiz3_correct = builder.label("quiz3_correct")

    # エピローグ
    epilogue = builder.label("epilogue")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 導入 - 説明の充実化
    # ========================================
    builder.step(main).say(
        "narr_1",
        "控え室に静寂が満ちている。窓より差し込む光が、漂う塵埃を淡く照らしていた。",
        "Silence fills the waiting room. Light streaming through the window faintly illuminates the drifting dust.",
        "休息室弥漫着寂静。从窗户照进来的光淡淡地照亮着飘浮的尘埃。",
        actor=narrator,
    ).say(
        "narr_2",
        "アイリスの手に白い布が揺れている。それが目の前に掲げられた。",
        "A white cloth sways in Iris's hand. It is raised before your eyes.",
        "白布在艾丽丝手中摇曳。它被举到你的眼前。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara("sukutsu_trainer").say(
        "iris_1",
        "ねー、戦闘中に視界奪われること、あるじゃん？ 暗闘とか目潰しとか",
        "Hey, you know how sometimes your vision gets taken during a fight? Like in darkness or when blinded",
        "诶，战斗中有时会失去视野对吧？像是在黑暗中或被弄瞎的时候",
        actor=iris,
    ).say(
        "iris_2",
        "そういう時、パニックになると負け確。だから今日は、目を使わないで相手の位置を掴む練習ね",
        "When that happens, panic means certain defeat. So today, we'll practice sensing your opponent's position without using your eyes",
        "那种时候一旦慌张就必输。所以今天练习不用眼睛感知对手的位置",
        actor=iris,
    ).say(
        "iris_3",
        "皮膚で空気の動きを感じて、耳で足音を拾って…気配を読む。それができれば、闇の中でも戦える",
        "Feel the air movement with your skin, pick up footsteps with your ears... read the presence. If you can do that, you can fight even in darkness",
        "用皮肤感受空气的流动，用耳朵捕捉脚步声……读懂气息。能做到这些的话，即使在黑暗中也能战斗",
        actor=iris,
    ).say(
        "iris_4",
        "ね、目隠しするよ。怖い？…それとも、ちょっとワクワクしてる？",
        "So, I'm gonna blindfold you. Scared? ...Or maybe a little excited?",
        "那，我要蒙上你的眼睛了。害怕吗？……还是说，有点兴奋？",
        actor=iris,
    )

    # 選択肢1
    builder.choice(
        react1_scared,
        "怖いけど、やる",
        "Scared, but I'll do it",
        "虽然害怕，但我会做",
        text_id="c1_scared",
    ).choice(
        react1_excited,
        "ワクワクしてる",
        "I'm excited",
        "我很兴奋",
        text_id="c1_excited",
    ).choice(
        react1_silent,
        "（無言で布を受け取る）",
        "(Silently take the cloth)",
        "（沉默地接过布）",
        text_id="c1_silent",
    )

    builder.step(react1_scared).say(
        "iris_r1",
        "それそれ。怖い方が集中できるし。…心拍、上がってんのバレてるよ？",
        "That's it. Fear helps you focus. ...Your heartbeat's going up, I can tell?",
        "就是这样。害怕的话反而能集中精神。……心跳加速被我发现了哦？",
        actor=iris,
    ).jump(scene2)

    builder.step(react1_excited).say(
        "iris_r2",
        "ウケる。じゃあ容赦しない。ドキドキも訓練のうちー",
        "Ha! Then I won't go easy. The thrill is part of the training~",
        "哈哈。那我就不客气了。心跳加速也是训练的一部分~",
        actor=iris,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "iris_r3",
        "潔っ。いいね、そういうの好き",
        "Straight to the point. I like that",
        "干脆。我喜欢这样的",
        actor=iris,
    ).jump(scene2)

    # ========================================
    # シーン2: 目隠し装着
    # ========================================
    builder.step(scene2).say(
        "narr_3",
        "白布が瞼に触れた刹那、世界から光が奪われる。",
        "The moment the white cloth touches your eyelids, light is stolen from the world.",
        "白布触及眼睑的刹那，光从世界中被夺走。",
        actor=narrator,
    ).say(
        "narr_4",
        "視覚を失った代わりに、他の感覚が目覚め始める。空気の流れ、微かな物音、肌を撫でる温度の揺らぎ。",
        "In place of lost sight, other senses begin to awaken. The flow of air, faint sounds, the fluctuation of temperature caressing your skin.",
        "失去视觉的代价是，其他感官开始觉醒。空气的流动、微弱的声响、抚摸肌肤的温度波动。",
        actor=narrator,
    ).say(
        "narr_5",
        "結び目を紡ぐ指先が後頭部を掠める。その軌跡にのみ、熱が残った。",
        "Fingertips weaving the knot graze the back of your head. Only along that path does warmth remain.",
        "编织结扣的指尖掠过后脑。只有那轨迹上残留着温热。",
        actor=narrator,
    ).say(
        "iris_5",
        "よし。じゃ、訓練開始。…ちょっと変わった方法でやるから、ついてきてね",
        "Alright. Let's start training. ...I'll do it in a bit of an unusual way, so keep up",
        "好。那么开始训练。……会用有点特别的方法，跟上来哦",
        actor=iris,
    ).say(
        "iris_6",
        "私が質問する。あなたは、目を閉じたまま答える。理屈じゃなくて、感覚で",
        "I'll ask questions. You answer with your eyes closed. Not with logic, but with feeling",
        "我来提问。你闭着眼睛回答。不是靠逻辑，而是靠感觉",
        actor=iris,
    ).jump(tsukkomi_choice)

    # ========================================
    # ツッコミ分岐
    # ========================================
    builder.step(tsukkomi_choice).choice(
        quiz1,
        "（黙って従う）",
        "(Silently comply)",
        "（沉默地服从）",
        text_id="c_comply",
    ).choice(
        tsukkomi_yith,
        "感覚で読心できるわけないだろ",
        "You can't read minds with senses",
        "感觉怎么可能读心",
        text_id="c_tsukkomi",
    )

    builder.step(tsukkomi_yith).say(
        "iris_yith_1",
        "あー、それね",
        "Ah, that",
        "啊，这个啊",
        actor=iris,
    ).say(
        "iris_yith_2",
        "実はね…私たち、意識を時空の彼方に投射できたりするの。過去にも未来にも",
        "Actually... we can project our consciousness beyond time and space. To the past, to the future",
        "其实呢……我们能把意识投射到时空的彼方。无论是过去还是未来",
        actor=iris,
    ).say(
        "iris_yith_3",
        "円錐の器に宿り、あらゆる知識を収集し続ける…そういう存在",
        "Dwelling in conical vessels, endlessly collecting all knowledge... that kind of being",
        "寄宿在圆锥形的容器中，不断收集一切知识……就是那种存在",
        actor=iris,
    ).say(
        "iris_yith_4",
        "だから知ってんの。感覚で読心もできるよ。間違いない",
        "That's why I know. You can read minds with senses too. No doubt about it",
        "所以我知道的。感觉也能读心哦。没错",
        actor=iris,
    ).say(
        "iris_yith_5",
        "……なーんて、冗談だよ、冗談。…たぶん。続きしよっか",
        "...Just kidding, it's a joke, a joke. ...Probably. Shall we continue?",
        "……骗你的，开玩笑的，开玩笑。……大概。继续吧",
        actor=iris,
    ).jump(quiz1)

    # ========================================
    # クイズ1: 色当て（理不尽）
    # ========================================
    builder.step(quiz1).say(
        "iris_q1",
        "ね、今の私の気持ち、何色だと思う？",
        "Hey, what color do you think my feelings are right now?",
        "诶，你觉得我现在的心情是什么颜色？",
        actor=iris,
    ).choice(quiz1_wrong, "赤", "Red", "红色", text_id="c_q1_red").choice(
        quiz1_wrong, "青", "Blue", "蓝色", text_id="c_q1_blue"
    ).choice(
        quiz1_correct, "透明", "Transparent", "透明", text_id="c_q1_transparent"
    ).choice(
        quiz1_wrong, "虹色", "Rainbow", "彩虹色", text_id="c_q1_rainbow"
    ).on_cancel(quiz1_wrong)

    builder.step(quiz1_wrong).say(
        "iris_q1_wrong",
        "ブー、違う",
        "Bzzt. Wrong",
        "哔--错了",
        actor=iris,
    ).action("modInvoke", param="apply_iris_training_punish('pinch')").say(
        "narr_q1_wrong",
        "頬に鋭い痛みが走る。指先で抓まれたのだ。",
        "A sharp pain runs through your cheek. You have been pinched by her fingertips.",
        "脸颊传来尖锐的疼痛。被指尖掐住了。",
        actor=narrator,
    ).say(
        "iris_q1_hint",
        "もっかい。感じて",
        "Try again. Feel it",
        "再来一次。用心感受",
        actor=iris,
    ).jump(quiz1)

    builder.step(quiz1_correct).say(
        "iris_q1_correct",
        "正解。今、何も考えてなかった",
        "Correct. I wasn't thinking about anything just now",
        "正确。我刚才什么都没想",
        actor=iris,
    ).say(
        "iris_q1_follow",
        "…マジで当たるんだ。センスあるじゃん",
        "...You actually got it. Not bad",
        "……真的猜对了。有天赋嘛",
        actor=iris,
    ).jump(quiz2)

    # ========================================
    # クイズ2: 数字当て（理不尽）
    # ========================================
    builder.step(quiz2).say(
        "narr_q2",
        "束の間の沈黙。やがて、アイリスの息遣いが近づいてくる。",
        "A brief silence. Soon, Iris's breathing draws near.",
        "短暂的沉默。不久，艾丽丝的呼吸声靠近了。",
        actor=narrator,
    ).say(
        "iris_q2",
        "次。私が今思い浮かべてる数字、当てて",
        "Next. Guess the number I'm thinking of right now",
        "下一个。猜猜我现在想的数字",
        actor=iris,
    ).choice(quiz2_wrong, "313", "313", "313", text_id="c_q2_313").choice(
        quiz2_wrong, "331", "331", "331", text_id="c_q2_331"
    ).choice(quiz2_wrong, "367", "367", "367", text_id="c_q2_367").choice(
        quiz2_correct, "全部", "All of them", "全部", text_id="c_q2_all"
    ).on_cancel(quiz2_wrong)

    builder.step(quiz2_wrong).say(
        "iris_q2_wrong",
        "残念ー",
        "Too bad~",
        "可惜~",
        actor=iris,
    ).action("modInvoke", param="apply_iris_training_punish('flick')").say(
        "narr_q2_wrong",
        "額に衝撃。弾かれた痛みが、鈍く頭蓋に響く。",
        "Impact on the forehead. The pain of being flicked echoes dully through the skull.",
        "额头受到冲击。被弹开的疼痛在头颅中沉闷地回响。",
        actor=narrator,
    ).say(
        "iris_q2_hint",
        "もっかい",
        "Again",
        "再来",
        actor=iris,
    ).jump(quiz2)

    builder.step(quiz2_correct).say(
        "iris_q2_correct",
        "正解。全部ハッピー素数",
        "Correct. They're all happy primes",
        "正确。全部都是幸福素数",
        actor=iris,
    ).say(
        "iris_q2_follow",
        "1と自分自身でしか割れない。ひとりぼっち。それでも幸せな数でもある",
        "Primes can only be divided by 1 and themselves. Alone. But they can still be happy numbers",
        "素数只能被1和自己整除。孤独。但它们也能成为幸福数",
        actor=iris,
    ).say(
        "iris_q2_follow2",
        "…いいよね、そういうの",
        "...I like that",
        "……挺好的，这种感觉",
        actor=iris,
    ).jump(quiz3)

    # ========================================
    # クイズ3: 思考当て（Elin/Elonaロア）
    # ========================================
    builder.step(quiz3).say(
        "iris_q3_setup",
        "ラスト。私が今、何を考えてたか当てて",
        "Last one. Guess what I was just thinking about",
        "最后一个。猜猜我刚才在想什么",
        actor=iris,
    ).say(
        "narr_q3",
        "沈黙が降りる。アイリスの気配が、一瞬だけ遠くなった気がした。",
        "Silence descends. For a moment, Iris's presence seemed to drift far away.",
        "沉默降临。有一瞬间，艾丽丝的气息似乎飘向了远方。",
        actor=narrator,
    ).say(
        "iris_q3",
        "…はい、何？",
        "...Okay, what is it?",
        "……好，是什么？",
        actor=iris,
    ).choice(
        quiz3_wrong,
        "第三紀の月が砕けた夜",
        "The night the Third Era moon shattered",
        "第三纪月亮破碎之夜",
        text_id="c_q3_moon",
    ).choice(
        quiz3_wrong,
        "レム・イドがメシェーラに飲まれた日",
        "The day Rem Id fell to Meshera",
        "雷姆·伊德被梅谢拉吞噬之日",
        text_id="c_q3_meshera",
    ).choice(
        quiz3_correct,
        "パルミアに核の炎が降る瞬間",
        "The moment nuclear fire falls on Palmia",
        "核焰降临帕尔米亚之时",
        text_id="c_q3_palmia",
    ).choice(
        quiz3_wrong,
        "異形の森が焼かれた後の静寂",
        "The silence after the Lesimas forest burned",
        "异形之森燃尽后的寂静",
        text_id="c_q3_lesimas",
    ).on_cancel(quiz3_wrong)

    builder.step(quiz3_wrong).say(
        "iris_q3_wrong",
        "違う。それは、もう考え終わった",
        "Wrong. I finished thinking about that already",
        "不对。那个我已经想完了",
        actor=iris,
    ).action("modInvoke", param="apply_iris_training_punish('flick')").say(
        "narr_q3_wrong",
        "頭頂に軽い衝撃。的確な一撃が、思考を揺さぶる。",
        "A light impact on the crown of your head. A precise strike shakes your thoughts.",
        "头顶传来轻微的冲击。精准的一击震动着思绪。",
        actor=narrator,
    ).say(
        "iris_q3_hint",
        "もっかい",
        "Again",
        "再来",
        actor=iris,
    ).jump(quiz3)

    builder.step(quiz3_correct).say(
        "iris_q3_correct",
        "正解。パルミア",
        "Correct. Palmia",
        "正确。帕尔米亚",
        actor=iris,
    ).say(
        "iris_q3_follow",
        "…あれは綺麗だったな。灰になった王都。愚かな選択の末路",
        "...It was beautiful. The royal capital reduced to ash. The end of a foolish choice",
        "……那很美。化为灰烬的王都。愚蠢选择的结局",
        actor=iris,
    ).say(
        "iris_q3_follow2",
        "まだ起きていないけど、起きた。時間って、そういうものでしょ？",
        "It hasn't happened yet, but it did. That's how time works, right?",
        "还没发生，但已经发生了。时间就是这样的，对吧？",
        actor=iris,
    ).jump(epilogue)

    # ========================================
    # エピローグ
    # ========================================
    builder.step(epilogue).say(
        "narr_epi_1",
        "白布が解かれ、世界に光が戻る。目を開けると、アイリスの顔が目の前にあった。想像よりも、ずっと近い。",
        "The white cloth is untied, and light returns to the world. Opening your eyes, Iris's face is right before you. Far closer than imagined.",
        "白布被解开，光回到世界。睁开眼睛，艾丽丝的脸就在眼前。比想象中更近得多。",
        actor=narrator,
    ).say(
        "iris_epi_1",
        "……驚いた。全部当てたじゃん",
        "...I'm surprised. You got them all right",
        "……吃惊了。全都猜对了啊",
        actor=iris,
    ).say(
        "iris_epi_2",
        "今の感覚、覚えといて。目が見えなくても、気配は読める。息遣いと、空気の揺れと…",
        "Remember that feeling. Even without sight, you can read presence. Breathing, air movement...",
        "记住这种感觉。即使看不见，也能读懂气息。呼吸声、空气的波动……",
        actor=iris,
    ).say(
        "iris_epi_3",
        "はい、おつ。今日の成績、悪くないじゃん",
        "Okay, good work. Today's results weren't bad",
        "好了，辛苦了。今天的成绩不错嘛",
        actor=iris,
    ).say(
        "iris_epi_4",
        "しばらくは感覚が冴えてるはず。次の試合、闇が来ても…たぶん大丈夫",
        "Your senses should stay sharp for a while. Even if darkness comes in the next match... you'll probably be fine",
        "感觉应该会保持敏锐一段时间。下一场比赛就算遇到黑暗……应该没问题",
        actor=iris,
    ).action("modInvoke", param="apply_iris_training_buff('sense')").say(
        "narr_epi_2",
        "五感の輪郭が、研ぎ澄まされていく。視界の縁が、僅かに広がったように感じられた。",
        "The contours of the five senses are being sharpened. The edges of your vision seem to have expanded slightly.",
        "五感的轮廓正在被磨砺。视野的边缘似乎稍微扩展了。",
        actor=narrator,
    ).jump(ending)

    # ========================================
    # 終了
    # ========================================
    builder.step(ending).finish()
