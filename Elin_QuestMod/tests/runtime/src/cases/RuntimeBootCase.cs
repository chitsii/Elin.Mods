using System.Collections.Generic;

// Shared runtime framework connectivity smoke test.
public sealed class RuntimeBootCase : RuntimeCaseBase
{
    public override string Id => "runtime.boot.pc_available";

    public override IReadOnlyList<string> Tags => new[] { "smoke" };

    public override void Prepare(RuntimeTestContext ctx)
    {
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.pc != null, "PC is unavailable.");
        ctx.Log("RuntimeBootCase passed.");
    }
}
