#!/usr/bin/env python3
"""
リリィの研究実験クエスト検証スクリプト

pg_01_lily_researchクエストの定義と関連ドラマを検証する。
TDDのREDフェーズ: クエストが追加される前は失敗する。

使用方法:
    cd tests
    uv run python verify_lily_research.py
"""

import json
import sys
from pathlib import Path
from typing import List, Dict, Any, Optional

# パス設定
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
TOOLS_DIR = PROJECT_ROOT / "tools"
DRAMA_DIR = PROJECT_ROOT / "LangMod" / "JP" / "Dialog" / "Drama"

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


def find_flag_condition(quest: dict, flag_key: str) -> dict | None:
    """クエストのrequiredFlagsから指定キーの条件を検索"""
    for cond in quest.get("requiredFlags", []):
        if cond.get("flagKey") == flag_key:
            return cond
    return None


# ============================================================================
# テスト関数
# ============================================================================


def test_lily_research_quest_defined() -> tuple[bool, str]:
    """pg_01_lily_researchクエストが定義されている"""
    definitions = load_quest_definitions()
    quest = find_quest(definitions, "pg_01_lily_research")

    if quest is None:
        return False, "Quest 'pg_01_lily_research' not found in quest_definitions.json"

    errors = []

    # フェーズチェック
    if quest.get("phase", "").upper() != "EPILOGUE":
        errors.append(f"Expected phase 'EPILOGUE', got '{quest.get('phase')}'")

    # 前提クエストチェック
    required = quest.get("requiredQuests", [])
    if "18_last_battle" not in required:
        errors.append("Quest should require '18_last_battle'")

    # クエストタイプチェック
    quest_type = quest.get("questType", "")
    if quest_type not in ("postgame", "character_event"):
        errors.append(f"Expected questType 'postgame' or 'character_event', got '{quest_type}'")

    if errors:
        return False, "; ".join(errors)

    return True, f"Quest 'pg_01_lily_research' defined correctly"


def test_lily_research_quest_ids_defined() -> tuple[bool, str]:
    """QuestIdsにpg_01_lily_researchが定義されている"""
    try:
        from arena.data.config import QuestIds

        if not hasattr(QuestIds, "PG_01_LILY_RESEARCH"):
            return False, "QuestIds.PG_01_LILY_RESEARCH not defined"

        quest_id = QuestIds.PG_01_LILY_RESEARCH
        if quest_id != "pg_01_lily_research":
            return False, f"Expected 'pg_01_lily_research', got '{quest_id}'"

        return True, "QuestIds.PG_01_LILY_RESEARCH defined"
    except Exception as e:
        return False, f"Error checking QuestIds: {e}"


def test_lily_research_drama_ids_defined() -> tuple[bool, str]:
    """DramaIdsにlily_researchが定義されている"""
    try:
        from arena.data.config import DramaIds, DramaNames

        if not hasattr(DramaIds, "LILY_RESEARCH"):
            return False, "DramaIds.LILY_RESEARCH not defined"

        if not hasattr(DramaNames, "LILY_RESEARCH"):
            return False, "DramaNames.LILY_RESEARCH not defined"

        return True, "DramaIds.LILY_RESEARCH and DramaNames.LILY_RESEARCH defined"
    except Exception as e:
        return False, f"Error checking DramaIds: {e}"


def test_lily_research_scenario_exists() -> tuple[bool, str]:
    """pg_01_lily_research.pyシナリオファイルが存在する"""
    scenario_path = TOOLS_DIR / "arena" / "scenarios" / "pg_01_lily_research.py"

    if not scenario_path.exists():
        return False, f"Scenario file not found: {scenario_path}"

    # ファイルが空でないか確認
    content = scenario_path.read_text(encoding="utf-8")
    if len(content) < 100:
        return False, "Scenario file appears to be empty or too small"

    # 必須関数が定義されているか
    if "def define_" not in content:
        return False, "No 'define_' function found in scenario file"

    return True, f"Scenario file exists: {scenario_path.name}"


def test_lily_research_in_lily_quests() -> tuple[bool, str]:
    """LILY_QUESTSにpg_01_lily_researchが登録されている"""
    try:
        from arena.scenarios import LILY_QUESTS
        from arena.data.config import QuestIds

        # LILY_QUESTSにエントリがあるか
        quest_ids_in_list = [entry.quest_id for entry in LILY_QUESTS]

        target_id = "pg_01_lily_research"
        if target_id not in quest_ids_in_list:
            return False, f"'{target_id}' not found in LILY_QUESTS: {quest_ids_in_list}"

        return True, f"'{target_id}' registered in LILY_QUESTS"
    except ImportError as e:
        return False, f"Import error: {e}"
    except Exception as e:
        return False, f"Error checking LILY_QUESTS: {e}"


def test_lily_research_has_ending_branches() -> tuple[bool, str]:
    """エンディングによる分岐が存在する（任意）"""
    scenario_path = TOOLS_DIR / "arena" / "scenarios" / "pg_01_lily_research.py"

    if not scenario_path.exists():
        return False, "Scenario file not found"

    content = scenario_path.read_text(encoding="utf-8")

    # エンディング分岐のヒント
    has_rescue = "rescue" in content.lower()
    has_inherit = "inherit" in content.lower()
    has_ending_check = "ENDING" in content or "ending" in content

    if has_ending_check and (has_rescue or has_inherit):
        return True, "Ending branches detected in scenario"

    # 分岐がなくてもOK（最初の実装では任意）
    return True, "Ending branches not required for initial implementation"


def test_quests_data_includes_lily_research() -> tuple[bool, str]:
    """arena/data/quests.pyにpg_01_lily_researchが定義されている"""
    try:
        from arena.data.quests import QUESTS

        quest_ids = [q.quest_id for q in QUESTS]

        if "pg_01_lily_research" not in quest_ids:
            return False, f"'pg_01_lily_research' not found in QUESTS"

        return True, "'pg_01_lily_research' defined in QUESTS"
    except Exception as e:
        return False, f"Error checking QUESTS: {e}"


# ============================================================================
# メイン
# ============================================================================


def main():
    print("=" * 60)
    print("Lily Research Quest Verification")
    print("=" * 60)

    tests = [
        ("QuestIds.PG_01_LILY_RESEARCH defined", test_lily_research_quest_ids_defined),
        ("DramaIds.LILY_RESEARCH defined", test_lily_research_drama_ids_defined),
        ("Quest in QUESTS list", test_quests_data_includes_lily_research),
        ("Quest in quest_definitions.json", test_lily_research_quest_defined),
        ("Scenario file exists", test_lily_research_scenario_exists),
        ("Registered in LILY_QUESTS", test_lily_research_in_lily_quests),
        ("Ending branches (optional)", test_lily_research_has_ending_branches),
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
        print("(Expected before pg_01_lily_research implementation)")
        return 1


if __name__ == "__main__":
    sys.exit(main())
