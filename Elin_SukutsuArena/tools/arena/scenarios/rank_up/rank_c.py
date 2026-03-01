# -*- coding: utf-8 -*-
"""
11_rank_up_C.md - Rank C 昇格試合『朱砂食い』
英雄の残党との戦い
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, Keys, QuestIds, Rank, SessionKeys


def define_rank_up_C(builder: DramaBuilder):
    """
    Rank C 昇格試合「朱砂食い」
    シナリオ: 11_rank_up_C.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_heroes")
    choice1 = builder.label("choice1")
    react1_who = builder.label("react1_who")
    react1_strong = builder.label("react1_strong")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_balgas_grief")
    choice2 = builder.label("choice2")
    react2_mercy = builder.label("react2_mercy")
    react2_necessary = builder.label("react2_necessary")
    react2_nod = builder.label("react2_nod")
    battle_start = builder.label("battle_start")

    # ========================================
    # シーン1: 堕ちた英雄たち
    # ========================================
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/Ominous_Suspense_01"
    ).say(
        "narr_1",
        "ロビーの空気が重い。",
        "The air in the lobby feels heavy.",
        "大厅的气氛很沉重。",
        actor=narrator,
    ).say(
        "narr_2",
        "バルガスは珍しく酒瓶を手にせず、険しい表情で闘技場の門を見つめている。",
        "Vargus stands without his usual bottle, his stern gaze fixed on the arena gates.",
        "巴尔加斯罕见地没有拿着酒瓶，用严峻的表情注视着角斗场的大门。",
        actor=narrator,
    ).say(
        "narr_3",
        "リリィも、いつもの妖艶な笑みを消し、羊皮紙を静かに整理していた。",
        "Even Lily has set aside her alluring smile, quietly organizing parchments.",
        "莉莉也收起了平日妖艳的笑容，静静地整理着羊皮纸。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.BALGAS).say(
        "balgas_1",
        "……おい、銅貨稼ぎ。\n次の試験は……俺にとっても、お前にとっても、辛いもんになる。",
        "...Hey, Copper-Scrounger.\nThis next trial... it's gonna be rough. For you, and for me.",
        "……喂，铜币赚取者。\n下一场考验……无论对老子还是对你小子，都会很难受。",
        actor=balgas,
    ).say(
        "balgas_3",
        "対戦相手は『堕ちた英雄たちの残党』だ。\n\n……かつて、俺と一緒に地獄を這いずり回った仲間たちだ。カインのように、このアリーナに魂を食い尽くされ、ただの戦闘人形と化した連中だ。",
        "Your opponents are the 'Remnants of the Fallen Heroes.'\n\n...They were my comrades. We crawled through hell together. Like Cain, this arena devoured their souls. Now they're just... fighting puppets.",
        "对手是『堕落英雄的残党』。\n\n……曾经和老子一起在地狱里爬行的伙伴们。和凯恩一样，灵魂被这个角斗场吞噬，变成了单纯的战斗人偶。",
        actor=balgas,
    ).say(
        "narr_4",
        "彼は拳を握りしめる。",
        "He clenches his fist.",
        "他握紧拳头。",
        actor=narrator,
    ).say(
        "balgas_5a",
        "あいつらは、もう『人間』じゃねえ。だが……俺にとっては、今でも仲間だ。\n\nお前が戦うのは、そんな連中だ。",
        "They ain't 'human' anymore. But... to me, they're still my comrades.\n\nThat's who you'll be fighting.",
        "他们已经不再是『人类』了。但是……对老子来说，现在仍然是伙伴。\n\n你小子要战斗的就是这样的家伙们。",
        actor=balgas,
    ).say(
        "balgas_5b",
        "あと、解毒の準備はしておけ。連中の一人は毒使いだ。",
        "One more thing - bring antidotes. One of 'em's a poison user.",
        "还有，准备好解毒药。那群家伙里有个用毒的。",
        actor=balgas,
    )

    # プレイヤーの選択肢1
    builder.choice(
        react1_who,
        "どんな相手だ？",
        "What kind of opponents?",
        "是什么样的对手？",
        text_id="c1_who",
    ).choice(
        react1_strong, "強いのか？", "Are they strong?", "很强吗？", text_id="c1_strong"
    ).choice(
        react1_silent,
        "（無言で聞く）",
        "(Listen in silence)",
        "（默默倾听）",
        text_id="c1_silent",
    )

    # 選択肢反応1
    builder.step(react1_who).say(
        "balgas_r1",
        "……剣士、弓使い、魔導師。俺たちが誇った『黄金のトライアングル』だ。",
        "...A swordsman, an archer, a mage. Our 'Golden Triangle' -- our pride and joy.",
        "……剑士、弓手、魔导师。是我们引以为傲的『黄金三角』。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_strong).say(
        "balgas_r2",
        "ああ。カインよりも、もっと強い。……だが、お前なら勝てる。",
        "Yeah. Stronger than Cain, even. ...But you can beat 'em.",
        "嗯。比凯恩还要强。……但你小子能赢。",
        actor=balgas,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "balgas_r3",
        "……まあ、黙って聞いててくれ。続きがある。",
        "...Hah, just keep listenin'. There's more.",
        "……算了，默默听着吧。还有下文。",
        actor=balgas,
    ).jump(scene2)

    # ========================================
    # シーン2: バルガスの悲痛な願い
    # ========================================
    builder.step(scene2).play_bgm("BGM/Emotional_Sorrow").say(
        "narr_5",
        "バルガスは深く息を吐く。",
        "Vargus exhales deeply.",
        "巴尔加斯深深地叹了口气。",
        actor=narrator,
    ).say(
        "balgas_7",
        "……頼む。あいつらを、この地獄から解放してやってくれ。",
        "...Please. Set them free from this hell.",
        "……拜托了。让那些家伙从这个地狱中解脱吧。",
        actor=balgas,
    ).say(
        "balgas_8",
        "俺には……もう、仲間を救う力がねえ。だが、お前なら……お前ならできる。",
        "I don't... I don't have the strength to save my comrades anymore. But you... you can do it.",
        "老子已经……没有力量拯救伙伴了。但你小子……你小子能做到。",
        actor=balgas,
    ).say(
        "narr_6",
        "リリィが近づいてくる。",
        "Lily approaches.",
        "莉莉走过来。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_1",
        "……お客様。\n彼らは『英雄』でした。ですが、今は……ただの『記憶』です。",
        "...Dear guest.\nThey were once 'heroes.' But now... they are merely 'echoes.'",
        "……客人。\n他们曾经是『英雄』。但现在……只是『残影』罢了。",
        actor=lily,
    ).say(
        "lily_3a",
        "あなたがどれほど優しく戦っても、彼らに意識は戻りません。\n\n……ですが、倒すことで、彼らの魂はこの牢獄から解放されます。",
        "No matter how gently you fight, their consciousness shall never return.\n\n...However, by defeating them, their souls shall be released from this prison.",
        "无论您多么温柔地战斗，他们的意识都不会回来。\n\n……但是，通过击败他们，他们的灵魂将从这个牢笼中解放。",
        actor=lily,
    ).say(
        "lily_3b",
        "これは、慈悲の戦いです。……準備はよろしいですか？",
        "This is a battle of mercy. ...Are you prepared?",
        "这是慈悲之战。……您准备好了吗？",
        actor=lily,
    )

    # プレイヤーの選択肢2
    builder.choice(
        react2_mercy,
        "……慈悲か。分かった",
        "...Mercy, huh. Understood.",
        "……慈悲吗。明白了",
        text_id="c2_mercy",
    ).choice(
        react2_necessary,
        "必要なことなら、やる",
        "If it must be done, I'll do it.",
        "如果是必要的事，我会做",
        text_id="c2_necessary",
    ).choice(
        react2_nod, "（無言で頷く）", "(Nod silently)", "（默默点头）", text_id="c2_nod"
    )

    # 選択肢反応2
    builder.step(react2_mercy).say(
        "lily_r4",
        "……ええ。あなたなら、きっと。",
        "...Yes. I believe you can, without a doubt.",
        "……是的。您一定可以的。",
        actor=lily,
    ).jump(battle_start)

    builder.step(react2_necessary).say(
        "lily_r5",
        "……強い方ですね。では、お任せします。",
        "...How resolute you are. Then, I shall leave it to you.",
        "……您真是坚强呢。那么，就交给您了。",
        actor=lily,
    ).jump(battle_start)

    builder.step(react2_nod).say(
        "lily_r6",
        "……無口ですが、覚悟は決まったようですね。",
        "...The silent type, yet your resolve is clear.",
        "……虽然沉默，但似乎已下定决心了呢。",
        actor=lily,
    ).jump(battle_start)

    # ========================================
    # シーン3: 戦闘開始
    # ========================================
    builder.step(battle_start).play_bgm("BGM/Battle_RankC_Heroic_Lament").say(
        "narr_7",
        "闘技場の門を潜ると、砂地の中央に三つの影が立っていた。",
        "Passing through the arena gates, three shadows stand in the center of the sand.",
        "穿过角斗场大门，三个身影站在沙地中央。",
        actor=narrator,
    ).say(
        "narr_8",
        "錆びついた鎧を纏う剣士、ボロボロのローブを羽織る魔導師、折れた弓を持つ弓使い。",
        "A swordsman in rusted armor, a mage in tattered robes, an archer with a broken bow.",
        "身穿锈迹斑斑铠甲的剑士、披着破烂长袍的魔导师、手持断弓的弓手。",
        actor=narrator,
    ).say(
        "narr_9",
        "彼らの瞳には光がなく、ただ機械的に武器を構えている。",
        "Their eyes hold no light; they raise their weapons mechanically.",
        "他们的眼中没有光芒，只是机械地举起武器。",
        actor=narrator,
    ).say(
        "narr_10",
        "だが、その動きには……かつての『誇り』の残滓が、微かに残っているように見えた。",
        "Yet in their movements... a faint trace of their former 'pride' seems to linger.",
        "但在他们的动作中……似乎隐约残留着曾经『骄傲』的痕迹。",
        actor=narrator,
    ).shake().say(
        "narr_11",
        "バルガスの声が、遠くから聞こえる。",
        "Vargus's voice echoes from afar.",
        "巴尔加斯的声音从远处传来。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "balgas_echo",
        "……頼んだぞ。",
        "...I'm countin' on ya.",
        "……拜托你了。",
        actor=balgas,
    ).start_battle_by_stage("rank_c_trial", master_id="sukutsu_arena_master").finish()


def add_rank_up_C_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    Rank C 昇格試合の勝利/敗北ステップを arena_master ビルダーに追加する

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
    # Rank C 昇格試合 勝利
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Emotional_Sacred_Triumph_Special"
    ).say(
        "rc_narr_v1",
        "最後の英雄が倒れた瞬間、その体が光の粒子となって消えていく。",
        "As the last hero falls, their body dissolves into particles of light.",
        "最后的英雄倒下的瞬间，他们的身体化为光的粒子消散。",
        actor=narrator,
    ).say(
        "rc_narr_v2",
        "彼らの顔に、一瞬だけ……安堵の表情が浮かんだように見えた。",
        "For just a moment... a look of relief seemed to cross their faces.",
        "他们的脸上，似乎有那么一瞬间……浮现出了安心的表情。",
        actor=narrator,
    ).say(
        "rc_narr_v3",
        "静寂の中、ロビーに戻ると、バルガスが背を向けたまま待っている。",
        "Returning to the lobby in silence, Vargus waits with his back turned.",
        "在寂静中回到大厅，巴尔加斯背对着你等待着。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "rc_balgas_v1",
        "……終わったか。",
        "...It's done, then.",
        "……结束了吗。",
        actor=balgas,
    ).say(
        "rc_narr_v4",
        "彼はゆっくりと振り返る。その目には涙の跡。",
        "He turns slowly. Traces of tears line his eyes.",
        "他缓缓转过身。眼角留有泪痕。",
        actor=narrator,
    ).say("rc_balgas_v2", "……ありがよ。", "...Thanks.", "……谢了。", actor=balgas).say(
        "rc_balgas_v3",
        "あいつらは、お前のおかげでようやく……この地獄から出られた。",
        "Because of you, they're finally... finally free from this hell.",
        "多亏了你小子，那些家伙终于……从这个地狱中解脱了。",
        actor=balgas,
    ).say(
        "rc_balgas_v4",
        "お前は今、ただの『銅貨稼ぎ』じゃねえ。",
        "You ain't just a 'Copper-Scrounger' anymore.",
        "你小子现在不再只是『铜币赚取者』了。",
        actor=balgas,
    ).say(
        "rc_balgas_v5",
        "血まみれの砂を喰らいながら這い上がってきた……『朱砂食い』だ。",
        "You clawed your way up, swallowing bloody sand... You're a 'Vermillion Devourer' now.",
        "吞噬着血淋淋的砂砾爬上来的……『朱砂吞噬者』。",
        actor=balgas,
    ).say(
        "rc_narr_v5",
        "リリィが近づいてくる。",
        "Lily approaches.",
        "莉莉走过来。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "rc_lily_v1",
        "……素晴らしい戦いでした。",
        "...That was a magnificent battle.",
        "……真是精彩的战斗。",
        actor=lily,
    ).say(
        "rc_lily_v2",
        "観客の皆様も、あなたの『慈悲』に感動されていたようです。",
        "The audience was moved by your 'mercy,' it seems.",
        "观众们似乎也被您的『慈悲』所感动呢。",
        actor=lily,
    ).say(
        "rc_lily_v3",
        "では、報酬の授与です。",
        "Now then, your reward awaits.",
        "那么，请接受您的报酬。",
        actor=lily,
    ).complete_quest(QuestIds.RANK_UP_C).grant_rank_reward(
        "C", actor=lily
    ).change_journal_phase("sukutsu_arena", 6).finish()

    # ========================================
    # Rank C 昇格試合 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Lobby_Normal"
    ).focus_chara(Actors.BALGAS).say(
        "rc_balgas_d1", "……チッ。", "...Tch.", "……啧。", actor=balgas
    ).say(
        "rc_balgas_d2",
        "まだ、お前には早かったか。",
        "Guess you weren't ready yet.",
        "看来你小子还太嫩了吗。",
        actor=balgas,
    ).say(
        "rc_balgas_d3",
        "……準備が整ったら、また来い。あいつらは、待ってる。",
        "...Come back when you're prepared. They'll be waiting.",
        "……准备好了再来。那些家伙会等着的。",
        actor=balgas,
    ).finish()
