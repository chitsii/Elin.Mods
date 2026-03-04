from dataclasses import dataclass
from typing import List


@dataclass(frozen=True)
class ThingDefinition:
    id: str
    name_jp: str
    name_en: str
    name_cn: str
    detail_jp: str
    detail_en: str
    detail_cn: str
    category: str = "mech"
    sort: str = "furniture_mech"
    tile_type: str = "ObjBig"
    render_data: str = "obj"
    tiles: str = "3033"
    value: int = 14000
    weight: int = 120000
    electricity: int = -10
    trait: str = "SlotMachine"


THING_DEFINITIONS: List[ThingDefinition] = [
    ThingDefinition(
        id="justdoomit_arcade",
        name_jp="アーケード筐体: DOOM",
        name_en="Arcade Cabinet: DOOM",
        name_cn="街机柜: DOOM",
        detail_jp="Doomをプレイできるアーケード筐体。",
        detail_en="An arcade cabinet that launches FreeDoom.",
        detail_cn="可启动 FreeDoom 的街机柜。",
    ),
]
