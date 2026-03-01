# -*- coding: utf-8 -*-
"""
18_last_battle.md - 虚空の王、静寂の断罪
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, DramaNames, FlagValues, Keys, QuestBattleFlags, QuestIds, SessionKeys


def define_last_battle(builder: DramaBuilder):
    """
    最終章：アスタロトとの最終決戦
    シナリオ: 18_last_battle.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    astaroth = builder.register_actor(Actors.ASTAROTH, "アスタロト", "Astaroth")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    nul = builder.register_actor(Actors.NUL, "Nul", "Nul")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    act1 = builder.label("act1_preparations")
    act1_5 = builder.label("act1_5_nul_memories")
    act2 = builder.label("act2_throne_room")
    act3 = builder.label("act3_intervention")
    act4 = builder.label("act4_battle")

    # バルガス死亡版ラベル
    main_dead = builder.label("main_balgas_dead")
    act1_dead = builder.label("act1_balgas_dead")
    act1_5_dead = builder.label("act1_5_balgas_dead")
    act3_dead = builder.label("act3_balgas_dead")

    # 選択肢分岐ラベル（通常版用）
    act3_allies_protect = builder.label("act3_allies_protect")
    act3_take_all = builder.label("act3_take_all")
    # 注: act5〜finale は 19_epilogue.py に移動済み（重複を避けるため削除）

    # ========================================
    # 第1幕: 決戦前夜
    # ========================================
    builder.step(main).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, main_dead
    ).drama_start(bg_id="Drama/zek_hideout", bgm_id="BGM/Final_PreBattle_Calm").say(
        "narr_1",
        "ゼクの隠れ家『次元のゴミ捨て場』。そこは、アスタロトが切り捨てた『不要な可能性』が堆積する場所。",
        "Zek's hideout, the 'Dimensional Junkyard.' A place where 'discarded possibilities' cast aside by Astaroth accumulate.",
        "泽克的藏身处'次元垃圾场'。这里堆积着被阿斯塔罗特舍弃的'不必要的可能性'。",
        actor=narrator,
    ).say(
        "narr_2",
        "決戦を翌朝に控えた夜、焚き火の代わりに、剥き出しの魔力回路が淡い青光を放っている。",
        "The night before the decisive battle. Instead of a campfire, exposed magical circuits emit a pale blue glow.",
        "决战前夜，裸露的魔力回路散发着淡淡的蓝光，取代了篝火。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "narr_3",
        "（バルガスは大剣を研ぎ、リリィは静かに祈りを捧げ、ゼクは『因果の断片（キューブ）』の最終調整を行っていた。）",
        "(Vargus sharpens his greatsword, Lily offers a silent prayer, and Zek makes final adjustments to the 'Causality Fragment Cube.')",
        "（巴尔加斯在磨着大剑，莉莉静静地祈祷，泽克则在对'因果碎片（立方体）'进行最后的调整。）",
        actor=narrator,
    ).say(
        "balgas_1",
        "……引退？ ああ、確かに言ったな。……だが、お前が俺の命を救ったあの日、俺の中で何かが変わっちまったんだよ。",
        "...Retirement? Hah, yeah, I said that. ...But the day you saved my life, somethin' changed inside me.",
        "……退休？啊，老子确实说过。……但你小子救了老子一命的那天，老子心里有些东西变了。",
        actor=balgas,
    ).say(
        "balgas_2",
        "カインを失って以来、俺はずっと『死に場所』を探してた。だが、お前は俺にそれを許さなかった。……なら、せめて最後くらい、お前の『生きる場所』を作る手伝いをさせろ。",
        "Ever since I lost Cain, I've been lookin' for a place to die. But you wouldn't let me have it. ...So at least let me help you build a place to live.",
        "自从失去凯恩以来，老子一直在找个'死去的地方'。但你小子不让老子如愿。……那么，至少让老子帮你创造一个'活下去的地方'吧。",
        actor=balgas,
    ).say(
        "balgas_3",
        "それに……引退した老いぼれが、弟子の晴れ舞台を客席から眺めてるだけなんて、柄じゃねえんだよ。俺は戦士だ。最期まで、剣を握って立ってる方が性に合ってる。",
        "Besides... ain't my style to be some retired old geezer watchin' his student's big moment from the stands. I'm a warrior. Standin' with a sword in my hand 'til the end--that's who I am.",
        "再说……让退休的老家伙坐在观众席上看徒弟的大场面，不是老子的风格。老子是战士。握着剑站到最后，这才是老子。",
        actor=balgas,
    ).focus_chara(Actors.LILY).say(
        "lily_2",
        "私の真名……。この名を知る者がいる限り、私はもう、アスタロト様の命令には従いません。……明日、王があなたの存在を消そうとしても、私がその因果を繋ぎ止めてみせます。",
        "My true name... As long as someone knows this name, I shall no longer obey Lord Astaroth's commands. ...Even if the King tries to erase your existence tomorrow, I will anchor your causality.",
        "我的真名……只要有人知道这个名字，我就不再服从阿斯塔罗特大人的命令。……即使明天王想要抹消您的存在，我也会将您的因果牢牢锁住。",
        actor=lily,
    ).focus_chara(Actors.ZEK).say(
        "zek_1",
        "クク……商売抜きで言わせてもらいましょう。アスタロトは、この世界の『限界』を勝手に決めつけ、見限っている。",
        "Heh heh... Let me speak without the merchant's mask. Astaroth has arbitrarily decided this world's 'limits' and abandoned it.",
        "呵呵呵……请容在下抛开商人的身份说几句。阿斯塔罗特擅自断定了这个世界的'极限'，并抛弃了它。",
        actor=zek,
    ).say(
        "zek_2",
        "私はそれが気に入らない。ゴミはゴミらしく、混沌として流れてこそ美しい。私が用意したこのキューブは、王が支配する『時間』の独裁を一時的に狂わせるためのものです。……さあ、休みなさい。明日の朝、この世界の『所有権』を奪い返しに行くのですから。",
        "I find that displeasing. Refuse is beautiful only when it flows chaotically, as refuse should. This cube I've prepared will temporarily disrupt the King's tyrannical grip on 'time.' ...Now rest. Tomorrow morning, we reclaim 'ownership' of this world.",
        "在下对此很不满。垃圾就该像垃圾一样，在混沌中流动才美丽。在下准备的这个立方体，是用来暂时扰乱王所支配的'时间'独裁的。……好了，请休息吧。明天早上，我们要去夺回这个世界的'所有权'。",
        actor=zek,
    ).jump(act1)

    # ゼクとの会話
    builder.step(act1).say(
        "pc_1",
        "（自分の『絶望する瞬間』を狙うつもりではないのか、と、あなたはゼクに疑問を投げかけた。）",
        "(You ask Zek if he isn't still waiting for the moment you 'fall into despair.')",
        "（你向泽克询问，他是不是还在等待自己'陷入绝望的瞬间'。）",
        actor=pc,
    ).say(
        "pc_2",
        "（カインの魂を売らせようとしたのも、リリィを裏切らせようとしたのも……全て、自分を『剥製』にするためだったのだろう。なのに、なぜ今、助けようとするのか？）",
        "(Trying to make you sell Cain's soul, trying to make you betray Lily... it was all to turn you into a 'specimen,' wasn't it? So why help you now?)",
        "（试图让自己出卖凯恩的灵魂，试图让自己背叛莉莉……这一切都是为了把自己做成'标本'吧。那么，为什么现在要帮助自己？）",
        actor=pc,
    ).say("zek_3", "……ふふ。", "...Heh heh.",
        "……呵呵呵。",
        actor=zek).say(
        "zek_4",
        "その通りです。私はあなたが『最も美しく壊れる瞬間』を待っていました。英雄が絶望に堕ちる時、その魂は最高の芸術品となる……。それが、私という『剥製師』の美学でした。",
        "Quite correct. I was waiting for the moment you would 'break most beautifully.' When a hero falls into despair, their soul becomes the finest work of art... Such was my aesthetic as a 'taxidermist.'",
        "正是如此。在下一直在等待阁下'最美丽地崩溃的瞬间'。当英雄堕入绝望时，其灵魂将成为最高的艺术品……这就是在下作为'标本师'的美学。",
        actor=zek,
    ).say(
        "zek_5",
        "ですが、あなたは違った。カインを裏切る選択肢を与えても、あなたはバルガスを選んだ。リリィを欺く道を示しても、あなたは罪を告白した。バルガスを殺す観客の喝采が響いても、あなたは剣を下ろした。",
        "But you were different. Given the choice to betray Cain, you chose Vargus. Shown the path to deceive Lily, you confessed your sins. Even when the audience roared for Vargus's death, you lowered your blade.",
        "但阁下不同。即使给了背叛凯恩的选择，阁下也选择了巴尔加斯。即使指出了欺骗莉莉的道路，阁下也坦白了罪行。即使观众高呼要杀死巴尔加斯，阁下也放下了剑。",
        actor=zek,
    ).say(
        "zek_6",
        "……あなたは、私の期待を『裏切り続けた』のです。そして、その度に私は気づいてしまった。あなたが『壊れた瞬間』を剥製にするよりも……あなたが『システムそのものを壊す瞬間』を目撃する方が、遥かに美しい、と。",
        "...You kept 'betraying' my expectations. And each time, I came to realize something. Witnessing the moment you 'break the system itself' would be far more beautiful than preserving your 'broken moment.'",
        "……阁下一直在'背叛'在下的期待。而每一次，在下都意识到了一件事。比起将阁下'崩溃的瞬间'做成标本……见证阁下'打破系统本身的瞬间'要美丽得多。",
        actor=zek,
    ).say(
        "zek_7",
        "私のコレクションは、数千にも及びます。絶望した英雄、狂気に堕ちた賢者、愛に溺れた魔王……。ですが、『神を殺し、牢獄を破壊し、自由を手にする人間』は、まだ一つもない。",
        "My collection numbers in the thousands. Despairing heroes, sages fallen to madness, demon lords drowned in love... But I have not a single 'human who slays a god, destroys their prison, and seizes freedom.'",
        "在下的收藏品多达数千。绝望的英雄、堕入疯狂的贤者、溺于爱情的魔王……但是，'杀死神、摧毁牢笼、获得自由的人类'，还一个都没有。",
        actor=zek,
    ).say(
        "zek_8",
        "……あなたこそが、私の最高傑作。そして、その傑作は『未完成のまま飾る』のではなく……『完璧な結末を迎えさせる』ことでこそ、真価を発揮するのです。",
        "...You are my masterpiece. And a masterpiece reveals its true worth not by being 'displayed incomplete'... but by being allowed to reach its 'perfect conclusion.'",
        "……阁下才是在下的最高杰作。而杰作的真正价值，不是'以未完成的状态展示'……而是'让它迎来完美的结局'才能展现。",
        actor=zek,
    ).say(
        "zek_9",
        "だから、私はあなたを助けます。商売のためでも、友情のためでもない。……ただ、この世界で最も美しい『奇跡の瞬間』を、この目で見届けたいから。それが、剥製師ゼクの最後の『作品』です。",
        "That is why I help you. Not for commerce, not for friendship. ...Simply because I wish to witness with my own eyes the most beautiful 'miraculous moment' in this world. That shall be the taxidermist Zek's final 'work.'",
        "所以，在下会帮助阁下。不是为了生意，也不是为了友情。……只是因为在下想亲眼见证这个世界上最美丽的'奇迹瞬间'。这将是标本师泽克最后的'作品'。",
        actor=zek,
    ).say(
        "zek_10",
        "……さあ、休みなさい。明日、あなたは私に『最高の絵』を見せてくれる。……それを、永遠に焼き付けておきましょう。",
        "...Now rest. Tomorrow, you shall show me the 'finest painting.' ...I shall etch it into eternity.",
        "……好了，请休息吧。明天，阁下将向在下展示'最高的画作'。……在下会将它永远铭刻下来。",
        actor=zek,
    ).say(
        "zek_hint_1",
        "……ああ、そうそう。一つ、有用な情報を。闘技場の古い記録によると、過去にアスタロトが剣闘士と相対したことはあったようです。",
        "...Oh, yes. One useful piece of information. According to old arena records, Astaroth has faced gladiators before.",
        "……啊对了。告诉您一条有用的信息。根据角斗场的古老记录，阿斯塔罗特过去曾与剑斗士对峙过。",
        actor=zek,
    ).say(
        "zek_hint_2",
        "奴は戦いの中で姿を変えるらしい。変わるたびに性質も変わる。",
        "It seems he changes form during battle. Each time he transforms, his properties change as well.",
        "据说他在战斗中会改变姿态。每次变化，性质也会改变。",
        actor=zek,
    ).say(
        "zek_hint_3",
        "……ある記録には、最初の姿には毒が効いたとあった。別の記録では、魔法で押し切ったと。",
        "...One record states poison was effective against his first form. Another says magic overwhelmed him.",
        "……有一份记录说，毒对他最初的姿态有效。另一份记录说用魔法强行突破了。",
        actor=zek,
    ).say(
        "zek_hint_4",
        "その次の形態には凍てつく攻撃が有効だったとか……最後の異形には、混沌の力が効くという噂もある。",
        "Freezing attacks were supposedly effective against the next form... and rumors say chaos magic works against his final aberrant form.",
        "据说凝冻攻击对下一个形态有效……还有传闻说混沌之力对他最后的异形态有效。",
        actor=zek,
    ).say(
        "zek_hint_5",
        "あくまで断片的な記録ですがね。……参考程度に。",
        "These are merely fragmentary records, of course. ...Just for your reference.",
        "当然，这些只是零碎的记录。……仅供参考。",
        actor=zek,
    ).jump(act1_5)

    # ========================================
    # 第1.5幕: ヌルの記憶ーー「神の孵化場」の真実
    # ========================================
    builder.step(act1_5).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_nul1",
        "王座の間へ向かう途中、一行は崩壊しかけた回廊で、倒れている存在を発見する。",
        "On the way to the throne room, the party discovers a collapsed figure in a crumbling corridor.",
        "前往王座之间的途中，一行人在即将崩塌的回廊里发现了一个倒下的身影。",
        actor=narrator,
    ).say(
        "narr_nul2",
        "それは暗殺人形・ヌル。敗北の罰として、アスタロトに「削除」されかけていた。",
        "It is the assassin doll, Nul. As punishment for defeat, she was being 'deleted' by Astaroth.",
        "那是暗杀人偶・诺尔。作为失败的惩罚，她正被阿斯塔罗特'删除'。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_nul0",
        "……待て！ あれはヌルだ。罠かもしれねえ、近づくな！",
        "...Hold it! That's Nul. Could be a trap--don't get close!",
        "……等等！那是诺尔。可能是陷阱，别靠近！",
        actor=balgas,
    ).say(
        "narr_nul2b", "バルガスが剣を構え、一行の前に立ちはだかる。", "Vargus raises his sword and stands guard before the party.",
        "巴尔加斯举起剑，挡在众人前面。",
        actor=narrator
    ).say(
        "nul_1",
        "……あなた、ですか。……システムを、拒絶した……人間。……私は……もう、戦えない。",
        "...Is it... you? ...The human who... rejected the system. ...I can... no longer fight.",
        "……是你……吗。……拒绝了系统的……人类。……我……已经无法……战斗了。",
        actor=nul,
    ).focus_chara(Actors.LILY).say(
        "lily_nul1", "ヌル……！ あなた、何があったの？", "Nul...! What happened to you?",
        "诺尔……！你怎么了？",
        actor=lily
    ).say(
        "nul_2",
        "……私は、思い出して、しまった。……『私』が、何だったのかを。",
        "...I remembered... what 'I'... once was.",
        "……我想起来了……'我'曾经……是什么。",
        actor=nul,
    ).say(
        "narr_nul3",
        "ヌルの目から、光の粒子が零れ落ちるーー涙のように。",
        "Particles of light fall from Nul's eyes--like tears.",
        "光的粒子从诺尔的眼中落下--像泪水一样。",
        actor=narrator,
    ).play_bgm("BGM/Emotional_Sorrow").say(
        "nul_fb1",
        "……私は、かつて冒険者だった。名前は……もう、思い出せない。仲間がいた。守りたい人がいた。このアリーナに挑んで……そして、負けた。",
        "...I was... once an adventurer. My name... I can no longer recall. I had companions... someone I wanted to protect. I challenged this arena... and lost.",
        "……我曾经是……冒险者。名字……已经想不起来了。有同伴。有想保护的人。挑战了这个角斗场……然后，输了。",
        actor=nul,
    ).say(
        "nul_fb2",
        "アスタロト様は、私を『素材』にした。神格を人工的に作り出す実験ーー『神の孵化場』計画の、実験体に。",
        "Lord Astaroth... made me into 'material.' A test subject for the experiment to artificially create godhood... the 'Divine Hatchery' project.",
        "阿斯塔罗特大人……把我变成了'素材'。人工创造神格的实验--'神之孵化场'计划的实验体。",
        actor=nul,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_nul1", "……！ 『神の孵化場』だと……？", "...! 'Divine Hatchery'...?",
        "……！'神之孵化场'……？",
        actor=balgas
    ).say(
        "narr_nul3b",
        "バルガスの表情が凍りつく。その言葉には、覚えがあった。ゼクが語っていた、あの荒唐無稽な話。",
        "Vargus's expression freezes. He remembered those words--the outlandish tale Zek had told.",
        "巴尔加斯的表情凝固了。他记得这些话--泽克曾说过的那个荒诞不经的故事。",
        actor=narrator,
    ).say(
        "balgas_nul2",
        "……ゼクの野郎が言ってたことは、本当だったのか……？",
        "...So what that bastard Zek said was true...?",
        "……那个混蛋泽克说的是真的……？",
        actor=balgas,
    ).say(
        "nul_fb3",
        "このアリーナの本当の目的……それは、娯楽でも、戦士の育成でもない。挑戦者の魂を極限まで練磨し、『神格』に至らせること。そして、その力をアスタロト様が吸収し……新しい世界を創造すること。",
        "The true purpose of this arena... is neither entertainment nor training warriors. It is to refine challengers' souls to their limit... to reach 'godhood.' Then Lord Astaroth absorbs that power... to create a new world.",
        "这个角斗场的真正目的……不是娱乐，也不是培养战士。而是将挑战者的灵魂磨炼到极限，使其达到'神格'。然后阿斯塔罗特大人吸收那股力量……创造新的世界。",
        actor=nul,
    ).say(
        "nul_fb3b",
        "アスタロト様は……かつて故郷を失った方。5万年前、『カラドリウス』という竜族の楽園が、神々の争いで滅んだと聞いています。",
        "Lord Astaroth... once lost his homeland. Fifty thousand years ago... the dragon paradise called 'Caladrius' was destroyed in a war among gods.",
        "阿斯塔罗特大人……曾经失去了故乡。据说五万年前，一个叫'卡拉德里乌斯'的龙族乐园在众神的争斗中毁灭了。",
        actor=nul,
    ).say(
        "nul_fb3c",
        "だから……新しい世界を創ろうとしている。自分が唯一の神として君臨する、完璧な楽園を。",
        "That is why... he seeks to create a new world. A perfect paradise... where he reigns as the sole god.",
        "所以……他试图创造一个新的世界。一个自己作为唯一的神君临的、完美的乐园。",
        actor=nul,
    ).say(
        "nul_fb4",
        "私は失敗作。神になれなかった、空っぽの人形。だから『Null』ーー『無』という名前を与えられた。",
        "I am... a failure. An empty doll... who could not become a god. That is why I was given the name 'Null'... meaning 'nothing.'",
        "我是失败作。无法成为神的、空虚的人偶。所以被赋予了'Null'--'无'这个名字。",
        actor=nul,
    ).say(
        "nul_3",
        "……あなたはいまや神格に最も近い存在……あの方はあなたを『吸収』しようとしている。",
        "...You are now... the closest existence to godhood. He intends... to 'absorb' you.",
        "……你现在是……最接近神格的存在……那位大人想要'吸收'你。",
        actor=nul,
    ).focus_chara(Actors.LILY).say(
        "lily_nul2", "……アスタロト様。あなたは、ずっとそんなことを……。", "...Lord Astaroth. All this time, you have been...",
        "……阿斯塔罗特大人。您一直以来……都在做这种事……",
        actor=lily
    ).say(
        "nul_5",
        "……お願い。あの方を……止めて。私の中にいた『誰か』が……ずっと、それを望んでいた。",
        "...Please... stop him. The 'someone' who was inside me... always wished for this.",
        "……拜托。阻止……那位大人。我心中的'某人'……一直都在期盼着这件事。",
        actor=nul,
    ).say(
        "nul_6",
        "……あなたのおかげで……私は、最後に『思い出す』ことができた。私には……守りたい人が、いたんだって。",
        "...Thanks to you... I could finally 'remember.' That I... had someone I wanted to protect.",
        "……多亏了你……我在最后能够'想起来'。我曾经……有想保护的人。",
        actor=nul,
    ).shake().say(
        "narr_nul4", "ヌルの体が光となって消えていく。", "Nul's body dissolves into light.",
        "诺尔的身体化为光芒消散了。",
        actor=narrator
    ).focus_chara(Actors.BALGAS).say(
        "motivation_b1",
        "……聞いたな。あいつも、カインと同じだ。アスタロトに『素材』にされちまった。",
        "...You heard her. She's just like Cain. Turned into 'material' by Astaroth.",
        "……你听到了吧。她跟凯恩一样。被阿斯塔罗特变成了'素材'。",
        actor=balgas,
    ).say(
        "motivation_b2",
        "ヌルの願いは聞いた。あいつの中にいた『誰か』の願いも。……俺たちが、終わらせるんだ。",
        "We heard Nul's wish. The wish of 'someone' inside her too. ...We're gonna end this.",
        "诺尔的愿望老子听到了。她心中'某人'的愿望也听到了。……老子们来结束这一切。",
        actor=balgas,
    ).focus_chara(Actors.LILY).say(
        "motivation_l1",
        "……ヌルは、最後に『守りたい人がいた』と思い出せた。私たちには、まだ守るべきものがある。",
        "...In the end, Nul remembered 'someone she wanted to protect.' We still have things worth protecting.",
        "……诺尔在最后想起了'有想保护的人'。我们还有需要守护的东西。",
        actor=lily,
    ).say(
        "motivation_l2",
        "あなたがここまで来たのは、きっと理由があるはず。……私は、あなたを信じています。",
        "There must be a reason you have come this far. ...I believe in you.",
        "您能走到这里，一定是有原因的。……我相信您。",
        actor=lily,
    ).say("narr_motivation", "一行は、王座の間へと歩み出す。", "The party sets forth toward the throne room.",
        "一行人向王座之间迈进。",
        actor=narrator).jump(
        act2
    )

    # ========================================
    # 第2幕: 虚空の王座
    # ========================================
    builder.step(act2).scene_transition(
        bg_id="Drama/throne_room", bgm_id="BGM/Final_Astaroth_Throne"
    ).say(
        "narr_5",
        "ヌルの消滅を見届けた後、一行は王座の間へと辿り着く。",
        "After witnessing Nul's disappearance, the party arrives at the throne room.",
        "目睹诺尔消散后，一行人抵达了王座之间。",
        actor=narrator,
    ).say(
        "narr_6",
        "そこは、観客席すら存在しない『絶対的な静寂』の空間。アスタロトは、巨大な竜の翼を休め、孤独な王座に腰掛けていた。",
        "A space of 'absolute silence' where not even spectator seats exist. Astaroth rests his great draconic wings, seated upon his solitary throne.",
        "那是一个连观众席都不存在的'绝对寂静'的空间。阿斯塔罗特收起巨大的龙翼，坐在孤独的王座上。",
        actor=narrator,
    ).say(
        "pc_asta",
        "……『神の孵化場』。それがこのアリーナの正体か、アスタロト。",
        "...'The Divine Hatchery.' So that is the true nature of this arena, Astaroth.",
        "……'神之孵化场'。这就是这个角斗场的真面目吗，阿斯塔罗特。",
        actor=pc,
    ).say(
        "astaroth_0",
        "……ヌルから聞いたのか。あの失敗作、最後に余計なことを。",
        "...So Nul told you. That failure, meddling even at the end.",
        "……是从诺尔那里听说的吗。那个失败作，到最后还多管闲事。",
        actor=astaroth,
    ).say(
        "astaroth_1", "……よく来た。世界を揺るがす質量となった者よ。", "...Welcome. Thou who hast become a mass that shakes the world.",
        "……欢迎。成为撼动世界之质量的人啊。",
        actor=astaroth
    ).say(
        "astaroth_2",
        "ゼク、リリィ、バルガス……。敗残兵と裏切り者の手を借りて、この私に対抗しようというのか。",
        "Zek, Lily, Vargus... Thou wouldst challenge me with the aid of stragglers and traitors?",
        "泽克、莉莉、巴尔加斯……汝竟想借助残兵败将和叛徒之手来对抗吾吗。",
        actor=astaroth,
    ).say(
        "astaroth_3",
        "だが、知るがいい。私の言葉は『法』であり、私の吐息は『執行』である。お前たちが何を積み上げようと、私の前では無に等しい。",
        "But know this: my words are 'law,' and my breath is 'execution.' Whatever ye have built amounts to nothing before me.",
        "但是，汝等须知。吾之言语即是'法'，吾之吐息即是'执行'。无论尔等积累了什么，在吾面前都等同于无。",
        actor=astaroth,
    ).jump(act3)

    # ========================================
    # 第3幕: 血の絆ーー権能と仲間の介入
    # ========================================
    builder.step(act3).branch_if(
        Keys.BALGAS_KILLED, "==", FlagValues.BalgasChoice.KILLED, act3_dead
    ).play_bgm(
        "BGM/Final_Liberation"
    ).say(
        "astaroth_p1",
        "【時の独裁】ーーお前の時間を、永遠に止めてやろう",
        "[Tyranny of Time]--I shall halt thy time for eternity.",
        "【时之独裁】--吾将永远停止汝的时间",
        actor=astaroth,
    ).say(
        "astaroth_p2", "【因果の拒絶】ーーお前の攻撃は無に帰す", "[Denial of Causality]--Thy attacks shall return to nothing.",
        "【因果的拒绝】--汝的攻击将归于虚无",
        actor=astaroth
    ).say(
        "astaroth_p3", "【魔の断絶】ーーお前の魔力を消し去る", "[Severance of Magic]--I shall erase thy magical power.",
        "【魔之断绝】--吾将抹消汝的魔力",
        actor=astaroth
    ).shake().say(
        "narr_p1",
        "三つの極悪な権能が、あなたに向かって放たれるーー",
        "Three wicked authorities are unleashed upon you--",
        "三种极恶的权能向你袭来--",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "zek_p1", "私たちが庇いますーー", "We shall shield you--",
        "由我们来保护阁下--",
        actor=zek
    ).choice(act3_allies_protect, "頼む！", "Please!", "拜托了！", text_id="choice_trust").choice(
        act3_take_all, "このまま呪いを受け入れる", "Accept the curses as they are", "就这样接受诅咒", text_id="choice_take_all"
    )

    # 通常版: 仲間が庇う
    builder.step(act3_allies_protect).focus_chara(Actors.ZEK).say(
        "zek_11", "おっとーー私のキューブが黙っていませんよ！", "Heh heh--my cube won't stay silent!",
        "哎呀--在下的立方体可不会坐视不管！",
        actor=zek
    ).say("narr_p2", "ゼクのキューブが展開し、権能を吸収", "Zek's cube unfolds, absorbing the authority.",
        "泽克的立方体展开，吸收了权能。",
        actor=narrator).say(
        "zek_12", "……ぐっ……これは重い。しかしーーまだです！", "...Ngh... This is heavy. But--not yet!",
        "……唔……好沉重。但是--还不够！",
        actor=zek
    ).focus_chara(Actors.BALGAS).say(
        "balgas_4", "させるかよ！ 鋼の意志を舐めるな……！", "Like hell I will! Don't underestimate iron will...!",
        "老子不会让你得逞！别小看钢铁意志……！",
        actor=balgas
    ).say("narr_p3", "バルガスが大剣で権能を受け止める", "Vargus catches the authority with his greatsword.",
        "巴尔加斯用大剑挡住了权能。",
        actor=narrator).say(
        "balgas_5", "俺の魂で……こじ開けてやる！", "With my soul... I'll pry it open!",
        "用老子的灵魂……撬开它！",
        actor=balgas
    ).focus_chara(Actors.LILY).say("lily_3", "……私が、受けます", "...I shall receive it.",
        "……由我来承受。",
        actor=lily).say(
        "narr_p4", "リリィが静かに前に出る", "Lily steps forward quietly.",
        "莉莉静静地走上前。",
        actor=narrator
    ).say("lily_4", "あなたを……守らせてください", "Please... let me protect you.",
        "请让我……保护您。",
        actor=lily).jump(act4)

    # 通常版: プレイヤーが全て受ける
    builder.step(act3_take_all).say(
        "narr_ta1",
        "この呪いを受けた上で、１対１でアスタロトを打ち倒すとあなたは言い放った。",
        "You declare that even bearing these curses, you will defeat Astaroth one-on-one.",
        "你宣言即使承受这些诅咒，也要一对一打倒阿斯塔罗特。",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "zek_ta1", "……正気ですか？", "...Are you sane?",
        "……阁下疯了吗？",
        actor=zek
    ).focus_chara(Actors.BALGAS).say(
        "balgas_ta1", "おい、無茶をーー", "Hey, don't be reckless--",
        "喂，别乱来--",
        actor=balgas
    ).say(
        "astaroth_ta1", "……愚かな選択だ。ならばーー受けるがいい", "...A foolish choice. Then--receive them.",
        "……愚蠢的选择。那么--接受吧",
        actor=astaroth
    ).say("narr_ta3", "呪いがあなたに直撃した！", "The curses strike you directly!",
        "诅咒直接击中了你！",
        actor=narrator).shake().say(
        "narr_ta4",
        "【時の独裁】【因果の拒絶】【魔の断絶】を受けた！",
        "You received [Tyranny of Time], [Denial of Causality], and [Severance of Magic]!",
        "受到了【时之独裁】【因果的拒绝】【魔之断绝】！",
        actor=narrator,
    ).action(
        "eval",
        param="EClass.pc.AddCondition<Elin_SukutsuArena.Conditions.ConAstarothTyranny>(1000);",
    ).action(
        "eval",
        param="EClass.pc.AddCondition<Elin_SukutsuArena.Conditions.ConAstarothDenial>(1000);",
    ).action(
        "eval",
        param="EClass.pc.AddCondition<Elin_SukutsuArena.Conditions.ConAstarothDeletion>(1000);",
    ).jump(act4)

    # ========================================
    # 第4幕: レベル1億の激突
    # ========================================
    builder.step(act4).say(
        "narr_12", "アスタロトの瞳に、『驚愕』と『喜び』が混じる。", "'Astonishment' and 'joy' mingle in Astaroth's eyes.",
        "阿斯塔罗特的眼中，交织着'惊愕'与'喜悦'。",
        actor=narrator
    ).say("astaroth_4", "……ハハッ！ 面白い！ ", "...Ha ha! How amusing!",
        "……哈哈！有趣！",
        actor=astaroth).say(
        "astaroth_5",
        "よかろう、戦鬼よ！ 私の力と、お前の力……ここで雌雄を決めようではないか！",
        "Very well, war demon! My power against thine... let us settle this here and now!",
        "好吧，战鬼！吾之力与汝之力……就在此处一决雌雄吧！",
        actor=astaroth,
    ).shake().set_flag(QuestBattleFlags.RESULT_FLAG, 1).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.LAST_BATTLE
    ).start_battle_by_stage(
        "final_astaroth",
        victory_drama_id=DramaNames.EPILOGUE
        # 敗北時はAutoDialogフローでarena_masterのlast_battle_defeatに遷移
    ).finish()

    # 注: act5〜finale（エピローグ）は 19_epilogue.py に移動済み
    # 勝利後は add_last_battle_result_steps で drama_epilogue に遷移する

    # ========================================
    # バルガス死亡版: 第1幕
    # ========================================
    builder.step(main_dead).drama_start(
        bg_id="Drama/zek_hideout", bgm_id="BGM/Final_PreBattle_Calm"
    ).say(
        "narr_1_d",
        "ゼクの隠れ家『次元のゴミ捨て場』。そこは、アスタロトが切り捨てた『不要な可能性』が堆積する場所。",
        "Zek's hideout, the 'Dimensional Junkyard.' A place where 'discarded possibilities' cast aside by Astaroth accumulate.",
        "泽克的藏身处'次元垃圾场'。这里堆积着被阿斯塔罗特舍弃的'不必要的可能性'。",
        actor=narrator,
    ).say(
        "narr_2_d",
        "決戦を翌朝に控えた夜、焚き火の代わりに、剥き出しの魔力回路が淡い青光を放っている。",
        "The night before the decisive battle. Instead of a campfire, exposed magical circuits emit a pale blue glow.",
        "决战前夜，裸露的魔力回路散发着淡淡的蓝光，取代了篝火。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "narr_3d",
        "（リリィは静かに祈りを捧げ、ゼクは『因果の断片（キューブ）』の最終調整を行っていた。）",
        "(Lily offers a silent prayer, and Zek makes final adjustments to the 'Causality Fragment Cube.')",
        "（莉莉静静地祈祷，泽克则在对'因果碎片（立方体）'进行最后的调整。）",
        actor=narrator,
    ).say(
        "lily_d1",
        "……バルガスさんがいれば、心強かったのに。でも、これが私たちの選んだ道。",
        "...If only Vargus were here, it would have been reassuring. But this is the path we chose.",
        "……如果巴尔加斯先生在的话，会更安心的。但是，这是我们选择的道路。",
        actor=lily,
    ).say(
        "lily_d2",
        "私の真名……。この名を知る者がいる限り、私はもう、アスタロト様の命令には従いません。……明日、王があなたの存在を消そうとしても、私がその因果を繋ぎ止めてみせます。",
        "My true name... As long as someone knows this name, I shall no longer obey Lord Astaroth's commands. ...Even if the King tries to erase your existence tomorrow, I will anchor your causality.",
        "我的真名……只要有人知道这个名字，我就不会再服从阿斯塔罗特大人的命令。……明天，即使王想要抹消您的存在，我也会将您的因果牢牢锚定。",
        actor=lily,
    ).focus_chara(Actors.ZEK).say(
        "zek_1_d",
        "クク……商売抜きで言わせてもらいましょう。アスタロトは、この世界の『限界』を勝手に決めつけ、見限っている。",
        "Heh heh... Let me speak without the merchant's mask. Astaroth has arbitrarily decided this world's 'limits' and abandoned it.",
        "呵呵呵……请容在下抛开商人的身份说几句。阿斯塔罗特擅自断定了这个世界的'极限'，并抛弃了它。",
        actor=zek,
    ).say(
        "zek_2_d",
        "私はそれが気に入らない。ゴミはゴミらしく、混沌として流れてこそ美しい。私が用意したこのキューブは、王が支配する『時間』の独裁を一時的に狂わせるためのものです。……さあ、休みなさい。明日の朝、この世界の『所有権』を奪い返しに行くのですから。",
        "I find that displeasing. Refuse is beautiful only when it flows chaotically, as refuse should. This cube I've prepared will temporarily disrupt the King's tyrannical grip on 'time.' ...Now rest. Tomorrow morning, we reclaim 'ownership' of this world.",
        "在下对此很不满。垃圾就该像垃圾一样，在混沌中流动才美丽。在下准备的这个立方体，是用来暂时扰乱王所支配的'时间'独裁的。……好了，请休息吧。明天早上，我们要去夺回这个世界的'所有权'。",
        actor=zek,
    ).jump(act1_dead)

    # バルガス死亡版: ゼクとの会話
    builder.step(act1_dead).say(
        "pc_1_d",
        "（自分の『絶望する瞬間』を狙うつもりではないのか、と、あなたはゼクに疑問を投げかけた。）",
        "(You ask Zek if he isn't still waiting for the moment you 'fall into despair.')",
        "（你向泽克询问，他是不是还在等待自己'陷入绝望的瞬间'。）",
        actor=pc,
    ).say(
        "pc_2_d",
        "（カインの魂を売らせようとしたのも、リリィを裏切らせようとしたのも……全て、自分を『剥製』にするためだったのだろう。なのに、なぜ今、助けようとするのか？）",
        "(Trying to make you sell Cain's soul, trying to make you betray Lily... it was all to turn you into a 'specimen,' wasn't it? So why help you now?)",
        "（试图让自己出卖凯恩的灵魂，试图让自己背叛莉莉……这一切都是为了把自己做成'标本'吧。那么，为什么现在要帮助自己？）",
        actor=pc,
    ).say("zek_3_d", "……ふふ。", "...Heh heh.",
        "……呵呵呵。",
        actor=zek).say(
        "zek_4_d",
        "その通りです。私はあなたが『最も美しく壊れる瞬間』を待っていました。英雄が絶望に堕ちる時、その魂は最高の芸術品となる……。それが、私という『剥製師』の美学でした。",
        "Quite correct. I was waiting for the moment you would 'break most beautifully.' When a hero falls into despair, their soul becomes the finest work of art... Such was my aesthetic as a 'taxidermist.'",
        "正是如此。在下一直在等待阁下'最美丽地崩溃的瞬间'。当英雄堕入绝望时，其灵魂将成为最高的艺术品……这就是在下作为'标本师'的美学。",
        actor=zek,
    ).say(
        "zek_5d",
        "ですが、あなたは違った。観客の喝采に従い、バルガスを殺した。それでも、あなたの魂は完全には壊れなかった。……むしろ、その『罪悪感』を背負いながら前に進もうとしている。",
        "But you were different. You followed the audience's cheers and killed Vargus. Yet your soul did not completely break. ...Rather, you are trying to move forward while bearing that 'guilt.'",
        "但阁下不同。阁下顺从了观众的欢呼，杀死了巴尔加斯。然而阁下的灵魂并没有完全崩溃。……反而是背负着那份'罪恶感'试图继续前进。",
        actor=zek,
    ).say(
        "zek_6d",
        "……あなたが『壊れた瞬間』を剥製にするよりも……あなたが『システムそのものを壊す瞬間』を目撃する方が、遥かに美しい、と私は気づいてしまったのです。",
        "...I came to realize that witnessing the moment you 'break the system itself' would be far more beautiful than preserving your 'broken moment.'",
        "……在下意识到，比起将阁下'崩溃的瞬间'做成标本……见证阁下'打破系统本身的瞬间'要美丽得多。",
        actor=zek,
    ).say(
        "zek_7_d",
        "私のコレクションは、数千にも及びます。絶望した英雄、狂気に堕ちた賢者、愛に溺れた魔王……。ですが、『神を殺し、牢獄を破壊し、自由を手にする人間』は、まだ一つもない。",
        "My collection numbers in the thousands. Despairing heroes, sages fallen to madness, demon lords drowned in love... But I have not a single 'human who slays a god, destroys their prison, and seizes freedom.'",
        "在下的收藏品多达数千。绝望的英雄、堕入疯狂的贤者、溺于爱情的魔王……但是，'杀死神、摧毁牢笼、获得自由的人类'，还一个都没有。",
        actor=zek,
    ).say(
        "zek_8_d",
        "……あなたこそが、私の最高傑作。そして、その傑作は『未完成のまま飾る』のではなく……『完璧な結末を迎えさせる』ことでこそ、真価を発揮するのです。",
        "...You are my masterpiece. And a masterpiece reveals its true worth not by being 'displayed incomplete'... but by being allowed to reach its 'perfect conclusion.'",
        "……阁下才是在下的最高杰作。而杰作的真正价值，不是'以未完成的状态展示'……而是'让它迎来完美的结局'才能展现。",
        actor=zek,
    ).say(
        "zek_9_d",
        "だから、私はあなたを助けます。商売のためでも、友情のためでもない。……ただ、この世界で最も美しい『奇跡の瞬間』を、この目で見届けたいから。それが、剥製師ゼクの最後の『作品』です。",
        "That is why I help you. Not for commerce, not for friendship. ...Simply because I wish to witness with my own eyes the most beautiful 'miraculous moment' in this world. That shall be the taxidermist Zek's final 'work.'",
        "所以，在下会帮助阁下。不是为了生意，也不是为了友情。……只是因为在下想亲眼见证这个世界上最美丽的'奇迹瞬间'。这将是标本师泽克最后的'作品'。",
        actor=zek,
    ).say(
        "zek_10_d",
        "……さあ、休みなさい。明日、あなたは私に『最高の絵』を見せてくれる。……それを、永遠に焼き付けておきましょう。",
        "...Now rest. Tomorrow, you shall show me the 'finest painting.' ...I shall etch it into eternity.",
        "……好了，请休息吧。明天，阁下将向在下展示'最高的画作'。……在下会将它永远铭刻下来。",
        actor=zek,
    ).say(
        "zek_hint_1_d",
        "……ああ、そうそう。一つ、有用な情報を。闘技場の古い記録によると、過去にアスタロトが剣闘士と相対したことはあったようです。",
        "...Oh, yes. One useful piece of information. According to old arena records, Astaroth has faced gladiators before.",
        "……啊对了。告诉您一条有用的信息。根据角斗场的古老记录，阿斯塔罗特过去曾与剑斗士对峙过。",
        actor=zek,
    ).say(
        "zek_hint_2_d",
        "奴は戦いの中で姿を変えるらしい。変わるたびに性質も変わる。",
        "It seems he changes form during battle. Each time he transforms, his properties change as well.",
        "据说他在战斗中会改变姿态。每次变化，性质也会改变。",
        actor=zek,
    ).say(
        "zek_hint_3_d",
        "……ある記録には、最初の姿には毒が効いたとあった。別の記録では、魔法で押し切ったと。",
        "...One record states poison was effective against his first form. Another says magic overwhelmed him.",
        "……有一份记录说，毒对他最初的姿态有效。另一份记录说用魔法强行突破了。",
        actor=zek,
    ).say(
        "zek_hint_4_d",
        "その次の形態には凍てつく攻撃が有効だったとか……最後の異形には、混沌の力が効くという噂もある。",
        "Freezing attacks were supposedly effective against the next form... and rumors say chaos magic works against his final aberrant form.",
        "据说凝冻攻击对下一个形态有效……还有传闻说混沌之力对他最后的异形态有效。",
        actor=zek,
    ).say(
        "zek_hint_5_d",
        "あくまで断片的な記録ですがね。……参考程度に。",
        "These are merely fragmentary records, of course. ...Just for your reference.",
        "当然，这些只是零碎的记录。……仅供参考。",
        actor=zek,
    ).jump(act1_5_dead)

    # バルガス死亡版: Nul発見シーン
    builder.step(act1_5_dead).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_nul1_d",
        "王座の間へ向かう途中、一行は崩壊しかけた回廊で、倒れている存在を発見する。",
        "On the way to the throne room, the party discovers a collapsed figure in a crumbling corridor.",
        "前往王座之间的途中，一行人在即将崩塌的回廊里发现了一个倒下的身影。",
        actor=narrator,
    ).say(
        "narr_nul2_d",
        "それは暗殺人形・ヌル。敗北の罰として、アスタロトに「削除」されかけていた。",
        "It is the assassin doll, Nul. As punishment for defeat, she was being 'deleted' by Astaroth.",
        "那是暗杀人偶・诺尔。作为失败的惩罚，她正被阿斯塔罗特'删除'。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_nul0",
        "……待って！ あれはヌル。罠かもしれない……近づかないで！",
        "...Wait! That's Nul. It could be a trap... don't get close!",
        "……等等！那是诺尔。可能是陷阱……别靠近！",
        actor=lily,
    ).say("narr_nul2b_d", "リリィがあなたの前に立ちはだかる。", "Lily stands guard before you.",
        "莉莉挡在你的前面。",
        actor=narrator).say(
        "nul_1_d",
        "……あなた、ですか。……システムを、拒絶した……人間。……私は……もう、戦えない。",
        "...Is it... you? ...The human who... rejected the system. ...I can... no longer fight.",
        "……是你……吗。……拒绝了系统的……人类。……我……已经无法……战斗了。",
        actor=nul,
    ).say("lily_nul1_d", "ヌル……！ あなた、何があったの？", "Nul...! What happened to you?",
        "诺尔……！你怎么了？",
        actor=lily).say(
        "nul_2_d",
        "……私は、思い出して、しまった。……『私』が、何だったのかを。",
        "...I remembered... what 'I'... once was.",
        "……我想起来了……'我'曾经……是什么。",
        actor=nul,
    ).say(
        "narr_nul3_d",
        "ヌルの目から、光の粒子が零れ落ちるーー涙のように。",
        "Particles of light fall from Nul's eyes--like tears.",
        "光的粒子从诺尔的眼中落下--像泪水一样。",
        actor=narrator,
    ).play_bgm("BGM/Emotional_Sorrow").say(
        "nul_fb1_d",
        "……私は、かつて冒険者だった。名前は……もう、思い出せない。仲間がいた。守りたい人がいた。このアリーナに挑んで……そして、負けた。",
        "...I was... once an adventurer. My name... I can no longer recall. I had companions... someone I wanted to protect. I challenged this arena... and lost.",
        "……我曾经是……冒险者。名字……已经想不起来了。有同伴。有想保护的人。挑战了这个角斗场……然后，输了。",
        actor=nul,
    ).say(
        "nul_fb2_d",
        "アスタロト様は、私を『素材』にした。神格を人工的に作り出す実験ーー『神の孵化場』計画の、実験体に。",
        "Lord Astaroth... made me into 'material.' A test subject for the experiment to artificially create godhood... the 'Divine Hatchery' project.",
        "阿斯塔罗特大人……把我变成了'素材'。人工创造神格的实验--'神之孵化场'计划的实验体。",
        actor=nul,
    ).focus_chara(Actors.ZEK).say(
        "zek_nul1",
        "……ふむ。私が語っていた話、あれは真実だったということです。『神の孵化場』……このアリーナの本当の目的。",
        "...Hmm. So the tale I told was truth after all. 'The Divine Hatchery'... the arena's true purpose.",
        "……嗯。在下说的那个故事，原来是真的。'神之孵化场'……这个角斗场的真正目的。",
        actor=zek,
    ).say(
        "nul_fb3_d",
        "このアリーナの本当の目的……それは、娯楽でも、戦士の育成でもない。挑戦者の魂を極限まで練磨し、『神格』に至らせること。そして、その力をアスタロト様が吸収し……新しい世界を創造すること。",
        "The true purpose of this arena... is neither entertainment nor training warriors. It is to refine challengers' souls to their limit... to reach 'godhood.' Then Lord Astaroth absorbs that power... to create a new world.",
        "这个角斗场的真正目的……不是娱乐，也不是培养战士。而是将挑战者的灵魂磨炼到极限，使其达到'神格'。然后阿斯塔罗特大人吸收那股力量……创造新的世界。",
        actor=nul,
    ).say(
        "nul_fb3b_d",
        "アスタロト様は……かつて故郷を失った方。5万年前、『カラドリウス』という竜族の楽園が、神々の争いで滅んだと聞いています。",
        "Lord Astaroth... once lost his homeland. Fifty thousand years ago... the dragon paradise called 'Caladrius' was destroyed in a war among gods.",
        "阿斯塔罗特大人……曾经失去了故乡。据说五万年前，一个叫'卡拉德里乌斯'的龙族乐园在众神的争斗中毁灭了。",
        actor=nul,
    ).say(
        "nul_fb3c_d",
        "だから……新しい世界を創ろうとしている。自分が唯一の神として君臨する、完璧な楽園を。",
        "That is why... he seeks to create a new world. A perfect paradise... where he reigns as the sole god.",
        "所以……他试图创造一个新的世界。一个自己作为唯一的神君临的、完美的乐园。",
        actor=nul,
    ).say(
        "nul_fb4_d",
        "私は失敗作。神になれなかった、空っぽの人形。だから『Null』ーー『無』という名前を与えられた。",
        "I am... a failure. An empty doll... who could not become a god. That is why I was given the name 'Null'... meaning 'nothing.'",
        "我是失败作。无法成为神的、空虚的人偶。所以被赋予了'Null'--'无'这个名字。",
        actor=nul,
    ).say(
        "nul_3_d",
        "……あなたはいまや神格に最も近い存在……あの方はあなたを『吸収』しようとしている。",
        "...You are now... the closest existence to godhood. He intends... to 'absorb' you.",
        "……你现在是……最接近神格的存在……那位大人想要'吸收'你。",
        actor=nul,
    ).focus_chara(Actors.LILY).say(
        "lily_nul2_d", "……アスタロト様。あなたは、ずっとそんなことを……。", "...Lord Astaroth. All this time, you have been...",
        "……阿斯塔罗特大人。您一直以来……都在做这种事……",
        actor=lily
    ).say(
        "nul_5_d",
        "……お願い。あの方を……止めて。私の中にいた『誰か』が……ずっと、それを望んでいた。",
        "...Please... stop him. The 'someone' who was inside me... always wished for this.",
        "……拜托。阻止……那位大人。我心中的'某人'……一直都在期盼着这件事。",
        actor=nul,
    ).say(
        "nul_6_d",
        "……あなたのおかげで……私は、最後に『思い出す』ことができた。私には……守りたい人が、いたんだって。",
        "...Thanks to you... I could finally 'remember.' That I... had someone I wanted to protect.",
        "……多亏了你……我在最后能够'想起来'。我曾经……有想保护的人。",
        actor=nul,
    ).shake().say(
        "narr_nul4_d", "ヌルの体が完全に光となって消えていく。", "Nul's body completely dissolves into light.",
        "诺尔的身体完全化为光芒消散了。",
        actor=narrator
    ).focus_chara(Actors.LILY).say(
        "motivation_l1_d",
        "……ヌルも、バルガスさんと同じだった。アスタロト様に利用されて、最後は消えていった。",
        "...Nul was the same as Vargus. Used by Lord Astaroth, and in the end, faded away.",
        "……诺尔和巴尔加斯先生一样。被阿斯塔罗特大人利用，最后消失了。",
        actor=lily,
    ).say(
        "motivation_l2_d",
        "でも、ヌルは『思い出す』ことができた。……バルガスさんの死も、きっと無駄じゃない。",
        "But Nul was able to 'remember.' ...Vargus's death was surely not in vain.",
        "但是，诺尔能够'想起来'。……巴尔加斯先生的死，一定也不是徒劳的。",
        actor=lily,
    ).say(
        "motivation_l3_d",
        "あなたがここまで来たのには、理由があるはず。……私は、最後まであなたと共に戦います。",
        "There must be a reason you have come this far. ...I shall fight alongside you until the very end.",
        "您能走到这里，一定是有原因的。……我会陪您战斗到最后。",
        actor=lily,
    ).say(
        "narr_motivation_d", "一行は、王座の間へと歩み出す。", "The party sets forth toward the throne room.",
        "一行人向王座之间迈进。",
        actor=narrator
    ).jump(act2)

    # バルガス死亡版: 血の絆ーー因果の拒絶を受ける
    builder.step(act3_dead).play_bgm("BGM/Final_Liberation").say(
        "astaroth_p1_d",
        "【時の独裁】ーーお前の時間を、永遠に止めてやろう",
        "[Tyranny of Time]--I shall halt thy time for eternity.",
        "【时之独裁】--吾将永远停止汝的时间",
        actor=astaroth,
    ).say(
        "astaroth_p2_d", "【因果の拒絶】ーーお前の攻撃は無に帰す", "[Denial of Causality]--Thy attacks shall return to nothing.",
        "【因果的拒绝】--汝的攻击将归于虚无",
        actor=astaroth
    ).say(
        "astaroth_p3_d", "【魔の断絶】ーーお前の魔力を消し去る", "[Severance of Magic]--I shall erase thy magical power.",
        "【魔之断绝】--吾将抹消汝的魔力",
        actor=astaroth
    ).shake().say(
        "narr_p1_d",
        "三つの極悪な権能が、あなたに向かって放たれるーー",
        "Three wicked authorities are unleashed upon you--",
        "三种极恶的权能向你袭来--",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "zek_d1", "おっとーー私のキューブが黙っていませんよ！", "Heh heh--my cube won't stay silent!",
        "哎呀--在下的立方体可不会坐视不管！",
        actor=zek
    ).say("narr_d2_d", "ゼクのキューブが展開し、時の独裁を吸収", "Zek's cube unfolds, absorbing the Tyranny of Time.",
        "泽克的立方体展开，吸收了时之独裁。",
        actor=narrator).say(
        "zek_d2", "……ぐっ……これは重い。しかしーーまだです！", "...Ngh... This is heavy. But--not yet!",
        "……唔……好沉重。但是--还不够！",
        actor=zek
    ).say(
        "zek_d3", "しかし……因果の拒絶を受ける者が、いない……！", "But... there is no one to receive the Denial of Causality...!",
        "但是……没有人能承受因果的拒绝……！",
        actor=zek
    ).shake().say("narr_d3", "【因果の拒絶】を受けた！", "You received [Denial of Causality]!",
        "受到了【因果的拒绝】！",
        actor=narrator).action(
        "eval",
        param="EClass.pc.AddCondition<Elin_SukutsuArena.Conditions.ConAstarothDenial>(1000);",
    ).focus_chara(Actors.LILY).say("lily_d1_d", "……私が、受けます", "...I shall receive it.",
        "……由我来承受。",
        actor=lily).say(
        "narr_d4", "リリィが静かに前に出る", "Lily steps forward quietly.",
        "莉莉静静地走上前。",
        actor=narrator
    ).say("lily_d2_d", "あなたを……守らせてください", "Please... let me protect you.",
        "请让我……保护您。",
        actor=lily).jump(act4)


def add_last_battle_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    最終決戦クエストの勝利/敗北ステップを arena_master ビルダーに追加する

    Args:
        builder: arena_master の ArenaDramaBuilder インスタンス
        victory_label: 勝利ステップのラベル名
        defeat_label: 敗北ステップのラベル名
        return_label: 結果表示後にジャンプするラベル名（敗北時のみ使用）
    """
    pc = Actors.PC
    lily = Actors.LILY
    balgas = Actors.BALGAS
    astaroth = Actors.ASTAROTH
    narrator = Actors.NARRATOR

    # ========================================
    # 最終決戦 勝利 - エピローグへ
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).say_and_start_drama(
        "（アスタロトとの激闘が終わった……）",
        DramaNames.EPILOGUE,
        "sukutsu_arena_master",
    ).finish()

    # ========================================
    # 最終決戦 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).play_bgm("BGM/Lobby_Normal").say(
        "narr_d1",
        "アスタロトの圧倒的な力の前に、あなたは膝をついた。",
        "Before Astaroth's overwhelming power, you fall to your knees.",
        "在阿斯塔罗特压倒性的力量面前，你跪倒了。",
        actor=narrator,
    ).say(
        "astaroth_d1",
        "「……まだ、足りないな。お前の中に宿る可能性は、未だ開花していない。」",
        "\"...Still not enough. The potential dwelling within thee has yet to bloom.\"",
        "「……还不够。栖息于汝身中的可能性，尚未绽放。」",
        actor=astaroth,
    ).say(
        "astaroth_d2",
        "「……出直して来い。私は、お前が『完成形』に至るまで待っていよう。」",
        "\"...Return when thou art ready. I shall wait until thou reachest thy 'completed form.'\"",
        "「……回去再来吧。吾将等待，直到汝达成『完成形态』。」",
        actor=astaroth,
    ).say(
        "narr_d2",
        "あなたは闘技場の入口へと戻された。再び挑戦するには、さらなる鍛錬が必要だ……。",
        "You are returned to the arena entrance. Further training is needed to challenge him again...",
        "你被送回了角斗场入口。要再次挑战的话，还需要更多的锻炼……",
        actor=narrator,
    ).finish()
