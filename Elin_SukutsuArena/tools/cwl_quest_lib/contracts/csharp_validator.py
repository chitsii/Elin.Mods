# -*- coding: utf-8 -*-
"""
CWL Quest Library - C# Code Contract Validator

This module validates that existing C# classes match the Python contract types.
It prevents runtime deserialization errors by catching type mismatches at build time.

Usage:
    from cwl_quest_lib.contracts.csharp_validator import validate_csharp_matches_contract

    errors = validate_csharp_matches_contract(
        csharp_file="src/ArenaQuestManager.cs",
        class_name="QuestData",
        contract_class=QuestContract
    )
    if errors:
        raise CSharpContractMismatchError(errors)
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, List, Optional, Type, get_args, get_origin

from pydantic import BaseModel


@dataclass
class TypeMismatch:
    """Represents a type mismatch between C# and Python contract."""

    json_property: str
    csharp_type: str
    expected_type: str
    csharp_file: str
    line_number: int

    def __str__(self) -> str:
        return (
            f"  - {self.json_property}: C# has '{self.csharp_type}' "
            f"but contract expects '{self.expected_type}' "
            f"({self.csharp_file}:{self.line_number})"
        )


class CSharpContractMismatchError(Exception):
    """Raised when C# code doesn't match the Python contract."""

    def __init__(
        self, mismatches: List[TypeMismatch], csharp_file: str, class_name: str
    ):
        self.mismatches = mismatches
        self.csharp_file = csharp_file
        self.class_name = class_name

        msg = self._format_error_message()
        super().__init__(msg)

    def _format_error_message(self) -> str:
        lines = [
            "=" * 80,
            f"C# CONTRACT MISMATCH: {self.class_name} in {self.csharp_file}",
            "=" * 80,
            "",
            "The following fields have type mismatches between C# and the Python contract:",
            "",
        ]

        for mismatch in self.mismatches:
            lines.append(str(mismatch))

        lines.extend(
            [
                "",
                "This can cause JSON deserialization failures at runtime!",
                "",
                "Options to fix:",
                "  1. Update C# types to match the contract",
                "  2. Update the Python contract if C# types are correct",
                "  3. Use generated QuestDataContract.cs instead of hand-written classes",
                "",
                "=" * 80,
            ]
        )

        return "\n".join(lines)


# C# type normalization map
CSHARP_TYPE_ALIASES = {
    "string": "string",
    "String": "string",
    "int": "int",
    "Int32": "int",
    "bool": "bool",
    "Boolean": "bool",
    "float": "float",
    "Single": "float",
    "double": "double",
    "Double": "double",
    "object": "object",
    "Object": "object",
}


def normalize_csharp_type(csharp_type: str) -> str:
    """Normalize C# type to standard form."""
    # Remove whitespace
    csharp_type = csharp_type.strip()

    # Check aliases
    if csharp_type in CSHARP_TYPE_ALIASES:
        return CSHARP_TYPE_ALIASES[csharp_type]

    # Handle List<T>
    list_match = re.match(r"List<(.+)>", csharp_type)
    if list_match:
        inner = normalize_csharp_type(list_match.group(1))
        return f"List<{inner}>"

    # Handle Dictionary<K, V>
    dict_match = re.match(r"Dictionary<(.+),\s*(.+)>", csharp_type)
    if dict_match:
        key_type = normalize_csharp_type(dict_match.group(1))
        value_type = normalize_csharp_type(dict_match.group(2))
        return f"Dictionary<{key_type}, {value_type}>"

    return csharp_type


def python_type_to_csharp(python_type: Any) -> str:
    """Convert Python type annotation to expected C# type string."""
    # Handle None type
    if python_type is None or python_type is type(None):
        return "null"

    # Get the origin (e.g., list, dict, Optional)
    origin = get_origin(python_type)
    args = get_args(python_type)

    # Handle Optional[T] -> T (nullable)
    if origin is type(None):
        return "null"

    # Handle Union (Optional is Union[T, None])
    import typing

    if hasattr(typing, "UnionType"):
        # Python 3.10+ union syntax (X | Y)
        if isinstance(python_type, typing.UnionType):  # type: ignore
            non_none_args = [a for a in args if a is not type(None)]
            if len(non_none_args) == 1:
                return python_type_to_csharp(non_none_args[0])

    if origin is type(None):
        return "null"

    # typing.Union
    try:
        from typing import Union

        if origin is Union:
            non_none_args = [a for a in args if a is not type(None)]
            if len(non_none_args) == 1:
                return python_type_to_csharp(non_none_args[0])
    except ImportError:
        pass

    # Handle List[T]
    if origin is list:
        if args:
            inner_type = python_type_to_csharp(args[0])
            return f"List<{inner_type}>"
        return "List<object>"

    # Handle Dict[K, V]
    if origin is dict:
        if len(args) >= 2:
            key_type = python_type_to_csharp(args[0])
            value_type = python_type_to_csharp(args[1])
            return f"Dictionary<{key_type}, {value_type}>"
        return "Dictionary<object, object>"

    # Handle Literal (always string for our use case)
    try:
        from typing import Literal

        if origin is Literal:
            return "string"
    except ImportError:
        pass

    # Handle basic types
    type_name = getattr(python_type, "__name__", str(python_type))

    type_map = {
        "str": "string",
        "int": "int",
        "bool": "bool",
        "float": "double",
        "Any": "object",
        "NoneType": "null",
    }

    if type_name in type_map:
        return type_map[type_name]

    # Handle Pydantic models (e.g., FlagConditionContract -> FlagConditionData)
    if isinstance(python_type, type) and issubclass(python_type, BaseModel):
        # Map contract name to C# class name
        class_name = python_type.__name__
        if class_name.endswith("Contract"):
            # FlagConditionContract -> FlagConditionData
            return class_name.replace("Contract", "Data")
        return class_name

    return type_name


@dataclass
class CSharpField:
    """Parsed C# field or property."""

    json_property: str
    csharp_type: str
    field_name: str
    line_number: int


def parse_csharp_class(csharp_code: str, class_name: str) -> Dict[str, CSharpField]:
    """
    Parse C# code to extract fields with [JsonProperty] attributes.

    Returns:
        Dict mapping JSON property name to CSharpField
    """
    fields: Dict[str, CSharpField] = {}

    # Find the class
    class_pattern = rf"(public\s+)?class\s+{re.escape(class_name)}\b"
    class_match = re.search(class_pattern, csharp_code)

    if not class_match:
        return fields

    # Find class body (simple brace matching)
    class_start = class_match.end()
    brace_start = csharp_code.find("{", class_start)
    if brace_start == -1:
        return fields

    # Find matching closing brace
    brace_count = 1
    pos = brace_start + 1
    while brace_count > 0 and pos < len(csharp_code):
        if csharp_code[pos] == "{":
            brace_count += 1
        elif csharp_code[pos] == "}":
            brace_count -= 1
        pos += 1

    class_body = csharp_code[brace_start:pos]
    class_body_start_line = csharp_code[:brace_start].count("\n") + 1

    # Pattern to match [JsonProperty("name")] followed by field/property declaration
    # Handles both:
    #   [JsonProperty("field")] public Type Name;
    #   [JsonProperty("field")] public Type Name { get; set; }
    pattern = r'\[JsonProperty\(["\']([^"\']+)["\']\)\]\s*\n?\s*public\s+([^\s;{]+(?:<[^>]+>)?)\s+(\w+)\s*[;{=]'

    for match in re.finditer(pattern, class_body):
        json_name = match.group(1)
        csharp_type = normalize_csharp_type(match.group(2))
        field_name = match.group(3)

        # Calculate line number
        line_offset = class_body[: match.start()].count("\n")
        line_number = class_body_start_line + line_offset

        fields[json_name] = CSharpField(
            json_property=json_name,
            csharp_type=csharp_type,
            field_name=field_name,
            line_number=line_number,
        )

    # Also match fields without [JsonProperty] (using field name as JSON key)
    # Pattern: public Type fieldName; or public Type fieldName = ...;
    simple_field_pattern = r"public\s+([^\s;{]+(?:<[^>]+>)?)\s+(\w+)\s*[;=]"

    for match in re.finditer(simple_field_pattern, class_body):
        csharp_type = normalize_csharp_type(match.group(1))
        field_name = match.group(2)

        # Skip if already captured via [JsonProperty]
        if field_name in fields:
            continue

        # Use camelCase field name as JSON property name
        json_name = field_name

        # Calculate line number
        line_offset = class_body[: match.start()].count("\n")
        line_number = class_body_start_line + line_offset

        fields[json_name] = CSharpField(
            json_property=json_name,
            csharp_type=csharp_type,
            field_name=field_name,
            line_number=line_number,
        )

    return fields


def validate_csharp_matches_contract(
    csharp_file: str | Path,
    class_name: str,
    contract_class: Type[BaseModel],
    strict: bool = False,
) -> List[TypeMismatch]:
    """
    Validate that a C# class matches a Pydantic contract.

    Args:
        csharp_file: Path to C# source file
        class_name: Name of C# class to validate
        contract_class: Pydantic BaseModel class (the contract)
        strict: If True, also check for missing fields

    Returns:
        List of TypeMismatch objects (empty if valid)
    """
    csharp_path = Path(csharp_file)

    if not csharp_path.exists():
        # File doesn't exist - skip validation
        return []

    csharp_code = csharp_path.read_text(encoding="utf-8")

    # Parse C# class
    csharp_fields = parse_csharp_class(csharp_code, class_name)

    if not csharp_fields:
        # Class not found - skip validation
        return []

    mismatches: List[TypeMismatch] = []

    # Get contract fields with their aliases
    for field_name, field_info in contract_class.model_fields.items():
        # Get JSON property name (alias or field name)
        json_name = field_info.alias if field_info.alias else field_name

        if json_name not in csharp_fields:
            if strict:
                # Field missing in C#
                expected = python_type_to_csharp(field_info.annotation)
                mismatches.append(
                    TypeMismatch(
                        json_property=json_name,
                        csharp_type="<missing>",
                        expected_type=expected,
                        csharp_file=str(csharp_path),
                        line_number=0,
                    )
                )
            continue

        csharp_field = csharp_fields[json_name]
        expected_csharp_type = python_type_to_csharp(field_info.annotation)

        # Compare types
        if not types_are_compatible(csharp_field.csharp_type, expected_csharp_type):
            mismatches.append(
                TypeMismatch(
                    json_property=json_name,
                    csharp_type=csharp_field.csharp_type,
                    expected_type=expected_csharp_type,
                    csharp_file=str(csharp_path),
                    line_number=csharp_field.line_number,
                )
            )

    return mismatches


def types_are_compatible(csharp_type: str, expected_type: str) -> bool:
    """
    Check if C# type is compatible with expected type from contract.

    Allows some flexibility:
    - object is compatible with any type
    - Nullable reference types are compatible with non-nullable
    """
    # Exact match
    if csharp_type == expected_type:
        return True

    # object is always compatible (dynamic typing)
    if expected_type == "object":
        return True

    # Handle nullable reference types (string? == string)
    if csharp_type.rstrip("?") == expected_type.rstrip("?"):
        return True

    # FlagConditionData vs FlagCondition (accept both naming conventions)
    if "FlagCondition" in csharp_type and "FlagCondition" in expected_type:
        csharp_base = csharp_type.replace("FlagConditionData", "FlagCondition")
        expected_base = expected_type.replace("FlagConditionData", "FlagCondition")
        if csharp_base == expected_base:
            return True

    return False


def validate_quest_data_class(
    project_root: str | Path,
    csharp_file: str = "src/ArenaQuestManager.cs",
    class_name: str = "QuestData",
) -> List[TypeMismatch]:
    """
    Validate QuestData class against QuestContract.

    This is a convenience function for the arena project.

    Args:
        project_root: Project root directory
        csharp_file: Relative path to C# file
        class_name: Name of C# class

    Returns:
        List of mismatches (empty if valid)
    """
    from cwl_quest_lib.contracts.quest_contract import QuestContract

    csharp_path = Path(project_root) / csharp_file

    return validate_csharp_matches_contract(
        csharp_file=csharp_path,
        class_name=class_name,
        contract_class=QuestContract,
        strict=False,
    )


# ============================================================================
# CLI Entry Point
# ============================================================================


def main():
    """CLI entry point for C# contract validation."""
    import argparse
    import sys

    parser = argparse.ArgumentParser(
        description="Validate C# classes against Python contracts"
    )
    parser.add_argument("--project-root", default="..", help="Project root directory")
    parser.add_argument(
        "--csharp-file",
        default="src/ArenaQuestManager.cs",
        help="Path to C# file (relative to project root)",
    )
    parser.add_argument(
        "--class-name", default="QuestData", help="Name of C# class to validate"
    )
    parser.add_argument(
        "--strict", action="store_true", help="Also check for missing fields"
    )

    args = parser.parse_args()

    from cwl_quest_lib.contracts.quest_contract import QuestContract

    csharp_path = Path(args.project_root) / args.csharp_file

    mismatches = validate_csharp_matches_contract(
        csharp_file=csharp_path,
        class_name=args.class_name,
        contract_class=QuestContract,
        strict=args.strict,
    )

    if mismatches:
        error = CSharpContractMismatchError(
            mismatches=mismatches,
            csharp_file=str(csharp_path),
            class_name=args.class_name,
        )
        print(str(error), file=sys.stderr)
        sys.exit(1)
    else:
        print(f"✓ C# class {args.class_name} matches contract")
        sys.exit(0)


if __name__ == "__main__":
    main()
