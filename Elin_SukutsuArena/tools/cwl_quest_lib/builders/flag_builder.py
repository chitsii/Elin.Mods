"""
CWL Quest Library - Generic Flag Builder

This module provides generic flag definition classes that can be reused
across different CWL mods. Game-specific flag definitions should extend
these base classes.

Usage:
    from cwl_quest_lib.flag_builder import FlagDef, EnumFlag, IntFlag, BoolFlag

    class MyFlags:
        motivation = EnumFlag(
            key="player.motivation",
            enum_type=MyMotivationEnum,
            description="Player motivation"
        )
"""

from dataclasses import dataclass, field
from enum import Enum
from typing import List, Optional, Type, Any


# ============================================================================
# Flag Definition Classes (Generic)
# ============================================================================


@dataclass
class FlagDef:
    """
    Base class for flag definitions.

    Attributes:
        key: The flag key (without prefix)
        description: Human-readable description
        prefix: Flag prefix (set by QuestConfig)
    """

    key: str
    description: str = ""
    _prefix: str = field(default="", repr=False)

    @property
    def full_key(self) -> str:
        """Get the full flag key with prefix."""
        if self._prefix:
            return f"{self._prefix}.{self.key}"
        return self.key

    def with_prefix(self, prefix: str) -> "FlagDef":
        """Create a copy with the specified prefix."""
        import copy

        new_flag = copy.copy(self)
        new_flag._prefix = prefix
        return new_flag


@dataclass
class EnumFlag(FlagDef):
    """
    Enum type flag.

    The value is stored as the enum's ordinal (integer index).

    Attributes:
        enum_type: The Enum class for this flag
        default: Default enum value (or None)
    """

    enum_type: Type[Enum] = None
    default: Optional[Enum] = None


@dataclass
class IntFlag(FlagDef):
    """
    Integer type flag.

    Attributes:
        default: Default value
        min_value: Minimum allowed value
        max_value: Maximum allowed value
    """

    default: int = 0
    min_value: int = -100
    max_value: int = 100


@dataclass
class BoolFlag(FlagDef):
    """
    Boolean type flag.

    Stored as 0 (False) or 1 (True) in dialogFlags.

    Attributes:
        default: Default value
    """

    default: bool = False


@dataclass
class StringFlag(FlagDef):
    """
    String type flag.

    Note: dialogFlags in Elin only supports integers, so string flags
    may require special handling or storage in a different system.

    Attributes:
        default: Default value
    """

    default: Optional[str] = None


# ============================================================================
# Helper Functions
# ============================================================================


def get_all_flags_from_class(flags_class: type) -> List[FlagDef]:
    """
    Extract all FlagDef instances from a flags class.

    Args:
        flags_class: A class containing FlagDef class attributes

    Returns:
        List of all FlagDef instances found in the class

    Example:
        class PlayerFlags:
            rank = EnumFlag(key="player.rank", ...)
            karma = IntFlag(key="player.karma", ...)

        flags = get_all_flags_from_class(PlayerFlags)
        # Returns [EnumFlag(...), IntFlag(...)]
    """
    flags = []
    for name in dir(flags_class):
        if not name.startswith("_"):
            attr = getattr(flags_class, name)
            if isinstance(attr, FlagDef):
                flags.append(attr)
    return flags


def validate_flag_value(flag: FlagDef, value: Any) -> List[str]:
    """
    Validate a value against a flag definition.

    Args:
        flag: The flag definition
        value: The value to validate

    Returns:
        List of error messages (empty if valid)
    """
    errors = []

    if isinstance(flag, EnumFlag):
        if flag.enum_type:
            valid_ordinals = list(range(len(flag.enum_type)))
            if isinstance(value, int) and value not in valid_ordinals and value != -1:
                errors.append(
                    f"Invalid ordinal {value} for enum flag '{flag.full_key}'. "
                    f"Valid: {valid_ordinals} or -1 (null)"
                )
    elif isinstance(flag, IntFlag):
        if not isinstance(value, int):
            errors.append(
                f"Flag '{flag.full_key}' expects int, got {type(value).__name__}"
            )
        elif value < flag.min_value or value > flag.max_value:
            errors.append(
                f"Value {value} out of range for '{flag.full_key}'. "
                f"Expected: [{flag.min_value}, {flag.max_value}]"
            )
    elif isinstance(flag, BoolFlag):
        if value not in (0, 1, True, False):
            errors.append(f"Flag '{flag.full_key}' expects bool (0/1), got {value}")

    return errors


# ============================================================================
# Unit Tests
# ============================================================================

if __name__ == "__main__":
    print("=== Flag Builder Test ===\n")

    # Test basic flag creation
    test_flag = IntFlag(
        key="player.karma",
        description="Player karma value",
        default=0,
        min_value=-100,
        max_value=100,
    )
    print(f"Created flag: {test_flag}")
    print(f"  key: {test_flag.key}")
    print(f"  full_key (no prefix): {test_flag.full_key}")

    # Test with prefix
    test_flag_with_prefix = test_flag.with_prefix("test.mod")
    print(f"  full_key (with prefix): {test_flag_with_prefix.full_key}")

    # Test validation
    errors = validate_flag_value(test_flag, 50)
    print(f"\nValidate 50: {errors if errors else 'OK'}")

    errors = validate_flag_value(test_flag, 150)
    print(f"Validate 150: {errors}")

    # Test flags class extraction
    class TestFlags:
        karma = IntFlag(key="player.karma", default=0)
        active = BoolFlag(key="player.active", default=True)

    extracted = get_all_flags_from_class(TestFlags)
    print(f"\nExtracted {len(extracted)} flags from TestFlags")
    for f in extracted:
        print(f"  - {f.key}: {type(f).__name__}")

    print("\n=== All Tests Passed! ===")
