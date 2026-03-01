# -*- coding: utf-8 -*-
"""
06_1_rank_up_03.md - Rank E 昇格試合『鉄屑の慟哭』
カインとの戦い
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, Keys, QuestIds, Rank, SessionKeys


def define_rank_up_E(builder: DramaBuilder):
    """
    Rank E 昇格試合「鉄屑の慟哭」
    シナリオ: 06_1_rank_up_03.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_night")
    choice1 = builder.label("choice1")
    react1_hard = builder.label("react1_hard")
    react1_kain = builder.label("react1_kain")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_declaration")
    choice2 = builder.label("choice2")
    react2_accept = builder.label("react2_accept")
    react2_saved = builder.label("react2_saved")
    react2_nod = builder.label("react2_nod")
    battle_start = builder.label("battle_start")

    # ========================================
    # シーン1: 酒の切れた夜に
    # ========================================
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/Lobby_Normal"
    ).say(
        "narr_1",
        "ロビーの喧騒が嘘のように静まり返った深夜。",
        "Late at night, the lobby fell silent as if the usual clamor had been a lie.",
        "深夜，大厅的喧嚣仿佛是谎言一般归于寂静。",
        actor=narrator,
    ).say(
        "narr_2",
        "バルガスはいつもの酒瓶を持たず、代わりに血錆にまみれた「古い戦士の兜」を無造作に眺めていた。",
        "Vargus held no bottle tonight. Instead, he gazed absently at an old warrior's helm, caked with blood-rust.",
        "巴尔加斯今晚没有拿着惯常的酒瓶，取而代之的是随意地凝视着一顶锈迹斑斑的『老战士头盔』。",
        actor=narrator,
    ).say(
        "narr_3",
        "彼がこれほどまでに静かなのは、あなたがここへ来てから初めてのことだった。",
        "You had never seen him this quiet since you first arrived.",
        "自你来到这里以来，从未见过他如此沉默。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_1",
        "……来たか。",
        "...So you came.",
        "……来了啊。",
        actor=balgas,
    ).say(
        "narr_4",
        "彼は兜を撫でながら、低く掠れた声で話し始める。",
        "He stroked the helm and began to speak in a low, raspy voice.",
        "他抚摸着头盔，用低沉沙哑的声音开始说道。",
        actor=narrator,
    ).say(
        "balgas_2a",
        "おい、新入り。お前は『鉄』がなぜ錆びるか知ってるか？手入れを怠るからじゃねえ。……持ち主の心が折れた時、鉄も一緒に死ぬんだよ。",
        "Hey, rookie. Know why iron rusts? It ain't from neglect. ...When the owner's spirit breaks, the iron dies with 'em.",
        "喂，新人。你小子知道『铁』为什么会生锈吗？不是因为疏于保养。……是主人的心折断的时候，铁也会跟着一起死。",
        actor=balgas,
    ).say(
        "balgas_2b",
        "かつて俺と一緒に地獄を這いずり回った相棒がいた。名はカイン。俺たちは『鉄の意志』を掲げてこのアリーナに挑んだが……あいつは、グランドマスターの影に触れ、魂をこの異次元の『錆』に食い尽くされた。",
        "I had a partner once. We crawled through hell together. Name was Cain. We came to this arena with an 'iron will'... but he touched the Grandmaster's shadow, and the 'rust' of this dimension devoured his soul.",
        "老子曾经有个跟我一起在地狱里爬行的搭档。名叫凯恩。我们带着『钢铁意志』挑战这个角斗场……但那家伙触碰了大宗师的阴影，灵魂被这个异次元的『锈』给吞噬殆尽了。",
        actor=balgas,
    ).say(
        "narr_5",
        "彼は兜を床に置き、深く息を吐く。",
        "He set the helm on the floor and exhaled deeply.",
        "他把头盔放在地上，深深地叹了口气。",
        actor=narrator,
    ).say(
        "balgas_4",
        "あいつは……強かった。だが、守るべきものを失って、壊れちまった。……俺は、あいつを救えなかった。",
        "He was... strong. But he lost what he was fightin' for, and broke. ...I couldn't save him.",
        "那家伙……很强。但失去了要守护的东西后，就崩溃了。……老子没能救他。",
        actor=balgas,
    )

    # プレイヤーの選択肢
    builder.choice(
        react1_hard,
        "……辛い話だな",
        "...That's a hard story.",
        "……真是沉重的故事",
        text_id="c1_hard",
    ).choice(
        react1_kain,
        "カインは今どうなっている？",
        "What happened to Cain?",
        "凯恩现在怎么样了？",
        text_id="c1_kain",
    ).choice(
        react1_silent,
        "（無言で聞く）",
        "(Listen in silence)",
        "（默默倾听）",
        text_id="c1_silent",
    )

    # 選択肢反応
    builder.step(react1_hard).say(
        "balgas_r1",
        "辛い？ ……ああ、そうだな。だが、それがこのアリーナの現実だ。",
        "Hard? ...Yeah, I suppose. But that's the reality of this arena.",
        "沉重？……嗯，是啊。但这就是这个角斗场的现实。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_kain).say(
        "balgas_r2",
        "……あいつは、この場所の『記憶』になっちまった。お前の次の相手だ。",
        "...He became a 'memory' of this place. He's your next opponent.",
        "……那家伙变成了这个地方的『记忆』。是你小子下一个对手。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "balgas_r3",
        "……まあ、黙って聞いててくれ。続きがある。",
        "...Well, just keep listenin'. There's more.",
        "……算了，默默听着吧。还有下文。",
        actor=balgas,
    ).jump(scene2)

    # ========================================
    # シーン2: 因縁の宣告
    # ========================================
    builder.step(scene2).play_bgm("BGM/Emotional_Sorrow").say(
        "narr_6",
        "バルガスは兜を床に転がすと、あなたを射抜くような鋭い視線で立ち上がった。",
        "Vargus rolled the helm aside and rose, fixing you with a piercing stare.",
        "巴尔加斯把头盔滚到一边，以锐利的目光注视着你站了起来。",
        actor=narrator,
    ).say(
        "balgas_5",
        "次のランクE……『鉄屑』への試験。相手は、そのカインの成れの果てだ。\n心も言葉も持たねえ、アンデッドだ。",
        "Your next trial for Rank E... 'Scrap Iron.' You'll be facin' what's left of Cain.\nAn undead. No heart, no words.",
        "下一个E级……『铁屑』的考验。对手是凯恩的残骸。\n没有心也没有言语的亡灵。",
        actor=balgas,
    ).say(
        "narr_7",
        "彼は拳を握りしめ、わずかに震える声で続ける。",
        "He clenched his fist, his voice trembling slightly as he continued.",
        "他握紧拳头，用微微颤抖的声音继续说道。",
        actor=narrator,
    ).say(
        "balgas_7a",
        "あいつを倒せば、お前は晴れて『鉄屑』だ。だが、もし負ければ……お前もあいつのように、闘技場に取り込まれることになる。",
        "Beat him, and you earn the title 'Scrap Iron.' But if you lose... you'll be absorbed into this arena, just like him.",
        "打倒他的话，你小子就能正式成为『铁屑』了。但如果输了……你也会像他一样，被角斗场吞噬。",
        actor=balgas,
    ).say(
        "balgas_7b",
        "……これは俺の我儘だ。あいつを……あの錆びついた悪夢を、終わらせてやってくれ。",
        "...Look, this is my selfish request. End him... end that rusted nightmare for me.",
        "……听好了，这是老子的私心。帮我了结他……了结那个锈蚀的噩梦。",
        actor=balgas,
    )

    # プレイヤーの選択肢
    builder.choice(
        react2_accept,
        "分かった。任せてくれ",
        "Got it. Leave it to me.",
        "明白了。交给我吧",
        text_id="c2_accept",
    ).choice(
        react2_saved,
        "カインを倒せば、彼は救われるのか？",
        "If I defeat Cain, will he be saved?",
        "打倒凯恩的话，他就能得救吗？",
        text_id="c2_saved",
    ).choice(
        react2_nod,
        "（無言で頷く）",
        "(Nod silently)",
        "（默默点头）",
        text_id="c2_nod",
    )

    # 選択肢反応
    builder.step(react2_accept).say(
        "balgas_r4",
        "……ありがよ。お前なら、やってくれると思ってた。",
        "...Thanks. I knew you'd do it.",
        "……谢了。老子就知道你小子会答应。",
        actor=balgas,
    ).jump(battle_start)

    builder.step(react2_saved).say(
        "balgas_r5",
        "……ああ。この地獄から解放される。それが俺にできる、最後の友情だ。",
        "...Yeah. He'll be freed from this hell. That's the last thing I can do for him as a friend.",
        "……嗯。他会从这个地狱中解脱。这是老子作为朋友能做的最后一件事了。",
        actor=balgas,
    ).jump(battle_start)

    builder.step(react2_nod).say(
        "balgas_r6",
        "……頼んだぜ。",
        "...I'm countin' on ya.",
        "……拜托你了。",
        actor=balgas,
    ).jump(battle_start)

    # ========================================
    # シーン3: 戦闘開始
    # ========================================
    builder.step(battle_start).play_bgm("BGM/Battle_Kain_Requiem").say(
        "narr_8",
        "闘技場の門を潜ると、冷たく不吉な風が吹いた。",
        "As you passed through the arena gates, a cold, ominous wind blew.",
        "穿过角斗场大门的瞬间，一阵冰冷不祥的风吹来。",
        actor=narrator,
    ).shake().say(
        "narr_9",
        "中央に立つのは、全身から赤黒い錆を滴らせ、顔のない兜から青い炎を揺らめかせる一人の戦士。",
        "In the center stood a warrior, dripping dark-red rust, blue flames flickering from a faceless helm.",
        "中央站立着一名战士，全身滴落着暗红色的锈迹，无面头盔中摇曳着蓝色的火焰。",
        actor=narrator,
    ).say(
        "narr_10",
        "戦士は巨大な錆びた剣を地に突き立て、ゆっくりと頭を上げる。",
        "The warrior drove a massive rusted blade into the ground and slowly raised his head.",
        "战士将巨大的锈剑插入地面，缓缓抬起头。",
        actor=narrator,
    ).shake().start_battle_by_stage(
        "rank_e_trial", master_id="sukutsu_arena_master"
    ).finish()


def add_rank_up_E_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    Rank E 昇格試合の勝利/敗北ステップを arena_master ビルダーに追加する

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
    # Rank E 昇格試合 勝利
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Emotional_Sorrow_2"
    ).say(
        "re_narr_v1",
        "カインの体が粒子となって崩れ去る瞬間、戦士は一瞬だけバルガスの方向を見つめ、静かに首を振ったように見えた。",
        "As Cain's body crumbled into particles, the warrior seemed to glance toward Vargus and quietly shake his head.",
        "凯恩的身体化为粒子消散的瞬间，战士似乎看了巴尔加斯一眼，轻轻摇了摇头。",
        actor=narrator,
    ).say(
        "re_narr_v2",
        "静寂の中、ロビーに戻ると、バルガスが背を向けたまま待っている。",
        "Returning to the lobby in silence, you found Vargus waiting with his back turned.",
        "在寂静中回到大厅，巴尔加斯背对着你等待着。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "re_balgas_v1",
        "……終わったか。",
        "...It's over, then.",
        "……结束了吗。",
        actor=balgas,
    ).say(
        "re_narr_v3",
        "彼はゆっくりと振り返る。その目には、涙の跡。",
        "He slowly turned around. Tear tracks marked his eyes.",
        "他缓缓转过身。眼角留有泪痕。",
        actor=narrator,
    ).say(
        "re_balgas_v2",
        "あの野郎、最期まで意地っ張りな面をしてやがったな。",
        "That stubborn bastard... kept his pride right to the end.",
        "那家伙，到最后都还是那副倔强的嘴脸啊。",
        actor=balgas,
    ).say(
        "re_narr_v4",
        "彼は拳でこっそりと目を拭う。",
        "He quietly wiped his eyes with his fist.",
        "他偷偷用拳头擦了擦眼睛。",
        actor=narrator,
    ).say(
        "re_balgas_v3",
        "……ありがよ。今日からお前は、ただの『泥犬』じゃねえ。何度叩かれても折れねえ、鈍く輝く『鉄屑』だ。",
        "...Thanks. From today, you ain't just some 'Mud Dog.' You're 'Scrap Iron' now - battered but never broken, shinin' dull.",
        "……谢了。从今天起你小子不再只是『泥犬』了。是怎么打都不会折断、发出暗淡光芒的『铁屑』。",
        actor=balgas,
    ).say(
        "re_balgas_v4",
        "……お前は、カインが持っていた以上の、本物の『鋼の心』を持った戦士だ。",
        "...You've got a true 'heart of steel' - stronger than Cain ever had.",
        "……你小子是拥有比凯恩更真实的『钢铁之心』的战士。",
        actor=balgas,
    ).focus_chara(Actors.LILY).say(
        "re_lily_v1",
        "お疲れ様でした。カインさんの魂の一部……回収いたしました。",
        "Well done. I have collected a fragment of Cain's soul.",
        "辛苦了。凯恩先生的灵魂碎片……已经回收了。",
        actor=lily,
    ).say(
        "re_lily_v2",
        "バルガスさんが珍しく涙ぐんでいたのは見なかったことにしてあげましょうか。さて、報酬の授与をさせていただきます。",
        "Hehe, shall we pretend we didn't see Vargus getting misty-eyed? Now then, allow me to present your reward.",
        "呵呵，巴尔加斯先生难得流泪的事情，就当没看到吧。那么，请允许我颁发报酬。",
        actor=lily,
    ).complete_quest(QuestIds.RANK_UP_E).grant_rank_reward(
        "E", actor=lily
    ).change_journal_phase("sukutsu_arena", 4).finish()

    # ========================================
    # Rank E 昇格試合 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).focus_chara(
        Actors.BALGAS
    ).say(
        "re_balgas_d1",
        "……チッ。終わったか。",
        "...Tch. It's over.",
        "……啧。结束了吗。",
        actor=balgas,
    ).say(
        "re_balgas_d2",
        "お前も、あいつと同じ錆の一部になっちまったか。",
        "So you've become part of the rust too, just like him.",
        "你小子也和他一样，变成锈迹的一部分了吗。",
        actor=balgas,
    ).finish()
