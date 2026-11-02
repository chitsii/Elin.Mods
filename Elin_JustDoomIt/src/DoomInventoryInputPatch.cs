using HarmonyLib;

namespace Elin_JustDoomIt
{
    [HarmonyPatch(typeof(UI), nameof(UI.ToggleInventory), new[] { typeof(bool) })]
    internal static class Patch_UI_ToggleInventory
    {
        // Elin's raw TAB check bypasses EInput.Consume.
        private static bool Prefix() => !DoomSessionManager.OwnsInventoryInput;
    }
}
