"""
依存破損を検知するスクリプト
- Harmonyパッチ対象の存在確認（可能な範囲で）
- アノテーション整合性チェック

Usage:
    cd tools && uv run python builder/verify_dependencies.py
    cd tools && uv run python builder/verify_dependencies.py --ci
    cd tools && uv run python builder/verify_dependencies.py --quiet
"""

import re
import sys
import argparse
from pathlib import Path
from dataclasses import dataclass
from typing import List, Tuple


@dataclass
class ValidationIssue:
    """検証で発見された問題"""

    severity: str  # "error", "warning", "info"
    file: str
    line: int
    message: str


def check_harmony_patches(src_dir: Path) -> List[ValidationIssue]:
    """HarmonyPatchアノテーションの整合性チェック"""
    issues = []

    # HarmonyPatch属性のパターン
    patch_pattern = re.compile(
        r'\[HarmonyPatch\s*\(\s*typeof\s*\(\s*(\w+)\s*\)\s*,\s*(?:nameof\s*\(\s*\w+\.(\w+)\s*\)|"([^"]+)")',
        re.MULTILINE,
    )

    # GameDependency("Patch", ...) のパターン
    dep_pattern = re.compile(r'\[GameDependency\s*\(\s*"Patch"')

    for cs_file in src_dir.rglob("*.cs"):
        try:
            content = cs_file.read_text(encoding="utf-8-sig")
        except Exception:
            continue

        lines = content.split("\n")

        # HarmonyPatch属性を持つがGameDependencyがないケースをチェック
        has_harmony = bool(patch_pattern.search(content))
        has_dep_annotation = bool(dep_pattern.search(content))

        if has_harmony and not has_dep_annotation:
            issues.append(
                ValidationIssue(
                    severity="warning",
                    file=str(cs_file.relative_to(src_dir.parent)),
                    line=0,
                    message='HarmonyPatch found but no [GameDependency("Patch", ...)] annotation',
                )
            )

    return issues


def check_reflection_usage(src_dir: Path) -> List[ValidationIssue]:
    """リフレクション使用箇所のチェック"""
    issues = []

    # 危険なリフレクションパターン
    reflection_patterns = [
        (r'AccessTools\.TypeByName\s*\(\s*"([^"]+)"', "AccessTools.TypeByName"),
        (r'Type\.GetType\s*\(\s*"([^"]+)"', "Type.GetType"),
        (r'GetMethod\s*\(\s*"([^"]+)"', "GetMethod"),
        (r'GetProperty\s*\(\s*"([^"]+)"', "GetProperty"),
        (r'GetField\s*\(\s*"([^"]+)"', "GetField"),
    ]

    dep_pattern = re.compile(r'\[GameDependency\s*\(\s*"Reflection"')

    for cs_file in src_dir.rglob("*.cs"):
        try:
            content = cs_file.read_text(encoding="utf-8-sig")
        except Exception:
            continue

        lines = content.split("\n")

        has_dep_annotation = bool(dep_pattern.search(content))

        for i, line in enumerate(lines, 1):
            for pattern, api_name in reflection_patterns:
                if re.search(pattern, line):
                    # この行または近くにGameDependencyがあるかチェック
                    context_start = max(0, i - 10)
                    context = "\n".join(lines[context_start:i])

                    if not dep_pattern.search(context) and not has_dep_annotation:
                        issues.append(
                            ValidationIssue(
                                severity="warning",
                                file=str(cs_file.relative_to(src_dir.parent)),
                                line=i,
                                message=f'{api_name} usage without [GameDependency("Reflection", ...)] annotation',
                            )
                        )
                    break  # 1行に複数マッチは1回だけ報告

    return issues


def check_inheritance(src_dir: Path) -> List[ValidationIssue]:
    """継承関係のアノテーションチェック"""
    issues = []

    # ゲームクラスを継承しているパターン
    game_base_classes = [
        "Zone",
        "Zone_Civilized",
        "ZoneInstance",
        "ZoneEvent",
        "ZonePreEnterEvent",
        "Condition",
        "Trait",
        "TraitChara",
        "TraitUniqueChara",
        "TraitMerchant",
    ]

    inheritance_pattern = re.compile(
        r"class\s+(\w+)\s*:\s*(" + "|".join(game_base_classes) + r")\b"
    )
    dep_pattern = re.compile(r'\[GameDependency\s*\(\s*"Inheritance"')

    for cs_file in src_dir.rglob("*.cs"):
        try:
            content = cs_file.read_text(encoding="utf-8-sig")
        except Exception:
            continue

        for match in inheritance_pattern.finditer(content):
            class_name = match.group(1)
            base_class = match.group(2)

            # クラス定義の前後にGameDependencyがあるかチェック
            match_pos = match.start()
            context_start = max(0, match_pos - 500)
            context = content[context_start:match_pos]

            if not dep_pattern.search(context):
                line_num = content[:match_pos].count("\n") + 1
                issues.append(
                    ValidationIssue(
                        severity="warning",
                        file=str(cs_file.relative_to(src_dir.parent)),
                        line=line_num,
                        message=f'Class {class_name} inherits from {base_class} without [GameDependency("Inheritance", ...)]',
                    )
                )

    return issues


def verify_dependencies(src_dir: Path) -> Tuple[List[ValidationIssue], bool]:
    """
    全ての検証を実行

    Args:
        src_dir: ソースディレクトリ

    Returns:
        (issues, has_errors): 問題リストとエラーの有無
    """
    all_issues = []

    all_issues.extend(check_harmony_patches(src_dir))
    all_issues.extend(check_reflection_usage(src_dir))
    all_issues.extend(check_inheritance(src_dir))

    has_errors = any(i.severity == "error" for i in all_issues)
    return all_issues, has_errors


def main() -> int:
    """
    メイン関数

    Returns:
        終了コード (0: OK, 1: 警告あり, 2: エラー)
    """
    parser = argparse.ArgumentParser(
        description="Verify dependency annotations",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    uv run python builder/verify_dependencies.py          # 通常実行
    uv run python builder/verify_dependencies.py --ci     # CI mode (exit codes)
    uv run python builder/verify_dependencies.py --quiet  # エラーのみ表示

Exit codes (with --ci):
    0: No issues found
    1: Warnings found (annotation missing, etc.)
    2: Errors found (reserved for future use)
""",
    )
    parser.add_argument("--src", default="src", help="Source directory (default: src)")
    parser.add_argument(
        "--ci",
        action="store_true",
        help="CI mode (exit codes: 0=ok, 1=warnings, 2=errors)",
    )
    parser.add_argument("--quiet", "-q", action="store_true", help="Only show errors")
    args = parser.parse_args()

    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent
    src_dir = project_root / args.src

    if not src_dir.exists():
        print(f"Error: Source directory not found: {src_dir}")
        return 2

    issues, has_errors = verify_dependencies(src_dir)

    # 結果表示
    if not args.quiet or has_errors:
        errors = [i for i in issues if i.severity == "error"]
        warnings = [i for i in issues if i.severity == "warning"]

        if errors:
            print(f"\n=== ERRORS ({len(errors)}) ===")
            for issue in errors:
                print(f"  [{issue.severity.upper()}] {issue.file}:{issue.line}")
                print(f"    {issue.message}")

        if warnings and not args.quiet:
            print(f"\n=== WARNINGS ({len(warnings)}) ===")
            for issue in warnings:
                print(f"  [{issue.severity.upper()}] {issue.file}:{issue.line}")
                print(f"    {issue.message}")

        if not issues:
            print("Dependency verification passed: No issues found")

    # Summary line
    if issues:
        error_count = len([i for i in issues if i.severity == "error"])
        warning_count = len([i for i in issues if i.severity == "warning"])
        print(f"\nSummary: {error_count} error(s), {warning_count} warning(s)")

    # CI mode: exit code
    if args.ci:
        if has_errors:
            return 2
        elif issues:
            return 1
        return 0

    return 0 if not has_errors else 1


if __name__ == "__main__":
    sys.exit(main())
