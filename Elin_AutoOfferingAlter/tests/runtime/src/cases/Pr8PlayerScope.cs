#if RUNTIME_TEST
using System;

// A reference swap only. Native callbacks must run before Dispose, including failure cleanup.
public sealed class Pr8PlayerScope<T> : IDisposable where T : class
{
    private readonly Func<T> read;
    private readonly Action<T> write;
    private readonly T original;
    private bool disposed;
    public Pr8PlayerScope(Func<T> read, Action<T> write)
    {
        this.read = read; this.write = write; original = read();
    }
    public void Select(T isolated)
    {
        if (disposed || isolated == null || ReferenceEquals(isolated, original))
            throw new InvalidOperationException("Invalid isolated Player selection.");
        write(isolated);
        if (!ReferenceEquals(read(), isolated)) throw new InvalidOperationException("Player selection failed.");
    }
    public void Dispose()
    {
        if (disposed) return;
        write(original);
        if (!ReferenceEquals(read(), original)) throw new InvalidOperationException("Original Player restoration failed.");
        disposed = true;
    }
}
#endif
