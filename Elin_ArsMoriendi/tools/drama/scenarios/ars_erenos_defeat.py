# -*- coding: utf-8 -*-
"""Drama: ars_erenos_defeat (Stage 6: After defeating Erenos's shadow)"""

from drama.data import BGM, Actors
from drama.drama_builder import DramaBuilder


def define_erenos_defeat(builder: DramaBuilder):
    narrator = builder.register_actor(
        Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书"
    )
    erenos = builder.register_actor(
        Actors.ERENOS, "エレノス", "Erenos", name_cn="艾雷诺斯"
    )
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_erenos_defeat"

    builder.step(main)
    builder.quest_check("ars_erenos_defeat", can_start_tmp_flag)
    builder.fade_out(1.0)
    builder.fade_in(1.0)
    builder.play_bgm(BGM.REQUIEM)
    builder.wait(0.5)
    builder.say("ed_1", "……そうか。", "...Well done.", actor=erenos, text_cn="……甚好。")
    builder.say(
        "ed_1b",
        "勝った。\n影は崩れた。\nだが、頭の中にエレノスの\n思考の残響が響いている。",
        "You won.\nThe shadow crumbled.\nBut echoes of Erenos's thoughts\nstill ring in your head.",
        actor=narrator,
        text_cn="你赢了。\n影溃散了。\n然而脑海中艾雷诺斯的\n思维残响仍在回荡。",
    )
    builder.conversation(
        [
            (
                "ed_2",
                "痛みは感じなかった。\nもう身体がないからだ。\n\n……痛みがないことへの\n名残惜しさだけがある。\n\nこれは私の感情か？\n四代目の残響か？\n……もう、どちらでもいい。",
                "I felt no pain.\nBecause I no longer have a body.\n\n...Only a lingering regret\nfor the absence of pain.\n\nIs this my emotion?\nThe fourth's echo?\n...It doesn't matter anymore.",
                "没有感到痛苦。\n因为已经没有身体了。\n\n……只剩下对无法感受痛苦的\n一丝惋惜。\n\n这是我的感情吗？\n还是第四代的残响？\n……已经无所谓了。",
            ),
            (
                "ed_3",
                "汝はもう……私だ。\nそう言えば不安になるか？",
                "You are already... me.\nDoes that unsettle you?",
                "汝已然……成为我。\n如此说来，会感到不安吗？",
            ),
            (
                "ed_4",
                "安心しろ。\n私もかつて同じことを\n言われた。四代目に。\n\n四代目は三代目に。\n連鎖だ。",
                "Rest assured.\nI was once told\nthe same thing. By the fourth.\n\nThe fourth by the third.\nA chain.",
                "安心吧。\n我也曾被告知过\n同样的话。被第四代。\n\n第四代被第三代。\n锁链如此延续。",
            ),
            (
                "ed_5",
                "一つだけ、遺言を。\n\n……遺言という形式自体が\n感傷的だが、許してほしい。",
                "One last testament.\n\n...The very format of a testament\nis sentimental, but forgive me.",
                "只留一句遗言。\n\n……遗言这种形式本身\n就很感伤，但请见谅。",
            ),
            (
                "ed_6",
                "汝も禁書に\n「次の者へ」と書くだろう。\n\n衝動が来る。\n逆らえない。\n逆らう必要もない。",
                'You will write\n"To the next one" in the tome.\n\nThe urge will come.\nYou cannot resist.\nNor need you.',
                "汝亦将在禁书上\n写下「致后来者」。\n\n冲动会降临。\n无法抗拒。\n也无需抗拒。",
            ),
            (
                "ed_7",
                "だが……\n書く内容だけは、\n自分で選べ。\n\nそれが汝に残された、\n最後の自由だ。",
                "But...\nchoose what you write\nyourself.\n\nThat is the last freedom\nleft to you.",
                "然而……\n唯有所写之内容，\n由汝自行抉择。\n\n那是留给汝的\n最后的自由。",
            ),
        ],
        actor=erenos,
    )
    builder.shake()
    builder.conversation(
        [
            (
                "ed_8",
                "エレノスの影が薄れていく。輪郭が崩れ、文字に戻り、禁書の頁に吸い込まれていく。",
                "Erenos's shadow fades. Its outline crumbles, returning to letters, absorbed back into the tome's pages.",
                "艾雷诺斯的影逐渐淡去。轮廓崩解，化为文字，被吸回禁书的书页之中。",
            ),
            (
                "ed_9",
                "禁書の表紙が変わっている。著者名の欄...エレノスの名があった場所に、今は空白がある。",
                "The tome's cover has changed. In the author's field -- where Erenos's name once was, now there is a blank.",
                "禁书的封面发生了变化。著者名栏……艾雷诺斯的名字所在之处，如今只剩空白。",
            ),
            (
                "ed_10",
                "……いや。空白ではない。自分の名前が、薄くだが確かに浮かび上がっている。書いた覚えはない。",
                "...No. Not blank. Your own name is faintly but surely surfacing. You don't remember writing it.",
                "……不。并非空白。你自己的名字，虽然淡薄，却确实浮现了出来。不记得写过。",
            ),
        ],
        actor=narrator,
    )
    builder.wait(0.8)
    builder.conversation(
        [
            (
                "ed_11",
                "禁書が閉じた。エレノスの声はもう聞こえない。代わりに...静寂がある。深い、底のない静寂。",
                "The tome closes. Erenos's voice is no longer heard. In its place -- silence. Deep, bottomless silence.",
                "禁书合上了。再也听不到艾雷诺斯的声音。取而代之的是……寂静。深邃的、无底的寂静。",
            ),
            (
                "ed_12",
                "声が消えた。\n頭の中が静かだ。\n自分の声だけが残った。\n……自分の、声だけが。",
                "The voice is gone.\nSilence inside your head.\nOnly your own voice remains.\n...Only your own.",
                "声音消失了。\n脑海中一片安静。\n只剩下自己的声音。\n……只剩下，自己的。",
            ),
        ],
        actor=narrator,
    )
    builder.wait(1.0)
    builder.say(
        "ed_13",
        "……禁書が、ひとりでに開いた。\n"
        "封じられていた最後の頁——昇華の儀式の手順。\n"
        "影を超えた今、ようやく読み解ける。",
        "...The tome opens on its own.\n"
        "The last sealed pages -- the apotheosis ritual's procedure.\n"
        "Now that the shadow is surpassed, you can finally decipher it.",
        actor=narrator,
        text_cn="……禁书自行翻开了。\n"
        "被封印的最后书页——升华仪式的步骤。\n"
        "超越了影之后，终于可以读解。",
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(1.0)
