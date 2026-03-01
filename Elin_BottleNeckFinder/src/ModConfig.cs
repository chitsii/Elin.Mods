using BepInEx.Configuration;

namespace Elin_BottleNeckFinder
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<bool> ShowOverlay;
        public static ConfigEntry<int> TopModCount;
        public static ConfigEntry<int> MaxErrorHistory;
        public static ConfigEntry<int> SampleInterval;

        public static void LoadConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true,
                "Enable the mod.");

            ShowOverlay = config.Bind("Overlay", "ShowOverlay", false,
                "Show the overlay.");

            TopModCount = config.Bind("Overlay", "TopModCount", 5,
                new ConfigDescription("Number of top mods to show in ranking.",
                    new AcceptableValueRange<int>(3, 20)));

            MaxErrorHistory = config.Bind("Overlay", "MaxErrorHistory", 5,
                new ConfigDescription("Maximum number of recent errors to display.",
                    new AcceptableValueRange<int>(1, 20)));

            SampleInterval = config.Bind("Profiler", "SampleInterval", 1,
                new ConfigDescription("Profile every Nth frame (1 = every frame).",
                    new AcceptableValueRange<int>(1, 10)));
        }
    }
}
