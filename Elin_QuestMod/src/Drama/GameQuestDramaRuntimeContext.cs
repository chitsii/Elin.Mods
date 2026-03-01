using System;
using System.Reflection;
using Elin_QuestMod.Quest;

namespace Elin_QuestMod.Drama
{
    /// <summary>
    /// In-game implementation for quest drama runtime commands.
    /// </summary>
    public sealed class GameQuestDramaRuntimeContext : IQuestDramaRuntimeContext
    {
        private const string StartedDramaLocalPrefix = "drama.started.";

        public bool CanStartDrama(string dramaId)
        {
            dramaId = NormalizeDramaId(dramaId);
            if (string.IsNullOrEmpty(dramaId))
            {
                return false;
            }

            if (EClass.player?.dialogFlags == null)
            {
                ModLog.Info("QuestBridge.CanStartDrama: false (dialogFlags unavailable), dramaId=" + dramaId);
                return false;
            }

            bool found = TryGetStartedFlag(dramaId, out int startedFlag);
            bool canStart = (!found || startedFlag != 1) && !IsDramaDone(dramaId);

            ModLog.Info(
                "QuestBridge.CanStartDrama: dramaId="
                + dramaId
                + ", startedFlag="
                + (found ? startedFlag.ToString() : "-1")
                + ", result="
                + (canStart ? "True" : "False"));

            return canStart;
        }

        public bool IsDramaDone(string dramaId)
        {
            dramaId = NormalizeDramaId(dramaId);
            if (string.IsNullOrEmpty(dramaId))
            {
                return false;
            }

            return QuestStateService.IsQuestCompleted(dramaId);
        }

        public bool TryStartDrama(string dramaId)
        {
            dramaId = NormalizeDramaId(dramaId);
            if (string.IsNullOrEmpty(dramaId))
            {
                return false;
            }

            if (!CanStartDrama(dramaId))
            {
                ModLog.Info("QuestBridge.TryStartDrama: skipped by policy (" + dramaId + ")");
                return false;
            }

            bool started = TryActivateDrama(dramaId);
            if (started)
            {
                SetStartedFlag(dramaId, 1);
                ModLog.Info("QuestBridge.TryStartDrama: started (" + dramaId + ")");
            }
            else
            {
                ModLog.Info("QuestBridge.TryStartDrama: not started (" + dramaId + ")");
            }

            return started;
        }

        public bool TryStartDramaRepeatable(string dramaId)
        {
            dramaId = NormalizeDramaId(dramaId);
            if (string.IsNullOrEmpty(dramaId))
            {
                return false;
            }

            bool started = TryActivateDrama(dramaId);
            if (started)
            {
                SetStartedFlag(dramaId, 1);
                ModLog.Info("QuestBridge.TryStartDramaRepeatable: started (" + dramaId + ")");
            }
            else
            {
                ModLog.Info("QuestBridge.TryStartDramaRepeatable: not started (" + dramaId + ")");
            }

            return started;
        }

        public bool TryStartDramaUntilComplete(string dramaId)
        {
            dramaId = NormalizeDramaId(dramaId);
            if (string.IsNullOrEmpty(dramaId))
            {
                return false;
            }

            if (IsDramaDone(dramaId))
            {
                ModLog.Info("QuestBridge.TryStartDramaUntilComplete: skipped complete (" + dramaId + ")");
                return false;
            }

            bool started = TryStartDramaRepeatable(dramaId);
            if (!started)
            {
                ModLog.Info("QuestBridge.TryStartDramaUntilComplete: not started (" + dramaId + ")");
            }

            return started;
        }

        public void CompleteDrama(string dramaId)
        {
            dramaId = NormalizeDramaId(dramaId);
            if (string.IsNullOrEmpty(dramaId))
            {
                return;
            }

            QuestStateService.CompleteQuest(dramaId);
            SetStartedFlag(dramaId, 1);
            ModLog.Info("QuestBridge.CompleteDrama: marked complete (" + dramaId + ")");
        }

        public bool RunCue(string cueKey)
        {
            if (string.IsNullOrEmpty(cueKey))
            {
                return false;
            }

            switch (cueKey)
            {
                case "cue.questmod.placeholder_pulse":
                case "cue.questmod.feature_showcase_pulse":
                    QuestFlow.Pulse();
                    return true;
                default:
                    return false;
            }
        }

        public void PlayPcEffect(string effectId, string soundId = null)
        {
            if (EClass.pc == null || string.IsNullOrEmpty(effectId))
            {
                return;
            }

            InvokeVoidIfExists(EClass.pc, "PlayEffect", effectId);
            if (!string.IsNullOrEmpty(soundId))
            {
                InvokeVoidIfExists(EClass.pc, "PlaySound", soundId);
            }
        }

        private static bool TryActivateDrama(string dramaId)
        {
            if (EClass.pc == null || EClass.ui == null)
            {
                return false;
            }

            string bookId = dramaId.StartsWith("drama_", StringComparison.Ordinal)
                ? dramaId
                : "drama_" + dramaId;

            LayerDrama layer = LayerDrama.Activate(bookId, dramaId, null, EClass.pc, null, null);
            return layer != null;
        }

        private static string NormalizeDramaId(string dramaId)
        {
            return dramaId == null ? string.Empty : dramaId.Trim();
        }

        private static bool TryGetStartedFlag(string dramaId, out int value)
        {
            value = 0;
            var flags = EClass.player?.dialogFlags;
            if (flags == null)
            {
                return false;
            }

            string key = GetStartedFlagKey(dramaId);
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return flags.TryGetValue(key, out value);
        }

        private static void SetStartedFlag(string dramaId, int value)
        {
            var flags = EClass.player?.dialogFlags;
            if (flags == null)
            {
                return;
            }

            string key = GetStartedFlagKey(dramaId);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            flags[key] = value;
        }

        private static string GetStartedFlagKey(string dramaId)
        {
            dramaId = NormalizeDramaId(dramaId);
            if (string.IsNullOrEmpty(dramaId))
            {
                return string.Empty;
            }

            return QuestStateService.BuildFlagKey(StartedDramaLocalPrefix + dramaId);
        }

        private static void InvokeVoidIfExists(object target, string methodName, params object[] args)
        {
            if (target == null || string.IsNullOrEmpty(methodName))
            {
                return;
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);
            method?.Invoke(target, args);
        }
    }
}
