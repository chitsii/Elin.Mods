using System;
using HarmonyLib;
using UnityEngine.Events;

namespace Elin_AutoEatSleep
{
    [HarmonyPatch]
    public class ConfigUI
    {
        [HarmonyPatch(typeof(ActPlan), "ShowContextMenu")]
        [HarmonyPrefix]
        public static void Prefix(ActPlan __instance)
        {
            // Ensure we are clicking on the player
            if (__instance.pos == null || EClass.pc == null || !__instance.pos.Equals(EClass.pc.pos))
            {
                return;
            }

            var config = Plugin.Instance.MyConfig;

            // Create a dynamic action for the menu item
            DynamicAct act = new DynamicAct(Localize.Get("Menu_Title"), () =>
            {
                UIContextMenu menu = EClass.ui.CreateContextMenu("ContextMenu");

                // ============================================================
                // 1. Auto Eat Section
                // ============================================================

                // Toggle Auto Eat
                string eatStatus = config.AutoEatEnabled.Value ? " (ON)" : " (OFF)";
                menu.AddButton(Localize.Get("Toggle_Eat") + eatStatus, () =>
                {
                    config.AutoEatEnabled.Value = !config.AutoEatEnabled.Value;
                    Msg.Say(Localize.Get("Toggle_Eat") + ": " + (config.AutoEatEnabled.Value ? "ON" : "OFF"));
                });

                // Hunger Threshold (Sub-menu)
                UIContextMenu hungerMenu = menu.AddChild();
                if (hungerMenu.popper != null && hungerMenu.popper.textName != null) {
                    hungerMenu.popper.textName.text = Localize.Get("Slider_Hunger") + ": " + GetHungerPhaseName(config.HungerThreshold.Value);
                }

                for (int i = 0; i <= 5; i++)
                {
                    int p = i;
                    string label = GetHungerPhaseName(p);
                    if (config.HungerThreshold.Value == p) label += Localize.Get("Filter_Current");

                    hungerMenu.AddButton(label, () =>
                    {
                        config.HungerThreshold.Value = p;
                        Msg.Say("Hunger Threshold: " + GetHungerPhaseName(p));
                    });
                }

                menu.AddSeparator();

                // ============================================================
                // 2. Auto Sleep Section
                // ============================================================

                // Toggle Auto Sleep
                string sleepStatus = config.AutoSleepEnabled.Value ? " (ON)" : " (OFF)";
                menu.AddButton(Localize.Get("Toggle_Sleep") + sleepStatus, () =>
                {
                    config.AutoSleepEnabled.Value = !config.AutoSleepEnabled.Value;
                    Msg.Say(Localize.Get("Toggle_Sleep") + ": " + (config.AutoSleepEnabled.Value ? "ON" : "OFF"));
                });

                // Toggle Resume AI
                string resumeStatus = config.ResumeAiOnWake.Value ? " (ON)" : " (OFF)";
                menu.AddButton(Localize.Get("Toggle_ResumeAI") + resumeStatus, () =>
                {
                    config.ResumeAiOnWake.Value = !config.ResumeAiOnWake.Value;
                    Msg.Say(Localize.Get("Toggle_ResumeAI") + ": " + (config.ResumeAiOnWake.Value ? "ON" : "OFF"));
                });

                // Sleep Threshold (Sub-menu for Sleepiness Phase)
                UIContextMenu sleepMenu = menu.AddChild();
                 if (sleepMenu.popper != null && sleepMenu.popper.textName != null) {
                     sleepMenu.popper.textName.text = "Sleep Threshold: " + GetSleepPhaseName(config.SleepStartPhase.Value);
                }

                // Phases 0 (Normal) to 4 (Dead)
                for (int i = 0; i <= 4; i++)
                {
                    int p = i;
                    string label = GetSleepPhaseName(p);
                     if (config.SleepStartPhase.Value == p) label += Localize.Get("Filter_Current");

                    sleepMenu.AddButton(label, () =>
                    {
                        config.SleepStartPhase.Value = p;
                        Msg.Say("Sleep Threshold: " + GetSleepPhaseName(p));
                    });
                }

               // Stamina Threshold Slider
               menu.AddSlider(Localize.Get("Slider_Stamina"),
                    (Func<float, string>)((float val) => ((int)val).ToString()),
                    (float)config.StaminaThreshold.Value,
                    (Action<float>)((float val) =>
                    {
                        config.StaminaThreshold.Value = (int)val;
                    }),
                    -100f, 100f, true, true, false);


                menu.AddSeparator();

                // ============================================================
                // 3. Container Filter Section
                // ============================================================

                // Container Filter Sub-menu
                UIContextMenu filterMenu = menu.AddChild();
                 if (filterMenu.popper != null && filterMenu.popper.textName != null) {
                    filterMenu.popper.textName.text = Localize.Get("Label_ContainerFilter");
                }

                // Option to Disable
                string offLabel = Localize.Get("Filter_Off");
                if (!config.UseContainerFilter.Value) offLabel += Localize.Get("Filter_Current");
                filterMenu.AddButton(offLabel, () =>
                {
                    config.UseContainerFilter.Value = false;
                    Msg.Say("Container Filter: OFF");
                });

                // Find unique containers
                var containers = new System.Collections.Generic.Dictionary<string, string>(); // ID -> Name
                if (EClass.pc != null && EClass.pc.things != null)
                {
                    foreach(Thing t in EClass.pc.things)
                    {
                        if (t.IsContainer)
                        {
                            if (!containers.ContainsKey(t.id))
                            {
                                containers.Add(t.id, t.Name);
                            }
                        }
                    }
                }

                foreach (var kvp in containers)
                {
                    string id = kvp.Key;
                    string name = kvp.Value;
                    string label = Localize.Get("Filter_Prefix") + name;

                    if (config.UseContainerFilter.Value && config.ContainerId.Value == id)
                    {
                        label += Localize.Get("Filter_Current");
                    }

                    filterMenu.AddButton(label, () =>
                    {
                        config.UseContainerFilter.Value = true;
                        config.ContainerId.Value = id;
                        Msg.Say("Container Filter: " + name);
                    });
                }

                menu.Show();
                return false;
            }, false);

            // Add the action to the list
            __instance.list.Add(new ActPlan.Item
            {
                act = act
            });
        }

        private static string GetHungerPhaseName(int val)
        {
             // 0=Bloated, 1=Filled, 2=Normal, 3=Hungry, 4=VeryHungry, 5=Starving
             if (val == 0) return Localize.Get("Hunger_Bloated");
             if (val == 1) return Localize.Get("Hunger_Filled");
             if (val == 2) return Localize.Get("Hunger_Normal");
             if (val == 3) return Localize.Get("Hunger_Hungry");
             if (val == 4) return Localize.Get("Hunger_VeryHungry");
             if (val == 5) return Localize.Get("Hunger_Starving");
             return "Phase " + val;
        }

        private static string GetSleepPhaseName(int val)
        {
            // 0=Normal, 1=Sleepy, 2=VerySleepy, 3=VeryVerySleepy
            if (val == 0) return Localize.Get("Sleep_Normal");
            if (val == 1) return Localize.Get("Sleep_Sleepy");
            if (val == 2) return Localize.Get("Sleep_VerySleepy");
            if (val == 3) return Localize.Get("Sleep_VeryVerySleepy");
            if (val == 4) return Localize.Get("Sleep_Dead");
            return val.ToString();
        }

        [HarmonyPatch(typeof(ActPlan.Item), "Perform")]
        [HarmonyPrefix]
        public static bool Prefix(ActPlan.Item __instance)
        {
            Act act = __instance.act;
            DynamicAct dynamicAct = act as DynamicAct;

            if (dynamicAct != null && (dynamicAct.id == "Auto Eat/Sleep Settings" || dynamicAct.id == Localize.Get("Menu_Title")))
            {
                dynamicAct.Perform();
                return false; // Skip original Perform
            }
            return true;
        }
    }
}
