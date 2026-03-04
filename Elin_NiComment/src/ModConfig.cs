using BepInEx.Configuration;
using Elin_NiComment.Llm;

namespace Elin_NiComment
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<float> ScrollSpeed;
        public static ConfigEntry<int> FontSize;
        public static ConfigEntry<int> MaxLanes;
        public static ConfigEntry<int> PoolSize;
        public static ConfigEntry<float> TopMargin;

        public static void LoadConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true, "Enable the mod.");
            LlmConfig.LoadConfig(config);

            ScrollSpeed = config.Bind("Comment", "ScrollSpeed", 200f,
                new ConfigDescription("Scroll speed in pixels per second.", new AcceptableValueRange<float>(50f, 800f)));

            FontSize = config.Bind("Comment", "FontSize", 36,
                new ConfigDescription("Font size of comments.", new AcceptableValueRange<int>(16, 72)));

            MaxLanes = config.Bind("Comment", "MaxLanes", 12,
                new ConfigDescription("Maximum number of comment lanes.", new AcceptableValueRange<int>(1, 30)));

            PoolSize = config.Bind("Comment", "PoolSize", 50,
                new ConfigDescription("Max simultaneous comments (pool size).", new AcceptableValueRange<int>(10, 200)));

            TopMargin = config.Bind("Comment", "TopMargin", 20f,
                new ConfigDescription("Top margin in pixels.", new AcceptableValueRange<float>(0f, 200f)));
        }
    }
}
