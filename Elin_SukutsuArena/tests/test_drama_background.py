"""Regression for the opening's real generated background eval row."""
import importlib
from pathlib import Path
import sys
import tempfile
import unittest

import openpyxl

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
from arena.builders import ArenaDramaBuilder
from cwl_quest_lib.builders.drama_builder import DramaBuilder


class DramaBackgroundTests(unittest.TestCase):
    def test_background_uses_explicit_native_loader(self):
        builder = DramaBuilder()
        self.assertIs(builder.set_background("Drama/arena_lobby"), builder)
        self.assertEqual(
            builder.entries[-1],
            {"action": "eval", "param": 'dm.imageBG.enabled = true; '
             'dm.imageBG.sprite = ModUtil.LoadSprite("Drama/arena_lobby");'},
        )

    def test_actual_opening_workbook_has_executable_background_call(self):
        builder = ArenaDramaBuilder()
        opening = importlib.import_module("arena.scenarios.01_opening")
        opening.define_opening_drama(builder)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "drama_sukutsu_opening.xlsx"
            builder.save(str(path), sheet_name="sukutsu_opening")
            workbook = openpyxl.load_workbook(path, read_only=True, data_only=True)
            try:
                rows = list(workbook.active.values)
                headers = list(rows[0])
                param = headers.index("param")
                action = headers.index("action")
                backgrounds = [row for row in rows[1:]
                               if "arena_lobby" in str(row[param])]
                self.assertEqual(len(backgrounds), 1)
                self.assertEqual(backgrounds[0][action], "eval")
                self.assertIn('ModUtil.LoadSprite("Drama/arena_lobby")',
                              backgrounds[0][param])
                self.assertNotIn('.LoadSprite()', backgrounds[0][param])
            finally:
                workbook.close()


if __name__ == "__main__":
    unittest.main()
