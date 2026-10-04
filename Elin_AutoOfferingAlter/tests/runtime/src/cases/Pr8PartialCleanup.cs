#if RUNTIME_TEST
public static class Pr8PartialCleanup
{
    // Only the unregistered, empty, renderer-less allocation stage permits a cleanup-only renderer.
    // Native Card.Destroy still performs destruction and sets isDestroyed; no success flag is fabricated.
    public static bool CanSupplyEmptyRenderer(bool owned, bool created, bool rendererMissing, bool parentMissing,
        bool mapKnown, bool inMap, bool globallyRegistered, bool partyMissing, bool heldMissing, int children)
    {
        return owned && !created && rendererMissing && parentMissing && mapKnown && !inMap
            && !globallyRegistered && partyMissing && heldMissing && children == 0;
    }
}
#endif
