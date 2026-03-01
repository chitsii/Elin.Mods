using System.Collections.Generic;

public abstract class RuntimeCaseBase : IRuntimeCase
{
    private static readonly IReadOnlyList<string> EmptyTags = new string[0];

    public abstract string Id { get; }

    public virtual IReadOnlyList<string> Tags => EmptyTags;

    public abstract void Prepare(RuntimeTestContext ctx);

    public abstract void Execute(RuntimeTestContext ctx);

    public abstract void Verify(RuntimeTestContext ctx);

    public virtual void Cleanup(RuntimeTestContext ctx)
    {
    }
}
