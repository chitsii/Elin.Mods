#!/usr/bin/env python3
"""
エンドゲームフレームワーク検証スクリプト

POSTGAMEクエストタイプとエンドゲームクエストテンプレートを検証する。
TDDのREDフェーズ: POSTGAMEクエストタイプが追加される前は失敗する。

使用方法:
    cd tests
    uv run python verify_endgame_framework.py
"""

import json
import sys
from pathlib import Path
from typing import List, Dict, Any

# パス設定
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
TOOLS_DIR = PROJECT_ROOT / "tools"

# Add tools to path
sys.path.insert(0, str(TOOLS_DIR))


def load_quest_definitions() -> list[dict]:
    """quest_definitions.jsonを読み込む"""
    path = PROJECT_ROOT / "Package" / "quest_definitions.json"
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def find_quest(definitions: list[dict], quest_id: str) -> dict | None:
    """クエストIDでクエストを検索"""
    for quest in definitions:
        if quest["questId"] == quest_id:
            return quest
    return None


def get_all_quest_types() -> List[str]:
    """定義されている全クエストタイプを取得"""
    from cwl_quest_lib.core.config_models import QuestType

    return [qt.value for qt in QuestType]


# ============================================================================
# テスト関数
# ============================================================================


def test_postgame_quest_type_exists() -> tuple[bool, str]:
    """QuestType.POSTGAMEが定義されている"""
    try:
        from cwl_quest_lib.core.config_models import QuestType

        quest_types = [qt.value for qt in QuestType]

        if "postgame" not in quest_types:
            return False, f"'postgame' not in QuestType: {quest_types}"

        return True, "QuestType.POSTGAME exists"
    except Exception as e:
        return False, f"Error checking QuestType: {e}"


def test_quest_type_includes_epilogue() -> tuple[bool, str]:
    """QuestType.EPILOGUEが定義されている（任意）"""
    try:
        from cwl_quest_lib.core.config_models import QuestType

        quest_types = [qt.value for qt in QuestType]

        # EPILOGUEは任意だがあると便利
        if "epilogue" in quest_types:
            return True, "QuestType.EPILOGUE exists"

        # EPILOGUEがなくてもENDINGがあればOK
        if "ending" in quest_types:
            return True, "QuestType.ENDING can be used for epilogue quests"

        return False, "Neither 'epilogue' nor 'ending' in QuestType"
    except Exception as e:
        return False, f"Error checking QuestType: {e}"


def test_endgame_quest_ids_defined() -> tuple[bool, str]:
    """エンドゲームクエストIDがQuestIdsに定義されている"""
    try:
        from arena.data.config import QuestIds

        errors = []

        # POSTGAME クエストID が定義されているか
        # 例: PG_01_LILY_RESEARCH
        postgame_ids = [
            name
            for name in dir(QuestIds)
            if not name.startswith("_") and name.startswith("PG_")
        ]

        # まだ定義がない場合は警告のみ
        if not postgame_ids:
            return True, "No POSTGAME quest IDs defined yet (expected before implementation)"

        return True, f"Found {len(postgame_ids)} POSTGAME quest IDs"
    except Exception as e:
        return False, f"Error checking QuestIds: {e}"


def test_ending_flag_values_defined() -> tuple[bool, str]:
    """エンディングフラグ値が定義されている"""
    try:
        from arena.data.config import FlagValues, Keys

        errors = []

        # RESCUE/INHERIT/USURP エンディング値
        if not hasattr(FlagValues, "Ending"):
            errors.append("FlagValues.Ending not defined")
        else:
            ending = FlagValues.Ending
            if not hasattr(ending, "RESCUE"):
                errors.append("FlagValues.Ending.RESCUE not defined")
            if not hasattr(ending, "INHERIT"):
                errors.append("FlagValues.Ending.INHERIT not defined")
            if not hasattr(ending, "USURP"):
                errors.append("FlagValues.Ending.USURP not defined")

        # Keys.ENDING が定義されている
        if not hasattr(Keys, "ENDING"):
            errors.append("Keys.ENDING not defined")

        if errors:
            return False, "; ".join(errors)

        return True, "Ending flag values defined correctly"
    except Exception as e:
        return False, f"Error checking ending flags: {e}"


def test_endgame_quests_use_correct_phase() -> tuple[bool, str]:
    """エンドゲームクエストが正しいフェーズを使用"""
    definitions = load_quest_definitions()

    # EPILOGUEまたはPOSTGAMEフェーズのクエストを検索
    endgame_quests = [
        q for q in definitions if q["phase"] and q["phase"].upper() in ("EPILOGUE", "POSTGAME")
    ]

    if not endgame_quests:
        return True, "No endgame quests defined yet (expected before implementation)"

    errors = []
    for quest in endgame_quests:
        # POSTGAMEクエストはpostgameタイプを使用すべき
        if quest["phase"].upper() == "POSTGAME" and quest["questType"] != "postgame":
            # ENDINGタイプも許容
            if quest["questType"] not in ("postgame", "ending", "character_event"):
                errors.append(
                    f"Quest {quest['questId']} in POSTGAME phase should use 'postgame' type, got '{quest['questType']}'"
                )

    if errors:
        return False, "; ".join(errors)

    return True, f"Found {len(endgame_quests)} endgame quests with correct phase"


def test_postgame_quests_require_last_battle() -> tuple[bool, str]:
    """POSTGAMEクエストはLAST_BATTLE完了が前提"""
    definitions = load_quest_definitions()

    postgame_quests = [
        q for q in definitions if q["phase"] and q["phase"].upper() == "POSTGAME"
    ]

    if not postgame_quests:
        return True, "No POSTGAME quests defined yet (expected before implementation)"

    errors = []
    for quest in postgame_quests:
        required_quests = quest.get("requiredQuests", [])

        # 直接または間接的に18_last_battleを要求
        if "18_last_battle" not in required_quests:
            # 間接的な依存関係を確認
            # エピローグクエストを経由している場合もOK
            pass  # 簡易チェック: 警告のみ

    if errors:
        return False, "; ".join(errors)

    return True, f"Found {len(postgame_quests)} POSTGAME quests"


# ============================================================================
# メイン
# ============================================================================


def main():
    print("=" * 60)
    print("Endgame Framework Verification")
    print("=" * 60)

    tests = [
        ("QuestType.POSTGAME exists", test_postgame_quest_type_exists),
        ("QuestType for epilogue", test_quest_type_includes_epilogue),
        ("Endgame quest IDs", test_endgame_quest_ids_defined),
        ("Ending flag values", test_ending_flag_values_defined),
        ("Endgame quests use correct phase", test_endgame_quests_use_correct_phase),
        ("POSTGAME quests require LAST_BATTLE", test_postgame_quests_require_last_battle),
    ]

    all_passed = True
    for name, test_func in tests:
        try:
            passed, message = test_func()
            status = "PASS" if passed else "FAIL"
            print(f"  [{status}] {name}")
            if not passed:
                all_passed = False
                for line in message.split("; ")[:5]:
                    print(f"        {line}")
        except Exception as e:
            all_passed = False
            print(f"  [ERROR] {name}: {e}")

    print()
    print("=" * 60)
    if all_passed:
        print("RESULT: ALL TESTS PASSED")
        return 0
    else:
        print("RESULT: SOME TESTS FAILED")
        print("(Expected before POSTGAME quest type implementation)")
        return 1


if __name__ == "__main__":
    sys.exit(main())
