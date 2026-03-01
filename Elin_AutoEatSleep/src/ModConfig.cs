using BepInEx.Configuration;

namespace Elin_AutoEatSleep
{
    public class ModConfig
    {
        public ConfigEntry<bool> AutoEatEnabled;
        public ConfigEntry<int> HungerThreshold; // Using int for enum value if needed, or string
        public ConfigEntry<bool> UseContainerFilter;
        public ConfigEntry<string> ContainerId;

        public ConfigEntry<bool> AutoSleepEnabled;
        public ConfigEntry<int> StaminaThreshold;
        public ConfigEntry<int> SleepStartPhase;
        public ConfigEntry<bool> ResumeAiOnWake;

        public ModConfig(ConfigFile config)
        {
            AutoEatEnabled = config.Bind("AutoEat", "Enabled", true, "Enable Auto Eat");
            // StatsHunger: 0=Normal, 1=Hungry, 2=VeryHungry, 3=Starving
            // Default to 1 (Hungry). Using literal to avoid dependency issues or definition mismatch.
            HungerThreshold = config.Bind("AutoEat", "HungerThreshold", 1, "Hunger threshold to trigger auto eat");
            UseContainerFilter = config.Bind("AutoEat", "UseContainerFilter", false, "Only search for food in specific containers");
            ContainerId = config.Bind("AutoEat", "ContainerId", "cooler", "Partial name or ID of the container to search for food");

            AutoSleepEnabled = config.Bind("AutoSleep", "Enabled", true, "Enable Auto Sleep");
            StaminaThreshold = config.Bind("AutoSleep", "StaminaThreshold", 0, "Stamina threshold to trigger auto sleep");
            SleepStartPhase = config.Bind("AutoSleep", "SleepStartPhase", 2, "Sleepiness phase to trigger sleeping (0-4)");
            ResumeAiOnWake = config.Bind("AutoSleep", "ResumeAiOnWake", true, "Resume previous AI action after waking up");
        }
    }
}
