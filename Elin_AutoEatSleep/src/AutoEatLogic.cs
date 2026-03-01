using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace Elin_AutoEatSleep
{
    // Logic is now purely event-driven via Harmony patches
    [HarmonyPatch]
    public class AutoEatLogic
    {
        public static AIAct _savedAI;

        // Hook into HotItemActionSleep.Perform
        [HarmonyPatch(typeof(HotItemActionSleep), "Perform")]
        [HarmonyPrefix]
        public static bool OnSleepPerform(HotItemActionSleep __instance)
        {
            if (Plugin.Instance.MyConfig.ResumeAiOnWake.Value)
            {
                // Fallback: Standard Capture (for vanilla AI)
                CaptureAI();
            }
            return true;
        }

        // Hook into ConSleep.OnRemoved to resume AI after waking up
        [HarmonyPatch(typeof(ConSleep), "OnRemoved")]
        [HarmonyPostfix]
        public static void OnSleepRemoved(ConSleep __instance)
        {
            if (__instance.owner != EClass.pc) return;

            if (Plugin.Instance.MyConfig.ResumeAiOnWake.Value)
            {
                TryResumeAI();
            }
        }

        // PATCH VERIFICATION LOGGING
        [HarmonyPatch(typeof(Game), "OnLoad")]
        [HarmonyPostfix]
        public static void OnGameLoad(Game __instance)
        {
            UnityEngine.Debug.Log("[Elin_AutoEatSleep] OnLoad Patch Activated");
            InitNextFrame();
        }

        [HarmonyPatch(typeof(Game), "StartNewGame")]
        [HarmonyPostfix]
        public static void OnStartNewGame(Game __instance)
        {
             UnityEngine.Debug.Log("[Elin_AutoEatSleep] OnStartNewGame Patch Activated");
             InitNextFrame();
        }

        private static void InitNextFrame()
        {
            EClass.core.actionsNextFrame.Add((System.Action)(() =>
            {
                CheckAutoEat();
                CheckAutoSleep();
            }));
        }

        [HarmonyPatch(typeof(Stats), "OnChangePhase")]
        [HarmonyPostfix]
        public static void OnChangePhase(Stats __instance, int phase, int lastPhase)
        {
            if (EClass.pc == null) return;

            // Debug logging for phase change
            if (__instance == EClass.pc.sleepiness)
            {
                 CheckAutoSleep();
            }
            else if (__instance == EClass.pc.stamina)
            {
                 CheckAutoSleep();
            }
            else if (__instance == EClass.pc.hunger)
            {
                 CheckAutoEat();
            }
        }

        public static void CheckAutoEat()
        {
             try
             {
                var pc = EClass.pc;
                if (pc == null || pc.hunger == null) return;

                if (!Plugin.Instance.MyConfig.AutoEatEnabled.Value) return;

                if (pc.hunger.GetPhase() < Plugin.Instance.MyConfig.HungerThreshold.Value)
                    return;

                if (pc.ai is AI_Eat) return;

                Thing food = FindFood(pc.things);
                if (food != null)
                {
                     // UnityEngine.Debug.Log("[Elin_AutoEatSleep] Eating " + food.Name);
                     pc.InstantEat(food, true);
                }
             }
             catch (System.Exception ex)
             {
                 UnityEngine.Debug.LogError("[Elin_AutoEatSleep] Error in CheckAutoEat: " + ex.Message);
             }
        }

        private static Thing FindFood(ThingContainer container)
        {
            var config = Plugin.Instance.MyConfig;
            bool useFilter = config.UseContainerFilter.Value;
            string filterId = config.ContainerId.Value.ToLower();

            foreach (Thing t in container)
            {
                if (t.IsContainer)
                {
                    if (t.things != null && t.things.Count > 0)
                    {
                        bool shouldSearch = true;
                        if (useFilter && !string.IsNullOrEmpty(filterId))
                        {
                            string tId = t.id.ToLower();
                            string tName = t.Name.ToLower();
                            if (!tId.Contains(filterId) && !tName.Contains(filterId))
                            {
                                shouldSearch = false;
                            }
                        }

                        if (shouldSearch)
                        {
                            var found = FindFood(t.things);
                            if (found != null) return found;
                        }
                    }
                }

                if (EClass.pc.CanEat(t, shouldEat: true) && !t.c_isImportant)
                {
                     return t;
                }
            }
            return null;
        }

        public static void CheckAutoSleep()
        {
             try
             {
                var pc = EClass.pc;
                if (pc == null) return;

                if (!Plugin.Instance.MyConfig.AutoSleepEnabled.Value) return;

                var config = Plugin.Instance.MyConfig;

                bool lowStamina = pc.stamina.value <= config.StaminaThreshold.Value;
                bool sleepy = pc.sleepiness.GetPhase() >= config.SleepStartPhase.Value;

                if (!lowStamina && !sleepy) return;

                if (pc.IsDeadOrSleeping) return;

                if (pc.IsInCombat) return;

                var bed = pc.things.Find<TraitBed>();

                if (bed != null)
                {
                     UnityEngine.Debug.Log("[Elin_AutoEatSleep] Bed found. Performing sleep...");
                     var action = new HotItemActionSleep();
                     action.Perform();
                }
             }
             catch (System.Exception ex)
             {
                 UnityEngine.Debug.LogError("[Elin_AutoEatSleep] Error in CheckAutoSleep: " + ex.Message);
             }
        }

        public static void CaptureAI()
        {
            var pc = EClass.pc;
            if (pc != null && pc.ai != null && !(pc.ai is AI_Idle))
            {
                _savedAI = pc.ai;
                // UnityEngine.Debug.Log("Debug: AI Captured (" + _savedAI.ToString() + ")");
            }
            else
            {
                _savedAI = null;
            }
        }

        public static void TryResumeAI()
        {
            var pc = EClass.pc;
            if (pc == null) return;

            if (_savedAI == null) return;

            if (pc.isDead) return;

            // 睡眠から覚めた直後のAIの状態が GoalEndTurn（ターン終了処理）
            if (pc.ai is AI_Idle || pc.ai == null || pc.ai.GetType().Name == "GoalEndTurn")
            {
                // Validation and Reset logic to prevent crashes (especially with AutoActMod)
                if (!ValidateAndResetAI(_savedAI))
                {
                     UnityEngine.Debug.LogError("[Elin_AutoEatSleep] Failed to resume AI: Validation failed or state corrupted.");
                     _savedAI = null;
                     return;
                }

                UnityEngine.Debug.Log("[Elin_AutoEatSleep] Resuming AI (" + _savedAI.ToString() + ")");
                pc.SetAI(_savedAI);
                _savedAI = null;
            }
        }

        private static bool ValidateAndResetAI(AIAct ai)
        {
            try
            {
                // 1. Reset Enumerator to ensure the action starts fresh (Run() is called again)
                var fieldEnum = typeof(AIAct).GetField("Enumerator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldEnum != null)
                {
                    fieldEnum.SetValue(ai, null);
                }

                // 2. AutoActMod handling
                var type = ai.GetType();
                if (type.Name.Contains("AutoAct"))
                {
                    // Set useOriginalPos = true (to allow resuming from current/original intent)
                    var fieldUseOriginalPos = type.GetField("useOriginalPos", BindingFlags.Public | BindingFlags.Instance);
                    if (fieldUseOriginalPos != null)
                    {
                        fieldUseOriginalPos.SetValue(ai, true);
                    }

                    // Check if child is null (Critical for AutoAct subclasses like AutoActHarvestMine)
                    if (ai.child == null)
                    {
                        UnityEngine.Debug.LogWarning("[Elin_AutoEatSleep] AutoAct child is null. Attempting restoration...");

                        AIAct restoredChild = null;

                        // Strategy: Look for specific fields that hold the source task
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach(var f in fields)
                        {
                            if (f.FieldType.IsSubclassOf(typeof(AIAct)) || f.FieldType.IsSubclassOf(typeof(Task)))
                            {
                                if (f.Name == "initTask" || f.Name == "taskHarvest" || f.Name == "taskMine" || f.Name == "source")
                                {
                                    var val = f.GetValue(ai) as AIAct;
                                    if (val != null)
                                    {
                                        restoredChild = val;
                                        break;
                                    }
                                }
                            }
                        }

                        if (restoredChild != null)
                        {
                            UnityEngine.Debug.Log("[Elin_AutoEatSleep] Restored child from field: " + restoredChild.ToString());
                            ai.child = restoredChild;
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("[Elin_AutoEatSleep] Could not find valid child task to restore for " + type.Name);
                            return false; // Safe abort
                        }
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("[Elin_AutoEatSleep] Error in ValidateAndResetAI: " + ex.Message);
                return false;
            }
        }
    }
}
