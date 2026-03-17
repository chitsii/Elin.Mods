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

# from drama.scenarios.ars_first_soul import define_first_soul
# from drama.scenarios.ars_karen_encounter import define_karen_encounter
# from drama.scenarios.ars_karen_retreat import define_karen_retreat
# from drama.scenarios.ars_cinder_records import define_cinder_records
# from drama.scenarios.ars_scout_encounter import define_scout_encounter
# from drama.scenarios.ars_stigmata import define_stigmata
# from drama.scenarios.ars_erenos_appear import define_erenos_appear
# from drama.scenarios.ars_erenos_defeat import define_erenos_defeat
# from drama.scenarios.ars_apotheosis import define_apotheosis
# from drama.scenarios.ars_first_servant import define_first_servant
# from drama.scenarios.ars_servant_rampage import define_servant_rampage
# from drama.scenarios.ars_servant_lost import define_servant_lost
# from drama.scenarios.ars_karen_shadow import define_karen_shadow
# from drama.scenarios.ars_seventh_sign import define_seventh_sign
# from drama.scenarios.ars_karen_ambush import define_karen_ambush
# from drama.scenarios.ars_erenos_ambush import define_erenos_ambush
# from drama.scenarios.ars_scout_ambush import define_scout_ambush
# from drama.scenarios.ars_hecatia_talk import define_hecatia_talk
from drama_v2.scenarios.ars_apotheosis import save_apotheosis_xlsx
from drama_v2.scenarios.ars_cinder_records import save_cinder_records_xlsx
from drama_v2.scenarios.ars_dormant_flavor import save_dormant_flavor_xlsx
from drama_v2.scenarios.ars_erenos_ambush import save_erenos_ambush_xlsx
from drama_v2.scenarios.ars_erenos_appear import save_erenos_appear_xlsx
from drama_v2.scenarios.ars_erenos_defeat import save_erenos_defeat_xlsx
from drama_v2.scenarios.ars_first_servant import save_first_servant_xlsx
from drama_v2.scenarios.ars_first_soul import save_first_soul_xlsx
from drama_v2.scenarios.ars_hecatia_talk import save_hecatia_talk_xlsx
from drama_v2.scenarios.ars_karen_ambush import save_karen_ambush_xlsx
from drama_v2.scenarios.ars_karen_encounter import save_karen_encounter_xlsx
from drama_v2.scenarios.ars_karen_retreat import save_karen_retreat_xlsx
from drama_v2.scenarios.ars_karen_shadow import save_karen_shadow_xlsx
from drama_v2.scenarios.ars_scout_ambush import save_scout_ambush_xlsx
from drama_v2.scenarios.ars_scout_encounter import save_scout_encounter_xlsx
from drama_v2.scenarios.ars_servant_lost import save_servant_lost_xlsx
from drama_v2.scenarios.ars_servant_rampage import save_servant_rampage_xlsx
from drama_v2.scenarios.ars_seventh_sign import save_seventh_sign_xlsx
from drama_v2.scenarios.ars_stigmata import save_stigmata_xlsx
from drama_v2.scenarios.ars_tome_awakening import save_tome_awakening_xlsx

from drama.data import DramaIds

# Output directory
OUTPUT_DIR = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Dialog", "Drama")

# Drama definitions: (drama_id, v2 save_function)
DRAMAS = [
    (DramaIds.FIRST_SOUL, save_first_soul_xlsx),
    (DramaIds.TOME_AWAKENING, save_tome_awakening_xlsx),
    (DramaIds.KAREN_ENCOUNTER, save_karen_encounter_xlsx),
    (DramaIds.KAREN_RETREAT, save_karen_retreat_xlsx),
    (DramaIds.CINDER_RECORDS, save_cinder_records_xlsx),
    (DramaIds.SCOUT_ENCOUNTER, save_scout_encounter_xlsx),
    (DramaIds.STIGMATA, save_stigmata_xlsx),
    (DramaIds.ERENOS_APPEAR, save_erenos_appear_xlsx),
    (DramaIds.ERENOS_DEFEAT, save_erenos_defeat_xlsx),
    (DramaIds.APOTHEOSIS, save_apotheosis_xlsx),
    (DramaIds.FIRST_SERVANT, save_first_servant_xlsx),
    (DramaIds.SERVANT_RAMPAGE, save_servant_rampage_xlsx),
    (DramaIds.SERVANT_LOST, save_servant_lost_xlsx),
    (DramaIds.DORMANT_FLAVOR, save_dormant_flavor_xlsx),
    (DramaIds.KAREN_SHADOW, save_karen_shadow_xlsx),
    (DramaIds.SEVENTH_SIGN, save_seventh_sign_xlsx),
    (DramaIds.KAREN_AMBUSH, save_karen_ambush_xlsx),
    (DramaIds.ERENOS_AMBUSH, save_erenos_ambush_xlsx),
    (DramaIds.SCOUT_AMBUSH, save_scout_ambush_xlsx),
    (DramaIds.HECATIA_TALK, save_hecatia_talk_xlsx),
]


def validate_drama_registry() -> None:
    missing = [drama_id for drama_id, save_fn in DRAMAS if not callable(save_fn)]
    if missing:
        missing_csv = ", ".join(missing)
        raise RuntimeError(
            "Drama generator registry is incomplete. "
            f"Missing v2 save handler(s): {missing_csv}"
        )


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    validate_drama_registry()

    success = 0
    errors = 0

    for drama_id, save_fn in DRAMAS:
        try:
            filename = f"drama_{drama_id}.xlsx"
            filepath = os.path.join(OUTPUT_DIR, filename)
            save_fn(filepath)
            success += 1
        except Exception as e:
            print(f"[ERROR] Failed to generate {drama_id}: {e}")
            errors += 1

    print(f"\nDrama generation complete: {success} succeeded, {errors} failed")

    if errors > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()
