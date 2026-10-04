using System;
using System.Reflection;
using Elin_QuestMod.Quest;
using UnityEngine;

namespace Elin_QuestMod.Drama
{
    /// <summary>
    /// In-game implementation for quest drama runtime commands.
    /// </summary>
    public sealed class GameQuestDramaRuntimeContext : IQuestDramaRuntimeContext
    {
        private const string StartedDramaLocalPrefix = "drama.started.";
        private static readonly Type[] PlayEffectParameterTypes =
            new[] { typeof(string), typeof(bool), typeof(float), typeof(Vector3) };
        private static readonly Type[] PlaySoundParameterTypes =
            new[] { typeof(string), typeof(float), typeof(bool) };

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

        public bool PlayPcEffect(string effectId, string soundId = null)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                ModLog.Warn("QuestBridge.PlayPcEffect: skipped empty effect id");
                return false;
            }

            if (EClass.pc == null)
            {
                ModLog.Warn("QuestBridge.PlayPcEffect: skipped (pc unavailable), effectId=" + effectId);
                return false;
            }

            bool effectOk = TryPlayPcEffect(effectId);
            bool soundOk = true;
            if (!string.IsNullOrEmpty(soundId))
            {
                soundOk = TryPlayPcSound(soundId);
            }

            return effectOk && soundOk;
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

        private static bool TryPlayPcEffect(string effectId)
        {
            try
            {
                MethodInfo method = ResolveCardMethod("PlayEffect", PlayEffectParameterTypes);
                if (method == null)
                {
                    ModLog.Warn("QuestBridge.PlayPcEffect: Card.PlayEffect signature unavailable");
                    return false;
                }

                method.Invoke(EClass.pc, new object[] { effectId, true, 0f, default(Vector3) });
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(
                    "QuestBridge.PlayPcEffect: effect failed, effectId="
                    + effectId
                    + ", error="
                    + GetDiagnosticException(ex));
                return false;
            }
        }

        private static bool TryPlayPcSound(string soundId)
        {
            try
            {
                MethodInfo method = ResolveCardMethod("PlaySound", PlaySoundParameterTypes);
                if (method == null)
                {
                    ModLog.Warn("QuestBridge.PlayPcEffect: Card.PlaySound signature unavailable");
                    return false;
                }

                method.Invoke(EClass.pc, new object[] { soundId, 1f, true });
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(
                    "QuestBridge.PlayPcEffect: sound failed, soundId="
                    + soundId
                    + ", error="
                    + GetDiagnosticException(ex));
                return false;
            }
        }

        private static MethodInfo ResolveCardMethod(string methodName, Type[] parameterTypes)
        {
            return typeof(Card).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                parameterTypes,
                null);
        }

        private static Exception GetDiagnosticException(Exception ex)
        {
            return (ex as TargetInvocationException)?.InnerException ?? ex;
        }
    }
}
