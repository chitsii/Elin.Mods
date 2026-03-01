from pathlib import Path
import unittest

import openpyxl

from tools.drama.data import Actors


ROOT = Path(__file__).resolve().parents[3]
SOURCE_GAME_XLSX = ROOT.parent / "SourceExcels" / "SourceGame.xlsx"


def _load_person_ids() -> set[str]:
    wb = openpyxl.load_workbook(SOURCE_GAME_XLSX, read_only=True, data_only=True)
    ws = wb["Person"]
    ids: set[str] = set()
    for row in ws.iter_rows(min_row=4, values_only=True):
        if not row:
            continue
        value = row[0]
        if isinstance(value, str) and value:
            ids.add(value)
    return ids


class SourceExcelContractTests(unittest.TestCase):
    def test_source_game_exists(self):
        if not SOURCE_GAME_XLSX.exists():
            self.skipTest(f"Optional local source excel not found: {SOURCE_GAME_XLSX}")
        self.assertTrue(SOURCE_GAME_XLSX.exists(), str(SOURCE_GAME_XLSX))

    def test_default_guide_actor_is_resolvable_or_builtin(self):
        # built-ins handled by drama runtime aliases
        builtin_aliases = {"pc", "tg", "narrator", "god"}
        if Actors.GUIDE in builtin_aliases:
            return
        if not SOURCE_GAME_XLSX.exists():
            self.skipTest(f"Optional local source excel not found: {SOURCE_GAME_XLSX}")

        person_ids = _load_person_ids()
        self.assertIn(
            Actors.GUIDE,
            person_ids,
            f"Actors.GUIDE='{Actors.GUIDE}' is not in SourceGame.xlsx Person table",
        )


if __name__ == "__main__":
    unittest.main()
