# -*- coding: utf-8 -*-
"""Drama 3: ars_karen_encounter (Stage 2->3: Karen confrontation)"""

from drama.data import BGM, Actors
from drama.drama_builder import ChoiceReaction, DramaBuilder


def define_karen_encounter(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    karen = builder.register_actor(Actors.KAREN, "カレン", "Karen", name_cn="卡伦")
    main = builder.label("main")
    questions = builder.label("questions")
    q_erenos = builder.label("q_erenos")
    q_successors = builder.label("q_successors")
    q_karen = builder.label("q_karen")
    proposal = builder.label("proposal")
    surrender = builder.label("surrender")
    defiance = builder.label("defiance")
    merge = builder.label("merge")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_karen_encounter"

    # ── main: Karen approaches ──
    builder.step(main)
    builder.quest_check("ars_karen_encounter", can_start_tmp_flag)
    builder.fade_out(0.5)
    builder.fade_in(0.5)
    builder.play_bgm_with_fallback(BGM.OMINOUS, BGM.REVELATION)

    builder.conversation(
        [
            (
                "ke_1",
                "前方に、鎧の軋む音。だが...武器を抜く気配はない。\n"
                "整然とした足取りが近づいてくる。",
                "Ahead, the creak of armor. But no sign of drawn weapons. \n"
                "Measured footsteps approach.",
                "前方传来铠甲的吱嘎声。然而……没有拔出武器的迹象。\n"
                "沉稳的脚步声逐渐靠近。",
            ),
            (
                "ke_2",
                "神殿騎士団の紋章。\n癒しの女神ジュアに仕える者たち。\n名前だけは聞いたことがある。\n\n"
                "だが、先頭の女は別だ。\n纏う空気が違う。",
                "The crest of the Temple Knights.\nServants of Jure, goddess of healing.\nYou've heard the name.\n\n"
                "But the woman at their head is different.\nShe carries a different air.",
                "神殿骑士团的纹章。\n侍奉治愈女神朱亚之人。\n名字倒是听说过。\n\n"
                "但领头的女子不同。\n她身上萦绕着别样的气场。",
            ),
            (
                "ke_3",
                "禁書が鞄の中で震えている。\n怯えた震えではない。\nこの女を、禁書は知っている。",
                "The tome trembles in your bag.\nNot a frightened tremor.\nThe tome knows this woman.",
                "禁书在包中颤抖着。\n不是恐惧的颤抖。\n禁书认识这个女人。",
            ),
        ],
        actor=narrator,
    )

    builder.wait(0.3)

    builder.conversation(
        [
            ("ke_4", "待ちなさい。敵じゃないわ。", "Wait. I will not draw my blade.",
             "等一下。我不是敌人。"),
            (
                "ke_5",
                "私はカレン・グレイヴォーン。\n神殿騎士団の灰爵。\n\n禁書が目覚めた時にだけ\n任命される役職よ。",
                "I am Karen Gravorn.\nAsh Knight of the Temple Knights.\n\nA post appointed only\nwhen the forbidden tome awakens.",
                "我是卡伦·格雷沃恩。\n神殿骑士团的灰爵。\n\n只有在禁书觉醒时\n才会被任命的职位。",
            ),
            (
                "ke_6",
                "あなたが継承者だということは\n分かっている。\n\n記録にある5人を含めて……\nあなたは6人目よ。",
                "I know you are the successor.\n\nCounting the five\nin the records...\nyou are the sixth.",
                "我知道你是继承者。\n\n加上记录中的五人……\n你是第六个。",
            ),
        ],
        actor=karen,
    )

    builder.say(
        "ke_7",
        "6人目。その言葉に、禁書が鞄の中でかすかに脈打った。",
        "The sixth. At those words, the tome pulses faintly in your bag.",
        actor=narrator,
        text_cn="第六个。听到这个词，禁书在包中微微搏动。",
    )

    builder.conversation(
        [
            (
                "ke_8",
                "今日は武器を抜きに来たのではない。まず、話をさせてほしい。",
                "I have not come to draw steel today. Please -- let me speak first.",
                "今天不是来动武的。请先……让我说完。",
            ),
            (
                "ke_9",
                "あの書を手にしてから、\n何か変わったでしょう。\n\n……それは力よ。\n否定はしない。\n\nだがその先を、\n私は知っている。",
                "Since you took hold of that book,\nsomething has changed.\n\n...That is power.\nI won't deny it.\n\nBut I know\nwhat lies beyond.",
                "自从你拿到那本书之后，\n有些东西变了吧。\n\n……那是力量。\n我不否认。\n\n但之后会发生什么，\n我知道。",
            ),
        ],
        actor=karen,
    )

    builder.say(
        "ke_10",
        "カレンの声が僅かに揺れた。\n記録を語っているのではない...記憶を語っている。",
        "Karen's voice wavers slightly. \n"
        "She is not recounting records -- she is recounting memories.",
        actor=narrator,
        text_cn="卡伦的声音微微颤抖。\n她不是在讲述记录……而是在讲述记忆。",
    )

    builder.conversation(
        [
            (
                "ke_11",
                "5人目の継承者……\nエレノスという男がいた。\n\n聡明で、慎重で、\n善良ですらあった。\n……最初は。",
                "The fifth successor...\na man named Erenos.\n\nBrilliant, cautious,\neven good-natured.\n...At first.",
                "第五个继承者……\n有一个叫艾雷诺斯的男人。\n\n聪慧、谨慎，\n甚至可以说善良。\n……起初是这样。",
            ),
            (
                "ke_12",
                "彼は私の部下を壊した。\n殺さなかった。\n\n……「理解させた」のよ。\n死の本質を。",
                "He broke one of my subordinates.\nHe didn't kill him.\n\n...He \"made him understand.\"\nThe true nature of death.",
                "他毁掉了我的部下。\n没有杀他。\n\n……而是让他「理解」了。\n死亡的本质。",
            ),
            (
                "ke_13",
                "聞きたいことがあれば\n答えるわ。\n\n私が何を見てきたか、\n知ってほしい。",
                "If you have questions,\nI will answer them.\n\nI want you to know\nwhat I have witnessed.",
                "若有想问的事，\n我会回答。\n\n我希望你了解\n我见证过的一切。",
            ),
        ],
        actor=karen,
    )

    # ── question choice (loops back after each answer) ──
    builder.step(questions)
    builder.say(
        "ke_q_prompt",
        "何を聞く？",
        "What do you ask?",
        actor=narrator,
        text_cn="问些什么？",
    )
    builder.choice_block(
        [
            ChoiceReaction(
                "エレノスは、最後にはどうなった？",
                "ke_qa",
                text_en="What happened to Erenos in the end?",
                text_cn="艾雷诺斯最后怎样了？",
            ).jump(q_erenos),
            ChoiceReaction(
                "全員が善意で始めたと、本当に？",
                "ke_qb",
                text_en="All of them truly began with good intentions?",
                text_cn="所有人真的都始于善意吗？",
            ).jump(q_successors),
            ChoiceReaction(
                "なぜそこまでする？ 任務だからか？",
                "ke_qc",
                text_en="Why go this far? Because it's your duty?",
                text_cn="你为何做到这种地步？因为是任务吗？",
            ).jump(q_karen),
            ChoiceReaction(
                "……もう聞くことはない。",
                "ke_qd",
                text_en="...I have no more questions.",
                text_cn="……没什么要问的了。",
            ).jump(proposal),
        ]
    )

    # ── q_erenos: Erenos's fate ──
    builder.step(q_erenos)
    builder.conversation(
        [
            (
                "ke_a1",
                "エレノスは12体以上の\n従者を作り上げた。\n\n私の部隊から7人を……\n「理解」によって\n寝返らせた。",
                "Erenos created over twelve\nservants.\n\nHe turned seven from my unit\nthrough \"understanding.\"",
                "艾雷诺斯制造了\n超过十二体仆从。\n\n从我的部队中使七人……\n通过「理解」\n叛变。",
            ),
            (
                "ke_a2",
                "討伐に3年かかった。\n\n追い詰めた時、\nエレノスは笑っていた。\n\n……善良だった頃の面影は\nもう残っていなかった。",
                "It took three years\nto bring him down.\n\nWhen we cornered him,\nhe was laughing.\n\n...Nothing remained\nof the good man he once was.",
                "讨伐花了三年。\n\n将他逼入绝境时，\n艾雷诺斯在笑。\n\n……善良时期的影子\n已荡然无存。",
            ),
            (
                "ke_a3",
                "あの男は善い人間だった。禁書に触れるまでは。",
                "He was a good man. Until he touched the tome.",
                "那个男人曾是个好人。直到他触碰了禁书。",
            ),
        ],
        actor=karen,
    )
    builder.jump(questions)

    # ── q_successors: the five predecessors ──
    builder.step(q_successors)
    builder.conversation(
        [
            (
                "ke_b1",
                "教団の公式見解では\n「全員が邪悪だった」\nことになっている。\n\n……だが灰爵として\n文書庫を読んだ。\n事実は違う。",
                "The official doctrine holds\nthat \"all successors were evil.\"\n\n...But as Ash Knight,\nI've read the vault records.\nThe truth differs.",
                "教团的官方见解\n认定「全员皆为邪恶之徒」。\n\n……然而作为灰爵，\n我阅读过档案库。\n事实并非如此。",
            ),
            (
                "ke_b2",
                "2代目のヴァルディスは、\nただ妻を蘇らせたかった。\n\n蘇生には成功した……\nだが戻ってきたのは\n「妻だったもの」だった。\n\n最後には自ら投降している。",
                "Valdis, the second,\nonly wanted to bring back his wife.\n\nHe succeeded...\nbut what returned\nwas \"what had been his wife.\"\n\nIn the end,\nhe surrendered himself.",
                "第二代瓦尔迪斯，\n只是想复活妻子。\n\n复活成功了……\n然而回来的\n是「曾经身为妻子的东西」。\n\n最后他自行投降了。",
            ),
            (
                "ke_b3",
                "4代目のミレーユは\n元聖職者だった。\n\n「死者の尊厳を守る蘇生」\nを掲げ、\n共感した騎士が何人も\n離反した。",
                "Mireille, the fourth,\nwas a former priestess.\n\nShe championed \"resurrection\nthat honors the dead's dignity.\"\nSeveral knights defected,\nmoved by her ideals.",
                "第四代米蕾尤\n曾是圣职者。\n\n她提倡「守护死者尊严的复活」，\n数名骑士被其理想感召\n而叛离。",
            ),
            (
                "ke_b4",
                "邪悪だったのではない。\n善良だったからこそ……\n禁書に選ばれた。",
                "They were not evil.\nIt was because they were good\nthat the tome chose them.",
                "他们并非邪恶。\n正因为善良……\n才被禁书选中。",
            ),
        ],
        actor=karen,
    )
    builder.jump(questions)

    # ── q_karen: Karen's motivation ──
    builder.step(q_karen)
    builder.conversation(
        [
            (
                "ke_c1",
                "……任務か。建前はそうだな。",
                "...Duty? That's the official reason, yes.",
                "……任务吗？官面上确实如此。",
            ),
            (
                "ke_c2",
                "公式記録には\n「邪悪な死霊術師を討伐」\nとしか残らない。\n\nエレノスが何を望んでいたか、\nアルヴィンがなぜ剣を捨てたか\n……誰も書き残さない。",
                "Official records say only\n\"evil necromancer eliminated.\"\n\nWhat Erenos wished for,\nwhy Alvin laid down his sword...\nno one writes that down.",
                "官方记录只留下\n「讨伐邪恶的死灵术士」。\n\n艾雷诺斯的愿望是什么，\n阿尔文为何放下剑……\n无人记载。",
            ),
            (
                "ke_c3",
                "だから自分の手帳に\n記録している。\n\n彼らが何者だったのか。\n何を望んでいたのか。\n……忘れないために。",
                "So I keep my own journal.\n\nWho they were.\nWhat they wished for.\n...So I don't forget.",
                "所以我用自己的手账\n做记录。\n\n他们是什么人。\n他们渴望什么。\n……为了不忘记。",
            ),
            (
                "ke_c4",
                "同じ悲劇を繰り返すだけの\n存在にはなりたくない。\n\nだから……\nこうして話をしに来た。",
                "I don't want to be someone\nwho merely repeats the tragedy.\n\nThat is why\nI have come to talk.",
                "我不想成为\n只会重复悲剧的人。\n\n所以……\n才会来这里与你交谈。",
            ),
        ],
        actor=karen,
    )
    builder.jump(questions)

    # ── proposal: Karen's request ──
    builder.step(proposal)
    builder.wait(0.3)
    builder.conversation(
        [
            (
                "ke_p1",
                "……あなたは聡明な人間だと思う。だからこそ聞いてほしい。",
                "...I believe you are perceptive. That is precisely why I ask.",
                "……我认为你是个聪明人。正因如此才要请求你。",
            ),
            (
                "ke_p2",
                "禁書を渡してくれないか。今なら、まだ間に合う。",
                "Will you hand over the tome? There is still time.",
                "能把禁书交出来吗？现在还来得及。",
            ),
        ],
        actor=karen,
    )

    builder.choice_block(
        [
            ChoiceReaction(
                "……わかった。渡そう。",
                "ke_fa",
                text_en="...All right. I'll hand it over.",
                text_cn="……好吧。交给你。",
            ).jump(surrender),
            ChoiceReaction("断る。", "ke_fb", text_en="I refuse.",
                          text_cn="拒绝。").jump(defiance),
        ]
    )

    # ── surrender: tome won't release ──
    builder.step(surrender)
    builder.conversation(
        [
            (
                "ke_s1",
                "あなたは禁書を差し出そうとした。\n"
                "...手が動かない。指が表紙に貼りついている。",
                "You tried to hand over the tome. \n"
                "Your hand won't move. Your fingers are stuck to the cover.",
                "你试图交出禁书。\n"
                "……手动不了。手指粘在封面上。",
            ),
            (
                "ke_s2",
                "引き剥がそうとする。\n痛みが走る。禁書が脈打つ。\n指が、自分の意志で\n表紙を握りしめていた。",
                "You try to pry them off.\nPain shoots through you. The tome pulses.\nYour fingers were gripping\nthe cover of their own will.",
                "试图强行剥离。\n疼痛袭来。禁书搏动。\n手指以自己的意志\n紧紧攥住了封面。",
            ),
        ],
        actor=narrator,
    )
    builder.say(
        "ke_s3",
        "……もう離れないのか。\n禁書があなたを\n選んでいる。\n\nあなたの意志とは……\nもう関係なく。",
        "...It won't let go.\nThe tome has chosen you.\n\nRegardless\nof your will.",
        actor=karen,
        text_cn="……已经分不开了吗。\n禁书选择了你。\n\n与你的意志……\n已无关系。",
    )
    builder.jump(merge)

    # ── defiance: refusal ──
    builder.step(defiance)
    builder.conversation(
        [
            (
                "ke_d1",
                "……そう。\nエレノスも同じことを\n言ったわ。\n「自分は違う」と。",
                "...I see.\nErenos said the same.\n\"I am different.\"",
                "……是吗。\n艾雷诺斯也说过\n同样的话。\n「我不一样」。",
            ),
            (
                "ke_d2",
                "全員がそう言った。一人の例外もなく。",
                "All of them said that. Without a single exception.",
                "所有人都这么说过。无一例外。",
            ),
        ],
        actor=karen,
    )
    builder.jump(merge)

    # ── merge: battle ──
    builder.step(merge)
    builder.play_bgm(BGM.BATTLE)
    builder.shake()
    builder.say(
        "ke_m1",
        "カレンが一歩退き、腰の聖槍のレプリカに手を添えた。\n"
        "迷いのない所作。だが...その瞳に宿るのは怒りではなかった。",
        "Karen steps back and places her hand on the replica holy lance \n"
        "at her hip. No hesitation in the gesture. \n"
        "Yet what dwells in her eyes is not anger.",
        actor=narrator,
        text_cn="卡伦退后一步，将手搭上腰间的圣枪仿品。\n"
        "毫不犹豫的举止。然而……她眼中并非怒意。",
    )
    builder.conversation(
        [
            (
                "ke_m2",
                "……残念よ。本当に。\n\nだがあなたを放置すれば\nどうなるか、\n私はこの目で見ている。",
                "...I am sorry. Truly.\n\nBut I have seen\nwhat happens when a successor\nis left unchecked.",
                "……遗憾。真的。\n\n但如果放任你不管\n会变成什么样，\n我亲眼见过。",
            ),
            ("ke_m3", "構えなさい。", "Prepare yourself.", "做好准备。"),
        ],
        actor=karen,
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.5)
