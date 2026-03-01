# -*- coding: utf-8 -*-
"""
CWL Quest Library - Unified Build System

This module provides a unified build system for CWL mods, integrating all
Python-based generation steps into a single entry point.

Usage:
    from cwl_quest_lib.unified_builder import UnifiedBuilder, BuildConfig

    builder = UnifiedBuilder(
        config=BuildConfig(
            project_root=Path("."),
            output_dirs={
                "drama": "LangMod/EN/Dialog/Drama",
                "c_sharp": "src/Generated",
                "json": "Package",
                "tsv_jp": "LangMod/EN",
                "tsv_en": "LangMod/EN",
            },
            scenarios_module="common.scenarios",
            flag_module="common.flag_definitions",
            quest_module="common.quest_dependencies",
        )
    )
    builder.build_all()
"""

from __future__ import annotations

import importlib
import os
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable, Dict, List, Optional

# Type alias for scenario module
ScenarioModule = Any


@dataclass
class BuildConfig:
    """Build configuration for a CWL mod."""

    # Project paths
    project_root: Path = field(default_factory=lambda: Path("."))

    # Output directories (relative to project_root)
    output_dirs: Dict[str, str] = field(
        default_factory=lambda: {
            "drama": "LangMod/EN/Dialog/Drama",
            "c_sharp": "src/Generated",
            "json": "Package",
            "tsv_jp": "LangMod/EN",
            "tsv_en": "LangMod/EN",
        }
    )

    # Module names (for dynamic import)
    scenarios_module: str = "common.scenarios"
    flag_module: str = "common.flag_definitions"
    quest_module: str = "common.quest_dependencies"
    item_module: str = "common.item_definitions"
    npc_module: str = "common.npc_definitions"
    battle_module: str = "common.battle_stages"
    reward_module: str = "common.rewards"

    # Build options
    debug_mode: bool = False
    verbose: bool = False

    def get_output_path(self, key: str) -> Path:
        """Get absolute output path for a key."""
        return self.project_root / self.output_dirs.get(key, key)


@dataclass
class BuildStep:
    """A single build step."""

    name: str
    description: str
    func: Callable[[], bool]
    required: bool = True


class BuildResult:
    """Result of a build step."""

    def __init__(
        self, name: str, success: bool, message: str = "", duration: float = 0.0
    ):
        self.name = name
        self.success = success
        self.message = message
        self.duration = duration

    def __repr__(self) -> str:
        status = "PASS" if self.success else "FAIL"
        return f"[{status}] {self.name}: {self.message}"


class UnifiedBuilder:
    """
    Unified build system for CWL mods.

    This class orchestrates all Python-based generation steps:
    1. Validation
    2. Drama Excel generation
    3. C# code generation (flags, quest data, mappings)
    4. JSON config generation
    5. TSV generation (characters, items, zones, etc.)
    """

    def __init__(self, config: BuildConfig):
        self.config = config
        self.results: List[BuildResult] = []
        self._modules: Dict[str, Any] = {}

    def _load_module(self, module_name: str) -> Any:
        """Dynamically load a module."""
        if module_name in self._modules:
            return self._modules[module_name]

        try:
            # Add tools to path if needed
            tools_path = str(self.config.project_root / "tools")
            if tools_path not in sys.path:
                sys.path.insert(0, tools_path)

            module = importlib.import_module(module_name)
            self._modules[module_name] = module
            return module
        except ImportError as e:
            if self.config.verbose:
                print(f"Warning: Could not load module {module_name}: {e}")
            return None

    def _run_step(self, step: BuildStep) -> BuildResult:
        """Run a single build step."""
        import time

        start_time = time.time()
        try:
            success = step.func()
            duration = time.time() - start_time
            result = BuildResult(
                name=step.name,
                success=success,
                message="OK" if success else "Failed",
                duration=duration,
            )
        except Exception as e:
            duration = time.time() - start_time
            result = BuildResult(
                name=step.name, success=False, message=str(e), duration=duration
            )

        self.results.append(result)
        if self.config.verbose:
            print(result)
        return result

    def build_all(self) -> bool:
        """
        Run all build steps.

        Returns:
            True if all required steps succeeded
        """
        steps = [
            BuildStep(
                "Validation", "Validate flag and scenario definitions", self._validate
            ),
            BuildStep("Drama Excel", "Generate drama Excel files", self._build_drama),
            BuildStep("C# Flags", "Generate ArenaFlags.cs", self._build_flags),
            BuildStep(
                "C# Quest Data", "Generate ArenaQuestData.cs", self._build_quest_data
            ),
            BuildStep(
                "Quest JSON", "Generate quest_definitions.json", self._build_quest_json
            ),
            BuildStep(
                "Contract Validation",
                "Validate quest JSON against contract",
                self._validate_contract,
            ),
            BuildStep(
                "C# Type Check",
                "Validate C# classes match contract",
                self._validate_csharp_types,
            ),
            BuildStep(
                "C# Contract Gen",
                "Generate QuestDataContract.cs",
                self._build_csharp_contract,
            ),
        ]

        print("=" * 60)
        print("CWL Unified Build System")
        print("=" * 60)

        all_success = True
        for i, step in enumerate(steps, 1):
            if self.config.verbose:
                print(f"\n[{i}/{len(steps)}] {step.description}...")
            else:
                print(f"[{i}/{len(steps)}] {step.name}... ", end="", flush=True)

            result = self._run_step(step)

            if not self.config.verbose:
                print("OK" if result.success else f"FAILED: {result.message}")

            if step.required and not result.success:
                all_success = False

        print("=" * 60)
        if all_success:
            print("BUILD SUCCESSFUL")
        else:
            print("BUILD FAILED")
        print("=" * 60)

        return all_success

    def _validate(self) -> bool:
        """Run validation."""
        validation_script = (
            self.config.project_root / "tools" / "common" / "validation.py"
        )
        if not validation_script.exists():
            return True  # Skip if no validation script

        result = subprocess.run(
            [sys.executable, str(validation_script)],
            cwd=str(self.config.project_root / "tools" / "common"),
            capture_output=True,
            text=True,
        )
        return result.returncode == 0

    def _build_drama(self) -> bool:
        """Generate drama Excel files."""
        drama_script = (
            self.config.project_root / "tools" / "builder" / "create_drama_excel.py"
        )
        if not drama_script.exists():
            return True  # Skip if no drama script

        result = subprocess.run(
            [sys.executable, str(drama_script)],
            cwd=str(self.config.project_root / "tools"),
            capture_output=True,
            text=True,
        )
        return result.returncode == 0

    def _build_flags(self) -> bool:
        """Generate C# flags file."""
        flags_script = (
            self.config.project_root / "tools" / "builder" / "generate_flags.py"
        )
        if not flags_script.exists():
            return True  # Skip if no flags script

        result = subprocess.run(
            [sys.executable, str(flags_script)],
            cwd=str(self.config.project_root / "tools"),
            capture_output=True,
            text=True,
        )
        return result.returncode == 0

    def _build_quest_data(self) -> bool:
        """Generate C# quest data file."""
        quest_script = (
            self.config.project_root / "tools" / "builder" / "generate_quest_data.py"
        )
        if not quest_script.exists():
            return True  # Skip if no quest script

        result = subprocess.run(
            [sys.executable, str(quest_script)],
            cwd=str(self.config.project_root / "tools"),
            capture_output=True,
            text=True,
        )
        return result.returncode == 0

    def _build_quest_json(self) -> bool:
        """Generate quest definitions JSON."""
        quest_module = self._load_module(self.config.quest_module)
        if quest_module is None:
            return True  # Skip if no quest module

        try:
            output_path = self.config.get_output_path("json") / "quest_definitions.json"
            output_path.parent.mkdir(parents=True, exist_ok=True)
            quest_module.export_quests_to_json(str(output_path))
            return True
        except Exception as e:
            if self.config.verbose:
                print(f"Error generating quest JSON: {e}")
            return False

    def _validate_contract(self) -> bool:
        """Validate quest JSON against contract."""
        quest_json_path = self.config.get_output_path("json") / "quest_definitions.json"
        if not quest_json_path.exists():
            if self.config.verbose:
                print("Quest JSON not found, skipping contract validation")
            return True  # Skip if no quest JSON

        try:
            from cwl_quest_lib.contracts.validator import (
                validate_quest_json,
                ContractValidationError,
            )

            quests = validate_quest_json(str(quest_json_path))
            if self.config.verbose:
                print(f"Contract validation passed: {len(quests)} quests")
            return True
        except ContractValidationError as e:
            print(str(e))
            return False
        except ImportError as e:
            if self.config.verbose:
                print(f"Contract validation module not available: {e}")
            return True  # Skip if module not available
        except Exception as e:
            if self.config.verbose:
                print(f"Error validating contract: {e}")
            return False

    def _validate_csharp_types(self) -> bool:
        """Validate that C# classes match contract types."""
        try:
            from cwl_quest_lib.contracts.csharp_validator import (
                validate_quest_data_class,
                CSharpContractMismatchError,
            )

            mismatches = validate_quest_data_class(
                project_root=self.config.project_root,
                csharp_file="src/ArenaQuestManager.cs",
                class_name="QuestData",
            )

            if mismatches:
                error = CSharpContractMismatchError(
                    mismatches=mismatches,
                    csharp_file="src/ArenaQuestManager.cs",
                    class_name="QuestData",
                )
                print(str(error))
                return False

            if self.config.verbose:
                print("C# QuestData class matches contract")
            return True

        except ImportError as e:
            if self.config.verbose:
                print(f"C# type validation module not available: {e}")
            return True  # Skip if module not available
        except Exception as e:
            if self.config.verbose:
                print(f"Error validating C# types: {e}")
            return False

    def _build_csharp_contract(self) -> bool:
        """Generate C# contract class."""
        try:
            from cwl_quest_lib.codegen.csharp_generator import write_quest_data_contract

            output_path = (
                self.config.get_output_path("c_sharp") / "QuestDataContract.cs"
            )
            output_path.parent.mkdir(parents=True, exist_ok=True)
            write_quest_data_contract(
                str(output_path), namespace="Elin_SukutsuArena.Generated"
            )
            if self.config.verbose:
                print(f"Generated: {output_path}")
            return True
        except ImportError as e:
            if self.config.verbose:
                print(f"C# code generation module not available: {e}")
            return True  # Skip if module not available
        except Exception as e:
            if self.config.verbose:
                print(f"Error generating C# contract: {e}")
            return False


def build_mod(
    project_root: str = ".", debug_mode: bool = False, verbose: bool = False, **kwargs
) -> bool:
    """
    Build a CWL mod with default configuration.

    This is the main entry point for building mods.

    Args:
        project_root: Path to project root
        debug_mode: Enable debug mode
        verbose: Enable verbose output
        **kwargs: Additional config options

    Returns:
        True if build succeeded

    Usage:
        # Simple build
        build_mod()

        # Debug build with verbose output
        build_mod(debug_mode=True, verbose=True)

        # Custom project root
        build_mod(project_root="/path/to/mod")
    """
    config = BuildConfig(
        project_root=Path(project_root),
        debug_mode=debug_mode,
        verbose=verbose,
    )

    # Apply any additional kwargs
    for key, value in kwargs.items():
        if hasattr(config, key):
            setattr(config, key, value)

    builder = UnifiedBuilder(config)
    return builder.build_all()


# ============================================================================
# CLI Entry Point
# ============================================================================


def main():
    """CLI entry point."""
    import argparse

    parser = argparse.ArgumentParser(description="CWL Unified Build System")
    parser.add_argument("--project-root", default=".", help="Path to project root")
    parser.add_argument("--debug", action="store_true", help="Enable debug mode")
    parser.add_argument(
        "-v", "--verbose", action="store_true", help="Enable verbose output"
    )

    args = parser.parse_args()

    success = build_mod(
        project_root=args.project_root,
        debug_mode=args.debug,
        verbose=args.verbose,
    )

    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
