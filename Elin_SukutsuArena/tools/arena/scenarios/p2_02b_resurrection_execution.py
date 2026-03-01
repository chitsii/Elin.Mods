# -*- coding: utf-8 -*-
"""
p2_02b_resurrection_execution.py - 蘇りの儀式・実行編（第二部）

素材を集めた後の儀式実行パート。
バルガスとカインを蘇らせる。
"""

from arena.builders import ArenaDramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds, SessionKeys


def define_resurrection_execution_drama(builder: ArenaDramaBuilder):
    """
    蘇りの儀式・実行編

    条件:
    - pg_02a_resurrection_intro 完了
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    nul = builder.register_actor(Actors.NUL, "Nul", "Nul")
    iris = builder.register_actor(Actors.IRIS, "アイリス", "Iris")
    cain = builder.register_actor(Actors.CAIN, "カイン", "Cain")

    # ========================================
    # ラベル定義
    # ========================================

    main = builder.label("main")
    material_check = builder.label("material_check")
    material_missing = builder.label("material_missing")
    ritual_scene = builder.label("ritual_scene")
    ritual_preparation = builder.label("ritual_preparation")
    chicken_farewell = builder.label("chicken_farewell")
    ritual_execution = builder.label("ritual_execution")
    success = builder.label("success")
    awakening = builder.label("awakening")
    comedy = builder.label("comedy")
    brothers_reunion = builder.label("brothers_reunion")
    nul_thanks = builder.label("nul_thanks")
    chicken_thanks = builder.label("chicken_thanks")
    epilogue_hint = builder.label("epilogue_hint")
    ending = builder.label("ending")

    # ========================================
    # メインエントリ
    # ========================================
    builder.step(main).jump(material_check)

    # ========================================
    # 素材チェック
    # ========================================

    builder.step(material_check).action(
        "modInvoke", param="check_resurrection_materials()", actor="pc"
    ).branch_if(SessionKeys.HAS_ALL_MATERIALS, "==", 1, ritual_scene).jump(
        material_missing
    )

    # 素材不足時
    builder.step(material_missing).say(
        "missing_1",
        "素材がまだ足りないようです。\n必要なのは……不老長寿のエリクシル 2本、産卵薬 2本、そしてペットの鶏 2羽です。\nイルヴァで探してきてください。",
        "It seems we're still short on materials.\nWhat we need is... 2 Elixirs of Eternal Youth, 2 Love Plus Potions, and 2 pet chickens.\nPlease search for them in Ylva.",
        "材料好像还不够。\n需要的是……不老长寿灵药2瓶、产卵药2瓶、以及宠物鸡2只。\n请在伊尔瓦寻找吧。",
        actor=lily,
    ).drama_end()

    # ========================================
    # 儀式シーン
    # ========================================

    builder.step(ritual_scene).jump(ritual_preparation)

    builder.step(ritual_preparation).play_bgm("BGM/Emotional_Sorrow_2").say(
        "prep_1",
        "素材は……全て揃っていますね。\n不老長寿のエリクシル、産卵薬、そして……この子たちを預かりましょう。",
        "The materials are... all gathered.\nElixir of Eternal Youth, Love Plus Potion, and... Let me take these little ones.",
        "材料……都齐全了呢。\n不老长寿灵药、产卵药、还有……把这两只交给我吧。",
        actor=lily,
    ).say(
        "prep_4",
        "では……儀式を始めます。\nまず、エリクシルを……次に、産卵薬を……最後に……。",
        "Now then... let us begin the ritual.\nFirst, the elixir... Next, the love plus potion... Finally...",
        "那么……开始仪式吧。\n首先，灵药……接下来，产卵药……最后……",
        actor=lily,
    ).jump(chicken_farewell)

    # 鶏との別れ
    builder.step(chicken_farewell).say(
        "farewell_1",
        "……ごめんね。",
        "...I'm sorry.",
        "……对不起。",
        actor=lily,
    ).say(
        "farewell_2",
        "鶏、何て言ってる？",
        "What are the chickens saying?",
        "鸡在说什么？",
        actor=nul,
    ).say(
        "farewell_3",
        "鶏語わかんないよ……。",
        "I don't understand chicken...",
        "我不懂鸡语啊……",
        actor=iris,
    ).say(
        "farewell_4",
        "……この子たちは、儀式に使います。",
        "...These little ones will be used in the ritual.",
        "……这些孩子将用于仪式。",
        actor=lily,
    ).say(
        "farewell_5",
        "鶏たち、犠牲。",
        "The chickens are the sacrifice.",
        "鸡们是牺牲品。",
        actor=nul,
    ).say(
        "farewell_6",
        "……ありがとね。",
        "...Thank you.",
        "……谢谢你们。",
        actor=iris,
    ).jump(ritual_execution)

    # 儀式の実行
    builder.step(ritual_execution).action(
        "modInvoke", param="consume_resurrection_materials()", actor="pc"
    ).say(
        "execute_1",
        "始めます。\n時を止めし霊薬よ、死の終点を巻き戻せ。\n生を促す秘薬よ、新たな始まりを与えよ。\nそして、循環の使者たちよ……。",
        "Let us begin.\nO elixir that halts time, rewind the endpoint of death.\nO potion that urges life, grant a new beginning.\nAnd ye messengers of the cycle...",
        "开始吧。\n停止时间的灵药啊，倒转死亡的终点吧。\n促进生命的秘药啊，赐予新的开始吧。\n还有，循环的使者们啊……",
        actor=lily,
    ).shake().say(
        "execute_5",
        "その命をもって、死者を導け——！",
        "With your lives, guide the dead back!",
        "以你们的生命，引导亡者归来——！",
        actor=lily,
    ).fade_out(duration=1.0, color="white").fade_in(duration=1.0, color="white").jump(
        success
    )

    # 成功
    builder.step(success).wait(1.0).say(
        "success_1",
        "……動かない。",
        "...They're not moving.",
        "……没有动静。",
        actor=nul,
    ).say(
        "success_2",
        "失敗……？",
        "Did it fail...?",
        "失败了……？",
        actor=iris,
    ).say(
        "success_3",
        "待って……。",
        "Wait...",
        "等等……",
        actor=lily,
    ).shake().say(
        "success_4",
        "……！ 二人とも！",
        "...! Both of them!",
        "……！两个人都！",
        actor=lily,
    ).action("modInvoke", param="spawn_cain()", actor="pc").jump(awakening)

    # 目覚め
    builder.step(awakening).play_bgm("BGM/Emotional_Hope").focus_chara(
        Actors.BALGAS
    ).say(
        "awake_1",
        "……んあ？",
        "...Huh?",
        "……嗯？",
        actor=balgas,
    ).focus_chara(Actors.CAIN).say(
        "awake_2",
        "……ここは……。",
        "...Where is this...?",
        "……这里是……",
        actor=cain,
    ).unfocus().say(
        "awake_3",
        "成功した！",
        "It worked!",
        "成功了！",
        actor=iris,
    ).focus_chara(Actors.BALGAS).say(
        "awake_4",
        "……ここは……？ なんで俺、寝て……。\n……カイン？",
        "...Where am I...? Why was I sleeping...?\n...Cain?",
        "……这里是……？我为什么在睡觉……\n……凯恩？",
        actor=balgas,
    ).focus_chara(Actors.CAIN).say(
        "awake_6",
        "……まさか、兄貴？",
        "...Could it be, big bro?",
        "……难道是，大哥？",
        actor=cain,
    ).say(
        "awake_7",
        "お前……お前、なんで……。",
        "You... why are you...?",
        "你……你怎么……",
        actor=balgas,
    ).say(
        "awake_8",
        "俺にもわかんねえよ。死んだと思ったのに……。\n……おい。なんで俺たち、裸なんだ。",
        "Beats me. I thought I was dead...\n...Hey. Why are we naked?",
        "我也不知道啊。明明以为死了……\n……喂。为什么我们是裸的。",
        actor=cain,
    ).say(
        "awake_10",
        "……本当だ。おい、何がどうなってる。",
        "...You're right. Hey, what's going on?",
        "……真的诶。喂，到底怎么回事。",
        actor=cain,
    ).jump(comedy)

    # コメディ
    builder.step(comedy).play_bgm("BGM/Nichijo").say(
        "comedy_1",
        "あ、えっと、儀式の都合で……。",
        "Ah, um, it's a side effect of the ritual...",
        "啊，那个，因为仪式的关系……",
        actor=lily,
    ).say(
        "comedy_2",
        "服までは蘇生しない。仕方ない。\n私、見てないよ。",
        "Clothes don't get resurrected. Can't help it.\nI'm not looking.",
        "衣服不会被复活。没办法。\n我没看哦。",
        actor=nul,
    ).say(
        "comedy_4",
        "おい！ せめてタオルくらい！",
        "Hey! At least give us towels!",
        "喂！至少给条毛巾啊！",
        actor=balgas,
    ).say(
        "comedy_5",
        "俺にもくれ！",
        "Me too!",
        "给我也来一条！",
        actor=cain,
    ).say(
        "comedy_6",
        "あー……はい、これ。",
        "Ah... here you go.",
        "啊……给，这个。",
        actor=iris,
    ).say(
        "comedy_7",
        "……で、俺は何日寝てたんだ？",
        "...So, how long was I asleep?",
        "……那么，我睡了几天？",
        actor=balgas,
    ).say(
        "comedy_8",
        "3ヶ月です。",
        "Three months.",
        "三个月。",
        actor=lily,
    ).say(
        "comedy_9",
        "俺は？",
        "And me?",
        "我呢？",
        actor=cain,
    ).say(
        "comedy_10",
        "……35年です。",
        "...35 years.",
        "……35年。",
        actor=lily,
    ).say(
        "comedy_11",
        "は？",
        "What?",
        "啥？",
        actor=cain,
    ).say(
        "comedy_12",
        "お前、35年前に死んだんだよ。覚えてねえのか。",
        "You died 35 years ago. Don't you remember?",
        "你35年前就死了啊。不记得了吗。",
        actor=balgas,
    ).say(
        "comedy_13",
        "いや、覚えてるけど……35年！？ マジで！？\n俺、何歳になってんだ……。",
        "No, I remember, but... 35 years!? Seriously!?\nHow old am I now...",
        "不，我记得，但是……35年！？真的假的！？\n我现在几岁了……",
        actor=cain,
    ).say(
        "comedy_15",
        "肉体は死亡時のまま再構成されていますので、お若いままですよ。",
        "Your body was reconstructed as it was at death, so you're still young.",
        "肉体是按照死亡时的状态重构的，所以还是年轻的哦。",
        actor=lily,
    ).say(
        "comedy_16",
        "……それはそれで複雑だな。",
        "...That's complicated in its own way.",
        "……那也挺复杂的。",
        actor=cain,
    ).jump(brothers_reunion)

    # 兄弟弟子の再会
    builder.step(brothers_reunion).say(
        "reunion_1",
        "……カイン。",
        "...Cain.",
        "……凯恩。",
        actor=balgas,
    ).say(
        "reunion_2",
        "なんだよ、兄貴。",
        "What is it, big bro?",
        "什么事啊，大哥。",
        actor=cain,
    ).say(
        "reunion_3",
        "……すまなかった。\nお前を……守れなかった。35年、ずっと……。",
        "...I'm sorry.\nI couldn't... protect you. For 35 years...",
        "……对不起。\n我没能……保护你。35年来，一直……",
        actor=balgas,
    ).say(
        "reunion_6",
        "……兄貴。\n相変わらずだな、兄貴は。\n俺が勝手に死んだんだろ。兄貴のせいじゃねえよ。",
        "...Big bro.\nYou haven't changed, big bro.\nI died on my own. It's not your fault.",
        "……大哥。\n还是老样子啊，大哥。\n是我自己死的。不是大哥的错。",
        actor=cain,
    ).say(
        "reunion_9",
        "……つーか、引きずってたのかよ。重いな。",
        "...Wait, you've been carrying that? That's heavy.",
        "……话说，你一直放不下吗。真沉重啊。",
        actor=cain,
    ).say(
        "reunion_10",
        "うるせえ！",
        "Shut up!",
        "吵死了！",
        actor=balgas,
    ).say(
        "reunion_11",
        "まあ、でも……蘇らせてくれたんだろ？\nありがとな、リリィさん。",
        "Well, but... You brought me back, right?\nThanks, Miss Lily.",
        "不过嘛……你让我复活了不是吗？\n谢谢啊，莉莉小姐。",
        actor=cain,
    ).say(
        "reunion_13",
        "……ケッ。",
        "...Tch.",
        "……切。",
        actor=balgas,
    ).jump(nul_thanks)

    # Nulへの感謝
    builder.step(nul_thanks).say(
        "nul_t1",
        "カインさんを蘇らせようと言ったのは、Nulですよ。",
        "It was Nul who suggested reviving Cain.",
        "提议复活凯恩的是Nul哦。",
        actor=lily,
    ).say(
        "nul_t2",
        "……あんたが？",
        "...You did?",
        "……是你？",
        actor=cain,
    ).say(
        "nul_t3",
        "バルガスには……後悔してほしくなかった。",
        "I didn't want Vargus... to have regrets.",
        "我不想让巴尔加斯……留有遗憾。",
        actor=nul,
    ).say(
        "nul_t4",
        "……そうか。ありがとな。名前、なんだっけ。",
        "...I see. Thanks. What's your name again?",
        "……这样啊。谢谢啊。你叫什么名字来着。",
        actor=cain,
    ).say(
        "nul_t6",
        "Nul。",
        "Nul.",
        "Nul。",
        actor=nul,
    ).say(
        "nul_t7",
        "Nul。覚えた。……借り、返すからな。",
        "Nul. Got it. ...I'll repay this debt.",
        "Nul。记住了。……我会还这份人情的。",
        actor=cain,
    ).say(
        "nul_t8",
        "……いい。気にしないで。",
        "...It's fine. Don't worry about it.",
        "……没关系。别在意。",
        actor=nul,
    ).say(
        "nul_t9",
        "Nul、照れてる？",
        "Nul, are you embarrassed?",
        "Nul，害羞了？",
        actor=iris,
    ).say(
        "nul_t10",
        "照れてない……！",
        "Not embarrassed...!",
        "没有害羞……！",
        actor=nul,
    ).say(
        "nul_t11",
        "それを照れてるって言うんだよ。",
        "That's what being embarrassed means.",
        "那就是害羞的意思啊。",
        actor=iris,
    ).jump(chicken_thanks)

    # 鶏への感謝
    builder.step(chicken_thanks).say(
        "chk_1",
        "……そうだ、鶏はどういうことだ？ さっき、なんか光ってたが。",
        "...Oh yeah, what about the chickens? They were glowing earlier.",
        "……对了，鸡是怎么回事？刚才好像在发光。",
        actor=balgas,
    ).wait(0.5).say(
        "chk_2",
        "……鶏たちは犠牲になったのだ。",
        "...The chickens were the sacrifice.",
        "……鸡们成了牺牲。",
        actor=nul,
    ).say(
        "chk_3",
        "……は？",
        "...Huh?",
        "……啥？",
        actor=balgas,
    ).say(
        "chk_4",
        "俺も聞きたい。何があった。",
        "I want to hear too. What happened?",
        "我也想知道。发生了什么。",
        actor=cain,
    ).say(
        "chk_5",
        "二人を蘇らせるために……命を捧げてくれたんです。",
        "To bring you both back... they sacrificed their lives.",
        "为了让你们两人复活……它们献出了生命。",
        actor=iris,
    ).say(
        "chk_6",
        "……そう、か。\n鶏に命救われるとはな……。",
        "...I see.\nSaved by chickens of all things...",
        "……是吗。\n被鸡救了命啊……",
        actor=balgas,
    ).say(
        "chk_8",
        "……すまねえな、鶏。\n……今度から鶏肉は食わねえ。",
        "...Sorry, chickens.\n...I won't eat chicken from now on.",
        "……抱歉啊，鸡们。\n……以后不吃鸡肉了。",
        actor=balgas,
    ).say(
        "chk_9",
        "……ああ。ありがとうな。\n俺も。",
        "...Yeah. Thank you.\nMe neither.",
        "……嗯。谢谢你们。\n我也是。",
        actor=cain,
    ).say(
        "chk_12",
        "それ、鶏が喜ぶ？",
        "Would chickens be happy about that?",
        "这样鸡会高兴吗？",
        actor=nul,
    ).say(
        "chk_13",
        "知らねえよ！ 気持ちの問題だ！",
        "Who knows! It's the thought that counts!",
        "谁知道啊！这是心意问题！",
        actor=balgas,
    ).say(
        "chk_14",
        "知らねえよ！ 気持ちの問題だ！",
        "Who knows! It's the thought that counts!",
        "谁知道啊！这是心意问题！",
        actor=cain,
    ).say(
        "chk_15",
        "ハモった。",
        "In sync.",
        "同步了。",
        actor=iris,
    ).say(
        "chk_16",
        "息ぴったり。さすが親子。",
        "Perfect harmony. As expected of father and son.",
        "默契十足。不愧是父子。",
        actor=nul,
    ).jump(epilogue_hint)

    # 後日談への伏線
    builder.step(epilogue_hint).say(
        "hint_1",
        "さて、お二人とも。",
        "Now then, you two.",
        "那么，两位。",
        actor=lily,
    ).say(
        "hint_2",
        "ん？",
        "Hm?",
        "嗯？",
        actor=balgas,
    ).say(
        "hint_3",
        "なんだ？",
        "What?",
        "什么？",
        actor=cain,
    ).say(
        "hint_4",
        "儀式の素材代、請求書を送りますね。\n不老長寿のエリクシル2本、産卵薬2本、その他活動費諸々……。\n二人分ですので、倍です。",
        "I'll send you the bill for the ritual materials.\n2 Elixirs of Eternal Youth, 2 Love Plus Potions, plus various operational costs...\nIt's for two people, so double.",
        "仪式材料的费用，我会寄账单给你们的。\n不老长寿灵药2瓶、产卵药2瓶、还有其他活动费用……\n因为是两个人的份，所以是双倍。",
        actor=lily,
    ).say(
        "hint_5",
        "……いくらだ。",
        "...How much?",
        "……多少钱？",
        actor=balgas,
    ).say(
        "hint_8",
        "おいおい、俺まで請求されんのかよ。",
        "Wait, I'm getting charged too?",
        "喂喂，我也要付钱吗。",
        actor=cain,
    ).say(
        "hint_9",
        "当然です。蘇った以上、払っていただきます。\n落ち着いてから書面で伝えますね。また心臓が止まっちゃいますから。",
        "Of course. Now that you're revived, you'll pay.\nI'll send it in writing once you've calmed down. Don't want your hearts stopping again.",
        "当然。既然复活了，就得付钱。\n等你们冷静下来再书面通知吧。免得心脏又停了。",
        actor=lily,
    ).say(
        "hint_11",
        "おい！ 俺たち、今蘇ったばっかりだぞ！？",
        "Hey! We just came back to life!",
        "喂！我们刚刚才复活啊！？",
        actor=balgas,
    ).say(
        "hint_12",
        "35年分の利子とか言うなよ！？",
        "Don't say 35 years of interest!",
        "别说35年的利息啊！？",
        actor=cain,
    ).say(
        "hint_13",
        "利子……それは考えていませんでしたね。検討します。",
        "Interest... I hadn't considered that. I'll think about it.",
        "利息……这个我没想过呢。考虑一下。",
        actor=lily,
    ).say(
        "hint_14",
        "やめろ！！",
        "Stop!!",
        "住手！！",
        actor=cain,
    ).say(
        "hint_15",
        "蘇りの儀式、また必要になる？",
        "Will we need another resurrection ritual?",
        "复活仪式还需要再来一次吗？",
        actor=nul,
    ).say(
        "hint_16",
        "縁起でもねえこと言うな！！",
        "Don't say such unlucky things!!",
        "别说这种不吉利的话！！",
        actor=balgas,
    ).say(
        "hint_17",
        "縁起でもねえこと言うな！！",
        "Don't say such unlucky things!!",
        "别说这种不吉利的话！！",
        actor=cain,
    ).say(
        "hint_18",
        "あはは……賑やかになったね。",
        "Ahaha... It's gotten lively.",
        "啊哈哈……变热闹了呢。",
        actor=iris,
    ).say(
        "hint_19",
        "ええ。これでようやく……また始められます。",
        "Yes. Now at last... we can start again.",
        "是啊。这下终于……可以重新开始了。",
        actor=lily,
    ).jump(ending)

    # ========================================
    # エンディング
    # ========================================

    builder.step(ending).set_flag(Keys.BALGAS_REVIVED, 1).set_flag(
        Keys.CAIN_REVIVED, 1
    ).set_flag(Keys.BALGAS_KILLED, FlagValues.BalgasChoice.SPARED).complete_quest(
        QuestIds.PG_02B_RESURRECTION_EXECUTION
    ).drama_end()
