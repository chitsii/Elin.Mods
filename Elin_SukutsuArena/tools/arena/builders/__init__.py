# -*- coding: utf-8 -*-
"""
arena/builders - Logic Layer

This package provides builder classes for the Sukutsu Arena mod.

Modules:
- drama: DramaBuilder, ArenaDramaBuilder, and mixins
- quest_graph: QuestDependencyGraph for quest management
"""

# ============================================================================
# Re-export from drama
# ============================================================================
from arena.builders.drama import (
    # Core builders
    DramaBuilder,
    ArenaDramaBuilder,
    DramaLabel,
    DramaActor,
    ChoiceReaction,
    HEADERS,
    # Mixins
    RankSystemMixin,
    BattleSystemMixin,
    QuestDispatcherMixin,
    GreetingMixin,
    MenuMixin,
    RewardMixin,
    QuestSystemMixin,  # Alias for QuestDispatcherMixin
)

# ============================================================================
# Re-export from quest_graph
# ============================================================================
from arena.builders.quest_graph import (
    QuestDependencyGraph,
    QuestDefinition,
    QUEST_DEFINITIONS,
    get_quest_graph,
    export_quests_to_json,
)


__all__ = [
    # Drama builders
    "DramaBuilder",
    "ArenaDramaBuilder",
    "DramaLabel",
    "DramaActor",
    "ChoiceReaction",
    "HEADERS",
    # Mixins
    "RankSystemMixin",
    "BattleSystemMixin",
    "QuestDispatcherMixin",
    "GreetingMixin",
    "MenuMixin",
    "RewardMixin",
    "QuestSystemMixin",
    # Quest graph
    "QuestDependencyGraph",
    "QuestDefinition",
    "QUEST_DEFINITIONS",
    "get_quest_graph",
    "export_quests_to_json",
]
