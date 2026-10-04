using System.Collections.Generic;

namespace Elin_AutoOfferingAlter
{
    public static class OfferLogic
    {
        public static void Process(Thing container)
        {
            Chara actor = EClass.pc;
            if (container == null || actor == null || actor.faith == null)
            {
                return;
            }

            Religion faith = actor.faith;
            OfferingEffectContext effectContext;
            // Validate the actual world map before any native Split or metadata changes.
            if (!OfferingEffectContext.TryCreate(container, actor, out effectContext)) return;

            // Setup fake altar
            TraitAltar fakeAltar = new TraitAltar();
            fakeAltar.SetOwner(container);

            string originalName = container.c_altName;
            string originalDeity = container.c_idDeity;
            fakeAltar.SetDeity(faith.id);
            container.c_altName = faith.Name;

            OfferingBatchRunner<Thing> batchRunner = new OfferingBatchRunner<Thing>(
                getNum: item => item.Num,
                isDestroyed: item => item.isDestroyed,
                isNonConsuming: item => item.id == "water",
                split: (item, amount) => item.Split(amount),
                isDetached: item => item.parent == null,
                returnDetached: item => container.AddThing(item),
                canContinue: () => CanContinue(actor, faith) && effectContext.IsCurrent());

            try
            {
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
                    if (!CanContinue(actor, faith) || !effectContext.IsCurrent() || t == null || t.isDestroyed || !fakeAltar.CanOffer(actor, t))
                    {
                        continue;
                    }

                    int unitValue = faith.GetOfferingValue(t, 1);
                    OfferingBatchResult result = batchRunner.Run(t, unitValue, itemToOffer =>
                    {
                        // A Split callback can change the context after the pre-split gate.
                        if (!CanContinue(actor, faith) || !effectContext.IsCurrent()) return;
                        if (ModConfig.EnableLog.Value)
                        {
                            Plugin.Log.LogInfo($"[Elin_AutoOfferingAlter] Offering item: {itemToOffer.Name} (x{itemToOffer.Num})");
                        }
                        using (effectContext.Enter())
                            fakeAltar.OnOffer(actor, itemToOffer);
                    });

                    if (result == OfferingBatchResult.Stopped)
                    {
                        return;
                    }
                }
            }
            finally
            {
                container.c_idDeity = originalDeity;
                container.c_altName = originalName;
            }
        }

        private static bool CanContinue(Chara actor, Religion faith)
        {
            return EClass.pc == actor && actor != null && !actor.isDead && actor.faith == faith;
        }
    }
}
