using System.Collections.Generic;

// Runtime Test V2 contract.
public interface IRuntimeCase
{
    string Id { get; }
    IReadOnlyList<string> Tags { get; }
    void Prepare(RuntimeTestContext ctx);
    void Execute(RuntimeTestContext ctx);
    void Verify(RuntimeTestContext ctx);
    void Cleanup(RuntimeTestContext ctx);
}
