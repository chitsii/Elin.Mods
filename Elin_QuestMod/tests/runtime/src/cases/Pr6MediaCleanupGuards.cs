// Pure managed guards shared by the native fixture and offline counterexamples.
public sealed class Pr6ActivationOwnership<T> where T : class
{
    private sealed class ReferenceComparer : System.Collections.Generic.IEqualityComparer<T>
    {
        public bool Equals(T one, T two) { return object.ReferenceEquals(one, two); }
        public int GetHashCode(T value) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value); }
    }
    private sealed class Claim
    {
        public long Generation;
        public bool AwaitingFirstActivation = true;
    }
    private readonly System.Collections.Generic.Dictionary<T, long> Generations =
        new System.Collections.Generic.Dictionary<T, long>(new ReferenceComparer());
    private readonly System.Collections.Generic.Dictionary<T, Claim> Claims =
        new System.Collections.Generic.Dictionary<T, Claim>(new ReferenceComparer());

    public void CaptureCreated(T instance)
    {
        if (instance == null) throw new System.ArgumentNullException("instance");
        if (Claims.ContainsKey(instance)) throw new System.InvalidOperationException("Activation reference already claimed.");
        Claims.Add(instance, new Claim { Generation = Generation(instance) });
    }
    public void ObserveActivation(T instance, bool expectedInvocation)
    {
        if (instance == null) throw new System.ArgumentNullException("instance");
        long generation = checked(Generation(instance) + 1);
        Generations[instance] = generation;
        Claim claim;
        if (Claims.TryGetValue(instance, out claim) && claim.AwaitingFirstActivation)
        {
            claim.AwaitingFirstActivation = false;
            if (expectedInvocation) claim.Generation = generation;
        }
    }
    public bool TryCleanup(T instance, System.Action<T> cleanup)
    {
        if (cleanup == null) throw new System.ArgumentNullException("cleanup");
        Claim claim;
        if (instance == null || !Claims.TryGetValue(instance, out claim) || claim.Generation != Generation(instance)) return false;
        cleanup(instance);
        return true;
    }
    private long Generation(T instance)
    {
        long generation;
        return Generations.TryGetValue(instance, out generation) ? generation : 0;
    }
}

public static class Pr6DialogFlagRestore
{
    public static void RequireLiveDictionary(
        System.Collections.Generic.Dictionary<string, int> original,
        System.Collections.Generic.Dictionary<string, int> live)
    {
        if (original == null || !object.ReferenceEquals(original, live))
            throw new System.InvalidOperationException("Live dialogFlags dictionary changed; refusing to overwrite flags.");
    }
    public static void Restore(
        System.Collections.Generic.Dictionary<string, int> original,
        System.Collections.Generic.Dictionary<string, int> live,
        System.Collections.Generic.Dictionary<string, int> baseline,
        string prefix)
    {
        RequireLiveDictionary(original, live);
        var keys = new System.Collections.Generic.List<string>();
        foreach (string key in original.Keys)
            if (key.StartsWith(prefix, System.StringComparison.Ordinal) || key.StartsWith("questmod.", System.StringComparison.Ordinal)) keys.Add(key);
        foreach (string key in keys) original.Remove(key);
        foreach (var flag in baseline) original[flag.Key] = flag.Value;
    }
}
