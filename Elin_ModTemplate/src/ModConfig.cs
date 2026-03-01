using BepInEx.Configuration;

namespace Elin_ModTemplate
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableMod;

        // TODO: Add your config entries here
        // Example:
        // public static ConfigEntry<float> MyValue;

        public static void LoadConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true, "Enable the mod.");

            // TODO: Bind your config entries here
            // Example:
            // MyValue = config.Bind("General", "MyValue", 50f,
            //     new ConfigDescription("Description of MyValue.", new AcceptableValueRange<float>(0f, 100f)));
        }
    }
}
