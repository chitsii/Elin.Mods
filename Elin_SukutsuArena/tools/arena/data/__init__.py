# -*- coding: utf-8 -*-
"""
arena/data - Pure Data Definitions

This package contains all data definitions for the Sukutsu Arena mod.
No business logic - just data.

Modules:
- config: Core configuration (flags, enums, actors, constants)
- quests: Quest definitions and rewards
- items: Custom item definitions
- battle: Battle stages and battle flags
- types: Arena-specific data types
"""

# ============================================================================
# Re-export from config
# ============================================================================
from arena.data.config import (
    # Constants
    PREFIX,
    MOD_NAME,
    MOD_VERSION,
    AUTHOR,
    QUEST_DONE_PREFIX,
    SESSION_PREFIX,
    # Classes
    Keys,
    SessionKeys,
    Actors,
    QuestIds,
    FlagValues,
    # Enums
    Phase,
    Rank,
    BottleChoice,
    KainSoulChoice,
    BalgasChoice,
    LilyBottleConfession,
    Ending,
    # Data collections
    ACTORS,
    ENUMS,
    FLAGS,
    FLAG_CONFIG,
    SESSION_FLAGS,
    JUMP_LABELS,
    # Drama
    DramaIds,
    DramaNames,
    DRAMA_DISPLAY_NAMES,
    ALL_DRAMA_IDS,
    get_drama_category,
    # BGM
    BGM_BASE_ID,
    DEFAULT_BGM_CONFIG,
    get_bgm_id,
    create_bgm_json_data,
    # Legacy flag types
    EnumFlag,
    IntFlag,
    BoolFlag,
    StringFlag,
    SessionFlagDef,
    # Helper functions
    get_all_enums,
    get_all_flags,
    get_all_session_flags,
    # cwl_quest_lib models
    ModConfig,
    FlagConfig,
    EnumDef,
    FlagDef,
    FlagType,
    QuestConfig,
    QuestDef,
    QuestType,
    FlagCondition,
    ItemDef,
    NpcDef,
    RewardDef,
    RewardItemDef,
    StageDef,
    ActorDef,
)

# ============================================================================
# Re-export from quests
# ============================================================================
from arena.data.quests import (
    PHASES,
    QUESTS,
    QUEST_DEFINITIONS,
    REWARDS,
    RANK_REWARDS,
    # Aliases
    RewardItem,
    Reward,
    QuestDefinition,
    # Validation
    validate_rank_rewards,
    validate_all as validate_rewards,
)

# ============================================================================
# Re-export from items
# ============================================================================
from arena.data.items import (
    TraitType,
    ItemEffect,
    ItemDefinition,
    CUSTOM_ITEMS,
    validate_items,
    get_items_by_seller,
)

# ============================================================================
# Re-export from battle
# ============================================================================
from arena.data.battle import (
    # Battle stage classes
    Rarity,
    SpawnPosition,
    GimmickConfig,
    EnemyConfig,
    BattleStage,
    # Battle flag classes
    QuestBattleType,
    RankUpTrialType,
    QuestBattleFlags,
    RankUpTrialFlags,
    # Stage data
    RANK_UP_STAGES,
    NORMAL_STAGES,
    DEBUG_STAGES,
    # Export functions
    export_stages_to_json,
    get_all_stage_ids,
    get_stage,
)

# ============================================================================
# Re-export from types
# ============================================================================
from arena.data.types import (
    # Arena-specific
    RankDefinition,
    BattleStageDefinition,
    # Generic (from cwl_quest_lib)
    GreetingDefinition,
    QuestEntry,
    MenuItem,
    QuestInfoDefinition,
    QuestStartDefinition,
    NpcDefinition,
)


# ============================================================================
# Module-level exports
# ============================================================================

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
    "PHASES",
    "QUESTS",
    "QUEST_DEFINITIONS",
    "REWARDS",
    "RANK_REWARDS",
    "CUSTOM_ITEMS",
    "RANK_UP_STAGES",
    "NORMAL_STAGES",
    "DEBUG_STAGES",
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
    # Types
    "TraitType",
    "ItemEffect",
    "ItemDefinition",
    "Rarity",
    "SpawnPosition",
    "GimmickConfig",
    "EnemyConfig",
    "BattleStage",
    "QuestBattleType",
    "RankUpTrialType",
    "QuestBattleFlags",
    "RankUpTrialFlags",
    "RankDefinition",
    "BattleStageDefinition",
    "GreetingDefinition",
    "QuestEntry",
    "MenuItem",
    "QuestInfoDefinition",
    "QuestStartDefinition",
    "NpcDefinition",
    # Legacy
    "EnumFlag",
    "IntFlag",
    "BoolFlag",
    "StringFlag",
    "SessionFlagDef",
    "RewardItem",
    "Reward",
    "QuestDefinition",
    # Functions
    "get_all_enums",
    "get_all_flags",
    "get_all_session_flags",
    "validate_rank_rewards",
    "validate_rewards",
    "validate_items",
    "get_items_by_seller",
    "export_stages_to_json",
    "get_all_stage_ids",
    "get_stage",
    # cwl_quest_lib models
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
