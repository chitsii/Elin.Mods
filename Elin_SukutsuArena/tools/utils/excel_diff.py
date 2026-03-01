"""
Excel Diff Tool - ExcelファイルをCSVに変換してDiff表示

Usage:
    python excel_diff.py file1.xlsx file2.xlsx
    python excel_diff.py file1.xlsx file2.xlsx --output diff.txt
    python excel_diff.py file1.xlsx file2.xlsx --context 5
    python excel_diff.py drama_folder1/ drama_folder2/  # フォルダ比較
"""

import argparse
import csv
import difflib
import io
import os
import sys
from pathlib import Path
from typing import List, Tuple, Optional

try:
    import openpyxl
except ImportError:
    print("Error: openpyxl is required. Install with: pip install openpyxl")
    sys.exit(1)


def excel_to_csv_lines(filepath: str, sheet_name: str = None) -> List[str]:
    """
    ExcelファイルをCSV形式の行リストに変換

    Args:
        filepath: Excelファイルパス
        sheet_name: シート名（省略時は最初のシート）

    Returns:
        CSV形式の行リスト
    """
    wb = openpyxl.load_workbook(filepath, read_only=True, data_only=True)

    if sheet_name:
        ws = wb[sheet_name]
    else:
        ws = wb.active

    output = io.StringIO()
    writer = csv.writer(output, quoting=csv.QUOTE_MINIMAL)

    for row in ws.iter_rows(values_only=True):
        # None を空文字に変換
        cleaned_row = [str(cell) if cell is not None else "" for cell in row]
        # 全て空の行はスキップ
        if any(cell for cell in cleaned_row):
            writer.writerow(cleaned_row)

    wb.close()

    output.seek(0)
    return output.read().splitlines()


def diff_excel_files(
    file1: str, file2: str, context: int = 3
) -> Tuple[List[str], bool]:
    """
    2つのExcelファイルのDiffを生成

    Args:
        file1: 比較元ファイル
        file2: 比較先ファイル
        context: コンテキスト行数

    Returns:
        (diff行リスト, 差分があるかどうか)
    """
    lines1 = excel_to_csv_lines(file1)
    lines2 = excel_to_csv_lines(file2)

    diff = list(
        difflib.unified_diff(
            lines1, lines2, fromfile=file1, tofile=file2, lineterm="", n=context
        )
    )

    has_diff = len(diff) > 0
    return diff, has_diff


def diff_excel_folders(
    folder1: str, folder2: str, context: int = 3
) -> List[Tuple[str, List[str], bool]]:
    """
    2つのフォルダ内のExcelファイルを比較

    Args:
        folder1: 比較元フォルダ
        folder2: 比較先フォルダ
        context: コンテキスト行数

    Returns:
        [(ファイル名, diff行リスト, 差分があるか), ...]
    """
    results = []

    # フォルダ1のファイル一覧
    files1 = {f.name: f for f in Path(folder1).glob("*.xlsx")}
    files2 = {f.name: f for f in Path(folder2).glob("*.xlsx")}

    all_files = sorted(set(files1.keys()) | set(files2.keys()))

    for filename in all_files:
        if filename not in files1:
            results.append((filename, [f"+++ NEW FILE: {filename}"], True))
        elif filename not in files2:
            results.append((filename, [f"--- DELETED FILE: {filename}"], True))
        else:
            diff, has_diff = diff_excel_files(
                str(files1[filename]), str(files2[filename]), context
            )
            results.append((filename, diff, has_diff))

    return results


def colorize_diff(lines: List[str]) -> List[str]:
    """
    Diff行に色を付ける（ターミナル用）
    """
    RESET = "\033[0m"
    RED = "\033[31m"
    GREEN = "\033[32m"
    CYAN = "\033[36m"
    YELLOW = "\033[33m"

    colored = []
    for line in lines:
        if line.startswith("+++") or line.startswith("---"):
            colored.append(f"{CYAN}{line}{RESET}")
        elif line.startswith("@@"):
            colored.append(f"{YELLOW}{line}{RESET}")
        elif line.startswith("+"):
            colored.append(f"{GREEN}{line}{RESET}")
        elif line.startswith("-"):
            colored.append(f"{RED}{line}{RESET}")
        else:
            colored.append(line)

    return colored


def main():
    parser = argparse.ArgumentParser(
        description="Excel Diff Tool - ExcelファイルをCSVに変換してDiff表示"
    )
    parser.add_argument("path1", help="比較元ファイル/フォルダ")
    parser.add_argument("path2", help="比較先ファイル/フォルダ")
    parser.add_argument("-o", "--output", help="出力ファイル（省略時は標準出力）")
    parser.add_argument(
        "-c", "--context", type=int, default=3, help="コンテキスト行数（デフォルト: 3）"
    )
    parser.add_argument("--no-color", action="store_true", help="色付けを無効化")
    parser.add_argument(
        "--only-changed", action="store_true", help="変更があるファイルのみ表示"
    )
    parser.add_argument("--summary", action="store_true", help="サマリーのみ表示")

    args = parser.parse_args()

    path1 = Path(args.path1)
    path2 = Path(args.path2)

    output_lines = []
    changed_count = 0
    total_count = 0

    # フォルダ比較かファイル比較か判定
    if path1.is_dir() and path2.is_dir():
        results = diff_excel_folders(str(path1), str(path2), args.context)

        for filename, diff, has_diff in results:
            total_count += 1
            if has_diff:
                changed_count += 1

            if args.only_changed and not has_diff:
                continue

            if not args.summary:
                output_lines.append(f"\n{'=' * 60}")
                output_lines.append(f"File: {filename}")
                output_lines.append("=" * 60)

                if has_diff:
                    output_lines.extend(diff)
                else:
                    output_lines.append("(no changes)")

        # サマリー
        output_lines.append(f"\n{'=' * 60}")
        output_lines.append(f"Summary: {changed_count}/{total_count} files changed")
        output_lines.append("=" * 60)

    elif path1.is_file() and path2.is_file():
        diff, has_diff = diff_excel_files(str(path1), str(path2), args.context)
        total_count = 1
        if has_diff:
            changed_count = 1
            output_lines.extend(diff)
        else:
            output_lines.append("No differences found.")
    else:
        print("Error: Both paths must be either files or directories")
        sys.exit(1)

    # 出力
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write("\n".join(output_lines))
        print(f"Diff written to: {args.output}")
        print(f"Changed: {changed_count}/{total_count} files")
    else:
        # 色付け（--no-colorでない場合）
        if not args.no_color and sys.stdout.isatty():
            output_lines = colorize_diff(output_lines)

        print("\n".join(output_lines))

    # 変更があった場合は exit code 1
    sys.exit(1 if changed_count > 0 else 0)


if __name__ == "__main__":
    main()
