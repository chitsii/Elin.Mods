import pathlib
import re
import unittest

from tools.drama.data import CommandKeys, CueKeys, DramaIds, ResolveKeys
from tools.drama.drama_builder import DramaBuilder
from tools.drama.schema.key_spec import KEY_SPECS


REPO_ROOT = pathlib.Path(__file__).resolve().parents[3]


def _public_constants(cls: type) -> dict[str, str]:
    values: dict[str, str] = {}
    for name, value in vars(cls).items():
        if name.startswith("_"):
            continue
        if isinstance(value, str):
            values[name] = value
    return values


def _find_resolver_source() -> tuple[pathlib.Path, str] | None:
    src_root = REPO_ROOT / "src"
    if not src_root.exists():
        return None

    for path in sorted(src_root.rglob("*.cs")):
        text = path.read_text(encoding="utf-8")
        if 'case "' in text and ("TryResolveBool(" in text or "TryExecute(" in text):
            return path, text
    return None


def _extract_switch_case_keys(source: str) -> set[str]:
    return set(re.findall(r'case "([^"]+)":', source))


class DependencyKeyContractTests(unittest.TestCase):
    def test_resolve_keys_follow_prefix_and_shape_convention(self):
        for key in _public_constants(ResolveKeys).values():
            self.assertRegex(key, r"^state\.quest\.(can_start|is_done)\.[a-z0-9_]+$")

    def test_command_keys_follow_prefix_and_shape_convention(self):
        for key in _public_constants(CommandKeys).values():
            self.assertRegex(
                key,
                r"^cmd\.quest\.(try_start|try_start_repeatable|try_start_until_complete|complete)\.[a-z0-9_]+$",
            )

    def test_cue_keys_follow_prefix_convention(self):
        for key in _public_constants(CueKeys).values():
            self.assertRegex(key, r"^cue\.[a-z0-9_]+\.[a-z0-9_]+$")

    def test_resolve_and_command_keys_target_declared_drama_ids(self):
        known_ids = set(DramaIds.ALL)
        for key in _public_constants(ResolveKeys).values():
            self.assertIn(key.split(".")[-1], known_ids)
        for key in _public_constants(CommandKeys).values():
            self.assertIn(key.split(".")[-1], known_ids)

    def test_generated_key_constants_match_schema(self):
        expected = {
            "resolve": {spec.name: spec.value for spec in KEY_SPECS if spec.kind == "resolve"},
            "command": {spec.name: spec.value for spec in KEY_SPECS if spec.kind == "command"},
            "cue": {spec.name: spec.value for spec in KEY_SPECS if spec.kind == "cue"},
        }
        self.assertEqual(expected["resolve"], _public_constants(ResolveKeys))
        self.assertEqual(expected["command"], _public_constants(CommandKeys))
        self.assertEqual(expected["cue"], _public_constants(CueKeys))

    def test_all_dependency_keys_can_be_emitted_by_builder_api(self):
        builder = DramaBuilder(mod_name="QuestMod")
        for key in _public_constants(ResolveKeys).values():
            builder.resolve_flag(key, "yourname.elin_quest_mod.tmp.test")
        for key in _public_constants(CommandKeys).values():
            builder.resolve_run(key)
        for key in _public_constants(CueKeys).values():
            builder.resolve_run(key)

        for entry in builder.entries:
            self.assertEqual("eval", entry.get("action"))
            self.assertIn("Elin_QuestMod.Drama.DramaRuntime.", entry.get("param", ""))

    def test_keys_are_covered_by_resolver_switch_when_resolver_exists(self):
        resolver = _find_resolver_source()
        expected = set(_public_constants(ResolveKeys).values())
        expected |= set(_public_constants(CommandKeys).values())
        expected |= set(_public_constants(CueKeys).values())

        if resolver is None:
            # QuestMod template keeps resolver in shared CWL runtime; enforce local API contract instead.
            builder = DramaBuilder(mod_name="QuestMod")
            for key in expected:
                builder.resolve_run(key)
            self.assertEqual(len(expected), len(builder.entries))
            return

        resolver_path, resolver_source = resolver
        switch_keys = _extract_switch_case_keys(resolver_source)
        missing = sorted(expected - switch_keys)
        self.assertEqual(
            [],
            missing,
            f"Resolver switch-case is missing dependency keys in {resolver_path}",
        )


if __name__ == "__main__":
    unittest.main()
