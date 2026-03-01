# -*- coding: utf-8 -*-
"""
Rank G 昇格試合「屑肉の洗礼」
"""

from arena.builders import ArenaDramaBuilder, ChoiceReaction, DramaBuilder
from arena.data import (
    Actors,
    Keys,
    QuestIds,
    Rank,
    SessionKeys,
)


def define_rank_up_G(builder: DramaBuilder):
    """
    Rank G 昇格試合「屑肉の洗礼」

    シナリオ: 02_rank_up_01.md
    """
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    vargus = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    narrator = Actors.NARRATOR

    main = builder.label("main")

    # シーン1: 受付での宣告
    reception = builder.label("reception")
    # シーン2: バルガスの餞別
    vargus_advice = builder.label("vargus_advice")
    # シーン3: 戦闘開始
    battle_start = builder.label("battle_start")

    # 勝利時・敗北時コメント
    victory = builder.label("victory")
    defeat = builder.label("defeat")

    # --- Reception ---
    # BGM: 不穏な静寂、遠くで鎖の擦れる音
    # (既存の sukutsu_arena_opening が近い雰囲気)
    # 注意: 結果チェックは arena_master.py 側で行い、
    # このドラマは戦闘開始前の会話のみを担当する
    builder.step(main).drama_start(
        bg_id="Drama/arena_battle_normal", bgm_id="BGM/sukutsu_arena_opening"
    ).focus_chara(Actors.LILY).say(
        "narr_1",
        "薄暗いロビーに、異次元の嵐が石壁を叩く音が不気味に響いている。空気は重く、血と錆の臭いが鼻腔を突く。",
        "In the dim lobby, the sound of interdimensional storms battering stone walls echoes ominously. The air hangs heavy, thick with the stench of blood and rust.",
        "昏暗的大厅里，异次元风暴拍打石壁的声音不祥地回荡着。空气沉重，血腥与铁锈的气味刺入鼻腔。",
        actor=narrator,
    ).say(
        "lily_r1",
        "……準備はよろしいですか？",
        "...Are you prepared?",
        "……您准备好了吗？",
        actor=lily,
    ).say(
        "narr_2",
        "彼女は細長い爪で、血塗られた羊皮紙を軽く叩いた。パチン、パチンと、まるで死刑執行の秒読みのように。",
        "She taps the blood-stained parchment with her slender nails. Click, click - like a countdown to execution.",
        "她用纤细的指甲轻轻敲击着沾满血迹的羊皮纸。啪嗒、啪嗒，仿佛死刑执行的倒计时。",
        actor=narrator,
    ).say(
        "lily_r2a",
        "これは単なる試合ではありません。あなたがこの闘技場の胃袋に放り込まれる、最初の『餌』になるための儀式です。",
        "This is not merely a match. It is a ritual - your initiation as the first 'bait' to be cast into this arena's maw.",
        "这不仅仅是一场比赛。这是一个仪式--您将成为被投入这个角斗场胃袋的第一块『饵食』。",
        actor=lily,
    ).say(
        "lily_r2b",
        "対戦相手は『飢えたヴォイド・プチ』の群れ。\n\n……ああ、地上にいる愛らしい彼らだと思わないことね。敗者の絶望を啜って肥大化した、純然たる殺意の塊ですから。",
        "Your opponents shall be a swarm of 'Starving Void Putits.'\n\n...Do not mistake them for those adorable creatures on the surface. These have bloated themselves on the despair of the fallen - pure, concentrated malice.",
        "您的对手是一群『饥饿的虚空普奇』。\n\n……呵呵，可别把它们当成地面上那些可爱的小家伙呢。它们吸食败者的绝望而膨胀，是纯粹杀意的凝聚体。",
        actor=lily,
    ).say(
        "lily_r2c",
        "もし、五体満足で戻られたら……その時は、正式に『闘士』として登録して差し上げます。死体袋の用意は、あちらの隅に。……ご武運を。",
        "Should you return with all your limbs intact... I shall formally register you as a 'gladiator.' The body bags are prepared in that corner. ...May fortune favor you.",
        "如果您能四肢完好地回来……届时，我会正式将您登记为『角斗士』。尸袋就放在那边的角落里。……祝您武运昌隆。",
        actor=lily,
    )

    # プレイヤー選択肢 - choice_block で選択肢と反応を一括定義
    builder.choice_block(
        [
            ChoiceReaction(
                "……死体袋は不要だ。俺は生きて帰る",
                text_id="c_r_1",
                text_en="...I won't need a body bag. I'm coming back alive.",
                text_cn="……尸袋就不必了。我会活着回来。",
            )
            .say(
                "lily_r5_a",
                "ふふ、自信はおありのようで。……では、存分に。",
                "Hehe, such confidence you have. ...Very well, do indulge yourself.",
                "呵呵，您似乎很有自信呢。……那么，请尽情发挥吧。",
                actor=lily,
            )
            .jump(vargus_advice),
            ChoiceReaction(
                "プチごときに負けるか。すぐに終わらせてやる",
                text_id="c_r_2",
                text_en="Lose to mere Putits? I'll finish this quickly.",
                text_cn="输给区区普奇？我会速战速决的。",
            )
            .say(
                "lily_r5_b",
                "まあ。勇ましいこと。……その自信が、どこまで保つか楽しみですね。",
                "My, how valiant. ...I do wonder how long that confidence shall last.",
                "哎呀，真是勇敢呢。……真期待您的自信能保持多久呢。",
                actor=lily,
            )
            .jump(vargus_advice),
            ChoiceReaction(
                "（無言で羊皮紙を受け取る）",
                text_id="c_r_3",
                text_en="(Silently accept the parchment)",
                text_cn="（默默接过羊皮纸）",
            )
            .say(
                "lily_r5_c",
                "……沈黙は恐怖の裏返しか、それとも覚悟の証か。まあ、どちらでもいいのですが。",
                "...Is your silence born of fear, or of resolve? Well, it matters not either way.",
                "……沉默是恐惧的反面，还是觉悟的证明呢。嗯，无论哪种都无所谓呢。",
                actor=lily,
            )
            .jump(vargus_advice),
        ],
        label_prefix="lily",
    )

    # --- Vargus Advice ---
    builder.step(vargus_advice).focus_chara(Actors.BALGAS).say(
        "narr_3",
        "闘技場へ繋がる鉄格子の前で、バルガスが研ぎ澄まされた剣を無造作に弄んでいる。",
        "Before the iron grate leading to the arena, Vargus idly toys with a well-honed blade.",
        "在通往角斗场的铁栅栏前，巴尔加斯随意地摆弄着一把磨利的剑。",
        actor=narrator,
    ).say(
        "vargus_r2a",
        "……いいか、一度だけ教えてやる。プチ共は『数』で来る。一匹一匹はゴミだが、囲まれればお前の肉は一瞬で削げ落ち、綺麗な骨の標本ができあがりだ。",
        "...Listen up, I'll only say this once. Those Putits attack in numbers. Each one's trash, but get surrounded and they'll strip your flesh clean off - leave behind a nice skeleton specimen.",
        "……听好了，老子只说一遍。那些普奇是靠『数量』取胜的。一只只虽然是垃圾，但被围住的话，你小子的肉会瞬间被啃光，留下一副漂亮的骨头标本。",
        actor=vargus,
    ).say(
        "vargus_r2b",
        "俺が戦った時、奴らを倒した後に荷物を確認したら、食料がごっそり減っていた。戦闘中に漁られていたらしい。油断するな。",
        "When I fought 'em, I checked my pack after the battle and found my food supplies nearly gone. Those bastards were rummaging through my stuff mid-fight. Don't let your guard down.",
        "老子跟它们打的时候，战斗结束后一检查行李，发现食物被吃得精光。估计是战斗中被偷走的。别大意了。",
        actor=vargus,
    ).say(
        "vargus_r3a",
        "火力があるならまとめて薩ぎ払え。それが無理なら、テレポート等の手段で距離を取れ。囲まれるのが一番危険だ。",
        "If you've got firepower, sweep 'em all at once. If not, use teleportation or something to keep your distance. Getting surrounded is the deadliest thing.",
        "有火力的话就一口气扫掉。做不到的话，用传送之类的手段拉开距离。被围住是最危险的。",
        actor=vargus,
    ).say(
        "vargus_r3b",
        "……ほら、行け。観客どもが、お前の悲鳴を心待ちにしてやがるぜ。",
        "...Now get going. The crowd's waiting to hear you scream.",
        "……好了，去吧。观众们正眼巴巴等着听你小子的惨叫呢。",
        actor=vargus,
    ).jump(battle_start)

    # --- Battle Start ---
    builder.step(battle_start).start_battle_by_stage(
        "rank_g_trial", master_id="sukutsu_arena_master"
    ).finish()


def add_rank_up_G_result_steps(
    builder: ArenaDramaBuilder, victory_label: str, defeat_label: str, return_label: str
):
    """
    Rank G 昇格試合の勝利/敗北ステップを arena_master ビルダーに追加する

    Args:
        builder: arena_master の ArenaDramaBuilder インスタンス
        victory_label: 勝利ステップのラベル名
        defeat_label: 敗北ステップのラベル名
        return_label: 結果表示後にジャンプするラベル名
    """
    pc = Actors.PC
    lily = Actors.LILY
    vargus = Actors.BALGAS
    narrator = Actors.NARRATOR

    # === Rank G 昇格試合 勝利 ===
    builder.step(victory_label).set_flag(SessionKeys.ARENA_RESULT, 0).focus_chara(
        Actors.BALGAS
    ).say(
        "rup_vic_v1",
        "……しぶとい奴だ。",
        "...Tough bastard.",
        "……还挺能扛的小子。",
        actor=vargus,
    ).say(
        "rup_vic_v2",
        "まぁ、合格だ。",
        "Hah, you pass.",
        "哈，算你过关了。",
        actor=vargus,
    ).focus_chara(Actors.LILY).say(
        "rup_vic_l1",
        "お疲れ様でした。約束通り、ギルドの台帳にあなたの名を刻んでおきました。",
        "Well done. As promised, I have inscribed your name in the guild ledger.",
        "辛苦了。如约定的那样，我已将您的名字刻在公会的账簿上了。",
        actor=lily,
    ).say(
        "rup_vic_l2",
        "ランクG『屑肉』。ふふ、あなたにぴったりの、美味しそうな二つ名だと思いませんか？",
        "Rank G, 'Scrap Meat.' Hehe, don't you think it's a deliciously fitting title for you?",
        "G级『碎肉』。呵呵，您不觉得这是个非常适合您的、听起来很美味的称号吗？",
        actor=lily,
    ).complete_quest(QuestIds.RANK_UP_G).grant_rank_reward(
        "G", actor=lily
    ).change_journal_phase("sukutsu_arena", 2).say(
        "rup_vic_sys",
        "報酬を受け取った。",
        "You received a reward.",
        "获得了报酬。",
        actor=narrator,
    ).focus_chara(Actors.BALGAS).say(
        "rup_vic_v3",
        "あと、この闘技場には胡散臭い商人が出入りすることがある。お前が一度外に出て戻ってくれば、そいつと出くわすかもしれんな。",
        "One more thing - there's a shady merchant who comes and goes from this arena. Leave and come back, and you might run into him.",
        "还有，这角斗场偶尔会有个可疑的商人出没。你小子出去一趟再回来的话，说不定能碰上。",
        actor=vargus,
    ).finish()

    # === Rank G 昇格試合 敗北 ===
    builder.step(defeat_label).set_flag(SessionKeys.ARENA_RESULT, 0).focus_chara(
        Actors.LILY
    ).say(
        "rup_def_l1",
        "あらあら……。期待外れでしたね。",
        "My, my... How disappointing.",
        "哎呀呀……真是令人失望呢。",
        actor=lily,
    ).say(
        "rup_def_l2",
        "死体袋の用意が無駄にならなくて何よりです。……次の方、どうぞ。",
        "At least the body bags didn't go to waste. ...Next, please.",
        "尸袋没有白准备，真是太好了呢。……下一位，请。",
        actor=lily,
    ).finish()
