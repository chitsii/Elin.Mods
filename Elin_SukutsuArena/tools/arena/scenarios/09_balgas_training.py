# -*- coding: utf-8 -*-
"""
09_balgas_training.md - 戦士の哲学：鉄を打つ鉄
バルガスの特別訓練とRank C昇格への道
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, Keys, QuestBattleFlags, QuestIds, SessionKeys


def define_balgas_training(builder: DramaBuilder):
    """
    バルガスの訓練
    シナリオ: 09_balgas_training.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_question")
    choice1 = builder.label("choice1")
    react1_philosophy = builder.label("react1_philosophy")
    react1_survive = builder.label("react1_survive")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_invitation")
    choice2 = builder.label("choice2")
    react2_accept = builder.label("react2_accept")
    react2_death = builder.label("react2_death")
    react2_nod = builder.label("react2_nod")
    scene3 = builder.label("scene3_combat")

    # ========================================
    # 導入: ランクC挑戦前の訓練
    # ========================================
    builder.step(main).play_bgm("BGM/Lobby_Normal").say(
        "narr_0",
        "バルガスに昇格試合を申し出ようとしたあなたを、彼が呼び止めた。",
        "As you were about to request a rank-up match from Vargus, he stopped you.",
        "正当你准备向巴尔加斯申请晋级比赛时，他叫住了你。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_0a",
        "……おい、ちょっと待て。\nランクCに挑む前に、お前に言っておきたいことがある。",
        "...Oi, hold up a sec.\nBefore you challenge Rank C, there's somethin' I gotta tell ya.",
        "……喂，等等。\n在你小子挑战C级之前，老子有些话要跟你说。",
        actor=balgas,
    ).say(
        "narr_1",
        "ロビーの隅、いつもの酒瓶を横に置き、バルガスが自慢の大剣を丁寧に研いでいる。\n研石が剣を研ぐ規則的な音が、静まり返ったロビーに響く。",
        "In a corner of the lobby, his usual bottle of booze beside him, Vargus carefully sharpens his prized greatsword.\nThe rhythmic sound of whetstone against steel echoes through the silent lobby.",
        "在大厅的角落，巴尔加斯把他惯常的酒瓶放在一旁，仔细地磨着他引以为傲的大剑。\n磨刀石研磨剑刃的规律声响回荡在寂静的大厅中。",
        actor=narrator,
    ).say(
        "narr_3",
        "あなたが近づくと、彼は研石を止め、濁った、しかし鋭い眼光を向けた。",
        "As you approach, he stops sharpening and fixes you with clouded yet piercing eyes.",
        "当你走近时，他停下了手中的磨刀石，用那浑浊却锐利的目光注视着你。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.BALGAS).say(
        "balgas_1",
        "……おい、銅貨稼ぎ。最近の戦いぶり、悪かねえ。\nだがな、今のままじゃランクCの壁……あそこで待ってる『歴戦の猛者』どもに、鼻歌交じりで解体されるのがオチだ。",
        "...Oi, copper-scrounger. Your recent fights ain't been half bad.\nBut the way you are now, the Rank C wall... them 'battle-hardened veterans' waitin' there will carve you up while whistlin' a tune.",
        "……喂，挣铜币的臭小子。你最近打得还行。\n但你小子现在这样的话，C级那道坎……那里等着的那些『身经百战的猛将』，会边哼着小曲边把你大卸八块。",
        actor=balgas,
    ).say("narr_4", "彼は研石を置き、腕を組む。", "He sets down the whetstone and crosses his arms.", "他放下磨刀石，双臂交叉抱在胸前。", actor=narrator).say(
        "balgas_3",
        "お前には『技術』がある。だが、『哲学』がねえ。\n……剣を振る時、お前は何を考えてる？ 敵のHPか？ 次に飲むポーションの種類か？",
        "You got 'skill.' But you ain't got 'philosophy.'\n...When you swing your blade, what're you thinkin' about? The enemy's HP? Which potion to chug next?",
        "你小子有『技术』。但没有『哲学』。\n……你挥剑的时候在想啥？敌人的血量？下一瓶该喝啥药水？",
        actor=balgas,
    ).say(
        "balgas_5",
        "そんなもんじゃねえ。戦士が最後に頼るのは、己の魂に刻んだ一文字の『理（ことわり）』だ。",
        "That ain't it. What a warrior relies on in the end is a single 'truth' carved into his soul.",
        "不是那些玩意儿。战士最后依靠的，是刻在自己灵魂里的那一个字的『道理』。",
        actor=balgas,
    )

    # プレイヤーの選択肢1
    builder.choice(react1_philosophy, "哲学……？", "Philosophy...?", "哲学……？", text_id="c1_philosophy").choice(
        react1_survive, "俺の哲学は、生き残ることだ", "My philosophy is to survive.", "我的哲学就是活下去。", text_id="c1_survive"
    ).choice(react1_silent, "（無言で聞く）", "(Listen in silence)", "（沉默地听着）", text_id="c1_silent")

    # 選択肢反応1
    builder.step(react1_philosophy).say(
        "balgas_r1",
        "ああ、哲学だ。お前が何のために剣を振るのか、その答えだ。",
        "Aye, philosophy. The answer to why you swing your blade.",
        "啊，哲学。就是你小子为啥要挥剑，那个答案。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_survive).say(
        "balgas_r2",
        "……ハッ、悪くねえ。だが、それだけじゃまだ足りねえ。",
        "...Hah! Not bad. But that alone ain't enough.",
        "……哈！还行。但光这样还不够。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "balgas_r3", "……まあ、黙って聞いててくれ。続きがある。", "...Well, just keep quiet and listen. There's more.", "……行，你就闭嘴听着。还有下文。", actor=balgas
    ).jump(scene2)

    # ========================================
    # シーン2: 特設訓練場への誘い
    # ========================================
    builder.step(scene2).play_bgm("BGM/Ominous_Suspense_01").say(
        "narr_5",
        "バルガスは重い腰を上げ、闘技場の中心を指差した。",
        "Vargus heaves himself up and points toward the center of the arena.",
        "巴尔加斯缓缓起身，指向角斗场的中央。",
        actor=narrator,
    ).say(
        "balgas_6",
        "来い。今日はリリィの事務仕事じゃねえ。俺が直接、そのなまくらな魂を叩き直してやる。\n……死んでも文句は言うなよ。地獄に落ちてから、俺の愚痴を肴に飲め。",
        "Come. Today ain't about Lily's paperwork. I'm gonna knock that dull soul of yours back into shape myself.\n...Don't complain if you die. When you're in hell, you can drink while listenin' to my gripes.",
        "过来。今天不是莉莉的文书工作。老子亲自来把你那钝了的灵魂给敲打回来。\n……死了也别抱怨。等你下了地狱，就拿老子的牢骚当下酒菜吧。",
        actor=balgas,
    ).say(
        "narr_6", "リリィが受付から顔を上げ、クスクスと笑う。", "Lily looks up from the reception desk and giggles softly.", "莉莉从接待台抬起头，轻声笑了起来。", actor=narrator
    ).focus_chara(Actors.LILY).say(
        "lily_1", "……ふふ、バルガスさんがここまでやるなんて珍しい。", "...Hehe, how rare for Vargus to go this far.", "……呵呵，巴尔加斯先生做到这种程度真是少见呢。", actor=lily
    ).say(
        "lily_2",
        "お客様、これは特別な『授業料』が必要かもしれませんね？\nあ、ご心配なく。お代は、あなたがそこで流す『美しい血の雫』で十分ですから。",
        "Dear customer, perhaps this requires a special 'tuition fee'?\nOh, do not worry. The 'beautiful drops of blood' you shed there will be payment enough.",
        "尊贵的客人，这可能需要一笔特别的『学费』呢？\n啊，请您不必担心。您在那里流下的『美丽血珠』就足以支付了。",
        actor=lily,
    )

    # プレイヤーの選択肢2
    builder.choice(react2_accept, "……分かった。行こう", "...Understood. Let's go.", "……明白了。走吧。", text_id="c2_accept").choice(
        react2_death, "本当に死なないよな？", "I won't really die, right?", "真的不会死吧？", text_id="c2_death"
    ).choice(react2_nod, "（無言で頷く）", "(Nod silently)", "（无言地点头）", text_id="c2_nod")

    # 選択肢反応2
    builder.step(react2_accept).focus_chara(Actors.BALGAS).say(
        "balgas_r4", "ハッ、良い返事だ。ついてこい。", "Hah! Good answer. Follow me.", "哈！回答得好。跟老子来。", actor=balgas
    ).jump(scene3)

    builder.step(react2_death).focus_chara(Actors.BALGAS).say(
        "balgas_r5", "……知らねえ。だが、死ぬ気で来い。", "...Don't know. But come at me like you're ready to die.", "……老子不知道。不过，你小子就当会死的来。", actor=balgas
    ).jump(scene3)

    builder.step(react2_nod).focus_chara(Actors.BALGAS).say(
        "balgas_r6", "……よし。じゃあ行くぞ。", "...Good. Let's go then.", "……行。那就走了。", actor=balgas
    ).jump(scene3)

    # ========================================
    # シーン3: 実戦講義『魂の重量』
    # ========================================
    builder.step(scene3).play_bgm("BGM/Battle_Kain_Requiem").say(
        "narr_7",
        "地下の練習場は、虚空から漏れる微かな光に照らされている。\nバルガスは武器を持たず、無造作に構えた。",
        "The underground training ground is illuminated by faint light leaking from the void.\nVargus takes a casual stance, unarmed.",
        "地下的训练场被从虚空中泄漏的微弱光芒照亮。\n巴尔加斯空着手，随意地摆出架势。",
        actor=narrator,
    ).say(
        "narr_9",
        "その背後からは、歴戦の猛者だけが放つ、物理的な「圧」が空間を歪ませている。",
        "Behind him, a physical 'pressure' that only battle-hardened veterans can emit warps the very space around him.",
        "从他身后散发出的，是只有身经百战的猛将才能释放的物理性『威压』，扭曲着周围的空间。",
        actor=narrator,
    ).shake().focus_chara(Actors.BALGAS).say(
        "balgas_8",
        "さあ、構えろ。ルールは一つ……俺を降伏させてみせろ。\n魔法でも、薬でも、卑怯な手でも何でも使え。",
        "Now, take your stance. One rule... make me surrender.\nMagic, potions, dirty tricks--use whatever you got.",
        "来吧，摆好架势。规则只有一条……让老子认输。\n魔法也好，药也好，卑鄙手段也好，啥都行。",
        actor=balgas,
    ).say(
        "balgas_10",
        "戦士の哲学とは、『手段』を尽くした先にある『目的』の純粋さだ！",
        "A warrior's philosophy is the purity of 'purpose' that lies beyond exhausting all 'means'!",
        "战士的哲学，就是用尽『手段』之后，『目的』的纯粹！",
        actor=balgas,
    ).say(
        "narr_10", "バルガスとの特別な手合わせが始まる……！", "The special sparring match with Vargus begins...!", "与巴尔加斯的特别切磋开始了……！", actor=narrator
    ).set_flag(QuestBattleFlags.RESULT_FLAG, 1).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.BALGAS_TRAINING
    ).start_battle_by_stage(
        "balgas_training_battle", master_id="sukutsu_arena_master"
    ).finish()


def add_balgas_training_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    バルガス訓練クエストの勝利/敗北ステップを arena_master ビルダーに追加する
    訓練なので勝敗どちらでも同じ結果（シーン4相当）に進む

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

    # 共通の結果処理ラベル
    training_result = builder.label("balgas_training_result")
    reward_stone_bt = builder.label("reward_stone_bt")
    reward_steel_bt = builder.label("reward_steel_bt")
    reward_bone_bt = builder.label("reward_bone_bt")
    reward_end_bt = builder.label("reward_end_bt")
    final_thanks_bt = builder.label("final_thanks_bt")
    final_again_bt = builder.label("final_again_bt")
    final_nod_bt = builder.label("final_nod_bt")
    ending_bt = builder.label("ending_bt")

    # ========================================
    # 勝利 → 共通結果へ
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).jump(
        training_result
    )

    # ========================================
    # 敗北 → 共通結果へ（訓練なので同じ扱い）
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).jump(training_result)

    # ========================================
    # 共通結果: シーン4相当（哲学の伝承）
    # ========================================
    builder.step(training_result).play_bgm("BGM/Emotional_Sacred_Triumph_Special").say(
        "narr_bt1",
        "息を切らし、膝をつくあなた。\nバルガスは一歩も動いていないが、その口元には満足げな笑みが浮かんでいた。",
        "You drop to your knees, gasping for breath.\nVargus hasn't moved a single step, but a satisfied smile plays at the corner of his mouth.",
        "你气喘吁吁，跪倒在地。\n巴尔加斯一步都没动，但他嘴角浮现出满意的笑容。",
        actor=narrator,
    ).say(
        "narr_bt3", "彼はあなたの肩を、岩のような拳で一つ叩いた。", "He gives your shoulder a single thump with his rock-like fist.", "他用岩石般的拳头拍了拍你的肩膀。", actor=narrator
    ).shake().focus_chara(Actors.BALGAS).say(
        "balgas_bt1",
        "……ハッ！ 少しは『意志』が剣に乗るようになったじゃねえか。",
        "...Hah! You're finally puttin' some 'will' behind that blade.",
        "……哈！总算能把点『意志』灌到剑上了嘛。",
        actor=balgas,
    ).say(
        "balgas_bt2",
        "いいか、戦いってのはな、ただの殺し合いじゃねえ。\n自分の命を、何のために『消費』するかを決める聖域だ。",
        "Listen up. A fight ain't just killin' each other.\nIt's a sacred place where you decide what you're gonna 'spend' your life for.",
        "听好了，战斗这玩意儿，不是单纯的互相杀戮。\n是决定你要为了啥『消耗』自己生命的圣域。",
        actor=balgas,
    ).say(
        "balgas_bt4",
        "その哲学を忘れるな。そうすれば、どんな異次元の闘もお前を呑み込むことはできねえ。",
        "Don't forget that philosophy. Do that, and no darkness from any dimension can swallow you whole.",
        "别忘了这个哲学。只要记住这个，不管啥异次元的黑暗都吞不了你。",
        actor=balgas,
    ).say(
        "narr_bt4",
        "ロビーに戻ると、リリィが台帳を開いて待っている。",
        "When you return to the lobby, Lily is waiting with the ledger open.",
        "回到大厅后，莉莉打开账簿等待着。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_bt1",
        "……素晴らしい。バルガスさんの説教を聞いて生き残るなんて、あなたは本当の意味で『朱砂食い』になる資格を得たようです。\nカラスは死肉を喰らい、戦場を飛び回る。……今のあなたに、相応しい二つ名ですね。",
        "...Splendid. To survive Vargus's sermon--you have truly earned the right to become a 'Cinnabar Eater.'\nCrows feast on carrion and soar across battlefields. ...A fitting title for you now, wouldn't you say?",
        "……太棒了。能听完巴尔加斯先生的说教还活着回来，您真正获得了成为『食朱砂者』的资格呢。\n乌鸦食腐肉，在战场上翱翔。……对现在的您来说，是很合适的称号呢。",
        actor=lily,
    ).say("narr_bt5", "彼女は台帳に何かを書き込む。", "She writes something in the ledger.", "她在账簿上写下了什么。", actor=narrator).say(
        "lily_bt3", "報酬として、プラチナコイン5枚を渡しましょう。", "As your reward, I shall present you with five platinum coins.", "作为报酬，我将赠予您五枚白金币。", actor=lily
    ).action(
        "eval",
        param='for(int i=0; i<5; i++) { EClass.pc.Pick(ThingGen.Create("plat")); }',
    ).say(
        "narr_bt6",
        "バルガスが酒瓶を傾けながら、あなたに背を向けたまま言う。",
        "Vargus tilts his bottle while speaking with his back still turned to you.",
        "巴尔加斯倾斜着酒瓶，背对着你说道。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "narr_bt7", "彼は深く息を吐き、酒を飲み干す。", "He exhales deeply and drains his drink.", "他深深地呼出一口气，饮尽杯中酒。", actor=narrator
    ).say("balgas_bt5", "……お前は本物の『鋼の心』を持った戦士だ。", "...You're a warrior with a real 'heart of steel.'", "……你小子是个有真正『钢铁之心』的战士。", actor=balgas)

    # 最終選択肢
    builder.choice(
        final_thanks_bt, "……ありがとう", "...Thank you.", "……谢谢。", text_id="c_final_thanks_bt"
    ).choice(
        final_again_bt, "次も教えてくれるか？", "Will you teach me again?", "下次还能教我吗？", text_id="c_final_again_bt"
    ).choice(final_nod_bt, "（無言で頷く）", "(Nod silently)", "（无言地点头）", text_id="c_final_nod_bt")

    builder.step(final_thanks_bt).say(
        "balgas_r7_bt",
        "ハッ、礼はいらねえ。生き残って、俺を超えてみせろ。",
        "Hah! Don't need your thanks. Survive and surpass me.",
        "哈！不用谢。活下去，超越老子给我看。",
        actor=balgas,
    ).jump(ending_bt)

    builder.step(final_again_bt).say(
        "balgas_r8_bt", "……ああ。必要なら、いつでも来い。", "...Aye. Come anytime you need.", "……啊。需要的话，随时来。", actor=balgas
    ).jump(ending_bt)

    builder.step(final_nod_bt).say(
        "balgas_r9_bt", "……よし。じゃあ行け。", "...Good. Now get goin'.", "……行。那就滚吧。", actor=balgas
    ).jump(ending_bt)

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending_bt).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.CompleteBalgasTrainingQuest();"
    ).finish()
