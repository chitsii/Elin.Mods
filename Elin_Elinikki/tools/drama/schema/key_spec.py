from dataclasses import dataclass
from typing import Optional


@dataclass(frozen=True)
class KeySpec:
    kind: str  # flag | resolve | command | cue
    name: str
    value: str
    description: str = ""
    deprecated_alias_of: Optional[str] = None


# Keys used by the Elinikki "帰らなかった遠足" drama scripts.
# Flag values MUST stay aligned with story/chapters/_index.md — any change
# here requires the story spec and the C# side
# (ElinikkiQuestStage / QuestDramaResolver) to be updated in lockstep.
KEY_SPECS = [
    # ---------------------------------------------------------------------
    # Flags (save-persisted dialogFlags, chitsii.elinikki.quest.*)
    # ---------------------------------------------------------------------
    # Temporary flags used by the intro drama to stash results before
    # branching. These do not persist beyond one drama run but still live
    # in dialogFlags so the drama DSL can read them.
    KeySpec("flag", "TMP_INTRO_CAN_START", "chitsii.elinikki.tmp.intro.can_start"),

    # ---------------------------------------------------------------------
    # Resolve keys (bool-returning dependencies)
    # ---------------------------------------------------------------------
    KeySpec(
        "resolve",
        "ELINIKKI_STAGE_AT_LEAST_ACCEPTED",
        "state.elinikki.stage.at_least.accepted",
        description="Returns true when quest.stage >= Accepted.",
    ),
    KeySpec(
        "resolve",
        "ELINIKKI_STAGE_AT_LEAST_LAYER1_CLEAR",
        "state.elinikki.stage.at_least.layer1_clear",
    ),
    KeySpec(
        "resolve",
        "ELINIKKI_STAGE_AT_LEAST_LAYER2_CLEAR",
        "state.elinikki.stage.at_least.layer2_clear",
    ),
    KeySpec(
        "resolve",
        "ELINIKKI_STAGE_AT_LEAST_LAYER3_CLEAR",
        "state.elinikki.stage.at_least.layer3_clear",
    ),
    KeySpec(
        "resolve",
        "ELINIKKI_STAGE_AT_LEAST_YUU_FOUND",
        "state.elinikki.stage.at_least.yuu_found",
    ),
    KeySpec(
        "resolve",
        "ELINIKKI_STAGE_AT_LEAST_RETURNED",
        "state.elinikki.stage.at_least.returned",
    ),
    KeySpec(
        "resolve",
        "ELINIKKI_STAGE_AT_LEAST_ENDING_SEEN",
        "state.elinikki.stage.at_least.ending_seen",
    ),

    # ---------------------------------------------------------------------
    # Command keys (execute-side dependencies)
    # ---------------------------------------------------------------------
    KeySpec(
        "command",
        "ELINIKKI_STAGE_ADVANCE_ACCEPTED",
        "cmd.elinikki.stage.advance.accepted",
        description="Advances quest.stage to Accepted. Chapter-0 intro drama calls this at the end.",
    ),
    KeySpec(
        "command",
        "ELINIKKI_STAGE_ADVANCE_YUU_FOUND",
        "cmd.elinikki.stage.advance.yuu_found",
        description="Advances quest.stage to YuuFound. Chapter-4 reunion drama calls this.",
    ),
    KeySpec(
        "command",
        "ELINIKKI_STAGE_ADVANCE_ENDING_SEEN",
        "cmd.elinikki.stage.advance.ending_seen",
        description="Advances quest.stage to EndingSeen. Chapter-5 ending dramas call this.",
    ),
]
