# -*- coding: utf-8 -*-
"""
create_drama_excel.py - Generate CWL drama Excel files for QuestMod template.

Output:
  LangMod/EN/Dialog/Drama/drama_<drama_id>.xlsx
"""

import os
import sys


DRAMA_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(DRAMA_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)
sys.path.insert(0, PROJECT_ROOT)

from tools.drama.data import DramaIds
from tools.drama.drama_builder import DramaBuilder
from tools.drama.scenarios.quest_drama_feature_showcase import (
    define_quest_drama_feature_showcase,
)
from tools.drama.scenarios.quest_drama_feature_showcase_followup import (
    define_quest_drama_feature_showcase_followup,
)
from tools.drama.scenarios.quest_drama_feature_branch_a import (
    define_quest_drama_feature_branch_a,
)
from tools.drama.scenarios.quest_drama_feature_branch_b import (
    define_quest_drama_feature_branch_b,
)
from tools.drama.scenarios.quest_drama_feature_merge import (
    define_quest_drama_feature_merge,
)
from tools.drama.scenarios.quest_drama_placeholder import define_quest_drama_placeholder


OUTPUT_DIR = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Dialog", "Drama")

DRAMAS = [
    (DramaIds.PLACEHOLDER, define_quest_drama_placeholder),
    (DramaIds.FEATURE_SHOWCASE, define_quest_drama_feature_showcase),
    (DramaIds.FEATURE_SHOWCASE_FOLLOWUP, define_quest_drama_feature_showcase_followup),
    (DramaIds.FEATURE_BRANCH_A, define_quest_drama_feature_branch_a),
    (DramaIds.FEATURE_BRANCH_B, define_quest_drama_feature_branch_b),
    (DramaIds.FEATURE_MERGE, define_quest_drama_feature_merge),
]


def main() -> None:
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    success = 0
    errors = 0

    for drama_id, define_fn in DRAMAS:
        try:
            builder = DramaBuilder(mod_name="QuestModTemplate")
            define_fn(builder)

            filename = f"drama_{drama_id}.xlsx"
            filepath = os.path.join(OUTPUT_DIR, filename)
            builder.save(filepath, sheet_name=drama_id)
            success += 1
        except Exception as ex:
            print(f"[ERROR] Failed to generate {drama_id}: {ex}")
            errors += 1

    print(f"Drama generation complete: {success} succeeded, {errors} failed")

    if errors > 0:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
