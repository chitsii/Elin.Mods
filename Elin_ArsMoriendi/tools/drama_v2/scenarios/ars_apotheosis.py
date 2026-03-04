from drama.data import BGM, CueKeys, DramaIds, FlagKeys

from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx
from drama_v2.scenarios.ars_stigmata import STIGMATA_MOTIVE_FLAG

KAREN_STAY_FLAG = FlagKeys.KAREN_STAY


def save_apotheosis_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")

    def l(jp: str, en: str, cn: str, actor):
        return d.line(jp, actor=actor, en=en, cn=cn)

    r = ArsModCommands()
    narrator = d.chara("narrator")
    karen = d.chara("ars_karen")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_apotheosis"

    d.node(
        "main",
        r.quest_check("ars_apotheosis", can_start_tmp_flag, actor=None),
        d.fade_out(1.5),
        d.fade_in(1.5),
        r.resolve_run(CueKeys.APOTHEOSIS_SILENCE),
        d.wait(1.0),
        l(
            "素材が消えた。禁書が静かだ。何も起きない。",
            "The materials vanished. The tome is silent. Nothing happens.",
            "素材消失了。禁书安静了。什么都没发生。",
            narrator,
        ),
        d.wait(1.5),
        l(
            "……長い沈黙。\nこの沈黙は、嵐の前の凪ではない。\n嵐そのものだ。",
            "...A long silence.\nThis silence is not the calm before the storm.\nIt is the storm itself.",
            "……漫长的沉默。\n这沉默不是暴风雨前的宁静。\n它本身就是暴风雨。",
            narrator,
        ),
        d.shake(),
        d.sound(d.id("sound", "spell_funnel")),
        d.go("phase_1"),
    )

    d.node(
        "phase_1",
        d.play_bgm_with_fallback(BGM.RITUAL, BGM.REVELATION),
        l(
            "儀式の準備を終えると、禁書から闇が溢れ、凝縮し始めた。",
            "As you complete the ritual preparations, darkness spills from the tome and begins to condense.",
            "仪式准备完成后，禁书中涌出黑暗，开始凝聚。",
            narrator,
        ),
        r.resolve_run(CueKeys.APOTHEOSIS_DARKWOMB),
        d.shake(),
        l(
            "質量を伴う闇は、あなたの身体を包み、未知の言語であなたに話しかける。\nその声は、あなたの骨の一本一本を震わせるようだ。",
            "The darkness, bearing weight, envelops your body and speaks to you in an unknown language.\nIts voice seems to make every bone in your body tremble.",
            "带有质量的黑暗包裹住你的身体，用未知的语言对你说话。\n那声音似乎让你每一根骨头都在颤抖。",
            narrator,
        ),
        l(
            "痛い。——いや。実際には痛くはない。\n存在の形が変わろうとしている。精神的な「成長痛」とでも言うべきだろうか。",
            "Pain. -- No. It doesn't actually hurt.\nThe shape of your existence is trying to change. A spiritual 'growing pain,' perhaps.",
            "痛。——不。实际上并不痛。\n存在的形态正试图改变。也许该称之为精神上的“成长痛”吧。",
            narrator,
        ),
        r.resolve_run(CueKeys.APOTHEOSIS_CURSE_BURST),
        l(
            "やがて、闇の中から、光が見え始めた。ベール状のなにか。二重に。三重に。五重に。\n——五人分の目を通して、同時に見ている。",
            "Before long, light begins to emerge from the darkness. Something veil-like. Double. Triple. Fivefold.\n-- You are seeing through five pairs of eyes at once.",
            "不久，黑暗中开始出现光芒。面纱般的什么。二重。三重。五重。\n——透过五个人的眼睛，同时在看。",
            narrator,
        ),
        d.glitch(),
        d.go("phase_2"),
    )

    d.node(
        "phase_2",
        d.wait(0.5),
        l(
            "あなたは、城壁の上から見下ろしていた。あなたを討伐しにきた騎士の大隊を。",
            "You were looking down from atop the ramparts. At the battalion of knights who came to subjugate you.",
            "你从城墙上俯瞰着。那些前来讨伐你的骑士大队。",
            narrator,
        ),
        l(
            "場面は移ろう。死者の瞼を閉じる手。\n『せめて尊厳を』……その願いの温度が、まだ残っている。",
            "The scene shifts. Hands closing a dead one's eyelids.\n'At least grant them dignity'... The warmth of that wish still lingers.",
            "场景在流转。合上死者眼帘的手。\n'至少给予尊严'……那份心愿的温度，仍然残留着。",
            narrator,
        ),
        l(
            "数式の海。魂の構造を解き明かす純粋な知的歓び。\n\n……自分が誰かを忘れていることへの、微かな不安。",
            "An ocean of equations. The pure intellectual joy of unraveling the structure of souls.\n\n...A faint unease about forgetting who you are.",
            "方程式的海洋。解析灵魂结构的纯粹求知喜悦。\n\n……对于忘记自己是谁这件事，隐约的不安。",
            narrator,
        ),
        l(
            "妻の名を呼ぶ声。それはあなたの声だった。\n\n蘇った妻の空っぽの目。\n……投降、そして安堵。",
            "A voice calling a wife's name. It was your voice.\n\nThe empty eyes of the wife brought back to life.\n...Surrender, and relief.",
            "呼唤妻子名字的声音。那是你的声音。\n\n复活的妻子，空洞的眼睛。\n……投降，然后是释然。",
            narrator,
        ),
        l(
            "そして、あなたは太古へ時を遡るのを感じた。ベールは曖昧になり、やがて、一つのイメージを投影する。",
            "And then, you feel time flowing backward to the ancient past. The veils blur, and at last, project a single image.",
            "然后，你感到时间正向太古回溯。面纱变得模糊，终于投射出一个影像。",
            narrator,
        ),
        d.shake(),
        d.glitch(),
        r.resolve_run(CueKeys.APOTHEOSIS_REVIVE),
        l(
            "……災厄の時代。死屍累々の野。薬草を握りしめた手。\n\n『死に対抗するためには、死を理解しなければ』\n……その切迫が、数万年の時を超えて突き刺さる。",
            "-- The age of calamity. Fields strewn with corpses.\nHands clutching medicinal herbs.\n'To fight death, I must understand death.'\n...That desperation pierces across tens of thousands of years.",
            "——灾厄的时代。尸横遍野。\n紧握药草的手。\n'要对抗死亡，就必须理解死亡。'\n……那份迫切，穿越数万年刺入心中。",
            narrator,
        ),
        r.resolve_run(CueKeys.APOTHEOSIS_DARKWOMB),
        l(
            "諦めたくない。世界中を敵に回しても。",
            "I don't want to give up. Even if it means turning the whole world against me.",
            "不想放弃。即使与全世界为敌。",
            narrator,
        ),
        d.go("phase_3"),
    )

    d.node(
        "phase_3",
        d.play_bgm(BGM.REQUIEM),
        r.resolve_run(CueKeys.APOTHEOSIS_MUTATION),
        d.shake(),
        l(
            "身体が軽くなった。\n\n……いや。あなたの身体は重要でなくなったのだ。\n筋肉が、骨が、皮膚が……意味を失っていく。",
            "Your body feels lighter.\n\n...No. Your body has become irrelevant.\nMuscle, bone, skin -- losing all meaning.",
            "身体变轻了。\n\n……不。是你的身体变得不重要了。\n肌肉、骨骼、皮肤——正在失去意义。",
            narrator,
        ),
        l(
            "代わりに、魂が膨張する。\n\n頭蓋の中で収まりきらないほどの……力。\n世界中のマナが、自分に向かって流れ込んでくるようだ。",
            "In its place, the soul expands.\n\nPower too vast for the skull to contain...\nIt feels as though all the mana in the world flows toward you.",
            "取而代之的是，灵魂在膨胀。\n\n头颅无法容纳的……力量。\n全世界的魔力，仿佛正向你涌来。",
            narrator,
        ),
        l(
            "あなたは感じた。生者の体内を流れる魂の色。\n死者が残した痕跡の匂い。\nそして声……生と死の境界にいる全ての声。",
            "You feel it. The colors of souls flowing within the living.\nThe scent of traces left by the dead.\nAnd voices -- all the voices at the boundary of life and death.",
            "你感受到了。活人体内流动的灵魂的颜色。\n死者留下的痕迹的气味。\n然后是声音——生死之境所有的声音。",
            narrator,
        ),
        l(
            "あなたは自然に悟った。もう人ではない。そして死者でもない。\n\n……自分の内側に、神性が芽生えたのだと。",
            "You understand it naturally. No longer human. Not dead either.\n\n...That within you, divinity has begun to take root.",
            "你自然地领悟了。已不再是人类。也不是死者。\n\n……在自己的内心深处，神性已经萌芽。",
            narrator,
        ),
        r.resolve_run(CueKeys.APOTHEOSIS_TELEPORT_REBIRTH),
        d.go("phase_4"),
    )

    d.node(
        "phase_4",
        l(
            "死霊術の呪文が、意味を超えて理解できる。\n言葉を唱える必要すらない。\n意志が、直接世界を書き換える。",
            "Necromantic incantations are understood beyond meaning.\nNo need to even chant the words.\nYour will directly rewrites the world.",
            "死灵术的咒语，超越了意义层面被理解。\n甚至不需要吟唱。\n意志直接改写世界。",
            narrator,
        ),
        l(
            "ただし……代償がある。身体は脆くなった。\n打たれれば折れる。刃は以前より深く刺さる。\n\n……些末なことだ。もはや。",
            "But... there is a price. Your body has grown fragile.\nA blow will break you. Blades cut deeper than before.\n\n...A trivial matter. Now.",
            "但是……有代价。身体变得脆弱了。\n被打就会折断。刀刃比以前刺得更深。\n\n……无关紧要的事。如今。",
            narrator,
        ),
        l(
            "あなたの従者たちが震えるのが分かる。それは恐怖ではなく、共鳴であり祝詞であった。\n\n主の変容を、魂が感じ取っている。",
            "You sense your servants trembling. Not from fear, but resonance -- a benediction.\n\nTheir souls sense the master's transformation.",
            "你能感觉到从者们在颤抖。那不是恐惧，而是共鸣，是祝词。\n\n灵魂感知到了主人的蜕变。",
            narrator,
        ),
        d.go("phase_5"),
    )

    d.node(
        "phase_5",
        l(
            "禁書の表紙を見る。\n著者名の欄……エレノスの名が消えている。",
            "You look at the tome's cover.\nThe author's name -- Erenos's name has vanished.",
            "看向禁书的封面。\n著者名一栏——艾雷诺斯的名字消失了。",
            narrator,
        ),
        l(
            "明確に。力強く。消えることのない墨で。\n……あなたの名前が刻まれている。",
            "Clearly. Boldly. In ink that will never fade.\n-- Your name is inscribed there.",
            "清晰地。有力地。用永不消褪的墨水。\n——刻着你的名字。",
            narrator,
        ),
        d.switch_on_flag(
            STIGMATA_MOTIVE_FLAG,
            cases=[
                ("==", 1, "ap_4_duty"),
                ("==", 2, "ap_4_direct"),
                ("==", 3, "ap_4_drift"),
                ("==", 0, "ap_4_drift"),
            ],
            actor="pc",
        ),
    )

    d.node(
        "ap_4_duty",
        l(
            "……次の者のために、何か書くべきだ。連鎖を止める方法を。\n...五人分の記憶に呑まれても、その一念は消えない。",
            "...You should write something for the next one. How to stop the cycle. \n-- Even drowning in five lifetimes of memory, that one resolve remains.",
            "……应该为后来者写下什么。断绝连锁的方法。\n……即使被五代人的记忆吞没，那一念仍不熄灭。",
            narrator,
        ),
        d.go("ap_continue"),
    )
    d.node(
        "ap_4_direct",
        l(
            "……次の者のために、何か書くべきだ。\nこの力の使い道を。\nペンを持つ手は、もう迷っていない。",
            "...You should write something for the next one.\nHow to use this power.\nThe hand holding the pen no longer hesitates.",
            "……应该为后来者写下什么。\n这份力量的用法。\n执笔的手已不再犹豫。",
            narrator,
        ),
        d.go("ap_continue"),
    )
    d.node(
        "ap_4_drift",
        l(
            "……次の者のために、何か書くべきだ。\nなぜそう思うのかはわからない。\nだが、書くべきだと確信している。",
            "...You should write something for the next one.\nYou don't know why you think so.\nBut you're certain you should.",
            "……应该为后来者写下什么。\n为何会这样想，不得而知。\n但确信必须写下。",
            narrator,
        ),
        d.go("ap_continue"),
    )

    d.node(
        "ap_continue",
        l(
            "もう問わない。\n区別は消えた。",
            "You no longer ask.\nThe distinction has vanished.",
            "不再追问。\n界限已消失。",
            narrator,
        ),
        d.wait(0.5),
        d.go("karen_scene"),
    )

    d.node(
        "karen_scene",
        d.play_bgm_with_fallback(BGM.OMINOUS, BGM.REVELATION),
        d.wait(1.5),
        l(
            "儀式の余韻が静まる。空気が冷えていく。\n——背後に気配。鎧の軋む音。",
            "The afterglow of the ritual fades. The air grows cold.\n-- A presence behind you. The creak of armor.",
            "仪式的余韵渐渐平息。空气冷了下来。\n——背后有气息。铠甲的吱嘎声。",
            narrator,
        ),
        d.wait(0.5),
        l(
            "お前の跡をつけていた。\n\n……儀式を止めるには\n一足遅かったようだけど。",
            "I've been following your trail.\n\n...It seems I was a step\ntoo late to stop the ritual.",
            "我一直跟着你的足迹。\n\n……看来要阻止仪式\n已经迟了一步。",
            karen,
        ),
        d.wait(0.5),
        l(
            "……終わったのか。始まったのか。もう、わからない。",
            "...Is it over? Or has it begun? I can no longer tell.",
            "……结束了吗？还是刚刚开始？已经分不清了。",
            karen,
        ),
        l(
            "あなたの目は変わった。\nエレノスの最後の日と\n同じ目をしている。",
            "Your eyes have changed.\nThe same eyes Erenos had\non his last day.",
            "你的眼睛变了。\n和艾雷诺斯最后一天\n一样的眼神。",
            karen,
        ),
        l(
            "目だけじゃない。\nあなたの周りの空気が……重い。\n死の匂いがする。",
            "Not just your eyes.\nThe air around you is... heavy.\nIt smells of death.",
            "不只是眼睛。\n你周围的空气……很沉重。\n有死亡的气息。",
            karen,
        ),
        l(
            "でも、それだけでは\n何も証明できない。\n\n目が変わっても、\n行動が変わるとは\n限らないから。",
            "But that alone\nproves nothing.\n\nChanged eyes don't\nnecessarily mean\nchanged actions.",
            "但仅凭这一点\n什么也证明不了。\n\n眼神变了，\n行动未必\n也会变。",
            karen,
        ),
        l(
            "エレノスも、\n最初は善意だった。\n\nあなたも……\nそうだと信じたい。",
            "Erenos, too,\nbegan with good intentions.\n\nI want to believe...\nthat you are the same.",
            "艾雷诺斯也是，\n起初出于善意。\n\n我想相信……\n你也一样。",
            karen,
        ),
        l(
            "六度目のサイクル。\n今度こそ違うと……\nいいのだけれど。",
            "The sixth cycle.\nI hope this time\nwill be different...",
            "第六次循环。\n但愿这次\n会不一样……",
            karen,
        ),
        l(
            "……もう会うことはないかもね。\n\n……もう疲れてしまったの。",
            "...We may never meet again.\n\n...I'm so tired.",
            "……也许不会再见了。\n\n……我已经累了。",
            karen,
        ),
        l("カレンが背を向けた。", "Karen turns away.", "卡伦转过身去。", narrator),
        d.go("karen_choice"),
    )

    d.node(
        "karen_choice",
        d.raw(
            {
                "action": "choice",
                "jump": "karen_choice_react_0",
                "id": "ap_ca",
                "text_JP": "あなたは私の理解者だ。側にいて、監視してはどうかな。",
                "text_EN": "You understand me. Why not stay by my side and keep watch?",
                "text_CN": "你是我的理解者。留在身边监视我，如何？",
            }
        ),
        d.raw(
            {
                "action": "choice",
                "jump": "karen_choice_react_1",
                "id": "ap_cb",
                "text_JP": "……見送る",
                "text_EN": "...Watch her leave.",
                "text_CN": "……目送她离去。",
            }
        ),
    )
    d.node("karen_choice_react_0", d.go("recruit_karen"))
    d.node("karen_choice_react_1", d.go("farewell_karen"))

    d.node(
        "recruit_karen",
        l("カレンが足を止めた。", "Karen stops.", "卡伦停下了脚步。", narrator),
        d.wait(0.3),
        l(
            "……監視、か。都合のいい言い方をする。",
            "...Watch over you. A convenient way to put it.",
            "……监视吗。倒是方便的说法。",
            karen,
        ),
        l(
            "考えてみよう。だが、今は教団を離れて、旅に出ようと思う。\n……あなたのためではなく、私のために。\n\nさようなら。",
            "I'll think about it. But for now, I plan to leave the order and set out on a journey.\n...Not for your sake, but for mine.\n\nFarewell.",
            "让我考虑一下。但现在，我打算离开教团去旅行。\n……不是为了你，而是为了我自己。\n\n再见。",
            karen,
        ),
        d.set_flag(KAREN_STAY_FLAG, 1, actor=None),
        d.go("end_scene"),
    )

    d.node(
        "farewell_karen",
        l(
            "教団を離れて、私を知らない土地へ行こうと思う。\n……あなたのためではなく、私のために。\n\nさようなら。",
            "I plan to leave the order and go somewhere no one knows me.\n...Not for your sake, but for mine.\n\nFarewell.",
            "我打算离开教团，去一个没人认识我的地方。\n……不是为了你，而是为了我自己。\n\n再见。",
            karen,
        ),
        d.go("end_scene"),
    )

    d.node(
        "end_scene",
        d.set_flag(can_start_tmp_flag, 0, actor=None),
        d.fade_out(1.5),
        d.end(),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.APOTHEOSIS)
