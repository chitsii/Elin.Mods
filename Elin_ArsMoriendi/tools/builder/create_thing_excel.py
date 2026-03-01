# -*- coding: utf-8 -*-
"""
create_thing_excel.py - SourceThing TSV auto-generation

Reads item definitions from tools/data/items.py and generates
CWL-format TSV files for JP and EN.
"""

import csv
import os
import sys

# Path setup
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

sys.path.insert(0, TOOLS_DIR)
from data.items import CUSTOM_ITEMS, TraitType

# Output paths
OUTPUT_JP_TSV = os.path.join(PROJECT_ROOT, "LangMod", "JP", "Thing.tsv")
OUTPUT_EN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Thing.tsv")
OUTPUT_CN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "CN", "Thing.tsv")

# SourceThing column headers (52 columns, matching vanilla)
HEADERS = [
    "id", "name_JP", "unknown_JP", "unit_JP", "naming", "name", "unit",
    "unknown", "category", "sort", "sort", "_tileType", "_idRenderData",
    "tiles", "altTiles", "anime", "skins", "size", "colorMod", "colorType",
    "recipeKey", "factory", "components", "disassemble", "defMat", "tierGroup",
    "value", "LV", "chance", "quality", "HP", "weight", "electricity", "trait",
    "elements", "range", "attackType", "offense", "substats", "defense",
    "lightData", "idExtra", "idToggleExtra", "idActorEx", "idSound", "tag",
    "workTag", "filter", "roomName_JP", "roomName", "detail_JP", "detail",
]

# Type info (row 2)
TYPES = [
    "string", "string", "string", "string", "string", "string", "string",
    "string", "string", "", "int", "string", "string", "int[]", "int[]",
    "int[]", "int[]", "int[]", "int", "string", "string[]", "string[]",
    "string[]", "string[]", "string", "string", "int", "int", "int", "int",
    "int", "int", "int", "string[]", "elements", "int", "string", "int[]",
    "int[]", "int[]", "string", "string", "string", "string", "string",
    "string[]", "string", "string[]", "string[]", "string[]", "string", "string",
]

# Default values (row 3)
DEFAULTS = [
    "", "", "", "個", "", "", "", "", "other", "", "100", "", "", "0", "",
    "", "", "", "100", "", "", "", "log", "", "oak", "", "100", "1", "1000",
    "", "100", "1000", "", "", "", "1", "", "", "", "", "", "", "", "", "",
    "", "", "", "", "", "", "",
]

# Column name -> index
HEADER_MAP = {name: idx for idx, name in enumerate(HEADERS)}


def set_cell(row, column_name, value):
    """Set value at column."""
    if column_name in HEADER_MAP:
        row[HEADER_MAP[column_name]] = value


def build_trait_string(item):
    """Build trait string (comma-separated)."""
    if not item.trait_name:
        return ""
    parts = [item.trait_name] + item.trait_params
    return ",".join(str(p) for p in parts)


def generate_rows(lang="jp"):
    """Generate rows for specified language.

    Args:
        lang: "jp", "en", or "cn". CN uses CN values with EN fallback.
    """
    rows = [HEADERS, TYPES, DEFAULTS]

    for item_id, item in CUSTOM_ITEMS.items():
        row = [""] * len(HEADERS)

        set_cell(row, "id", item.id)
        set_cell(row, "name_JP", item.name_jp)
        set_cell(row, "category", item.category)

        # Language-specific fields
        if lang == "cn":
            set_cell(row, "name", item.name_cn or item.name_en)
            set_cell(row, "detail", item.detail_cn or item.detail_en)
        else:
            set_cell(row, "name", item.name_en)
            set_cell(row, "detail", item.detail_en)
        set_cell(row, "detail_JP", item.detail_jp)

        # Trait
        trait_str = build_trait_string(item)
        if trait_str:
            set_cell(row, "trait", trait_str)

        # Elements
        if item.elements:
            set_cell(row, "elements", item.elements)

        # Game data
        set_cell(row, "value", item.value)
        set_cell(row, "LV", item.lv)
        set_cell(row, "weight", item.weight)
        set_cell(row, "chance", item.chance)

        # Rendering
        set_cell(row, "tiles", item.tiles)
        set_cell(row, "_idRenderData", item.render_data)

        # Equipment
        if item.def_mat:
            set_cell(row, "defMat", item.def_mat)
        if item.tier_group:
            set_cell(row, "tierGroup", item.tier_group)
        if item.defense:
            set_cell(row, "defense", item.defense)

        # Tags
        if item.tags:
            set_cell(row, "tag", ",".join(item.tags))

        # Sort (first sort column at index 9)
        if item.sort:
            row[9] = item.sort

        # Quality
        if item.quality:
            set_cell(row, "quality", item.quality)

        # Filter
        if item.filter:
            set_cell(row, "filter", item.filter)

        # Components
        if item.components:
            set_cell(row, "components", item.components)

        # Color
        if item.color_mod:
            set_cell(row, "colorMod", item.color_mod)
        if item.color_type:
            set_cell(row, "colorType", item.color_type)

        rows.append(row)

    return rows


def write_tsv(path, row_data):
    """Write TSV file."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, delimiter="\t", quoting=csv.QUOTE_MINIMAL)
        writer.writerows(row_data)
    print(f"  Created TSV: {path}")


def main():
    print(f"Generating Thing TSV from {len(CUSTOM_ITEMS)} item definition(s)...")

    # JP
    rows_jp = generate_rows(lang="jp")
    write_tsv(OUTPUT_JP_TSV, rows_jp)

    # EN
    rows_en = generate_rows(lang="en")
    write_tsv(OUTPUT_EN_TSV, rows_en)

    # CN
    rows_cn = generate_rows(lang="cn")
    write_tsv(OUTPUT_CN_TSV, rows_cn)

    print(f"Generated {len(CUSTOM_ITEMS)} item(s) (JP + EN + CN)")


if __name__ == "__main__":
    main()
