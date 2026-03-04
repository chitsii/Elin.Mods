from drama.data import DramaIds

from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_hecatia_talk_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    ars_hecatia = d.chara("ars_hecatia")
    narrator = d.chara("narrator")
    ars_erenos_pet = d.chara("ars_erenos_pet")

    d.node(
        "main",
        *d.seq(
            r.resolve_flag("state.quest.is_complete", "chitsii.ars.quest.complete"),
            d.switch_on_flag(
                "chitsii.ars.event.hecatia_revealed",
                cases=[("==", 1, "post_reveal_greeting")],
                actor="pc",
            ),
            d.switch_on_flag(
                "chitsii.ars.quest.complete",
                cases=[("==", 1, "reveal_scene")],
                actor="pc",
            ),
            d.go("normal_greeting"),
        ),
    )

    d.node(
        "normal_greeting",
        *d.seq(
            d.line(
                "いらっしゃい、六代目。ゆっくり見てってや。",
                actor=ars_hecatia,
                en="Welcome, Sixth. Take your time browsing.",
                cn="来啦，第六代。慢慢看吧。",
            ),
            d.go("choices"),
        ),
    )

    d.node(
        "post_reveal_greeting",
        *d.seq(
            d.line(
                "ほな、今日は何がいるん？",
                actor=ars_hecatia,
                en="So, what do you need today?",
                cn="那，今天需要什么？",
            ),
            d.go("choices"),
        ),
    )

    d.node(
        "reveal_scene",
        *d.seq(
            d.raw(
                {
                    "action": "addActor",
                    "actor": "narrator",
                    "text_JP": "禁書",
                    "text_EN": "The Tome",
                    "text": "The Tome",
                }
            ),
            d.scene_open(1.0, color="black"),
            d.play_bgm("BGM/ManuscriptByCandlelight"),
            d.wait(0.5),
            d.lines(
                [
                    (
                        "禁断の書から商人を召喚した。だが...いつもと様子が違う。",
                        "You summon the merchant from the tome. But -- something is different.",
                        "从禁书中召唤出了商人。然而……与平时不同。",
                    ),
                    (
                        "ヘカティアの輪郭が揺らいでいる。まるで頁の文字が人の形をとったように。",
                        "Hecatia's outline wavers. As if the letters on the pages took human form.",
                        "赫卡提亚的轮廓在摇曳。仿佛书页上的文字化为了人形。",
                    ),
                ],
                actor=narrator,
            ),
            d.wait(0.5),
            d.go("reveal_2"),
        ),
    )

    d.node(
        "reveal_2",
        *d.seq(
            d.lines(
                [
                    (
                        "ようこそ、こちら側へ。思ったより静かやろ？",
                        "Welcome to the other side. Quieter than you expected, right?",
                        "欢迎来到这一侧。比想象中安静吧？",
                    ),
                    (
                        "……ふふ。その顔。",
                        "...Heh. That look on your face.",
                        "……呵呵。你这表情。",
                    ),
                    (
                        "もう隠してもしゃあないか。あんたはもう、こちら側の人間やし。",
                        "No point hiding it anymore, I suppose. You're one of us now.",
                        "再藏也没意义了嘛。你已经是这一侧的人了。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.wait(0.3),
            d.shake(),
            d.lines(
                [
                    ("うちが初代や。", "I'm the first.", "我就是初代。"),
                    (
                        "名前が残ってへんのは、文字通り本になったからやねん。\n……笑えるやろ？ 笑ってええよ。",
                        "My name's gone 'cause I literally became the book. \n...Funny, right? Go ahead and laugh.",
                        "名字没有留下来，是因为我字面意义上变成了书嘛。\n……好笑吧？笑就笑呗。",
                    ),
                    (
                        "「見つけた」のはうちや。原初のアーティファクト。\nほんで最初の昇華の儀式をやってん。",
                        "I'm the one who found it. The primordial artifact. \nAnd I performed the first apotheosis ritual.",
                        "「发现」它的人是我。原初的神器。\n然后我施行了第一次升华仪式。",
                    ),
                    (
                        "結果は...まあ、見ての通り。「成功しすぎた」んやね。\n力を得る代わりに、アーティファクトと融合してもうた。",
                        "The result -- well, you can see for yourself. 'Succeeded too well.' \nInstead of gaining power, I fused with the artifact.",
                        "结果嘛……你也看到了。「成功过头了」呗。\n没有得到力量，反而与神器融为一体了。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.wait(0.5),
            d.go("reveal_epilogue"),
        ),
    )

    d.node(
        "reveal_epilogue",
        *d.seq(
            d.lines(
                [
                    (
                        "あんたらが感じてた人格漂流な、あれ、ほとんどはうちの視座やで。\n五代分のフィルター通ってるから、ぼやけてるけど。",
                        "That personality drift you've all been feeling? That's all my perspective. \nFiltered through five generations, so it's a bit blurry.",
                        "你们感受到的人格漂流啊，那基本上都是我的视角。\n经过了五代人的滤镜，所以模糊了些。",
                    ),
                    (
                        "禁書の本質は取引や。知識と引き換えに、人格を少しずつ侵す。\nうちはその最初の客。代金は...まあ、うちの全部やね。",
                        "The tome's nature is a transaction. Knowledge in exchange for slow erosion of self. \nI was the first customer. The price was -- well, everything.",
                        "禁书的本质是交易。以知识为代价，一点点侵蚀人格。\n我是第一个客人。代价嘛……就是我的一切。",
                    ),
                    (
                        "「七代目は、問いを持つ者であれ」...あれ書いたん、うちや。\nあんたが禁書を開いた時に震えたんも、うち。嬉しかってん。",
                        "'Let the seventh carry questions' -- I wrote that. \nWhen you opened the tome and it trembled? That was me. I was happy.",
                        "「愿第七代，是持有疑问之人」……那是我写的。\n你翻开禁书时它颤抖了吧？那是我。我很高兴。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.wait(0.3),
            d.lines(
                [
                    (
                        "あんたは七番目になるかもしれへん。\nそれとも六番目で終わるかもしれへん。",
                        "You might become the seventh. Or you might end as the sixth.",
                        "你也许会成为第七代。\n也许就止步于第六代。",
                    ),
                    (
                        "どっちでもええねん。うちはもう、どうでもええわ。\n……嘘やけど。嘘つけへんのやった。禁書の一部やからな。",
                        "Either way's fine by me. I don't care anymore. \n...That's a lie. I can't lie -- I'm part of the tome, after all.",
                        "哪边都无所谓。我已经不在乎了。\n……骗你的。我没法撒谎——毕竟我是禁书的一部分嘛。",
                    ),
                    (
                        "ほな、買い物でもしてき。いつも通りに。\nうちはずっとここにおるし。文字通り。",
                        "Anyway, go do some shopping. Business as usual. \nI'll be right here. Literally.",
                        "好啦，去逛逛买点东西吧。照常就行。\n我一直都在这里。字面意义上的。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.set_flag("chitsii.ars.event.hecatia_revealed", 1, actor=None),
            d.fade_in(1.0, color="black"),
            d.go("choices"),
        ),
    )

    d.node(
        "choices",
        *d.seq(
            d.line(
                "何にする？", actor=ars_hecatia, en="What'll it be?", cn="想要什么？"
            ),
            d.choice_block(
                [
                    d.option(
                        "聞きたいことがある",
                        "ask_menu",
                        en="I have a question",
                        cn="有事想问",
                    ),
                    d.option(
                        "死霊術の概要を知りたい",
                        "spell_menu",
                        en="I want an overview of necromancy",
                        cn="我想了解死灵术概要",
                    ),
                    d.option(
                        "死霊術呪文の戦術指南をしてほしい",
                        "guidebook_info",
                        en="I want tactical guidance for necromancy spells",
                        cn="请给我死灵术咒文的战术指南",
                    ),
                ]
            ),
            d.choice_block(
                [
                    d.option(
                        "禁書について聞く",
                        "lore_menu",
                        if_expr="hasFlag,chitsii.ars.event.hecatia_revealed",
                        en="Ask about the tome",
                        cn="询问禁书",
                    ),
                    d.option("取引する", "_buy", en="Trade", cn="交易"),
                    d.option("去る", "end", en="Leave", cn="离开"),
                ],
                cancel="end",
            ),
        ),
    )

    d.node(
        "guidebook_info",
        *d.seq(
            d.lines(
                [
                    (
                        "ああ、『死霊術師のための戦術指南』のことやね。\n死霊術の戦い方を、実戦向けにまとめた本や。",
                        "Ah, you mean 'A Necromancer's Tactical Guide.' \nIt's a practical manual on how to fight with necromancy.",
                        "啊，你是说《死灵术师的战术指南》吧。\n那是一本把死灵术实战思路整理好的手册。",
                    )
                ],
                actor=ars_hecatia,
            ),
            d.line(
                "どうする？",
                actor=ars_hecatia,
                en="What do you want?",
                cn="你想怎么做？",
            ),
            d.choice_block(
                [
                    d.option(
                        "概要だけ教えて",
                        "guidebook_summary",
                        en="Give me the short version",
                        cn="只讲概要",
                    ),
                    d.option(
                        "本で詳しく読む",
                        "guidebook_buy",
                        en="Read the full book",
                        cn="去读完整书",
                    ),
                    d.option("今はいい", "choices", en="Not now", cn="现在先不用"),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "guidebook_summary",
        *d.seq(
            d.lines(
                [
                    (
                        "ほな、口頭で超圧縮版や。\n勝ち筋は三本柱。間接制圧・従者運用・死体経済。\n即効火力で押し切る流派やない。長期戦ほど強い。\n開戦前は「魂魄保存」か「魂縛の檻」で魂回収ラインを敷く。\n同時に「屍体保存」で素材供給を止めへんこと。",
                        "Alright, here's the ultra-compressed oral version.\nThree pillars: indirect control, servant operation, and corpse economy.\nThis style is not burst-first; it gets stronger in longer fights.\nBefore combat, cast Preserve Soul/Soul Snare to establish soul intake.\nThen keep Preserve Corpse up so materials never run dry.",
                        "那就先给你口头超压缩版。\n三支柱：间接压制、仆从运用、尸体经济。\n这套不是瞬间爆发，战斗越长越有利。\n开战前先用灵魂封存/灵魂囚笼铺好回收线。\n再维持保存尸体，别让素材断档。",
                    ),
                    (
                        "本番の回しはこうや。\n「黄泉の泥濘」で速度を奪い、「恐怖」で危険個体を止める。\n「衰弱の呪い」「衰弱の呪い（重）」「疫病の手」で敵性能を削る。\n「死軍号令」で軍団火力を引き上げ、「骸骨壁」で前線を保つ。\n死体が溜まったら「屍鎖爆砕」で攻撃・妨害・回復を一手で回収。\n要するに、盤面を腐らせてから連鎖で刈り取るんや。",
                        "Your live rotation goes like this.\nUse Grave Quagmire to steal tempo, then pin dangerous targets with Terror.\nKeep shaving stats with Curse of Weakness/Frailty and Plague Touch.\nRaise legion output with Funeral March, stabilize frontlines with Wall of Skeleton.\nWhen corpses stack up, Corpse Chain Burst cashes them into offense/control/sustain.\nIn short: rot the board first, then harvest with chains.",
                        "实战循环是这样。\n先用黄泉泥泞夺节奏，再用恐惧点停关键威胁。\n用衰弱诅咒/重衰弱诅咒/瘟疫之触持续拆参数。\n死军号令抬军团输出，骸骨之墙稳前线。\n尸体堆起来后，用尸锁爆砕一手回收进攻/控制/续航。\n总结：先把盘面腐化，再用连锁收割。",
                    ),
                    (
                        "最後に生存の話や。\n「魂の鎖」は致死を従者に肩代わりさせる最終保険。\nただし従者切れ・MP切れ・行動阻害が重なると循環は止まる。\nせやから死霊術は、戦闘中より準備段階で勝負が決まる。\nここまでが口頭版や。細かい優先順位と例外処理は本にまとめてある。",
                        "Final point: survival.\nSoul Bind is your last insurance, letting a servant take lethal for you.\nBut the loop collapses if servants run out, MP dries up, or you get disabled.\nSo necromancy is decided in preparation more than in the fight itself.\nThat's the oral version. Priority tables and edge cases are in the book.",
                        "最后说生存。\n灵魂锁链是终极保险，让仆从替你吃致死伤。\n但仆从断档、法力见底、被控叠加时，循环会直接崩。\n所以死灵术胜负往往在开战前准备阶段就决定了。\n以上是口头版，细优先级和例外处理都在书里。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("choices"),
        ),
    )

    d.node(
        "guidebook_buy",
        *d.seq(
            d.lines(
                [
                    (
                        "ええ？ 売りもんやっちゅうねん。店主の前で立ち読みか？ \n\n...ええよ。しょうがないにゃあ...",
                        "Huh? This is merchandise, y'know. Reading it right in front of the shopkeeper?\n\n...Fine. Can't be helped...",
                        "诶？这是要卖的商品耶。当着店主的面白看吗？\n\n……行吧。真拿你没办法……",
                    )
                ],
                actor=ars_hecatia,
            ),
            d.raw(
                {
                    "action": "invoke*",
                    "param": "show_book(Book/ars_hecatia_guide)",
                    "actor": "pc",
                }
            ),
            d.go("choices"),
        ),
    )

    d.node(
        "spell_menu",
        *d.seq(
            d.line(
                "何が知りたいん？ うちで分かることやったら教えたるで。",
                actor=ars_hecatia,
                en="What do you want to know? I'll tell you what I can.",
                cn="想知道什么？我知道的都可以告诉你。",
            ),
            d.choice_block(
                [
                    d.option("魂の術", "spell_soul", en="Soul magic", cn="灵魂之术"),
                    d.option(
                        "従者の扱い",
                        "spell_servant",
                        en="Handling servants",
                        cn="仆从的使用",
                    ),
                    d.option(
                        "呪いと防御",
                        "spell_curse",
                        en="Curses and defense",
                        cn="诅咒与防御",
                    ),
                    d.option(
                        "蘇生の仕組み",
                        "ritual_detail",
                        en="How resurrection works",
                        cn="复活的机制",
                    ),
                    d.option(
                        "従者強化の仕組み",
                        "enhance_detail",
                        en="How enhancement works",
                        cn="仆从强化的机制",
                    ),
                    d.option(
                        "鶏が蘇生に使えると聞いた",
                        "chicken_question",
                        en="I heard chickens work for resurrection",
                        cn="听说鸡可以用来复活",
                    ),
                    d.option("もういい", "choices", en="Never mind", cn="算了"),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "spell_soul",
        *d.seq(
            d.lines(
                [
                    (
                        "魂の術はな、死霊術の根幹や。殺す術やない...死と生の境界を操る術や。",
                        "Soul magic is the foundation of necromancy. It's not about killing -- it's about manipulating the boundary between life and death.",
                        "灵魂之术啊，是死灵术的根基。不是杀戮之术……而是操控生死边界的术。",
                    ),
                    (
                        "「魂魄保存」は基本中の基本。敵の魂を保存して、素材にする。",
                        '"Preserve Soul" is the most basic spell. You preserve enemy souls and use them as materials.',
                        "「灵魂封存」是基础中的基础。保存敌人的灵魂，用作素材。",
                    ),
                    (
                        "「魂縛の檻」はそのエリア版。周囲をまとめて保存できる。忙しい時に便利やで。",
                        '"Soul Snare" is the area version. You can preserve everything nearby at once. Handy when you\'re busy.',
                        "「灵魂囚笼」是它的范围版。可以一次性保存周围的全部灵魂。忙的时候很方便呢。",
                    ),
                    (
                        "「魂の鎖」はうちのお気に入り。あんたと従者の魂を繋いで、致命傷を肩代わりさせるんや。",
                        '"Soul Bind" is my favorite. It links your soul with your servant\'s, letting them take fatal blows for you.',
                        "「灵魂锁链」是我的最爱。将你和仆从的灵魂相连，让仆从替你承受致命伤。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("spell_menu"),
        ),
    )

    d.node(
        "spell_servant",
        *d.seq(
            d.lines(
                [
                    (
                        "従者はな、ただの操り人形やない。魂の残響...生前の意志の欠片や。",
                        "Servants aren't mere puppets. They're echoes of a soul -- fragments of the will they had in life.",
                        "仆从啊，不是单纯的提线木偶。是灵魂的残响……生前意志的碎片。",
                    ),
                    (
                        "「アンデッド召喚」は手軽な一時兵。消えるけど、使い捨てと割り切るんが大事。",
                        '"Summon Undead" gives you quick temporary troops. They vanish, so think of them as disposable.',
                        "「召唤亡灵」可以快速获得临时兵力。虽然会消失，但当成消耗品就对了。",
                    ),
                    (
                        "本気で育てるなら、「屍体保存」で素材を確保して、儀式で蘇らせ。手間かけた分だけ強なる。",
                        'If you\'re serious about raising one, use "Preserve Corpse" to secure materials, then resurrect it through the ritual. The more effort you put in, the stronger they become.',
                        "要认真培养的话，先用「保存尸体」确保素材，然后通过仪式复活。花越多功夫就越强。",
                    ),
                    (
                        "「従者強化」で底上げもできる。大事にしたってな。……まあ、消耗品扱いしても怒らへんけど。",
                        'You can also boost them with "Servant Enhancement". Take good care of them. ...Well, I won\'t be mad if you treat them as expendable, though.',
                        "也可以用「仆从强化」来提升底力。好好珍惜吧。……嘛，当消耗品用我也不会生气就是了。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("spell_menu"),
        ),
    )

    d.node(
        "spell_curse",
        *d.seq(
            d.lines(
                [
                    (
                        "死霊術にな、ドカンとぶっ放す呪文はあらへん。性分やないんよ。",
                        "Necromancy doesn't have any big flashy attack spells. It's just not in its nature.",
                        "死灵术啊，没有轰的一声放出去的咒语。不对路嘛。",
                    ),
                    (
                        "代わりに、相手の戦意を奪う。\n「衰弱の呪い」「衰弱の呪い（重）」「恐怖」「疫病の手」…じわじわ蝕むんが流儀や。",
                        'Instead, you drain the enemy\'s will to fight.\n"Curse of Weakness", "Curse of Frailty", "Terror", and "Plague Touch" -- slowly wearing them down is the way.',
                        "取而代之的是夺去对手的战意。\n「衰弱诅咒」「衰弱诅咒（重）」「恐惧」「瘟疫之触」……慢慢侵蚀才是死灵术的流派。",
                    ),
                    (
                        "自分を守るなら「骸骨壁」と「死の領域」。あと吸収術で相手の精気を奪い取る。",
                        'For defense, use "Wall of Skeleton" and "Death Zone". And drain the enemy\'s vitality with absorption spells.',
                        "要保护自己就用「骸骨之墙」和「死亡领域」。再用吸收术夺取对手的精气。",
                    ),
                    (
                        "地味やけどな、死なへん奴が結局一番強いねん。覚えときや。",
                        "It's not flashy, but the one who doesn't die is the strongest in the end. Remember that.",
                        "虽然不起眼，但不会死的家伙终究是最强的。记住这一点。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("spell_menu"),
        ),
    )

    d.node(
        "ritual_detail",
        *d.seq(
            d.lines(
                [
                    (
                        "蘇生はな、屍体の質がまず大事や。\n鶏の屍体にどんだけ魂注いでも、鶏は鶏や。",
                        "First thing about resurrection -- the corpse's quality matters. \nNo matter how many souls you pour into a chicken corpse, it's still a chicken.",
                        "复活嘛，首先尸体的质量很重要。\n往鸡的尸体里灌再多灵魂，鸡还是鸡。",
                    ),
                    (
                        "逆に、竜やら上位種の屍体やったら、ちょっとの魂でもそこそこの力を引き出せる。\n器がでかいんやな。",
                        "On the other hand, a dragon or higher being's corpse can draw decent power \nfrom just a few souls. The vessel is simply larger.",
                        "反过来，龙啊高等种之类的尸体，只用少量灵魂就能引出相当的力量。\n容器大嘛。",
                    ),
                    (
                        "で、魂の数や。多く注ぐほど、生前の力に近づく。\nうちの現役の頃の感覚にはなるんやけど、\n強い魂30個くらい注げば、ほぼ生前と変わらんとこまで戻せるで。",
                        "Now, the number of souls. The more you pour in, the closer \nthey get to their living power. From my own experience back \nin the day, about thirty strong souls will restore them almost completely.",
                        "然后是灵魂的数量。灌得越多，就越接近生前的力量。\n以我当年的经验来说，\n灌入三十个左右的强大灵魂，基本就能恢复到和生前差不多的程度。",
                    ),
                    (
                        "ただし、あんた自身の経験も関係する。\n深い階層まで潜ったことない術者が、大物を完全に操れるわけないやろ？\n身の丈ってやつや。",
                        "But your own experience matters too. A caster who's never ventured \ndeep can't fully control a powerful being, right? \nKnow your limits.",
                        "不过，你自身的经验也有关系。\n没去过深层的术者怎么可能完全操控大家伙？\n量力而行啊。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("spell_menu"),
        ),
    )

    d.node(
        "enhance_detail",
        *d.seq(
            d.lines(
                [
                    (
                        "蘇った従者はな、そのままでも戦えるけど、もっと強くしたいやろ？",
                        "A resurrected servant can fight as is, but you want them stronger, right?",
                        "复活的仆从嘛，虽然原样也能战斗，但想让它更强吧？",
                    ),
                    (
                        "魂を注いで、力とか耐久とか個別に底上げできる。\nただし、同じとこに何度も注ぐと、そのうち染み込まんくなってくる。\n身体が飽和するんやな。まんべんなくやるのがコツや。",
                        "You can inject souls to boost individual stats like strength or endurance. \nBut if you keep pouring into the same stat, it stops soaking in. \nThe body saturates. The trick is to spread it around.",
                        "可以注入灵魂来逐项提升力量、耐久之类的。\n不过同一项灌太多次，慢慢就渗不进去了。\n身体会饱和嘛。均匀分配才是诀窍。",
                    ),
                    (
                        "もっと大胆にやるなら、部位の増設や。\n腕がもう一本欲しいなら、腕のある屍体を素材に持ってき。\n成功するかは運次第やけど……失敗しても諦めんといてな。",
                        "If you want to go bolder, try body augmentation. \nWant an extra arm? Bring a corpse that has arms as material. \nSuccess depends on luck, but... don't give up after a failure.",
                        "想更大胆的话，就试试部位增设。\n想多一条手臂？带一具有手臂的尸体来当素材。\n成功与否看运气……但失败了也别放弃。",
                    ),
                    (
                        "何回か失敗すると、身体のほうが馴染んできて、\n次はちょっと通りやすくなる。共鳴、言うてな。",
                        "After a few failures, the body adapts and becomes more receptive \nnext time. We call it resonance.",
                        "失败几次之后，身体那边就会慢慢适应，\n下次就更容易成功了。叫做共鸣。",
                    ),
                    (
                        "……ただし、やりすぎは禁物。\n強化しすぎると、禁書の力が暴走することがある。\nでもな...暴走の半分は、むしろ当たりや。覚醒とか、突然変異とかな。",
                        "...But don't overdo it. Too much enhancement can cause a rampage \nfrom the tome's power. Though -- half the time, a rampage is actually \na jackpot. Dark awakenings, spontaneous mutations, that sort of thing.",
                        "……不过别过头了。\n强化过度的话，禁书的力量可能会暴走。\n但呢……暴走有一半其实反而是好事。觉醒啦、突变啦之类的。",
                    ),
                    (
                        "要はな、リスク込みで楽しめるかどうかや。\nあんたの腕と覚悟次第やで。",
                        "The point is, can you enjoy it knowing the risks? \nIt all depends on your skill and resolve.",
                        "总之呢，能不能连风险一起享受，\n就看你的本事和觉悟了。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("spell_menu"),
        ),
    )

    d.node(
        "chicken_question",
        *d.seq(
            d.lines(
                [
                    (
                        "……はっ！？ あんたそれ知ってるん！？ やるやん！",
                        "...Huh!? You know about that!? Well look at you!",
                        "……哈！？你知道这个！？有两下子嘛！",
                    ),
                    (
                        "いやあ嬉しいわ。うちが現役の頃でも、\nそれ知ってた死霊術師は片手で数えるほどやで。\nあんた才能あるわ。ほんまに。",
                        "Oh, this makes me so happy. Even in my day, the necromancers who \nknew about that could be counted on one hand. \nYou've got real talent. Truly.",
                        "哎呀真高兴。就算在我那个时代，\n知道这个的死灵术士也是屈指可数的。\n你有天赋啊。真的。",
                    ),
                    (
                        "鶏はな...この世界のいちばん根っこにおる生き物やねん。\nどんな存在も、正体が曖昧になると、なぜか鶏に行き着く。\n世界の初期値みたいなもんや。",
                        "Chickens -- they're the creature at the very foundation of this world. \nWhen any being's identity becomes ambiguous, it somehow ends up as a chicken. \nLike the world's default setting.",
                        "鸡啊……是这个世界最根源的生物。\n不管什么存在，一旦身份变得暧昧，就会莫名其妙变成鸡。\n就像世界的默认值一样。",
                    ),
                    (
                        "せやから蘇生の器としてはこの上なく寛容でな。\nどんな魂でも拒まへん。受け入れて、鶏として蘇る。",
                        "That's why they're incomparably tolerant as a vessel for resurrection. \nThey won't reject any soul. They'll accept it and rise again -- as a chicken.",
                        "所以作为复活的容器，鸡极其宽容。\n任何灵魂都不会拒绝。接纳之后，以鸡的形态复活。",
                    ),
                    (
                        "……まあ、蘇っても鶏は鶏やけどな。そこだけは覚悟しときや。",
                        "...Well, even resurrected, a chicken is still a chicken. \nJust be prepared for that part.",
                        "……不过嘛，就算复活了鸡还是鸡。这一点要有心理准备。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("spell_menu"),
        ),
    )

    d.node(
        "ask_menu",
        *d.seq(
            d.line(
                "ええよ、何でも聞き。うちで分かることやったら答えたるわ。",
                actor=ars_hecatia,
                en="Sure, ask away. I'll answer what I can.",
                cn="好啊，什么都可以问。我知道的都会回答。",
            ),
            d.choice_block(
                [
                    d.option(
                        "六代目ってなに？",
                        "ask_sixth",
                        if_expr="!hasFlag,chitsii.ars.event.hecatia_revealed",
                        en="What does 'the Sixth' mean?",
                        cn="「第六代」是什么意思？",
                    )
                ]
            ),
            d.choice_block(
                [
                    d.option(
                        "あなたは誰？",
                        "who_question",
                        if_expr="!hasFlag,chitsii.ars.event.hecatia_revealed",
                        en="Who are you?",
                        cn="你是谁？",
                    )
                ]
            ),
            d.choice_block(
                [
                    d.option(
                        "禁書の中で声を聞いた",
                        "voice_question",
                        if_expr="!hasFlag,chitsii.ars.event.hecatia_revealed",
                        en="I heard a voice in the tome",
                        cn="我在禁书中听到了声音",
                    ),
                    d.option(
                        "アルス・モリエンディとは？",
                        "ask_ars",
                        en="What is Ars Moriendi?",
                        cn="什么是死亡的艺术？",
                    ),
                ]
            ),
            d.choice_block(
                [
                    d.option(
                        "エレノス・ヴェルデクトって誰？",
                        "ask_erenos",
                        if_expr="!hasFlag,chitsii.ars.event.hecatia_revealed",
                        en="Who is Erenos Verdict?",
                        cn="艾雷诺斯·维尔迪克特是谁？",
                    ),
                    d.option(
                        "アンデッドにも死霊術は効く？",
                        "ask_undead",
                        en="Does necromancy work on undead?",
                        cn="死灵术对亡灵也有效吗？",
                    ),
                    d.option(
                        "機械に魂はある？",
                        "ask_machine",
                        en="Do machines have souls?",
                        cn="机械有灵魂吗？",
                    ),
                    d.option(
                        "ネームドボスも使役できる？",
                        "ask_boss",
                        en="Can I control named bosses?",
                        cn="可以使役命名Boss吗？",
                    ),
                    d.option(
                        "従者の数に制限はある？",
                        "ask_limit",
                        en="Is there a limit on servants?",
                        cn="仆从数量有限制吗？",
                    ),
                    d.option("もういい", "choices", en="Never mind", cn="算了"),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ask_sixth",
        *d.seq(
            d.lines(
                [
                    (
                        "六代目。そのまんまや。この禁書を手に取った六番目の人間ってこと。",
                        "The Sixth. Exactly what it sounds like. \nYou're the sixth person to take up this tome.",
                        "第六代。就是字面意思。拿起这本禁书的第六个人。",
                    ),
                    (
                        "あんたの前に五人おった。\nそれぞれ事情があって、それぞれの結末を迎えた。",
                        "Five came before you. Each had their own reasons, \neach met their own end.",
                        "你之前有五个人。\n各有各的缘由，各有各的结局。",
                    ),
                    (
                        "ちなみにな、この本は誰にでも開けるわけやないんよ。\nあんたが読めてるんは、禁書があんたを選んだからや。",
                        "By the way, not just anyone can open this book. \nYou can read it because the tome chose you.",
                        "话说回来，这本书不是谁都能翻开的。\n你之所以读得了，是因为禁书选中了你。",
                    ),
                    (
                        "詳しいことはそのうち分かるわ。焦らんでええ。",
                        "You'll learn the details in time. No need to rush.",
                        "详情以后慢慢就知道了。别急嘛。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "ask_ars",
        *d.seq(
            d.lines(
                [
                    (
                        "アルス・モリエンディ。死の技法、いうてな。",
                        "Ars Moriendi. 'The art of dying,' so to speak.",
                        "死亡的艺术。所谓死之技法。",
                    ),
                    (
                        "この禁書に記された術の総称や。\n魂を保存し、屍体に注ぎ、死者を蘇らせる...全部これの一部。",
                        "It's the collective name for the arts inscribed in this tome. \nPreserving souls, pouring them into corpses, raising the dead \n-- all part of it.",
                        "是记载于这本禁书中所有术法的总称。\n保存灵魂、注入尸体、复活死者……全都是其一部分。",
                    ),
                    (
                        "世間では禁忌扱いやけど、\nうちに言わせたら一番正直な魔法やで。\n死は嘘つけへんからな。",
                        "The world treats it as taboo, but if you ask me, \nit's the most honest form of magic. \nDeath can't lie, after all.",
                        "世人将其视为禁忌，\n但依我看这是最诚实的魔法。\n死亡是不会撒谎的嘛。",
                    ),
                    (
                        "あんたが今学んどるのは、その技法の端っこや。\n奥はもっと深い。……楽しみにしとき。",
                        "What you're learning now is just the edge of the art. \nIt goes much deeper. ...Look forward to it.",
                        "你现在学的不过是技法的皮毛。\n里面深着呢。……好好期待吧。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "ask_erenos",
        *d.seq(
            d.lines(
                [
                    (
                        "お、五代目の名前知ってるん。やるなあ。",
                        "Oh, you know the Fifth's name? Not bad.",
                        "哦，你知道第五代的名字？不错嘛。",
                    ),
                    (
                        "エレノス・ヴェルデクト。\nあんたの前にこの禁書を持ってた人間や。",
                        "Erenos Verdict. \nThe one who held this tome before you.",
                        "艾雷诺斯·维尔迪克特。\n在你之前持有这本禁书的人。",
                    ),
                    (
                        "頭は切れるけど、ちょっと堅物でな。\n真面目すぎるんが玉に瑕やったわ。",
                        "Sharp mind, but a bit rigid. \nBeing too serious was his one flaw.",
                        "脑子很好使，就是有点死板。\n太一本正经是他唯一的缺点。",
                    ),
                    (
                        "最後にどうなったかは...\nまあ、そのうち分かるで。禁書が全部覚えとるから。",
                        "What happened to him in the end -- \nwell, you'll find out eventually. The tome remembers everything.",
                        "最后怎样了嘛……\n慢慢就会知道的。禁书全都记得。",
                    ),
                    (
                        "あんたがたまに、自分らしくない考え方しぃひん？\nあれ、ちょっとあいつの影響かもな。",
                        "Don't you sometimes think in ways that don't feel like you? \nThat might be a bit of his influence.",
                        "你有没有偶尔出现不像自己的想法？\n那个嘛，可能是受了他一点影响。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "ask_undead",
        *d.seq(
            d.lines(
                [
                    (
                        "ああ、よう聞かれるわ、それ。",
                        "Ah, I get that one a lot.",
                        "啊，这个问题经常被问到。",
                    ),
                    (
                        "死霊術の大原則はな...\n死ねるもんには魂がある。",
                        "The fundamental rule of necromancy -- \nif it can die, it has a soul.",
                        "死灵术的大原则嘛……\n能死的东西就有灵魂。",
                    ),
                    (
                        "アンデッドかて倒されたら終わるやろ？\nほなら魂は出る。それだけの話や。",
                        "Even undead perish when defeated, right? \nThen a soul comes out. Simple as that.",
                        "亡灵被打倒了也会完蛋吧？\n那灵魂就会出来。就这么简单。",
                    ),
                    (
                        "変に遠慮せんでええよ。\n素材に貴賎なし。それが死霊術師や。",
                        "Don't overthink it. \nNo material is beneath a necromancer.",
                        "别想太多嘛。\n素材不分贵贱。这就是死灵术士。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "ask_machine",
        *d.seq(
            d.lines(
                [
                    (
                        "おもろいこと聞くなあ。",
                        "Now that's an interesting question.",
                        "问了个有意思的问题嘛。",
                    ),
                    (
                        "ないで。機械には魂がない。",
                        "No. Machines don't have souls.",
                        "没有。机械没有灵魂。",
                    ),
                    (
                        "機械は壊れるけど、死なへん。\n壊れることと死ぬことは違うんよ。",
                        "Machines break, but they don't die. \nBreaking and dying are different things.",
                        "机械会坏，但不会死。\n坏掉和死掉是不同的。",
                    ),
                    (
                        "死霊術は死と生の境界を操る術やからな。\nその境界がないもんには、手の出しようがないんや。",
                        "Necromancy manipulates the boundary between life and death. \nIf that boundary doesn't exist, there's nothing to work with.",
                        "死灵术是操控生死边界的术法嘛。\n没有那条边界的东西，就无从下手了。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "ask_boss",
        *d.seq(
            d.lines(
                [
                    (
                        "おっ、大きく出たなぁ。ええやん、その心意気。",
                        "Oh, thinking big, are we? I like that spirit.",
                        "哦，口气不小嘛。不错，有这个志气。",
                    ),
                    (
                        "普通の召喚はな、向こうが来てくれるのを待つもんや。\n格が合わんと門前払いされる。",
                        "With regular summoning, you wait for them to come to you. \nIf you're not worthy, they turn you away.",
                        "普通的召唤嘛，是等对方来找你的。\n格不够就会被拒之门外。",
                    ),
                    (
                        "死霊術は逆や。あんたが自分の手で倒したもんが、\nそのまま素材になる。ネームドやろうがボスやろうが関係あらへん。",
                        "Necromancy is the opposite. Whatever you defeat with your own hands \nbecomes your material. Named or boss, doesn't matter.",
                        "死灵术正好相反。你亲手打倒的东西，\n直接变成素材。命名怪也好Boss也好都无所谓。",
                    ),
                    (
                        "もちろん、大物には大物なりの魂が要る。\nけどな...あんたの強さが、そのまま従者の格になるんや。\nこれが死霊術の醍醐味やで。",
                        "Of course, the bigger the prey, the more souls you'll need. \nBut your strength directly becomes your servant's caliber. \nThat's the real beauty of necromancy.",
                        "当然，大家伙需要相应数量的灵魂。\n但是呢……你的强大直接成为仆从的档次。\n这就是死灵术的醍醐味。",
                    ),
                    (
                        "ただしな、大物を従者にしたら、元の居場所にはもう現れへんくなる。\n魂が禁書に繋がるから、世界の側では「もうおらん」扱いになるんよ。",
                        "One thing, though -- once you make a big shot your servant, \nthey won't show up in their old haunt anymore. Their soul's bound \nto the tome, so the world considers them 'gone.'",
                        "不过啊，大家伙变成仆从之后，原来的地方就不会再出现了。\n灵魂连接到禁书上，世界那边就会当作「已经不在了」处理。",
                    ),
                    (
                        "気ぃ変わったら解放したらええ。縁が切れたら、世界が勝手に元の場所に帰すから。\n要は、そいつの存在を世界から借りるいうことや。覚えときや。",
                        "If you change your mind, just release them. Once the bond breaks, \nthe world puts them back where they belong. \nBasically, you're borrowing their existence from the world. Remember that.",
                        "改变主意的话解放就行。断了联系，世界会自动把它送回原来的地方。\n简单来说就是从世界那里借用它的存在。记住这一点。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "ask_limit",
        *d.seq(
            d.lines(
                [
                    (
                        "ここが死霊術のいちばんの売りやねん。聞いて驚き。",
                        "This is necromancy's biggest selling point. Brace yourself.",
                        "这可是死灵术最大的卖点。听好了别吓到。",
                    ),
                    (
                        "普通の召喚は、術者の器が上限やから数に限りがある。\n何体も抱えたら術者がもたへん。",
                        "Regular summoning is capped by the caster's capacity. \nToo many and the caster can't handle it.",
                        "普通的召唤受限于术者的容量，数量有上限。\n抱太多的话术者撑不住。",
                    ),
                    (
                        "けど死霊術の従者は、注ぎ入れた魂が各々器になっとる。\nあんたの器を使わへんから、負荷がかからんのよ。",
                        "But with necromantic servants, each soul you pour in becomes its own vessel. \nThey don't draw on your capacity, so there's no strain on you.",
                        "但死灵术的仆从啊，注入的灵魂各自成为容器。\n不占用你的容量，所以不会有负担。",
                    ),
                    (
                        "つまりな...制限なし。\n魂さえあれば好きなだけ蘇らせらるねん。",
                        "In other words -- no limit. \nAs long as you have souls, raise as many as you like.",
                        "也就是说……没有限制。\n只要有灵魂，想复活多少就复活多少。",
                    ),
                    (
                        "これができるんは死霊術師だけや。\n胸張ってええ特権やで。",
                        "Only necromancers can do this. \nIt's a privilege worth being proud of.",
                        "能做到这一点的只有死灵术士。\n挺起胸膛吧，这是值得骄傲的特权。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "lore_menu",
        *d.seq(
            d.line(
                "何を知りたい？ うちに嘘はつけへんからな。禁書の一部やし。",
                actor=ars_hecatia,
                en="What do you want to know? I can't lie -- I'm part of the tome, after all.",
                cn="想知道什么？我没法撒谎的。毕竟是禁书的一部分嘛。",
            ),
            d.choice_block(
                [
                    d.option(
                        "禁書の正体",
                        "lore_tome",
                        en="The tome's true nature",
                        cn="禁书的真面目",
                    ),
                    d.option(
                        "歴代の継承者",
                        "lore_inheritors",
                        en="Past inheritors",
                        cn="历代继承者",
                    ),
                    d.option(
                        "七代目の予言",
                        "lore_seventh",
                        en="The seventh's prophecy",
                        cn="第七代的预言",
                    ),
                    d.option(
                        "過去のことを聞いてもいいか？",
                        "ask_identity",
                        en="May I ask about your past?",
                        cn="可以问问你过去的事吗？",
                    ),
                ]
            ),
            d.choice_block(
                [
                    d.option(
                        "エレノスの魂はどうなった？",
                        "lore_erenos",
                        if_expr="hasFlag,chitsii.ars.quest.event.erenos_defeated",
                        en="Erenos's soul",
                        cn="艾雷诺斯的灵魂怎样了？",
                    ),
                    d.option("もういい", "choices", en="Never mind", cn="算了"),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "lore_tome",
        *d.seq(
            d.lines(
                [
                    (
                        "禁書はな……うちが見つけた時は、ただのアーティファクトやった。意思なんかあらへん。",
                        "The tome... when I first found it, it was just an artifact. No will of its own.",
                        "禁书啊……我发现它的时候，只是个普通的神器。没有任何意志。",
                    ),
                    (
                        "でも使い続けるうちに、人格を侵し始めた。知識と引き換えにな。意図的な設計やない。副作用や。",
                        "But the more you use it, the more it erodes your personality. In exchange for knowledge. It's not by design -- it's a side effect.",
                        "但用着用着，它开始侵蚀人格了。以知识为代价。不是刻意设计的。是副作用。",
                    ),
                    (
                        "強すぎる知識は持ち主を変えてまう。水が器の形になるように、禁書の知識があんたの形を変える。",
                        "Knowledge too powerful changes its bearer. Like water taking the shape of its vessel, the tome's knowledge reshapes you.",
                        "过于强大的知识会改变持有者。就像水会变成容器的形状，禁书的知识也在改变你的形态。",
                    ),
                    (
                        "あんたも気づいてるやろ？ 最近、昔の自分を思い出しにくくなってへん？",
                        "You've noticed too, haven't you? It's getting harder to remember who you used to be.",
                        "你也察觉到了吧？最近越来越难想起以前的自己了吧？",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("lore_menu"),
        ),
    )

    d.node(
        "lore_inheritors",
        *d.seq(
            d.lines(
                [
                    (
                        "二代目のヴァルディスは医者やった。妻を蘇らせようとして……別のもんが返ってきた。自分で神殿に出頭したわ。",
                        "The second, Valdis, was a doctor. He tried to resurrect his wife... something else came back. He turned himself in to the temple.",
                        "第二代瓦尔迪斯是个医生。想复活妻子……回来的却是别的东西。他自己去神殿自首了。",
                    ),
                    (
                        "三代目のセリューは学者。理論を体系化してくれたけど、自分が誰かわからんくなった。ある日ふっと消えた。",
                        "The third, Seryu, was a scholar. She systematized the theory, but lost track of who she was. One day she just vanished.",
                        "第三代瑟琉是学者。把理论体系化了，但搞不清自己是谁了。某天突然就消失了。",
                    ),
                    (
                        "四代目のミレイユは元神官。「尊厳ある蘇生」を目指してた。一番まともやったかもしれん。",
                        "The fourth, Mireille, was a former priestess. She pursued 'resurrection with dignity.' She might have been the sanest of them all.",
                        "第四代米蕾尤是前神官。追求「有尊严的复活」。也许是最正常的一个。",
                    ),
                    (
                        "全員に共通してるのはな...皆、善意で始めたってことなんよ。禁書を手に取った動機は、いつも正しい目的やった。",
                        "What they all had in common -- they all started with good intentions. The reason for picking up the tome was always righteous.",
                        "所有人的共同点就是……都是出于善意开始的。拿起禁书的动机，每次都是正当的。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("lore_menu"),
        ),
    )

    d.node(
        "lore_seventh",
        *d.seq(
            d.lines(
                [
                    (
                        "「七代目は、問いを持つ者であれ」...あれ書いたん、うちや。",
                        "'Let the seventh carry questions' -- I wrote that.",
                        "「愿第七代，是持有疑问之人」……那是我写的。",
                    ),
                    (
                        "歴代の継承者はみんな、答えを求めた。蘇生の方法、力の秘密、自分の正義。答えを見つけた瞬間、固定される。",
                        "Every inheritor sought answers. How to resurrect, the secret of power, their own justice. The moment they found their answer, they became fixed.",
                        "历代继承者都在寻求答案。复活的方法、力量的秘密、自己的正义。找到答案的瞬间，就被固定了。",
                    ),
                    (
                        "でも問い続ける者は、変わり続ける。固定されへん。それが禁書への唯一の対抗策かもしれん。",
                        "But those who keep questioning keep changing. They can't be fixed. That might be the only counter to the tome.",
                        "但持续追问的人会持续改变。不会被固定。这也许是对抗禁书的唯一手段。",
                    ),
                    (
                        "サイクルを断ち切れるかは分からん。でもな...",
                        "I don't know if the cycle can be broken. But --",
                        "能否斩断循环，不得而知。但是呢……",
                    ),
                    (
                        "うちは六代見てきて、初めて期待してるんよ。あんたにな。",
                        "After watching six generations, I'm hoping for the first time. In you.",
                        "看了六代人之后，我第一次抱有期待。对你。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("lore_menu"),
        ),
    )

    d.node(
        "ask_identity",
        *d.seq(
            d.lines(
                [
                    (
                        "ええよ。隠すようなもんでもないし。",
                        "Sure. It's not like I'm hiding anything.",
                        "好啊。又不是什么需要隐瞒的事。",
                    ),
                    (
                        "うちはな、前の紀の終わり頃に生きてた薬師や。\n災厄の時代...聞いたことあるやろ？",
                        "I was a healer who lived near the end of the previous age.\nThe Age of Calamity... you've heard of it, right?",
                        "我是上一纪末期的一名药师。\n灾厄的时代……你应该听说过吧？",
                    ),
                    (
                        "メシェーラっちゅう細菌が暴走してな。\n黒い腫物ができて、数日で死ぬ。治療法なんかあらへん。\nうちの村も、隣の村も、その隣も。全部やられた。",
                        "A bacterium called Meshera went out of control.\nBlack swellings, dead in days. No cure.\nMy village, the next one, the one after that. All wiped out.",
                        "一种叫梅谢拉的细菌失控了。\n长出黑色肿块，几天就死。没有治愈方法。\n我的村子、隔壁的村子、再隔壁的。全部被灭了。",
                    ),
                    (
                        "薬草も魔法も祈りも効かへん。毎日人が死ぬ。\n看取ることしかできへんかった。",
                        "Herbs, magic, prayers -- nothing worked. People died every day.\nAll I could do was watch them go.",
                        "草药、魔法、祈祷都不管用。每天都有人死去。\n我能做的只有看着他们离开。",
                    ),
                    (
                        "ほんでな、廃墟の奥で見つけたんや。原初のアーティファクト。\n死の仕組みそのものを記した...何か。",
                        "And then, deep in the ruins, I found it. The primordial artifact.\nSomething that described the mechanism of death itself.",
                        "然后，在废墟深处我找到了它。原初的神器。\n记述了死亡机制本身的……某种东西。",
                    ),
                    (
                        "死霊術の応用で助かった人もおった。\nけど、疫病の根治は最後までできへんかった。",
                        "Some were saved through the applications of necromancy.\nBut I could never truly cure the plague.",
                        "靠死灵术的应用救了一些人。\n但疫病始终没能根治。",
                    ),
                    (
                        "そうこうしてな、うちの活動が冒涜やー、疫病の原因やー、\n言われ始めてな。",
                        "And before long, people started saying my work was profanity,\nthat I was the cause of the plague.",
                        "没过多久，人们开始说我的行为是亵渎，\n说疫病就是我造成的。",
                    ),
                    (
                        "最後にはな...世界中敵だらけや。一人で対抗するしかなかった。\n昇華の儀式の素材は、疫病のおかげでそこら中にあった。\n……あとは、お察しの通りや。",
                        "In the end... I'd made enemies of the whole world. Had to fight alone.\nThe materials for the apotheosis ritual were everywhere, thanks to the plague.\n...I'm sure you can figure out the rest.",
                        "最后嘛……全世界都成了敌人。只能一个人对抗了。\n升华仪式的素材嘛，拜疫病所赐到处都是。\n……剩下的，你应该能猜到。",
                    ),
                    (
                        "……けど、もう随分昔の話や。紀がまるまる一つ変わるくらいにはな。",
                        "...But that was a very long time ago. An entire age has passed since then.",
                        "……不过那已经是很久以前的事了。久到连纪元都整个换了一轮。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("lore_menu"),
        ),
    )

    d.node(
        "who_question",
        *d.seq(
            d.line(
                "うちはランプの魔神やと思ってもらえばええで。\nかといって、別に自由は求めとらんけどな。\nうちの魔法を広める。頼まれたもんも用意する。今は、それだけが生きがいや。",
                actor=ars_hecatia,
                en="Think of me as a genie in a lamp. \nNot that I'm asking to be set free, though. \nSpreading my magic, preparing what's asked for -- that's all I live for.",
                cn="把我当成灯中精灵就好。\n倒也不是在追求自由啦。\n传播我的魔法，准备被拜托的东西。现在这就是全部的意义了。",
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "voice_question",
        *d.seq(
            d.lines(
                [
                    (
                        "それ、うちちゃうよ。うちの居候やな。",
                        "Wasn't me. That'd be my freeloader.",
                        "那不是我。是我这里的寄居者啦。",
                    ),
                    (
                        "勝手に住み着いとるだけや。うちも、直接の面識はそこまでないねんけどな。",
                        "We're not exactly close. Just squatting in my pages, really.",
                        "自己住进来的而已。我和它也不算太熟就是了。",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.go("ask_menu"),
        ),
    )

    d.node(
        "lore_erenos",
        *d.seq(
            r.resolve_run("cmd.erenos.ensure_near_player"),
            d.switch_on_flag(
                "chitsii.ars.event.erenos_borrowed",
                cases=[("==", 1, "lore_erenos_short")],
                actor="pc",
            ),
            d.lines(
                [
                    (
                        "あー、あいつな。おるよ。居候やし。",
                        "Oh, him? He's still here. Freeloader, y'know.",
                        "啊，那家伙啊。还在呢。寄居者嘛。",
                    ),
                    (
                        "おーい、エレノスー。出てきー。",
                        "Oi, Erenos! Get out here!",
                        "喂——艾雷诺斯——出来——",
                    ),
                ],
                actor=ars_hecatia,
            ),
            d.wait(0.3),
            d.line(
                "……あいつほんま呼んでもすぐ来んわ。",
                actor=ars_hecatia,
                en="...That guy never comes when you call.",
                cn="……那家伙叫了也不马上来。",
            ),
            d.line(
                "おら！ 出てこんかったら明日からパン買いに行かすで！",
                actor=ars_hecatia,
                en="Hey! If you don't come out, I'm sending you on bread runs starting tomorrow!",
                cn="喂！再不出来明天开始给我去买面包！",
            ),
            d.lines(
                [
                    (
                        "禁書の頁が慌ただしくめくれた。\n影が飛び出してくる。だが...5代目の荘厳な姿ではない。\n小柄な女性の輪郭。頬を膨らませている。",
                        "The tome's pages flip frantically. \nA shadow leaps out. But -- not the solemn figure of the fifth. \nA petite woman's silhouette. Cheeks puffed in annoyance.",
                        "禁书的书页慌忙翻动。\n一个影子跳了出来。然而——并非第五代庄严的身姿。\n娇小女性的轮廓。鼓着腮帮子。",
                    )
                ],
                actor=narrator,
            ),
            d.lines(
                [
                    (
                        "……先輩。その脅しはもう17回目です。",
                        "...Senpai. That's the seventeenth time you've made that threat.",
                        "……前辈。这种威胁已经是第十七次了。",
                    )
                ],
                actor=ars_erenos_pet,
            ),
            d.lines(
                [
                    (
                        "数えとんの？ 暇やなあ。ほら、6代目に挨拶しい。",
                        "You've been counting? You really are bored. Go on, say hi to the sixth.",
                        "你还在数？真闲啊。快，跟第六代打个招呼。",
                    )
                ],
                actor=ars_hecatia,
            ),
            d.lines(
                [
                    (
                        "……どうも。かつて「汝はもう……私だ」などと偉そうに語った者です。\nあの台詞は撤回させてください。",
                        "...Hello. I'm the one who grandly declared \"You are already... me.\" I'd like to retract that line, please.",
                        "……你好。在下就是曾经大言不惭说出「汝已然……成为我」之人。\n那句台词请允许在下收回。",
                    ),
                    (
                        "……こんな辱めを受けるのは生まれて初めてだ。\n死霊術とは、かくも邪悪な術だったのだな。",
                        "...This is the first humiliation I've ever experienced in my life. \nNecromancy truly is a wicked art.",
                        "……这是在下有生以来第一次受此等屈辱。\n死灵术竟是如此邪恶的术法。",
                    ),
                ],
                actor=ars_erenos_pet,
            ),
            d.lines(
                [
                    (
                        "こいつ暇で愚痴ばっかり言うねん。良かったら連れてってくれへん？ 貸したるから。",
                        "This one just complains all day 'cause he's bored. Wanna take him off my hands? I'll lend him to you.",
                        "这家伙闲得只会抱怨。愿意的话带走好不好？借给你。",
                    )
                ],
                actor=ars_hecatia,
            ),
            d.lines(
                [
                    (
                        "先輩、僕はモノではありません。",
                        "Senpai, I am not an object.",
                        "前辈，在下并非物品。",
                    )
                ],
                actor=ars_erenos_pet,
            ),
            d.line(
                "居候がでかい口叩くな。",
                actor=ars_hecatia,
                en="Freeloaders don't get to talk back.",
                cn="寄居者少在这里大放厥词。",
            ),
            d.lines(
                [
                    (
                        "エレノスの影は何か言いかけたが...先輩の一睨みで黙った。",
                        "Erenos's shadow started to say something -- but one glare from his senior silenced him.",
                        "艾雷诺斯的影正要说什么……被前辈一瞪就沉默了。",
                    )
                ],
                actor=narrator,
            ),
            d.line(
                "どうする？", actor=ars_hecatia, en="What will you do?", cn="怎么样？"
            ),
            d.choice_block(
                [
                    d.option(
                        "連れて行く", "borrow_erenos", en="Take him along", cn="带走"
                    ),
                    d.option("やめておく", "lore_menu", en="Never mind", cn="算了"),
                ],
                cancel="lore_menu",
            ),
        ),
    )

    d.node(
        "lore_erenos_short",
        *d.seq(
            r.resolve_flag(
                "state.erenos.is_borrowed", "chitsii.ars.event.erenos_with_player"
            ),
            d.switch_on_flag(
                "chitsii.ars.event.erenos_with_player",
                cases=[("==", 1, "lore_erenos_short_has")],
                actor="pc",
            ),
            d.line(
                "エレノスか？ また連れてくか？",
                actor=ars_hecatia,
                en="Erenos? Wanna take him again?",
                cn="艾雷诺斯？又要带走？",
            ),
            d.choice_block(
                [
                    d.option(
                        "連れて行く", "borrow_erenos", en="Take him along", cn="带走"
                    ),
                    d.option("やめておく", "lore_menu", en="Never mind", cn="算了"),
                ],
                cancel="lore_menu",
            ),
        ),
    )

    d.node(
        "lore_erenos_short_has",
        *d.seq(
            d.line(
                "あいつまだ外におるんやろ。早よ返してや。……嘘やけど。",
                actor=ars_hecatia,
                en="He's still out there with you, right? Bring him back soon. ...Just kidding.",
                cn="那家伙还在外面吧。快点还回来。……骗你的。",
            ),
            d.go("lore_menu"),
        ),
    )

    d.node(
        "borrow_erenos",
        *d.seq(
            r.resolve_run("cmd.erenos.borrow"),
            d.set_flag("chitsii.ars.event.erenos_borrowed", 1, actor=None),
            d.line(
                "ほな、よろしゅうな。……あんまりいじめたらあかんで。うちの居候やし。",
                actor=ars_hecatia,
                en="Right then, take care of him. ...Don't bully him too much. He's my freeloader, after all.",
                cn="那就拜托啦。……别欺负他太狠了。好歹是我的寄居者。",
            ),
            d.go("lore_menu"),
        ),
    )

    d.node(
        "party_song",
        *d.seq(
            d.line(
                "相談か。ええで、何があったん？",
                actor="tg",
                en="Need advice? Sure. What happened?",
                cn="想咨询吗？行啊，发生什么了？",
            ),
            d.choice_block(
                [
                    d.option(
                        "仲間たちに「暗い・キモい・くさい・地味」って言われる",
                        "ps_stings",
                        en="My companions call my magic dark, gross, stinky, and boring",
                        cn="同伴们说我的魔法又暗又恶心又臭又土",
                    )
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_stings",
        *d.seq(
            d.line(
                "うわ、それは刺さるな…。いや、くさくはないやろ。……本当にくさい？",
                actor="tg",
                en="Oof, that stings... Wait, stinky? No way. ...Is it actually stinky?",
                cn="哎，这话真扎心……等等，臭？不至于吧……真的有味道吗？",
            ),
            d.choice_block(
                [
                    d.option(
                        "術式をぜんぶ「黒いモヤ」扱いされるんだ…",
                        "ps_empathy",
                        en="They treat all my spells as just 'black haze'...",
                        cn="他们把我的术式全都当成“黑雾特效”……",
                    )
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_empathy",
        *d.seq(
            d.line(
                "それは否定できへんな。",
                actor="tg",
                en="I can't deny that.",
                cn="这个我确实没法否认。",
            ),
            d.line(
                "うちも昔、気づいたら「だいたい黒」で押し切ってもうてた時期ある。",
                actor="tg",
                en="I had a phase too - before I knew it, I was forcing everything through with 'mostly black.'",
                cn="我以前也有那段时期——回过神来，发现自己什么都在用“反正黑就对了”硬撑。",
            ),
            d.choice_block(
                [
                    d.option(
                        "どうしたら良いと思う？",
                        "ps_ask_love",
                        en="What do you think I should do?",
                        cn="你觉得我该怎么办？",
                    )
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_ask_love",
        *d.seq(
            d.line(
                "ちょっと聞くけど——自分の術のこと、好き？",
                actor="tg",
                en="Let me ask you something - do you love your own craft?",
                cn="先问你一句——你喜欢自己的术吗？",
            ),
            d.choice_block(
                [
                    d.option(
                        "もちろんだ。誇りに思う。言うまでもない！",
                        "ps_proud",
                        en="Of course. I'm proud of it. Goes without saying!",
                        cn="当然。我以此为傲，不言自明！",
                    ),
                    d.option(
                        "好き。でも伝わらないのが悔しい",
                        "ps_proud",
                        en="I do. But it hurts that it doesn't come across.",
                        cn="喜欢。但传达不出去，真的不甘心。",
                    ),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_proud",
        *d.seq(
            d.line(
                "ほら。今の声、ちゃんと誇りあるやん。",
                actor="tg",
                en="See? That voice right there is full of pride.",
                cn="你看，你刚才那语气，明明就很有骄傲。",
            ),
            d.line(
                "術は悪ない。問題は、見せ方と時代がまだ追いついてへんだけや。",
                actor="tg",
                en="Your craft is not the problem. The presentation - and the times - just haven't caught up yet.",
                cn="术本身没问题。只是表现方式和这个时代，还没追上你。",
            ),
            d.line(
                "実はうちもな、昔やらかしてん。",
                actor="tg",
                en="Truth is, I messed this up myself back then.",
                cn="其实我当年也翻过车。",
            ),
            d.line(
                "黒マント・黒ローブ・黒背景で術を披露したら——",
                actor="tg",
                en="I performed in a black cloak, black robe, black backdrop - and then...",
                cn="我那时黑披风、黑长袍、黑背景地上台施术——结果……",
            ),
            d.line(
                "自分で召喚した雪プチに「ご主人様……？どこですか……？」っていわれてもうた。",
                actor="tg",
                en="Even the Snow Puti I summoned asked, 'Master...? Where are you...?'",
                cn="连我自己召出来的雪噗奇都问我：‘主人……？您在哪……？’",
            ),
            d.line(
                "闇の魔術師が闇に溶けてどうすんねん。存在意義の危機や。",
                actor="tg",
                en="What kind of dark mage disappears into the dark? That's an existential crisis.",
                cn="黑魔法师自己融进黑里算什么事？这都存在意义危机了。",
            ),
            d.choice_block(
                [
                    d.option(
                        "それは……深刻な問題だ",
                        "ps_reframe",
                        en="That's... a serious problem.",
                        cn="这……确实是严重问题。",
                    )
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_reframe",
        *d.seq(
            d.line(
                "あの日学んだのは一つだけ。",
                actor="tg",
                en="I learned one thing that day.",
                cn="那天我只学会了一件事。",
            ),
            d.line(
                "「黒は映えるけど、黒の上で黒は消える」。",
                actor="tg",
                en="Black pops - until you put black on black, then it vanishes.",
                cn="黑色本来很出彩——但黑叠黑，只会消失。",
            ),
            d.line(
                "術の中身はそのまま。舞台だけ変えたる。",
                actor="tg",
                en="Keep the spell itself. Change only the stage.",
                cn="术的内核不动，只换舞台。",
            ),
            d.line(
                "あんた、死霊術師がもっとも苦手としてきたもの、知ってるか？",
                actor="tg",
                en="You know what necromancers have historically been worst at?",
                cn="你知道死灵术师历史上最不擅长的是什么吗？",
            ),
            d.choice_block(
                [
                    d.option("聖属性？", "ps_pastel", en="Holy element?", cn="圣属性?"),
                    d.option("日光？", "ps_pastel", en="Sunlight?", cn="阳光？"),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_pastel",
        *d.seq(
            d.wait(1.0),
            d.line(
                "パステルカラーや。", actor="tg", en="Pastel colors.", cn="粉彩色。"
            ),
            d.line(
                "でもな、逆に考えてみ。死霊術師が急にピンク出したら——",
                actor="tg",
                en="But think about it the other way. If a necromancer suddenly throws pink -",
                cn="但你反过来想。死灵术师要是突然甩出粉色——",
            ),
            d.line(
                "みんな「え？」ってなるやろ。そのギャップが武器になるんよ。",
                actor="tg",
                en="Everyone goes, 'Huh?' That contrast becomes your weapon.",
                cn="大家都会“诶？”一下。这个反差，就是武器。",
            ),
            d.choice_block(
                [
                    d.option(
                        "それはもう魔法というよりエンタメでは",
                        "ps_react_a",
                        en="Isn't that entertainment more than magic?",
                        cn="这已经更像演出而不是魔法了吧。",
                    ),
                    d.option(
                        "おもしろそう。でも死霊術の誇りは？",
                        "ps_react_b",
                        en="Sounds fun. But what about necromancer pride?",
                        cn="听起来挺有趣。但死灵术的骄傲呢？",
                    ),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_react_a",
        *d.seq(
            d.line(
                "エンタメ上等。先に注目を奪う。理解はその後でええ。",
                actor="tg",
                en="Entertainment is fine. Seize their eyes first. Understanding can come later.",
                cn="演出就演出，没问题。先把视线夺过来，理解放后面。",
            ),
            d.go("ps_demo"),
        ),
    )

    d.node(
        "ps_react_b",
        *d.seq(
            d.line(
                "誇りはあるよ。あるから見せ方にも手を抜かんのや。",
                actor="tg",
                en="We do have pride. That's exactly why we don't cut corners on presentation.",
                cn="骄傲当然有。正因为有，才更不能在表现上偷懒。",
            ),
            d.line(
                "中身で勝って、演出でも勝つ。それが本物の誇りやろ。",
                actor="tg",
                en="Win on substance, and win on presentation too. That's real pride.",
                cn="内容要赢，演出也要赢。那才是真正的骄傲。",
            ),
            d.go("ps_demo"),
        ),
    )

    d.node(
        "ps_demo",
        *d.seq(
            d.line(
                "ええか。「暗い・キモい・くさい・地味」——",
                actor="tg",
                en="Listen. 'Dark, creepy, stinky, bland' -",
                cn="听好了。‘暗、怪、臭、土’——",
            ),
            r.resolve_run("cmd.hecatia.set_party_portrait"),
            d.line(
                "見せたる。ないがしろにされてきた、死霊術の真のポテンシャルをな。",
                actor="tg",
                en="I'll show you the true potential of necromancy that's been underestimated for too long.",
                cn="我给你看看，被长期轻视的死灵术真正潜力。",
            ),
            r.resolve_run("cmd.hecatia.party_show"),
            d.play_bgm_with_fallback("BGM/Hecatia_Rap_Edit", "BGM/AshAndHolyLance"),
            d.wait(3.0),
            d.line(
                "極彩色に溺れるがいい。「送魂祭（ソウルフェス）」 エンチャントォォォオオオ！",
                actor="tg",
                en="Drown in riotous color! 'Soulsend Festival' - ENCHANTOOOO!",
                cn="沉溺于极彩吧！「送魂祭」——附魔啊啊啊啊！",
            ),
            d.choice_block(
                [
                    d.option(
                        "……え、ちょっと待って今の何",
                        "ps_punchline",
                        en="...Wait, hold on - what was that?",
                        cn="……等下，刚才那是什么？",
                    )
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_punchline",
        *d.seq(
            d.line("死霊術や。", actor="tg", en="Necromancy.", cn="死灵术。"),
            d.line(
                "——ピンク入りの。",
                actor="tg",
                en="- With a touch of pink.",
                cn="——带粉色的。",
            ),
            d.choice_block(
                [
                    d.option(
                        "……反論の余地がない",
                        "ps_epilogue",
                        en="...No room to argue.",
                        cn="……无从反驳。",
                    ),
                    d.option(
                        "これ、仲間に見せたい",
                        "ps_epilogue",
                        en="I want to show this to my companions.",
                        cn="这个我想给同伴们看。",
                    ),
                ],
                cancel="choices",
            ),
        ),
    )

    d.node(
        "ps_epilogue",
        *d.seq(
            d.line(
                'せやろ。"黒いモヤ"にも祭りの夜は来るんやで。',
                actor="tg",
                en='See? Even "black haze" gets its festival night.',
                cn="对吧？就连“黑雾”也会迎来属于它的祭典之夜。",
            ),
            d.go("choices"),
        ),
    )

    d.node(
        "end",
        *d.seq(
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.HECATIA_TALK)
