# -*- coding: utf-8 -*-
"""
GameDependencyAttribute extractor - Extract dependencies from C# source.

Scans C# source files for [GameDependency] annotations and generates
DEPENDENCIES.md with risk-level grouped dependency documentation.

Usage:
    cd tools && uv run python builder/extract_dependencies.py
    cd tools && uv run python builder/extract_dependencies.py -o .planning/DEPENDENCIES.md
    cd tools && uv run python builder/extract_dependencies.py --quiet
"""

import re
import sys
import argparse
from pathlib import Path
from dataclasses import dataclass
from typing import List, Dict


@dataclass
class Dependency:
    """Represents a single GameDependency annotation found in source code."""

    file: str
    line: int
    dep_type: str  # Inheritance, Direct, Patch, Reflection
    target: str  # Target API name
    risk: str  # High, Medium, Low
    notes: str  # Optional notes
    context: str  # Class/method name containing the annotation


# Pattern for C# attribute with positional/named parameters
# Matches: [GameDependency("Type", "Target", "Risk", "Notes")]
# or:      [GameDependency("Type", "Target", risk: "Risk", notes: "Notes")]
ATTR_PATTERN = re.compile(
    r'\[GameDependency\s*\(\s*"(\w+)"\s*,\s*"([^"]+)"'
    r'(?:\s*,\s*"(\w+)")?'  # risk (optional positional 3rd param)
    r'(?:\s*,\s*"([^"]*)")?'  # notes (optional positional 4th param)
    r'(?:\s*,\s*risk\s*:\s*"(\w+)")?'  # risk (optional named param)
    r'(?:\s*,\s*notes\s*:\s*"([^"]*)")?'  # notes (optional named param)
    r"\s*\)\]",
    re.MULTILINE,
)


def find_context(lines: List[str], attr_line: int) -> str:
    """
    Find the class/method name following the attribute declaration.

    Args:
        lines: All lines from the file
        attr_line: 0-indexed line number where attribute was found

    Returns:
        Context string like "class Zone_SukutsuArena" or "method OnActivate"
    """
    for i in range(attr_line, min(attr_line + 10, len(lines))):
        line = lines[i].strip()

        # Skip empty lines, comments, other attributes
        if not line or line.startswith("//") or line.startswith("["):
            continue

        # Class/struct definition
        match = re.search(
            r"(?:public|internal|private|protected)?\s*"
            r"(?:sealed\s+)?(?:partial\s+)?"
            r"(?:class|struct)\s+(\w+)",
            line,
        )
        if match:
            return f"class {match.group(1)}"

        # Method definition
        match = re.search(
            r"(?:public|private|protected|internal|static|override|virtual|async)\s+"
            r"[\w<>\[\],\s]+\s+(\w+)\s*\(",
            line,
        )
        if match:
            return f"method {match.group(1)}"

        # Property definition
        match = re.search(
            r"(?:public|private|protected|internal|static|override|virtual)\s+"
            r"[\w<>\[\],\s]+\s+(\w+)\s*(?:\{|=>)",
            line,
        )
        if match:
            return f"property {match.group(1)}"

    return "unknown"


def extract_from_file(filepath: Path, project_root: Path) -> List[Dependency]:
    """
    Extract all GameDependency annotations from a single C# file.

    Args:
        filepath: Path to the C# file
        project_root: Project root for relative path calculation

    Returns:
        List of Dependency objects found in the file
    """
    try:
        content = filepath.read_text(encoding="utf-8-sig")
    except Exception as e:
        print(f"Warning: Could not read {filepath}: {e}", file=sys.stderr)
        return []

    lines = content.split("\n")
    deps = []

    for i, line in enumerate(lines):
        for match in ATTR_PATTERN.finditer(line):
            # Get risk: check positional first, then named param
            risk = match.group(3) or match.group(5) or "Medium"
            # Get notes: check positional first, then named param
            notes = match.group(4) or match.group(6) or ""

            deps.append(
                Dependency(
                    file=str(filepath.relative_to(project_root)),
                    line=i + 1,
                    dep_type=match.group(1),
                    target=match.group(2),
                    risk=risk,
                    notes=notes,
                    context=find_context(lines, i),
                )
            )

    return deps


def generate_markdown(deps: List[Dependency]) -> str:
    """
    Generate DEPENDENCIES.md content from extracted dependencies.

    Args:
        deps: List of all dependencies found

    Returns:
        Markdown content as string
    """
    # Group by risk level
    by_risk: Dict[str, List[Dependency]] = {"High": [], "Medium": [], "Low": []}
    for d in deps:
        if d.risk in by_risk:
            by_risk[d.risk].append(d)
        else:
            by_risk.setdefault(d.risk, []).append(d)

    lines = [
        "# Dependency Map",
        "",
        "> Auto-generated from [GameDependency] annotations in source code.",
        "> Do not edit manually - run `build.bat` to regenerate.",
        "",
        f"**Total dependencies:** {len(deps)}",
        f"**High risk:** {len(by_risk['High'])} | **Medium risk:** {len(by_risk['Medium'])} | **Low risk:** {len(by_risk['Low'])}",
        "",
    ]

    for risk_level in ["High", "Medium", "Low"]:
        risk_deps = by_risk.get(risk_level, [])
        if not risk_deps:
            continue

        lines.append(f"## {risk_level} Risk")
        lines.append("")

        # Sub-group by type
        by_type: Dict[str, List[Dependency]] = {}
        for d in risk_deps:
            by_type.setdefault(d.dep_type, []).append(d)

        for dep_type in ["Inheritance", "Patch", "Reflection", "Direct"]:
            type_deps = by_type.get(dep_type, [])
            if not type_deps:
                continue

            lines.append(f"### {dep_type}")
            lines.append("")
            lines.append("| Target | File | Line | Context | Notes |")
            lines.append("|--------|------|------|---------|-------|")

            for d in sorted(type_deps, key=lambda x: x.target):
                notes = d.notes if d.notes else "-"
                # Escape pipe characters in notes
                notes = notes.replace("|", "\\|")
                file_path = d.file.replace("\\", "/")
                lines.append(
                    f"| `{d.target}` | `{file_path}` | {d.line} | {d.context} | {notes} |"
                )

            lines.append("")

    # Repair templates section
    lines.extend(
        [
            "---",
            "",
            "## Repair Templates",
            "",
            "### Inheritance Breakage",
            '1. Locate class with `[GameDependency("Inheritance", ...)]`',
            "2. Check Elin-Decompiled for new base class signature",
            "3. Update overridden members to match new signatures",
            "",
            "### Harmony Patch Breakage",
            '1. Locate patches with `[GameDependency("Patch", ...)]`',
            "2. Check Elin-Decompiled for new method signature",
            "3. Update HarmonyPatch attribute parameters",
            "4. Update Prefix/Postfix parameter types to match",
            "",
            "### Reflection Breakage",
            '1. Locate code with `[GameDependency("Reflection", ...)]`',
            "2. Search Elin-Decompiled for the type/member",
            "3. If renamed: Update string parameters",
            "4. If removed: Implement alternative or remove feature",
            "",
            "### Direct API Breakage",
            '1. Locate code with `[GameDependency("Direct", ...)]`',
            "2. Search Elin-Decompiled for the API signature",
            "3. Update method calls to match new signature",
            "4. Check for new required parameters or return type changes",
            "",
        ]
    )

    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(
        description="Extract GameDependency annotations from C# source"
    )
    parser.add_argument(
        "--output",
        "-o",
        default=".planning/DEPENDENCIES.md",
        help="Output file path (default: .planning/DEPENDENCIES.md)",
    )
    parser.add_argument(
        "--src", default="src", help="Source directory to scan (default: src)"
    )
    parser.add_argument(
        "--quiet", "-q", action="store_true", help="Suppress output except errors"
    )
    args = parser.parse_args()

    # Calculate project root (tools/builder/ -> project root)
    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent

    src_dir = project_root / args.src
    output_path = project_root / args.output

    if not src_dir.exists():
        print(f"Error: Source directory not found: {src_dir}", file=sys.stderr)
        return 1

    # Scan all C# files
    all_deps = []
    for cs_file in src_dir.rglob("*.cs"):
        deps = extract_from_file(cs_file, project_root)
        all_deps.extend(deps)

    if not all_deps:
        if not args.quiet:
            print("Warning: No [GameDependency] annotations found")
        # Generate empty file to record the state
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(
            "# Dependency Map\n\nNo dependencies annotated yet.\n", encoding="utf-8"
        )
        return 0

    # Generate markdown
    md_content = generate_markdown(all_deps)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(md_content, encoding="utf-8")

    if not args.quiet:
        print(f"Extracted {len(all_deps)} dependencies to {output_path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
