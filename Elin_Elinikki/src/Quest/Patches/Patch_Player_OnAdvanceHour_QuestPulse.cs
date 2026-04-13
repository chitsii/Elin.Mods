using System;
using Elin_Elinikki.Quest.Quest;
using HarmonyLib;

namespace Elin_Elinikki.Quest.Patches
{
    /// <summary>
    /// Secondary quest pulse source. The primary pulse runs on
    /// <c>Zone.Activate</c> via <see cref="Patch_Zone_Activate_QuestPulse"/>,
    /// but that only fires on zone transitions. Some quest triggers —
    /// the chapter-0 intro gate, the chapter-4 reunion drama, and any
    /// other drama-start gate — can fail on the initial pulse because
    /// the UI is still busy (load screen, prior drama finishing,
    /// another menu on top, etc.). Hooking <c>Player.OnAdvanceHour</c>
    /// gives us a reliable retry cadence (once per in-game hour) so
    /// stuck drama starts resolve without requiring the player to
    /// perform an extra zone transition.
    ///
    /// IMPORTANT: this path uses
    /// <see cref="ElinikkiQuestFlow.RetryPulse"/>, not the full
    /// <see cref="ElinikkiQuestFlow.Pulse"/>. Zone-based stage
    /// transitions must only be evaluated on a real Zone.Activate —
    /// running the full pulse from an hour tick would let the quest
    /// advance zone stages while the player is simply idling inside
    /// a chapter zone (for example after loading a save mid-dungeon),
    /// which would bypass the intended zone-transition semantics.
    /// <see cref="ElinikkiQuestFlow.RetryPulse"/> only retries
    /// drama-start dispatchers and never calls AdvanceForZone.
    ///
    /// Fail-soft: the postfix swallows exceptions so an unexpected pulse
    /// failure never corrupts the game's hour tick.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnAdvanceHour))]
    public static class Patch_Player_OnAdvanceHour_QuestPulse
    {
        public static void Postfix()
        {
            try
            {
                ElinikkiQuestFlow.RetryPulse();
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Player.OnAdvanceHour postfix pulse failed: " + ex.Message);
            }
        }
    }
}
