"""
SourceStat.xlsx 生成スクリプト

カスタムConditionをゲームに登録するためのTSVファイルを生成する。
build.batでsofficeによりxlsxに変換される。
EN版とCN版の両方を生成。
"""

import os

# パス設定
BUILDER_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.dirname(BUILDER_DIR)
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

OUTPUT_EN = os.path.join(PROJECT_ROOT, "LangMod", "EN", "Stat.tsv")
OUTPUT_CN = os.path.join(PROJECT_ROOT, "LangMod", "CN", "Stat.tsv")

# SourceStat のヘッダー列（SourceStat.cs より）
HEADERS = [
    "id",
    "alias",
    "name_JP",
    "name",
    "type",
    "group",
    "curse",
    "duration",
    "hexPower",
    "negate",
    "defenseAttb",
    "resistance",
    "gainRes",
    "elements",
    "nullify",
    "tag",
    "phase",
    "colors",
    "element",
    "effect",
    "strPhase_JP",
    "strPhase",
    "textPhase_JP",
    "textPhase",
    "textEnd_JP",
    "textEnd",
    "textPhase2_JP",
    "textPhase2",
    "gradient",
    "invert",
    "detail_JP",
    "detail",
]

# 型情報（2行目）
TYPES = [
    "int",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "int",
    "string[]",
    "string[]",
    "string[]",
    "int",
    "string[]",
    "string[]",
    "string[]",
    "int[]",
    "string",
    "string",
    "string[]",
    "string[]",
    "string[]",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "string",
    "bool",
    "string",
    "string",
]

# デフォルト値（3行目）- バニラに合わせる
# 注意: elementsはデフォルト値を設定しない（カスタムConditionで問題が発生するため）
# MODのカスタムConditionは明示的にidを指定するので、デフォルト値はバニラと同じでOK
DEFAULTS = {
    "id": "103",  # バニラのデフォルト
    "group": "Neutral",
    "duration": "p/10",
    "hexPower": "10",
    "phase": "0,1,2,3,4,5,6,7,8,9",  # バニラのデフォルト
    "gradient": "condition",
}

# CN翻訳辞書
CN_TRANSLATIONS = {
    "時の独裁": "时之独裁",
    "因果の拒絶": "因果之拒绝",
    "魔の断絶": "魔之断绝",
    "冷気耐性": "寒冷抗性",
    "幻惑耐性": "幻惑抗性",
    "混沌耐性": "混沌抗性",
    "轟音耐性": "轰鸣抗性",
    "衝撃耐性": "冲击抗性",
    "出血耐性": "出血抗性",
    "PV強化": "PV强化",
    "覚醒": "觉醒",
    "内出血": "内出血",
    "薬物中毒": "药物中毒",
    # detail translations
    "アスタロトの権能により、速度が断ち切られた。教会での治療が必要。": "因阿斯塔罗特的权能，速度被切断。需要在教会治疗。",
    "アスタロトの権能により、筋力が断ち切られた。教会での治療が必要。": "因阿斯塔罗特的权能，力量被切断。需要在教会治疗。",
    "アスタロトの権能により、魔力が断ち切られた": "因阿斯塔罗特的权能，魔力被切断。需要在教会治疗。",
    "冷気への耐性が上昇している。": "对寒冷的抗性上升中。",
    "幻惑への耐性が上昇している。": "对幻惑的抗性上升中。",
    "混沌への耐性が上昇している。": "对混沌的抗性上升中。",
    "轟音への耐性が上昇している。": "对轰鸣的抗性上升中。",
    "衝撃への耐性が上昇している。": "对冲击的抗性上升中。",
    "出血への耐性が上昇している。": "对出血的抗性上升中。",
    "一時的にPVが上昇している。": "PV暂时上升中。",
    "身体能力が一時的に大幅上昇している。副作用に注意。": "身体能力暂时大幅上升。注意副作用。",
    "覚醒剤の副作用で内出血している。毎ターンダメージを受ける。": "兴奋剂副作用导致内出血。每回合受到伤害。",
    "痛覚遮断薬の副作用で中毒状態。自然回復が阻害され、時々吐く。": "痛觉阻断药副作用导致中毒状态。自然恢复被阻碍，偶尔呕吐。",
    # textEnd/textPhase2 translations
    "#1は耐性の加護を得た。": "#1获得了抗性的加护。",
    "#1の耐性の加護が消えた。": "#1的抗性加护消失了。",
    "#1の防御力が上昇した。": "#1的防御力上升了。",
    "#1の防御力が元に戻った。": "#1的防御力恢复原状。",
    "#1は力に目覚めた！": "#1觉醒了力量！",
    "#1の覚醒が終わった。": "#1的觉醒结束了。",
    "#1は内出血を起こした。": "#1发生了内出血。",
    "#1の内出血が止まった。": "#1的内出血停止了。",
    "#1は薬物中毒を起こした。": "#1发生了药物中毒。",
    "#1の薬物中毒が治った。": "#1的药物中毒治愈了。",
    # Iris training buffs
    "第六感": "第六感",
    "鉄の足腰": "铁腿",
    "境界の安らぎ": "边界的安宁",
    "感覚が研ぎ澄まされ、視認範囲が拡大している。": "感官变得敏锐，视野范围扩大。",
    "足腰が安定し、完全回避率が上昇している。": "腿脚稳定，完全回避率上升。",
    "体が温まり、スタミナが自然回復している。": "身体温暖，体力自然恢复。",
    "#1は感覚が研ぎ澄まされた！": "#1的感官变得敏锐了！",
    "#1の感覚が元に戻った。": "#1的感官恢复正常。",
    "#1は足腰が安定した！": "#1的腿脚变得稳定！",
    "#1の足腰が元に戻った。": "#1的腿脚恢复正常。",
    "#1は体が温まった！": "#1的身体变得温暖！",
    "#1の温もりが消えた。": "#1的温暖消失了。",
}


def translate_cn(text):
    """JP テキストを CN に翻訳"""
    return CN_TRANSLATIONS.get(text, text)


# 耐性バフの共通設定
def make_resist_buff(id, alias, name_jp, name_en, element_alias, detail_jp, detail_en):
    """耐性バフConditionの定義を生成"""
    return {
        "id": str(id),
        "alias": alias,
        "name_JP": name_jp,
        "name": name_en,
        "type": "BaseBuff",  # バニラのBaseBuffクラスを使用（GetPhase=>0でphase配列不要）
        "group": "Buff",
        "duration": "300",  # 固定300ターン（絶対耐性のため短め）
        "phase": "",  # BaseBuffはGetPhase=>0なので不要
        "colors": "buff",
        "elements": f"{element_alias},15",  # エレメントと値をカンマ区切りで指定
        "textEnd_JP": "#1は耐性の加護を得た。",
        "textEnd": "#1 gains resistance protection.",
        "textPhase2_JP": "#1の耐性の加護が消えた。",
        "textPhase2": "#1's resistance protection fades.",
        "invert": "TRUE",
        "detail_JP": detail_jp,
        "detail": detail_en,
    }


# カスタムCondition定義
CONDITIONS = [
    # アスタロトの権能（永続ダメージはC#側で付与）
    {
        "id": "10001",
        "alias": "ConAstarothTyranny",
        "name_JP": "時の独裁",
        "name": "Tyranny of Time",
        "type": "Elin_SukutsuArena.Conditions.ConAstarothTyranny",
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "elements": "",  # 永続ダメージはC#側で付与
        "detail_JP": "アスタロトの権能により、速度が断ち切られた。",
        "detail": "Your speed is severed by Astaroth's power. Requires healing at a church.",
    },
    {
        "id": "10002",
        "alias": "ConAstarothDenial",
        "name_JP": "因果の拒絶",
        "name": "Denial of Causality",
        "type": "Elin_SukutsuArena.Conditions.ConAstarothDenial",
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "elements": "",  # 永続ダメージはC#側で付与
        "detail_JP": "アスタロトの権能により、筋力が断ち切られた。",
        "detail": "Your strength is severed by Astaroth's power. Requires healing at a church.",
    },
    {
        "id": "10003",
        "alias": "ConAstarothDeletion",
        "name_JP": "魔の断絶",
        "name": "Severance of Magic",
        "type": "Elin_SukutsuArena.Conditions.ConAstarothDeletion",
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "elements": "",  # 永続ダメージはC#側で付与
        "detail_JP": "アスタロトの権能により、魔力が断ち切られた",
        "detail": "Your magical power is severed by Astaroth's power. Requires healing at a church.",
    },
    # 個別の耐性バフ（バニラのConResEleパターン）
    make_resist_buff(
        10011,
        "ConSukutsuResCold",
        "冷気耐性",
        "Cold Resistance",
        "resCold",
        "冷気への耐性が上昇している。",
        "Resistance to cold is increased.",
    ),
    make_resist_buff(
        10012,
        "ConSukutsuResDarkness",
        "幻惑耐性",
        "Darkness Resistance",
        "resMind",
        "幻惑への耐性が上昇している。",
        "Resistance to mind is increased.",
    ),
    make_resist_buff(
        10013,
        "ConSukutsuResChaos",
        "混沌耐性",
        "Chaos Resistance",
        "resChaos",
        "混沌への耐性が上昇している。",
        "Resistance to chaos is increased.",
    ),
    make_resist_buff(
        10014,
        "ConSukutsuResSound",
        "轟音耐性",
        "Sound Resistance",
        "resSound",
        "轟音への耐性が上昇している。",
        "Resistance to sound is increased.",
    ),
    make_resist_buff(
        10015,
        "ConSukutsuResImpact",
        "衝撃耐性",
        "Impact Resistance",
        "resImpact",
        "衝撃への耐性が上昇している。",
        "Resistance to impact is increased.",
    ),
    make_resist_buff(
        10016,
        "ConSukutsuResCut",
        "出血耐性",
        "Cut Resistance",
        "resCut",
        "出血への耐性が上昇している。",
        "Resistance to cut is increased.",
    ),
    # PVバフ（痛覚遮断薬用）
    {
        "id": "10020",
        "alias": "ConSukutsuPVBuff",
        "name_JP": "PV強化",
        "name": "PV Buff",
        "type": "BaseBuff",  # バニラのBaseBuffクラスを使用
        "group": "Buff",
        "duration": "500",  # 固定500ターン
        "phase": "",  # BaseBuffはGetPhase=>0なので不要
        "colors": "buff",
        "elements": "PV,50,resCut,75,resImpact,75",  # PV+50, 出血/衝撃耐性+75
        "textEnd_JP": "#1の防御力が上昇した。",
        "textEnd": "#1's defense is increased.",
        "textPhase2_JP": "#1の防御力が元に戻った。",
        "textPhase2": "#1's defense returns to normal.",
        "invert": "TRUE",
        "detail_JP": "一時的にPVが上昇している。",
        "detail": "PV is temporarily increased.",
    },
    # ブースト効果（禁断の覚醒剤用メリット）
    {
        "id": "10021",
        "alias": "ConSukutsuBoost",
        "name_JP": "覚醒",
        "name": "Awakening",
        "type": "Elin_SukutsuArena.Conditions.ConSukutsuBoost",
        "group": "Buff",
        "duration": "500",  # 固定500ターン
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "colors": "buff",
        "elements": "STR,666,END,666,DEX,666,SPD,99",  # 超強化（ハイリスク・ハイリターン）
        "textEnd_JP": "#1は力に目覚めた！",
        "textEnd": "#1 awakens to power!",
        "textPhase2_JP": "#1の覚醒が終わった。",
        "textPhase2": "#1's awakening ends.",
        "invert": "TRUE",
        "detail_JP": "身体能力が一時的に大幅上昇している。副作用に注意。",
        "detail": "Physical abilities are temporarily enhanced. Beware of side effects.",
    },
    # 出血効果（禁断の覚醒剤用デメリット）- elementsなし（Tickでダメージ処理）
    {
        "id": "10022",
        "alias": "ConSukutsuBleed",
        "name_JP": "内出血",
        "name": "Internal Bleeding",
        "type": "Elin_SukutsuArena.Conditions.ConSukutsuBleed",
        "group": "Bad",
        "duration": "500",  # 固定500ターン（メイン効果と同じ）
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "colors": "debuff",
        "elements": "",  # elementsなし（Tickでダメージ処理）
        "textEnd_JP": "#1は内出血を起こした。",
        "textEnd": "#1 suffers internal bleeding.",
        "textPhase2_JP": "#1の内出血が止まった。",
        "textPhase2": "#1's internal bleeding stops.",
        "detail_JP": "覚醒剤の副作用で内出血している。毎ターンダメージを受ける。",
        "detail": "Internal bleeding from stimulant side effects. Takes damage every turn.",
    },
    # 毒効果（痛覚遮断薬用デメリット）
    {
        "id": "10023",
        "alias": "ConSukutsuPoison",
        "name_JP": "薬物中毒",
        "name": "Drug Poisoning",
        "type": "Elin_SukutsuArena.Conditions.ConSukutsuPoison",
        "group": "Bad",
        "duration": "500",  # 固定500ターン（メイン効果と同じ）
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "colors": "debuff",
        "elements": "",  # 吐く効果はC#側で実装
        "textEnd_JP": "#1は薬物中毒を起こした。",
        "textEnd": "#1 suffers from drug poisoning.",
        "textPhase2_JP": "#1の薬物中毒が治った。",
        "textPhase2": "#1 recovers from drug poisoning.",
        "detail_JP": "痛覚遮断薬の副作用で中毒状態。自然回復が阻害され、時々吐く。",
        "detail": "Poisoned by painkiller side effects. Natural regeneration is blocked and occasionally vomit.",
    },
    # アイリス訓練バフ：第六感（視認範囲+4）
    {
        "id": "10030",
        "alias": "ConIrisSixthSense",
        "name_JP": "第六感",
        "name": "Sixth Sense",
        "type": "Elin_SukutsuArena.Conditions.ConIrisSixthSense",
        "group": "Buff",
        "duration": "1200",  # 固定1200ターン
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "colors": "buff",
        "elements": "",  # C#側でfarseeを付与
        "textEnd_JP": "#1は感覚が研ぎ澄まされた！",
        "textEnd": "#1's senses sharpen!",
        "textPhase2_JP": "#1の感覚が元に戻った。",
        "textPhase2": "#1's senses return to normal.",
        "invert": "TRUE",
        "detail_JP": "感覚が研ぎ澄まされ、視認範囲が拡大している。",
        "detail": "Senses are sharpened, expanding vision range.",
    },
    # アイリス訓練バフ：鉄の足腰（完全回避率+20%）
    {
        "id": "10031",
        "alias": "ConIrisIronLegs",
        "name_JP": "鉄の足腰",
        "name": "Iron Legs",
        "type": "Elin_SukutsuArena.Conditions.ConIrisIronLegs",
        "group": "Buff",
        "duration": "1200",  # 固定1200ターン
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "colors": "buff",
        "elements": "",  # C#側でevasionPerfectを付与
        "textEnd_JP": "#1は足腰が安定した！",
        "textEnd": "#1's legs become steady!",
        "textPhase2_JP": "#1の足腰が元に戻った。",
        "textPhase2": "#1's legs return to normal.",
        "invert": "TRUE",
        "detail_JP": "足腰が安定し、完全回避率が上昇している。",
        "detail": "Legs are stable, increasing perfect evasion rate.",
    },
    # アイリス訓練バフ：境界の安らぎ（スタミナ自然回復+1/ターン）
    {
        "id": "10032",
        "alias": "ConIrisBoundaryPeace",
        "name_JP": "境界の安らぎ",
        "name": "Boundary Peace",
        "type": "Elin_SukutsuArena.Conditions.ConIrisBoundaryPeace",
        "group": "Buff",
        "duration": "1200",  # 固定1200ターン
        "phase": "0,0,0,0,0,0,0,0,0,0",
        "colors": "buff",
        "elements": "",  # C#側のTickでスタミナ回復
        "textEnd_JP": "#1は体が温まった！",
        "textEnd": "#1's body warms up!",
        "textPhase2_JP": "#1の温もりが消えた。",
        "textPhase2": "#1's warmth fades.",
        "invert": "TRUE",
        "detail_JP": "体が温まり、スタミナが自然回復している。",
        "detail": "Body is warm, stamina naturally regenerates.",
    },
]


def create_tsv(output_path, lang="en"):
    """Stat.tsv を生成"""
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    lines = []

    # Row 1: ヘッダー
    lines.append("\t".join(HEADERS))

    # Row 2: 型情報
    lines.append("\t".join(TYPES))

    # Row 3: デフォルト値
    default_row = [DEFAULTS.get(h, "") for h in HEADERS]
    lines.append("\t".join(default_row))

    # Row 4+: データ
    for condition in CONDITIONS:
        row = []
        for h in HEADERS:
            value = str(condition.get(h, ""))
            # CN版: サフィックスなしの列（name, detail, textEnd, textPhase2）に中国語を入れる
            if lang == "cn":
                if h == "name":
                    value = translate_cn(condition.get("name_JP", ""))
                elif h == "detail":
                    value = translate_cn(condition.get("detail_JP", ""))
                elif h == "textEnd":
                    value = translate_cn(condition.get("textEnd_JP", ""))
                elif h == "textPhase2":
                    value = translate_cn(condition.get("textPhase2_JP", ""))
            row.append(value)
        lines.append("\t".join(row))

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"Created: {output_path}")


def main():
    print("Generating Stat.tsv for custom Conditions...")
    create_tsv(OUTPUT_EN, lang="en")
    create_tsv(OUTPUT_CN, lang="cn")
    print("Done! (EN + CN)")


if __name__ == "__main__":
    main()
