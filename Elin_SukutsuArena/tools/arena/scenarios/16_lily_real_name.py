"""
16_lily_real_name.md - リリィの告白『真名の刻印、永遠の共犯』
リリィが真名を明かし、運命を共にすることを誓う
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds


def define_lily_real_name(builder: DramaBuilder):
    """
    リリィの真名
    シナリオ: 16_lily_real_name.md

    条件:
    - Lily relationship ≥ 20
    - Saved Balgas (scenario 15)
    - Didn't betray Lily with bottle swap (scenario 13)
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_meeting")
    choice1 = builder.label("choice1")
    react1_what = builder.label("react1_what")
    react1_want = builder.label("react1_want")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_observer_end")
    choice2 = builder.label("choice2")
    react2_enchanted = builder.label("react2_enchanted")
    react2_me_too = builder.label("react2_me_too")
    react2_touch = builder.label("react2_touch")
    scene2_5 = builder.label("scene2_5_no_home")
    choice2_5 = builder.label("choice2_5")
    react2_5_hard = builder.label("react2_5_hard")
    react2_5_place = builder.label("react2_5_place")
    react2_5_hand = builder.label("react2_5_hand")
    scene2_5_cont = builder.label("scene2_5_cont")
    scene3 = builder.label("scene3_true_name")
    choice3 = builder.label("choice3")
    react3_tell = builder.label("react3_tell")
    react3_carry = builder.label("react3_carry")
    react3_nod = builder.label("react3_nod")
    name_revelation = builder.label("name_revelation")
    scene4 = builder.label("scene4_contract")
    final_choice = builder.label("final_choice")
    final_thanks = builder.label("final_thanks")
    final_protect = builder.label("final_protect")
    final_embrace = builder.label("final_embrace")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 月光のない密会
    # ========================================
    builder.step(main).play_bgm("BGM/Lily_Private_Room").say(
        "narr_1",
        "激闘が終わり、静まり返ったアリーナの私室。\nいつも以上に濃い紫煙が揺らめき、その奥でリリィはバルガスから贈られた古い酒瓶を愛おしそうに眺めていた。",
        "After the fierce battle, silence fills Lily's private chamber.\nThicker than usual, purple smoke drifts through the air. Beyond it, Lily gazes lovingly at the old wine bottle Balgas gave her.",
        '激战结束后，莉莉的私室一片寂静。\n比平时更浓的紫烟袅袅升起，莉莉正深情地凝视着巴尔加斯送给她的那瓶陈年老酒。',
        actor=narrator,
    ).say(
        "narr_3",
        "あなたが部屋に入ると、彼女はゆっくりと立ち上がり、普段の事務的な仮面を完全に脱ぎ捨てた「一人の女」の顔で微笑んだ。",
        "As you enter the room, she rises slowly, her professional mask cast aside entirely, revealing the smile of a woman laid bare.",
        '当您走进房间时，她缓缓起身，完全卸下了平日公事公办的面具，以一个女人最真实的面容微笑着。',
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.LILY).say(
        "lily_1",
        "……おかえりなさい。バルガスさんは、今頃泥のように眠っています。\nあんなに安らかな寝顔を見たのは、私も初めてかもしれません。……ふふ、本当に、あなたという人は。",
        "...Welcome back. Balgas is sleeping like a log right now.\nI may have never seen him sleep so peacefully before. ...Hehe, you truly are something.",
        '……您回来了。巴尔加斯先生现在睡得像泥一样沉呢。\n我可能从未见过他睡得如此安详。……呵呵，您这个人啊，真是……',
        actor=lily,
    ).say(
        "narr_4",
        "彼女は酒瓶を置き、あなたに近づく。",
        "She sets down the wine bottle and approaches you.",
        '她放下酒瓶，向您走来。',
        actor=narrator,
    ).say(
        "lily_3",
        "バルガスさんを助けてくれたこと、お礼を言わせてください。……でも、それは単なる『感謝』ではありません。\nあなたが今日、あの残酷な喝采を黙らせた時……私の中で、何かが音を立てて崩れたのです。",
        "Let me thank you for saving Balgas. ...But this is not merely 'gratitude.'\nWhen you silenced that cruel cheering today... something inside me crumbled with an audible sound.",
        '请允许我为您救了巴尔加斯先生而道谢。……但这并非单纯的「感谢」。\n当您今天让那残酷的喝彩声沉默时……我内心深处有什么东西，轰然崩塌了。',
        actor=lily,
    ).jump(choice1)

    # プレイヤーの選択肢1
    builder.choice(
        react1_what, "……何が崩れたんだ？", "...What crumbled?",
        '……什么崩塌了？', text_id="c1_what"
    ).choice(
        react1_want,
        "お前は、俺に何を求めている？",
        "What do you want from me?",
        '你想从我这里得到什么？',
        text_id="c1_want",
    ).choice(
        react1_silent, "（無言で聞く）", "(Listen in silence)",
        '（沉默倾听）', text_id="c1_silent"
    )

    # 選択肢反応1
    builder.step(react1_what).say(
        "lily_r1",
        "私の……『観察者』としての仮面です。もう、あなたを冷静に見ることができません。",
        "My... mask as an 'observer.' I can no longer look at you with detachment.",
        '我的……作为「观察者」的面具。我已经无法冷静地看待您了。',
        actor=lily,
    ).jump(scene2)

    builder.step(react1_want).say(
        "lily_r2",
        "……全てです。あなたの全てを、私に委ねてほしい。",
        "...Everything. I want you to entrust all of yourself to me.",
        '……一切。我希望您将一切都托付给我。',
        actor=lily,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "lily_r3",
        "……無口ですが、その瞳は饒舌ですね。続けさせていただきます。",
        "...You say nothing, yet your eyes speak volumes. Allow me to continue.",
        '……您虽然沉默，但那双眼睛却很坦诚呢。请允许我继续。',
        actor=lily,
    ).jump(scene2)

    # ========================================
    # シーン2: 観察者の終焉
    # ========================================
    builder.step(scene2).play_bgm("BGM/Lily_Seductive_Danger").say(
        "narr_5",
        "リリィはあなたの至近距離まで歩み寄り、その冷たいはずの指先であなたの胸元に触れた。\nそこには「戦鬼」としての力強い鼓動が刻まれている。",
        "Lily steps close to you, her fingertips--which should be cold--touching your chest.\nBeneath them, the powerful heartbeat of a 'War Demon' pulses.",
        '莉莉走到您近前，用那本应冰冷的指尖轻触您的胸口。\n那里跳动着「战鬼」强劲有力的心跳。',
        actor=narrator,
    ).say(
        "lily_5",
        "私は、このアリーナに囚われた魂が、絶望に染まり、最後の一滴まで絞り出されるのを見届けるのが役割でした。\n……あなたも、その一人になるはずだった。",
        "My role was to watch over souls trapped in this arena, witnessing them stain with despair, drained to their very last drop.\n...You were supposed to be one of them.",
        '我的职责，是注视被囚禁在这角斗场的灵魂，看着他们被绝望染黑，直到榨干最后一滴。\n……您本该也是其中之一。',
        actor=lily,
    ).say(
        "lily_7",
        "けれど、あなたは強くなるほどに優しく、孤独になるほどに誰かの手を握ろうとした。\n……その姿を特等席で眺めているうちに、私の方が、あなたの魂に『魅了』されてしまったようです。",
        "Yet the stronger you became, the gentler you grew. The more alone you were, the more you reached out for someone's hand.\n...Watching you from my privileged seat, it seems I was the one who became 'enchanted' by your soul.",
        '然而，您越是变强就越温柔，越是孤独就越想握住他人的手。\n……在特等席上注视着您的身影时，似乎是我，被您的灵魂「魅惑」了呢。',
        actor=lily,
    ).jump(choice2)

    # プレイヤーの選択肢2
    builder.choice(
        react2_enchanted,
        "サキュバスなのに……？",
        "A succubus, enchanted...?",
        '魅魔被魅惑了……？',
        text_id="c2_enchanted",
    ).choice(
        react2_me_too,
        "俺も、お前に魅了されている",
        "I'm enchanted by you too.",
        '我也被你迷住了。',
        text_id="c2_me_too",
    ).choice(
        react2_touch,
        "（無言で頬に触れる）",
        "(Touch her cheek in silence)",
        '（默默地触碰她的脸颊）',
        text_id="c2_touch",
    )

    # 選択肢反応2
    builder.step(react2_enchanted).say(
        "lily_r4",
        "ええ。サキュバスが、人間に魅了される……滑稽でしょう？",
        "Yes. A succubus, enchanted by a mortal... How absurd, wouldn't you say?",
        '是的。魅魔被凡人魅惑……很可笑吧？',
        actor=lily,
    ).jump(scene2_5)

    builder.step(react2_me_too).say(
        "lily_r5",
        "……！ あなた、そんな……。",
        "...! You... saying such things...",
        '……！您，竟然这样说……',
        actor=lily,
    ).say(
        "narr_7",
        "リリィは頬を染め、目を逸らす。",
        "Lily blushes and looks away.",
        '莉莉双颊绯红，移开了视线。',
        actor=narrator,
    ).jump(scene2_5)

    builder.step(react2_touch).say(
        "lily_r6",
        "……あぁ、温かい。あなたの手は、いつも温かいですね。",
        "...Ahh, so warm. Your hands are always so warm.",
        '……啊，好温暖。您的手，总是这么温暖呢。',
        actor=lily,
    ).jump(scene2_5)

    # ========================================
    # シーン2.5: 帰る場所のない者
    # ========================================
    builder.step(scene2_5).play_bgm("BGM/Lily_Confession").say(
        "narr_nh1",
        "リリィは窓辺に歩み寄り、次元の狭間に浮かぶ歪んだ景色を眺める。",
        "Lily walks to the window, gazing at the distorted scenery floating in the dimensional rift.",
        '莉莉走到窗边，凝视着漂浮在次元夹缝中扭曲的景象。',
        actor=narrator,
    ).say(
        "lily_nh1",
        "……ねえ、あなたは知っていますか？ 私がどこから来たのか。\n私には『故郷』がないのです。他のサキュバスのように、どこかの確定次元で生まれ、そこから落ちてきたわけではありません。",
        "...Tell me, do you know where I came from?\nI have no 'homeland.' Unlike other succubi, I was not born in some fixed dimension and fell from there.",
        '……您知道吗？我是从哪里来的。\n我没有「故乡」。不像其他魅魔，我并非在某个固定次元出生后坠落至此。',
        actor=lily,
    ).say(
        "lily_nh3",
        "私は……この次元の狭間そのもので生まれました。崩壊した世界の残滓、漂流する感情の欠片、そして『誰かに愛されたい』という無数の魂の願い……。それらが凝縮して、私という存在が生まれた。",
        "I was... born in this dimensional rift itself. The remnants of collapsed worlds, drifting fragments of emotion, and countless souls' wishes to 'be loved by someone'... They condensed to form my existence.",
        '我……就诞生于这次元夹缝本身。崩溃世界的残骸、漂泊的情感碎片、以及无数灵魂「想被某人爱着」的愿望……这一切凝聚在一起，诞生了我这个存在。',
        actor=lily,
    ).say(
        "lily_nh4",
        "だから私には、家族も、帰る場所もない。どこの世界にも属さない。本当の意味で、誰とも繋がっていない、と。……500年もの間、ずっとそう思って生きてきました。",
        "That is why I have no family, no place to return to. I belong to no world. Truly connected to no one. ...For 500 years, I have lived believing this.",
        '所以我没有家人，没有归处。不属于任何世界。真正意义上，与任何人都没有联系。……五百年来，我一直这样认为着。',
        actor=lily,
    ).jump(choice2_5)

    # プレイヤーの選択肢2.5
    builder.choice(
        react2_5_hard,
        "……それは、辛かっただろう",
        "...That must have been painful.",
        '……那一定很痛苦吧。',
        text_id="c2_5_hard",
    ).choice(
        react2_5_hand,
        "（無言で手を握る）",
        "(Take her hand in silence)",
        '（默默握住她的手）',
        text_id="c2_5_hand",
    )

    # 選択肢反応2.5
    builder.step(react2_5_hard).say(
        "lily_nh_r1",
        "……ふふ、それが当たり前でしたから。だからこそ、私は、この闘技場での義務に忠実であることができた。",
        "...Hehe, it was simply my normal. That is precisely why I could remain faithful to my duties in this arena.",
        '……呵呵，因为那是理所当然的。正因如此，我才能忠实于在这角斗场的职责。',
        actor=lily,
    ).jump(scene2_5_cont)

    builder.step(react2_5_hand).say(
        "lily_nh_r3",
        "……温かい。あなたの手は、いつも温かいですね……。",
        "...So warm. Your hands are always so warm...",
        '……好温暖。您的手，总是这么温暖呢……',
        actor=lily,
    ).jump(scene2_5_cont)

    # シーン2.5続き
    builder.step(scene2_5_cont).say(
        "lily_nh5",
        "あなたは違う。あなたには、イルヴァという帰る場所がある。神々との繋がりがあって、いつでもここを去ることができる。\n……最初、それが羨ましく苛立たしかった。『どうせこの人も、いつかは帰るべき場所へ去っていく。私を置いて』と。",
        "You are different. You have Ylva--a place to return to. You have ties to the gods, free to leave here whenever you wish.\n...At first, I envied it. It frustrated me. 'This person will leave too, eventually. Return to where they belong. And leave me behind.'",
        '您不同。您有伊尔瓦--一个可以回去的地方。您与神明有着联系，随时可以离开这里。\n……最初，这让我嫉妒又烦躁。「反正这个人迟早也会回到该去的地方。把我留下」。',
        actor=lily,
    ).say(
        "lily_nh7",
        "でも、あなたは一度去っても、また帰ってきてくれる。バルガスさんのために戦い、私のために怒り……。『義務』ではなく『選択』として、ここにいてくれる。",
        "But you--even when you leave, you return. You fought for Balgas, you raged for me... You stay here not out of 'duty,' but by 'choice.'",
        '但是您--即使离开了，也会再回来。为巴尔加斯先生而战，为我而怒……您留在这里，不是出于「义务」，而是「选择」。',
        actor=lily,
    ).say(
        "lily_nh8",
        "それが、どれほど私を救ったか……あなたには分からないでしょう。帰る場所がある人に、『それでも傍にいる』と選ばれること。それは、居場所のない私にとって、初めて与えられた『居場所』でした。",
        "You cannot fathom how much that saved me... To be chosen by someone with a home, someone who says 'I will stay by your side anyway.' For me, who had nowhere to belong--that was the first 'place' I was ever given.",
        '这救了我多少……您无法想象。被一个有归处的人选择，说「即便如此也要陪在身边」。对于无处可归的我而言，那是我第一次被赋予的「归属之地」。',
        actor=lily,
    ).shake().say(
        "narr_nh3",
        "リリィは涙を流しながら、微笑む。",
        "Lily smiles, tears streaming down her face.",
        '莉莉流着泪，微笑着。',
        actor=narrator,
    ).jump(scene3)

    # ========================================
    # シーン3: 禁忌の真名
    # ========================================
    builder.step(scene3).play_bgm("BGM/Lily_Confession").say(
        "narr_8",
        "部屋の照明が一段と暗くなり、リリィの背中にある翼が、意思を持つかのように微かに震える。\n彼女は意を決したように、あなたの耳元に唇を寄せた。",
        "The room's light dims further, and the wings on Lily's back tremble faintly, as if with a will of their own.\nAs if resolved, she brings her lips close to your ear.",
        '房间的灯光更加昏暗，莉莉背上的翅膀微微颤动，仿佛有了自己的意志。\n她似乎下定了决心，将双唇凑近您的耳畔。',
        actor=narrator,
    ).say(
        "lily_9",
        "……これからお話しするのは、この世界のどこにも記録されていない、私の本当の名前。\nアスタロト様すら知らない……私の魂の、一番奥にある『鍵』。",
        "...What I am about to tell you is my true name--recorded nowhere in this world.\nNot even Lord Astaroth knows it... The 'key' hidden in the deepest part of my soul.",
        '……接下来我要告诉您的，是这个世界任何地方都没有记载的，我真正的名字。\n连阿斯塔罗特大人都不知道……我灵魂最深处的「钥匙」。',
        actor=lily,
    ).say(
        "lily_11",
        "これを知る者は、私の全てを支配し、同時に私の運命を一生背負うことになります。……あなたに知っておいてもらいたいのです。",
        "He who knows it will command all of me, and bear my fate for eternity. ...I want you to know it.",
        '知晓此名者，将支配我的一切，同时将背负我的命运至生命尽头。……我想让您知道。',
        actor=lily,
    ).jump(choice3)

    # プレイヤーの選択肢3（重要）
    builder.choice(
        react3_tell, "……聞かせてくれ", "...Tell me.",
        '……告诉我吧。', text_id="c3_tell"
    ).choice(
        react3_carry,
        "お前の全てを、背負わせてくれ",
        "Let me carry all of you.",
        '让我背负你的一切。',
        text_id="c3_carry",
    ).choice(react3_nod, "（無言で頷く）", "(Nod in silence)",
        '（默默点头）', text_id="c3_nod")

    # 選択肢反応3
    builder.step(react3_tell).say(
        "lily_r7",
        "……ふふ、やっぱり、あなたはそう言ってくれるのですね。",
        "...Hehe, I knew you would say that.",
        '……呵呵，果然，您会这么说呢。',
        actor=lily,
    ).jump(name_revelation)

    builder.step(react3_carry).say(
        "narr_11",
        "リリィは涙を流し、微笑む。",
        "Lily sheds tears and smiles.",
        '莉莉流下泪水，微笑着。',
        actor=narrator,
    ).jump(name_revelation)

    builder.step(react3_nod).say(
        "lily_r9",
        "……無口ですが、その瞳は真剣ですね。では、お伝えします。",
        "...You say nothing, yet your eyes are earnest. Then, I shall tell you.",
        '……您虽然沉默，但那双眼睛很认真呢。那么，我告诉您。',
        actor=lily,
    ).jump(name_revelation)

    # 真名の啓示
    builder.step(name_revelation).say(
        "lily_12",
        "……私の名は、『リリシエル・サングイス・ルナエ（Lilithiel Sanguis Lunae）』。",
        "...My name is 'Lilithiel Sanguis Lunae.'",
        '……我的名字是「莉莉西尔·桑奎斯·卢娜（Lilithiel Sanguis Lunae）」。',
        actor=lily,
    ).shake().say(
        "lily_14",
        "これからは、単なるあなたのマネージャーとしてではなく……\nあなたの行く末を地獄の果てまで共にする、『共犯者』として隣に置かせてちょうだい。",
        "From now on, not merely as your manager...\nPlease keep me by your side as your 'accomplice'--one who will follow you to the very depths of hell.",
        '从今以后，不再只是作为您的经理……\n请让我作为「共犯」留在您身边--陪您走到地狱尽头。',
        actor=lily,
    ).jump(scene4)

    # ========================================
    # シーン4: 契約の接吻
    # ========================================
    builder.step(scene4).say(
        "narr_13",
        "彼女の真名が告げられた瞬間、あなたの視界に未知のルーンが浮かび上がり、リリィの魔力があなたの体内に流れ込んでくる。\nそれは契約であり、守護であり、深い愛情の証だった。",
        "The moment her true name is spoken, unknown runes rise before your eyes, and Lily's magic flows into your body.\nIt is a contract, a protection, and a testament of deep love.",
        '她的真名被说出的瞬间，未知的符文浮现在您眼前，莉莉的魔力流入您的体内。\n那是契约，是守护，是深爱的证明。',
        actor=narrator,
    ).say(
        "lily_16",
        "……ふふ、これで逃げられなくなりましたね。\nアスタロト様の待つ頂上で、何が起きようとも……あなたの背中は私が守ります。",
        "...Hehe, now you cannot escape.\nWhatever awaits at the summit where Lord Astaroth waits... I shall protect your back.",
        '……呵呵，这下您跑不掉了呢。\n在阿斯塔罗特大人等待的顶端，无论发生什么……我会守护您的背后。',
        actor=lily,
    ).say(
        "narr_15",
        "リリィはあなたの頬に手を添え、優しく口づけをする。",
        "Lily places her hand on your cheek and gently presses her lips to yours.",
        '莉莉将手贴上您的脸颊，温柔地献上一吻。',
        actor=narrator,
    ).shake().say(
        "lily_18",
        "さあ、行きましょう。あなたの伝説に、私の名前を添えて。",
        "Now, let us go. Let my name be written alongside your legend.",
        '好了，我们走吧。让我的名字，与您的传说一同铭刻。',
        actor=lily,
    ).jump(final_choice)

    # 最終選択肢
    builder.choice(
        final_thanks,
        "……ありがとう、リリシエル",
        "...Thank you, Lilithiel.",
        '……谢谢你，莉莉西尔。',
        text_id="c_final_thanks",
    ).choice(
        final_protect,
        "お前を、必ず守る",
        "I will protect you. Always.",
        '我一定会保护你。',
        text_id="c_final_protect",
    ).choice(
        final_embrace, "（抱きしめる）", "(Embrace her)",
        '（拥抱她）', text_id="c_final_embrace"
    )

    builder.step(final_thanks).say(
        "lily_r10",
        "その名で呼ばれるのは、初めてです。……嬉しいわ。",
        "That is the first time anyone has called me by that name. ...I am happy.",
        '这是第一次有人这样称呼我。……我很高兴。',
        actor=lily,
    ).jump(ending)

    builder.step(final_protect).say(
        "lily_r11",
        "ふふ、守るのは私の役目ですよ。でも……ありがとう。",
        "Hehe, protecting is my role. But... thank you.",
        '呵呵，守护是我的职责哦。但是……谢谢您。',
        actor=lily,
    ).jump(ending)

    builder.step(final_embrace).say(
        "lily_r12",
        "……あぁ、温かい。あなたの温もりが、私を満たしてくれる……。",
        "...Ahh, so warm. Your warmth fills me completely...",
        '……啊，好温暖。您的温暖，将我填满……',
        actor=lily,
    ).jump(ending)

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending).complete_quest(QuestIds.LILY_REAL_NAME).finish()
