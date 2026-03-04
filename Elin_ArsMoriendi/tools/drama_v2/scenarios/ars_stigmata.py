from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx

STIGMATA_MOTIVE_FLAG = 'chitsii.ars.stigmata_motive'


def save_stigmata_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')
    ars_karen = d.chara('ars_karen')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_stigmata', 'chitsii.ars.tmp.can_start.ars_stigmata'),
            d.scene_open(1.0, color='black'),
            d.play_bgm_with_fallback('BGM/Mystical_Ritual', 'BGM/ManuscriptByCandlelight'),
            d.line('禁書に三度の攻城戦の記録が浮かび上がった。...エレノス自身の視点で。', actor=narrator, en="Records of the three sieges surface in the tome -- from Erenos's own perspective.", cn='禁书中浮现了三次攻城战的记录。……以艾雷诺斯本人的视角。'),
            d.wait(0.5),
            d.lines([('三度目の攻城戦について記録する。事実のみを。', 'I will record the third siege. Facts only.', '记录第三次攻城战。仅记事实。'), ('騎士カレン・グレイヴォーンの部隊が最も手強かった。\n練度だけではない。あの女には...確信がある。', "Knight Karen Gravorn's unit was the most formidable. \nNot just skill -- that woman has conviction.", '骑士卡伦·格雷沃恩的部队最为棘手。\n不仅是训练有素。那个女人有……确信。'), ('彼女の部下、名をアルヴィンと言ったか...優秀な剣士だった。', 'Her subordinate, Alvin, I believe -- an excellent swordsman.', '她的部下，叫阿尔文吧……优秀的剑士。'), ('私はアルヴィンを殺さなかった。殺す必要がなかった。\n禁書の第参章を三日間読ませた。それだけだ。', 'I did not kill Alvin. There was no need.\nI had him read Chapter Three of the tome for three days. That was all.', '我没有杀阿尔文。没有必要。\n让他读了三天禁书的第叁章。仅此而已。'), ('三日後、アルヴィンは自分の意志で騎士団を離れた。\nカレンは彼を「変えられた」と言った。私は「理解した」と言う。', 'Three days later, Alvin left the order of his own will.\nKaren said he was "changed." I say he "understood."', '三天后，阿尔文自愿离开了骑士团。\n卡伦说他被「改变」了。我说他「理解」了。')], actor=narrator),
            d.go('karen_record'),
        ),
    )

    d.node(
        'karen_record',
        *d.seq(
            d.line('...禁書の頁の間に、別の筆跡の断片が挟まれていた。\nカレンの字だ。エレノスに宛てた、届かなかった手紙。', actor=narrator, en="-- Between the pages, a fragment in a different hand was tucked away.\nKaren's writing. An unsent letter addressed to Erenos.", cn='……禁书的书页间夹着一段不同笔迹的文字。\n是卡伦的字。写给艾雷诺斯的、未曾送达的信。'),
            d.wait(0.3),
            d.line('アルヴィンは笑っていた。\n自分が何を失ったか\n理解していない……\n\nあるいは理解した上で、\nそれを損失と\n思わなくなっていた。', actor=ars_karen, en='Alvin was smiling.\nHe did not understand\nwhat he had lost...\n\nOr perhaps he understood,\nand no longer considered\nit a loss.', cn='阿尔文在笑。\n他不理解\n自己失去了什么……\n\n又或者他理解了，\n却不再认为\n那是一种损失。'),
            d.line('どちらが正しいのか\n確かめる術は、\nもう私にはない。', actor=ars_karen, en='I no longer have the means\nto determine\nwhich is true.', cn='哪一种才是真相，\n我已无从\n判断。'),
            d.wait(0.5),
            d.go('confession'),
        ),
    )

    d.node(
        'confession',
        *d.seq(
            d.wait(0.3),
            d.lines([('……白状しよう。アルヴィンの件は、私にとっても不愉快だった。\n彼は禁書を読んだ後、私のことを「師」と呼んだ。\n私はその称号を望んでいなかった。', "...I will confess. The matter of Alvin was unpleasant even for me.\nAfter reading the tome, he called me 'Master.'\nI did not want that title.", '……坦白说吧。阿尔文之事，对我而言同样不快。\n他读完禁书后，称我为「师父」。\n这个称号并非我所愿。'), ('だが...「望んでいなかった」と感じること自体が、\n本当に私の感情なのか、それとも四代目から受け継いだ\n「人間らしく見える反応」の残響なのか...確信がない。', "But -- whether this feeling of 'not wanting it'\nis truly my own emotion, or merely an echo of\n'human-seeming responses' inherited from the fourth -- I cannot be certain.", '然而……「不愿意」这种感觉本身，\n究竟是我自己的情感，还是继承自第四代的\n「看似人性的反应」之残响……无法确定。'), ('この種の自己分析は無意味だ。結果だけが残る。\n結果として、アルヴィンは騎士ではなくなり、私の従者になった。\n過程がどうであれ。', 'This kind of self-analysis is meaningless. Only results remain.\nAs a result, Alvin was no longer a knight, but my servant.\nRegardless of the process.', '此类自我剖析毫无意义。唯有结果留存。\n结果便是，阿尔文不再是骑士，而成了我的仆从。\n无论过程如何。'), ('……だが無意味と知りながら分析を止められないこと自体が\n...四代目の癖かもしれない。', "...But the very inability to stop analyzing, \neven knowing it is meaningless -- that may itself be the fourth's habit.", '……然而明知无意义却无法停止剖析这件事本身……\n或许正是第四代的习惯。')], actor=narrator),
            d.go('lineage'),
        ),
    )

    d.node(
        'lineage',
        *d.seq(
            d.line('……四代目の癖。三代目の論理。二代目の絶望。\n――では、初代はどうだった。\n五代分の視座が積層していても、初代だけは...像が結ばない。', actor=narrator, en="...The fourth's habit. The third's logic. The second's despair.\n-- Then what of the first?\nEven with five generations layered within, the first alone -- remains out of focus.", cn='……第四代的习惯。第三代的逻辑。第二代的绝望。\n——那么，初代呢？\n纵使五代人的视角层层叠加，唯独初代……无法聚焦。'),
            d.line('カレンの手帳に記されていた。\n初代の結末は、討伐でも自滅でもなかった。「変容」。\nそして余白に、別の筆跡で――「昇華」「超越」と。', actor=narrator, en="Karen's journal recorded it.\nThe first's end was neither slaying nor self-destruction. 'Transformation.'\nAnd in the margin, in a different hand -- 'apotheosis' and 'transcendence.'", cn='卡伦的手账上有记载。\n初代的结局既非讨伐也非自灭。「变容」。\n而在页边，以另一种笔迹写着——「升华」「超越」。'),
            d.shake(),
            d.wait(0.3),
            d.line('禁書が震えた。頁が勝手にめくれていく。\n封じられていた章――「遺灰の系譜」。\nその奥の、さらに奥に...初代の痕跡が残っていた。', actor=narrator, en="The tome trembles. Pages turn on their own.\nThe sealed chapter -- 'Lineage of Ashes.'\nBeyond it, deeper still -- traces of the first remain.", cn='禁书震颤了。书页自行翻动。\n被封印的篇章——「遗灰的系谱」。\n在其深处，更深处……残留着初代的痕迹。'),
            d.line('文字ではない。文字以前の何か。\n図式、数式、そして...手順。\n初代だけが知っていた儀式の、残滓。', actor=narrator, en='Not words. Something before words.\nDiagrams, formulas, and -- a procedure.\nThe residue of a ritual only the first knew.', cn='不是文字。是文字之前的某种东西。\n图式、公式，以及……步骤。\n唯有初代知晓的仪式之残滓。'),
            d.line('……気づいた。頁の隅に、自分の筆跡が増えている。\nいつの間にか書き写していた。\n伝説の魂、心臓、霊酒、呪いの器、強い魂の束。\n――五つの素材。昇華の儀式に必要なもの。', actor=narrator, en='...You notice. In the margins, your own handwriting has grown.\nYou had been copying without realizing.\nA legendary soul, a heart, spirits, a cursed vessel, strong souls.\n-- Five materials. What is needed for the apotheosis ritual.', cn='……注意到了。页角处自己的笔迹增多了。\n不知不觉中已在抄写。\n传说之魂、心脏、灵酒、诅咒之器、强大的灵魂束。\n——五种素材。升华仪式所需之物。'),
            d.line('だが...手順の記述はそこで途切れている。\n文字が霞んで読めない。初代が仕掛けた試練——\n先代の影を超えなければ、この頁は開かない。', actor=narrator, en="But -- the procedural text breaks off there.\nThe letters blur beyond reading. A trial set by the first --\nthese pages will not open until the predecessor's shadow is surpassed.", cn='然而……步骤的记述到此中断了。\n文字模糊不清，无法阅读。初代设下的试炼——\n不超越先代之影，此页便不会打开。'),
            d.wait(0.5),
            d.line('……これは私の記述ではない。初代の...？\n私でさえ知らなかった。この書のさらに深い層に、こんなものが。\n汝の手が、封印を解いたのか。', actor=narrator, en="...This is not my writing. The first's...?\nEven I did not know. That the tome held such depths.\nYour hand broke the seal.", cn='……这不是我的记述。是初代的……？\n连我都不知道。这本书竟有如此深层。\n是汝的手，解开了封印吗。'),
            d.line('ページの端に、もう一行。やはり自分の字だ。\n「初代は成功した」\n……カレンの言葉を、自分が引用している。', actor=narrator, en="At the edge of the page, one more line. Your handwriting again.\n'The first succeeded.'\n...You are quoting Karen's words.", cn='页边又多了一行。果然是自己的字迹。\n「初代成功了」\n……你在引用卡伦的话。'),
            d.line('禁書が静かに脈打っている。\n初代と同じ道が、目の前に開かれた。\n……儀式を行うかどうかではない。問いはもっと手前にある。', actor=narrator, en='The tome pulses quietly.\nThe same path the first walked now lies open before you.\n...The question is not whether to perform the ritual. The question lies further back.', cn='禁书在静静地搏动。\n与初代相同的道路，在眼前展开了。\n……问题不在于是否施行仪式。问题在更前方。'),
            d.go('motive'),
        ),
    )

    d.node(
        'motive',
        *d.seq(
            d.line('初代の昇華の儀式について知った今、私はどうするべきだろうか？\n……あなたは自問自答した。', actor=narrator, en="Now that you know of the first's apotheosis ritual, what should you do?\n...You asked yourself.", cn='如今已知晓初代的升华仪式，我该怎么做？\n……你自问自答。'),
            d.choice_block([d.option('実践しよう。これで連鎖を止められるかもしれない', 'motive_react_0', en='I will attempt it. Perhaps this can end the cycle', cn='去实践吧。或许这能终结连锁', text_id='st_ca'), d.option('力が手に入る。ならば、やってみる価値がある', 'motive_react_1', en='Power awaits. Then it is worth the attempt', cn='力量在前方。既如此，值得一试', text_id='st_cb'), d.option('やめたいとは思わない。...それが答えだ', 'motive_react_2', en="I don't want to stop. That is my answer", cn='不想停下。……这便是答案', text_id='st_cc')]),
        ),
    )

    d.node(
        'motive_react_0',
        *d.seq(
            d.set_flag('chitsii.ars.stigmata_motive', 1, actor=None),
            d.go('done'),
        ),
    )

    d.node(
        'motive_react_1',
        *d.seq(
            d.set_flag('chitsii.ars.stigmata_motive', 2, actor=None),
            d.go('done'),
        ),
    )

    d.node(
        'motive_react_2',
        *d.seq(
            d.set_flag('chitsii.ars.stigmata_motive', 3, actor=None),
            d.go('done'),
        ),
    )

    d.node(
        'done',
        *d.seq(
            d.set_flag('chitsii.ars.tmp.can_start.ars_stigmata', 0, actor=None),
            d.fade_out(1.0, color='black'),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.STIGMATA)
