using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class FixturePreservationTests
{
    private static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Expected actual managed DLL directory.");
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
        {
            string path = Path.Combine(args[0], new AssemblyName(request.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        Run(); return 0;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        SharedStateOwnershipTests.Run();
        DiagnosticFailureTests.Run();
        CreationOrderTests.Run();
        SourceSnapshotTests.Run();
        // Actual native codex data methods only; no Player/Game/Unity/card/zone objects are constructed.
        foreach (string failure in new[] { "none", "setup", "cleanup" })
        {
            var original = new CodexManager(); original.AddSpawn("putty");
            var isolated = new CodexManager();
            CodexManager current = original;
            string before = State(original);
            var scope = new Pr8PlayerScope<CodexManager>(() => current, value => current = value);
            bool injected = false;
            try
            {
                scope.Select(isolated);
                current.AddSpawn("putty"); current.AddSpawn("putty");
                if (failure == "setup") throw new InvalidOperationException("expected setup failure");
            }
            catch (InvalidOperationException ex)
            {
                Require(ex.Message == "expected setup failure", "Unexpected setup exception."); injected = true;
            }
            finally
            {
                try
                {
                    current.AddSpawn("cleanup callback");
                    if (failure == "cleanup") throw new InvalidOperationException("expected cleanup failure");
                }
                catch (InvalidOperationException ex)
                {
                    Require(ex.Message == "expected cleanup failure", "Unexpected cleanup exception."); injected = true;
                }
                finally { scope.Dispose(); }
            }
            Require(ReferenceEquals(current, original) && State(original) == before, "Original reference/codex changed.");
            Require(isolated.creatures["putty"].spawns == 2 && isolated.creatures["cleanup callback"].spawns == 1,
                "Native callback did not target isolated codex.");
            Require((failure == "none") != injected, "Fault injection did not run.");
            scope.Dispose();
            Console.WriteLine("PASS: callback isolation + finally restoration (" + failure + ")");
        }
        foreach (string badOrder in new[] { "spawn before selection", "cleanup after restoration" })
        {
            var original = new CodexManager(); original.AddSpawn("putty");
            string before = State(original);
            var isolated = new CodexManager();
            CodexManager current = original;
            var scope = new Pr8PlayerScope<CodexManager>(() => current, value => current = value);
            if (badOrder == "cleanup after restoration") { scope.Select(isolated); scope.Dispose(); }
            // The exact native Zone.AddCard -> CodexManager.AddSpawn data mutation
            // targets the currently selected data owner in both bad lifecycle orders.
            current.AddSpawn("putty"); current.AddSpawn("putty");
            if (badOrder == "spawn before selection") scope.Select(isolated);
            scope.Dispose();
            var changes = Pr8StateDiff.Describe(before, State(original));
            Require(State(original) != before && changes.Count == 1
                && changes[0].StartsWith("$.codex.creatures.putty._ints[4]:", StringComparison.Ordinal),
                "Native codex preservation counterexample falsely passed or wrong concrete field.");
            Console.WriteLine("PASS: rejects " + badOrder + "; " + changes[0]);
        }
        Require(Pr8StateDiff.Describe("{\"a\":1,\"b\":2}", "{\"b\":2,\"a\":1}").Count == 1,
            "Diagnostics must not hide raw serialization differences.");
        Console.WriteLine("PASS: diagnostics preserve raw equality failures; no field excluded or repaired");
    }
    private static string State(CodexManager codex)
    {
        return "{\"codex\":" + Newtonsoft.Json.JsonConvert.SerializeObject(codex) + "}";
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
