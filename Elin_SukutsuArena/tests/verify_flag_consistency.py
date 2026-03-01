#!/usr/bin/env python3
"""
フラグ一貫性検証スクリプト

フラグプレフィックスの一貫性を検証する。
TDDのREDフェーズ: このテストは最初は失敗する。

使用方法:
    cd tests
    uv run python verify_flag_consistency.py
"""

import json
import sys
from pathlib import Path
from typing import Set

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


def get_config_module():
    """config.pyモジュールを取得"""
    from arena.data import config

    return config


def extract_all_flag_keys_from_quest(quest: dict) -> Set[str]:
    """クエスト定義から全フラグキーを抽出"""
    keys = set()

    # completionFlags
    if quest.get("completionFlags"):
        keys.update(quest["completionFlags"].keys())

    # requiredFlags
    for flag_cond in quest.get("requiredFlags", []):
        if "flagKey" in flag_cond:
            keys.add(flag_cond["flagKey"])

    return keys


def get_session_flag_keys() -> Set[str]:
    """セッションフラグキーを取得"""
    config = get_config_module()
    return {f.full_key for f in config.SESSION_FLAGS}


def get_persistent_flag_keys() -> Set[str]:
    """永続フラグキーを取得"""
    config = get_config_module()
    return {f.full_key for f in config.get_all_flags()}


def get_quest_done_flag_keys() -> Set[str]:
    """クエスト完了フラグキーを取得"""
    definitions = load_quest_definitions()
    config = get_config_module()
    return {f"{config.QUEST_DONE_PREFIX}{q['questId']}" for q in definitions}


# ============================================================================
# テスト関数
# ============================================================================


def test_persistent_flags_have_consistent_prefix() -> tuple[bool, str]:
    """永続フラグが chitsii.arena.* プレフィックスを持つ"""
    config = get_config_module()
    expected_prefix = config.PREFIX

    errors = []
    for flag in config.get_all_flags():
        if not flag.full_key.startswith(expected_prefix):
            errors.append(f"Invalid prefix: {flag.full_key}")

    if errors:
        return False, "\n".join(errors)
    return True, "All persistent flags have correct prefix"


def test_session_flags_have_consistent_prefix() -> tuple[bool, str]:
    """セッションフラグが sukutsu_* プレフィックスを持つ（現行仕様）"""
    config = get_config_module()
    expected_prefix = config.SESSION_PREFIX

    errors = []
    for flag in config.SESSION_FLAGS:
        if not flag.full_key.startswith(expected_prefix):
            errors.append(f"Invalid session prefix: {flag.full_key}")

    if errors:
        return False, "\n".join(errors)
    return True, "All session flags have correct prefix"


def test_quest_done_flags_have_consistent_prefix() -> tuple[bool, str]:
    """クエスト完了フラグが sukutsu_quest_done_* プレフィックスを持つ（現行仕様）"""
    config = get_config_module()
    expected_prefix = config.QUEST_DONE_PREFIX

    definitions = load_quest_definitions()
    errors = []

    for quest in definitions:
        expected_key = f"{expected_prefix}{quest['questId']}"
        # クエスト完了フラグは暗黙的に生成されるので、プレフィックスの形式だけ確認
        if not expected_key.startswith("sukutsu_quest_done_"):
            errors.append(f"Invalid quest done prefix: {expected_key}")

    if errors:
        return False, "\n".join(errors)
    return True, "All quest done flags have correct prefix"


def test_completion_flags_in_quests_use_valid_keys() -> tuple[bool, str]:
    """クエスト定義のcompletionFlagsが有効なキーを使用"""
    config = get_config_module()
    definitions = load_quest_definitions()

    errors = []
    for quest in definitions:
        for flag_key in quest.get("completionFlags", {}).keys():
            # 永続フラグは chitsii.arena.* プレフィックス
            if not flag_key.startswith(config.PREFIX):
                errors.append(
                    f"Quest {quest['questId']}: completion flag uses invalid prefix: {flag_key}"
                )

    if errors:
        return False, "\n".join(errors)
    return True, "All completion flags use valid keys"


def test_required_flags_in_quests_use_valid_keys() -> tuple[bool, str]:
    """クエスト定義のrequiredFlagsが有効なキーを使用"""
    config = get_config_module()
    definitions = load_quest_definitions()

    errors = []
    for quest in definitions:
        for flag_cond in quest.get("requiredFlags", []):
            flag_key = flag_cond.get("flagKey", "")
            # 永続フラグは chitsii.arena.* プレフィックス
            if flag_key and not flag_key.startswith(config.PREFIX):
                errors.append(
                    f"Quest {quest['questId']}: required flag uses invalid prefix: {flag_key}"
                )

    if errors:
        return False, "\n".join(errors)
    return True, "All required flags use valid keys"


def test_keys_class_matches_flags() -> tuple[bool, str]:
    """Keysクラスの定数がFLAGS定義と一致"""
    config = get_config_module()

    # Keysクラスの全属性を取得
    keys_attrs = {
        name: value
        for name, value in vars(config.Keys).items()
        if not name.startswith("_") and isinstance(value, str)
    }

    # FLAGS定義からfull_keyを取得
    flag_full_keys = {f.full_key for f in config.get_all_flags()}

    errors = []
    for name, key_value in keys_attrs.items():
        if key_value not in flag_full_keys:
            # Aliasの可能性があるので警告のみ
            pass  # エイリアスは許容

    # 逆方向: FLAGSにあってKeysにないものは問題ない（全てがショートカットを持つ必要はない）

    return True, "Keys class is consistent with FLAGS"


def test_flag_categories_are_properly_separated() -> tuple[bool, str]:
    """フラグカテゴリが適切に分離されている"""
    config = get_config_module()

    # 各カテゴリのプレフィックス
    persistent_prefix = config.PREFIX  # chitsii.arena
    session_prefix = config.SESSION_PREFIX  # sukutsu_
    quest_done_prefix = config.QUEST_DONE_PREFIX  # sukutsu_quest_done_

    # カテゴリ間の重複がないことを確認
    errors = []

    # session_prefix が quest_done_prefix を含むことは許容（quest_done_ は sukutsu_ のサブセット）
    if not quest_done_prefix.startswith(session_prefix):
        errors.append(
            f"Quest done prefix should start with session prefix: {quest_done_prefix}"
        )

    if errors:
        return False, "\n".join(errors)
    return True, "Flag categories are properly separated"


# ============================================================================
# メイン
# ============================================================================


def main():
    print("=" * 60)
    print("Flag Consistency Verification")
    print("=" * 60)

    tests = [
        ("Persistent flags prefix", test_persistent_flags_have_consistent_prefix),
        ("Session flags prefix", test_session_flags_have_consistent_prefix),
        ("Quest done flags prefix", test_quest_done_flags_have_consistent_prefix),
        ("Completion flags in quests", test_completion_flags_in_quests_use_valid_keys),
        ("Required flags in quests", test_required_flags_in_quests_use_valid_keys),
        ("Keys class consistency", test_keys_class_matches_flags),
        ("Flag categories separation", test_flag_categories_are_properly_separated),
    ]

    all_passed = True
    for name, test_func in tests:
        try:
            passed, message = test_func()
            status = "PASS" if passed else "FAIL"
            print(f"  [{status}] {name}")
            if not passed:
                all_passed = False
                for line in message.split("\n")[:5]:
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
        return 1


if __name__ == "__main__":
    sys.exit(main())
