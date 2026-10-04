// Counterexamples exercise the exact test-only gate used before native destruction.
// These are offline policy tests, not a substitute for the game's integration cases.
public static class Pr5OwnedCleanupGuardTests
{
    private sealed class Node
    {
        public int Uid;
        public object Parent;
        public bool Destroyed;
        public readonly System.Collections.Generic.List<Node> Children = new System.Collections.Generic.List<Node>();
    }
    private sealed class Fixture
    {
        public readonly object Zone = new object();
        public readonly Node Root = new Node { Uid = 1 };
        public readonly Node Bag = new Node { Uid = 2 };
        public readonly Node Item = new Node { Uid = 3 };
        public readonly Pr5OwnedCleanupGuard<Node> Guard;
        public Fixture()
        {
            Root.Children.Add(Bag); Bag.Parent = Root;
            Bag.Children.Add(Item); Item.Parent = Bag;
            Guard = new Pr5OwnedCleanupGuard<Node>(Root, n => n.Uid, n => n.Children,
                n => n.Parent, n => n.Destroyed, Zone);
            Root.Parent = Zone;
        }
    }
    private static int passed;
    private static int failed;
    public static int Main()
    {
        Run("unknown_direct_child", () =>
        {
            var f = new Fixture();
            f.Root.Children.Add(new Node { Uid = 99, Parent = f.Root });
            RejectDestroy(f, f.Root);
        });
        Run("unknown_nested_child_blocks_every_entry", () =>
        {
            var f = new Fixture();
            f.Bag.Children.Add(new Node { Uid = 99, Parent = f.Bag });
            RejectDestroy(f, f.Root); RejectDestroy(f, f.Bag); RejectDestroy(f, f.Item);
        });
        Run("same_uid_replacement_reference", () =>
        {
            var f = new Fixture();
            f.Bag.Children[0] = new Node { Uid = f.Item.Uid, Parent = f.Bag };
            RejectDestroy(f, f.Root);
        });
        Run("captured_uid_changed", () =>
        {
            var f = new Fixture(); f.Item.Uid = 99; RejectDestroy(f, f.Root);
        });
        Run("root_owner_changed", () =>
        {
            var f = new Fixture(); f.Root.Parent = new object(); RejectDestroy(f, f.Root);
        });
        Run("item_parent_changed", () =>
        {
            var f = new Fixture(); f.Item.Parent = new object(); RejectDestroy(f, f.Item);
        });
        Run("owned_item_moved_to_foreign_inventory", () =>
        {
            var f = new Fixture(); f.Bag.Children.Remove(f.Item); f.Item.Parent = new object();
            RejectDestroy(f, f.Root); RejectDestroy(f, f.Item);
        });
        Run("missing_live_child", () =>
        {
            var f = new Fixture(); f.Bag.Children.Remove(f.Item); RejectDestroy(f, f.Root);
        });
        Run("unknown_destroy_target", () =>
        {
            var f = new Fixture(); RejectDestroy(f, new Node { Uid = 99 });
        });
        Run("condition_owner_changed_callback_zero", () =>
        {
            var f = new Fixture(); int callbacks = 0;
            bool rejected = false;
            try { f.Guard.MutateOwned(f.Root, new object(), f.Root, () => callbacks++); }
            catch (System.InvalidOperationException) { rejected = true; }
            Require(rejected && callbacks == 0, "Foreign Condition owner invoked cleanup callback.");
        });
        Run("condition_kill_inserts_unknown_before_destroy", () =>
        {
            var f = new Fixture();
            f.Guard.MutateOwned(f.Root, f.Root, f.Root,
                () => f.Bag.Children.Add(new Node { Uid = 99, Parent = f.Bag }));
            RejectDestroy(f, f.Root); RejectDestroy(f, f.Item);
        });
        Run("valid_postorder_cleanup", () =>
        {
            var f = new Fixture(); int callbacks = 0;
            f.Guard.MutateOwned(f.Root, f.Root, f.Root, () => callbacks++);
            foreach (var node in f.Guard.DestructionOrder)
                f.Guard.DestroyOwned(node, () =>
                {
                    callbacks++;
                    if (node.Parent is Node p) p.Children.Remove(node);
                    node.Parent = null; node.Destroyed = true;
                });
            Require(callbacks == 4 && f.Root.Destroyed && f.Bag.Destroyed && f.Item.Destroyed,
                "Valid owned cleanup was blocked or reordered.");
        });
        Run("late_unknown_after_first_cleanup_entry", () =>
        {
            var f = new Fixture();
            f.Guard.DestroyOwned(f.Item, () =>
            {
                f.Bag.Children.Remove(f.Item); f.Item.Destroyed = true; f.Item.Parent = null;
                f.Bag.Children.Add(new Node { Uid = 99, Parent = f.Bag });
            });
            RejectDestroy(f, f.Bag); RejectDestroy(f, f.Root);
        });
        System.Console.WriteLine("offline ownership guard: passed=" + passed + ":failed=" + failed + ":runtimeExecuted=false");
        return failed == 0 ? 0 : 1;
    }
    private static void RejectDestroy(Fixture f, Node node)
    {
        int callbacks = 0; bool rejected = false;
        try { f.Guard.DestroyOwned(node, () => callbacks++); }
        catch (System.InvalidOperationException) { rejected = true; }
        Require(rejected && callbacks == 0, "Unsafe tree invoked destruction callback: " + node.Uid);
    }
    private static void Run(string id, System.Action test)
    {
        try { test(); passed++; System.Console.WriteLine("PASS " + id); }
        catch (System.Exception ex) { failed++; System.Console.WriteLine("FAIL " + id + ":" + ex.Message); }
    }
    private static void Require(bool ok, string reason)
    {
        if (!ok) throw new System.InvalidOperationException(reason);
    }
}
