using System;
using DG.Tweening;
using Elin_CommonDrama;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Helper for playing CWL drama files (LayerDrama).
    /// Falls back gracefully if drama activation fails.
    /// </summary>
    public static class QuestDrama
    {
        public static bool TryPlay(string dramaId, Action? onComplete = null)
        {
            try
            {
                var book = dramaId.StartsWith("drama_") ? dramaId : $"drama_{dramaId}";
                var layer = LayerDrama.Activate(book, dramaId, null, EClass.pc, null, null);
                if (layer == null) return false;

                if (onComplete != null)
                {
                    layer.SetOnKill(() =>
                    {
                        DOVirtual.DelayedCall(0.1f, () =>
                        {
                            try { onComplete(); }
                            catch (Exception ex) { ModLog.Warn($"QuestDrama callback: {ex.Message}"); }
                        });
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"QuestDrama.TryPlay({dramaId}) failed: {ex.Message}");
                return false;
            }
        }

        public static void Play(string dramaId, Action? onComplete = null)
        {
            if (!TryPlay(dramaId, onComplete))
            {
                onComplete?.Invoke();
            }
        }

        public static void PlayDeferred(string dramaId, Action? onComplete = null, float delay = 0.3f)
        {
            DOVirtual.DelayedCall(delay, () => Play(dramaId, onComplete));
        }

        /// <summary>
        /// Starts drama via common runtime idempotent command.
        /// </summary>
        public static void TryStartDeferred(string dramaId, float delay = 0.3f)
        {
            DOVirtual.DelayedCall(delay, () =>
                DramaRuntime.ResolveRun($"cmd.quest.try_start.{dramaId}"));
        }

        /// <summary>
        /// Starts drama via common runtime repeatable command (no started flag lock).
        /// </summary>
        public static void TryStartRepeatableDeferred(string dramaId, float delay = 0.3f)
        {
            DOVirtual.DelayedCall(delay, () =>
                DramaRuntime.ResolveRun($"cmd.quest.try_start_repeatable.{dramaId}"));
        }

        /// <summary>
        /// Starts drama while quest is not complete. Retries on next trigger until complete.
        /// </summary>
        public static void TryStartUntilCompleteDeferred(string dramaId, float delay = 0.3f)
        {
            DOVirtual.DelayedCall(delay, () =>
                DramaRuntime.ResolveRun($"cmd.quest.try_start_until_complete.{dramaId}"));
        }
    }
}
