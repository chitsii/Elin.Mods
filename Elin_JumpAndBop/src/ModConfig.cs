using BepInEx.Configuration;

namespace Elin_JumpAndBop
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<float> JumpHeight;
        public static ConfigEntry<bool> EnableOnWait;
        public static ConfigEntry<bool> EnableOnSex;

        public static void LoadConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true,
                "Enable the mod.");

            JumpHeight = config.Bind("General", "JumpHeight", 0.03f,
                new ConfigDescription("Height of the bounce animation.",
                    new AcceptableValueRange<float>(0.01f, 0.10f)));

            EnableOnWait = config.Bind("General", "EnableOnWait", true,
                "Enable bounce on wait (Space key).");

            EnableOnSex = config.Bind("General", "EnableOnSex", true,
                "Enable bounce during intimacy.");
        }
    }
}
