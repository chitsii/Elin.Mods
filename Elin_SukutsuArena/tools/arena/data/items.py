# -*- coding: utf-8 -*-
"""
arena/data/items.py - Custom Item Definitions

This module defines all custom items for the Sukutsu Arena mod.
"""

from dataclasses import dataclass, field
from enum import Enum
from typing import Dict, List, Optional


class TraitType(Enum):
    """Trait種別"""

    VANILLA = "vanilla"  # バニラTrait使用
    CUSTOM = "custom"  # カスタムTrait（TraitSukutsuItem使用）


@dataclass
class ItemEffect:
    """アイテム効果定義"""

    # 一時バフ
    buff_element_id: Optional[int] = None  # 951=resCold など
    buff_value: int = 0
    buff_duration_power: int = 100  # 持続時間計算用

    # カルマ変動
    karma_change: int = 0

    # 永続効果（ModBase）
    permanent_stats: Dict[int, int] = field(default_factory=dict)


@dataclass
class ItemDefinition:
    """
    アイテム定義

    SourceThing の各カラムに対応するフィールドを持つ。
    """

    # 基本情報 (必須)
    id: str  # id（ユニーク識別子）
    name_jp: str  # name_JP
    name_en: str  # name
    category: str  # category（drink, amulet, ring, armor など）

    # 表示（オプション）
    name_cn: str = ""  # name_CN（中国語名）
    detail_jp: str = ""  # detail_JP
    detail_en: str = ""  # detail
    detail_cn: str = ""  # detail_CN（中国語説明）

    # Trait設定
    trait_type: TraitType = TraitType.VANILLA
    trait_name: str = "Drink"  # trait - Trait名
    trait_params: List[str] = field(default_factory=list)  # trait パラメータ

    # 効果（カスタムTraitの場合に使用）
    effect: Optional[ItemEffect] = None

    # エレメント（装備エンチャント/フィート付与）
    # 形式: "alias/value,alias/value,..." (例: "featHeavyEater/1,r_life/10")
    elements: str = ""

    # ゲームデータ
    value: int = 100  # value（売却価格）
    lv: int = 1  # LV
    weight: int = 100  # weight (1/1000単位、100=0.1kg)
    chance: int = 0  # chance（0=ランダム生成対象外）

    # レンダリング
    tiles: int = 0  # tiles（スプライトID）
    render_data: str = "item"  # _idRenderData

    # 装備品用（鎧、武器など）
    def_mat: str = ""  # defMat（デフォルト素材: iron, gold など）
    tier_group: str = ""  # tierGroup（metal, wood など）
    defense: str = ""  # defense（防御値: "6,21" など）

    # タグ
    tags: List[str] = field(default_factory=list)  # tag

    # 販売情報
    sell_at: Optional[str] = None  # 販売NPCのID
    stock_rarity: str = "Random"  # 在庫のレアリティ（Randomでquality優先）
    stock_num: int = 1  # 在庫数
    stock_restock: bool = True  # 補充するか

    sort: Optional[str] = ""
    quality: Optional[str] = ""
    component: Optional[str] = ""
    components: Optional[str] = "log"
    filter: Optional[str] = ""

    # TraitTape用（カセットテープのBGM ID）
    ref_val: Optional[int] = None


# ============================================================================
# アイテム定義
# ============================================================================

CUSTOM_ITEMS: Dict[str, ItemDefinition] = {
    # ========================================
    # 万難のエリクサー（Elixir of Trials）- 複合耐性
    # ========================================
    "sukutsu_kiss_of_inferno": ItemDefinition(
        id="sukutsu_kiss_of_inferno",
        name_jp="万難のエリクサー",
        name_en="Elixir of All-Protection",
        name_cn="万难灵药",
        category="_drink",
        sort="drink",
        components="potion_empty",
        quality="4",
        filter="start",
        def_mat="grass",
        detail_jp="異端の錬金術師が生涯をかけて完成させた禁忌の霊薬。あらゆる災厄を退けるが、その代償として魂の一部を蝕む。術師は完成の日、自ら服用し、そのまま灰となった。",
        detail_en="A forbidden elixir perfected by a heretic alchemist over a lifetime. It wards off all calamities, but corrodes a part of the soul. On the day of completion, the alchemist drank it himself and turned to ash.",
        detail_cn="异端炼金术师穷其一生完成的禁忌灵药。能抵御一切灾厄，但代价是侵蚀灵魂的一部分。术师在完成之日亲自服下，随即化为灰烬。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["kiss_of_inferno"],
        effect=ItemEffect(
            karma_change=-50,
        ),
        value=50000,
        lv=30,
        weight=50,
        tiles=1551,
        render_data="obj_S",
        tags=["neg", "noShop"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=3,
        stock_restock=False,
    ),
    # ========================================
    # 個別耐性ポーション
    # ========================================
    "sukutsu_frost_ward": ItemDefinition(
        id="sukutsu_frost_ward",
        name_jp="凍牙の護り",
        name_en="Frostfang Ward",
        name_cn="冰牙之护",
        category="_drink",
        sort="drink",
        components="potion_empty",
        def_mat="grass",
        quality="4",
        filter="start",
        detail_jp="北の凍土で採れる霜竜の血から精製された青き秘薬。飲めば身体の芯まで凍えるが、いかなる冷気も肌を刺すことはない。",
        detail_en="A blue elixir distilled from the blood of frost dragons found in the northern tundra. It chills to the bone, yet no cold can pierce the skin thereafter.",
        detail_cn="从北方冻土的霜龙之血中提炼的蓝色秘药。饮下后全身冰寒彻骨，但任何寒气都无法刺透肌肤。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["frost_ward"],
        effect=ItemEffect(
            buff_element_id=951,
            karma_change=-15,
        ),
        value=2000,
        lv=10,
        weight=50,
        tiles=1614,
        render_data="obj_S",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=5,
        stock_restock=True,
    ),
    "sukutsu_mind_ward": ItemDefinition(
        id="sukutsu_mind_ward",
        name_jp="明鏡の護り",
        name_en="Clarity Ward",
        name_cn="明镜之护",
        category="_drink",
        sort="drink",
        components="potion_empty",
        def_mat="grass",
        quality="4",
        filter="start",
        detail_jp="真実のみを映す鏡を砕いて作られた銀の薬。飲めば全ての幻が剥がれ落ちる。だが、真実を見続けた者は皆、やがて己の目を抉り取ったという。",
        detail_en="A silver elixir made from a shattered mirror that reflected only truth. All illusions fall away upon drinking. Yet all who gazed upon truth unending eventually gouged out their own eyes.",
        detail_cn="由只映真实之镜的碎片制成的银色药剂。饮下后一切幻象尽皆剥落。然而凝视真实太久之人，最终都挖去了自己的双眼。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["mind_ward"],
        effect=ItemEffect(
            buff_element_id=953,
            karma_change=-15,
        ),
        value=30000,
        lv=10,
        weight=50,
        tiles=1614,
        render_data="obj_S",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=5,
        stock_restock=True,
    ),
    "sukutsu_chaos_ward": ItemDefinition(
        id="sukutsu_chaos_ward",
        name_jp="秩序の護り",
        name_en="Order Ward",
        name_cn="秩序之护",
        category="_drink",
        sort="drink",
        def_mat="grass",
        components="potion_empty",
        quality="4",
        filter="start",
        detail_jp="混沌の深淵を覗き込み、正気を失った賢者が遺した虹色の薬。秩序なき力を退けるが、その製法は狂気の書物にのみ記されている。",
        detail_en="A rainbow elixir left by a sage who gazed into the abyss of chaos and lost his mind. It repels the forces of disorder, though its recipe exists only in tomes of madness.",
        detail_cn="窥视混沌深渊而失去理智的贤者所留下的虹色药剂。能抵御无序之力，但配方只记载于疯狂的典籍中。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["chaos_ward"],
        effect=ItemEffect(
            buff_element_id=959,
            karma_change=-15,
        ),
        value=30000,
        lv=10,
        weight=50,
        tiles=1614,
        render_data="obj_S",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=5,
        stock_restock=True,
    ),
    "sukutsu_sound_ward": ItemDefinition(
        id="sukutsu_sound_ward",
        name_jp="静寂の護り",
        name_en="Silence Ward",
        name_cn="寂静之护",
        category="_drink",
        sort="drink",
        def_mat="grass",
        components="potion_empty",
        quality="4",
        filter="start",
        detail_jp="雷神の落とした耳栓を溶かして作られたという黄金色の薬。一時的に聴覚が鈍るが、いかなる轟音も鼓膜を破ることはない。",
        detail_en="A golden potion said to be made from melted earplugs dropped by the thunder god. Hearing dulls temporarily, but no roar can burst the eardrums.",
        detail_cn="据说是用雷神遗落的耳塞熔化而成的金色药剂。暂时使听觉迟钝，但任何轰鸣都无法震破耳膜。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["sound_ward"],
        effect=ItemEffect(
            buff_element_id=957,
            karma_change=-15,
        ),
        value=30000,
        lv=10,
        weight=50,
        tiles=1614,
        render_data="obj_S",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=5,
        stock_restock=True,
    ),
    "sukutsu_impact_ward": ItemDefinition(
        id="sukutsu_impact_ward",
        name_jp="鋼鉄の護り",
        name_en="Steel Ward",
        name_cn="钢铁之护",
        category="_drink",
        sort="drink",
        def_mat="grass",
        components="potion_empty",
        quality="4",
        filter="start",
        detail_jp="鋼鉄の巨人の心臓から抽出された灰色の液体。飲めば全身が鉄のように硬くなり、いかなる衝撃も骨を砕くことはできない。",
        detail_en="A gray liquid extracted from the heart of a steel colossus. The body hardens like iron upon consumption, and no impact can shatter bone.",
        detail_cn="从钢铁巨人心脏中提取的灰色液体。饮下后全身坚如钢铁，任何冲击都无法粉碎骨骼。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["impact_ward"],
        effect=ItemEffect(
            buff_element_id=965,
            karma_change=-15,
        ),
        value=30000,
        lv=10,
        weight=50,
        tiles=1614,
        render_data="obj_S",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=5,
        stock_restock=True,
    ),
    "sukutsu_bleed_ward": ItemDefinition(
        id="sukutsu_bleed_ward",
        name_jp="凝血の護り",
        name_en="Clotting Ward",
        name_cn="凝血之护",
        category="_drink",
        sort="drink",
        def_mat="grass",
        components="potion_empty",
        quality="4",
        filter="start",
        detail_jp="不死の吸血鬼から採取した血を凝固させた赤黒い秘薬。飲めば傷口が瞬時に塞がり、いかなる刃も血を流させることはできない。",
        detail_en="A dark red elixir made from coagulated blood of an immortal vampire. Wounds close instantly, and no blade can draw blood.",
        detail_cn="用不死吸血鬼的血液凝固而成的暗红色秘药。饮下后伤口瞬间愈合，任何利刃都无法使其流血。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["bleed_ward"],
        effect=ItemEffect(
            buff_element_id=964,
            karma_change=-15,
        ),
        value=30000,
        lv=10,
        weight=50,
        tiles=1614,
        render_data="obj_S",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=5,
        stock_restock=True,
    ),
    # ========================================
    # 装備品
    # ========================================
    "sukutsu_hunger_amulet": ItemDefinition(
        id="sukutsu_hunger_amulet",
        name_jp="飢餓の首飾り",
        name_en="Amulet of Hunger",
        name_cn="饥饿项链",
        category="amulet",
        quality="4",
        detail_jp="餓鬼道に堕ちた僧侶が首に掛けていた呪物。装備すると底なしの飢えに苛まれ、いくら食べても満たされることはない。僧侶は最期、己の腕を喰らったという。",
        detail_en="A cursed relic worn by a monk who fell into the realm of hungry ghosts. The wearer suffers endless hunger that cannot be sated. The monk is said to have devoured his own arms in the end.",
        detail_cn="堕入饿鬼道的僧侣曾佩戴的诅咒遗物。装备后会遭受无尽的饥饿折磨，无论吃多少都无法满足。据说那僧侣最后吞噬了自己的双臂。",
        trait_type=TraitType.VANILLA,
        trait_name="",
        elements="featHeavyEater/1",
        value=40000,
        lv=15,
        weight=50,
        tiles=1221,
        render_data="obj_S flat",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=10,
        stock_restock=True,
    ),
    "sukutsu_ephemeral_gift": ItemDefinition(
        id="sukutsu_ephemeral_gift",
        name_jp="儚き天禀",
        name_en="Glass Sovereignty",
        name_cn="脆薄天禀",
        category="ring",
        quality="4",
        detail_jp="夭折した天才魔術師の指に嵌められていた硝子の指輪。魔力を極限まで高めるが、生命の炎を急速に燃やし尽くす。彼女は二十歳を迎えることなく灰となった。",
        detail_en="A glass ring found on the finger of a prodigy sorceress who died young. It amplifies magic to its limits but rapidly burns away the flame of life. She turned to ash before her twentieth year.",
        detail_cn="夭折的天才魔法师手指上戴着的玻璃戒指。能将魔力提升到极限，但会迅速燃尽生命之火。她未满二十岁便化为灰烬。",
        trait_type=TraitType.VANILLA,
        trait_name="",
        elements="r_life/-90,r_mana/200",
        value=50000,
        lv=25,
        weight=10,
        tiles=1219,
        render_data="obj_S flat",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=1,
        stock_restock=True,
    ),
    "sukutsu_fools_peace": ItemDefinition(
        id="sukutsu_fools_peace",
        name_jp="愚者の平穏",
        name_en="Fool's Serenity",
        name_cn="愚者之安",
        category="ring",
        quality="4",
        detail_jp="魔法を恐れた愚かな王が鍛冶師に作らせた鉛の指輪。肉体を頑強にするが、一切の魔力を封じてしまう。王は魔術師の呪いから逃れたが、知恵までも失うこととなった。",
        detail_en="A leaden ring forged by a smith for a foolish king who feared magic. It grants robust flesh but seals all magical power. The king escaped the sorcerer's curse but lost his wisdom as well.",
        detail_cn="恐惧魔法的愚蠢国王命铁匠打造的铅制戒指。能使肉体强健，但会封印一切魔力。国王虽逃脱了魔法师的诅咒，却也失去了智慧。",
        trait_type=TraitType.VANILLA,
        trait_name="",
        elements="r_life/200,r_mana/-90",
        value=50000,
        lv=25,
        weight=10,
        tiles=1219,
        render_data="obj_S flat",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=1,
        stock_restock=True,
    ),
    # ========================================
    # カスタム効果アイテム
    # ========================================
    "sukutsu_painkiller": ItemDefinition(
        id="sukutsu_painkiller",
        name_jp="痛覚遮断薬",
        name_en="Pain Suppressant",
        name_cn="痛觉阻断药",
        category="_drink",
        sort="drink",
        components="potion_empty",
        def_mat="grass",
        quality="4",
        filter="start",
        detail_jp="拷問官が囚人に与えていた黒い薬。苦痛を遮断し肉体を守るが、臓腑を蝕む猛毒を含む。囚人たちは痛みを忘れたまま、静かに腐っていったという。",
        detail_en="A black drug given to prisoners by torturers. It blocks pain and protects the flesh, but contains a deadly poison that rots the organs. The prisoners forgot their pain and quietly decayed.",
        detail_cn="拷问官给囚犯服用的黑色药物。能阻断痛觉保护肉体，但含有腐蚀脏腑的剧毒。囚犯们忘却了痛苦，却在沉默中慢慢腐烂。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["painkiller"],
        effect=ItemEffect(karma_change=-10),
        value=30000,
        lv=20,
        weight=50,
        tiles=1614,
        render_data="obj_S",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=3,
        stock_restock=True,
    ),
    "sukutsu_stimulant": ItemDefinition(
        id="sukutsu_stimulant",
        name_jp="禁断の覚醒剤",
        name_en="Forbidden Stimulant",
        name_cn="禁忌兴奋剂",
        category="_drink",
        sort="drink",
        def_mat="grass",
        components="potion_empty",
        quality="4",
        filter="start",
        detail_jp="狂戦士たちが決死の戦いの前に服用した禁断の薬。神経を極限まで研ぎ澄ませるが、同時に血管が内側から破裂する。",
        detail_en="A forbidden drug taken by berserkers before battles to the death. It sharpens the nerves to their limit, but blood vessels rupture from within.",
        detail_cn="狂战士在决死战斗前服用的禁忌药物。能将神经磨砺到极限，但同时血管会从内部破裂。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuItem",
        trait_params=["stimulant"],
        effect=ItemEffect(karma_change=-15),
        value=40000,
        lv=30,
        weight=50,
        tiles=1311,
        render_data="obj_S flat",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=2,
        stock_restock=True,
    ),
    "sukutsu_gilded_armor": ItemDefinition(
        id="sukutsu_gilded_armor",
        name_jp="虚飾の黄金鎧",
        name_en="Gilded Vanity Armor",
        name_cn="虚饰黄金甲",
        category="torso",
        quality="4",
        detail_jp="かつて強欲な王が纏った呪われし黄金の鎧。その輝きは持ち主の富を喰らい、傷の代わりに金貨を剥ぎ取る。王は最期、一枚の金貨も残さず骸と化したという。",
        detail_en="A cursed golden armor once worn by a greedy king. Its radiance devours the wearer's wealth, shedding gold coins instead of blood. The king met his end as a penniless corpse.",
        detail_cn="贪婪之王曾穿戴的诅咒黄金铠甲。其光辉吞噬穿戴者的财富，以金币代替鲜血脱落。据说那国王最后一枚金币也没留下，化为了枯骨。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuGildedArmor",
        trait_params=[],
        effect=ItemEffect(),
        elements="SPD/-60",
        value=80000,
        lv=48,
        weight=7500,
        tiles=1255,
        render_data="obj_S flat",
        def_mat="gold",
        tier_group="metal",
        defense="6,21",
        tags=["neg", "fixedMat"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=1,
        stock_restock=True,
    ),
    "sukutsu_twin_mirror": ItemDefinition(
        id="sukutsu_twin_mirror",
        name_jp="双子の鏡",
        name_en="Twin Mirror",
        name_cn="双子之镜",
        category="amulet",
        quality="4",
        detail_jp="双子の魔女が互いを封じ込めた呪われた鏡。装備すると鏡の中からもう一人の自分が這い出し、主に付き従う。外せば影は鏡の中へ還る。",
        detail_en="A cursed mirror in which twin witches sealed each other. When worn, another self crawls out from the mirror to serve its master. Remove it, and the shadow returns within.",
        detail_cn="双子魔女相互封印的诅咒之镜。装备后另一个自己会从镜中爬出追随主人。摘下后影子便会回到镜中。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuTwinMirror",
        trait_params=[],
        effect=ItemEffect(),
        value=80000,
        lv=35,
        weight=100,
        tiles=1318,
        render_data="obj_S flat",
        tags=["neg"],
        sell_at="sukutsu_shady_merchant",
        # stock_rarity="Artifact",
        stock_num=1,
        stock_restock=True,
    ),
}

# ============================================================================
# カセットテープ定義（BGMコレクション用）
# ============================================================================

# BGMファイル名 → (日本語名, 英語名, 中国語名)
BGM_DISPLAY_NAMES: Dict[str, tuple] = {
    "Battle_Astaroth_Phase1": (
        "アスタロト戦 第一楽章",
        "Battle: Astaroth Phase 1",
        "阿斯塔罗特战 第一乐章",
    ),
    "Battle_Astaroth_Phase2": (
        "アスタロト戦 第二楽章",
        "Battle: Astaroth Phase 2",
        "阿斯塔罗特战 第二乐章",
    ),
    "Battle_Balgas_Prime": ("バルガス戦", "Battle: Vargus Prime", "巴尔加斯战"),
    "Battle_Kain_Requiem": ("カイン・レクイエム", "Battle: Cain Requiem", "凯恩安魂曲"),
    "Battle_Null_Assassin": (
        "暗殺人形との戦い",
        "Battle: Null Assassin",
        "与刺客人偶之战",
    ),
    "Battle_RankC_Heroic_Lament": ("英雄の嘆き", "Battle: Heroic Lament", "英雄的悲叹"),
    "Battle_RankD_Chaos": ("混沌の戦い", "Battle: Chaos", "混沌之战"),
    "Battle_RankE_Ice": ("氷の試練", "Battle: Ice Trial", "冰之试炼"),
    "Battle_RankG_VoidOoze": ("ヴォイド・プチ", "Battle: Void Ooze", "虚空史莱姆"),
    "Battle_Shadow_Self": ("影なる自己", "Battle: Shadow Self", "影之自我"),
    "Emotional_Hope": ("希望", "Emotional: Hope", "希望"),
    "Emotional_Resolve": ("決意", "Emotional: Resolve", "决意"),
    "Emotional_Sacred_Triumph_Normal": ("神聖なる勝利", "Sacred Triumph", "神圣的胜利"),
    "Emotional_Sacred_Triumph_Special": (
        "神聖なる勝利（特別版）",
        "Sacred Triumph (Special)",
        "神圣的胜利（特别版）",
    ),
    "Emotional_Sorrow": ("悲哀", "Emotional: Sorrow", "悲哀"),
    "Emotional_Sorrow_2": ("悲哀 II", "Emotional: Sorrow II", "悲哀 II"),
    "Ending_Celebration": ("祝勝", "Ending: Celebration", "庆祝"),
    "Fanfare_Audience": ("観客のファンファーレ", "Fanfare: Audience", "观众欢呼"),
    "Final_Astaroth_Throne": (
        "アスタロトの玉座",
        "Final: Astaroth's Throne",
        "阿斯塔罗特的王座",
    ),
    "Final_Liberation": ("解放", "Final: Liberation", "解放"),
    "Final_PreBattle_Calm": (
        "決戦前の静けさ",
        "Final: Pre-Battle Calm",
        "决战前的宁静",
    ),
    "Lily_Confession": ("リリィの告白", "Lily: Confession", "莉莉的告白"),
    "Lily_Confession_2": ("リリィの告白 II", "Lily: Confession II", "莉莉的告白 II"),
    "Lily_Private_Room": ("リリィの私室", "Lily: Private Room", "莉莉的私室"),
    "Lily_Seductive_Danger": ("誘惑と危険", "Lily: Seductive Danger", "诱惑与危险"),
    "Lily_Tranquil": ("穏やかなリリィ", "Lily: Tranquil", "宁静的莉莉"),
    "Lobby_Normal": ("ロビー", "Lobby: Normal", "大厅"),
    "Mystical_Ritual": ("神秘の儀式", "Mystical Ritual", "神秘仪式"),
    "Ominous_Heartbeat": ("不吉な鼓動", "Ominous: Heartbeat", "不祥的心跳"),
    "Ominous_Suspense_01": ("不穏な空気 I", "Ominous: Suspense I", "不祥的气氛 I"),
    "Ominous_Suspense_02": ("不穏な空気 II", "Ominous: Suspense II", "不祥的气氛 II"),
    "ProgressiveDance": ("プログレッシブ・ダンス", "Progressive Dance", "渐进舞曲"),
    "sukutsu_arena_opening": (
        "闘技場オープニング",
        "Arena Opening",
        "开场",
    ),
    "Zek_Merchant": ("ゼクのテーマ", "Zek: Merchant", "泽克主题曲"),
    "Zek_Merchant_2": ("ゼクのテーマ II", "Zek: Merchant II", "泽克主题曲 II"),
    "Iris_Theme": ("アイリスのテーマ", "Iris: Theme", "艾丽丝主题曲"),
    "Iris_Funk": ("アイリスのファンク", "Iris: Funk", "艾丽丝放克"),
    "Nichijo": ("日常", "Daily Life", "日常"),
}

# BGMファイル名の固定順序（ID互換維持用）
# 重要: 既存セーブ互換のため、この順序は変更しないこと。
# 新しいBGMは末尾に追加する。
BGM_ID_ORDER = [
    "Battle_Astaroth_Phase1",
    "Battle_Astaroth_Phase2",
    "Battle_Balgas_Prime",
    "Battle_Kain_Requiem",
    "Battle_Null_Assassin",
    "Battle_RankC_Heroic_Lament",
    "Battle_RankD_Chaos",
    "Battle_RankE_Ice",
    "Battle_RankG_VoidOoze",
    "Battle_Shadow_Self",
    "Emotional_Hope",
    "Emotional_Resolve",
    "Emotional_Sacred_Triumph_Normal",
    "Emotional_Sacred_Triumph_Special",
    "Emotional_Sorrow",
    "Emotional_Sorrow_2",
    "Ending_Celebration",
    "Fanfare_Audience",
    "Final_Astaroth_Throne",
    "Final_Liberation",
    "Final_PreBattle_Calm",
    "Iris_Funk",
    "Iris_Theme",
    "Lily_Confession",
    "Lily_Confession_2",
    "Lily_Private_Room",
    "Lily_Seductive_Danger",
    "Lily_Tranquil",
    "Lobby_Normal",
    "Mystical_Ritual",
    "Ominous_Heartbeat",
    "Ominous_Suspense_01",
    "Ominous_Suspense_02",
    "ProgressiveDance",
    "sukutsu_arena_opening",
    "Zek_Merchant",
    "Zek_Merchant_2",
    "Nichijo",
]

BGM_ID_MAP = {name: index for index, name in enumerate(BGM_ID_ORDER)}

# BGM_BASE_ID（config.pyからインポートせずに直接定義して循環参照を避ける）
_BGM_BASE_ID = 1000000


def _create_cassette_tape(bgm_filename: str) -> ItemDefinition:
    """カセットテープのItemDefinitionを生成"""
    if bgm_filename not in BGM_ID_MAP:
        raise ValueError(f"Unknown BGM file for cassette ID mapping: {bgm_filename}")
    bgm_id = _BGM_BASE_ID + BGM_ID_MAP[bgm_filename]
    name_jp, name_en, name_cn = BGM_DISPLAY_NAMES[bgm_filename]
    item_id = f"sukutsu_tape_{bgm_filename.lower()}"

    # Mod由来とわかるようにプレフィックスを付ける
    return ItemDefinition(
        id=item_id,
        name_jp=f"[闘技場 BGM] {name_jp}",
        name_en=f"[Arena BGM] Cassette: {name_en}",
        name_cn=f"[Arena BGM] {name_cn}",
        category="junk",
        trait_type=TraitType.CUSTOM,  # カスタムTrait使用
        trait_name="SukutsuTape",  # TraitTapeをオーバーライド
        trait_params=[str(bgm_id)],  # BGM IDをtraitパラメータで渡す
        ref_val=bgm_id,
        def_mat="gold",  # ゴールド素材
        value=1000,  # 価格1000g
        lv=1,
        weight=50,
        tiles=1728,  # バニラのカセットテープと同じスプライト
        render_data="obj_S",
        sell_at="sukutsu_receptionist",
        stock_rarity="Normal",
        stock_num=1,
        stock_restock=True,
    )


def _generate_cassette_tapes() -> Dict[str, ItemDefinition]:
    """全BGM対応のカセットテープ定義を生成（固定順）"""
    missing = set(BGM_DISPLAY_NAMES.keys()) - set(BGM_ID_ORDER)
    if missing:
        raise ValueError(
            "BGM_ID_ORDER に未登録のBGMがあります: " + ", ".join(sorted(missing))
        )
    tapes = {}
    for bgm_filename in BGM_ID_ORDER:
        tape = _create_cassette_tape(bgm_filename)
        tapes[tape.id] = tape
    return tapes


# カセットテープをCUSTOM_ITEMSにマージ
CUSTOM_ITEMS.update(_generate_cassette_tapes())


# ============================================================================
# マップ兵器スクロール定義
# ============================================================================

MAP_WEAPON_SCROLLS: Dict[str, ItemDefinition] = {
    # ========================================
    # 収穫系スクロール
    # ========================================
    "sukutsu_scroll_harvest": ItemDefinition(
        id="sukutsu_scroll_harvest",
        name_jp="大部屋の巻物",
        name_en="Scroll of Great Hall",
        name_cn="大房间卷轴",
        category="scroll",
        sort="book_scroll",
        quality="4",
        filter="start",
        detail_jp="古の地精が残した禁呪の巻物。読み上げると大地そのものが震え、あらゆる壁と障害物が崩れ落ちる。ただし他人の領地で使用すれば、その代償は重い。",
        detail_en="A forbidden scroll left by ancient earth spirits. When read, the earth itself trembles and all walls and obstacles crumble away. However, using it on another's land carries a heavy price.",
        detail_cn="古代地精留下的禁咒卷轴。诵读时大地本身会震动，所有墙壁和障碍物都会崩塌。但若在他人领地使用，代价将十分沉重。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuScrollHarvest",
        trait_params=[],
        effect=ItemEffect(),
        value=30000,
        lv=30,
        weight=50,
        tiles=1613,
        render_data="obj_S flat",
        def_mat="paper",
        tags=["neg"],
        sell_at="sukutsu_scroll_showcase",
        stock_rarity="Normal",
        stock_num=10,
        stock_restock=True,
    ),
    # ========================================
    # 攻撃系スクロール（炎）
    # ========================================
    "sukutsu_scroll_attack_fire": ItemDefinition(
        id="sukutsu_scroll_attack_fire",
        name_jp="業火殲滅の巻物",
        name_en="Scroll of Infernal Annihilation",
        name_cn="业火歼灭卷轴",
        category="scroll",
        sort="book_scroll",
        quality="4",
        filter="start",
        detail_jp="地獄の業火を封じ込めた禁断の巻物。読み上げると天から炎の雨が降り注ぎ、視界の全ての敵を焼き尽くす。巻物自体も高熱を帯びており、素手で触れると火傷する。",
        detail_en="A forbidden scroll containing hellfire. When read, a rain of fire falls from the sky and burns all enemies in sight. The scroll itself radiates intense heat that burns bare hands.",
        detail_cn="封印着地狱业火的禁忌卷轴。诵读时火焰之雨从天而降，焚尽视野中的一切敌人。卷轴本身也散发着高温，赤手触碰会被灼伤。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuScrollAttack",
        trait_params=["eleFire", "500"],
        effect=ItemEffect(),
        value=50000,
        lv=50,
        weight=50,
        tiles=1613,
        render_data="obj_S flat",
        def_mat="paper",
        tags=["neg"],
        sell_at=None,
        stock_rarity="Normal",
        stock_num=0,
        stock_restock=False,
    ),
    # ========================================
    # 攻撃系スクロール（氷）
    # ========================================
    "sukutsu_scroll_attack_cold": ItemDefinition(
        id="sukutsu_scroll_attack_cold",
        name_jp="絶対零度の巻物",
        name_en="Scroll of Absolute Zero",
        name_cn="绝对零度卷轴",
        category="scroll",
        sort="book_scroll",
        quality="4",
        filter="start",
        detail_jp="永久凍土の精霊が封じられた極寒の巻物。読み上げると周囲の温度が瞬時に氷点下に達し、全ての敵が凍てつく。保管には特殊な魔法の容器が必要。",
        detail_en="A frozen scroll containing the spirit of permafrost. When read, the surrounding temperature instantly drops below freezing and all enemies are frozen. Special magical containers are required for storage.",
        detail_cn="封印着永久冻土精灵的极寒卷轴。诵读时周围温度瞬间降至冰点以下，所有敌人都会被冻结。保存需要特殊的魔法容器。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuScrollAttack",
        trait_params=["eleCold", "500"],
        effect=ItemEffect(),
        value=50000,
        lv=50,
        weight=50,
        tiles=1613,
        render_data="obj_S flat",
        def_mat="paper",
        tags=["neg"],
        sell_at=None,
        stock_rarity="Normal",
        stock_num=0,
        stock_restock=False,
    ),
    # ========================================
    # 攻撃系スクロール（雷）
    # ========================================
    "sukutsu_scroll_attack_lightning": ItemDefinition(
        id="sukutsu_scroll_attack_lightning",
        name_jp="天雷轟く巻物",
        name_en="Scroll of Divine Thunder",
        name_cn="天雷轰鸣卷轴",
        category="scroll",
        sort="book_scroll",
        quality="4",
        filter="start",
        detail_jp="雷神の怒りを封じた神罰の巻物。読み上げると無数の稲妻が天から降り注ぎ、逃れる術なく全ての敵を撃ち抜く。使用者も感電の危険がある。",
        detail_en="A divine punishment scroll containing the wrath of the thunder god. When read, countless lightning bolts rain from the sky, striking down all enemies with no escape. The user also risks electrocution.",
        detail_cn="封印着雷神之怒的神罚卷轴。诵读时无数闪电从天而降，毫无逃脱之法地击穿所有敌人。使用者也有触电的危险。",
        trait_type=TraitType.CUSTOM,
        trait_name="SukutsuScrollAttack",
        trait_params=["eleLightning", "500"],
        effect=ItemEffect(),
        value=50000,
        lv=50,
        weight=50,
        tiles=1613,
        render_data="obj_S flat",
        def_mat="paper",
        tags=["neg"],
        sell_at=None,
        stock_rarity="Normal",
        stock_num=0,
        stock_restock=False,
    ),
}

# マップ兵器スクロールをCUSTOM_ITEMSにマージ
CUSTOM_ITEMS.update(MAP_WEAPON_SCROLLS)


# ============================================================================
# バリデーション
# ============================================================================


def validate_items() -> List[str]:
    """アイテム定義のバリデーション"""
    errors = []

    for item_id, item in CUSTOM_ITEMS.items():
        prefix = f"CUSTOM_ITEMS['{item_id}']"

        # ID一致チェック
        if item.id != item_id:
            errors.append(f"{prefix}: id が key と一致しません")

        # 必須フィールド
        if not item.name_jp:
            errors.append(f"{prefix}: name_jp が空です")
        if not item.name_en:
            errors.append(f"{prefix}: name_en が空です")

        # カスタムTraitの場合、effectが必要
        if item.trait_type == TraitType.CUSTOM and item.effect is None:
            errors.append(f"{prefix}: カスタムTraitですが effect が定義されていません")

        # 販売設定の整合性
        if item.sell_at and item.stock_rarity not in [
            "Random",
            "Normal",
            "Superior",
            "Legendary",
            "Mythical",
            "Artifact",
        ]:
            errors.append(f"{prefix}: stock_rarity が不正です: {item.stock_rarity}")

    return errors


def get_items_by_seller(seller_id: str) -> List[ItemDefinition]:
    """特定のNPCが販売するアイテムを取得"""
    return [item for item in CUSTOM_ITEMS.values() if item.sell_at == seller_id]
