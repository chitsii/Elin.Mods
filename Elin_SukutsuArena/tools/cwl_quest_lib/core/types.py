"""
CWL Quest Library - Common Type Definitions

This module provides generic data classes for quest system components
that can be reused across different CWL mods.

Data Classes:
- GreetingDefinition: Flag-based greeting configuration
- QuestEntry: Quest dispatcher entry
- MenuItem: Menu item with optional condition
- QuestInfoDefinition: Quest info display definition
- QuestStartDefinition: Quest start confirmation definition
- RewardItem: Item reward definition
- Reward: Generic reward bundle
"""

from dataclasses import dataclass, field
from typing import List, Dict, Optional, Union, Any, TYPE_CHECKING

if TYPE_CHECKING:
    from .drama_builder import DramaLabel


# ============================================================================
# Greeting System Types
# ============================================================================


@dataclass
class GreetingDefinition:
    """
    Flag-based greeting definition.

    Used to generate NPC greetings that vary based on a flag value
    (e.g., player rank, story progress, relationship level).

    Example:
        GreetingDefinition(0, "greet_new", "Welcome, newcomer!", "Welcome, newcomer!")
        GreetingDefinition(1, "greet_member", "Ah, good to see you again.")
    """

    flag_value: int  # The flag value that triggers this greeting
    text_id: str  # Unique text ID for localization
    text_jp: str  # Japanese text
    text_en: str = ""  # English text (defaults to Japanese if empty)


# ============================================================================
# Quest Dispatcher Types
# ============================================================================


@dataclass
class QuestEntry:
    """
    Quest dispatcher entry.

    Used in quest dispatcher systems to route players to available quests.
    The dispatcher checks quest availability and jumps to the appropriate step.

    Example:
        QuestEntry("intro_quest", 1, "quest_intro_step")
        QuestEntry("side_quest_a", 2, "quest_a_step")
    """

    quest_id: str  # Quest ID (matches QuestDefinition.quest_id)
    flag_value: int  # Flag value set when this quest is selected
    step_name: str  # Drama step name to jump to


# ============================================================================
# Menu Types
# ============================================================================


@dataclass
class MenuItem:
    """
    Menu item with optional condition.

    Used to build dynamic menus where items can be conditionally shown
    based on flag values or other conditions.

    Example:
        MenuItem("Start Battle", battle_step, "menu_battle", text_en="Start Battle")
        MenuItem("Secret Option", secret_step, "menu_secret", condition="flag >= 5")
    """

    text_jp: str  # Japanese display text
    jump_to: Union[str, "DramaLabel"]  # Target step or label
    text_id: str  # Unique text ID for localization (REQUIRED for non-JP languages)
    text_en: str = ""  # English display text
    text_cn: str = ""  # Chinese display text
    condition: str = ""  # CWL condition expression (empty = always show)


# ============================================================================
# Quest Info Types
# ============================================================================


@dataclass
class QuestInfoDefinition:
    """
    Quest info display definition.

    Used when an NPC provides information about a quest without
    immediately starting it (e.g., hints, directions, lore).

    Example:
        QuestInfoDefinition(
            step_name="quest_merchant_info",
            text_id_prefix="merchant_info",
            messages=["A strange merchant has arrived.", "They're in the corner."],
            messages_en=["A strange merchant has arrived.", "They're in the corner."],
            messages_cn=["一个奇怪的商人来了。", "他在角落里。"]
        )
    """

    step_name: str  # Drama step name
    text_id_prefix: str  # Prefix for text IDs (appended with index)
    messages: List[str]  # Japanese message lines
    messages_en: List[str] = field(default_factory=list)  # English message lines
    messages_cn: List[str] = field(default_factory=list)  # Chinese message lines


@dataclass
class QuestStartDefinition:
    """
    Quest start confirmation definition.

    Used when a quest requires player confirmation before starting.
    Generates info display step and confirmation step.

    Example:
        QuestStartDefinition(
            info_step="quest_dangerous_info",
            start_step="quest_dangerous_start",
            info_messages=["This quest is dangerous.", "Are you ready?"],
            accept_button="Yes, I'm ready",
            decline_button="Not yet",
            start_message="Very well. Let's begin.",
            drama_name="drama_dangerous_quest"
        )
    """

    info_step: str  # Info display step name
    start_step: str  # Start confirmation step name
    info_messages: List[str]  # Japanese info messages
    info_messages_en: List[str] = field(default_factory=list)  # English info messages
    info_messages_cn: List[str] = field(default_factory=list)  # Chinese info messages
    info_id_prefix: str = ""  # Prefix for info text IDs
    accept_button: str = "Accept"  # Accept button text (JP)
    accept_button_en: str = ""  # Accept button text (EN)
    accept_button_cn: str = ""  # Accept button text (CN)
    accept_id: str = ""  # Accept button text ID
    decline_button: str = "Not now"  # Decline button text (JP)
    decline_button_en: str = ""  # Decline button text (EN)
    decline_button_cn: str = ""  # Decline button text (CN)
    decline_id: str = ""  # Decline button text ID
    start_message: str = ""  # Message shown when quest starts (JP)
    start_message_en: str = ""  # Message shown when quest starts (EN)
    start_message_cn: str = ""  # Message shown when quest starts (CN)
    drama_name: str = ""  # Drama to start


# ============================================================================
# Reward Types
# ============================================================================


@dataclass
class RewardItem:
    """
    Item reward definition.

    Example:
        RewardItem("medal", 10)  # 10 medals
        RewardItem("potion", 5)  # 5 potions
    """

    item_id: str  # CWL item ID or vanilla item ID
    count: int = 1  # Number of items


@dataclass
class Reward:
    """
    Generic reward bundle.

    Combines items, flags, and messages into a single reward definition.
    Can be extended for game-specific reward types.

    Example:
        Reward(
            items=[RewardItem("medal", 10), RewardItem("gold", 1000)],
            flags={"quest_complete": 1},
            message_jp="Thank you for your help!",
            system_message_jp="Quest Complete!"
        )
    """

    # Item rewards
    items: List[RewardItem] = field(default_factory=list)

    # Flag changes
    flags: Dict[str, Any] = field(default_factory=dict)

    # NPC message
    message_jp: str = ""
    message_en: str = ""

    # System message (announcements, achievements, etc.)
    system_message_jp: str = ""
    system_message_en: str = ""


# ============================================================================
# NPC Types
# ============================================================================


@dataclass
class NpcDefinition:
    """
    NPC definition for CWL Chara Excel generation.

    This defines all the properties needed to generate a CWL-compatible
    character entry.

    Required fields:
    - id: Unique character ID
    - name_jp/name_en: Display names
    - race: Character race (e.g., 'human', 'elf', 'succubus')
    - job: Character job/class (e.g., 'warrior', 'wizard', 'merchant')

    Example:
        NpcDefinition(
            id="my_merchant",
            name_jp="商人ジョン",
            name_en="John the Merchant",
            race="human",
            job="merchant",
            lv=50,
            hostility="Friend",
            zone_id="my_zone",
            drama_id="drama_my_merchant"
        )
    """

    id: str
    name_jp: str
    name_en: str = ""
    name_cn: str = ""  # Chinese name
    aka_jp: str = ""  # Title/alias (Japanese)
    aka_en: str = ""  # Title/alias (English)
    aka_cn: str = ""  # Title/alias (Chinese)
    race: str = "human"
    job: str = "warrior"
    lv: int = 1
    hostility: str = "Friend"  # Friend, Neutral, Enemy
    tiles: int = 0  # Fallback tile ID
    render_data: str = "@chara"  # Render data (_idRenderData)
    bio: str = ""  # Character bio string
    id_text: str = ""  # Text ID for dialogue
    portrait: str = ""  # Custom portrait ID

    # CWL tags
    zone_id: str = ""  # Zone to spawn in (addZone_xxx)
    stay_home: bool = True  # Disable random movement (addFlag_StayHomeZone)
    drama_id: str = ""  # Drama to link (addDrama_xxx)
    stock_id: str = ""  # Merchant stock (addStock_xxx)
    human_speak: bool = True  # Human-style dialogue (humanSpeak)
    extra_tags: List[str] = field(default_factory=list)  # Additional tags

    # Stats
    main_element: str = ""
    elements: str = ""  # Element string
    act_combat: str = ""  # Combat actions
    trait: str = ""  # Trait class name
    quality: int = 3
    chance: int = 0  # 0 = no random spawn

    # Author info
    author: str = ""


# ============================================================================
# Item Types
# ============================================================================


@dataclass
class ItemDefinition:
    """
    Item definition for CWL Thing Excel generation.

    Example:
        ItemDefinition(
            id="my_sword",
            name_jp="伝説の剣",
            name_en="Legendary Sword",
            category="weapon",
            value=10000,
            trait_name="MyCustomTrait"
        )
    """

    id: str
    name_jp: str
    name_en: str = ""
    name_cn: str = ""  # Chinese name
    category: str = "other"
    detail_jp: str = ""
    detail_en: str = ""
    detail_cn: str = ""  # Chinese description

    # Game data
    value: int = 100
    lv: int = 1
    weight: int = 1000
    chance: int = 0  # 0 = no random generation

    # Rendering
    tiles: int = 0
    render_data: str = ""

    # Trait
    trait_name: str = ""
    trait_params: List[str] = field(default_factory=list)

    # Elements/enchantments
    elements: str = ""

    # Equipment
    def_mat: str = ""
    tier_group: str = ""
    defense: str = ""

    # Tags
    tags: List[str] = field(default_factory=list)
