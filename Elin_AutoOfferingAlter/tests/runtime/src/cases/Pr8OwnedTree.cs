#if RUNTIME_TEST
using System;
using System.Collections.Generic;

public sealed class Pr8ReferenceComparer<T> : IEqualityComparer<T> where T : class
{
    public static readonly Pr8ReferenceComparer<T> Instance = new Pr8ReferenceComparer<T>();
    public bool Equals(T x, T y) { return ReferenceEquals(x, y); }
    public int GetHashCode(T value) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value); }
}

// Ownership evidence is a fresh allocation observed BEFORE native creation callbacks, or a split from an owned source.
public sealed class Pr8OwnedTree<T> where T : class
{
    private readonly Func<T, int> uid;
    private readonly Func<T, IEnumerable<T>> children;
    private readonly int uidFloor;
    private readonly HashSet<T> originals = new HashSet<T>(Pr8ReferenceComparer<T>.Instance);
    private readonly HashSet<int> originalUids = new HashSet<int>();
    private readonly HashSet<T> owned = new HashSet<T>(Pr8ReferenceComparer<T>.Instance);
    private readonly HashSet<T> persisted = new HashSet<T>(Pr8ReferenceComparer<T>.Instance);
    public Pr8OwnedTree(int uidFloor, Func<T, int> uid, Func<T, IEnumerable<T>> children)
    { this.uidFloor = uidFloor; this.uid = uid; this.children = children; }
    public IEnumerable<T> Owned { get { return owned; } }
    public void CaptureOriginal(T item)
    {
        if (item == null || !originals.Add(item)) return;
        originalUids.Add(uid(item));
        foreach (T child in children(item)) CaptureOriginal(child);
    }
    public void ObserveAllocation(T item)
    {
        if (item == null || originals.Contains(item) || uid(item) != 0)
            throw new InvalidOperationException("Native Create attempted on an existing/unknown card; refusing ownership.");
        owned.Add(item);
    }
    public void ObserveSplit(T source, T result)
    {
        RequireOwned(source);
        if (result == null || originals.Contains(result) || originalUids.Contains(uid(result)) || uid(result) < uidFloor)
            throw new InvalidOperationException("Split returned an original/unknown card; refusing ownership.");
        owned.Add(result);
    }
    // Caller must validate the exact persisted UID/source/name/owner manifest before authorizing these nodes.
    public void AuthorizePersisted(T item)
    {
        if (item == null || uid(item) <= 0) throw new InvalidOperationException("Invalid persisted fixture UID.");
        originals.Remove(item); originalUids.Remove(uid(item)); persisted.Add(item); owned.Add(item);
    }
    public void RequireOwned(T item)
    {
        if (item == null || !owned.Contains(item) || originals.Contains(item) || originalUids.Contains(uid(item))
            || (!persisted.Contains(item) && uid(item) < uidFloor))
            throw new InvalidOperationException("Foreign/unobserved/original UID in fixture; preserve it and reload baseline.");
    }
    public void GuardTree(T root)
    {
        GuardTree(root, new HashSet<T>(Pr8ReferenceComparer<T>.Instance));
    }
    private void GuardTree(T root, HashSet<T> visiting)
    {
        RequireOwned(root);
        if (!visiting.Add(root)) throw new InvalidOperationException("Cyclic fixture ownership tree.");
        foreach (T child in children(root)) GuardTree(child, visiting);
        visiting.Remove(root);
    }
}
#endif
