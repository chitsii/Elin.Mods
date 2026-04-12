using System.Collections;

// Additive contract for smoke/runtime cases that need frame-based waiting.
public interface IRuntimeCoroutineCase
{
    IEnumerator PrepareAsync(RuntimeTestContext ctx);
    IEnumerator ExecuteAsync(RuntimeTestContext ctx);
    IEnumerator VerifyAsync(RuntimeTestContext ctx);
    IEnumerator CleanupAsync(RuntimeTestContext ctx);
}
