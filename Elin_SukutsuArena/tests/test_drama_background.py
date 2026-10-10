"""Regression for the opening's real generated background eval row."""
import importlib
from pathlib import Path
import sys
import tempfile
import unittest
import shutil

import openpyxl

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
from arena.builders import ArenaDramaBuilder
from cwl_quest_lib.builders.drama_builder import DramaBuilder
from builder.validate_drama_package import OPENING, validate_package


class DramaBackgroundTests(unittest.TestCase):
    def make_package(self, root, code):
        path = root / OPENING
        path.parent.mkdir(parents=True)
        workbook = openpyxl.Workbook()
        workbook.active.append(["action", "param"])
        workbook.active.append(["eval", code])
        workbook.save(path)
        workbook.close()
        return path

    def test_package_gate_rejects_reproduced_subscription_eval(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_package(root, 'dm.imageBG.sprite = "Drama/arena_lobby".LoadSprite();')
            errors = validate_package(root)
            self.assertTrue(any("obsolete string.LoadSprite" in error for error in errors), errors)

    def test_package_gate_accepts_native_payload_and_identical_copy(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, target = root / "source", root / "target"
            self.make_package(source, 'dm.imageBG.sprite = ModUtil.LoadSprite("Drama/arena_lobby");')
            shutil.copytree(source, target)
            self.assertEqual(validate_package(source), [])
            self.assertEqual(validate_package(target, source), [])

    def test_package_gate_rejects_missing_or_different_copied_workbook(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, target = root / "source", root / "target"
            self.make_package(source, 'dm.imageBG.sprite = ModUtil.LoadSprite("Drama/arena_lobby");')
            self.assertTrue(any("Missing copied drama" in e for e in validate_package(target, source)))
            self.make_package(target, 'dm.imageBG.sprite = ModUtil.LoadSprite("Drama/another");')
            self.assertTrue(any("SHA256 differs" in e for e in validate_package(target, source)))

    def test_package_gate_rejects_obsolete_extra_workbook(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, target = root / "source", root / "target"
            self.make_package(source, 'dm.imageBG.sprite = ModUtil.LoadSprite("Drama/arena_lobby");')
            shutil.copytree(source, target)
            shutil.copy2(target / OPENING, target / OPENING.parent / "drama_removed.xlsx")
            self.assertTrue(any("Unexpected stale drama" in e for e in validate_package(target, source)))

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
