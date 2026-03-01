import tempfile
import unittest
from collections import defaultdict
from pathlib import Path

from tools.drama.drama_builder import ChoiceReaction, DramaBuilder
from tools.drama.scenarios.quest_drama_feature_showcase import (
    define_quest_drama_feature_showcase,
)
from tools.drama.scenarios.quest_drama_feature_showcase_followup import (
    define_quest_drama_feature_showcase_followup,
)
from tools.drama.scenarios.quest_drama_feature_branch_a import (
    define_quest_drama_feature_branch_a,
)
from tools.drama.scenarios.quest_drama_feature_branch_b import (
    define_quest_drama_feature_branch_b,
)
from tools.drama.scenarios.quest_drama_feature_merge import (
    define_quest_drama_feature_merge,
)
from tools.drama.scenarios.quest_drama_placeholder import define_quest_drama_placeholder


class CommonRuntimeApiTests(unittest.TestCase):
    @staticmethod
    def _extract_step_edges(entries: list[dict]) -> dict[str, set[str]]:
        edges: dict[str, set[str]] = defaultdict(set)
        current_step = ""
        for entry in entries:
            step = entry.get("step")
            if isinstance(step, str) and step:
                current_step = step
            jump = entry.get("jump")
            if current_step and isinstance(jump, str) and jump:
                edges[current_step].add(jump)
        return edges

    @staticmethod
    def _find_cycles(edges: dict[str, set[str]]) -> list[list[str]]:
        nodes = set(edges.keys())
        for dests in edges.values():
            nodes.update(dests)

        state: dict[str, int] = {}
        stack: list[str] = []
        cycles: list[list[str]] = []

        def dfs(node: str) -> None:
            state[node] = 1
            stack.append(node)
            for nxt in edges.get(node, set()):
                if nxt not in nodes:
                    continue
                if state.get(nxt, 0) == 0:
                    dfs(nxt)
                elif state.get(nxt) == 1:
                    idx = stack.index(nxt)
                    cycles.append(stack[idx:] + [nxt])
            stack.pop()
            state[node] = 2

        for node in nodes:
            if state.get(node, 0) == 0:
                dfs(node)
        return cycles

    def test_cs_call_emits_eval_entry(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.cs_call("Foo.Bar.Baz", "1", '"x"')

        self.assertEqual(1, len(builder.entries))
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual('Foo.Bar.Baz(1, "x");', builder.entries[0]["param"])

    def test_cs_call_common_runtime_uses_default_runtime_type(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.cs_call_common_runtime("SyncFlags", "EClass.pc")

        self.assertEqual(1, len(builder.entries))
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            "Elin_QuestMod.Drama.DramaRuntime.SyncFlags(EClass.pc);",
            builder.entries[0]["param"],
        )

    def test_cs_call_common_runtime_supports_actor(self):
        builder = DramaBuilder(mod_name="QuestMod")
        actor = builder.register_actor("npc", "NPC", "NPC")
        builder.cs_call_common_runtime("PlayCue", '"ritual"', actor=actor)

        self.assertEqual(1, len(builder.entries))
        self.assertEqual("npc", builder.entries[0]["actor"])

    def test_stop_bgm_now_uses_standard_action(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.stop_bgm_now()

        self.assertEqual("stopBGM", builder.entries[0]["action"])
        self.assertNotIn("param", builder.entries[0])

    def test_play_pc_effect_with_sound_uses_dependency_command(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.play_pc_effect_with_sound("teleport", "revive")

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("fx.pc.teleport+sfx.pc.revive");',
            builder.entries[0]["param"],
        )

    def test_show_book_uses_single_path_argument(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.show_book("questmod_feature_guide", "Book")

        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("show_book(Book/questmod_feature_guide)", builder.entries[0]["param"])

    def test_set_background_uses_standard_setbg_action(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.set_background("bg3")

        self.assertEqual("setBG", builder.entries[0]["action"])
        self.assertEqual("bg3", builder.entries[0]["param"])

    def test_drama_end_clears_background_before_finish(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.drama_end(0.2)

        self.assertEqual("fadeOut", builder.entries[0]["action"])
        self.assertEqual("setBG", builder.entries[1]["action"])
        self.assertEqual("", builder.entries[1]["param"])
        self.assertEqual("end", builder.entries[2]["action"])

    def test_run_cue_uses_dependency_command(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.run_cue("questmod.feature_showcase_pulse")

        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("cue.questmod.feature_showcase_pulse");',
            builder.entries[0]["param"],
        )

    def test_resolve_flag_uses_common_runtime(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.resolve_flag("state.quest.can_start.quest_drama_feature_showcase", "tmp.can_start")

        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveFlag("state.quest.can_start.quest_drama_feature_showcase", "tmp.can_start");',
            builder.entries[0]["param"],
        )

    def test_resolve_flags_all_emits_eval_inline(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.resolve_flags_all(["flag.a", "flag.b"], "tmp.all")

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertIn('var __keys="flag.a,flag.b".Split', builder.entries[0]["param"])
        self.assertIn('__flags["tmp.all"]', builder.entries[0]["param"])

    def test_resolve_flags_any_emits_eval_inline(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.resolve_flags_any(["flag.a", "flag.b"], "tmp.any")

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertIn('var __keys="flag.a,flag.b".Split', builder.entries[0]["param"])
        self.assertIn('__flags["tmp.any"]', builder.entries[0]["param"])

    def test_resolve_cooldown_elapsed_days_emits_eval_inline(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.resolve_cooldown_elapsed_days("flag.last_raw", "tmp.cooldown_ready", 2)

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertIn('__flags.TryGetValue("flag.last_raw"', builder.entries[0]["param"])
        self.assertIn("__th=2880", builder.entries[0]["param"])
        self.assertIn('__flags["tmp.cooldown_ready"]', builder.entries[0]["param"])

    def test_stamp_current_raw_time_emits_eval_inline(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.stamp_current_raw_time("flag.last_raw")

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertIn('__flags["flag.last_raw"]=EClass.world.date.GetRaw()', builder.entries[0]["param"])

    def test_resolve_run_uses_common_runtime(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.resolve_run("cmd.quest.try_start.quest_drama_feature_showcase")

        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("cmd.quest.try_start.quest_drama_feature_showcase");',
            builder.entries[0]["param"],
        )

    def test_quest_check_uses_quest_start_condition_key(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.quest_check("quest_drama_feature_showcase", "tmp.can_start")

        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveFlag("state.quest.can_start.quest_drama_feature_showcase", "tmp.can_start");',
            builder.entries[0]["param"],
        )

    def test_check_quests_uses_quest_check_and_branch_if(self):
        builder = DramaBuilder(mod_name="QuestMod")
        target = builder.label("next")
        builder.check_quests([("quest_drama_feature_showcase", target)])

        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertIn(
            'ResolveFlag("state.quest.can_start.quest_drama_feature_showcase", "tmp.quest_check.0")',
            builder.entries[0]["param"],
        )
        self.assertEqual("invoke*", builder.entries[1]["action"])
        self.assertEqual("if_flag(tmp.quest_check.0, ==1)", builder.entries[1]["param"])
        self.assertEqual("next", builder.entries[1]["jump"])

    def test_quest_try_start_uses_quest_start_command_key(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.quest_try_start("quest_drama_feature_showcase")

        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("cmd.quest.try_start.quest_drama_feature_showcase");',
            builder.entries[0]["param"],
        )

    def test_quest_try_start_repeatable_uses_repeatable_command_key(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.quest_try_start_repeatable("quest_drama_feature_showcase")

        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("cmd.quest.try_start_repeatable.quest_drama_feature_showcase");',
            builder.entries[0]["param"],
        )

    def test_quest_try_start_until_complete_uses_until_complete_command_key(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.quest_try_start_until_complete("quest_drama_feature_showcase")

        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("cmd.quest.try_start_until_complete.quest_drama_feature_showcase");',
            builder.entries[0]["param"],
        )

    def test_start_quest_uses_standard_action(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.start_quest("quest_drama_feature_showcase")

        self.assertEqual("startQuest", builder.entries[0]["action"])
        self.assertEqual("quest_drama_feature_showcase", builder.entries[0]["param"])

    def test_complete_quest_uses_standard_action(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.complete_quest("quest_drama_feature_showcase")

        self.assertEqual("completeQuest", builder.entries[0]["action"])
        self.assertEqual("quest_drama_feature_showcase", builder.entries[0]["param"])

    def test_choice_reaction_complete_quest_uses_standard_action(self):
        reaction = ChoiceReaction("ok").complete_quest("quest_drama_feature_showcase")
        self.assertEqual("completeQuest", reaction.actions[0]["action"])
        self.assertEqual("quest_drama_feature_showcase", reaction.actions[0]["param"])

    def test_legacy_modinvoke_apis_are_removed(self):
        builder = DramaBuilder(mod_name="QuestMod")
        self.assertFalse(hasattr(builder, "mod_invoke"))
        self.assertFalse(hasattr(builder, "switch_flag"))
        self.assertFalse(hasattr(builder, "check_quest_available"))
        self.assertFalse(hasattr(builder, "flash_lut"))

    def test_add_temp_actor_uses_standard_action(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.add_temp_actor("tg")
        self.assertEqual("addTempActor", builder.entries[0]["action"])
        self.assertEqual("tg", builder.entries[0]["actor"])

    def test_move_next_to_uses_invoke_expansion(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.move_next_to("pc", actor="tg")
        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("move_next_to(pc)", builder.entries[0]["param"])
        self.assertEqual("tg", builder.entries[0]["actor"])

    def test_invoke_expansion_supports_jump(self):
        builder = DramaBuilder(mod_name="QuestMod")
        jump = builder.label("next")
        builder.invoke_expansion("if_flag", "foo.bar", ">=1", actor="pc", jump=jump)
        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("if_flag(foo.bar, >=1)", builder.entries[0]["param"])
        self.assertEqual("next", builder.entries[0]["jump"])

    def test_move_tile_uses_invoke_expansion(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.move_tile(1, -2, actor="tg")
        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("move_tile(1, -2)", builder.entries[0]["param"])
        self.assertEqual("tg", builder.entries[0]["actor"])

    def test_set_portrait_and_set_sprite_use_invoke_expansion(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.set_portrait(actor="tg")
        builder.set_sprite(actor="tg")
        self.assertEqual("set_portrait()", builder.entries[0]["param"])
        self.assertEqual("set_sprite()", builder.entries[1]["param"])
        self.assertEqual("tg", builder.entries[0]["actor"])
        self.assertEqual("tg", builder.entries[1]["actor"])

    def test_spawn_npc_uses_eval_without_modinvoke(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.spawn_npc("chicken", level=1)
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertIn('CharaGen.Create("chicken", 1)', builder.entries[0]["param"])
        self.assertNotIn("modInvoke", builder.entries[0]["param"])

    def test_spawn_npc_can_bind_spawned_actor_alias(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.spawn_npc("chicken", level=1, actor_alias="showcase_chicken")
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertIn('drama.sequence.AddActor("showcase_chicken", new Person(spawned));', builder.entries[0]["param"])

    def test_placeholder_scenario_can_be_saved(self):
        builder = DramaBuilder(mod_name="QuestMod")
        define_quest_drama_placeholder(builder)

        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "drama_quest_drama_replace_me.xlsx"
            builder.save(str(out), sheet_name="quest_drama_replace_me")
            self.assertTrue(out.exists())

    def test_feature_showcase_scenario_can_be_saved(self):
        builder = DramaBuilder(mod_name="QuestMod")
        define_quest_drama_feature_showcase(builder)

        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "drama_quest_drama_feature_showcase.xlsx"
            builder.save(str(out), sheet_name="quest_drama_feature_showcase")
            self.assertTrue(out.exists())

    def test_feature_showcase_followup_scenario_can_be_saved(self):
        builder = DramaBuilder(mod_name="QuestMod")
        define_quest_drama_feature_showcase_followup(builder)

        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "drama_quest_drama_feature_followup.xlsx"
            builder.save(str(out), sheet_name="quest_drama_feature_followup")
            self.assertTrue(out.exists())

    def test_feature_branch_a_scenario_can_be_saved(self):
        builder = DramaBuilder(mod_name="QuestMod")
        define_quest_drama_feature_branch_a(builder)

        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "drama_quest_drama_feature_branch_a.xlsx"
            builder.save(str(out), sheet_name="quest_drama_feature_branch_a")
            self.assertTrue(out.exists())

    def test_feature_branch_b_scenario_can_be_saved(self):
        builder = DramaBuilder(mod_name="QuestMod")
        define_quest_drama_feature_branch_b(builder)

        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "drama_quest_drama_feature_branch_b.xlsx"
            builder.save(str(out), sheet_name="quest_drama_feature_branch_b")
            self.assertTrue(out.exists())

    def test_feature_merge_scenario_can_be_saved(self):
        builder = DramaBuilder(mod_name="QuestMod")
        define_quest_drama_feature_merge(builder)

        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "drama_quest_drama_feature_merge.xlsx"
            builder.save(str(out), sheet_name="quest_drama_feature_merge")
            self.assertTrue(out.exists())

    def test_feature_showcase_uses_temp_flags_and_linear_flow(self):
        text = Path("tools/drama/scenarios/quest_drama_feature_showcase.py").read_text(
            encoding="utf-8"
        )
        self.assertIn("builder.set_flag(FlagKeys.TMP_CAN_START_FEATURE, 0)", text)
        self.assertIn("builder.set_flag(FlagKeys.TMP_IS_DONE_FEATURE, 0)", text)
        self.assertIn("builder.set_flag(FlagKeys.TMP_BRANCH_FEATURE, 0)", text)
        self.assertIn("builder.set_flag(FlagKeys.TMP_COUNT_FEATURE, 0)", text)
        self.assertIn(
            'builder.set_flag("yourname.elin_quest_mod.flag.feature_followup.run_count", 0)',
            text,
        )
        self.assertIn(
            'builder.set_flag("yourname.elin_quest_mod.flag.feature_followup.last_advance_raw", 0)',
            text,
        )
        self.assertIn(
            'builder.set_flag("yourname.elin_quest_mod.flag.feature.branch.last_choice", 0)',
            text,
        )
        self.assertIn("builder.quest_check(", text)
        self.assertIn("builder.resolve_flag(", text)
        self.assertIn("builder.set_flag(", text)
        self.assertIn("builder.mod_flag(", text)
        self.assertIn("builder.play_bgm_vanilla(", text)
        self.assertIn("builder.scene_transition(", text)
        self.assertIn("builder.move_next_to(", text)
        self.assertIn("builder.move_tile(", text)
        self.assertIn("builder.set_sprite(", text)
        self.assertIn("builder.spawn_npc(", text)
        self.assertIn("builder.set_portrait(", text)
        self.assertIn("builder.play_pc_effect_with_sound(", text)
        self.assertIn(
            'builder.stamp_current_raw_time("yourname.elin_quest_mod.flag.feature_followup.last_advance_raw")',
            text,
        )
        self.assertIn("quest_drama_feature_followup", text)
        self.assertNotIn("builder.choice_block(", text)
        self.assertNotIn("builder.switch_on_flag(", text)
        self.assertNotIn('builder.quest_try_start("quest_drama_feature_showcase")', text)
        self.assertNotIn("mod_invoke(", text)
        self.assertNotIn("modInvoke", text)

    def test_feature_showcase_step_graph_has_no_cycle(self):
        builder = DramaBuilder(mod_name="QuestMod")
        define_quest_drama_feature_showcase(builder)
        cycles = self._find_cycles(self._extract_step_edges(builder.entries))
        self.assertEqual([], cycles)

    def test_feature_followup_covers_state_and_diagnostics(self):
        text = Path(
            "tools/drama/scenarios/quest_drama_feature_showcase_followup.py"
        ).read_text(encoding="utf-8")
        self.assertIn("FLAG_FOLLOWUP_RUN_COUNT", text)
        self.assertIn("FLAG_FOLLOWUP_STAGE", text)
        self.assertIn("FLAG_SHOWCASE_DONE_AT_ENTRY", text)
        self.assertIn("FLAG_FOLLOWUP_DEMO_START_COND", text)
        self.assertIn("FLAG_FOLLOWUP_DEMO_COOLDOWN", text)
        self.assertIn("builder.set_flag(FLAG_SHOWCASE_DONE_AT_ENTRY, 0)", text)
        self.assertIn("builder.set_flag(FLAG_FOLLOWUP_CAN_START, 0)", text)
        self.assertIn("builder.set_flag(FLAG_FOLLOWUP_IS_DONE, 0)", text)
        self.assertIn("builder.resolve_flags_all(", text)
        self.assertIn("builder.resolve_cooldown_elapsed_days(", text)
        self.assertIn("builder.mod_flag(FLAG_FOLLOWUP_RUN_COUNT, \"+\", 1)", text)
        self.assertIn("builder.resolve_flag(", text)
        self.assertIn("ResolveKeys.QUEST_DONE_FEATURE", text)
        self.assertIn("builder.switch_on_flag(", text)
        self.assertIn("builder.choice(", text)
        self.assertIn("quest_drama_feature_branch_a", text)
        self.assertIn("quest_drama_feature_branch_b", text)
        self.assertNotIn("builder.choice_block(", text)
        self.assertIn("builder.clear_background()", text)
        self.assertIn(
            "builder.resolve_run(\"cmd.quest.complete.quest_drama_feature_followup\")",
            text,
        )


if __name__ == "__main__":
    unittest.main()
