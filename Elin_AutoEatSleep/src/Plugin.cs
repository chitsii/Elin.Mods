using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Elin_AutoEatSleep
{
    [BepInPlugin("com.elin.autoeatsleep", "Elin Auto Eat & Sleep", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public ModConfig MyConfig { get; private set; }
        public AutoEatLogic Logic { get; private set; }

        private void Awake()
        {
            Instance = this;
            MyConfig = new ModConfig(base.Config);
            Logic = new AutoEatLogic();

            var harmony = new Harmony("com.elin.autoeatsleep");
            harmony.PatchAll();
        }

        private void Update()
        {
            // Event driven.
        }
    }
}
