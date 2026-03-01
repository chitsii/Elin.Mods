# -*- coding: utf-8 -*-
"""
04_rank_up_02.md - ランクF昇格試合『凍土の魔犬と凍てつく咆哮』
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, Keys, QuestIds, Rank, SessionKeys


def define_rank_up_F(builder: DramaBuilder):
    """
    Rank F 昇格試合「凍土の魔犬と凍てつく咆哮」
    シナリオ: 04_rank_up_02.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1_reception = builder.label("scene1_reception")
    choice_lily = builder.label("choice_lily")
    lily_r1 = builder.label("lily_r1")
    lily_r2 = builder.label("lily_r2")
    lily_r3 = builder.label("lily_r3")
    lily_r3_ready = builder.label("lily_r3_ready")
    cancel = builder.label("cancel")
    scene2_balgas = builder.label("scene2_balgas")
    choice_balgas = builder.label("choice_balgas")
    balgas_r1 = builder.label("balgas_r1")
    balgas_r2 = builder.label("balgas_r2")
    balgas_r3 = builder.label("balgas_r3")
    battle_start = builder.label("battle_start")

    # ========================================
    # シーン1: 凍てつく境界線（受付）
    # ========================================
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/Lobby_Normal"
    ).focus_chara(Actors.LILY).say(
        "narr_1",
        "ロビーの空気が一変している。受付カウンターには薄く霜が降り、リリィの吐息すら白く濁っている。",
        "The lobby's atmosphere has transformed entirely. Frost clings to the reception counter, and even Lily's breath hangs white in the air.",
        "大厅的气氛完全变了。前台柜台上覆着薄霜，连莉莉的呼吸都凝成了白雾。",
        actor=narrator,
    ).say(
        "narr_2",
        "彼女は冷たくなった指先を温めるように摩りながら、氷の結晶が浮き出た登録証を提示した。",
        "She rubs her chilled fingertips together for warmth, presenting a registration card adorned with ice crystals.",
        "她搓着冰凉的指尖取暖，同时递出一张浮现着冰晶的登记证。",
        actor=narrator,
    ).say(
        "narr_3",
        "松明の炎すら、凍気に押されて小さく震えている。床の石畳には薄い氷膜が張り、踏むたびにパリパリと音を立てる。",
        "Even the torch flames tremble, pressed back by the freezing air. A thin layer of ice coats the stone floor, crackling with each footstep.",
        "就连火把的火焰也被寒气逼迫得微微颤抖。地面的石板上覆着薄冰，每走一步都发出咔嚓咔嚓的声响。",
        actor=narrator,
    ).jump(scene1_reception)

    builder.step(scene1_reception).say(
        "lily_1",
        "……次は『冷気』の洗礼です。",
        "...Next comes the baptism of 'frost.'",
        "……接下来是『寒气』的洗礼。",
        actor=lily,
    ).say(
        "narr_4",
        "彼女は氷の結晶が浮き出た登録証を、あなたの前に滑らせる。",
        "She slides a registration card, crystalline with ice, across the counter toward you.",
        "她将一张浮现着冰晶的登记证滑向您面前。",
        actor=narrator,
    ).say(
        "lily_2a",
        "お気の毒に。観客席の『あの方々』は、今夜は新鮮なフローズン・ミートを召し上がりたい気分だそうですよ。\n\n対戦相手は『ヴォイド・アイスハウンド』の群れ。",
        "How unfortunate. 'Those patrons' in the stands are rather craving fresh frozen meat this evening.\n\nYour opponents shall be a pack of 'Void Ice Hounds.'",
        "真是可怜呢。观众席上的『那些大人物们』说，今晚想品尝新鲜的冷冻肉呢。\n\n您的对手是一群『虚空冰犬』。",
        actor=lily,
    ).say(
        "lily_2b",
        "その咆哮は空気を凍らせ、その牙は魂の熱を奪い去る。",
        "Their howls freeze the very air, and their fangs drain the warmth from one's soul.",
        "它们的咆哮能冻结空气，它们的獠牙能夺走灵魂的热度。",
        actor=lily,
    ).say(
        "lily_2c",
        "……もし、心臓の鼓動を止められたくないのであれば、あのおぞましい商人から何か『温かいもの』でも買い取っておくことでしたね。ま、今からでも遅くはありませんが。",
        "...If you wish to keep your heart beating, you might have purchased something 'warm' from that dreadful merchant. Hehe, though it is not too late, I suppose.",
        "……如果您不想让心脏停止跳动的话，或许应该从那个可怕的商人那里买些『温暖的东西』呢。嗯，现在开始也不算晚。",
        actor=lily,
    )

    # プレイヤーの選択肢（リリィ）
    builder.choice(
        lily_r1,
        "自分の力だけで、正々堂々と勝つ",
        "I'll win fair and square, with my own strength",
        "我要凭自己的力量，光明正大地取胜",
        text_id="c_lily_1",
    ).choice(
        lily_r2,
        "準備は整っている。すぐに始めよう",
        "I'm prepared. Let's begin now",
        "我已经准备好了。马上开始吧",
        text_id="c_lily_2",
    ).choice(
        lily_r3,
        "少し……待ってくれ",
        "Wait... just a moment",
        "稍等……让我缓缓",
        text_id="c_lily_3",
    )

    # 選択肢反応: 自分の力で勝つ
    builder.step(lily_r1).say(
        "lily_r1_1",
        "ふふ、自信はおありのようで。……では、凍り付いた死体にならないよう、お祈りしておりますね。",
        "Hehe, such confidence you possess. ...Very well, I shall pray you do not become a frozen corpse.",
        "呵呵，您似乎很有自信呢。……那么，我会祈祷您不会变成冻僵的尸体的。",
        actor=lily,
    ).jump(scene2_balgas)

    # 選択肢反応: すぐに始めよう
    builder.step(lily_r2).say(
        "lily_r2_1",
        "準備万端、と。頼もしいこと。……それでは、どうぞ。地獄の冷凍庫へ。",
        "All prepared, are we? How reassuring. ...Then please, proceed to hell's freezer.",
        "准备万全了吗。真是可靠呢。……那么，请进吧。欢迎来到地狱的冷冻库。",
        actor=lily,
    ).jump(scene2_balgas)

    # 選択肢反応: 待ってくれ → キャンセル可能
    builder.step(lily_r3).say(
        "lily_r3_1",
        "おや、少し震えているようですね。……この寒さ、無理もありませんが。",
        "Oh my, you seem to be trembling. ...Though with this cold, one can hardly blame you.",
        "哎呀，您似乎有些发抖呢。……这么冷的天，也是情有可原的。",
        actor=lily,
    ).say(
        "lily_r3_2",
        "急ぎませんから、準備が整ったらお声掛けくださいませ。",
        "There is no rush. Please inform me when you are prepared.",
        "不着急，准备好了请告诉我。",
        actor=lily,
    ).choice(
        lily_r3_ready, "準備ができた", "I'm ready", "我准备好了", text_id="c_lily_ready"
    ).choice(
        cancel,
        "やっぱりやめる",
        "Never mind, I'll pass",
        "还是算了",
        text_id="c_lily_cancel",
    ).on_cancel(cancel)

    builder.step(lily_r3_ready).say(
        "lily_r3_ready",
        "……では、行ってらっしゃいませ。",
        "...Then, do take care.",
        "……那么，请慢走。",
        actor=lily,
    ).jump(scene2_balgas)

    builder.step(cancel).say(
        "lily_cancel",
        "賢明な判断かもしれませんね。……またのお越しをお待ちしております。",
        "A wise decision, perhaps. ...I shall await your return.",
        "或许这是明智的判断呢。……期待您下次光临。",
        actor=lily,
    ).finish()

    # ========================================
    # シーン2: バルガスの冷徹な眼差し
    # ========================================
    builder.step(scene2_balgas).focus_chara(Actors.BALGAS).say(
        "narr_5",
        "闘技場の門扉からは、身を切るような極寒の風が吹き荒れている。バルガスは分厚い外套に身を包み、凍りついた鉄格子を乱暴に叩いた。",
        "A bitter, bone-cutting wind howls from the arena gates. Vargus, wrapped in a thick cloak, pounds roughly on the frozen iron bars.",
        "角斗场的大门吹来刺骨的寒风。巴尔加斯裹着厚重的斗篷，粗暴地敲打着结冰的铁栏杆。",
        actor=narrator,
    ).say(
        "balgas_1",
        "おい、顔色が青白いぜ。戦う前から死体ごっこか？\nいいか、寒さってのは『恐怖』と同じだ。一度足が止まれば、そこが終着駅だと思え。",
        "Oi, you're lookin' pale. Playin' corpse before the fight even starts?\nListen up. Cold's the same as fear. Once your feet stop movin', that's your final stop.",
        "喂，你小子脸色发白啊。还没开打就装死吗？\n听好了，寒冷跟『恐惧』是一回事。脚步一停，那就是你小子的终点站了。",
        actor=balgas,
    ).say(
        "narr_6",
        "彼は凍りついた門扉を蹴り、氷の破片を散らす。",
        "He kicks the frozen gate, scattering shards of ice.",
        "他踢向结冰的门扉，冰屑四处飞溅。",
        actor=narrator,
    ).say(
        "balgas_2b",
        "厄介なのは、奴らは周囲の環境に溶け込む能力があることだ。俺が戦った時は、吹雪の中で何かに襲われて、何が起きているのか分からないまま傷だらけになった。",
        "The nasty part is, those things can blend into their surroundings. When I fought 'em, something attacked me in the blizzard and I got cut to ribbons without knowing what was happening.",
        "麻烦的是，那些家伙能融入周围环境。老子跟它们打的时候，在暴风雪里被什么东西袭击，不知道发生了什么就已经伤痕累累了。",
        actor=balgas,
    ).say(
        "balgas_2c",
        "奴らが姿を消しているのが分かったのは、俺の血がやつらの一匹に付着した時だった。",
        "I only realized they were invisible when my blood splattered on one of 'em.",
        "老子意识到它们隐身了，是在我的血溅到其中一只身上的时候。",
        actor=balgas,
    ).say(
        "balgas_3a",
        "奴らは群れで動く。一匹が吠えれば、次の一匹がお前の死角に回り込む。",
        "They hunt in packs. When one howls, the next one's already circlin' to your blind spot.",
        "那些家伙是成群行动的。一只嚎叫，下一只就绕到你小子的死角了。",
        actor=balgas,
    ).say(
        "balgas_3b",
        "常に動き続けろ。止まれば凍る、動けば血が巡る……単純な理屈だ。",
        "Keep movin', always. Stop and you freeze, move and the blood flows... simple as that.",
        "一直保持移动。停下就冻僵，动起来血液就流通……就这么简单。",
        actor=balgas,
    ).say(
        "narr_7",
        "彼は酒瓶を一口飲み、吐息を白く吐き出す。",
        "He takes a swig from his bottle, exhaling a white breath.",
        "他灌了一口酒，呼出白色的气息。",
        actor=narrator,
    ).say(
        "balgas_5",
        "……死にたくなければ、その体内の火種を絶やすんじゃねえぞ。生きる意志ってのは、案外そういうところから潰えていくもんだ。",
        "...If ya don't wanna die, don't let that fire inside ya go out. The will to live... it crumbles from places like that, more than you'd think.",
        "……不想死的话，别让体内的火种熄灭。生存的意志这东西，意外地会从那种地方开始崩溃。",
        actor=balgas,
    )

    # プレイヤーの選択肢（バルガス）
    builder.choice(
        balgas_r1,
        "分かった。必ず生きて帰る",
        "Got it. I'll come back alive",
        "明白了。我一定会活着回来",
        text_id="c_balgas_1",
    ).choice(
        balgas_r2,
        "動き続ける……覚えておく",
        "Keep moving... I'll remember that",
        "一直移动……我记住了",
        text_id="c_balgas_2",
    ).choice(
        balgas_r3,
        "（無言で頷く）",
        "(Nod silently)",
        "（默默点头）",
        text_id="c_balgas_3",
    )

    # 選択肢反応: 必ず生きて帰る
    builder.step(balgas_r1).say(
        "balgas_r1_1",
        "……ああ。その目だ。それを忘れるな。",
        "...Yeah. Those eyes. Don't forget 'em.",
        "……嗯。就是这眼神。别忘了。",
        actor=balgas,
    ).jump(battle_start)

    # 選択肢反応: 覚えておく
    builder.step(balgas_r2).say(
        "balgas_r2_1",
        "覚えるんじゃねえ。体に刻み込め。",
        "Don't just remember it. Carve it into your body.",
        "别光记住。给老子刻进身体里。",
        actor=balgas,
    ).jump(battle_start)

    # 選択肢反応: 無言で頷く
    builder.step(balgas_r3).say(
        "balgas_r3_1",
        "……フン。行ってこい。",
        "...Hah. Get goin'.",
        "……哼。去吧。",
        actor=balgas,
    ).jump(battle_start)

    # ========================================
    # シーン3: 戦闘開始
    # ========================================
    builder.step(battle_start).play_bgm("BGM/Battle_RankE_Ice").say(
        "narr_8",
        "ゲートが開いた瞬間、視界を覆うのは吹き荒れる銀世界の白。",
        "The moment the gate opens, a raging silver blizzard fills your vision with white.",
        "大门打开的瞬间，映入眼帘的是狂风呼啸的银色世界。",
        actor=narrator,
    ).shake().say(
        "narr_9",
        "地面は氷に閉ざされ、踏みしめるたびに不吉な軋みを上げる。天井はなく、ただ虚無の闇と、そこから降り注ぐ氷の結晶。",
        "The ground is locked in ice, groaning ominously with each step. No ceiling above, only void darkness and ice crystals raining down.",
        "地面被冰封住，每踩一步都发出不祥的吱嘎声。头顶没有天花板，只有虚无的黑暗和从中洒落的冰晶。",
        actor=narrator,
    ).say(
        "narr_10",
        "突如、雪霧の奥から無数の青白い眼光が浮かび上がった。",
        "Suddenly, countless pale blue eyes emerge from the depths of the snow mist.",
        "突然，从雪雾深处浮现出无数苍白的眼眸。",
        actor=narrator,
    ).say(
        "narr_11",
        "それは、体躯が氷の結晶で形成された、異形の魔犬の群れ。彼らが喉を鳴らすたび、大気が結晶化して地面に降り注ぐ。",
        "A pack of grotesque demon hounds, their bodies formed of ice crystals. Each time they growl, the air crystallizes and falls to the ground.",
        "那是一群身躯由冰晶构成的异形魔犬。每当它们低吼，空气就会结晶化并洒落在地上。",
        actor=narrator,
    ).start_battle_by_stage("rank_f_trial", master_id="sukutsu_arena_master").finish()


def add_rank_up_F_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    Rank F 昇格試合の勝利/敗北ステップを arena_master ビルダーに追加する

    Args:
        builder: arena_master の ArenaDramaBuilder インスタンス
        victory_label: 勝利ステップのラベル名
        defeat_label: 敗北ステップのラベル名
        return_label: 結果表示後にジャンプするラベル名
    """
    pc = Actors.PC
    lily = Actors.LILY
    balgas = Actors.BALGAS
    narrator = Actors.NARRATOR

    # ========================================
    # Rank F 昇格試合 勝利
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Lobby_Normal"
    ).say(
        "rf_narr_v1",
        "砕け散った氷の破片が砂に混じり、冷気がゆっくりと霧散していく。",
        "Shattered ice fragments mix with the sand as the cold slowly dissipates.",
        "碎裂的冰片与沙土混杂，寒气缓缓消散。",
        actor=narrator,
    ).say(
        "rf_narr_v2",
        "体温を奪われ、這うようにしてロビーに戻ったあなたを、バルガスが力強く、しかし乱暴に迎える。",
        "Drained of warmth, you crawl back to the lobby where Vargus greets you with powerful yet rough acknowledgment.",
        "被夺走体温、几乎是爬着回到大厅的你，被巴尔加斯有力而粗暴地迎接。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "rf_balgas_v1",
        "……ハッ！ 泥を啜ってでも生き残ったか。",
        "...Hah! Survived by eatin' dirt, did ya?",
        "……哈！就算吃泥也要活下来吗。",
        actor=balgas,
    ).say(
        "rf_narr_v3",
        "彼はあなたの肩を乱暴に叩く。その手は、熱を帯びている。",
        "He claps your shoulder roughly. His hand carries warmth.",
        "他粗暴地拍打你的肩膀。他的手带着热度。",
        actor=narrator,
    ).say(
        "rf_balgas_v2",
        "いいぜ、その執念深さ。今の無様な姿こそ、このアリーナに相応しい。",
        "Good, that tenacity. That sorry state of yours... that's what this arena's all about.",
        "不错，就是这股执着劲。你小子现在这副狼狈样，才配得上这个角斗场。",
        actor=balgas,
    ).say(
        "rf_balgas_v3",
        "これで『屑肉』は卒業だ。今日からお前はランクF……泥にまみれても食らいつく『泥犬』だ。",
        "You're done bein' 'scrap meat.' From today, you're Rank F... a 'Mud Hound' who bites back even covered in filth.",
        "『碎肉』毕业了。从今天起你小子就是F级……就算满身泥泞也要咬住不放的『泥犬』。",
        actor=balgas,
    ).say(
        "rf_balgas_v4",
        "……まあ、悪くねえ。次も、その泥臭さを忘れるんじゃねえぞ。",
        "...Not bad. Don't forget that grit next time either.",
        "……还不赖。下次也别忘了这股土味儿。",
        actor=balgas,
    ).focus_chara(Actors.LILY).say(
        "rf_lily_v1",
        "おめでとうございます。死体袋は、また次回まで取っておきましょう。",
        "Congratulations. I shall keep the body bag for next time.",
        "恭喜您。尸袋就留到下次再用吧。",
        actor=lily,
    ).say(
        "rf_lily_v2",
        "この称号『泥犬』は、あなたがどれほど理不尽な環境でも生き延びる『害虫』のような生命力を持っている証です。……ふふ、褒めているのですよ？",
        "This title, 'Mud Hound,' is proof that you possess the vitality of a 'pest' that survives even the most unreasonable conditions. ...Hehe, I am complimenting you, you know?",
        "这个称号『泥犬』，证明您拥有能在多么不合理的环境中生存下来的、如同『害虫』般的生命力。……呵呵，我是在夸您呢？",
        actor=lily,
    ).complete_quest(QuestIds.RANK_UP_F).grant_rank_reward(
        "F", actor=lily
    ).change_journal_phase("sukutsu_arena", 3).finish()

    # ========================================
    # Rank F 昇格試合 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).focus_chara(
        Actors.LILY
    ).say(
        "rf_lily_d1",
        "あらあら……。凍死は、思ったより早かったですね。",
        "Oh my, my... Death by freezing came sooner than expected.",
        "哎呀呀……冻死比想象中来得更快呢。",
        actor=lily,
    ).say(
        "rf_lily_d2",
        "死体袋の用意が無駄にならなくて何よりです。……次の方、どうぞ。",
        "I am glad the body bag did not go to waste. ...Next challenger, please.",
        "尸袋没有白准备，真是太好了。……下一位，请。",
        actor=lily,
    ).finish()
