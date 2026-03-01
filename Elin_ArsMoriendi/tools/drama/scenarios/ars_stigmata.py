# -*- coding: utf-8 -*-
"""Drama 5: ars_stigmata (Stage 4->5: Erenos's siege records, Alvin's story, PC's motive choice)"""

from drama.drama_builder import DramaBuilder, ChoiceReaction
from drama.data import Actors, BGM

# Flag key for PC's motive choice (referenced by ap_4, sn_12, ks_6b)
STIGMATA_MOTIVE_FLAG = "chitsii.ars.stigmata_motive"
# Values: 1=duty, 2=direct, 3=drift


def define_stigmata(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    karen = builder.register_actor(Actors.KAREN, "カレン", "Karen", name_cn="卡伦")
    main = builder.label("main")
    karen_record = builder.label("karen_record")
    confession = builder.label("confession")
    lineage = builder.label("lineage")
    motive = builder.label("motive")
    done = builder.label("done")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_stigmata"

    # ── main: siege records ──
    builder.step(main)
    builder.quest_check("ars_stigmata", can_start_tmp_flag)
    builder.drama_start(fade_duration=1.0)
    builder.play_bgm_with_fallback(BGM.RITUAL, BGM.REVELATION)
    builder.say("st_1",
        "禁書に三度の攻城戦の記録が浮かび上がった。...エレノス自身の視点で。",
        "Records of the three sieges surface in the tome -- from Erenos's own perspective.",
        actor=narrator,
        text_cn="禁书中浮现了三次攻城战的记录。……以艾雷诺斯本人的视角。")
    builder.wait(0.5)
    builder.conversation([
        ("st_2", "三度目の攻城戦について記録する。事実のみを。",
                 "I will record the third siege. Facts only.",
                 "记录第三次攻城战。仅记事实。"),
        ("st_3", "騎士カレン・グレイヴォーンの部隊が最も手強かった。\n"
                 "練度だけではない。あの女には...確信がある。",
                 "Knight Karen Gravorn's unit was the most formidable. \n"
                 "Not just skill -- that woman has conviction.",
                 "骑士卡伦·格雷沃恩的部队最为棘手。\n"
                 "不仅是训练有素。那个女人有……确信。"),
        ("st_4", "彼女の部下、名をアルヴィンと言ったか...優秀な剣士だった。",
                 "Her subordinate, Alvin, I believe -- an excellent swordsman.",
                 "她的部下，叫阿尔文吧……优秀的剑士。"),
        ("st_5", "私はアルヴィンを殺さなかった。殺す必要がなかった。\n"
                 "禁書の第参章を三日間読ませた。それだけだ。",
                 "I did not kill Alvin. There was no need.\n"
                 "I had him read Chapter Three of the tome for three days. That was all.",
                 "我没有杀阿尔文。没有必要。\n"
                 "让他读了三天禁书的第叁章。仅此而已。"),
        ("st_6", "三日後、アルヴィンは自分の意志で騎士団を離れた。\n"
                 "カレンは彼を「変えられた」と言った。私は「理解した」と言う。",
                 'Three days later, Alvin left the order of his own will.\n'
                 'Karen said he was "changed." I say he "understood."',
                 "三天后，阿尔文自愿离开了骑士团。\n"
                 "卡伦说他被「改变」了。我说他「理解」了。"),
    ], actor=narrator)
    builder.jump(karen_record)

    # ── karen_record: Karen's unsent letter fragment ──
    builder.step(karen_record)
    builder.say("st_7",
        "...禁書の頁の間に、別の筆跡の断片が挟まれていた。\n"
        "カレンの字だ。エレノスに宛てた、届かなかった手紙。",
        "-- Between the pages, a fragment in a different hand was tucked away.\n"
        "Karen's writing. An unsent letter addressed to Erenos.",
        actor=narrator,
        text_cn="……禁书的书页间夹着一段不同笔迹的文字。\n"
        "是卡伦的字。写给艾雷诺斯的、未曾送达的信。")
    builder.wait(0.3)
    builder.say("st_8",
        "アルヴィンは笑っていた。\n自分が何を失ったか\n理解していない……\n\nあるいは理解した上で、\nそれを損失と\n思わなくなっていた。",
        "Alvin was smiling.\nHe did not understand\nwhat he had lost...\n\nOr perhaps he understood,\nand no longer considered\nit a loss.",
        actor=karen,
        text_cn="阿尔文在笑。\n他不理解\n自己失去了什么……\n\n又或者他理解了，\n却不再认为\n那是一种损失。")
    builder.say("st_9",
        "どちらが正しいのか\n確かめる術は、\nもう私にはない。",
        "I no longer have the means\nto determine\nwhich is true.",
        actor=karen,
        text_cn="哪一种才是真相，\n我已无从\n判断。")
    builder.wait(0.5)
    builder.jump(confession)

    # ── confession: Erenos's self-analysis ──
    builder.step(confession)
    builder.wait(0.3)
    builder.conversation([
        ("st_10", "……白状しよう。アルヴィンの件は、私にとっても不愉快だった。\n"
                  "彼は禁書を読んだ後、私のことを「師」と呼んだ。\n"
                  "私はその称号を望んでいなかった。",
                  "...I will confess. The matter of Alvin was unpleasant even for me.\n"
                  "After reading the tome, he called me 'Master.'\n"
                  "I did not want that title.",
                  "……坦白说吧。阿尔文之事，对我而言同样不快。\n"
                  "他读完禁书后，称我为「师父」。\n"
                  "这个称号并非我所愿。"),
        ("st_11", "だが...「望んでいなかった」と感じること自体が、\n"
                  "本当に私の感情なのか、それとも四代目から受け継いだ\n"
                  "「人間らしく見える反応」の残響なのか...確信がない。",
                  "But -- whether this feeling of 'not wanting it'\n"
                  "is truly my own emotion, or merely an echo of\n"
                  "'human-seeming responses' inherited from the fourth -- I cannot be certain.",
                  "然而……「不愿意」这种感觉本身，\n"
                  "究竟是我自己的情感，还是继承自第四代的\n"
                  "「看似人性的反应」之残响……无法确定。"),
        ("st_12", "この種の自己分析は無意味だ。結果だけが残る。\n"
                  "結果として、アルヴィンは騎士ではなくなり、私の従者になった。\n"
                  "過程がどうであれ。",
                  "This kind of self-analysis is meaningless. Only results remain.\n"
                  "As a result, Alvin was no longer a knight, but my servant.\n"
                  "Regardless of the process.",
                  "此类自我剖析毫无意义。唯有结果留存。\n"
                  "结果便是，阿尔文不再是骑士，而成了我的仆从。\n"
                  "无论过程如何。"),
        ("st_13", "……だが無意味と知りながら分析を止められないこと自体が\n"
                  "...四代目の癖かもしれない。",
                  "...But the very inability to stop analyzing, \n"
                  "even knowing it is meaningless -- that may itself be the fourth's habit.",
                  "……然而明知无意义却无法停止剖析这件事本身……\n"
                  "或许正是第四代的习惯。"),
    ], actor=narrator)
    builder.jump(lineage)

    # ── lineage: the Ash Lineage chapter surfaces ──
    builder.step(lineage)
    builder.say("st_14",
        "……四代目の癖。三代目の論理。二代目の絶望。\n"
        "――では、初代はどうだった。\n"
        "五代分の視座が積層していても、初代だけは...像が結ばない。",
        "...The fourth's habit. The third's logic. The second's despair.\n"
        "-- Then what of the first?\n"
        "Even with five generations layered within, the first alone -- remains out of focus.",
        actor=narrator,
        text_cn="……第四代的习惯。第三代的逻辑。第二代的绝望。\n"
        "——那么，初代呢？\n"
        "纵使五代人的视角层层叠加，唯独初代……无法聚焦。")
    builder.say("st_15",
        "カレンの手帳に記されていた。\n"
        "初代の結末は、討伐でも自滅でもなかった。「変容」。\n"
        "そして余白に、別の筆跡で――「昇華」「超越」と。",
        "Karen's journal recorded it.\n"
        "The first's end was neither slaying nor self-destruction. 'Transformation.'\n"
        "And in the margin, in a different hand -- 'apotheosis' and 'transcendence.'",
        actor=narrator,
        text_cn="卡伦的手账上有记载。\n"
        "初代的结局既非讨伐也非自灭。「变容」。\n"
        "而在页边，以另一种笔迹写着——「升华」「超越」。")
    builder.shake()
    builder.wait(0.3)
    builder.say("st_16",
        "禁書が震えた。頁が勝手にめくれていく。\n"
        "封じられていた章――「遺灰の系譜」。\n"
        "その奥の、さらに奥に...初代の痕跡が残っていた。",
        "The tome trembles. Pages turn on their own.\n"
        "The sealed chapter -- 'Lineage of Ashes.'\n"
        "Beyond it, deeper still -- traces of the first remain.",
        actor=narrator,
        text_cn="禁书震颤了。书页自行翻动。\n"
        "被封印的篇章——「遗灰的系谱」。\n"
        "在其深处，更深处……残留着初代的痕迹。")
    builder.say("st_17",
        "文字ではない。文字以前の何か。\n"
        "図式、数式、そして...手順。\n"
        "初代だけが知っていた儀式の、残滓。",
        "Not words. Something before words.\n"
        "Diagrams, formulas, and -- a procedure.\n"
        "The residue of a ritual only the first knew.",
        actor=narrator,
        text_cn="不是文字。是文字之前的某种东西。\n"
        "图式、公式，以及……步骤。\n"
        "唯有初代知晓的仪式之残滓。")
    builder.say("st_18",
        "……気づいた。頁の隅に、自分の筆跡が増えている。\n"
        "いつの間にか書き写していた。\n"
        "伝説の魂、心臓、霊酒、呪いの器、強い魂の束。\n"
        "――五つの素材。昇華の儀式に必要なもの。",
        "...You notice. In the margins, your own handwriting has grown.\n"
        "You had been copying without realizing.\n"
        "A legendary soul, a heart, spirits, a cursed vessel, strong souls.\n"
        "-- Five materials. What is needed for the apotheosis ritual.",
        actor=narrator,
        text_cn="……注意到了。页角处自己的笔迹增多了。\n"
        "不知不觉中已在抄写。\n"
        "传说之魂、心脏、灵酒、诅咒之器、强大的灵魂束。\n"
        "——五种素材。升华仪式所需之物。")
    builder.say("st_18b",
        "だが...手順の記述はそこで途切れている。\n"
        "文字が霞んで読めない。初代が仕掛けた試練——\n"
        "先代の影を超えなければ、この頁は開かない。",
        "But -- the procedural text breaks off there.\n"
        "The letters blur beyond reading. A trial set by the first --\n"
        "these pages will not open until the predecessor's shadow is surpassed.",
        actor=narrator,
        text_cn="然而……步骤的记述到此中断了。\n"
        "文字模糊不清，无法阅读。初代设下的试炼——\n"
        "不超越先代之影，此页便不会打开。")
    builder.wait(0.5)
    builder.say("st_19",
        "……これは私の記述ではない。初代の...？\n"
        "私でさえ知らなかった。この書のさらに深い層に、こんなものが。\n"
        "汝の手が、封印を解いたのか。",
        "...This is not my writing. The first's...?\n"
        "Even I did not know. That the tome held such depths.\n"
        "Your hand broke the seal.",
        actor=narrator,
        text_cn="……这不是我的记述。是初代的……？\n"
        "连我都不知道。这本书竟有如此深层。\n"
        "是汝的手，解开了封印吗。")
    builder.say("st_20",
        "ページの端に、もう一行。やはり自分の字だ。\n"
        "「初代は成功した」\n"
        "……カレンの言葉を、自分が引用している。",
        "At the edge of the page, one more line. Your handwriting again.\n"
        "'The first succeeded.'\n"
        "...You are quoting Karen's words.",
        actor=narrator,
        text_cn="页边又多了一行。果然是自己的字迹。\n"
        "「初代成功了」\n"
        "……你在引用卡伦的话。")
    builder.say("st_21",
        "禁書が静かに脈打っている。\n"
        "初代と同じ道が、目の前に開かれた。\n"
        "……儀式を行うかどうかではない。問いはもっと手前にある。",
        "The tome pulses quietly.\n"
        "The same path the first walked now lies open before you.\n"
        "...The question is not whether to perform the ritual. The question lies further back.",
        actor=narrator,
        text_cn="禁书在静静地搏动。\n"
        "与初代相同的道路，在眼前展开了。\n"
        "……问题不在于是否施行仪式。问题在更前方。")
    builder.jump(motive)

    # ── motive: PC's choice ──
    builder.step(motive)
    builder.say("st_choice_prompt",
        "初代の昇華の儀式について知った今、私はどうするべきだろうか？\n"
        "……あなたは自問自答した。",
        "Now that you know of the first's apotheosis ritual, what should you do?\n"
        "...You asked yourself.",
        actor=narrator,
        text_cn="如今已知晓初代的升华仪式，我该怎么做？\n"
        "……你自问自答。")
    builder.choice_block([
        ChoiceReaction(
            "実践しよう。これで連鎖を止められるかもしれない", "st_ca",
            text_en="I will attempt it. Perhaps this can end the cycle",
            text_cn="去实践吧。或许这能终结连锁",
        )
            .set_flag(STIGMATA_MOTIVE_FLAG, 1)
            .jump(done),

        ChoiceReaction(
            "力が手に入る。ならば、やってみる価値がある", "st_cb",
            text_en="Power awaits. Then it is worth the attempt",
            text_cn="力量在前方。既如此，值得一试",
        )
            .set_flag(STIGMATA_MOTIVE_FLAG, 2)
            .jump(done),

        ChoiceReaction(
            "やめたいとは思わない。...それが答えだ", "st_cc",
            text_en="I don't want to stop. That is my answer",
            text_cn="不想停下。……这便是答案",
        )
            .set_flag(STIGMATA_MOTIVE_FLAG, 3)
            .jump(done),
    ])

    # ── done: end ──
    builder.step(done)
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(1.0)
