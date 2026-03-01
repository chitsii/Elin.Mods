"""
アリーナマスター用ドラマスクリプト生成

arena.scenarios の定義を使用してExcelを生成する。
全言語（JP/EN/CN）を1ファイルに出力。CWLが言語設定に応じて適切なカラムを選択。
"""

import os
import sys

# toolsディレクトリをパスに追加してモジュールをインポート可能にする
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
sys.path.insert(0, TOOLS_DIR)

import importlib

# arena パッケージから直接インポート
from arena.builders import ArenaDramaBuilder
from arena.data import DramaIds

# Import scenario modules from arena.scenarios
from arena.scenarios.rank_up import define_rank_up_G
from arena.scenarios.rank_up.rank_f import define_rank_up_F
from arena.scenarios.rank_up.rank_e import define_rank_up_E
from arena.scenarios.rank_up.rank_d import define_rank_up_D
from arena.scenarios.rank_up.rank_c import define_rank_up_C
from arena.scenarios.rank_up.rank_b import define_rank_up_B
from arena.scenarios.rank_up.rank_a import define_rank_up_A

# Import numbered scenario modules using importlib
define_arena_master_drama = importlib.import_module(
    "arena.scenarios.00_arena_master"
).define_arena_master_drama
define_zek_main_drama = importlib.import_module(
    "arena.scenarios.00_zek"
).define_zek_main_drama
define_lily_main_drama = importlib.import_module(
    "arena.scenarios.00_lily"
).define_lily_main_drama
define_astaroth_main_drama = importlib.import_module(
    "arena.scenarios.00_astaroth"
).define_astaroth_main_drama
define_null_main_drama = importlib.import_module(
    "arena.scenarios.00_null"
).define_null_main_drama
define_trainer_main_drama = importlib.import_module(
    "arena.scenarios.00_trainer"
).define_trainer_main_drama
define_iris_sense_training = importlib.import_module(
    "arena.scenarios.iris_sense_training"
).define_iris_sense_training
define_iris_leg_training = importlib.import_module(
    "arena.scenarios.iris_leg_training"
).define_iris_leg_training
define_iris_hotspring = importlib.import_module(
    "arena.scenarios.iris_hotspring"
).define_iris_hotspring
define_zek_intro = importlib.import_module(
    "arena.scenarios.03_zek_intro"
).define_zek_intro
define_lily_experiment = importlib.import_module(
    "arena.scenarios.05_1_lily_experiment"
).define_lily_experiment
define_zek_steal_bottle = importlib.import_module(
    "arena.scenarios.05_2_zek_steal_bottle"
).define_zek_steal_bottle
define_zek_steal_soulgem = importlib.import_module(
    "arena.scenarios.06_2_zek_steal_soulgem"
).define_zek_steal_soulgem
define_upper_existence = importlib.import_module(
    "arena.scenarios.07_upper_existence"
).define_upper_existence
define_lily_private = importlib.import_module(
    "arena.scenarios.08_lily_private"
).define_lily_private
define_balgas_training = importlib.import_module(
    "arena.scenarios.09_balgas_training"
).define_balgas_training
define_makuma = importlib.import_module("arena.scenarios.12_makuma").define_makuma
define_makuma2 = importlib.import_module("arena.scenarios.13_makuma2").define_makuma2
define_vs_balgas = importlib.import_module(
    "arena.scenarios.15_vs_balgas"
).define_vs_balgas
define_lily_real_name = importlib.import_module(
    "arena.scenarios.16_lily_real_name"
).define_lily_real_name
define_vs_astaroth = importlib.import_module(
    "arena.scenarios.17_vs_astaroth"
).define_vs_astaroth
define_last_battle = importlib.import_module(
    "arena.scenarios.18_last_battle"
).define_last_battle
define_epilogue = importlib.import_module("arena.scenarios.19_epilogue").define_epilogue
define_opening_drama = importlib.import_module(
    "arena.scenarios.01_opening"
).define_opening_drama
define_debug_menu = importlib.import_module(
    "arena.scenarios.99_debug_menu"
).define_debug_menu

# Part 2 scenarios
define_resurrection_intro_drama = importlib.import_module(
    "arena.scenarios.p2_02a_resurrection_intro"
).define_resurrection_intro_drama

define_resurrection_execution_drama = importlib.import_module(
    "arena.scenarios.p2_02b_resurrection_execution"
).define_resurrection_execution_drama
define_scroll_showcase = importlib.import_module(
    "arena.scenarios.p2_03_scroll_showcase"
).define_scroll_showcase

PROJECT_ROOT = os.path.dirname(TOOLS_DIR)


def process_scenario(output_dir, drama_id, define_func, sheet_name=None):
    """シナリオを処理してExcelを生成（全言語を1ファイルに出力）"""
    if sheet_name is None:
        sheet_name = drama_id

    output_path = os.path.join(output_dir, f"drama_{drama_id}.xlsx")
    builder = ArenaDramaBuilder()
    define_func(builder)
    builder.save(output_path, sheet_name=sheet_name)


def process_npc_drama(output_dir, drama_id, define_func, sheet_name):
    """NPC用ドラマを処理してExcelを生成（全言語を1ファイルに出力）"""
    output_path = os.path.join(output_dir, f"drama_{drama_id}.xlsx")
    builder = ArenaDramaBuilder()
    define_func(builder)
    builder.save(output_path, sheet_name=sheet_name)


def main():
    output_dir = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Dialog", "Drama")
    os.makedirs(output_dir, exist_ok=True)

    # Sukutsu Arena Master
    process_npc_drama(output_dir, "sukutsu_arena_master",
                      define_arena_master_drama, "sukutsu_arena_master")

    # Sukutsu Shady Merchant (Zek)
    process_npc_drama(output_dir, "sukutsu_shady_merchant",
                      define_zek_main_drama, "sukutsu_shady_merchant")

    # Sukutsu Receptionist (Lily)
    process_npc_drama(output_dir, "sukutsu_receptionist",
                      define_lily_main_drama, "sukutsu_receptionist")

    # Sukutsu Astaroth
    process_npc_drama(output_dir, "sukutsu_astaroth",
                      define_astaroth_main_drama, "sukutsu_astaroth")

    # Sukutsu Null
    process_npc_drama(output_dir, "sukutsu_null",
                      define_null_main_drama, "sukutsu_null")

    # Sukutsu Trainer (Iris)
    process_npc_drama(output_dir, "sukutsu_trainer",
                      define_trainer_main_drama, "sukutsu_trainer")

    # --- Rank Up Trials ---
    process_scenario(output_dir, DramaIds.RANK_UP_G, define_rank_up_G)
    process_scenario(output_dir, DramaIds.RANK_UP_F, define_rank_up_F)
    process_scenario(output_dir, DramaIds.RANK_UP_E, define_rank_up_E)
    process_scenario(output_dir, DramaIds.RANK_UP_D, define_rank_up_D)
    process_scenario(output_dir, DramaIds.RANK_UP_C, define_rank_up_C)
    process_scenario(output_dir, DramaIds.RANK_UP_B, define_rank_up_B)
    process_scenario(output_dir, DramaIds.RANK_UP_A, define_rank_up_A)

    # --- Opening ---
    process_scenario(output_dir, DramaIds.SUKUTSU_OPENING, define_opening_drama)

    # --- Story Events ---
    process_scenario(output_dir, DramaIds.ZEK_INTRO, define_zek_intro)
    process_scenario(output_dir, DramaIds.LILY_EXPERIMENT, define_lily_experiment)
    process_scenario(output_dir, DramaIds.ZEK_STEAL_BOTTLE, define_zek_steal_bottle)
    process_scenario(output_dir, DramaIds.ZEK_STEAL_SOULGEM, define_zek_steal_soulgem)
    process_scenario(output_dir, DramaIds.UPPER_EXISTENCE, define_upper_existence)
    process_scenario(output_dir, DramaIds.LILY_PRIVATE, define_lily_private)
    process_scenario(output_dir, DramaIds.BALGAS_TRAINING, define_balgas_training)
    process_scenario(output_dir, DramaIds.IRIS_SENSE_TRAINING, define_iris_sense_training)
    process_scenario(output_dir, DramaIds.IRIS_LEG_TRAINING, define_iris_leg_training)
    process_scenario(output_dir, DramaIds.IRIS_HOTSPRING, define_iris_hotspring)
    process_scenario(output_dir, DramaIds.MAKUMA, define_makuma)
    process_scenario(output_dir, DramaIds.MAKUMA2, define_makuma2)
    process_scenario(output_dir, DramaIds.VS_BALGAS, define_vs_balgas)
    process_scenario(output_dir, DramaIds.LILY_REAL_NAME, define_lily_real_name)
    process_scenario(output_dir, DramaIds.VS_ASTAROTH, define_vs_astaroth)
    process_scenario(output_dir, DramaIds.LAST_BATTLE, define_last_battle)
    process_scenario(output_dir, DramaIds.EPILOGUE, define_epilogue)

    # --- Part 2 ---
    process_scenario(output_dir, DramaIds.RESURRECTION_INTRO, define_resurrection_intro_drama)
    process_scenario(output_dir, DramaIds.RESURRECTION_EXECUTION, define_resurrection_execution_drama)
    process_scenario(output_dir, DramaIds.P2_03_SCROLL_SHOWCASE, define_scroll_showcase)

    # --- Debug Menu ---
    process_scenario(output_dir, DramaIds.DEBUG_MENU, define_debug_menu)

    print("\n[INFO] Drama Excel generation successful (all languages in single file).")


if __name__ == "__main__":
    main()
