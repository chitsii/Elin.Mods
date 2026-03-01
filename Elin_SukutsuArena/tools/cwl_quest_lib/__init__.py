# -*- coding: utf-8 -*-
"""
CWL Quest Library - Generic quest system framework for CWL mods

This library provides generic building blocks for quest systems that can be
reused across different CWL mods. It extracts common patterns from game-specific
implementations.

Package Structure:
    cwl_quest_lib/
    ├── core/           # Type definitions and Pydantic config models
    ├── builders/       # Builder classes (Drama, Quest, Flag, Item, NPC)
    ├── mixins/         # Mixin classes for extending builders
    └── build/          # Unified build system

Usage:
    from cwl_quest_lib import (
        # Flag building
        FlagDef, EnumFlag, IntFlag, BoolFlag, StringFlag,
        # Quest building
        QuestDefinition, FlagCondition, QuestType, QuestDependencyGraph, QuestConfig,
        # Drama building
        DramaBuilder, DramaLabel, DramaActor, ChoiceReaction, HEADERS,
        # Types
        GreetingDefinition, QuestEntry, MenuItem, QuestInfoDefinition, QuestStartDefinition,
        RewardItem, Reward, NpcDefinition, ItemDefinition,
        # Build system
        UnifiedBuilder, BuildConfig, build_mod,
    )
"""

# =============================================================================
# Core Types (dataclass-based)
# =============================================================================
from .core.types import (
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

# =============================================================================
# Builders
# =============================================================================
from .builders.flag_builder import (
    FlagDef,
    EnumFlag,
    IntFlag,
    BoolFlag,
    StringFlag,
    get_all_flags_from_class,
)

from .builders.quest_builder import (
    QuestType,
    FlagCondition,
    QuestDefinition,
    QuestDependencyGraph,
    QuestConfig,
)

from .builders.drama_builder import (
    DramaBuilder,
    DramaLabel,
    DramaActor,
    ChoiceReaction,
    HEADERS,
)

from .builders.item_builder import (
    ItemBuilder,
    TraitType,
)

from .builders.npc_builder import (
    NpcBuilder,
)

# =============================================================================
# Mixins
# =============================================================================
from .mixins.quest_drama_builder import (
    QuestDispatcherMixin,
    GreetingMixin,
    MenuMixin,
    RewardMixin,
    QuestDramaBuilder,
)

# =============================================================================
# Build System
# =============================================================================
from .build.unified_builder import (
    UnifiedBuilder,
    BuildConfig,
    BuildStep,
    BuildResult,
    build_mod,
)

# =============================================================================
# Pydantic Config Models (optional - requires pydantic)
# =============================================================================
_PYDANTIC_AVAILABLE = False
try:
    from .core.config_models import (
        # Flag models (Pydantic)
        EnumDef,
        FlagType,
        FlagDef as FlagDefPydantic,
        FlagConfig,
        # Quest models (Pydantic)
        QuestType as QuestTypePydantic,
        FlagCondition as FlagConditionPydantic,
        QuestDef,
        QuestConfig as QuestConfigPydantic,
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
except ImportError:
    # Pydantic not installed - config_models not available
    pass

# =============================================================================
# Version
# =============================================================================
__version__ = "0.2.0"

# =============================================================================
# Public API
# =============================================================================
__all__ = [
    # Core types (dataclass-based)
    "GreetingDefinition",
    "QuestEntry",
    "MenuItem",
    "QuestInfoDefinition",
    "QuestStartDefinition",
    "RewardItem",
    "Reward",
    "NpcDefinition",
    "ItemDefinition",
    # Flag building
    "FlagDef",
    "EnumFlag",
    "IntFlag",
    "BoolFlag",
    "StringFlag",
    "get_all_flags_from_class",
    # Quest building
    "QuestType",
    "FlagCondition",
    "QuestDefinition",
    "QuestDependencyGraph",
    "QuestConfig",
    # Drama building
    "DramaBuilder",
    "DramaLabel",
    "DramaActor",
    "ChoiceReaction",
    "HEADERS",
    # Quest drama mixins
    "QuestDispatcherMixin",
    "GreetingMixin",
    "MenuMixin",
    "RewardMixin",
    "QuestDramaBuilder",
    # Item building
    "ItemBuilder",
    "TraitType",
    # NPC building
    "NpcBuilder",
    # Build system
    "UnifiedBuilder",
    "BuildConfig",
    "BuildStep",
    "BuildResult",
    "build_mod",
]

# Add Pydantic config models to __all__ if available
if _PYDANTIC_AVAILABLE:
    __all__.extend(
        [
            "EnumDef",
            "FlagType",
            "FlagDefPydantic",
            "FlagConfig",
            "QuestTypePydantic",
            "FlagConditionPydantic",
            "QuestDef",
            "QuestConfigPydantic",
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
