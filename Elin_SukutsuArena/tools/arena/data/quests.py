# -*- coding: utf-8 -*-
"""
arena/data/quests.py - Quest and Reward Definitions

This module defines all quests and rewards for the Sukutsu Arena mod.
"""

from typing import Dict, List

from arena.data.config import (
    PREFIX,
    FlagCondition,
    Keys,
    QuestDef,
    QuestIds,
    QuestType,
    RewardDef,
    RewardItemDef,
)

# ============================================================================
# Phases
# ============================================================================

PHASES = ["PROLOGUE", "INITIATION", "RISING", "AWAKENING", "CONFRONTATION", "CLIMAX", "EPILOGUE", "POSTGAME"]


# ============================================================================
# Quests
# ============================================================================

QUESTS = [
    # === Main Story - Opening ===
    QuestDef(
        quest_id=QuestIds.OPENING,
        quest_type=QuestType.MAIN_STORY,
        drama_id="sukutsu_opening",
        name_jp="異次元の闘技場への到着",
        name_en="Arrival at the Dimensional Arena",
        description="プレイヤーがアリーナに到着し、リリィとバルガスに出会う",
        phase="PROLOGUE",
        auto_trigger=True,
        completion_flags={f"{PREFIX}.player.rank": "unranked"},
        priority=1000,
    ),
    # === Rank Up Quests ===
    QuestDef(
        quest_id=QuestIds.RANK_UP_G,
        quest_type=QuestType.RANK_UP,
        drama_id="rank_up_G",
        name_jp="ランクG昇格試合（屑肉の洗礼）",
        name_en="Rank G Promotion Trial (Baptism of Scraps)",
        description="最初の試練を突破し、ランクGを獲得する",
        phase="PROLOGUE",
        quest_giver="sukutsu_arena_master",
        advances_phase="INITIATION",
        required_quests=[QuestIds.OPENING],
        priority=950,
    ),
    QuestDef(
        quest_id=QuestIds.RANK_UP_F,
        quest_type=QuestType.RANK_UP,
        drama_id="rank_up_F",
        name_jp="ランクF昇格試合（ヴォイド・アイスハウンド）",
        name_en="Rank F Promotion Trial (Void Ice Hound)",
        description="ヴォイド・アイスハウンドを倒し、ランクFを獲得する",
        phase="INITIATION",
        quest_giver="sukutsu_arena_master",
        advances_phase="RISING",
        required_quests=[QuestIds.RANK_UP_G, QuestIds.ZEK_INTRO],
        priority=900,
    ),
    QuestDef(
        quest_id=QuestIds.RANK_UP_E,
        quest_type=QuestType.RANK_UP,
        drama_id="rank_up_E",
        name_jp="ランクE昇格試合（錆びついた英雄カイン）",
        name_en="Rank E Promotion Trial (Rusted Hero Cain)",
        description="錆びついた英雄カインを倒し、ランクEを獲得する",
        phase="RISING",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.RANK_UP_F],
        priority=900,
    ),
    QuestDef(
        quest_id=QuestIds.RANK_UP_D,
        quest_type=QuestType.RANK_UP,
        drama_id="rank_up_D",
        name_jp="ランクD昇格試合（銅貨稼ぎの洗礼）",
        name_en="Rank D Promotion Trial (Copper Earner's Baptism)",
        description="観客の介入を避けながら戦い、ランクDを獲得する",
        phase="RISING",
        quest_giver="sukutsu_arena_master",
        advances_phase="AWAKENING",
        required_quests=[QuestIds.UPPER_EXISTENCE],
        priority=900,
    ),
    QuestDef(
        quest_id=QuestIds.RANK_UP_C,
        quest_type=QuestType.RANK_UP,
        drama_id="rank_up_C",
        name_jp="ランクC昇格試合（朱砂食い）",
        name_en="Rank C Promotion Trial (Cinnabar Eater)",
        description="堕ちた英雄たちを解放し、ランクCを獲得する",
        phase="AWAKENING",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.BALGAS_TRAINING],
        priority=900,
    ),
    QuestDef(
        quest_id=QuestIds.RANK_UP_B,
        quest_type=QuestType.RANK_UP,
        drama_id="rank_up_B",
        name_jp="ランクB昇格試合（虚無の処刑人ヌル）",
        name_en="Rank B Promotion Trial (Null the Void Executioner)",
        description="虚無の処刑人ヌルを倒し、ランクBを獲得する",
        phase="AWAKENING",
        quest_giver="sukutsu_arena_master",
        advances_phase="CONFRONTATION",
        required_quests=[QuestIds.RANK_UP_C],
        priority=900,
    ),
    QuestDef(
        quest_id=QuestIds.RANK_UP_A,
        quest_type=QuestType.RANK_UP,
        drama_id="rank_up_A",
        name_jp="ランクA昇格試合（影との戦い）",
        name_en="Rank A Promotion Trial (Shadow Battle)",
        description="自分の影（第二のヌル）を倒し、ランクAを獲得する",
        phase="CONFRONTATION",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.RANK_UP_B],
        priority=900,
    ),
    # Rank S - Parent quest
    QuestDef(
        quest_id=QuestIds.RANK_UP_S,
        quest_type=QuestType.RANK_UP,
        drama_id="vs_balgas",
        name_jp="Rank S 昇格試合『竜断ちへの道』",
        name_en="Rank S Trial: Path to Dragon Slayer",
        description="全盛期のバルガスとの最終決戦",
        phase="CONFRONTATION",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.RANK_UP_A],
        priority=910,
    ),
    # Rank S - Branch: Spared
    QuestDef(
        quest_id=QuestIds.RANK_UP_S_BALGAS_SPARED,
        quest_type=QuestType.RANK_UP,
        drama_id="vs_balgas",
        name_jp="ランクS昇格：バルガスを見逃す",
        name_en="Rank S: Spare Vargus",
        description="バルガスを見逃し、慈悲の道を選んだ",
        phase="CONFRONTATION",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.RANK_UP_A, QuestIds.RANK_UP_S],
        blocks_quests=[QuestIds.RANK_UP_S_BALGAS_KILLED],
        priority=900,
    ),
    # Rank S - Branch: Killed
    QuestDef(
        quest_id=QuestIds.RANK_UP_S_BALGAS_KILLED,
        quest_type=QuestType.RANK_UP,
        drama_id="vs_balgas",
        name_jp="ランクS昇格：バルガスを殺す",
        name_en="Rank S: Kill Vargus",
        description="観客の命令に従い、バルガスを殺した",
        phase="CONFRONTATION",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.RANK_UP_A, QuestIds.RANK_UP_S],
        blocks_quests=[QuestIds.RANK_UP_S_BALGAS_SPARED, QuestIds.LILY_PRIVATE],
        priority=900,
    ),
    # === Character Events / Side Quests ===
    QuestDef(
        quest_id=QuestIds.ZEK_INTRO,
        quest_type=QuestType.CHARACTER_EVENT,
        drama_id="zek_intro",
        name_jp="影歩きの邂逅",
        name_en="Encounter with the Shadow Walker",
        description="商人ゼクとの初遭遇",
        phase="INITIATION",
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.RANK_UP_G],
        priority=800,
    ),
    QuestDef(
        quest_id=QuestIds.LILY_EXPERIMENT,
        quest_type=QuestType.SIDE_QUEST,
        drama_id="lily_experiment",
        name_jp="リリィの私的依頼『残響の器』",
        name_en="Lily's Private Request: Vessel of Echoes",
        description="リリィのために死の共鳴瓶を製作する",
        phase="INITIATION",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.RANK_UP_F],
        priority=700,
    ),
    # Bottle swap - Parent
    QuestDef(
        quest_id=QuestIds.ZEK_STEAL_BOTTLE,
        quest_type=QuestType.SIDE_QUEST,
        drama_id="zek_steal_bottle",
        name_jp="ゼクの器すり替え",
        name_en="Zek's Bottle Swap",
        description="ゼクが共鳴瓶のすり替えを提案してくる",
        phase="INITIATION",
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.LILY_EXPERIMENT],
        priority=710,
    ),
    # Bottle swap - Branches
    QuestDef(
        quest_id=QuestIds.ZEK_STEAL_BOTTLE_ACCEPT,
        quest_type=QuestType.SIDE_QUEST,
        drama_id="zek_steal_bottle",
        name_jp="ゼクの器すり替え：応諾",
        name_en="Zek's Bottle Swap: Accepted",
        description="ゼクの提案に応じて共鳴瓶をすり替えた",
        phase="INITIATION",
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.LILY_EXPERIMENT, QuestIds.ZEK_STEAL_BOTTLE],
        blocks_quests=[QuestIds.ZEK_STEAL_BOTTLE_REFUSE],
        priority=700,
    ),
    QuestDef(
        quest_id=QuestIds.ZEK_STEAL_BOTTLE_REFUSE,
        quest_type=QuestType.SIDE_QUEST,
        drama_id="zek_steal_bottle",
        name_jp="ゼクの器すり替え：拒否",
        name_en="Zek's Bottle Swap: Refused",
        description="ゼクの提案を断り、正直にリリィに渡した",
        phase="INITIATION",
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.LILY_EXPERIMENT, QuestIds.ZEK_STEAL_BOTTLE],
        blocks_quests=[QuestIds.ZEK_STEAL_BOTTLE_ACCEPT],
        priority=700,
    ),
    # Kain soul - Parent
    QuestDef(
        quest_id=QuestIds.ZEK_STEAL_SOULGEM,
        quest_type=QuestType.SIDE_QUEST,
        drama_id="zek_steal_soulgem",
        name_jp="カインの魂の行方",
        name_en="Fate of Cain's Soul",
        description="ゼクがカインの魂の取引を持ちかけてくる",
        phase="RISING",
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.RANK_UP_E],
        priority=860,
    ),
    # Kain soul - Branches
    QuestDef(
        quest_id=QuestIds.ZEK_STEAL_SOULGEM_SELL,
        quest_type=QuestType.SIDE_QUEST,
        drama_id="zek_steal_soulgem",
        name_jp="カインの魂：売却",
        name_en="Cain's Soul: Sold",
        description="カインの魂をゼクに売った",
        phase="RISING",
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.RANK_UP_E, QuestIds.ZEK_STEAL_SOULGEM],
        blocks_quests=[QuestIds.ZEK_STEAL_SOULGEM_RETURN],
        priority=850,
    ),
    QuestDef(
        quest_id=QuestIds.ZEK_STEAL_SOULGEM_RETURN,
        quest_type=QuestType.SIDE_QUEST,
        drama_id="zek_steal_soulgem",
        name_jp="カインの魂：返還",
        name_en="Cain's Soul: Returned",
        description="カインの魂をバルガスに返した",
        phase="RISING",
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.RANK_UP_E, QuestIds.ZEK_STEAL_SOULGEM],
        blocks_quests=[QuestIds.ZEK_STEAL_SOULGEM_SELL],
        priority=850,
    ),
    QuestDef(
        quest_id=QuestIds.UPPER_EXISTENCE,
        quest_type=QuestType.MAIN_STORY,
        drama_id="upper_existence",
        name_jp="高次元存在の真実",
        name_en="Truth of Higher Dimensional Beings",
        description="観客の正体と闘技場の真実が明らかになる",
        phase="RISING",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.RANK_UP_E],
        priority=899,
    ),
    QuestDef(
        quest_id=QuestIds.LILY_PRIVATE,
        quest_type=QuestType.CHARACTER_EVENT,
        drama_id="lily_private",
        name_jp="リリィの私室招待",
        name_en="Invitation to Lily's Private Room",
        description="リリィの私室に招待される",
        phase="AWAKENING",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.RANK_UP_D],
        priority=600,
    ),
    QuestDef(
        quest_id=QuestIds.BALGAS_TRAINING,
        quest_type=QuestType.CHARACTER_EVENT,
        drama_id="balgas_training",
        name_jp="戦士の哲学：鉄を打つ鉄",
        name_en="Warrior's Philosophy: Iron Forges Iron",
        description="バルガスから戦士の哲学を学ぶ特別訓練",
        phase="AWAKENING",
        quest_giver="sukutsu_arena_master",
        required_quests=[QuestIds.RANK_UP_D],
        priority=899,
    ),
    QuestDef(
        quest_id=QuestIds.MAKUMA,
        quest_type=QuestType.MAIN_STORY,
        drama_id="makuma",
        name_jp="ヌルの記憶チップとリリィの衣装",
        name_en="Null's Memory Chip and Lily's Outfit",
        description="ランクB達成報酬として特別な衣装を授与され、ゼクがヌルの真実を暴露する",
        phase="CONFRONTATION",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.RANK_UP_B],
        priority=850,
    ),
    QuestDef(
        quest_id=QuestIds.MAKUMA2,
        quest_type=QuestType.MAIN_STORY,
        drama_id="makuma2",
        name_jp="虚空の心臓製作【複数分岐統合】",
        name_en="Void Core Crafting [Multiple Branch Convergence]",
        description="虚空の心臓を製作し、過去の選択の清算が行われる",
        phase="CONFRONTATION",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.MAKUMA],
        priority=850,
    ),
    QuestDef(
        quest_id=QuestIds.LILY_REAL_NAME,
        quest_type=QuestType.CHARACTER_EVENT,
        drama_id="lily_real_name",
        name_jp="リリィの真名告白",
        name_en="Lily's True Name Revelation",
        description="リリィが真名『リリシエル』を明かす",
        phase="CONFRONTATION",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.RANK_UP_S_BALGAS_SPARED],
        priority=600,
    ),
    # === Final Chapter ===
    QuestDef(
        quest_id=QuestIds.VS_ASTAROTH,
        quest_type=QuestType.MAIN_STORY,
        drama_id="vs_astaroth",
        name_jp="アスタロト初遭遇、ゼクによる救出",
        name_en="First Astaroth Encounter, Zek's Rescue",
        description="アスタロトが降臨し、ゼクが介入して救出する",
        phase="CONFRONTATION",
        auto_trigger=True,
        advances_phase="CLIMAX",
        required_flags=[
            FlagCondition(
                flag_key=f"{PREFIX}.player.balgas_battle_complete",
                operator="==",
                value=1,
            )
        ],
        required_quests=[QuestIds.RANK_UP_A],
        priority=900,
    ),
    QuestDef(
        quest_id=QuestIds.LAST_BATTLE,
        quest_type=QuestType.ENDING,
        drama_id="last_battle",
        name_jp="最終決戦：アスタロト撃破",
        name_en="Final Battle: Defeat Astaroth",
        description="アスタロトとの最終決戦、複数のエンディング分岐",
        phase="CLIMAX",
        advances_phase="POSTGAME",  # エピローグ完了後にPOSTGAMEクエストが利用可能に
        quest_giver="sukutsu_shady_merchant",
        required_quests=[QuestIds.VS_ASTAROTH],
        priority=500,
    ),
    # === Postgame ===
    QuestDef(
        quest_id=QuestIds.PG_02A_RESURRECTION_INTRO,
        quest_type=QuestType.POSTGAME,
        drama_id="resurrection_intro",
        name_jp="蘇りの儀式・導入",
        name_en="The Resurrection Ritual - Preparation",
        description="バルガスとカインを蘇らせるための儀式について説明を受ける。",
        phase="POSTGAME",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.LAST_BATTLE],
        priority=590,
    ),

    QuestDef(
        quest_id=QuestIds.PG_02B_RESURRECTION_EXECUTION,
        quest_type=QuestType.POSTGAME,
        drama_id="resurrection_execution",
        name_jp="蘇りの儀式・実行",
        name_en="The Resurrection Ritual - Execution",
        description="素材を集めて蘇りの儀式を行う。",
        phase="POSTGAME",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.PG_02A_RESURRECTION_INTRO],
        priority=580,
    ),
    QuestDef(
        quest_id=QuestIds.PG_03_SCROLL_SHOWCASE,
        quest_type=QuestType.POSTGAME,
        drama_id="p2_03_scroll_showcase",
        name_jp="大部屋の巻物お披露目会",
        name_en="Scroll of Great Hall Showcase",
        description="リリィの研究成果「大部屋の巻物」のお披露目会が開催される。",
        phase="POSTGAME",
        quest_giver="sukutsu_receptionist",
        required_quests=[QuestIds.PG_02B_RESURRECTION_EXECUTION],
        priority=570,
    ),
]


# ============================================================================
# Rewards
# ============================================================================

REWARDS = [
    RewardDef(
        reward_id="rank_up_G",
        items=[
            RewardItemDef(item_id="medal", count=10),
            RewardItemDef(item_id="1165", count=3),
            RewardItemDef(item_id="lovepotion", count=30),
        ],
        flags={f"{PREFIX}.player.rank": "G"},
        message_jp="報酬として、小さなメダル10枚、エーテル抗体3本、媚薬30本をお渡しします。",
        message_en="As your reward, I present you with 10 small medals, 3 ether antibodies, and 30 love potions.",
        message_cn="作为奖励，我将给您10枚小勋章、3瓶以太抗体和30瓶媚药。",
        system_message_jp="【システム】称号『屑肉』を獲得しました。フィート【闘志】Lv1（活力+2）を習得！",
        system_message_en="[System] Title 'Scrap' acquired. Feat 'Arena Spirit' Lv1 (Vigor+2) obtained!",
        system_message_cn="【系统】获得称号『废肉』。习得专长【斗志】Lv1（活力+2）！",
    ),
    RewardDef(
        reward_id="rank_up_F",
        items=[
            RewardItemDef(item_id="medal", count=20),
            RewardItemDef(item_id="1165", count=6),
            RewardItemDef(item_id="lovepotion", count=60),
        ],
        flags={f"{PREFIX}.player.rank": "F"},
        message_jp="報酬として、小さなメダル20枚、エーテル抗体6本、媚薬60本をお渡しします。",
        message_en="As your reward, I present you with 20 small medals, 6 ether antibodies, and 60 love potions.",
        message_cn="作为奖励，我将给您20枚小勋章、6瓶以太抗体和60瓶媚药。",
        system_message_jp="【システム】称号『泥犬』を獲得しました。フィート【闘志】Lv2（活力+4）に成長！",
        system_message_en="[System] Title 'Mud Dog' acquired. Feat 'Arena Spirit' Lv2 (Vigor+4) obtained!",
        system_message_cn="【系统】获得称号『泥狗』。专长【斗志】成长至Lv2（活力+4）！",
    ),
    RewardDef(
        reward_id="rank_up_E",
        items=[
            RewardItemDef(item_id="medal", count=30),
            RewardItemDef(item_id="1165", count=9),
            RewardItemDef(item_id="lovepotion", count=90),
        ],
        flags={f"{PREFIX}.player.rank": "E"},
        message_jp="報酬として、小さなメダル30枚、エーテル抗体9本、媚薬90本をお渡しします。",
        message_en="As your reward, I present you with 30 small medals, 9 ether antibodies, and 90 love potions.",
        message_cn="作为奖励，我将给您30枚小勋章、9瓶以太抗体和90瓶媚药。",
        system_message_jp="【システム】称号『鉄屑』を獲得しました。フィート【闘志】Lv3（活力+6）に成長！",
        system_message_en="[System] Title 'Iron Scrap' acquired. Feat 'Arena Spirit' Lv3 (Vigor+6) obtained!",
        system_message_cn="【系统】获得称号『铁渣』。专长【斗志】成长至Lv3（活力+6）！",
    ),
    RewardDef(
        reward_id="rank_up_D",
        items=[
            RewardItemDef(item_id="medal", count=40),
            RewardItemDef(item_id="1165", count=12),
            RewardItemDef(item_id="lovepotion", count=120),
        ],
        flags={f"{PREFIX}.player.rank": "D"},
        message_jp="報酬として、小さなメダル40枚、エーテル抗体12本、媚薬120本をお渡しします。",
        message_en="As your reward, I present you with 40 small medals, 12 ether antibodies, and 120 love potions.",
        message_cn="作为奖励，我将给您40枚小勋章、12瓶以太抗体和120瓶媚药。",
        system_message_jp="【システム】称号『銅貨稼ぎ』を獲得しました。フィート【闘志】Lv4（活力+8）に成長！",
        system_message_en="[System] Title 'Copper Earner' acquired. Feat 'Arena Spirit' Lv4 (Vigor+8) obtained!",
        system_message_cn="【系统】获得称号『铜币赚取者』。专长【斗志】成长至Lv4（活力+8）！",
    ),
    RewardDef(
        reward_id="rank_up_C",
        items=[
            RewardItemDef(item_id="medal", count=50),
            RewardItemDef(item_id="1165", count=15),
            RewardItemDef(item_id="lovepotion", count=150),
        ],
        flags={f"{PREFIX}.player.rank": "C"},
        message_jp="報酬として、小さなメダル50枚、エーテル抗体15本、媚薬150本をお渡しします。",
        message_en="As your reward, I present you with 50 small medals, 15 ether antibodies, and 150 love potions.",
        message_cn="作为奖励，我将给您50枚小勋章、15瓶以太抗体和150瓶媚药。",
        system_message_jp="【システム】称号『朱砂食い』を獲得しました。フィート【闘志】Lv5（活力+10）に成長！",
        system_message_en="[System] Title 'Cinnabar Eater' acquired. Feat 'Arena Spirit' Lv5 (Vigor+10) obtained!",
        system_message_cn="【系统】获得称号『食朱砂者』。专长【斗志】成长至Lv5（活力+10）！",
    ),
    RewardDef(
        reward_id="rank_up_B",
        items=[
            RewardItemDef(item_id="medal", count=60),
            RewardItemDef(item_id="1165", count=18),
            RewardItemDef(item_id="lovepotion", count=180),
        ],
        flags={f"{PREFIX}.player.rank": "B"},
        message_jp="報酬として、小さなメダル60枚、エーテル抗体18本、媚薬180本をお渡しします。",
        message_en="As your reward, I present you with 60 small medals, 18 ether antibodies, and 180 love potions.",
        message_cn="作为奖励，我将给您60枚小勋章、18瓶以太抗体和180瓶媚药。",
        system_message_jp="【システム】称号『銀翼』を獲得しました。フィート【闘志】Lv6（活力+12）に成長！",
        system_message_en="[System] Title 'Silver Wing' acquired. Feat 'Arena Spirit' Lv6 (Vigor+12) obtained!",
        system_message_cn="【系统】获得称号『银翼』。专长【斗志】成长至Lv6（活力+12）！",
    ),
    RewardDef(
        reward_id="rank_up_A",
        items=[
            RewardItemDef(item_id="medal", count=70),
            RewardItemDef(item_id="1165", count=21),
            RewardItemDef(item_id="lovepotion", count=210),
        ],
        flags={f"{PREFIX}.player.rank": "A"},
        message_jp="報酬として、小さなメダル70枚、エーテル抗体21本、媚薬210本をお渡しします。",
        message_en="As your reward, I present you with 70 small medals, 21 ether antibodies, and 210 love potions.",
        message_cn="作为奖励，我将给您70枚小勋章、21瓶以太抗体和210瓶媚药。",
        system_message_jp="【システム】称号『戦鬼』を獲得しました。フィート【闘志】Lv7（活力+14）に成長！",
        system_message_en="[System] Title 'War Demon' acquired. Feat 'Arena Spirit' Lv7 (Vigor+14) obtained!",
        system_message_cn="【系统】获得称号『战鬼』。专长【斗志】成长至Lv7（活力+14）！",
    ),
]


# ============================================================================
# RANK_REWARDS dictionary (derived from REWARDS)
# ============================================================================


def _build_rank_rewards() -> Dict[str, RewardDef]:
    """Build RANK_REWARDS dictionary from REWARDS list."""
    rank_rewards = {}
    for reward in REWARDS:
        if reward.reward_id.startswith("rank_up_"):
            rank_letter = reward.reward_id.replace("rank_up_", "")
            rank_rewards[rank_letter] = reward
    return rank_rewards


RANK_REWARDS = _build_rank_rewards()


# ============================================================================
# Validation
# ============================================================================


def validate_rank_rewards() -> List[str]:
    """
    ランク報酬定義のバリデーション

    Returns:
        エラーメッセージのリスト（空なら問題なし）
    """
    errors = []

    expected_ranks = ["G", "F", "E", "D", "C", "B", "A"]

    # 全ランクが定義されているか
    for rank in expected_ranks:
        if rank not in RANK_REWARDS:
            errors.append(f"RANK_REWARDS: ランク '{rank}' が定義されていません")

    # 各ランク報酬の検証
    for rank, reward in RANK_REWARDS.items():
        prefix = f"RANK_REWARDS['{rank}']"

        # message_jp が設定されているか
        if not reward.message_jp:
            errors.append(f"{prefix}: message_jp が設定されていません")

        # アイテムが空でないか
        if not reward.items:
            errors.append(f"{prefix}: items が空です")

    return errors


def validate_all() -> List[str]:
    """
    全ての報酬定義をバリデーション

    Returns:
        エラーメッセージのリスト（空なら問題なし）
    """
    errors = []
    errors.extend(validate_rank_rewards())
    return errors


# ============================================================================
# Re-exports for backward compatibility
# ============================================================================

# Aliases
RewardItem = RewardItemDef
Reward = RewardDef
QuestDefinition = QuestDef

# QUEST_DEFINITIONS alias
QUEST_DEFINITIONS = QUESTS
