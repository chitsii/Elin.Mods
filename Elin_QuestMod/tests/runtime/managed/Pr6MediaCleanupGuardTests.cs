public static class Pr6MediaCleanupGuardTests
{
    private sealed class EqualValue
    {
        public override bool Equals(object other) { return other is EqualValue; }
        public override int GetHashCode() { return 1; }
    }

    private static int Passed;
    public static int Main()
    {
        try
        {
            Check("no audio coverage needs no manager data or playback", () =>
            {
                Pr6AudioPrerequisites.RequireEnvironment(Pr6AudioCoverage.None, false, false, true, true);
            });
            Check("dispatch permits ignoreSounds and paused listener without claiming playback", () =>
            {
                Pr6AudioPrerequisites.RequireEnvironment(Pr6AudioCoverage.Dispatch, true, true, true, true);
            });
            Check("native sound coverage still needs manager and requested data", () =>
            {
                foreach (var coverage in new[] { Pr6AudioCoverage.Dispatch, Pr6AudioCoverage.Playback })
                {
                    RejectAudio(coverage, false, true, false, false);
                    RejectAudio(coverage, true, false, false, false);
                }
            });
            Check("playback rejects ignored or paused engine audio", () =>
            {
                RejectAudio(Pr6AudioCoverage.Playback, true, true, true, false);
                RejectAudio(Pr6AudioCoverage.Playback, true, true, false, true);
                RejectAudio(Pr6AudioCoverage.Playback, true, true, true, true);
            });
            Check("playback permits running channel observation", () =>
            {
                Pr6AudioPrerequisites.RequireEnvironment(Pr6AudioCoverage.Playback, true, true, false, false);
            });
            Check("unknown audio coverage cannot silently pass", () =>
            {
                RejectAudio((Pr6AudioCoverage)99, true, true, false, false);
            });
            Check("paused live channel stays protected while inactive pool remains available", () =>
            {
                Require(Pr6AudioPrerequisites.ProtectBaselineChannel(false, true, true, true), "Paused live channel lost baseline protection.");
                Require(Pr6AudioPrerequisites.ProtectBaselineChannel(true, false, true, true), "Playing baseline channel lost protection.");
                Require(!Pr6AudioPrerequisites.ProtectBaselineChannel(false, true, false, true), "Inactive pooled channel incorrectly protected.");
                Require(!Pr6AudioPrerequisites.ProtectBaselineChannel(false, true, true, false), "Clipless paused channel incorrectly protected.");
            });
            Check("initial activation remains owned", () =>
            {
                var item = new object(); var leases = new Pr6ActivationOwnership<object>();
                leases.CaptureCreated(item); leases.ObserveActivation(item, true);
                int calls = 0;
                Require(leases.TryCleanup(item, _ => calls++) && calls == 1, "Initial native activation cannot be cleaned.");
            });
            Check("alternate Play(Vector3) generation prevents cleanup", () =>
            {
                var item = new object(); var leases = Claimed(item);
                leases.ObserveActivation(item, false);
                int calls = 0;
                Require(!leases.TryCleanup(item, _ => calls++) && calls == 0, "Same-reference alternate activation was destroyed.");
            });
            Check("callback activation inside expected window loses ownership", () =>
            {
                var item = new object(); var leases = Claimed(item);
                leases.ObserveActivation(item, true);
                int calls = 0;
                Require(!leases.TryCleanup(item, _ => calls++) && calls == 0, "Callback replay was adopted as the original activation.");
            });
            Check("foreign first activation cannot fulfill pending claim", () =>
            {
                var item = new object(); var leases = new Pr6ActivationOwnership<object>();
                leases.CaptureCreated(item); leases.ObserveActivation(item, false);
                leases.ObserveActivation(item, true);
                Require(!leases.TryCleanup(item, _ => { throw new System.Exception("Destroyed foreign activation"); }), "Foreign first activation became owned.");
            });
            Check("queued cleanup checks current generation at execution", () =>
            {
                var item = new object(); var leases = Claimed(item); int calls = 0;
                System.Action queued = () => Require(!leases.TryCleanup(item, _ => calls++), "Queued cleanup used stale ownership.");
                leases.ObserveActivation(item, false); queued();
                Require(calls == 0, "Queued callback destroyed reused item.");
            });
            Check("reference identity wins over value equality", () =>
            {
                var one = new EqualValue(); var two = new EqualValue();
                var leases = new Pr6ActivationOwnership<EqualValue>();
                leases.CaptureCreated(one); leases.ObserveActivation(one, true);
                leases.ObserveActivation(two, false);
                int calls = 0;
                Require(leases.TryCleanup(one, _ => calls++) && calls == 1, "Equal-valued distinct object changed ownership.");
                Require(!leases.TryCleanup(two, _ => calls++), "Observed but unclaimed object became owned.");
            });
            Check("lost lease does not suppress independent cleanup", () =>
            {
                var one = new object(); var two = new object(); var leases = Claimed(one);
                leases.CaptureCreated(two); leases.ObserveActivation(two, true); leases.ObserveActivation(one, false);
                int calls = 0;
                Require(!leases.TryCleanup(one, _ => calls++), "Lost item accepted.");
                Require(leases.TryCleanup(two, _ => calls++) && calls == 1, "Independent owned item was suppressed.");
            });
            Check("duplicate claim cannot reset generation evidence", () =>
            {
                var item = new object(); var leases = Claimed(item); leases.ObserveActivation(item, false);
                bool rejected = false;
                try { leases.CaptureCreated(item); } catch (System.InvalidOperationException) { rejected = true; }
                Require(rejected && !leases.TryCleanup(item, _ => { }), "Reclaim erased reuse evidence.");
            });
            Check("same live flags restore only scoped keys", () =>
            {
                var live = Flags("qmod.original", 9, "outside", 8);
                var baseline = Flags("qmod.original", 1, "questmod.legacy", 2);
                live["qmod.new"] = 3;
                Pr6DialogFlagRestore.Restore(live, live, baseline, "qmod.");
                Require(live.Count == 3 && live["qmod.original"] == 1 && live["questmod.legacy"] == 2 && live["outside"] == 8, "Scoped flag restore mismatch.");
            });
            Check("swapped live dictionary rejects before either dictionary changes", () =>
            {
                var original = Flags("qmod.original", 9, "outside", 8);
                var replacement = Flags("qmod.original", 7, "qmod.new", 6);
                bool rejected = false;
                try { Pr6DialogFlagRestore.Restore(original, replacement, Flags("qmod.original", 1), "qmod."); }
                catch (System.InvalidOperationException) { rejected = true; }
                Require(rejected && original.Count == 2 && original["qmod.original"] == 9 && original["outside"] == 8 && replacement.Count == 2 && replacement["qmod.original"] == 7 && replacement["qmod.new"] == 6, "Dictionary swap changed flags.");
            });
            Check("missing live dictionary rejects before mutation", () =>
            {
                var original = Flags("qmod.original", 9); bool rejected = false;
                try { Pr6DialogFlagRestore.Restore(original, null, Flags("qmod.original", 1), "qmod."); }
                catch (System.InvalidOperationException) { rejected = true; }
                Require(rejected && original.Count == 1 && original["qmod.original"] == 9, "Missing-live guard changed original flags.");
            });
            System.Console.WriteLine("passed=" + Passed + "; game_types_created=false");
            return 0;
        }
        catch (System.Exception ex) { System.Console.Error.WriteLine(ex); return 1; }
    }
    private static Pr6ActivationOwnership<object> Claimed(object instance)
    {
        var leases = new Pr6ActivationOwnership<object>();
        leases.CaptureCreated(instance); leases.ObserveActivation(instance, true); return leases;
    }
    private static void RejectAudio(Pr6AudioCoverage coverage, bool manager, bool data, bool ignored, bool paused)
    {
        bool rejected = false;
        try { Pr6AudioPrerequisites.RequireEnvironment(coverage, manager, data, ignored, paused); }
        catch (System.InvalidOperationException) { rejected = true; }
        Require(rejected, "Audio prerequisite silently accepted an unsupported environment.");
    }
    private static System.Collections.Generic.Dictionary<string, int> Flags(params object[] pairs)
    {
        var flags = new System.Collections.Generic.Dictionary<string, int>();
        for (int i = 0; i < pairs.Length; i += 2) flags[(string)pairs[i]] = (int)pairs[i + 1];
        return flags;
    }
    private static void Check(string name, System.Action test)
    {
        test(); Passed++; System.Console.WriteLine("PASS " + name);
    }
    private static void Require(bool value, string message)
    {
        if (!value) throw new System.InvalidOperationException(message);
    }
}
