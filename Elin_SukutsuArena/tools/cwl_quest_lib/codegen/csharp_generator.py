# -*- coding: utf-8 -*-
"""
CWL Quest Library - C# Class Generator

This module generates C# classes from Python Pydantic contracts.
Generated code matches the JSON structure exactly for type-safe deserialization.

Usage:
    from cwl_quest_lib.codegen.csharp_generator import generate_quest_data_class

    csharp_code = generate_quest_data_class()
    print(csharp_code)
"""

from __future__ import annotations


from typing import Any, Dict, List, Optional, Type, get_args, get_origin

from pydantic import BaseModel
from pydantic.fields import FieldInfo

from cwl_quest_lib.contracts.quest_contract import (
    FlagConditionContract,
    QuestContract,
)


# Python type to C# type mapping
TYPE_MAPPING: Dict[str, str] = {
    "str": "string",
    "int": "int",
    "bool": "bool",
    "float": "double",
    "NoneType": "null",
    "Any": "object",
    "FlagConditionContract": "FlagConditionDto",
}


def python_type_to_csharp(python_type: Any) -> str:
    """
    Convert a Python type annotation to C# type.

    Args:
        python_type: Python type annotation

    Returns:
        C# type string
    """
    from typing import Literal as TypingLiteral

    # Handle None
    if python_type is None or python_type is type(None):
        return "object"

    # Handle string type names
    if isinstance(python_type, str):
        return TYPE_MAPPING.get(python_type, python_type)

    # Get the origin type (e.g., List from List[str])
    origin = get_origin(python_type)
    args = get_args(python_type)

    # Handle Optional[T] (Union[T, None])
    if origin is type(None):
        return "object"

    # Handle Literal types - treat as string (C# uses enums or strings)
    if origin is TypingLiteral:
        return "string"

    # Check for Optional (Union with None)
    if hasattr(python_type, "__origin__"):
        origin_name = getattr(
            python_type.__origin__, "__name__", str(python_type.__origin__)
        )

        if origin_name == "Union":
            # Filter out NoneType
            non_none_args = [a for a in args if a is not type(None)]
            if len(non_none_args) == 1:
                inner = python_type_to_csharp(non_none_args[0])
                # Value types need ? for nullable
                if inner in ("int", "bool", "double", "float"):
                    return f"{inner}?"
                return inner  # Reference types are already nullable in C#

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
            val_type = python_type_to_csharp(args[1])
            return f"Dictionary<{key_type}, {val_type}>"
        return "Dictionary<string, object>"

    # Handle basic types
    type_name = getattr(python_type, "__name__", str(python_type))
    return TYPE_MAPPING.get(type_name, type_name)


def get_json_property_name(field_name: str, field_info: FieldInfo) -> str:
    """Get the JSON property name (alias or field name)."""
    if field_info.alias:
        return field_info.alias
    return field_name


def generate_csharp_class(
    model: Type[BaseModel],
    class_name: Optional[str] = None,
    namespace: str = "CWL.Quest",
    include_json_attributes: bool = True,
) -> str:
    """
    Generate a C# class from a Pydantic model.

    Args:
        model: Pydantic model class
        class_name: Override class name (default: model.__name__)
        namespace: C# namespace
        include_json_attributes: Include [JsonProperty] attributes

    Returns:
        C# class definition as string
    """
    class_name = class_name or model.__name__.replace("Contract", "")
    lines = []

    # Class definition
    lines.append(f"    public class {class_name}")
    lines.append("    {")

    # Process fields
    for field_name, field_info in model.model_fields.items():
        json_name = get_json_property_name(field_name, field_info)
        csharp_type = python_type_to_csharp(field_info.annotation)

        # Convert alias name to PascalCase for C# property name
        # Use alias (json_name) as base, capitalize first letter
        # e.g., "displayNameJP" -> "DisplayNameJP"
        pascal_name = json_name[0].upper() + json_name[1:] if json_name else field_name

        # Add JsonProperty attribute if alias differs
        if include_json_attributes and json_name != pascal_name:
            lines.append(f'        [JsonProperty("{json_name}")]')

        lines.append(f"        public {csharp_type} {pascal_name} {{ get; set; }}")
        lines.append("")

    lines.append("    }")

    return "\n".join(lines)


def generate_flag_condition_class(namespace: str = "CWL.Quest") -> str:
    """Generate C# class for FlagConditionDto."""
    return generate_csharp_class(
        FlagConditionContract,
        class_name="FlagConditionDto",
        namespace=namespace,
    )


def generate_quest_data_class(namespace: str = "CWL.Quest") -> str:
    """
    Generate complete C# file with all quest-related classes.

    Returns:
        Complete C# file content
    """
    header = """// =============================================================================
// AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
// Generated by cwl_quest_lib.codegen.csharp_generator
//
// This file is generated from Python contract definitions.
// Any changes will be overwritten on next build.
//
// Source: cwl_quest_lib/contracts/quest_contract.py
// =============================================================================

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace {namespace}
{{
"""

    footer = """
}
"""

    # Generate classes
    flag_condition = generate_flag_condition_class(namespace)
    quest_data = generate_csharp_class(
        QuestContract, class_name="QuestDataDto", namespace=namespace
    )

    return header + flag_condition + "\n" + quest_data + footer


def write_quest_data_contract(output_path: str, namespace: str = "CWL.Quest") -> str:
    """
    Generate and write C# quest data contract to file.

    Args:
        output_path: Path to write the C# file
        namespace: C# namespace

    Returns:
        Path to written file
    """
    content = generate_quest_data_class(namespace)

    with open(output_path, "w", encoding="utf-8") as f:
        f.write(content)

    return output_path


if __name__ == "__main__":
    # Test generation
    print(generate_quest_data_class())
