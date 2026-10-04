using Elin_AutoOfferingAlter;

static class EffectScopeTests
{
    sealed class P(int x, int z) { public int X = x, Z = z; public P Copy() => new(X, Z); }
    sealed class Inventory(P point)
    {
        public P Position = point;
        public int SavedX = point.X, SavedZ = point.Z;
        public int Num = 330;
        public int InvX => Position.X;
        public int InvY => Position.Z;
        public void Serialize() { SavedX = Position.X; SavedZ = Position.Z; }
    }
    public static void Run()
    {
        var owner = new P(-1, 0); var actor = new P(4, 7);
        var inventory = new Inventory(owner);
        bool current = true;
        P from = owner;
        Check(!OfferingEffectScope<P>.TryRedirect(ref from) && ReferenceEquals(from, owner), "FX outside scope changed");
        using (var scope = new OfferingEffectScope<P>(owner, () => current, actor.Copy))
        {
            from = owner;
            Check(OfferingEffectScope<P>.TryRedirect(ref from) && from.X == 4 && from.Z == 7
                && !ReferenceEquals(from, owner) && !ReferenceEquals(from, actor), "owner FX not copied to actor");
            from.X = 99; inventory.Serialize();
            Check(ReferenceEquals(inventory.Position, owner) && owner.X == -1 && owner.Z == 0 && actor.X == 4
                && inventory.InvX == -1 && inventory.InvY == 0 && inventory.SavedX == -1 && inventory.SavedZ == 0 && inventory.Num == 330,
                "FX/synchronous save altered Point, inventory, backing fields or quantity");
            from = new P(-1, 0);
            Check(!OfferingEffectScope<P>.TryRedirect(ref from) && from.X == -1, "equal coordinates passed exact-reference filter");
            from = actor;
            Check(!OfferingEffectScope<P>.TryRedirect(ref from) && ReferenceEquals(from, actor), "unrelated actor FX redirected");
            current = false; from = owner;
            Check(!OfferingEffectScope<P>.TryRedirect(ref from) && ReferenceEquals(from, owner), "changed PC/map/death gate redirected");
            current = true;
            bool foreignRedirected = true;
            var thread = new Thread(() => { P foreign = owner; foreignRedirected = OfferingEffectScope<P>.TryRedirect(ref foreign); });
            thread.Start(); thread.Join(); Check(!foreignRedirected, "scope flowed to another thread");
            var innerOwner = new P(-1, 2); var innerActor = new P(8, 9);
            using (new OfferingEffectScope<P>(innerOwner, () => true, innerActor.Copy))
            {
                from = owner; Check(!OfferingEffectScope<P>.TryRedirect(ref from), "inner scope used outer owner");
                from = innerOwner; Check(OfferingEffectScope<P>.TryRedirect(ref from) && from.X == 8, "inner destination lost");
            }
            from = owner; Check(OfferingEffectScope<P>.TryRedirect(ref from) && from.X == 4, "outer scope not restored");
        }
        from = owner; Check(!OfferingEffectScope<P>.TryRedirect(ref from), "scope leaked after success");
        var failure = new InvalidOperationException("native failure");
        try { using (new OfferingEffectScope<P>(owner, () => true, actor.Copy)) throw failure; }
        catch (InvalidOperationException ex) { Check(ReferenceEquals(ex, failure), "native exception replaced"); }
        from = owner; Check(!OfferingEffectScope<P>.TryRedirect(ref from), "scope leaked after exception");
        try
        {
            using (new OfferingEffectScope<P>(owner, () => true, () => throw failure))
                OfferingEffectScope<P>.TryRedirect(ref from);
        }
        catch (InvalidOperationException ex) { Check(ReferenceEquals(ex, failure), "copy exception replaced"); }
        from = owner; Check(!OfferingEffectScope<P>.TryRedirect(ref from), "scope leaked after destination exception");
        var outer = new OfferingEffectScope<P>(owner, () => true, actor.Copy);
        var inner = new OfferingEffectScope<P>(new P(-1, 3), () => true, actor.Copy);
        bool rejected = false;
        try { outer.Dispose(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "out-of-order disposal silently lost the scope stack");
        inner.Dispose(); outer.Dispose(); outer.Dispose();
        from = owner; Check(!OfferingEffectScope<P>.TryRedirect(ref from), "stack leaked after ordered recovery");
        var cells = new int[3, 5];
        foreach (var p in new[] { new P(0, 0), new P(2, 4) }) Check(OfferingMapBounds.Contains(cells, p.X, p.Z), "valid actual bounds rejected");
        foreach (var p in new[] { new P(-1, 0), new P(0, -1), new P(3, 0), new P(0, 5), new P(20, 20) })
            Check(!OfferingMapBounds.Contains(cells, p.X, p.Z), "invalid actual bounds accepted");
        Check(!OfferingMapBounds.Contains(null, 0, 0) && !OfferingMapBounds.Contains(new int[0, 5], 0, 0)
            && !OfferingMapBounds.Contains(new int[5], 0, 0), "missing/empty/rank-mismatched map accepted");
        foreach (string placement in new[] { "inventory", "equipped", "toolbelt" })
        {
            var data = new Inventory(new P(placement == "equipped" ? 42 : -1, placement == "toolbelt" ? 2 : 0));
            P slot = data.Position; int x = data.InvX, z = data.InvY;
            using (new OfferingEffectScope<P>(slot, () => true, actor.Copy))
            { P fx = slot; OfferingEffectScope<P>.TryRedirect(ref fx); data.Serialize(); }
            Check(ReferenceEquals(data.Position, slot) && data.InvX == x && data.InvY == z && data.SavedX == x && data.SavedZ == z && data.Num == 330,
                placement + " inventory state changed");
        }
        // Counterexample: the rejected position-swap proposal corrupts inventory/save state DURING callbacks.
        var bad = new Inventory(owner); P saved = bad.Position; bad.Position = actor.Copy(); bad.Serialize(); bad.Position = saved;
        Check(bad.SavedX != bad.InvX, "position-swap counterexample was not detected");
        Console.WriteLine("PASS: owner reference only; copied destination; save/quantity conservation; nested/thread/exception/PC gate; actual map bounds; rejects temporary owner position substitution");
    }
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
