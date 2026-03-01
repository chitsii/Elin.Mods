using BepInEx;
using HarmonyLib;
using System;

namespace Elin_AutoOfferingAlter
{
    [BepInPlugin("tishi.elin.auto_offering_alter", "Elin_AutoOfferingAlter", "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ID_OFFERING_BOX = "tishi.offering_box";

        private void Awake()
        {
            ModConfig.Init(Config);
            Log = Logger;
            var harmony = new Harmony("tishi.elin.auto_offering_alter");
            harmony.PatchAll();
            Logger.LogInfo("Elin_AutoOfferingAlter loaded");
        }

        public static BepInEx.Logging.ManualLogSource Log;
    }
}
