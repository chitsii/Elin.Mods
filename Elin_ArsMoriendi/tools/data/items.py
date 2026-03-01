# -*- coding: utf-8 -*-
"""
items.py - Ars Moriendi Item Definitions

Defines all custom items: the tome, soul items, etc.
"""

from dataclasses import dataclass, field
from enum import Enum
from typing import Dict, List, Optional


class TraitType(Enum):
    """Trait type"""
    VANILLA = "vanilla"
    CUSTOM = "custom"


@dataclass
class ItemDefinition:
    """Item definition mapping to SourceThing columns."""

    # Required
    id: str
    name_jp: str
    name_en: str
    category: str

    # Display
    detail_jp: str = ""
    detail_en: str = ""

    # CN localization
    name_cn: str = ""
    detail_cn: str = ""

    # Trait
    trait_type: TraitType = TraitType.VANILLA
    trait_name: str = ""
    trait_params: List[str] = field(default_factory=list)

    # Elements (enchantments)
    elements: str = ""

    # Game data
    value: int = 100
    lv: int = 1
    weight: int = 100
    chance: int = 0  # 0 = no random spawn

    # Rendering
    tiles: int = 0
    render_data: str = "item"

    # Equipment
    def_mat: str = ""
    tier_group: str = ""
    defense: str = ""

    # Tags
    tags: List[str] = field(default_factory=list)

    # Color
    color_mod: int = 0
    color_type: str = ""

    # Optional
    sort: str = ""
    quality: str = ""
    components: str = "log"
    filter: str = ""


# ============================================================================
# Item Definitions
# ============================================================================

CUSTOM_ITEMS: Dict[str, ItemDefinition] = {
    # ========================================
    # Ars Moriendi Tome (furniture, TraitArsMoriendi)
    # ========================================
    "ars_moriendi_tome": ItemDefinition(
        id="ars_moriendi_tome",
        name_jp="禁断の書 アルス・モリエンディ",
        name_en="Ars Moriendi - The Forbidden Tome",
        category="book",
        detail_jp="死者の秘術が記された禁断の書物。表紙に触れると、冷たい何かがあなたの指先を這い上がる。",
        detail_en="A forbidden tome inscribed with the secrets of the dead. When you touch its cover, something cold crawls up your fingertips.",
        name_cn="禁书 Ars Moriendi",
        detail_cn="记载着死者秘术的禁忌书物。触碰封面时，冰冷的什么沿着指尖攀爬而上。",
        trait_type=TraitType.CUSTOM,
        trait_name="ArsMoriendi",
        trait_params=[],
        value=50000,
        lv=10,
        weight=500,
        tiles=0,
        render_data="@obj",
        tags=["noShop", "unique"],
        sort="book",
        quality="4",
    ),

    # ========================================
    # Soul Items
    # ========================================
    "ars_soul_weak": ItemDefinition(
        id="ars_soul_weak",
        name_jp="弱い魂",
        name_en="Weak Soul",
        category="junk",
        detail_jp="かすかに光を放つ魂の欠片。触れると僅かな温もりを感じる。",
        detail_en="A faintly glowing soul fragment. You feel a slight warmth when touching it.",
        name_cn="微弱的灵魂",
        detail_cn="微微发光的灵魂碎片。触碰时能感到一丝温暖。",
        trait_type=TraitType.VANILLA,
        trait_name="",
        value=200,
        lv=1,
        weight=10,
        tiles=1551,  # orb sprite
        render_data="obj_S",
    ),
    "ars_soul_normal": ItemDefinition(
        id="ars_soul_normal",
        name_jp="魂",
        name_en="Soul",
        category="junk",
        detail_jp="安定した光を放つ魂。まだ生前の記憶の断片が残っているようだ。",
        detail_en="A soul emitting a steady glow. Fragments of memories from life still linger within.",
        name_cn="灵魂",
        detail_cn="散发着稳定光芒的灵魂。似乎还残留着生前记忆的碎片。",
        trait_type=TraitType.VANILLA,
        trait_name="",
        value=500,
        lv=5,
        weight=10,
        tiles=1551,
        render_data="obj_S",
    ),
    "ars_soul_strong": ItemDefinition(
        id="ars_soul_strong",
        name_jp="強い魂",
        name_en="Strong Soul",
        category="junk",
        detail_jp="力強く脈打つ魂。持っているだけで死の気配が漂う。",
        detail_en="A powerfully pulsing soul. Merely holding it brings the scent of death.",
        name_cn="强大的灵魂",
        detail_cn="强力脉动的灵魂。仅是持有便散发出死亡的气息。",
        trait_type=TraitType.VANILLA,
        trait_name="",
        value=2000,
        lv=15,
        weight=10,
        tiles=1551,
        render_data="obj_S",
    ),
    "ars_soul_legendary": ItemDefinition(
        id="ars_soul_legendary",
        name_jp="伝説の魂",
        name_en="Legendary Soul",
        category="junk",
        detail_jp="圧倒的な存在感を放つ魂。周囲の空気すら歪み、この世ならざるものの気配が漂う。",
        detail_en="A soul radiating overwhelming presence. The very air warps around it, carrying an otherworldly aura.",
        name_cn="传说之灵魂",
        detail_cn="散发着压倒性存在感的灵魂。周围的空气都因此扭曲，弥漫着超凡的气息。",
        trait_type=TraitType.VANILLA,
        trait_name="",
        value=10000,
        lv=30,
        weight=10,
        tiles=1551,
        render_data="obj_S",
        quality="4",
    ),

    # ========================================
    # Karen's Journal
    # ========================================
    "ars_karen_journal": ItemDefinition(
        id="ars_karen_journal",
        name_jp="カレンの手帳",
        name_en="Karen's Journal",
        category="book",
        detail_jp="聖騎士カレンが落とした手帳。禁書の前の継承者に関する記録が残されている。",
        detail_en="A journal dropped by Holy Knight Karen. It contains records about the previous inheritor of the forbidden tome.",
        name_cn="卡伦的笔记",
        detail_cn="圣骑士卡伦遗落的笔记。记载着关于禁书前代继承者的记录。",
        trait_type=TraitType.VANILLA,
        trait_name="Book",
        trait_params=["ars_karen_journal"],
        value=1000,
        lv=1,
        weight=100,
        tiles=1709,
        render_data="item",
        tags=["noShop"],
        sort="book",
    ),
    # ========================================
    # Alvin's Letter (scout encounter drop)
    # ========================================
    "ars_alvin_letter": ItemDefinition(
        id="ars_alvin_letter",
        name_jp="アルヴィンからカレンへの手紙",
        name_en="Alvin's Letter to Karen",
        category="book",
        detail_jp="偵察騎士が持っていた手紙。「騎士長、私は変わったのではありません。初めて理解したのです」",
        detail_en="A letter carried by a scout. 'Commander, I have not changed. I understood for the first time.'",
        name_cn="阿尔文致卡伦的信",
        detail_cn="侦察骑士携带的信件。「骑士长，我并非变了。我是第一次真正理解了。」",
        trait_type=TraitType.VANILLA,
        trait_name="Book",
        trait_params=["ars_alvin_letter"],
        value=500,
        lv=1,
        weight=50,
        tiles=1709,
        render_data="item",
        tags=["noShop"],
        sort="book",
    ),
    # ========================================
    # Scout Directive (scout encounter drop)
    # ========================================
    "ars_scout_directive": ItemDefinition(
        id="ars_scout_directive",
        name_jp="偵察指令書",
        name_en="Reconnaissance Directive",
        category="book",
        detail_jp="封蝋付きの指令書。「対象の監視および報告のみ。交戦を禁ずる」——署名はカレン・グレイヴォーン。",
        detail_en="A sealed directive. 'Observe and report on the subject only. Engagement is forbidden.' -- Signed by Karen Gravorn.",
        name_cn="侦察指令书",
        detail_cn="带封蜡的指令书。「仅限对目标进行监视和报告。禁止交战。」——署名：卡伦·格雷沃恩。",
        trait_type=TraitType.VANILLA,
        trait_name="Book",
        trait_params=["ars_scout_directive"],
        value=500,
        lv=1,
        weight=50,
        tiles=1709,
        render_data="item",
        tags=["noShop"],
        sort="book",
    ),
    # ========================================
    # Hecatia's Tactical Guide (readable book)
    # ========================================
    "ars_hecatia_guide": ItemDefinition(
        id="ars_hecatia_guide",
        name_jp="指南書",
        name_en="guidebook",
        category="book",
        detail_jp="死霊術の戦闘哲学と呪文運用を体系的にまとめた指南書。著者はヘカティア。",
        detail_en="A systematic guide to necromantic combat philosophy and spell usage. Authored by Hecatia.",
        name_cn="指南书",
        detail_cn="系统整理了死灵术战斗哲学与咒文运用的指南书。作者：赫卡提亚。",
        trait_type=TraitType.VANILLA,
        trait_name="Book",
        trait_params=["ars_hecatia_guide"],
        value=2000,
        lv=1,
        weight=150,
        tiles=1709,
        render_data="item",
        tags=["noShop"],
        sort="book",
    ),
    # ========================================
    # Ritual Notes (apotheosis catalyst memorandum)
    # ========================================
    "ars_ritual_notes": ItemDefinition(
        id="ars_ritual_notes",
        name_jp="初代の覚書",
        name_en="The Predecessor's Memorandum",
        category="book",
        detail_jp="禁書の頁の間から見つかった古い紙片。インクは退色し、端は焼け焦げている。",
        detail_en="Old papers found between the pages of the forbidden tome. The ink is faded and the edges are singed.",
        name_cn="(placeholder)",
        detail_cn="(placeholder)",
        trait_type=TraitType.VANILLA,
        trait_name="Book",
        trait_params=["ars_ritual_notes"],
        value=1000,
        lv=1,
        weight=50,
        tiles=1709,
        render_data="item",
        tags=["noShop"],
        sort="book",
    ),
}

# ============================================================================
# Necromancy Spellbooks (TraitNecroSpellbook)
# NecromancyManager.SpellUnlocks と1対1対応
# ============================================================================

_SPELLBOOK_DEFS = [
    # (alias, name_jp, name_en, name_cn)
    ("actSoulTrap",              "魂魄保存",               "Preserve Soul",              "灵魂封存"),
    ("actPreserveCorpse",        "屍体保存",             "Preserve Corpse",            "保存尸体"),
    ("actCurseWeakness",         "衰弱の呪い",           "Curse of Weakness",          "衰弱诅咒"),
    ("actSummonUndead",          "アンデッド召喚",       "Summon Undead",              "召唤亡灵"),
    ("actTerror",                "恐怖",                 "Terror",                     "恐惧"),
    ("actCurseFrailty",          "衰弱の呪い（重）",     "Curse of Frailty",           "衰弱诅咒（重）"),
    ("actLifeDrain",             "生命吸収",             "Life Drain",                 "生命汲取"),
    ("actStaminaDrain",          "精力吸収",             "Stamina Drain",              "精力汲取"),
    ("actManaDrain",             "魔力吸収",             "Mana Drain",                 "魔力汲取"),
    ("actPlagueTouch",           "疫病の手",             "Plague Touch",               "瘟疫之触"),
    ("actBoneAegisLegion",       "骸骨壁",               "Wall of Skeleton",           "骸骨之墙"),
    ("actGraveQuagmire",         "黄泉の泥濘",           "Grave Quagmire",             "黄泉泥泞"),
    ("actCorpseChainBurst",      "屍鎖爆砕",             "Corpse Chain Burst",         "尸锁爆砕"),
    ("actSoulRecall",            "死兵還生",             "Soul Recall",                "死兵还生"),
    ("actGraveExile",            "共連れ転送",           "Grave Exile",                "共连转送"),
    ("actFuneralMarch",          "死軍号令",             "Funeral March",              "死军号令"),
    ("actSummonSkeletonWarrior", "骸骨兵召喚",           "Summon Skeleton Warrior",    "召唤骷髅战士"),
    ("actUnholyVigor",           "不浄な活力",           "Unholy Vigor",               "不洁之活力"),
    ("actSoulBind",              "魂の鎖",               "Soul Bind",                  "灵魂锁链"),
    ("actDeathZone",             "死の領域",             "Death Zone",                 "死亡领域"),
    ("actSoulTrapMass",          "魂縛の檻",             "Soul Snare",                 "灵魂囚笼"),
]

for _alias, _name_jp, _name_en, _name_cn in _SPELLBOOK_DEFS:
    _item_id = f"ars_book_{_alias}"
    CUSTOM_ITEMS[_item_id] = ItemDefinition(
        id=_item_id,
        name_jp=f"{_name_jp}の魔法書",
        name_en=f"spellbook of {_name_en}",
        name_cn=f"{_name_cn}的魔法书" if _name_cn else "",
        category="spellbook",
        detail_jp="禁断の死霊術が記された魔法書。",
        detail_en="A spellbook inscribed with forbidden necromancy.",
        detail_cn="记载着禁忌死灵术的魔法书。",
        trait_type=TraitType.CUSTOM,
        trait_name="NecroSpellbook",
        trait_params=[_alias],
        value=500,
        lv=25,
        weight=200,
        tiles=1615,
        render_data="obj_S flat",
        def_mat="paper",
        components="texture",
        sort="book_skill",
        color_mod=150,
        color_type="random",
        chance=0,
    )
