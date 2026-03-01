# -*- coding: utf-8 -*-
"""
iris_leg_training.py - 足腰マジで裏切らん
アイリスの足腰基礎トレーニングシナリオ

理不尽クイズ方式で、試行錯誤を楽しむシナリオ。
正解するまでループし、全問正解でバフ付与。
"""

from arena.builders import DramaBuilder
from arena.data import Actors


def define_iris_leg_training(builder: DramaBuilder):
    """
    アイリスの足腰基礎トレーニング
    シナリオ: 00_iris_trainer.md クエスト2
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
    react1_tough = builder.label("react1_tough")
    react1_trust = builder.label("react1_trust")
    react1_silent = builder.label("react1_silent")

    # シーン2: フォーム矯正
    scene2 = builder.label("scene2_form")

    # クイズ1: 好きな筋肉
    quiz1 = builder.label("quiz1")
    quiz1_wrong = builder.label("quiz1_wrong")
    quiz1_correct = builder.label("quiz1_correct")

    # クイズ2: 蹴りの回数
    quiz2 = builder.label("quiz2")
    quiz2_wrong = builder.label("quiz2_wrong")
    quiz2_correct = builder.label("quiz2_correct")

    # クイズ3: 痛いのはどこ
    quiz3 = builder.label("quiz3")
    quiz3_wrong = builder.label("quiz3_wrong")
    quiz3_correct = builder.label("quiz3_correct")

    # エピローグ
    epilogue = builder.label("epilogue")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 基礎の日。逃げんな
    # ========================================
    builder.step(main).play_bgm("BGM/Iris_Funk").say(
        "narr_1",
        "稽古場に静かな熱気が漂う。木床の硬い感触が、足裏から全身に伝わる。",
        "A quiet heat drifts through the training hall. The hard texture of the wooden floor spreads from the soles to the whole body.",
        "练功房弥漫着静谧的热气。木地板坚硬的触感从脚底传遍全身。",
        actor=narrator,
    ).say(
        "narr_2",
        "呼吸を重ねるたび、肉体が動くことを求めている。本能が、鍛錬を欲していた。",
        "With each breath, the body demands movement. Instinct craves training.",
        "每次呼吸，肉体都在渴求运动。本能渴望着锻炼。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara("sukutsu_trainer").say(
        "iris_1",
        "立って。今の重心、上に逃げてる。…そのままだと蹴り、軽い",
        "Stand up. Your center of gravity is drifting up. ...Your kicks will be weak like that",
        "站好。你现在的重心在往上飘。……那样的话踢腿会很轻",
        actor=iris,
    ).say(
        "iris_2",
        "足腰ってさ、戦いの土台。ここが崩れたら、どんな技も意味ない",
        "Your legs and hips are the foundation of combat. If this crumbles, no technique matters",
        "腿和腰是战斗的基础。这里垮了的话，什么招式都没用",
        actor=iris,
    ).say(
        "iris_3",
        "だから今日は基礎。地味だけど、これが一番大事",
        "So today is fundamentals. Boring, but this is the most important",
        "所以今天练基础。虽然无聊，但这是最重要的",
        actor=iris,
    ).jump(choice1)

    builder.step(choice1).choice(
        react1_tough,
        "今日はきつそう",
        "Today looks tough",
        "今天看起来很辛苦",
        text_id="c1_tough",
    ).choice(
        react1_trust,
        "任せる",
        "I'll leave it to you",
        "交给你了",
        text_id="c1_trust",
    ).choice(
        react1_silent,
        "無言で構える",
        "Silently take stance",
        "无言地摆好架势",
        text_id="c1_silent",
    ).on_cancel(react1_silent)

    builder.step(react1_tough).say(
        "react1_tough",
        "きつい日ほど伸びるんだわ。汗かいた分だけ強くなる",
        "The tougher the day, the more you grow. You get stronger for every drop of sweat",
        "越辛苦的日子进步越大。流的汗都会变成力量",
        actor=iris,
    ).jump(scene2)

    builder.step(react1_trust).say(
        "react1_trust",
        "よし。じゃ、触る。フォーム直すから文句言うな",
        "Good. Then I'll touch you. Don't complain, I'm fixing your form",
        "好。那我要碰你了。不要抱怨，我在纠正你的姿势",
        actor=iris,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "react1_silent",
        "黙ってやるタイプね。嫌いじゃない",
        "The silent type, huh. I don't hate that",
        "沉默寡言的类型啊。不讨厌",
        actor=iris,
    ).jump(scene2)

    # ========================================
    # シーン2: フォーム矯正
    # ========================================
    builder.step(scene2).say(
        "narr_3",
        "アイリスが背後に回る。一瞬で距離が詰まった。",
        "Iris moves behind you. The distance closes in an instant.",
        "艾丽丝绕到身后。距离瞬间拉近。",
        actor=narrator,
    ).say(
        "narr_4",
        "腰に掌が触れる。圧がじわりと入り、骨盤が正しい角度へ導かれていく。",
        "A palm touches your waist. Pressure seeps in, guiding the pelvis to the correct angle.",
        "手掌触及腰部。压力渗透进来，将骨盆引导至正确的角度。",
        actor=narrator,
    ).say(
        "iris_form_1",
        "腰、落として。はい、そこで止める",
        "Lower your hips. Yes, hold it there",
        "腰沉下去。好，保持住",
        actor=iris,
    ).say(
        "iris_form_2",
        "膝、内側入んな。…うん、いい。いまの筋肉、ちゃんと働いてる",
        "Don't let your knees go inward. ...Yeah, good. Your muscles are working properly now",
        "膝盖不要往内收。……嗯，很好。现在肌肉在正常工作了",
        actor=iris,
    ).say(
        "iris_form_3",
        "呼吸止めてんじゃん。ほら吸って。…そう",
        "You're holding your breath. Come on, breathe in. ...That's it",
        "你在憋气呢。来，吸气。……对了",
        actor=iris,
    ).say(
        "iris_form_4",
        "よし、基礎ができたところで…ちょっとクイズね",
        "Alright, now that we've got the basics down... time for a little quiz",
        "好，基础做好了……来点小测验",
        actor=iris,
    ).say(
        "iris_form_5",
        "スクワットしながら答えて。間違えたら…まあ、わかるっしょ",
        "Answer while doing squats. If you get it wrong... well, you know what happens",
        "边深蹲边回答。答错的话……你懂的",
        actor=iris,
    ).jump(quiz1)

    # ========================================
    # クイズ1: 好きな筋肉（理不尽）
    # ========================================
    builder.step(quiz1).say(
        "narr_q1",
        "蹲踞の姿勢を保ったまま、アイリスの問いが飛んでくる。",
        "While maintaining the crouching posture, Iris's question comes flying.",
        "保持着蹲踞的姿势，艾丽丝的问题飞了过来。",
        actor=narrator,
    ).say(
        "iris_q1",
        "ね、私の好きな筋肉ってどれだと思う？",
        "Hey, which muscle do you think is my favorite?",
        "诶，你觉得我最喜欢哪块肌肉？",
        actor=iris,
    ).choice(
        quiz1_wrong, "大腿四頭筋", "Quadriceps", "股四头肌", text_id="c_q1_quad"
    ).choice(
        quiz1_wrong, "ハムストリングス", "Hamstrings", "腘绳肌", text_id="c_q1_ham"
    ).choice(
        quiz1_wrong, "大殿筋", "Gluteus maximus", "臀大肌", text_id="c_q1_glute"
    ).choice(
        quiz1_correct, "ヒラメ筋", "Soleus", "比目鱼肌", text_id="c_q1_soleus"
    ).on_cancel(quiz1_wrong)

    builder.step(quiz1_wrong).say(
        "iris_q1_wrong",
        "違うー",
        "Nope~",
        "不对~",
        actor=iris,
    ).action("modInvoke", param="apply_iris_training_punish('push')").say(
        "narr_q1_wrong",
        "背中に衝撃が走り、バランスが崩れる。咄嗟に踏ん張らなければ、倒れていた。",
        "Impact runs through your back, breaking your balance. Without bracing instantly, you would have fallen.",
        "冲击传过后背，平衡被打破。要是没有瞬间稳住，就会倒下。",
        actor=narrator,
    ).say(
        "iris_q1_hint",
        "ヒント。名前がかわいいやつ。もっかい",
        "Hint: It's got a cute name. Again",
        "提示：名字很可爱的那个。再来",
        actor=iris,
    ).jump(quiz1)

    builder.step(quiz1_correct).say(
        "iris_q1_correct",
        "当たり。名前がかわいいから",
        "Bingo. Because the name is cute",
        "答对了。因为名字很可爱",
        actor=iris,
    ).say(
        "iris_q1_follow",
        "ヒラメ筋。ほら、ヒラメ。魚みたいでしょ？ …それだけの理由",
        "Soleus. See, flatfish. It sounds like a fish, right? ...That's the only reason",
        "比目鱼肌。你看，比目鱼。像鱼一样对吧？……就这个理由",
        actor=iris,
    ).jump(quiz2)

    # ========================================
    # クイズ2: 蹴り避け訓練
    # ========================================
    builder.step(quiz2).say(
        "narr_q2_1",
        "アイリスが片足を上げる。裸足の甲が、宙に浮かぶ。",
        "Iris raises one leg. Her bare instep floats in the air.",
        "艾丽丝抬起一只脚。赤裸的脚背悬浮在空中。",
        actor=narrator,
    ).say(
        "narr_q2_2",
        "白く滑らかな肌。うっすらと浮かぶ血管の青。形良く揃った指先。淡く桜色に染まった爪。",
        "White, smooth skin. Faint blue veins showing through. Well-shaped toes. Nails tinted pale cherry blossom.",
        "白皙光滑的肌肤。隐约可见的青色血管。形状优美的脚趾。淡淡樱花色的指甲。",
        actor=narrator,
    ).say(
        "narr_q2_3",
        "その繊細さからは想像もつかない速度で、蹴りが放たれた。",
        "A kick is unleashed at a speed unimaginable from such delicacy.",
        "以那纤细难以想象的速度，踢腿挥出。",
        actor=narrator,
    ).say(
        "narr_q2_4",
        "頬に柔らかな感触。避けきれない。足の甲が顔を掠める。しっとりとした肌の感触。",
        "A soft sensation on the cheek. Unable to dodge. The instep grazes the face. The moist feel of skin.",
        "脸颊传来柔软的触感。无法躲开。脚背掠过面庞。湿润肌肤的触感。",
        actor=narrator,
    ).say(
        "narr_q2_5",
        "微かに漂う、石鹸と汗の混じった匂い。甘く、どこか獣めいた香り。",
        "A faint scent of soap and sweat. Sweet, somewhat animalistic.",
        "隐约飘来肥皂和汗水混合的气息。甜美，带着几分野性。",
        actor=narrator,
    ).say(
        "iris_q2_1",
        "遅い。集中して",
        "Too slow. Focus",
        "太慢了。集中",
        actor=iris,
    ).say(
        "narr_q2_6",
        "再び蹴りが飛ぶ。今度は横から。足裏が見える。薄桃色の肉。土踏まずの優美なアーチ。",
        "Another kick comes. From the side this time. The sole is visible. Pale pink flesh. The graceful arch of the instep.",
        "又一脚飞来。这次从侧面。可以看到脚底。淡桃色的肉。足弓的优美曲线。",
        actor=narrator,
    ).say(
        "narr_q2_7",
        "踵の丸みが、白熱灯に照らされて艶めく。",
        "The curve of the heel gleams under the incandescent light.",
        "脚跟的弧度在白炽灯下闪耀。",
        actor=narrator,
    ).say(
        "narr_q2_8",
        "額に触れる。汗ばんだ足の甲。生温かい熱が、肌を通して伝わってくる。",
        "It touches the forehead. The sweaty instep. Lukewarm heat transmits through the skin.",
        "触及额头。汗津津的脚背。温热通过肌肤传递过来。",
        actor=narrator,
    ).say(
        "narr_q2_9",
        "湿った柔肌の感触が、思考を乱す。意識が足に引きずられる。",
        "The sensation of moist soft skin disrupts thought. Consciousness is drawn to the foot.",
        "湿润柔软肌肤的触感扰乱了思绪。意识被那只脚牵引。",
        actor=narrator,
    ).say(
        "iris_q2_2",
        "だーめ。見惚れてんじゃないの",
        "No good. Are you getting distracted?",
        "不行。你在发呆吗",
        actor=iris,
    ).say(
        "narr_q2_10",
        "三度目。上から振り下ろされる踵。ふくらはぎの筋肉がしなやかに躍動する。足首の細さが、その力を際立たせる。",
        "Third time. A heel swings down from above. Calf muscles flex gracefully. The slenderness of the ankle accentuates the power.",
        "第三次。脚跟从上方挥下。小腿肌肉优雅地跃动。纤细的脚踝衬托出力量。",
        actor=narrator,
    ).say(
        "narr_q2_11",
        "引き締まった腱のライン。皮膚の下で脈打つ血流。生命の熱を帯びた、美しい足。",
        "Taut tendon lines. Blood pulsing beneath the skin. A beautiful foot radiating the heat of life.",
        "紧绷的肌腱线条。皮肤下跳动的血流。散发着生命热度的美丽双足。",
        actor=narrator,
    ).say(
        "narr_q2_12",
        "踵が鼻先を掠める。風圧が顔を撫で、ツンとした香りを運んでくる。訓練の汗と、アイリス自身の体臭。嫌ではない。むしろ...",
        "The heel grazes the tip of the nose. Wind pressure caresses the face, carrying the scent. Training sweat and Iris's own body odor. Not unpleasant. Rather--",
        "脚跟掠过鼻尖。风压抚过面庞，带来汗水和艾丽丝本身的体味。并不讨厌。反而--",
        actor=narrator,
    ).say(
        "iris_q2_3",
        "お、避けた。…ギリギリだけど",
        "Oh, you dodged. ...Just barely though",
        "哦，躲开了。……虽然只是勉强",
        actor=iris,
    ).say(
        "iris_q2_4",
        "で、質問。次、どこ狙うと思う？",
        "So, question. Where do you think I'll aim next?",
        "那么，问题。你觉得我下一次会瞄准哪里？",
        actor=iris,
    ).choice(quiz2_wrong, "顔", "Face", "脸", text_id="c_q2_face").choice(
        quiz2_wrong, "腹", "Stomach", "腹部", text_id="c_q2_stomach"
    ).choice(quiz2_correct, "急所", "Vital spot", "要害", text_id="c_q2_vital").choice(
        quiz2_wrong, "足", "Legs", "腿", text_id="c_q2_legs"
    ).on_cancel(quiz2_wrong)

    builder.step(quiz2_wrong).say(
        "iris_q2_wrong",
        "ブー。甘い",
        "Bzzt. Too naive",
        "哔--太天真了",
        actor=iris,
    ).action("modInvoke", param="apply_iris_training_punish('push')").say(
        "narr_q2_wrong",
        "予測が外れた。蹴りは顔ではなく、別の場所に--柔らかい足の甲が、内腿を優しく叩く。戒めの一撃。",
        "The prediction was wrong. The kick went not to the face, but elsewhere--the soft instep gently strikes the inner thigh. A warning blow.",
        "预测错了。踢腿没有瞄准脸，而是别的地方--柔软的脚背轻轻拍打内侧大腿。警告性的一击。",
        actor=narrator,
    ).say(
        "iris_q2_hint",
        "本番だったら、今ので終わってたよ？ もっかい。次こそ読んで",
        "In a real fight, that would've been the end. Again. Read me this time",
        "真正战斗的话，刚才就结束了哦？再来。这次要读懂我",
        actor=iris,
    ).jump(quiz2)

    builder.step(quiz2_correct).say(
        "narr_q2_correct",
        "急所--それしかない。予測した瞬間、体が動いていた。",
        "Vital spot--it has to be. The moment of prediction, the body was already moving.",
        "要害--只能是那里。预测的瞬间，身体已经在动了。",
        actor=narrator,
    ).say(
        "narr_q2_correct2",
        "アイリスの蹴りが、鍛えようのない場所を掠めて空を切る。紙一重。冷や汗が背を伝う。",
        "Iris's kick grazes the place that cannot be trained, cutting through air. Paper-thin margin. Cold sweat runs down the back.",
        "艾丽丝的踢腿掠过那无法锻炼的地方，划破空气。千钧一发。冷汗顺着后背流下。",
        actor=narrator,
    ).say(
        "narr_q2_correct3",
        "もし避けていなければ--考えたくもない。",
        "If it hadn't been dodged--too terrible to imagine.",
        "如果没躲开的话--不敢想象。",
        actor=narrator,
    ).say(
        "iris_q2_correct",
        "正解。よく読んだ",
        "Correct. You read me well",
        "正确。读得不错",
        actor=iris,
    ).say(
        "iris_q2_follow",
        "本気で来られたら、そこ狙うしかないでしょ。どんなに鍛えても、守れない場所",
        "In a serious fight, that's the only place to aim. A spot that can't be protected no matter how much you train",
        "认真打的话，只能瞄准那里。无论怎么锻炼都无法保护的地方",
        actor=iris,
    ).say(
        "iris_q2_follow2",
        "…でも、ギリギリね。もうちょい反応速くしないと",
        "...But that was close. You need to react a bit faster",
        "……不过，好险。反应还要再快一点",
        actor=iris,
    ).jump(quiz3)

    # ========================================
    # クイズ3: スクワット耐久 + のしかかり
    # ========================================
    builder.step(quiz3).say(
        "iris_q3_setup",
        "ラスト。このままスクワットキープね",
        "Last one. Hold this squat position",
        "最后一个。保持这个深蹲姿势",
        actor=iris,
    ).say(
        "narr_q3_1",
        "膝が震える。限界が近い。",
        "Knees trembling. Limit approaching.",
        "膝盖在颤抖。快到极限了。",
        actor=narrator,
    ).say(
        "narr_q3_2",
        "アイリスが背後から近づく。衣擦れの音が、静かな稽古場に響く。",
        "Iris approaches from behind. The rustle of clothes echoes in the quiet training hall.",
        "艾丽丝从身后靠近。衣物摩擦的声音在安静的练功房中回响。",
        actor=narrator,
    ).say(
        "narr_q3_3",
        "そして--背中に、重みがかかった。",
        "And then--weight presses against the back.",
        "然后--背部感受到了重量。",
        actor=narrator,
    ).say(
        "narr_q3_4",
        "じんわりと温かい。背中にあたる、柔らかい感触。そちらに意識が引きずられそうになる。",
        "Warmth spreading gradually. A soft sensation against the back. Attention threatens to drift toward it.",
        "渐渐温暖起来。背部感受到柔软的触感。注意力差点被吸引过去。",
        actor=narrator,
    ).say(
        "narr_q3_5",
        "服越しでも伝わる、体温。じんわりと染み込んでくる、人肌の熱。",
        "Body heat transmitted even through clothes. The warmth of human skin gradually seeping in.",
        "即使隔着衣服也能感受到体温。人体的热度渐渐渗透进来。",
        actor=narrator,
    ).say(
        "narr_q3_6",
        "太ももが脇腹を挟む。引き締まった筋肉と、その上を覆う柔らかな肉。",
        "Thighs clamp around the sides. Toned muscles covered by soft flesh.",
        "大腿夹住腰侧。紧实的肌肉上覆盖着柔软的肉。",
        actor=narrator,
    ).say(
        "narr_q3_7",
        "汗ばんだ素肌の熱が、薄い生地を通して溶け合うように伝わる。",
        "The heat of sweaty bare skin transmits through thin fabric as if melting together.",
        "汗津津的肌肤热度穿过薄薄的布料，仿佛要融为一体般传递过来。",
        actor=narrator,
    ).say(
        "narr_q3_8",
        "重い。膝が、さらに震える。限界が近い。",
        "Heavy. Knees tremble even more. Limit approaching.",
        "好重。膝盖颤抖得更厉害了。快到极限了。",
        actor=narrator,
    ).say(
        "narr_q3_9",
        "だが--この重さが、どこか心地よい。苦しいのに、離れてほしくない。",
        "But--this weight is somehow pleasant. It hurts, yet wanting it not to leave.",
        "但是--这重量却莫名地令人舒适。明明很痛苦，却不想让她离开。",
        actor=narrator,
    ).say(
        "narr_q3_10",
        "吐息が首筋にかかる。規則正しい呼吸。温かく湿った空気が、肌を撫でる。",
        "Breath falls on the nape. Regular breathing. Warm, moist air caresses the skin.",
        "呼吸落在颈后。规律的呼吸。温暖潮湿的空气抚摸着肌肤。",
        actor=narrator,
    ).say(
        "narr_q3_11",
        "甘い香りが鼻腔をくすぐる。汗と、アイリス自身の匂い。",
        "A sweet scent tickles the nostrils. Sweat and Iris's own scent.",
        "甜美的香气挠着鼻腔。汗水和艾丽丝本身的气息。",
        actor=narrator,
    ).say(
        "narr_q3_12",
        "つらい。苦しい。膝が悲鳴を上げている。",
        "Painful. Agonizing. Knees screaming.",
        "好累。好痛苦。膝盖在尖叫。",
        actor=narrator,
    ).say(
        "narr_q3_13",
        "なのに--この瞬間が、永遠に続けばいいとすら思う。",
        "Yet--wishing this moment could last forever.",
        "然而--甚至希望这一刻能永远持续下去。",
        actor=narrator,
    ).say(
        "iris_q3",
        "で、質問。あと何秒耐えられる？",
        "So, question. How many more seconds can you endure?",
        "那么，问题。你还能撑几秒？",
        actor=iris,
    ).choice(quiz3_wrong, "10秒", "10 seconds", "10秒", text_id="c_q3_10").choice(
        quiz3_wrong, "30秒", "30 seconds", "30秒", text_id="c_q3_30"
    ).choice(
        quiz3_wrong, "もう無理", "Can't anymore", "已经不行了", text_id="c_q3_cant"
    ).choice(
        quiz3_correct, "わからない", "I don't know", "不知道", text_id="c_q3_dunno"
    ).on_cancel(quiz3_wrong)

    builder.step(quiz3_wrong).say(
        "iris_q3_wrong",
        "ブー。まだいける",
        "Bzzt. You can still go",
        "哔--还能撑",
        actor=iris,
    ).say(
        "narr_q3_wrong1",
        "アイリスがさらに体重をかけてくる。背中の柔らかさが、より深く押し付けられる。",
        "Iris puts even more weight on. The softness against the back presses deeper.",
        "艾丽丝加上了更多的重量。背后的柔软更深地压了上来。",
        actor=narrator,
    ).say(
        "narr_q3_wrong2",
        "太ももの締め付けが強くなる。逃げ場がない。",
        "The grip of her thighs tightens. No escape.",
        "大腿的夹紧更强了。无处可逃。",
        actor=narrator,
    ).say(
        "narr_q3_wrong3",
        "首筋にかかる吐息が、より近くなる。耳朶に、唇が触れそうなほど。",
        "The breath on the nape grows closer. Lips almost touching the earlobe.",
        "颈后的呼吸更近了。嘴唇近得几乎要触碰耳垂。",
        actor=narrator,
    ).say(
        "narr_q3_wrong4",
        "膝が限界を超えて震える。でも——倒れたくない。この温もりを、もう少しだけ。",
        "Knees tremble beyond their limit. But—don't want to collapse. Just a little more of this warmth.",
        "膝盖颤抖着超越了极限。但是——不想倒下。只想再多感受一会儿这份温暖。",
        actor=narrator,
    ).say(
        "iris_q3_hint",
        "ほら、まだ立ってる。…もっかい",
        "See, you're still standing. ...Again",
        "看，你还站着呢。……再来",
        actor=iris,
    ).jump(quiz3)

    builder.step(quiz3_correct).say(
        "iris_q3_correct",
        "正解。限界は、自分でもわからない",
        "Correct. Even you don't know your own limit",
        "正确。连你自己都不知道自己的极限",
        actor=iris,
    ).say(
        "narr_q3_correct",
        "アイリスがゆっくりと体重を抜いていく。背中から、温もりが離れていく。名残惜しい。",
        "Iris slowly lifts her weight. The warmth leaves the back. A lingering reluctance.",
        "艾丽丝慢慢地减轻了重量。温暖从背部离开。令人依依不舍。",
        actor=narrator,
    ).say(
        "iris_q3_follow",
        "でも、意外と耐えられるもんでしょ",
        "But you can endure more than you think, right?",
        "但是，意外地能撑住对吧",
        actor=iris,
    ).say(
        "iris_q3_follow2",
        "…覚えといて。限界だと思ってからが、本当の勝負",
        "...Remember this. The real battle starts when you think you've reached your limit",
        "……记住。当你觉得到极限的时候，才是真正的战斗开始",
        actor=iris,
    ).jump(epilogue)

    # ========================================
    # エピローグ
    # ========================================
    builder.step(epilogue).say(
        "narr_epi_1",
        "二人とも汗だくだ。呼吸が揃うにつれ、体にも静けさが戻ってきた。",
        "Both drenched in sweat. As breathing aligns, calm returns to the body.",
        "两人都是汗流浃背。随着呼吸同步，身体也恢复了平静。",
        actor=narrator,
    ).say(
        "narr_epi_2",
        "今日の鍛錬が、確かに身体へと刻まれた。その実感が、静かに胸中に満ちていく。",
        "Today's training has certainly been carved into the body. That realization quietly fills the heart.",
        "今天的锻炼确实铭刻在身体中。那份实感静静地充满心中。",
        actor=narrator,
    ).say(
        "iris_epi_1",
        "おつ。全問正解じゃん。やるね",
        "Good work. You got all of them right. Not bad",
        "辛苦了。全都答对了。不错嘛",
        actor=iris,
    ).say(
        "iris_epi_2",
        "ほら水。飲め。倒れたら運ぶの私なんだけど？",
        "Here, water. Drink. If you collapse, I'm the one who has to carry you, you know?",
        "给，水。喝。你要是倒了可是我来扛你哦？",
        actor=iris,
    ).say(
        "iris_epi_3",
        "しばらくは足腰、裏切んないはず。次の試合、床が裏切っても…",
        "Your legs should stay reliable for a while. Even if the floor betrays you in the next match...",
        "腿脚应该会可靠一段时间。下一场比赛就算地板背叛你……",
        actor=iris,
    ).action("modInvoke", param="apply_iris_training_buff('legs')").say(
        "narr_epi_3",
        "下肢に力が漲る。どんな攻撃を受けても、簡単には崩れない。そんな確信が、足元から湧き上がってきた。",
        "Power surges through the lower limbs. No matter what attack comes, it won't crumble easily. Such conviction wells up from beneath the feet.",
        "力量充盈下肢。无论承受何种攻击，都不会轻易崩溃。这样的确信从脚下涌起。",
        actor=narrator,
    ).say(
        "iris_epi_4",
        "…また明日も来いよ",
        "...Come again tomorrow",
        "……明天也来吧",
        actor=iris,
    ).jump(ending)

    # ========================================
    # 終了
    # ========================================
    builder.step(ending).finish()
