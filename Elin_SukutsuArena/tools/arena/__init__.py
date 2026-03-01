# -*- coding: utf-8 -*-
"""
arena - Sukutsu Arena Mod Python Package

This package provides all data definitions, builders, and utilities
for the Sukutsu Arena mod.

Package structure:
    arena/
    ├── __init__.py         # This file - package initialization
    ├── validation.py       # Build-time validation
    ├── data/              # Pure data (no logic)
    │   ├── config.py      # Flags, Enums, Actors, Constants
    │   ├── quests.py      # Quest definitions, rewards
    │   ├── items.py       # Item definitions
    │   ├── battle.py      # Battle stages + flags
    │   └── types.py       # Arena-specific types
    ├── builders/          # Logic layer
    │   ├── drama.py       # ArenaDramaBuilder + Mixin
    │   └── quest_graph.py # QuestDependencyGraph
    └── scenarios/         # Scenario definitions

Usage:
    from arena.data import Keys, Actors, FlagValues, QuestIds, PREFIX
    from arena.data import QUESTS, REWARDS, CUSTOM_ITEMS
    from arena.builders import ArenaDramaBuilder, DramaBuilder
    from arena.builders import QuestDependencyGraph
"""

__version__ = "1.0.0"

# Re-export commonly used items at package level for convenience
from arena.data import (
    # Constants
    PREFIX,
    MOD_NAME,
    # Key classes
    Keys,
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
    QUESTS,
    REWARDS,
    JUMP_LABELS,
)
