import unittest
from pathlib import Path

from tools.drama.drama_builder import DramaBuilder


class CommonRuntimeApiTests(unittest.TestCase):
    _QUEST_CHECK_DRAMAS = [
        "ars_apotheosis",
        "ars_cinder_records",
        "ars_dormant_flavor",
        "ars_erenos_ambush",
        "ars_erenos_appear",
        "ars_erenos_defeat",
        "ars_first_servant",
        "ars_first_soul",
        "ars_karen_ambush",
        "ars_karen_encounter",
        "ars_karen_retreat",
        "ars_karen_shadow",
        "ars_scout_ambush",
        "ars_scout_encounter",
        "ars_servant_lost",
        "ars_servant_rampage",
        "ars_seventh_sign",
        "ars_stigmata",
        "ars_tome_awakening",
    ]

    def test_cs_call_emits_eval_entry(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.cs_call("Foo.Bar.Baz", "1", '"x"')

        self.assertEqual(1, len(builder.entries))
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual('Foo.Bar.Baz(1, "x");', builder.entries[0]["param"])

    def test_cs_call_common_runtime_uses_default_runtime_type(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.cs_call_common_runtime("SyncFlags", "EClass.pc")

        self.assertEqual(1, len(builder.entries))
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            "Elin_CommonDrama.DramaRuntime.SyncFlags(EClass.pc);",
            builder.entries[0]["param"],
        )

    def test_cs_call_common_runtime_supports_actor(self):
        builder = DramaBuilder(mod_name="TestMod")
        actor = builder.register_actor("npc", "NPC", "NPC")
        builder.cs_call_common_runtime("PlayCue", '"ritual"', actor=actor)

        self.assertEqual(1, len(builder.entries))
        self.assertEqual("npc", builder.entries[0]["actor"])

    def test_stop_bgm_now_uses_common_runtime(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.stop_bgm_now()

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveRun("cmd.scene.stop_bgm");',
            builder.entries[0]["param"],
        )

    def test_play_pc_effect_with_sound_uses_dependency_command(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.play_pc_effect_with_sound("teleport", "revive")

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveRun("fx.pc.teleport+sfx.pc.revive");',
            builder.entries[0]["param"],
        )

    def test_show_book_uses_single_path_argument(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.show_book("ars_hecatia_guide", "Book")

        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("show_book(Book/ars_hecatia_guide)", builder.entries[0]["param"])

    def test_run_cue_uses_dependency_command(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.run_cue("apotheosis.teleport_rebirth")

        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveRun("cue.apotheosis.teleport_rebirth");',
            builder.entries[0]["param"],
        )

    def test_resolve_flag_uses_common_runtime(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.resolve_flag("state.quest.is_complete", "chitsii.ars.quest.complete")

        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveFlag("state.quest.is_complete", "chitsii.ars.quest.complete");',
            builder.entries[0]["param"],
        )

    def test_resolve_run_uses_common_runtime(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.resolve_run("cmd.erenos.borrow")

        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveRun("cmd.erenos.borrow");',
            builder.entries[0]["param"],
        )

    def test_quest_check_uses_quest_start_condition_key(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.quest_check("ars_first_soul", "chitsii.ars.tmp.can_start")

        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveFlag("state.quest.can_start.ars_first_soul", "chitsii.ars.tmp.can_start");',
            builder.entries[0]["param"],
        )

    def test_quest_try_start_uses_quest_start_command_key(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.quest_try_start("ars_first_soul")

        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveRun("cmd.quest.try_start.ars_first_soul");',
            builder.entries[0]["param"],
        )

    def test_quest_try_start_repeatable_uses_repeatable_command_key(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.quest_try_start_repeatable("ars_karen_ambush")

        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveRun("cmd.quest.try_start_repeatable.ars_karen_ambush");',
            builder.entries[0]["param"],
        )

    def test_quest_try_start_until_complete_uses_until_complete_command_key(self):
        builder = DramaBuilder(mod_name="TestMod")
        builder.quest_try_start_until_complete("ars_karen_ambush")

        self.assertEqual(
            'Elin_CommonDrama.DramaRuntime.ResolveRun("cmd.quest.try_start_until_complete.ars_karen_ambush");',
            builder.entries[0]["param"],
        )

    def test_karen_encounter_starts_with_quest_check(self):
        text = Path("tools/drama/scenarios/ars_karen_encounter.py").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            'builder.quest_check("ars_karen_encounter", can_start_tmp_flag)',
            text,
        )

    def test_karen_shadow_starts_with_quest_check(self):
        text = Path("tools/drama/scenarios/ars_karen_shadow.py").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            'builder.quest_check("ars_karen_shadow", can_start_tmp_flag)',
            text,
        )

    def test_karen_ambush_starts_with_quest_check(self):
        text = Path("tools/drama/scenarios/ars_karen_ambush.py").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            'builder.quest_check("ars_karen_ambush", can_start_tmp_flag)',
            text,
        )

    def test_scout_ambush_starts_with_quest_check(self):
        text = Path("tools/drama/scenarios/ars_scout_ambush.py").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            'builder.quest_check("ars_scout_ambush", can_start_tmp_flag)',
            text,
        )

    def test_erenos_ambush_starts_with_quest_check(self):
        text = Path("tools/drama/scenarios/ars_erenos_ambush.py").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            'builder.quest_check("ars_erenos_ambush", can_start_tmp_flag)',
            text,
        )

    def test_seventh_sign_starts_with_quest_check(self):
        text = Path("tools/drama/scenarios/ars_seventh_sign.py").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            'builder.quest_check("ars_seventh_sign", can_start_tmp_flag)',
            text,
        )

    def test_all_non_hecatia_scenarios_include_quest_check(self):
        for drama_id in self._QUEST_CHECK_DRAMAS:
            with self.subTest(drama_id=drama_id):
                text = Path(f"tools/drama/scenarios/{drama_id}.py").read_text(
                    encoding="utf-8"
                )
                self.assertIn(
                    f'builder.quest_check("{drama_id}", can_start_tmp_flag)',
                    text,
                )

    def test_hecatia_uses_flag_key_constants(self):
        text = Path("tools/drama/scenarios/ars_hecatia_talk.py").read_text(
            encoding="utf-8"
        )
        self.assertNotIn("chitsii.ars.event.hecatia_revealed", text)
        self.assertIn("FlagKeys.HECATIA_REVEALED", text)


if __name__ == "__main__":
    unittest.main()
