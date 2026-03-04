from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_cinder_records_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_cinder_records', 'chitsii.ars.tmp.can_start.ars_cinder_records'),
            d.fade_out(0.8, color='black'),
            d.fade_in(0.8, color='black'),
            d.play_bgm('BGM/ManuscriptByCandlelight'),
            d.lines([('禁書を開くと、カレンの手帳に対する添削が記されていた。\n見覚えのない筆跡...だが、どこか懐かしい。', "You open the tome. Annotations on Karen's journal have appeared. \nAn unfamiliar hand -- yet somehow nostalgic.", '翻开禁书，上面出现了对卡伦手账的批注。\n陌生的笔迹……却莫名地令人怀念。'), ('これがエレノスの文字だと、なぜわかるのだろう。教えられていないのに。', "How do you know this is Erenos's writing? No one told you.", '为什么知道这是艾雷诺斯的字迹？明明无人告知。')], actor=narrator),
            d.wait(0.3),
            d.lines([('騎士の記録は概ね正確だ。感心するほどに。だが、いくつかの文脈が欠けている。', "The knight's records are largely accurate. Impressively so. But some context is missing.", '骑士的记录大体准确。令人佩服。然而缺少某些背景。'), ('「殺すより効率的な方法を見つけた」……効率のために殺さなかったのではない。\n殺す理由がなかっただけだ。', '"Found a more efficient method than killing" -- I did not spare them for efficiency.\nThere was simply no reason to kill.', '「找到了比杀戮更高效的方法」……并非为了效率才不杀。\n只是没有杀的理由罢了。'), ('「説得」と「理解」の違いは、観察者の立場による。\nカレンは前者を選んだ。私は後者を主張する。どちらが正しいかは...結果が決める。', 'The difference between "persuasion" and "understanding" depends on the observer\'s position. Karen chose the former. I assert the latter. The result will determine which is correct.', '「说服」与「理解」之别，取决于观察者的立场。\n卡伦选择了前者。我主张后者。孰是孰非……由结果裁定。'), ('騎士は「精神汚染」と呼ぶ。だが、汚染とは...\n元の状態が正しいという前提に立っている。その前提自体を疑ったことはあるか？', 'The knights call it "mental contamination." But contamination assumes the original state was correct. Have you ever questioned that assumption?', '骑士将之称为「精神污染」。然而所谓污染……\n是建立在原始状态即为正确的前提之上的。你质疑过这个前提本身吗？'), ('……このような問いを投げること自体が「精神汚染」の証拠だと、カレンなら言うだろう。\n皮肉なことだ。', '...Karen would say that even posing such a question is proof of "mental contamination." How ironic.', '……卡伦大概会说，仅仅是提出这样的问题本身，就已经是「精神污染」的证据了。\n何其讽刺。')], actor=narrator),
            d.go('drift'),
        ),
    )

    d.node(
        'drift',
        *d.seq(
            d.shake(),
            d.wait(0.3),
            d.lines([('人格漂流について説明しておくべきだろう。', 'I should explain personality drift.', '有必要说明一下人格漂流。'), ('禁書は知識を伝達する。だがその過程で、著者の視座...\n物の見方、判断の枠組み...が読者に移る。これは意図的な設計ではない。副作用だ。', "The tome transmits knowledge. But in the process, the author's perspective -- \nways of seeing, frameworks of judgment -- transfers to the reader. This is not by design. It is a side effect.", '禁书传递知识。但在这个过程中，著者的视角——\n观察方式、判断的框架——会转移到读者身上。这并非刻意设计。是副作用。'), ('重要なのは結果だ。汝は今、私の視座の一部を持っている。\nそれは汝自身の判断を歪めているか？\u3000おそらく。', 'What matters is the result. You now carry part of my perspective.\nDoes it distort your own judgment? Probably.', '重要的是结果。汝如今已持有吾之视角的一部分。\n这是否扭曲了汝自身的判断？大概是的。'), ('だが「歪み」という言葉は不正確だ。\nレンズが光を曲げるのは歪みか？\u3000それとも焦点か？', 'But "distortion" is an imprecise word. When a lens bends light, is that distortion? Or focus?', '然而「扭曲」一词并不精确。\n透镜使光折射，是扭曲？还是聚焦？')], actor=narrator),
            d.wait(0.3),
            d.lines([('ページの隅に、見覚えのある筆跡が増えている。\n「この論理は正しい」...そう書いてある。自分の字で。', 'In the margins, familiar handwriting has grown. "This logic is correct" -- it reads. In your handwriting.', '页角处，似曾相识的笔迹增多了。\n「此论证无误」……如此写着。用你的字迹。'), ('……この論理は正しい。\n正しい、と頷いた。\nその頷きが、\n自分のものだったか。', '...This logic is correct.\nYou nodded.\nWhether that nod\nwas your own.', '……此论证无误。\n你点了点头。\n那一点头，\n是否出于自己的意志。')], actor=narrator),
            d.set_flag('chitsii.ars.tmp.can_start.ars_cinder_records', 0, actor=None),
            d.fade_out(0.8, color='black'),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.CINDER_RECORDS)
