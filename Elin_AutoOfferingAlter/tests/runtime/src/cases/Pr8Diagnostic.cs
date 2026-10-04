#if RUNTIME_TEST
using System;

public static class Pr8Diagnostic
{
    // The complete diagnostic, including deferred snapshot argument evaluation, must run inside this guard.
    // A diagnostic exception must never replace gameplay errors or prevent mandatory rollback.
    public static void BestEffort(Action write)
    {
        try { write(); }
        catch (Exception) { }
    }
}
#endif
