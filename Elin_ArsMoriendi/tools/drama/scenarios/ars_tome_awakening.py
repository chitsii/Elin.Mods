# -*- coding: utf-8 -*-
"""Drama 2: ars_tome_awakening (Stage 1->2: Opening the tome for the first time)"""

from drama.drama_builder import DramaBuilder
from drama.data import Actors, BGM


def define_tome_awakening(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_tome_awakening"

    builder.step(main)
    builder.quest_check("ars_tome_awakening", can_start_tmp_flag)
    builder.fade_out(0.8)
    builder.fade_in(0.8)
    builder.play_bgm(BGM.REVELATION)
    builder.conversation([
        ("ta_1", "禁書を開く。頁に新たな文字が浮かび上がった。前回閉じた時には、なかったものだ。",
                 "You open the tome. New characters surface on the pages. They weren't there when you last closed it.",
                 "翻开禁书。书页上浮现了新的文字。上次合上时，这些并不存在。"),
        ("ta_2", "読み進める。文字が目から入り、血管を通って全身に広がるような感覚。頭痛はない。むしろ心地よい。",
                 "You read on. It feels as if the words enter through your eyes and spread through your veins. No headache. Rather, it feels pleasant.",
                 "继续阅读。文字仿佛从眼睛进入，经由血管扩散至全身。没有头痛。反而令人愉悦。"),
        ("ta_3", "……舌の奥に、かすかな甘さ。\n身体が警戒を解いていく。\nそれが一番、気にかかる。",
                 "...A faint sweetness at the back of your tongue.\nYour body lets its guard down.\nThat is what bothers you most.",
                 "……舌根处，隐约的甘甜。\n身体逐渐卸下戒备。\n这一点最令人在意。"),
        ("ta_4", "時間の感覚が曖昧になる。どれくらい読んでいたのか。窓の外の光が変わっている。",
                 "Your sense of time blurs. How long have you been reading? The light outside the window has changed.",
                 "时间的感觉变得模糊。读了多久？窗外的光线已经改变。"),
    ], actor=narrator)
    builder.wait(0.3)
    builder.conversation([
        ("ta_5", "ふと気づく。ページの隅に、自分の筆跡がある。書いた覚えはない。",
                 "You notice something. In the margin of the page, there is handwriting -- your handwriting. You don't remember writing it.",
                 "忽然注意到。页角处有字迹。是自己的笔迹。却不记得写过。"),
        ("ta_6", "「この論理は正しい」...と書かれている。自分の字で。自分の言葉遣いで。だが、書いた記憶がない。",
                 '"This logic is correct" -- it reads. In your handwriting. In your phrasing. But you have no memory of writing it.',
                 "「此论证无误」……如此写着。用你的字迹。你的措辞。然而你毫无印象。"),
        ("ta_7", "筆跡を消そうとして、\n手が止まった。\n消す理由が、見つからない。",
                 "You moved to erase the writing.\nYour hand stopped.\nYou couldn't find a reason to.",
                 "正要擦去笔迹，\n手却停住了。\n找不到要擦掉它的理由。"),
    ], actor=narrator)
    builder.shake()
    builder.conversation([
        ("ta_8", "禁書の奥から、再びあの声が聞こえる。今度は、もう少しはっきりと。",
                 "From deep within the tome, you hear that voice again. This time, a little more clearly.",
                 "从禁书深处，那个声音再次传来。这一次，更清晰了一些。"),
        ("ta_9", "「恐れることはない。これは自然な過程だ。知識が身体に馴染んでいるだけだ。\n"
                 "...信じなくてもいい。だが、事実だ。」",
                 '"There is nothing to fear. This is a natural process. The knowledge is simply settling into your body. '
                 '-- You need not believe me. But it is the truth."',
                 "「无需恐惧。这是自然的过程。知识正在融入你的身体。\n"
                 "……信不信由你。但这是事实。」"),
        ("ta_10", "……「信じなくてもいい」。\nその声を聞いた瞬間、\n肩の力が抜けていた。\n抜けていたことに、気づく。",
                  '..."You need not believe me."\nThe moment you heard those words,\nyour shoulders had already relaxed.\nYou only noticed after.',
                  "……「信不信由你」。\n听到这句话的瞬间，\n双肩已然放松下来。\n之后才意识到这一点。"),
    ], actor=narrator)
    builder.wait(0.5)
    builder.conversation([
        ("ta_11", "禁書を閉じようとして、\n手が止まる。\nもう少しだけ。もう少しだけ。\n指が、頁を離さない。",
                  "You try to close the tome.\nYour hand stops.\nJust a little more. Just a little more.\nYour fingers won't leave the page.",
                  "正要合上禁书，\n手停住了。\n再看一点。再看一点。\n手指离不开书页。"),
        ("ta_12", "……結局、閉じなかった。指先に残った痺れが、前回より深くなっている。",
                  "...In the end, you didn't close it. The numbness lingering in your fingertips has grown deeper than last time.",
                  "……终究没有合上。指尖残留的麻痹感，比上一次更深了。"),
    ], actor=narrator)
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.8)
