# -*- coding: utf-8 -*-
"""
Chapter-4 post-reunion dialogue menu drama.

Runs automatically after ``elinikki_reunion`` closes and re-opens
after every completed truth conversation, until the player selects
"leave" or walks out of YuuCamp. Wired from
``ElinikkiQuestFlow.TryDispatchTruthMenu`` (C#), triggered on the
drama-close hook ``Patch_LayerDrama_OnKill_QuestPulse``.

Selection hand-off is via two transient dialogFlags:

* ``chitsii.elinikki.tmp.truth_menu.slot`` — set to the picked truth
  slot (1..8); read and cleared by
  ``ElinikkiQuestFlow.TryDispatchPendingTruth`` which then launches
  the matching ``elinikki_truth_<topic>`` drama.
* ``chitsii.elinikki.tmp.truth_menu.dismissed`` — set to 1 when the
  player picks "また後で"; suppresses menu re-dispatch until the
  player exits YuuCamp. ``ElinikkiQuestFlow`` clears both flags on
  any Pulse whose zone id is NOT YuuCamp.

Truth topics are shown unconditionally (all 8 + leave) in this MVP.
Filter-by-trace and filter-by-seen-truth are deferred to a future
polish pass — the truth dramas are idempotent in content and
re-hearing one is at worst a minor UX wart, not a bug.

Actors: all Yuu lines in this menu use the shared narrator alias
because real Chara rows for the party aren't registered yet
(tools/drama/data.py Actors.YUU == narrator).
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


# Slot encoding matches ElinikkiQuestFlow.TruthDramaIds[] in
# src/Quest/Quest/ElinikkiQuestFlow.cs. Keep these in lockstep:
# changing the order here or there without updating both sides
# will route a pick to the wrong truth drama.
_SLOT_MARKS = 1
_SLOT_CHANNEL = 2
_SLOT_STONES = 3
_SLOT_ECHO = 4
_SLOT_MAP = 5
_SLOT_SHADOW = 6
_SLOT_FLOWERS = 7
_SLOT_WEAVE = 8


def define_elinikki_reunion_menu(builder: DramaBuilder) -> None:
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    main = builder.label("main")
    pick_marks = builder.label("pick_marks")
    pick_channel = builder.label("pick_channel")
    pick_stones = builder.label("pick_stones")
    pick_echo = builder.label("pick_echo")
    pick_map = builder.label("pick_map")
    pick_shadow = builder.label("pick_shadow")
    pick_flowers = builder.label("pick_flowers")
    pick_weave = builder.label("pick_weave")
    leave = builder.label("leave")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)

    builder.step(main)
    builder.set_dialog_style("Window")
    builder.say(
        "truth_menu_prompt",
        "ユウ:「まだ聞きたいこと、あるか？」",
        "Yuu: 'Anything else you want to ask?'",
        "",
        actor=yuu,
    )

    builder.choice(pick_marks, "壁の刻み目について", "About the wall markings")
    builder.choice(pick_channel, "水路について", "About the water channel")
    builder.choice(pick_stones, "並んだ石について", "About the sorted stones")
    builder.choice(pick_echo, "反響について", "About the echoes")
    builder.choice(pick_map, "床の地図について", "About the floor map")
    builder.choice(pick_shadow, "壁の影について", "About the wall shadow")
    builder.choice(pick_flowers, "花について", "About the flowers")
    builder.choice(pick_weave, "編まれた紐について", "About the woven cord")
    builder.choice(leave, "また後で", "Maybe later")
    builder.on_cancel(leave)

    # Per-topic target steps. Each sets the slot flag and jumps to
    # end; the drama-close hook reads the slot and launches the
    # corresponding elinikki_truth_* drama.
    _emit_pick_step(builder, pick_marks, _SLOT_MARKS, end)
    _emit_pick_step(builder, pick_channel, _SLOT_CHANNEL, end)
    _emit_pick_step(builder, pick_stones, _SLOT_STONES, end)
    _emit_pick_step(builder, pick_echo, _SLOT_ECHO, end)
    _emit_pick_step(builder, pick_map, _SLOT_MAP, end)
    _emit_pick_step(builder, pick_shadow, _SLOT_SHADOW, end)
    _emit_pick_step(builder, pick_flowers, _SLOT_FLOWERS, end)
    _emit_pick_step(builder, pick_weave, _SLOT_WEAVE, end)

    builder.step(leave)
    builder.set_flag(FlagKeys.TMP_TRUTH_MENU_DISMISSED, 1)
    builder.jump(end)

    builder.step(end)
    builder.drama_end(0.3)


def _emit_pick_step(
    builder: DramaBuilder,
    step_label,
    slot: int,
    end_label,
) -> None:
    """Emit a single 'set slot + jump end' step for one truth pick.

    Wrapped in a helper so the main flow reads as a flat list of
    choice wire-ups instead of a repeated three-call block.
    """
    builder.step(step_label)
    builder.set_flag(FlagKeys.TMP_TRUTH_MENU_SLOT, slot)
    builder.jump(end_label)
