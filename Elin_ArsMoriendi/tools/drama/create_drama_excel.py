# -*- coding: utf-8 -*-
"""
create_drama_excel.py - Generate CWL drama Excel files

Reads scenario definitions from tools/drama/scenarios/ and generates
xlsx files in LangMod/EN/Dialog/Drama/.
"""

import os
import sys

# Path setup
DRAMA_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(DRAMA_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

sys.path.insert(0, TOOLS_DIR)

from drama.drama_builder import DramaBuilder
from drama.data import DramaIds
from drama.scenarios.ars_first_soul import define_first_soul
from drama.scenarios.ars_tome_awakening import define_tome_awakening
from drama.scenarios.ars_karen_encounter import define_karen_encounter
from drama.scenarios.ars_karen_retreat import define_karen_retreat
from drama.scenarios.ars_cinder_records import define_cinder_records
from drama.scenarios.ars_scout_encounter import define_scout_encounter
from drama.scenarios.ars_stigmata import define_stigmata
from drama.scenarios.ars_erenos_appear import define_erenos_appear
from drama.scenarios.ars_erenos_defeat import define_erenos_defeat
from drama.scenarios.ars_apotheosis import define_apotheosis
from drama.scenarios.ars_first_servant import define_first_servant
from drama.scenarios.ars_servant_rampage import define_servant_rampage
from drama.scenarios.ars_servant_lost import define_servant_lost
from drama.scenarios.ars_dormant_flavor import define_dormant_flavor
from drama.scenarios.ars_karen_shadow import define_karen_shadow
from drama.scenarios.ars_seventh_sign import define_seventh_sign
from drama.scenarios.ars_karen_ambush import define_karen_ambush
from drama.scenarios.ars_erenos_ambush import define_erenos_ambush
from drama.scenarios.ars_scout_ambush import define_scout_ambush
from drama.scenarios.ars_hecatia_talk import define_hecatia_talk

# Output directory
OUTPUT_DIR = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Dialog", "Drama")

# Drama definitions: (drama_id, define_function)
DRAMAS = [
    (DramaIds.FIRST_SOUL, define_first_soul),
    (DramaIds.TOME_AWAKENING, define_tome_awakening),
    (DramaIds.KAREN_ENCOUNTER, define_karen_encounter),
    (DramaIds.KAREN_RETREAT, define_karen_retreat),
    (DramaIds.CINDER_RECORDS, define_cinder_records),
    (DramaIds.SCOUT_ENCOUNTER, define_scout_encounter),
    (DramaIds.STIGMATA, define_stigmata),
    (DramaIds.ERENOS_APPEAR, define_erenos_appear),
    (DramaIds.ERENOS_DEFEAT, define_erenos_defeat),
    (DramaIds.APOTHEOSIS, define_apotheosis),
    (DramaIds.FIRST_SERVANT, define_first_servant),
    (DramaIds.SERVANT_RAMPAGE, define_servant_rampage),
    (DramaIds.SERVANT_LOST, define_servant_lost),
    (DramaIds.DORMANT_FLAVOR, define_dormant_flavor),
    (DramaIds.KAREN_SHADOW, define_karen_shadow),
    (DramaIds.SEVENTH_SIGN, define_seventh_sign),
    (DramaIds.KAREN_AMBUSH, define_karen_ambush),
    (DramaIds.ERENOS_AMBUSH, define_erenos_ambush),
    (DramaIds.SCOUT_AMBUSH, define_scout_ambush),
    (DramaIds.HECATIA_TALK, define_hecatia_talk),
]


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    success = 0
    errors = 0

    for drama_id, define_fn in DRAMAS:
        try:
            builder = DramaBuilder(mod_name="ArsMoriendi")
            define_fn(builder)

            filename = f"drama_{drama_id}.xlsx"
            filepath = os.path.join(OUTPUT_DIR, filename)
            builder.save(filepath, sheet_name=drama_id)
            success += 1
        except Exception as e:
            print(f"[ERROR] Failed to generate {drama_id}: {e}")
            errors += 1

    print(f"\nDrama generation complete: {success} succeeded, {errors} failed")

    if errors > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()
