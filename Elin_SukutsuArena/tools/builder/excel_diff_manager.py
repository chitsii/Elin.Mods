"""
excel_diff_manager.py - Build-time Excel Diff Management

Manages Excel file backups and shows content-level differences after builds.

Usage:
    # Before build: backup current Excel files (only if no backup exists)
    python excel_diff_manager.py backup

    # Force overwrite existing backup
    python excel_diff_manager.py backup --force

    # After build: show differences from backup
    python excel_diff_manager.py diff

    # After Debug build: show differences with Debug annotation
    python excel_diff_manager.py diff --build-mode debug

    # Clean up backup directory (allows next backup to be created)
    python excel_diff_manager.py clean

The backup is stored in tools/.excel_backup/ and preserved across builds
until explicitly cleaned or force-overwritten.

Note:
    Golden data should be from Release builds.
    When comparing with Debug builds, Debug-specific changes are annotated.
"""

import sys
import shutil
import argparse
from pathlib import Path
from datetime import datetime
from diff_excel import compare_excel_files, compare_directories, load_excel_data, normalize_value


# Debug-specific changes (Release -> Debug differences)
# These are expected changes when comparing Release golden data with Debug builds
DEBUG_SPECIFIC_CHANGES = {
    "SourceChara.xlsx": {
        "columns": ["LV"],  # LV column set to 1 in Debug
        "added_ids": ["sukutsu_debug_master"],  # Debug-only character
    },
    "SourceThing.xlsx": {
        "columns": ["value"],  # value column set to 1 in Debug
        "added_ids": [],
    },
}

# Paths relative to this script
SCRIPT_DIR = Path(__file__).parent
TOOLS_DIR = SCRIPT_DIR.parent
PROJECT_ROOT = TOOLS_DIR.parent
BACKUP_DIR = TOOLS_DIR / ".excel_backup"

# Excel directories to track (EN is the primary build target)
EXCEL_DIRS = [
    PROJECT_ROOT / "LangMod" / "EN" / "Dialog" / "Drama",
    PROJECT_ROOT / "LangMod" / "EN",
]


def get_all_excel_files() -> list[Path]:
    """Get all Excel files to track."""
    files = []
    for dir_path in EXCEL_DIRS:
        if dir_path.exists():
            # Get xlsx files directly in the directory
            if "Dialog" in str(dir_path):
                files.extend(dir_path.glob("*.xlsx"))
            else:
                # For LangMod/EN, only get Source*.xlsx
                files.extend(dir_path.glob("Source*.xlsx"))
    return sorted(files)


def analyze_debug_specific_changes(
    backup_path: Path, current_path: Path, filename: str
) -> tuple[list[str], list[str]]:
    """
    Analyze differences and separate Debug-specific changes from real changes.

    Returns:
        (debug_changes, other_changes): Lists of diff messages
    """
    if filename not in DEBUG_SPECIFIC_CHANGES:
        return [], []

    config = DEBUG_SPECIFIC_CHANGES[filename]
    debug_columns = set(config.get("columns", []))
    debug_added_ids = set(config.get("added_ids", []))

    try:
        data_backup, headers_backup = load_excel_data(backup_path)
        data_current, headers_current = load_excel_data(current_path)
    except Exception:
        return [], []

    debug_changes = []
    other_changes = []

    # Analyze each sheet
    for sheet_name in set(data_backup.keys()) & set(data_current.keys()):
        headers = headers_current.get(sheet_name, [])
        rows_backup = data_backup[sheet_name]
        rows_current = data_current[sheet_name]

        # Skip header rows (first 3 rows for Source files)
        start_row = 3 if filename.startswith("Source") else 0

        # Build id -> row mapping
        def get_id(row):
            return normalize_value(row[0]) if row else ""

        backup_by_id = {get_id(r): r for r in rows_backup[start_row:] if get_id(r)}
        current_by_id = {get_id(r): r for r in rows_current[start_row:] if get_id(r)}

        # Check for Debug-only added rows
        added_ids = set(current_by_id.keys()) - set(backup_by_id.keys())
        for aid in added_ids:
            if aid in debug_added_ids:
                debug_changes.append(f"    [{sheet_name}] Debug-only row added: id={aid}")
            else:
                other_changes.append(f"    [{sheet_name}] Row added: id={aid}")

        # Check for removed rows (shouldn't happen in Debug, but check anyway)
        removed_ids = set(backup_by_id.keys()) - set(current_by_id.keys())
        for rid in removed_ids:
            if rid in debug_added_ids:
                # This would be Release build removing Debug-only rows
                pass
            else:
                other_changes.append(f"    [{sheet_name}] Row removed: id={rid}")

        # Check for changed rows
        common_ids = set(backup_by_id.keys()) & set(current_by_id.keys())
        for cid in common_ids:
            row_backup = backup_by_id[cid]
            row_current = current_by_id[cid]

            # Find changed columns
            changed_cols = []
            for col_idx in range(max(len(row_backup), len(row_current))):
                val_b = normalize_value(row_backup[col_idx] if col_idx < len(row_backup) else None)
                val_c = normalize_value(row_current[col_idx] if col_idx < len(row_current) else None)
                if val_b != val_c:
                    col_name = headers[col_idx] if col_idx < len(headers) else f"Col{col_idx+1}"
                    changed_cols.append(col_name)

            if changed_cols:
                # Check if all changes are Debug-specific
                if all(col in debug_columns for col in changed_cols):
                    debug_changes.append(
                        f"    [{sheet_name}] id={cid}: {', '.join(changed_cols)} (Debug override)"
                    )
                else:
                    non_debug_cols = [c for c in changed_cols if c not in debug_columns]
                    debug_cols = [c for c in changed_cols if c in debug_columns]
                    if non_debug_cols:
                        other_changes.append(
                            f"    [{sheet_name}] id={cid}: {', '.join(non_debug_cols)}"
                        )
                    if debug_cols:
                        debug_changes.append(
                            f"    [{sheet_name}] id={cid}: {', '.join(debug_cols)} (Debug override)"
                        )

    return debug_changes, other_changes


def backup_excel_files(force: bool = False) -> int:
    """Backup all Excel files before build.

    Args:
        force: If True, overwrite existing backup. If False, skip if backup exists.
    """
    # Check if backup already exists
    if BACKUP_DIR.exists() and not force:
        backup_files = list(BACKUP_DIR.rglob("*.xlsx"))
        if backup_files:
            print(
                f"[Excel Diff] Backup exists ({len(backup_files)} files). Use --force to overwrite."
            )
            return len(backup_files)

    # Clean and recreate backup directory
    if BACKUP_DIR.exists():
        shutil.rmtree(BACKUP_DIR)
    BACKUP_DIR.mkdir(parents=True)

    files = get_all_excel_files()
    if not files:
        print("[Excel Diff] No Excel files found to backup")
        return 0

    backed_up = 0
    for file_path in files:
        # Create relative path structure in backup
        rel_path = file_path.relative_to(PROJECT_ROOT)
        backup_path = BACKUP_DIR / rel_path
        backup_path.parent.mkdir(parents=True, exist_ok=True)

        shutil.copy2(file_path, backup_path)
        backed_up += 1

    print(f"[Excel Diff] Backed up {backed_up} files")
    return backed_up


def show_diff(build_mode: str = "release") -> int:
    """Show differences between backup and current files.

    Args:
        build_mode: "debug" or "release". If "debug", Debug-specific changes are shown separately.
    """
    if not BACKUP_DIR.exists():
        print("[Excel Diff] No backup found. Run 'backup' first.")
        return 2

    # Get backup files
    backup_files = list(BACKUP_DIR.rglob("*.xlsx"))
    if not backup_files:
        print("[Excel Diff] Backup is empty")
        return 2

    is_debug = build_mode.lower() == "debug"

    total_files = 0
    changed_files = 0
    new_files = 0
    debug_only_changes = []  # Files with only Debug-specific changes

    # Compare each backed up file with current
    for backup_path in sorted(backup_files):
        rel_path = backup_path.relative_to(BACKUP_DIR)
        current_path = PROJECT_ROOT / rel_path
        filename = rel_path.name

        total_files += 1

        if not current_path.exists():
            print(f"[DELETED] {rel_path}")
            changed_files += 1
            continue

        identical, diffs = compare_excel_files(backup_path, current_path)

        if not identical:
            # In Debug mode, analyze Debug-specific changes for Source files
            if is_debug and filename in DEBUG_SPECIFIC_CHANGES:
                debug_changes, other_changes = analyze_debug_specific_changes(
                    backup_path, current_path, filename
                )

                if other_changes:
                    # Has real changes beyond Debug-specific
                    changed_files += 1
                    print(f"[CHANGED] {rel_path}")
                    for change in other_changes[:5]:
                        print(f"  {change}")
                    if len(other_changes) > 5:
                        print(f"    ... and {len(other_changes) - 5} more changes")

                if debug_changes:
                    # Collect Debug-specific changes for summary
                    debug_only_changes.append((rel_path, debug_changes))
                    if not other_changes:
                        # Only Debug-specific changes, not counted as "changed"
                        pass
            else:
                changed_files += 1
                print(f"[CHANGED] {rel_path}")
                # Show first few differences
                for diff in diffs[:5]:
                    print(f"  {diff}")
                if len(diffs) > 5:
                    print(f"  ... and {len(diffs) - 5} more changes")

    # Check for new files
    current_files = get_all_excel_files()
    for current_path in current_files:
        rel_path = current_path.relative_to(PROJECT_ROOT)
        backup_path = BACKUP_DIR / rel_path
        if not backup_path.exists():
            new_files += 1
            print(f"[NEW] {rel_path}")

    # Show Debug-specific changes summary
    if is_debug and debug_only_changes:
        print()
        print("--- Debug Build Differences (expected, not errors) ---")
        for rel_path, changes in debug_only_changes:
            print(f"[DEBUG] {rel_path}")
            for change in changes[:3]:
                print(f"  {change}")
            if len(changes) > 3:
                print(f"    ... and {len(changes) - 3} more Debug overrides")

    # Summary
    print()
    if changed_files == 0 and new_files == 0:
        if debug_only_changes:
            print(f"[Excel Diff] All {total_files} files unchanged (Debug overrides only)")
        else:
            print(f"[Excel Diff] All {total_files} files unchanged")
        return 0
    else:
        print(
            f"[Excel Diff] {changed_files} changed, {new_files} new (of {total_files} tracked)"
        )
        return 1


def clean_backup() -> None:
    """Remove backup directory."""
    if BACKUP_DIR.exists():
        shutil.rmtree(BACKUP_DIR)
        print("[Excel Diff] Backup cleaned")
    else:
        print("[Excel Diff] No backup to clean")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    parser = argparse.ArgumentParser(
        description="Manage Excel file backups and show build differences"
    )
    parser.add_argument(
        "command",
        choices=["backup", "diff", "clean"],
        help="backup: save current files, diff: show changes, clean: remove backup",
    )
    parser.add_argument(
        "--force", "-f", action="store_true", help="Force overwrite existing backup"
    )
    parser.add_argument(
        "--build-mode", "-m",
        choices=["debug", "release"],
        default="release",
        help="Build mode (affects diff display). Debug-specific changes are shown separately in debug mode."
    )

    args = parser.parse_args()

    if args.command == "backup":
        backup_excel_files(force=args.force)
        sys.exit(0)
    elif args.command == "diff":
        result = show_diff(build_mode=args.build_mode)
        sys.exit(result)
    elif args.command == "clean":
        clean_backup()
        sys.exit(0)


if __name__ == "__main__":
    main()
