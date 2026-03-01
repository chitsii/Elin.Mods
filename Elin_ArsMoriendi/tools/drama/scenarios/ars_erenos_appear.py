# -*- coding: utf-8 -*-
"""Drama: ars_erenos_appear (Stage 5->6: Erenos's final lecture + question hub + shadow)

The most complex drama: question_hub with 6 choices (5 questions + proceed),
conditional truth section, and shadow emergence.
"""

from drama.drama_builder import DramaBuilder
from drama.data import Actors, BGM

# Flag keys for tracking which questions have been asked
_FLAG_PREFIX = "chitsii.ars.drama.ea_asked_"
FLAG_ASKED_WHO = f"{_FLAG_PREFIX}who"
FLAG_ASKED_WHY = f"{_FLAG_PREFIX}why"
FLAG_ASKED_KAREN = f"{_FLAG_PREFIX}karen"
FLAG_ASKED_RITUAL = f"{_FLAG_PREFIX}ritual"
FLAG_ASKED_TRUTH = f"{_FLAG_PREFIX}truth"


def define_erenos_appear(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    erenos = builder.register_actor(Actors.ERENOS, "エレノス", "Erenos", name_cn="艾雷诺斯")
    main = builder.label("main")
    question_hub = builder.label("question_hub")
    q_who = builder.label("who")
    q_why = builder.label("why")
    q_karen = builder.label("karen")
    q_ritual = builder.label("ritual")
    q_truth = builder.label("truth")
    proceed = builder.label("proceed")
    conditional_truth = builder.label("conditional_truth")
    shadow_emerge = builder.label("shadow_emerge")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_erenos_appear"

    # ── main: introduction ──
    builder.step(main)
    builder.quest_check("ars_erenos_appear", can_start_tmp_flag)
    builder.drama_start(fade_duration=1.0)
    builder.play_bgm(BGM.REVELATION)
    builder.conversation([
        ("ea_1", "禁書が唸り始めた。頁が高速で捲れていく。文字が浮かび上がり、消え、また浮かぶ。",
                 "The tome begins to groan. Pages flip rapidly. Characters surface, vanish, and surface again.",
                 "禁书开始低鸣。书页飞速翻动。文字浮现、消失、又再浮现。"),
        ("ea_2", "...最後の章が開いた。エレノスの最後の講義が始まる。",
                 "-- The final chapter opens. Erenos's last lecture begins.",
                 "……最后的篇章翻开了。艾雷诺斯最后的讲义开始了。"),
    ], actor=narrator)
    builder.wait(0.5)
    builder.conversation([
        ("ea_3", "これが最後の講義だ。",
                 "This is the final lecture.",
                 "这是最后一课。"),
        ("ea_4", "お前は十分に学んだ。\n十分に変わった。\n\n……だが、まだ\n問いが残っているだろう。",
                 "You have learned enough.\nChanged enough.\n\n...But questions\nmust remain.",
                 "汝已学得足够。\n也已改变足够。\n\n……但尚有\n疑问残存吧。"),
        ("ea_5", "最後の機会だ。\n聞きたいことがあれば\n答えよう。\n\n嘘はつかない。\n……省略はするかもしれないが。",
                 "This is your last chance.\nIf you have questions,\nI will answer.\n\nI will not lie.\n...Though I may omit.",
                 "这是最后的机会。\n若有想问的事，\n我会回答。\n\n不会说谎。\n……但或许会有所省略。"),
    ], actor=erenos)
    builder.jump(question_hub)

    # ── question_hub: dynamic choice menu ──
    builder.step(question_hub)
    builder.say("ea_hub_prompt",
        "...何を聞く？",
        "-- What will you ask?",
        actor=erenos,
        text_cn="……问什么？")

    # Dynamic choices: hide questions already asked
    builder.choice_dynamic(q_who,
        "お前は何者だ", f"not(if_flag({FLAG_ASKED_WHO}))",
        text_en="Who are you?", text_cn="你是什么人？", text_id="ea_ca")
    builder.choice_dynamic(q_why,
        "なぜ後継者を求める", f"not(if_flag({FLAG_ASKED_WHY}))",
        text_en="Why do you seek a successor?", text_cn="你为何寻求继承者？", text_id="ea_cb")
    builder.choice_dynamic(q_karen,
        "カレンについてどう思う", f"not(if_flag({FLAG_ASKED_KAREN}))",
        text_en="What do you think of Karen?", text_cn="你怎么看卡伦？", text_id="ea_cc")
    builder.choice_dynamic(q_ritual,
        "昇華の儀式で、何が起きる", f"not(if_flag({FLAG_ASKED_RITUAL}))",
        text_en="What happens in the apotheosis ritual?", text_cn="升华仪式中会发生什么？", text_id="ea_cd")
    builder.choice_dynamic(q_truth,
        "何を省略している", f"not(if_flag({FLAG_ASKED_TRUTH}))",
        text_en="What are you omitting?", text_cn="你省略了什么？", text_id="ea_ce")
    builder.choice(proceed, "もう聞くことはない",
        text_en="I have no more questions.", text_cn="没什么要问的了。", text_id="ea_cf")
    builder.on_cancel(proceed)

    # ── who: identity ──
    builder.step(q_who)
    builder.conversation([
        ("ea_w1", "エレノス・ヴェルデクト。\n五代目の継承者……\nだったもの。\n今はただの残響だ。",
                  "Erenos Verdict.\nThe fifth successor...\nor what remains of one.\nNow merely an echo.",
                  "艾雷诺斯·维尔迪克特。\n第五代继承者……\n的残存之物。\n如今不过是一缕残响。"),
        ("ea_w2", "だが「ただの」は\n不正確だな。\n五代分の視座が\n積層している。\n\n私の中に四代目がいるように、\nお前の中にも\nすでに私がいる。",
                  'But "merely" is imprecise.\nFive generations of perspective\nare layered within.\n\nJust as the fourth resides in me,\nI already reside\nin you.',
                  "然而「不过」一词\n并不精确。\n五代人的视角\n层层叠加。\n\n正如第四代存于我内心，\n汝之中亦已\n有我存在。"),
        ("ea_w3", "「私は誰か」よりも\n「お前は誰か」を問え。\n\n……その答えが、\n以前と同じかどうか。",
                  'Rather than "who am I,"\nyou should ask "who are you."\n\n...Whether that answer\nis the same as before.',
                  "与其问「我是谁」，\n不如问「汝是谁」。\n\n……那个答案，\n是否与从前相同。"),
    ], actor=erenos)
    builder.set_flag(FLAG_ASKED_WHO)
    builder.jump(question_hub)

    # ── why: motivation ──
    builder.step(q_why)
    builder.conversation([
        ("ea_y1", "……正直に言おう。善意ではない。",
                  "...Let me be honest. It is not out of goodwill.",
                  "……坦率地说吧。并非出于善意。"),
        ("ea_y2", "五代分の研究が\n途絶えることが……怖い。\n\n知識が消えることが、\n自分が消えることより。",
                  "Five generations of research\nending... terrifies me.\n\nKnowledge vanishing\nmore than myself vanishing.",
                  "五代人的研究\n就此中断……令我恐惧。\n\n知识的消亡，\n比自身的消亡更甚。"),
        ("ea_y3", "利己的だと思うか？\nそうかもしれない。\n\nだが……著者が読者を求め、\n教師が弟子を求めるように、\nこの衝動は止められない。\n\nそれが自分のものかどうかは\nもう問わない。",
                  "You think it selfish?\nPerhaps.\n\nBut as authors seek readers\nand teachers seek students,\nthis impulse cannot be stopped.\n\nWhether it is my own,\nI no longer ask.",
                  "你觉得自私吗？\n也许吧。\n\n但正如作者寻求读者、\n教师寻求弟子，\n这种冲动无法遏止。\n\n它是否属于自己，\n已不再追问。"),
    ], actor=erenos)
    builder.set_flag(FLAG_ASKED_WHY)
    builder.jump(question_hub)

    # ── karen: about Karen ──
    builder.step(q_karen)
    builder.conversation([
        ("ea_k1", "見事な女だ。\n確信がある。\n……それが彼女を\n手強くしている。",
                  "A remarkable woman.\nShe has conviction.\n...That is what makes her\nformidable.",
                  "了不起的女人。\n有确信。\n……正是这一点\n使她难以对付。"),
        ("ea_k2", "彼女は「汚染」と呼ぶ。\n私は「理解」と呼ぶ。\n\n……同じものを\n見ているのだがな。",
                  'She calls it "contamination."\nI call it "understanding."\n\n...Though we are looking\nat the same thing.',
                  "她称之为「污染」。\n我称之为「理解」。\n\n……明明看到的\n是同一件事。"),
        ("ea_k3", "彼女が禁書を読んでいたら、\n最も優れた継承者に\nなっていただろう。\n\n……だからこそ、\n決して読まない。\nそれもまた確信だ。",
                  "Had she read the tome,\nshe would have been\nthe finest successor.\n\n...That is precisely\nwhy she never will.\nThat, too, is conviction.",
                  "若她读过禁书，\n必将成为\n最杰出的继承者。\n\n……正因如此，\n她绝不会读。\n这同样是一种确信。"),
    ], actor=erenos)
    builder.set_flag(FLAG_ASKED_KAREN)
    builder.jump(question_hub)

    # ── ritual: what happens ──
    builder.step(q_ritual)
    builder.conversation([
        ("ea_r1", "正確には...わからない。",
                  "Precisely -- I do not know.",
                  "准确来说……不清楚。"),
        ("ea_r2", "私は儀式を行わなかった。その前に討たれた。\n"
                  "禁書の最も古い頁にその記述はある。だが……私には読み解けなかった。",
                  "I did not perform the ritual. I was slain before that.\n"
                  "The oldest pages of the tome describe it. But... I could not decipher them.",
                  "我未曾施行仪式。在那之前就被讨伐了。\n"
                  "禁书最古老的书页上有相关记述。但……我无法读解。"),
        ("ea_r3", "確かなのは、\nお前が変わるということだ。\n\nそれが「昇華」なのか\n「溶解」なのか……\n保証はできない。\n\n嘘をつくより、\n正直にそう言う。",
                  "What is certain\nis that you will change.\n\nWhether that is \"apotheosis\"\nor \"dissolution\"...\nI cannot guarantee.\n\nI would rather be honest\nthan lie.",
                  "唯一确定的是，\n汝会改变。\n\n那究竟是「升华」\n还是「溶解」……\n无法保证。\n\n与其说谎，\n不如坦言相告。"),
        ("ea_r4", "ただ一つ確かなのは、\nこの書は先代の影を超えるまで\n最後の手順を明かさない。\n\n……つまり、私の影を。",
                  "One thing is certain.\nThe tome will not reveal\nthe final procedure until\nthe predecessor's shadow is surpassed.\n\n...That is -- my shadow.",
                  "唯有一点毋庸置疑，\n此书在超越先代之影之前\n不会揭示最终步骤。\n\n……也就是说，我的影。"),
    ], actor=erenos)
    builder.set_flag(FLAG_ASKED_RITUAL)
    builder.jump(question_hub)

    # ── truth: what is omitted ──
    builder.step(q_truth)
    builder.conversation([
        ("ea_t1", "……聞くか。\n省略するつもりだったが……\n問われた以上、答えよう。",
                  "...You ask.\nI intended to omit this...\nbut since you ask, I will answer.",
                  "……要问吗。\n本打算省略……\n既然被问到，便回答吧。"),
        ("ea_t2", "完全な継承には、\n二つの意識の融合が要る。\n\nだが一つの器に\n二つは共存できない。\n片方が……消える。",
                  "Complete succession requires\nthe fusion of two minds.\n\nBut two cannot coexist\nin one vessel.\nOne... vanishes.",
                  "完全的继承，\n需要两个意识的融合。\n\n然而一个容器中\n无法共存两者。\n其中一方……会消失。"),
        ("ea_t3", "お前が勝てば、\n私の視座を継ぎ、\n私は消える。\n\nお前が負ければ……\n私がお前の身体を得る。\n\n恐れて当然だ。\n私も恐れている。",
                  "If you prevail,\nyou inherit my perspective\nand I vanish.\n\nIf you fail...\nI gain your body.\n\nFear is natural.\nI, too, am afraid.",
                  "若汝胜出，\n继承我的视角，\n我则消亡。\n\n若汝败北……\n我将获得汝的身体。\n\n恐惧是理所当然的。\n我亦然。"),
    ], actor=erenos)
    builder.set_flag(FLAG_ASKED_TRUTH)
    builder.jump(question_hub)

    # ── proceed: end of questions ──
    builder.step(proceed)
    builder.wait(0.5)
    builder.say("ea_6",
        "……もう十分だろう。\n言葉で伝えられることは\nすべて伝えた。",
        "...That should be enough.\nEverything that can be conveyed\nthrough words has been conveyed.",
        actor=erenos,
        text_cn="……够了吧。\n能用言语传达的\n已悉数传达。")

    # If truth was NOT asked, reveal it now
    builder.branch_if(FLAG_ASKED_TRUTH, "==", 1, shadow_emerge)
    builder.jump(conditional_truth)

    # ── conditional_truth: forced disclosure ──
    builder.step(conditional_truth)
    builder.conversation([
        ("ea_7", "だが、完全な継承には...もう一つ必要なものがある。",
                 "But for complete succession -- there is one more thing needed.",
                 "然而，完全的继承……还需要另一样东西。"),
        ("ea_8", "二つの意識は、\n一つの器に共存できない。\n\n片方が消えなければ、\n融合は完成しない。",
                 "Two consciousnesses\ncannot coexist in one vessel.\n\nUnless one vanishes,\nthe fusion cannot be complete.",
                 "两个意识\n无法共存于一个容器。\n\n除非一方消失，\n融合便无法完成。"),
        ("ea_9", "お前が勝てば、\n私の視座を継ぎ、\n私は消える。\n\nお前が負ければ……\n私がお前の身体を得る。",
                 "If you prevail,\nyou inherit my perspective\nand I vanish.\n\nIf you fail...\nI gain your body.",
                 "若汝胜出，\n继承我的视角，\n我则消亡。\n\n若汝败北……\n我将获得汝的身体。"),
    ], actor=erenos)
    builder.say("ea_10",
        "……省略はする、と言っていた。この事だったのか。",
        '..."I may omit," he said. So this is what he meant.',
        actor=narrator,
        text_cn="……他说过「或许会有所省略」。原来指的是这件事。")
    builder.say("ea_11",
        "恐れるな、とは言わない。\n恐れて当然だ。\n私もまた……恐れている。",
        "I will not tell you\nnot to fear.\nFear is natural.\nI, too... am afraid.",
        actor=erenos,
        text_cn="不会叫汝不要恐惧。\n恐惧是理所当然的。\n我亦……心怀畏惧。")
    builder.jump(shadow_emerge)

    # ── shadow_emerge: the shadow appears ──
    builder.step(shadow_emerge)
    builder.play_bgm(BGM.BATTLE)
    builder.conversation([
        ("ea_12", "禁書の文字が蠢いている。頁の上で文字が立体になり、影を落とし始めた。",
                  "The tome's characters writhe. On the page, letters become three-dimensional and begin to cast shadows.",
                  "禁书的文字在蠕动。书页上的文字变得立体，开始投下影子。"),
        ("ea_13", "...影が頁から剥がれていく。紙の上の影が、三次元の存在になっていく。",
                  "-- The shadows peel away from the page. Shadows on paper become three-dimensional beings.",
                  "……影从书页上剥离。纸上的影子化为三维的存在。"),
    ], actor=narrator)
    builder.shake()
    builder.wait(0.3)
    builder.conversation([
        ("ea_14", "禁書の中から、影が這い出てきた。人の形をしている。エレノスの...形を。",
                  "A shadow crawls out of the tome. It has a human shape. Erenos's -- shape.",
                  "影从禁书中爬出。有着人的形态。艾雷诺斯的……形态。"),
        ("ea_15", "私の研究は途絶えない。\nどちらが勝っても。\n\n……だが、正直に言えば\n消えたくはない。",
                  "My research will not end.\nRegardless of who prevails.\n\n...But honestly,\nI do not want to vanish.",
                  "我的研究不会中断。\n无论谁胜出。\n\n……但坦白说，\n我不想消失。"),
    ], actor=erenos)
    builder.say("ea_16",
        "影が動いた。",
        "The shadow moved.",
        actor=narrator,
        text_cn="影动了。")
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.8)
