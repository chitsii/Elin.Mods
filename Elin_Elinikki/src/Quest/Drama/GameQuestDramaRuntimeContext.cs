using System;
using System.Reflection;
using Elin_Elinikki.Quest.Quest;

namespace Elin_Elinikki.Quest.Drama
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
                QuestModLog.Info("QuestBridge.CanStartDrama: false (dialogFlags unavailable), dramaId=" + dramaId);
                return false;
            }

            bool found = TryGetStartedFlag(dramaId, out int startedFlag);
            bool canStart = (!found || startedFlag != 1) && !IsDramaDone(dramaId);

            QuestModLog.Info(
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
                QuestModLog.Info("QuestBridge.TryStartDrama: skipped by policy (" + dramaId + ")");
                return false;
            }

            bool started = TryActivateDrama(dramaId);
            if (started)
            {
                SetStartedFlag(dramaId, 1);
                QuestModLog.Info("QuestBridge.TryStartDrama: started (" + dramaId + ")");
            }
            else
            {
                QuestModLog.Info("QuestBridge.TryStartDrama: not started (" + dramaId + ")");
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
                QuestModLog.Info("QuestBridge.TryStartDramaRepeatable: started (" + dramaId + ")");
            }
            else
            {
                QuestModLog.Info("QuestBridge.TryStartDramaRepeatable: not started (" + dramaId + ")");
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
                QuestModLog.Info("QuestBridge.TryStartDramaUntilComplete: skipped complete (" + dramaId + ")");
                return false;
            }

            bool started = TryStartDramaRepeatable(dramaId);
            if (!started)
            {
                QuestModLog.Info("QuestBridge.TryStartDramaUntilComplete: not started (" + dramaId + ")");
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
            QuestModLog.Info("QuestBridge.CompleteDrama: marked complete (" + dramaId + ")");
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
                    ElinikkiQuestFlow.Pulse();
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

        public bool PlayBgm(string bgmId)
        {
            if (string.IsNullOrEmpty(bgmId))
            {
                return false;
            }

            try
            {
                var manager = SoundManager.current;
                if (manager == null)
                {
                    QuestModLog.Warn(
                        "QuestBridge.PlayBgm: SoundManager unavailable (" + bgmId + ")");
                    return false;
                }

                var data = manager.GetData(bgmId);
                if (data == null)
                {
                    QuestModLog.Warn("QuestBridge.PlayBgm: BGM not found (" + bgmId + ")");
                    return false;
                }

                if (data is BGMData bgm)
                {
                    // Start playback first, then latch the drama
                    // audio flags. Elin's built-in drama BGM action
                    // uses the same order: a failing PlayBGM call
                    // must not leave the drama layer in a "custom
                    // BGM active" state, because SoundManager's
                    // funcCanPlayBGM and ActorEx.keepAmbientBGM both
                    // key off those flags and would suppress the
                    // zone playlist + ambient actors for no reason.
                    manager.PlayBGM(bgm);

                    LayerDrama.haltPlaylist = true;
                    LayerDrama.maxBGMVolume = true;
                    try
                    {
                        var dramaInstance = LayerDrama.Instance?.drama;
                        if (dramaInstance != null)
                        {
                            dramaInstance.bgmChanged = true;
                        }
                    }
                    catch (Exception markEx)
                    {
                        QuestModLog.Warn(
                            "QuestBridge.PlayBgm: bgmChanged flag update failed: "
                            + markEx.Message);
                    }

                    QuestModLog.Info("QuestBridge.PlayBgm: started (" + bgmId + ")");
                    return true;
                }

                // Not a BGM asset — fall back to the generic Play
                // path. The drama DSL's play_bgm helper does the
                // same dance, and it is the only sane thing to do
                // for non-BGMData entries that still exist in the
                // sound table.
                manager.Play(data);
                QuestModLog.Info(
                    "QuestBridge.PlayBgm: played as generic sound (" + bgmId + ")");
                return true;
            }
            catch (Exception ex)
            {
                QuestModLog.Warn(
                    "QuestBridge.PlayBgm raised: " + ex.Message + " (" + bgmId + ")");
                return false;
            }
        }

        public void StopBgm()
        {
            try
            {
                // Keep haltPlaylist = true so the zone playlist does
                // not immediately restart when SoundManager.StopBGM()
                // resets the playback interval. Elin's built-in
                // `stopBGM` drama command does the same: the drama
                // hand-off is intended to leave silence under the
                // dialogue, not resume area BGM under it. Only the
                // max-volume hold is released, because that is what
                // keeps the drama layer dominating the mixer.
                LayerDrama.haltPlaylist = true;
                LayerDrama.maxBGMVolume = false;

                var manager = SoundManager.current;
                if (manager == null)
                {
                    return;
                }

                manager.StopBGM();

                // Elin's built-in stopBGM action also clears
                // currentBGM. Without this, AI_PlayMusic and other
                // core systems still apply ducking/jingle timing
                // against a track that has already been stopped.
                try
                {
                    manager.currentBGM = null;
                }
                catch (Exception clearEx)
                {
                    QuestModLog.Warn(
                        "QuestBridge.StopBgm: currentBGM clear failed: "
                        + clearEx.Message);
                }

                QuestModLog.Info("QuestBridge.StopBgm: stopped BGM (playlist stays halted)");
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("QuestBridge.StopBgm raised: " + ex.Message);
            }
        }

        public bool PlaySe(string seId)
        {
            if (string.IsNullOrEmpty(seId))
            {
                return false;
            }

            try
            {
                var manager = SoundManager.current;
                if (manager == null)
                {
                    QuestModLog.Warn(
                        "QuestBridge.PlaySe: SoundManager unavailable (" + seId + ")");
                    return false;
                }

                var data = manager.GetData(seId);
                if (data == null)
                {
                    QuestModLog.Warn("QuestBridge.PlaySe: SE not found (" + seId + ")");
                    return false;
                }

                manager.Play(data);
                QuestModLog.Info("QuestBridge.PlaySe: played (" + seId + ")");
                return true;
            }
            catch (Exception ex)
            {
                QuestModLog.Warn(
                    "QuestBridge.PlaySe raised: " + ex.Message + " (" + seId + ")");
                return false;
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

            // Resolve the method by matching arg types so both
            // zero-arg (e.g. SoundManager.StopBGM) and single-string
            // (e.g. Card.PlayEffect) signatures are callable from
            // the same helper. A null arg element maps to typeof(object)
            // which the binder still accepts because the real arg is
            // null anyway.
            Type[] argTypes;
            if (args == null || args.Length == 0)
            {
                argTypes = Type.EmptyTypes;
            }
            else
            {
                argTypes = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    argTypes[i] = args[i]?.GetType() ?? typeof(object);
                }
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                argTypes,
                null);
            method?.Invoke(target, args);
        }
    }
}
