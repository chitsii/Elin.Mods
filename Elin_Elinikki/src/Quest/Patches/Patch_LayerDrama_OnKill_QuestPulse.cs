using System;
using Elin_Elinikki.Quest.Quest;
using HarmonyLib;

namespace Elin_Elinikki.Quest.Patches
{
    /// <summary>
    /// Tertiary quest pulse source. The primary pulse runs on
    /// <c>Zone.Activate</c> and the secondary runs on
    /// <c>Player.OnAdvanceHour</c>, but neither fires the moment a
    /// drama closes — which is exactly when the chapter-4/5 chains
    /// need a retry: the reunion menu / truth dramas / return
    /// journey drama all close with side effects that downstream
    /// dispatchers wait on, but those dispatchers were skipped
    /// earlier in the same tick because the drama layer had UI
    /// focus. Without a hook here the next drama would not
    /// auto-start until either an in-game hour ticks or the player
    /// performs a manual zone transition, which is a visible
    /// progression stall in the common flow.
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
    /// Ownership: this patch runs for every
    /// <see cref="LayerDrama"/> close in the game, not just
    /// Elinikki-owned dramas. That is deliberate. If a
    /// non-Elinikki cutscene was blocking the UI when Pulse
    /// first tried to open the reunion / menu / return-journey /
    /// ending drama, we WANT the retry to fire as soon as that
    /// other drama closes — otherwise the quest stalls until an
    /// hour tick or a manual zone change. All dispatchers inside
    /// <see cref="ElinikkiQuestFlow.RetryPulse"/> are idempotent
    /// and gated on precise stage/zone/flag conditions, so the
    /// retry is a no-op unless our own pre-conditions actually
    /// hold. For the specific case of the chapter-4 truth menu,
    /// the <c>TMP_TRUTH_MENU_DISMISSED</c> latch ensures that a
    /// menu the player already closed with "また後で" does not
    /// re-pop when any later drama closes in the same zone visit.
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
