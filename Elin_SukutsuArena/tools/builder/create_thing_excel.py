# -*- coding: utf-8 -*-
"""
create_thing_excel.py - SourceThing.xlsx 自動生成

item_definitions.py の定義を読み込み、CWL形式のExcelを生成する。
create_zone_excel.py / create_chara_excel.py と同様のパターン。

Usage:
    python create_thing_excel.py [--debug]

    --debug: デバッグモード（価格を1に設定）
"""

import csv
import os
import sys

# パス設定
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

sys.path.insert(0, TOOLS_DIR)
from arena.data import CUSTOM_ITEMS, TraitType

# デバッグモード判定
DEBUG_MODE = "--debug" in sys.argv

# 出力パス
OUTPUT_EN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Thing.tsv")
OUTPUT_CN_TSV = os.path.join(PROJECT_ROOT, "LangMod", "CN", "Thing.tsv")

# SourceThing カラム定義（ヘッダー行）
# バニラと同じ52カラム
HEADERS = [
    "id",  # 1
    "name_JP",  # 2
    "unknown_JP",  # 3
    "unit_JP",  # 4
    "naming",  # 5
    "name",  # 6
    "unit",  # 7
    "unknown",  # 8
    "category",  # 9
    "sort",  # 10
    "sort",  # 11 (重複)
    "_tileType",  # 12
    "_idRenderData",  # 13
    "tiles",  # 14
    "altTiles",  # 15
    "anime",  # 16
    "skins",  # 17
    "size",  # 18
    "colorMod",  # 19
    "colorType",  # 20
    "recipeKey",  # 21
    "factory",  # 22
    "components",  # 23
    "disassemble",  # 24
    "defMat",  # 25
    "tierGroup",  # 26
    "value",  # 27
    "LV",  # 28
    "chance",  # 29
    "quality",  # 30
    "HP",  # 31
    "weight",  # 32
    "electricity",  # 33
    "trait",  # 34
    "elements",  # 35
    "range",  # 36
    "attackType",  # 37
    "offense",  # 38
    "substats",  # 39
    "defense",  # 40
    "lightData",  # 41
    "idExtra",  # 42
    "idToggleExtra",  # 43
    "idActorEx",  # 44
    "idSound",  # 45
    "tag",  # 46
    "workTag",  # 47
    "filter",  # 48
    "roomName_JP",  # 49
    "roomName",  # 50
    "detail_JP",  # 51
    "detail",  # 52
]

# 型情報（2行目）- オリジナルのSourceThingに合わせる
TYPES = [
    "string",  # id
    "string",  # name_JP
    "string",  # unknown_JP
    "string",  # unit_JP
    "string",  # naming
    "string",  # name
    "string",  # unit
    "string",  # unknown
    "string",  # category
    "",  # sort (1)
    "int",  # sort (2)
    "string",  # _tileType
    "string",  # _idRenderData
    "int[]",  # tiles
    "int[]",  # altTiles
    "int[]",  # anime
    "int[]",  # skins
    "int[]",  # size
    "int",  # colorMod
    "string",  # colorType
    "string[]",  # recipeKey
    "string[]",  # factory
    "string[]",  # components
    "string[]",  # disassemble
    "string",  # defMat
    "string",  # tierGroup
    "int",  # value
    "int",  # LV
    "int",  # chance
    "int",  # quality
    "int",  # HP
    "int",  # weight
    "int",  # electricity
    "string[]",  # trait
    "elements",  # elements
    "int",  # range
    "string",  # attackType
    "int[]",  # offense
    "int[]",  # substats
    "int[]",  # defense
    "string",  # lightData
    "string",  # idExtra
    "string",  # idToggleExtra
    "string",  # idActorEx
    "string",  # idSound
    "string[]",  # tag
    "string",  # workTag
    "string[]",  # filter
    "string[]",  # roomName_JP
    "string[]",  # roomName
    "string",  # detail_JP
    "string",  # detail
]

# デフォルト値（3行目）- オリジナルのSourceThingに合わせる
DEFAULTS = [
    "",  # id
    "",  # name_JP
    "",  # unknown_JP
    "個",  # unit_JP
    "",  # naming
    "",  # name
    "",  # unit
    "",  # unknown
    "other",  # category
    "",  # sort (1)
    "100",  # sort (2)
    "",  # _tileType
    "",  # _idRenderData
    "0",  # tiles
    "",  # altTiles
    "",  # anime
    "",  # skins
    "",  # size
    "100",  # colorMod
    "",  # colorType
    "",  # recipeKey
    "",  # factory
    "log",  # components
    "",  # disassemble
    "oak",  # defMat
    "",  # tierGroup
    "100",  # value
    "1",  # LV
    "1000",  # chance
    "",  # quality
    "100",  # HP
    "1000",  # weight
    "",  # electricity
    "",  # trait
    "",  # elements
    "1",  # range
    "",  # attackType
    "",  # offense
    "",  # substats
    "",  # defense
    "",  # lightData
    "",  # idExtra
    "",  # idToggleExtra
    "",  # idActorEx
    "",  # idSound
    "",  # tag
    "",  # workTag
    "",  # filter
    "",  # roomName_JP
    "",  # roomName
    "",  # detail_JP
    "",  # detail
]

# カラム名→インデックスのマップ
HEADER_MAP = {name: idx for idx, name in enumerate(HEADERS)}


def generate_rows(lang="en"):
    """指定言語用のデータ行を生成"""
    rows = []

    # Row 1: ヘッダー
    rows.append(HEADERS)

    # Row 2: 型情報
    rows.append(TYPES)

    # Row 3: デフォルト値
    rows.append(DEFAULTS)

    # Row 4+: アイテムデータ
    for item_id, item in CUSTOM_ITEMS.items():
        row = [""] * len(HEADERS)

        # 基本情報
        set_cell(row, "id", item.id)
        set_cell(row, "name_JP", item.name_jp)
        set_cell(row, "category", item.category)

        # 言語に応じてnameとdetailを設定
        if lang == "cn":
            # CN: nameカラムに中国語、なければ英語にフォールバック
            set_cell(row, "name", item.name_cn if item.name_cn else item.name_en)
            set_cell(row, "detail", item.detail_cn if item.detail_cn else item.detail_en)
        else:
            # EN: nameカラムに英語
            set_cell(row, "name", item.name_en)
            set_cell(row, "detail", item.detail_en)

        set_cell(row, "detail_JP", item.detail_jp)

        # Trait設定
        trait_str = build_trait_string(item)
        set_cell(row, "trait", trait_str)

        # エレメント
        if item.elements:
            set_cell(row, "elements", item.elements)

        # ゲームデータ
        item_value = 1 if DEBUG_MODE else item.value
        set_cell(row, "value", item_value)
        set_cell(row, "LV", item.lv)
        set_cell(row, "weight", item.weight)
        set_cell(row, "chance", item.chance)

        # レンダリング
        set_cell(row, "tiles", item.tiles)
        set_cell(row, "_idRenderData", item.render_data)

        # 装備品用フィールド
        if item.def_mat:
            set_cell(row, "defMat", item.def_mat)
        if item.tier_group:
            set_cell(row, "tierGroup", item.tier_group)
        if item.defense:
            set_cell(row, "defense", item.defense)

        # タグ
        if item.tags:
            tags_str = ",".join(item.tags)
            set_cell(row, "tag", tags_str)

        # sort（カラム10）- 最初の sort カラムに設定
        if item.sort:
            # HEADER_MAP["sort"] は最初のsortを指すので直接インデックス9を使用
            row[9] = item.sort

        # quality（カラム30）
        if item.quality:
            set_cell(row, "quality", item.quality)

        # filter（カラム48）
        if item.filter:
            set_cell(row, "filter", item.filter)

        # components（カラム23）
        if item.components:
            set_cell(row, "components", item.components)

        rows.append(row)

    return rows


def main():
    if DEBUG_MODE:
        print("[DEBUG MODE] Item prices set to 1")
    print(f"Generating Thing TSV from {len(CUSTOM_ITEMS)} item definition(s)...")

    # EN用TSV出力
    rows_en = generate_rows(lang="en")
    write_tsv(OUTPUT_EN_TSV, rows_en)

    # CN用TSV出力
    rows_cn = generate_rows(lang="cn")
    write_tsv(OUTPUT_CN_TSV, rows_cn)

    print(f"Generated {len(CUSTOM_ITEMS)} item(s)")
    print(f"  EN: {OUTPUT_EN_TSV}")
    print(f"  CN: {OUTPUT_CN_TSV}")


def set_cell(row, column_name, value):
    """指定カラムに値を設定"""
    if column_name in HEADER_MAP:
        row[HEADER_MAP[column_name]] = value


def build_trait_string(item):
    """Trait文字列を構築（カンマ区切り）

    Elinは自動で「Trait」プレフィックスを付けるため、名前空間なしの短い名前を使用。
    CWLのTypeQualifierが実行時に完全修飾名に解決する。

    例: SukutsuItem,kiss_of_inferno
    → Elinが TraitSukutsuItem に変換
    → CWLが Elin_SukutsuArena.TraitSukutsuItem に解決

    カセットテープの場合: Tape,{bgm_id}
    → TraitTape が refVal として BGM ID を受け取る
    """
    if item.trait_type == TraitType.CUSTOM:
        # カスタムTrait: 名前空間なしで指定（CWLが解決）
        parts = [item.trait_name] + item.trait_params
    else:
        # バニラTrait
        parts = [item.trait_name] + item.trait_params

    # ref_val がある場合（カセットテープ等）、traitパラメータに追加
    if item.ref_val is not None:
        parts.append(str(item.ref_val))

    return ",".join(str(p) for p in parts)


def write_tsv(path, row_data):
    """TSVファイル出力"""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, delimiter="\t", quoting=csv.QUOTE_MINIMAL)
        writer.writerows(row_data)
    print(f"  Created TSV: {path}")


if __name__ == "__main__":
    main()
