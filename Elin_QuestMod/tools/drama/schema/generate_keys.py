#!/usr/bin/env python3
import argparse
import re
import sys
from pathlib import Path

from key_spec import KEY_SPECS


ROOT = Path(__file__).resolve().parents[3]
PY_OUT = ROOT / "tools" / "drama" / "data_generated.py"
CS_OUT = ROOT / "src" / "Drama" / "Generated" / "DramaKeys.g.cs"

KIND_TO_CLASS = {
    "flag": "FlagKeys",
    "resolve": "ResolveKeys",
    "command": "CommandKeys",
    "cue": "CueKeys",
}


def _validate() -> list[str]:
    errors = []
    seen_names = set()
    seen_values = set()
    prefixes = {
        "flag": "yourname.",
        "resolve": "state.",
        "command": "cmd.",
        "cue": "cue.",
    }
    for spec in KEY_SPECS:
        if spec.kind not in KIND_TO_CLASS:
            errors.append(f"Unknown kind: {spec.kind} ({spec.name})")
            continue
        if not re.match(r"^[A-Z0-9_]+$", spec.name):
            errors.append(f"Invalid constant name: {spec.name}")
        if not spec.value.startswith(prefixes[spec.kind]):
            errors.append(f"Invalid prefix for {spec.name}: {spec.value}")
        if spec.name in seen_names:
            errors.append(f"Duplicate name: {spec.name}")
        seen_names.add(spec.name)
        if spec.value in seen_values:
            errors.append(f"Duplicate value: {spec.value}")
        seen_values.add(spec.value)
    return errors


def _group_specs():
    grouped = {k: [] for k in KIND_TO_CLASS}
    for spec in KEY_SPECS:
        grouped[spec.kind].append(spec)
    return grouped


def _emit_python() -> str:
    grouped = _group_specs()
    lines = [
        "# AUTO-GENERATED. DO NOT EDIT.",
        "",
    ]
    for kind in ("flag", "resolve", "command", "cue"):
        cls = KIND_TO_CLASS[kind]
        lines.append(f"class {cls}:")
        lines.append('    """Generated constants."""')
        lines.append("")
        for spec in grouped[kind]:
            lines.append(f'    {spec.name} = "{spec.value}"')
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def _emit_csharp() -> str:
    grouped = _group_specs()
    lines = [
        "// AUTO-GENERATED. DO NOT EDIT.",
        "namespace Elin_QuestMod.DramaKeys",
        "{",
    ]
    for kind in ("flag", "resolve", "command", "cue"):
        cls = KIND_TO_CLASS[kind]
        lines.append(f"    public static class {cls}")
        lines.append("    {")
        for spec in grouped[kind]:
            lines.append(f'        public const string {spec.name} = "{spec.value}";')
        lines.append("    }")
    lines.append("}")
    return "\n".join(lines).rstrip() + "\n"


def _check_or_write(path: Path, content: str, write: bool) -> bool:
    existing = path.read_text(encoding="utf-8") if path.exists() else None
    if write:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        return True
    return existing == content


def main() -> int:
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true")
    mode.add_argument("--write", action="store_true")
    args = parser.parse_args()

    errors = _validate()
    if errors:
        for err in errors:
            print(f"[ERROR] {err}")
        return 1

    py_content = _emit_python()
    cs_content = _emit_csharp()

    ok_py = _check_or_write(PY_OUT, py_content, write=args.write)
    ok_cs = _check_or_write(CS_OUT, cs_content, write=args.write)

    if args.write:
        print(f"[WRITE] {PY_OUT}")
        print(f"[WRITE] {CS_OUT}")
        return 0

    failed = []
    if not ok_py:
        failed.append(str(PY_OUT))
    if not ok_cs:
        failed.append(str(CS_OUT))
    if failed:
        for file_path in failed:
            print(f"[ERROR] Generated file is out of date: {file_path}")
        print("Run: python tools/drama/schema/generate_keys.py --write")
        return 1
    print("[OK] Generated key interfaces are up to date.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

