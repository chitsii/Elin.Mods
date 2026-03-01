# -*- coding: utf-8 -*-
"""
data.py - Drama constants (actor IDs, drama IDs)
"""


class Actors:
    PC = "pc"
    NARRATOR = "narrator"  # The forbidden tome's narrator
    KAREN = "ars_karen"
    ERENOS = "ars_erenos_shadow"
    ERENOS_PET = "ars_erenos_pet"
    HECATIA = "ars_hecatia"


class BGM:
    # 自前BGM（常に利用可能）
    BATTLE = "BGM/AshAndHolyLance"
    REQUIEM = "BGM/TheFadingSignature"
    REVELATION = "BGM/ManuscriptByCandlelight"
    HECATIA_RAP = "BGM/Hecatia_Rap_Edit"
    # SukutsuArena BGM（フォールバック付き）
    OMINOUS = "BGM/Ominous_Suspense_01"
    SORROW = "BGM/Emotional_Sorrow"
    RITUAL = "BGM/Mystical_Ritual"


class DramaIds:
    FIRST_SOUL = "ars_first_soul"
    TOME_AWAKENING = "ars_tome_awakening"
    KAREN_ENCOUNTER = "ars_karen_encounter"
    KAREN_RETREAT = "ars_karen_retreat"
    CINDER_RECORDS = "ars_cinder_records"
    SCOUT_ENCOUNTER = "ars_scout_encounter"
    STIGMATA = "ars_stigmata"
    ERENOS_APPEAR = "ars_erenos_appear"
    ERENOS_DEFEAT = "ars_erenos_defeat"
    APOTHEOSIS = "ars_apotheosis"
    FIRST_SERVANT = "ars_first_servant"
    SERVANT_RAMPAGE = "ars_servant_rampage"
    SERVANT_LOST = "ars_servant_lost"
    DORMANT_FLAVOR = "ars_dormant_flavor"
    KAREN_SHADOW = "ars_karen_shadow"
    SEVENTH_SIGN = "ars_seventh_sign"
    KAREN_AMBUSH = "ars_karen_ambush"
    ERENOS_AMBUSH = "ars_erenos_ambush"
    SCOUT_AMBUSH = "ars_scout_ambush"
    HECATIA_TALK = "ars_hecatia"

    ALL = [
        FIRST_SOUL,
        TOME_AWAKENING,
        KAREN_ENCOUNTER,
        KAREN_RETREAT,
        CINDER_RECORDS,
        SCOUT_ENCOUNTER,
        STIGMATA,
        ERENOS_APPEAR,
        ERENOS_DEFEAT,
        APOTHEOSIS,
        FIRST_SERVANT,
        SERVANT_RAMPAGE,
        SERVANT_LOST,
        DORMANT_FLAVOR,
        KAREN_SHADOW,
        SEVENTH_SIGN,
        KAREN_AMBUSH,
        ERENOS_AMBUSH,
        SCOUT_AMBUSH,
        HECATIA_TALK,
    ]

# Generated key interfaces (single source: tools/drama/schema/key_spec.py)
from .data_generated import FlagKeys, ResolveKeys, CommandKeys, CueKeys
