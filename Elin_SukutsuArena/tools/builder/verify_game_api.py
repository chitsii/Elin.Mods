# -*- coding: utf-8 -*-
"""
verify_game_api.py - Validate that game APIs referenced by [GameDependency] exist.

This script extracts dependencies from C# source and validates that types/members
exist in the game's managed assemblies (Assembly-CSharp.dll).

Usage:
    cd tools && uv run python builder/verify_game_api.py
    cd tools && uv run python builder/verify_game_api.py --ci
    cd tools && uv run python builder/verify_game_api.py --assembly "C:\\...\\Assembly-CSharp.dll"
"""

from __future__ import annotations

import argparse
import json
import re
import os
import sys
import tempfile
from pathlib import Path
from typing import List

SCRIPT_DIR = Path(__file__).parent
TOOLS_DIR = SCRIPT_DIR.parent
sys.path.insert(0, str(TOOLS_DIR))

from builder.extract_dependencies import extract_from_file


HARMONY_PATCH_PATTERN = re.compile(
    r'\[HarmonyPatch\s*\(\s*typeof\s*\(\s*(?P<type>\w+)\s*\)\s*,\s*'
    r'(?:nameof\s*\(\s*\w+\.(?P<method1>\w+)\s*\)|"(?P<method2>[^"]+)")'
    r'(?:\s*,\s*new\[\]\s*\{\s*(?P<types>[^}]*)\s*\})?',
    re.MULTILINE,
)

TYPEOF_PATTERN = re.compile(r'typeof\s*\(\s*([^\)]+)\s*\)')


def find_default_managed_dir() -> Path | None:
    candidates = [
        Path(r"C:\Program Files (x86)\Steam\steamapps\common\Elin\Elin_Data\Managed"),
        Path(r"C:\Program Files\Steam\steamapps\common\Elin\Elin_Data\Managed"),
    ]
    for c in candidates:
        if c.exists():
            return c
    return None


def resolve_assembly_path(args) -> Path | None:
    if args.assembly:
        return Path(args.assembly)

    if args.managed_dir:
        managed_dir = Path(args.managed_dir)
        return managed_dir / "Assembly-CSharp.dll"

    if args.game_dir:
        managed_dir = Path(args.game_dir) / "Elin_Data" / "Managed"
        return managed_dir / "Assembly-CSharp.dll"

    env_managed = os.environ.get("ELIN_MANAGED_DIR")
    if env_managed:
        return Path(env_managed) / "Assembly-CSharp.dll"

    env_game = os.environ.get("ELIN_GAME_DIR")
    if env_game:
        return Path(env_game) / "Elin_Data" / "Managed" / "Assembly-CSharp.dll"

    default_dir = find_default_managed_dir()
    if default_dir:
        return default_dir / "Assembly-CSharp.dll"

    return None


def pick_best_game_assembly(assembly_path: Path) -> Path:
    if not assembly_path.exists():
        return assembly_path

    managed_dir = assembly_path.parent
    elin_dll = managed_dir / "Elin.dll"

    try:
        size = assembly_path.stat().st_size
    except OSError:
        size = 0

    if assembly_path.name.lower() == "assembly-csharp.dll" and size < 200_000 and elin_dll.exists():
        return elin_dll

    return assembly_path


def resolve_plugin_dir(args) -> Path | None:
    if args.plugin_dir:
        return Path(args.plugin_dir)

    env_game = os.environ.get("ELIN_GAME_DIR")
    if env_game:
        candidate = Path(env_game) / "BepInEx" / "plugins"
        if candidate.exists():
            return candidate

    return None


def extract_harmony_signatures(src_dir: Path) -> dict[tuple[str, str], list[str]]:
    signatures: dict[tuple[str, str], list[str]] = {}
    for cs_file in src_dir.rglob("*.cs"):
        try:
            content = cs_file.read_text(encoding="utf-8-sig")
        except Exception:
            continue

        for match in HARMONY_PATCH_PATTERN.finditer(content):
            type_name = match.group("type")
            method_name = match.group("method1") or match.group("method2") or ""
            if not type_name or not method_name:
                continue
            type_block = match.group("types")
            if not type_block:
                continue
            type_names = [t.strip() for t in TYPEOF_PATTERN.findall(type_block)]
            if not type_names:
                continue
            signatures[(type_name, method_name)] = type_names
    return signatures


def extract_dependencies(project_root: Path) -> List[dict]:
    src_dir = project_root / "src"
    harmony_signatures = extract_harmony_signatures(src_dir)
    deps = []
    for cs_file in src_dir.rglob("*.cs"):
        for d in extract_from_file(cs_file, project_root):
            signature = None
            if d.dep_type == "Patch":
                type_name, member_name = split_target(d.target)
                if type_name and member_name:
                    signature_list = harmony_signatures.get((type_name, member_name))
                    if signature_list:
                        signature = ",".join(signature_list)
            deps.append(
                {
                    "DepType": d.dep_type,
                    "Target": d.target,
                    "Risk": d.risk,
                    "Notes": d.notes,
                    "File": d.file,
                    "Line": d.line,
                    "Context": d.context,
                    "Signature": signature,
                }
            )
    return deps


def split_target(target: str) -> tuple[str, str | None]:
    if not target:
        return "", None
    if "." not in target:
        return target, None
    left, right = target.rsplit(".", 1)
    return left, right


def run_inspector(project_root: Path, assembly_path: Path, deps_json: Path, args) -> int:
    inspector_proj = project_root / "tools" / "builder" / "GameApiInspector" / "GameApiInspector.csproj"
    if not inspector_proj.exists():
        print(f"Error: GameApiInspector project not found: {inspector_proj}")
        return 2

    cmd = [
        "dotnet",
        "run",
        "--project",
        str(inspector_proj),
        "--",
        "--assembly",
        str(assembly_path),
        "--deps",
        str(deps_json),
    ]
    if args.extra_dir:
        cmd.extend(["--extra-dir", args.extra_dir])
    if args.ci:
        cmd.append("--ci")
    if args.strict:
        cmd.append("--strict")
    if args.quiet:
        cmd.append("--quiet")

    import subprocess

    result = subprocess.run(
        cmd, cwd=str(project_root), capture_output=True, text=True
    )
    if not args.quiet and (result.stdout or result.stderr):
        print((result.stdout or "") + (result.stderr or ""))
    return result.returncode


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify Game API dependencies")
    parser.add_argument("--assembly", help="Path to Assembly-CSharp.dll")
    parser.add_argument("--managed-dir", help="Path to Elin_Data\\Managed")
    parser.add_argument("--game-dir", help="Path to game root (contains Elin_Data)")
    parser.add_argument("--ci", action="store_true", help="CI mode (exit codes)")
    parser.add_argument("--strict", action="store_true", help="Treat warnings as errors")
    parser.add_argument("--quiet", "-q", action="store_true", help="Only show errors")
    parser.add_argument("--plugin-dir", help="Path to BepInEx plugins dir (for CWL types)")
    parser.add_argument("--extra-dir", help="Additional directory to scan for assemblies")
    args = parser.parse_args()

    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent

    assembly_path = resolve_assembly_path(args)
    if assembly_path is None or not assembly_path.exists():
        if not args.quiet:
            print(
                "Game API verification skipped: Assembly-CSharp.dll not found.\n"
                "Set ELIN_GAME_DIR or ELIN_MANAGED_DIR, or pass --assembly."
            )
        return 0

    assembly_path = pick_best_game_assembly(assembly_path)

    deps = extract_dependencies(project_root)
    if not deps:
        if not args.quiet:
            print("Game API verification skipped: No dependencies found.")
        return 0

    if not args.extra_dir:
        plugin_dir = resolve_plugin_dir(args)
        if plugin_dir:
            args.extra_dir = str(plugin_dir)

    with tempfile.TemporaryDirectory() as tmpdir:
        deps_path = Path(tmpdir) / "game_api_deps.json"
        deps_path.write_text(json.dumps(deps, ensure_ascii=False, indent=2), encoding="utf-8")
        return run_inspector(project_root, assembly_path, deps_path, args)


if __name__ == "__main__":
    sys.exit(main())
