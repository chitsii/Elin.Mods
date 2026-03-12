using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Elin_ElinFPSView
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "chitsii.elin_fpsview";
        public const string ModName = "Elin_ElinFPSView";
        public const string ModVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static ModConfig Settings;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Settings = ModConfig.Bind(base.Config);
            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            FpsViewManager.EnsureCreated();
            Logger.LogInfo($"{ModName} v{ModVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
        }
    }
}
