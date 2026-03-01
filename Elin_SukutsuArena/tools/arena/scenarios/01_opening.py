# -*- coding: utf-8 -*-
"""
オープニングドラマ「虚無の呼び声」
"""

from arena.builders import DramaBuilder
from arena.data import (
    Actors,
    FlagValues,
    Keys,
    QuestIds,
    Rank,
    SessionKeys,
)


def define_opening_drama(builder: DramaBuilder):
    """
    オープニングドラマ「虚無の呼び声」を定義
    フラグ管理システムを使用したバージョン + 演出強化
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    vargus = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")

    # ラベル定義
    main = builder.label("main")
    greed = builder.label("greed")
    battle = builder.label("battle")
    void = builder.label("void")
    pride = builder.label("pride")
    drift = builder.label("drift")
    ending = builder.label("ending")

    # ==== メインステップ: 異次元への転落 ====
    builder.step(main)

    # === 演出: ドラマ開始（fadeOut → 背景設定 → fadeIn → BGM）===
    builder.drama_start(
        bg_id="Drama/arena_lobby", bgm_id="BGM/sukutsu_arena_opening", fade_duration=3.0
    )

    # === 演出: グリッチ + 独白 ===
    builder.glitch().say(
        "narr1",
        "(……どこだ、ここは。)",
        "(...Where am I?)",
        "(……这是哪里。)",
        actor=pc,
    ).say(
        "narr2",
        "(奇妙な場所だ。時間の流れすら曖昧に感じる。)",
        "(A strange place. Even the flow of time feels... uncertain.)",
        "(奇怪的地方。连时间的流逝都感觉模糊不清。)",
        actor=pc,
    ).say(
        "narr3",
        "(遠くから響く、雷鳴のような音……歓声か？ 何かに見られているーーその感覚だけは確かだ。)",
        "(A distant sound, like thunder... or is it cheering? One thing is certain--something is watching me.)",
        "(远处传来的声音，像雷鸣……还是欢呼？唯一确定的是--有什么东西在注视着我。)",
        actor=pc,
    )

    # === リリィに声をかけられる ===
    builder.focus_chara(Actors.LILY).say(
        "lily1",
        "……あら。召喚の儀も、空間の歪みもなしに、ここまで迷い込む『生きた肉』がいるなんて。",
        "...Oh my. Living flesh that wandered here without a summoning ritual or spatial distortion? How unusual.",
        "……哎呀。没有召唤仪式，也没有空间扭曲，竟然有『活着的血肉』迷路到这里来。",
        actor=lily,
    ).say(
        "lily1b",
        "……おかしいですね。普通、この狭間に落ちた者は、イルヴァとの繋がりを失うはずなのに。あなた、まだ『帰り道』を持っている……？ イルヴァの神々の加護でもあるのかしら。",
        "...How peculiar. Normally, those who fall into this rift lose their connection to Ylva. Yet you still have a 'way back'...? Do the gods of Ylva favor you, perhaps?",
        "……真奇怪呢。通常，落入这夹缝的人会失去与伊尔瓦的联系。您居然还保有『归途』……？难道是受到了伊尔瓦诸神的庇护？",
        actor=lily,
    ).say(
        "lily2",
        "まあ、いいでしょう。お客様、それとも……新たな『商品』かしら？ここは次元の狭間、そして絶望の始まり。",
        "Well, no matter. Are you a customer, or perhaps... new 'merchandise'? This is the dimensional rift--and the beginning of despair.",
        "算了，无所谓。您是客人，还是……新的『商品』呢？这里是次元夹缝，也是绝望的开端。",
        actor=lily,
    ).say(
        "lily3",
        "……あなたは自由に出入りできるようですけれど、ここの『仕組み』はあそこにいる「飲んだくれ」に聞くのが作法ですから。",
        "...You seem free to come and go as you please, but protocol dictates you speak with that 'drunkard' over there about how things work here.",
        "……您似乎可以自由出入，但按照规矩，这里的『规则』要向那边的「醉鬼」询问。",
        actor=lily,
    )

    # バルガス登場（フォーカスにウェイト内蔵）
    builder.focus_chara(Actors.BALGAS).say(
        "vargus1",
        "これはこれは。ちょっとばかり魔物に追われて、運悪く次元の割れ目に滑り落ちた『迷い犬』か？",
        "Well, well. A stray dog who got chased by monsters and stumbled into a dimensional crack, is it?",
        "哟哟。被魔物追着跑，倒霉掉进次元裂缝的『迷途小狗』？",
        actor=vargus,
    ).say(
        "vargus1b",
        "……お前、まだイルヴァの神々との繋がりが切れてねえな。こいつは珍しい。普通、この狭間に落ちりゃ、どんな加護も消し飛ぶんだが。",
        "...Huh. Your connection to Ylva's gods ain't severed yet. That's rare. Normally, falling into this rift strips away any divine protection.",
        "……哼，你跟伊尔瓦诸神的联系还没断。这可稀罕。一般掉进这夹缝，什么加护都会消失殆尽。",
        actor=vargus,
    ).say(
        "vargus2",
        "ここは、選ばれた狂人どもが、『観客』の暇つぶしのために、殺し合うための場所だ。お前は『帰れる』んだろう？ なら、さっさと帰んな。ここに留まる理由なんざ、正気の奴にはねえはずだ。",
        "This place is where chosen madmen slaughter each other for the 'audience's entertainment. You can go back, right? Then get the hell out. No sane person has any reason to stay here.",
        "这里是被选中的疯子们为了『观众』的消遣而互相厮杀的地方。你能『回去』对吧？那就赶紧滚。正常人没有理由留在这里。",
        actor=vargus,
    ).say(
        "vargus3",
        "……あ？ なんだその目は？ 言いたいことでもあるのか？",
        "...Huh? What's with that look? Got somethin' to say?",
        "……啊？什么眼神？有话要说？",
        actor=vargus,
    )

    # プレイヤー決意 (選択肢)
    vargus_react = builder.label("vargus_react")

    builder.choice(
        vargus_react,
        "腕っぷしには自信がある。闘技場に参加したい",
        "I'm confident in my combat skills. I want to join the arena.",
        "我对自己的武力很有信心。我想参加角斗场。",
        text_id="c_resolve_fight",
    ).choice(
        vargus_react,
        "ここで魔法の腕試しをしたい",
        "I'd like to test my magical abilities here.",
        "我想在这里试试自己的魔法实力。",
        text_id="c_resolve_magic",
    ).choice(
        vargus_react,
        "心強い仲間と一緒なら怖くはない",
        "With reliable allies, I have nothing to fear.",
        "有可靠的伙伴在，我什么都不怕。",
        text_id="c_resolve_survive",
    ).choice(
        vargus_react,
        "観光がてら、試合に参加したい",
        "I'll participate in a match while sightseeing.",
        "顺便观光，参加一场比赛。",
        text_id="c_resolve_drift",
    )

    # バルガス反応
    builder.step(vargus_react).shake().say(
        "vargus_react1",
        "……ハッ、正気か？ 帰れるのに、自分の意志でこの地獄の底に留まりてえと言いやがったか。",
        "...Hah! Are you insane? You can leave, but you're choosing to stay in this hellhole of your own free will?",
        "……哈！你疯了？明明能回去，却自愿留在这地狱深渊？",
        actor=vargus,
    ).say(
        "vargus_react2",
        "いいぜ。囚われた奴らが生き残るために戦うのとは訳が違う。『選んで』来る奴は、最高に面白いか、最高に馬鹿か、どっちかだ。\n聞かせろ、お前は何のために戦う？",
        "Fine by me. It's different from those who fight just to survive. Those who 'choose' to come here are either extremely entertaining or extremely stupid.\nTell me--what do you fight for?",
        "行啊。这跟被囚禁而不得不战斗求生的家伙不一样。『选择』来这里的人，要么极其有趣，要么极其愚蠢。\n说吧，你为了什么而战？",
        actor=vargus,
    )

    # 動機選択 (フラグ管理システムのキーを使用)
    builder.choice(
        greed,
        "【強欲】富と名声、そして力！",
        "[Greed] Wealth, fame, and power!",
        "【贪婪】财富、名声、还有力量！",
        text_id="c1",
    ).choice(
        battle,
        "【求道】己の限界を量るためだ。強い奴と戦わせろ",
        "[Seeker] To test my limits. Let me fight the strong.",
        "【求道】为了探索自己的极限。让我与强者一战。",
        text_id="c2",
    ).choice(
        void,
        "【虚無】もとより帰る場所などない",
        "[Void] I have nowhere to return to.",
        "【虚无】我本来就无处可归。",
        text_id="c3",
    ).choice(
        pride,
        "【傲慢】闘技場を支配下に置くのも悪くない",
        "[Pride] Taking over this arena wouldn't be so bad.",
        "【傲慢】把角斗场纳入掌控也不错。",
        text_id="c4",
    ).choice(
        drift,
        "【狂人】理由はない",
        "[Madman] No reason.",
        "【狂人】没有理由。",
        text_id="c5",
    ).on_cancel(drift)

    # --- Greed Route ---
    builder.step(greed).say(
        "greed_v1",
        "ハッ！ わかりやすくていいぜ。金と権力、そして力……地上のクズどもが一生かけて追いまわす、人を魅了してやまないゴミ屑だ。ここでゲロゲロ程も価値がないとしてもな。",
        "Hah! Straightforward. I like it. Gold, power, and strength... the garbage that surface scum chase their whole lives, that enthralling trash. Even if it's worth less than vomit here.",
        "哈！够直白，我喜欢。金钱、权力、力量……地上那些渣滓穷尽一生追逐的、令人着迷的垃圾。哪怕在这里连呕吐物都不如。",
        actor=vargus,
    ).say(
        "greed_v2",
        "お前が手にするのは、神々すら平伏させる「圧倒的な階位」……。それが欲しけりゃ、他人の内臓を積み上げて階段を作るんだな。",
        "What you'll gain is 'overwhelming rank'--enough to make even gods kneel. If that's what you want, build your stairway from other people's guts.",
        "你能得到的是让神明都俯首的『压倒性阶位』……想要的话，就用别人的内脏堆成台阶吧。",
        actor=vargus,
    ).say(
        "greed_l1",
        "ふふ、強欲な魂は好物ですよ。あなたの掛け金……その何割を私が手数料としていただくことになるのか、楽しみです。精々、死なずに稼いでくださいね？",
        "Hehe, greedy souls are my favorite. I wonder how much of your winnings I'll take as commission... Do try to earn without dying, won't you?",
        "呵呵，贪婪的灵魂是我的最爱。您的赌注……我能抽取多少手续费呢，真令人期待。请尽量别死着赚钱哦？",
        actor=lily,
    ).jump(ending)

    # --- Battle Route ---
    builder.step(battle).say(
        "battle_v1",
        "……チッ、一番厄介な手合いだ。己の限界だと？ 深淵を前にそんなセリフが吐けるか。",
        "...Tch. The most troublesome type. Test your limits? You can say that standing before the abyss?",
        "……啧，最麻烦的类型。探索极限？面对深渊还能说出这种话？",
        actor=vargus,
    ).say(
        "battle_v2",
        "いいぜ、お前のその真っ直ぐな瞳が、絶望で濁っていく様を見るのは……",
        "Fine. Watching those clear eyes of yours cloud over with despair...",
        "行吧，看着你那双清澈的眼睛被绝望染浊……",
        actor=vargus,
    ).say(
        "battle_v3",
        "……かつての俺を見るようで、反吐が出るがな。",
        "...reminds me of myself. Makes me sick.",
        "……就像看到过去的自己，让人作呕。",
        actor=vargus,
    ).say(
        "battle_l1",
        "……あら。戦うこと自体が目的、ですか。あなたの放つその濃密な「闘気」……少し当てられただけで、サキュバスとしての本能が疼いてしまいます。壊れてしまう前に、その輝きを存分に見せてくださいね。",
        "...Oh my. Fighting itself is your purpose? That dense 'battle aura' you exude... just a taste makes my succubus instincts ache. Do show me that brilliance before you shatter.",
        "……哎呀。战斗本身就是目的吗。您散发的浓郁『斗气』……仅是稍微感受一下，魅魔的本能就开始躁动了。在崩溃之前，请尽情展现那份光辉吧。",
        actor=lily,
    ).jump(ending)

    # --- Void Route ---
    builder.step(void).say(
        "void_v1",
        "……フン、訳ありか。だがな小僧、ここは逃げ込むための掃き溜めじゃねえ。生への執着を捨てた奴から死んでいく。",
        "...Hmph. Got baggage, huh? But kid, this ain't some dump to run away to. Those who abandon their will to live die first.",
        "……哼，有隐情吗。不过小子，这里不是用来逃避的垃圾场。放弃求生意志的人最先死。",
        actor=vargus,
    ).say(
        "void_v2",
        "……いいか、戦いの中でしか己の輪郭を保てねえってんなら、死に物狂いで剣を振れ。そうすりゃ、その虚無も少しは埋まるかもしれねえぜ。",
        "...Listen. If you can only hold onto yourself through battle, then swing your sword like your life depends on it. Maybe that'll fill some of that void.",
        "……听好了，如果只有在战斗中才能保持自我的话，就拼命挥剑吧。说不定能稍微填补那份虚无。",
        actor=vargus,
    ).say(
        "void_l1",
        "……居場所を求めて、わざわざ異次元まで。少し同情してしまいますね。ですが、事務手続きに私情は挟みませんよ？",
        "...Seeking a place to belong, all the way to another dimension. I almost pity you. But I don't let personal feelings interfere with paperwork.",
        "……为了寻找容身之处，特地来到异次元。有点同情您呢。不过，办事手续不会掺杂私人感情哦？",
        actor=lily,
    ).jump(ending)

    # --- Pride Route ---
    builder.step(pride).shake().say(
        "pride_v1",
        "……ハハハ！傑作だ！聞こえたかリリィ？ この新入り、初日から『王』を気取ってやがる！",
        "...Hahaha! Priceless! Did you hear that, Lily? This rookie's acting like a king on day one!",
        "……哈哈哈！绝了！听到了吗莉莉？这新人第一天就摆出『王者』架势！",
        actor=vargus,
    ).say(
        "pride_v2",
        "その青臭さには反吐が出るが、傲慢さが武器になることもある。神を殺すのはいつだって、己の身の程を知らぬ大馬鹿野郎だ。",
        "That naivety makes me sick, but arrogance can be a weapon. The ones who kill gods are always fools who don't know their place.",
        "那股青涩让人恶心，但傲慢有时也能成为武器。弑神的永远是不知天高地厚的大蠢货。",
        actor=vargus,
    ).say(
        "pride_l1",
        "グランドマスターの座を狙うなんて。ふふ、夢物語でも期待しておきましょう。",
        "Aiming for the Grand Master's seat? Hehe, I'll look forward to this fairy tale.",
        "觊觎大师的宝座吗。呵呵，就当作童话故事期待一下吧。",
        actor=lily,
    ).jump(ending)

    # --- Drift (Madman) Route ---
    builder.step(drift).say(
        "drift_v1",
        "……ヤク中か、それとも脳みそまでエーテルに冒されたか。会話もできねえ壊れた玩具に用はねえんだがな。",
        "...A junkie? Or has ether rotted your brain? I got no use for a broken toy that can't even hold a conversation.",
        "……瘾君子？还是脑子被以太侵蚀了？连对话都做不到的坏掉的玩具我可没兴趣。",
        actor=vargus,
    ).say(
        "drift_l1",
        "あら、私は嫌いではありませんよ？ 理由なき衝動ほど、純粋で美しいものはありません。あなたのその濁った瞳……何を見るのか楽しみです。",
        "Oh my, I don't dislike it. Nothing is purer or more beautiful than impulse without reason. Those clouded eyes of yours... I wonder what they'll see.",
        "哎呀，我倒不讨厌呢？没有理由的冲动，才是最纯粹、最美丽的。您那浑浊的眼眸……会看到什么呢，真令人期待。",
        actor=lily,
    ).jump(ending)

    # --- Ending ---
    builder.step(ending).shake().say(
        "end_v1",
        "リリィ！ こいつの名前を剣闘士の列に書き加えろ！",
        "Lily! Add this one's name to the gladiator roster!",
        "莉莉！把这家伙的名字加到角斗士名册上！",
        actor=vargus,
    ).say(
        "end_v2",
        "お前がただの肉塊か、それとも多少は骨のある肉塊か……。証明してみせな。",
        "Whether you're just a lump of flesh or one with some spine... prove it.",
        "你是普通的肉块，还是有点骨气的肉块……证明给我看。",
        actor=vargus,
    ).set_flag(Keys.RANK, 0).set_flag(SessionKeys.GLADIATOR, 1).set_flag(
        SessionKeys.ARENA_STAGE, 1
    ).set_flag(SessionKeys.OPENING_SEEN, 1).complete_quest(
        QuestIds.OPENING
    ).start_journal_quest("sukutsu_arena").action(
        "eval", param="LayerDrama.haltPlaylist = false;"
    ).drama_end(fade_duration=1.0)
