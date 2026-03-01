# -*- coding: utf-8 -*-
"""
data.py - Drama constants for QuestMod template.
Replace these IDs with your mod-specific IDs after copying the template.
"""


class Actors:
    PC = "pc"
    NARRATOR = "narrator"
    # Use built-in narrator as safe default actor to avoid missing Person ID issues.
    GUIDE = NARRATOR


class DramaIds:
    PLACEHOLDER = "quest_drama_replace_me"
    FEATURE_SHOWCASE = "quest_drama_feature_showcase"
    FEATURE_SHOWCASE_FOLLOWUP = "quest_drama_feature_followup"
    FEATURE_BRANCH_A = "quest_drama_feature_branch_a"
    FEATURE_BRANCH_B = "quest_drama_feature_branch_b"
    FEATURE_MERGE = "quest_drama_feature_merge"

    ALL = [
        PLACEHOLDER,
        FEATURE_SHOWCASE,
        FEATURE_SHOWCASE_FOLLOWUP,
        FEATURE_BRANCH_A,
        FEATURE_BRANCH_B,
        FEATURE_MERGE,
    ]


# Generated key interfaces (single source: tools/drama/schema/key_spec.py)
from .data_generated import CommandKeys, CueKeys, FlagKeys, ResolveKeys
