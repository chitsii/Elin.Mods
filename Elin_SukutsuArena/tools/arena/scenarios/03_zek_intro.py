"""
03_zek.md - 影歩きの邂逅
ゼクとの初遭遇イベント
"""

from arena.builders import DramaBuilder
from arena.data import Actors, Keys, QuestIds


def define_zek_intro(builder: DramaBuilder):
    """
    ゼクとの初遭遇イベント
    シナリオ: 03_zek.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_lobby_anomaly")
    scene2 = builder.label("scene2_introduction")
    react_who = builder.label("react_who")
    react_merchant = builder.label("react_merchant")
    react_silent = builder.label("react_silent")
    scene3 = builder.label("scene3_forbidden_offer")
    price_ask = builder.label("price_ask")
    price_refuse = builder.label("price_refuse")
    price_consider = builder.label("price_consider")
    scene4 = builder.label("scene4_return_shadow")
    ending = builder.label("ending")

    # ========================================
    # シーン1: ロビーの異変
    # ========================================
    builder.step(main).play_bgm("BGM/Ominous_Suspense_01").say(
        "narr_1",
        "ロビーの喧騒が、突如として膜を隔てたかのように遠のく。バルガスの怒声も、リリィのペンが紙を削る音も、水の底に沈んだように遠い。",
        "The clamor of the lobby suddenly recedes, as if separated by an invisible membrane. Vargus's bellowing and the scratch of Lily's pen on paper seem distant, submerged beneath water.",
        "大厅的喧嚣突然远去，仿佛隔着一层薄膜。巴尔加斯的怒吼，莉莉笔尖划过纸张的声音，都如同沉入水底般遥远。",
        actor=narrator,
    ).say(
        "narr_2",
        "ロビーの北西の隅、瓦礫が積み上がった影が、生き物のように蠢き始めた。\n空間がガラスのように鋭くひび割れ、その亀裂から、煤けたぼろ布を幾重にも纏った長身の影が滑り出す。",
        "In the northwest corner of the lobby, the shadow behind a pile of rubble begins to writhe as if alive.\nSpace fractures like glass, sharp and sudden. From that fissure, a tall shadow draped in layers of soot-stained rags glides forth.",
        "大厅西北角，瓦砾堆积的阴影开始如活物般蠕动。\n空间如玻璃般锐利地碎裂，从裂缝中滑出一个披着层层煤灰破布的高大身影。",
        actor=narrator,
    ).shake().say(
        "narr_4",
        "男が踏み出す足跡からは、黒い霧のような魔力の残滓が立ち上り、一瞬で消えていく。周囲の温度が数度下がり、肌を刺すような違和感がその場の空気を支配した。",
        "From each footstep the man takes, residual magic rises like black mist before vanishing in an instant. The temperature drops several degrees, and a prickling unease pervades the air.",
        "男人每迈出一步，都有如黑雾般的魔力残渣升起，瞬间消散。周围温度骤降数度，刺骨的不适感支配了空气。",
        actor=narrator,
    ).jump(scene1)

    # ========================================
    # シーン1: ゼク登場
    # ========================================
    builder.step(scene1).focus_chara(Actors.ZEK).say(
        "zek_1",
        "……おや。失礼、驚かせるつもりはなかったのですよ。\nただ、あまりに芳しい『敗北の予感』が漂ってきたもので……。つい、こちらへ顔を出してしまいました。",
        "...Oh my. Do forgive me, I had no intention of startling you.\nIt's simply that such a fragrant 'premonition of defeat' wafted my way... I couldn't resist making an appearance.",
        "……哎呀。抱歉，我并非有意吓到您。\n只是那芬芳的『败北预感』飘了过来……忍不住就露面了。",
        actor=zek,
    ).say(
        "narr_5",
        "彼は優雅に、しかしどこか爬虫類を思わせる動作で一礼する。",
        "He bows with elegance, yet there is something reptilian in his movements.",
        "他优雅地鞠躬，动作中却带着几分爬行动物的意味。",
        actor=narrator,
    ).jump(scene2)

    # ========================================
    # シーン2: 商人の自己紹介
    # ========================================
    builder.step(scene2).play_bgm("BGM/Zek_Merchant").say(
        "narr_6",
        "ゼクは懐から、何か金属的なキューブ状の魔道具らしきものを取り出し、愛おしげに撫でる。\nその手つきは商人が商品を扱うそれではない。まるで、二度と失いたくない宝物を確かめるように。",
        "Zek produces something from within his robes - a metallic, cube-shaped artifact - and caresses it with evident affection.\nHis touch is not that of a merchant handling wares. It is more like someone confirming a treasure they never wish to lose again.",
        "泽克从怀中取出一个金属立方体状的魔道具，爱怜地抚摸着。\n那手法不像商人对待商品。更像是在确认再也不想失去的珍宝。",
        actor=narrator,
    ).say(
        "zek_3",
        "お初にお目に掛かります、若き闘士殿。私はエゼキエル……単にゼクとお呼び下さい。この虚無の狭間で、行き場を失った『価値ある遺物』を拾い集める趣味人に過ぎません。そして、見つけた有用な品々を必要な方にお届けしています。",
        "Allow me to introduce myself, young gladiator. I am Ezekiel... though Zek shall suffice. I am merely a hobbyist who collects 'valuable relics' lost in this void between worlds. And I deliver these useful items to those in need.",
        "初次见面，年轻的斗士。我是以西结……请叫我泽克就好。我只是在这虚无夹缝中收集失落『珍贵遗物』的爱好者。并将有用的物品送到需要的人手中。",
        actor=zek,
    ).say(
        "zek_4",
        "バルガスのように、あなたの肉体を試すような無作法はいたしません。私はただ、あなたの行く先に待つ『絶望』を、少しだけ華やかなものに変えるお手伝いをしたいだけなのです……ふふ。",
        "I shall not impose upon your person as Vargus does. I merely wish to help transform the 'despair' that awaits you into something a touch more... colorful. Heh heh...",
        "我不会像巴尔加斯那样鲁莽地考验您的肉体。我只是想帮您把前方等待的『绝望』，变得稍微华丽一些……呵呵。",
        actor=zek,
    )

    # プレイヤーの選択肢
    builder.choice(react_who, "……何者だ？", "...Who are you?", "……你是什么人？", text_id="c_who").choice(
        react_merchant, "商人なのか？ 売り物はなんだ？", "A merchant? What do you sell?", "商人吗？卖什么的？", text_id="c_merchant"
    ).choice(react_silent, "（無言で警戒する）", "(Remain silent and wary)", "（沉默警戒）", text_id="c_silent")

    # 選択肢反応: お前は何者だ？
    builder.step(react_who).say(
        "zek_r1",
        "ふふ、警戒心がおありで結構。ですが、私はただの商人。あなたに害を加える気など、毛頭ございませんよ。……少なくとも、今は。",
        "Heh heh... How delightful that you're cautious. But I'm afraid I'm merely a merchant. I have no intention whatsoever of harming you. ...At least, not at present.",
        "呵呵，有警惕心真好。但我只是个商人。完全没有伤害您的意思哦。……至少现在没有。",
        actor=zek,
    ).jump(scene3)

    # 選択肢反応: 商人だと？
    builder.step(react_merchant).say(
        "zek_r2",
        "おや、興味をお持ちで？ では、ご覧に入れましょう。この世界の果てで、命を繋ぐための『道具』を。",
        "Oh my, you're interested? Then allow me to show you. The 'tools' for sustaining life at the edge of this world.",
        "哎呀，您感兴趣？那就让您看看吧。在这世界尽头维系生命的『道具』。",
        actor=zek,
    ).jump(scene3)

    # 選択肢反応: 無言で警戒
    builder.step(react_silent).say(
        "zek_r3",
        "……ほう。言葉少なに、しかし鋭い視線。あなたの本能が、私を『危険』だと囁いているのでしょうね。賢明です。",
        "...I see. Few words, yet such a piercing gaze. Your instincts must be whispering that I am 'dangerous.' How wise of you.",
        "……哦。话虽少，目光却很锐利。您的本能一定在低语我是『危险的』吧。真是明智。",
        actor=zek,
    ).jump(scene3)

    # ========================================
    # シーン3: 禁忌の誘い
    # ========================================
    builder.step(scene3).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_7",
        "ゼクが長い指先を虚空で躍らせると、空間の裂け目から禍々しいオーラを放つ品々が浮かび上がる。",
        "As Zek's long fingers dance through the air, items radiating an ominous aura emerge from rifts in space.",
        "泽克修长的手指在虚空中舞动，散发不祥气息的物品从空间裂缝中浮现。",
        actor=narrator,
    ).say(
        "narr_8",
        "焼けただれた魔導書、ページが勝手にめくれ、奇妙な文字が光る。不規則に脈打つ肉塊、まるで心臓のように拍動している。毒々しく発光する薬瓶、中の液体が泡立っている。",
        "A scorched grimoire whose pages turn on their own, strange letters glowing. A lump of flesh pulsating irregularly like a heart. Vials glowing with toxic light, the liquid within bubbling.",
        "烧焦的魔导书，书页自动翻动，奇异文字发光。不规则跳动的肉块，如同心脏般搏动。散发毒光的药瓶，里面的液体冒着泡。",
        actor=narrator,
    ).say(
        "zek_5",
        "ああ、次なるランクF……『泥犬』への試練。そこには、あなたの体温を根こそぎ奪い去る『凍てつく魔物』が待ち構えている。",
        "Ah yes, the trial for Rank F... the 'Mud Hound.' There awaits a 'freezing beast' that shall strip away every last bit of your body heat.",
        "啊是的，下一个F级……『泥犬』的试炼。那里有『冰冻魔物』等着，会彻底夺走您的体温。",
        actor=zek,
    ).say(
        "zek_6",
        "地上の安っぽい防具では、魂まで凍り付いてしまうでしょう。……ああ、想像できますよ。あなたが氷漬けになり、そのまま観客たちの冷笑の中で砕け散る様が。",
        "With surface-world armor of such poor quality, even your soul shall freeze solid. ...Ah, I can picture it now. You, encased in ice, shattering amidst the audience's cold laughter.",
        "穿着地上那廉价防具，连灵魂都会冻住。……啊，我能想象。您被冰封，在观众的冷笑中碎裂的样子。",
        actor=zek,
    ).say(
        "zek_7",
        "どうでしょう、この薬……名は『万難のエリクサー』。飲めば血潮が沸き立ち、氷をも溶かす力を得られます。",
        "What say you to this potion? It is called the 'Elixir of Tribulations.' Drink it, and your blood shall boil with the power to melt even ice.",
        "这药如何……名为『万难灵药』。喝下后热血沸腾，获得融化冰雪的力量。",
        actor=zek,
    ).say(
        "zek_8",
        "……もっとも、引き換えに、あなたの善性を少しばかり消費することになりますが。命を落とす不条理に比べれば、安い対価だと思いませんか？",
        "...Of course, in exchange, you shall expend a small portion of your virtue. But compared to the absurdity of losing your life, don't you think that's a small cost to pay?",
        "……当然，作为代价，您的善性会稍微消耗一些。但比起失去性命的荒谬，这是很小的代价吧？",
        actor=zek,
    )

    # プレイヤーの選択肢
    builder.choice(
        price_ask, "代償とは、具体的には？", "What exactly is the cost?", "具体的代价是什么？", text_id="c_price_ask"
    ).choice(price_refuse, "怪しすぎる。断る", "Too suspicious. I refuse.", "太可疑了。我拒绝。", text_id="c_price_refuse").choice(
        price_consider, "……考えておく", "...I'll think about it.", "……我考虑一下。", text_id="c_price_consider"
    )

    # 選択肢反応: 代償とは？
    builder.step(price_ask).say(
        "zek_p1",
        "ふふ、慎重なこと。ええ、あなたの善性……いわゆる『カルマ』を少々捧げていただくだけです。……まあ、『少々』がどの程度かは、使ってみてのお楽しみということで。",
        "Heh heh... How cautious you are. Yes, you need only offer a small portion of your virtue... your 'karma,' as it were. ...Well, precisely how 'small' that portion is shall be a delightful surprise upon use.",
        "呵呵，真谨慎。是的，只需献上您的善性……也就是『业力』的一小部分。……嘛，『一小部分』到底是多少，用了才知道的惊喜。",
        actor=zek,
    ).jump(scene4)

    # 選択肢反応: 断る
    builder.step(price_refuse).say(
        "zek_p2",
        "おや、残念。ですが、無理強いはいたしません。……いつでもお声掛けくださいませ。あなたが『絶望』を前にした時、私の品が恋しくなるでしょうから。",
        "Oh my, how unfortunate. But I shall not force the transaction upon you. ...Do call upon me anytime. When you face 'despair,' you'll find yourself longing for my wares.",
        "哎呀，真遗憾。但我不会强迫您。……随时来找我。当您面对『绝望』时，一定会想念我的商品的。",
        actor=zek,
    ).jump(scene4)

    # 選択肢反応: 考えておく
    builder.step(price_consider).say(
        "zek_p3",
        "賢明な判断です。焦る必要はございません。私は常に、影の中で待っておりますから。",
        "A wise decision. There is no need to rush. I shall always be waiting in the shadows.",
        "明智的决定。不必着急。我会一直在暗处等候。",
        actor=zek,
    ).jump(scene4)

    # ========================================
    # シーン4: 影への帰還
    # ========================================
    builder.step(scene4).play_bgm("BGM/Lobby_Normal").say(
        "narr_cube2",
        "ゼクはキューブを虚空にかざし、何かを記録するような仕草を見せた。\nキューブの表面に、あなたの姿が一瞬、幻影のように映り込む。",
        "Zek holds the cube aloft in the void, making a gesture as if recording something.\nFor an instant, your figure is reflected on the cube's surface like a phantom.",
        "泽克将立方体举向虚空，做出仿佛在记录什么的动作。\n立方体表面一瞬间映出了您的身影，如同幻影。",
        actor=narrator,
    ).say(
        "zek_9",
        "さあ、賢明な選択を。私は常に、この歪んだ影の中に潜んでおりますよ。",
        "Now then, make a wise choice. I shall always be lurking within these twisted shadows.",
        "那么，做出明智的选择吧。我会一直潜伏在这扭曲的暗影中。",
        actor=zek,
    ).say(
        "zek_10",
        "あなたが『力』を、あるいは『救い』を求めたくなったら……いつでもお声掛けください。あなたの魂が、完熟した果実のように弾けるその時まで、ね。",
        "When you desire 'power,' or perhaps 'salvation'... do call upon me anytime. Until that moment when your soul bursts like a ripened fruit. Heh heh...",
        "当您渴望『力量』或『救赎』时……随时来找我。直到您的灵魂像成熟果实般迸裂的那一刻，呵呵……",
        actor=zek,
    ).jump(ending)

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending).complete_quest(QuestIds.ZEK_INTRO).finish()
