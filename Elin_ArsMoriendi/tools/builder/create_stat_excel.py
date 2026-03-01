# -*- coding: utf-8 -*-
"""
create_stat_excel.py - SourceStat TSV auto-generation

Reads condition definitions from tools/data/stats.py and generates
CWL-format TSV files for JP and EN.
"""

import os
import sys

# Path setup
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

sys.path.insert(0, TOOLS_DIR)
from data.stats import CONDITIONS

# Output paths
OUTPUT_JP_TSV = os.path.join(PROJECT_ROOT, "LangMod", "JP", "Stat.tsv")
OUTPUT_EN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Stat.tsv")
OUTPUT_CN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "CN", "Stat.tsv")

# SourceStat column headers (33 columns)
HEADERS = [
    "id", "alias", "name_JP", "name", "type", "group", "curse", "duration",
    "hexPower", "negate", "defenseAttb", "resistance", "gainRes", "elements",
    "nullify", "tag", "phase", "colors", "element", "effect", "strPhase_JP",
    "strPhase", "textPhase_JP", "textPhase", "textEnd_JP", "textEnd",
    "textPhase2_JP", "textPhase2", "gradient", "invert", "detail_JP", "detail",
]

# Type info (row 2)
TYPES = [
    "int", "string", "string", "string", "string", "string", "string",
    "string", "int", "string[]", "string[]", "string[]", "int", "string[]",
    "string[]", "string[]", "int[]", "string", "string", "string[]",
    "string[]", "string[]", "string", "string", "string", "string",
    "string", "string", "string", "bool", "string", "string",
]

# Default values (row 3)
DEFAULTS = {
    "id": "103",
    "group": "Neutral",
    "duration": "p/10",
    "hexPower": "10",
    "phase": "0,1,2,3,4,5,6,7,8,9",
    "gradient": "condition",
}


_CN_SWAP_FIELDS = {"name", "detail", "textEnd", "textPhase2"}


def create_tsv(output_path, lang="jp"):
    """Generate Stat.tsv.

    Args:
        lang: "jp", "en", or "cn". CN uses CN values with EN fallback.
    """
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    lines = []

    # Row 1: Headers
    lines.append("\t".join(HEADERS))

    # Row 2: Types
    lines.append("\t".join(TYPES))

    # Row 3: Defaults
    default_row = [DEFAULTS.get(h, "") for h in HEADERS]
    lines.append("\t".join(default_row))

    # Row 4+: Data
    for condition in CONDITIONS:
        row = []
        for h in HEADERS:
            if lang == "cn" and h in _CN_SWAP_FIELDS:
                cn_key = f"{h}_CN"
                value = str(condition.get(cn_key, "") or condition.get(h, ""))
            else:
                value = str(condition.get(h, ""))
            row.append(value)
        lines.append("\t".join(row))

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"  Created TSV: {output_path}")


def main():
    print(f"Generating Stat TSV from {len(CONDITIONS)} condition definition(s)...")
    create_tsv(OUTPUT_JP_TSV, lang="jp")
    create_tsv(OUTPUT_EN_TSV, lang="en")
    create_tsv(OUTPUT_CN_TSV, lang="cn")
    print(f"Generated {len(CONDITIONS)} condition(s) (JP + EN + CN)")


if __name__ == "__main__":
    main()
