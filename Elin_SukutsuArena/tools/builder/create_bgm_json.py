"""
BGM JSON生成スクリプト

Sound/BGM/*.ogg からJSONメタデータを生成する。
"""

import json
import os
import sys
from pathlib import Path

# パス設定
BUILDER_DIR = Path(__file__).parent
TOOLS_DIR = BUILDER_DIR.parent
PROJECT_ROOT = TOOLS_DIR.parent

# arena パッケージをパスに追加
sys.path.insert(0, str(TOOLS_DIR))

from arena.data import BGM_BASE_ID, create_bgm_json_data
from arena.data.items import BGM_ID_ORDER, BGM_ID_MAP

# BGMフォルダ
BGM_DIR = PROJECT_ROOT / "Sound" / "BGM"


def get_bgm_files() -> list[str]:
    """Sound/BGM/*.ogg のファイル名リスト（拡張子なし）を取得"""
    if not BGM_DIR.exists():
        return []

    return [f.stem for f in BGM_DIR.glob("*.ogg")]


def generate_bgm_json():
    """BGM JSONファイルを生成"""
    bgm_files = get_bgm_files()

    if not bgm_files:
        print("No BGM files found in Sound/BGM/")
        return

    print(f"Generating BGM JSON files...")
    print(f"  Base ID: {BGM_BASE_ID}")
    print(f"  Found {len(bgm_files)} BGM files")

    bgm_set = set(bgm_files)
    missing_files = [name for name in BGM_ID_ORDER if name not in bgm_set]
    extra_files = sorted(bgm_set - set(BGM_ID_ORDER), key=str.lower)

    if missing_files:
        print("  Warning: Missing BGM files for known IDs:")
        for name in missing_files:
            print(f"    - {name}.ogg")

    if extra_files:
        print("  Warning: BGM files not registered in BGM_ID_ORDER:")
        for name in extra_files:
            print(f"    - {name}.ogg")

    generated = 0
    for filename in BGM_ID_ORDER:
        if filename not in bgm_set:
            continue
        bgm_id = BGM_BASE_ID + BGM_ID_MAP[filename]
        json_data = create_bgm_json_data(bgm_id)

        json_path = BGM_DIR / f"{filename}.json"
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(json_data, f, indent=2, ensure_ascii=False)
        generated += 1

    print(f"  Generated {generated} JSON files")


if __name__ == "__main__":
    generate_bgm_json()
