# -*- coding: utf-8 -*-
"""
arena/data/battle.py - Battle Stage and Battle Flag Definitions

This module defines:
- Battle stages (rank-up trials, normal stages, debug stages)
- Battle flags (quest battle types, rank-up trial types)
"""

from dataclasses import dataclass, field
from typing import List, Optional, Dict, Any, TYPE_CHECKING
from enum import Enum, IntEnum
import json
import os

from arena.data.config import SessionKeys


# ============================================================================
# Battle Stage Enums and Classes
# ============================================================================


class Rarity(Enum):
    NORMAL = "Normal"
    SUPERIOR = "Superior"
    LEGENDARY = "Legendary"
    MYTHICAL = "Mythical"


class SpawnPosition(Enum):
    CENTER = "center"
    RANDOM = "random"
    FIXED = "fixed"


@dataclass
class GimmickConfig:
    """ギミック設定"""

    gimmick_type: str
    interval: float = 5.0
    damage: int = 15
    radius: int = 3
    start_delay: float = 3.0
    effect_name: str = "explosion"
    sound_name: str = "explosion"
    enable_escalation: bool = True
    escalation_rate: float = 0.9
    min_interval: float = 0.5
    max_radius: int = 8
    radius_growth_interval: float = 30.0
    enable_item_drop: bool = True
    item_drop_chance: float = 0.15
    blast_radius: int = 2
    direct_hit_chance: float = 0.4
    explosion_count: int = 1

    def to_dict(self) -> Dict[str, Any]:
        return {
            "gimmickType": self.gimmick_type,
            "interval": self.interval,
            "damage": self.damage,
            "radius": self.radius,
            "startDelay": self.start_delay,
            "effectName": self.effect_name,
            "soundName": self.sound_name,
            "enableEscalation": self.enable_escalation,
            "escalationRate": self.escalation_rate,
            "minInterval": self.min_interval,
            "maxRadius": self.max_radius,
            "radiusGrowthInterval": self.radius_growth_interval,
            "enableItemDrop": self.enable_item_drop,
            "itemDropChance": self.item_drop_chance,
            "blastRadius": self.blast_radius,
            "directHitChance": self.direct_hit_chance,
            "explosionCount": self.explosion_count,
        }


@dataclass
class EnemyConfig:
    """敵の設定"""

    chara_id: str
    rarity: str = "Normal"
    position: str = "random"
    position_x: int = 0
    position_z: int = 0
    is_boss: bool = False
    count: int = 1

    def to_dict(self) -> Dict[str, Any]:
        return {
            "charaId": self.chara_id,
            "rarity": self.rarity,
            "position": self.position,
            "positionX": self.position_x,
            "positionZ": self.position_z,
            "isBoss": self.is_boss,
            "count": self.count,
        }


@dataclass
class BattleStage:
    """バトルステージの設定"""

    stage_id: str
    display_name_jp: str
    display_name_en: str
    zone_type: str = "field_fine"
    biome: str = ""
    bgm_battle: str = ""
    bgm_victory: str = "BGM/Fanfare_Audience"
    reward_plat: int = 10
    enemies: List[EnemyConfig] = field(default_factory=list)
    gimmicks: List[GimmickConfig] = field(default_factory=list)
    description_jp: str = ""
    description_en: str = ""

    def to_dict(self) -> Dict[str, Any]:
        return {
            "stageId": self.stage_id,
            "displayNameJp": self.display_name_jp,
            "displayNameEn": self.display_name_en,
            "zoneType": self.zone_type,
            "biome": self.biome,
            "bgmBattle": self.bgm_battle,
            "bgmVictory": self.bgm_victory,
            "rewardPlat": self.reward_plat,
            "enemies": [e.to_dict() for e in self.enemies],
            "gimmicks": [g.to_dict() for g in self.gimmicks],
            "descriptionJp": self.description_jp,
            "descriptionEn": self.description_en,
        }


# ============================================================================
# Battle Flag Types
# ============================================================================


class QuestBattleType(IntEnum):
    """クエストバトルの種類"""

    NONE = 0
    UPPER_EXISTENCE = 1
    VS_BALGAS = 2
    LAST_BATTLE = 3
    BALGAS_TRAINING = 4


class RankUpTrialType(IntEnum):
    """ランクアップ試験の種類"""

    NONE = 0
    RANK_G = 1
    RANK_F = 2
    RANK_E = 3
    RANK_D = 4
    RANK_C = 5
    RANK_B = 6
    RANK_A = 7


if TYPE_CHECKING:
    from arena.builders.drama import DramaBuilder


class QuestBattleFlags:
    """クエストバトル用フラグ管理"""

    FLAG_NAME = SessionKeys.QUEST_BATTLE
    RESULT_FLAG = SessionKeys.IS_QUEST_BATTLE_RESULT

    # 定数（後方互換）
    NONE = QuestBattleType.NONE
    UPPER_EXISTENCE = QuestBattleType.UPPER_EXISTENCE
    VS_BALGAS = QuestBattleType.VS_BALGAS
    LAST_BATTLE = QuestBattleType.LAST_BATTLE
    BALGAS_TRAINING = QuestBattleType.BALGAS_TRAINING

    QUEST_MAPPING: Dict[QuestBattleType, str] = {
        QuestBattleType.UPPER_EXISTENCE: "07_upper_existence",
        QuestBattleType.VS_BALGAS: "15_vs_balgas",
        QuestBattleType.LAST_BATTLE: "18_last_battle",
        QuestBattleType.BALGAS_TRAINING: "09_balgas_training",
    }

    @classmethod
    def set_for_battle(
        cls, builder: "DramaBuilder", battle_type: QuestBattleType
    ) -> "DramaBuilder":
        return builder.set_flag(cls.FLAG_NAME, int(battle_type)).set_flag(
            cls.RESULT_FLAG, 1
        )

    @classmethod
    def clear(cls, builder: "DramaBuilder") -> "DramaBuilder":
        return builder.set_flag(cls.FLAG_NAME, int(cls.NONE))

    @classmethod
    def get_switch_cases(
        cls, labels: Dict[QuestBattleType, str], fallback: str
    ) -> List[str]:
        cases = [fallback]
        for battle_type in [
            cls.UPPER_EXISTENCE,
            cls.VS_BALGAS,
            cls.LAST_BATTLE,
            cls.BALGAS_TRAINING,
        ]:
            cases.append(labels.get(battle_type, fallback))
        return cases


class RankUpTrialFlags:
    """ランクアップ試験用フラグ管理"""

    FLAG_NAME = SessionKeys.RANK_UP_TRIAL
    RESULT_FLAG = SessionKeys.IS_RANK_UP_RESULT

    # 定数（後方互換）
    NONE = RankUpTrialType.NONE
    RANK_G = RankUpTrialType.RANK_G
    RANK_F = RankUpTrialType.RANK_F
    RANK_E = RankUpTrialType.RANK_E
    RANK_D = RankUpTrialType.RANK_D
    RANK_C = RankUpTrialType.RANK_C
    RANK_B = RankUpTrialType.RANK_B
    RANK_A = RankUpTrialType.RANK_A

    RANK_MAPPING: Dict[str, RankUpTrialType] = {
        "G": RankUpTrialType.RANK_G,
        "F": RankUpTrialType.RANK_F,
        "E": RankUpTrialType.RANK_E,
        "D": RankUpTrialType.RANK_D,
        "C": RankUpTrialType.RANK_C,
        "B": RankUpTrialType.RANK_B,
        "A": RankUpTrialType.RANK_A,
    }

    @classmethod
    def get_trial_type(cls, rank: str) -> RankUpTrialType:
        return cls.RANK_MAPPING.get(rank.upper(), cls.NONE)

    @classmethod
    def set_for_trial(cls, builder: "DramaBuilder", rank: str) -> "DramaBuilder":
        trial_type = cls.get_trial_type(rank)
        return builder.set_flag(cls.FLAG_NAME, int(trial_type))

    @classmethod
    def clear(cls, builder: "DramaBuilder") -> "DramaBuilder":
        return builder.set_flag(cls.FLAG_NAME, int(cls.NONE))

    @classmethod
    def get_switch_cases(
        cls, labels: Dict[RankUpTrialType, str], fallback: str
    ) -> List[str]:
        cases = [fallback]
        for trial_type in [
            cls.RANK_G,
            cls.RANK_F,
            cls.RANK_E,
            cls.RANK_D,
            cls.RANK_C,
            cls.RANK_B,
            cls.RANK_A,
        ]:
            cases.append(labels.get(trial_type, fallback))
        cases.append(fallback)
        return cases


# ============================================================================
# Battle Stage Definitions
# ============================================================================

RANK_UP_STAGES: Dict[str, BattleStage] = {
    "rank_g_trial": BattleStage(
        stage_id="rank_g_trial",
        display_name_jp="屑肉の洗礼",
        display_name_en="Baptism of Scrap Meat",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_RankG_VoidOoze",
        reward_plat=5,
        enemies=[
            EnemyConfig("sukutsu_void_ooze", count=10),
        ],
        description_jp="混沌の落とし子たち——彼らのブレスは魂を蝕む。耐性なくして生き残れない。",
        description_en="Children of Chaos—their breath corrodes the soul. Without resistance, you cannot survive.",
    ),
    "rank_f_trial": BattleStage(
        stage_id="rank_f_trial",
        display_name_jp="霜牙の試練",
        display_name_en="Trial of the Frostfang",
        zone_type="field_snow",
        biome="Snow",
        bgm_battle="BGM/Battle_RankE_Ice",
        reward_plat=10,
        enemies=[
            EnemyConfig("sukutsu_frost_hound", rarity="Legendary", count=10),
        ],
        gimmicks=[
            GimmickConfig(
                gimmick_type="elemental_scar",
                interval=8.0,
                start_delay=5.0,
                enable_escalation=True,
                escalation_rate=0.85,
                min_interval=3.0,
            ),
        ],
        description_jp="姿なき牙——見えぬ敵に怯えるな。透明視を得よ、さもなくば凍え死ぬのみ。",
        description_en="The Invisible Fang—do not fear an unseen enemy. Gain true sight, or freeze to death.",
    ),
    "rank_e_trial": BattleStage(
        stage_id="rank_e_trial",
        display_name_jp="錆びついた英雄",
        display_name_en="The Rusty Hero",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_Kain_Requiem",
        reward_plat=20,
        enemies=[
            EnemyConfig(
                "sukutsu_kain_ghost", rarity="Legendary", is_boss=True
            ),
        ],
        description_jp="鉄血団の元副団長——バルガスが息子のように愛した男。",
        description_en="Former vice-captain of the Iron Blood Corps—the man Vargus loved like a son.",
    ),
    "rank_d_trial": BattleStage(
        stage_id="rank_d_trial",
        display_name_jp="観客の代弁者",
        display_name_en="Voice of the Audience",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_RankD_Chaos",
        reward_plat=30,
        enemies=[
            EnemyConfig("sukutsu_greed", rarity="Legendary", is_boss=True),
            EnemyConfig("sukutsu_metal_putty", rarity="Superior"),
            EnemyConfig("sukutsu_metal_putty", rarity="Superior"),
        ],
        gimmicks=[
            GimmickConfig(
                gimmick_type="audience_interference",
                interval=3.0,
                damage=25,
                radius=3,
                start_delay=0.5,
                enable_escalation=True,
                escalation_rate=0.85,
                min_interval=0.5,
                max_radius=6,
                radius_growth_interval=20.0,
                enable_item_drop=True,
                item_drop_chance=0.12,
                blast_radius=2,
                direct_hit_chance=0.35,
                explosion_count=1,
            ),
        ],
        description_jp="観客の「注目」に魅せられ、その力を取り込んだ傀儡——グリード。",
        description_en="Greed—a puppet who absorbed the audience's power.",
    ),
    "rank_c_trial": BattleStage(
        stage_id="rank_c_trial",
        display_name_jp="朱砂食い",
        display_name_en="Cinnabar Eater",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_RankC_Heroic_Lament",
        reward_plat=50,
        enemies=[
            EnemyConfig("sukutsu_crow_shadow", rarity="Legendary"),
            EnemyConfig("sukutsu_raven_blade", rarity="Legendary"),
            EnemyConfig(
                "sukutsu_karasu_venom", rarity="Legendary", is_boss=True
            ),
        ],
        description_jp="アリーナに巣食う「鴉」たち——かつての挑戦者だった者たちが、今は番人として立ちはだかる。",
        description_en="The 'Ravens' that nest in the arena—former challengers who now stand as guardians.",
    ),
    "rank_b_trial": BattleStage(
        stage_id="rank_b_trial",
        display_name_jp="虚無の処刑人",
        display_name_en="Void Executioner Null",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_Null_Assassin",
        reward_plat=80,
        enemies=[
            EnemyConfig(
                "sukutsu_null_enemy", rarity="Mythical", is_boss=True
            ),
        ],
        description_jp="「神の孵化場」計画の失敗作——暗殺人形ヌル。",
        description_en="A failure of the 'God Hatchery' project—the assassin doll Null.",
    ),
    "rank_a_trial": BattleStage(
        stage_id="rank_a_trial",
        display_name_jp="戦鬼",
        display_name_en="War Demon",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_Shadow_Self",
        reward_plat=120,
        enemies=[
            EnemyConfig(
                "sukutsu_shadow_self", rarity="Mythical", is_boss=True
            ),
        ],
        description_jp="観客の「注目」が生み出した、お前自身の影。",
        description_en="A shadow born from the audience's 'attention'—it knows everything about you.",
    ),
    "rank_s_trial": BattleStage(
        stage_id="rank_s_trial",
        display_name_jp="竜断ちへの道",
        display_name_en="Path to Dragon Slayer",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_Balgas_Prime",
        reward_plat=200,
        enemies=[
            EnemyConfig(
                "sukutsu_balgas_prime", rarity="Mythical", is_boss=True
            ),
        ],
        description_jp="「若返りの薬」を服用し、全盛期の力を取り戻したバルガス。",
        description_en="Vargus restored to his prime by the 'Rejuvenation Potion.'",
    ),
    "final_astaroth": BattleStage(
        stage_id="final_astaroth",
        display_name_jp="竜神との対峙",
        display_name_en="Confrontation with the Dragon God",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_Astaroth_Phase1",
        bgm_victory="BGM/Final_Liberation",
        reward_plat=500,
        enemies=[
            EnemyConfig(
                "sukutsu_astaroth", rarity="Mythical", is_boss=True
            ),
        ],
        description_jp="イルヴァの神々と同格の竜神。",
        description_en="A dragon god on par with the gods of Ilva.",
    ),
}


NORMAL_STAGES: Dict[str, BattleStage] = {
    "stage_1": BattleStage(
        stage_id="stage_1",
        display_name_jp="森の狼",
        display_name_en="Forest Wolf",
        zone_type="field_fine",
        reward_plat=10,
        enemies=[EnemyConfig("wolf")],
    ),
    "stage_2": BattleStage(
        stage_id="stage_2",
        display_name_jp="ケンタウロス",
        display_name_en="Centaur",
        zone_type="field_fine",
        reward_plat=20,
        enemies=[EnemyConfig("centaur", rarity="Superior")],
    ),
    "stage_3": BattleStage(
        stage_id="stage_3",
        display_name_jp="ミノタウロス",
        display_name_en="Minotaur",
        zone_type="field_fine",
        reward_plat=30,
        enemies=[EnemyConfig("minotaur", rarity="Superior")],
    ),
    "stage_4": BattleStage(
        stage_id="stage_4",
        display_name_jp="竜との対峙",
        display_name_en="Dragon Confrontation",
        zone_type="field_fine",
        reward_plat=50,
        enemies=[EnemyConfig("dragon", rarity="Legendary", is_boss=True)],
    ),
    "balgas_training_battle": BattleStage(
        stage_id="balgas_training_battle",
        display_name_jp="戦士の哲学",
        display_name_en="Warrior's Philosophy",
        zone_type="field_fine",
        bgm_battle="BGM/Battle_Kain_Requiem",
        reward_plat=0,
        enemies=[
            EnemyConfig(
                "sukutsu_balgas_training", rarity="Legendary", is_boss=True
            )
        ],
        description_jp="「俺の足を一歩でも動かしてみせろ」——バルガスによる特別訓練。",
        description_en="'Try to make me move even one step.'—Special training by Vargus.",
    ),
    "upper_existence_battle": BattleStage(
        stage_id="upper_existence_battle",
        display_name_jp="見えざる観客の供物",
        display_name_en="Offering to the Unseen Audience",
        zone_type="field_fine",
        bgm_battle="BGM/ProgressiveDance",
        reward_plat=15,
        enemies=[
            EnemyConfig(
                "sukutsu_metal_putty", rarity="Superior", is_boss=True
            ),
            EnemyConfig("sukutsu_void_ooze", count=3),
        ],
        gimmicks=[
            GimmickConfig(
                gimmick_type="audience_interference",
                interval=3.5,
                damage=20,
                radius=3,
                start_delay=0.5,
                enable_escalation=True,
                escalation_rate=0.88,
                min_interval=0.7,
                max_radius=5,
                radius_growth_interval=18.0,
                enable_item_drop=True,
                item_drop_chance=0.15,
                blast_radius=2,
                direct_hit_chance=0.35,
                explosion_count=1,
            ),
        ],
        description_jp="観客の「注目」が初めて向けられる戦い。",
        description_en="The first battle where the audience's 'attention' focuses on you.",
    ),
}


DEBUG_STAGES: Dict[str, BattleStage] = {
    **RANK_UP_STAGES,
    **NORMAL_STAGES,
    "debug_weak": BattleStage(
        stage_id="debug_weak",
        display_name_jp="[DEBUG] 弱い敵",
        display_name_en="[DEBUG] Weak Enemy",
        zone_type="field_fine",
        reward_plat=1,
        enemies=[EnemyConfig("putty")],
        description_jp="デバッグ用：レベル1のパティ",
    ),
    "debug_strong": BattleStage(
        stage_id="debug_strong",
        display_name_jp="[DEBUG] 強い敵",
        display_name_en="[DEBUG] Strong Enemy",
        zone_type="field_fine",
        reward_plat=100,
        enemies=[EnemyConfig("dragon", rarity="Mythical", is_boss=True)],
        description_jp="デバッグ用：レベル100の神話級ドラゴン",
    ),
    "debug_horde": BattleStage(
        stage_id="debug_horde",
        display_name_jp="[DEBUG] 敵の群れ",
        display_name_en="[DEBUG] Enemy Horde",
        zone_type="field_fine",
        reward_plat=50,
        enemies=[EnemyConfig("goblin", count=10)],
        description_jp="デバッグ用：10体のゴブリン",
    ),
    "debug_gimmick": BattleStage(
        stage_id="debug_gimmick",
        display_name_jp="[DEBUG] ギミックテスト",
        display_name_en="[DEBUG] Gimmick Test",
        zone_type="field_fine",
        reward_plat=10,
        enemies=[EnemyConfig("putty", count=3)],
        gimmicks=[
            GimmickConfig(
                gimmick_type="audience_interference",
                interval=2.0,
                damage=10,
                radius=2,
                start_delay=1.0,
                enable_escalation=True,
                escalation_rate=0.80,
                min_interval=0.3,
                max_radius=10,
                radius_growth_interval=10.0,
                enable_item_drop=True,
                item_drop_chance=0.20,
                blast_radius=3,
                direct_hit_chance=0.6,
                explosion_count=2,
            ),
        ],
        description_jp="デバッグ用：高命中率ギミック確認",
        description_en="Debug: High hit rate gimmick test",
    ),
}


# ============================================================================
# JSON Export Functions
# ============================================================================


def _apply_debug_overrides(stages: Dict[str, BattleStage]) -> Dict[str, Any]:
    """DEBUGビルド用: 互換のため同一内容を返す（LvはSourceCharaで管理）"""
    result = {}
    for stage_id, stage in stages.items():
        stage_dict = stage.to_dict()
        result[stage_id] = stage_dict
    return result


def export_stages_to_json(output_path: str, debug_mode: bool = False):
    """ステージ定義をJSONにエクスポート"""
    if debug_mode:
        data = {
            "rankUpStages": _apply_debug_overrides(RANK_UP_STAGES),
            "normalStages": _apply_debug_overrides(NORMAL_STAGES),
            "debugStages": _apply_debug_overrides(DEBUG_STAGES),
        }
        print("[DEBUG MODE] No enemy level override (levels are defined in SourceChara)")
    else:
        data = {
            "rankUpStages": {k: v.to_dict() for k, v in RANK_UP_STAGES.items()},
            "normalStages": {k: v.to_dict() for k, v in NORMAL_STAGES.items()},
            "debugStages": {k: v.to_dict() for k, v in DEBUG_STAGES.items()},
        }

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    print(f"Exported battle stages to: {output_path}")


def get_all_stage_ids() -> List[str]:
    """全ステージIDのリストを取得"""
    return list(DEBUG_STAGES.keys())


def get_stage(stage_id: str) -> Optional[BattleStage]:
    """ステージIDからステージ定義を取得"""
    return DEBUG_STAGES.get(stage_id)
