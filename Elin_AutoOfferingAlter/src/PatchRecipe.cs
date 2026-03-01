using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Elin_AutoOfferingAlter
{
    [HarmonyPatch]
    public class PatchRecipe
    {
        // Patch SourceManager.Init to inject our custom item BEFORE the game builds recipes.
        // This allows the game to naturally discover the item and generate a valid recipe for it.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SourceManager), "Init")]
        public static void Prefix_SourceManager_Init(SourceManager __instance)
        {
            string customId = Plugin.ID_OFFERING_BOX;

            // Efficient check if already added
            if (__instance.things.map.ContainsKey(customId)) return;

            // Find base row for "chest6" (Sturdy Box)
            SourceThing.Row row = __instance.things.map.TryGetValue("chest6", out var chest6) ? chest6 : null;

            if (row == null) return;

            // Clone the row using Reflection
            MethodInfo cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
            SourceThing.Row newRow = (SourceThing.Row)cloneMethod.Invoke(row, null);

            if (ModConfig.EnableLog.Value)
            {
                Plugin.Log.LogInfo("Src ID: " + row.id);
                Plugin.Log.LogInfo("Src Trait: " + (row.trait != null ? string.Join(",", row.trait) : "null"));
                Plugin.Log.LogInfo("Src Components: " + (row.components != null ? string.Join(",", row.components) : "null"));
            }

            // Set properties
            newRow.id = customId;
            newRow.factory = new string[] { "self" }; // Simple craft requires "self" explicitly
            // Retain original components/level from chest6
            newRow.recipeKey = new string[] { "*" }; // Ensure it is treated as a known/valid recipe source
            newRow.name_JP = "信仰の箱";
            newRow.name = "Offering Box";

            // Add to database
            __instance.things.rows.Add(newRow);
            if (!__instance.things.map.ContainsKey(customId))
            {
                __instance.things.map.Add(customId, newRow);
            }

            if (ModConfig.EnableLog.Value) Plugin.Log.LogInfo("Injected custom item: " + customId);
        }

        // Keep the Craft patch to apply specific tags and names
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Recipe), "Craft")]
        public static void Postfix_Craft(Recipe __instance, ref Thing __result)
        {
            if (__result == null) return;

            if (__instance.id == Plugin.ID_OFFERING_BOX)
            {
                __result.c_idDeity = Plugin.ID_OFFERING_BOX;
                __result.c_altName = "信仰の箱";

                if (ModConfig.EnableLog.Value)
                {
                    Plugin.Log.LogInfo("Crafted Offering Box!");
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player))]
    public class PatchPlayer
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnLoad")]
        public static void Postfix_OnLoad(Player __instance)
        {
            AddCustomRecipe();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnStartNewGame")]
        public static void Postfix_OnStartNewGame(Player __instance)
        {
            AddCustomRecipe();
        }

        private static void AddCustomRecipe()
        {
            try
            {
                string customId = Plugin.ID_OFFERING_BOX;
                if (!EClass.player.recipes.knownRecipes.ContainsKey(customId))
                {
                    EClass.player.recipes.Add(customId, false);
                    if (ModConfig.EnableLog.Value) Plugin.Log.LogInfo("Learned custom recipe: " + customId);
                }
            }
            catch { }
        }
    }
}
