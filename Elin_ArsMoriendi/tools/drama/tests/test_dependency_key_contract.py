import pathlib
import re
import unittest

from tools.drama.data import CommandKeys, CueKeys, ResolveKeys


REPO_ROOT = pathlib.Path(__file__).resolve().parents[3]
RESOLVER_FILE = REPO_ROOT / "src" / "Drama" / "ArsDramaResolver.cs"


def _extract_cases(block: str) -> set[str]:
    return set(re.findall(r'case "([^"]+)":', block))


def _extract_method_block(source: str, start_marker: str, end_marker: str) -> str:
    start = source.index(start_marker)
    end = source.index(end_marker, start)
    return source[start:end]


def _public_constants(cls: type) -> set[str]:
    values = set()
    for name, value in vars(cls).items():
        if name.startswith("_"):
            continue
        if isinstance(value, str):
            values.add(value)
    return values


class DependencyKeyContractTests(unittest.TestCase):
    def test_resolve_keys_follow_prefix_convention(self):
        for key in _public_constants(ResolveKeys):
            self.assertTrue(key.startswith("state."), key)

    def test_command_keys_follow_prefix_convention(self):
        for key in _public_constants(CommandKeys):
            self.assertTrue(key.startswith("cmd."), key)

    def test_cue_keys_follow_prefix_convention(self):
        for key in _public_constants(CueKeys):
            self.assertTrue(key.startswith("cue."), key)

    def test_resolve_keys_are_handled_by_resolver(self):
        source = RESOLVER_FILE.read_text(encoding="utf-8")
        block = _extract_method_block(source, "public bool TryResolveBool(", "public bool TryExecute(")
        resolver_keys = _extract_cases(block)
        self.assertTrue(_public_constants(ResolveKeys).issubset(resolver_keys))

    def test_command_and_cue_keys_are_handled_by_resolver(self):
        source = RESOLVER_FILE.read_text(encoding="utf-8")
        block = _extract_method_block(
            source, "public bool TryExecute(", "private bool TryExecuteGenericFxCommand("
        )
        resolver_keys = _extract_cases(block)
        expected = _public_constants(CommandKeys) | _public_constants(CueKeys)
        self.assertTrue(expected.issubset(resolver_keys))


if __name__ == "__main__":
    unittest.main()
