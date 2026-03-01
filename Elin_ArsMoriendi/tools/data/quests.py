# -*- coding: utf-8 -*-
"""
quests.py - Ars Moriendi Quest/Journal Phase Definitions

QuestSequence のフェーズ定義。
Phase 0 = 親行（type=QuestSequence）、Phase 1-10 = 各ストーリーステージ。

注意: detail テキストは改行禁止（Elin が改行で分割してランダムに1行を選ぶため）
"""

from dataclasses import dataclass
from typing import List


@dataclass
class QuestPhase:
    """ジャーナルフェーズ定義"""

    phase: int
    name_jp: str
    name_en: str
    detail_jp: str
    detail_en: str
    name_cn: str = ""
    detail_cn: str = ""


QUEST_ID = "ars_moriendi"
QUEST_NAME_JP = "アルス・モリエンディ"
QUEST_NAME_EN = "Ars Moriendi"
QUEST_NAME_CN = "死亡的艺术"

# Phase 0 = 親行、Phase 1-10 = 各ストーリーステージ
QUEST_PHASES: List[QuestPhase] = [
    QuestPhase(
        phase=0,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="禁断の書『アルス・モリエンディ』を手にした。古い皮革の表紙が脈打つように温かい。この書は、魂を求めている。",
        detail_en="You have obtained the forbidden tome 'Ars Moriendi.' Its ancient leather cover pulses with warmth. This book hungers for souls.",
        name_cn=QUEST_NAME_CN,
        detail_cn="你获得了禁书《Ars Moriendi》。古老的皮革封面如脉搏般温热。这本书，渴求着灵魂。",
    ),
    QuestPhase(
        phase=1,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【血染めの序章】最初の魂を禁書に捧げた。禁書が応え、未知の章が姿を現し始めている。死霊術の入口がここにある。禁書を開き、その知識を読み解け。",
        detail_en="[Blood-Stained Prologue] The first soul has been offered to the tome. It responds -- unknown chapters begin to reveal themselves. The gateway to necromancy lies here. Open the tome and decipher its knowledge.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【血色序章】向禁书献上了第一个灵魂。禁书作出回应，未知的章节开始显现。死灵术的入口就在此处。打开禁书，解读其中的知识。",
    ),
    QuestPhase(
        phase=2,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【消えぬ墨跡】禁書の封印が解かれ、死霊術の深淵が垣間見えた。だが禁書の力は痕跡を残す。どこかで、誰かがその痕跡を辿っている。力を磨き、来たるべき試練に備えよ。",
        detail_en="[Indelible Ink] The tome's seal is broken, revealing glimpses of necromancy's abyss. But the tome's power leaves traces. Somewhere, someone follows them. Hone your strength and prepare for the trials ahead.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【不灭的墨迹】禁书的封印已被解除，死灵术的深渊初现端倪。但禁书的力量留下了痕迹。在某处，有人正沿着这些痕迹追踪而来。磨练你的力量，为即将到来的试炼做好准备。",
    ),
    QuestPhase(
        phase=3,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【灰の歩哨】神殿騎士団の聖騎士カレンが立ちはだかった。禁書の継承者は「災厄の種」 -- しかも、お前が最初ではないという。カレンを退け、禁書を開いて先代の記録を調べよ。",
        detail_en="[Ashen Sentinel] Holy Knight Karen of the Temple Order bars your path. The tome's inheritor is a 'seed of calamity'-and you are not the first, she says. Defeat Karen, then open the tome to examine your predecessor's records.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【灰烬哨兵】神殿骑士团的圣骑士卡伦挡住了你的去路。禁书的继承者乃是「灾厄之种」——而且你并非第一个，她如此说道。击退卡伦，打开禁书调查前代的记录。",
    ),
    QuestPhase(
        phase=4,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【灰塵の記録】五代目の継承者エレノスの手記が禁書に浮かび上がった。彼もまたこの書に魅入られ、やがて自らの影に呑まれた。その影が、今も禁書の深層で蠢いている。従者を作成し、来たる対決に備えよ。",
        detail_en="[Ashen Records] The writings of Erenos, the fifth inheritor, have surfaced within the tome. He too was captivated by this book, only to be consumed by his own shadow-a shadow that still stirs in the tome's depths. Create a servant and prepare for the confrontation to come.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【灰烬记录】第五代继承者艾雷诺斯的手记在禁书中浮现。他同样被这本书所迷惑，最终被自己的影所吞噬。那道影，至今仍在禁书深处蠢动。制作仆从，为即将到来的对决做好准备。",
    ),
    QuestPhase(
        phase=5,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【聖痕】エレノスの影が禁書から解き放たれた。先代の意志が形を得て、禁書の真の主は誰かを問うている。この書に二人の主はいらない。エレノスの影を打ち倒せ。",
        detail_en="[Stigmata] Erenos's shadow has broken free from the tome. The predecessor's will has taken form, demanding to know who truly masters the book. The tome suffers no two masters. Defeat the Shadow of Erenos.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【圣痕】艾雷诺斯之影从禁书中挣脱。前代的意志化为实体，质问谁才是禁书真正的主人。这本书不容二主。打倒艾雷诺斯之影。",
    ),
    QuestPhase(
        phase=6,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【第七の徴】禁書に見知らぬ文字が浮かんだ。まだ存在しない誰かの文字。連鎖は続いている。",
        detail_en="[The Seventh Sign] Unknown characters have appeared in the tome. Written by someone who does not yet exist. The chain continues.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【第七征兆】禁书上浮现了陌生的文字。是尚不存在之人书写的文字。连锁仍在继续。",
    ),
    QuestPhase(
        phase=7,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【先代の影法師】聖騎士カレンが再び現れた。従者の数が先代と同じ水準に達したことへの警告。先代の影を受け止め、最後の試練に備えよ。",
        detail_en="[Karen's Shadow] Holy Knight Karen has appeared once more, warning that your servant count matches the predecessor's. Accept the shadow of the past and prepare for the final trial.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【前代的影法师】圣骑士卡伦再次现身。她警告你的仆从数量已达到与前代同等的水平。接受前代之影，为最终试炼做好准备。",
    ),
    QuestPhase(
        phase=8,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【先代の影】エレノスの影を退けた。禁書の最深部が開かれ、昇華の儀式 -- 人の限界を超え、禁書と一体化する禁断の術式 -- が姿を現した。儀式に必要な素材を集めよ。",
        detail_en="[Shadow of the Predecessor] Erenos's shadow has been vanquished. The tome's innermost depths lie open, revealing the Apotheosis-a forbidden rite to transcend human limits and merge with the tome. Gather the materials the ritual demands.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【前代之影】击退了艾雷诺斯之影。禁书的最深处已经敞开，升华仪式——超越人类极限、与禁书融为一体的禁忌术式——显露了真容。收集仪式所需的素材。",
    ),
    QuestPhase(
        phase=9,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【不浄なる覚醒】昇華の儀式を完遂し、禁書と一体化した。六代目の継承者として人の域を超え、先代たちが辿り着けなかった境地に立つ。もはやこの書の呪いか祝福かを問う者はいない。",
        detail_en="[Unhallowed Awakening] The Apotheosis is complete. United with the tome, you have transcended humanity as the sixth inheritor, standing where none before you could reach. None remain to question whether this book is a curse or a blessing.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【不洁的觉醒】完成了升华仪式，与禁书融为一体。作为第六代继承者超越了人类的界限，站在了前代们无法企及的境界。再无人质问这本书究竟是诅咒还是祝福。",
    ),
    QuestPhase(
        phase=10,
        name_jp=QUEST_NAME_JP,
        name_en=QUEST_NAME_EN,
        detail_jp="【継承者の手記】昇華を経て世界の見え方が変わった。六代目としての章が始まった。禁書が自動的に記録を刻んでいる。",
        detail_en="[Successor's Notes] The world looks different after Apotheosis. Your chapter as the sixth inheritor has begun. The tome inscribes records on its own.",
        name_cn=QUEST_NAME_CN,
        detail_cn="【继承者的手记】经历升华后，世界的面貌已然改变。作为第六代继承者的篇章已经开始。禁书在自行刻录着记录。",
    ),
]
