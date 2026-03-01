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
    KeySpec("flag", "BOOTSTRAPPED", "yourname.elin_quest_mod.flag.bootstrapped"),
    KeySpec("flag", "PLACEHOLDER_DONE", "yourname.elin_quest_mod.flag.placeholder_done"),
    KeySpec("flag", "TMP_CAN_START_FEATURE", "yourname.elin_quest_mod.tmp.can_start.feature_showcase"),
    KeySpec("flag", "TMP_IS_DONE_FEATURE", "yourname.elin_quest_mod.tmp.is_done.feature_showcase"),
    KeySpec("flag", "TMP_BRANCH_FEATURE", "yourname.elin_quest_mod.tmp.branch.feature_showcase"),
    KeySpec("flag", "TMP_COUNT_FEATURE", "yourname.elin_quest_mod.tmp.count.feature_showcase"),
    # Resolve keys
    KeySpec("resolve", "QUEST_CAN_START_PLACEHOLDER", "state.quest.can_start.quest_drama_replace_me"),
    KeySpec("resolve", "QUEST_DONE_PLACEHOLDER", "state.quest.is_done.quest_drama_replace_me"),
    KeySpec("resolve", "QUEST_CAN_START_FEATURE", "state.quest.can_start.quest_drama_feature_showcase"),
    KeySpec("resolve", "QUEST_DONE_FEATURE", "state.quest.is_done.quest_drama_feature_showcase"),
    KeySpec(
        "resolve",
        "QUEST_CAN_START_FEATURE_FOLLOWUP",
        "state.quest.can_start.quest_drama_feature_followup",
    ),
    KeySpec(
        "resolve",
        "QUEST_DONE_FEATURE_FOLLOWUP",
        "state.quest.is_done.quest_drama_feature_followup",
    ),
    # Command keys
    KeySpec("command", "QUEST_TRY_START_PLACEHOLDER", "cmd.quest.try_start.quest_drama_replace_me"),
    KeySpec("command", "QUEST_COMPLETE_PLACEHOLDER", "cmd.quest.complete.quest_drama_replace_me"),
    KeySpec("command", "QUEST_TRY_START_FEATURE", "cmd.quest.try_start.quest_drama_feature_showcase"),
    KeySpec("command", "QUEST_TRY_START_FEATURE_REPEATABLE", "cmd.quest.try_start_repeatable.quest_drama_feature_showcase"),
    KeySpec("command", "QUEST_TRY_START_FEATURE_UNTIL_COMPLETE", "cmd.quest.try_start_until_complete.quest_drama_feature_showcase"),
    KeySpec("command", "QUEST_COMPLETE_FEATURE", "cmd.quest.complete.quest_drama_feature_showcase"),
    KeySpec(
        "command",
        "QUEST_TRY_START_FEATURE_FOLLOWUP",
        "cmd.quest.try_start.quest_drama_feature_followup",
    ),
    KeySpec(
        "command",
        "QUEST_COMPLETE_FEATURE_FOLLOWUP",
        "cmd.quest.complete.quest_drama_feature_followup",
    ),
    # Cue keys
    KeySpec("cue", "PLACEHOLDER_PULSE", "cue.questmod.placeholder_pulse"),
    KeySpec("cue", "FEATURE_SHOWCASE_PULSE", "cue.questmod.feature_showcase_pulse"),
]
