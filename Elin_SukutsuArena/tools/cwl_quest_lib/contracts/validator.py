# -*- coding: utf-8 -*-
"""
CWL Quest Library - Contract Validator

This module provides validation logic for JSON contracts.
Validation runs at build time to catch type mismatches early.

Usage:
    from cwl_quest_lib.contracts.validator import validate_quest_json

    # Validate quest_definitions.json
    validate_quest_json("Package/quest_definitions.json")

    # Custom contract validation
    from pydantic import BaseModel
    validate_json_file("output.json", MyContract)
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any, List, Type, TypeVar, Union

from pydantic import BaseModel, ValidationError

from cwl_quest_lib.contracts.quest_contract import QuestContract

T = TypeVar("T", bound=BaseModel)


class ContractValidationError(Exception):
    """
    Raised when JSON data fails contract validation.

    Attributes:
        file_path: Path to the JSON file
        errors: List of validation error details
        raw_errors: Pydantic ValidationError (if available)
    """

    def __init__(
        self,
        file_path: str,
        errors: List[dict],
        raw_errors: ValidationError | None = None,
    ):
        self.file_path = file_path
        self.errors = errors
        self.raw_errors = raw_errors

        # Build error message
        error_lines = [
            "=" * 80,
            f"Contract Validation FAILED: {Path(file_path).name}",
            "=" * 80,
            "Errors:",
        ]
        for err in errors:
            loc = ".".join(str(x) for x in err.get("loc", []))
            msg = err.get("msg", "Unknown error")
            error_lines.append(f"  - [{loc}]: {msg}")

        error_lines.extend(
            [
                "",
                "Fix: Ensure JSON structure matches the contract definition.",
                "     See cwl_quest_lib/contracts/quest_contract.py for the expected schema.",
                "=" * 80,
                "BUILD ABORTED",
                "=" * 80,
            ]
        )

        super().__init__("\n".join(error_lines))


def validate_json_file(
    file_path: Union[str, Path], contract: Type[T], is_array: bool = False
) -> List[T] | T:
    """
    Validate a JSON file against a Pydantic contract.

    Args:
        file_path: Path to the JSON file
        contract: Pydantic model class to validate against
        is_array: If True, the JSON is expected to be an array of objects

    Returns:
        Validated model instance(s)

    Raises:
        ContractValidationError: If validation fails
        FileNotFoundError: If JSON file doesn't exist
        json.JSONDecodeError: If JSON is malformed
    """
    file_path = Path(file_path)

    if not file_path.exists():
        raise FileNotFoundError(f"JSON file not found: {file_path}")

    with open(file_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    try:
        if is_array:
            if not isinstance(data, list):
                raise ContractValidationError(
                    str(file_path),
                    [{"loc": [], "msg": f"Expected array, got {type(data).__name__}"}],
                )
            return [contract(**item) for item in data]
        else:
            return contract(**data)
    except ValidationError as e:
        raise ContractValidationError(str(file_path), e.errors(), raw_errors=e)


def validate_quest_json(file_path: Union[str, Path]) -> List[QuestContract]:
    """
    Validate quest_definitions.json against QuestContract.

    This is the primary validation function for quest data.

    Args:
        file_path: Path to quest_definitions.json

    Returns:
        List of validated QuestContract instances

    Raises:
        ContractValidationError: If validation fails

    Example:
        >>> from cwl_quest_lib.contracts.validator import validate_quest_json
        >>> quests = validate_quest_json("Package/quest_definitions.json")
        >>> print(f"Validated {len(quests)} quests")
    """
    return validate_json_file(file_path, QuestContract, is_array=True)


def validate_quest_data(data: List[dict]) -> List[QuestContract]:
    """
    Validate quest data in memory (without reading from file).

    Useful for validating data before writing to JSON.

    Args:
        data: List of quest dictionaries

    Returns:
        List of validated QuestContract instances

    Raises:
        ContractValidationError: If validation fails
    """
    errors = []
    validated = []

    for i, item in enumerate(data):
        try:
            validated.append(QuestContract(**item))
        except ValidationError as e:
            for err in e.errors():
                err_copy = err.copy()
                err_copy["loc"] = (i,) + tuple(err_copy.get("loc", ()))
                errors.append(err_copy)

    if errors:
        raise ContractValidationError("<in-memory>", errors)

    return validated


def get_contract_schema(contract: Type[BaseModel]) -> dict:
    """
    Get JSON Schema for a contract.

    Useful for generating documentation or external validation.

    Args:
        contract: Pydantic model class

    Returns:
        JSON Schema dictionary
    """
    return contract.model_json_schema()


def print_validation_result(file_path: str, success: bool, quest_count: int = 0):
    """Print validation result in a consistent format."""
    if success:
        print(
            f"[PASS] Contract validation: {Path(file_path).name} ({quest_count} quests)"
        )
    else:
        print(f"[FAIL] Contract validation: {Path(file_path).name}")
