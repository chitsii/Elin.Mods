# -*- coding: utf-8 -*-
"""Drama: ars_cinder_records (Stage 3->4: Erenos annotates Karen's records)

Karen's journal is spawned by UnhallowedPath.SpawnKarenJournal() before this drama plays.
"""

from drama.drama_builder import DramaBuilder
from drama.data import Actors, BGM


def define_cinder_records(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    drift = builder.label("drift")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_cinder_records"

    # ── main: annotations discovered ──
    builder.step(main)
    builder.quest_check("ars_cinder_records", can_start_tmp_flag)
    builder.fade_out(0.8)
    builder.fade_in(0.8)
    builder.play_bgm(BGM.REVELATION)
    builder.conversation([
        ("cr_1", "禁書を開くと、カレンの手帳に対する添削が記されていた。\n"
                 "見覚えのない筆跡...だが、どこか懐かしい。",
                 "You open the tome. Annotations on Karen's journal have appeared. \n"
                 "An unfamiliar hand -- yet somehow nostalgic.",
                 "翻开禁书，上面出现了对卡伦手账的批注。\n"
                 "陌生的笔迹……却莫名地令人怀念。"),
        ("cr_2", "これがエレノスの文字だと、なぜわかるのだろう。教えられていないのに。",
                 "How do you know this is Erenos's writing? No one told you.",
                 "为什么知道这是艾雷诺斯的字迹？明明无人告知。"),
    ], actor=narrator)
    builder.wait(0.3)
    builder.conversation([
        ("cr_3", "騎士の記録は概ね正確だ。感心するほどに。だが、いくつかの文脈が欠けている。",
                 "The knight's records are largely accurate. Impressively so. But some context is missing.",
                 "骑士的记录大体准确。令人佩服。然而缺少某些背景。"),
        ("cr_4", "「殺すより効率的な方法を見つけた」……効率のために殺さなかったのではない。\n"
                 "殺す理由がなかっただけだ。",
                 '"Found a more efficient method than killing" -- I did not spare them for efficiency.\n'
                 "There was simply no reason to kill.",
                 "「找到了比杀戮更高效的方法」……并非为了效率才不杀。\n"
                 "只是没有杀的理由罢了。"),
        ("cr_5", "「説得」と「理解」の違いは、観察者の立場による。\n"
                 "カレンは前者を選んだ。私は後者を主張する。どちらが正しいかは...結果が決める。",
                 'The difference between "persuasion" and "understanding" depends on the observer\'s position. '
                 "Karen chose the former. I assert the latter. The result will determine which is correct.",
                 "「说服」与「理解」之别，取决于观察者的立场。\n"
                 "卡伦选择了前者。我主张后者。孰是孰非……由结果裁定。"),
        ("cr_6", "騎士は「精神汚染」と呼ぶ。だが、汚染とは...\n"
                 "元の状態が正しいという前提に立っている。その前提自体を疑ったことはあるか？",
                 'The knights call it "mental contamination." But contamination assumes '
                 "the original state was correct. Have you ever questioned that assumption?",
                 "骑士将之称为「精神污染」。然而所谓污染……\n"
                 "是建立在原始状态即为正确的前提之上的。你质疑过这个前提本身吗？"),
        ("cr_7", "……このような問いを投げること自体が「精神汚染」の証拠だと、カレンなら言うだろう。\n"
                 "皮肉なことだ。",
                 '...Karen would say that even posing such a question is proof of "mental contamination." '
                 "How ironic.",
                 "……卡伦大概会说，仅仅是提出这样的问题本身，就已经是「精神污染」的证据了。\n"
                 "何其讽刺。"),
    ], actor=narrator)
    builder.jump(drift)

    # ── drift: personality drift explanation ──
    builder.step(drift)
    builder.shake()
    builder.wait(0.3)
    builder.conversation([
        ("cr_8", "人格漂流について説明しておくべきだろう。",
                 "I should explain personality drift.",
                 "有必要说明一下人格漂流。"),
        ("cr_9", "禁書は知識を伝達する。だがその過程で、著者の視座...\n"
                 "物の見方、判断の枠組み...が読者に移る。これは意図的な設計ではない。副作用だ。",
                 "The tome transmits knowledge. But in the process, the author's perspective -- \n"
                 "ways of seeing, frameworks of judgment -- transfers to the reader. This is not by design. It is a side effect.",
                 "禁书传递知识。但在这个过程中，著者的视角——\n"
                 "观察方式、判断的框架——会转移到读者身上。这并非刻意设计。是副作用。"),
        ("cr_10", "重要なのは結果だ。汝は今、私の視座の一部を持っている。\n"
                  "それは汝自身の判断を歪めているか？　おそらく。",
                  "What matters is the result. You now carry part of my perspective.\n"
                  "Does it distort your own judgment? Probably.",
                  "重要的是结果。汝如今已持有吾之视角的一部分。\n"
                  "这是否扭曲了汝自身的判断？大概是的。"),
        ("cr_11", "だが「歪み」という言葉は不正確だ。\n"
                  "レンズが光を曲げるのは歪みか？　それとも焦点か？",
                  'But "distortion" is an imprecise word. '
                  "When a lens bends light, is that distortion? Or focus?",
                  "然而「扭曲」一词并不精确。\n"
                  "透镜使光折射，是扭曲？还是聚焦？"),
    ], actor=narrator)
    builder.wait(0.3)
    builder.conversation([
        ("cr_12", "ページの隅に、見覚えのある筆跡が増えている。\n"
                  "「この論理は正しい」...そう書いてある。自分の字で。",
                  'In the margins, familiar handwriting has grown. '
                  '"This logic is correct" -- it reads. In your handwriting.',
                  "页角处，似曾相识的笔迹增多了。\n"
                  "「此论证无误」……如此写着。用你的字迹。"),
        ("cr_13", "……この論理は正しい。\n正しい、と頷いた。\nその頷きが、\n自分のものだったか。",
                  "...This logic is correct.\nYou nodded.\nWhether that nod\nwas your own.",
                  "……此论证无误。\n你点了点头。\n那一点头，\n是否出于自己的意志。"),
    ], actor=narrator)

    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.8)
