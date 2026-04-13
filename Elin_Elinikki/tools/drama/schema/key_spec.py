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

    # Trace-examine event flags. Set by chapter 1-3 examine dramas when the
    # player first investigates the corresponding trace. Read in chapter 4
    # to unlock the matching truth conversation with Yuu, and counted at
    # chapter 5 to resolve the ending.
    # trace_echo is set when the echo experiment reaches stage 4 (chapter 2);
    # it lives alongside the pure examine traces for parity with the story
    # spec (story/chapters/_index.md "quest.event.trace_*" table).
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_MARKS",
        "chitsii.elinikki.quest.event.trace_marks",
        description="Wall markings examined (chapter 1).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_CHANNEL",
        "chitsii.elinikki.quest.event.trace_channel",
        description="Water channel examined (chapter 1).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_STONES",
        "chitsii.elinikki.quest.event.trace_stones",
        description="Sorted stones examined (chapter 1).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_ECHO",
        "chitsii.elinikki.quest.event.trace_echo",
        description="Echo response experiment cleared (chapter 2, stage 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_MAP",
        "chitsii.elinikki.quest.event.trace_map",
        description="Floor map examined (chapter 2).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_SHADOW",
        "chitsii.elinikki.quest.event.trace_shadow",
        description="Wall shadow examined (chapter 2).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_FLOWERS",
        "chitsii.elinikki.quest.event.trace_flowers",
        description="Flower roots examined (chapter 3).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRACE_WEAVE",
        "chitsii.elinikki.quest.event.trace_weave",
        description="Woven cord examined (chapter 3).",
    ),

    # Echo experiment state. Progresses 0 -> 4 across the four chapter-2
    # echo dramas. Used as a gate so each stage drama fires exactly once,
    # in order. Stage 4 also sets quest.event.trace_echo.
    KeySpec(
        "flag",
        "ELINIKKI_ECHO_EXPERIMENT",
        "chitsii.elinikki.quest.state.echo_experiment",
        description="Echo experiment stage (int 0-4, chapter 2).",
    ),

    # Truth flags. Set when the player asks Yuu about the corresponding
    # trace in the chapter-4 reunion. Read at chapter 5 to resolve the
    # ending (8/8 -> return, 0/8 -> silence, 1-7/8 -> silence with
    # "partial knowledge" flavor).
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_MARKS",
        "chitsii.elinikki.quest.event.truth_marks",
        description="Wall-markings truth heard from Yuu (chapter 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_CHANNEL",
        "chitsii.elinikki.quest.event.truth_channel",
        description="Water channel truth heard from Yuu (chapter 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_STONES",
        "chitsii.elinikki.quest.event.truth_stones",
        description="Sorted stones truth heard from Yuu (chapter 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_ECHO",
        "chitsii.elinikki.quest.event.truth_echo",
        description="Echo response truth heard from Yuu (chapter 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_MAP",
        "chitsii.elinikki.quest.event.truth_map",
        description="Floor map truth heard from Yuu (chapter 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_SHADOW",
        "chitsii.elinikki.quest.event.truth_shadow",
        description="Wall shadow truth heard from Yuu (chapter 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_FLOWERS",
        "chitsii.elinikki.quest.event.truth_flowers",
        description="Flower roots truth heard from Yuu (chapter 4).",
    ),
    KeySpec(
        "flag",
        "ELINIKKI_TRUTH_WEAVE",
        "chitsii.elinikki.quest.event.truth_weave",
        description="Woven cord truth heard from Yuu (chapter 4).",
    ),

    # Ending bucket (int 0-3). Set by the chapter-5 ending dramas.
    #   0 = none, 1 = return, 2 = silence, 3 = revisit (hidden)
    # Matches the "quest.ending" table in story/chapters/_index.md.
    KeySpec(
        "flag",
        "ELINIKKI_QUEST_ENDING",
        "chitsii.elinikki.quest.ending",
        description="Ending reached (0 none, 1 return, 2 silence, 3 revisit).",
    ),

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

    # Audio commands. PlayBgm/PlaySe use prefix-based keys with the
    # BGM/SE id embedded in the suffix so adding a new audio asset
    # does not require a new constant here — Task 4.2's per-layer
    # BGM mapping and Task 4.3's echo SE trigger points build their
    # keys dynamically. Stop is the only fixed-suffix audio command
    # because it takes no argument.
    KeySpec(
        "command",
        "ELINIKKI_AUDIO_BGM_STOP",
        "cmd.elinikki.audio.bgm.stop",
        description="Stops the current BGM and releases the drama layer's volume hold.",
    ),
]
