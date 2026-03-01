# -*- coding: utf-8 -*-
"""
SourceQuest.xlsx 生成スクリプト

CWLネイティブのジャーナル表示用クエスト定義を生成する。
バニラと同じ17カラム構造を使用。
build.batでsofficeによりxlsxに変換される。
EN版とCN版の両方を生成。
"""

import os
import sys

# パス設定
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

# arena/data を import パスに追加
sys.path.insert(0, TOOLS_DIR)

from arena.data.quest_journal import (
    JOURNAL_PHASES,
    QUEST_ID,
    QUEST_NAME_CN,
    QUEST_NAME_EN,
    QUEST_NAME_JP,
)

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

# デフォルト値（3行目）- バニラは空なので空のまま
DEFAULTS = {}


def generate_quest_rows(lang="en") -> list[dict]:
    """クエスト行を生成"""
    rows = []

    # メインクエスト行（Phase 0相当）
    # type=QuestSequence: フェーズに応じてidSource が id+phase になり、
    # 対応するSourceQuest行（sukutsu_arena1, sukutsu_arena2...）のdetailが表示される
    first_phase = JOURNAL_PHASES[0]
    if lang == "cn":
        # CN版: サフィックスなしの列（name, detail）に中国語を入れる
        name = QUEST_NAME_CN
        detail = first_phase.detail_cn or first_phase.detail_en
    else:
        name = QUEST_NAME_EN
        detail = first_phase.detail_en

    rows.append(
        {
            "id": QUEST_ID,
            "name_JP": QUEST_NAME_JP,
            "name": name,
            "type": "QuestSequence",
            "detail_JP": first_phase.detail_jp,
            "detail": detail,
        }
    )

    # 各フェーズ行
    for jp in JOURNAL_PHASES:
        if lang == "cn":
            # CN版: サフィックスなしの列（name, detail）に中国語を入れる
            name = QUEST_NAME_CN
            detail = jp.detail_cn or jp.detail_en
        else:
            name = QUEST_NAME_EN
            detail = jp.detail_en

        rows.append(
            {
                "id": f"{QUEST_ID}{jp.phase}",
                "name_JP": QUEST_NAME_JP,
                "name": name,
                "type": "Quest",
                "detail_JP": jp.detail_jp,
                "detail": detail,
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
            # 改行を含む場合はダブルクォートで囲む
            if isinstance(value, str) and ("\n" in value or "\t" in value):
                value = f'"{value}"'
            row.append(str(value))
        lines.append("\t".join(row))

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"Created: {output_path}")


def main():
    print("Generating Quest.tsv for journal display...")

    # EN版
    rows_en = generate_quest_rows(lang="en")
    create_tsv(OUTPUT_EN, rows_en)

    # CN版
    rows_cn = generate_quest_rows(lang="cn")
    create_tsv(OUTPUT_CN, rows_cn)

    print(f"Generated {len(rows_en)} quest entries (including phases) (EN + CN)")
    print("Done!")


if __name__ == "__main__":
    main()
