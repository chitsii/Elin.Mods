# -*- coding: utf-8 -*-
"""
p2_02a_resurrection_intro.py - 蘇りの儀式・導入編（第二部）

バルガスとカインを蘇らせるための儀式イベント（説明パート）。
バルガスの生死状態によって導入部分が分岐する。

必要素材（2人分）:
- 不老長寿のエリクシル ×2
- 産卵薬 ×2
- ペットの鶏 ×2
"""

from arena.builders import ArenaDramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds


def define_resurrection_intro_drama(builder: ArenaDramaBuilder):
    """
    蘇りの儀式・導入編

    条件:
    - LAST_BATTLE 完了（POSTGAME状態）
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    nul = builder.register_actor(Actors.NUL, "Nul", "Nul")
    iris = builder.register_actor(Actors.IRIS, "アイリス", "Iris")

    # ========================================
    # ラベル定義
    # ========================================

    main = builder.label("main")
    intro_branch = builder.label("intro_branch")

    # 分岐A: バルガス生存→衰弱死ルート
    a_emergency = builder.label("a_emergency")
    a_silence = builder.label("a_silence")
    a_hope = builder.label("a_hope")

    # 分岐B: バルガス死亡済みルート
    b_prepared = builder.label("b_prepared")

    # 共通パート
    ritual_explanation = builder.label("ritual_explanation")
    chicken_explanation = builder.label("chicken_explanation")
    cain_discussion = builder.label("cain_discussion")
    material_reason = builder.label("material_reason")
    ending = builder.label("ending")

    # ========================================
    # メインエントリ
    # ========================================
    builder.step(main).jump(intro_branch)

    builder.step(intro_branch).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, b_prepared
    ).jump(a_emergency)

    # ========================================
    # 分岐A: バルガス生存→衰弱死ルート
    # ========================================

    builder.step(a_emergency).action(
        "modInvoke", param="hide_npc(sukutsu_arena_master)", actor="pc"
    ).play_bgm("BGM/Ominous_Suspense_02").say(
        "a_emergency_1",
        "大変です……！\nバルガスさんが……倒れました。",
        "Something terrible has happened...!\nBalgas has... collapsed.",
        "不好了……！\n巴尔加斯先生……倒下了。",
        actor=lily,
    ).say(
        "a_nul_1",
        "どうした？ 顔色、悪い。",
        "What's wrong? You look pale.",
        "怎么了？脸色不好。",
        actor=nul,
    ).say(
        "a_lily_2",
        "あなたと対峙するために服用した強化薬や、アスタロトに受けた後遺症が……全部、重なったんです。\nダルフィで、念願のお酒を飲んだ後……笑いながら倒れて、そのまま……。",
        "The enhancing drugs he took to face you, the aftereffects from Astaroth... they all piled up.\nAfter finally having that drink in Derphy... he collapsed while laughing, and then...",
        "为了与您对峙而服用的强化药，以及阿斯塔罗特留下的后遗症……全部叠加了。\n在达尔菲喝了心心念念的酒之后……笑着倒下，然后……",
        actor=lily,
    ).jump(a_silence)

    builder.step(a_silence).wait(2.0).say(
        "a_iris_2",
        "……嘘、だよね？\nだって、あんなに元気だったじゃん。\n『地上で一番うまい酒を飲みに行くぞ』って……。",
        "...That's a lie, right?\nBut he was so full of energy...\n'Let's go get the best drink on the surface,' he said...",
        "……骗人的吧？\n明明那么有精神……\n他还说『去地面喝最好喝的酒』……",
        actor=iris,
    ).say(
        "a_lily_6",
        "……最後まで、バルガスさんらしかったです。",
        "...To the end, he was true to himself.",
        "……到最后，他都很像巴尔加斯先生。",
        actor=lily,
    ).say(
        "a_nul_3",
        "……バルガス。",
        "...Balgas.",
        "……巴尔加斯。",
        actor=nul,
    ).jump(a_hope)

    builder.step(a_hope).say(
        "a_lily_7",
        "……でも、方法があります。\n蘇りの儀式。ずっと研究していたんです。",
        "...But there is a way.\nThe Resurrection Ritual. I've been researching it all along.",
        "……但是，有办法。\n复活仪式。我一直在研究。",
        actor=lily,
    ).say(
        "a_nul_4",
        "知ってる。リリィ、夜遅くまで古文書を読んでた。\n掃除の時、見た。",
        "I know. Lily was reading old texts late into the night.\nI saw it while cleaning.",
        "我知道。莉莉一直到深夜都在读古籍。\n打扫的时候看到的。",
        actor=nul,
    ).say(
        "a_lily_9",
        "……気づいてたんですか。",
        "...You noticed?",
        "……你发现了啊。",
        actor=lily,
    ).say(
        "a_nul_6",
        "リリィの部屋、いつも散らかってる。",
        "Lily's room is always messy.",
        "莉莉的房间总是很乱。",
        actor=nul,
    ).say(
        "a_iris_6",
        "Nul、掃除ついでに監視してたの……。",
        "Nul, were you spying while cleaning...?",
        "Nul，你打扫的时候顺便监视了吧……",
        actor=iris,
    ).say(
        "a_lily_10",
        "と、とにかく……イルヴァとの繋がりができた今なら、素材が手に入ります。\nバルガスさんを……必ず、取り戻します。",
        "A-anyway... Now that we have a connection to Ylva, we can get the materials.\nBalgas... I will definitely bring him back.",
        "总、总之……既然现在与伊尔瓦建立了联系，就能获得材料了。\n巴尔加斯先生……我一定会把他带回来的。",
        actor=lily,
    ).jump(ritual_explanation)

    # ========================================
    # 分岐B: バルガス死亡済みルート
    # ========================================

    builder.step(b_prepared).play_bgm("BGM/Emotional_Sorrow").say(
        "b_lily_1",
        "お待たせしました。\nバルガスさんを蘇らせる準備が、ようやく整いました。",
        "Thank you for waiting.\nThe preparations to revive Balgas are finally complete.",
        "让您久等了。\n复活巴尔加斯先生的准备，终于完成了。",
        actor=lily,
    ).say(
        "b_nul_1",
        "蘇りの儀式。リリィ、ずっと研究してた。",
        "The Resurrection Ritual. Lily's been researching it all along.",
        "复活仪式。莉莉一直在研究。",
        actor=nul,
    ).say(
        "b_iris_1",
        "知ってたの？",
        "You knew?",
        "你知道吗？",
        actor=iris,
    ).say(
        "b_nul_2",
        "リリィの部屋、掃除した時に見た。古文書、たくさん。",
        "I saw it when cleaning Lily's room. Lots of old texts.",
        "打扫莉莉房间的时候看到的。很多古籍。",
        actor=nul,
    ).say(
        "b_lily_3",
        "……Nul、私の部屋に勝手に入らないでください。",
        "...Nul, please don't enter my room without permission.",
        "……Nul，请不要擅自进入我的房间。",
        actor=lily,
    ).say(
        "b_nul_3",
        "アスタロトの命令。仕方ない。",
        "Astaroth's orders. Couldn't help it.",
        "阿斯塔罗特的命令。没办法。",
        actor=nul,
    ).say(
        "b_lily_4",
        "……まあ、いいです。\nイルヴァとの繋がりができた今なら、素材が手に入ります。",
        "...Well, fine.\nNow that we have a connection to Ylva, we can get the materials.",
        "……算了。\n既然现在与伊尔瓦建立了联系，就能获得材料了。",
        actor=lily,
    ).jump(ritual_explanation)

    # ========================================
    # 共通パート: 儀式の説明
    # ========================================

    builder.step(ritual_explanation).say(
        "ritual_1",
        "蘇りの儀式には、三つの素材が必要です。\n一つ目は『不老長寿のエリクシル』。死という終点を『遠ざける』ための触媒です。",
        "The Resurrection Ritual requires three materials.\nFirst, the 'Elixir of Eternal Youth.' A catalyst to 'distance' the endpoint called death.",
        "复活仪式需要三种材料。\n第一是『不老长寿的灵药』。是用来『延缓』名为死亡的终点的触媒。",
        actor=lily,
    ).say(
        "ritual_4",
        "二つ目は？",
        "The second?",
        "第二是？",
        actor=nul,
    ).say(
        "ritual_5",
        "……『産卵薬』です。",
        "...'Love Plus Potion.'",
        "……『产卵药』。",
        actor=lily,
    ).say(
        "ritual_6",
        "……え？",
        "...What?",
        "……诶？",
        actor=iris,
    ).say(
        "ritual_7",
        "生命を生み出す作用が必要なんです！\n死んだ肉体に、もう一度『生まれる』きっかけを与える……理論的には完璧なんです！",
        "The effect of creating life is necessary!\nTo give the dead body another chance to 'be born'... Theoretically, it's perfect!",
        "需要产生生命的作用！\n给死去的肉体再一次『诞生』的契机……理论上是完美的！",
        actor=lily,
    ).say(
        "ritual_10",
        "いや理屈はわかるけど……産卵？",
        "I get the logic, but... egg-laying?",
        "我理解原理，但是……产卵？",
        actor=iris,
    ).say(
        "ritual_11",
        "三つ目は？",
        "The third?",
        "第三是？",
        actor=nul,
    ).say(
        "ritual_12",
        "……生きた鶏です。",
        "...A living chicken.",
        "……活鸡。",
        actor=lily,
    ).jump(chicken_explanation)

    builder.step(chicken_explanation).say(
        "chicken_1",
        "鶏？",
        "Chicken?",
        "鸡？",
        actor=iris,
    ).say(
        "chicken_2",
        "卵から生まれ、卵を産む——生命の循環の象徴です。\n一説には、生まれ損なった生命は、鶏になるとも言われています。\n死者が生まれ直すための礎を与えるために、その存在を捧げます。",
        "Born from an egg, laying eggs—a symbol of life's cycle.\nSome say failed lives become chickens.\nIt offers its existence to provide a foundation for the dead to be reborn.",
        "从蛋中出生，又产蛋——生命循环的象征。\n据说，出生失败的生命会变成鸡。\n为了给死者提供重生的基石，献上其存在。",
        actor=lily,
    ).say(
        "chicken_4",
        "つまり……生贄。",
        "So... a sacrifice.",
        "也就是说……活祭。",
        actor=nul,
    ).say(
        "chicken_5",
        "……はい。\nですから……あなたに鶏を調達してきてもらいたいんです。",
        "...Yes.\nSo... I'd like you to procure the chickens.",
        "……是的。\n所以……我想请您准备好鸡。",
        actor=lily,
    ).say(
        "chicken_7",
        "鶏泥棒から始まる蘇生術…。ちょっとイメージと違うかも。",
        "Resurrection starting with chicken theft... Not quite what I imagined.",
        "从偷鸡开始的复活术……跟想象的有点不一样。",
        actor=nul,
    ).say(
        "chicken_8",
        "バルガスさんを取り戻すためですから……。",
        "It's to bring Balgas back, so...",
        "为了救回巴尔加斯先生……",
        actor=iris,
    ).jump(cain_discussion)

    # シーン7: カインの話
    builder.step(cain_discussion).say(
        "cain_1",
        "……カインは？",
        "...What about Cain?",
        "……凯恩呢？",
        actor=nul,
    ).say(
        "cain_2",
        "あ、そうだ。バルガスさんの弟子……蘇らせないの？",
        "Oh right. Balgas's disciple... Won't we revive him?",
        "啊，对了。巴尔加斯先生的徒弟……不复活吗？",
        actor=iris,
    ).say(
        "cain_3",
        "カインさん……35年前に亡くなった方ですね。\n正直、蘇生自体も試したことがないんです。それに、あまりに昔のことで……。",
        "Cain... He passed away 35 years ago.\nHonestly, we've never even tried resurrection before. And it was so long ago...",
        "凯恩……是35年前去世的人呢。\n老实说，我们从没试过复活术。而且那是很久以前的事了……",
        actor=lily,
    ).say(
        "cain_5",
        "……。",
        "...",
        "……",
        actor=nul,
    ).say(
        "cain_6",
        "Nul？",
        "Nul?",
        "Nul？",
        actor=iris,
    ).say(
        "cain_7",
        "……私も、守りたい人がいたはず。\nでも、もう覚えていない。名前も、顔も。\n私には、どうしようもない。",
        "...I must have had someone I wanted to protect too.\nBut I don't remember anymore. Not the name, not the face.\nThere's nothing I can do about it.",
        "……我也应该有想守护的人。\n但是，已经不记得了。名字也好，面孔也好。\n我无能为力。",
        actor=nul,
    ).wait(1.0).say(
        "cain_10",
        "……だから、せめてカインは試してあげて。\nバルガス、カインのこと、ずっと後悔してた。\n二人一緒に、蘇らせられるなら……。",
        "...So, at least try it with Cain.\nBalgas always regretted what happened to Cain.\nIf we can bring them both back together...",
        "……所以，至少试试凯恩吧。\n巴尔加斯一直在后悔凯恩的事。\n如果能把两人一起复活的话……",
        actor=nul,
    ).say(
        "cain_13",
        "……わかりました。\n鶏を2羽、用意してもらえますか？\nバルガスさんとカインさん、二人一緒に蘇らせましょう。",
        "...Understood.\nCould you prepare two chickens?\nLet's revive both Balgas and Cain together.",
        "……我明白了。\n能准备两只鸡吗？\n让我们一起复活巴尔加斯先生和凯恩吧。",
        actor=lily,
    ).say(
        "cain_16",
        "Nul……。",
        "Nul...",
        "Nul……",
        actor=iris,
    ).say(
        "cain_17",
        "私のことは、いい。二人を助けて。",
        "Don't worry about me. Help them.",
        "不用管我。救他们吧。",
        actor=nul,
    ).jump(material_reason)

    # シーン8: 素材入手理由
    builder.step(material_reason).say(
        "reason_1",
        "でも、なんで今まで蘇りの儀式、できなかったの？",
        "But why couldn't you do the resurrection ritual until now?",
        "但是，为什么到现在才能进行复活仪式？",
        actor=iris,
    ).say(
        "reason_2",
        "……アスタロト様の時代、このアリーナは閉じた世界でした。\n外の素材は手に入らなかった。\nエリクシルも、産卵薬も……イルヴァでしか作れないものです。",
        "...In Astaroth's time, this arena was a closed world.\nWe couldn't obtain outside materials.\nBoth elixir and love plus potion... can only be made in Ylva.",
        "……在阿斯塔罗特大人的时代，这个斗技场是封闭的世界。\n无法获得外面的材料。\n灵药也好，产卵药也好……都是只有在伊尔瓦才能制作的东西。",
        actor=lily,
    ).say(
        "reason_5",
        "今は違う。繋がってる。",
        "Now it's different. We're connected.",
        "现在不一样了。我们已经联系上了。",
        actor=nul,
    ).say(
        "reason_6",
        "ええ。あなたのおかげで……。\n……本当は、エリクシルは自分用に欲しかったんですけどね。",
        "Yes. Thanks to you...\n...Actually, I wanted the elixir for myself.",
        "是的。多亏了您……\n……其实，我本来想把灵药留给自己的。",
        actor=lily,
    ).say(
        "reason_8",
        "自分用？",
        "For yourself?",
        "给自己？",
        actor=iris,
    ).say(
        "reason_9",
        "私だってサキュバスです。永遠の美貌、憧れますよ？\nでも……二人を取り戻す方が、今は大事です。\n……後でバルガスさんに請求しますけどね。",
        "I'm a succubus too. Eternal beauty is appealing, you know?\nBut... bringing them back is more important now.\n...I'll bill Balgas for it later though.",
        "我也是魅魔啊。永恒的美貌，很向往的好吗？\n但是……现在更重要的是把他们带回来。\n……之后会向巴尔加斯先生收费的哦。",
        actor=lily,
    ).say(
        "reason_12",
        "リリィ、優しい。",
        "Lily, you're kind.",
        "莉莉，你真温柔。",
        actor=nul,
    ).jump(ending)

    # ========================================
    # エンディング（素材集め依頼）
    # ========================================

    builder.step(ending).say(
        "ending_1",
        "では、素材を集めてきてください。\n必要なのは……不老長寿のエリクシル 2本、産卵薬 2本、そしてペットの鶏 2羽です。",
        "Now then, please gather the materials.\nWhat we need is... 2 Elixirs of Eternal Youth, 2 Love Plus Potions, and 2 pet chickens.",
        "那么，请去收集材料吧。\n需要的是……不老长寿灵药2瓶、产卵药2瓶、以及宠物鸡2只。",
        actor=lily,
    ).say(
        "ending_4",
        "イルヴァで探す。手伝う。",
        "I'll search in Ylva. I'll help.",
        "在伊尔瓦寻找。我来帮忙。",
        actor=nul,
    ).say(
        "ending_5",
        "私も。二人のためだもん。",
        "Me too. It's for them after all.",
        "我也是。毕竟是为了他们两人。",
        actor=iris,
    ).say(
        "ending_6",
        "……よろしくお願いします。",
        "...Thank you.",
        "……拜托了。",
        actor=lily,
    ).complete_quest(QuestIds.PG_02A_RESURRECTION_INTRO).drama_end()
