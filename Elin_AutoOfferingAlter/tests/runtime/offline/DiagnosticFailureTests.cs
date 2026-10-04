using System;
using System.IO;
using System.Runtime.Serialization;

internal static class DiagnosticFailureTests
{
    private sealed class BrokenJson
    {
        public string Snapshot { get { throw new InvalidOperationException("diagnostic JSON getter fault"); } }
    }

    internal static void Run()
    {
        foreach (string fault in new[] { "snapshot", "JSON", "file", "log" })
        {
            int probes = 0;
            Action probe = () =>
            {
                probes++;
                if (fault == "snapshot") throw new InvalidOperationException("diagnostic snapshot fault");
                if (fault == "JSON") Newtonsoft.Json.JsonConvert.SerializeObject(new BrokenJson());
                if (fault == "file") throw new IOException("diagnostic file writer fault");
                if (fault == "log") throw new InvalidOperationException("diagnostic log writer fault");
                throw new InvalidOperationException("Fault injection did not throw.");
            };
            var nativeError = new InvalidOperationException("original native creation fault");
            Exception observed = CreationError(Pr8Diagnostic.BestEffort, probe, nativeError);
            Require(ReferenceEquals(observed, nativeError) && probes == 1, "Diagnostic replaced native exception or was not evaluated.");
            observed = CreationError(action => action(), probe, nativeError);
            Require(!ReferenceEquals(observed, nativeError) && probes == 2, "Unguarded native exception replacement counterexample did not fail.");
            Console.WriteLine("PASS: " + fault + " diagnostic preserves identical creation exception; unguarded replacement rejected");

            CleanupFault(Pr8Diagnostic.BestEffort, probe, true);
            CleanupFault(action => action(), probe, false);
            Require(probes == 4, "Cleanup fault probes were not evaluated.");
            Console.WriteLine("PASS: " + fault + " cleanup diagnostic preserves observer/Player/native Stats/CC/Rand restoration; unguarded skip rejected");
        }
    }

    private static Exception CreationError(Action<Action> write, Action probe, Exception original)
    {
        try
        {
            try { throw original; }
            catch { write(probe); throw; }
        }
        catch (Exception observed) { return observed; }
    }

    private static void CleanupFault(Action<Action> write, Action probe, bool expectedRestoration)
    {
        object original = new object(), isolated = new object(), current = original;
        var player = new Pr8PlayerScope<object>(() => current, value => current = value);
        var native = new Pr8NativeStateScope();
        bool observerRemoved = false, completed = false;
        try
        {
            player.Select(isolated);
            native.Begin();
            // Native data bodies only, with an opaque identity marker; no Chara constructor or gameplay code.
            var owned = (Chara)FormatterServices.GetUninitializedObject(typeof(Chara));
            Stats.Depression.Set(new[] { 13, 17 }, 1, owned);
            Rand.rnd(999);
            try
            {
                // Match the formerly vulnerable diagnostic before mandatory cleanup try/finally.
                write(probe);
                try { observerRemoved = true; }
                finally { player.Dispose(); native.Dispose(); }
                completed = true;
            }
            catch (Exception)
            {
                if (expectedRestoration) throw;
            }
            if (expectedRestoration)
            {
                Require(completed && observerRemoved && ReferenceEquals(current, original), "Mandatory cleanup was skipped.");
                native.AssertRestored();
            }
            else
            {
                Require(!completed && !observerRemoved && ReferenceEquals(current, isolated), "Unguarded restoration-skip counterexample did not fail.");
                bool nativeUnrestored = false;
                try { native.AssertRestored(); }
                catch (InvalidOperationException) { nativeUnrestored = true; }
                Require(nativeUnrestored, "Counterexample concealed unrestored native state.");
            }
        }
        finally
        {
            // Reclaim the intentional counterexample contamination after checking it; never count this as fixture success.
            player.Dispose(); native.Dispose();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
