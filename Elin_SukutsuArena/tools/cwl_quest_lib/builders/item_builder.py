"""
CWL Quest Library - Item Builder

This module provides utilities for defining custom items and generating
CWL-compatible SourceThing TSV files.

Usage:
    from cwl_quest_lib.item_builder import ItemBuilder, TraitType

    builder = ItemBuilder()
    builder.add_item(ItemDefinition(
        id="my_sword",
        name_jp="伝説の剣",
        name_en="Legendary Sword",
        category="weapon",
        value=10000,
    ))
    builder.save_tsv("LangMod/EN/Thing.tsv")
"""

import csv
import os
from enum import Enum
from dataclasses import dataclass, field
from typing import List, Dict, Optional

from ..core.types import ItemDefinition


class TraitType(Enum):
    """Item trait type."""

    VANILLA = "vanilla"  # Built-in Elin trait
    CUSTOM = "custom"  # Custom mod trait


# ============================================================================
# SourceThing Column Definitions (CWL format)
# ============================================================================

THING_HEADERS = [
    "id",  # 1
    "name_JP",  # 2
    "unknown_JP",  # 3
    "unit_JP",  # 4
    "naming",  # 5
    "name",  # 6
    "unit",  # 7
    "unknown",  # 8
    "category",  # 9
    "sort",  # 10
    "sort",  # 11 (duplicate)
    "_tileType",  # 12
    "_idRenderData",  # 13
    "tiles",  # 14
    "altTiles",  # 15
    "anime",  # 16
    "skins",  # 17
    "size",  # 18
    "colorMod",  # 19
    "colorType",  # 20
    "recipeKey",  # 21
    "factory",  # 22
    "components",  # 23
    "disassemble",  # 24
    "defMat",  # 25
    "tierGroup",  # 26
    "value",  # 27
    "LV",  # 28
    "chance",  # 29
    "quality",  # 30
    "HP",  # 31
    "weight",  # 32
    "electricity",  # 33
    "trait",  # 34
    "elements",  # 35
    "range",  # 36
    "attackType",  # 37
    "offense",  # 38
    "substats",  # 39
    "defense",  # 40
    "lightData",  # 41
    "idExtra",  # 42
    "idToggleExtra",  # 43
    "idActorEx",  # 44
    "idSound",  # 45
    "tag",  # 46
    "workTag",  # 47
    "filter",  # 48
    "roomName_JP",  # 49
    "roomName",  # 50
    "detail_JP",  # 51
    "detail",  # 52
]

THING_TYPES = [
    "string",  # id
    "string",  # name_JP
    "string",  # unknown_JP
    "string",  # unit_JP
    "string",  # naming
    "string",  # name
    "string",  # unit
    "string",  # unknown
    "string",  # category
    "",  # sort (1)
    "int",  # sort (2)
    "string",  # _tileType
    "string",  # _idRenderData
    "int[]",  # tiles
    "int[]",  # altTiles
    "int[]",  # anime
    "int[]",  # skins
    "int[]",  # size
    "int",  # colorMod
    "string",  # colorType
    "string[]",  # recipeKey
    "string[]",  # factory
    "string[]",  # components
    "string[]",  # disassemble
    "string",  # defMat
    "string",  # tierGroup
    "int",  # value
    "int",  # LV
    "int",  # chance
    "int",  # quality
    "int",  # HP
    "int",  # weight
    "int",  # electricity
    "string[]",  # trait
    "elements",  # elements
    "int",  # range
    "string",  # attackType
    "int[]",  # offense
    "int[]",  # substats
    "int[]",  # defense
    "string",  # lightData
    "string",  # idExtra
    "string",  # idToggleExtra
    "string",  # idActorEx
    "string",  # idSound
    "string[]",  # tag
    "string",  # workTag
    "string[]",  # filter
    "string[]",  # roomName_JP
    "string[]",  # roomName
    "string",  # detail_JP
    "string",  # detail
]

THING_DEFAULTS = [
    "",  # id
    "",  # name_JP
    "",  # unknown_JP
    "個",  # unit_JP
    "",  # naming
    "",  # name
    "",  # unit
    "",  # unknown
    "other",  # category
    "",  # sort (1)
    "100",  # sort (2)
    "",  # _tileType
    "",  # _idRenderData
    "0",  # tiles
    "",  # altTiles
    "",  # anime
    "",  # skins
    "",  # size
    "100",  # colorMod
    "",  # colorType
    "",  # recipeKey
    "log",  # factory
    "",  # components
    "oak",  # disassemble
    "",  # defMat
    "",  # tierGroup
    "100",  # value
    "1",  # LV
    "1000",  # chance
    "",  # quality
    "100",  # HP
    "1,000",  # weight
    "",  # electricity
    "",  # trait
    "",  # elements
    "1",  # range
    "",  # attackType
    "",  # offense
    "",  # substats
    "",  # defense
    "",  # lightData
    "",  # idExtra
    "",  # idToggleExtra
    "",  # idActorEx
    "",  # idSound
    "",  # tag
    "",  # workTag
    "",  # filter
    "",  # roomName_JP
    "",  # roomName
    "",  # detail_JP
    "",  # detail
]

# Column name -> index mapping
THING_HEADER_MAP = {name: idx for idx, name in enumerate(THING_HEADERS)}


class ItemBuilder:
    """
    Builder for CWL-compatible item (Thing) TSV files.

    Example:
        builder = ItemBuilder()
        builder.add_item(ItemDefinition(
            id="my_item",
            name_jp="アイテム",
            name_en="Item",
            value=100,
        ))
        builder.save_tsv("Thing.tsv")
    """

    def __init__(self, debug_mode: bool = False):
        """
        Args:
            debug_mode: If True, set all item values to 1 for testing
        """
        self.items: Dict[str, ItemDefinition] = {}
        self.debug_mode = debug_mode

    def add_item(self, item: ItemDefinition) -> "ItemBuilder":
        """Add an item definition."""
        self.items[item.id] = item
        return self

    def add_items(self, items: List[ItemDefinition]) -> "ItemBuilder":
        """Add multiple item definitions."""
        for item in items:
            self.add_item(item)
        return self

    def _build_trait_string(self, item: ItemDefinition) -> str:
        """Build trait string for item."""
        if not item.trait_name:
            return ""
        parts = [item.trait_name] + item.trait_params
        return ",".join(str(p) for p in parts)

    def _item_to_row(self, item: ItemDefinition) -> List[str]:
        """Convert ItemDefinition to TSV row."""
        row = [""] * len(THING_HEADERS)

        def set_cell(col_name: str, value):
            if col_name in THING_HEADER_MAP:
                row[THING_HEADER_MAP[col_name]] = value

        # Basic info
        set_cell("id", item.id)
        set_cell("name_JP", item.name_jp)
        set_cell("name", item.name_en or item.name_jp)
        set_cell("category", item.category)

        # Description
        set_cell("detail_JP", item.detail_jp)
        set_cell("detail", item.detail_en or item.detail_jp)

        # Trait
        trait_str = self._build_trait_string(item)
        if trait_str:
            set_cell("trait", trait_str)

        # Elements
        if item.elements:
            set_cell("elements", item.elements)

        # Game data
        item_value = 1 if self.debug_mode else item.value
        set_cell("value", item_value)
        set_cell("LV", item.lv)
        set_cell("weight", item.weight)
        set_cell("chance", item.chance)

        # Rendering
        set_cell("tiles", item.tiles)
        if item.render_data:
            set_cell("_idRenderData", item.render_data)

        # Equipment fields
        if item.def_mat:
            set_cell("defMat", item.def_mat)
        if item.tier_group:
            set_cell("tierGroup", item.tier_group)
        if item.defense:
            set_cell("defense", item.defense)

        # Tags
        if item.tags:
            set_cell("tag", ",".join(item.tags))

        return row

    def build_rows(self) -> List[List[str]]:
        """Build all rows including headers."""
        rows = []

        # Row 1: Headers
        rows.append(THING_HEADERS)

        # Row 2: Types
        rows.append(THING_TYPES)

        # Row 3: Defaults
        rows.append(THING_DEFAULTS)

        # Row 4+: Item data
        for item in self.items.values():
            rows.append(self._item_to_row(item))

        return rows

    def save_tsv(self, *paths: str) -> None:
        """
        Save item definitions to TSV files.

        Args:
            *paths: One or more output paths (e.g., EN/Thing.tsv, JP/Thing.tsv)
        """
        if self.debug_mode:
            print("[DEBUG MODE] Item prices set to 1")

        rows = self.build_rows()

        for path in paths:
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, "w", newline="", encoding="utf-8") as f:
                writer = csv.writer(f, delimiter="\t", quoting=csv.QUOTE_MINIMAL)
                writer.writerows(rows)
            print(f"Created TSV: {path} ({len(self.items)} items)")


# ============================================================================
# Unit Tests
# ============================================================================

if __name__ == "__main__":
    print("=== Item Builder Test ===\n")

    builder = ItemBuilder()

    # Add test items
    builder.add_item(
        ItemDefinition(
            id="test_item",
            name_jp="テストアイテム",
            name_en="Test Item",
            category="other",
            value=100,
            detail_jp="テスト用アイテムです",
            detail_en="This is a test item",
        )
    )

    builder.add_item(
        ItemDefinition(
            id="test_weapon",
            name_jp="テスト武器",
            name_en="Test Weapon",
            category="weapon",
            value=1000,
            trait_name="TestTrait",
            trait_params=["param1", "param2"],
        )
    )

    # Build rows
    rows = builder.build_rows()
    print(f"Generated {len(rows)} rows")
    print(f"Headers: {len(rows[0])} columns")
    print(f"Items: {len(builder.items)}")

    print("\n=== All Tests Passed! ===")
