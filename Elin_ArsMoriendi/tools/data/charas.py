# -*- coding: utf-8 -*-
"""
charas.py - Ars Moriendi Character Definitions

Defines all custom NPCs for the mod.
"""

from dataclasses import dataclass, field
from typing import Dict, List


@dataclass
class CharaDefinition:
    """Character definition mapping to SourceChara columns."""

    # Required
    id: str
    name_jp: str
    name_en: str
    race: str

    # Identity
    job: str = ""
    trait_name: str = ""
    hostility: str = "Friend"
    bio: str = ""

    # Stats
    lv: int = 1
    chance: int = 0  # 0 = no random spawn

    # Rendering
    tiles: int = 0
    render_data: str = "@chara"

    # Tags
    tag: List[str] = field(default_factory=list)

    # Elements (feats, etc.)
    elements: List[str] = field(default_factory=list)  # elements列（例: ["featBloodBond/3"]）

    # AI / Combat
    tactics: str = ""           # SourceTactics ID（空ならjobから自動解決）
    act_combat: List[str] = field(default_factory=list)  # actCombat列（例: ["actLifeDrain/30"]）
    ai_param: List[int] = field(default_factory=list)     # aiParam列 [destDist, chanceMove]

    # Faith
    faith: str = ""  # 信仰ID（healing, wind, earth, etc.）

    # Description
    detail_jp: str = ""
    detail_en: str = ""

    # CN localization
    name_cn: str = ""
    detail_cn: str = ""

    # CWL
    author: str = "tishi.elin.ars_moriendi"


# ============================================================================
# Character Definitions
# ============================================================================

CUSTOM_CHARAS: Dict[str, CharaDefinition] = {
    "ars_hecatia": CharaDefinition(
        id="ars_hecatia",
        name_jp="ヘカティア",
        name_en="Hecatia",
        race="undeadgod",
        job="merchant",
        trait_name="NecroMerchant",
        hostility="Friend",
        lv=1000,
        chance=0,
        tiles=340,
        bio="f/1001/165/52",
        tag=["addStock", "addDrama_drama_ars_hecatia", "humanSpeak", "neutral"],
        elements=[
            "featBloodBond/3",
            "featElder/1",
            "featManaMeat/3",
        ],
        act_combat=[
            "miasma_Darkness/20",
            "SpTeleportShort/50",
            "SpSummonUndead/20",
            "arrow_Nerve/30",
            "ball_Darkness/30",
            "SpSilence/20",
            "Wait",
            "Wait",
        ],
        ai_param=[3, 0],
        detail_jp="禁断の書から現れる死霊術の商人。その口調は軽いが、"
                  "禁書について誰よりも詳しいようだ。",
        detail_en="A necromancy merchant who emerges from the forbidden tome. "
                  "Her tone is light, but she seems to know the tome better than anyone.",
        name_cn="赫卡提亚",
        detail_cn="从禁书中现身的死灵术商人。语气虽轻，却似乎比任何人都了解这本禁书。",
    ),
    "ars_karen": CharaDefinition(
        id="ars_karen",
        name_jp="灰爵カレン",
        name_en="Karen the Ash Baron",
        race="elea",
        job="paladin",
        hostility="Enemy",
        lv=100,
        chance=0,
        tiles=0,
        bio="f/1001/165/52",
        tag=[],
        faith="healing",
        act_combat=[
            "ActJureHeal/40",
            "SpHolyVeil/50/pt",
            "SpHero/40",
            "SpRebirth/30",
            "SpSilence/40",
            "ActRush/50",
        ],
        ai_param=[1, 80],
        detail_jp="禁書の継承者を追う神殿騎士団の聖騎士。",
        detail_en="A holy knight of the Temple Order, hunting the inheritor of the forbidden tome.",
        name_cn="灰爵卡伦",
        detail_cn="追捕禁书继承者的神殿骑士团圣骑士。",
    ),
    "ars_temple_knight": CharaDefinition(
        id="ars_temple_knight",
        name_jp="神殿騎士",
        name_en="Temple Knight",
        race="norland",
        job="guard",
        hostility="Enemy",
        lv=20,
        chance=0,
        tiles=812,
        render_data="chara",
        tag=[],
        faith="healing",
        act_combat=[
            "ActRush/40",
            "ActBash/30",
        ],
        ai_param=[1, 70],
        detail_jp="神殿騎士団の兵士。",
        detail_en="A soldier of the Temple Order.",
        name_cn="神殿骑士",
        detail_cn="神殿骑士团的士兵。",
    ),
    "ars_temple_scout": CharaDefinition(
        id="ars_temple_scout",
        name_jp="神殿偵察兵",
        name_en="Temple Scout",
        race="norland",
        job="thief",
        hostility="Enemy",
        lv=15,
        chance=0,
        tiles=1112,
        render_data="chara",
        tag=[],
        faith="healing",
        act_combat=[
            "SpCatsEye/50",
            "SpTeleportShort/40",
            "ActTouchSleep/30",
        ],
        ai_param=[3, 60],
        detail_jp="死霊術の気配を追う神殿騎士団の偵察兵。",
        detail_en="A temple scout tracking the scent of necromancy.",
        name_cn="神殿侦察兵",
        detail_cn="追踪死灵术气息的神殿骑士团侦察兵。",
    ),
    "ars_erenos_guard": CharaDefinition(
        id="ars_erenos_guard",
        name_jp="エレノスの骸骨衛兵",
        name_en="Erenos's Skeleton Guard",
        race="skeleton",
        job="warrior",
        hostility="Enemy",
        lv=50,
        chance=0,
        tiles=0,
        tag=[],
        act_combat=[
            "ActRush/50",
            "ActBash/40",
            "actBoneAegisLegion/20",
        ],
        ai_param=[1, 80],
        detail_jp="エレノスに仕える骸骨の衛兵。主の命に従い、侵入者を排除する。",
        detail_en="A skeleton guard serving Erenos. Follows its master's command to eliminate intruders.",
        name_cn="艾雷诺斯的骷髅卫兵",
        detail_cn="侍奉艾雷诺斯的骷髅卫兵。遵从主人之命，清除入侵者。",
    ),
    "ars_erenos_shade": CharaDefinition(
        id="ars_erenos_shade",
        name_jp="エレノスの影術師",
        name_en="Erenos's Shade Caster",
        race="ghost",
        job="wizard",
        hostility="Enemy",
        lv=60,
        chance=0,
        tiles=0,
        tag=[],
        act_combat=[
            "bolt_Darkness/40",
            "actCurseWeakness/30",
            "SpDarkness/20",
            "SpTeleportShort/30",
        ],
        ai_param=[3, 50],
        detail_jp="エレノスに仕える影の呪術師。闇の魔術で主を守護する。",
        detail_en="A shade caster serving Erenos. Guards its master with dark magic.",
        name_cn="艾雷诺斯的影术师",
        detail_cn="侍奉艾雷诺斯的暗影咒术师。以暗之魔术守护主人。",
    ),
    "ars_erenos_shadow": CharaDefinition(
        id="ars_erenos_shadow",
        name_jp="エレノスの影",
        name_en="Shadow of Erenos",
        race="lich",
        job="wizard",
        hostility="Enemy",
        bio="m",
        lv=200,
        chance=0,
        tiles=0,
        tag=[],
        act_combat=[
            "actSummonUndead/50",
            "actLifeDrain/30",
            "actManaDrain/20",
            "actStaminaDrain/20",
            "actDeathZone/25",
            "miasma_Nether/40",
            "bolt_Darkness/30",
            "SpDarkness/20",
            "SpTeleportShort/30",
            "ActDeathSentense/15",
        ],
        ai_param=[2, 60],
        detail_jp="アルス・モリエンディの先代継承者の影。",
        detail_en="The shadow of the previous inheritor of Ars Moriendi.",
        name_cn="艾雷诺斯之影",
        detail_cn="Ars Moriendi前代继承者之影。",
    ),
    "ars_erenos_pet": CharaDefinition(
        id="ars_erenos_pet",
        name_jp="エレノス",
        name_en="Erenos",
        race="lich",
        job="wizard",
        hostility="Friend",
        lv=200,
        chance=0,
        tiles=0,
        bio="f/1001/165/52",
        tag=["humanSpeak"],
        act_combat=[
            "actSummonUndead/50",
            "actLifeDrain/30",
            "actManaDrain/20",
            "actStaminaDrain/20",
            "actDeathZone/25",
            "miasma_Nether/40",
            "bolt_Darkness/30",
            "SpTeleportShort/30",
            "SpHeal/20",
        ],
        ai_param=[2, 60],
        detail_jp="禁書の居候。先輩（ヘカティア）に貸し出された。",
        detail_en="A freeloader in the tome. Lent out by her senior (Hecatia).",
        name_cn="艾雷诺斯",
        detail_cn="禁书中的寄居者。被前辈（赫卡提亚）借了出去。",
    ),
}
