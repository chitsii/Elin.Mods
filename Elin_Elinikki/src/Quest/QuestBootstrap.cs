using System;
using BepInEx;
using Elin_Elinikki.Quest.Drama;
using HarmonyLib;
#if DEBUG
using Elin_Elinikki.Quest.DebugTools;
#endif

namespace Elin_Elinikki.Quest
{
    // NOTE: BepInPlugin attribute intentionally left in place for Task 1.2 integration.
    // Task 1.2 will remove this class's BepInPlugin role and call QuestBootstrap from
    // the main Elin_Elinikki.Plugin lifecycle. ModGuid will be replaced at the same time.
    [BepInPlugin(ModGuid, "Quest Mod Skeleton", "0.1.0")]
    public sealed class QuestBootstrap : BaseUnityPlugin
    {
        public const string ModGuid = "yourname.elin_quest_mod";

        private void Awake()
        {
            QuestModLog.SetLogger(Logger);

            try
            {
                DramaRuntime.ConfigureResolver(new QuestDramaResolver(new GameQuestDramaRuntimeContext()));

                var harmony = new Harmony(ModGuid);
                harmony.PatchAll();
#if DEBUG
                QuestModDebugConsole.Register();
#endif
                QuestModLog.Info("Quest mod skeleton initialized.");
            }
            catch (Exception ex)
            {
                QuestModLog.Error("Harmony patch failed: " + ex.Message);
            }
        }
    }
}
