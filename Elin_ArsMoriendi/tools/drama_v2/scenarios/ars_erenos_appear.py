from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_erenos_appear_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')
    ars_erenos_shadow = d.chara('ars_erenos_shadow')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_erenos_appear', 'chitsii.ars.tmp.can_start.ars_erenos_appear'),
            d.scene_open(1.0, color='black'),
            d.play_bgm('BGM/ManuscriptByCandlelight'),
            d.lines([('禁書が唸り始めた。頁が高速で捲れていく。文字が浮かび上がり、消え、また浮かぶ。', 'The tome begins to groan. Pages flip rapidly. Characters surface, vanish, and surface again.', '禁书开始低鸣。书页飞速翻动。文字浮现、消失、又再浮现。'), ('...最後の章が開いた。エレノスの最後の講義が始まる。', "-- The final chapter opens. Erenos's last lecture begins.", '……最后的篇章翻开了。艾雷诺斯最后的讲义开始了。')], actor=narrator),
            d.wait(0.5),
            d.lines([('これが最後の講義だ。', 'This is the final lecture.', '这是最后一课。'), ('お前は十分に学んだ。\n十分に変わった。\n\n……だが、まだ\n問いが残っているだろう。', 'You have learned enough.\nChanged enough.\n\n...But questions\nmust remain.', '汝已学得足够。\n也已改变足够。\n\n……但尚有\n疑问残存吧。'), ('最後の機会だ。\n聞きたいことがあれば\n答えよう。\n\n嘘はつかない。\n……省略はするかもしれないが。', 'This is your last chance.\nIf you have questions,\nI will answer.\n\nI will not lie.\n...Though I may omit.', '这是最后的机会。\n若有想问的事，\n我会回答。\n\n不会说谎。\n……但或许会有所省略。')], actor=ars_erenos_shadow),
            d.go('question_hub'),
        ),
    )

    d.node(
        'question_hub',
        *d.seq(
            d.line('...何を聞く？', actor=ars_erenos_shadow, en='-- What will you ask?', cn='……问什么？'),
            d.raw({'action': 'choice', 'jump': 'who', 'param': 'not(if_flag(chitsii.ars.drama.ea_asked_who))', 'id': 'ea_ca', 'text_JP': 'お前は何者だ', 'text_EN': 'Who are you?', 'text_CN': '你是什么人？'}),
            d.raw({'action': 'choice', 'jump': 'why', 'param': 'not(if_flag(chitsii.ars.drama.ea_asked_why))', 'id': 'ea_cb', 'text_JP': 'なぜ後継者を求める', 'text_EN': 'Why do you seek a successor?', 'text_CN': '你为何寻求继承者？'}),
            d.raw({'action': 'choice', 'jump': 'karen', 'param': 'not(if_flag(chitsii.ars.drama.ea_asked_karen))', 'id': 'ea_cc', 'text_JP': 'カレンについてどう思う', 'text_EN': 'What do you think of Karen?', 'text_CN': '你怎么看卡伦？'}),
            d.raw({'action': 'choice', 'jump': 'ritual', 'param': 'not(if_flag(chitsii.ars.drama.ea_asked_ritual))', 'id': 'ea_cd', 'text_JP': '昇華の儀式で、何が起きる', 'text_EN': 'What happens in the apotheosis ritual?', 'text_CN': '升华仪式中会发生什么？'}),
            d.raw({'action': 'choice', 'jump': 'truth', 'param': 'not(if_flag(chitsii.ars.drama.ea_asked_truth))', 'id': 'ea_ce', 'text_JP': '何を省略している', 'text_EN': 'What are you omitting?', 'text_CN': '你省略了什么？'}),
            d.raw({'action': 'choice', 'jump': 'proceed', 'id': 'ea_cf', 'text_JP': 'もう聞くことはない', 'text_EN': 'I have no more questions.', 'text_CN': '没什么要问的了。'}),
            d.raw({'action': 'cancel', 'jump': 'proceed'}),
        ),
    )

    d.node(
        'who',
        *d.seq(
            d.lines([('エレノス・ヴェルデクト。\n五代目の継承者……\nだったもの。\n今はただの残響だ。', 'Erenos Verdict.\nThe fifth successor...\nor what remains of one.\nNow merely an echo.', '艾雷诺斯·维尔迪克特。\n第五代继承者……\n的残存之物。\n如今不过是一缕残响。'), ('だが「ただの」は\n不正確だな。\n五代分の視座が\n積層している。\n\n私の中に四代目がいるように、\nお前の中にも\nすでに私がいる。', 'But "merely" is imprecise.\nFive generations of perspective\nare layered within.\n\nJust as the fourth resides in me,\nI already reside\nin you.', '然而「不过」一词\n并不精确。\n五代人的视角\n层层叠加。\n\n正如第四代存于我内心，\n汝之中亦已\n有我存在。'), ('「私は誰か」よりも\n「お前は誰か」を問え。\n\n……その答えが、\n以前と同じかどうか。', 'Rather than "who am I,"\nyou should ask "who are you."\n\n...Whether that answer\nis the same as before.', '与其问「我是谁」，\n不如问「汝是谁」。\n\n……那个答案，\n是否与从前相同。')], actor=ars_erenos_shadow),
            d.set_flag('chitsii.ars.drama.ea_asked_who', 1, actor=None),
            d.go('question_hub'),
        ),
    )

    d.node(
        'why',
        *d.seq(
            d.lines([('……正直に言おう。善意ではない。', '...Let me be honest. It is not out of goodwill.', '……坦率地说吧。并非出于善意。'), ('五代分の研究が\n途絶えることが……怖い。\n\n知識が消えることが、\n自分が消えることより。', 'Five generations of research\nending... terrifies me.\n\nKnowledge vanishing\nmore than myself vanishing.', '五代人的研究\n就此中断……令我恐惧。\n\n知识的消亡，\n比自身的消亡更甚。'), ('利己的だと思うか？\nそうかもしれない。\n\nだが……著者が読者を求め、\n教師が弟子を求めるように、\nこの衝動は止められない。\n\nそれが自分のものかどうかは\nもう問わない。', 'You think it selfish?\nPerhaps.\n\nBut as authors seek readers\nand teachers seek students,\nthis impulse cannot be stopped.\n\nWhether it is my own,\nI no longer ask.', '你觉得自私吗？\n也许吧。\n\n但正如作者寻求读者、\n教师寻求弟子，\n这种冲动无法遏止。\n\n它是否属于自己，\n已不再追问。')], actor=ars_erenos_shadow),
            d.set_flag('chitsii.ars.drama.ea_asked_why', 1, actor=None),
            d.go('question_hub'),
        ),
    )

    d.node(
        'karen',
        *d.seq(
            d.lines([('見事な女だ。\n確信がある。\n……それが彼女を\n手強くしている。', 'A remarkable woman.\nShe has conviction.\n...That is what makes her\nformidable.', '了不起的女人。\n有确信。\n……正是这一点\n使她难以对付。'), ('彼女は「汚染」と呼ぶ。\n私は「理解」と呼ぶ。\n\n……同じものを\n見ているのだがな。', 'She calls it "contamination."\nI call it "understanding."\n\n...Though we are looking\nat the same thing.', '她称之为「污染」。\n我称之为「理解」。\n\n……明明看到的\n是同一件事。'), ('彼女が禁書を読んでいたら、\n最も優れた継承者に\nなっていただろう。\n\n……だからこそ、\n決して読まない。\nそれもまた確信だ。', 'Had she read the tome,\nshe would have been\nthe finest successor.\n\n...That is precisely\nwhy she never will.\nThat, too, is conviction.', '若她读过禁书，\n必将成为\n最杰出的继承者。\n\n……正因如此，\n她绝不会读。\n这同样是一种确信。')], actor=ars_erenos_shadow),
            d.set_flag('chitsii.ars.drama.ea_asked_karen', 1, actor=None),
            d.go('question_hub'),
        ),
    )

    d.node(
        'ritual',
        *d.seq(
            d.lines([('正確には...わからない。', 'Precisely -- I do not know.', '准确来说……不清楚。'), ('私は儀式を行わなかった。その前に討たれた。\n禁書の最も古い頁にその記述はある。だが……私には読み解けなかった。', 'I did not perform the ritual. I was slain before that.\nThe oldest pages of the tome describe it. But... I could not decipher them.', '我未曾施行仪式。在那之前就被讨伐了。\n禁书最古老的书页上有相关记述。但……我无法读解。'), ('確かなのは、\nお前が変わるということだ。\n\nそれが「昇華」なのか\n「溶解」なのか……\n保証はできない。\n\n嘘をつくより、\n正直にそう言う。', 'What is certain\nis that you will change.\n\nWhether that is "apotheosis"\nor "dissolution"...\nI cannot guarantee.\n\nI would rather be honest\nthan lie.', '唯一确定的是，\n汝会改变。\n\n那究竟是「升华」\n还是「溶解」……\n无法保证。\n\n与其说谎，\n不如坦言相告。'), ('ただ一つ確かなのは、\nこの書は先代の影を超えるまで\n最後の手順を明かさない。\n\n……つまり、私の影を。', "One thing is certain.\nThe tome will not reveal\nthe final procedure until\nthe predecessor's shadow is surpassed.\n\n...That is -- my shadow.", '唯有一点毋庸置疑，\n此书在超越先代之影之前\n不会揭示最终步骤。\n\n……也就是说，我的影。')], actor=ars_erenos_shadow),
            d.set_flag('chitsii.ars.drama.ea_asked_ritual', 1, actor=None),
            d.go('question_hub'),
        ),
    )

    d.node(
        'truth',
        *d.seq(
            d.lines([('……聞くか。\n省略するつもりだったが……\n問われた以上、答えよう。', '...You ask.\nI intended to omit this...\nbut since you ask, I will answer.', '……要问吗。\n本打算省略……\n既然被问到，便回答吧。'), ('完全な継承には、\n二つの意識の融合が要る。\n\nだが一つの器に\n二つは共存できない。\n片方が……消える。', 'Complete succession requires\nthe fusion of two minds.\n\nBut two cannot coexist\nin one vessel.\nOne... vanishes.', '完全的继承，\n需要两个意识的融合。\n\n然而一个容器中\n无法共存两者。\n其中一方……会消失。'), ('お前が勝てば、\n私の視座を継ぎ、\n私は消える。\n\nお前が負ければ……\n私がお前の身体を得る。\n\n恐れて当然だ。\n私も恐れている。', 'If you prevail,\nyou inherit my perspective\nand I vanish.\n\nIf you fail...\nI gain your body.\n\nFear is natural.\nI, too, am afraid.', '若汝胜出，\n继承我的视角，\n我则消亡。\n\n若汝败北……\n我将获得汝的身体。\n\n恐惧是理所当然的。\n我亦然。')], actor=ars_erenos_shadow),
            d.set_flag('chitsii.ars.drama.ea_asked_truth', 1, actor=None),
            d.go('question_hub'),
        ),
    )

    d.node(
        'proceed',
        *d.seq(
            d.wait(0.5),
            d.line('……もう十分だろう。\n言葉で伝えられることは\nすべて伝えた。', actor=ars_erenos_shadow, en='...That should be enough.\nEverything that can be conveyed\nthrough words has been conveyed.', cn='……够了吧。\n能用言语传达的\n已悉数传达。'),
            d.switch_on_flag('chitsii.ars.drama.ea_asked_truth', cases=[('==', 1, 'shadow_emerge')], actor='pc'),
            d.go('conditional_truth'),
        ),
    )

    d.node(
        'conditional_truth',
        *d.seq(
            d.lines([('だが、完全な継承には...もう一つ必要なものがある。', 'But for complete succession -- there is one more thing needed.', '然而，完全的继承……还需要另一样东西。'), ('二つの意識は、\n一つの器に共存できない。\n\n片方が消えなければ、\n融合は完成しない。', 'Two consciousnesses\ncannot coexist in one vessel.\n\nUnless one vanishes,\nthe fusion cannot be complete.', '两个意识\n无法共存于一个容器。\n\n除非一方消失，\n融合便无法完成。'), ('お前が勝てば、\n私の視座を継ぎ、\n私は消える。\n\nお前が負ければ……\n私がお前の身体を得る。', 'If you prevail,\nyou inherit my perspective\nand I vanish.\n\nIf you fail...\nI gain your body.', '若汝胜出，\n继承我的视角，\n我则消亡。\n\n若汝败北……\n我将获得汝的身体。')], actor=ars_erenos_shadow),
            d.line('……省略はする、と言っていた。この事だったのか。', actor=narrator, en='..."I may omit," he said. So this is what he meant.', cn='……他说过「或许会有所省略」。原来指的是这件事。'),
            d.line('恐れるな、とは言わない。\n恐れて当然だ。\n私もまた……恐れている。', actor=ars_erenos_shadow, en='I will not tell you\nnot to fear.\nFear is natural.\nI, too... am afraid.', cn='不会叫汝不要恐惧。\n恐惧是理所当然的。\n我亦……心怀畏惧。'),
            d.go('shadow_emerge'),
        ),
    )

    d.node(
        'shadow_emerge',
        *d.seq(
            d.play_bgm('BGM/AshAndHolyLance'),
            d.lines([('禁書の文字が蠢いている。頁の上で文字が立体になり、影を落とし始めた。', "The tome's characters writhe. On the page, letters become three-dimensional and begin to cast shadows.", '禁书的文字在蠕动。书页上的文字变得立体，开始投下影子。'), ('...影が頁から剥がれていく。紙の上の影が、三次元の存在になっていく。', '-- The shadows peel away from the page. Shadows on paper become three-dimensional beings.', '……影从书页上剥离。纸上的影子化为三维的存在。')], actor=narrator),
            d.shake(),
            d.wait(0.3),
            d.lines([('禁書の中から、影が這い出てきた。人の形をしている。エレノスの...形を。', "A shadow crawls out of the tome. It has a human shape. Erenos's -- shape.", '影从禁书中爬出。有着人的形态。艾雷诺斯的……形态。'), ('私の研究は途絶えない。\nどちらが勝っても。\n\n……だが、正直に言えば\n消えたくはない。', 'My research will not end.\nRegardless of who prevails.\n\n...But honestly,\nI do not want to vanish.', '我的研究不会中断。\n无论谁胜出。\n\n……但坦白说，\n我不想消失。')], actor=ars_erenos_shadow),
            d.line('影が動いた。', actor=narrator, en='The shadow moved.', cn='影动了。'),
            d.set_flag('chitsii.ars.tmp.can_start.ars_erenos_appear', 0, actor=None),
            d.fade_out(0.8, color='black'),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.ERENOS_APPEAR)
