# -*- coding: utf-8 -*-
"""
arena/data/types.py - Arena-specific Data Types

This module defines arena-specific data classes used throughout the arena drama system.
Generic types are imported from cwl_quest_lib.types for backward compatibility.

Arena-specific Data Classes:
- RankDefinition: Rank-up trial definition
- BattleStageDefinition: Battle stage definition

Generic Data Classes (re-exported from cwl_quest_lib):
- GreetingDefinition: Flag-based greeting definition
- QuestEntry: Quest dispatcher entry
- MenuItem: Menu item definition
- QuestInfoDefinition: Quest info display definition
- QuestStartDefinition: Quest start definition
"""

import os
import sys
from dataclasses import dataclass, field
from typing import TYPE_CHECKING, Callable, List, Optional, Union

# Add cwl_quest_lib to path
_current_dir = os.path.dirname(os.path.abspath(__file__))
_arena_dir = os.path.dirname(_current_dir)
_tools_dir = os.path.dirname(_arena_dir)
if _tools_dir not in sys.path:
    sys.path.insert(0, _tools_dir)

# 汎用型を cwl_quest_lib から再エクスポート（後方互換性のため）
from cwl_quest_lib import (
    GreetingDefinition,
    ItemDefinition,
    MenuItem,
    NpcDefinition,
    QuestEntry,
    QuestInfoDefinition,
    QuestStartDefinition,
    Reward,
    RewardItem,
)

if TYPE_CHECKING:
    from arena.builders.drama import DramaLabel


# ============================================================================
# Arena-specific Data Classes
# ============================================================================


@dataclass
class RankDefinition:
    """
    ランクアップ試験の定義（Arena固有）

    これ1つで以下のステップを自動生成:
    - rank_up_result_{rank}: 結果チェック（勝利/敗北分岐）
    - rank_up_victory_{rank}: 勝利処理
    - rank_up_defeat_{rank}: 敗北処理
    - start_rank_{rank}: 試験開始確認
    - start_rank_{rank}_confirmed: 試験実行
    """

    rank: str  # "g", "f", "e" など（小文字）
    quest_id: str  # QuestIds.RANK_UP_G
    drama_name: str  # DramaNames.RANK_UP_G
    confirm_msg: str  # 確認メッセージ（JP）
    confirm_msg_en: str = ""  # 確認メッセージ（EN）
    confirm_msg_cn: str = ""  # 確認メッセージ（CN）
    confirm_button: str = "挑む"  # 確認ボタンテキスト（JP）
    confirm_button_en: str = ""  # 確認ボタンテキスト（EN）
    confirm_button_cn: str = ""  # 確認ボタンテキスト（CN）
    trial_flag_value: int = 0  # sukutsu_rank_up_trial の値
    quest_flag_value: int = 0  # sukutsu_quest_target_name の値
    # 外部で定義された勝利/敗北ステップ追加関数
    result_steps_func: Optional[Callable] = None


@dataclass
class BattleStageDefinition:
    """
    バトルステージの定義（Arena固有）

    Example:
        BattleStageDefinition(
            stage_num=1,
            stage_id="stage_1",
            advice="お前の最初の相手は...",
            advice_en="Your first opponent is...",
            advice_cn="你的第一个对手是...",
            sendoff="よし、行け！",
            sendoff_en="Alright, go!",
            sendoff_cn="好，去吧！",
            go_button="準備できた、行く！",
            go_button_en="Ready, let's go!",
            go_button_cn="准备好了，出发！",
            cancel_button="もう少し準備してくる",
            cancel_button_en="I need more time to prepare",
            cancel_button_cn="我还需要再准备一下",
            next_stage_flag=2,  # この値以上なら次ステージへ
        )
    """

    stage_num: int  # ステージ番号 (1, 2, 3, 4)
    stage_id: str  # ステージID ("stage_1", "stage_2", etc.)
    advice: str  # 戦闘前アドバイス（JP）
    advice_en: str = ""  # 戦闘前アドバイス（EN）
    advice_cn: str = ""  # 戦闘前アドバイス（CN）
    advice_id: str = ""  # アドバイスのtext_id
    sendoff: str = ""  # 送り出しメッセージ（JP）
    sendoff_en: str = ""  # 送り出しメッセージ（EN）
    sendoff_cn: str = ""  # 送り出しメッセージ（CN）
    sendoff_id: str = ""  # 送り出しのtext_id
    go_button: str = "準備できた！"  # 開始ボタン（JP）
    go_button_en: str = ""  # 開始ボタン（EN）
    go_button_cn: str = ""  # 開始ボタン（CN）
    cancel_button: str = "待ってくれ"  # キャンセルボタン（JP）
    cancel_button_en: str = ""  # キャンセルボタン（EN）
    cancel_button_cn: str = ""  # キャンセルボタン（CN）
    next_stage_flag: Optional[int] = None  # この値以上なら次ステージにスキップ


# ============================================================================
# Re-export all for backward compatibility
# ============================================================================

__all__ = [
    # Arena-specific
    "RankDefinition",
    "BattleStageDefinition",
    # Generic (from cwl_quest_lib)
    "GreetingDefinition",
    "QuestEntry",
    "MenuItem",
    "QuestInfoDefinition",
    "QuestStartDefinition",
    "RewardItem",
    "Reward",
    "NpcDefinition",
    "ItemDefinition",
]
