from dataclasses import dataclass
from typing import Optional


@dataclass(frozen=True)
class KeySpec:
    kind: str  # flag | resolve | command | cue
    name: str
    value: str
    description: str = ""
    deprecated_alias_of: Optional[str] = None


KEY_SPECS = [
    # Flags (save-persisted dialogFlags)
    KeySpec("flag", "HECATIA_REVEALED", "chitsii.ars.event.hecatia_revealed"),
    KeySpec("flag", "QUEST_COMPLETE", "chitsii.ars.quest.complete"),
    KeySpec("flag", "ERENOS_WITH_PLAYER", "chitsii.ars.event.erenos_with_player"),
    KeySpec("flag", "ERENOS_BORROWED", "chitsii.ars.event.erenos_borrowed"),
    KeySpec("flag", "ERENOS_DEFEATED", "chitsii.ars.quest.event.erenos_defeated"),
    KeySpec("flag", "KAREN_STAY", "chitsii.ars.karen_stay"),
    # Resolve keys
    KeySpec("resolve", "QUEST_IS_COMPLETE", "state.quest.is_complete"),
    KeySpec("resolve", "ERENOS_IS_BORROWED", "state.erenos.is_borrowed"),
    # Command keys
    KeySpec("command", "ERENOS_ENSURE_NEAR_PLAYER", "cmd.erenos.ensure_near_player"),
    KeySpec("command", "ERENOS_BORROW", "cmd.erenos.borrow"),
    KeySpec("command", "SCENE_STOP_BGM", "cmd.scene.stop_bgm"),
    KeySpec("command", "HECATIA_PARTY_SHOW", "cmd.hecatia.party_show"),
    # Cue keys
    KeySpec("cue", "APOTHEOSIS_SILENCE", "cue.apotheosis.silence"),
    KeySpec("cue", "APOTHEOSIS_DARKWOMB", "cue.apotheosis.darkwomb"),
    KeySpec("cue", "APOTHEOSIS_CURSE_BURST", "cue.apotheosis.curse_burst"),
    KeySpec("cue", "APOTHEOSIS_REVIVE", "cue.apotheosis.revive"),
    KeySpec("cue", "APOTHEOSIS_MUTATION", "cue.apotheosis.mutation"),
    KeySpec("cue", "APOTHEOSIS_TELEPORT_REBIRTH", "cue.apotheosis.teleport_rebirth"),
]
