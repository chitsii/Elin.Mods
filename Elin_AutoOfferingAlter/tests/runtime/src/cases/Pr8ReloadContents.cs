#if RUNTIME_TEST
using System;
using System.Collections.Generic;

// Shared by the live assertions and offline counterexamples; no native processing is replaced.
public static class Pr8ReloadContents
{
    public static void Assert<T>(IEnumerable<T> children, Func<T, int> uid, Func<T, int> num,
        int waterUid, int rejectUid, int waterNum, int rejectNum) where T : class
    {
        var items = new List<T>(children);
        if (items.Count != 2) throw new InvalidOperationException("Reload fixture must retain exactly two children.");
        if (waterUid == 0 || rejectUid == 0 || waterUid == rejectUid)
            throw new InvalidOperationException("Reload manifest must identify two distinct children.");
        T water = null, rejected = null;
        foreach (T item in items)
        {
            if (uid(item) == waterUid) water = item;
            if (uid(item) == rejectUid) rejected = item;
        }
        if (water == null) throw new InvalidOperationException("Persisted water UID missing.");
        if (rejected == null) throw new InvalidOperationException("Persisted rejected UID missing.");
        if (num(water) != waterNum) throw new InvalidOperationException("Persisted water quantity mismatch.");
        if (num(rejected) != rejectNum) throw new InvalidOperationException("Persisted rejected quantity mismatch.");
    }
}
#endif
