using System;

internal static class CreationOrderTests
{
    private sealed class Actor { public bool Created, Renderer; }
    internal static void Run()
    {
        foreach (string fault in new[] { "none", "create", "incomplete" })
        {
            object originalPlayer = new object(), isolatedPlayer = new object(), selectedPlayer = originalPlayer;
            var normalPc = new Actor { Created = true, Renderer = true };
            var actor = new Actor();
            Actor pc = normalPc;
            var playerScope = new Pr8PlayerScope<object>(() => selectedPlayer, value => selectedPlayer = value);
            Exception nativeError = new InvalidOperationException("native creation failure"), observed = null;
            int createCalls = 0;
            try
            {
                playerScope.Select(isolatedPlayer);
                try
                {
                    Pr8CreationOrder.CompleteBeforeSelection(() =>
                    {
                        createCalls++;
                        Require(ReferenceEquals(selectedPlayer, isolatedPlayer) && ReferenceEquals(pc, normalPc) && pc.Renderer,
                            "Creation changed data owner or selected an incomplete PC.");
                        if (fault == "create") throw nativeError;
                        // Native OnCreate/SetAI runs while this is a different actor; renderer is created afterward.
                        actor.Created = true;
                        if (fault != "incomplete") actor.Renderer = true;
                    }, () => actor.Created && actor.Renderer, () => pc = actor);
                }
                catch (Exception ex) { observed = ex; }
                Require(createCalls == 1, "Native creation was skipped or repeated.");
                if (fault == "none") Require(observed == null && ReferenceEquals(pc, actor), "Completed actor was not selected.");
                else Require(observed != null && ReferenceEquals(pc, normalPc), "Failed/incomplete actor became selected PC.");
                if (fault == "create") Require(ReferenceEquals(observed, nativeError), "Native creation exception was replaced.");
                Require(normalPc.Created && normalPc.Renderer, "Normal creation PC was mutated.");
            }
            finally { playerScope.Dispose(); }
            Require(ReferenceEquals(selectedPlayer, originalPlayer), "Player scope was not restored.");
            Console.WriteLine("PASS: native generation completes before PC selection; isolated Player restored (" + fault + ")");
        }
        var partial = new Actor();
        Actor prematurePc = partial;
        bool rejected = false;
        try
        {
            Pr8CreationOrder.CompleteBeforeSelection(() =>
                Require(!ReferenceEquals(prematurePc, partial) && prematurePc.Created && prematurePc.Renderer,
                    "Native generation was entered with the incomplete target as PC."),
                () => true, () => { throw new InvalidOperationException("Unexpected selection after failed creation gate."); });
        }
        catch (InvalidOperationException ex) { rejected = ex.Message.StartsWith("Native generation was entered", StringComparison.Ordinal); }
        Require(rejected, "Premature PC selection counterexample falsely passed.");
        Console.WriteLine("PASS: rejects premature PC selection into renderer-less actor");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
