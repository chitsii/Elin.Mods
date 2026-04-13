using System;
using Elin_Elinikki.Quest.Quest;
using HarmonyLib;

namespace Elin_Elinikki.Quest.Patches
{
    /// <summary>
    /// Per-frame hook that drives
    /// <see cref="ElinikkiTraceExaminer.Tick"/>. Elin does not
    /// expose a public "player just stepped to a new tile" event,
    /// and <see cref="Player.OnAdvanceHour"/> is too coarse for
    /// position-based proximity triggers (an hour tick only fires
    /// every 60+ turns, so the player can walk past a trace and
    /// back several times between ticks). Piggybacking on
    /// <see cref="Game.OnUpdate"/> gives us a reliable per-frame
    /// callback, and the tick implementation early-exits after a
    /// position compare when the player has not moved, so the
    /// amortised cost is a handful of int compares per frame.
    ///
    /// <para>This patch never advances quest stages. It only
    /// starts trace-examine dramas, which set the corresponding
    /// <c>chitsii.elinikki.quest.event.trace_*</c> flag as their
    /// final action. The chapter-5 ending resolver reads those
    /// flags separately, so the examiner is a pure flag-setter
    /// and cannot corrupt the zone-rule advancement path.</para>
    ///
    /// <para>Fail-soft: the postfix swallows exceptions so a bug
    /// in the examiner cannot break <see cref="Game.OnUpdate"/> or
    /// other mods' per-frame hooks.</para>
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    public static class Patch_Game_OnUpdate_TraceExaminer
    {
        public static void Postfix()
        {
            try
            {
                ElinikkiTraceExaminer.Tick();
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Game.OnUpdate trace examiner postfix failed: " + ex.Message);
            }
        }
    }
}
