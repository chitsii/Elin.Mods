using System;
using System.Collections.Generic;

public static class Pr9FixtureDestructionGuard
{
    public static void Validate<T>(T root, Func<T, bool> owned, Func<T, IEnumerable<T>> children) where T : class
    {
        if (owned == null || children == null) throw new ArgumentNullException("fixture predicates");
        var pending = new Stack<T>();
        var visited = new HashSet<T>();
        pending.Push(root);
        while (pending.Count != 0) {
            var node = pending.Pop();
            if (node == null || !owned(node))
                throw new InvalidOperationException("Untracked fixture root/descendant; refusing destruction.");
            if (!visited.Add(node))
                throw new InvalidOperationException("Repeated fixture descendant; refusing recursive destruction.");
            foreach (var child in children(node)) pending.Push(child);
        }
    }

    public static void Destroy<T>(T root, Func<T, bool> owned, Func<T, IEnumerable<T>> children, Action<T> destroy) where T : class
    {
        if (destroy == null) throw new ArgumentNullException("destroy");
        Validate(root, owned, children);
        destroy(root);
    }
}
