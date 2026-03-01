"""
CWL Quest Library - Pydantic Configuration Models

This module provides type-safe, validated configuration models for CWL mods.
Use these models to define flags, quests, items, NPCs, and other game data
with full IDE support and runtime validation.

Usage:
    from cwl_quest_lib.config_models import (
        ModConfig, FlagConfig, QuestConfig, ItemConfig, NpcConfig,
        EnumDef, FlagDef, QuestDef, ItemDef, NpcDef
    )

    # Define your mod configuration
    config = ModConfig(
        prefix="mymod",
        mod_name="My Awesome Mod",
        flags=FlagConfig(...),
        quests=[...],
    )
"""

from __future__ import annotations

from enum import Enum
from typing import Any, Dict, List, Literal, Optional, Type, Union

from pydantic import BaseModel, Field, field_validator, model_validator


# ============================================================================
# Flag Models
# ============================================================================


class EnumDef(BaseModel):
    """
    Enum definition for flag values.

    Example:
        EnumDef(
            name="Rank",
            values=["UNRANKED", "G", "F", "E", "D", "C", "B", "A", "S"]
        )
    """

    name: str = Field(..., description="Enum type name (used in C# generation)")
    values: List[str] = Field(..., min_length=1, description="Enum values in order")
    description: str = Field(default="", description="Documentation")
    def get_ordinal(self, value: str) -> int:
        """Get ordinal (0-based index) of a value."""
        return self.values.index(value)

    def get_value(self, ordinal: int) -> str:
        """Get value by ordinal."""
        return self.values[ordinal]


class FlagType(str, Enum):
    """Supported flag types."""

    ENUM = "enum"
    INT = "int"
    BOOL = "bool"
    STRING = "string"


class FlagDef(BaseModel):
    """
    Flag definition with type information and validation.

    Examples:
        # Enum flag
        FlagDef(
            key="player.rank",
            flag_type="enum",
            enum_name="Rank",
            default="UNRANKED",
            description="Player's arena rank"
        )

        # Int flag
        FlagDef(
            key="player.karma",
            flag_type="int",
            default=0,
            min_value=-100,
            max_value=100,
            description="Karma value"
        )

        # Bool flag
        FlagDef(
            key="player.is_fugitive",
            flag_type="bool",
            default=False,
            description="Fugitive status"
        )
    """

    key: str = Field(..., description="Flag key (without prefix)")
    flag_type: FlagType = Field(..., description="Flag type")
    description: str = Field(default="", description="Documentation")

    # Enum type specific
    enum_name: Optional[str] = Field(
        default=None, description="Enum type name (for enum flags)"
    )
    default: Any = Field(default=None, description="Default value")

    # Int type specific
    min_value: Optional[int] = Field(
        default=None, description="Minimum value (for int flags)"
    )
    max_value: Optional[int] = Field(
        default=None, description="Maximum value (for int flags)"
    )

    @model_validator(mode="after")
    def validate_type_specific_fields(self) -> "FlagDef":
        """Validate that type-specific fields are set correctly."""
        if self.flag_type == FlagType.ENUM and not self.enum_name:
            raise ValueError(f"Enum flag '{self.key}' requires enum_name")
        if self.flag_type == FlagType.INT:
            if self.default is None:
                self.default = 0
        if self.flag_type == FlagType.BOOL:
            if self.default is None:
                self.default = False
        return self

    def full_key(self, prefix: str) -> str:
        """Get full flag key with prefix."""
        return f"{prefix}.{self.key}"


class FlagConfig(BaseModel):
    """
    Flag configuration for a mod.

    Example:
        FlagConfig(
            enums=[
                EnumDef(name="Rank", values=["UNRANKED", "G", "F", ...]),
                EnumDef(name="Phase", values=["PROLOGUE", "INITIATION", ...]),
            ],
            flags=[
                FlagDef(key="player.rank", flag_type="enum", enum_name="Rank"),
                FlagDef(key="player.karma", flag_type="int", min_value=-100, max_value=100),
            ]
        )
    """

    enums: List[EnumDef] = Field(default_factory=list, description="Enum definitions")
    flags: List[FlagDef] = Field(default_factory=list, description="Flag definitions")

    def get_enum(self, name: str) -> Optional[EnumDef]:
        """Get enum definition by name."""
        for enum in self.enums:
            if enum.name == name:
                return enum
        return None

    def get_flag(self, key: str) -> Optional[FlagDef]:
        """Get flag definition by key."""
        for flag in self.flags:
            if flag.key == key:
                return flag
        return None

    @model_validator(mode="after")
    def validate_enum_references(self) -> "FlagConfig":
        """Validate that all enum flags reference existing enums."""
        enum_names = {e.name for e in self.enums}
        for flag in self.flags:
            if flag.flag_type == FlagType.ENUM and flag.enum_name not in enum_names:
                raise ValueError(
                    f"Flag '{flag.key}' references non-existent enum '{flag.enum_name}'"
                )
        return self


# ============================================================================
# Quest Models
# ============================================================================


class QuestType(str, Enum):
    """Quest type enumeration."""

    MAIN_STORY = "main_story"
    RANK_UP = "rank_up"
    SIDE_QUEST = "side_quest"
    CHARACTER_EVENT = "character_event"
    ENDING = "ending"
    DAILY = "daily"
    REPEATABLE = "repeatable"
    POSTGAME = "postgame"  # エンドゲームコンテンツ（メインストーリー完了後）


class FlagCondition(BaseModel):
    """
    Flag condition for quest requirements.

    Example:
        FlagCondition(flag_key="player.rank", operator=">=", value="F")
    """

    flag_key: str = Field(..., description="Full flag key")
    operator: Literal["==", "!=", ">=", ">", "<=", "<"] = Field(
        default="==", description="Comparison operator"
    )
    value: Any = Field(..., description="Expected value")


class QuestDef(BaseModel):
    """
    Quest definition with full validation.

    Example:
        QuestDef(
            quest_id="01_opening",
            quest_type="main_story",
            drama_id="sukutsu_opening",
            name_jp="Opening",
            name_en="Opening",
            description="Introduction to the arena",
            phase="PROLOGUE",
            auto_trigger=True,
            required_quests=[],
            priority=1000
        )
    """

    quest_id: str = Field(..., description="Unique quest ID")
    quest_type: QuestType = Field(..., description="Quest type")
    drama_id: str = Field(..., description="Associated drama file ID")

    # Display names
    name_jp: str = Field(..., description="Japanese display name")
    name_en: str = Field(default="", description="English display name")
    description: str = Field(default="", description="Quest description")

    # Phase system
    phase: str = Field(..., description="Story phase this quest belongs to")
    advances_phase: Optional[str] = Field(
        default=None, description="Phase to advance to on completion"
    )

    # Trigger
    quest_giver: Optional[str] = Field(
        default=None, description="NPC ID who gives this quest"
    )
    auto_trigger: bool = Field(
        default=False, description="Automatically trigger when available"
    )

    # Dependencies
    required_flags: List[FlagCondition] = Field(
        default_factory=list, description="Flag conditions required"
    )
    required_quests: List[str] = Field(
        default_factory=list, description="Quest IDs that must be completed"
    )
    blocks_quests: List[str] = Field(
        default_factory=list, description="Quest IDs blocked by completing this"
    )

    # Completion
    completion_flags: Dict[str, Any] = Field(
        default_factory=dict, description="Flags set on completion"
    )
    # IMPORTANT: List[str], NOT Dict! Empty list [] serializes correctly for C#.
    branch_choices: List[str] = Field(
        default_factory=list, description="Branch quest choices (List, not Dict)"
    )

    # Priority (higher = checked first)
    priority: int = Field(default=500, ge=0, le=10000, description="Quest priority")


class QuestConfig(BaseModel):
    """
    Quest configuration for a mod.

    Example:
        QuestConfig(
            phases=["PROLOGUE", "INITIATION", "RISING", ...],
            quests=[
                QuestDef(quest_id="01_opening", ...),
                QuestDef(quest_id="02_rank_up_G", ...),
            ]
        )
    """

    phases: List[str] = Field(..., min_length=1, description="Story phases in order")
    quests: List[QuestDef] = Field(
        default_factory=list, description="Quest definitions"
    )

    def get_phase_ordinal(self, phase: str) -> int:
        """Get ordinal of a phase."""
        return self.phases.index(phase)

    def get_quest(self, quest_id: str) -> Optional[QuestDef]:
        """Get quest by ID."""
        for quest in self.quests:
            if quest.quest_id == quest_id:
                return quest
        return None

    @model_validator(mode="after")
    def validate_quest_references(self) -> "QuestConfig":
        """Validate quest references and phases."""
        quest_ids = {q.quest_id for q in self.quests}
        phase_set = set(self.phases)

        for quest in self.quests:
            # Validate phase
            if quest.phase not in phase_set:
                raise ValueError(
                    f"Quest '{quest.quest_id}' references non-existent phase '{quest.phase}'"
                )
            if quest.advances_phase and quest.advances_phase not in phase_set:
                raise ValueError(
                    f"Quest '{quest.quest_id}' advances to non-existent phase '{quest.advances_phase}'"
                )

            # Validate required quests
            for req in quest.required_quests:
                if req not in quest_ids:
                    raise ValueError(
                        f"Quest '{quest.quest_id}' requires non-existent quest '{req}'"
                    )

            # Validate blocked quests
            for blocked in quest.blocks_quests:
                if blocked not in quest_ids:
                    raise ValueError(
                        f"Quest '{quest.quest_id}' blocks non-existent quest '{blocked}'"
                    )

        return self


# ============================================================================
# Item Models
# ============================================================================


class ItemDef(BaseModel):
    """
    Item definition for CWL Thing Excel generation.

    Example:
        ItemDef(
            item_id="my_sword",
            name_jp="Legendary Sword",
            name_en="Legendary Sword",
            name_cn="传说之剑",
            category="weapon",
            value=10000
        )
    """

    item_id: str = Field(..., description="Unique item ID")
    name_jp: str = Field(..., description="Japanese name")
    name_en: str = Field(default="", description="English name")
    name_cn: str = Field(default="", description="Chinese name")
    detail_jp: str = Field(default="", description="Japanese description")
    detail_en: str = Field(default="", description="English description")
    detail_cn: str = Field(default="", description="Chinese description")

    category: str = Field(default="other", description="Item category")
    value: int = Field(default=100, ge=0, description="Base value")
    lv: int = Field(default=1, ge=1, description="Item level")
    weight: int = Field(default=1000, ge=0, description="Weight in grams")
    chance: int = Field(default=0, ge=0, description="Random spawn chance (0=never)")

    # Rendering
    tiles: int = Field(default=0, description="Tile ID")
    render_data: str = Field(default="", description="Render data reference")

    # Trait
    trait_name: str = Field(default="", description="Trait class name")
    trait_params: List[str] = Field(
        default_factory=list, description="Trait parameters"
    )

    # Elements
    elements: str = Field(default="", description="Element string")

    # Equipment
    def_mat: str = Field(default="", description="Default material")
    tier_group: str = Field(default="", description="Tier group")
    defense: str = Field(default="", description="Defense value")

    # Tags
    tags: List[str] = Field(default_factory=list, description="Additional tags")


# ============================================================================
# NPC Models
# ============================================================================


class NpcDef(BaseModel):
    """
    NPC definition for CWL Chara Excel generation.

    Example:
        NpcDef(
            npc_id="my_merchant",
            name_jp="Merchant John",
            name_en="Merchant John",
            name_cn="商人约翰",
            race="human",
            job="merchant",
            lv=50,
            zone_id="my_zone"
        )
    """

    npc_id: str = Field(..., description="Unique NPC ID")
    name_jp: str = Field(..., description="Japanese name")
    name_en: str = Field(default="", description="English name")
    name_cn: str = Field(default="", description="Chinese name")
    aka_jp: str = Field(default="", description="Title/alias (Japanese)")
    aka_en: str = Field(default="", description="Title/alias (English)")
    aka_cn: str = Field(default="", description="Title/alias (Chinese)")

    race: str = Field(default="human", description="Character race")
    job: str = Field(default="warrior", description="Character job/class")
    lv: int = Field(default=1, ge=1, description="Character level")
    hostility: Literal["Friend", "Neutral", "Enemy"] = Field(
        default="Friend", description="Default hostility"
    )

    # Rendering
    tiles: int = Field(default=0, description="Fallback tile ID")
    render_data: str = Field(default="@chara", description="Render data reference")
    portrait: str = Field(default="", description="Custom portrait ID")

    # Bio
    bio: str = Field(default="", description="Character biography")
    id_text: str = Field(default="", description="Text ID for dialogue")

    # CWL tags
    zone_id: str = Field(default="", description="Zone to spawn in")
    stay_home: bool = Field(default=True, description="Disable random movement")
    drama_id: str = Field(default="", description="Linked drama ID")
    stock_id: str = Field(default="", description="Merchant stock ID")
    human_speak: bool = Field(default=True, description="Use human-style dialogue")
    extra_tags: List[str] = Field(
        default_factory=list, description="Additional CWL tags"
    )

    # Stats
    main_element: str = Field(default="", description="Main element")
    elements: str = Field(default="", description="Element string")
    act_combat: str = Field(default="", description="Combat actions")
    trait: str = Field(default="", description="Trait class name")
    quality: int = Field(default=3, ge=0, le=5, description="Quality tier")
    chance: int = Field(default=0, ge=0, description="Random spawn chance (0=never)")

    # Author
    author: str = Field(default="", description="Author credit")


# ============================================================================
# Reward Models
# ============================================================================


class RewardItemDef(BaseModel):
    """Item reward in a reward bundle."""

    item_id: str = Field(..., description="Item ID")
    count: int = Field(default=1, ge=1, description="Quantity")


class RewardDef(BaseModel):
    """
    Reward bundle definition.

    Example:
        RewardDef(
            reward_id="rank_up_g",
            items=[RewardItemDef(item_id="medal", count=10)],
            flags={"player.rank": "G"},
            message_jp="Congratulations!"
        )
    """

    reward_id: str = Field(..., description="Unique reward ID")
    items: List[RewardItemDef] = Field(default_factory=list, description="Item rewards")
    flags: Dict[str, Any] = Field(default_factory=dict, description="Flag changes")
    message_jp: str = Field(default="", description="NPC message (Japanese)")
    message_en: str = Field(default="", description="NPC message (English)")
    message_cn: str = Field(default="", description="NPC message (Chinese)")
    system_message_jp: str = Field(default="", description="System message (Japanese)")
    system_message_en: str = Field(default="", description="System message (English)")
    system_message_cn: str = Field(default="", description="System message (Chinese)")


# ============================================================================
# Stage Models (Arena-specific, extendable)
# ============================================================================


class StageDef(BaseModel):
    """
    Battle stage definition (arena-specific).

    Example:
        StageDef(
            stage_id="rank_g_trial",
            name_jp="Rank G Trial",
            name_cn="G级试炼",
            enemies=["slime", "goblin"],
            bg_music="battle_01"
        )
    """

    stage_id: str = Field(..., description="Unique stage ID")
    name_jp: str = Field(..., description="Japanese name")
    name_en: str = Field(default="", description="English name")
    name_cn: str = Field(default="", description="Chinese name")
    description: str = Field(default="", description="Stage description")
    description_cn: str = Field(default="", description="Stage description (Chinese)")

    enemies: List[str] = Field(default_factory=list, description="Enemy IDs")
    bg_music: str = Field(default="", description="Background music ID")
    time_limit: Optional[int] = Field(
        default=None, ge=0, description="Time limit in seconds"
    )

    # Extra data (extensible)
    extra: Dict[str, Any] = Field(
        default_factory=dict, description="Additional stage data"
    )


# ============================================================================
# Actor Definition
# ============================================================================


class ActorDef(BaseModel):
    """
    Actor ID definition for drama files.

    Example:
        ActorDef(
            actor_id="sukutsu_receptionist",
            name_jp="Lily",
            name_en="Lily",
            name_cn="莉莉",
            description="Arena receptionist"
        )
    """

    actor_id: str = Field(..., description="Actor ID (matches CWL chara ID)")
    name_jp: str = Field(default="", description="Display name (Japanese)")
    name_en: str = Field(default="", description="Display name (English)")
    name_cn: str = Field(default="", description="Display name (Chinese)")
    description: str = Field(default="", description="Role description")


# ============================================================================
# Mod Configuration
# ============================================================================


class ModConfig(BaseModel):
    """
    Complete mod configuration.

    This is the top-level configuration model that aggregates all
    mod-specific settings, flags, quests, items, NPCs, etc.

    Example:
        config = ModConfig(
            prefix="chitsii.arena",
            mod_name="Sukutsu Arena",
            flags=FlagConfig(...),
            quests=QuestConfig(...),
            items=[...],
            npcs=[...],
        )
    """

    # Basic info
    prefix: str = Field(..., description="Mod prefix for flags/IDs")
    mod_name: str = Field(..., description="Mod display name")
    mod_version: str = Field(default="1.0.0", description="Mod version")
    author: str = Field(default="", description="Mod author")

    # Actors
    actors: List[ActorDef] = Field(
        default_factory=list, description="Actor definitions"
    )

    # Flags
    flags: FlagConfig = Field(
        default_factory=FlagConfig, description="Flag configuration"
    )

    # Quests
    quests: Optional[QuestConfig] = Field(
        default=None, description="Quest configuration"
    )

    # Items
    items: List[ItemDef] = Field(default_factory=list, description="Item definitions")

    # NPCs
    npcs: List[NpcDef] = Field(default_factory=list, description="NPC definitions")

    # Rewards
    rewards: List[RewardDef] = Field(
        default_factory=list, description="Reward definitions"
    )

    # Stages (arena-specific)
    stages: List[StageDef] = Field(
        default_factory=list, description="Stage definitions"
    )

    def get_actor(self, actor_id: str) -> Optional[ActorDef]:
        """Get actor by ID."""
        for actor in self.actors:
            if actor.actor_id == actor_id:
                return actor
        return None

    def get_item(self, item_id: str) -> Optional[ItemDef]:
        """Get item by ID."""
        for item in self.items:
            if item.item_id == item_id:
                return item
        return None

    def get_npc(self, npc_id: str) -> Optional[NpcDef]:
        """Get NPC by ID."""
        for npc in self.npcs:
            if npc.npc_id == npc_id:
                return npc
        return None

    def get_reward(self, reward_id: str) -> Optional[RewardDef]:
        """Get reward by ID."""
        for reward in self.rewards:
            if reward.reward_id == reward_id:
                return reward
        return None

    def get_stage(self, stage_id: str) -> Optional[StageDef]:
        """Get stage by ID."""
        for stage in self.stages:
            if stage.stage_id == stage_id:
                return stage
        return None

    @model_validator(mode="after")
    def validate_cross_references(self) -> "ModConfig":
        """Validate cross-references between different config sections."""
        # Collect all valid IDs
        actor_ids = {a.actor_id for a in self.actors}
        item_ids = {i.item_id for i in self.items}
        npc_ids = {n.npc_id for n in self.npcs}

        # Validate quest givers reference valid NPCs
        if self.quests:
            for quest in self.quests.quests:
                if quest.quest_giver and quest.quest_giver not in actor_ids:
                    # Allow unregistered actors (they might be vanilla NPCs)
                    pass  # Just a warning, not an error

        # Validate reward items
        for reward in self.rewards:
            for item_reward in reward.items:
                if item_reward.item_id not in item_ids:
                    # Allow vanilla items
                    pass  # Just a warning, not an error

        return self


# ============================================================================
# Helper Functions
# ============================================================================


def create_enum_from_def(enum_def: EnumDef) -> Type[Enum]:
    """Create a Python Enum type from EnumDef."""
    return Enum(enum_def.name, {v: v.lower() for v in enum_def.values})


def load_config_from_file(path: str) -> ModConfig:
    """
    Load ModConfig from a Python file.

    The file should define a `CONFIG` variable of type ModConfig.

    Example file (config.py):
        from cwl_quest_lib.config_models import ModConfig, FlagConfig, ...

        CONFIG = ModConfig(
            prefix="mymod",
            ...
        )

    Usage:
        config = load_config_from_file("config.py")
    """
    import importlib.util

    spec = importlib.util.spec_from_file_location("config_module", path)
    if spec is None or spec.loader is None:
        raise ValueError(f"Cannot load config from {path}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    if not hasattr(module, "CONFIG"):
        raise ValueError(f"Config file {path} must define a CONFIG variable")

    config = getattr(module, "CONFIG")
    if not isinstance(config, ModConfig):
        raise TypeError(f"CONFIG must be a ModConfig instance, got {type(config)}")

    return config
