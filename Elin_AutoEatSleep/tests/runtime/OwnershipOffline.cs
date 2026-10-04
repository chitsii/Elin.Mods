public static class Pr1OwnershipOffline
{
    private sealed class Node
    {
        public int Id;
        public object Parent;
        public readonly System.Collections.Generic.List<Node> Children = new System.Collections.Generic.List<Node>();
    }
    public static int Main()
    {
        try
        {
            int destroys = 0;
            var root = new Node { Id = 1 };
            var child = new Node { Id = 2, Parent = root };
            root.Children.Add(child);
            var gate = new Pr1FixtureOwnership<Node>(n => n.Id, n => n.Parent, n => n.Children, new[] { 99 });
            gate.RecordCreatedTree(root);
            var stranger = new Node { Id = 99, Parent = root };
            root.Children.Add(stranger);
            MustReject(() => gate.DestroyChecked(root, n => destroys++), "unknown original child");
            MustReject(() => gate.RecordCreatedTree(stranger), "adopting original item");
            if (destroys != 0) throw new System.Exception("Unknown child reached destruction callback.");
            root.Children.Remove(stranger);
            child.Parent = new object();
            MustReject(() => gate.DestroyChecked(root, n => destroys++), "changed owner");
            child.Parent = root;
            root.Children[0] = new Node { Id = 2, Parent = root };
            MustReject(() => gate.DestroyChecked(root, n => destroys++), "same UID different reference");
            root.Children[0] = child;
            child.Id = 3;
            MustReject(() => gate.DestroyChecked(root, n => destroys++), "changed UID");
            child.Id = 2;
            gate.DestroyChecked(root, n => destroys++);
            if (destroys != 1) throw new System.Exception("Known unchanged tree did not reach destruction exactly once.");
            System.Console.WriteLine("Fixture destruction gate: original/unknown child, owner change, reference replacement, UID change rejected; known tree accepted.");
            return 0;
        }
        catch (System.Exception ex) { System.Console.WriteLine("FAIL: " + ex.Message); return 1; }
    }
    private static void MustReject(System.Action action, string label)
    {
        bool rejected = false;
        try { action(); } catch (System.InvalidOperationException) { rejected = true; }
        if (!rejected) throw new System.Exception("Expected cleanup rejection: " + label);
    }
}
