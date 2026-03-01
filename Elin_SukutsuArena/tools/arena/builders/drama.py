# -*- coding: utf-8 -*-
"""
arena/builders/drama.py - Arena Drama Builder

This module provides:
- DramaBuilder: Base builder with Arena flag validation
- ArenaDramaBuilder: Full Arena-specific drama builder with mixins
- RankSystemMixin: Rank-up system builder
- BattleSystemMixin: Battle stage system builder
"""

import os
import sys
from typing import TYPE_CHECKING, Dict, List, Tuple, Union

# Add cwl_quest_lib to path
_current_dir = os.path.dirname(os.path.abspath(__file__))
_arena_dir = os.path.dirname(_current_dir)
_tools_dir = os.path.dirname(_arena_dir)
if _tools_dir not in sys.path:
    sys.path.insert(0, _tools_dir)

# cwl_quest_lib から基底クラスとヘルパーをインポート
# arena.data からインポート
from arena.data import (
    PREFIX,
    Actors,
    BattleStageDefinition,
    BoolFlag,
    EnumFlag,
    FlagValues,
    IntFlag,
    Keys,
    RankDefinition,
    RankUpTrialFlags,
    SessionKeys,
    StringFlag,
    get_all_enums,
    get_all_flags,
)
from cwl_quest_lib import (
    HEADERS,
    DramaActor,
    DramaLabel,
    GreetingMixin,
    MenuMixin,
    # Mixins
    QuestDispatcherMixin,
    RewardMixin,
)
from cwl_quest_lib import (
    ChoiceReaction as BaseChoiceReaction,
)
from cwl_quest_lib import (
    DramaBuilder as BaseDramaBuilder,
)

if TYPE_CHECKING:
    from arena.data.quests import RankReward, Reward

# Flag validation enabled
FLAG_VALIDATION_ENABLED = True


# ============================================================================
# Flag Validation
# ============================================================================


def _validate_flag(flag_key: str, value) -> list:
    """
    フラグキーと値を検証。
    エラーがあればメッセージのリストを返す。
    """
    if not FLAG_VALIDATION_ENABLED:
        return []

    errors = []

    # プレフィクスを持つフラグのみ検証
    if not flag_key.startswith(PREFIX + "."):
        return []

    # 登録されたフラグか確認
    all_flags = {f.full_key: f for f in get_all_flags()}

    if flag_key not in all_flags:
        errors.append(f"Unknown flag key: '{flag_key}'")
        return errors

    flag_def = all_flags[flag_key]

    # 型チェック
    if isinstance(flag_def, EnumFlag):
        if flag_def.enum_type:
            valid_ordinals = list(range(len(flag_def.enum_type)))
            if isinstance(value, int) and value not in valid_ordinals and value != -1:
                errors.append(
                    f"Invalid ordinal {value} for enum flag '{flag_key}'. "
                    f"Valid: {valid_ordinals} or -1 (null)"
                )
    elif isinstance(flag_def, IntFlag):
        if not isinstance(value, int):
            errors.append(f"Flag '{flag_key}' expects int, got {type(value).__name__}")
        elif hasattr(flag_def, "min_value") and hasattr(flag_def, "max_value"):
            if value < flag_def.min_value or value > flag_def.max_value:
                errors.append(
                    f"Value {value} out of range for '{flag_key}'. "
                    f"Expected: [{flag_def.min_value}, {flag_def.max_value}]"
                )
    elif isinstance(flag_def, BoolFlag):
        if value not in (0, 1, True, False):
            errors.append(f"Flag '{flag_key}' expects bool (0/1), got {value}")

    return errors


# ============================================================================
# ChoiceReaction
# ============================================================================


class ChoiceReaction(BaseChoiceReaction):
    """
    選択肢とその反応を一体化して定義するクラス。
    cwl_quest_lib.ChoiceReaction を継承し、Arena固有の検証を追加。
    """

    pass


# ============================================================================
# DramaBuilder (Base with Arena validation)
# ============================================================================


class DramaBuilder(BaseDramaBuilder):
    """
    CWLドラマファイルを構築するビルダークラス。
    cwl_quest_lib.DramaBuilder を継承し、Arena固有のフラグ検証を追加。
    """

    def __init__(self) -> None:
        super().__init__(mod_name="SukutsuArena")
        self._validation_errors = []

    def _add_validation_error(self, message: str) -> None:
        """検証エラーを追加"""
        self._validation_errors.append(message)
        print(f"[DramaBuilder] ERROR: {message}")

    def get_validation_errors(self) -> list:
        """検証エラーを取得"""
        return self._validation_errors

    def set_flag(self, flag: str, value: int = 1) -> "DramaBuilder":
        """フラグを設定（検証付き）"""
        errors = _validate_flag(flag, value)
        if errors:
            for err in errors:
                self._add_validation_error(f"set_flag: {err}")

        return super().set_flag(flag, value)

    def mod_flag(
        self, flag: str, operator: str, value: int, actor=None
    ) -> "DramaBuilder":
        """フラグを変更 (invoke* mod_flag) - 検証付き"""
        if operator == "=":
            errors = _validate_flag(flag, value)
            if errors:
                for err in errors:
                    self._add_validation_error(f"mod_flag: {err}")

        return super().mod_flag(flag, operator, value, actor)


# ============================================================================
# Arena-specific Mixins
# ============================================================================


class RankSystemMixin:
    """
    ランクアップシステム関連のビルダーメソッド（Arena固有）
    """

    def build_rank_system(
        self,
        ranks: List[RankDefinition],
        actor: Union[str, "DramaActor"],
        fallback_step: Union[str, "DramaLabel"],
        cancel_step: Union[str, "DramaLabel"],
        end_step: Union[str, "DramaLabel"],
    ) -> Dict[str, "DramaLabel"]:
        """
        ランクアップシステム全体を自動生成
        """
        labels = {}
        actor_key = actor.key if isinstance(actor, DramaActor) else actor

        # 各ランクのラベルを先に作成
        for rank_def in ranks:
            r = rank_def.rank.lower()
            labels[f"rank_up_result_{r}"] = self.label(f"rank_up_result_{r}")
            labels[f"rank_up_victory_{r}"] = self.label(f"rank_up_victory_{r}")
            labels[f"rank_up_defeat_{r}"] = self.label(f"rank_up_defeat_{r}")
            labels[f"start_rank_{r}"] = self.label(f"start_rank_{r}")
            labels[f"start_rank_{r}_watch"] = self.label(f"start_rank_{r}_watch")
            labels[f"start_rank_{r}_skip"] = self.label(f"start_rank_{r}_skip")
            labels[f"start_rank_{r}_confirmed"] = self.label(
                f"start_rank_{r}_confirmed"
            )

        # 結果分岐ステップを生成
        for rank_def in ranks:
            r = rank_def.rank.lower()
            result_label = labels[f"rank_up_result_{r}"]
            victory_label = labels[f"rank_up_victory_{r}"]
            defeat_label = labels[f"rank_up_defeat_{r}"]

            self.step(result_label).set_flag(
                RankUpTrialFlags.FLAG_NAME, RankUpTrialFlags.NONE
            ).switch_flag(
                SessionKeys.ARENA_RESULT,
                [
                    fallback_step,
                    victory_label,
                    defeat_label,
                ],
            )

        # 勝利/敗北ステップを追加（外部関数経由）
        for rank_def in ranks:
            r = rank_def.rank.lower()
            if rank_def.result_steps_func:
                rank_def.result_steps_func(
                    self,
                    labels[f"rank_up_victory_{r}"],
                    labels[f"rank_up_defeat_{r}"],
                    fallback_step,
                )

        # 試験開始確認ステップ
        for rank_def in ranks:
            r = rank_def.rank.lower()
            start_label = labels[f"start_rank_{r}"]
            confirmed_label = labels[f"start_rank_{r}_confirmed"]

            self.step(start_label).say(
                f"rank_up_advice_{r}",
                "助言は必要か？",
                "Need advice?",
                "需要建议吗？",
                actor=actor,
            ).choice(
                confirmed_label,
                "助言が欲しい（ドラマを見る）",
                "I want advice (watch drama)",
                "需要建议（观看剧情）",
                text_id=f"c_need_advice_{r}",
            ).choice(
                labels[f"start_rank_{r}_skip"],
                "不要だ（すぐ戦闘）",
                "No (fight now)",
                "不要（直接战斗）",
                text_id=f"c_skip_advice_{r}",
            ).on_cancel(cancel_step)

        # 試験実行ステップ
        for rank_def in ranks:
            r = rank_def.rank.lower()
            confirmed_label = labels[f"start_rank_{r}_confirmed"]

            self.step(confirmed_label).set_flag(
                RankUpTrialFlags.FLAG_NAME, rank_def.trial_flag_value
            )._start_drama(rank_def.drama_name).jump(end_step)

        # スキップして直接戦闘
        for rank_def in ranks:
            r = rank_def.rank.lower()
            skip_label = labels[f"start_rank_{r}_skip"]
            stage_id = f"rank_{r}_trial"

            self.step(skip_label).set_flag(
                RankUpTrialFlags.FLAG_NAME, rank_def.trial_flag_value
            ).start_battle_and_end(stage_id, actor_key)

        return labels


class BattleSystemMixin:
    """
    バトルステージシステム関連のビルダーメソッド（Arena固有）
    """

    def build_battle_stages(
        self,
        stages: List[BattleStageDefinition],
        actor: Union[str, "DramaActor"],
        entry_step: Union[str, "DramaLabel"],
        cancel_step: Union[str, "DramaLabel"],
        stage_flag: str = SessionKeys.ARENA_STAGE,
    ) -> Dict[int, "DramaLabel"]:
        """
        バトルステージシステムを自動生成
        """
        labels = {}

        # ラベル作成
        for stage in stages:
            labels[stage.stage_num] = {
                "prep": self.label(f"stage{stage.stage_num}_prep"),
                "start": self.label(f"battle_start_stage{stage.stage_num}"),
            }

        # エントリーステップ
        current_builder = self.step(entry_step)

        for i, stage in enumerate(stages):
            if stage.next_stage_flag is not None and i < len(stages) - 1:
                next_stage = stages[i + 1]
                current_builder = current_builder.branch_if(
                    stage_flag,
                    ">=",
                    stage.next_stage_flag,
                    labels[next_stage.stage_num]["prep"],
                )

        # 最初のステージのアドバイス
        first_stage = stages[0]
        advice_en = first_stage.advice_en or first_stage.advice
        advice_cn = getattr(first_stage, "advice_cn", "") or ""
        go_button_en = first_stage.go_button_en or first_stage.go_button
        go_button_cn = getattr(first_stage, "go_button_cn", "") or ""
        cancel_button_en = first_stage.cancel_button_en or first_stage.cancel_button
        cancel_button_cn = getattr(first_stage, "cancel_button_cn", "") or ""
        current_builder.say(
            first_stage.advice_id or f"stage{first_stage.stage_num}_advice",
            first_stage.advice,
            advice_en,
            advice_cn,
            actor=actor,
        ).choice(
            labels[first_stage.stage_num]["start"],
            first_stage.go_button,
            go_button_en,
            go_button_cn,
            text_id=f"c_go{first_stage.stage_num}",
        ).choice(
            cancel_step,
            first_stage.cancel_button,
            cancel_button_en,
            cancel_button_cn,
            text_id=f"c_cancel{first_stage.stage_num}",
        ).on_cancel(cancel_step)

        # 各ステージのprepステップ（2番目以降）
        for i, stage in enumerate(stages[1:], start=1):
            prep_label = labels[stage.stage_num]["prep"]
            start_label = labels[stage.stage_num]["start"]

            stage_builder = self.step(prep_label)

            if stage.next_stage_flag is not None and i < len(stages) - 1:
                next_stage = stages[i + 1]
                stage_builder = stage_builder.branch_if(
                    stage_flag,
                    ">=",
                    stage.next_stage_flag,
                    labels[next_stage.stage_num]["prep"],
                )

            advice_en = stage.advice_en or stage.advice
            advice_cn = getattr(stage, "advice_cn", "") or ""
            go_button_en = stage.go_button_en or stage.go_button
            go_button_cn = getattr(stage, "go_button_cn", "") or ""
            cancel_button_en = stage.cancel_button_en or stage.cancel_button
            cancel_button_cn = getattr(stage, "cancel_button_cn", "") or ""
            stage_builder.say(
                stage.advice_id or f"stage{stage.stage_num}_advice",
                stage.advice,
                advice_en,
                advice_cn,
                actor=actor,
            ).choice(
                start_label,
                stage.go_button,
                go_button_en,
                go_button_cn,
                text_id=f"c_go{stage.stage_num}",
            ).choice(
                cancel_step,
                stage.cancel_button,
                cancel_button_en,
                cancel_button_cn,
                text_id=f"c_cancel{stage.stage_num}",
            ).on_cancel(cancel_step)

        # 各ステージのstartステップ
        for stage in stages:
            start_label = labels[stage.stage_num]["start"]

            if stage.sendoff:
                sendoff_en = stage.sendoff_en or stage.sendoff
                sendoff_cn = getattr(stage, "sendoff_cn", "") or ""
                self.step(start_label).say(
                    stage.sendoff_id or f"sendoff{stage.stage_num}",
                    stage.sendoff,
                    sendoff_en,
                    sendoff_cn,
                    actor=actor,
                ).start_battle_and_end(stage.stage_id)
            else:
                self.step(start_label).start_battle_and_end(stage.stage_id)

        return labels


# ============================================================================
# ArenaDramaBuilder (Full builder with all mixins)
# ============================================================================


class ArenaDramaBuilder(
    RankSystemMixin,
    BattleSystemMixin,
    QuestDispatcherMixin,
    GreetingMixin,
    MenuMixin,
    RewardMixin,
    DramaBuilder,
):
    """
    アリーナMod専用の拡張DramaBuilder
    """

    def build_quest_dispatcher(
        self,
        quests,
        entry_step,
        fallback_step,
        actor,
        intro_message: str = "",
        intro_message_en: str = "",
        intro_message_cn: str = "",
        intro_id: str = "",
        target_flag: str = SessionKeys.QUEST_TARGET_NAME,
        found_flag: str = SessionKeys.QUEST_FOUND,
    ):
        """クエストディスパッチャーを構築（Arena固有のフラグ名をデフォルトで使用）"""
        return super().build_quest_dispatcher(
            quests=quests,
            entry_step=entry_step,
            fallback_step=fallback_step,
            actor=actor,
            intro_message=intro_message,
            intro_message_en=intro_message_en,
            intro_message_cn=intro_message_cn,
            intro_id=intro_id,
            target_flag=target_flag,
            found_flag=found_flag,
        )

    def show_rank_info_log(self) -> "ArenaDramaBuilder":
        """ランク情報をログウィンドウに表示する"""
        script = "Elin_SukutsuArena.ArenaManager.ShowRankInfoLog();"
        return self.action("eval", param=script)

    # =========================================================================
    # 低レベルAPI
    # =========================================================================

    def _start_drama(self, drama_name: str) -> "ArenaDramaBuilder":
        """[内部用] 別のドラマを開始する"""
        self.action(
            "eval",
            param=f'UnityEngine.Debug.Log("[SukutsuArena] Pre-invoke StartDrama: {drama_name}");',
        )
        self.action("modInvoke", param=f"start_drama({drama_name})", actor="pc")
        self.action(
            "eval",
            param='UnityEngine.Debug.Log("[SukutsuArena] Post-invoke StartDrama");',
        )
        return self

    def _start_battle_by_stage(
        self,
        stage_id: str,
        master_id: str = None,
        victory_drama_id: str = None,
        defeat_drama_id: str = None,
    ) -> "ArenaDramaBuilder":
        """[内部用] ステージIDを指定して戦闘を開始する

        Args:
            stage_id: ステージID
            master_id: マスターID（省略時はtgを使用）
            victory_drama_id: 勝利時に直接開始するドラマID（NPCダイアログをスキップ）
            defeat_drama_id: 敗北時に直接開始するドラマID（NPCダイアログをスキップ）
        """
        if victory_drama_id or defeat_drama_id:
            # 直接ドラマを指定する場合（最終決戦など）
            v_drama = victory_drama_id or ""
            d_drama = defeat_drama_id or ""
            script = f'Elin_SukutsuArena.ArenaManager.StartBattleByStageWithoutMaster("{stage_id}", "{v_drama}", "{d_drama}");'
        elif master_id:
            script = f'Elin_SukutsuArena.ArenaManager.StartBattleByStage("{stage_id}", "{master_id}");'
        else:
            script = (
                f'Elin_SukutsuArena.ArenaManager.StartBattleByStage("{stage_id}", tg);'
            )
        return self.action("eval", param=script)

    # =========================================================================
    # 高レベルAPI
    # =========================================================================

    def start_quest_drama(self, quest_id: str, drama_name: str) -> "ArenaDramaBuilder":
        """クエストを開始し、対応するドラマを再生して終了"""
        self.start_quest(quest_id)
        self._start_drama(drama_name)
        self.finish()
        return self

    def start_battle_and_end(
        self, stage_id: str, master_id: str = None
    ) -> "ArenaDramaBuilder":
        """バトルを開始してドラマを終了"""
        self._start_battle_by_stage(stage_id, master_id)
        self.finish()
        return self

    def complete_quest_with_rewards(
        self, quest_id: str, flags: Dict[str, int] = None
    ) -> "ArenaDramaBuilder":
        """クエスト完了、フラグ設定を一括処理"""
        self.complete_quest(quest_id)

        if flags:
            for flag_key, value in flags.items():
                self.set_flag(flag_key, value)

        return self

    def dramatic_scene(
        self,
        actor: Union[str, DramaActor],
        lines: List[Tuple[str, str, str]],
        bgm: str = None,
        shake: bool = False,
        focus: bool = True,
    ) -> "ArenaDramaBuilder":
        """演出付きシーンを一括設定"""
        if bgm:
            self.play_bgm(bgm)

        if shake:
            self.shake()

        if focus:
            actor_key = actor.key if isinstance(actor, DramaActor) else actor
            self.focus_chara(actor_key)

        for text_id, text_jp, text_en in lines:
            self.say(text_id, text_jp, text_en, actor=actor)

        return self

    # =========================================================================
    # 後方互換性エイリアス
    # =========================================================================

    def start_drama(self, drama_name: str) -> "ArenaDramaBuilder":
        """[非推奨] _start_drama のエイリアス"""
        return self._start_drama(drama_name)

    def start_battle_by_stage(
        self,
        stage_id: str,
        master_id: str = None,
        victory_drama_id: str = None,
        defeat_drama_id: str = None,
    ) -> "ArenaDramaBuilder":
        """ステージIDを指定して戦闘を開始する

        Args:
            stage_id: ステージID
            master_id: マスターID（省略時はtgを使用）
            victory_drama_id: 勝利時に直接開始するドラマID（NPCダイアログをスキップ）
            defeat_drama_id: 敗北時に直接開始するドラマID（NPCダイアログをスキップ）
        """
        return self._start_battle_by_stage(
            stage_id, master_id, victory_drama_id, defeat_drama_id
        )

    def say_and_start_drama(
        self, message: str, drama_name: str, actor_id: str = "sukutsu_arena_master"
    ) -> "ArenaDramaBuilder":
        """メッセージを表示してからドラマを開始する"""
        script = f'Elin_SukutsuArena.ArenaManager.SayAndStartDrama("{actor_id}", "{message}", "{drama_name}");'
        return self.action("eval", param=script)

    # =========================================================================
    # 報酬システムAPI
    # =========================================================================

    def grant_reward(
        self,
        reward: "Reward",
        actor: Union[str, DramaActor] = None,
        text_id_prefix: str = "reward",
    ) -> "ArenaDramaBuilder":
        """汎用報酬付与API"""
        if reward.message_jp and actor:
            self.say(
                f"{text_id_prefix}_msg",
                reward.message_jp,
                reward.message_en,
                getattr(reward, "message_cn", ""),
                actor=actor,
            )

        if reward.items:
            self._grant_items(reward.items)

        if hasattr(reward, "feat_level") and reward.feat_level > 0:
            script = (
                f"Elin_SukutsuArena.ArenaManager.GrantArenaFeat({reward.feat_level});"
            )
            self.action("eval", param=script)

        self._apply_flags(reward.flags)

        if reward.system_message_jp:
            self.say(
                f"{text_id_prefix}_sys",
                reward.system_message_jp,
                reward.system_message_en,
                getattr(reward, "system_message_cn", ""),
                actor=Actors.NARRATOR,
            )

        return self

    def grant_rank_reward(
        self, rank: str, actor: Union[str, DramaActor] = None, text_id_suffix: str = ""
    ) -> "ArenaDramaBuilder":
        """ランク報酬を付与

        Args:
            rank: ランク名（G, F, E, D, C, B, A）
            actor: 報酬を授与するキャラクター
            text_id_suffix: text_idに追加するサフィックス（重複回避用）
        """
        from arena.data import RANK_REWARDS

        rank_upper = rank.upper()
        if rank_upper not in RANK_REWARDS:
            raise ValueError(f"Unknown rank: {rank}")

        reward = RANK_REWARDS[rank_upper]
        text_id_prefix = f"rup_{rank.lower()}{text_id_suffix}"

        rank_order = ["G", "F", "E", "D", "C", "B", "A"]
        feat_level = rank_order.index(rank_upper) + 1 if rank_upper in rank_order else 0

        if reward.message_jp and actor:
            self.say(
                f"{text_id_prefix}_msg",
                reward.message_jp,
                reward.message_en,
                getattr(reward, "message_cn", ""),
                actor=actor,
            )

        if reward.items:
            self._grant_items(reward.items)

        if reward.system_message_jp:
            self.say(
                f"{text_id_prefix}_sys",
                reward.system_message_jp,
                reward.system_message_en,
                getattr(reward, "system_message_cn", ""),
                actor=Actors.NARRATOR,
            )

        if feat_level > 0:
            script = f"Elin_SukutsuArena.ArenaManager.GrantArenaFeat({feat_level});"
            self.action("eval", param=script)

        return self

    def grant_quest_reward(
        self, quest_key: str, actor: Union[str, DramaActor] = None
    ) -> "ArenaDramaBuilder":
        """クエスト報酬を付与"""
        from arena.data.quests import QUEST_REWARDS

        if quest_key not in QUEST_REWARDS:
            raise ValueError(f"Unknown quest reward key: {quest_key}")

        reward = QUEST_REWARDS[quest_key]
        return self.grant_reward(reward, actor, f"qr_{quest_key}")

    # =========================================================================
    # 内部ヘルパー
    # =========================================================================

    def _grant_items(self, items: list) -> "ArenaDramaBuilder":
        """アイテムリストを付与"""
        item_counts = {}
        for item in items:
            if item.item_id not in item_counts:
                item_counts[item.item_id] = 0
            item_counts[item.item_id] += item.count

        parts = []
        for item_id, count in item_counts.items():
            if count == 1:
                parts.append(f'EClass.pc.Pick(ThingGen.Create("{item_id}"));')
            else:
                parts.append(
                    f'for(int i=0; i<{count}; i++) {{ EClass.pc.Pick(ThingGen.Create("{item_id}")); }}'
                )

        script = " ".join(parts)
        return self.action("eval", param=script)

    def _apply_flags(self, flags: Dict[str, int]) -> "ArenaDramaBuilder":
        """フラグを設定"""
        if not flags:
            return self

        for flag_key, value in flags.items():
            self.set_flag(flag_key, value)

        return self


# ============================================================================
# Re-exports
# ============================================================================

# For backward compatibility
QuestSystemMixin = QuestDispatcherMixin

__all__ = [
    # Core
    "DramaBuilder",
    "ArenaDramaBuilder",
    "DramaLabel",
    "DramaActor",
    "ChoiceReaction",
    "HEADERS",
    # Mixins
    "RankSystemMixin",
    "BattleSystemMixin",
    "QuestDispatcherMixin",
    "GreetingMixin",
    "MenuMixin",
    "RewardMixin",
    "QuestSystemMixin",  # Alias
]
