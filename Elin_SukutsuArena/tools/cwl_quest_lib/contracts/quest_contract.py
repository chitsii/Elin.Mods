# -*- coding: utf-8 -*-
"""
CWL Quest Library - Quest JSON Contract (SSOT)

This module defines the exact JSON structure for quest_definitions.json.
It serves as the Single Source of Truth (SSOT) for Python-C# interoperability.

The contract ensures:
1. Python export matches this schema exactly
2. C# deserialization receives predictable types
3. Build-time validation catches type mismatches

Key Design Decision:
    branchChoices is List[str], NOT Dict[str, str]!
    Empty list [] is correctly deserialized as List<string> in C#.
    Empty dict {} fails to deserialize as List<string>.
"""

from __future__ import annotations

from typing import Any, Dict, List, Literal, Optional

from pydantic import BaseModel, Field, RootModel


class FlagConditionContract(BaseModel):
    """
    Flag condition for quest requirements (JSON output format).

    JSON Example:
        {
            "flagKey": "chitsii.arena.player.balgas_battle_complete",
            "operator": "==",
            "value": 1
        }
    """

    flag_key: str = Field(..., alias="flagKey", description="Full flag key")
    operator: Literal["==", "!=", ">=", ">", "<=", "<"] = Field(
        default="==", description="Comparison operator"
    )
    value: Any = Field(..., description="Expected value (int, str, bool)")

    model_config = {
        "populate_by_name": True,
        "json_schema_extra": {
            "examples": [
                {"flagKey": "chitsii.arena.player.rank", "operator": ">=", "value": "F"}
            ]
        },
    }


class QuestContract(BaseModel):
    """
    Quest definition contract (JSON output format).

    This defines the exact structure that quest_definitions.json must follow.
    C# ArenaQuestManager expects this exact schema.

    IMPORTANT: branchChoices is List[str], not Dict!
        - Empty: []
        - With choices: ["choice1", "choice2"]
    """

    # Identity
    quest_id: str = Field(..., alias="questId", description="Unique quest ID")
    quest_type: str = Field(..., alias="questType", description="Quest type")
    drama_id: str = Field(..., alias="dramaId", description="Associated drama file ID")

    # Display
    display_name_jp: str = Field(
        ..., alias="displayNameJP", description="Japanese display name"
    )
    display_name_en: str = Field(
        default="", alias="displayNameEN", description="English display name"
    )
    description: str = Field(default="", description="Quest description")

    # Phase System
    phase: str = Field(..., description="Story phase this quest belongs to")
    phase_ordinal: int = Field(
        ..., alias="phaseOrdinal", description="Phase ordinal (0-based)"
    )
    advances_phase: Optional[str] = Field(
        default=None, alias="advancesPhase", description="Phase to advance to"
    )
    advances_phase_ordinal: int = Field(
        default=-1,
        alias="advancesPhaseOrdinal",
        description="Advances phase ordinal (-1 if none)",
    )

    # Trigger
    quest_giver: Optional[str] = Field(
        default=None, alias="questGiver", description="NPC ID who gives this quest"
    )
    auto_trigger: bool = Field(
        default=False, alias="autoTrigger", description="Auto-trigger when available"
    )

    # Dependencies
    required_flags: List[FlagConditionContract] = Field(
        default_factory=list,
        alias="requiredFlags",
        description="Flag conditions required",
    )
    required_quests: List[str] = Field(
        default_factory=list,
        alias="requiredQuests",
        description="Quest IDs that must be completed",
    )
    blocks_quests: List[str] = Field(
        default_factory=list,
        alias="blocksQuests",
        description="Quest IDs blocked by completing this",
    )

    # Completion
    completion_flags: Dict[str, Any] = Field(
        default_factory=dict,
        alias="completionFlags",
        description="Flags set on completion",
    )

    # Branch Choices - CRITICAL: This is List[str], NOT Dict!
    branch_choices: List[str] = Field(
        default_factory=list,
        alias="branchChoices",
        description="Branch quest choices (List, NOT Dict!)",
    )

    # Priority
    priority: int = Field(
        default=500, description="Quest priority (higher = checked first)"
    )

    model_config = {
        "populate_by_name": True,
        "json_schema_extra": {
            "examples": [
                {
                    "questId": "01_opening",
                    "questType": "main_story",
                    "dramaId": "sukutsu_opening",
                    "displayNameJP": "異次元の闘技場への到着",
                    "displayNameEN": "Arrival at the Dimensional Arena",
                    "description": "プレイヤーがアリーナに到着",
                    "phase": "prologue",
                    "phaseOrdinal": 0,
                    "questGiver": None,
                    "autoTrigger": True,
                    "advancesPhase": None,
                    "advancesPhaseOrdinal": -1,
                    "requiredFlags": [],
                    "requiredQuests": [],
                    "completionFlags": {},
                    "branchChoices": [],
                    "blocksQuests": [],
                    "priority": 1000,
                }
            ]
        },
    }


class QuestListContract(RootModel[List[QuestContract]]):
    """
    Root contract for quest_definitions.json (array of quests).

    The JSON file is a top-level array, so this wrapper is needed for validation.
    Uses Pydantic v2 RootModel for proper list handling.
    """

    @classmethod
    def from_list(cls, quests: List[dict]) -> "QuestListContract":
        """Create from a list of quest dictionaries."""
        return cls(root=[QuestContract(**q) for q in quests])

    def __iter__(self):
        return iter(self.root)

    def __len__(self):
        return len(self.root)


# Type mapping for C# code generation
PYTHON_TO_CSHARP_TYPES = {
    "str": "string",
    "int": "int",
    "bool": "bool",
    "float": "double",
    "Any": "object",
    "List[str]": "List<string>",
    "List[int]": "List<int>",
    "List[FlagConditionContract]": "List<FlagCondition>",
    "Dict[str, Any]": "Dictionary<string, object>",
    "Dict[str, str]": "Dictionary<string, string>",
    "Optional[str]": "string",  # Nullable reference type in C#
    "Optional[int]": "int?",  # Nullable value type in C#
}
