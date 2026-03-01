# -*- coding: utf-8 -*-
"""
15_vs_bulgas.md - 理を拒む者：師匠との最終決戦
Rank S昇格試合 - バルガスとの決戦と慈悲の選択
"""

from arena.builders import ArenaDramaBuilder, DramaBuilder
from arena.data import Actors, FlagValues, Keys, QuestBattleFlags, QuestIds, SessionKeys


def define_vs_balgas(builder: DramaBuilder):
    """
    バルガス戦 - 最終決戦
    シナリオ: 15_vs_bulgas.md
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    balgas = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    zek = builder.register_actor(Actors.ZEK, "ゼク", "Zek")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_prime")
    scene1_5 = builder.label("scene1_5_flashback")
    scene2 = builder.label("scene2_lily_prayer")
    scene3 = builder.label("scene3_battle")

    # ========================================
    # シーン1: 全盛期の幻影
    # ========================================
    builder.step(main).play_bgm("BGM/Ominous_Suspense_02").say(
        "narr_1",
        "ロビーの喧騒が消え、冷たい風が吹き抜ける。\nバルガスはいつになく整った足取りで、あなたの前に立った。",
        "The clamor of the lobby fades, and a cold wind blows through.\nVargus stands before you with an unusually steady gait.",
        "大厅的喧嚣消失，冷风吹过。\n巴尔加斯以罕见的稳健步伐站在你面前。",
        actor=narrator,
    ).say(
        "narr_3",
        "その手には、ゼクから手に入れたと思わしき、ドクドクと脈打つ紫黒色の薬瓶が握られている。",
        "In his hand, he grips a dark purple vial that pulses ominously--likely obtained from Zek.",
        "他手中握着一个咚咚跳动的紫黑色药瓶--想必是从泽克那里得到的。",
        actor=narrator,
    ).say(
        "balgas_1",
        "……おい、戦鬼。お前はもう、俺の手の届かねえ高みへ行こうとしてやがる。\n\nだがな、それでもアスタロト……あの竜神は、別格なんだ。",
        "...Hey, War Demon. You're reachin' heights I can't touch anymore.\n\nBut even so... Astaroth, that dragon god, is in a whole different league.",
        "……喂，战鬼。你小子已经要去老子够不着的高度了。\n\n不过啊，就算这样……阿斯塔罗特那个龙神，是另一个级别的存在。",
        actor=balgas,
    ).say(
        "balgas_2_1",
        "だからこそ、ここで俺が試金石になってやる。おまえを倒すために、対戦させてもらう。......冗談なんかじゃねえぞ。\n\n俺を......『全盛期の俺』を越えてみせろ。それができなきゃ、このさきお前は犠牲になっちまう。",
        "That's why I'm gonna be your touchstone right here. I'll be your opponent--to defeat you. ...This ain't no joke.\n\nSurpass me... surpass 'me in my prime.' If you can't do that, you'll end up a sacrifice down the road.",
        "所以，老子在这里当你的试金石。为了打倒你，让我来当你的对手。……可不是开玩笑。\n\n超越老子……超越『全盛期的老子』给我看。做不到的话，往后你就会成为牺牲品。",
        actor=balgas,
    ).say(
        "narr_4",
        "バルガスは一気に薬を煽った。",
        "Vargus downs the potion in one gulp.",
        "巴尔加斯一口气灌下了药。",
        actor=narrator,
    ).shake().say(
        "narr_5",
        "瞬間、彼の全身を覆っていた古い傷跡が消え、萎みかけていた筋肉が鋼のように膨れ上がる。\n\n白髪は黒々とした輝きを取り戻し、放たれる闘気だけでアリーナの石壁に亀裂が入った。",
        "In an instant, the old scars covering his body vanish, and his withered muscles swell like steel.\n\nHis white hair regains its lustrous black sheen, and his fighting spirit alone cracks the arena's stone walls.",
        "瞬间，覆盖他全身的旧伤疤消失，萎缩的肌肉如钢铁般膨胀起来。\n\n白发恢复了乌黑的光泽，仅是释放的斗气就让角斗场的石墙出现了裂痕。",
        actor=narrator,
    ).shake().say(
        "balgas_4",
        "……あぁ、いい気分だ。これなら、一度くらいはお前を本気で『殺し』にいける。\n来い！ 手加減は無しだ！ 殺すつもりで打ってこい！",
        "...Ahh, this feels good. Now I can go at you for real--at least once, I can try to 'kill' you.\nCome at me! No holdin' back! Fight like you mean to kill!",
        "……啊，感觉真好。这样的话，至少能认真地『杀』你一次了。\n来吧！不要留手！当作要杀了老子一样打过来！",
        actor=balgas,
    ).jump(scene1_5)

    # ========================================
    # シーン1.5: 回想ーー鉄血団の記憶
    # ========================================
    builder.step(scene1_5).play_bgm("BGM/Emotional_Sorrow").say(
        "narr_fb1",
        "ーー35年前、ノースティリス。\n若き日のバルガスが、傭兵団「鉄血団」を率いていた頃の記憶。",
        "--Thirty-five years ago, in North Tyris.\nA memory from when a young Vargus led the mercenary band 'Iron Blood.'",
        "--三十五年前，北提里斯。\n年轻时的巴尔加斯率领佣兵团「铁血团」的记忆。",
        actor=narrator,
    ).say(
        "balgas_fb1",
        "おい、ガキ。俺の懐に手を突っ込もうってのか？",
        "Hey, kid. You tryin' to reach into my pocket?",
        "喂，小鬼。想把手伸进老子口袋里？",
        actor=balgas,
    ).say(
        "narr_fb3",
        "10歳の孤児ーー後のカインは、バルガスの財布を盗もうとして捕まった。痩せこけた体、汚れた衣服、しかしその目だけは諦めていなかった。",
        "A ten-year-old orphan--the future Cain--was caught trying to steal Vargus's wallet. Emaciated body, filthy clothes, but those eyes hadn't given up.",
        "十岁的孤儿--日后的凯恩--因试图偷巴尔加斯的钱包而被抓住。瘦骨嶙峋的身体，肮脏的衣服，但只有那双眼睛没有放弃。",
        actor=narrator,
    ).say(
        "kain_fb1",
        "カインは震える声で呟いた。「……殺すなら殺せよ。どうせ、誰も俺のことなんか……」",
        "Cain muttered in a trembling voice. '...If you're gonna kill me, just do it. Nobody cares about me anyway...'",
        "凯恩用颤抖的声音嘟囔道。「……要杀就杀吧。反正，没人会在意我……」",
        actor=narrator,
    ).say(
        "balgas_fb2",
        "……盗むしかなかったんだろう。なら、正しく戦う術を教えてやる。明日から俺の部下だ。飯は食わせてやる。その代わり、死ぬほど鍛えてやるからな。",
        "...You had no choice but to steal, huh. Then I'll teach you how to fight proper. You're my subordinate startin' tomorrow. I'll feed ya. But in return, I'm gonna train you 'til you're half dead.",
        "……只能靠偷是吧。那老子就教你正经的战斗方法。明天开始你是老子的部下了。给你饭吃。不过作为代价，要把你练到半死。",
        actor=balgas,
    ).say(
        "narr_fb4",
        "ーー18年後。カインは鉄血団の副団長となった。バルガスの右腕として、誰よりも信頼された戦士。",
        "--Eighteen years later. Cain became vice-captain of Iron Blood. As Vargus's right hand, he was the most trusted warrior of all.",
        "--十八年后。凯恩成为了铁血团的副团长。作为巴尔加斯的左膀右臂，是最受信任的战士。",
        actor=narrator,
    ).say(
        "kain_fb2",
        "カインは報告した。「団長、次の依頼……『禁断の遺跡』の調査だそうです。報酬は破格ですが、嫌な予感がします。」",
        "Cain reported. 'Captain, the next job... it's to investigate the \"Forbidden Ruins.\" The pay's exceptional, but I've got a bad feeling.'",
        "凯恩报告道。「团长，下一个委托……据说是调查『禁忌遗迹』。报酬很丰厚，但我有不好的预感。」",
        actor=narrator,
    ).say(
        "balgas_fb3",
        "……嫌な予感ってのは当たるもんだ。だが、団員たちの冬越しの金がいる。行くしかねえだろ。",
        "...Bad feelings tend to be right. But we need money for the troops to survive winter. We ain't got a choice.",
        "……不好的预感往往是对的。但是，团员们过冬需要钱。只能去了吧。",
        actor=balgas,
    ).say(
        "kain_fb3",
        "カインは頷いた。「……分かりました。俺がしんがりを務めます。」",
        "Cain nodded. '...Understood. I'll take the rear guard.'",
        "凯恩点了点头。「……明白了。我来殿后。」",
        actor=narrator,
    ).say(
        "balgas_fb4",
        "馬鹿野郎。お前は俺の後継者だ。死ぬんじゃねえぞ。",
        "Don't be stupid. You're my successor. Don't you dare die.",
        "蠢货。你是老子的继承人。别给老子死了。",
        actor=balgas,
    ).play_bgm("BGM/Ominous_Suspense_02").shake().say(
        "narr_fb5",
        "ーー遺跡の奥で、次元の裂け目が開いた。団員たちが次々と狭間に引きずり込まれていく。\nカインは叫んだ。「団長……俺を、置いていけ……！ でないと、あんたまで……！」",
        "--Deep within the ruins, a dimensional rift opened. The members were dragged into the void one after another.\nCain screamed. 'Captain... leave me behind...! Otherwise, you'll be...!'",
        "--在遗迹深处，次元的裂缝打开了。团员们一个接一个地被拖入夹缝中。\n凯恩大喊。「团长……丢下我……！不然的话，你也会……！」",
        actor=narrator,
    ).say(
        "narr_fb6",
        "カインは罠で重傷を負い、動けなかった。バルガスはカインを背負い、狭間の中を彷徨った。",
        "Cain was gravely wounded by a trap and couldn't move. Vargus carried Cain on his back, wandering through the void.",
        "凯恩因陷阱身受重伤，无法动弹。巴尔加斯背着凯恩，在夹缝中徘徊。",
        actor=narrator,
    ).say(
        "balgas_fb5",
        "馬鹿野郎……！ お前を置いていけるか……！ ",
        "You idiot...! Like hell I'm leavin' you behind...!",
        "蠢货……！老子怎么可能丢下你……！",
        actor=balgas,
    ).shake().say(
        "narr_fb7",
        "しかし、カインの体は限界を迎えていた。アスタロトに「拾われた」時、カインは既に息絶えていた。\n\nーー回想が終わり、現在に戻る。",
        "But Cain's body had reached its limit. When Astaroth 'picked them up,' Cain had already breathed his last.\n\n--The flashback ends, returning to the present.",
        "然而，凯恩的身体已经到了极限。当被阿斯塔罗特「捡到」时，凯恩已经断气了。\n\n--回忆结束，回到现在。",
        actor=narrator,
    ).play_bgm("BGM/Ominous_Suspense_02").focus_chara(Actors.BALGAS).say(
        "balgas_fb6",
        "……俺はあの時、カインを救えなかった。\n\nだがな、お前は違う。お前は帰れるんだ。イルヴァに、生きている世界に。……だからこそ、俺は本気でお前を試す。中途半端な強さじゃ、アスタロトには勝てねえ。\n\nカインに教えてやれなかったことを、今度こそお前に叩き込んでやる……！",
        "...I couldn't save Cain back then.\n\nBut you're different. You can go back. To Ylva, to the world of the living. ...That's why I'm gonna test you for real. Half-assed strength won't beat Astaroth.\n\nWhat I couldn't teach Cain... this time, I'm gonna beat it into you...!",
        "……老子那时候没能救下凯恩。\n\n但你不一样。你能回去。回到伊尔瓦，回到活人的世界。……正因如此，老子要认真考验你。半吊子的实力是赢不了阿斯塔罗特的。\n\n没能教给凯恩的东西，这次一定要打进你身体里……！",
        actor=balgas,
    ).jump(scene2)

    # ========================================
    # シーン2: リリィの絶望と祈り
    # ========================================
    builder.step(scene2).play_bgm("BGM/Lily_Seductive_Danger").say(
        "narr_7",
        "リリィが震える手で水晶を握りしめている。\nその瞳には、事務的な冷徹さは微塵も残っていない。",
        "Lily clutches her crystal with trembling hands.\nNot a trace of her businesslike coldness remains in those eyes.",
        "莉莉用颤抖的手紧握着水晶。\n那双眼眸中，公事公办的冷漠荡然无存。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_1",
        "……馬鹿な人。それは、命の火花を浪費する禁忌の薬……！\n\nお願いです。彼を止めて……！\nもし彼が死んだら、このアリーナにはもう、私を叱ってくれる人は誰もいなくなってしまうわ……！",
        "...That fool. That's a forbidden potion that burns away one's life force...!\n\nPlease. Stop him...!\nIf he dies, there won't be anyone left in this arena to scold me...!",
        "……真是个傻瓜。那是燃烧生命之火的禁忌之药……！\n\n求求您。阻止他……！\n如果他死了，这个角斗场里就再也没有人会骂我了……！",
        actor=lily,
    ).jump(scene3)

    # ========================================
    # シーン3: 闘技場：師弟の極致 → バトル開始
    # ========================================
    builder.step(scene3).play_bgm("BGM/Battle_Balgas_Prime").say(
        "narr_10",
        "若き日の姿を取り戻した「伝説の戦士バルガス」との一騎打ち。\n彼の動きは重く、速く、そして無駄がない。\n教えてもらった技が、今度は殺意を持ってあなたを襲う。",
        "A one-on-one duel with 'Vargus the Legendary Warrior,' restored to his youthful form.\nHis movements are heavy, fast, and without waste.\nThe techniques he taught you now come at you with killing intent.",
        "与恢复年轻姿态的「传说战士巴尔加斯」的一对一决斗。\n他的动作沉重、迅速、毫无多余。\n他教给你的技巧，这次带着杀意向你袭来。",
        actor=narrator,
    ).shake().say(
        "obs_1",
        "観客席から声が響く。「...殺せ！ ...師匠を殺せ！」\n「...魂を捧げろ！」",
        "Voices echo from the stands. '...Kill! ...Kill your master!'\n'...Offer his soul!'",
        "观众席传来声音。「……杀！……杀了你的师傅！」\n「……献上他的灵魂！」",
        actor=narrator,
    ).say(
        "lily_voice",
        "（リリィの小さな懇願の声が、あなたの耳に届く。\n\n「お願い……殺さないで……」）",
        "(Lily's small, pleading voice reaches your ears. 'Please... don't kill him...')",
        "（莉莉微弱的恳求声传入你的耳中。「求求你……不要杀他……」）",
        actor=lily,
    ).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.VS_BALGAS
    ).start_battle_by_stage("rank_s_trial", master_id="sukutsu_arena_master").finish()


def add_vs_balgas_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    バルガス戦の勝利/敗北ステップを arena_master ビルダーに追加する

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

    # 勝利後の選択ラベル
    post_victory_choice = builder.label("vs_balgas_post_victory_choice")
    spare_balgas = builder.label("vs_balgas_spare")
    kill_balgas = builder.label("vs_balgas_kill")
    killed_ending = builder.label("vs_balgas_killed_ending")

    # ========================================
    # 勝利: 選択肢表示
    # ========================================
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).play_bgm("BGM/Emotional_Sorrow_2").say(
        "vb_narr_v1",
        "膝をつき、肩で息をするバルガス。\n全盛期の輝きが失われ、急速に元の老いた姿へと戻っていく。\n上空からは、観客たちの残酷な「処刑」を促す喝采が響き渡る。",
        "Vargus kneels, breathing heavily through his shoulders.\nThe radiance of his prime fades, and he rapidly reverts to his aged form.\nFrom above, the crowd's cruel cheers urging 'execution' resound.",
        "巴尔加斯单膝跪地，喘着粗气。\n全盛期的光芒消失，迅速恢复到原本苍老的样子。\n从上空传来观众们催促『处刑』的残酷喝彩声。",
        actor=narrator,
    ).shake().say(
        "obs_void",
        "虚空から声が降りてくる。「……殺セ。英雄ノ魂ヲ捧ゲ、真ノ『竜断ち』ト成レ……。」",
        "A voice descends from the void. '...Kill. Offer the hero's soul, and become the true \"Dragonslayer\"...'",
        "虚空中传来声音。「……杀。献上英雄的灵魂，成为真正的『屠龙者』……」",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "vb_balgas_v1",
        "……な、何をしてやがる。……刺せ。それがアリーナの、戦士のケジメだろうが……！",
        "...Wh-what're you doin'. ...Finish me. That's a warrior's honor in the arena...!",
        "……你、你在干什么。……刺下来。这是角斗场战士的规矩……！",
        actor=balgas,
    ).jump(post_victory_choice)

    # 選択肢: 見逃す or 殺す
    builder.choice(
        spare_balgas,
        "（武器を下ろし、手を差し伸べる）",
        "(Lower your weapon and extend your hand)",
        "（放下武器，伸出手）",
        text_id="c_spare_balgas",
    ).choice(
        kill_balgas,
        "（観客の命令に従い、とどめを刺す）",
        "(Obey the crowd's command and deliver the finishing blow)",
        "（服从观众的命令，给予致命一击）",
        text_id="c_kill_balgas",
    )

    # ========================================
    # 見逃すルート: 慈悲の選択
    # ========================================
    spare_choice = builder.label("vs_balgas_spare_choice")
    react_philosophy = builder.label("vs_balgas_react_philosophy")
    react_rule = builder.label("vs_balgas_react_rule")
    react_hand = builder.label("vs_balgas_react_hand")
    spare_ending = builder.label("vs_balgas_spare_ending")

    builder.step(spare_balgas).say(
        "narr_s1",
        "あなたは剣を引き、バルガスの喉元に突きつけた刃を下ろす。",
        "You withdraw your sword and lower the blade from Vargus's throat.",
        "你收回剑，放下抵在巴尔加斯咽喉的刀刃。",
        actor=narrator,
    ).say(
        "balgas_s1",
        "……な、何をしてやがる。……刺せ。それがアリーナの、戦士のケジメだろうが……！",
        "...Wh-what're you doin'. ...Finish me. That's a warrior's honor in the arena...!",
        "……你、你在干什么。……刺下来。这是角斗场战士的规矩……！",
        actor=balgas,
    ).jump(spare_choice)

    # 選択肢: なぜ見逃すのか
    builder.choice(
        react_philosophy,
        "俺の哲学には、師匠を殺すという項目はない",
        "My philosophy doesn't include killing my master",
        "我的哲学里没有杀死师傅这一条",
        text_id="c_spare_philosophy",
    ).choice(
        react_rule,
        "アリーナのルールに従うつもりはない。俺がルールだ",
        "I won't follow the arena's rules. I am the rule",
        "我不打算遵守角斗场的规则。我就是规则",
        text_id="c_spare_rule",
    ).choice(
        react_hand,
        "（無言で手を差し伸べる）",
        "(Silently extend your hand)",
        "（沉默地伸出手）",
        text_id="c_spare_hand",
    )

    builder.step(react_philosophy).say(
        "balgas_rp",
        "……ハッ。甘っちょろい野郎だ……。",
        "...Hah. Soft-hearted fool...",
        "……哈。心软的家伙……",
        actor=balgas,
    ).jump(spare_ending)

    builder.step(react_rule).say(
        "balgas_rr",
        "……傲慢な野郎だ。……だが、その傲慢が、俺が求めていた強さなのかもしれねえな……。",
        "...Arrogant bastard. ...But maybe that arrogance is the strength I was lookin' for...",
        "……傲慢的家伙。……不过，那份傲慢也许就是老子一直在寻找的力量……",
        actor=balgas,
    ).jump(spare_ending)

    builder.step(react_hand).say(
        "balgas_rh",
        "……無口な野郎だ。……だが、その手は……温かいな……。",
        "...Quiet one, ain't ya. ...But that hand... it's warm...",
        "……沉默寡言的家伙。……但是，那只手……很温暖啊……",
        actor=balgas,
    ).jump(spare_ending)

    # ラベル追加（リリィ離反対応）
    spare_lily_check = builder.label("vs_balgas_spare_lily_check")
    spare_lily_hostile = builder.label("vs_balgas_spare_lily_hostile")

    builder.step(spare_ending).play_bgm("BGM/Emotional_Sacred_Triumph_Special").say(
        "narr_s2",
        "あなたがバルガスの手を取り、立ち上がらせる。\nリリィが駆け寄り、泣きながらバルガスに回復魔法を注ぎ込む。",
        "You take Vargus's hand and help him to his feet.\nLily rushes over, pouring healing magic into Vargus while tears stream down her face.",
        "你拉住巴尔加斯的手，扶他站起来。\n莉莉跑过来，一边流泪一边向巴尔加斯注入治愈魔法。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_s1",
        "……ありがとう。本当に、ありがとうございます。",
        "...Thank you. Thank you so much.",
        "……谢谢您。真的，非常感谢。",
        actor=lily,
    ).focus_chara(Actors.BALGAS).say(
        "vb_balgas_s2",
        "……負けたよ。今日からお前がランクS『竜断ち』だ。",
        "...I lost. From today, you're Rank S, 'Dragonslayer.'",
        "……老子输了。从今天起你就是S级『屠龙者』了。",
        actor=balgas,
    ).set_flag(Keys.RANK, FlagValues.Rank.S).complete_quest(
        QuestIds.RANK_UP_S
    ).complete_quest(QuestIds.RANK_UP_S_BALGAS_SPARED).change_journal_phase(
        "sukutsu_arena", 9
    ).finish()

    # ========================================
    # 殺すルート: 観客に従う
    # ========================================
    builder.step(kill_balgas).play_bgm("BGM/Ominous_Suspense_02").shake().say(
        "narr_k1",
        "あなたは剣を振り下ろした。\nバルガスは最後まで、あなたを見つめていた。その瞳には……失望と、どこか安堵のような光があった。",
        "You bring your sword down.\nVargus watched you until the very end. In his eyes... there was disappointment, and something like relief.",
        "你挥下了剑。\n巴尔加斯直到最后都注视着你。他的眼中……有失望，也有某种释然般的光芒。",
        actor=narrator,
    ).say(
        "narr_k3",
        "観客席から、狂喜の叫びが轟く。「魂を捧げよ！ 竜断ちよ！」\nリリィが悲鳴を上げて駆け寄るが、もう遅い。",
        "Ecstatic screams thunder from the stands. 'Offer his soul! Dragonslayer!'\nLily screams and rushes over, but it's too late.",
        "观众席传来狂喜的呐喊。「献上他的灵魂！屠龙者！」\n莉莉尖叫着跑过来，但已经太迟了。",
        actor=narrator,
    ).focus_chara(Actors.LILY).say(
        "lily_k1",
        "……バルガスさん……！ どうして……！",
        "...Vargus...! Why...!",
        "……巴尔加斯先生……！为什么……！",
        actor=lily,
    ).say(
        "lily_k2",
        "リリィはあなたを見る。その瞳には、恐怖と悲しみが入り混じっている。",
        "Lily looks at you. Fear and sorrow mingle in her eyes.",
        "莉莉看向你。她的眼中交织着恐惧与悲伤。",
        actor=narrator,
    ).say(
        "lily_k3",
        "……あなたは……観客に魂を売ったのね……。",
        "...You... sold your soul to the crowd...",
        "……你……把灵魂卖给观众了呢……",
        actor=lily,
    ).jump(killed_ending)

    builder.step(killed_ending).set_flag(
        Keys.BALGAS_KILLED, FlagValues.BalgasChoice.KILLED
    ).eval(
        # バルガスを即座に死亡させる
        'var balgas = EClass._map.charas.Find(c => c.id == "sukutsu_arena_master"); '
        "if (balgas != null) { balgas.Die(); }"
    ).set_flag(Keys.RANK, FlagValues.Rank.S).complete_quest(
        QuestIds.RANK_UP_S
    ).complete_quest(QuestIds.RANK_UP_S_BALGAS_KILLED).change_journal_phase(
        "sukutsu_arena", 9
    ).finish()

    # ========================================
    # 敗北
    # ========================================
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).play_bgm("BGM/Lobby_Normal").focus_chara(Actors.LILY).say(
        "vb_lily_d1",
        "……バルガスさんには、まだ勝てなかったようですね。\nでも、生きているだけで十分です。彼は本気であなたを試したのですから。\n準備が整ったら、また挑戦してください。",
        "...It seems you couldn't defeat Vargus yet.\nBut being alive is enough. He was testing you with all his might, after all.\nWhen you're ready, please try again.",
        "……看来您还没能战胜巴尔加斯先生呢。\n不过，活着就足够了。毕竟他是在认真考验您。\n准备好了之后，请再次挑战。",
        actor=lily,
    ).finish()
