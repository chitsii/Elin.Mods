// This is the real fixture cleanup boundary, shared by runtime cleanup and offline safety tests.
public static class ArsPr4CleanupBoundary
{
    public static void Run<T>(T owned, int uid, System.Func<T, int> uidOf,
        System.Collections.Generic.IEnumerable<T> global,
        System.Collections.Generic.IEnumerable<T> map,
        System.Collections.Generic.List<T> carryover,
        System.Action<T, System.Action> mutation) where T : class
    {
        if (owned == null || uidOf == null || global == null || map == null || carryover == null || mutation == null)
            throw new System.InvalidOperationException("Cleanup inputs unavailable; no mutation attempted.");
        if (uid <= 0 || uidOf(owned) != uid)
            throw new System.InvalidOperationException("Invalid fixture UID; no mutation attempted.");
        foreach (var source in new[] { global, map, carryover })
        {
            foreach (var card in source)
                if (card != null && uidOf(card) == uid && !object.ReferenceEquals(card, owned))
                    throw new System.InvalidOperationException(
                        "Fixture UID has an unowned instance in global/map/carryover; no mutation attempted. Restore dedicated save.");
        }
        mutation(owned, () => carryover.RemoveAll(c => object.ReferenceEquals(c, owned)));
    }
}
