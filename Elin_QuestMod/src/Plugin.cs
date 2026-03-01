using System;
using BepInEx;
using Elin_QuestMod.Drama;
using HarmonyLib;
#if DEBUG
using Elin_QuestMod.DebugTools;
#endif

namespace Elin_QuestMod
{
    [BepInPlugin(ModGuid, "Quest Mod Skeleton", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "yourname.elin_quest_mod";

        private void Awake()
        {
            ModLog.SetLogger(Logger);

            try
            {
                DramaRuntime.ConfigureResolver(new QuestDramaResolver(new GameQuestDramaRuntimeContext()));

                var harmony = new Harmony(ModGuid);
                harmony.PatchAll();
#if DEBUG
                QuestModDebugConsole.Register();
#endif
                ModLog.Info("Quest mod skeleton initialized.");
            }
            catch (Exception ex)
            {
                ModLog.Error("Harmony patch failed: " + ex.Message);
            }
        }
    }
}
