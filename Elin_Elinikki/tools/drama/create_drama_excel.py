# -*- coding: utf-8 -*-
"""
create_drama_excel.py - Generate CWL drama Excel files for Elinikki.

Output:
  LangMod/EN/Dialog/Drama/drama_<drama_id>.xlsx

Each drama is registered here via (drama_id, define_fn) pairs. Individual
scenario files live under tools/drama/scenarios/ and are added as Phase 2
tasks are completed (see docs/plans/2026-04-13-elinikki-quest-implementation-
progress.md).

Until scenarios are authored this script is a no-op that still exits 0 so
build.bat can invoke it unconditionally.
"""

import os
import sys


DRAMA_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(DRAMA_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)
sys.path.insert(0, PROJECT_ROOT)

from tools.drama.data import DramaIds
from tools.drama.drama_builder import DramaBuilder  # noqa: F401
from tools.drama.scenarios.elinikki_quest_intro import define_elinikki_quest_intro
from tools.drama.scenarios.elinikki_trace_marks import define_elinikki_trace_marks
from tools.drama.scenarios.elinikki_trace_channel import define_elinikki_trace_channel
from tools.drama.scenarios.elinikki_trace_stones import define_elinikki_trace_stones
from tools.drama.scenarios.elinikki_trace_journal import define_elinikki_trace_journal
from tools.drama.scenarios.elinikki_trace_map import define_elinikki_trace_map
from tools.drama.scenarios.elinikki_trace_shadow import define_elinikki_trace_shadow
from tools.drama.scenarios.elinikki_trace_flowers import define_elinikki_trace_flowers
from tools.drama.scenarios.elinikki_trace_weave import define_elinikki_trace_weave


OUTPUT_DIR = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Dialog", "Drama")

# (drama_id, define_fn) pairs. Populated incrementally as Phase 2 tasks
# author each scenario.
DRAMAS = [
    (DramaIds.QUEST_INTRO, define_elinikki_quest_intro),
    # Chapter 1 traces (水石の層)
    (DramaIds.TRACE_MARKS, define_elinikki_trace_marks),
    (DramaIds.TRACE_CHANNEL, define_elinikki_trace_channel),
    (DramaIds.TRACE_STONES, define_elinikki_trace_stones),
    (DramaIds.TRACE_JOURNAL, define_elinikki_trace_journal),
    # Chapter 2 traces (反響の層). Echo experiment stages are authored
    # separately in Task 2.4.
    (DramaIds.TRACE_MAP, define_elinikki_trace_map),
    (DramaIds.TRACE_SHADOW, define_elinikki_trace_shadow),
    # Chapter 3 traces (花の層)
    (DramaIds.TRACE_FLOWERS, define_elinikki_trace_flowers),
    (DramaIds.TRACE_WEAVE, define_elinikki_trace_weave),
]


def main() -> None:
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    if not DRAMAS:
        print("Drama generation: no scenarios registered yet (Phase 2 pending).")
        return

    success = 0
    errors = 0

    for drama_id, define_fn in DRAMAS:
        try:
            builder = DramaBuilder(mod_name="Elinikki")
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
