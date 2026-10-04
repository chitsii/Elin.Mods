// A generation-time allowlist; an unsafe tree never reaches a mutation callback.
public sealed class Pr5OwnedCleanupGuard<T> where T : class
{
    private sealed class Entry
    {
        public T Node;
        public int Uid;
        public object Parent;
    }
    private readonly T root;
    private readonly object placementParent;
    private readonly System.Func<T, int> uid;
    private readonly System.Func<T, System.Collections.Generic.IEnumerable<T>> children;
    private readonly System.Func<T, object> parent;
    private readonly System.Func<T, bool> destroyed;
    private readonly System.Collections.Generic.Dictionary<int, Entry> entries = new System.Collections.Generic.Dictionary<int, Entry>();
    private readonly System.Collections.Generic.List<T> order = new System.Collections.Generic.List<T>();
    public System.Collections.Generic.IReadOnlyList<T> DestructionOrder => order.AsReadOnly();
    public Pr5OwnedCleanupGuard(T root, System.Func<T, int> uid,
        System.Func<T, System.Collections.Generic.IEnumerable<T>> children,
        System.Func<T, object> parent, System.Func<T, bool> destroyed, object placementParent)
    {
        this.root = root;
        this.uid = uid;
        this.children = children;
        this.parent = parent;
        this.destroyed = destroyed;
        this.placementParent = placementParent;
        Capture(root);
        RequireSafe();
    }
    private void Capture(T node)
    {
        if (node == null || uid(node) <= 0 || entries.ContainsKey(uid(node)))
            throw new System.InvalidOperationException("Invalid/cyclic/duplicate generation-time fixture UID.");
        entries.Add(uid(node), new Entry { Node = node, Uid = uid(node), Parent = parent(node) });
        foreach (var child in children(node)) Capture(child);
        order.Add(node);
    }
    public bool OwnsReference(T node)
    {
        foreach (var entry in entries.Values) if (object.ReferenceEquals(entry.Node, node)) return true;
        return false;
    }
    public void RequireSafe()
    {
        foreach (var entry in entries.Values)
        {
            if (uid(entry.Node) != entry.Uid)
                throw new System.InvalidOperationException("Captured fixture UID changed: " + entry.Uid);
            if (destroyed(entry.Node)) continue;
            object now = parent(entry.Node);
            bool permitted = object.ReferenceEquals(now, entry.Parent);
            if (object.ReferenceEquals(entry.Node, root)) permitted |= object.ReferenceEquals(now, placementParent);
            if (!permitted) throw new System.InvalidOperationException("Captured fixture parent/owner changed: " + entry.Uid);
        }
        var seen = new System.Collections.Generic.HashSet<int>();
        Inspect(root, seen);
        foreach (var entry in entries.Values)
            if (!destroyed(entry.Node) && !seen.Contains(entry.Uid))
                throw new System.InvalidOperationException("Live owned fixture escaped its captured tree: " + entry.Uid);
    }
    private void Inspect(T node, System.Collections.Generic.HashSet<int> seen)
    {
        Entry entry;
        if (node == null || !entries.TryGetValue(uid(node), out entry) || !object.ReferenceEquals(entry.Node, node))
            throw new System.InvalidOperationException("Unknown child or same-UID replacement in fixture tree.");
        if (!seen.Add(entry.Uid)) throw new System.InvalidOperationException("Cycle/duplicate child in fixture tree.");
        foreach (var child in children(node))
        {
            if (child == null || (!destroyed(child) && !object.ReferenceEquals(parent(child), node)))
                throw new System.InvalidOperationException("Fixture child has a foreign parent.");
            Inspect(child, seen);
        }
    }
    public void DestroyOwned(T node, System.Action destroy)
    {
        if (!OwnsReference(node)) throw new System.InvalidOperationException("Unknown destruction target.");
        RequireSafe();
        if (!destroyed(node)) destroy();
    }
    public void MutateOwned(T node, object observedOwner, object expectedOwner, System.Action mutation)
    {
        if (!OwnsReference(node) || !object.ReferenceEquals(observedOwner, expectedOwner))
            throw new System.InvalidOperationException("Mutation target/Condition owner is not the expected fixture.");
        RequireSafe();
        mutation();
    }
}
