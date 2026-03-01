from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[3]


def _read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


class CompatDefaultTests(unittest.TestCase):
    def test_point_compat_preserves_allow_installed_default_true(self):
        text = _read("src/Compat/PointCompat.cs")
        self.assertIn("bool allowInstalled = true", text)

    def test_safeinvoke_preserves_allow_installed_default_true(self):
        text = _read("src/Compat/SafeInvoke.cs")
        self.assertIn("bool allowInstalled = true", text)

    def test_quest_create_strict_signatures_include_optional_parameter_form(self):
        text = _read("src/Compat/CompatSymbol.cs")
        self.assertIn(
            'new CompatMethodSignature("Create", typeof(Quest), new[] { typeof(string), typeof(string), typeof(Chara), typeof(bool) }, allowDerivedReturnType: true)',
            text,
        )


if __name__ == "__main__":
    unittest.main()
