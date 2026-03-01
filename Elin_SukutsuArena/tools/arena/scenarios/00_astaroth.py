# -*- coding: utf-8 -*-
"""
00_astaroth.py - アスタロト（グランドマスター）のメインダイアログ

NPCクリック時の会話処理
- メインクエスト完了前（VS_ASTAROTH前）のみ会話可能
- FUGITIVE状態後はロビーから不在（会話不可）
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QUEST_DONE_PREFIX, QuestIds


def define_astaroth_main_drama(builder: DramaBuilder):
    """
    アスタロトのメインダイアログ
    NPCクリック時に表示される会話
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    astaroth = builder.register_actor(Actors.ASTAROTH, "アスタロト", "Astaroth")

    # ========================================
    # ラベル定義
    # ========================================
    main = builder.label("main")
    end = builder.label("end")
    greeting = builder.label("greeting")
    choices = builder.label("choices")

    # サブメニュー
    ask_arena = builder.label("ask_arena")
    arena_purpose = builder.label("arena_purpose")
    arena_audience = builder.label("arena_audience")
    ask_others = builder.label("ask_others")
    about_balgas = builder.label("about_balgas")
    about_lily = builder.label("about_lily")
    about_zek = builder.label("about_zek")
    about_nul = builder.label("about_nul")
    about_iris = builder.label("about_iris")
    ask_past = builder.label("ask_past")

    # エピローグ後ラベル
    post_game = builder.label("post_game")
    post_game_choices = builder.label("post_game_choices")
    quest_done_last_battle = f"{QUEST_DONE_PREFIX}{QuestIds.LAST_BATTLE}"

    # ========================================
    # エントリーポイント
    # ========================================
    # エピローグ完了後は専用フローへ（FUGITIVEチェックより優先）
    # FUGITIVE状態（VS_ASTAROTH後、エピローグ前）は会話不可
    builder.step(main).branch_if(
        quest_done_last_battle, "==", 1, post_game
    ).branch_if(Keys.FUGITIVE, "==", FlagValues.TRUE, end).jump(greeting)

    # ========================================
    # 挨拶
    # ========================================
    builder.step(greeting).say(
        "greet",
        "……ごきげんよう。私に用があるのか？",
        "...Good day. Do you have business with me?",
        "……日安。你有何事要找我？",
        actor=astaroth,
    ).jump(choices)

    # ========================================
    # メイン選択肢
    # ========================================
    builder.step(choices).choice(
        ask_arena,
        "この闘技場について",
        "About this arena",
        "关于这个角斗场",
        text_id="c_arena",
    ).choice(
        ask_others,
        "他の者について",
        "About the others",
        "关于其他人",
        text_id="c_others",
    ).choice(
        ask_past,
        "5万年前のこと",
        "About 50,000 years ago",
        "关于五万年前",
        text_id="c_past",
    ).choice(
        end, "失礼する", "I shall take my leave", "告辞", text_id="c_bye"
    ).on_cancel(end)

    # ========================================
    # 闘技場について
    # ========================================
    builder.step(ask_arena).say(
        "ask_arena",
        "……この場所について知りたいか。何を聞きたい？",
        "...You wish to know about this place. What would you ask?",
        "……想了解这个地方吗。你想问什么？",
        actor=astaroth,
    ).choice(
        arena_purpose,
        "目的は？",
        "What is its purpose?",
        "目的是什么？",
        text_id="c_purpose",
    ).choice(
        arena_audience,
        "観客について",
        "About the audience",
        "关于观众",
        text_id="c_audience",
    ).choice(choices, "戻る", "Back", "返回", text_id="c_back_arena").on_cancel(choices)

    # 闘技場の目的
    builder.step(arena_purpose).say(
        "purpose_1",
        "……この場所の目的？ 愚かな問いだ。",
        "...The purpose of this place? What a foolish question.",
        "……这个地方的目的？愚蠢的问题。",
        actor=astaroth,
    ).say(
        "purpose_2",
        "強者を選別し、弱者を淘汰する。それだけのこと。",
        "To select the strong and eliminate the weak. Nothing more.",
        "选拔强者，淘汰弱者。仅此而已。",
        actor=astaroth,
    ).say(
        "purpose_3",
        "……だが、真の目的を知りたいのなら……まずは、生き残ることだ。",
        "...But if you wish to know the true purpose... first, you must survive.",
        "……但若想知晓真正的目的……首先得活下去。",
        actor=astaroth,
    ).say(
        "purpose_4",
        "その資格を得た者にのみ、私は『真実』を語る。",
        "Only to those who earn that right shall I speak the 'truth'.",
        "唯有获得那资格之人，我才会诉说『真相』。",
        actor=astaroth,
    ).jump(ask_arena)

    # 観客について
    builder.step(arena_audience).say(
        "audience_1",
        "……観客？ ああ、あの連中か。",
        "...The audience? Ah, that lot.",
        "……观众？啊，那群家伙吗。",
        actor=astaroth,
    ).say(
        "audience_2",
        "彼らは『物語』を欲している。戦いを、苦悩を、絶望を、そして……稀な勝利を。",
        "They crave 'stories'. Battles, suffering, despair, and... rare victories.",
        "他们渴望『故事』。战斗、苦难、绝望，以及……罕见的胜利。",
        actor=astaroth,
    ).say(
        "audience_3",
        "お前の足掻きは、彼らにとっては上等な『娯楽』だ。",
        "Your pathetic struggles are fine 'entertainment' for them.",
        "你的挣扎对他们而言是上好的『娱乐』。",
        actor=astaroth,
    ).say(
        "audience_4",
        "……だが、案ずるな。彼らの注目は、時にお前を『守る』こともある。主人公は、そう簡単には死なないものだ。",
        "...But fret not. Their attention may sometimes 'protect' you. The protagonist does not die so easily.",
        "……但无须担忧。他们的关注有时也会『保护』你。主角不会轻易死去。",
        actor=astaroth,
    ).jump(ask_arena)

    # ========================================
    # 他の者について
    # ========================================
    builder.step(ask_others).say(
        "ask_others",
        "……誰のことが知りたい？",
        "...Whom do you wish to know about?",
        "……想知道谁的事？",
        actor=astaroth,
    ).choice(about_balgas, "バルガス", "Vargus", "巴尔加斯", text_id="c_balgas").choice(
        about_lily, "リリィ", "Lily", "莉莉", text_id="c_lily"
    ).choice(about_zek, "ゼク", "Zek", "泽克", text_id="c_zek").choice(
        about_nul, "ヌル", "Null", "Nul", text_id="c_nul"
    ).choice(about_iris, "アイリス", "Iris", "艾莉丝", text_id="c_iris").choice(
        choices, "戻る", "Back", "返回", text_id="c_back_others"
    ).on_cancel(choices)

    # バルガスについて
    builder.step(about_balgas).say(
        "balgas_1",
        "……バルガスか。あれは『壊れかけの剣』だ。",
        "...Vargus? That one is a 'broken blade'.",
        "……巴尔加斯吗。那是把『残缺的剑』。",
        actor=astaroth,
    ).say(
        "balgas_2",
        "35年前、私があの男を拾った時……あれの目は、既に死んでいた。",
        "Thirty-five years ago, when I found that man... his eyes were already dead.",
        "35年前，当我捡到那个男人时……他的眼神已经死了。",
        actor=astaroth,
    ).say(
        "balgas_3",
        "カインという男の魂に縋り、生き永らえている哀れな老犬。",
        "A pathetic old hound, clinging to the soul of a man named Cain to survive.",
        "依附于名为凯恩之人的灵魂苟延残喘的可悲老狗。",
        actor=astaroth,
    ).say(
        "balgas_4",
        "……だが、最近は少し違う光が宿っている。お前のせいだろうな。",
        "...But lately, a different light dwells in him. That would be your doing.",
        "……但最近他眼中有了不同的光芒。想必是你的缘故。",
        actor=astaroth,
    ).jump(ask_others)

    # リリィについて
    builder.step(about_lily).say(
        "lily_1",
        "……リリィ？ あれは便利な道具だ。",
        "...Lily? That one is a useful tool.",
        "……莉莉？那是个好用的工具。",
        actor=astaroth,
    ).say(
        "lily_2",
        "狭間で生まれた存在に、居場所など必要ない。私がそれを与えてやった。",
        "A being born in the void needs no place to belong. I gave her one.",
        "生于夹缝的存在不需要容身之所。是我赐予了她。",
        actor=astaroth,
    ).say(
        "lily_3",
        "……あれが何を考えているかなど、興味はない。",
        "...What she thinks is of no interest to me.",
        "……她在想什么，我没有兴趣。",
        actor=astaroth,
    ).say(
        "lily_4",
        "お前に執着しているようだが……まあ、好きにさせておけ。",
        "She seems fixated on you, but... very well, let her do as she pleases.",
        "她似乎对你很执着……算了，随她去吧。",
        actor=astaroth,
    ).jump(ask_others)

    # ゼクについて
    builder.step(about_zek).say(
        "zek_1",
        "……あの剥製師か。アルカディアの残骸が。",
        "...That taxidermist? A remnant of Arcadia.",
        "……那个标本师吗。阿卡迪亚的残骸。",
        actor=astaroth,
    ).say(
        "zek_2",
        "あれは私の客ではあるが、味方ではない。覚えておけ。",
        "He is my guest, but not my ally. Remember that.",
        "他是我的客人，但不是我的盟友。记住这点。",
        actor=astaroth,
    ).say(
        "zek_3",
        "『記録する』ことしか知らない。それが、あれの存在意義だ。",
        "All he knows is to 'record'. That is his sole purpose.",
        "他只知道『记录』。那便是他存在的意义。",
        actor=astaroth,
    ).say(
        "zek_4",
        "お前もいずれ、あれの『コレクション』になるかもしれんぞ。……ふふ。",
        "You too may become part of his 'collection' someday. ...How amusing.",
        "你迟早也会成为他的『收藏品』吧。……呵呵。",
        actor=astaroth,
    ).jump(ask_others)

    # ヌルについて
    builder.step(about_nul).say(
        "nul_1",
        "……ヌル？ あれは『失敗作』だ。",
        "...Null? That one is a 'failure'.",
        "……Nul？那是个『失败品』。",
        actor=astaroth,
    ).say(
        "nul_2",
        "私が試みた実験の、忌まわしい残滓。",
        "An abhorrent remnant of an experiment I once conducted.",
        "我曾尝试的实验所留下的可憎残渣。",
        actor=astaroth,
    ).say(
        "nul_3",
        "道具として使えるから生かしているだけだ。",
        "I keep it alive only because it serves as a useful tool.",
        "只因还能当工具用才让它活着。",
        actor=astaroth,
    ).say(
        "nul_4",
        "……興味を持つな。あれには何もない。『無（ヌル）』なのだから。",
        "...Do not take interest. There is nothing there. It is 'Null', after all.",
        "……不要感兴趣。那里什么都没有。毕竟是『Nul』。",
        actor=astaroth,
    ).jump(ask_others)

    # アイリスについて
    builder.step(about_iris).say(
        "iris_1",
        "……アイリス？ あの漂泊者か。",
        "...Iris? That wanderer?",
        "……艾莉丝？那个漂泊者吗。",
        actor=astaroth,
    ).say(
        "iris_2",
        "古い種族だ。私よりも……遥かに。",
        "An ancient race. Far older... than even I.",
        "古老的种族。比我……还要古老得多。",
        actor=astaroth,
    ).say(
        "iris_3",
        "知識を追い求める精神体だけの生物。肉体は借り物に過ぎぬ。",
        "Beings of pure spirit, endlessly pursuing knowledge. Their flesh is merely borrowed.",
        "只是追求知识的精神体。肉体不过是借用之物。",
        actor=astaroth,
    ).say(
        "iris_4",
        "『エルトダウン・シャーズ』という石版を知っているか？ あれらが残した知識の断片だ。",
        "Have you heard of the 'Eltdown Shards'? Fragments of knowledge they left behind.",
        "你知道『埃尔特当碎片』吗？那是他们留下的知识碎片。",
        actor=astaroth,
    ).say(
        "iris_5",
        "……私ですら、その全てを読み解くことはできなかった。",
        "...Even I could not decipher them all.",
        "……就连我也无法解读其全部内容。",
        actor=astaroth,
    ).say(
        "iris_6",
        "あれが何を知っているのか。どこまで見通しているのか。……それは、奴にしかわからぬ。",
        "What it knows, how far it sees... only it can say.",
        "它知道什么。能看透多远。……只有它自己知道。",
        actor=astaroth,
    ).say(
        "iris_7",
        "だが、敵ではない。今のところはな。",
        "But it is not an enemy. For now, at least.",
        "但它不是敌人。至少现在不是。",
        actor=astaroth,
    ).say(
        "iris_8",
        "……むしろ、私は少しばかり……敬意を持っている。5万年を生きた私が、そう思う相手は稀だ。",
        "...In fact, I hold a measure of... respect for it. Few earn that from one who has lived fifty thousand years.",
        "……不如说，我对它抱有一丝……敬意。活了五万年的我，很少会这样评价他人。",
        actor=astaroth,
    ).jump(ask_others)

    # ========================================
    # 5万年前のこと（カラドリウス）
    # ========================================
    builder.step(ask_past).say(
        "past_1",
        "……5万年？ よく知っているな。誰から聞いた？",
        "...Fifty thousand years? You are well informed. Who told you?",
        "……五万年？你消息真灵通。从谁那里听说的？",
        actor=astaroth,
    ).say(
        "past_2",
        "……まあいい。語ってやろう。",
        "...Very well. I shall tell you.",
        "……算了。我来告诉你吧。",
        actor=astaroth,
    ).say(
        "past_3",
        "かつて私には『故郷』があった。カラドリウスという名の、美しい世界が。",
        "Once, I had a 'homeland'. A beautiful world called Caladrius.",
        "曾经我有个『故乡』。一个名为卡拉德里乌斯的美丽世界。",
        actor=astaroth,
    ).say(
        "past_4",
        "竜が空を舞い、神々が語り合い、永遠の陽光が大地を照らしていた。",
        "Dragons soared through the skies, gods conversed, and eternal sunlight bathed the land.",
        "龙在天空翱翔，神明彼此交谈，永恒的阳光照耀大地。",
        actor=astaroth,
    ).say(
        "past_5",
        "……だが、神々は愚かだった。互いを殺し合い、世界ごと滅んだ。",
        "...But the gods were foolish. They slaughtered each other and destroyed the world with them.",
        "……但神明是愚蠢的。互相残杀，连同世界一起毁灭了。",
        actor=astaroth,
    ).say(
        "past_6",
        "私だけが残った。永遠に。この虚無の中に。",
        "I alone remained. Forever. In this void.",
        "唯有我留了下来。永远地。在这虚无之中。",
        actor=astaroth,
    ).say(
        "past_7",
        "……5万年の孤独を、お前は想像できるか？",
        "...Can you imagine fifty thousand years of solitude?",
        "……你能想象五万年的孤独吗？",
        actor=astaroth,
    ).say(
        "past_8",
        "星が生まれ、燃え尽き、また生まれる。その全てを、ただ見ているだけの時間を。",
        "Stars born, burned out, born again. Watching it all... merely watching.",
        "星辰诞生、燃尽、再次诞生。只是注视着这一切的时光。",
        actor=astaroth,
    ).say(
        "past_9",
        "……だから私は、このアリーナを作った。",
        "...That is why I created this arena.",
        "……所以我创建了这个角斗场。",
        actor=astaroth,
    ).say(
        "past_10",
        "観客が私を見る限り、私は『存在』できる。孤独は、少しだけ紛れる。",
        "As long as the audience watches me, I can 'exist'. The solitude... is somewhat eased.",
        "只要观众注视着我，我就能『存在』。孤独……稍微得到了缓解。",
        actor=astaroth,
    ).say(
        "past_11",
        "……お前には関係のない話だ。さっさと戦場に戻れ。",
        "...This does not concern you. Return to the battlefield at once.",
        "……与你无关的话。快回战场去吧。",
        actor=astaroth,
    ).jump(choices)

    # ========================================
    # エピローグ後の会話
    # ========================================
    builder.step(post_game).say(
        "pg_greet",
        "……ふむ。自由の身か。……悪くない気分だ。",
        "...Hmm. Freedom at last. ...It feels rather pleasant.",
        "……嗯。自由了吗。……感觉还不错。",
        actor=astaroth,
    ).jump(post_game_choices)

    # パーティメンバー用選択肢
    # inject_unique(): バニラの_invite, _joinParty, _leaveParty, _buy, _heal等を追加
    builder.step(post_game_choices).inject_unique().choice(
        end,
        "また話そう",
        "Let's talk again later",
        "下次再聊",
        text_id="c_pg_bye",
    ).on_cancel(end)

    # ========================================
    # 終了
    # ========================================
    builder.step(end).finish()
