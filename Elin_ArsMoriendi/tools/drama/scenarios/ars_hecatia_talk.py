# -*- coding: utf-8 -*-
"""Drama: hecatia (addDrama talk: Hecatia's merchant conversation)

Branching NPC talk triggered by addDrama tag.
- Default (Stage 0-6): casual merchant greeting → Q&A / trade
- Stage 7+, first time: full identity reveal cutscene → Q&A / trade
- Stage 7+, after reveal: short post-reveal greeting → Q&A / trade

Q&A menus:
- spell_menu (always): necromancy spell advice (soul, servant, curse)
- ask_menu (always): basic lore Q&A (the Sixth, Ars Moriendi, Erenos, undead, machines, bosses, limits)
- lore_menu (post-reveal): tome lore (nature, inheritors, prophecy)

Kansai dialect (JP), casual/witty (EN).
"""

from drama.data import BGM, Actors, CommandKeys, FlagKeys, ResolveKeys
from drama.drama_builder import DramaBuilder


def define_hecatia_talk(builder: DramaBuilder):
    hecatia = builder.register_actor(
        Actors.HECATIA, "ヘカティア", "Hecatia", name_cn="赫卡提亚"
    )
    narrator = builder.register_actor(
        Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书"
    )

    erenos = builder.register_actor(
        Actors.ERENOS_PET, "エレノス", "Erenos", name_cn="艾雷诺斯"
    )

    main = builder.label("main")
    normal_greeting = builder.label("normal_greeting")
    post_reveal_greeting = builder.label("post_reveal_greeting")
    reveal_scene = builder.label("reveal_scene")
    reveal_2 = builder.label("reveal_2")
    reveal_epilogue = builder.label("reveal_epilogue")
    choices = builder.label("choices")
    guidebook_info = builder.label("guidebook_info")
    guidebook_summary = builder.label("guidebook_summary")
    guidebook_buy = builder.label("guidebook_buy")
    who_question = builder.label("who_question")
    voice_question = builder.label("voice_question")
    spell_menu = builder.label("spell_menu")
    spell_soul = builder.label("spell_soul")
    spell_servant = builder.label("spell_servant")
    spell_curse = builder.label("spell_curse")
    ritual_detail = builder.label("ritual_detail")
    enhance_detail = builder.label("enhance_detail")
    chicken_question = builder.label("chicken_question")
    ask_menu = builder.label("ask_menu")
    ask_sixth = builder.label("ask_sixth")
    ask_ars = builder.label("ask_ars")
    ask_erenos = builder.label("ask_erenos")
    ask_undead = builder.label("ask_undead")
    ask_machine = builder.label("ask_machine")
    ask_boss = builder.label("ask_boss")
    ask_limit = builder.label("ask_limit")
    lore_menu = builder.label("lore_menu")
    lore_tome = builder.label("lore_tome")
    lore_inheritors = builder.label("lore_inheritors")
    lore_seventh = builder.label("lore_seventh")
    lore_erenos = builder.label("lore_erenos")
    lore_erenos_short = builder.label("lore_erenos_short")
    lore_erenos_short_has = builder.label("lore_erenos_short_has")
    borrow_erenos = builder.label("borrow_erenos")
    ask_identity = builder.label("ask_identity")
    party_song = builder.label("party_song")
    ps_stings = builder.label("ps_stings")
    ps_empathy = builder.label("ps_empathy")
    ps_ask_love = builder.label("ps_ask_love")
    ps_proud = builder.label("ps_proud")
    ps_reframe = builder.label("ps_reframe")
    ps_pastel = builder.label("ps_pastel")
    ps_react_a = builder.label("ps_react_a")
    ps_react_b = builder.label("ps_react_b")
    ps_demo = builder.label("ps_demo")
    ps_punchline = builder.label("ps_punchline")
    ps_epilogue = builder.label("ps_epilogue")
    end = builder.label("end")

    # ── main: sync flags and branch ──
    builder.step(main)
    builder.resolve_flag(ResolveKeys.QUEST_IS_COMPLETE, FlagKeys.QUEST_COMPLETE)
    builder.branch_if(FlagKeys.HECATIA_REVEALED, "==", 1, post_reveal_greeting)
    builder.branch_if(FlagKeys.QUEST_COMPLETE, "==", 1, reveal_scene)
    builder.jump(normal_greeting)

    # ── normal_greeting: pre-apotheosis merchant ──
    builder.step(normal_greeting)
    builder.say(
        "ng_1",
        "いらっしゃい、六代目。ゆっくり見てってや。",
        "Welcome, Sixth. Take your time browsing.",
        actor=hecatia,
        text_cn="来啦，第六代。慢慢看吧。",
    )
    builder.jump(choices)

    # ── post_reveal_greeting: identity already known ──
    builder.step(post_reveal_greeting)
    builder.say(
        "prg_1",
        "ほな、今日は何がいるん？",
        "So, what do you need today?",
        actor=hecatia,
        text_cn="那，今天需要什么？",
    )
    builder.jump(choices)

    # ── reveal_scene: the tome reacts (narrator intro) ──
    builder.step(reveal_scene)
    builder.add_actor(narrator, "禁書", "The Tome")
    builder.drama_start(fade_duration=1.0)
    builder.play_bgm(BGM.REVELATION)
    builder.wait(0.5)
    builder.conversation(
        [
            (
                "hr_1",
                "禁断の書から商人を召喚した。だが...いつもと様子が違う。",
                "You summon the merchant from the tome. But -- something is different.",
                "从禁书中召唤出了商人。然而……与平时不同。",
            ),
            (
                "hr_2",
                "ヘカティアの輪郭が揺らいでいる。まるで頁の文字が人の形をとったように。",
                "Hecatia's outline wavers. As if the letters on the pages took human form.",
                "赫卡提亚的轮廓在摇曳。仿佛书页上的文字化为了人形。",
            ),
        ],
        actor=narrator,
    )
    builder.wait(0.5)
    builder.jump(reveal_2)

    # ── reveal_2: Hecatia speaks ──
    builder.step(reveal_2)
    builder.conversation(
        [
            (
                "hr_3",
                "ようこそ、こちら側へ。思ったより静かやろ？",
                "Welcome to the other side. Quieter than you expected, right?",
                "欢迎来到这一侧。比想象中安静吧？",
            ),
            (
                "hr_4",
                "……ふふ。その顔。",
                "...Heh. That look on your face.",
                "……呵呵。你这表情。",
            ),
            (
                "hr_5",
                "もう隠してもしゃあないか。あんたはもう、こちら側の人間やし。",
                "No point hiding it anymore, I suppose. You're one of us now.",
                "再藏也没意义了嘛。你已经是这一侧的人了。",
            ),
        ],
        actor=hecatia,
    )
    builder.wait(0.3)
    builder.shake()
    builder.conversation(
        [
            ("hr_6", "うちが初代や。", "I'm the first.", "我就是初代。"),
            (
                "hr_7",
                "名前が残ってへんのは、文字通り本になったからやねん。\n"
                "……笑えるやろ？ 笑ってええよ。",
                "My name's gone 'cause I literally became the book. \n"
                "...Funny, right? Go ahead and laugh.",
                "名字没有留下来，是因为我字面意义上变成了书嘛。\n……好笑吧？笑就笑呗。",
            ),
            (
                "hr_8",
                "「見つけた」のはうちや。原初のアーティファクト。\n"
                "ほんで最初の昇華の儀式をやってん。",
                "I'm the one who found it. The primordial artifact. \n"
                "And I performed the first apotheosis ritual.",
                "「发现」它的人是我。原初的神器。\n然后我施行了第一次升华仪式。",
            ),
            (
                "hr_9",
                "結果は...まあ、見ての通り。「成功しすぎた」んやね。\n"
                "力を得る代わりに、アーティファクトと融合してもうた。",
                "The result -- well, you can see for yourself. 'Succeeded too well.' \n"
                "Instead of gaining power, I fused with the artifact.",
                "结果嘛……你也看到了。「成功过头了」呗。\n"
                "没有得到力量，反而与神器融为一体了。",
            ),
        ],
        actor=hecatia,
    )
    builder.wait(0.5)
    builder.jump(reveal_epilogue)

    # ── reveal_epilogue: the merchant returns ──
    builder.step(reveal_epilogue)
    builder.conversation(
        [
            (
                "hr_10",
                "あんたらが感じてた人格漂流な、あれ、ほとんどはうちの視座やで。\n"
                "五代分のフィルター通ってるから、ぼやけてるけど。",
                "That personality drift you've all been feeling? That's all my perspective. \n"
                "Filtered through five generations, so it's a bit blurry.",
                "你们感受到的人格漂流啊，那基本上都是我的视角。\n"
                "经过了五代人的滤镜，所以模糊了些。",
            ),
            (
                "hr_11",
                "禁書の本質は取引や。知識と引き換えに、人格を少しずつ侵す。\n"
                "うちはその最初の客。代金は...まあ、うちの全部やね。",
                "The tome's nature is a transaction. Knowledge in exchange for slow erosion of self. \n"
                "I was the first customer. The price was -- well, everything.",
                "禁书的本质是交易。以知识为代价，一点点侵蚀人格。\n"
                "我是第一个客人。代价嘛……就是我的一切。",
            ),
            (
                "hr_12",
                "「七代目は、問いを持つ者であれ」...あれ書いたん、うちや。\n"
                "あんたが禁書を開いた時に震えたんも、うち。嬉しかってん。",
                "'Let the seventh carry questions' -- I wrote that. \n"
                "When you opened the tome and it trembled? That was me. I was happy.",
                "「愿第七代，是持有疑问之人」……那是我写的。\n"
                "你翻开禁书时它颤抖了吧？那是我。我很高兴。",
            ),
        ],
        actor=hecatia,
    )
    builder.wait(0.3)
    builder.conversation(
        [
            (
                "hr_13",
                "あんたは七番目になるかもしれへん。\n"
                "それとも六番目で終わるかもしれへん。",
                "You might become the seventh. Or you might end as the sixth.",
                "你也许会成为第七代。\n也许就止步于第六代。",
            ),
            (
                "hr_14",
                "どっちでもええねん。うちはもう、どうでもええわ。\n"
                "……嘘やけど。嘘つけへんのやった。禁書の一部やからな。",
                "Either way's fine by me. I don't care anymore. \n"
                "...That's a lie. I can't lie -- I'm part of the tome, after all.",
                "哪边都无所谓。我已经不在乎了。\n"
                "……骗你的。我没法撒谎——毕竟我是禁书的一部分嘛。",
            ),
            (
                "hr_15",
                "ほな、買い物でもしてき。いつも通りに。\n"
                "うちはずっとここにおるし。文字通り。",
                "Anyway, go do some shopping. Business as usual. \n"
                "I'll be right here. Literally.",
                "好啦，去逛逛买点东西吧。照常就行。\n我一直都在这里。字面意义上的。",
            ),
        ],
        actor=hecatia,
    )
    builder.set_flag(FlagKeys.HECATIA_REVEALED, 1)
    builder.fade_in(duration=1.0, color="black")
    builder.jump(choices)

    # ── choices: trade menu ──
    builder.step(choices)
    builder.say(
        "ch_prompt", "何にする？", "What'll it be?", actor=hecatia, text_cn="想要什么？"
    )
    builder.choice(
        ask_menu, "聞きたいことがある", text_en="I have a question", text_cn="有事想问"
    )
    builder.choice(
        spell_menu,
        "死霊術の概要を知りたい",
        text_en="I want an overview of necromancy",
        text_cn="我想了解死灵术概要",
    )
    builder.choice(
        guidebook_info,
        "死霊術呪文の戦術指南をしてほしい",
        text_en="I want tactical guidance for necromancy spells",
        text_cn="请给我死灵术咒文的战术指南",
    )
    builder.choice_if(
        lore_menu,
        "禁書について聞く",
        condition=builder.cond_has_flag(FlagKeys.HECATIA_REVEALED),
        text_en="Ask about the tome",
        text_cn="询问禁书",
    )
    # 一時的にコメントアウト。公開するかは未定
    # builder.choice(
    #     party_song,
    #     "死霊術の悩みを聞いてくれないか",
    #     text_en="I need advice about necromancy",
    #     text_cn="我想咨询死灵术",
    # )
    builder.choice("_buy", "取引する", text_en="Trade", text_cn="交易")
    builder.choice(end, "去る", text_en="Leave", text_cn="离开")
    builder.on_cancel(end)

    # ── guidebook_info ──
    builder.step(guidebook_info)
    builder.conversation(
        [
            (
                "gbi_1",
                "ああ、『死霊術師のための戦術指南』のことやね。\n"
                "死霊術の戦い方を、実戦向けにまとめた本や。",
                "Ah, you mean 'A Necromancer's Tactical Guide.' \n"
                "It's a practical manual on how to fight with necromancy.",
                "啊，你是说《死灵术师的战术指南》吧。\n"
                "那是一本把死灵术实战思路整理好的手册。",
            ),
        ],
        actor=hecatia,
    )
    builder.say(
        "gbi_prompt",
        "どうする？",
        "What do you want?",
        actor=hecatia,
        text_cn="你想怎么做？",
    )
    builder.choice(
        guidebook_summary,
        "概要だけ教えて",
        text_en="Give me the short version",
        text_cn="只讲概要",
    )
    builder.choice(
        guidebook_buy,
        "本で詳しく読む",
        text_en="Read the full book",
        text_cn="去读完整书",
    )
    builder.choice(choices, "今はいい", text_en="Not now", text_cn="现在先不用")
    builder.on_cancel(choices)

    # ── guidebook_summary ──
    builder.step(guidebook_summary)
    builder.conversation(
        [
            (
                "gbs_1",
                "ほな、口頭で超圧縮版や。\n"
                "勝ち筋は三本柱。間接制圧・従者運用・死体経済。\n"
                "即効火力で押し切る流派やない。長期戦ほど強い。\n"
                "開戦前は「魂魄保存」か「魂縛の檻」で魂回収ラインを敷く。\n"
                "同時に「屍体保存」で素材供給を止めへんこと。",
                "Alright, here's the ultra-compressed oral version.\n"
                "Three pillars: indirect control, servant operation, and corpse economy.\n"
                "This style is not burst-first; it gets stronger in longer fights.\n"
                "Before combat, cast Preserve Soul/Soul Snare to establish soul intake.\n"
                "Then keep Preserve Corpse up so materials never run dry.",
                "那就先给你口头超压缩版。\n"
                "三支柱：间接压制、仆从运用、尸体经济。\n"
                "这套不是瞬间爆发，战斗越长越有利。\n"
                "开战前先用灵魂封存/灵魂囚笼铺好回收线。\n"
                "再维持保存尸体，别让素材断档。",
            ),
            (
                "gbs_2",
                "本番の回しはこうや。\n"
                "「黄泉の泥濘」で速度を奪い、「恐怖」で危険個体を止める。\n"
                "「衰弱の呪い」「衰弱の呪い（重）」「疫病の手」で敵性能を削る。\n"
                "「死軍号令」で軍団火力を引き上げ、「骸骨壁」で前線を保つ。\n"
                "死体が溜まったら「屍鎖爆砕」で攻撃・妨害・回復を一手で回収。\n"
                "要するに、盤面を腐らせてから連鎖で刈り取るんや。",
                "Your live rotation goes like this.\n"
                "Use Grave Quagmire to steal tempo, then pin dangerous targets with Terror.\n"
                "Keep shaving stats with Curse of Weakness/Frailty and Plague Touch.\n"
                "Raise legion output with Funeral March, stabilize frontlines with Wall of Skeleton.\n"
                "When corpses stack up, Corpse Chain Burst cashes them into offense/control/sustain.\n"
                "In short: rot the board first, then harvest with chains.",
                "实战循环是这样。\n"
                "先用黄泉泥泞夺节奏，再用恐惧点停关键威胁。\n"
                "用衰弱诅咒/重衰弱诅咒/瘟疫之触持续拆参数。\n"
                "死军号令抬军团输出，骸骨之墙稳前线。\n"
                "尸体堆起来后，用尸锁爆砕一手回收进攻/控制/续航。\n"
                "总结：先把盘面腐化，再用连锁收割。",
            ),
            (
                "gbs_3",
                "最後に生存の話や。\n"
                "「魂の鎖」は致死を従者に肩代わりさせる最終保険。\n"
                "ただし従者切れ・MP切れ・行動阻害が重なると循環は止まる。\n"
                "せやから死霊術は、戦闘中より準備段階で勝負が決まる。\n"
                "ここまでが口頭版や。細かい優先順位と例外処理は本にまとめてある。",
                "Final point: survival.\n"
                "Soul Bind is your last insurance, letting a servant take lethal for you.\n"
                "But the loop collapses if servants run out, MP dries up, or you get disabled.\n"
                "So necromancy is decided in preparation more than in the fight itself.\n"
                "That's the oral version. Priority tables and edge cases are in the book.",
                "最后说生存。\n"
                "灵魂锁链是终极保险，让仆从替你吃致死伤。\n"
                "但仆从断档、法力见底、被控叠加时，循环会直接崩。\n"
                "所以死灵术胜负往往在开战前准备阶段就决定了。\n"
                "以上是口头版，细优先级和例外处理都在书里。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(choices)

    # ── guidebook_buy ──
    builder.step(guidebook_buy)
    builder.conversation(
        [
            (
                "gbb_1",
                "ええ？ 売りもんやっちゅうねん。店主の前で立ち読みか？ \n"
                "\n...ええよ。しょうがないにゃあ...",
                "Huh? This is merchandise, y'know. Reading it right in front of the shopkeeper?\n"
                "\n...Fine. Can't be helped...",
                "诶？这是要卖的商品耶。当着店主的面白看吗？\n\n……行吧。真拿你没办法……",
            )
        ],
        actor=hecatia,
    )
    builder.show_book("ars_hecatia_guide", "Book")
    builder.jump(choices)

    # ── spell_menu: necromancy Q&A ──
    builder.step(spell_menu)
    builder.say(
        "sm_intro",
        "何が知りたいん？ うちで分かることやったら教えたるで。",
        "What do you want to know? I'll tell you what I can.",
        actor=hecatia,
        text_cn="想知道什么？我知道的都可以告诉你。",
    )
    builder.choice(spell_soul, "魂の術", text_en="Soul magic", text_cn="灵魂之术")
    builder.choice(
        spell_servant, "従者の扱い", text_en="Handling servants", text_cn="仆从的使用"
    )
    builder.choice(
        spell_curse, "呪いと防御", text_en="Curses and defense", text_cn="诅咒与防御"
    )
    builder.choice(
        ritual_detail,
        "蘇生の仕組み",
        text_en="How resurrection works",
        text_cn="复活的机制",
    )
    builder.choice(
        enhance_detail,
        "従者強化の仕組み",
        text_en="How enhancement works",
        text_cn="仆从强化的机制",
    )
    builder.choice(
        chicken_question,
        "鶏が蘇生に使えると聞いた",
        text_en="I heard chickens work for resurrection",
        text_cn="听说鸡可以用来复活",
    )
    builder.choice(choices, "もういい", text_en="Never mind", text_cn="算了")
    builder.on_cancel(choices)

    # ── spell_soul ──
    builder.step(spell_soul)
    builder.conversation(
        [
            (
                "ss_1",
                "魂の術はな、死霊術の根幹や。殺す術やない...死と生の境界を操る術や。",
                "Soul magic is the foundation of necromancy. It's not about killing -- it's about manipulating the boundary between life and death.",
                "灵魂之术啊，是死灵术的根基。不是杀戮之术……而是操控生死边界的术。",
            ),
            (
                "ss_2",
                "「魂魄保存」は基本中の基本。敵の魂を保存して、素材にする。",
                '"Preserve Soul" is the most basic spell. You preserve enemy souls and use them as materials.',
                "「灵魂封存」是基础中的基础。保存敌人的灵魂，用作素材。",
            ),
            (
                "ss_3",
                "「魂縛の檻」はそのエリア版。周囲をまとめて保存できる。忙しい時に便利やで。",
                '"Soul Snare" is the area version. You can preserve everything nearby at once. Handy when you\'re busy.',
                "「灵魂囚笼」是它的范围版。可以一次性保存周围的全部灵魂。忙的时候很方便呢。",
            ),
            (
                "ss_4",
                "「魂の鎖」はうちのお気に入り。あんたと従者の魂を繋いで、致命傷を肩代わりさせるんや。",
                '"Soul Bind" is my favorite. It links your soul with your servant\'s, letting them take fatal blows for you.',
                "「灵魂锁链」是我的最爱。将你和仆从的灵魂相连，让仆从替你承受致命伤。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(spell_menu)

    # ── spell_servant ──
    builder.step(spell_servant)
    builder.conversation(
        [
            (
                "sv_1",
                "従者はな、ただの操り人形やない。魂の残響...生前の意志の欠片や。",
                "Servants aren't mere puppets. They're echoes of a soul -- fragments of the will they had in life.",
                "仆从啊，不是单纯的提线木偶。是灵魂的残响……生前意志的碎片。",
            ),
            (
                "sv_2",
                "「アンデッド召喚」は手軽な一時兵。消えるけど、使い捨てと割り切るんが大事。",
                '"Summon Undead" gives you quick temporary troops. They vanish, so think of them as disposable.',
                "「召唤亡灵」可以快速获得临时兵力。虽然会消失，但当成消耗品就对了。",
            ),
            (
                "sv_3",
                "本気で育てるなら、「屍体保存」で素材を確保して、儀式で蘇らせ。手間かけた分だけ強なる。",
                'If you\'re serious about raising one, use "Preserve Corpse" to secure materials, then resurrect it through the ritual. The more effort you put in, the stronger they become.',
                "要认真培养的话，先用「保存尸体」确保素材，然后通过仪式复活。花越多功夫就越强。",
            ),
            (
                "sv_4",
                "「従者強化」で底上げもできる。大事にしたってな。……まあ、消耗品扱いしても怒らへんけど。",
                'You can also boost them with "Servant Enhancement". Take good care of them. ...Well, I won\'t be mad if you treat them as expendable, though.',
                "也可以用「仆从强化」来提升底力。好好珍惜吧。……嘛，当消耗品用我也不会生气就是了。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(spell_menu)

    # ── spell_curse ──
    builder.step(spell_curse)
    builder.conversation(
        [
            (
                "sc_1",
                "死霊術にな、ドカンとぶっ放す呪文はあらへん。性分やないんよ。",
                "Necromancy doesn't have any big flashy attack spells. It's just not in its nature.",
                "死灵术啊，没有轰的一声放出去的咒语。不对路嘛。",
            ),
            (
                "sc_2",
                "代わりに、相手の戦意を奪う。\n"
                "「衰弱の呪い」「衰弱の呪い（重）」「恐怖」「疫病の手」…じわじわ蝕むんが流儀や。",
                "Instead, you drain the enemy's will to fight.\n"
                '"Curse of Weakness", "Curse of Frailty", "Terror", and "Plague Touch" -- slowly wearing them down is the way.',
                "取而代之的是夺去对手的战意。\n"
                "「衰弱诅咒」「衰弱诅咒（重）」「恐惧」「瘟疫之触」……慢慢侵蚀才是死灵术的流派。",
            ),
            (
                "sc_3",
                "自分を守るなら「骸骨壁」と「死の領域」。あと吸収術で相手の精気を奪い取る。",
                'For defense, use "Wall of Skeleton" and "Death Zone". And drain the enemy\'s vitality with absorption spells.',
                "要保护自己就用「骸骨之墙」和「死亡领域」。再用吸收术夺取对手的精气。",
            ),
            (
                "sc_4",
                "地味やけどな、死なへん奴が結局一番強いねん。覚えときや。",
                "It's not flashy, but the one who doesn't die is the strongest in the end. Remember that.",
                "虽然不起眼，但不会死的家伙终究是最强的。记住这一点。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(spell_menu)

    # ── ritual_detail ──
    builder.step(ritual_detail)
    builder.conversation(
        [
            (
                "rd_1",
                "蘇生はな、屍体の質がまず大事や。\n鶏の屍体にどんだけ魂注いでも、鶏は鶏や。",
                "First thing about resurrection -- the corpse's quality matters. \nNo matter how many souls you pour into a chicken corpse, it's still a chicken.",
                "复活嘛，首先尸体的质量很重要。\n往鸡的尸体里灌再多灵魂，鸡还是鸡。",
            ),
            (
                "rd_2",
                "逆に、竜やら上位種の屍体やったら、ちょっとの魂でもそこそこの力を引き出せる。\n器がでかいんやな。",
                "On the other hand, a dragon or higher being's corpse can draw decent power \nfrom just a few souls. The vessel is simply larger.",
                "反过来，龙啊高等种之类的尸体，只用少量灵魂就能引出相当的力量。\n容器大嘛。",
            ),
            (
                "rd_3",
                "で、魂の数や。多く注ぐほど、生前の力に近づく。\nうちの現役の頃の感覚にはなるんやけど、\n強い魂30個くらい注げば、ほぼ生前と変わらんとこまで戻せるで。",
                "Now, the number of souls. The more you pour in, the closer \nthey get to their living power. From my own experience back \nin the day, about thirty strong souls will restore them almost completely.",
                "然后是灵魂的数量。灌得越多，就越接近生前的力量。\n以我当年的经验来说，\n灌入三十个左右的强大灵魂，基本就能恢复到和生前差不多的程度。",
            ),
            (
                "rd_4",
                "ただし、あんた自身の経験も関係する。\n深い階層まで潜ったことない術者が、大物を完全に操れるわけないやろ？\n身の丈ってやつや。",
                "But your own experience matters too. A caster who's never ventured \ndeep can't fully control a powerful being, right? \nKnow your limits.",
                "不过，你自身的经验也有关系。\n没去过深层的术者怎么可能完全操控大家伙？\n量力而行啊。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(spell_menu)

    # ── enhance_detail ──
    builder.step(enhance_detail)
    builder.conversation(
        [
            (
                "ed_1",
                "蘇った従者はな、そのままでも戦えるけど、もっと強くしたいやろ？",
                "A resurrected servant can fight as is, but you want them stronger, right?",
                "复活的仆从嘛，虽然原样也能战斗，但想让它更强吧？",
            ),
            (
                "ed_2",
                "魂を注いで、力とか耐久とか個別に底上げできる。\nただし、同じとこに何度も注ぐと、そのうち染み込まんくなってくる。\n身体が飽和するんやな。まんべんなくやるのがコツや。",
                "You can inject souls to boost individual stats like strength or endurance. \nBut if you keep pouring into the same stat, it stops soaking in. \nThe body saturates. The trick is to spread it around.",
                "可以注入灵魂来逐项提升力量、耐久之类的。\n不过同一项灌太多次，慢慢就渗不进去了。\n身体会饱和嘛。均匀分配才是诀窍。",
            ),
            (
                "ed_3",
                "もっと大胆にやるなら、部位の増設や。\n腕がもう一本欲しいなら、腕のある屍体を素材に持ってき。\n成功するかは運次第やけど……失敗しても諦めんといてな。",
                "If you want to go bolder, try body augmentation. \nWant an extra arm? Bring a corpse that has arms as material. \nSuccess depends on luck, but... don't give up after a failure.",
                "想更大胆的话，就试试部位增设。\n想多一条手臂？带一具有手臂的尸体来当素材。\n成功与否看运气……但失败了也别放弃。",
            ),
            (
                "ed_4",
                "何回か失敗すると、身体のほうが馴染んできて、\n次はちょっと通りやすくなる。共鳴、言うてな。",
                "After a few failures, the body adapts and becomes more receptive \nnext time. We call it resonance.",
                "失败几次之后，身体那边就会慢慢适应，\n下次就更容易成功了。叫做共鸣。",
            ),
            (
                "ed_5",
                "……ただし、やりすぎは禁物。\n強化しすぎると、禁書の力が暴走することがある。\nでもな...暴走の半分は、むしろ当たりや。覚醒とか、突然変異とかな。",
                "...But don't overdo it. Too much enhancement can cause a rampage \nfrom the tome's power. Though -- half the time, a rampage is actually \na jackpot. Dark awakenings, spontaneous mutations, that sort of thing.",
                "……不过别过头了。\n强化过度的话，禁书的力量可能会暴走。\n但呢……暴走有一半其实反而是好事。觉醒啦、突变啦之类的。",
            ),
            (
                "ed_6",
                "要はな、リスク込みで楽しめるかどうかや。\nあんたの腕と覚悟次第やで。",
                "The point is, can you enjoy it knowing the risks? \nIt all depends on your skill and resolve.",
                "总之呢，能不能连风险一起享受，\n就看你的本事和觉悟了。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(spell_menu)

    # ── chicken_question ──
    builder.step(chicken_question)
    builder.conversation(
        [
            (
                "cq_1",
                "……はっ！？ あんたそれ知ってるん！？ やるやん！",
                "...Huh!? You know about that!? Well look at you!",
                "……哈！？你知道这个！？有两下子嘛！",
            ),
            (
                "cq_2",
                "いやあ嬉しいわ。うちが現役の頃でも、\nそれ知ってた死霊術師は片手で数えるほどやで。\nあんた才能あるわ。ほんまに。",
                "Oh, this makes me so happy. Even in my day, the necromancers who \nknew about that could be counted on one hand. \nYou've got real talent. Truly.",
                "哎呀真高兴。就算在我那个时代，\n知道这个的死灵术士也是屈指可数的。\n你有天赋啊。真的。",
            ),
            (
                "cq_3",
                "鶏はな...この世界のいちばん根っこにおる生き物やねん。\nどんな存在も、正体が曖昧になると、なぜか鶏に行き着く。\n世界の初期値みたいなもんや。",
                "Chickens -- they're the creature at the very foundation of this world. \nWhen any being's identity becomes ambiguous, it somehow ends up as a chicken. \nLike the world's default setting.",
                "鸡啊……是这个世界最根源的生物。\n不管什么存在，一旦身份变得暧昧，就会莫名其妙变成鸡。\n就像世界的默认值一样。",
            ),
            (
                "cq_4",
                "せやから蘇生の器としてはこの上なく寛容でな。\nどんな魂でも拒まへん。受け入れて、鶏として蘇る。",
                "That's why they're incomparably tolerant as a vessel for resurrection. \nThey won't reject any soul. They'll accept it and rise again -- as a chicken.",
                "所以作为复活的容器，鸡极其宽容。\n任何灵魂都不会拒绝。接纳之后，以鸡的形态复活。",
            ),
            (
                "cq_5",
                "……まあ、蘇っても鶏は鶏やけどな。そこだけは覚悟しときや。",
                "...Well, even resurrected, a chicken is still a chicken. \nJust be prepared for that part.",
                "……不过嘛，就算复活了鸡还是鸡。这一点要有心理准备。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(spell_menu)

    # ── ask_menu ──
    builder.step(ask_menu)
    builder.say(
        "am_intro",
        "ええよ、何でも聞き。うちで分かることやったら答えたるわ。",
        "Sure, ask away. I'll answer what I can.",
        actor=hecatia,
        text_cn="好啊，什么都可以问。我知道的都会回答。",
    )
    builder.choice_if(
        ask_sixth,
        "六代目ってなに？",
        condition=builder.cond_no_flag(FlagKeys.HECATIA_REVEALED),
        text_en="What does 'the Sixth' mean?",
        text_cn="「第六代」是什么意思？",
    )
    builder.choice_if(
        who_question,
        "あなたは誰？",
        condition=builder.cond_no_flag(FlagKeys.HECATIA_REVEALED),
        text_en="Who are you?",
        text_cn="你是谁？",
    )
    builder.choice_if(
        voice_question,
        "禁書の中で声を聞いた",
        condition=builder.cond_no_flag(FlagKeys.HECATIA_REVEALED),
        text_en="I heard a voice in the tome",
        text_cn="我在禁书中听到了声音",
    )
    builder.choice(
        ask_ars,
        "アルス・モリエンディとは？",
        text_en="What is Ars Moriendi?",
        text_cn="什么是死亡的艺术？",
    )
    builder.choice_if(
        ask_erenos,
        "エレノス・ヴェルデクトって誰？",
        condition=builder.cond_no_flag(FlagKeys.HECATIA_REVEALED),
        text_en="Who is Erenos Verdict?",
        text_cn="艾雷诺斯·维尔迪克特是谁？",
    )
    builder.choice(
        ask_undead,
        "アンデッドにも死霊術は効く？",
        text_en="Does necromancy work on undead?",
        text_cn="死灵术对亡灵也有效吗？",
    )
    builder.choice(
        ask_machine,
        "機械に魂はある？",
        text_en="Do machines have souls?",
        text_cn="机械有灵魂吗？",
    )
    builder.choice(
        ask_boss,
        "ネームドボスも使役できる？",
        text_en="Can I control named bosses?",
        text_cn="可以使役命名Boss吗？",
    )
    builder.choice(
        ask_limit,
        "従者の数に制限はある？",
        text_en="Is there a limit on servants?",
        text_cn="仆从数量有限制吗？",
    )
    builder.choice(choices, "もういい", text_en="Never mind", text_cn="算了")
    builder.on_cancel(choices)

    # ── ask_sixth ──
    builder.step(ask_sixth)
    builder.conversation(
        [
            (
                "as_1",
                "六代目。そのまんまや。この禁書を手に取った六番目の人間ってこと。",
                "The Sixth. Exactly what it sounds like. \nYou're the sixth person to take up this tome.",
                "第六代。就是字面意思。拿起这本禁书的第六个人。",
            ),
            (
                "as_2",
                "あんたの前に五人おった。\nそれぞれ事情があって、それぞれの結末を迎えた。",
                "Five came before you. Each had their own reasons, \neach met their own end.",
                "你之前有五个人。\n各有各的缘由，各有各的结局。",
            ),
            (
                "as_3",
                "ちなみにな、この本は誰にでも開けるわけやないんよ。\nあんたが読めてるんは、禁書があんたを選んだからや。",
                "By the way, not just anyone can open this book. \nYou can read it because the tome chose you.",
                "话说回来，这本书不是谁都能翻开的。\n你之所以读得了，是因为禁书选中了你。",
            ),
            (
                "as_4",
                "詳しいことはそのうち分かるわ。焦らんでええ。",
                "You'll learn the details in time. No need to rush.",
                "详情以后慢慢就知道了。别急嘛。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── ask_ars ──
    builder.step(ask_ars)
    builder.conversation(
        [
            (
                "aa_1",
                "アルス・モリエンディ。死の技法、いうてな。",
                "Ars Moriendi. 'The art of dying,' so to speak.",
                "死亡的艺术。所谓死之技法。",
            ),
            (
                "aa_2",
                "この禁書に記された術の総称や。\n魂を保存し、屍体に注ぎ、死者を蘇らせる...全部これの一部。",
                "It's the collective name for the arts inscribed in this tome. \nPreserving souls, pouring them into corpses, raising the dead \n-- all part of it.",
                "是记载于这本禁书中所有术法的总称。\n保存灵魂、注入尸体、复活死者……全都是其一部分。",
            ),
            (
                "aa_3",
                "世間では禁忌扱いやけど、\nうちに言わせたら一番正直な魔法やで。\n死は嘘つけへんからな。",
                "The world treats it as taboo, but if you ask me, \nit's the most honest form of magic. \nDeath can't lie, after all.",
                "世人将其视为禁忌，\n但依我看这是最诚实的魔法。\n死亡是不会撒谎的嘛。",
            ),
            (
                "aa_4",
                "あんたが今学んどるのは、その技法の端っこや。\n奥はもっと深い。……楽しみにしとき。",
                "What you're learning now is just the edge of the art. \nIt goes much deeper. ...Look forward to it.",
                "你现在学的不过是技法的皮毛。\n里面深着呢。……好好期待吧。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── ask_erenos ──
    builder.step(ask_erenos)
    builder.conversation(
        [
            (
                "ae_1",
                "お、五代目の名前知ってるん。やるなあ。",
                "Oh, you know the Fifth's name? Not bad.",
                "哦，你知道第五代的名字？不错嘛。",
            ),
            (
                "ae_2",
                "エレノス・ヴェルデクト。\nあんたの前にこの禁書を持ってた人間や。",
                "Erenos Verdict. \nThe one who held this tome before you.",
                "艾雷诺斯·维尔迪克特。\n在你之前持有这本禁书的人。",
            ),
            (
                "ae_3",
                "頭は切れるけど、ちょっと堅物でな。\n真面目すぎるんが玉に瑕やったわ。",
                "Sharp mind, but a bit rigid. \nBeing too serious was his one flaw.",
                "脑子很好使，就是有点死板。\n太一本正经是他唯一的缺点。",
            ),
            (
                "ae_4",
                "最後にどうなったかは...\nまあ、そのうち分かるで。禁書が全部覚えとるから。",
                "What happened to him in the end -- \nwell, you'll find out eventually. The tome remembers everything.",
                "最后怎样了嘛……\n慢慢就会知道的。禁书全都记得。",
            ),
            (
                "ae_5",
                "あんたがたまに、自分らしくない考え方しぃひん？\nあれ、ちょっとあいつの影響かもな。",
                "Don't you sometimes think in ways that don't feel like you? \nThat might be a bit of his influence.",
                "你有没有偶尔出现不像自己的想法？\n那个嘛，可能是受了他一点影响。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── ask_undead ──
    builder.step(ask_undead)
    builder.conversation(
        [
            (
                "au_1",
                "ああ、よう聞かれるわ、それ。",
                "Ah, I get that one a lot.",
                "啊，这个问题经常被问到。",
            ),
            (
                "au_2",
                "死霊術の大原則はな...\n死ねるもんには魂がある。",
                "The fundamental rule of necromancy -- \nif it can die, it has a soul.",
                "死灵术的大原则嘛……\n能死的东西就有灵魂。",
            ),
            (
                "au_3",
                "アンデッドかて倒されたら終わるやろ？\nほなら魂は出る。それだけの話や。",
                "Even undead perish when defeated, right? \nThen a soul comes out. Simple as that.",
                "亡灵被打倒了也会完蛋吧？\n那灵魂就会出来。就这么简单。",
            ),
            (
                "au_4",
                "変に遠慮せんでええよ。\n素材に貴賎なし。それが死霊術師や。",
                "Don't overthink it. \nNo material is beneath a necromancer.",
                "别想太多嘛。\n素材不分贵贱。这就是死灵术士。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── ask_machine ──
    builder.step(ask_machine)
    builder.conversation(
        [
            (
                "amc_1",
                "おもろいこと聞くなあ。",
                "Now that's an interesting question.",
                "问了个有意思的问题嘛。",
            ),
            (
                "amc_2",
                "ないで。機械には魂がない。",
                "No. Machines don't have souls.",
                "没有。机械没有灵魂。",
            ),
            (
                "amc_3",
                "機械は壊れるけど、死なへん。\n壊れることと死ぬことは違うんよ。",
                "Machines break, but they don't die. \nBreaking and dying are different things.",
                "机械会坏，但不会死。\n坏掉和死掉是不同的。",
            ),
            (
                "amc_4",
                "死霊術は死と生の境界を操る術やからな。\nその境界がないもんには、手の出しようがないんや。",
                "Necromancy manipulates the boundary between life and death. \nIf that boundary doesn't exist, there's nothing to work with.",
                "死灵术是操控生死边界的术法嘛。\n没有那条边界的东西，就无从下手了。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── ask_boss ──
    builder.step(ask_boss)
    builder.conversation(
        [
            (
                "ab_1",
                "おっ、大きく出たなぁ。ええやん、その心意気。",
                "Oh, thinking big, are we? I like that spirit.",
                "哦，口气不小嘛。不错，有这个志气。",
            ),
            (
                "ab_2",
                "普通の召喚はな、向こうが来てくれるのを待つもんや。\n格が合わんと門前払いされる。",
                "With regular summoning, you wait for them to come to you. \nIf you're not worthy, they turn you away.",
                "普通的召唤嘛，是等对方来找你的。\n格不够就会被拒之门外。",
            ),
            (
                "ab_3",
                "死霊術は逆や。あんたが自分の手で倒したもんが、\nそのまま素材になる。ネームドやろうがボスやろうが関係あらへん。",
                "Necromancy is the opposite. Whatever you defeat with your own hands \nbecomes your material. Named or boss, doesn't matter.",
                "死灵术正好相反。你亲手打倒的东西，\n直接变成素材。命名怪也好Boss也好都无所谓。",
            ),
            (
                "ab_4",
                "もちろん、大物には大物なりの魂が要る。\nけどな...あんたの強さが、そのまま従者の格になるんや。\nこれが死霊術の醍醐味やで。",
                "Of course, the bigger the prey, the more souls you'll need. \nBut your strength directly becomes your servant's caliber. \nThat's the real beauty of necromancy.",
                "当然，大家伙需要相应数量的灵魂。\n但是呢……你的强大直接成为仆从的档次。\n这就是死灵术的醍醐味。",
            ),
            (
                "ab_5",
                "ただしな、大物を従者にしたら、元の居場所にはもう現れへんくなる。\n魂が禁書に繋がるから、世界の側では「もうおらん」扱いになるんよ。",
                "One thing, though -- once you make a big shot your servant, \nthey won't show up in their old haunt anymore. Their soul's bound \nto the tome, so the world considers them 'gone.'",
                "不过啊，大家伙变成仆从之后，原来的地方就不会再出现了。\n灵魂连接到禁书上，世界那边就会当作「已经不在了」处理。",
            ),
            (
                "ab_6",
                "気ぃ変わったら解放したらええ。縁が切れたら、世界が勝手に元の場所に帰すから。\n要は、そいつの存在を世界から借りるいうことや。覚えときや。",
                "If you change your mind, just release them. Once the bond breaks, \nthe world puts them back where they belong. \nBasically, you're borrowing their existence from the world. Remember that.",
                "改变主意的话解放就行。断了联系，世界会自动把它送回原来的地方。\n简单来说就是从世界那里借用它的存在。记住这一点。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── ask_limit ──
    builder.step(ask_limit)
    builder.conversation(
        [
            (
                "al_1",
                "ここが死霊術のいちばんの売りやねん。聞いて驚き。",
                "This is necromancy's biggest selling point. Brace yourself.",
                "这可是死灵术最大的卖点。听好了别吓到。",
            ),
            (
                "al_2",
                "普通の召喚は、術者の器が上限やから数に限りがある。\n何体も抱えたら術者がもたへん。",
                "Regular summoning is capped by the caster's capacity. \nToo many and the caster can't handle it.",
                "普通的召唤受限于术者的容量，数量有上限。\n抱太多的话术者撑不住。",
            ),
            (
                "al_3",
                "けど死霊術の従者は、注ぎ入れた魂が各々器になっとる。\nあんたの器を使わへんから、負荷がかからんのよ。",
                "But with necromantic servants, each soul you pour in becomes its own vessel. \nThey don't draw on your capacity, so there's no strain on you.",
                "但死灵术的仆从啊，注入的灵魂各自成为容器。\n不占用你的容量，所以不会有负担。",
            ),
            (
                "al_4",
                "つまりな...制限なし。\n魂さえあれば好きなだけ蘇らせらるねん。",
                "In other words -- no limit. \nAs long as you have souls, raise as many as you like.",
                "也就是说……没有限制。\n只要有灵魂，想复活多少就复活多少。",
            ),
            (
                "al_5",
                "これができるんは死霊術師だけや。\n胸張ってええ特権やで。",
                "Only necromancers can do this. \nIt's a privilege worth being proud of.",
                "能做到这一点的只有死灵术士。\n挺起胸膛吧，这是值得骄傲的特权。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── lore_menu ──
    builder.step(lore_menu)
    builder.say(
        "lm_intro",
        "何を知りたい？ うちに嘘はつけへんからな。禁書の一部やし。",
        "What do you want to know? I can't lie -- I'm part of the tome, after all.",
        actor=hecatia,
        text_cn="想知道什么？我没法撒谎的。毕竟是禁书的一部分嘛。",
    )
    builder.choice(
        lore_tome,
        "禁書の正体",
        text_en="The tome's true nature",
        text_cn="禁书的真面目",
    )
    builder.choice(
        lore_inheritors, "歴代の継承者", text_en="Past inheritors", text_cn="历代继承者"
    )
    builder.choice(
        lore_seventh,
        "七代目の予言",
        text_en="The seventh's prophecy",
        text_cn="第七代的预言",
    )
    builder.choice(
        ask_identity,
        "過去のことを聞いてもいいか？",
        text_en="May I ask about your past?",
        text_cn="可以问问你过去的事吗？",
    )
    builder.choice_if(
        lore_erenos,
        "エレノスの魂はどうなった？",
        condition=builder.cond_has_flag(FlagKeys.ERENOS_DEFEATED),
        text_en="Erenos's soul",
        text_cn="艾雷诺斯的灵魂怎样了？",
    )
    builder.choice(choices, "もういい", text_en="Never mind", text_cn="算了")
    builder.on_cancel(choices)

    # ── lore_tome ──
    builder.step(lore_tome)
    builder.conversation(
        [
            (
                "lt_1",
                "禁書はな……うちが見つけた時は、ただのアーティファクトやった。意思なんかあらへん。",
                "The tome... when I first found it, it was just an artifact. No will of its own.",
                "禁书啊……我发现它的时候，只是个普通的神器。没有任何意志。",
            ),
            (
                "lt_2",
                "でも使い続けるうちに、人格を侵し始めた。知識と引き換えにな。意図的な設計やない。副作用や。",
                "But the more you use it, the more it erodes your personality. In exchange for knowledge. It's not by design -- it's a side effect.",
                "但用着用着，它开始侵蚀人格了。以知识为代价。不是刻意设计的。是副作用。",
            ),
            (
                "lt_3",
                "強すぎる知識は持ち主を変えてまう。水が器の形になるように、禁書の知識があんたの形を変える。",
                "Knowledge too powerful changes its bearer. Like water taking the shape of its vessel, the tome's knowledge reshapes you.",
                "过于强大的知识会改变持有者。就像水会变成容器的形状，禁书的知识也在改变你的形态。",
            ),
            (
                "lt_4",
                "あんたも気づいてるやろ？ 最近、昔の自分を思い出しにくくなってへん？",
                "You've noticed too, haven't you? It's getting harder to remember who you used to be.",
                "你也察觉到了吧？最近越来越难想起以前的自己了吧？",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(lore_menu)

    # ── lore_inheritors ──
    builder.step(lore_inheritors)
    builder.conversation(
        [
            (
                "li_1",
                "二代目のヴァルディスは医者やった。妻を蘇らせようとして……別のもんが返ってきた。自分で神殿に出頭したわ。",
                "The second, Valdis, was a doctor. He tried to resurrect his wife... something else came back. He turned himself in to the temple.",
                "第二代瓦尔迪斯是个医生。想复活妻子……回来的却是别的东西。他自己去神殿自首了。",
            ),
            (
                "li_2",
                "三代目のセリューは学者。理論を体系化してくれたけど、自分が誰かわからんくなった。ある日ふっと消えた。",
                "The third, Seryu, was a scholar. She systematized the theory, but lost track of who she was. One day she just vanished.",
                "第三代瑟琉是学者。把理论体系化了，但搞不清自己是谁了。某天突然就消失了。",
            ),
            (
                "li_3",
                "四代目のミレイユは元神官。「尊厳ある蘇生」を目指してた。一番まともやったかもしれん。",
                "The fourth, Mireille, was a former priestess. She pursued 'resurrection with dignity.' She might have been the sanest of them all.",
                "第四代米蕾尤是前神官。追求「有尊严的复活」。也许是最正常的一个。",
            ),
            (
                "li_4",
                "全員に共通してるのはな...皆、善意で始めたってことなんよ。禁書を手に取った動機は、いつも正しい目的やった。",
                "What they all had in common -- they all started with good intentions. The reason for picking up the tome was always righteous.",
                "所有人的共同点就是……都是出于善意开始的。拿起禁书的动机，每次都是正当的。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(lore_menu)

    # ── lore_seventh ──
    builder.step(lore_seventh)
    builder.conversation(
        [
            (
                "ls_1",
                "「七代目は、問いを持つ者であれ」...あれ書いたん、うちや。",
                "'Let the seventh carry questions' -- I wrote that.",
                "「愿第七代，是持有疑问之人」……那是我写的。",
            ),
            (
                "ls_2",
                "歴代の継承者はみんな、答えを求めた。蘇生の方法、力の秘密、自分の正義。答えを見つけた瞬間、固定される。",
                "Every inheritor sought answers. How to resurrect, the secret of power, their own justice. The moment they found their answer, they became fixed.",
                "历代继承者都在寻求答案。复活的方法、力量的秘密、自己的正义。找到答案的瞬间，就被固定了。",
            ),
            (
                "ls_3",
                "でも問い続ける者は、変わり続ける。固定されへん。それが禁書への唯一の対抗策かもしれん。",
                "But those who keep questioning keep changing. They can't be fixed. That might be the only counter to the tome.",
                "但持续追问的人会持续改变。不会被固定。这也许是对抗禁书的唯一手段。",
            ),
            (
                "ls_4",
                "サイクルを断ち切れるかは分からん。でもな...",
                "I don't know if the cycle can be broken. But --",
                "能否斩断循环，不得而知。但是呢……",
            ),
            (
                "ls_5",
                "うちは六代見てきて、初めて期待してるんよ。あんたにな。",
                "After watching six generations, I'm hoping for the first time. In you.",
                "看了六代人之后，我第一次抱有期待。对你。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(lore_menu)

    # ── ask_identity: Hecatia's past ──
    builder.step(ask_identity)
    builder.conversation(
        [
            (
                "ai_1",
                "ええよ。隠すようなもんでもないし。",
                "Sure. It's not like I'm hiding anything.",
                "好啊。又不是什么需要隐瞒的事。",
            ),
            (
                "ai_2",
                "うちはな、前の紀の終わり頃に生きてた薬師や。\n"
                "災厄の時代...聞いたことあるやろ？",
                "I was a healer who lived near the end of the previous age.\n"
                "The Age of Calamity... you've heard of it, right?",
                "我是上一纪末期的一名药师。\n灾厄的时代……你应该听说过吧？",
            ),
            (
                "ai_3",
                "メシェーラっちゅう細菌が暴走してな。\n"
                "黒い腫物ができて、数日で死ぬ。治療法なんかあらへん。\n"
                "うちの村も、隣の村も、その隣も。全部やられた。",
                "A bacterium called Meshera went out of control.\n"
                "Black swellings, dead in days. No cure.\n"
                "My village, the next one, the one after that. All wiped out.",
                "一种叫梅谢拉的细菌失控了。\n"
                "长出黑色肿块，几天就死。没有治愈方法。\n"
                "我的村子、隔壁的村子、再隔壁的。全部被灭了。",
            ),
            (
                "ai_4",
                "薬草も魔法も祈りも効かへん。毎日人が死ぬ。\n"
                "看取ることしかできへんかった。",
                "Herbs, magic, prayers -- nothing worked. People died every day.\n"
                "All I could do was watch them go.",
                "草药、魔法、祈祷都不管用。每天都有人死去。\n"
                "我能做的只有看着他们离开。",
            ),
            (
                "ai_5",
                "ほんでな、廃墟の奥で見つけたんや。原初のアーティファクト。\n"
                "死の仕組みそのものを記した...何か。",
                "And then, deep in the ruins, I found it. The primordial artifact.\n"
                "Something that described the mechanism of death itself.",
                "然后，在废墟深处我找到了它。原初的神器。\n"
                "记述了死亡机制本身的……某种东西。",
            ),
            (
                "ai_6",
                "死霊術の応用で助かった人もおった。\n"
                "けど、疫病の根治は最後までできへんかった。",
                "Some were saved through the applications of necromancy.\n"
                "But I could never truly cure the plague.",
                "靠死灵术的应用救了一些人。\n但疫病始终没能根治。",
            ),
            (
                "ai_7",
                "そうこうしてな、うちの活動が冒涜やー、疫病の原因やー、\n"
                "言われ始めてな。",
                "And before long, people started saying my work was profanity,\n"
                "that I was the cause of the plague.",
                "没过多久，人们开始说我的行为是亵渎，\n说疫病就是我造成的。",
            ),
            (
                "ai_8",
                "最後にはな...世界中敵だらけや。一人で対抗するしかなかった。\n"
                "昇華の儀式の素材は、疫病のおかげでそこら中にあった。\n"
                "……あとは、お察しの通りや。",
                "In the end... I'd made enemies of the whole world. Had to fight alone.\n"
                "The materials for the apotheosis ritual were everywhere, thanks to the plague.\n"
                "...I'm sure you can figure out the rest.",
                "最后嘛……全世界都成了敌人。只能一个人对抗了。\n"
                "升华仪式的素材嘛，拜疫病所赐到处都是。\n"
                "……剩下的，你应该能猜到。",
            ),
            (
                "ai_9",
                "……けど、もう随分昔の話や。紀がまるまる一つ変わるくらいにはな。",
                "...But that was a very long time ago. An entire age has passed since then.",
                "……不过那已经是很久以前的事了。久到连纪元都整个换了一轮。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(lore_menu)

    # ── who_question ──
    builder.step(who_question)
    builder.say(
        "wq_1",
        "うちはランプの魔神やと思ってもらえばええで。\n"
        "かといって、別に自由は求めとらんけどな。\n"
        "うちの魔法を広める。頼まれたもんも用意する。今は、それだけが生きがいや。",
        "Think of me as a genie in a lamp. \n"
        "Not that I'm asking to be set free, though. \n"
        "Spreading my magic, preparing what's asked for -- that's all I live for.",
        actor=hecatia,
        text_cn="把我当成灯中精灵就好。\n"
        "倒也不是在追求自由啦。\n"
        "传播我的魔法，准备被拜托的东西。现在这就是全部的意义了。",
    )
    builder.jump(ask_menu)

    # ── voice_question ──
    builder.step(voice_question)
    builder.conversation(
        [
            (
                "vq_2",
                "それ、うちちゃうよ。うちの居候やな。",
                "Wasn't me. That'd be my freeloader.",
                "那不是我。是我这里的寄居者啦。",
            ),
            (
                "vq_3",
                "勝手に住み着いとるだけや。うちも、直接の面識はそこまでないねんけどな。",
                "We're not exactly close. Just squatting in my pages, really.",
                "自己住进来的而已。我和它也不算太熟就是了。",
            ),
        ],
        actor=hecatia,
    )
    builder.jump(ask_menu)

    # ── lore_erenos: post-reveal, first time full comedy scene ──
    builder.step(lore_erenos)
    builder.resolve_run(CommandKeys.ERENOS_ENSURE_NEAR_PLAYER)
    builder.branch_if(FlagKeys.ERENOS_BORROWED, "==", 1, lore_erenos_short)
    builder.conversation(
        [
            (
                "le_1",
                "あー、あいつな。おるよ。居候やし。",
                "Oh, him? He's still here. Freeloader, y'know.",
                "啊，那家伙啊。还在呢。寄居者嘛。",
            ),
            (
                "le_2",
                "おーい、エレノスー。出てきー。",
                "Oi, Erenos! Get out here!",
                "喂——艾雷诺斯——出来——",
            ),
        ],
        actor=hecatia,
    )
    builder.wait(0.3)
    builder.say(
        "le_3",
        "……あいつほんま呼んでもすぐ来んわ。",
        "...That guy never comes when you call.",
        actor=hecatia,
        text_cn="……那家伙叫了也不马上来。",
    )
    builder.say(
        "le_4",
        "おら！ 出てこんかったら明日からパン買いに行かすで！",
        "Hey! If you don't come out, I'm sending you on bread runs starting tomorrow!",
        actor=hecatia,
        text_cn="喂！再不出来明天开始给我去买面包！",
    )
    builder.conversation(
        [
            (
                "le_5",
                "禁書の頁が慌ただしくめくれた。\n"
                "影が飛び出してくる。だが...5代目の荘厳な姿ではない。\n"
                "小柄な女性の輪郭。頬を膨らませている。",
                "The tome's pages flip frantically. \n"
                "A shadow leaps out. But -- not the solemn figure of the fifth. \n"
                "A petite woman's silhouette. Cheeks puffed in annoyance.",
                "禁书的书页慌忙翻动。\n"
                "一个影子跳了出来。然而——并非第五代庄严的身姿。\n"
                "娇小女性的轮廓。鼓着腮帮子。",
            ),
        ],
        actor=narrator,
    )
    builder.conversation(
        [
            (
                "le_6",
                "……先輩。その脅しはもう17回目です。",
                "...Senpai. That's the seventeenth time you've made that threat.",
                "……前辈。这种威胁已经是第十七次了。",
            ),
        ],
        actor=erenos,
    )
    builder.conversation(
        [
            (
                "le_7",
                "数えとんの？ 暇やなあ。ほら、6代目に挨拶しい。",
                "You've been counting? You really are bored. Go on, say hi to the sixth.",
                "你还在数？真闲啊。快，跟第六代打个招呼。",
            ),
        ],
        actor=hecatia,
    )
    builder.conversation(
        [
            (
                "le_8",
                "……どうも。かつて「汝はもう……私だ」などと偉そうに語った者です。\n"
                "あの台詞は撤回させてください。",
                '...Hello. I\'m the one who grandly declared "You are already... me." '
                "I'd like to retract that line, please.",
                "……你好。在下就是曾经大言不惭说出「汝已然……成为我」之人。\n"
                "那句台词请允许在下收回。",
            ),
            (
                "le_9",
                "……こんな辱めを受けるのは生まれて初めてだ。\n"
                "死霊術とは、かくも邪悪な術だったのだな。",
                "...This is the first humiliation I've ever experienced in my life. \n"
                "Necromancy truly is a wicked art.",
                "……这是在下有生以来第一次受此等屈辱。\n死灵术竟是如此邪恶的术法。",
            ),
        ],
        actor=erenos,
    )
    builder.conversation(
        [
            (
                "le_10",
                "こいつ暇で愚痴ばっかり言うねん。良かったら連れてってくれへん？ 貸したるから。",
                "This one just complains all day 'cause he's bored. Wanna take him off my hands? I'll lend him to you.",
                "这家伙闲得只会抱怨。愿意的话带走好不好？借给你。",
            ),
        ],
        actor=hecatia,
    )
    builder.conversation(
        [
            (
                "le_11",
                "先輩、僕はモノではありません。",
                "Senpai, I am not an object.",
                "前辈，在下并非物品。",
            ),
        ],
        actor=erenos,
    )
    builder.say(
        "le_12",
        "居候がでかい口叩くな。",
        "Freeloaders don't get to talk back.",
        actor=hecatia,
        text_cn="寄居者少在这里大放厥词。",
    )
    builder.conversation(
        [
            (
                "le_13",
                "エレノスの影は何か言いかけたが...先輩の一睨みで黙った。",
                "Erenos's shadow started to say something -- but one glare from his senior silenced him.",
                "艾雷诺斯的影正要说什么……被前辈一瞪就沉默了。",
            ),
        ],
        actor=narrator,
    )
    builder.say(
        "le_prompt",
        "どうする？",
        "What will you do?",
        actor=hecatia,
        text_cn="怎么样？",
    )
    builder.choice(
        borrow_erenos, "連れて行く", text_en="Take him along", text_cn="带走"
    )
    builder.choice(lore_menu, "やめておく", text_en="Never mind", text_cn="算了")
    builder.on_cancel(lore_menu)

    # ── lore_erenos_short: repeat visit ──
    builder.step(lore_erenos_short)
    builder.resolve_flag(ResolveKeys.ERENOS_IS_BORROWED, FlagKeys.ERENOS_WITH_PLAYER)
    builder.branch_if(FlagKeys.ERENOS_WITH_PLAYER, "==", 1, lore_erenos_short_has)
    builder.say(
        "les_1",
        "エレノスか？ また連れてくか？",
        "Erenos? Wanna take him again?",
        actor=hecatia,
        text_cn="艾雷诺斯？又要带走？",
    )
    builder.choice(
        borrow_erenos, "連れて行く", text_en="Take him along", text_cn="带走"
    )
    builder.choice(lore_menu, "やめておく", text_en="Never mind", text_cn="算了")
    builder.on_cancel(lore_menu)

    # ── lore_erenos_short_has ──
    builder.step(lore_erenos_short_has)
    builder.say(
        "les_2",
        "あいつまだ外におるんやろ。早よ返してや。……嘘やけど。",
        "He's still out there with you, right? Bring him back soon. ...Just kidding.",
        actor=hecatia,
        text_cn="那家伙还在外面吧。快点还回来。……骗你的。",
    )
    builder.jump(lore_menu)

    # ── borrow_erenos ──
    builder.step(borrow_erenos)
    builder.resolve_run(CommandKeys.ERENOS_BORROW)
    builder.set_flag(FlagKeys.ERENOS_BORROWED, 1)
    builder.say(
        "be_1",
        "ほな、よろしゅうな。……あんまりいじめたらあかんで。うちの居候やし。",
        "Right then, take care of him. ...Don't bully him too much. He's my freeloader, after all.",
        actor=hecatia,
        text_cn="那就拜托啦。……别欺负他太狠了。好歹是我的寄居者。",
    )
    builder.jump(lore_menu)

    # ── party_song: consultation → party magic demo ──
    builder.step(party_song)
    builder.say(
        "ps_0",
        "相談か。ええで、何があったん？",
        text_en="Need advice? Sure. What happened?",
        text_cn="想咨询吗？行啊，发生什么了？",
        actor="tg",
    )
    builder.choice(
        ps_stings,
        "仲間たちに「暗い・キモい・くさい・地味」って言われる",
        text_en="My companions call my magic dark, gross, stinky, and boring",
        text_cn="同伴们说我的魔法又暗又恶心又臭又土",
    )
    builder.on_cancel(choices)

    builder.step(ps_stings)
    builder.say(
        "ps_1",
        "うわ、それは刺さるな…。いや、くさくはないやろ。……本当にくさい？",
        text_en="Oof, that stings... Wait, stinky? No way. ...Is it actually stinky?",
        text_cn="哎，这话真扎心……等等，臭？不至于吧……真的有味道吗？",
        actor="tg",
    )
    builder.choice(
        ps_empathy,
        "術式をぜんぶ「黒いモヤ」扱いされるんだ…",
        text_en="They treat all my spells as just 'black haze'...",
        text_cn="他们把我的术式全都当成“黑雾特效”……",
    )
    builder.on_cancel(choices)

    builder.step(ps_empathy)
    builder.say(
        "ps_2",
        "それは否定できへんな。",
        text_en="I can't deny that.",
        text_cn="这个我确实没法否认。",
        actor="tg",
    )
    builder.say(
        "ps_3",
        "うちも昔、気づいたら「だいたい黒」で押し切ってもうてた時期ある。",
        text_en="I had a phase too - before I knew it, I was forcing everything through with 'mostly black.'",
        text_cn="我以前也有那段时期——回过神来，发现自己什么都在用“反正黑就对了”硬撑。",
        actor="tg",
    )
    builder.choice(
        ps_ask_love,
        "どうしたら良いと思う？",
        text_en="What do you think I should do?",
        text_cn="你觉得我该怎么办？",
    )
    builder.on_cancel(choices)

    # ── ps_ask_love: turn toward pride ──
    builder.step(ps_ask_love)
    builder.say(
        "ps_5",
        "ちょっと聞くけど——自分の術のこと、好き？",
        text_en="Let me ask you something - do you love your own craft?",
        text_cn="先问你一句——你喜欢自己的术吗？",
        actor="tg",
    )
    builder.choice(
        ps_proud,
        "もちろんだ。誇りに思う。言うまでもない！",
        text_en="Of course. I'm proud of it. Goes without saying!",
        text_cn="当然。我以此为傲，不言自明！",
    )
    builder.choice(
        ps_proud,
        "好き。でも伝わらないのが悔しい",
        text_en="I do. But it hurts that it doesn't come across.",
        text_cn="喜欢。但传达不出去，真的不甘心。",
    )
    builder.on_cancel(choices)

    # ── ps_proud: affirm + funny anecdote (笑点1) ──
    builder.step(ps_proud)
    builder.say(
        "ps_6",
        "ほら。今の声、ちゃんと誇りあるやん。",
        text_en="See? That voice right there is full of pride.",
        text_cn="你看，你刚才那语气，明明就很有骄傲。",
        actor="tg",
    )
    builder.say(
        "ps_7",
        "術は悪ない。問題は、見せ方と時代がまだ追いついてへんだけや。",
        text_en="Your craft is not the problem. The presentation - and the times - just haven't caught up yet.",
        text_cn="术本身没问题。只是表现方式和这个时代，还没追上你。",
        actor="tg",
    )
    builder.say(
        "ps_8",
        "実はうちもな、昔やらかしてん。",
        text_en="Truth is, I messed this up myself back then.",
        text_cn="其实我当年也翻过车。",
        actor="tg",
    )
    builder.say(
        "ps_9",
        "黒マント・黒ローブ・黒背景で術を披露したら——",
        text_en="I performed in a black cloak, black robe, black backdrop - and then...",
        text_cn="我那时黑披风、黑长袍、黑背景地上台施术——结果……",
        actor="tg",
    )
    builder.say(
        "ps_10",
        "自分で召喚した雪プチに「ご主人様……？どこですか……？」っていわれてもうた。",
        text_en="Even the Snow Puti I summoned asked, 'Master...? Where are you...?'",
        text_cn="连我自己召出来的雪噗奇都问我：‘主人……？您在哪……？’",
        actor="tg",
    )
    builder.say(
        "ps_11",
        "闇の魔術師が闇に溶けてどうすんねん。存在意義の危機や。",
        text_en="What kind of dark mage disappears into the dark? That's an existential crisis.",
        text_cn="黑魔法师自己融进黑里算什么事？这都存在意义危机了。",
        actor="tg",
    )
    builder.choice(
        ps_reframe,
        "それは……深刻な問題だ",
        text_en="That's... a serious problem.",
        text_cn="这……确实是严重问题。",
    )
    builder.on_cancel(choices)

    # ── ps_reframe: lesson + quiz setup (笑点2 フリ) ──
    builder.step(ps_reframe)
    builder.say(
        "ps_12",
        "あの日学んだのは一つだけ。",
        text_en="I learned one thing that day.",
        text_cn="那天我只学会了一件事。",
        actor="tg",
    )
    builder.say(
        "ps_13",
        "「黒は映えるけど、黒の上で黒は消える」。",
        text_en="Black pops - until you put black on black, then it vanishes.",
        text_cn="黑色本来很出彩——但黑叠黑，只会消失。",
        actor="tg",
    )
    builder.say(
        "ps_14",
        "術の中身はそのまま。舞台だけ変えたる。",
        text_en="Keep the spell itself. Change only the stage.",
        text_cn="术的内核不动，只换舞台。",
        actor="tg",
    )
    builder.say(
        "ps_15",
        "あんた、死霊術師がもっとも苦手としてきたもの、知ってるか？",
        text_en="You know what necromancers have historically been worst at?",
        text_cn="你知道死灵术师历史上最不擅长的是什么吗？",
        actor="tg",
    )
    builder.choice(
        ps_pastel,
        "聖属性？",
        text_en="Holy element?",
        text_cn="圣属性?",
    )
    builder.choice(
        ps_pastel,
        "日光？",
        text_en="Sunlight?",
        text_cn="阳光？",
    )
    builder.on_cancel(choices)

    # ── ps_pastel: punchline + gap theory (笑点2 オチ) ──
    builder.step(ps_pastel)
    builder.wait(1.0)
    builder.say(
        "ps_16",
        "パステルカラーや。",
        text_en="Pastel colors.",
        text_cn="粉彩色。",
        actor="tg",
    )
    builder.say(
        "ps_18",
        "でもな、逆に考えてみ。死霊術師が急にピンク出したら——",
        text_en="But think about it the other way. If a necromancer suddenly throws pink -",
        text_cn="但你反过来想。死灵术师要是突然甩出粉色——",
        actor="tg",
    )
    builder.say(
        "ps_19",
        "みんな「え？」ってなるやろ。そのギャップが武器になるんよ。",
        text_en="Everyone goes, 'Huh?' That contrast becomes your weapon.",
        text_cn="大家都会“诶？”一下。这个反差，就是武器。",
        actor="tg",
    )
    builder.choice(
        ps_react_a,
        "それはもう魔法というよりエンタメでは",
        text_en="Isn't that entertainment more than magic?",
        text_cn="这已经更像演出而不是魔法了吧。",
    )
    builder.choice(
        ps_react_b,
        "おもしろそう。でも死霊術の誇りは？",
        text_en="Sounds fun. But what about necromancer pride?",
        text_cn="听起来挺有趣。但死灵术的骄傲呢？",
    )
    builder.on_cancel(choices)

    # ── ps_react_a ──
    builder.step(ps_react_a)
    builder.say(
        "psa_1",
        "エンタメ上等。先に注目を奪う。理解はその後でええ。",
        text_en="Entertainment is fine. Seize their eyes first. Understanding can come later.",
        text_cn="演出就演出，没问题。先把视线夺过来，理解放后面。",
        actor="tg",
    )
    builder.jump(ps_demo)

    # ── ps_react_b ──
    builder.step(ps_react_b)
    builder.say(
        "psb_1",
        "誇りはあるよ。あるから見せ方にも手を抜かんのや。",
        text_en="We do have pride. That's exactly why we don't cut corners on presentation.",
        text_cn="骄傲当然有。正因为有，才更不能在表现上偷懒。",
        actor="tg",
    )
    builder.say(
        "psb_2",
        "中身で勝って、演出でも勝つ。それが本物の誇りやろ。",
        text_en="Win on substance, and win on presentation too. That's real pride.",
        text_cn="内容要赢，演出也要赢。那才是真正的骄傲。",
        actor="tg",
    )
    builder.jump(ps_demo)

    # ── ps_demo: the party magic demonstration ──
    builder.step(ps_demo)
    builder.say(
        "psm_1",
        "ええか。「暗い・キモい・くさい・地味」——",
        text_en="Listen. 'Dark, creepy, stinky, bland' -",
        text_cn="听好了。‘暗、怪、臭、土’——",
        actor="tg",
    )
    builder.resolve_run("cmd.hecatia.set_party_portrait")
    builder.say(
        "psm_2",
        "見せたる。ないがしろにされてきた、死霊術の真のポテンシャルをな。",
        text_en="I'll show you the true potential of necromancy that's been underestimated for too long.",
        text_cn="我给你看看，被长期轻视的死灵术真正潜力。",
        actor="tg",
    )
    # Execute show/bgm before the final line to avoid being skipped by immediate choice transition.
    builder.resolve_run(CommandKeys.HECATIA_PARTY_SHOW)
    builder.play_bgm_with_fallback(BGM.HECATIA_RAP, BGM.BATTLE)
    builder.wait(3.0)
    builder.say(
        "psm_3",
        "極彩色に溺れるがいい。「送魂祭（ソウルフェス）」 エンチャントォォォオオオ！",
        text_en="Drown in riotous color! 'Soulsend Festival' - ENCHANTOOOO!",
        text_cn="沉溺于极彩吧！「送魂祭」——附魔啊啊啊啊！",
        actor="tg",
    )
    builder.choice(
        ps_punchline,
        "……え、ちょっと待って今の何",
        text_en="...Wait, hold on - what was that?",
        text_cn="……等下，刚才那是什么？",
    )
    builder.on_cancel(choices)

    # ── ps_punchline: deadpan callback (笑点3) ──
    builder.step(ps_punchline)
    builder.say(
        "psm_4",
        "死霊術や。",
        text_en="Necromancy.",
        text_cn="死灵术。",
        actor="tg",
    )
    builder.say(
        "psm_5",
        "——ピンク入りの。",
        text_en="- With a touch of pink.",
        text_cn="——带粉色的。",
        actor="tg",
    )
    builder.choice(
        ps_epilogue,
        "……反論の余地がない",
        text_en="...No room to argue.",
        text_cn="……无从反驳。",
    )
    builder.choice(
        ps_epilogue,
        "これ、仲間に見せたい",
        text_en="I want to show this to my companions.",
        text_cn="这个我想给同伴们看。",
    )
    builder.on_cancel(choices)

    # ── ps_epilogue ──
    builder.step(ps_epilogue)
    builder.say(
        "psm_6",
        'せやろ。"黒いモヤ"にも祭りの夜は来るんやで。',
        text_en='See? Even "black haze" gets its festival night.',
        text_cn="对吧？就连“黑雾”也会迎来属于它的祭典之夜。",
        actor="tg",
    )
    builder.jump(choices)

    # ── end ──
    builder.step(end)
    builder.finish()
