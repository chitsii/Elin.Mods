from pathlib import Path
import unittest

from tools.drama.drama_builder import DramaBuilder


ROOT = Path(__file__).resolve().parents[3]


def _read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


class CompatDefaultTests(unittest.TestCase):
    def test_point_compat_preserves_allow_installed_default_true(self):
        text = _read("src/Compat/PointCompat.cs")
        self.assertIn("bool allowInstalled = true", text)

    def test_sync_flag_from_dependency_uses_resolve_flag_contract(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.sync_flag_from_dependency("state.quest.can_start.quest_drama_replace_me", "tmp.flag")
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveFlag("state.quest.can_start.quest_drama_replace_me", "tmp.flag");',
            builder.entries[0]["param"],
        )

    def test_run_dependency_command_uses_resolve_run_contract(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.run_dependency_command("cmd.quest.try_start.quest_drama_replace_me")
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("cmd.quest.try_start.quest_drama_replace_me");',
            builder.entries[0]["param"],
        )

    def test_show_book_default_category_is_book(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.show_book("questmod_feature_guide")
        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("show_book(Book/questmod_feature_guide)", builder.entries[0]["param"])


if __name__ == "__main__":
    unittest.main()
