# -*- coding: utf-8 -*-
"""
create_thing_excel.py - Thing data generator

Builds CWL Thing.tsv from Python definitions.
"""

import csv
from pathlib import Path

from thing_definitions import THING_DEFINITIONS


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent

OUTPUT_EN_THING_TSV = PROJECT_ROOT / "LangMod" / "EN" / "Thing.tsv"
OUTPUT_JP_THING_TSV = PROJECT_ROOT / "LangMod" / "JP" / "Thing.tsv"
OUTPUT_CN_THING_TSV = PROJECT_ROOT / "LangMod" / "CN" / "Thing.tsv"

# SourceThing schema (52 columns).
HEADERS = [
    "id",
    "name_JP",
    "unknown_JP",
    "unit_JP",
    "naming",
    "name",
    "unit",
    "unknown",
    "category",
    "sort",
    "sort",
    "_tileType",
    "_idRenderData",
    "tiles",
    "altTiles",
    "anime",
    "skins",
    "size",
    "colorMod",
    "colorType",
    "recipeKey",
    "factory",
    "components",
    "disassemble",
    "defMat",
    "tierGroup",
    "value",
    "LV",
    "chance",
    "quality",
    "HP",
    "weight",
    "electricity",
    "trait",
    "elements",
    "range",
    "attackType",
    "offense",
    "substats",
    "defense",
    "lightData",
    "idExtra",
    "idToggleExtra",
    "idActorEx",
    "idSound",
    "tag",
    "workTag",
    "filter",
    "roomName_JP",
    "roomName",
    "detail_JP",
    "detail",
]

TYPES = [
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "",
    "int",
    "string",
    "string",
    "int[]",
    "int[]",
    "int[]",
    "int[]",
    "int[]",
    "int",
    "string",
    "string[]",
    "string[]",
    "string[]",
    "string[]",
    "string",
    "string",
    "int",
    "int",
    "int",
    "int",
    "int",
    "int",
    "int",
    "string[]",
    "elements",
    "int",
    "string",
    "int[]",
    "int[]",
    "int[]",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string[]",
    "string",
    "string[]",
    "string[]",
    "string[]",
    "string",
    "string",
]

DEFAULTS = [
    "",
    "",
    "",
    "個",
    "",
    "",
    "",
    "",
    "other",
    "",
    "100",
    "",
    "",
    "0",
    "",
    "",
    "",
    "",
    "100",
    "",
    "",
    "",
    "log",
    "",
    "oak",
    "",
    "100",
    "1",
    "1000",
    "",
    "100",
    "1000",
    "",
    "",
    "",
    "1",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
]

HEADER_MAP = {name: i for i, name in enumerate(HEADERS)}


def set_cell(row, column_name, value):
    idx = HEADER_MAP.get(column_name)
    if idx is not None:
        row[idx] = value


def build_rows(lang: str):
    rows = [HEADERS, TYPES, DEFAULTS]
    for thing in THING_DEFINITIONS:
        row = [""] * len(HEADERS)
        set_cell(row, "id", thing.id)
        set_cell(row, "name_JP", thing.name_jp)
        if lang == "CN":
            set_cell(row, "name", thing.name_cn or thing.name_en)
            set_cell(row, "detail", thing.detail_cn or thing.detail_en)
        else:
            set_cell(row, "name", thing.name_en)
            set_cell(row, "detail", thing.detail_en)
        set_cell(row, "category", thing.category)
        # first "sort" column must be populated for furniture category sort.
        row[9] = thing.sort
        set_cell(row, "_tileType", thing.tile_type)
        set_cell(row, "_idRenderData", thing.render_data)
        set_cell(row, "tiles", thing.tiles)
        set_cell(row, "value", thing.value)
        set_cell(row, "weight", thing.weight)
        set_cell(row, "electricity", thing.electricity)
        set_cell(row, "trait", thing.trait)
        set_cell(row, "detail_JP", thing.detail_jp)
        rows.append(row)
    return rows


def write_tsv(path: Path, rows):
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, delimiter="\t", quoting=csv.QUOTE_MINIMAL)
        writer.writerows(rows)


def main():
    rows_en = build_rows("EN")
    rows_jp = build_rows("JP")
    rows_cn = build_rows("CN")
    write_tsv(OUTPUT_EN_THING_TSV, rows_en)
    write_tsv(OUTPUT_JP_THING_TSV, rows_jp)
    write_tsv(OUTPUT_CN_THING_TSV, rows_cn)
    print(f"Generated: {OUTPUT_EN_THING_TSV}")
    print(f"Generated: {OUTPUT_JP_THING_TSV}")
    print(f"Generated: {OUTPUT_CN_THING_TSV}")
    print(f"Rows: {len(rows_en) - 3}")


if __name__ == "__main__":
    main()
