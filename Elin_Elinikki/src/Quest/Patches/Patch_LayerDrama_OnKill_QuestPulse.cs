using System;
using Elin_Elinikki.Quest.Quest;
using HarmonyLib;

namespace Elin_Elinikki.Quest.Patches
{
    /// <summary>
    /// Tertiary quest pulse source. The primary pulse runs on
    /// <c>Zone.Activate</c> and the secondary runs on
    /// <c>Player.OnAdvanceHour</c>, but neither fires the moment a
    /// drama closes — which is exactly when the chapter-5 chain
    /// needs a retry: the return journey drama emits
    /// <c>cmd.quest.complete.elinikki_return_journey</c> on its last
    /// step and then fades out, but <see cref="ElinikkiQuestFlow.TryDispatchEnding"/>
    /// was skipped earlier in this tick because the return-journey
    /// layer had UI focus. Without a hook here the ending drama
    /// would not auto-start until either an in-game hour ticks or
    /// the player manually re-enters the entrance, which is a
    /// visible progression stall in the common flow.
    ///
    /// Hooking <see cref="LayerDrama.OnKill"/> as a postfix gives us
    /// an "any drama just closed" signal — the layer has already
    /// torn down and <c>EClass.ui.IsActive</c> is free, so the
    /// retry dispatchers inside <see cref="ElinikkiQuestFlow.RetryPulse"/>
    /// can open a follow-up drama without stacking on the one that
    /// just closed. We deliberately reuse
    /// <see cref="ElinikkiQuestFlow.RetryPulse"/> rather than the
    /// full <see cref="ElinikkiQuestFlow.Pulse"/>: zone stage
    /// transitions must only fire on real zone activations, so the
    /// drama-close hook never calls <c>AdvanceForZone</c>.
    ///
    /// Fail-soft: the postfix swallows exceptions so any future
    /// dispatcher bug cannot leak back into <c>LayerDrama.OnKill</c>
    /// and break other mods' drama teardown.
    /// </summary>
    [HarmonyPatch(typeof(LayerDrama), nameof(LayerDrama.OnKill))]
    public static class Patch_LayerDrama_OnKill_QuestPulse
    {
        public static void Postfix()
        {
            try
            {
                ElinikkiQuestFlow.RetryPulse();
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("LayerDrama.OnKill postfix pulse failed: " + ex.Message);
            }
        }
    }
}
