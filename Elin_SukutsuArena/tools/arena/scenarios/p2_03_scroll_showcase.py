# -*- coding: utf-8 -*-
"""
p2_03_scroll_showcase.py - 大部屋の巻物お披露目会（第二部・第2話）

リリィの研究成果「大部屋の巻物」のお披露目会。
実演で闘技場が崩壊し、強制退場となるコメディイベント。
"""

from arena.builders import ArenaDramaBuilder
from arena.data import Actors, QuestIds


def define_scroll_showcase(builder: ArenaDramaBuilder):
    """
    大部屋の巻物お披露目会

    条件:
    - PG_02B_RESURRECTION_EXECUTION 完了
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    nul = builder.register_actor(Actors.NUL, "Nul", "Nul")
    iris = builder.register_actor(Actors.IRIS, "アイリス", "Iris")
    cain = builder.register_actor(Actors.CAIN, "カイン", "Cain")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    gather = builder.label("gather")
    announce = builder.label("announce")
    qna = builder.label("qna")
    move_to_arena = builder.label("move_to_arena")
    activate_small = builder.label("activate_small")
    result_small = builder.label("result_small")
    excuse = builder.label("excuse")
    practical = builder.label("practical")
    balgas_take = builder.label("balgas_take")
    collapse = builder.label("collapse")
    lily_monologue = builder.label("lily_monologue")
    bill = builder.label("bill")
    sales_start = builder.label("sales_start")
    warning = builder.label("warning")
    epilogue = builder.label("epilogue")
    ending = builder.label("ending")

    # エントリーポイント
    builder.step(main).jump(gather)

    # シーン1: 招集
    builder.step(gather).play_bgm("BGM/Ending_Celebration").say(
        "gather_1",
        "みなさん、集まってください！\n今日は、私の研究成果を発表します！",
        "Everyone, please gather round!\nToday, I shall present the fruits of my research!",
        "大家请集合！\n今天，我要发表我的研究成果！",
        actor=lily,
    ).say(
        "gather_2",
        "研究ぅ？ お前ぇ、いつの間にそんなことしてたんだぁ？ ひっく。",
        "Research? When'd ya start doin' that? *hic*",
        "研究？你小子什么时候开始搞那玩意儿的？嗝。",
        actor=balgas,
    ).say(
        "gather_3",
        "知ってた。リリィの部屋、古文書だらけ。",
        "Knew already. Lily's room. Full of ancient texts.",
        "早就知道了。莉莉的房间，全是古文献。",
        actor=nul,
    ).say(
        "gather_4",
        "……そうだったかぁ？ うぃー。",
        "...Was it? *hic*~",
        "……是吗？嗝~",
        actor=balgas,
    ).say(
        "gather_5",
        "またNulの掃除情報……。",
        "Nul's cleaning intel strikes again...",
        "又是Nul的打扫情报……",
        actor=iris,
    ).jump(announce)

    # シーン2: 発表
    builder.step(announce).say(
        "announce_1",
        "見てください！ これが私の研究成果です！\n名付けて……『大部屋の巻物』！",
        "Behold! This is the result of my research!\nI call it... the 'Scroll of Big Room'!",
        "请看！这就是我的研究成果！\n命名为……『大房间卷轴』！",
        actor=lily,
    ).say(
        "announce_2",
        "大部屋？",
        "Big Room?",
        "大房间？",
        actor=iris,
    ).say(
        "announce_3",
        "この巻物を使えば、部屋全体を『大きく』できるんです！\n小さい家でも、中は広々！革命的！",
        "With this scroll, you can make an entire room 'bigger'!\nEven a tiny house becomes spacious inside! Revolutionary!",
        "用了这个卷轴，就能把整个房间变『大』！\n再小的房子，里面也能变得宽敞无比！革命性的发明！",
        actor=lily,
    ).say(
        "announce_4",
        "ほぉう、そりゃすげぇ！ ひっく。\n……つまりどういうことだぁ？",
        "Ohhh, that's amazin'! *hic*\n...So what does that mean exactly?",
        "哦哦，那可太厉害了！嗝。\n……所以到底是啥意思？",
        actor=balgas,
    ).say(
        "announce_5",
        "……今説明しましたよ。酔ってるんですか？",
        "...I just explained it. Are you drunk?",
        "……我刚刚解释过了。您是喝醉了吗？",
        actor=lily,
    ).say(
        "announce_6",
        "酔ってねぇよぉ！ 聞いただけだろぉ！",
        "I ain't drunk! I was just askin'!",
        "老子没醉！只是问问而已！",
        actor=balgas,
    ).say(
        "announce_7",
        "バルガス、今のはないと思う。",
        "Vargus. That was not acceptable.",
        "巴尔加斯。刚才那个不太行。",
        actor=nul,
    ).say(
        "announce_8",
        "お前もかぁ！ うぃー。",
        "Not you too! *hic*~",
        "连你也来！嗝~",
        actor=balgas,
    ).jump(qna)

    # シーン3: 質問タイム
    builder.step(qna).say(
        "qna_1",
        "空間拡張。面白い。\n掃除、楽になる？",
        "Spatial expansion. Interesting.\nDoes it make cleaning easier?",
        "空间拓展。有意思。\n打扫会变轻松吗？",
        actor=nul,
    ).say(
        "qna_2",
        "えっと……広くなるから掃除は大変に……。",
        "Well... since it gets bigger, cleaning would be harder...",
        "呃……因为会变大，打扫反而会更麻烦……",
        actor=lily,
    ).say(
        "qna_3",
        "却下。",
        "Rejected.",
        "否决。",
        actor=nul,
    ).say(
        "qna_4",
        "即答！？",
        "That fast!?",
        "秒答！？",
        actor=lily,
    ).say(
        "qna_5",
        "ね、実演してみてよ。\n百聞は一見にしかずっしょ？",
        "Hey, why not give us a demo?\nSeeing is believing, right?",
        "诶，不如演示一下呗。\n百闻不如一见嘛？",
        actor=iris,
    ).say(
        "qna_6",
        "もちろんです！\nでは、バルガスさんの小部屋に移動して……。",
        "Of course!\nThen, let us move to Vargus's small room...",
        "当然可以！\n那么，请移步到巴尔加斯先生的小房间……",
        actor=lily,
    ).jump(move_to_arena)

    # シーン4: 発動（小部屋）
    builder.step(move_to_arena).say(
        "move_1",
        "（闘技場の小部屋へ移動する）",
        "(They move to the arena's small room.)",
        "（移动到斗技场的小房间。）",
        actor=narrator,
    ).jump(activate_small)

    builder.step(activate_small).say(
        "act_1",
        "では、発動します。\n大部屋の巻物……発動！",
        "Now then, activating.\nScroll of Big Room... activate!",
        "那么，启动了。\n大房间卷轴……发动！",
        actor=lily,
    ).action(
        "modInvoke", param="invoke_oheya_scroll(5)", actor="pc"
    ).shake().wait(0.5).shake().wait(0.5).shake().wait(4.0).jump(result_small)

    # シーン5: 結果
    builder.step(result_small).play_bgm("BGM/Nichijo").say(
        "res_1",
        "……壁、消えた。",
        "...Walls. Gone.",
        "……墙壁，消失了。",
        actor=nul,
    ).say(
        "res_2",
        "いや、消えたっていうか……破壊されてない？",
        "Uh, less 'gone' and more... destroyed?",
        "不是，与其说是消失了……不如说是被毁了吧？",
        actor=iris,
    ).wait(1.0).say(
        "res_3",
        "……。\n……広く、なりましたね？",
        "......\n...It did get bigger, didn't it?",
        "……。\n……变大了，对吧？",
        actor=lily,
    ).jump(excuse)

    # シーン7: リリィの弁明
    builder.step(excuse).say(
        "exc_1",
        "リリィ。これ、空間拡張じゃない。ただの破壊。",
        "Lily. This is not spatial expansion. Just destruction.",
        "莉莉。这不是空间拓展。只是破坏。",
        actor=nul,
    ).say(
        "exc_2",
        "壁とか床とか、全部吹っ飛んでるんだけど。",
        "The walls, the floor... everything's been blown to bits.",
        "墙壁啊地板啊，全都炸飞了耶。",
        actor=iris,
    ).say(
        "exc_3",
        "……俺が死んでる間に、魔法の定義変わったのか？",
        "...Did the definition of magic change while I was dead?",
        "……我死着的那段时间，魔法的定义变了吗？",
        actor=cain,
    ).say(
        "exc_4",
        "俺の部屋が……！",
        "My room...!",
        "老子的房间……！",
        actor=balgas,
    ).say(
        "exc_5",
        "でも、広くはなった。嘘は言ってない。",
        "But it did get bigger. She did not lie.",
        "但确实变大了。她没有撒谎。",
        actor=nul,
    ).say(
        "exc_6",
        "フォローになってないよそれ。",
        "That's not helping, Nul.",
        "那不叫帮忙好吗。",
        actor=iris,
    ).say(
        "exc_7",
        "……理論上は空間を拡張するはずだったんです……。\n次元の狭間に干渉して、内部空間だけを広げる……。\n完璧な計算だったのに……。",
        "...In theory, it was supposed to expand the space...\nBy interfacing with the dimensional rift, expanding only the interior...\nThe calculations were perfect...",
        "……理论上应该是拓展空间的……\n通过干涉次元裂缝，只扩大内部空间……\n计算明明是完美的……",
        actor=lily,
    ).say(
        "exc_8",
        "で、なんで破壊になったの？",
        "So why'd it turn into destruction?",
        "所以，为什么变成破坏了呢？",
        actor=iris,
    ).say(
        "exc_9",
        "……わかりません。\nなぜか実装したら、障害物を消滅させる機能に……。",
        "...I don't know.\nSomehow when I implemented it, it became an obstacle-annihilation function...",
        "……不知道。\n不知为什么实际做出来之后，就变成了消灭障碍物的功能……",
        actor=lily,
    ).say(
        "exc_10",
        "結果オーライ？",
        "All's well that ends well?",
        "结果还行？",
        actor=nul,
    ).say(
        "exc_11",
        "オーライじゃないです！ 別の機能じゃないですか！",
        "It is NOT well! That's a completely different function!",
        "才不行呢！这完全是另一个功能了吧！",
        actor=lily,
    ).jump(practical)

    # シーン8: 実用性の議論
    builder.step(practical).say(
        "prac_1",
        "まぁ、壊れたもんは仕方ねぇよなぁ。ひっく。\nうまく使えばぁ、ダンジョンで壁ぶち抜いて近道できるぞぉ！",
        "Well, what's broke is broke. *hic*\nUse it right an' ya can blast through dungeon walls for shortcuts!",
        "嘛，坏了的东西也没办法了。嗝。\n好好利用的话，可以在迷宫里炸穿墙壁抄近路啊！",
        actor=balgas,
    ).say(
        "prac_2",
        "あー、それは確かに便利かも。",
        "Hmm, that does sound handy actually.",
        "啊——那确实挺方便的。",
        actor=iris,
    ).say(
        "prac_3",
        "掃除も楽。壁や床がなくなる。それ以上汚れもしない。",
        "Cleaning is easier too. No walls or floor. Nothing left to get dirty.",
        "打扫也轻松了。墙壁和地板都没了。也不会再变脏了。",
        actor=nul,
    ).say(
        "prac_4",
        "そういう使い方じゃないんです！！\n私が作りたかったのは、夢のある空間拡張魔法で……。",
        "That's not how it's supposed to be used!!\nWhat I wanted to create was a magical, dreamy spatial expansion spell...",
        "不是那样用的啦！！\n我想做的，是充满梦想的空间拓展魔法……",
        actor=lily,
    ).say(
        "prac_5",
        "夢より実用性だろぉ？ うぃー。",
        "Practicality over dreams, yeah? *hic*~",
        "比起梦想，实用性更重要吧？嗝~",
        actor=balgas,
    ).say(
        "prac_6",
        "うぅ……。",
        "Uuu...",
        "呜呜……",
        actor=lily,
    ).jump(balgas_take)

    # シーン9: バルガスの暴走
    builder.step(balgas_take).say(
        "take_1",
        "それじゃあ、仕切りなおしてっとぉ。コホン……。\nおお、広くなったじゃねえかぁ！ ひっく。\n次はもっとデカいとこでやってみようぜぇ！",
        "Awright then, let's start fresh. *Ahem*...\nOoh, it really did get bigger! *hic*\nLet's try it somewhere even bigger next!",
        "那好，重新来过吧。咳咳……\n哦哦，真的变大了嘛！嗝。\n下次找个更大的地方试试吧！",
        actor=balgas,
    ).say(
        "take_2",
        "えっ、ちょっと待っ——",
        "Wha-- wait a--",
        "诶、等一下——",
        actor=lily,
    ).say(
        "take_3",
        "大部屋の巻物ぉ、発動ぉ！ うぃー！",
        "Scroll of Big Rooom, activaaate! *hic*!",
        "大房间卷轴，发动！嗝！",
        actor=balgas,
    ).jump(collapse)

    # シーン10: 崩壊
    builder.step(collapse).action(
        "modInvoke", param="invoke_oheya_scroll(0)", actor="pc"
    ).shake().wait(0.5).shake().wait(0.5).shake().say(
        "col_1",
        "……闘技場、壊れた。",
        "...Arena. Broken.",
        "……斗技场，坏了。",
        actor=nul,
    ).say(
        "col_2",
        "バルガスさん何してんの！？",
        "Vargus, what are you doing!?",
        "巴尔加斯先生你在干什么啊！？",
        actor=iris,
    ).say(
        "col_3",
        "いやぁ、もう一回見たくてよぉ……。ひっく。",
        "I just wanted to see it one more time... *hic*",
        "就是想再看一次嘛……嗝。",
        actor=balgas,
    ).say(
        "col_4",
        "闘技場でやらないでって言おうとしたのに！！",
        "I was about to say don't use it in the arena!!",
        "我正想说不要在斗技场里用的！！",
        actor=lily,
    ).say(
        "col_5",
        "バルガス。もう飲酒しないで。金輪際。",
        "Vargus. Stop drinking. Forever.",
        "巴尔加斯。别再喝酒了。永远。",
        actor=nul,
    ).say(
        "col_6",
        "う……すまんすまん。うぃー。",
        "Ugh... sorry, sorry. *hic*~",
        "呃……对不起对不起。嗝~",
        actor=balgas,
    ).jump(lily_monologue)

    # シーン11: リリィの独白
    builder.step(lily_monologue).say(
        "mono_1",
        "（ふぅ……、全部私の責任になるところでした。\nバルガスさんがバカで助かりました。）",
        "(Phew... it almost became entirely my fault.\nThank goodness Vargus is an idiot.)",
        "（呼……差点全变成我的责任了。\n多亏巴尔加斯先生是个笨蛋。）",
        actor=lily,
    ).jump(bill)

    # シーン12: 請求書
    builder.step(bill).say(
        "bill_0",
        "……さすがに酔いも冷めちまった。",
        "...Even I've sobered up after that.",
        "……连老子的酒都醒了。",
        actor=balgas,
    ).say(
        "bill_1",
        "……修復が終わったら連絡します。\nバルガスさんには後で請求書を送りますからね。",
        "...I'll contact you when repairs are done.\nAnd Vargus, I'll be sending you the bill later.",
        "……修复完成后会通知您的。\n巴尔加斯先生，账单之后会寄给您的哦。",
        actor=lily,
    ).say(
        "bill_2",
        "おい、待て、蘇生費用もまだ返せてないんだぞ……。",
        "Oi, hold on, I haven't even paid off the resurrection costs yet...",
        "喂，等等，复活费用都还没还完呢……",
        actor=balgas,
    ).say(
        "bill_3",
        "大丈夫ですよ。\n払い終わるまで、いくらでも蘇らせてあげますから。",
        "Don't worry.\nI'll resurrect you as many times as it takes until you've paid it all off.",
        "没关系的。\n在您还完之前，我会无论多少次都把您复活的。",
        actor=lily,
    ).say(
        "bill_4",
        "リリィ、怖い……。",
        "Lily. Scary...",
        "莉莉，好可怕……",
        actor=nul,
    ).say(
        "bill_5",
        "縁起でもねえこと言うな！",
        "Don't say stuff like that!",
        "别说那么不吉利的话！",
        actor=balgas,
    ).say(
        "bill_6",
        "いや、でもバルガスさんのせいだからね、これ。",
        "I mean, this IS your fault, Vargus.",
        "不过，这确实是巴尔加斯先生的错吧。",
        actor=iris,
    ).say(
        "bill_7",
        "……おう。",
        "...Yeah.",
        "……嗯。",
        actor=balgas,
    ).jump(sales_start)

    # シーン14: 販売開始
    builder.step(sales_start).say(
        "sale_1",
        "さて、改めまして。\n『大部屋の巻物』、本日より販売開始です。",
        "Now then, allow me to start over.\nThe 'Scroll of Big Room' is now on sale starting today.",
        "那么，重新介绍一下。\n『大房间卷轴』，从今天起正式开卖。",
        actor=lily,
    ).say(
        "sale_2",
        "売るんだ……。",
        "She's actually selling it...",
        "居然真的要卖啊……",
        actor=iris,
    ).say(
        "sale_3",
        "空間を広げます。物理的に。障害物を消滅させて。",
        "It expands space. Physically. By annihilating obstacles.",
        "它可以拓展空间。物理意义上的。通过消灭障碍物。",
        actor=lily,
    ).say(
        "sale_4",
        "正直欲しい。",
        "Honestly want one.",
        "说实话，想要。",
        actor=nul,
    ).say(
        "sale_5",
        "嘘はついてないもんね！",
        "Well, she's technically not lying!",
        "确实没撒谎嘛！",
        actor=iris,
    ).say(
        "sale_6",
        "俺は……しばらく買うのはやめとく。",
        "I'll... pass for a while.",
        "老子……暂时就不买了。",
        actor=balgas,
    ).say(
        "sale_7",
        "当然です。",
        "Naturally.",
        "那是当然的。",
        actor=lily,
    ).jump(warning)

    # シーン15: 注意事項
    builder.step(warning).say(
        "warn_1",
        "一応、注意事項を。\n闘技場や自宅では絶対に使わないでください。\n一応安全装置をつけましたが、万が一発動したら大惨事ですから。\n使用による損害は、全て使用者の責任です。",
        "A word of caution.\nNever use it in the arena or at home.\nI've added a safety mechanism, but if it goes off, it would be catastrophic.\nAll damages from use are the sole responsibility of the user.",
        "另外，有一些注意事项。\n绝对不要在斗技场或自己家里使用。\n虽然加了安全装置，但万一发动的话会酿成大祸。\n使用造成的一切损害，由使用者自行承担。",
        actor=lily,
    ).say(
        "warn_2",
        "免責事項、大事。",
        "Disclaimers. Important.",
        "免责条款，很重要。",
        actor=nul,
    ).say(
        "warn_3",
        "というか、バルガスさんが実証したんだよね。",
        "I mean, Vargus already proved that firsthand.",
        "话说，巴尔加斯先生已经亲身证明了吧。",
        actor=iris,
    ).say(
        "warn_4",
        "……もう言うな。",
        "...Drop it already.",
        "……别再说了。",
        actor=balgas,
    ).jump(epilogue)

    # シーン18: エピローグ
    builder.step(epilogue).say(
        "epi_1",
        "まあ、結果的には便利なもんができたんじゃないかな？",
        "Well, you ended up making something useful, didn't you?",
        "嘛，结果不是做出了挺方便的东西嘛？",
        actor=iris,
    ).say(
        "epi_2",
        "……そうですね。",
        "...I suppose so.",
        "……说的也是。",
        actor=lily,
    ).say(
        "epi_3",
        "リリィ、元気出して。次はきっとうまくいく。",
        "Lily. Cheer up. Next time will work.",
        "莉莉，打起精神来。下次一定会成功的。",
        actor=nul,
    ).say(
        "epi_4",
        "……ありがとう、Nul。",
        "...Thank you, Nul.",
        "……谢谢你，Nul。",
        actor=lily,
    ).say(
        "epi_5",
        "次の研究は何？",
        "What's the next research project?",
        "下一个研究是什么？",
        actor=iris,
    ).say(
        "epi_6",
        "……まだ決めてません。\nでも、今度こそ夢のある魔法を……！",
        "...I haven't decided yet.\nBut this time, a truly magical, dreamy spell...!",
        "……还没决定。\n但这次一定要做出充满梦想的魔法……！",
        actor=lily,
    ).say(
        "epi_7",
        "頼むから、実験は外でやってくれ。",
        "Please, do your experiments outside.",
        "拜托了，实验去外面做吧。",
        actor=balgas,
    ).say(
        "epi_8",
        "それは同意。",
        "Agreed.",
        "同意。",
        actor=nul,
    ).say(
        "epi_9",
        "ひどい！！",
        "So mean!!",
        "过分！！",
        actor=lily,
    ).jump(ending)

    # エンディング
    builder.step(ending).complete_quest(QuestIds.PG_03_SCROLL_SHOWCASE).action(
        "modInvoke", param="update_lily_shop_stock()", actor="pc"
    ).drama_end()
