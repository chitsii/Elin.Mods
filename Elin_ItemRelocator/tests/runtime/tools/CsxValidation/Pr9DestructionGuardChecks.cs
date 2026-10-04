public static class Pr9DestructionGuardChecks
{
    private sealed class Node
    {
        public bool Owned;
        public bool Destroyed;
        public readonly List<Node> Children = new List<Node>();
    }

    public static void Verify()
    {
        var known = new Node { Owned = true };
        var unknown = new Node();
        var root = new Node { Owned = true };
        root.Children.Add(known);
        root.Children.Add(unknown);
        Reject(root, "unknown-child");

        var nested = new Node { Owned = true };
        nested.Children.Add(root);
        Reject(nested, "unknown-grandchild");
        Reject(new Node(), "unknown-root");
        Reject(null, "null-root");

        var cycle = new Node { Owned = true };
        cycle.Children.Add(cycle);
        Reject(cycle, "cyclic-descendant");

        var safe = new Node { Owned = true };
        safe.Children.Add(new Node { Owned = true });
        int calls = 0;
        Pr9FixtureDestructionGuard.Destroy(safe, n => n.Owned, n => n.Children, n => {
            calls++;
            DestroyTree(n);
        });
        if (calls != 1 || !safe.Destroyed || !safe.Children[0].Destroyed)
            throw new InvalidOperationException("Owned fixture destroy was not called exactly once.");
        Console.WriteLine("destruction-guard-checks=6/6; unknown-descendant-destroy-calls=0; native-game-executed=false");
    }

    private static void Reject(Node root, string label)
    {
        int calls = 0;
        bool rejected = false;
        try {
            Pr9FixtureDestructionGuard.Destroy(root, n => n.Owned, n => n.Children, n => {
                calls++;
                DestroyTree(n);
            });
        }
        catch (InvalidOperationException) { rejected = true; }
        if (!rejected || calls != 0 || (root != null && root.Destroyed))
            throw new InvalidOperationException(label + ": destructive callback was not prevented.");
    }

    private static void DestroyTree(Node node)
    {
        node.Destroyed = true;
        foreach (var child in node.Children) DestroyTree(child);
    }
}
