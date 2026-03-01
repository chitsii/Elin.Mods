# -*- coding: utf-8 -*-
"""
iris_hotspring.py - 境界の足湯、今日も行こ
アイリスの足湯リカバリーシナリオ

理不尽クイズ方式で、試行錯誤を楽しむシナリオ。
正解するまでループし、全問正解でバフ付与。
"""

from arena.builders import DramaBuilder
from arena.data import Actors


def define_iris_hotspring(builder: DramaBuilder):
    """
    アイリスの足湯リカバリー
    シナリオ: 00_iris_trainer.md クエスト3
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
    react1_guide = builder.label("react1_guide")
    react1_tired = builder.label("react1_tired")
    react1_nod = builder.label("react1_nod")

    # シーン2: 地下への道
    scene2 = builder.label("scene2_descent")

    # シーン3: 泉
    scene3 = builder.label("scene3_spring")

    # クイズ1: この泉の名前
    quiz1 = builder.label("quiz1")
    quiz1_wrong = builder.label("quiz1_wrong")
    quiz1_correct = builder.label("quiz1_correct")

    # クイズ2: 観客の数
    quiz2 = builder.label("quiz2")
    quiz2_wrong = builder.label("quiz2_wrong")
    quiz2_correct = builder.label("quiz2_correct")

    # クイズ3: 眠気
    quiz3 = builder.label("quiz3")
    quiz3_wrong = builder.label("quiz3_wrong")
    quiz3_correct = builder.label("quiz3_correct")

    # エピローグ
    epilogue = builder.label("epilogue")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 疲れ顔。足、終わってんじゃん
    # ========================================
    builder.step(main).say(
        "narr_1",
        "闘技場の裏手。石壁が湿気を帯び、錆びた鉄扉の匂いが鼻につく。",
        "Behind the arena. The stone walls are damp, and the smell of rusted iron doors lingers.",
        "斗技场后面。石壁带着湿气，生锈的铁门气味刺鼻。",
        actor=narrator,
    ).say(
        "narr_2",
        "体が重い。一歩ごとに、戦いで酷使した筋肉が抗議する。",
        "The body is heavy. With each step, muscles strained in battle protest.",
        "身体沉重。每走一步，战斗中过度使用的肌肉都在抗议。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara("sukutsu_trainer").say(
        "iris_1",
        "ね、顔。疲れすぎ",
        "Hey, your face. Too tired",
        "诶，你的脸。太累了",
        actor=iris,
    ).say(
        "iris_2",
        "足、終わってんじゃん。…足湯、行こ。今日も",
        "Your legs are done for. ...Let's go to the hot spring. Again today",
        "腿已经废了吧。……去泡脚吧。今天也是",
        actor=iris,
    ).say(
        "iris_3",
        "ここ行くとさ、体のノイズ消えるんだよね。疲れとか、緊張とか",
        "When you go there, the noise in your body disappears. Fatigue, tension, all of it",
        "去那里的话，身体的杂音就会消失呢。疲劳啊、紧张啊什么的",
        actor=iris,
    ).say(
        "iris_4",
        "…疲れ、溜めすぎると試合に響くっしょ？",
        "...If you build up too much fatigue, it'll affect your matches, right?",
        "……疲劳积累太多会影响比赛的吧？",
        actor=iris,
    ).jump(choice1)

    builder.step(choice1).choice(
        react1_guide,
        "案内して",
        "Show me the way",
        "带我去",
        text_id="c1_guide",
    ).choice(
        react1_tired,
        "疲れ、そんなに溜まってた？",
        "Was I that tired?",
        "这么累吗？",
        text_id="c1_tired",
    ).choice(
        react1_nod,
        "無言で頷く",
        "Nod silently",
        "无言地点头",
        text_id="c1_nod",
    ).on_cancel(react1_nod)

    builder.step(react1_guide).say(
        "react1_guide",
        "よし。置いてかないからついて来て",
        "Okay. I won't leave you behind, so follow me",
        "好。不会丢下你的，跟上来",
        actor=iris,
    ).jump(scene2)

    builder.step(react1_tired).say(
        "react1_tired",
        "顔に出てたよ？…ま、私もだけど",
        "It showed on your face, you know? ...Well, same for me though",
        "脸上都写着呢？……嘛，我也是",
        actor=iris,
    ).jump(scene2)

    builder.step(react1_nod).say(
        "react1_nod",
        "うん。そういう返事、落ち着く",
        "Yeah. That kind of response is calming",
        "嗯。这样的回答让人安心",
        actor=iris,
    ).jump(scene2)

    # ========================================
    # シーン2: 地下への道：湿った石と、近い手
    # ========================================
    builder.step(scene2).say(
        "narr_3",
        "狭い階段を降りていく。湿った石の冷気が、足裏から体に染みてくる。",
        "Descending the narrow stairway. The chill of damp stone seeps into the body through the soles.",
        "沿着狭窄的阶梯下行。潮湿石头的冷气从脚底渗入身体。",
        actor=narrator,
    ).say(
        "narr_4",
        "アイリスが先を行き、時折振り返る。暗がりの中、自然と距離が縮まっていく。",
        "Iris goes ahead, occasionally looking back. In the dim passage, the distance naturally closes.",
        "艾丽丝走在前面，偶尔回头看。在昏暗中，距离自然拉近。",
        actor=narrator,
    ).say(
        "narr_5",
        "差し出された手を取る。石の冷たさと、掌の温もり。その対比が、妙に心地よい。",
        "Taking the offered hand. The cold of stone, the warmth of her palm. The contrast feels strangely pleasant.",
        "握住伸出的手。石头的冷，手掌的暖。这种对比竟然很舒服。",
        actor=narrator,
    ).say(
        "iris_descent_1",
        "足元ヤバいよ。コケたら笑うからね",
        "The footing is dangerous. If you slip, I'll laugh",
        "脚下很危险哦。要是摔倒了我会笑的",
        actor=iris,
    ).say(
        "iris_descent_2",
        "…いや、嘘。ちゃんと支える",
        "...No, just kidding. I'll support you properly",
        "……不，开玩笑的。我会好好扶着你",
        actor=iris,
    ).jump(scene3)

    # ========================================
    # シーン3: 泉：青白いぬるさと、変な空気
    # ========================================
    builder.step(scene3).say(
        "narr_6",
        "蒼白き光が、岩肌を撫でている。水面は静かに脈動し、まるで生きているかのように揺らめいていた。",
        "A pale blue light caresses the rocky surface. The water's surface pulsates quietly, swaying as if alive.",
        "苍白的光抚摸着岩壁。水面静静地脉动，宛如活物般摇曳。",
        actor=narrator,
    ).say(
        "narr_7",
        "大気が異質だ。湯気もなく温かい。鉱物めいた気配と、雨上がりの空気に似た何かが、漂っている。",
        "The atmosphere is foreign. Warm without steam. Something mineral-like and something resembling air after rain drifts about.",
        "空气很异样。没有蒸汽却温暖。矿物般的气息和类似雨后空气的什么东西在飘浮。",
        actor=narrator,
    ).say(
        "iris_spring_1",
        "ここ。境界の泉",
        "Here. The Boundary Spring",
        "这里。边界之泉",
        actor=iris,
    ).say(
        "iris_spring_2",
        "空気変でしょ？ これ、ふつーの水じゃないやつ",
        "The air is weird, right? This isn't normal water",
        "空气很奇怪吧？这不是普通的水",
        actor=iris,
    ).say(
        "iris_spring_3",
        "理由は聞くな。私もよくわかんない",
        "Don't ask why. I don't really understand either",
        "别问为什么。我也不太明白",
        actor=iris,
    ).say(
        "iris_spring_4",
        "とりあえず足、浸けよ。クイズしながら",
        "Anyway, let's dip our feet. While doing some quizzes",
        "总之先把脚泡进去。边做测验",
        actor=iris,
    ).say(
        "narr_8",
        "靴を脱ぎ、足を水に浸す。ぬるいのに、芯から温まる。疲れが足先から抜けていく。",
        "Removing shoes, dipping feet in the water. Lukewarm, yet warming from the core. Fatigue drains from the toes.",
        "脱掉鞋子，把脚浸入水中。虽然温吞，却从身体深处温暖起来。疲劳从脚尖流走。",
        actor=narrator,
    ).jump(quiz1)

    # ========================================
    # クイズ1: この泉の名前（理不尽）
    # ========================================
    builder.step(quiz1).say(
        "iris_q1",
        "この泉、私が勝手につけてる名前あるんだけど。何だと思う？",
        "I have a name I gave this spring on my own. What do you think it is?",
        "这个泉，我自己起了个名字。你觉得是什么？",
        actor=iris,
    ).choice(
        quiz1_wrong, "癒やしの泉", "Healing Spring", "治愈之泉", text_id="c_q1_heal"
    ).choice(
        quiz1_wrong, "忘却の泉", "Spring of Oblivion", "遗忘之泉", text_id="c_q1_forget"
    ).choice(
        quiz1_wrong, "境界の泉", "Boundary Spring", "边界之泉", text_id="c_q1_boundary"
    ).choice(
        quiz1_correct, "ぬるま湯", "Lukewarm Water", "温吞水", text_id="c_q1_lukewarm"
    ).on_cancel(quiz1_wrong)

    builder.step(quiz1_wrong).say(
        "iris_q1_wrong",
        "違うー",
        "Nope~",
        "不对~",
        actor=iris,
    ).action(
        "modInvoke", param="apply_iris_training_punish('splash')"
    ).say(
        "narr_q1_wrong",
        "水飛沫が顔にかかる。温い。だが、その温さが妙に心地よかった。",
        "Water droplets splash on your face. Lukewarm. Yet, that warmth was strangely pleasant.",
        "水花溅到脸上。温温的。但那温暖却奇妙地舒适。",
        actor=narrator,
    ).say(
        "iris_q1_hint",
        "もっと適当な名前。もっかい",
        "A more casual name. Again",
        "更随便的名字。再来",
        actor=iris,
    ).jump(quiz1)

    builder.step(quiz1_correct).say(
        "iris_q1_correct",
        "当たり。だってぬるいし",
        "Bingo. Because it's lukewarm",
        "答对了。因为水是温的嘛",
        actor=iris,
    ).say(
        "iris_q1_follow",
        "境界とか癒やしとか、そういう大げさなの…なんか恥ずいじゃん",
        "Boundary, healing... those grand names... kinda embarrassing, right?",
        "什么边界啊治愈啊，那种夸张的名字……总觉得很羞耻",
        actor=iris,
    ).jump(quiz2)

    # ========================================
    # クイズ2: 観客の数（シュール）
    # ========================================
    builder.step(quiz2).say(
        "narr_q2",
        "静寂の空間。水の揺らぎのみが、微かに響いている。",
        "A space of silence. Only the rippling of water echoes faintly.",
        "寂静的空间。只有水的波动微微回响。",
        actor=narrator,
    ).say(
        "iris_q2",
        "今、私たちを見てる観客、何人いると思う？",
        "How many audience members do you think are watching us right now?",
        "你觉得现在有多少观众在看着我们？",
        actor=iris,
    ).choice(
        quiz2_wrong, "0人", "0", "0人", text_id="c_q2_0"
    ).choice(
        quiz2_wrong, "3人", "3", "3人", text_id="c_q2_3"
    ).choice(
        quiz2_wrong, "無限", "Infinite", "无限", text_id="c_q2_inf"
    ).choice(
        quiz2_correct, "わからない", "I don't know", "不知道", text_id="c_q2_unknown"
    ).on_cancel(quiz2_wrong)

    builder.step(quiz2_wrong).say(
        "iris_q2_wrong",
        "はずれ",
        "Wrong",
        "错了",
        actor=iris,
    ).action(
        "modInvoke", param="apply_iris_training_punish('poke')"
    ).say(
        "narr_q2_wrong",
        "肩を押され、均衡が揺らぐ。水面が波立ち、蒼い光が乱れた。",
        "Pushed on the shoulder, your balance wavers. The water surface ripples, the blue light disturbed.",
        "肩膀被推，平衡摇晃。水面泛起涟漪，蓝光扰乱。",
        actor=narrator,
    ).say(
        "iris_q2_hint",
        "確信持って答えられる？ もっかい",
        "Can you answer with certainty? Again",
        "能确定地回答吗？再来",
        actor=iris,
    ).jump(quiz2)

    builder.step(quiz2_correct).say(
        "iris_q2_correct",
        "正解。わからないのが正解",
        "Correct. Not knowing is the correct answer",
        "正确。不知道就是正确答案",
        actor=iris,
    ).say(
        "iris_q2_follow",
        "だから怖いの。見えない、数えられない、でもいる。…たぶん",
        "That's why it's scary. Can't see them, can't count them, but they're there. ...Probably",
        "所以才可怕。看不见，数不清，但是在那里。……大概",
        actor=iris,
    ).jump(quiz3)

    # ========================================
    # クイズ3: 眠気（ナンセンス）
    # ========================================
    builder.step(quiz3).say(
        "narr_q3",
        "少女の瞼が、微かに下がり始める。その肩が、こちらへと傾きかけた。",
        "The girl's eyelids begin to droop faintly. Her shoulder starts to tilt toward you.",
        "少女的眼睑开始微微下垂。她的肩膀开始向你倾斜。",
        actor=narrator,
    ).say(
        "iris_q3",
        "……ラスト。私今、起きてる？ 寝てる？",
        "...Last one. Am I awake right now? Or asleep?",
        "……最后一个。我现在是醒着还是睡着？",
        actor=iris,
    ).choice(
        quiz3_wrong, "起きてる", "Awake", "醒着", text_id="c_q3_awake"
    ).choice(
        quiz3_wrong, "寝てる", "Asleep", "睡着", text_id="c_q3_asleep"
    ).choice(
        quiz3_wrong, "半分", "Half and half", "一半一半", text_id="c_q3_half"
    ).choice(
        quiz3_correct, "どっちでもない", "Neither", "都不是", text_id="c_q3_neither"
    ).on_cancel(quiz3_wrong)

    builder.step(quiz3_wrong).say(
        "iris_q3_wrong",
        "ブー",
        "Bzzt",
        "哔--",
        actor=iris,
    ).action(
        "modInvoke", param="apply_iris_training_punish('poke')"
    ).say(
        "narr_q3_wrong",
        "頬を突かれる。軽い刺激で、意識が引き戻される。",
        "Cheek poked. The light stimulus pulls consciousness back.",
        "脸颊被戳。轻微的刺激把意识拉了回来。",
        actor=narrator,
    ).say(
        "iris_q3_hint",
        "二択じゃない。もっかい",
        "It's not binary. Again",
        "不是二选一。再来",
        actor=iris,
    ).jump(quiz3)

    builder.step(quiz3_correct).say(
        "iris_q3_correct",
        "正解。この体、たまに曖昧になるんだよね",
        "Correct. This body sometimes becomes ambiguous",
        "正确。这具身体有时候会变得模糊",
        actor=iris,
    ).say(
        "iris_q3_follow",
        "境界だから。起きてるとか寝てるとか、そういう区切りが…ぼやける",
        "Because it's a boundary. The distinctions like awake or asleep... they blur",
        "因为是边界。醒着还是睡着什么的，那种界限……会变模糊",
        actor=iris,
    ).jump(epilogue)

    # ========================================
    # エピローグ
    # ========================================
    builder.step(epilogue).say(
        "narr_epi_1",
        "立ち上がれば、足が軽い。重力から解き放たれたかのような錯覚が、全身を包んでいた。",
        "Standing up, your feet feel light. An illusion of being freed from gravity envelops your entire body.",
        "站起来时，脚变得轻盈。仿佛从重力中解放的错觉包裹着全身。",
        actor=narrator,
    ).say(
        "narr_epi_2",
        "肉体の重さが消え、泉の温もりのみが残る。戦いの荒々しさが、僅かに丸みを帯びた。",
        "The weight of the body disappears, leaving only the warmth of the spring. The roughness of battle has taken on a slight roundness.",
        "肉体的重量消失，只留下泉水的温暖。战斗的粗糙感稍微变得圆润。",
        actor=narrator,
    ).say(
        "iris_epi_1",
        "全問正解。…やるね。私の適当な問題に付き合ってくれて",
        "All correct. ...Not bad. Thanks for going along with my random questions",
        "全都答对了。……不错嘛。谢谢配合我的随便问题",
        actor=iris,
    ).say(
        "iris_epi_2",
        "そろそろ戻ろ。長居するとさ、変な夢みるんだよね",
        "Let's head back soon. If we stay too long, I have weird dreams",
        "差不多该回去了。待太久的话会做奇怪的梦呢",
        actor=iris,
    ).say(
        "iris_epi_3",
        "しばらくは体、楽なはず。疲れが溜まりにくくなってる",
        "Your body should feel easy for a while. Fatigue won't build up as easily",
        "身体应该会轻松一段时间。疲劳不容易积累了",
        actor=iris,
    ).action(
        "modInvoke", param="apply_iris_training_buff('peace')"
    ).say(
        "narr_epi_3",
        "体の奥から、温もりが湧き上がる。静かに、でも確かに、力が満ちていく。",
        "Warmth wells up from deep within. Quietly, but surely, strength fills the body.",
        "温暖从身体深处涌起。静静地，但确实地，力量充盈起来。",
        actor=narrator,
    ).say(
        "narr_epi_4",
        "階段を登る途中、アイリスが振り返った。青い光を背に、少しだけ笑っている。",
        "Climbing the stairs, Iris looks back. With the blue light behind her, she wears a slight smile.",
        "爬楼梯的时候，艾丽丝回头看。背着蓝光，她微微笑着。",
        actor=narrator,
    ).say(
        "iris_epi_4",
        "また来るっしょ？ 今日みたいに",
        "You'll come again, right? Like today",
        "还会来吧？像今天一样",
        actor=iris,
    ).jump(ending)

    # ========================================
    # 終了
    # ========================================
    builder.step(ending).finish()
