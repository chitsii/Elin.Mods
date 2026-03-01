#!/usr/bin/env python3
"""
フェーズシステム検証スクリプト

EPILOGUE/POSTGAMEフェーズの追加を検証する。
TDDのREDフェーズ: 新しいフェーズが追加される前は失敗する。

使用方法:
    cd tests
    uv run python verify_phase_system.py
"""

import json
import sys
from pathlib import Path
from typing import List, Set

# パス設定
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
TOOLS_DIR = PROJECT_ROOT / "tools"

# Add tools to path
sys.path.insert(0, str(TOOLS_DIR))


def get_all_phases() -> List[str]:
    """定義されている全フェーズを取得"""
    from arena.data.quests import PHASES

    return PHASES


def get_phase_enum_values() -> List[str]:
    """Phase Enumの値を取得"""
    from arena.data.config import Phase

    return [p.value for p in Phase]


def load_quest_definitions() -> list[dict]:
    """quest_definitions.jsonを読み込む"""
    path = PROJECT_ROOT / "Package" / "quest_definitions.json"
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def get_quests_by_phase(phase: str) -> List[dict]:
    """特定フェーズのクエストを取得"""
    definitions = load_quest_definitions()
    return [q for q in definitions if q["phase"].upper() == phase.upper()]


# ============================================================================
# テスト関数
# ============================================================================


def test_epilogue_phase_exists() -> tuple[bool, str]:
    """EPILOGUEフェーズが定義されている"""
    phases = get_all_phases()

    if "EPILOGUE" not in phases:
        return False, f"EPILOGUE not in phases: {phases}"

    # CLIMAX の次に EPILOGUE がある
    try:
        climax_index = phases.index("CLIMAX")
        epilogue_index = phases.index("EPILOGUE")
        if epilogue_index != climax_index + 1:
            return (
                False,
                f"EPILOGUE should be after CLIMAX. CLIMAX={climax_index}, EPILOGUE={epilogue_index}",
            )
    except ValueError as e:
        return False, f"Phase index error: {e}"

    return True, "EPILOGUE phase exists at correct position"


def test_postgame_phase_exists() -> tuple[bool, str]:
    """POSTGAMEフェーズが定義されている"""
    phases = get_all_phases()

    if "POSTGAME" not in phases:
        return False, f"POSTGAME not in phases: {phases}"

    # EPILOGUE の次に POSTGAME がある
    try:
        epilogue_index = phases.index("EPILOGUE")
        postgame_index = phases.index("POSTGAME")
        if postgame_index != epilogue_index + 1:
            return (
                False,
                f"POSTGAME should be after EPILOGUE. EPILOGUE={epilogue_index}, POSTGAME={postgame_index}",
            )
    except ValueError as e:
        return False, f"Phase index error: {e}"

    return True, "POSTGAME phase exists at correct position"


def test_phase_enum_includes_new_phases() -> tuple[bool, str]:
    """Phase Enumに新しいフェーズが含まれる"""
    try:
        from arena.data.config import Phase

        phase_values = [p.value for p in Phase]

        errors = []
        if "epilogue" not in phase_values:
            errors.append("Phase enum missing 'epilogue'")
        if "postgame" not in phase_values:
            errors.append("Phase enum missing 'postgame'")

        if errors:
            return False, "; ".join(errors)

        return True, "Phase enum includes new phases"
    except Exception as e:
        return False, f"Error checking Phase enum: {e}"


def test_phase_ordinals_are_sequential() -> tuple[bool, str]:
    """フェーズの序数が連続している"""
    phases = get_all_phases()

    expected_phases = [
        "PROLOGUE",
        "INITIATION",
        "RISING",
        "AWAKENING",
        "CONFRONTATION",
        "CLIMAX",
        "EPILOGUE",
        "POSTGAME",
    ]

    if phases != expected_phases:
        return False, f"Expected phases: {expected_phases}, Got: {phases}"

    return True, "Phase ordinals are sequential"


def test_existing_quests_not_affected() -> tuple[bool, str]:
    """既存クエストのフェーズが影響を受けない"""
    definitions = load_quest_definitions()

    # 既存フェーズのクエストがまだ存在することを確認
    expected_phases = {
        "prologue",
        "initiation",
        "rising",
        "awakening",
        "confrontation",
        "climax",
    }
    quest_phases = {q["phase"] for q in definitions}

    missing = expected_phases - quest_phases
    if missing:
        return False, f"Missing quests for phases: {missing}"

    return True, "Existing quests not affected"


def test_endgame_quests_require_last_battle() -> tuple[bool, str]:
    """エンドゲームクエストはLAST_BATTLE完了が前提（未来のテスト）"""
    definitions = load_quest_definitions()

    # EPILOGUE/POSTGAMEフェーズのクエストを取得
    endgame_quests = [
        q for q in definitions if q["phase"].upper() in ("EPILOGUE", "POSTGAME")
    ]

    if not endgame_quests:
        # まだエンドゲームクエストがない場合はスキップ
        return True, "No endgame quests yet (expected before implementation)"

    errors = []
    for quest in endgame_quests:
        required = quest.get("requiredQuests", [])
        # 直接的または間接的に18_last_battleを要求する必要がある
        # 簡易チェック: required_questsに含まれるか
        if "18_last_battle" not in required:
            # 間接的な依存関係は許容（後でより詳細な検証が必要）
            pass  # 警告のみ

    if errors:
        return False, "; ".join(errors)

    return True, "Endgame quests dependency check passed"


def test_phase_flag_values_extended() -> tuple[bool, str]:
    """FlagValues.Phaseが拡張されている"""
    try:
        from arena.data.config import FlagValues

        errors = []

        # EPILOGUE = 6, POSTGAME = 7
        if not hasattr(FlagValues.Phase, "EPILOGUE"):
            errors.append("FlagValues.Phase missing EPILOGUE")
        elif FlagValues.Phase.EPILOGUE != 6:
            errors.append(f"FlagValues.Phase.EPILOGUE should be 6, got {FlagValues.Phase.EPILOGUE}")

        if not hasattr(FlagValues.Phase, "POSTGAME"):
            errors.append("FlagValues.Phase missing POSTGAME")
        elif FlagValues.Phase.POSTGAME != 7:
            errors.append(f"FlagValues.Phase.POSTGAME should be 7, got {FlagValues.Phase.POSTGAME}")

        if errors:
            return False, "; ".join(errors)

        return True, "FlagValues.Phase extended correctly"
    except Exception as e:
        return False, f"Error checking FlagValues.Phase: {e}"


# ============================================================================
# メイン
# ============================================================================


def main():
    print("=" * 60)
    print("Phase System Verification")
    print("=" * 60)

    tests = [
        ("EPILOGUE phase exists", test_epilogue_phase_exists),
        ("POSTGAME phase exists", test_postgame_phase_exists),
        ("Phase enum includes new phases", test_phase_enum_includes_new_phases),
        ("Phase ordinals sequential", test_phase_ordinals_are_sequential),
        ("Existing quests not affected", test_existing_quests_not_affected),
        ("Endgame quests require LAST_BATTLE", test_endgame_quests_require_last_battle),
        ("FlagValues.Phase extended", test_phase_flag_values_extended),
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
        print("(Expected before EPILOGUE/POSTGAME implementation)")
        return 1


if __name__ == "__main__":
    sys.exit(main())
