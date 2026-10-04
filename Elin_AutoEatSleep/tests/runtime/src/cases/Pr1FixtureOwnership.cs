// Test-only destruction gate; snapshots are recorded only at known creation boundaries.
public sealed class Pr1FixtureOwnership<T> where T : class
{
    private readonly System.Func<T, int> uid;
    private readonly System.Func<T, object> parent;
    private readonly System.Func<T, System.Collections.Generic.IEnumerable<T>> children;
    private readonly System.Collections.Generic.HashSet<int> originalUids;
    private readonly System.Collections.Generic.Dictionary<int, T> created = new System.Collections.Generic.Dictionary<int, T>();
    private readonly System.Collections.Generic.Dictionary<int, object> parents = new System.Collections.Generic.Dictionary<int, object>();
    public Pr1FixtureOwnership(System.Func<T, int> getUid, System.Func<T, object> getParent,
        System.Func<T, System.Collections.Generic.IEnumerable<T>> getChildren, System.Collections.Generic.IEnumerable<int> original)
    {
        uid = getUid; parent = getParent; children = getChildren;
        originalUids = new System.Collections.Generic.HashSet<int>(original);
    }
    public void RecordCreatedTree(T node)
    {
        int id = uid(node);
        if (id <= 0 || originalUids.Contains(id)) throw new System.InvalidOperationException("Original/invalid UID cannot become fixture: " + id);
        T known;
        if (created.TryGetValue(id, out known))
        {
            if (!object.ReferenceEquals(known, node) || !object.ReferenceEquals(parents[id], parent(node)))
                throw new System.InvalidOperationException("Fixture reference/owner changed during creation: " + id);
            return;
        }
        created.Add(id, node);
        parents.Add(id, parent(node));
        foreach (var child in children(node)) RecordCreatedTree(child);
    }
    public void RecordMove(T node, object destination)
    {
        RequireIdentity(node);
        if (!object.ReferenceEquals(parent(node), destination)) throw new System.InvalidOperationException("Native fixture move did not reach destination.");
        parents[uid(node)] = destination;
    }
    private void RequireIdentity(T node)
    {
        T known;
        if (!created.TryGetValue(uid(node), out known) || !object.ReferenceEquals(known, node))
            throw new System.InvalidOperationException("Unknown/replaced fixture UID: " + uid(node));
    }
    public void RequireTree(T node)
    {
        RequireTree(node, new System.Collections.Generic.HashSet<int>());
    }
    private void RequireTree(T node, System.Collections.Generic.HashSet<int> visited)
    {
        RequireIdentity(node);
        int id = uid(node);
        if (!visited.Add(id)) throw new System.InvalidOperationException("Fixture tree cycle/duplicate: " + id);
        if (!object.ReferenceEquals(parents[id], parent(node))) throw new System.InvalidOperationException("Fixture owner changed: " + id);
        foreach (var child in children(node)) RequireTree(child, visited);
    }
    public void DestroyChecked(T node, System.Action<T> destroy)
    {
        RequireTree(node);
        destroy(node);
    }
}
