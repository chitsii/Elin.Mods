# -*- coding: utf-8 -*-
"""
07_upper_existence.md - Rank D 初戦『見えざる観客の供物』
観客の介入が始まる戦い
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, Keys, QuestBattleFlags, QuestIds, SessionKeys


def define_upper_existence(builder: ArenaDramaBuilder):
    """
    上位存在の観察 - Rank D初戦
    シナリオ: 07_upper_existence.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_warning")
    choice1 = builder.label("choice1")
    react1_how = builder.label("react1_how")
    react1_silence = builder.label("react1_silence")
    react1_ok = builder.label("react1_ok")
    scene2 = builder.label("scene2_announcement")
    scene3 = builder.label("scene3_battle")

    # ========================================
    # 導入: ランクD挑戦前の講義
    # ========================================
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/Lobby_Normal"
    ).say(
        "narr_0", "ロビーに戻ると、バルガスがあなたを呼び止めた。", "Upon returning to the lobby, Vargus called out to you.", "回到大厅时，巴尔加斯叫住了你。", actor=narrator
    ).focus_chara(Actors.BALGAS).say(
        "balgas_0a",
        "……おい、お前。次はランクD『銅貨稼ぎ』への挑戦だな。\nまぁ待て。その前に、一つ教えておくことがある……",
        "...Hey, you. Your next fight's the Rank D 'Copper Earner' challenge.\nHold up. Before that, there's somethin' I gotta teach ya...",
        "……喂，你小子。下一场是D级『铜币赚手』的挑战吧。\n等等。在那之前，有件事得教教你……",
        actor=balgas,
    ).say("balgas_0c", "来い。実際に見せてやる。", "Come on. I'll show ya firsthand.", "跟我来。让你亲眼看看。", actor=balgas).say(
        "narr_0b", "バルガスに連れられ、闘技場の門扉前へと向かう。", "Led by Vargus, you headed toward the arena's main gates.", "跟着巴尔加斯，你走向角斗场的大门。", actor=narrator
    ).play_bgm("BGM/Ominous_Suspense_01").say(
        "narr_1",
        "門扉の前に立つあなたに対し、バルガスはいつになく真剣な表情で、武器の調子を確認している。\n\n上空からは、地鳴りのような低い笑い声が降ってきている。",
        "Standing before the gates, Vargus inspected his weapon with an unusually grave expression.\n\nFrom above, deep laughter rumbled down like distant thunder.",
        "站在大门前的你面前，巴尔加斯正以罕见的严肃表情检查着武器的状态。\n\n从上空传来如地鸣般低沉的笑声。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.BALGAS).say(
        "balgas_1",
        "……おい、耳を澄ませ。あの連中の笑い声が聞こえるか？\nランクDからは、連中はお前をただ観るだけじゃ満足しねえ。",
        "...Hey, listen close. Hear those bastards laughin'?\nFrom Rank D on, they ain't satisfied just watchin' ya.",
        "……喂，竖起耳朵听。听到那帮家伙的笑声了吗？\n从D级开始，那帮家伙光看着你可不会满足。",
        actor=balgas,
    ).say(
        "balgas_3",
        "お前の踊りが退屈だったり、逆に『もっと血が見たい』と思われりゃ……次元の向こうから『プレゼント』が投げ込まれるぜ。",
        "If your little dance bores 'em, or they want more blood... they'll toss 'presents' from beyond the veil.",
        "要是你的表演太无聊，或者他们『想看更多血』的话……就会从次元那边扔『礼物』过来。",
        actor=balgas,
    ).say("narr_3", "彼は武器を叩き、続ける。", "He tapped his weapon and continued.", "他敲了敲武器，继续说道。", actor=narrator).say(
        "balgas_4",
        "ヤジ（物理的な嫌がらせ）だ。\n爆風とともに飛んでくるのは、ポーションかもしれねえし、鈍器かもしれねえ。あるいは地上の物理法則を無視した『異次元のゴミ』だ。",
        "Heckles. Physical ones.\nWhat comes flyin' at ya could be potions, could be blunt objects. Or some dimensional garbage that don't follow our physics.",
        "起哄--物理意义上的。\n随着爆风飞来的可能是药水，也可能是钝器。或者是无视地面物理法则的『异次元垃圾』。",
        actor=balgas,
    ).say(
        "balgas_6",
        "敵だけを見てりゃ、頭を割られて終わりだぞ。\n空の機嫌も伺いながら戦え……クソッタレな商売だろう？",
        "Keep your eyes only on the enemy, and you'll get your skull cracked.\nFight while keepin' the sky happy too... Hell of a job, ain't it?",
        "光盯着敌人的话，脑袋会被砸开的。\n得一边看天空的脸色一边战斗……狗屁差事吧？",
        actor=balgas,
    ).jump(choice1)

    # プレイヤーの選択肢1
    builder.choice(react1_how, "対処法はあるのか？", "Is there any way to deal with it?", "有应对方法吗？", text_id="c1_how").choice(
        react1_silence, "観客を黙らせることはできないのか？", "Can't we shut the audience up?", "不能让观众闭嘴吗？", text_id="c1_silence"
    ).choice(react1_ok, "……分かった", "...Got it.", "……明白了", text_id="c1_ok")

    # 選択肢反応1
    builder.step(react1_how).say(
        "balgas_r1",
        "動き続けることだ。止まれば標的になる。……あと、運を祈れ。",
        "Keep movin'. Stop and you're a target. ...And pray for luck.",
        "不停地动。停下来就成靶子了。……还有，祈祷好运吧。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_silence).say(
        "balgas_r2",
        "無理だ。奴らは高次元にいる。俺たちには手が届かねえ。",
        "Can't be done. They're in a higher dimension. We can't reach 'em.",
        "不可能。那帮家伙在高次元。我们够不着。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_ok).say(
        "balgas_r3", "……気をつけろ。死ぬなよ。", "...Watch yourself. Don't die.", "……小心点。别死了。", actor=balgas
    ).jump(scene2)

    # ========================================
    # シーン2: リリィの残酷なアナウンス
    # ========================================
    builder.step(scene2).play_bgm("BGM/Fanfare_Audience").say(
        "narr_4",
        "闘技場に足を踏み入れると、姿の見えない観客たちの熱気が、肌を焼くような不快な波動となって押し寄せる。\n\nリリィの声が、魔術的な拡声によって会場全体に響き渡った。",
        "Stepping into the arena, the fervor of the unseen spectators washed over you like a searing, oppressive wave.\n\nLily's voice, magically amplified, resonated throughout the entire venue.",
        "踏入角斗场，看不见的观众们的热情如灼烧肌肤的不适波动般涌来。\n\n莉莉的声音通过魔法扩音，回荡在整个会场。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_1",
        "……皆様、お待たせいたしました。\n本日のメインディッシュは、新たな『銅貨稼ぎ』……期待の新人による、命の切り売りでございます！",
        "...Ladies and gentlemen, thank you for your patience.\nToday's main course is a new 'Copper Earner'... a promising newcomer, selling their life piece by piece!",
        "……各位，让您久等了。\n今日的主菜是新晋『铜币赚手』……一位备受期待的新人，正在零售自己的性命！",
        actor=lily,
    ).say("obs_1", "（観客の歓声と拍手のような音が響く。）", "(Cheers and applause-like sounds echoed from the audience.)", "（观众的欢呼声和掌声般的声音回荡着。）", actor=pc).say(
        "lily_3",
        "さあ、皆様。もしこの闘士の戦いぶりがお気に召さない、あるいは『もっと刺激が欲しい』と感じられましたら……\nどうぞ、お手元の『慈悲』を……次元を超えて投げ込んであげてくださいな！",
        "Now then, everyone. If this fighter's performance displeases you, or if you crave 'more excitement'...\nPlease feel free to toss your 'mercy'... across the dimensions!",
        "好了，各位。如果这位角斗士的表现不合您意，或者您觉得『想要更多刺激』的话……\n请把您手边的『慈悲』……跨越次元投给他吧！",
        actor=lily,
    ).say("obs_2", "（観客の笑い声、何かが飛んでくる音。）", "(Laughter from the audience, the sound of objects being thrown.)", "（观众的笑声，有什么东西飞来的声音。）", actor=pc).jump(scene3)

    # ========================================
    # シーン3: 戦闘開始
    # ========================================
    builder.step(scene3).say(
        "narr_6",
        "対戦相手である「メタルプチ」と刃を交えた瞬間、頭上の虚空が歪む。\n紫色の閃光と共に、戦場に「異質な物体」が次々と降り注ぎ始めた……！",
        "The moment you crossed blades with your opponent, a Metal Putit, the void above distorted.\nWith flashes of purple light, 'foreign objects' began raining down upon the battlefield...!",
        "与对手「金属史莱姆」交锋的瞬间，头顶的虚空扭曲了。\n伴随着紫色闪光，「异质物体」开始接连落向战场……！",
        actor=narrator,
    ).shake().set_flag(QuestBattleFlags.RESULT_FLAG, 1).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.UPPER_EXISTENCE
    ).start_battle_by_stage(
        "upper_existence_battle", master_id="sukutsu_arena_master"
    ).finish()


def add_upper_existence_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    上位存在クエストの勝利/敗北ステップを arena_master ビルダーに追加する

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

    # 報酬授与用ラベル
    reward_end_ue = builder.label("reward_end_ue")

    # ========================================
    # 上位存在クエスト 勝利
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Lobby_Normal"
    ).say(
        "ue_narr_v1",
        "満身創痍で敵を倒した瞬間、会場を包んだのは喝采ではなく、勝者を馬鹿にするような高い笑い声だった。\n\nロビーに戻ると、リリィがクスクスと笑いながら出迎える。",
        "The moment you defeated the enemy, battered and bruised, what filled the arena was not cheers, but shrill laughter mocking the victor.\n\nReturning to the lobby, Lily greeted you with a soft giggle.",
        "浑身是伤地击败敌人的瞬间，充斥会场的不是喝彩，而是嘲笑胜者的尖锐笑声。\n\n回到大厅时，莉莉轻笑着迎接你。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "ue_lily_v1",
        "……ふふ、見事な逃げ回りっぷりでした。\n上の方々は、あなたが水のボトルに足を取られた時の慌てた顔が、今日一番の傑作だったと仰っていますよ。",
        "...Fufu, what splendid scurrying about.\nThose above say your panicked face when you tripped over a water bottle was today's finest masterpiece.",
        "……呵呵，逃窜得真精彩呢。\n上面的大人们说，您被水瓶绊倒时慌张的表情是今天最精彩的杰作呢。",
        actor=lily,
    ).say(
        "narr_process1",
        "ふと闘技場の方を振り返ると、敗北したプチの体が霧状の粒子となっていくのが見えた。\n霧が晴れた後、そこには何も残っていなかったーー衣服も、武器も、血痕すらも。",
        "Glancing back at the arena, you saw the defeated Putit's body dissolving into mist-like particles.\nAfter the mist cleared, nothing remained--no clothing, no weapons, not even a trace of blood.",
        "不经意间回头望向角斗场，看到战败的史莱姆的身体正化为雾状粒子。\n雾气消散后，那里什么都没有留下--衣服、武器、甚至血迹都没有。",
        actor=narrator,
    ).say(
        "lily_process",
        "……ああ、気になさらないでください。アスタロト様に『回収』されただけですから。\n敗者の肉体は、アスタロト様の力の糧となるのです。……それがこの闘技場の掟。",
        "...Oh, please don't concern yourself. They've simply been 'collected' by Lord Astaroth.\nThe bodies of the defeated become sustenance for Lord Astaroth's power. ...Such is the law of this arena.",
        "……啊，请不要在意。只是被阿斯塔罗特大人『回收』了而已。\n败者的肉体会成为阿斯塔罗特大人力量的养分。……这就是这个角斗场的法则。",
        actor=lily,
    ).say(
        "narr_process3",
        "リリィの声には、どこか悲しげな響きがあった。",
        "There was a hint of sorrow in Lily's voice.",
        "莉莉的声音中带着一丝悲伤。",
        actor=narrator,
    ).say("ue_narr_v3", "バルガスが近づいてくる。", "Vargus approached.", "巴尔加斯走了过来。", actor=narrator).focus_chara(
        Actors.BALGAS
    ).say(
        "ue_balgas_v1",
        "……笑わせておけ。\n生き残れば、そのヤジもいつかは『金』に変わる。",
        "...Let 'em laugh.\nSurvive, and those heckles'll turn to gold someday.",
        "……让他们笑去吧。\n活下来的话，那些起哄总有一天会变成『金子』。",
        actor=balgas,
    ).say(
        "ue_balgas_v3",
        "だがな、次はもっと酷いもんが降ってくるぜ。連中はすぐに飽きるからな。",
        "But next time, worse stuff's gonna rain down. They get bored fast.",
        "不过啊，下次会有更糟糕的东西砸下来。那帮家伙很快就会腻的。",
        actor=balgas,
    ).focus_chara(Actors.LILY).say(
        "lily_v3",
        "では、報酬の授与です。\n観客からの投げ銭……小さなコイン10枚とプラチナコイン2枚です。",
        "Now then, for your reward.\nTips from the audience... 10 small coins and 2 platinum coins.",
        "那么，颁发奖励。\n观众的打赏……小硬币10枚和铂金币2枚。",
        actor=lily,
    ).jump(reward_end_ue)

    builder.step(reward_end_ue).action(
        "eval",
        param='for(int i=0; i<10; i++) { EClass.pc.Pick(ThingGen.Create("medal")); } for(int i=0; i<2; i++) { EClass.pc.Pick(ThingGen.Create("plat")); }',
    ).complete_quest(QuestIds.UPPER_EXISTENCE).finish()

    # ========================================
    # 上位存在クエスト 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Lobby_Normal"
    ).focus_chara(Actors.LILY).say(
        "ue_lily_d1",
        "……あらあら、落下物に潰されてしまいましたね。\n観客の皆様も、少し期待外れだったようです。また挑戦してくださいね。",
        "...Oh my, you were crushed by falling debris.\nThe audience seems a bit disappointed as well. Please do try again.",
        "……哎呀呀，被落下物压扁了呢。\n观众们似乎也有点失望呢。请再次挑战吧。",
        actor=lily,
    ).finish()
