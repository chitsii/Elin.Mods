# -*- coding: utf-8 -*-
"""
SourceQuest TSV 生成スクリプト

CWLネイティブのジャーナル表示用クエスト定義を生成する。
バニラと同じ17カラム構造を使用。
build.batでsofficeによりxlsxに変換される。
"""

import os
import sys

# パス設定
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

sys.path.insert(0, TOOLS_DIR)

from data.quests import QUEST_ID, QUEST_NAME_CN, QUEST_NAME_EN, QUEST_NAME_JP, QUEST_PHASES

OUTPUT_JP = os.path.join(PROJECT_ROOT, "LangMod", "JP", "Quest.tsv")
OUTPUT_EN = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Quest.tsv")
OUTPUT_CN = os.path.join(PROJECT_ROOT, "LangMod", "CN", "Quest.tsv")

# SourceQuest のヘッダー列（バニラと同じ17カラム）
HEADERS = [
    "id",  # 0
    "name_JP",  # 1
    "name",  # 2
    "type",  # 3
    "drama",  # 4
    "idZone",  # 5
    "group",  # 6
    "tags",  # 7
    "money",  # 8
    "chance",  # 9
    "minFame",  # 10
    "detail_JP",  # 11
    "detail",  # 12
    "talkProgress_JP",  # 13
    "talkProgress",  # 14
    "talkComplete_JP",  # 15
    "talkComplete",  # 16
]

# 型情報（2行目）- バニラに合わせる
TYPES = [
    "string",  # id
    "string",  # name_JP
    "string",  # name
    "string",  # type
    "string[]",  # drama
    "string",  # idZone
    "string",  # group
    "string[]",  # tags
    "int",  # money
    "int",  # chance
    "int",  # minFame
    "string",  # detail_JP
    "string",  # detail
    "string",  # talkProgress_JP
    "string",  # talkProgress
    "string",  # talkComplete_JP
    "string",  # talkComplete
]

# デフォルト値（3行目）
DEFAULTS = {}


def _resolve_name(lang: str) -> str:
    """言語に応じたクエスト名を返す"""
    if lang == "cn":
        return QUEST_NAME_CN or QUEST_NAME_EN
    elif lang == "en":
        return QUEST_NAME_EN
    return QUEST_NAME_JP


def _resolve_detail(qp, lang: str) -> str:
    """言語に応じたdetailを返す"""
    if lang == "cn":
        return qp.detail_cn or qp.detail_en
    elif lang == "en":
        return qp.detail_en
    return qp.detail_jp


def generate_quest_rows(lang: str) -> list[dict]:
    """クエスト行を生成"""
    rows = []

    # Phase 0 = 親行（type=QuestSequence）
    phase0 = QUEST_PHASES[0]
    rows.append(
        {
            "id": QUEST_ID,
            "name_JP": QUEST_NAME_JP,
            "name": _resolve_name(lang),
            "type": "QuestSequence",
            "detail_JP": phase0.detail_jp,
            "detail": _resolve_detail(phase0, lang),
        }
    )

    # Phase 1-7 = 各ステージ行
    for qp in QUEST_PHASES:
        rows.append(
            {
                "id": f"{QUEST_ID}{qp.phase}" if qp.phase > 0 else f"{QUEST_ID}0",
                "name_JP": QUEST_NAME_JP,
                "name": _resolve_name(lang),
                "type": "Quest",
                "detail_JP": qp.detail_jp,
                "detail": _resolve_detail(qp, lang),
            }
        )

    return rows


def create_tsv(output_path: str, rows: list[dict]):
    """Quest.tsv を生成"""
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    lines = []

    # Row 1: ヘッダー
    lines.append("\t".join(HEADERS))

    # Row 2: 型情報
    lines.append("\t".join(TYPES))

    # Row 3: デフォルト値
    default_row = [DEFAULTS.get(h, "") for h in HEADERS]
    lines.append("\t".join(default_row))

    # Row 4+: データ
    for quest in rows:
        row = []
        for h in HEADERS:
            value = quest.get(h, "")
            if isinstance(value, str) and ("\n" in value or "\t" in value):
                value = f'"{value}"'
            row.append(str(value))
        lines.append("\t".join(row))

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"  Created: {output_path}")


def main():
    print("Generating Quest.tsv for journal display...")

    # EN版
    rows_en = generate_quest_rows(lang="en")
    create_tsv(OUTPUT_EN, rows_en)

    # JP版
    rows_jp = generate_quest_rows(lang="jp")
    create_tsv(OUTPUT_JP, rows_jp)

    # CN版
    rows_cn = generate_quest_rows(lang="cn")
    create_tsv(OUTPUT_CN, rows_cn)

    print(f"  Generated {len(rows_en)} quest entries (parent + {len(rows_en) - 1} phases)")
    print("Done!")


if __name__ == "__main__":
    main()
