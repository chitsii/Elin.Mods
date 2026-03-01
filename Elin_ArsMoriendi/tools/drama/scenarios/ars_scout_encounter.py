# -*- coding: utf-8 -*-
"""Drama: ars_scout_encounter (Stage 4: after defeating scouts)"""

from drama.data import BGM, Actors
from drama.drama_builder import DramaBuilder


def define_scout_encounter(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_scout_encounter"

    builder.step(main)
    builder.quest_check("ars_scout_encounter", can_start_tmp_flag)
    builder.drama_start(fade_duration=0.8)
    builder.play_bgm(BGM.REVELATION)
    builder.action("drop", "ars_scout_directive")
    builder.action("drop", "ars_alvin_letter")
    builder.conversation(
        [
            (
                "sc_1",
                "偵察兵を倒した。\n静寂が戻る。\n……いや、最初からあった。\n気づかなかっただけだ。",
                "You defeated the scouts.\nSilence returns.\n...No. It was always there.\nYou just hadn't noticed.",
                "侦察兵被击倒了。\n寂静回归。\n……不，从一开始就在。\n只是没有察觉罢了。",
            ),
            (
                "sc_2",
                "倒れた兵士の装備を調べる。標準的な神殿騎士団の装備。だが、場違いなものが二つある。",
                "You examine the fallen soldier's equipment. Standard Temple Knight gear. But two things are out of place.",
                "查看倒下的士兵的装备。标准的神殿骑士团制式装备。但有两样东西格格不入。",
            ),
            (
                "sc_2b",
                "革袋の中に封蝋付きの指令書。\n「対象の監視および報告のみ。交戦を禁ずる」",
                "A sealed directive inside a leather pouch.\n'Observe and report on the subject only. Engagement is forbidden.'",
                "皮袋中有一封火漆密封的指令书。\n「仅限监视目标并报告。禁止交战」",
            ),
            (
                "sc_2c",
                "署名はカレン・グレイヴォーン。\n追記: 「正面からの制圧は失敗した。\n以降は対象の行動記録を優先せよ」",
                "Signed by Karen Gravorn.\nAddendum -- 'Direct suppression has failed.\nPrioritize recording the subject\\'s activities hereafter.'",
                "署名为卡伦·格雷沃恩。\n附注——「正面压制已告失败。\n此后以记录目标行动为优先」",
            ),
            (
                "sc_3",
                "もう一つ。丁寧に折り畳まれた手紙。封はされていない。差出人の名は...アルヴィン。",
                "The other item. A carefully folded letter. Unsealed. The sender's name -- Alvin.",
                "另一样。仔细折叠的信件。没有封口。寄信人的名字是……阿尔文。",
            ),
        ],
        actor=narrator,
    )
    builder.wait(0.5)
    builder.conversation(
        [
            (
                "sc_4",
                "カレンの手帳に出てきた名前だ。エレノスの記録にも。あの「理解した」アルヴィン。",
                "The same name that appeared in Karen's journal. And in Erenos's records. That Alvin who 'understood.'",
                "卡伦手账中出现过的名字。艾雷诺斯的记录中也有。那个「理解了」的阿尔文。",
            ),
            (
                "sc_5",
                "手紙を開く。筆跡は穏やかで、迷いがない。",
                "You open the letter. The handwriting is calm, without hesitation.",
                "展开信件。字迹平和，毫无犹豫。",
            ),
        ],
        actor=narrator,
    )
    builder.wait(0.3)
    builder.conversation(
        [
            (
                "sc_6",
                "『騎士長殿...私たちは間違っていました。\n"
                "死者は消えていません。ずっとそこにいるのです。\n"
                "私たちが見えなかっただけです。』",
                "'Commander -- we were wrong.\n"
                "The dead have not vanished. They have always been there.\n"
                "We simply could not see them.'",
                "『骑士长阁下……我们错了。\n"
                "死者并未消逝。他们一直都在。\n"
                "只是我们看不见而已。』",
            ),
            (
                "sc_7",
                "『二日目、文字が歌いました。とても美しい旋律でした。\n"
                "三日目——文字が私の中に入りました。\n"
                "目を閉じても見えます。今も。まぶたの裏に、びっしりと。』",
                "'On the second day, the letters sang. A beautiful melody.\n"
                "On the third day -- the letters entered me.\n"
                "I can see them even with my eyes closed. Even now. Covering the inside of my eyelids.'",
                "『第二天，文字唱歌了。非常优美的旋律。\n"
                "第三天——文字进入了我的体内。\n"
                "即使闭上眼睛也能看见。现在也是。密密麻麻地，布满了眼皮内侧。』",
            ),
            (
                "sc_8",
                "『あなたにも読んでほしい。\n"
                "読まなければ、この手紙はただの狂人の戯言に見えるでしょう。\n"
                "それでいいのです。——アルヴィン』",
                "'I want you to read it too.\n"
                "If you do not, this letter will appear to be nothing more than the ramblings of a madman.\n"
                "That is fine. -- Alvin'",
                "『也希望您能读一读。\n"
                "若您不读，这封信看起来不过是疯子的呓语。\n"
                "那也无妨。——阿尔文』",
            ),
        ],
        actor=narrator,
    )
    builder.wait(0.5)
    builder.conversation(
        [
            (
                "sc_9",
                "……「狂人の戯言」。そうだろうか。\n"
                "アルヴィンは書いている。「禁書が私を呼んだ」と。\n"
                "……自分にも覚えがある。禁書の隅に、知らないうちに文字を書いていた。",
                '..."The ramblings of a madman." Is it?\n'
                "Alvin wrote -- 'the tome called to me.'\n"
                "...You remember it too. Writing in the margins of the tome without realizing.",
                "……「疯子的呓语」。果真如此吗？\n"
                "阿尔文写道——「禁书呼唤了我」。\n"
                "……你也有过同样的经历。不知不觉中在禁书的角落写下了文字。",
            ),
            (
                "sc_10",
                "アルヴィンは変わったのか。\nそう問いかけて、はっとする。\n自分の言葉が、\nエレノスに似ている。",
                "Did Alvin change?\nAsking that, you catch yourself.\nYour own words sound\nlike Erenos.",
                "阿尔文是否改变了？\n刚问出口便愣住了。\n自己的措辞，\n与艾雷诺斯如出一辙。",
            ),
            (
                "sc_11",
                "手紙を鞄に仕舞った。\nこの手紙は、偵察兵のものではない。\n偵察兵がなぜこれを……\n考えるのをやめた。",
                "You put the letter away.\nThis letter doesn't belong to the scout.\nWhy the scout carried it --\nYou stopped thinking about it.",
                "将信件收入包中。\n这封信不属于侦察兵。\n侦察兵为何会带着它……\n不再去想了。",
            ),
        ],
        actor=narrator,
    )
    builder.say(
        "sc_12",
        "……禁書が脈打った。何かが...近づいている。",
        "...The tome pulses. Something -- is approaching.",
        actor=narrator,
        text_cn="……禁书搏动了。有什么……正在接近。",
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.8)
