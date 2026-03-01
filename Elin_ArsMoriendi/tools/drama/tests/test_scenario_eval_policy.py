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


class ScenarioEvalPolicyTests(unittest.TestCase):
    def test_raw_eval_usage_is_not_allowed_in_scenarios(self):
        offenders = {}
        for py_file in sorted(SCENARIOS_DIR.glob("*.py")):
            eval_count, cs_eval_count = _count_eval_calls(py_file)
            if eval_count or cs_eval_count:
                offenders[py_file.name] = (eval_count, cs_eval_count)
        self.assertEqual({}, offenders)


if __name__ == "__main__":
    unittest.main()
