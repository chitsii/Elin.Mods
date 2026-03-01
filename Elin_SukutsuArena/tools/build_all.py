#!/usr/bin/env python3
"""
Unified Build Script for Sukutsu Arena Mod
===========================================

このスクリプトは以下を順番に実行します:
1. バリデーション（Enum同期、クエスト定義）
2. クエストJSONの生成
3. ドラマExcelの生成
4. C#コード生成

エラーがあればビルドを中止します。
"""

import os
import sys
import subprocess
from pathlib import Path

# Path setup
SCRIPT_DIR = Path(__file__).parent.absolute()
COMMON_DIR = SCRIPT_DIR / "common"
BUILDER_DIR = SCRIPT_DIR / "builder"
PROJECT_ROOT = SCRIPT_DIR.parent

sys.path.append(str(SCRIPT_DIR))
sys.path.append(str(COMMON_DIR))


def run_step(name: str, func) -> bool:
    """ビルドステップを実行"""
    print(f"\n{'=' * 60}")
    print(f"[STEP] {name}")
    print("=" * 60)
    try:
        result = func()
        if result is False:
            print(f"\n[FAILED] {name}")
            return False
        print(f"\n[OK] {name}")
        return True
    except Exception as e:
        print(f"\n[ERROR] {name}: {e}")
        import traceback

        traceback.print_exc()
        return False


def step_validation():
    """バリデーションを実行"""
    from validation import run_all_validations

    return run_all_validations()


def step_generate_quest_json():
    """クエストJSONを生成"""
    from quest_dependencies import export_quests_to_json

    output_path = PROJECT_ROOT / "Package" / "quest_definitions.json"
    output_path.parent.mkdir(parents=True, exist_ok=True)
    export_quests_to_json(str(output_path))
    print(f"Generated: {output_path}")
    return True


def step_generate_drama_excel():
    """ドラマExcelを生成"""
    # builderディレクトリのスクリプトを実行
    script_path = BUILDER_DIR / "create_drama_excel.py"
    result = subprocess.run(
        [sys.executable, str(script_path)],
        cwd=str(BUILDER_DIR),
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.stderr:
        print(result.stderr)
    return result.returncode == 0


def step_generate_csharp():
    """C#コードを生成"""
    script_path = BUILDER_DIR / "generate_quest_data.py"
    result = subprocess.run(
        [sys.executable, str(script_path)],
        cwd=str(BUILDER_DIR),
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.stderr:
        print(result.stderr)
    return result.returncode == 0


def step_generate_stat_excel():
    """SourceStat.xlsx を生成（カスタムCondition用）"""
    script_path = BUILDER_DIR / "create_stat_excel.py"
    result = subprocess.run(
        [sys.executable, str(script_path)],
        cwd=str(BUILDER_DIR),
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.stderr:
        print(result.stderr)
    return result.returncode == 0


def step_generate_source_xlsx():
    """SourceSukutsu.xlsx を生成（Zone + Element シート）"""
    script_path = BUILDER_DIR / "create_source_xlsx.py"
    result = subprocess.run(
        [sys.executable, str(script_path)],
        cwd=str(BUILDER_DIR),
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.stderr:
        print(result.stderr)
    return result.returncode == 0


def step_generate_thing_tsv():
    """Thing.tsv を生成（カスタムアイテム定義）"""
    script_path = BUILDER_DIR / "create_thing_excel.py"
    result = subprocess.run(
        [sys.executable, str(script_path)],
        cwd=str(BUILDER_DIR),
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.stderr:
        print(result.stderr)
    return result.returncode == 0


def step_generate_merchant_stock():
    """商人在庫JSONを生成"""
    script_path = BUILDER_DIR / "create_merchant_stock.py"
    result = subprocess.run(
        [sys.executable, str(script_path)],
        cwd=str(BUILDER_DIR),
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.stderr:
        print(result.stderr)
    return result.returncode == 0


def main():
    print("=" * 60)
    print("Sukutsu Arena Mod - Build System")
    print("=" * 60)

    steps = [
        ("Validation", step_validation),
        ("Generate Source XLSX", step_generate_source_xlsx),
        ("Generate Thing TSV", step_generate_thing_tsv),
        ("Generate Merchant Stock", step_generate_merchant_stock),
        ("Generate Quest JSON", step_generate_quest_json),
        ("Generate Drama Excel", step_generate_drama_excel),
        ("Generate Stat Excel", step_generate_stat_excel),
        ("Generate C# Code", step_generate_csharp),
    ]

    failed = False
    for name, func in steps:
        if not run_step(name, func):
            failed = True
            print("\n" + "!" * 60)
            print(f"BUILD FAILED at step: {name}")
            print("!" * 60)
            break

    if not failed:
        print("\n" + "=" * 60)
        print("BUILD SUCCESSFUL")
        print("=" * 60)
        return 0
    else:
        return 1


if __name__ == "__main__":
    sys.exit(main())
