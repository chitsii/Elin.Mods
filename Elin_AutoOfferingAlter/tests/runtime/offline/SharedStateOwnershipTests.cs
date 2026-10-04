using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

internal static class SharedStateOwnershipTests
{
    private sealed class Node
    {
        public int Uid;
        public bool Destroyed;
        public readonly List<Node> Children = new List<Node>();
    }
    internal static void Run()
    {
        // Opaque managed identity markers only: no Chara constructor or gameplay body runs.
        var ownerA = (Chara)FormatterServices.GetUninitializedObject(typeof(Chara));
        var ownerB = (Chara)FormatterServices.GetUninitializedObject(typeof(Chara));
        var outer = new Pr8NativeStateScope();
        try
        {
            foreach (string fault in new[] { "none", "creation", "cleanup" })
            {
                List<Stats> stats = NativeStats();
                var originalBuffers = new List<int[]>();
                foreach (Stats stat in stats) { var raw = new[] { 2, 7, 11 }; stat.Set(raw, 1, ownerA); originalBuffers.Add(raw); }
                var originalRng = new Random(321);
                var twin = new Random(321);
                Rand._random = originalRng; Rand.baseSeed = 77; Rand.MaxBytes = 8; Rand.bytes = new byte[] { 1, 2, 3 };
                byte[] bytes = Rand.bytes;
                var scope = new Pr8NativeStateScope();
                bool injected = false;
                try
                {
                    scope.Begin();
                    foreach (Stats stat in stats) stat.Set(new[] { 23, 29 }, 0, ownerB);
                    for (int i = 0; i < 20; i++) Rand.rnd(9999);
                    Rand.SetSeed(912); Rand.rnd(9999); Rand.SetBaseSeed(123); Rand.InitBytes(413);
                    if (fault == "creation") throw new InvalidOperationException("expected creation failure");
                }
                catch (InvalidOperationException ex) { Require(ex.Message == "expected creation failure", "Unexpected setup failure."); injected = true; }
                finally
                {
                    try
                    {
                        Rand.SetSeed(615);
                        foreach (Stats stat in stats) stat.Set(new[] { 31, 37 }, 1, ownerB);
                        if (fault == "cleanup") throw new InvalidOperationException("expected cleanup failure");
                    }
                    catch (InvalidOperationException ex) { Require(ex.Message == "expected cleanup failure", "Unexpected cleanup failure."); injected = true; }
                    finally { scope.Dispose(); }
                }
                scope.AssertRestored();
                Require(ReferenceEquals(BaseStats.CC, ownerA), "Original native CC not restored.");
                for (int i = 0; i < stats.Count; i++)
                    Require(ReferenceEquals(stats[i].raw, originalBuffers[i]) && stats[i].rawIndex == 1 && stats[i].raw[1] == 7,
                        "Native shared Stats binding/data changed.");
                Require(ReferenceEquals(Rand._random, originalRng) && Rand.baseSeed == 77 && Rand.MaxBytes == 8
                    && ReferenceEquals(Rand.bytes, bytes), "Native Rand reference/settings changed.");
                for (int i = 0; i < 12; i++) Require(originalRng.Next() == twin.Next(), "Original RNG stream was consumed.");
                Require((fault == "none") != injected, "Expected fault did not run.");
                Console.WriteLine("PASS: all native Stats raw/index/CC + unconsumed Rand/settings restored (" + fault + ")");
            }
            foreach (string bad in new[] { "Stats raw", "Stats index", "CC", "static Stats replacement", "RNG reference", "seed settings" })
            {
                var scope = new Pr8NativeStateScope();
                try
                {
                    if (bad == "Stats raw") Stats.Depression.raw = new[] { 43 };
                    if (bad == "Stats index") Stats.Depression.rawIndex++;
                    if (bad == "CC") BaseStats.CC = ownerB;
                    if (bad == "static Stats replacement") Stats.Depression = new Stats();
                    if (bad == "RNG reference") Rand.SetSeed(55);
                    if (bad == "seed settings") Rand.SetBaseSeed(56);
                    Reject(scope.AssertRestored, "unrestored " + bad);
                }
                finally { scope.Dispose(); }
            }
            // Restoring only the reference cannot undo stream consumption that occurred before isolation.
            var consumedScope = new Pr8NativeStateScope();
            Rand.rnd(9999); consumedScope.Begin(); Rand.SetSeed(73);
            Reject(consumedScope.Dispose, "original RNG consumed before isolation despite final reference restoration");
            var dataScope = new Pr8NativeStateScope();
            Stats.Depression.raw[0]++;
            Reject(dataScope.Dispose, "original Stats data mutation (never repaired)");
        }
        finally { outer.Dispose(); }

        Require(Pr8PartialCleanup.CanSupplyEmptyRenderer(true, false, true, true, true, false, false, true, true, 0),
            "Owned empty unregistered partial actor must permit native cleanup preparation.");
        bool[] safe = { true, false, true, true, true, false, false, true, true };
        for (int i = 0; i < safe.Length; i++)
        {
            bool[] bad = (bool[])safe.Clone(); bad[i] = !bad[i];
            Require(!Pr8PartialCleanup.CanSupplyEmptyRenderer(bad[0], bad[1], bad[2], bad[3], bad[4], bad[5], bad[6], bad[7], bad[8], 0),
                "Unsafe partial actor condition accepted: " + i);
        }
        Require(!Pr8PartialCleanup.CanSupplyEmptyRenderer(true, false, true, true, true, false, false, true, true, 1),
            "Nonempty partial actor must fail this cleanup path.");
        Console.WriteLine("PASS: partial cleanup accepts only owned/uncreated/renderer-less/unregistered/empty/party-less/held-less actors; 10 unsafe counterexamples rejected");

        var ledger = Ledger();
        Node box = Allocate(ledger, 100), child = Allocate(ledger, 101), nested = Allocate(ledger, 102);
        box.Children.Add(child); child.Children.Add(nested);
        Destroy(ledger, box, null);
        Require(box.Destroyed && child.Destroyed && nested.Destroyed, "Owned nested generation cleanup failed.");
        Console.WriteLine("PASS: observed fresh nested generation can be reclaimed");
        foreach (string bad in new[] { "existing callback insertion", "unobserved old UID", "unobserved new UID", "original UID collision", "nested foreign item" })
        {
            ledger = Ledger(); var original = new Node { Uid = 5 }; ledger.CaptureOriginal(original);
            box = Allocate(ledger, 100); child = Allocate(ledger, 101); box.Children.Add(child);
            Node foreign = bad == "existing callback insertion" || bad == "nested foreign item" ? original
                : new Node { Uid = bad == "unobserved old UID" ? 6 : 150 };
            if (bad == "original UID collision") { foreign = Allocate(ledger, 151); foreign.Uid = 5; }
            if (bad == "nested foreign item") child.Children.Add(foreign); else box.Children.Add(foreign);
            Reject(() => Destroy(ledger, box, null), bad);
            Require(!foreign.Destroyed && !box.Destroyed && !child.Destroyed, "Foreign card or enclosing tree was deleted.");
            Reject(() => ledger.ObserveSplit(child, original), "split result substituted with original");
        }
        ledger = Ledger(); var lateOriginal = new Node { Uid = 7 }; ledger.CaptureOriginal(lateOriginal);
        box = Allocate(ledger, 100); child = Allocate(ledger, 101); box.Children.Add(child);
        bool insert = true;
        Reject(() => Destroy(ledger, box, () => { if (insert) { insert = false; child.Children.Add(lateOriginal); } }),
            "foreign callback insertion after outer precheck, before recursive native Destroy");
        Require(!lateOriginal.Destroyed, "Recursive guard missed late original insertion.");
        ledger = Ledger(); var persisted = new Node { Uid = 8 }; var persistedChild = new Node { Uid = 9 };
        persisted.Children.Add(persistedChild); ledger.CaptureOriginal(persisted);
        ledger.AuthorizePersisted(persisted);
        Reject(() => ledger.GuardTree(persisted), "persisted box with unauthorized child");
        ledger.AuthorizePersisted(persistedChild); Destroy(ledger, persisted, null);
        Require(persisted.Destroyed && persistedChild.Destroyed, "Exact authorized persisted tree cleanup failed.");
        Console.WriteLine("PASS: persisted cleanup requires explicit authorization of every child");
    }
    private static List<Stats> NativeStats()
    {
        var result = new List<Stats>();
        foreach (FieldInfo field in typeof(Stats).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (typeof(Stats).IsAssignableFrom(field.FieldType)) result.Add((Stats)field.GetValue(null));
        return result;
    }
    private static Pr8OwnedTree<Node> Ledger() { return new Pr8OwnedTree<Node>(100, node => node.Uid, node => node.Children); }
    private static Node Allocate(Pr8OwnedTree<Node> ledger, int uid)
    { var node = new Node(); ledger.ObserveAllocation(node); node.Uid = uid; return node; }
    private static void Destroy(Pr8OwnedTree<Node> ledger, Node root, Action afterOuterGuard)
    {
        ledger.GuardTree(root);
        if (afterOuterGuard != null) afterOuterGuard();
        foreach (Node child in root.Children) Destroy(ledger, child, null);
        root.Destroyed = true;
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidOperationException) { Console.WriteLine("PASS: rejects " + name); return; }
        throw new InvalidOperationException("Counterexample falsely passed: " + name);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
