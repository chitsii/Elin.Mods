# -*- coding: utf-8 -*-
"""
CWL Quest Library - Core Module

This module contains core type definitions and configuration models.
"""

from .types import (
    GreetingDefinition,
    QuestEntry,
    MenuItem,
    QuestInfoDefinition,
    QuestStartDefinition,
    RewardItem,
    Reward,
    NpcDefinition,
    ItemDefinition,
)

__all__ = [
    "GreetingDefinition",
    "QuestEntry",
    "MenuItem",
    "QuestInfoDefinition",
    "QuestStartDefinition",
    "RewardItem",
    "Reward",
    "NpcDefinition",
    "ItemDefinition",
]

# Pydantic config models (optional - requires pydantic)
_PYDANTIC_AVAILABLE = False
try:
    from .config_models import (
        # Flag models (Pydantic)
        EnumDef,
        FlagType,
        FlagDef,
        FlagConfig,
        # Quest models (Pydantic)
        QuestType,
        FlagCondition,
        QuestDef,
        QuestConfig,
        # Item models (Pydantic)
        ItemDef,
        # NPC models (Pydantic)
        NpcDef,
        # Reward models (Pydantic)
        RewardItemDef,
        RewardDef,
        # Stage models (Pydantic)
        StageDef,
        # Actor models (Pydantic)
        ActorDef,
        # Top-level config
        ModConfig,
        # Helpers
        create_enum_from_def,
        load_config_from_file,
    )

    _PYDANTIC_AVAILABLE = True
    __all__.extend(
        [
            "EnumDef",
            "FlagType",
            "FlagDef",
            "FlagConfig",
            "QuestType",
            "FlagCondition",
            "QuestDef",
            "QuestConfig",
            "ItemDef",
            "NpcDef",
            "RewardItemDef",
            "RewardDef",
            "StageDef",
            "ActorDef",
            "ModConfig",
            "create_enum_from_def",
            "load_config_from_file",
        ]
    )
except ImportError:
    # Pydantic not installed - config_models not available
    pass
