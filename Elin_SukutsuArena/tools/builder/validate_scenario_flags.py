"""
Scenario Flag Validator

Validates flag usage in scenarios.py against flag_definitions.py.
Run during build to catch typos and invalid values.
"""

import re
import sys
import os

# Path setup
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
sys.path.insert(0, TOOLS_DIR)

from typing import List, Tuple, Set
from arena.data import (
    get_all_flags,
    get_all_enums,
    EnumFlag,
    IntFlag,
    BoolFlag,
    StringFlag,
    PREFIX,
)


class ValidationError:
    def __init__(self, file: str, line: int, message: str):
        self.file = file
        self.line = line
        self.message = message

    def __str__(self):
        return f"{self.file}:{self.line}: {self.message}"


def get_valid_flag_keys() -> Set[str]:
    """Get all valid flag keys"""
    return {flag.full_key for flag in get_all_flags()}


def get_enum_values() -> dict:
    """Get all valid enum values mapped by flag key"""
    result = {}
    for flag in get_all_flags():
        if isinstance(flag, EnumFlag) and flag.enum_type:
            result[flag.full_key] = {e.value for e in flag.enum_type}
    return result


def validate_file(filepath: str) -> List[ValidationError]:
    """Validate a Python file for flag usage"""
    errors = []
    valid_keys = get_valid_flag_keys()
    enum_values = get_enum_values()

    # Pattern to find strings that look like flag keys
    flag_pattern = re.compile(rf'["\']({re.escape(PREFIX)}\.[a-z_.]+)["\']')

    # Pattern to find flag assignments like .flag("key", "value") or builder.flag("key", value)
    flag_assign_pattern = re.compile(
        rf'\.flag\s*\(\s*["\']({re.escape(PREFIX)}\.[a-z_.]+)["\']\s*,\s*["\']?([^"\'\)]+)["\']?\s*\)'
    )

    try:
        with open(filepath, "r", encoding="utf-8") as f:
            lines = f.readlines()
    except FileNotFoundError:
        return [ValidationError(filepath, 0, f"File not found: {filepath}")]

    for line_num, line in enumerate(lines, 1):
        # Skip comments
        if line.strip().startswith("#"):
            continue

        # Check for unknown flag keys
        for match in flag_pattern.finditer(line):
            key = match.group(1)
            if key not in valid_keys:
                errors.append(
                    ValidationError(filepath, line_num, f"Unknown flag key: '{key}'")
                )

        # Check for invalid enum values in flag assignments
        for match in flag_assign_pattern.finditer(line):
            key = match.group(1)
            value = match.group(2).strip("\"'")

            if key in enum_values:
                valid_values = enum_values[key]
                # Allow special values like "None" or variable references
                if (
                    not value.startswith(("None", "null", "True", "False"))
                    and not value[0].isupper()
                    and value not in valid_values
                ):
                    errors.append(
                        ValidationError(
                            filepath,
                            line_num,
                            f"Invalid value '{value}' for enum flag '{key}'. "
                            f"Valid values: {valid_values}",
                        )
                    )

    return errors


def validate_scenarios() -> Tuple[int, int]:
    """Validate all scenario files. Returns (error_count, warning_count)"""

    # arena/scenarios ディレクトリ内のシナリオファイルを検索
    scenarios_dir = os.path.join(TOOLS_DIR, "arena", "scenarios")
    files_to_check = []

    # Check scenario files in arena/scenarios
    if os.path.exists(scenarios_dir):
        for root, dirs, files in os.walk(scenarios_dir):
            for fname in files:
                if fname.endswith(".py") and fname != "__init__.py":
                    files_to_check.append(os.path.join(root, fname))

    total_errors = 0

    for filepath in files_to_check:
        if not os.path.exists(filepath):
            continue

        errors = validate_file(filepath)
        total_errors += len(errors)

        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)

    return total_errors, 0


def main():
    print("=== Arena Flag Validator ===\n")

    # Show valid flags for reference
    print(f"Valid flag keys ({len(get_valid_flag_keys())}):")
    for key in sorted(get_valid_flag_keys()):
        print(f"  - {key}")

    print()

    # Run validation
    errors, warnings = validate_scenarios()

    if errors == 0:
        print("\n[OK] All flag usages are valid!")
        return 0
    else:
        print(f"\n[ERROR] Found {errors} error(s)")
        return 1


if __name__ == "__main__":
    sys.exit(main())
