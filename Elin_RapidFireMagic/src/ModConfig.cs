using BepInEx.Configuration;

namespace Elin_RapidFireMagic
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<float> PollInterval;

        public static void LoadConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true, "Enable the mod.");
            PollInterval = config.Bind("General", "PollInterval", 0.1f,
                new ConfigDescription("Polling interval in seconds.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        }
    }
}
