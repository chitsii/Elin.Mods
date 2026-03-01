# -*- coding: utf-8 -*-
"""
arena/data/quest_journal.py - CWL Native Quest Journal Definition

ジャーナル表示用のSourceQuest定義。
現行のArenaQuestManagerシステムと並行して使用し、
ジャーナル表示のみをCWLネイティブ機能で実現する。

フェーズはランクに対応:
  Phase 1: Unranked (新人)
  Phase 2: G (屑肉)
  Phase 3: F (泥犬)
  Phase 4: E (鉄屑)
  Phase 5: D (銅貨稼ぎ)
  Phase 6: C (朱砂食い)
  Phase 7: B (銀翼)
  Phase 8: A (戦鬼)
  Phase 9: S (竜断ち)

注意: detailテキストは改行禁止（Elinが改行で分割してランダムに1行を選ぶため）
"""

from dataclasses import dataclass
from typing import List


@dataclass
class JournalPhase:
    """ジャーナルフェーズ定義"""

    phase: int
    rank: str
    title_jp: str
    title_en: str
    detail_jp: str
    detail_en: str
    detail_cn: str = ""  # 中国語


# クエストID
QUEST_ID = "sukutsu_arena"
QUEST_NAME_JP = "闘技場"
QUEST_NAME_EN = "Arena"
QUEST_NAME_CN = "斗技场"


# フェーズ定義（フレーバーテキスト）
# 注意: detailは1行で書くこと（改行するとランダムに1行だけ表示される）
JOURNAL_PHASES: List[JournalPhase] = [
    JournalPhase(
        phase=1,
        rank="UNRANKED",
        title_jp="Unranked - 無銘",
        title_en="Unranked",
        detail_jp="【Unranked - 無銘】名を持たぬ者が、血臭漂う砂の上に立つ。観衆の歓声は、どこか遠い世界の残響のように聞こえる。ここでは誰もが肉塊から始まり、そして多くが肉塊のまま終わる。生き残りたければ、己の弱さを砂に埋めよ。",
        detail_en="[Unranked] A nameless one stands upon blood-scented sand. The cheers of the crowd echo like remnants from a distant world. Here, all begin as meat, and most end as meat. If you wish to survive, bury your weakness in the sand.",
        detail_cn="【Unranked - 无名】无名之人站在血腥弥漫的沙地上。观众的欢呼声如同来自遥远世界的回响。在这里，所有人都从肉块开始，大多数也以肉块结束。若想生存，就将你的软弱埋入沙中。",
    ),
    JournalPhase(
        phase=2,
        rank="G",
        title_jp="G - 屑肉",
        title_en="G - Scrap",
        detail_jp="【G - 屑肉】屑肉。その名は侮蔑ではない。砕かれ、踏みにじられ、それでも立ち上がった者だけが名乗れる称号。最初の血は自分のものだった。二度目の血は、そうではなかった。歯を食いしばれ。これは始まりに過ぎない。",
        detail_en="[G - Scrap] Scrap. The name is not an insult. It is a title earned only by those who rise after being crushed and trampled. The first blood was your own. The second was not. Grit your teeth. This is only the beginning.",
        detail_cn="【G - 残渣】残渣。这个名字并非侮辱。它是只有在被粉碎、被践踏后仍能站起来的人才能获得的称号。第一滴血是你自己的。第二滴则不是。咬紧牙关。这只是开始。",
    ),
    JournalPhase(
        phase=3,
        rank="F",
        title_jp="F - 泥犬",
        title_en="F - Mud Dog",
        detail_jp="【F - 泥犬】泥犬は誇り高き獣ではない。汚泥の中を這い、残飯を漁り、それでも生き延びることを選んだ者。だが覚えておけ。追い詰められた犬の牙は、時に獅子をも穿つ。血の味を覚えた舌は、もう清い水では満たされない。",
        detail_en="[F - Mud Dog] A mud dog is not a proud beast. It crawls through filth, scavenges scraps, yet chooses to survive. But remember: a cornered dog's fangs can pierce even a lion. A tongue that has tasted blood will never be satisfied with pure water.",
        detail_cn="【F - 泥犬】泥犬并非高傲的野兽。它在污泥中爬行，翻找残羹剩饭，却选择活下去。但请记住：被逼入绝境的狗的獠牙，有时能刺穿雄狮。尝过血味的舌头，再也无法被清水满足。",
    ),
    JournalPhase(
        phase=4,
        rank="E",
        title_jp="E - 鉄屑",
        title_en="E - Iron Scrap",
        detail_jp="【E - 鉄屑】かつて英雄と呼ばれた男が、この砂の上で朽ちていった。錆びついた剣、色褪せた誓い、忘れ去られた名誉。だが鉄屑にも刃は残る。折れた剣でさえ、喉は裂ける。お前は彼の魂を踏み越えて、ここに立っている。",
        detail_en="[E - Iron Scrap] A man once called hero rotted away upon this sand. A rusted sword, faded oaths, forgotten honor. Yet even scrap iron retains an edge. Even a broken blade can slit a throat. You stand here, having stepped over his soul.",
        detail_cn="【E - 铁屑】一个曾被称为英雄的男人，在这片沙地上腐朽消逝。生锈的剑、褪色的誓言、被遗忘的荣誉。但即使是废铁也留有锋刃。即使是断剑，也能割开喉咙。你踏过他的灵魂，站在这里。",
    ),
    JournalPhase(
        phase=5,
        rank="D",
        title_jp="D - 銅貨稼ぎ",
        title_en="D - Copper Earner",
        detail_jp="【D - 銅貨稼ぎ】観衆の正体を知った時、多くの闘士は膝を折る。我々は娯楽だ。銅貨一枚の価値もない見世物だ。だがお前は立っている。理由などいらない。意味を問うな。ただ拳を握れ。それがお前の答えだ。",
        detail_en="[D - Copper Earner] When fighters learn the truth of the audience, many fall to their knees. We are entertainment. A spectacle worth less than a copper coin. Yet you still stand. You need no reason. Do not ask for meaning. Just clench your fist. That is your answer.",
        detail_cn="【D - 铜币赚手】当斗士们得知观众的真相时，许多人跪倒在地。我们是娱乐。是连一枚铜币都不值的表演。然而你依然站着。你不需要理由。不要追问意义。只需握紧拳头。那就是你的答案。",
    ),
    JournalPhase(
        phase=6,
        rank="C",
        title_jp="C - 朱砂食い",
        title_en="C - Cinnabar Eater",
        detail_jp="【C - 朱砂食い】闘技場の砂は、幾千もの血と臓物で朱く染まっている。その朱砂を喰らいながら這い上がる者だけが、この名を名乗れる。堕ちた英雄たちの魂を背負い、彼らの無念を拳に宿せ。お前の拳は、もはやお前だけのものではない。",
        detail_en="[C - Cinnabar Eater] The arena sand is stained crimson with the blood and viscera of thousands. Only those who claw their way up while choking on that vermillion grit may bear this name. Carry the souls of fallen heroes. Let their regrets dwell in your fists. Your fists are no longer yours alone.",
        detail_cn="【C - 朱砂食者】斗技场的沙被成千上万的鲜血和内脏染成朱红。只有在吞噬那朱砂的同时向上攀爬的人，才能承担这个名号。背负堕落英雄们的灵魂，让他们的遗憾寄宿在你的拳头中。你的拳头，已不再只属于你。",
    ),
    JournalPhase(
        phase=7,
        rank="B",
        title_jp="B - 銀翼",
        title_en="B - Silver Wing",
        detail_jp="【B - 銀翼】虚無を纏う処刑人が、無表情のままお前を見つめていた。人形は笑わない。人形は泣かない。人形は、何も感じない。——はずだった。銀の翼は、壊れた人形の涙を映して輝く。",
        detail_en="[B - Silver Wing] The executioner clad in void watched you, expressionless. Dolls do not laugh. Dolls do not cry. Dolls feel nothing. —Or so it should have been. Silver wings shine, reflecting the tears of a broken doll.",
        detail_cn="【B - 银翼】披着虚无的处刑人面无表情地注视着你。人偶不会笑。人偶不会哭。人偶什么都感觉不到。——本应如此。银色的翅膀闪耀着，映照出破碎人偶的泪水。",
    ),
    JournalPhase(
        phase=8,
        rank="A",
        title_jp="A - 戦鬼",
        title_en="A - War Demon",
        detail_jp="【A - 戦鬼】影と対峙せよ。それはお前自身だ。お前が殺した者たちの顔、お前が裏切った約束、お前が捨てた弱さ。師は言った。「鉄が鉄を鍛えるように、鬼が鬼を喰らう」と。戦鬼の名は、己を喰らい尽くした者だけが名乗れる。",
        detail_en="[A - War Demon] Face your shadow. It is yourself. The faces of those you killed, the promises you betrayed, the weakness you discarded. Your master said: 'As iron forges iron, demons devour demons.' Only those who have consumed themselves may bear the name War Demon.",
        detail_cn="【A - 战鬼】直面你的影子。那就是你自己。你杀死之人的面孔、你背叛的承诺、你抛弃的软弱。师父说过：「如同铁炼铁，鬼吞鬼。」只有吞噬了自己的人，才能拥有战鬼之名。",
    ),
    JournalPhase(
        phase=9,
        rank="S",
        title_jp="S - 竜断ち",
        title_en="S - Dragon Slayer",
        detail_jp="【S - 竜断ち】頂に立つ者は、常に孤独だ。師の血か、師の涙か。お前は何を選んだ？ 竜断ちの玉座は、竜の骨で出来ている。座るがいい。そして見届けよ。お前が壊したものと、お前が守ったものを。",
        detail_en="[S - Dragon Slayer] Those who stand at the summit are always alone. Your master's blood, or your master's tears. Which did you choose? The throne of the Dragon Slayer is made of dragon bones. Take your seat. And witness what you destroyed, and what you protected.",
        detail_cn="【S - 屠龙者】站在顶峰的人，总是孤独的。师父的血，还是师父的泪。你选择了什么？屠龙者的王座由龙骨铸成。坐上去吧。然后见证你所毁灭的，和你所守护的。",
    ),
    JournalPhase(
        phase=10,
        rank="SS",
        title_jp="SS - 天穿ち",
        title_en="SS - Heaven Piercer",
        detail_jp="【SS - 天穿ち】永遠を嘲笑う拳が、天を穿ち、神の喉を砕いた。観客席は空だ。喝采も嘲笑も、もう届かない。お前は天蓋の向こう側に立っている。だが忘れるな。天を穿った者は、もう地上には戻れない。",
        detail_en="[SS - Heaven Piercer] A fist that mocks eternity pierced the heavens and crushed a god's throat. The stands are empty. Neither cheers nor jeers reach you now. You stand beyond the firmament. But never forget: those who pierce the heavens can never return to earth.",
        detail_cn="【SS - 穿天者】嘲笑永恒的拳头穿透苍穹，粉碎了神的喉咙。观众席空无一人。欢呼与嘲笑都已无法传达。你站在天幕的彼端。但切勿忘记：穿透苍穹之人，再也无法回到地上。",
    ),
]


# ランクからフェーズへのマッピング
RANK_TO_PHASE = {
    "UNRANKED": 1,
    "G": 2,
    "F": 3,
    "E": 4,
    "D": 5,
    "C": 6,
    "B": 7,
    "A": 8,
    "S": 9,
    "SS": 10,
}


def get_phase_for_rank(rank: str) -> int:
    """ランクからジャーナルフェーズを取得"""
    return RANK_TO_PHASE.get(rank.upper(), 1)


def get_journal_phase(phase: int) -> JournalPhase | None:
    """フェーズ番号からJournalPhaseを取得"""
    for jp in JOURNAL_PHASES:
        if jp.phase == phase:
            return jp
    return None


def generate_source_quest_rows() -> list[dict]:
    """
    SourceQuest.xlsx用の行データを生成

    Returns:
        TSV/Excel出力用の辞書リスト
    """
    rows = []

    # メインクエスト行
    rows.append(
        {
            "id": QUEST_ID,
            "name_JP": QUEST_NAME_JP,
            "name": QUEST_NAME_EN,
            "type": "Quest",
            "detail_JP": JOURNAL_PHASES[0].detail_jp,
            "detail": JOURNAL_PHASES[0].detail_en,
        }
    )

    # 各フェーズ行
    for jp in JOURNAL_PHASES:
        rows.append(
            {
                "id": f"{QUEST_ID}{jp.phase}",
                "name_JP": QUEST_NAME_JP,
                "name": QUEST_NAME_EN,
                "type": "Quest",
                "detail_JP": jp.detail_jp,
                "detail": jp.detail_en,
            }
        )

    return rows
