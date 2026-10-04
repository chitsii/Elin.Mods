using System;
using System.Collections.Generic;

// A snapshot difference never grants ownership. Record is called only at the observed
// ThingGen.Create callsites inside the real fixture instance's GiveReward method.
public sealed class Pr3RewardOwnership
{
    private sealed class Evidence
    {
        public object Card;
        public string Id;
        public int Quantity;
    }

    private readonly Dictionary<int, Evidence> created = new Dictionary<int, Evidence>();
    private readonly HashSet<int> ambiguous = new HashSet<int>();
    public int Count => created.Count;

    public bool Record(int uid, object card, string id, int quantity, object producer, object fixture, bool inBaseline)
    {
        if (card == null || producer == null || fixture == null || !object.ReferenceEquals(producer, fixture) ||
            inBaseline || quantity <= 0 || (id != "plat" && id != "lovepotion") || ambiguous.Contains(uid)) return false;
        Evidence previous;
        if (created.TryGetValue(uid, out previous))
        {
            // A repeated/reused UID cannot expand cleanup authority.
            ambiguous.Add(uid);
            created.Remove(uid);
            return false;
        }
        created.Add(uid, new Evidence { Card = card, Id = id, Quantity = quantity });
        return true;
    }

    public bool CanRemove(int uid, object card, string id, int quantity)
    {
        Evidence evidence;
        return created.TryGetValue(uid, out evidence) && object.ReferenceEquals(evidence.Card, card) &&
            evidence.Id == id && evidence.Quantity == quantity;
    }
}
