# -*- coding: utf-8 -*-
"""
create_element_excel.py - SourceElement TSV auto-generation

Reads spell definitions from tools/data/elements.py and generates
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
from data.elements import CUSTOM_ELEMENTS

# Output paths
OUTPUT_JP_TSV = os.path.join(PROJECT_ROOT, "LangMod", "JP", "Element.tsv")
OUTPUT_EN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Element.tsv")
OUTPUT_CN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "CN", "Element.tsv")

# SourceElement column headers (57 columns, matching vanilla)
HEADERS = [
    "id", "alias", "name_JP", "name", "altname_JP", "altname",
    "aliasParent", "aliasRef", "aliasMtp", "parentFactor", "lvFactor",
    "encFactor", "encSlot", "mtp", "LV", "chance", "value", "cost",
    "geneSlot", "sort", "target", "proc", "type", "group", "category",
    "categorySub", "abilityType", "tag", "thing", "eleP", "cooldown",
    "charge", "radius", "max", "req", "idTrainer", "partySkill",
    "tagTrainer", "levelBonus_JP", "levelBonus", "foodEffect", "note",
    "langAct", "detail_JP", "detail", "textPhase_JP", "textPhase",
    "textExtra_JP", "textExtra", "textInc_JP", "textInc", "textDec_JP",
    "textDec", "textAlt_JP", "textAlt", "adjective_JP", "adjective",
]

# Type info (row 2)
TYPES = [
    "int", "string", "string", "string", "string", "string", "string",
    "string", "string", "float", "int", "int", "string", "int", "int",
    "int", "int", "int[]", "int", "int", "string", "string[]", "string",
    "string", "string", "string", "string[]", "string[]", "string", "int",
    "int", "int", "float", "int", "string[]", "string", "int", "string",
    "string", "string", "string[]", "", "string[]", "string", "string",
    "string", "string", "string", "string", "string", "string", "string",
    "string", "string[]", "string[]", "string[]", "string[]",
]

# Default values (row 3)
DEFAULTS = {
    "encFactor": "100",
    "mtp": "1",
    "LV": "1",
    "chance": "1000",
    "cost": "0",
    "geneSlot": "1",
    "type": "Element",
    "eleP": "50",
    "charge": "10",
    "radius": "5",
}


def create_row(element, lang="jp"):
    """Generate a row for the given language.

    Args:
        lang: "jp", "en", or "cn". CN uses CN values with EN fallback.
    """
    row = []
    for header in HEADERS:
        if lang == "cn" and header == "name":
            value = element.get("name_CN", "") or element.get("name", "")
        elif lang == "cn" and header == "detail":
            value = element.get("detail_CN", "") or element.get("detail", "")
        elif lang == "cn" and header == "textPhase":
            value = element.get("textPhase_CN", "") or element.get("textPhase", "")
        elif lang == "cn" and header == "textExtra":
            value = element.get("textExtra_CN", "") or element.get("textExtra", "")
        elif header == "lvFactor":
            # Spell exp gain in vanilla is gated by source.lvFactor > 0.
            # Ensure custom SPELL rows level up unless explicitly overridden.
            value = element.get("lvFactor", "100" if element.get("group") == "SPELL" else "")
        else:
            value = element.get(header, "")
        if (value is None or value == "") and element.get("group") == "SPELL":
            if header == "textExtra_JP":
                value = element.get("detail_JP", "")
            elif header == "textExtra":
                if lang == "cn":
                    value = element.get("detail_CN", "") or element.get("detail", "")
                else:
                    value = element.get("detail", "")

        row.append(str(value) if value is not None else "")
    return row


def write_tsv(path, row_data):
    """Write TSV file."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, delimiter="\t", quoting=csv.QUOTE_MINIMAL)
        writer.writerows(row_data)
    print(f"  Created TSV: {path}")


def main():
    print(f"Generating Element TSV from {len(CUSTOM_ELEMENTS)} element definition(s)...")

    default_row = [DEFAULTS.get(h, "") for h in HEADERS]

    # JP
    rows_jp = [HEADERS, TYPES, default_row]
    for element in CUSTOM_ELEMENTS:
        rows_jp.append(create_row(element, lang="jp"))
    write_tsv(OUTPUT_JP_TSV, rows_jp)

    # EN
    rows_en = [HEADERS, TYPES, default_row]
    for element in CUSTOM_ELEMENTS:
        rows_en.append(create_row(element, lang="en"))
    write_tsv(OUTPUT_EN_TSV, rows_en)

    # CN
    rows_cn = [HEADERS, TYPES, default_row]
    for element in CUSTOM_ELEMENTS:
        rows_cn.append(create_row(element, lang="cn"))
    write_tsv(OUTPUT_CN_TSV, rows_cn)

    print(f"Generated {len(CUSTOM_ELEMENTS)} element(s) (JP + EN + CN)")


if __name__ == "__main__":
    main()
