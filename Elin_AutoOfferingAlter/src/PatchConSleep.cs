using HarmonyLib;

namespace Elin_AutoOfferingAlter
{
    [HarmonyPatch(typeof(ConSleep))]
    public class PatchConSleep
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnRemoved")]
        public static void Postfix_OnRemoved(ConSleep __instance)
        {
            if (__instance.owner.IsPC && __instance.slept)
            {
                if (ModConfig.EnableLog.Value)
                {
                    Plugin.Log.LogInfo("[Elin_AutoOfferingAlter] Woke up. Checking for offerings...");
                }

                // Scan inventory (excluding equipped items to avoid duplication)
                foreach (Thing t in EClass.pc.things)
                {
                    if (!t.isEquipped && t.id == Plugin.ID_OFFERING_BOX && t.IsContainer)
                    {
                        OfferLogic.Process(t);
                    }
                }

                // Scan body slots (for equipped Offering Boxes)
                // Scan body slots (for equipped Offering Boxes or containers holding them)
                foreach (BodySlot slot in EClass.pc.body.slots)
                {
                    if (slot.thing == null) continue;

                    /*
                    if (ModConfig.EnableLog.Value)
                    {
                        Plugin.Log.LogInfo($"[DEBUG] Checking slot {slot.elementId}: {slot.thing.Name}");
                    }
                    */

                    // Case 1: The equipped item IS the Offering Box (e.g., held in hand)
                    if (slot.thing.id == Plugin.ID_OFFERING_BOX && slot.thing.IsContainer)
                    {
                        // Plugin.Log.LogInfo($"[DEBUG] Found equipped Offering Box directly in slot {slot.elementId}");
                        OfferLogic.Process(slot.thing);
                    }
                    // Case 2: The equipped item is a Tool Belt that might CONTAIN an Offering Box
                    // User requested to limit search to tool belts.
                    else if (slot.thing.trait is TraitToolBelt && slot.thing.things.Count > 0)
                    {
                        foreach (Thing innerThing in slot.thing.things)
                        {
                            if (innerThing.id == Plugin.ID_OFFERING_BOX && innerThing.IsContainer)
                            {
                                // Plugin.Log.LogInfo($"[DEBUG] Found Offering Box inside equipped Tool Belt ({slot.thing.Name}) in slot {slot.elementId}");
                                OfferLogic.Process(innerThing);
                            }
                        }
                    }
                }
            }
        }
    }
}
