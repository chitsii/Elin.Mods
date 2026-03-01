# -*- coding: utf-8 -*-
"""
CWL Quest Library - Builders Module

This module contains all builder classes for generating game content.
"""

from .flag_builder import (
    FlagDef,
    EnumFlag,
    IntFlag,
    BoolFlag,
    StringFlag,
    get_all_flags_from_class,
)

from .quest_builder import (
    QuestType,
    FlagCondition,
    QuestDefinition,
    QuestDependencyGraph,
    QuestConfig,
)

from .drama_builder import (
    DramaBuilder,
    DramaLabel,
    DramaActor,
    ChoiceReaction,
    HEADERS,
)

from .item_builder import (
    ItemBuilder,
    TraitType,
)

from .npc_builder import (
    NpcBuilder,
)

__all__ = [
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
    # Item building
    "ItemBuilder",
    "TraitType",
    # NPC building
    "NpcBuilder",
]
