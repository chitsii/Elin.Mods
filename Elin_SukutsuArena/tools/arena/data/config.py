# -*- coding: utf-8 -*-
"""
arena/data/config.py - Core Configuration (SSOT)

This module defines all core configuration for the Sukutsu Arena mod:
- Constants (PREFIX, MOD_NAME, etc.)
- Enums (Phase, Rank, etc.)
- Key classes (Keys, Actors, QuestIds)
- Flag values (FlagValues)
- Jump labels (JUMP_LABELS)
- Drama constants (DramaIds, DramaNames)
- BGM constants

This is the Single Source of Truth (SSOT) for configuration.

Flag design rules (Part 2 scaling):
- Prefer enum states over multiple boolean flags.
- Use session flags for temporary state; avoid persistence when possible.
- Before adding a new flag, check if it can be derived from quest completion.
"""

import os
import sys
from dataclasses import dataclass
from enum import Enum as PyEnum
from typing import Any, Dict, List, Optional

# Add cwl_quest_lib to path
_current_dir = os.path.dirname(os.path.abspath(__file__))
_arena_dir = os.path.dirname(_current_dir)
_tools_dir = os.path.dirname(_arena_dir)
if _tools_dir not in sys.path:
    sys.path.insert(0, _tools_dir)

# Import pydantic models from cwl_quest_lib
from cwl_quest_lib.core.config_models import (
    ActorDef,
    EnumDef,
    FlagCondition,
    FlagConfig,
    FlagDef,
    FlagType,
    ItemDef,
    ModConfig,
    NpcDef,
    QuestConfig,
    QuestDef,
    QuestType,
    RewardDef,
    RewardItemDef,
    StageDef,
)

# ============================================================================
# Constants
# ============================================================================

PREFIX = "chitsii.arena"
MOD_NAME = "Sukutsu Arena"
MOD_VERSION = "1.0.0"
AUTHOR = "Chitsii"

# Quest-related prefixes
QUEST_DONE_PREFIX = "sukutsu_quest_done_"


# ============================================================================
# Actors (Character IDs)
# ============================================================================

ACTORS = [
    ActorDef(
        actor_id="pc",
        name_jp="プレイヤー",
        name_en="Player",
        name_cn="玩家",
        description="プレイヤーキャラクター",
    ),
    ActorDef(
        actor_id="sukutsu_receptionist",
        name_jp="リリィ",
        name_en="Lily",
        name_cn="莉莉",
        description="アリーナの運営を管理している",
    ),
    ActorDef(
        actor_id="sukutsu_arena_master",
        name_jp="バルガス",
        name_en="Vargus",
        name_cn="巴尔加斯",
        description="アリーナマスター",
    ),
    ActorDef(
        actor_id="sukutsu_shady_merchant",
        name_jp="ゼク",
        name_en="Zek",
        name_cn="泽克",
        description="怪しい商人",
    ),
    ActorDef(
        actor_id="sukutsu_astaroth",
        name_jp="アスタロト",
        name_en="Astaroth",
        name_cn="阿斯塔罗特",
        description="グランドマスター",
    ),
    ActorDef(
        actor_id="sukutsu_null",
        name_jp="Nul",
        name_en="Nul",
        name_cn="Nul",
        description="暗殺人形",
    ),
    ActorDef(
        actor_id="sukutsu_cain",
        name_jp="カイン",
        name_en="Cain",
        name_cn="凯恩",
        description="バルガスの弟弟子、35年前に死亡",
    ),
    ActorDef(
        actor_id="sukutsu_trainer",
        name_jp="アイリス",
        name_en="Iris",
        name_cn="艾莉丝",
        description="トレーナー",
    ),
]


class Actors:
    """Actor ID constants."""

    PC = "pc"
    NARRATOR = "narrator"  # Elin組み込み（名前表示なしのナレーション用）
    LILY = "sukutsu_receptionist"
    BALGAS = "sukutsu_arena_master"
    ZEK = "sukutsu_shady_merchant"
    ASTAROTH = "sukutsu_astaroth"
    NUL = "sukutsu_null"
    CAIN = "sukutsu_cain"
    IRIS = "sukutsu_trainer"


# ============================================================================
# Enums
# ============================================================================

ENUMS = [
    EnumDef(
        name="Rank",
        values=[
            "UNRANKED",
            "G",
            "F",
            "E",
            "D",
            "C",
            "B",
            "A",
            "S",
            "SS",
            "SSS",
            "U",
            "Z",
            "GOD_SLAYER",
            "SINGULARITY",
            "VOID_KING",
        ],
        description="闘技場ランク（昇順）",
    ),
    EnumDef(
        name="Phase",
        values=[
            "PROLOGUE",
            "INITIATION",
            "RISING",
            "AWAKENING",
            "CONFRONTATION",
            "CLIMAX",
            "EPILOGUE",
            "POSTGAME",
        ],
        description="ストーリーフェーズ",
    ),
    EnumDef(
        name="BottleChoice", values=["REFUSED", "SWAPPED"], description="共鳴瓶の選択"
    ),
    EnumDef(
        name="KainSoulChoice",
        values=["RETURNED", "SOLD"],
        description="カインの魂の選択",
    ),
    EnumDef(
        name="BalgasChoice", values=["SPARED", "KILLED"], description="バルガス戦の選択"
    ),
    EnumDef(
        name="LilyBottleConfession",
        values=["CONFESSED", "BLAMED_ZEK", "DENIED"],
        description="瓶すり替え発覚時の告白",
    ),
    EnumDef(
        name="Ending",
        values=["RESCUE", "INHERIT", "USURP"],
        description="エンディング選択",
    ),
]


# ============================================================================
# Python Enums (for runtime use)
# ============================================================================


def _create_ordered_enum(name: str, values: list[str]) -> type:
    """Create a Python Enum with comparison operators."""
    members = {v: v.lower() for v in values}
    base_enum = PyEnum(name, members)

    def _get_order(self):
        return list(type(self)).index(self)

    def __ge__(self, other):
        if not isinstance(other, type(self)):
            return NotImplemented
        return self._get_order() >= other._get_order()

    def __gt__(self, other):
        if not isinstance(other, type(self)):
            return NotImplemented
        return self._get_order() > other._get_order()

    def __le__(self, other):
        if not isinstance(other, type(self)):
            return NotImplemented
        return self._get_order() <= other._get_order()

    def __lt__(self, other):
        if not isinstance(other, type(self)):
            return NotImplemented
        return self._get_order() < other._get_order()

    base_enum._get_order = _get_order
    base_enum.__ge__ = __ge__
    base_enum.__gt__ = __gt__
    base_enum.__le__ = __le__
    base_enum.__lt__ = __lt__

    return base_enum


# Generate Python enums from config
Phase = _create_ordered_enum(
    "Phase",
    [
        "PROLOGUE",
        "INITIATION",
        "RISING",
        "AWAKENING",
        "CONFRONTATION",
        "CLIMAX",
        "EPILOGUE",
        "POSTGAME",
    ],
)
Rank = _create_ordered_enum(
    "Rank",
    [
        "UNRANKED",
        "G",
        "F",
        "E",
        "D",
        "C",
        "B",
        "A",
        "S",
        "SS",
        "SSS",
        "U",
        "Z",
        "GOD_SLAYER",
        "SINGULARITY",
        "VOID_KING",
    ],
)
BottleChoice = PyEnum("BottleChoice", {v: v.lower() for v in ["REFUSED", "SWAPPED"]})
KainSoulChoice = PyEnum("KainSoulChoice", {v: v.lower() for v in ["RETURNED", "SOLD"]})
BalgasChoice = PyEnum("BalgasChoice", {v: v.lower() for v in ["SPARED", "KILLED"]})
LilyBottleConfession = PyEnum(
    "LilyBottleConfession",
    {v: v.lower() for v in ["CONFESSED", "BLAMED_ZEK", "DENIED"]},
)
Ending = PyEnum("Ending", {v: v.lower() for v in ["RESCUE", "INHERIT", "USURP"]})


# ============================================================================
# Keys Class (flag key shortcuts)
# ============================================================================


class Keys:
    """Quick access to full flag keys."""

    # Player state
    RANK = f"{PREFIX}.player.rank"
    CURRENT_PHASE = f"{PREFIX}.player.current_phase"
    BOSS_DAMAGE_RATE = f"{PREFIX}.player.boss_damage_rate"
    FUGITIVE_STATUS = f"{PREFIX}.player.fugitive_status"
    FUGITIVE = f"{PREFIX}.player.fugitive_status"  # Alias
    ENDING = f"{PREFIX}.player.ending"
    # Choices
    BOTTLE_CHOICE = f"{PREFIX}.player.bottle_choice"
    KAIN_SOUL_CHOICE = f"{PREFIX}.player.kain_soul_choice"
    BALGAS_KILLED = f"{PREFIX}.player.balgas_killed"
    LILY_BOTTLE_CONFESSION = f"{PREFIX}.player.lily_bottle_confession"
    # State
    BALGAS_BATTLE_COMPLETE = f"{PREFIX}.player.balgas_battle_complete"
    # Resurrection (Part 2)
    BALGAS_REVIVED = f"{PREFIX}.player.balgas_revived"
    CAIN_REVIVED = f"{PREFIX}.player.cain_revived"


# ============================================================================
# Quest IDs (Constants)
# ============================================================================


class QuestIds:
    """Quest ID constants."""

    # Main story
    OPENING = "01_opening"
    # Rank ups
    RANK_UP_G = "02_rank_up_G"
    RANK_UP_F = "04_rank_up_F"
    RANK_UP_E = "06_rank_up_E"
    RANK_UP_D = "10_rank_up_D"
    RANK_UP_C = "09_rank_up_C"
    RANK_UP_B = "11_rank_up_B"
    RANK_UP_A = "12_rank_up_A"
    RANK_UP_S = "15_vs_balgas"
    RANK_UP_S_BALGAS_SPARED = "15_vs_balgas_spared"
    RANK_UP_S_BALGAS_KILLED = "15_vs_balgas_killed"
    # Zek route
    ZEK_INTRO = "03_zek_intro"
    ZEK_STEAL_BOTTLE = "05_2_zek_steal_bottle"
    ZEK_STEAL_BOTTLE_ACCEPT = "05_2_zek_steal_bottle_accept"
    ZEK_STEAL_BOTTLE_REFUSE = "05_2_zek_steal_bottle_refuse"
    ZEK_STEAL_SOULGEM = "06_2_zek_steal_soulgem"
    ZEK_STEAL_SOULGEM_SELL = "06_2_zek_steal_soulgem_sell"
    ZEK_STEAL_SOULGEM_RETURN = "06_2_zek_steal_soulgem_return"
    # Character events
    LILY_EXPERIMENT = "05_1_lily_experiment"
    LILY_PRIVATE = "08_lily_private"
    LILY_REAL_NAME = "16_lily_real_name"
    BALGAS_TRAINING = "09_balgas_training"
    UPPER_EXISTENCE = "07_upper_existence"
    # Late story
    MAKUMA = "12_makuma"
    MAKUMA2 = "13_makuma2"
    VS_ASTAROTH = "17_vs_astaroth"
    LAST_BATTLE = "18_last_battle"
    # Postgame
    PG_02A_RESURRECTION_INTRO = "pg_02a_resurrection_intro"
    PG_02B_RESURRECTION_EXECUTION = "pg_02b_resurrection_execution"
    PG_03_SCROLL_SHOWCASE = "pg_03_scroll_showcase"


# ============================================================================
# Flag Values (for dialogFlags - integers only)
# ============================================================================


class FlagValues:
    """フラグ値定数（CWL dialogFlagsは整数のみ）"""

    FALSE = 0
    TRUE = 1

    class BottleChoice:
        REFUSED = 0
        SWAPPED = 1

    class KainSoulChoice:
        RETURNED = 0
        SOLD = 1

    class LilyBottleConfession:
        CONFESSED = 0
        BLAMED_ZEK = 1
        DENIED = 2

    class BalgasChoice:
        SPARED = 0
        KILLED = 1

    class Ending:
        RESCUE = 0
        INHERIT = 1
        USURP = 2

    class Phase:
        PROLOGUE = 0
        INITIATION = 1
        RISING = 2
        AWAKENING = 3
        CONFRONTATION = 4
        CLIMAX = 5
        EPILOGUE = 6
        POSTGAME = 7

    class Rank:
        UNRANKED = 0
        G = 1
        F = 2
        E = 3
        D = 4
        C = 5
        B = 6
        A = 7
        S = 8
        SS = 9


# ============================================================================
# Jump Labels (sync with C# JumpLabelMapping.cs)
# ============================================================================

JUMP_LABELS = {
    # Rank up start (11-17)
    "start_rank_g": 11,
    "start_rank_f": 12,
    "start_rank_e": 13,
    "start_rank_d": 14,
    "start_rank_c": 15,
    "start_rank_b": 16,
    "start_rank_a": 17,
    # Quest confirmations (same values)
    "quest_rank_up_g": 11,
    "quest_rank_up_f": 12,
    "quest_rank_up_e": 13,
    "quest_rank_up_d": 14,
    "quest_rank_up_c": 15,
    "quest_rank_up_b": 16,
    "quest_rank_up_a": 17,
    # Story quests (21-33)
    "quest_zek_intro": 21,
    "start_zek_intro": 21,
    "quest_lily_exp": 22,
    "start_lily_experiment": 22,
    "quest_zek_steal_bottle": 23,
    "start_zek_steal_bottle": 23,
    "quest_zek_steal_soulgem": 24,
    "start_zek_steal_soulgem": 24,
    "quest_upper_existence": 25,
    "quest_lily_private": 26,
    "start_lily_private": 26,
    "quest_balgas_training": 27,
    "quest_makuma": 28,
    "quest_makuma2": 29,
    "quest_vs_balgas": 30,
    "quest_lily_real_name": 31,
    "start_lily_real_name": 31,
    "quest_vs_astaroth": 32,
    "quest_last_battle": 33,
}


# ============================================================================
# Drama Constants
# ============================================================================


class DramaIds:
    """
    ドラマID定数クラス (短縮形)
    Excel生成時のファイル名生成に使用: f"drama_{ID}.xlsx"
    """

    # メインシナリオ
    SUKUTSU_OPENING = "sukutsu_opening"
    SUKUTSU_ARENA_MASTER = "sukutsu_arena_master"
    SUKUTSU_ASTAROTH = "sukutsu_astaroth"
    SUKUTSU_NULL = "sukutsu_null"

    # ランクアップドラマ
    RANK_UP_G = "rank_up_G"
    RANK_UP_F = "rank_up_F"
    RANK_UP_E = "rank_up_E"
    RANK_UP_D = "rank_up_D"
    RANK_UP_C = "rank_up_C"
    RANK_UP_B = "rank_up_B"
    RANK_UP_A = "rank_up_A"

    # キャラクター関連
    ZEK_INTRO = "zek_intro"
    ZEK_STEAL_BOTTLE = "zek_steal_bottle"
    ZEK_STEAL_SOULGEM = "zek_steal_soulgem"

    LILY_EXPERIMENT = "lily_experiment"
    LILY_PRIVATE = "lily_private"
    LILY_REAL_NAME = "lily_real_name"
    RESURRECTION_INTRO = "resurrection_intro"  # Part 2 - 02a
    RESURRECTION_EXECUTION = "resurrection_execution"  # Part 2 - 02b
    P2_03_SCROLL_SHOWCASE = "p2_03_scroll_showcase"  # Part 2 - 03

    BALGAS_TRAINING = "balgas_training"
    VS_BALGAS = "vs_balgas"

    # アイリス（トレーナー）関連
    IRIS_SENSE_TRAINING = "iris_sense_training"
    IRIS_LEG_TRAINING = "iris_leg_training"
    IRIS_HOTSPRING = "iris_hotspring"
    SUKUTSU_TRAINER = "sukutsu_trainer"

    # ストーリー進行
    UPPER_EXISTENCE = "upper_existence"
    MAKUMA = "makuma"
    MAKUMA2 = "makuma2"
    VS_ASTAROTH = "vs_astaroth"
    LAST_BATTLE = "last_battle"
    EPILOGUE = "epilogue"

    # デバッグ用
    DEBUG_BATTLE = "debug_battle"
    DEBUG_MENU = "debug_menu"


class DramaNames:
    """
    ドラマ名定数クラス (完全名)
    C#からdramaを呼び出す際に使用する完全なドラマ名
    """

    # メインシナリオ
    OPENING = f"drama_{DramaIds.SUKUTSU_OPENING}"
    ARENA_MASTER = f"drama_{DramaIds.SUKUTSU_ARENA_MASTER}"
    ASTAROTH = f"drama_{DramaIds.SUKUTSU_ASTAROTH}"
    NULL = f"drama_{DramaIds.SUKUTSU_NULL}"

    # ランクアップドラマ
    RANK_UP_G = f"drama_{DramaIds.RANK_UP_G}"
    RANK_UP_F = f"drama_{DramaIds.RANK_UP_F}"
    RANK_UP_E = f"drama_{DramaIds.RANK_UP_E}"
    RANK_UP_D = f"drama_{DramaIds.RANK_UP_D}"
    RANK_UP_C = f"drama_{DramaIds.RANK_UP_C}"
    RANK_UP_B = f"drama_{DramaIds.RANK_UP_B}"
    RANK_UP_A = f"drama_{DramaIds.RANK_UP_A}"

    # キャラクター関連
    ZEK_INTRO = f"drama_{DramaIds.ZEK_INTRO}"
    ZEK_STEAL_BOTTLE = f"drama_{DramaIds.ZEK_STEAL_BOTTLE}"
    ZEK_STEAL_SOULGEM = f"drama_{DramaIds.ZEK_STEAL_SOULGEM}"

    LILY_EXPERIMENT = f"drama_{DramaIds.LILY_EXPERIMENT}"
    LILY_PRIVATE = f"drama_{DramaIds.LILY_PRIVATE}"
    LILY_REAL_NAME = f"drama_{DramaIds.LILY_REAL_NAME}"
    RESURRECTION_INTRO = f"drama_{DramaIds.RESURRECTION_INTRO}"  # Part 2 - 02a
    RESURRECTION_EXECUTION = f"drama_{DramaIds.RESURRECTION_EXECUTION}"  # Part 2 - 02b
    P2_03_SCROLL_SHOWCASE = f"drama_{DramaIds.P2_03_SCROLL_SHOWCASE}"  # Part 2 - 03

    BALGAS_TRAINING = f"drama_{DramaIds.BALGAS_TRAINING}"
    VS_BALGAS = f"drama_{DramaIds.VS_BALGAS}"

    # アイリス（トレーナー）関連
    IRIS_SENSE_TRAINING = f"drama_{DramaIds.IRIS_SENSE_TRAINING}"
    IRIS_LEG_TRAINING = f"drama_{DramaIds.IRIS_LEG_TRAINING}"
    IRIS_HOTSPRING = f"drama_{DramaIds.IRIS_HOTSPRING}"
    TRAINER = f"drama_{DramaIds.SUKUTSU_TRAINER}"

    # ストーリー進行
    UPPER_EXISTENCE = f"drama_{DramaIds.UPPER_EXISTENCE}"
    MAKUMA = f"drama_{DramaIds.MAKUMA}"
    MAKUMA2 = f"drama_{DramaIds.MAKUMA2}"
    VS_ASTAROTH = f"drama_{DramaIds.VS_ASTAROTH}"
    LAST_BATTLE = f"drama_{DramaIds.LAST_BATTLE}"
    EPILOGUE = f"drama_{DramaIds.EPILOGUE}"

    # デバッグ用
    DEBUG_MENU = f"drama_{DramaIds.DEBUG_MENU}"


def get_drama_category(drama_id: str) -> str:
    """
    ドラマIDからカテゴリを自動判定

    Returns:
        'rank': ランクアップ試練関連
        'character': キャラクター個別イベント
        'story': ストーリー進行
    """
    # ランクアップ・対戦系
    if drama_id.startswith(("rank_up_", "vs_")):
        return "rank"
    # キャラクター個別
    if any(drama_id.startswith(p) for p in ("lily_", "zek_", "balgas_")):
        return "character"
    # それ以外はストーリー
    return "story"


# ドラマ表示名マッピング（デバッグメニュー用）
DRAMA_DISPLAY_NAMES = {
    DramaIds.SUKUTSU_OPENING: ("オープニング", "Opening"),
    DramaIds.SUKUTSU_ARENA_MASTER: ("アリーナマスター", "Arena Master"),
    DramaIds.SUKUTSU_ASTAROTH: ("アスタロト", "Astaroth"),
    DramaIds.SUKUTSU_NULL: ("ヌル", "Nul"),
    DramaIds.RANK_UP_G: ("ランクG昇格", "Rank G Trial"),
    DramaIds.RANK_UP_F: ("ランクF昇格", "Rank F Trial"),
    DramaIds.RANK_UP_E: ("ランクE昇格", "Rank E Trial"),
    DramaIds.RANK_UP_D: ("ランクD昇格", "Rank D Trial"),
    DramaIds.RANK_UP_C: ("ランクC昇格", "Rank C Trial"),
    DramaIds.RANK_UP_B: ("ランクB昇格", "Rank B Trial"),
    DramaIds.RANK_UP_A: ("ランクA昇格", "Rank A Trial"),
    DramaIds.VS_BALGAS: ("vsバルガス", "vs Vargus"),
    DramaIds.ZEK_INTRO: ("ゼク登場", "Zek Intro"),
    DramaIds.ZEK_STEAL_BOTTLE: ("ボトル交換", "Bottle Swap"),
    DramaIds.ZEK_STEAL_SOULGEM: ("魂宝石選択", "Soul Gem Choice"),
    DramaIds.LILY_EXPERIMENT: ("リリィ実験", "Lily Experiment"),
    DramaIds.LILY_PRIVATE: ("リリィ私室", "Lily Private"),
    DramaIds.LILY_REAL_NAME: ("リリィ真名", "Lily Real Name"),
    DramaIds.P2_03_SCROLL_SHOWCASE: ("大部屋の巻物お披露目", "Scroll Showcase"),
    DramaIds.RESURRECTION_INTRO: ("蘇りの儀式・導入", "Resurrection Intro"),
    DramaIds.RESURRECTION_EXECUTION: ("蘇りの儀式・実行", "Resurrection Execution"),
    DramaIds.BALGAS_TRAINING: ("バルガス訓練", "Vargus Training"),
    DramaIds.UPPER_EXISTENCE: ("上位存在", "Upper Existence"),
    DramaIds.MAKUMA: ("マクマ", "Makuma"),
    DramaIds.MAKUMA2: ("マクマ2", "Makuma 2"),
    DramaIds.VS_ASTAROTH: ("vsアスタロト", "vs Astaroth"),
    DramaIds.LAST_BATTLE: ("最終決戦", "Last Battle"),
    DramaIds.EPILOGUE: ("エピローグ", "Epilogue"),
}


# 全ドラマIDのリスト（バリデーション用）
ALL_DRAMA_IDS = [
    DramaIds.SUKUTSU_OPENING,
    DramaIds.SUKUTSU_ARENA_MASTER,
    DramaIds.SUKUTSU_ASTAROTH,
    DramaIds.SUKUTSU_NULL,
    DramaIds.RANK_UP_G,
    DramaIds.RANK_UP_F,
    DramaIds.RANK_UP_E,
    DramaIds.RANK_UP_D,
    DramaIds.RANK_UP_C,
    DramaIds.RANK_UP_B,
    DramaIds.RANK_UP_A,
    DramaIds.ZEK_INTRO,
    DramaIds.ZEK_STEAL_BOTTLE,
    DramaIds.ZEK_STEAL_SOULGEM,
    DramaIds.LILY_EXPERIMENT,
    DramaIds.LILY_PRIVATE,
    DramaIds.LILY_REAL_NAME,
    DramaIds.P2_03_SCROLL_SHOWCASE,
    DramaIds.RESURRECTION_INTRO,
    DramaIds.RESURRECTION_EXECUTION,
    DramaIds.BALGAS_TRAINING,
    DramaIds.VS_BALGAS,
    DramaIds.UPPER_EXISTENCE,
    DramaIds.MAKUMA,
    DramaIds.MAKUMA2,
    DramaIds.VS_ASTAROTH,
    DramaIds.LAST_BATTLE,
    DramaIds.EPILOGUE,
    DramaIds.DEBUG_MENU,
]


# ============================================================================
# BGM Constants
# ============================================================================

BGM_BASE_ID = 1000000

DEFAULT_BGM_CONFIG = {
    "allowMultiple": True,
    "bgmDataOptional": {
        "fadeIn": 0.1,
        "fadeOut": 0.5,
        "failDuration": 0.7,
        "failPitch": 0.12,
        "parts": [{"start": 0.5, "duration": 1.0}],
        "pitchDuration": 0.01,
    },
    "chance": 1.0,
    "pitch": 1.0,
    "reverbMix": 1.0,
    "type": "BGM",
    "volume": 0.5,
}


def get_bgm_id(filename: str, sorted_filenames: list[str]) -> int:
    """
    ファイル名からBGM IDを取得する。

    Args:
        filename: BGMファイル名（拡張子なし）
        sorted_filenames: ソート済みファイル名リスト

    Returns:
        BGM ID (1000000 + オフセット)
    """
    try:
        offset = sorted_filenames.index(filename)
        return BGM_BASE_ID + offset
    except ValueError:
        raise ValueError(f"Unknown BGM file: {filename}")


def create_bgm_json_data(bgm_id: int) -> dict:
    """
    BGM IDからJSON用のデータを作成する。

    Args:
        bgm_id: BGM ID

    Returns:
        JSON用の辞書
    """
    return {"id": bgm_id, **DEFAULT_BGM_CONFIG}


# ============================================================================
# Flags Definition
# ============================================================================

FLAGS = [
    # Player state
    FlagDef(
        key="player.rank",
        flag_type=FlagType.ENUM,
        enum_name="Rank",
        default="UNRANKED",
        description="現在の闘技場ランク",
    ),
    FlagDef(
        key="player.current_phase",
        flag_type=FlagType.ENUM,
        enum_name="Phase",
        default="PROLOGUE",
        description="現在のストーリーフェーズ",
    ),
    FlagDef(
        key="player.boss_damage_rate",
        flag_type=FlagType.INT,
        default=40,
        min_value=1,
        max_value=200,
        description="ボスの与ダメージ倍率(%)",
    ),
    FlagDef(
        key="player.fugitive_status",
        flag_type=FlagType.BOOL,
        default=False,
        description="逃亡者状態",
    ),
    FlagDef(
        key="player.ending",
        flag_type=FlagType.ENUM,
        enum_name="Ending",
        default=None,
        description="選択したエンディング",
    ),
    # Choice flags
    FlagDef(
        key="player.bottle_choice",
        flag_type=FlagType.ENUM,
        enum_name="BottleChoice",
        default=None,
        description="共鳴瓶すり替えの選択",
    ),
    FlagDef(
        key="player.kain_soul_choice",
        flag_type=FlagType.ENUM,
        enum_name="KainSoulChoice",
        default=None,
        description="カインの魂の選択",
    ),
    FlagDef(
        key="player.balgas_killed",
        flag_type=FlagType.ENUM,
        enum_name="BalgasChoice",
        default=None,
        description="バルガス戦での選択",
    ),
    FlagDef(
        key="player.lily_bottle_confession",
        flag_type=FlagType.ENUM,
        enum_name="LilyBottleConfession",
        default=None,
        description="瓶すり替え発覚時の告白",
    ),
    # State flags
    FlagDef(
        key="player.balgas_battle_complete",
        flag_type=FlagType.BOOL,
        default=False,
        description="バルガス戦完了",
    ),
    # Resurrection flags (Part 2)
    FlagDef(
        key="player.balgas_revived",
        flag_type=FlagType.BOOL,
        default=False,
        description="バルガス復活済み",
    ),
    FlagDef(
        key="player.cain_revived",
        flag_type=FlagType.BOOL,
        default=False,
        description="カイン復活済み",
    ),
]

FLAG_CONFIG = FlagConfig(enums=ENUMS, flags=FLAGS)


# ============================================================================
# Session Flags (non-persistent, dialog-based)
# ============================================================================

# Session flag prefix (no chitsii.arena prefix - these use dialogFlags directly)
SESSION_PREFIX = "sukutsu_"


@dataclass
class SessionFlagDef:
    """Session flag definition for C# code generation"""

    key: str
    description: str = ""

    @property
    def full_key(self) -> str:
        return f"{SESSION_PREFIX}{self.key}"


SESSION_FLAGS = [
    # Quest availability (set by CheckAvailableQuestsCommand)
    SessionFlagDef(key="available_quest_count", description="利用可能クエスト数"),
    # Resurrection materials check (set by CheckResurrectionMaterialsCommand)
    SessionFlagDef(key="has_all_materials", description="全素材所持"),
    SessionFlagDef(key="elixir_count", description="エリクシル所持数"),
    SessionFlagDef(key="loveplus_count", description="産卵薬所持数"),
    SessionFlagDef(key="chicken_count", description="鶏パーティ数"),
    SessionFlagDef(key="consume_success", description="素材消費成功フラグ"),
    SessionFlagDef(key="has_rank_up", description="ランクアップクエスト有無"),
    SessionFlagDef(key="has_character_event", description="キャラクターイベント有無"),
    SessionFlagDef(key="has_sub_quest", description="サブクエスト有無"),
    SessionFlagDef(key="top_quest_id", description="最優先クエストID"),
    SessionFlagDef(key="next_rank_up", description="次ランクアップ値"),
    # Player state
    SessionFlagDef(key="gladiator", description="闘技者登録フラグ"),
    SessionFlagDef(key="opening_seen", description="オープニング視聴済み"),
    # Battle results
    SessionFlagDef(key="arena_result", description="アリーナ戦闘結果"),
    SessionFlagDef(key="quest_battle", description="クエスト戦闘フラグ"),
    SessionFlagDef(key="is_quest_battle_result", description="クエスト戦闘結果フラグ"),
    SessionFlagDef(key="is_rank_up_result", description="ランクアップ戦闘結果フラグ"),
    # Dialog control
    SessionFlagDef(key="auto_dialog", description="自動ダイアログ対象UID"),
    SessionFlagDef(
        key="direct_drama", description="直接ドラマ開始ID（戦闘後にNPC対話をスキップ）"
    ),
    # Quest state
    SessionFlagDef(key="quest_found", description="クエスト発見フラグ"),
    SessionFlagDef(key="quest_target_name", description="クエストターゲット名"),
    SessionFlagDef(key="quest_active_", description="クエストアクティブプレフィックス"),
    SessionFlagDef(key="rank_up_trial", description="Rank-up trial type (session)"),
    SessionFlagDef(key="arena_stage", description="Arena stage progression (session)"),
    SessionFlagDef(
        key="lily_contract_type", description="Lily contract type (session)"
    ),
    SessionFlagDef(
        key="reroll_result", description="Random battle reroll result (session)"
    ),
    SessionFlagDef(
        key="last_random_day", description="Last random battle day (session)"
    ),
]


class SessionKeys:
    """Session flag key constants (quick access)."""

    AVAILABLE_QUEST_COUNT = f"{SESSION_PREFIX}available_quest_count"
    # Resurrection materials
    HAS_ALL_MATERIALS = f"{SESSION_PREFIX}has_all_materials"
    ELIXIR_COUNT = f"{SESSION_PREFIX}elixir_count"
    LOVEPLUS_COUNT = f"{SESSION_PREFIX}loveplus_count"
    CHICKEN_COUNT = f"{SESSION_PREFIX}chicken_count"
    CONSUME_SUCCESS = f"{SESSION_PREFIX}consume_success"
    HAS_RANK_UP = f"{SESSION_PREFIX}has_rank_up"
    HAS_CHARACTER_EVENT = f"{SESSION_PREFIX}has_character_event"
    HAS_SUB_QUEST = f"{SESSION_PREFIX}has_sub_quest"
    TOP_QUEST_ID = f"{SESSION_PREFIX}top_quest_id"
    NEXT_RANK_UP = f"{SESSION_PREFIX}next_rank_up"
    GLADIATOR = f"{SESSION_PREFIX}gladiator"
    OPENING_SEEN = f"{SESSION_PREFIX}opening_seen"
    ARENA_RESULT = f"{SESSION_PREFIX}arena_result"
    QUEST_BATTLE = f"{SESSION_PREFIX}quest_battle"
    IS_QUEST_BATTLE_RESULT = f"{SESSION_PREFIX}is_quest_battle_result"
    IS_RANK_UP_RESULT = f"{SESSION_PREFIX}is_rank_up_result"
    RANK_UP_TRIAL = f"{SESSION_PREFIX}rank_up_trial"
    ARENA_STAGE = f"{SESSION_PREFIX}arena_stage"
    AUTO_DIALOG = f"{SESSION_PREFIX}auto_dialog"
    DIRECT_DRAMA = f"{SESSION_PREFIX}direct_drama"
    LILY_CONTRACT_TYPE = f"{SESSION_PREFIX}lily_contract_type"
    REROLL_RESULT = f"{SESSION_PREFIX}reroll_result"
    LAST_RANDOM_DAY = f"{SESSION_PREFIX}last_random_day"
    QUEST_FOUND = f"{SESSION_PREFIX}quest_found"
    QUEST_TARGET_NAME = f"{SESSION_PREFIX}quest_target_name"
    QUEST_ACTIVE_PREFIX = f"{SESSION_PREFIX}quest_active_"


def get_all_session_flags() -> list[SessionFlagDef]:
    """Get all session flag definitions for C# code generation"""
    return SESSION_FLAGS


# ============================================================================
# Legacy Flag Types (for generate_flags.py compatibility)
# ============================================================================


@dataclass
class EnumFlag:
    """Enum flag definition (for C# code generation)"""

    key: str
    enum_type: type
    default: Any = None
    description: str = ""

    @property
    def full_key(self) -> str:
        return f"{PREFIX}.{self.key}"


@dataclass
class IntFlag:
    """Integer flag definition"""

    key: str
    default: int = 0
    min_value: int = 0
    max_value: int = 100
    description: str = ""

    @property
    def full_key(self) -> str:
        return f"{PREFIX}.{self.key}"


@dataclass
class BoolFlag:
    """Boolean flag definition"""

    key: str
    default: bool = False
    description: str = ""

    @property
    def full_key(self) -> str:
        return f"{PREFIX}.{self.key}"


@dataclass
class StringFlag:
    """String flag definition"""

    key: str
    default: Optional[str] = None
    description: str = ""

    @property
    def full_key(self) -> str:
        return f"{PREFIX}.{self.key}"


# ============================================================================
# Helper Functions (for generate_flags.py compatibility)
# ============================================================================

# Map enum names to Python enum types
_ENUM_MAP = {
    "Rank": Rank,
    "Phase": Phase,
    "BottleChoice": BottleChoice,
    "KainSoulChoice": KainSoulChoice,
    "BalgasChoice": BalgasChoice,
    "LilyBottleConfession": LilyBottleConfession,
    "Ending": Ending,
}


def get_all_enums() -> list:
    """Get all enum types for C# code generation"""
    return list(_ENUM_MAP.values())


def get_all_flags() -> list:
    """Get all flag definitions for C# code generation"""
    flags = []
    for flag_def in FLAGS:
        if flag_def.flag_type == FlagType.ENUM:
            enum_type = _ENUM_MAP.get(flag_def.enum_name)
            flags.append(
                EnumFlag(
                    key=flag_def.key,
                    enum_type=enum_type,
                    default=flag_def.default,
                    description=flag_def.description or "",
                )
            )
        elif flag_def.flag_type == FlagType.INT:
            flags.append(
                IntFlag(
                    key=flag_def.key,
                    default=flag_def.default or 0,
                    min_value=flag_def.min_value or 0,
                    max_value=flag_def.max_value or 100,
                    description=flag_def.description or "",
                )
            )
        elif flag_def.flag_type == FlagType.BOOL:
            flags.append(
                BoolFlag(
                    key=flag_def.key,
                    default=flag_def.default or False,
                    description=flag_def.description or "",
                )
            )
        elif flag_def.flag_type == FlagType.STRING:
            flags.append(
                StringFlag(
                    key=flag_def.key,
                    default=flag_def.default,
                    description=flag_def.description or "",
                )
            )
    return flags


# ============================================================================
# Re-exports for cwl_quest_lib models
# ============================================================================

# These are re-exported for use elsewhere in the arena package
__all__ = [
    # Constants
    "PREFIX",
    "MOD_NAME",
    "MOD_VERSION",
    "AUTHOR",
    "QUEST_DONE_PREFIX",
    "SESSION_PREFIX",
    # Classes
    "Keys",
    "SessionKeys",
    "Actors",
    "QuestIds",
    "FlagValues",
    # Enums
    "Phase",
    "Rank",
    "BottleChoice",
    "KainSoulChoice",
    "BalgasChoice",
    "LilyBottleConfession",
    "Ending",
    # Data
    "ACTORS",
    "ENUMS",
    "FLAGS",
    "FLAG_CONFIG",
    "SESSION_FLAGS",
    "JUMP_LABELS",
    # Drama
    "DramaIds",
    "DramaNames",
    "DRAMA_DISPLAY_NAMES",
    "ALL_DRAMA_IDS",
    "get_drama_category",
    # BGM
    "BGM_BASE_ID",
    "DEFAULT_BGM_CONFIG",
    "get_bgm_id",
    "create_bgm_json_data",
    # Legacy flag types
    "EnumFlag",
    "IntFlag",
    "BoolFlag",
    "StringFlag",
    "SessionFlagDef",
    # Helper functions
    "get_all_enums",
    "get_all_flags",
    "get_all_session_flags",
    # cwl_quest_lib models (re-exported)
    "ModConfig",
    "FlagConfig",
    "EnumDef",
    "FlagDef",
    "FlagType",
    "QuestConfig",
    "QuestDef",
    "QuestType",
    "FlagCondition",
    "ItemDef",
    "NpcDef",
    "RewardDef",
    "RewardItemDef",
    "StageDef",
    "ActorDef",
]
