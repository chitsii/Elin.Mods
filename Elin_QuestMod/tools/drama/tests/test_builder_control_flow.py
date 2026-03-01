import unittest

from tools.drama.drama_builder import DramaBuilder


class BuilderControlFlowTests(unittest.TestCase):
    def test_switch_on_flag_fallback_is_unconditional_default_jump(self):
        builder = DramaBuilder(mod_name="QuestMod")
        step_a = builder.label("a")
        step_b = builder.label("b")
        fallback = builder.label("fallback")

        builder.switch_on_flag("yourname.elin_quest_mod.tmp.route", {1: step_a, 2: step_b}, fallback=fallback)

        self.assertEqual(3, len(builder.entries))
        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("if_flag(yourname.elin_quest_mod.tmp.route, ==1)", builder.entries[0]["param"])
        self.assertEqual("a", builder.entries[0]["jump"])

        self.assertEqual("invoke*", builder.entries[1]["action"])
        self.assertEqual("if_flag(yourname.elin_quest_mod.tmp.route, ==2)", builder.entries[1]["param"])
        self.assertEqual("b", builder.entries[1]["jump"])

        # default branch must work for all unmatched values, not only ==0
        self.assertEqual({"jump": "fallback"}, builder.entries[2])


if __name__ == "__main__":
    unittest.main()
