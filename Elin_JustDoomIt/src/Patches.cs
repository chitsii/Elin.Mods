using HarmonyLib;

namespace Elin_ModTemplate
{
    [HarmonyPatch(typeof(TraitSlotMachine), nameof(TraitSlotMachine.OnUse))]
    public static class Patch_TraitSlotMachine_OnUse
    {
        private const string DoomArcadeThingId = "justdoomit_arcade";

        static bool Prefix(TraitSlotMachine __instance, Chara c, ref bool __result)
        {
            try
            {
                if (__instance == null || __instance.owner == null)
                {
                    return true;
                }

                // Only handle the custom DOOM cabinet here.
                if (__instance.owner.id != DoomArcadeThingId)
                {
                    return true;
                }

                if (DoomSessionManager.Instance == null)
                {
                    return true;
                }

                if (!DoomSessionManager.Instance.TryHandleMachineUse(__instance.owner, c, ref __result))
                {
                    // Fallback to vanilla behavior when no WAD is available, etc.
                    return true;
                }

                // We handled the interaction.
                return false;
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] TraitSlotMachine.OnUse patch failed. Fallback to vanilla.", ex);
                return true;
            }
        }
    }
}
