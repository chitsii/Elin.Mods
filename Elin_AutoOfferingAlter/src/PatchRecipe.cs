using HarmonyLib;
using System;
using System.Reflection;

namespace Elin_AutoOfferingAlter
{
    [HarmonyPatch]
    public class PatchRecipe
    {
        public static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.DeclaredMethod(typeof(SourceManager), nameof(SourceManager.Init), Type.EmptyTypes);
            if (method == null ||
                method.DeclaringType != typeof(SourceManager) ||
                method.IsGenericMethod ||
                method.ContainsGenericParameters ||
                method.GetParameters().Length != 0)
            {
                throw new MissingMethodException("Expected declared non-generic SourceManager.Init().");
            }

            return method;
        }

        // SourceManager.Init calls things.Init before recipes.Init. Add the row before
        // that standard Init builds SourceThing.map so the recipe can resolve it.
        [HarmonyPrefix]
        public static void Prefix_SourceManager_Init(SourceManager __instance)
        {
            if (__instance == null || __instance.initialized)
            {
                return;
            }

            InjectOfferingBoxRow(__instance.things);
        }

        private static void InjectOfferingBoxRow(SourceThing source)
        {
            if (source == null || source.rows == null)
            {
                return;
            }

            string customId = Plugin.ID_OFFERING_BOX;
            SourceThing.Row row = null;

            foreach (SourceThing.Row sourceRow in source.rows)
            {
                if (sourceRow.id == customId)
                {
                    return;
                }
                if (sourceRow.id == "chest6")
                {
                    row = sourceRow;
                }
            }

            if (row == null)
            {
                return;
            }

            SourceThing.Row newRow = CloneSourceThingRow(row);

            if (ModConfig.EnableLog.Value)
            {
                Plugin.Log.LogInfo("Src ID: " + row.id);
                Plugin.Log.LogInfo("Src Trait: " + (row.trait != null ? string.Join(",", row.trait) : "null"));
                Plugin.Log.LogInfo("Src Components: " + (row.components != null ? string.Join(",", row.components) : "null"));
            }

            newRow.id = customId;
            newRow.factory = new string[] { "self" };
            newRow.recipeKey = new string[] { "*" };
            newRow.name_JP = "信仰の箱";
            newRow.name = "Offering Box";

            source.rows.Add(newRow);

            if (ModConfig.EnableLog.Value) Plugin.Log.LogInfo("Injected custom item: " + customId);
        }

        private static SourceThing.Row CloneSourceThingRow(SourceThing.Row row)
        {
            MethodInfo cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
            SourceThing.Row clone = (SourceThing.Row)cloneMethod.Invoke(row, null);

            foreach (FieldInfo field in typeof(SourceThing.Row).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.FieldType.IsArray)
                {
                    continue;
                }

                Array array = field.GetValue(row) as Array;
                if (array != null)
                {
                    field.SetValue(clone, array.Clone());
                }
            }

            return clone;
        }

        // Keep the Craft patch to apply the saved display name.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Recipe), "Craft")]
        public static void Postfix_Craft(Recipe __instance, ref Thing __result)
        {
            if (__result == null) return;

            if (__instance.id == Plugin.ID_OFFERING_BOX)
            {
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
