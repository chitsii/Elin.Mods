using System;
using BepInEx.Logging;
using Elin_Elinikki.Quest.Drama;
using Elin_Elinikki.Quest.Quest;
#if DEBUG
using Elin_Elinikki.Quest.DebugTools;
#endif

namespace Elin_Elinikki.Quest
{
    /// <summary>
    /// Quest subsystem bootstrapper. Not a BepInEx plugin — the main
    /// <see cref="Elin_Elinikki.Plugin"/> is the sole plugin entry point. Main
    /// Plugin.Awake() calls <see cref="Initialize"/> after its Harmony PatchAll,
    /// which already picks up the patches under Elin_Elinikki.Quest.Patches
    /// because PatchAll scans the whole assembly.
    /// </summary>
    public static class QuestBootstrap
    {
        /// <summary>
        /// Canonical flag prefix for all Elinikki quest flags. The spec in
        /// `story/chapters/_index.md` writes full keys as `chitsii.elinikki.quest.*`,
        /// and QuestStateService already supplies the `quest.` segment in its local
        /// keys (e.g. `quest.current_phase`, `quest.done.*`, `quest.active.*`).
        /// So the prefix must be `chitsii.elinikki` — adding a trailing `.quest`
        /// would double up to `chitsii.elinikki.quest.quest.*`.
        /// Keep this in sync with `story/chapters/_index.md` if the spec changes.
        /// </summary>
        public const string FlagPrefix = "chitsii.elinikki";

        private static bool _initialized;

        public static void Initialize(ManualLogSource logger)
        {
            if (_initialized)
            {
                return;
            }

            QuestModLog.SetLogger(logger);

            try
            {
                QuestStateService.SetDefaultPrefix(FlagPrefix);
                DramaRuntime.ConfigureResolver(
                    new QuestDramaResolver(new GameQuestDramaRuntimeContext()));
                ElinikkiQuestFlow.RegisterDefaultZoneRules();
#if DEBUG
                QuestModDebugConsole.Register();
#endif
                QuestModLog.Info("Elinikki quest subsystem initialized. flag prefix=" + FlagPrefix);
                _initialized = true;
            }
            catch (Exception ex)
            {
                QuestModLog.Error("Quest subsystem init failed: " + ex.Message);
            }
        }
    }
}
