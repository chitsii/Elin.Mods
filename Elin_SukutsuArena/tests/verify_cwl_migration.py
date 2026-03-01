#!/usr/bin/env python3
"""
CwlQuestFramework移行検証スクリプト

現在のArenaQuestManagerの動作とCwlQuestFrameworkの動作を比較し、
移行前に等価性を確認する。

使用方法:
    cd tests
    uv run python verify_cwl_migration.py
"""

import json
import sys
from pathlib import Path
from dataclasses import dataclass
from typing import Any

# パス設定
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
QUEST_DEFINITIONS_PATH = PROJECT_ROOT / "Package" / "quest_definitions.json"


@dataclass
class FlagCondition:
    """フラグ条件"""
    flag_key: str
    operator: str
    value: Any


@dataclass
class QuestDefinition:
    """クエスト定義"""
    quest_id: str
    phase_ordinal: int
    required_flags: list[FlagCondition]
    required_quests: list[str]
    blocks_quests: list[str]
    auto_trigger: bool
    quest_giver: str | None
    priority: int


def load_quest_definitions() -> list[QuestDefinition]:
    """quest_definitions.jsonを読み込み"""
    with open(QUEST_DEFINITIONS_PATH, 'r', encoding='utf-8') as f:
        data = json.load(f)

    quests = []
    for q in data:
        required_flags = [
            FlagCondition(
                flag_key=rf.get('flagKey', ''),
                operator=rf.get('operator', '=='),
                value=rf.get('value')
            )
            for rf in q.get('requiredFlags', [])
        ]

        quests.append(QuestDefinition(
            quest_id=q['questId'],
            phase_ordinal=q.get('phaseOrdinal', 0),
            required_flags=required_flags,
            required_quests=q.get('requiredQuests', []),
            blocks_quests=q.get('blocksQuests', []),
            auto_trigger=q.get('autoTrigger', False),
            quest_giver=q.get('questGiver'),
            priority=q.get('priority', 0)
        ))

    return quests


class MockFlagStorage:
    """テスト用フラグストレージ"""
    def __init__(self, flags: dict[str, int] = None):
        self._flags = flags or {}

    def get_int(self, key: str, default: int = 0) -> int:
        return self._flags.get(key, default)

    def has_key(self, key: str) -> bool:
        return key in self._flags


class EnumMappingProvider:
    """Enumマッピングプロバイダー（ArenaEnumMappingsと同等）"""
    MAPPINGS = {
        "chitsii.arena.story.phase": {
            "prologue": 0,
            "initiation": 1,
            "rising": 2,
            "awakening": 3,
            "confrontation": 4,
            "climax": 5
        },
        "chitsii.arena.player.rank": {
            "unranked": 0,
            "G": 1,
            "F": 2,
            "E": 3,
            "D": 4,
            "C": 5,
            "B": 6,
            "A": 7,
            "S": 8
        }
    }

    def try_get_mapping(self, flag_key: str) -> dict[str, int] | None:
        return self.MAPPINGS.get(flag_key)


# ============================================================
# ArenaQuestManager互換実装（現在の動作をシミュレート）
# ============================================================

class ArenaQuestManagerCompat:
    """ArenaQuestManagerの動作をPythonで再現"""

    def __init__(self, quests: list[QuestDefinition], storage: MockFlagStorage,
                 completed_quests: set[str], enum_provider: EnumMappingProvider):
        self.quests = quests
        self.storage = storage
        self.completed_quests = completed_quests
        self.enum_provider = enum_provider

    def is_quest_completed(self, quest_id: str) -> bool:
        return quest_id in self.completed_quests

    def check_flag_condition(self, condition: FlagCondition) -> bool:
        """ArenaQuestManager.CheckFlagCondition の動作を再現"""
        current_value = self.storage.get_int(condition.flag_key, 0)

        # int/long比較
        if isinstance(condition.value, (int, float)):
            expected_int = int(condition.value)
            return self._compare_int(current_value, condition.operator, expected_int)

        # string比較（Enumマッピング試行）
        if isinstance(condition.value, str):
            mapping = self.enum_provider.try_get_mapping(condition.flag_key)
            if mapping and condition.value in mapping:
                expected_int = mapping[condition.value]
                return self._compare_int(current_value, condition.operator, expected_int)

            # 文字列として比較（ArenaQuestManager固有の動作）
            current_str = str(current_value)
            if condition.operator == "==":
                return current_str == condition.value
            elif condition.operator == "!=":
                return current_str != condition.value
            return False

        # bool比較
        if isinstance(condition.value, bool):
            current_bool = bool(current_value)
            if condition.operator == "==":
                return current_bool == condition.value
            elif condition.operator == "!=":
                return current_bool != condition.value
            return False

        return False

    def _compare_int(self, current: int, op: str, expected: int) -> bool:
        if op == "==": return current == expected
        if op == "!=": return current != expected
        if op == ">=": return current >= expected
        if op == ">": return current > expected
        if op == "<=": return current <= expected
        if op == "<": return current < expected
        return False

    def get_available_quests(self, current_phase: int) -> list[str]:
        """利用可能なクエストを取得"""
        available = []

        for quest in self.quests:
            # 完了済み
            if self.is_quest_completed(quest.quest_id):
                continue

            # フェーズチェック
            if quest.phase_ordinal > current_phase:
                continue

            # 前提クエスト
            if any(not self.is_quest_completed(req) for req in quest.required_quests):
                continue

            # フラグ条件
            if any(not self.check_flag_condition(fc) for fc in quest.required_flags):
                continue

            # ブロック確認
            is_blocked = False
            for other in self.quests:
                if quest.quest_id in other.blocks_quests and self.is_quest_completed(other.quest_id):
                    is_blocked = True
                    break
            if is_blocked:
                continue

            available.append(quest.quest_id)

        # 優先度でソート
        available.sort(key=lambda qid: next(
            (q.priority for q in self.quests if q.quest_id == qid), 0
        ), reverse=True)

        return available


# ============================================================
# CwlQuestFramework実装（移行後の動作をシミュレート）
# ============================================================

class CwlQuestConditionChecker:
    """QuestConditionChecker の動作を再現"""

    def __init__(self, storage: MockFlagStorage, enum_provider: EnumMappingProvider):
        self.storage = storage
        self.enum_provider = enum_provider

    def check_condition(self, condition: FlagCondition) -> bool:
        current_value = self.storage.get_int(condition.flag_key, 0)
        expected_value = self._resolve_expected_value(condition.flag_key, condition.value)
        return self._evaluate_comparison(current_value, condition.operator, expected_value)

    def _resolve_expected_value(self, flag_key: str, value: Any) -> int:
        if isinstance(value, int):
            return value
        if isinstance(value, float):
            return int(value)
        if isinstance(value, bool):
            return 1 if value else 0
        if isinstance(value, str):
            # Enumマッピング試行
            mapping = self.enum_provider.try_get_mapping(flag_key)
            if mapping is not None:
                if value in mapping:
                    return mapping[value]
                # マッピングは存在するが、値が見つからない → エラー
                raise ValueError(
                    f"Unknown enum value '{value}' for flag '{flag_key}'. "
                    f"Valid values: {list(mapping.keys())}"
                )
            # 数値パース試行
            try:
                return int(value)
            except ValueError:
                # マッピングもなく、整数パースも失敗 → エラー (Fail-fast)
                raise ValueError(
                    f"Cannot resolve string value '{value}' for flag '{flag_key}'. "
                    f"No enum mapping exists and value is not a valid integer."
                )
        return 0

    def _evaluate_comparison(self, current: int, op: str, expected: int) -> bool:
        if op == "==": return current == expected
        if op == "!=": return current != expected
        if op == ">=": return current >= expected
        if op == ">": return current > expected
        if op == "<=": return current <= expected
        if op == "<": return current < expected
        return False

    def check_all_conditions(self, conditions: list[FlagCondition]) -> bool:
        return all(self.check_condition(c) for c in conditions)


class CwlQuestAvailabilityEvaluator:
    """QuestAvailabilityEvaluator の動作を再現"""

    def __init__(self, condition_checker: CwlQuestConditionChecker,
                 is_quest_completed: callable):
        self.condition_checker = condition_checker
        self.is_quest_completed = is_quest_completed

    def is_available(self, quest: QuestDefinition, current_phase: int,
                     all_quests: list[QuestDefinition]) -> bool:
        # 完了済み
        if self.is_quest_completed(quest.quest_id):
            return False

        # フェーズチェック
        if quest.phase_ordinal > current_phase:
            return False

        # 前提クエスト
        for req in quest.required_quests:
            if not self.is_quest_completed(req):
                return False

        # フラグ条件
        if not self.condition_checker.check_all_conditions(quest.required_flags):
            return False

        # ブロック確認
        for other in all_quests:
            if quest.quest_id in other.blocks_quests and self.is_quest_completed(other.quest_id):
                return False

        return True


class CwlQuestManagerCompat:
    """CwlQuestFramework経由のクエスト管理"""

    def __init__(self, quests: list[QuestDefinition], storage: MockFlagStorage,
                 completed_quests: set[str], enum_provider: EnumMappingProvider):
        self.quests = quests
        self.completed_quests = completed_quests

        self.condition_checker = CwlQuestConditionChecker(storage, enum_provider)
        self.evaluator = CwlQuestAvailabilityEvaluator(
            self.condition_checker,
            lambda qid: qid in completed_quests
        )

    def get_available_quests(self, current_phase: int) -> list[str]:
        available = [
            q.quest_id for q in self.quests
            if self.evaluator.is_available(q, current_phase, self.quests)
        ]

        # 優先度でソート
        available.sort(key=lambda qid: next(
            (q.priority for q in self.quests if q.quest_id == qid), 0
        ), reverse=True)

        return available


# ============================================================
# テストケース
# ============================================================

def run_comparison_tests():
    """両実装の動作を比較"""
    print("=" * 60)
    print("CwlQuestFramework Migration Verification")
    print("=" * 60)

    quests = load_quest_definitions()
    enum_provider = EnumMappingProvider()

    all_passed = True
    test_cases = []

    # テストケース1: 初期状態（クエスト未完了）
    test_cases.append({
        "name": "Initial state (no quests completed)",
        "completed": set(),
        "phase": 0,
        "flags": {}
    })

    # テストケース2: オープニング完了後
    test_cases.append({
        "name": "After opening completed",
        "completed": {"01_opening"},
        "phase": 0,
        "flags": {}
    })

    # テストケース3: Prologue完了、Initiation開始
    test_cases.append({
        "name": "Initiation phase start",
        "completed": {"01_opening", "02_rank_up_G"},
        "phase": 1,
        "flags": {}
    })

    # テストケース4: Rising phase
    test_cases.append({
        "name": "Rising phase",
        "completed": {"01_opening", "02_rank_up_G", "03_zek_intro", "04_rank_up_F"},
        "phase": 2,
        "flags": {}
    })

    # テストケース5: 分岐選択後（zek_steal_bottle完了）
    test_cases.append({
        "name": "After branch choice (zek_steal_bottle)",
        "completed": {"01_opening", "02_rank_up_G", "03_zek_intro", "04_rank_up_F",
                     "05_1_lily_experiment", "05_2_zek_steal_bottle"},
        "phase": 2,
        "flags": {}
    })

    # テストケース6: Awakening phase
    test_cases.append({
        "name": "Awakening phase",
        "completed": {"01_opening", "02_rank_up_G", "03_zek_intro", "04_rank_up_F",
                     "05_1_lily_experiment", "05_2_zek_steal_bottle", "06_rank_up_E",
                     "06_2_zek_steal_soulgem", "07_upper_existence"},
        "phase": 3,
        "flags": {}
    })

    # テストケース7: Confrontation phase
    test_cases.append({
        "name": "Confrontation phase",
        "completed": {"01_opening", "02_rank_up_G", "03_zek_intro", "04_rank_up_F",
                     "05_1_lily_experiment", "05_2_zek_steal_bottle", "06_rank_up_E",
                     "06_2_zek_steal_soulgem", "07_upper_existence", "10_rank_up_D",
                     "08_lily_private", "09_balgas_training", "09_rank_up_C", "11_rank_up_B",
                     "12_makuma", "13_makuma2"},
        "phase": 4,
        "flags": {}
    })

    # テストケース8: requiredFlags テスト - vs_astaroth (balgas_battle_complete == 1)
    test_cases.append({
        "name": "After Balgas battle (requiredFlags test)",
        "completed": {"01_opening", "02_rank_up_G", "03_zek_intro", "04_rank_up_F",
                     "05_1_lily_experiment", "05_2_zek_steal_bottle", "06_rank_up_E",
                     "06_2_zek_steal_soulgem", "07_upper_existence", "10_rank_up_D",
                     "08_lily_private", "09_balgas_training", "09_rank_up_C", "11_rank_up_B",
                     "12_makuma", "13_makuma2", "12_rank_up_A", "15_vs_balgas"},
        "phase": 4,
        "flags": {"chitsii.arena.player.balgas_battle_complete": 1}  # フラグが立っている
    })

    # テストケース9: requiredFlags テスト - lily_real_name (lily_hostile != 1)
    test_cases.append({
        "name": "Lily hostile (requiredFlags negative test)",
        "completed": {"01_opening", "02_rank_up_G", "03_zek_intro", "04_rank_up_F",
                     "05_1_lily_experiment", "05_2_zek_steal_bottle", "06_rank_up_E",
                     "06_2_zek_steal_soulgem", "07_upper_existence", "10_rank_up_D",
                     "08_lily_private", "09_balgas_training", "09_rank_up_C", "11_rank_up_B",
                     "12_makuma", "13_makuma2", "12_rank_up_A", "15_vs_balgas_spared"},
        "phase": 4,
        "flags": {"chitsii.arena.player.lily_hostile": 1}  # リリィが敵対 → lily_real_name は利用不可
    })

    print("\n[1] Comparing GetAvailableQuests() output")
    print("-" * 40)

    for tc in test_cases:
        storage = MockFlagStorage(tc["flags"])

        arena_mgr = ArenaQuestManagerCompat(quests, storage, tc["completed"], enum_provider)
        cwl_mgr = CwlQuestManagerCompat(quests, storage, tc["completed"], enum_provider)

        arena_result = arena_mgr.get_available_quests(tc["phase"])
        cwl_result = cwl_mgr.get_available_quests(tc["phase"])

        if arena_result == cwl_result:
            print(f"  [PASS] {tc['name']}")
            print(f"         Available: {arena_result[:3]}{'...' if len(arena_result) > 3 else ''}")
        else:
            all_passed = False
            print(f"  [FAIL] {tc['name']}")
            print(f"         Arena:  {arena_result}")
            print(f"         Cwl:    {cwl_result}")

            # 差分を表示
            arena_set = set(arena_result)
            cwl_set = set(cwl_result)
            if arena_set - cwl_set:
                print(f"         Only in Arena: {arena_set - cwl_set}")
            if cwl_set - arena_set:
                print(f"         Only in Cwl: {cwl_set - arena_set}")

    # Fail-fast テスト（マッピングのない文字列でエラーが発生することを確認）
    print("\n[2] Fail-fast: String flag without mapping raises error")
    print("-" * 40)

    test_flag_condition = FlagCondition(
        flag_key="test.string.flag",
        operator="==",
        value="some_string_without_mapping"  # Enumマッピングなし
    )

    storage = MockFlagStorage({"test.string.flag": 0})
    cwl_checker = CwlQuestConditionChecker(storage, enum_provider)

    try:
        cwl_checker.check_condition(test_flag_condition)
        print(f"  [FAIL] Expected ValueError but no exception was raised")
        all_passed = False
    except ValueError as e:
        print(f"  [PASS] Correctly raises ValueError:")
        print(f"         {e}")

    # 結果サマリー
    print("\n" + "=" * 60)
    if all_passed:
        print("RESULT: ALL TESTS PASSED")
        print("CwlQuestFramework migration is safe for current quest definitions.")
    else:
        print("RESULT: SOME TESTS FAILED")
        print("Please review the differences before migrating.")

    return 0 if all_passed else 1


if __name__ == "__main__":
    sys.exit(run_comparison_tests())
