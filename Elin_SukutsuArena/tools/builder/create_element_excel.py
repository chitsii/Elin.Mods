# -*- coding: utf-8 -*-
"""
create_element_excel.py - SourceElement.xlsx 自動生成

カスタムフィート（闘志など）をCWL形式のExcelファイルとして生成する。
バニラと同じ57カラム構造を使用。
EN版とCN版の両方を生成。
"""

import csv
import os

# パス設定
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

# 出力パス
OUTPUT_EN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Element.tsv")
OUTPUT_CN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "CN", "Element.tsv")

# SourceElement カラム定義（バニラと同じ57カラム）
HEADERS = [
    "id",  # 0
    "alias",  # 1
    "name_JP",  # 2
    "name",  # 3
    "altname_JP",  # 4
    "altname",  # 5
    "aliasParent",  # 6
    "aliasRef",  # 7
    "aliasMtp",  # 8
    "parentFactor",  # 9
    "lvFactor",  # 10
    "encFactor",  # 11
    "encSlot",  # 12
    "mtp",  # 13
    "LV",  # 14
    "chance",  # 15
    "value",  # 16
    "cost",  # 17
    "geneSlot",  # 18
    "sort",  # 19
    "target",  # 20
    "proc",  # 21
    "type",  # 22
    "group",  # 23
    "category",  # 24
    "categorySub",  # 25
    "abilityType",  # 26
    "tag",  # 27
    "thing",  # 28
    "eleP",  # 29
    "cooldown",  # 30
    "charge",  # 31
    "radius",  # 32
    "max",  # 33
    "req",  # 34
    "idTrainer",  # 35
    "partySkill",  # 36
    "tagTrainer",  # 37
    "levelBonus_JP",  # 38
    "levelBonus",  # 39
    "foodEffect",  # 40
    "note",  # 41
    "langAct",  # 42
    "detail_JP",  # 43
    "detail",  # 44
    "textPhase_JP",  # 45
    "textPhase",  # 46
    "textExtra_JP",  # 47
    "textExtra",  # 48
    "textInc_JP",  # 49
    "textInc",  # 50
    "textDec_JP",  # 51
    "textDec",  # 52
    "textAlt_JP",  # 53
    "textAlt",  # 54
    "adjective_JP",  # 55
    "adjective",  # 56
]

# 型情報（2行目）- バニラに合わせる
TYPES = [
    "int",  # id
    "string",  # alias
    "string",  # name_JP
    "string",  # name
    "string",  # altname_JP
    "string",  # altname
    "string",  # aliasParent
    "string",  # aliasRef
    "string",  # aliasMtp
    "float",  # parentFactor
    "int",  # lvFactor
    "int",  # encFactor
    "string",  # encSlot
    "int",  # mtp
    "int",  # LV
    "int",  # chance
    "int",  # value
    "int[]",  # cost
    "int",  # geneSlot
    "int",  # sort
    "string",  # target
    "string[]",  # proc
    "string",  # type
    "string",  # group
    "string",  # category
    "string",  # categorySub
    "string[]",  # abilityType
    "string[]",  # tag
    "string",  # thing
    "int",  # eleP
    "int",  # cooldown
    "int",  # charge
    "float",  # radius
    "int",  # max
    "string[]",  # req
    "string",  # idTrainer
    "int",  # partySkill
    "string",  # tagTrainer
    "string",  # levelBonus_JP
    "string",  # levelBonus
    "string[]",  # foodEffect
    "",  # note (バニラは型が空)
    "string[]",  # langAct
    "string",  # detail_JP
    "string",  # detail
    "string",  # textPhase_JP
    "string",  # textPhase
    "string",  # textExtra_JP
    "string",  # textExtra
    "string",  # textInc_JP
    "string",  # textInc
    "string",  # textDec_JP
    "string",  # textDec
    "string[]",  # textAlt_JP
    "string[]",  # textAlt
    "string[]",  # adjective_JP
    "string[]",  # adjective
]

# デフォルト値（3行目）- バニラに合わせる
DEFAULTS = {
    "encFactor": "100",
    "mtp": "1",
    "LV": "1",
    "chance": "1000",
    "cost": "0",
    "geneSlot": "1",
    "type": "Element",
    "eleP": "50",
    "charge": "10",
    "radius": "5",
}

# カスタムフィート定義（CN含む）
CUSTOM_FEATS = [
    {
        "id": 10001,
        "alias": "featArenaSpirit",
        "name_JP": "闘志",
        "name": "Arena Spirit",
        "name_CN": "斗志",
        "type": "FeatArenaSpirit",
        "group": "FEAT",
        "category": "feat",
        "tag": "innate",  # バニラFEATと同様
        # abilityType を追加する場合は noRandomAbility,specialAbility を付与（ランダム冒険者対策）
        "max": 7,
        "cost": "0",
        "detail_JP": "倒れても立ち上がる。それが剣闘士の本能だ。",
        "detail": "Fall and rise again. That is the instinct of a gladiator.",
        "detail_CN": "倒下后再次站起。这是剑斗士的本能。",
        # textExtra は空にする（_OnApply 内で hints.Add() を使用して表示を制御）
        "textPhase_JP": "闘志",
        "textPhase": "Arena Spirit",
        "textPhase_CN": "斗志",
    },
]


def create_row(feat, lang="en"):
    """言語に応じた行を生成"""
    row = []
    for header in HEADERS:
        if lang == "cn":
            # CN版: サフィックスなしの列に中国語を入れる
            if header == "name":
                value = feat.get("name_CN", feat.get("name", ""))
            elif header == "detail":
                value = feat.get("detail_CN", feat.get("detail", ""))
            elif header == "textPhase":
                value = feat.get("textPhase_CN", feat.get("textPhase", ""))
            elif header == "textExtra":
                value = feat.get("textExtra_CN", feat.get("textExtra", ""))
            else:
                value = feat.get(header, "")
        else:
            value = feat.get(header, "")
        row.append(str(value) if value is not None else "")
    return row


def write_tsv(path, row_data):
    """TSVファイル出力"""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, delimiter="\t", quoting=csv.QUOTE_MINIMAL)
        writer.writerows(row_data)
    print(f"  Created TSV: {path}")


def main():
    print(f"Generating Element TSV from {len(CUSTOM_FEATS)} feat definition(s)...")

    # デフォルト行を生成
    default_row = [DEFAULTS.get(h, "") for h in HEADERS]

    # EN版
    rows_en = [HEADERS, TYPES, default_row]
    for feat in CUSTOM_FEATS:
        rows_en.append(create_row(feat, lang="en"))
    write_tsv(OUTPUT_EN_TSV, rows_en)

    # CN版
    rows_cn = [HEADERS, TYPES, default_row]
    for feat in CUSTOM_FEATS:
        rows_cn.append(create_row(feat, lang="cn"))
    write_tsv(OUTPUT_CN_TSV, rows_cn)

    print(f"Generated {len(CUSTOM_FEATS)} feat(s) (EN + CN)")


if __name__ == "__main__":
    main()
