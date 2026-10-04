// Pure test-helper policy: placement is not evidence of creation or ownership.
public sealed class ArsPr4SummonOwnership
{
    private readonly System.Collections.Generic.HashSet<int> protectedUids;
    private readonly object expectedSpell;
    private readonly System.Collections.Generic.List<object> births = new System.Collections.Generic.List<object>();
    private readonly System.Collections.Generic.Dictionary<int, object> owned = new System.Collections.Generic.Dictionary<int, object>();

    public ArsPr4SummonOwnership(System.Collections.Generic.IEnumerable<int> existingUids, object spell)
    {
        protectedUids = new System.Collections.Generic.HashSet<int>(existingUids);
        expectedSpell = spell;
    }

    public bool RejectedResult { get; private set; }
    public System.Collections.Generic.IReadOnlyList<object> OwnedObjects => new System.Collections.Generic.List<object>(owned.Values);

    public void ObserveBirth(object card)
    {
        if (card == null) return;
        foreach (var birth in births) if (object.ReferenceEquals(birth, card)) return;
        births.Add(card);
    }

    public bool ObserveSpellResult(object card, int uid, object producer)
    {
        if (card == null || !object.ReferenceEquals(producer, expectedSpell)) return false;
        bool born = false;
        foreach (var birth in births) if (object.ReferenceEquals(birth, card)) { born = true; break; }
        if (uid <= 0 || protectedUids.Contains(uid) || !born)
        {
            RejectedResult = true;
            return false;
        }
        object previous;
        if (owned.TryGetValue(uid, out previous) && !object.ReferenceEquals(previous, card))
        {
            RejectedResult = true;
            return false;
        }
        owned[uid] = card;
        return true;
    }

    public bool IsOwned(object card, int uid)
    {
        object found;
        return owned.TryGetValue(uid, out found) && object.ReferenceEquals(found, card);
    }
}
