using BepInEx.Configuration;

namespace Elin_AutoOfferingAlter
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableLog;

        public static void Init(ConfigFile config)
        {
            EnableLog = config.Bind("General", "EnableLog", true, "Enable verbose logging for debugging.");
        }
    }
}
