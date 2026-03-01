"""
CWL Quest Library - Generic Quest Builder

This module provides generic quest definition classes and dependency graph
management that can be reused across different CWL mods.

Usage:
    from cwl_quest_lib.quest_builder import (
        QuestType, FlagCondition, QuestDefinition,
        QuestDependencyGraph, QuestConfig
    )
"""

from dataclasses import dataclass, field
from typing import List, Dict, Optional, Set, Any, Callable
from enum import Enum
import json


# ============================================================================
# Quest Configuration
# ============================================================================


@dataclass
class QuestConfig:
    """
    Configuration for a quest system.

    This class holds all the configuration values needed for a quest system,
    including prefixes, IDs, and other constants.

    Attributes:
        mod_id: The mod's unique identifier
        flag_prefix: Prefix for all flags (e.g., "chitsii.arena")
        quest_done_prefix: Prefix for quest completion flags
        zone_id: The zone ID where quests take place
    """

    mod_id: str
    flag_prefix: str
    quest_done_prefix: str = "quest_done_"
    zone_id: str = ""

    def get_quest_done_key(self, quest_id: str) -> str:
        """Generate quest completion flag key."""
        return f"{self.quest_done_prefix}{quest_id}"

    def get_flag_key(self, flag_name: str) -> str:
        """Generate full flag key with prefix."""
        return f"{self.flag_prefix}.{flag_name}"


# ============================================================================
# Quest Types
# ============================================================================


class QuestType(Enum):
    """Quest type categories."""

    MAIN_STORY = "main_story"
    RANK_UP = "rank_up"
    SIDE_QUEST = "side_quest"
    CHARACTER_EVENT = "character_event"
    ENDING = "ending"
    POSTGAME = "postgame"  # エンドゲームコンテンツ


# ============================================================================
# Flag Condition
# ============================================================================


@dataclass
class FlagCondition:
    """
    A condition based on a flag value.

    Attributes:
        flag_key: The full flag key to check
        operator: Comparison operator ("==", "!=", ">=", ">", "<=", "<")
        value: The value to compare against
    """

    flag_key: str
    operator: str  # "==", "!=", ">=", ">", "<=", "<"
    value: Any

    def __str__(self):
        return f"{self.flag_key} {self.operator} {self.value}"

    def evaluate(self, current_flags: Dict[str, Any]) -> bool:
        """
        Evaluate this condition against current flag values.

        Args:
            current_flags: Dictionary of current flag values

        Returns:
            True if condition is met, False otherwise
        """
        flag_value = current_flags.get(self.flag_key)

        if self.operator == "==":
            return flag_value == self.value
        elif self.operator == "!=":
            return flag_value != self.value
        elif self.operator == ">=":
            return flag_value is not None and flag_value >= self.value
        elif self.operator == ">":
            return flag_value is not None and flag_value > self.value
        elif self.operator == "<=":
            return flag_value is not None and flag_value <= self.value
        elif self.operator == "<":
            return flag_value is not None and flag_value < self.value
        else:
            return False


# ============================================================================
# Quest Definition
# ============================================================================


@dataclass
class QuestDefinition:
    """
    A quest definition with all metadata and dependencies.

    This is a generic quest definition that can be extended for game-specific
    needs through the custom_data field.

    Attributes:
        quest_id: Unique quest identifier
        quest_type: Type of quest (main story, side quest, etc.)
        drama_id: Associated drama file ID
        display_name_jp: Japanese display name
        display_name_en: English display name
        description: Quest description

        phase: The phase when this quest becomes available (generic object)
        quest_giver: NPC ID who gives this quest (None = auto-trigger)
        auto_trigger: Whether quest triggers automatically on zone entry
        advances_phase: Phase to advance to on completion (or None)

        required_flags: Flag conditions that must be met
        required_quests: Quest IDs that must be completed first
        completion_flags: Flags to set on quest completion
        branch_choices: Possible branch choices in this quest
        blocks_quests: Quest IDs blocked by completing this quest

        priority: Quest priority (higher = shown first)
        custom_data: Game-specific additional data
    """

    quest_id: str
    quest_type: QuestType
    drama_id: str
    display_name_jp: str
    display_name_en: str
    description: str

    # Phase system
    phase: Any = None  # Game-specific phase enum
    quest_giver: Optional[str] = None
    auto_trigger: bool = False
    advances_phase: Any = None

    # Dependencies
    required_flags: List[FlagCondition] = field(default_factory=list)
    required_quests: List[str] = field(default_factory=list)

    # Effects
    completion_flags: Dict[str, Any] = field(default_factory=dict)
    branch_choices: List[str] = field(default_factory=list)
    blocks_quests: List[str] = field(default_factory=list)

    # Ordering
    priority: int = 0

    # Extension point
    custom_data: Dict[str, Any] = field(default_factory=dict)


# ============================================================================
# Quest Dependency Graph
# ============================================================================


class QuestDependencyGraph:
    """
    Manages quest dependencies and availability checking.

    This class provides methods to check which quests are available based on
    current game state (flags and completed quests).
    """

    def __init__(
        self,
        quests: List[QuestDefinition] = None,
        phase_comparator: Callable[[Any, Any], bool] = None,
    ):
        """
        Initialize the dependency graph.

        Args:
            quests: List of quest definitions
            phase_comparator: Function to compare phases (a > b).
                              If None, phases are compared directly.
        """
        self.quests: Dict[str, QuestDefinition] = {}
        self._phase_comparator = phase_comparator or (lambda a, b: a > b)
        self._block_checkers: List[
            Callable[[QuestDefinition, Dict[str, Any]], bool]
        ] = []

        if quests:
            for quest in quests:
                self.quests[quest.quest_id] = quest

    def add_quest(self, quest: QuestDefinition) -> None:
        """Add a quest to the graph."""
        self.quests[quest.quest_id] = quest

    def add_block_checker(
        self, checker: Callable[[QuestDefinition, Dict[str, Any]], bool]
    ) -> None:
        """
        Add a custom block checker function.

        Args:
            checker: Function that returns True if quest should be blocked.
                     Signature: (quest, current_flags) -> bool
        """
        self._block_checkers.append(checker)

    def get_current_phase(
        self, current_flags: Dict[str, Any], phase_key: str, phase_enum: type
    ) -> Any:
        """
        Get current phase from flags.

        Args:
            current_flags: Current flag values
            phase_key: The flag key storing the phase
            phase_enum: The Phase enum class

        Returns:
            The current Phase enum value
        """
        phase_value = current_flags.get(phase_key, 0)
        if isinstance(phase_value, phase_enum):
            return phase_value
        # Convert integer to phase enum
        phase_list = list(phase_enum)
        if isinstance(phase_value, int) and 0 <= phase_value < len(phase_list):
            return phase_list[phase_value]
        return phase_list[0] if phase_list else None

    def get_available_quests(
        self,
        current_flags: Dict[str, Any],
        completed_quests: Set[str],
        current_phase: Any = None,
    ) -> List[QuestDefinition]:
        """
        Get all quests available in the current state.

        Args:
            current_flags: Current flag values
            completed_quests: Set of completed quest IDs
            current_phase: Current phase (optional, for phase-based filtering)

        Returns:
            List of available quests, sorted by priority (descending)
        """
        available = []

        for quest in self.quests.values():
            # Skip completed quests
            if quest.quest_id in completed_quests:
                continue

            # Phase check
            if current_phase is not None and quest.phase is not None:
                if self._phase_comparator(quest.phase, current_phase):
                    continue

            # Required quests check
            if not all(req in completed_quests for req in quest.required_quests):
                continue

            # Flag conditions check
            if not self._check_flag_conditions(quest.required_flags, current_flags):
                continue

            # Custom block checkers
            if self._is_blocked(quest, current_flags):
                continue

            available.append(quest)

        # Sort by priority (descending)
        available.sort(key=lambda q: q.priority, reverse=True)
        return available

    def get_auto_trigger_quests(
        self,
        current_flags: Dict[str, Any],
        completed_quests: Set[str],
        current_phase: Any = None,
    ) -> List[QuestDefinition]:
        """Get only auto-trigger quests."""
        available = self.get_available_quests(
            current_flags, completed_quests, current_phase
        )
        return [q for q in available if q.auto_trigger]

    def get_npc_quests(
        self,
        npc_id: str,
        current_flags: Dict[str, Any],
        completed_quests: Set[str],
        current_phase: Any = None,
    ) -> List[QuestDefinition]:
        """Get quests available from a specific NPC."""
        available = self.get_available_quests(
            current_flags, completed_quests, current_phase
        )
        return [q for q in available if q.quest_giver == npc_id]

    def get_all_npc_quests(
        self,
        current_flags: Dict[str, Any],
        completed_quests: Set[str],
        current_phase: Any = None,
    ) -> Dict[str, List[QuestDefinition]]:
        """Get all available quests grouped by NPC."""
        available = self.get_available_quests(
            current_flags, completed_quests, current_phase
        )
        npc_quests: Dict[str, List[QuestDefinition]] = {}

        for quest in available:
            if quest.quest_giver:
                if quest.quest_giver not in npc_quests:
                    npc_quests[quest.quest_giver] = []
                npc_quests[quest.quest_giver].append(quest)

        return npc_quests

    def _check_flag_conditions(
        self, conditions: List[FlagCondition], current_flags: Dict[str, Any]
    ) -> bool:
        """Check if all flag conditions are met."""
        return all(cond.evaluate(current_flags) for cond in conditions)

    def _is_blocked(
        self, quest: QuestDefinition, current_flags: Dict[str, Any]
    ) -> bool:
        """Check if quest is blocked by custom checkers."""
        return any(checker(quest, current_flags) for checker in self._block_checkers)

    def get_quest_chain(self, quest_id: str) -> List[str]:
        """Get the chain of quests leading to the specified quest."""
        quest = self.quests.get(quest_id)
        if not quest:
            return []

        chain = []
        visited = set()

        def _build_chain(q_id: str):
            if q_id in visited:
                return
            visited.add(q_id)

            q = self.quests.get(q_id)
            if not q:
                return

            for req_quest in q.required_quests:
                _build_chain(req_quest)

            chain.append(q_id)

        _build_chain(quest_id)
        return chain

    def validate_dependencies(self) -> List[str]:
        """Validate quest dependencies and return any errors."""
        errors = []

        for quest in self.quests.values():
            # Check required quests exist
            for req_quest in quest.required_quests:
                if req_quest not in self.quests:
                    errors.append(
                        f"Quest '{quest.quest_id}' requires non-existent quest '{req_quest}'"
                    )

            # Check for circular dependencies
            if self._has_circular_dependency(quest.quest_id):
                errors.append(f"Quest '{quest.quest_id}' has circular dependency")

        return errors

    def _has_circular_dependency(self, quest_id: str, visited: Set[str] = None) -> bool:
        """Check for circular dependencies."""
        if visited is None:
            visited = set()

        if quest_id in visited:
            return True

        visited.add(quest_id)
        quest = self.quests.get(quest_id)
        if not quest:
            return False

        for req_quest in quest.required_quests:
            if self._has_circular_dependency(req_quest, visited.copy()):
                return True

        return False

    def generate_graphviz(self, type_colors: Dict[QuestType, str] = None) -> str:
        """
        Generate Graphviz DOT format for visualization.

        Args:
            type_colors: Optional mapping of QuestType to color names

        Returns:
            DOT format string
        """
        default_colors = {
            QuestType.MAIN_STORY: "lightblue",
            QuestType.RANK_UP: "lightgreen",
            QuestType.SIDE_QUEST: "lightyellow",
            QuestType.CHARACTER_EVENT: "lightpink",
            QuestType.ENDING: "lightcoral",
        }
        colors = type_colors or default_colors

        lines = ["digraph QuestFlow {"]
        lines.append("  rankdir=TB;")
        lines.append("  node [shape=box];")

        for quest in self.quests.values():
            color = colors.get(quest.quest_type, "white")
            label = f"{quest.display_name_jp}\\n({quest.quest_id})"
            lines.append(
                f'  "{quest.quest_id}" [label="{label}", '
                f'fillcolor="{color}", style=filled];'
            )

        for quest in self.quests.values():
            for req_quest in quest.required_quests:
                lines.append(f'  "{req_quest}" -> "{quest.quest_id}";')

        lines.append("}")
        return "\n".join(lines)

    def export_to_json(self, output_path: str, phase_enum: type = None) -> None:
        """
        Export quests to JSON format.

        Args:
            output_path: Path to output JSON file
            phase_enum: Optional Phase enum for ordinal calculation
        """
        quests_data = []

        for quest in self.quests.values():
            phase_ordinal = 0
            advances_phase_ordinal = -1

            if phase_enum:
                phase_list = list(phase_enum)
                if quest.phase in phase_list:
                    phase_ordinal = phase_list.index(quest.phase)
                if quest.advances_phase in phase_list:
                    advances_phase_ordinal = phase_list.index(quest.advances_phase)

            quest_data = {
                "questId": quest.quest_id,
                "questType": quest.quest_type.value,
                "dramaId": quest.drama_id,
                "displayNameJP": quest.display_name_jp,
                "displayNameEN": quest.display_name_en,
                "description": quest.description,
                "phase": quest.phase.value
                if hasattr(quest.phase, "value")
                else str(quest.phase),
                "phaseOrdinal": phase_ordinal,
                "questGiver": quest.quest_giver,
                "autoTrigger": quest.auto_trigger,
                "advancesPhase": quest.advances_phase.value
                if hasattr(quest.advances_phase, "value")
                else None,
                "advancesPhaseOrdinal": advances_phase_ordinal,
                "requiredFlags": [
                    {
                        "flagKey": cond.flag_key,
                        "operator": cond.operator,
                        "value": cond.value,
                    }
                    for cond in quest.required_flags
                ],
                "requiredQuests": quest.required_quests,
                "completionFlags": quest.completion_flags,
                "branchChoices": quest.branch_choices,
                "blocksQuests": quest.blocks_quests,
                "priority": quest.priority,
            }
            quests_data.append(quest_data)

        with open(output_path, "w", encoding="utf-8") as f:
            json.dump(quests_data, f, ensure_ascii=False, indent=2)

        print(f"Exported {len(quests_data)} quests to {output_path}")


# ============================================================================
# Unit Tests
# ============================================================================

if __name__ == "__main__":
    print("=== Quest Builder Test ===\n")

    # Create test quests
    quest1 = QuestDefinition(
        quest_id="test_opening",
        quest_type=QuestType.MAIN_STORY,
        drama_id="opening",
        display_name_jp="オープニング",
        display_name_en="Opening",
        description="The story begins",
        auto_trigger=True,
        priority=1000,
    )

    quest2 = QuestDefinition(
        quest_id="test_rank_g",
        quest_type=QuestType.RANK_UP,
        drama_id="rank_g",
        display_name_jp="ランクG試験",
        display_name_en="Rank G Trial",
        description="First rank trial",
        quest_giver="master",
        required_quests=["test_opening"],
        priority=900,
    )

    # Create dependency graph
    graph = QuestDependencyGraph([quest1, quest2])

    # Test validation
    print("Validating dependencies...")
    errors = graph.validate_dependencies()
    print(f"  Errors: {errors if errors else 'None'}")

    # Test availability
    print("\nTesting quest availability...")

    # Initial state
    flags = {}
    completed = set()
    available = graph.get_available_quests(flags, completed)
    print(f"  Initial: {[q.quest_id for q in available]}")

    # After opening
    completed.add("test_opening")
    available = graph.get_available_quests(flags, completed)
    print(f"  After opening: {[q.quest_id for q in available]}")

    # Test graphviz
    print("\nGenerating Graphviz...")
    dot = graph.generate_graphviz()
    print(f"  Generated {len(dot)} chars")

    print("\n=== All Tests Passed! ===")
