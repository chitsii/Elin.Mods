using BepInEx;
using HarmonyLib;

namespace Elin_ModTemplate
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "chitsii.elin_justdoomit";
        public const string ModName = "Elin_JustDoomIt";
        public const string ModVersion = "0.23.254";

        private void Awake()
        {
            ModConfig.LoadConfig(Config);
            DoomDiagnostics.Initialize(Logger);
            DoomSessionManager.Ensure(Logger);
            var harmony = new Harmony(ModGuid);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            DoomSessionManager.Instance?.StopSession();
        }
    }
}
