"""Validate generated/deployed drama evals before publishing a package."""
import argparse
import hashlib
from pathlib import Path
import re
import openpyxl

OPENING = Path("LangMod/EN/Dialog/Drama/drama_sukutsu_opening.xlsx")
OLD_LOADER = re.compile(r"[\"']\s*\.\s*LoadSprite\s*\(")


def drama_files(root):
    return {path.relative_to(root): path for path in
            (root / "LangMod").glob("**/Dialog/Drama/*.xlsx")}


def validate_package(root, reference_root=None):
    """Return errors without modifying either package directory."""
    root = Path(root)
    files = drama_files(root)
    errors = []
    if OPENING not in files:
        errors.append(f"Missing opening workbook: {OPENING.as_posix()}")
    opening_backgrounds = 0
    for relative, path in sorted(files.items()):
        workbook = None
        try:
            workbook = openpyxl.load_workbook(path, read_only=True, data_only=True)
            for sheet in workbook:
                rows = sheet.iter_rows(values_only=True)
                headers = tuple(next(rows, ()))
                if "action" not in headers or "param" not in headers:
                    continue
                action, param = headers.index("action"), headers.index("param")
                for row_number, row in enumerate(rows, 2):
                    if len(row) <= max(action, param) or row[action] != "eval":
                        continue
                    code = str(row[param] or "")
                    location = f"{relative.as_posix()}:{sheet.title}:row {row_number}"
                    if OLD_LOADER.search(code):
                        errors.append(f"{location}: obsolete string.LoadSprite call")
                    if "dm.imageBG.sprite" in code and "LoadSprite" in code:
                        if not re.search(r"\bModUtil\s*\.\s*LoadSprite\s*\(", code):
                            errors.append(f"{location}: background must use ModUtil.LoadSprite")
                        elif relative == OPENING:
                            opening_backgrounds += 1
        except Exception as ex:
            errors.append(f"Cannot read {relative.as_posix()}: {ex}")
        finally:
            if workbook is not None:
                workbook.close()
    if OPENING in files and opening_backgrounds == 0:
        errors.append("Opening has no native ModUtil.LoadSprite background eval")
    if reference_root is not None:
        reference = drama_files(Path(reference_root))
        for relative in sorted(reference.keys() - files.keys()):
            errors.append(f"Missing copied drama: {relative.as_posix()}")
        for relative in sorted(files.keys() - reference.keys()):
            errors.append(f"Unexpected stale drama: {relative.as_posix()}")
        for relative in sorted(files.keys() & reference.keys()):
            actual = hashlib.sha256(files[relative].read_bytes()).digest()
            expected = hashlib.sha256(reference[relative].read_bytes()).digest()
            if actual != expected:
                errors.append(f"Copied drama SHA256 differs: {relative.as_posix()}")
    return errors


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package-root", type=Path, required=True)
    parser.add_argument("--reference-root", type=Path)
    args = parser.parse_args(argv)
    errors = validate_package(args.package_root, args.reference_root)
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        return 1
    print(f"Drama package verified: {len(drama_files(args.package_root))} workbooks"
          + ("; copied file inventory and SHA256 match" if args.reference_root else ""))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
