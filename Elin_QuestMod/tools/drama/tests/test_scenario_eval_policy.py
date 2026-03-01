import ast
import pathlib
import unittest


REPO_ROOT = pathlib.Path(__file__).resolve().parents[3]
SCENARIOS_DIR = REPO_ROOT / "tools" / "drama" / "scenarios"


def _count_eval_calls(py_file: pathlib.Path) -> tuple[int, int]:
    source = py_file.read_text(encoding="utf-8")
    tree = ast.parse(source, filename=str(py_file))

    eval_count = 0
    cs_eval_count = 0

    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        func = node.func
        if not isinstance(func, ast.Attribute):
            continue
        if func.attr == "eval":
            eval_count += 1
        elif func.attr == "cs_eval":
            cs_eval_count += 1
    return eval_count, cs_eval_count


def _count_prohibited_api_calls(py_file: pathlib.Path, prohibited: set[str]) -> dict[str, int]:
    source = py_file.read_text(encoding="utf-8")
    tree = ast.parse(source, filename=str(py_file))
    counts: dict[str, int] = {}

    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        func = node.func
        if not isinstance(func, ast.Attribute):
            continue
        if func.attr in prohibited:
            counts[func.attr] = counts.get(func.attr, 0) + 1
    return counts


class ScenarioEvalPolicyTests(unittest.TestCase):
    def test_raw_eval_usage_is_not_allowed_in_scenarios(self):
        offenders = {}
        for py_file in sorted(SCENARIOS_DIR.glob("*.py")):
            eval_count, cs_eval_count = _count_eval_calls(py_file)
            if eval_count or cs_eval_count:
                offenders[py_file.name] = (eval_count, cs_eval_count)
        self.assertEqual({}, offenders)

    def test_modinvoke_dependent_builder_apis_are_not_used_in_scenarios(self):
        prohibited = {"mod_invoke", "switch_flag", "check_quest_available"}
        offenders = {}
        for py_file in sorted(SCENARIOS_DIR.glob("*.py")):
            counts = _count_prohibited_api_calls(py_file, prohibited)
            if counts:
                offenders[py_file.name] = counts
        self.assertEqual({}, offenders)

    def test_feature_showcase_does_not_restart_itself(self):
        py_file = SCENARIOS_DIR / "quest_drama_feature_showcase.py"
        source = py_file.read_text(encoding="utf-8")
        self.assertNotIn('builder.quest_try_start("quest_drama_feature_showcase")', source)
        self.assertNotIn(
            'builder.quest_try_start_repeatable("quest_drama_feature_showcase")',
            source,
        )
        self.assertNotIn(
            'builder.quest_try_start_until_complete("quest_drama_feature_showcase")',
            source,
        )
        self.assertNotIn(
            'builder.resolve_run("cmd.quest.try_start.quest_drama_feature_showcase")',
            source,
        )
        self.assertNotIn(
            'builder.resolve_run("cmd.quest.try_start_repeatable.quest_drama_feature_showcase")',
            source,
        )
        self.assertNotIn(
            'builder.resolve_run("cmd.quest.try_start_until_complete.quest_drama_feature_showcase")',
            source,
        )

    def test_feature_showcase_is_linear_without_branch_choice(self):
        py_file = SCENARIOS_DIR / "quest_drama_feature_showcase.py"
        source = py_file.read_text(encoding="utf-8")
        self.assertNotIn(
            "builder.choice_block(",
            source,
        )
        self.assertNotIn(
            "builder.switch_on_flag(",
            source,
        )
        self.assertNotIn(
            'builder.resolve_run("cmd.quest.try_start_until_complete.quest_drama_feature_followup")',
            source,
        )


if __name__ == "__main__":
    unittest.main()
