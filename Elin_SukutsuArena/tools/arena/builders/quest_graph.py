# -*- coding: utf-8 -*-
"""
arena/builders/quest_graph.py - Quest Dependency Graph

This module provides the QuestDependencyGraph class for managing
quest availability and dependencies.
"""

import sys
import os
import json
from dataclasses import dataclass
from typing import List, Dict, Optional, Set, Any, Callable

# Add cwl_quest_lib to path
_current_dir = os.path.dirname(os.path.abspath(__file__))
_arena_dir = os.path.dirname(_current_dir)
_tools_dir = os.path.dirname(_arena_dir)
if _tools_dir not in sys.path:
    sys.path.insert(0, _tools_dir)

from arena.data import (
    PREFIX,
    Keys,
    Actors,
    QuestIds,
    Phase,
    Rank,
    QUESTS,
    QuestDef,
    QuestType,
    FlagCondition,
)


# For backward compatibility
QuestDefinition = QuestDef
QUEST_DEFINITIONS = QUESTS


class QuestDependencyGraph:
    """
    Arena-specific quest dependency graph.
    Handles quest availability based on flags, completed quests, and phase.
    """

    def __init__(self, quests: List[QuestDef] = None):
        """
        Initialize with quest definitions.

        Args:
            quests: List of quest definitions. Defaults to QUESTS.
        """
        quest_list = quests if quests is not None else QUESTS
        self.quests: Dict[str, QuestDef] = {q.quest_id: q for q in quest_list}
        self._block_checkers: List[Callable[[QuestDef, Dict[str, Any]], bool]] = []

        # Add Arena-specific block checker
        self.add_block_checker(self._arena_block_checker)

    def add_block_checker(self, checker: Callable[[QuestDef, Dict[str, Any]], bool]):
        """Add a custom block checker function."""
        self._block_checkers.append(checker)

    def get_current_phase(self, current_flags: Dict[str, Any]) -> Phase:
        """現在のフェーズを取得 (Arena固有)"""
        phase_value = current_flags.get(Keys.CURRENT_PHASE, 0)
        if isinstance(phase_value, Phase):
            return phase_value
        phase_list = list(Phase)
        if isinstance(phase_value, int) and 0 <= phase_value < len(phase_list):
            return phase_list[phase_value]
        return Phase.PROLOGUE

    def _get_quest_phase(self, quest: QuestDef) -> Optional[Phase]:
        """クエストのフェーズを取得（文字列 -> Enum変換）"""
        if not quest.phase:
            return None
        try:
            return Phase(quest.phase.lower())
        except (ValueError, KeyError):
            return None

    def _arena_block_checker(
        self, quest: QuestDef, current_flags: Dict[str, Any]
    ) -> bool:
        """Arena固有のブロック条件チェック"""
        if quest.quest_id == "16_lily_real_name":
            bottle_confession = current_flags.get(Keys.LILY_BOTTLE_CONFESSION)

            if bottle_confession in ["blamed_zek", "denied"]:
                return True

        return False

    def get_available_quests(
        self, current_flags: Dict[str, Any], completed_quests: Set[str]
    ) -> List[QuestDef]:
        """現在のフラグと完了済みクエストから、利用可能なクエストを取得"""
        current_phase = self.get_current_phase(current_flags)
        available = []

        for quest in self.quests.values():
            if quest.quest_id in completed_quests:
                continue

            quest_phase = self._get_quest_phase(quest)
            if quest_phase and quest_phase > current_phase:
                continue

            if not all(
                req_quest in completed_quests for req_quest in quest.required_quests
            ):
                continue

            if not self._check_flag_conditions(quest.required_flags, current_flags):
                continue

            if self._is_blocked(quest, current_flags):
                continue

            available.append(quest)

        available.sort(key=lambda q: q.priority, reverse=True)
        return available

    def get_auto_trigger_quests(
        self, current_flags: Dict[str, Any], completed_quests: Set[str]
    ) -> List[QuestDef]:
        """自動発動クエストのみを取得"""
        available = self.get_available_quests(current_flags, completed_quests)
        return [q for q in available if q.auto_trigger]

    def get_npc_quests(
        self, npc_id: str, current_flags: Dict[str, Any], completed_quests: Set[str]
    ) -> List[QuestDef]:
        """特定NPCが持つ利用可能なクエストを取得"""
        available = self.get_available_quests(current_flags, completed_quests)
        return [q for q in available if q.quest_giver == npc_id]

    def get_all_npc_quests(
        self, current_flags: Dict[str, Any], completed_quests: Set[str]
    ) -> Dict[str, List[QuestDef]]:
        """全NPCの利用可能クエストをNPC別に取得"""
        available = self.get_available_quests(current_flags, completed_quests)
        npc_quests: Dict[str, List[QuestDef]] = {}

        for quest in available:
            if quest.quest_giver:
                if quest.quest_giver not in npc_quests:
                    npc_quests[quest.quest_giver] = []
                npc_quests[quest.quest_giver].append(quest)

        return npc_quests

    def _check_flag_conditions(
        self, conditions: List[FlagCondition], current_flags: Dict[str, Any]
    ) -> bool:
        """フラグ条件をチェック"""
        for condition in conditions:
            flag_value = current_flags.get(condition.flag_key)

            if condition.operator == "==":
                if flag_value != condition.value:
                    return False
            elif condition.operator == "!=":
                if flag_value == condition.value:
                    return False
            elif condition.operator == ">=":
                if flag_value is None or flag_value < condition.value:
                    return False
            elif condition.operator == ">":
                if flag_value is None or flag_value <= condition.value:
                    return False
            elif condition.operator == "<=":
                if flag_value is None or flag_value > condition.value:
                    return False
            elif condition.operator == "<":
                if flag_value is None or flag_value >= condition.value:
                    return False

        return True

    def _is_blocked(self, quest: QuestDef, current_flags: Dict[str, Any]) -> bool:
        """クエストがブロックされているかチェック"""
        for checker in self._block_checkers:
            if checker(quest, current_flags):
                return True
        return False

    def get_quest_chain(self, quest_id: str) -> List[str]:
        """指定クエストに至るまでのクエストチェーンを取得"""
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
        """依存関係の妥当性をチェック"""
        errors = []

        for quest in self.quests.values():
            for req_quest in quest.required_quests:
                if req_quest not in self.quests:
                    errors.append(
                        f"Quest '{quest.quest_id}' requires non-existent quest '{req_quest}'"
                    )

            if self._has_circular_dependency(quest.quest_id):
                errors.append(f"Quest '{quest.quest_id}' has circular dependency")

        return errors

    def _has_circular_dependency(self, quest_id: str, visited: Set[str] = None) -> bool:
        """循環依存をチェック"""
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

    def generate_quest_graph_viz(self) -> str:
        """Graphviz形式のクエストグラフを生成（可視化用）"""
        lines = ["digraph QuestFlow {"]
        lines.append("  rankdir=TB;")
        lines.append("  node [shape=box];")

        colors = {
            QuestType.MAIN_STORY: "lightblue",
            QuestType.RANK_UP: "lightgreen",
            QuestType.SIDE_QUEST: "lightyellow",
            QuestType.CHARACTER_EVENT: "lightpink",
            QuestType.ENDING: "lightcoral",
        }

        for quest in self.quests.values():
            color = colors.get(quest.quest_type, "white")
            lines.append(
                f'  "{quest.quest_id}" [label="{quest.name_jp}\\n({quest.quest_id})", '
                f'fillcolor="{color}", style=filled];'
            )

        for quest in self.quests.values():
            for req_quest in quest.required_quests:
                lines.append(f'  "{req_quest}" -> "{quest.quest_id}";')

        lines.append("}")
        return "\n".join(lines)


# ============================================================================
# Utility Functions
# ============================================================================


def get_quest_graph() -> QuestDependencyGraph:
    """クエスト依存関係グラフのシングルトンインスタンスを取得"""
    return QuestDependencyGraph()


def export_quests_to_json(output_path: str):
    """クエスト定義をJSON形式でエクスポート"""
    quests_data = []
    for quest in QUESTS:
        phase_enum = None
        if quest.phase:
            try:
                phase_enum = Phase(quest.phase.lower())
            except (ValueError, KeyError):
                pass

        advances_phase_enum = None
        if quest.advances_phase:
            try:
                advances_phase_enum = Phase(quest.advances_phase.lower())
            except (ValueError, KeyError):
                pass

        quest_data = {
            "questId": quest.quest_id,
            "questType": quest.quest_type.value,
            "dramaId": quest.drama_id,
            "displayNameJP": quest.name_jp,
            "displayNameEN": quest.name_en,
            "description": quest.description,
            "phase": phase_enum.value if phase_enum else None,
            "phaseOrdinal": list(Phase).index(phase_enum) if phase_enum else 0,
            "questGiver": quest.quest_giver,
            "autoTrigger": quest.auto_trigger,
            "advancesPhase": advances_phase_enum.value if advances_phase_enum else None,
            "advancesPhaseOrdinal": list(Phase).index(advances_phase_enum)
            if advances_phase_enum
            else -1,
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


__all__ = [
    "QuestDependencyGraph",
    "QuestDefinition",
    "QUEST_DEFINITIONS",
    "get_quest_graph",
    "export_quests_to_json",
]
