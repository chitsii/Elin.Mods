# -*- coding: utf-8 -*-
"""
create_merchant_stock.py - 商人在庫JSON自動生成

item_definitions.py の sell_at 情報を元に、
CWL形式の商人在庫JSONファイルを生成する。
"""

import os
import sys
import json
from collections import defaultdict

# パス設定
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

sys.path.insert(0, TOOLS_DIR)
from arena.data import CUSTOM_ITEMS, get_items_by_seller


def generate_stock_json(seller_id: str) -> dict:
    """
    指定した販売者の在庫JSONを生成

    Args:
        seller_id: 販売NPCのID

    Returns:
        CWL形式の在庫辞書
    """
    items = get_items_by_seller(seller_id)

    stock_items = []
    for item in items:
        item_entry = {
            "Id": item.id,
            "Num": item.stock_num,
            "Restock": item.stock_restock,
            "Type": "Item",
            # Note: Rarity は使用しない（バグあり）
        }
        # カセットテープ等でRefValが必要な場合
        if item.ref_val is not None:
            item_entry["RefVal"] = item.ref_val
        # 素材指定がある場合
        if item.def_mat:
            item_entry["Material"] = item.def_mat
        stock_items.append(item_entry)

    return {"Items": stock_items}


def get_all_sellers() -> list:
    """全ての販売NPCのIDを取得"""
    sellers = set()
    for item in CUSTOM_ITEMS.values():
        if item.sell_at:
            sellers.add(item.sell_at)
    return list(sellers)


def main():
    print("Generating merchant stock JSON files...")

    sellers = get_all_sellers()
    print(f"Found {len(sellers)} seller(s): {sellers}")

    for seller_id in sellers:
        stock_data = generate_stock_json(seller_id)
        item_count = len(stock_data["Items"])

        # EN/JP 両方に出力
        for lang in ["EN", "JP"]:
            output_dir = os.path.join(PROJECT_ROOT, "LangMod", lang, "Data")
            os.makedirs(output_dir, exist_ok=True)

            output_path = os.path.join(output_dir, f"stock_{seller_id}.json")
            with open(output_path, "w", encoding="utf-8") as f:
                json.dump(stock_data, f, indent=2, ensure_ascii=False)

            print(f"  Generated: {output_path} ({item_count} items)")

    print(f"\nDone! Generated stock files for {len(sellers)} seller(s)")


if __name__ == "__main__":
    main()
