using HarmonyLib;

namespace Elin_ModTemplate
{
    // TODO: Rename this class and specify the target type/method.
    //
    // Usage:
    //   [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    //
    // Prefix runs before the original method.
    //   - Return false to skip the original method.
    //   - Use __instance to access the patched object.
    //   - Use __result to set the return value (when skipping).
    //
    // Postfix runs after the original method.
    //   - Use __result to read/modify the return value.
    //   - Use __instance to access the patched object.
    //
    // See: https://harmony.pardeike.net/articles/patching.html

    // [HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeMethod))]
    // public static class SamplePatch
    // {
    //     static void Prefix(SomeClass __instance)
    //     {
    //         if (!ModConfig.EnableMod.Value) return;
    //         // TODO: Your prefix logic
    //     }
    //
    //     static void Postfix(SomeClass __instance, ref SomeReturnType __result)
    //     {
    //         if (!ModConfig.EnableMod.Value) return;
    //         // TODO: Your postfix logic
    //     }
    // }
}
