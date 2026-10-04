#if RUNTIME_TEST
using System;
using System.Collections.Generic;
using System.Reflection;

// Restore the actual shared bindings and the unconsumed managed RNG, including native SetSeed replacements.
public sealed class Pr8NativeStateScope : IDisposable
{
    private sealed class Binding
    {
        public FieldInfo Field;
        public Stats Stat;
        public int[] Raw, Values;
        public int Index;
    }
    private readonly List<Binding> bindings = new List<Binding>();
    private readonly Chara originalCC;
    private readonly System.Random originalRandom;
    private readonly Pr8ManagedFields randomFields;
    private readonly int baseSeed, maxBytes;
    private readonly byte[] bytes, byteValues;
    private bool begun, disposed;
    public Pr8NativeStateScope()
    {
        originalCC = BaseStats.CC;
        foreach (FieldInfo field in typeof(Stats).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!typeof(Stats).IsAssignableFrom(field.FieldType)) continue;
            var stat = field.GetValue(null) as Stats;
            if (stat == null) throw new InvalidOperationException("Missing native shared Stats: " + field.Name);
            bindings.Add(new Binding { Field = field, Stat = stat, Raw = stat.raw, Index = stat.rawIndex,
                Values = stat.raw == null ? null : (int[])stat.raw.Clone() });
        }
        if (bindings.Count == 0) throw new InvalidOperationException("No native shared Stats bindings found.");
        originalRandom = Rand._random;
        if (originalRandom == null) throw new InvalidOperationException("Native Rand RNG missing.");
        randomFields = new Pr8ManagedFields(originalRandom);
        baseSeed = Rand.baseSeed; maxBytes = Rand.MaxBytes; bytes = Rand.bytes;
        byteValues = bytes == null ? null : (byte[])bytes.Clone();
    }
    public void Begin()
    {
        if (begun || disposed) throw new InvalidOperationException("Native state scope already started/disposed.");
        begun = true;
        Rand._random = new System.Random(807248);
        // A callback must not mutate the original shared byte buffer either.
        Rand.bytes = bytes == null ? null : (byte[])bytes.Clone();
    }
    public void Dispose()
    {
        if (disposed) return;
        // Restore every binding before asserting; one damaged buffer must not prevent reference restoration.
        foreach (Binding b in bindings) { b.Field.SetValue(null, b.Stat); b.Stat.raw = b.Raw; b.Stat.rawIndex = b.Index; }
        BaseStats.CC = originalCC;
        Rand._random = originalRandom; Rand.baseSeed = baseSeed; Rand.MaxBytes = maxBytes; Rand.bytes = bytes;
        disposed = true;
        AssertRestored();
    }
    public void AssertRestored()
    {
        foreach (Binding b in bindings)
        {
            if (!ReferenceEquals(b.Field.GetValue(null), b.Stat) || !ReferenceEquals(b.Stat.raw, b.Raw) || b.Stat.rawIndex != b.Index)
                throw new InvalidOperationException("Native Stats binding not restored: " + b.Field.Name);
            if (b.Raw != null)
            {
                if (b.Raw.Length != b.Values.Length) throw new InvalidOperationException("Native original Stats buffer length changed.");
                for (int i = 0; i < b.Raw.Length; i++)
                    if (b.Raw[i] != b.Values[i]) throw new InvalidOperationException("Original Stats buffer changed: " + b.Field.Name + "[" + i + "]");
            }
        }
        if (!ReferenceEquals(BaseStats.CC, originalCC) || !ReferenceEquals(Rand._random, originalRandom)
            || Rand.baseSeed != baseSeed || Rand.MaxBytes != maxBytes || !ReferenceEquals(Rand.bytes, bytes))
            throw new InvalidOperationException("Native CC/Rand reference or seed settings not restored.");
        if (bytes != null)
            for (int i = 0; i < bytes.Length; i++)
                if (bytes[i] != byteValues[i]) throw new InvalidOperationException("Original Rand byte buffer changed.");
        randomFields.AssertUnchanged();
    }
}

// Capture Random's managed fields without taking a sample, reseeding it, or constructing a clone.
public sealed class Pr8ManagedFields
{
    private readonly List<Action> assertions = new List<Action>();
    private readonly HashSet<object> visited = new HashSet<object>(Pr8ReferenceComparer<object>.Instance);
    public Pr8ManagedFields(object root) { Capture(root); }
    private void Capture(object value)
    {
        if (value == null || !visited.Add(value)) return;
        var array = value as Array;
        if (array != null)
        {
            for (int i = 0; i < array.Length; i++)
            {
                int index = i; object before = array.GetValue(i);
                assertions.Add(() => Check(before, array.GetValue(index), "Random array[" + index + "]"));
                if (before != null && !before.GetType().IsValueType && !(before is string)) Capture(before);
            }
            return;
        }
        for (Type type = value.GetType(); type != null; type = type.BaseType)
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                object before = field.GetValue(value);
                assertions.Add(() => Check(before, field.GetValue(value), "Random." + field.Name));
                if (before != null && !before.GetType().IsValueType && !(before is string)) Capture(before);
            }
    }
    private static void Check(object before, object after, string name)
    {
        bool equal = before == null ? after == null : before.GetType().IsValueType || before is string
            ? before.Equals(after) : ReferenceEquals(before, after);
        if (!equal) throw new InvalidOperationException("Original managed RNG consumed/changed: " + name);
    }
    public void AssertUnchanged() { foreach (Action check in assertions) check(); }
}
#endif
