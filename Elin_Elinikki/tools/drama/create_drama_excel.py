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

from tools.drama.data import DramaIds  # noqa: F401  (imported for side-effects / key validation)
from tools.drama.drama_builder import DramaBuilder  # noqa: F401


OUTPUT_DIR = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Dialog", "Drama")

# (drama_id, define_fn) pairs. Populated incrementally as Phase 2 tasks
# author each scenario. Empty list == Phase 2 not yet started.
DRAMAS: list = []


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
