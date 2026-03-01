# -*- coding: utf-8 -*-
"""
10_rank_up_D.md - Rank D 昇格試合『観客の代弁者』
観客の介入が本格化する戦い - グリードとの対決
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, Keys, QuestIds, Rank, SessionKeys


def define_rank_up_D(builder: DramaBuilder):
    """
    Rank D 昇格試合「観客の代弁者」
    シナリオ: 10_rank_up_D.md - グリードとの対決
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_announcement")
    choice1 = builder.label("choice1")
    react1_audience = builder.label("react1_audience")
    react1_items = builder.label("react1_items")
    react1_silent = builder.label("react1_silent")
    scene2 = builder.label("scene2_balgas_warning")
    choice2 = builder.label("choice2")
    react2_dodge = builder.label("react2_dodge")
    react2_use = builder.label("react2_use")
    react2_nod = builder.label("react2_nod")
    battle_start = builder.label("battle_start")

    # ========================================
    # シーン1: リリィの宣告
    # ========================================
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/Lobby_Normal"
    ).say(
        "narr_1",
        "ロビーに戻ったあなたを、リリィが妖艶な笑みで迎える。",
        "As you return to the lobby, Lily greets you with a bewitching smile.",
        "回到大厅的你，被莉莉以妖艳的微笑迎接。",
        actor=narrator,
    ).say(
        "narr_2",
        "彼女の台帳には、あなたの戦績と共に、新たな「試練」の記録が追加されている。",
        "In her ledger, alongside your battle record, a new 'trial' has been added.",
        "她的账簿上，连同你的战绩一起，又添加了一条新的『试炼』记录。",
        actor=narrator,
    ).jump(scene1)

    builder.step(scene1).focus_chara(Actors.LILY).say(
        "lily_1",
        "……お疲れ様でした。カインさんの魂を巡る選択、興味深く拝見させていただきました。\nさて、次はRank D『銅貨稼ぎ』への昇格試合です。",
        "...Well done. Your choice regarding Cain's soul was quite fascinating to observe.\nNow then, your next match is the promotion trial to Rank D, 'Copper Earner.'",
        "……辛苦了。关于凯恩先生灵魂的选择，我饶有兴致地拜见了呢。\n那么，接下来是D级『铜币赚取者』的晋级赛。",
        actor=lily,
    ).say(
        "lily_3",
        "ここからは、ただ敵を倒すだけでは不十分。観客の皆様を『満足』させる必要があります。\n\n彼らが退屈すれば、あなたに『プレゼント』が投げ込まれます。",
        "From here on, merely defeating enemies won't suffice. You must 'satisfy' our dear audience.\n\nShould they grow bored, they shall shower you with 'gifts.'",
        "从这里开始，仅仅打倒敌人是不够的。您需要让观众们『满意』。\n\n如果他们感到无聊，就会向您投掷『礼物』呢。",
        actor=lily,
    ).say(
        "narr_3",
        "彼女は指を鳴らす。すると、ロビーの天井から突如、石塊が落下してきた。",
        "She snaps her fingers. Suddenly, a boulder crashes down from the lobby ceiling.",
        "她打了个响指。随即，一块巨石从大厅天花板上落下。",
        actor=narrator,
    ).say(
        "lily_5a",
        "……ふふ、驚きました？ これが『観客の介入』です。\n\n戦闘中、高次元から爆風とともに様々な物が投げ込まれます。石、薬、武器……次元を超えた衝撃も伴います。",
        "...Hehe, startled? This is what we call 'audience intervention.'\n\nDuring combat, various objects are hurled from higher dimensions along with explosions. Stones, potions, weapons... accompanied by dimensional shockwaves.",
        "……呵呵，吓到了吗？这就是『观众干预』。\n\n战斗中，各种物品会伴随着爆炸从高次元投掷下来。石头、药剂、武器……还伴随着跨次元的冲击。",
        actor=lily,
    ).say(
        "lily_5b",
        "それらを避けながら、あるいは利用しながら戦う……それがRank Dの『芸』ですよ。",
        "Fighting while evading or utilizing these... that is the 'art' of Rank D.",
        "一边躲避或利用它们一边战斗……这就是D级的『技艺』呢。",
        actor=lily,
    )

    # プレイヤーの選択肢1
    builder.choice(
        react1_audience,
        "観客を止められないのか？",
        "Can't you stop the audience?",
        "不能阻止观众吗？",
        text_id="c1_audience",
    ).choice(
        react1_items,
        "どんな物が降ってくる？",
        "What kind of things will fall?",
        "会掉下什么东西？",
        text_id="c1_items",
    ).choice(
        react1_silent,
        "（無言で聞く）",
        "(Listen in silence)",
        "（默默倾听）",
        text_id="c1_silent",
    )

    # 選択肢反応1
    builder.step(react1_audience).say(
        "lily_r1",
        "無理です。観客は次元の外側から私たちを見ている存在。手が届きません。……諦めて、楽しませてあげてください。",
        "Impossible. The audience exists beyond our dimension, watching us from the outside. They are beyond our reach. ...Do give up and entertain them, won't you?",
        "不可能呢。观众是从次元外部观看我们的存在。我们无法触及。……请放弃吧，好好取悦他们呢。",
        actor=lily,
    ).jump(scene2)

    builder.step(react1_items).say(
        "lily_r2",
        "何が来るかは分かりません。ポーションが爆風とともに飛んでくることもあれば、鉄の塊が直撃することも。……運を祈ってくださいね？",
        "One never knows what may come. Sometimes potions arrive with explosive force, other times a chunk of iron strikes directly. ...Do pray for good fortune, yes?",
        "谁知道会来什么呢。有时药剂会伴随爆炸飞来，有时铁块会直接砸中。……请祈祷好运吧？",
        actor=lily,
    ).jump(scene2)

    builder.step(react1_silent).say(
        "lily_r3",
        "……無口ですが、理解はされたようですね。では、バルガスさんからも一言。",
        "...Quiet, but you seem to understand. Now then, a word from Vargus as well.",
        "……虽然沉默，但似乎理解了呢。那么，巴尔加斯先生也说几句吧。",
        actor=lily,
    ).jump(scene2)

    # ========================================
    # シーン2: バルガスの実践的助言
    # ========================================
    builder.step(scene2).play_bgm("BGM/Ominous_Suspense_01").focus_chara(
        Actors.BALGAS
    ).say(
        "narr_4",
        "バルガスが近づいてくる。その表情はいつになく真剣だ。",
        "Vargus approaches. His expression is unusually serious.",
        "巴尔加斯走过来。他的表情异常严肃。",
        actor=narrator,
    ).say(
        "balgas_1",
        "……おい、銅貨稼ぎ予備軍。\n観客のヤジは、ただの嫌がらせじゃねえ。戦況を一変させる『変数』だ。",
        "...Oi, copper earner wannabe.\nThe audience's heckling ain't just harassment. It's a 'variable' that can flip the whole battle.",
        "……喂，铜币赚取者预备军。\n观众的起哄不只是骚扰。是能让战局瞬间逆转的『变数』。",
        actor=balgas,
    ).say(
        "balgas_3",
        "石が降ってきたら、敵に当たるように誘導しろ。薬が降ってきたら、素早く拾って飲め。\n爆発物が降ってきたら……全力で逃げろ。",
        "When rocks fall, lure 'em so they hit your enemy. When potions drop, grab and drink 'em fast.\nWhen explosives come down... run like hell.",
        "石头掉下来的话，诱导它砸向敌人。药剂掉下来的话，迅速捡起来喝掉。\n爆炸物掉下来的话……全力逃跑。",
        actor=balgas,
    ).say(
        "balgas_5a",
        "大事なのは、『動き続ける』ことだ。止まれば的になる。\n\nそれと……観客を楽しませることも忘れるな。派手に戦えば、即死するような物は投げてこないからな。",
        "What matters is 'keep movin'.' Stop and you're a sittin' duck.\n\nAnd don't forget... entertain the crowd. Fight flashy enough, and they won't throw anything that'll kill ya instantly.",
        "重要的是『一直移动』。停下来就变成活靶子了。\n\n还有……别忘了取悦观众。打得够精彩的话，他们就不会扔能让你小子当场毙命的东西。",
        actor=balgas,
    ).say(
        "balgas_5b",
        "……死ぬなよ。行ってこい。",
        "...Don't die. Now get goin'.",
        "……别死了。去吧。",
        actor=balgas,
    )

    # プレイヤーの選択肢2
    builder.choice(
        react2_dodge,
        "避けることに集中する",
        "I'll focus on dodging",
        "我会专注于躲避",
        text_id="c2_dodge",
    ).choice(
        react2_use,
        "落下物を利用してみせる",
        "I'll make use of the falling objects",
        "我会利用掉落物",
        text_id="c2_use",
    ).choice(
        react2_nod, "（無言で頷く）", "(Nod silently)", "（默默点头）", text_id="c2_nod"
    )

    # 選択肢反応2
    builder.step(react2_dodge).say(
        "balgas_r1",
        "賢明だ。まずは生き残ることが最優先だからな。",
        "Smart. Stayin' alive comes first, after all.",
        "明智。毕竟活下来才是最优先的。",
        actor=balgas,
    ).jump(battle_start)

    builder.step(react2_use).say(
        "balgas_r2",
        "ハッ、強気だな。だが、その意気込みは悪くねえ。",
        "Hah! Gutsy. But I don't hate that spirit.",
        "哈！真有胆量。不过这股劲头不赖。",
        actor=balgas,
    ).jump(battle_start)

    builder.step(react2_nod).say(
        "balgas_r3",
        "……よし。じゃあ行け。死ぬなよ。",
        "...Alright. Now go. Don't die.",
        "……好。那就去吧。别死了。",
        actor=balgas,
    ).jump(battle_start)

    # ========================================
    # シーン3: 戦闘開始
    # ========================================
    builder.step(battle_start).play_bgm("BGM/Battle_RankD_Chaos").say(
        "narr_5",
        "闘技場の門を潜ると、既に観客たちの熱気が空気を震わせている。",
        "Passing through the arena gates, the audience's fervor already sets the air trembling.",
        "穿过角斗场大门，观众们的热情已经让空气都在震颤。",
        actor=narrator,
    ).say(
        "narr_6",
        "砂地の中央に立つのは、ただ一人の男。だが、その眼は虚ろで、口元には不自然な笑みが張り付いている。",
        "Standing alone in the center of the sandy arena is a single man. Yet his eyes are hollow, an unnatural smile frozen on his lips.",
        "站在沙地中央的只有一个男人。但他的眼神空洞，嘴角挂着不自然的笑容。",
        actor=narrator,
    ).say(
        "narr_6b",
        "男の名は『グリード』ーーかつて『朱砂食い』の一員だったが、観客の力に魅せられ、自らその傀儡となった者。",
        "The man's name is 'Greed' -- once a member of the 'Cinnabar Eaters,' he became entranced by the audience's power and willingly became their puppet.",
        "男人名叫『贪婪』--曾是『朱砂吞噬者』的一员，却被观众的力量迷惑，自愿成为了他们的傀儡。",
        actor=narrator,
    ).say(
        "narr_7",
        "リリィの声が、魔術的な拡声によって会場全体に響き渡る。",
        "Lily's voice echoes throughout the venue, amplified by magic.",
        "莉莉的声音通过魔法扩音响彻整个会场。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_ann1",
        "……皆様、本日のメインディッシュです！",
        "...Ladies and gentlemen, today's main course!",
        "……各位，这是今天的主菜！",
        actor=lily,
    ).say(
        "lily_ann2",
        "『観客の代弁者』グリードと、新たな『銅貨稼ぎ』候補の命懸けの舞踏をお楽しみください！",
        "Please enjoy this dance of death between Greed, the 'Voice of the Audience,' and our newest 'Copper Earner' candidate!",
        "请欣赏『观众代言人』贪婪与新晋『铜币赚取者』候选人之间的生死之舞！",
        actor=lily,
    ).say(
        "narr_8",
        "グリードの口から、観客の歓声を模した轟音が響く。戦いが始まった瞬間、頭上の虚空が紫色に光った……！",
        "From Greed's mouth erupts a roar mimicking the audience's cheers. The moment battle begins, the void above glows purple...!",
        "贪婪的口中发出模仿观众欢呼的轰鸣。战斗开始的瞬间，头顶的虚空发出紫色的光芒……！",
        actor=narrator,
    ).shake().start_battle_by_stage(
        "rank_d_trial", master_id="sukutsu_arena_master"
    ).finish()


def add_rank_up_D_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    Rank D 昇格試合の勝利/敗北ステップを arena_master ビルダーに追加する

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
    # Rank D 昇格試合 勝利
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Fanfare_Audience"
    ).say(
        "rd_narr_v1",
        "最後の敵が倒れた瞬間、観客たちの歓声が一気に爆発した。",
        "The moment the last enemy falls, the audience's cheers explode in unison.",
        "最后的敌人倒下的瞬间，观众们的欢呼声一齐爆发。",
        actor=narrator,
    ).say(
        "rd_narr_v2",
        "砂地には、戦闘中に降ってきた無数の物品が散乱している。石塊、割れた薬瓶、曲がった剣……。",
        "Scattered across the sand lie countless objects that fell during the battle. Boulders, shattered potion bottles, bent swords...",
        "沙地上散落着战斗中掉落的无数物品。巨石、破碎的药剂瓶、弯曲的剑……",
        actor=narrator,
    ).say(
        "rd_narr_v3",
        "ロビーに戻ると、リリィが満足げに微笑んでいた。",
        "Upon returning to the lobby, Lily wears a satisfied smile.",
        "回到大厅，莉莉满意地微笑着。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "rd_lily_v1",
        "……素晴らしい。観客の皆様も、大変お喜びでしたよ。",
        "...Splendid. Our dear audience was most pleased.",
        "……太精彩了。观众们也非常高兴呢。",
        actor=lily,
    ).say(
        "rd_lily_v2",
        "落下物を巧みに避け、時には利用する。その立ち回り、まさに『銅貨稼ぎ』の名に相応しい。",
        "Skillfully evading the falling objects, sometimes turning them to your advantage. Such finesse is truly worthy of the 'Copper Earner' title.",
        "巧妙地躲避掉落物，有时还加以利用。这种身手，确实配得上『铜币赚取者』的名号。",
        actor=lily,
    ).focus_chara(Actors.BALGAS).say(
        "rd_narr_v4",
        "バルガスが酒瓶を傾けながら近づいてくる。",
        "Vargus approaches, tilting his liquor bottle.",
        "巴尔加斯一边倾斜着酒瓶一边走过来。",
        actor=narrator,
    ).say(
        "rd_balgas_v1",
        "……やるじゃねえか。",
        "...Not bad at all.",
        "……还不赖嘛。",
        actor=balgas,
    ).say(
        "rd_balgas_v2",
        "お前は今、ただの『鉄屑』から、観客に媚びを売って生き延びる『銅貨稼ぎ』になった。",
        "You've gone from just 'Scrap Iron' to a 'Copper Earner' who survives by playin' to the crowd.",
        "你小子现在从单纯的『铁屑』变成了靠讨好观众生存的『铜币赚取者』。",
        actor=balgas,
    ).say(
        "rd_balgas_v3",
        "誇れることじゃねえが……生き残るためには必要なスキルだ。",
        "Ain't somethin' to be proud of... but it's a skill ya need to survive.",
        "虽然不是什么值得骄傲的事……但这是生存必需的技能。",
        actor=balgas,
    ).focus_chara(Actors.LILY).say(
        "rd_lily_v3",
        "では、報酬の授与です。",
        "Now then, time to bestow your reward.",
        "那么，请接受您的报酬。",
        actor=lily,
    ).complete_quest(QuestIds.RANK_UP_D).grant_rank_reward(
        "D", actor=lily
    ).change_journal_phase("sukutsu_arena", 5).finish()

    # ========================================
    # Rank D 昇格試合 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).play_bgm(
        "BGM/Lobby_Normal"
    ).focus_chara(Actors.LILY).say(
        "rd_lily_d1",
        "……あらあら、落下物に潰されてしまいましたね。",
        "...Oh my, you were crushed by falling debris.",
        "……哎呀呀，被掉落物压扁了呢。",
        actor=lily,
    ).say(
        "rd_lily_d2",
        "観客の皆様も、少し期待外れだったようです。",
        "Our dear audience seemed a tad disappointed.",
        "观众们似乎也有些失望呢。",
        actor=lily,
    ).say(
        "rd_lily_d3",
        "準備が整ったら、また挑戦してください。次はもっと上手く避けられるといいですね。",
        "When you're ready, do try again. Hopefully you'll dodge more gracefully next time.",
        "准备好了请再来挑战。希望下次能躲得更漂亮一些呢。",
        actor=lily,
    ).finish()
