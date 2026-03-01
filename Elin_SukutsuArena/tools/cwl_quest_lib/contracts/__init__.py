# -*- coding: utf-8 -*-
"""
CWL Quest Library - JSON Contract System

This package provides type-safe JSON contracts for Python-C# interoperability.
Contracts are validated at build time to catch type mismatches early.

Usage:
    from cwl_quest_lib.contracts import QuestContract, validate_quest_json

    # Validate JSON file against contract
    validate_quest_json("quest_definitions.json")

    # Generate C# class from contract
    from cwl_quest_lib.codegen import generate_quest_data_class
    csharp_code = generate_quest_data_class()
"""

from cwl_quest_lib.contracts.quest_contract import (
    FlagConditionContract,
    QuestContract,
    QuestListContract,
)
from cwl_quest_lib.contracts.validator import (
    ContractValidationError,
    validate_json_file,
    validate_quest_json,
)
from cwl_quest_lib.contracts.csharp_validator import (
    CSharpContractMismatchError,
    TypeMismatch,
    validate_csharp_matches_contract,
    validate_quest_data_class,
)

__all__ = [
    # Contracts
    "FlagConditionContract",
    "QuestContract",
    "QuestListContract",
    # JSON Validation
    "ContractValidationError",
    "validate_json_file",
    "validate_quest_json",
    # C# Validation
    "CSharpContractMismatchError",
    "TypeMismatch",
    "validate_csharp_matches_contract",
    "validate_quest_data_class",
]
