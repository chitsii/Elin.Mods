using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_AutoOfferingAlter
{
    public static class OfferLogic
    {
        public static void Process(Thing container)
        {
            // Setup fake altar
            TraitAltar fakeAltar = new TraitAltar();
            fakeAltar.SetOwner(container);

            if (ModConfig.EnableLog.Value)
            {
                Plugin.Log.LogInfo($"[Elin_AutoOfferingAlter] Processing container: {container.Name} (UID:{container.uid})");
            }

            // Create a safe list of items to iterate since offerings destroy items
            List<Thing> thingsToProcess = new List<Thing>();
            foreach (Thing t in container.things)
            {
                thingsToProcess.Add(t);
            }

            /*
            if (ModConfig.EnableLog.Value)
            {
                Plugin.Log.LogInfo($"[DEBUG] Container contains {thingsToProcess.Count} items.");
            }
            */

            foreach (Thing t in thingsToProcess)
            {
                if (fakeAltar.CanOffer(t))
                {
                    // Temporarily rename container for better log messages ("You offer to [God]")
                    string originalName = container.c_altName;
                    fakeAltar.SetDeity(EClass.pc.faith.id);
                    container.c_altName = EClass.pc.faith.Name;

                    try
                    {
                        OptimizeAndOffer(t, fakeAltar.Deity, (itemToOffer) =>
                        {
                            if (ModConfig.EnableLog.Value)
                            {
                                Plugin.Log.LogInfo($"[Elin_AutoOfferingAlter] Offering item: {itemToOffer.Name} (x{itemToOffer.Num})");
                            }
                            fakeAltar.OnOffer(EClass.pc, itemToOffer);
                        });
                    }
                    finally
                    {
                        container.c_altName = originalName;
                    }
                }
            }
        }

        // Formerly StackOptimizer.cs
        private static void OptimizeAndOffer(Thing t, Religion faith, Action<Thing> onOffer)
        {
            if (t.Num <= 1)
            {
                onOffer(t);
                return;
            }

            int unitVal = faith.GetOfferingValue(t, 1);
            if (unitVal <= 0)
            {
                onOffer(t);
                return;
            }

            // Calculate optimal batch size to hit ~1500 value (min 1 faith point)
            int targetVal = 1500;
            int batchSize = (targetVal + unitVal - 1) / unitVal;

            // Cap at 3000 value if possible to avoid waste
            int maxVal = 3000;
            if (batchSize * unitVal > maxVal)
            {
                batchSize = Math.Max(1, maxVal / unitVal);
            }

            // Digest remainder first
            int remainder = t.Num % batchSize;

            if (remainder > 0)
            {
                if (remainder < t.Num)
                {
                    onOffer(t.Split(remainder));
                }
                else
                {
                    onOffer(t);
                    return;
                }
            }

            // Digest batches
            while (t.Num > 0)
            {
                if (t.Num > batchSize)
                {
                    onOffer(t.Split(batchSize));
                }
                else
                {
                    onOffer(t);
                    break;
                }
            }
        }
    }
}
