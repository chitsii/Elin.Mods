# -*- coding: utf-8 -*-
"""
data.py - Drama constants for Elinikki "帰らなかった遠足" quest.

Drama ids here MUST match the string literals in the C# side:
- ElinikkiQuestFlow.IntroDramaId (src/Quest/Quest/ElinikkiQuestFlow.cs)
- Any future stage-advance triggers in the drama resolver
"""


class Actors:
    PC = "pc"
    NARRATOR = "narrator"
    # Party members. Matches character ids in story/characters/_index.md.
    # When adding custom CWL characters, register the real SourcePerson id
    # here so the drama points at the correct Chara row at runtime.
    MINA = "narrator"  # TODO: swap to real Mina character id once authored
    SORA = "narrator"  # TODO: swap to real Sora character id once authored
    YUU = "narrator"   # TODO: swap to real Yuu character id once authored
    # Safe fallback: the built-in narrator prevents missing-Person errors
    # until the real character rows are registered.
    GUIDE = NARRATOR


class DramaIds:
    # Chapter 0: Mina arrives at the player's home to deliver the quest.
    QUEST_INTRO = "elinikki_quest_intro"

    # Chapter 1: Waterstone layer trace examine dramas.
    TRACE_MARKS = "elinikki_trace_marks"
    TRACE_CHANNEL = "elinikki_trace_channel"
    TRACE_STONES = "elinikki_trace_stones"
    TRACE_JOURNAL = "elinikki_trace_journal"

    # Chapter 2: Echo layer experiment stages (one drama per point) plus
    # the map and shadow traces.
    ECHO_STAGE_1 = "elinikki_echo_stage_1"
    ECHO_STAGE_2 = "elinikki_echo_stage_2"
    ECHO_STAGE_3 = "elinikki_echo_stage_3"
    ECHO_STAGE_4 = "elinikki_echo_stage_4"
    TRACE_MAP = "elinikki_trace_map"
    TRACE_SHADOW = "elinikki_trace_shadow"

    # Chapter 3: Bloom layer traces.
    TRACE_FLOWERS = "elinikki_trace_flowers"
    TRACE_WEAVE = "elinikki_trace_weave"

    # Chapter 4: Reunion with Yuu and the 8 truth-reveal conversations.
    REUNION = "elinikki_reunion"
    TRUTH_MARKS = "elinikki_truth_marks"
    TRUTH_CHANNEL = "elinikki_truth_channel"
    TRUTH_STONES = "elinikki_truth_stones"
    TRUTH_ECHO = "elinikki_truth_echo"
    TRUTH_MAP = "elinikki_truth_map"
    TRUTH_SHADOW = "elinikki_truth_shadow"
    TRUTH_FLOWERS = "elinikki_truth_flowers"
    TRUTH_WEAVE = "elinikki_truth_weave"

    # Chapter 5: Return journey + the two named endings.
    RETURN_JOURNEY = "elinikki_return_journey"
    ENDING_RETURN = "elinikki_ending_return"
    ENDING_SILENCE = "elinikki_ending_silence"

    ALL = [
        QUEST_INTRO,
        TRACE_MARKS,
        TRACE_CHANNEL,
        TRACE_STONES,
        TRACE_JOURNAL,
        ECHO_STAGE_1,
        ECHO_STAGE_2,
        ECHO_STAGE_3,
        ECHO_STAGE_4,
        TRACE_MAP,
        TRACE_SHADOW,
        TRACE_FLOWERS,
        TRACE_WEAVE,
        REUNION,
        TRUTH_MARKS,
        TRUTH_CHANNEL,
        TRUTH_STONES,
        TRUTH_ECHO,
        TRUTH_MAP,
        TRUTH_SHADOW,
        TRUTH_FLOWERS,
        TRUTH_WEAVE,
        RETURN_JOURNEY,
        ENDING_RETURN,
        ENDING_SILENCE,
    ]


# Generated key interfaces (single source: tools/drama/schema/key_spec.py)
from .data_generated import CommandKeys, CueKeys, FlagKeys, ResolveKeys
