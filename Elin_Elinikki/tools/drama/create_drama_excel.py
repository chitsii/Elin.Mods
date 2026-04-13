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
from tools.drama.scenarios.elinikki_echo_stage_1 import define_elinikki_echo_stage_1
from tools.drama.scenarios.elinikki_echo_stage_2 import define_elinikki_echo_stage_2
from tools.drama.scenarios.elinikki_echo_stage_3 import define_elinikki_echo_stage_3
from tools.drama.scenarios.elinikki_echo_stage_4 import define_elinikki_echo_stage_4
from tools.drama.scenarios.elinikki_reunion import define_elinikki_reunion
from tools.drama.scenarios.elinikki_reunion_menu import define_elinikki_reunion_menu
from tools.drama.scenarios.elinikki_truth_marks import define_elinikki_truth_marks
from tools.drama.scenarios.elinikki_truth_channel import define_elinikki_truth_channel
from tools.drama.scenarios.elinikki_truth_stones import define_elinikki_truth_stones
from tools.drama.scenarios.elinikki_truth_echo import define_elinikki_truth_echo
from tools.drama.scenarios.elinikki_truth_map import define_elinikki_truth_map
from tools.drama.scenarios.elinikki_truth_shadow import define_elinikki_truth_shadow
from tools.drama.scenarios.elinikki_truth_flowers import define_elinikki_truth_flowers
from tools.drama.scenarios.elinikki_truth_weave import define_elinikki_truth_weave
from tools.drama.scenarios.elinikki_return_journey import define_elinikki_return_journey
from tools.drama.scenarios.elinikki_ending_return import define_elinikki_ending_return
from tools.drama.scenarios.elinikki_ending_silence import define_elinikki_ending_silence


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
    # Chapter 2 (反響の層): echo experiment stages 1-4 plus map/shadow traces.
    (DramaIds.ECHO_STAGE_1, define_elinikki_echo_stage_1),
    (DramaIds.ECHO_STAGE_2, define_elinikki_echo_stage_2),
    (DramaIds.ECHO_STAGE_3, define_elinikki_echo_stage_3),
    (DramaIds.ECHO_STAGE_4, define_elinikki_echo_stage_4),
    (DramaIds.TRACE_MAP, define_elinikki_trace_map),
    (DramaIds.TRACE_SHADOW, define_elinikki_trace_shadow),
    # Chapter 3 traces (花の層)
    (DramaIds.TRACE_FLOWERS, define_elinikki_trace_flowers),
    (DramaIds.TRACE_WEAVE, define_elinikki_trace_weave),
    # Chapter 4 (再会): reunion cutscene + 8 truth conversations. The
    # journal "伝わった" beat is folded into truth_echo rather than
    # being its own drama.
    (DramaIds.REUNION, define_elinikki_reunion),
    (DramaIds.REUNION_MENU, define_elinikki_reunion_menu),
    (DramaIds.TRUTH_MARKS, define_elinikki_truth_marks),
    (DramaIds.TRUTH_CHANNEL, define_elinikki_truth_channel),
    (DramaIds.TRUTH_STONES, define_elinikki_truth_stones),
    (DramaIds.TRUTH_ECHO, define_elinikki_truth_echo),
    (DramaIds.TRUTH_MAP, define_elinikki_truth_map),
    (DramaIds.TRUTH_SHADOW, define_elinikki_truth_shadow),
    (DramaIds.TRUTH_FLOWERS, define_elinikki_truth_flowers),
    (DramaIds.TRUTH_WEAVE, define_elinikki_truth_weave),
    # Chapter 5 (帰還と選択): return walk + two named endings. The
    # hidden "revisit" ending has no drama id in tools/drama/data.py —
    # it is an environmental beat, not a scripted one.
    (DramaIds.RETURN_JOURNEY, define_elinikki_return_journey),
    (DramaIds.ENDING_RETURN, define_elinikki_ending_return),
    (DramaIds.ENDING_SILENCE, define_elinikki_ending_silence),
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
