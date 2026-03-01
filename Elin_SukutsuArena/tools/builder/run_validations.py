# -*- coding: utf-8 -*-
"""
run_validations.py - ビルド時バリデーションの一括実行

全てのバリデーションを実行し、結果をまとめて報告する。

Usage:
    # 全バリデーション実行（サイレントモード）
    uv run python builder/run_validations.py

    # 詳細表示
    uv run python builder/run_validations.py -v

Exit codes:
    0 - 全てパス
    1 - バリデーションエラー
"""

import argparse
import sys
import os

# パス設定
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

# toolsディレクトリをパスに追加
sys.path.insert(0, TOOLS_DIR)


def run_scenario_validation(verbose: bool = False) -> tuple[bool, str]:
    """シナリオバリデーション"""
    try:
        from arena.validation import run_all_validations
        result = run_all_validations()
        return result, "Scenario validation"
    except Exception as e:
        if verbose:
            print(f"  Error: {e}")
        return False, "Scenario validation"


def run_source_excel_validation(verbose: bool = False) -> tuple[bool, str]:
    """Source Excelヘッダー検証（バニラとの一致確認）"""
    try:
        from builder.extract_source_defaults import cmd_compare
        result = cmd_compare(quiet=not verbose)
        return result, "Source Excel headers"
    except Exception as e:
        if verbose:
            print(f"  Error: {e}")
        return False, "Source Excel headers"


def run_flag_validation(verbose: bool = False) -> tuple[bool, str]:
    """フラグバリデーション"""
    try:
        import subprocess
        result = subprocess.run(
            ["uv", "run", "python", "builder/validate_scenario_flags.py"],
            cwd=TOOLS_DIR,
            capture_output=True,
            text=True,
        )
        if result.returncode != 0 and verbose:
            print(result.stdout)
            print(result.stderr)
        return result.returncode == 0, "Scenario flags"
    except Exception as e:
        if verbose:
            print(f"  Error: {e}")
        return False, "Scenario flags"


def run_contract_validation(verbose: bool = False) -> tuple[bool, str]:
    """契約バリデーション（JSON）"""
    try:
        from cwl_quest_lib.contracts import validate_quest_json
        quest_json = os.path.join(PROJECT_ROOT, "Package", "quest_definitions.json")
        # validate_quest_jsonは例外を投げる可能性がある
        validate_quest_json(quest_json)
        return True, "Quest contract (JSON)"
    except Exception as e:
        if verbose:
            print(f"  Error: {e}")
        return False, "Quest contract (JSON)"


def run_csharp_type_validation(verbose: bool = False) -> tuple[bool, str]:
    """C#型バリデーション"""
    try:
        from cwl_quest_lib.contracts import validate_quest_data_class
        mismatches = validate_quest_data_class(PROJECT_ROOT)
        if mismatches and verbose:
            print(f"  Mismatches: {mismatches}")
        return not mismatches, "C# type check"
    except Exception as e:
        if verbose:
            print(f"  Error: {e}")
        return False, "C# type check"


def main():
    parser = argparse.ArgumentParser(description="ビルド時バリデーションの一括実行")
    parser.add_argument(
        "-v", "--verbose",
        action="store_true",
        help="詳細表示",
    )
    parser.add_argument(
        "--skip-source-excel",
        action="store_true",
        help="Source Excelヘッダー検証をスキップ",
    )
    args = parser.parse_args()

    validations = [
        ("Scenario", run_scenario_validation),
        ("Flags", run_flag_validation),
    ]

    if not args.skip_source_excel:
        validations.append(("Source Excel", run_source_excel_validation))

    # 契約・型チェックはビルド後に実行されるのでここでは含めない
    # validations.append(("Contract", run_contract_validation))
    # validations.append(("C# Types", run_csharp_type_validation))

    all_passed = True
    results = []

    for name, func in validations:
        passed, label = func(verbose=args.verbose)
        status = "OK" if passed else "FAILED"
        results.append((label, status))
        if not passed:
            all_passed = False

    # サマリー表示
    if args.verbose or not all_passed:
        print("\n=== Validation Results ===")
        for label, status in results:
            print(f"  {label}: {status}")

    sys.exit(0 if all_passed else 1)


if __name__ == "__main__":
    main()
