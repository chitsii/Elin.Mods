# -*- coding: utf-8 -*-
"""Drama B-2: ars_karen_shadow (Stage 7+: Karen returns when servant count >= 5)

ks_6b uses say_if with cond_flag_equals for stigmata_motive-dependent insertion.
drift has no insertion (ks_6 flows directly to ks_7).
"""

from drama.data import BGM, Actors
from drama.drama_builder import DramaBuilder
from drama.scenarios.ars_stigmata import STIGMATA_MOTIVE_FLAG


def define_karen_shadow(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    karen = builder.register_actor(Actors.KAREN, "カレン", "Karen", name_cn="卡伦")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_karen_shadow"

    builder.step(main)
    builder.quest_check("ars_karen_shadow", can_start_tmp_flag)
    builder.fade_out(0.8)
    builder.fade_in(0.8)
    builder.play_bgm_with_fallback(BGM.SORROW, BGM.REQUIEM)
    builder.say(
        "ks_1",
        "気配がした。殺気ではない。だが、隠す気もない。...知っている気配だ。",
        "You sense a presence. Not killing intent. But no attempt to hide, either. -- A familiar presence.",
        actor=narrator,
        text_cn="感知到了气息。不是杀意。但也没有隐藏的意图。……一种熟悉的气息。",
    )
    builder.say("ks_2", "……約束通り、来た。", "...As promised, I've come.", actor=karen,
                text_cn="……如约而至。")
    builder.wait(0.5)
    builder.conversation(
        [
            (
                "ks_3",
                "あなたの従者の噂を聞いた。\n少なくとも五体以上。\n\n先代が暴走する直前と同じ水準よ。",
                "I've heard rumors\nof your servants.\nAt least five.\n\nThe same level the predecessor had\njust before he lost control.",
                "听到了关于你的仆从的传闻。\n至少五体以上。\n\n与先代暴走之前相同的水准。",
            ),
            (
                "ks_4",
                "...偶然を信じない。パターンを信じる。",
                "-- I don't believe in coincidence. I believe in patterns.",
                "……我不信巧合。我信规律。",
            ),
            (
                "ks_5",
                "あなたの目は、先代の目と同じ色。\n\nだが……光が違う。",
                "I'm looking at your eyes.\nThe same color as his.\n\nBut... the light is different.",
                "你的眼睛，与先代的眼睛同色。\n\n但……光芒不同。",
            ),
            (
                "ks_6",
                "先代の目には確信があった。\n自分が正しいという確信。\n\nあなたの目には……\n迷いがある。",
                "His eyes held conviction.\nConviction that he was right.\n\nIn your eyes...\nthere is doubt.",
                "先代的目光中有确信。\n确信自己是对的。\n\n而你的目光中……\n有着迷茫。",
            ),
        ],
        actor=karen,
    )

    # ks_6b: conditional insertion based on motive (drift = no insertion)
    builder.say_if(
        "ks_6b_duty",
        "いや……\n迷いの下に覚悟がある。\n使命のようなものが。\n\n……先代にもあった。\n最初は。",
        builder.cond_flag_equals(STIGMATA_MOTIVE_FLAG, 1),
        text_en="No...\nbeneath the doubt, there is resolve.\nSomething like a mission.\n\n...The predecessor had it too.\nAt first.",
        text_cn="不……\n迷茫之下有着觉悟。\n某种类似使命的东西。\n\n……先代也曾有过。\n起初是这样。",
        actor=karen,
    )
    builder.say_if(
        "ks_6b_direct",
        "いや……\n迷いの下に渇きがある。\n隠す気もないようね。",
        builder.cond_flag_equals(STIGMATA_MOTIVE_FLAG, 2),
        text_en="No...\nbeneath the doubt, there is thirst.\nYou don't even try to hide it.",
        text_cn="不……\n迷茫之下有着渴望。\n你似乎也不打算掩饰。",
        actor=karen,
    )

    builder.conversation(
        [
            (
                "ks_7",
                "迷いがあることは...良いことだ。たぶん。",
                "Having doubt is -- a good thing. Probably.",
                "有迷茫……是好事。大概。",
            ),
            (
                "ks_8",
                "止めに来たのか、ただ話をしに来たのか……\n自分でもわからない。\n\nこの年になって、まだ迷うとは思わなかった。",
                "Whether I came to stop you\nor to observe...\nI don't know myself.\n\nI didn't think\nI'd still be uncertain\nat my age.",
                "是来阻止你的，还是只是来说说话……\n连自己都不清楚。\n\n没想到到了这把年纪，还会犹豫。",
            ),
            (
                "ks_9",
                "一つだけ聞きたい。あなたは従者を...道具だと思っているか。",
                "I want to ask just one thing. Do you consider your servants -- tools?",
                "只想问一件事。你把仆从……当作道具吗？",
            ),
        ],
        actor=karen,
    )
    builder.shake()
    builder.say(
        "ks_10",
        "……答えを探す。だが、正直な答えがわからない。道具でもあり、仲間でもあり、どちらでもない。",
        "...You search for an answer. But you don't know the honest one. Tools and companions and neither.",
        actor=narrator,
        text_cn="……寻找答案。但找不到诚实的回答。是道具，也是同伴，又都不是。",
    )
    builder.wait(0.3)
    builder.conversation(
        [
            (
                "ks_11",
                "……その顔は、先代とは違う。\n先代は即答した。\n「道具だ」と。\n\nそして……\n嘘をついていた。",
                '...That face is different.\nThe predecessor answered\nimmediately. "Tools."\n\nAnd...\nhe was lying.',
                "……你的表情，与先代不同。\n先代毫不犹豫地回答了。\n「是道具」。\n\n然后……\n他在撒谎。",
            ),
            (
                "ks_12",
                "くれぐれも道を踏み外さないで。\n\nあなたの一挙一動を我々が見ている。\n\nそしてジュア様もね。",
                "Do not stray from the path.\n\nYour every move\nis being watched by us.\n\nAnd by Lady Jure as well.",
                "务必不要误入歧途。\n\n你的一举一动我们都在看。\n\n朱亚女神也在注视着。",
            ),
        ],
        actor=karen,
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.8)
