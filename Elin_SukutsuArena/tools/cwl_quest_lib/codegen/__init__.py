# -*- coding: utf-8 -*-
"""
CWL Quest Library - C# Code Generation

This package generates C# classes from Python Pydantic contracts.
Generated code ensures type synchronization between Python and C#.

Usage:
    from cwl_quest_lib.codegen import generate_quest_data_class

    # Generate C# class for quest data
    csharp_code = generate_quest_data_class()

    # Write to file
    with open("QuestDataContract.cs", "w") as f:
        f.write(csharp_code)
"""

from cwl_quest_lib.codegen.csharp_generator import (
    generate_csharp_class,
    generate_flag_condition_class,
    generate_quest_data_class,
)

__all__ = [
    "generate_csharp_class",
    "generate_flag_condition_class",
    "generate_quest_data_class",
]
