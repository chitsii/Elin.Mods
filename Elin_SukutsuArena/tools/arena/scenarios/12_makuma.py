# -*- coding: utf-8 -*-
"""
12_makuma.md - 銀翼を彩る背徳の衣、そして遺棄された真実
"""

from arena.builders import DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestIds


def define_makuma(builder: DramaBuilder):
    """
    ランクB達成報酬：リリィの礼装授与とゼクによるヌルの真実暴露
    シナリオ: 12_makuma.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You", "你")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily", "莉莉")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek", "泽克")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus", "巴尔加斯")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_lily_gift")
    choice1 = builder.label("choice1")
    react1_thanks = builder.label("react1_thanks")
    react1_property = builder.label("react1_property")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_zek_appears")
    choice2 = builder.label("choice2")
    react2_failure = builder.label("react2_failure")
    react2_adventurer = builder.label("react2_adventurer")
    react2_silent = builder.label("react2_silent")
    scene3 = builder.label("scene3_null_truth")
    choice4 = builder.label("choice4")
    react4_bored = builder.label("react4_bored")
    react4_scared = builder.label("react4_scared")
    react4_defiant = builder.label("react4_defiant")
    scene3_continue = builder.label("scene3_continue")
    choice3 = builder.label("choice3")
    react3_wait = builder.label("react3_wait")
    react3_become = builder.label("react3_become")
    react3_stare = builder.label("react3_stare")
    scene4 = builder.label("scene4_balgas")
    ending = builder.label("ending")

    # ========================================
    # シーン1: リリィの作業室『次元固定装置のメンテナンス』
    # ========================================
    builder.step(main).play_bgm("BGM/Lily_Seductive_Danger").focus_chara(Actors.LILY).say(
        "narr_1",
        "リリィの作業室。普段の甘い香りはなく、代わりに金属とエーテルの匂いが漂っている。\n彼女は机の上に分解された魔道具の部品を並べ、細かな調整を行っていた。",
        "Lily's workshop. The usual sweet fragrance is absent, replaced by the scent of metal and ether.\nShe has arranged disassembled magical device components on her desk, making delicate adjustments.",
        "莉莉的工作室。平日里的甜香已然消散，取而代之的是金属与以太的气息。\n她将拆解的魔导器零件排列在桌上，正在进行精细的调整。",
        actor=narrator,
    ).say(
        "narr_3",
        "それは複雑な魔術回路が刻まれた球体状の装置ーーアリーナという異次元空間を維持するための『次元固定装置』の一部だった。",
        "It is a spherical device etched with intricate magical circuits--a component of the 'Dimensional Anchor' that maintains the Arena's extradimensional space.",
        "那是一个刻有复杂魔术回路的球状装置--用于维持角斗场这一异次元空间的「次元固定装置」的一部分。",
        actor=narrator,
    ).say(
        "lily_1",
        "……あら、いらっしゃい。少し待っていて。今、繊細な調整中なの。",
        "...Oh my, welcome. Please wait a moment. I'm in the middle of some delicate calibrations.",
        "……呀，欢迎您。请稍等片刻。我正在进行精密调整呢。",
        actor=lily,
    ).say(
        "narr_4",
        "リリィは集中しながらも、あなたの存在を意識するように髪をかきあげた。",
        "While maintaining her concentration, Lily brushes her hair aside as if acknowledging your presence.",
        "莉莉一边保持专注，一边撩起秀发，似乎在意识着你的存在。",
        actor=narrator,
    ).say(
        "lily_2",
        "これはね、アリーナという空間そのものを維持する装置よ。この次元は本来、不安定で存在できないはずの場所。それを『在る』ものとして固定しているの。……私の担当なの。",
        "This device maintains the very space that is the Arena. This dimension is inherently unstable--a place that shouldn't exist. I anchor it into 'being.' ...It's my responsibility.",
        "这个装置呢，是用来维持角斗场这个空间本身的。这个次元原本是不稳定、不该存在的地方。我将它固定为「存在」的状态。……这是我的职责。",
        actor=lily,
    ).say(
        "lily_2b",
        "……ねえ、もう少し近くに来て？ あなたの体温を感じていたいの。この部屋、少し冷えるから。",
        "...Come a little closer, won't you? I wish to feel your warmth. This room is rather... chilly.",
        "……呐，能再靠近一些吗？我想感受您的体温。这房间有些冷呢。",
        actor=lily,
    ).say(
        "lily_3",
        "さて……あなたの戦績、確認しましたよ。最初に比べて、動きの質が変わったわね。以前は荒削りだったけれど、今は無駄がない。",
        "Now then... I've reviewed your combat record. The quality of your movements has changed considerably since the beginning. You were rough around the edges before, but now there's no wasted motion.",
        "好了……我确认过您的战绩了。与最初相比，您的动作质量有了很大变化呢。以前还很粗糙，但现在已经没有多余的动作了。",
        actor=lily,
    ).say(
        "narr_5",
        "リリィの視線があなたの体をゆっくりと這うように観察する。",
        "Lily's gaze slowly traces over your body, observing you intently.",
        "莉莉的视线缓缓游走，仔细观察着你的身体。",
        actor=narrator,
    ).say(
        "lily_3b",
        "……体つきも変わったわ。筋肉の付き方、重心の取り方……\nふふ、数字や記録以上に、あなたの戦いには『華』がある。私を魅了する何かがね。",
        "...Your physique has changed as well. The way your muscles have developed, your center of gravity...\nHehe, beyond mere numbers and records, your battles possess a certain... 'elegance.' Something that captivates me.",
        "……体型也变了呢。肌肉的附着方式、重心的把握……\n呵呵，比起数字和记录，您的战斗更有一种「华彩」。有某种令我着迷的东西呢。",
        actor=lily,
    ).jump(choice1)

    # プレイヤーの選択肢
    builder.choice(react1_thanks, "……ありがとう", "...Thank you.", "……谢谢。", text_id="c1_thanks").choice(
        react1_property, "技術者の仕事もするのか", "You do technical work as well?", "你也做技术工作吗？", text_id="c1_technical"
    ).choice(react1_silent, "（無言で頷く）", "(Nod silently)", "（默默点头）", text_id="c1_silent")

    # 選択肢反応
    builder.step(react1_thanks).say(
        "lily_r1",
        "どういたしまして。……あなたの成長を見守るのは、私の楽しみなの。……特別な意味で、ね。",
        "You're most welcome. ...Watching you grow is my pleasure. ...In a very special sense.",
        "不客气。……看着您成长是我的乐趣呢。……是非常特别的那种意义。",
        actor=lily,
    ).jump(scene2)

    builder.step(react1_property).say(
        "lily_r2",
        "ええ。運営や闘士の管理だけが私の仕事じゃないの。闘技場という空間全体、私が管理しているのよ。……それに、あなた専属の『観察係』もね。",
        "Indeed. Managing operations and fighters isn't all I do. I oversee the entire arena space. ...And I serve as your personal 'observer' as well.",
        "是的。运营和斗士管理并不是我全部的工作。整个角斗场空间都由我管理。……而且，我还是您专属的「观察员」呢。",
        actor=lily,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "lily_r3",
        "……相変わらず無口ね。でも、その沈黙も悪くないわ。言葉がなくても、あなたの鼓動は伝わってくるから。",
        "...Still so quiet, aren't you? But I don't mind your silence. Even without words, I can feel your heartbeat.",
        "……还是那么沉默寡言呢。但是，这份沉默也不坏。即使没有言语，您的心跳也能传达给我。",
        actor=lily,
    ).jump(scene2)

    # ========================================
    # シーン2: ゼクの囁き『遺棄された記憶：ヌルの正体』
    # ========================================
    builder.step(scene2).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_6",
        "リリィの部屋を辞し、薄暗い廊下を歩くあなたの背後に不自然な影が伸びる。\nゼクが、まるで壁のシミから染み出すように姿を現した。",
        "Leaving Lily's room, you walk through the dimly lit corridor as an unnatural shadow stretches behind you.\nZek materializes as if seeping out from a stain on the wall.",
        "离开莉莉的房间，走在昏暗的走廊上，一道不自然的影子在你身后延伸开来。\n泽克现身了，仿佛是从墙上的污渍中渗透出来一般。",
        actor=narrator,
    ).focus_chara(Actors.ZEK).say(
        "narr_8",
        "その手には、鈍く明滅するクリスタルの破片ーー『記録チップ』が握られていた。",
        "In his hand, he holds a dimly flickering crystal shard--a 'memory chip.'",
        "他手中握着一块闪烁着微光的水晶碎片--「记录芯片」。",
        actor=narrator,
    ).say(
        "zek_2",
        "昇格おめでとうございます。ところで、あの人形……ヌル。あれが何であったか、知りたくはありませんか？",
        "Heh heh... Congratulations on your promotion. By the way, that puppet... Null. Would you care to know what she truly was?",
        "嘿嘿……恭喜您晋升了。话说，那个人偶……诺尔。您不想知道她究竟是什么吗？",
        actor=zek,
    ).say("narr_9", "彼はクリスタルの破片を掲げる。", "He raises the crystal shard.", "他举起水晶碎片。", actor=narrator).say(
        "zek_3",
        "このチップに残されていたのは……かつてグランドマスター・アスタロトに挑み、敗れ、魂を『整理』された……ある冒険者の最期の記録です。\n\nヌルは、アスタロトが退屈しのぎに作った『失敗作』に過ぎない。",
        "What remains on this chip is... the final record of an adventurer who once challenged Grandmaster Astaroth, lost, and had their soul 'reorganized.'\n\nNull was merely a 'failed experiment' Astaroth created to pass the time.",
        "这枚芯片上残留的是……曾经挑战大宗师阿斯塔罗特、败北后灵魂被「整理」的……某位冒险者的最后记录。\n\n诺尔不过是阿斯塔罗特为了消遣而制造的「失败品」罢了。",
        actor=zek,
    ).jump(choice2)

    # プレイヤーの選択肢
    builder.choice(
        react2_adventurer, "アスタロトが冒険者を……？", "Astaroth did this to an adventurer...?", "阿斯塔罗特对冒险者……？", text_id="c2_adventurer"
    ).choice(react2_silent, "（無言で聞く）", "(Listen in silence)", "（沉默聆听）", text_id="c2_silent")

    # 選択肢反応
    builder.step(react2_adventurer).say(
        "zek_r2", "ええ。彼にとって、闘士は『素材』に過ぎません。", "Indeed. To him, fighters are merely 'raw materials.'", "是的。对他而言，斗士不过是「素材」而已。", actor=zek
    ).jump(scene3)

    builder.step(react2_silent).say(
        "zek_r3",
        "……ふふ、沈黙は賢明さの証。では、続けさせていただきましょう。",
        "Heh heh... Silence is a mark of wisdom. Then allow me to continue.",
        "……呵呵，沉默是智慧的象征。那么，请允许我继续。",
        actor=zek,
    ).jump(scene3)

    # ========================================
    # シーン3: ヌルの真実とアリーナの目的
    # ========================================
    builder.step(scene3).say(
        "narr_10",
        "ゼクはクリスタルの破片を翳し、その中に浮かぶ記憶を語る。",
        "Zek holds up the crystal shard and speaks of the memories contained within.",
        "泽克举起水晶碎片，述说着其中浮现的记忆。",
        actor=narrator,
    ).say(
        "zek_5",
        'チップにはこう記されています。……『アリーナは、"神の種"を育てるための孵化器だ』と。\nこの計画は……アスタロト様がかつて失ったものを取り戻すためのもの、らしいですよ。',
        "The chip contains this inscription... 'The Arena is an incubator for cultivating the Seeds of Divinity.'\nThis plan, it seems... is for Lord Astaroth to reclaim something he once lost.",
        "芯片上是这样记载的。……「角斗场是培育『神之种子』的孵化器」。\n这个计划……似乎是为了让阿斯塔罗特大人夺回他曾经失去的东西。",
        actor=zek,
    ).say(
        "zek_5c",
        "『カラドリウス』……竜族の楽園。5万年前に滅んだとか。詳しいことは私も知りませんがね。",
        "'Caladrius'... The paradise of the dragon race. Destroyed fifty thousand years ago, or so I'm told. I don't know the details myself.",
        "「卡拉德里乌斯」……龙族的乐园。据说五万年前就毁灭了。具体的事情我也不清楚。",
        actor=zek,
    ).say(
        "zek_6b",
        "敗北した闘士がどうなるか……あなたはもうご覧になったはず。禍々しい霧に包まれ、『回収』された者たちは……アスタロトの計画の礎となるのです。\n\nあなたも、本来ならば彼女と同じ運命を辿るはずだった。……あなたは『特別な事情がある』ようですが。",
        "What becomes of defeated fighters... you've already witnessed it. Those enveloped by that ominous mist and 'harvested'... they become the foundation of Astaroth's plan.\n\nYou, too, should have met the same fate as her. ...But it seems you have 'special circumstances.'",
        "败北的斗士会变成什么样……您应该已经见过了。被那诡异的雾气包围、被「回收」的人们……会成为阿斯塔罗特计划的基石。\n\n您原本也应该和她遭遇同样的命运。……但您似乎有「特殊情况」。",
        actor=zek,
    ).say(
        "zek_6d",
        "……不思議に思いませんか？ なぜあなたは、敗北しても『回収』されないのか。\n私の推測ですが……あなたはイルヴァと繋がりがあるでしょう？ あなただけが、外部のリソースを使って、効率よく強くなれる。",
        "...Doesn't it strike you as strange? Why aren't you 'harvested' when you lose?\nMy hypothesis is... you have a connection to Ylva, don't you? Only you can efficiently grow stronger using external resources.",
        "……您不觉得奇怪吗？为什么您败北了也不会被「回收」。\n这是我的推测……您与伊尔瓦有联系吧？只有您能利用外部资源高效地变强。",
        actor=zek,
    ).say(
        "zek_6f",
        "それは、アスタロト様にとって、あなたが『投資価値のある商品』であることを意味します。今すぐ回収するより、もっと育ててからの方が得だと判断されたのでしょうね。\n……つまり、あなたは大事に育てられているのですよ。おめでとうございます。",
        "To Lord Astaroth, this means you are 'merchandise worth investing in.' He must have determined it's more profitable to nurture you further before harvesting.\n...In other words, you're being carefully cultivated. Heh heh... Congratulations.",
        "对阿斯塔罗特大人来说，这意味着您是「值得投资的商品」。他大概判断，比起现在就回收，继续培养后再收割更有利可图。\n……也就是说，您正在被精心培育呢。嘿嘿……恭喜您。",
        actor=zek,
    ).jump(choice4)

    # プレイヤーのRP選択肢（回収されない理由への反応）
    builder.choice(react4_bored, "……与太話はうんざりだ", "...I'm tired of your tall tales.", "……你的胡言乱语让我厌烦。", text_id="c4_bored").choice(
        react4_scared, "ゾッとする話だね", "That's a chilling story.", "真是令人毛骨悚然的故事。", text_id="c4_scared"
    ).choice(
        react4_defiant, "私に手を出す奴は必ず後悔させてやる", "Anyone who tries to touch me will regret it.", "谁敢动我，我一定让他后悔。", text_id="c4_defiant"
    )

    builder.step(react4_bored).say(
        "zek_r4a",
        "与太話……ふふ、そう思いたい気持ちは分かりますよ。でも、真実は変わりません。",
        "Tall tales... Heh heh, I understand the desire to think so. But the truth remains unchanged.",
        "胡言乱语……呵呵，我理解您想这么认为的心情。但是，真相不会改变。",
        actor=zek,
    ).jump(scene3_continue)

    builder.step(react4_scared).say(
        "zek_r4b", "ええ、そうでしょうとも。", "Yes, I imagine it would be.", "是啊，确实如此。", actor=zek
    ).jump(scene3_continue)

    builder.step(react4_defiant).say(
        "zek_r4c",
        "クク……その気概、嫌いではありません。アスタロト様もきっと、そう言われたら喜ぶでしょうね。",
        "Heh heh heh... That spirit of yours--I don't dislike it. I'm sure Lord Astaroth would be delighted to hear such defiance.",
        "呵呵呵……这份气概，我并不讨厌。阿斯塔罗特大人听到这番话，一定也会很高兴吧。",
        actor=zek,
    ).jump(scene3_continue)

    # シーン3続き
    builder.step(scene3_continue).say(
        "narr_11", "遠くでバルガスの足音が聞こえる。", "The sound of Vargus's footsteps echoes from a distance.", "远处传来巴尔加斯的脚步声。", actor=narrator
    ).say(
        "zek_8",
        "おや、バルガスさんが来られましたね。あの方には嫌われているので、私はここでおいとましましょうか。\n……いずれまた、この続きを話すとしましょう。",
        "Oh my, here comes Vargus. He's not fond of me, so perhaps I should take my leave.\n...We shall continue this conversation another time.",
        "哎呀，巴尔加斯先生来了呢。那位不太喜欢我，我还是先告辞吧。\n……改日再继续这个话题吧。",
        actor=zek,
    ).jump(
        choice3
    )

    # プレイヤーの選択肢
    builder.choice(react3_wait, "待て！", "Wait!", "等等！", text_id="c3_wait").choice(
        react3_become, "私も……ヌルのようになるのか？", "Will I... end up like Null?", "我也会……变成诺尔那样吗？", text_id="c3_become"
    ).choice(react3_stare, "（無言を貫く）", "(Remain silent)", "（保持沉默）", text_id="c3_stare")

    # 選択肢反応
    builder.step(react3_wait).say(
        "zek_r4",
        "……ふふ、焦らないで。時が来れば、全てが明らかになるでしょう。",
        "Heh heh... Don't be impatient. When the time comes, all shall be revealed.",
        "……呵呵，别着急。时机成熟时，一切都会真相大白。",
        actor=zek,
    ).say("narr_12", "ゼクが影の中へと消えていく。", "Zek fades into the shadows.", "泽克消失在阴影之中。", actor=narrator).jump(scene4)

    builder.step(react3_become).say(
        "zek_r5", "……それは、あなた次第です。", "...That depends entirely on you.", "……那取决于您自己。", actor=zek
    ).say("narr_13", "ゼクが影の中へと消えていく。", "Zek fades into the shadows.", "泽克消失在阴影之中。", actor=narrator).jump(scene4)

    builder.step(react3_stare).say(
        "zek_r6", "……では、またお会いしましょう。", "...Until we meet again.", "……那么，后会有期。", actor=zek
    ).say("narr_14", "ゼクが影の中へと消えていく。", "Zek fades into the shadows.", "泽克消失在阴影之中。", actor=narrator).jump(scene4)

    # ========================================
    # シーン4: バルガスの警告
    # ========================================
    builder.step(scene4).play_bgm("BGM/Lobby_Normal").focus_chara(Actors.BALGAS).say(
        "narr_15", "バルガスが廊下に現れる。", "Vargus appears in the corridor.", "巴尔加斯出现在走廊上。", actor=narrator
    ).say(
        "balgas_1",
        "……おい、ゼクの野郎、また何か吹き込んでいったんじゃねえだろうな？",
        "...Oi. That bastard Zek didn't go fillin' your head with nonsense again, did he?",
        "……喂，那个混蛋泽克，该不会又给你灌输了什么乱七八糟的东西吧？",
        actor=balgas,
    ).say(
        "balgas_nul1",
        "……ヌルの話だと？\n\n……俺は、ヌルを見るとカインを思い出す。あいつも、アスタロトの『素材』にされちまったからな。",
        "...Null, you say?\n\n...When I see Null, I'm reminded of Cain. He became Astaroth's 'raw material' too.",
        "……诺尔的事？\n\n……我一看到诺尔就会想起凯恩。那家伙也被阿斯塔罗特变成了『素材』。",
        actor=balgas,
    ).say(
        "balgas_nul3",
        "ヌルを恨んでも仕方ねえ。恨むべきはアスタロトだ。あいつがカインを、そしてヌルを『作った』んだからな。\n……忌々しいのは確かだが、それはヌル自身じゃねえ。あいつが纏っている『カインの残滓』を見せつけられることが、な。",
        "No point hatin' Null. Astaroth's the one to blame. He's the one who 'made' Cain into that... and Null too.\nTch. ...It's infuriating, sure, but not because of Null herself. It's havin' to look at the 'remnants of Cain' she wears.",
        "恨诺尔也没有意义。该恨的是阿斯塔罗特。是那家伙把凯恩、还有诺尔『制造』出来的。\n……确实让人恼火，但不是因为诺尔本身。是因为不得不看着她身上披着的『凯恩的残渣』。",
        actor=balgas,
    ).jump(ending)

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.GrantMakumaReward();"
    ).complete_quest(QuestIds.MAKUMA).finish()
