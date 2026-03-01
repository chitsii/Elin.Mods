# -*- coding: utf-8 -*-
"""
17_vs_astaroth.py - 虚空の王、静寂の断罪：影の介入
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds


def define_vs_astaroth(builder: DramaBuilder):
    """
    最終試練：アスタロトとの初遭遇、ゼクによる救出
    シナリオ: 17_vs_astaroth.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    astaroth = builder.register_actor(Actors.ASTAROTH, "アスタロト", "Astaroth")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_descent")

    # シーン2: 分岐用ラベル
    scene2_normal = builder.label("scene2_normal")  # 両方いる
    scene2_balgas_dead = builder.label("scene2_balgas_dead")  # バルガス死亡
    scene2_common = builder.label("scene2_common")

    # シーン3: 分岐用ラベル
    scene3_normal = builder.label("scene3_normal")  # 両方いる
    scene3_balgas_dead = builder.label("scene3_balgas_dead")  # バルガス死亡

    # シーン4,5: 分岐用ラベル
    scene4_normal = builder.label("scene4_normal")
    scene4_balgas_dead = builder.label("scene4_balgas_dead")

    scene5_normal = builder.label("scene5_normal")
    scene5_balgas_dead = builder.label("scene5_balgas_dead")
    scene5_common = builder.label("scene5_common")

    choice1 = builder.label("choice1")
    react1_proceed = builder.label("react1_proceed")
    react1_unforgivable = builder.label("react1_unforgivable")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 王の降臨（アリーナロビー）
    # ========================================
    # BGMを最初に再生して間を作らない
    builder.step(main).drama_start(
        bg_id="Drama/arena_lobby", bgm_id="BGM/Final_Astaroth_Throne"
    ).say(
        "narr_1",
        "闘技場の空が、墨を流したように真っ黒に染まる。",
        "The sky above the arena turns pitch black, as if ink has been spilled across it.",
        "角斗场上空的天色如同泼墨一般染成漆黑。",
        actor=narrator,
    ).say(
        "narr_2",
        "観客の声は一瞬で消え失せ、代わりにこの世のものとは思えない巨大な『心音』が次元全体を揺さぶり始めた。",
        "The voices of the spectators vanish in an instant, replaced by an otherworldly 'heartbeat' that shakes the very fabric of this dimension.",
        "观众的声音瞬间消失殆尽，取而代之的是一种不似人间之物的巨大「心跳声」，开始震撼整个次元。",
        actor=narrator,
    ).shake().say(
        "narr_3",
        "王座の間に続く大扉が粉々に砕け散り、そこから、背中に巨大な紅蓮の翼を湛えた男ーーアスタロトが、重力を無視してゆっくりと降り立つ。",
        "The great doors leading to the throne room shatter into pieces. From within, a man bearing massive crimson wings upon his back -- Astaroth -- descends slowly, defying gravity itself.",
        "通往王座之间的巨门粉碎四散，从中走出一名背负巨大深红双翼的男子--阿斯塔罗特，无视重力缓缓降临。",
        actor=narrator,
    ).focus_chara(Actors.ASTAROTH).say(
        "narr_4",
        "彼が地を踏んだ瞬間、床の石畳が恐怖に震えるように粉砕された。",
        "The moment his feet touch the ground, the stone floor shatters as if trembling in fear.",
        "他双足落地的刹那，地面的石板如同恐惧颤抖般碎裂开来。",
        actor=narrator,
    ).shake().say(
        "astaroth_1",
        "……ごきげんよう、私の庭を騒がせる『特異点』よ。",
        "...Good evening, 'anomaly' who disturbs my garden.",
        "……安好，扰吾庭园的「特异点」啊。",
        actor=astaroth,
    ).say(
        "astaroth_3",
        "お前にはいつも驚かされている。",
        "You never cease to amuse me with your... surprises.",
        "汝总能给吾带来惊奇。",
        actor=astaroth,
    ).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, scene2_balgas_dead
    ).jump(scene2_normal)

    # シーン2分岐: バルガス死亡
    builder.step(scene2_balgas_dead).focus_chara(Actors.LILY).say(
        "narr_5_bd",
        "アスタロトは、震えるリリィを一瞥し、嘲笑するように鼻を鳴らした。",
        "Astaroth glances at the trembling Lily and snorts with contempt.",
        "阿斯塔罗特瞥了一眼瑟瑟发抖的莉莉，轻蔑地哼了一声。",
        actor=narrator,
    ).focus_chara(Actors.ASTAROTH).say(
        "astaroth_4_bd",
        "リリィ……その人間は、もはやお前の手には負えぬバケモノだ。",
        "Lily... this human has become a monster beyond your control.",
        "莉莉……此人已成为汝无法驾驭的怪物。",
        actor=astaroth,
    ).say(
        "astaroth_6_bd",
        "（アスタロトはあなたに視線を向けた）",
        "(Astaroth turns his gaze upon you.)",
        "（阿斯塔罗特将目光投向你）",
        actor=astaroth,
    ).jump(scene2_common)

    # ========================================
    # シーン2: 静かなる殺意（各パターン）
    # ========================================
    # 通常パターン（両方いる）
    builder.step(scene2_normal).focus_chara(Actors.LILY).say(
        "narr_5",
        "アスタロトは、震えるリリィと、彼女を庇って前に出るバルガスを一瞥し、嘲笑するように鼻を鳴らした。",
        "Astaroth glances at the trembling Lily and Vargus stepping forward to shield her, then snorts with contempt.",
        "阿斯塔罗特瞥了一眼瑟瑟发抖的莉莉和挺身护在她面前的巴尔加斯，轻蔑地哼了一声。",
        actor=narrator,
    ).focus_chara(Actors.ASTAROTH).say(
        "astaroth_4",
        "リリィ……その人間は、もはやお前の手には負えぬバケモノだ。",
        "Lily... this human has become a monster beyond your control.",
        "莉莉……此人已成为汝无法驾驭的怪物。",
        actor=astaroth,
    ).say(
        "astaroth_5",
        "そしてバルガス、敗残兵の分際で、私に逆らうとはな。活かしてやったことを後悔させたいのか？",
        "And Vargus... a defeated soldier dares defy me? Do you wish to make me regret sparing your pathetic life?",
        "还有巴尔加斯，区区一介残兵败将，竟敢忤逆于吾。汝是想让吾后悔当初饶汝一命吗？",
        actor=astaroth,
    ).say(
        "astaroth_6_n",
        "（アスタロトはあなたに視線を向けた）",
        "(Astaroth turns his gaze upon you.)",
        "（阿斯塔罗特将目光投向你）",
        actor=astaroth,
    ).jump(scene2_common)

    # ========================================
    # シーン2続き: アスタロトの宣告（共通）
    # ========================================
    builder.step(scene2_common).say(
        "astaroth_6f",
        "おまえはイルヴァの加護を持っている。システムの想定外。だからこそ、育てる価値があった。……そして今、十分に育った。収穫の時だ。",
        "You bear Ylva's blessing. An anomaly outside the system's design. That is precisely why you were worth cultivating. ...And now, you have ripened sufficiently. The harvest is at hand.",
        "汝身负伊尔瓦之加护，乃系统预料之外的存在。正因如此，汝方有培育之价值。……而今，汝已成熟。收获之时已至。",
        actor=astaroth,
    ).say(
        "astaroth_6g",
        "……私は5万年、この孤独な牢獄で待ち続けた。失った故郷を取り戻すために。",
        "...For fifty thousand years, I have waited in this solitary prison. All to reclaim my lost homeland.",
        "……吾在这孤寂的牢笼中等待了五万年。只为夺回失去的故土。",
        actor=astaroth,
    ).say(
        "astaroth_6h",
        "お前の魂も、その礎となる。……さあ、私に捧げよ。",
        "Your soul shall become another foundation stone. ...Now, offer yourself to me.",
        "汝之魂魄，亦将成为其基石。……来，献于吾罢。",
        actor=astaroth,
    ).say(
        "narr_6",
        "アスタロトが右手を掲げると、その周囲に凝縮された形而上の炎が渦巻く。",
        "As Astaroth raises his right hand, metaphysical flames condense and swirl around it.",
        "阿斯塔罗特举起右手，凝聚的形而上之炎在其周围盘旋。",
        actor=narrator,
    ).say(
        "astaroth_7",
        "さて、お前の物語は終わりだ。その力、引き継がせてもらうぞ。",
        "Very well. Your tale ends here. I shall inherit your power.",
        "罢了，汝的故事到此为止。那份力量，吾便收下了。",
        actor=astaroth,
    ).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, scene3_balgas_dead
    ).jump(scene3_normal)

    # バルガス死亡時
    builder.step(scene3_balgas_dead).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_7_bd",
        "アスタロトの放った一撃が、次元そのものを焼き尽くさんばかりの勢いであなたに迫る。",
        "Astaroth's strike rushes toward you with such force it threatens to incinerate the dimension itself.",
        "阿斯塔罗特释放的一击以足以焚尽次元的气势向你袭来。",
        actor=narrator,
    ).shake().say(
        "narr_9_bd",
        "リリィが叫ぶ。その直前ーー。",
        "Lily screams. But just before the flames reach you--",
        "莉莉发出尖叫。就在那之前--",
        actor=narrator,
    ).say(
        "narr_10_bd",
        "空間がガラスのようにパリンと音を立てて割れ、あなたの目の前に、ゼクがひょっこりと現れた。",
        "Space shatters like glass with a crystalline sound, and Zek pops into existence right before your eyes.",
        "空间如玻璃般发出清脆的碎裂声，泽克突然出现在你眼前。",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "narr_11_bd",
        "その手には、不気味に明滅し、バグったように色彩が反転し続ける立方体が握られている。",
        "In his hand, he holds a cube that flickers ominously, its colors inverting continuously as if glitched.",
        "他手中握着一个诡异闪烁、色彩如同故障般不断反转的立方体。",
        actor=narrator,
    ).say(
        "zek_1_bd",
        "おっと……！ 王様、あまり急いではいけません。",
        "Whoops...! Your Majesty, you mustn't be so hasty.",
        "哎呀……！王上，您可不能太心急呀。",
        actor=zek,
    ).say(
        "zek_2_bd",
        "最高級の商材が灰になっては、商売あがったりですからね！",
        "It would be terrible for business if my finest merchandise turned to ash!",
        "要是上等的货物化为灰烬，在下可就没生意做了呢！",
        actor=zek,
    ).say(
        "narr_12_bd",
        "ゼクがキューブを起動させると、アスタロトの炎があなたを通り抜け、まるで『そこには誰もいない』かのように虚空へ消えていく。",
        "As Zek activates the cube, Astaroth's flames pass right through you and dissipate into the void, as if 'no one was ever there.'",
        "泽克启动立方体后，阿斯塔罗特的火焰穿透了你，如同「那里空无一人」般消散于虚空之中。",
        actor=narrator,
    ).jump(scene4_balgas_dead)

    # ========================================
    # シーン3: 断罪の炎と、影の道具（各パターン）
    # ========================================
    # 通常パターン
    builder.step(scene3_normal).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_7_n",
        "アスタロトの放った一撃が、次元そのものを焼き尽くさんばかりの勢いであなたに迫る。",
        "Astaroth's strike rushes toward you with such force it threatens to incinerate the dimension itself.",
        "阿斯塔罗特释放的一击以足以焚尽次元的气势向你袭来。",
        actor=narrator,
    ).shake().say(
        "narr_9",
        "リリィが叫び、バルガスが叫ぶ。その直前ーー。",
        "Lily screams. Vargus screams. But just before the flames reach you--",
        "莉莉发出尖叫，巴尔加斯也在呼喊。就在那之前--",
        actor=narrator,
    ).say(
        "narr_10_n",
        "空間がガラスのようにパリンと音を立てて割れ、あなたの目の前に、ゼクがひょっこりと現れた。",
        "Space shatters like glass with a crystalline sound, and Zek pops into existence right before your eyes.",
        "空间如玻璃般发出清脆的碎裂声，泽克突然出现在你眼前。",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "narr_11_n",
        "その手には、不気味に明滅し、バグったように色彩が反転し続ける立方体が握られている。",
        "In his hand, he holds a cube that flickers ominously, its colors inverting continuously as if glitched.",
        "他手中握着一个诡异闪烁、色彩如同故障般不断反转的立方体。",
        actor=narrator,
    ).say(
        "zek_1_n",
        "おっと……！ 王様、あまり急いではいけません。",
        "Whoops...! Your Majesty, you mustn't be so hasty.",
        "哎呀……！王上，您可不能太心急呀。",
        actor=zek,
    ).say(
        "zek_2_n",
        "最高級の商材が灰になっては、商売あがったりですからね！",
        "It would be terrible for business if my finest merchandise turned to ash!",
        "要是上等的货物化为灰烬，在下可就没生意做了呢！",
        actor=zek,
    ).say(
        "narr_12_n",
        "ゼクがキューブを起動させると、アスタロトの炎があなたを通り抜け、まるで『そこには誰もいない』かのように虚空へ消えていく。",
        "As Zek activates the cube, Astaroth's flames pass right through you and dissipate into the void, as if 'no one was ever there.'",
        "泽克启动立方体后，阿斯塔罗特的火焰穿透了你，如同「那里空无一人」般消散于虚空之中。",
        actor=narrator,
    ).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, scene4_balgas_dead
    ).jump(scene4_normal)

    # バルガス死亡時
    builder.step(scene4_balgas_dead).focus_chara(Actors.ASTAROTH).say(
        "narr_13_bd",
        "ゼクは冷や汗を流しながらも、不敵な笑みを浮かべてアスタロトを見上げる。",
        "Despite the cold sweat running down his face, Zek looks up at Astaroth with a defiant smirk.",
        "泽克虽然冷汗直流，却仍带着不羁的笑容仰望着阿斯塔罗特。",
        actor=narrator,
    ).say(
        "narr_14_bd",
        "キューブの周りでは、数式やノイズが乱舞し、あなたたちの存在を一時的に『世界の演算』から切り離していた。",
        "Around the cube, equations and noise dance wildly, temporarily disconnecting your existence from the 'world's calculations.'",
        "立方体周围，数式与噪点狂乱飞舞，暂时将你们的存在从「世界的运算」中分离出来。",
        actor=narrator,
    ).say(
        "astaroth_8_bd",
        "……ゼクか。ゴミ拾いの分際で、この私の『確定した死』を歪めたか。",
        "...Zek. A mere scavenger dares to distort my 'ordained death'?",
        "……泽克吗。区区一介拾荒者，竟敢扭曲吾所裁定的「必然之死」。",
        actor=astaroth,
    ).focus_chara(Actors.ZEK).say(
        "zek_3_bd",
        "クク……私のコレクションは大したものでしょう？",
        "Heh heh... Quite impressive, my collection, wouldn't you say?",
        "呵呵呵……在下的收藏还算不错吧？",
        actor=zek,
    ).say(
        "zek_4_bd",
        "さあ、闘士殿！ リリィを連れて、私の影に飛び込みなさい！",
        "Now, gladiator! Take Lily and dive into my shadow!",
        "来吧，角斗士阁下！带上莉莉，跳进在下的影子里！",
        actor=zek,
    ).say(
        "zek_5_bd",
        "この道具が保つのはあと数秒だ。アスタロトを倒すための『本当の力』、それを手に入れる猶予を……私が作ってあげましょう！",
        "This device will only hold for a few more seconds. The time you need to obtain the 'true power' to defeat Astaroth... I shall provide it!",
        "这道具只能再撑几秒了。打倒阿斯塔罗特所需的「真正力量」，获取它的时间……就由在下来为您争取吧！",
        actor=zek,
    ).say(
        "narr_15_bd",
        "あなたはリリィを連れて、ゼクの影の中へ飛び込む。",
        "You grab Lily and leap into Zek's shadow.",
        "你带着莉莉，跃入了泽克的影子之中。",
        actor=narrator,
    ).say(
        "narr_16_bd",
        "背後からアスタロトの怒号に近い咆哮が聞こえるが、視界は瞬時に暗転した。",
        "You hear Astaroth's enraged roar behind you, but your vision instantly goes dark.",
        "身后传来阿斯塔罗特近乎咆哮的怒吼，但视野瞬间陷入黑暗。",
        actor=narrator,
    ).jump(scene5_balgas_dead)

    # ========================================
    # シーン4: 影の救済と撤退（各パターン）
    # ========================================
    # 通常パターン
    builder.step(scene4_normal).focus_chara(Actors.ASTAROTH).say(
        "narr_13_n",
        "ゼクは冷や汗を流しながらも、不敵な笑みを浮かべてアスタロトを見上げる。",
        "Despite the cold sweat running down his face, Zek looks up at Astaroth with a defiant smirk.",
        "泽克虽然冷汗直流，却仍带着不羁的笑容仰望着阿斯塔罗特。",
        actor=narrator,
    ).say(
        "narr_14_n",
        "キューブの周りでは、数式やノイズが乱舞し、あなたたちの存在を一時的に『世界の演算』から切り離していた。",
        "Around the cube, equations and noise dance wildly, temporarily disconnecting your existence from the 'world's calculations.'",
        "立方体周围，数式与噪点狂乱飞舞，暂时将你们的存在从「世界的运算」中分离出来。",
        actor=narrator,
    ).say(
        "astaroth_8_n",
        "……ゼクか。ゴミ拾いの分際で、この私の『確定した死』を歪めたか。",
        "...Zek. A mere scavenger dares to distort my 'ordained death'?",
        "……泽克吗。区区一介拾荒者，竟敢扭曲吾所裁定的「必然之死」。",
        actor=astaroth,
    ).focus_chara(Actors.ZEK).say(
        "zek_3_n",
        "クク……私のコレクションは大したものでしょう？",
        "Heh heh... Quite impressive, my collection, wouldn't you say?",
        "呵呵呵……在下的收藏还算不错吧？",
        actor=zek,
    ).say(
        "zek_4",
        "さあ、闘士殿！ リリィもバルガスも連れて、私の影に飛び込みなさい！",
        "Now, gladiator! Take Lily and Vargus and dive into my shadow!",
        "来吧，角斗士阁下！带上莉莉和巴尔加斯，跳进在下的影子里！",
        actor=zek,
    ).say(
        "zek_5_n",
        "この道具が保つのはあと数秒だ。アスタロトを倒すための『本当の力』、それを手に入れる猶予を……私が作ってあげましょう！",
        "This device will only hold for a few more seconds. The time you need to obtain the 'true power' to defeat Astaroth... I shall provide it!",
        "这道具只能再撑几秒了。打倒阿斯塔罗特所需的「真正力量」，获取它的时间……就由在下来为您争取吧！",
        actor=zek,
    ).say(
        "narr_15",
        "あなたはバルガスとリリィを連れて、ゼクの影の中へ飛び込む。",
        "You grab Vargus and Lily and leap into Zek's shadow.",
        "你带着巴尔加斯和莉莉，跃入了泽克的影子之中。",
        actor=narrator,
    ).say(
        "narr_16_n",
        "背後からアスタロトの怒号に近い咆哮が聞こえるが、視界は瞬時に暗転した。",
        "You hear Astaroth's enraged roar behind you, but your vision instantly goes dark.",
        "身后传来阿斯塔罗特近乎咆哮的怒吼，但视野瞬间陷入黑暗。",
        actor=narrator,
    ).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, scene5_balgas_dead
    ).jump(scene5_normal)

    # バルガス死亡時
    builder.step(scene5_balgas_dead).drama_start(bg_id="Drama/zek_hideout", bgm_id="BGM/Mystical_Ritual").say(
        "narr_17_bd",
        "辿り着いたのは、アリーナのロビーでも私室でもない、無数のガラクタと遺品が積み上がった不気味な異空間ーーゼクの本拠地だった。",
        "You arrive not at the arena lobby nor in private quarters, but in an eerie otherworldly space piled high with countless relics and remnants -- Zek's hideout.",
        "你抵达的地方既不是角斗场大厅，也不是私人房间，而是一个堆满无数废品与遗物的诡异异空间--泽克的据点。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "narr_18_bd",
        "リリィが膝をついて震えている。",
        "Lily has fallen to her knees, trembling.",
        "莉莉跪倒在地，瑟瑟发抖。",
        actor=narrator,
    ).jump(scene5_common)

    # ========================================
    # シーン5: 次元のゴミ捨て場（各パターン）
    # ========================================
    # 通常パターン
    builder.step(scene5_normal).drama_start(
        bg_id="Drama/zek_hideout", bgm_id="BGM/Mystical_Ritual"
    ).say(
        "narr_17_n",
        "辿り着いたのは、アリーナのロビーでも私室でもない、無数のガラクタと遺品が積み上がった不気味な異空間ーーゼクの本拠地だった。",
        "You arrive not at the arena lobby nor in private quarters, but in an eerie otherworldly space piled high with countless relics and remnants -- Zek's hideout.",
        "你抵达的地方既不是角斗场大厅，也不是私人房间，而是一个堆满无数废品与遗物的诡异异空间--泽克的据点。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "narr_18",
        "バルガスが激しく咳き込み、リリィが膝をついて震えている。",
        "Vargus coughs violently while Lily has fallen to her knees, trembling.",
        "巴尔加斯剧烈咳嗽着，莉莉跪倒在地瑟瑟发抖。",
        actor=narrator,
    ).jump(scene5_common)

    # ========================================
    # シーン5: 共通部分（ゼクのコレクション）
    # ========================================
    builder.step(scene5_common).say(
        "narr_19",
        "薄暗い空間の奥に、無数の『展示台』が並んでいる。そこに置かれているのは、ガラスケースに封じられた『瞬間』の数々。",
        "In the depths of this dim space, countless 'display pedestals' stand in rows. Upon them rest 'moments' sealed within glass cases.",
        "在这昏暗空间的深处，排列着无数的「展示台」。上面陈列的是被封存在玻璃柜中的一个个「瞬间」。",
        actor=narrator,
    ).say(
        "narr_20",
        "ある戦士は、剣を掲げたまま絶望の表情で凍りついている。その顔には、『なぜ俺が……』という最期の疑問が刻まれたまま。",
        "One warrior stands frozen with sword raised, despair etched on his face. His expression forever captures his final thought: 'Why me...?'",
        "一名战士高举着剑，面带绝望的表情定格在那里。他的脸上永远刻着最后的疑问：「为什么是我……」",
        actor=narrator,
    ).say(
        "narr_21",
        "ある魔術師は、魔導書を抱きしめ、涙を流したまま時が止まっている。その指先には、唱えきれなかった最後の呪文の残滓が漂う。",
        "A mage clutches her grimoire, frozen in time with tears streaming down her face. At her fingertips linger the remnants of a final spell she never completed.",
        "一名魔法师紧抱着魔导书，泪流满面地被定格在时间中。她指尖还残留着未能念完的最后一道咒文的余韵。",
        actor=narrator,
    ).say(
        "narr_22",
        "ある盗賊は、仲間の死体にすがりつき、狂ったように笑い続けている。その笑顔は、正気を失った瞬間を永遠に閉じ込められた地獄。",
        "A thief clings to a comrade's corpse, laughing maniacally. That smile is a hell that eternally imprisons the moment sanity was lost.",
        "一名盗贼紧抱着同伴的尸体，疯狂地笑个不停。那笑容是将失去理智瞬间永恒封印的地狱。",
        actor=narrator,
    ).say(
        "narr_23",
        "ガラスケースの一つに、見覚えのある鎧を纏った姿が展示されている。",
        "Within one glass case, a figure clad in familiar armor is on display.",
        "其中一个玻璃柜里，陈列着一个身穿熟悉铠甲的身影。",
        actor=narrator,
    ).say(
        "narr_23b",
        "ーーそれは、あなたが倒した『錆びついた英雄・カイン』……いや、その『最期の瞬間』を切り取ったものだ。",
        "--It is the 'Rusted Hero, Cain' you once defeated... or rather, a captured fragment of his 'final moment.'",
        "--那是你曾击败的「锈蚀的英雄·凯恩」……不，是截取他「最后时刻」的片段。",
        actor=narrator,
    ).say(
        "narr_23c",
        "あなたが戦ったのはアスタロトが作り出したレイスだったが、このガラスケースの中には、35年前に失われた本物のカインの『記録』が眠っている。",
        "What you fought was a wraith created by Astaroth, but within this glass case sleeps the 'record' of the real Cain, lost 35 years ago.",
        "你所对战的是阿斯塔罗特创造的怨灵，而这玻璃柜中沉睡着的，是35年前失落的真正凯恩的「记录」。",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "zek_coll1",
        "……これは『保存』です。",
        "...This is 'preservation.'",
        "……这是「保存」。",
        actor=zek,
    ).say(
        "zek_coll2",
        "アスタロトは敗者の肉体を回収し、力の糧とする。残されるのは、忘却のみ。",
        "Astaroth collects the bodies of the defeated and uses them as fuel for his power. All that remains is oblivion.",
        "阿斯塔罗特会回收败者的肉体，作为力量的养分。留下的只有遗忘。",
        actor=zek,
    ).say(
        "zek_coll3",
        "私はその『最期の瞬間』を切り取り、永遠に留めているだけです。……彼らが生きた証を、誰かが覚えていなければ。",
        "I merely cut out their 'final moments' and preserve them forever. ...Someone must remember that they lived.",
        "在下只是将他们「最后的瞬间」截取下来，永远保存罢了。……总得有人记住他们曾经活过。",
        actor=zek,
    ).say(
        "narr_coll",
        "ゼクの声には、かすかな寂しさが滲んでいた。",
        "A faint loneliness tinges Zek's voice.",
        "泽克的声音中透出一丝淡淡的寂寥。",
        actor=narrator,
    ).say(
        "zek_6",
        "……ふぅ。命拾いしましたね。",
        "...Phew. A narrow escape, wouldn't you say?",
        "……呼。捡回一条命了呢。",
        actor=zek,
    ).say(
        "zek_6b",
        "ようこそ、私の『書庫』へ。消えゆく者たちの記録が、ここに眠っています。",
        "Welcome to my 'Archive.' Here sleep the records of those who have faded away.",
        "欢迎来到在下的「藏书阁」。消逝之人的记录，就沉睡在这里。",
        actor=zek,
    ).say(
        "zek_7",
        "今のあなたは、アスタロトにとっては『目障りな不具合』に過ぎない。彼を殺すには……システムの外側、イルヴァの地で、力をつけてもらうしかありません。",
        "As you are now, you're merely an 'irritating glitch' to Astaroth. To slay him... you must grow stronger in the lands of Ylva, outside the system.",
        "现在的您，对阿斯塔罗特来说不过是「碍眼的漏洞」而已。要杀死他……只能在系统之外的伊尔瓦之地积蓄力量。",
        actor=zek,
    ).say(
        "zek_9",
        "……それらを手に入れる覚悟があるなら、このゴミの山をさらに奥へ進んでいただきましょうか。",
        "...If you have the resolve to obtain such power, shall we proceed deeper into this mountain of refuse?",
        "……若您有觉悟获取那份力量，不妨再深入这废品之山一探究竟？",
        actor=zek,
    ).jump(choice1)

    # プレイヤーの選択肢
    builder.choice(
        react1_proceed,
        "……分かった。進もう",
        "...Understood. Let's proceed.",
        "……明白了。继续前进吧",
        text_id="c1_proceed",
    ).choice(
        react1_unforgivable,
        "お前のコレクション……許せない",
        "Your collection... I can't forgive this.",
        "你的收藏……我无法原谅",
        text_id="c1_unforgivable",
    )

    # 選択肢反応
    builder.step(react1_proceed).say(
        "zek_r1",
        "ふふ、素直ですこと。では、こちらへどうぞ。",
        "Heh heh, how obedient. This way, if you please.",
        "呵呵，真听话呢。那么，请这边走。",
        actor=zek,
    ).jump(ending)

    builder.step(react1_unforgivable).say(
        "zek_r2",
        "許さなくて結構。ですが、今はアスタロトを倒すことが先決でしょう？",
        "You needn't forgive me. But surely defeating Astaroth takes priority for now, does it not?",
        "不原谅也无妨。不过，现在打倒阿斯塔罗特才是当务之急，不是吗？",
        actor=zek,
    ).jump(ending)

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending).focus_chara(Actors.ZEK).say(
        "zek_final_1",
        "さあ、闘士殿。イルヴァの地で力を蓄えてきなさい。",
        "Now then, gladiator. Go forth and gather your strength in the lands of Ylva.",
        "那么，角斗士阁下。去伊尔瓦之地积蓄力量吧。",
        actor=zek,
    ).say(
        "zek_final_2",
        "……準備が整い、『王殺し』の覚悟が決まったら、私に声をかけてください。",
        "...When you are prepared and your resolve to slay the king is set, come find me.",
        "……待您准备就绪，下定「弑王」的决心后，请来找在下。",
        actor=zek,
    ).say(
        "zek_final_3",
        "あの忌々しい暴君との最終決戦へ……私がご案内いたしましょう。",
        "To the final battle against that accursed tyrant... I shall guide you there.",
        "前往与那可恶暴君的最终决战……在下将为您引路。",
        actor=zek,
    ).say(
        "narr_final",
        "ゼクは不敵な笑みを浮かべながら、影の中へ溶けていった。",
        "With a fearless smirk, Zek melts away into the shadows.",
        "泽克带着不羁的笑容，融入了阴影之中。",
        actor=narrator,
    ).set_flag(Keys.FUGITIVE, FlagValues.TRUE).complete_quest(
        QuestIds.VS_ASTAROTH
    ).finish()
