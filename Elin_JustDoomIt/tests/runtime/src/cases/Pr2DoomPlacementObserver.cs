#if PR2_RUNTIME_TEST
// Pass-through observation is separate from explicitly tagged fault injection.
public static class Pr2DoomPlacementObserver
{
    public static Pr2DoomPlacementFixture Fixture;
    public static bool Busy, AddedOnValidTile;
    public static int ActivateCalls, CreateAttempts, CreateCalls, AddCalls;
    public static string GeneratedId, GeneratedTrait;
    public static readonly System.Reflection.MethodInfo ActivateMethod = HarmonyLib.AccessTools.Method(typeof(Zone), "Activate", System.Type.EmptyTypes);
    public static readonly System.Reflection.MethodInfo CreateMethod = HarmonyLib.AccessTools.Method(typeof(ThingGen), "Create", new[] { typeof(string), typeof(int), typeof(int) });
    public static readonly System.Reflection.MethodInfo AddMethod = HarmonyLib.AccessTools.Method(typeof(Zone), "AddCard", new[] { typeof(Card), typeof(Point) });

    public static bool InProduct(string method)
    {
        foreach (var frame in new System.Diagnostics.StackTrace().GetFrames())
        {
            var m = frame.GetMethod();
            if (m != null && m.DeclaringType == typeof(Elin_JustDoomIt.Patch_Zone_Activate_CasinoPlacement) && m.Name == method) return true;
        }
        return false;
    }

    public static void Begin()
    {
        RuntimeAssertions.Require(!Busy, "Concurrent PR2 activation rejected.");
        ActivateCalls = CreateAttempts = CreateCalls = AddCalls = 0;
        GeneratedId = GeneratedTrait = null;
        AddedOnValidTile = false;
        Busy = true;
    }
    public static void End() { Busy = false; }

    public static void ActivatePostfix(Zone __instance)
    {
        if (Busy && __instance == Fixture.Zone) ActivateCalls++;
    }
    public static void CreatePrefix(out bool __state)
    {
        __state = Busy && InProduct("CreateValidatedArcadeThing");
        if (__state) CreateAttempts++;
    }
    public static void CreatePostfix(Thing __result, bool __state)
    {
        if (!__state) return;
        CreateCalls++;
        Fixture.Track(__result);
        GeneratedId = __result == null ? null : __result.id;
        GeneratedTrait = __result == null || __result.trait == null ? null : __result.trait.GetType().FullName;
    }
    public static void AddPrefix(Zone __instance, Card __0, Point __1)
    {
        if (!Busy || __instance != Fixture.Zone || !InProduct("Postfix")) return;
        AddCalls++;
        AddedOnValidTile = Pr2DoomPlacementFixture.Valid(__1);
    }

    public static void Install(RuntimeTestContext ctx, Pr2DoomPlacementFixture f)
    {
        RuntimeAssertions.Require(Fixture == null && !Busy, "PR2 observer already active.");
        RuntimeAssertions.Require(ActivateMethod != null && CreateMethod != null && AddMethod != null, "Native targets unavailable.");
        var info = HarmonyLib.Harmony.GetPatchInfo(ActivateMethod);
        bool product = false;
        if (info != null) foreach (var p in info.Postfixes)
            if (p.owner == "chitsii.elin_justdoomit" && p.PatchMethod.DeclaringType == typeof(Elin_JustDoomIt.Patch_Zone_Activate_CasinoPlacement)) product = true;
        RuntimeAssertions.Require(product, "Real product Zone.Activate postfix is not installed.");
        var harmony = new HarmonyLib.Harmony("runtime_test.pr2.observe." + System.Guid.NewGuid().ToString("N"));
        ctx.RegisterRollback("pr2.observer_unpatch", () => {
            try {
                harmony.UnpatchSelf();
                foreach (var target in new[] { ActivateMethod, CreateMethod, AddMethod }) {
                    var patches = HarmonyLib.Harmony.GetPatchInfo(target);
                    RuntimeAssertions.Require(patches == null || !patches.Owners.Contains(harmony.Id), "PR2 observer owner remains.");
                }
            } finally { Fixture = null; Busy = false; }
            ctx.Log("cleanup:observer_unpatched=" + harmony.Id);
        });
        Fixture = f;
        harmony.Patch(ActivateMethod, postfix: new HarmonyLib.HarmonyMethod(typeof(Pr2DoomPlacementObserver), "ActivatePostfix") { priority = HarmonyLib.Priority.Last });
        harmony.Patch(CreateMethod,
            prefix: new HarmonyLib.HarmonyMethod(typeof(Pr2DoomPlacementObserver), "CreatePrefix") { priority = HarmonyLib.Priority.First },
            postfix: new HarmonyLib.HarmonyMethod(typeof(Pr2DoomPlacementObserver), "CreatePostfix") { priority = HarmonyLib.Priority.Last });
        harmony.Patch(AddMethod, prefix: new HarmonyLib.HarmonyMethod(typeof(Pr2DoomPlacementObserver), "AddPrefix"));
        ctx.Log("observer_owner=" + harmony.Id + "; coverage=native_activation_and_creation; DOOM_gameplay=not_exercised");
    }
}
public static class Pr2DoomPlacementFaults
{
    public static string Mode;
    public static int Injections;
    public static void CreatePrefix(ref string __0)
    {
        if (!Pr2DoomPlacementObserver.Busy || !Pr2DoomPlacementObserver.InProduct("CreateValidatedArcadeThing")) return;
        if (Mode == "wrong_id") { __0 = "money"; Injections++; }
        if (Mode == "fallback") { __0 = "RUNTIME_TEST_PR2_UNREGISTERED"; Injections++; }
        if (Mode == "exception") { Injections++; throw new System.InvalidOperationException("PR2 fixture-only factory failure"); }
    }
    public static void CreatePostfix(Thing __result)
    {
        if (Mode != "wrong_trait" || __result == null || !Pr2DoomPlacementObserver.Busy ||
            !Pr2DoomPlacementObserver.InProduct("CreateValidatedArcadeThing")) return;
        __result.trait = new Trait();
        __result.trait.SetOwner(__result);
        Injections++;
    }
    public static void NearestPostfix(Point __instance, ref Point __result)
    {
        if (Mode != "invalid_tile" || !Pr2DoomPlacementObserver.Busy ||
            !Pr2DoomPlacementObserver.InProduct("FindPlacementPoint")) return;
        __result = new Point(-1, -1);
        Injections++;
    }
    public static System.Action Install(RuntimeTestContext ctx, string mode)
    {
        RuntimeAssertions.Require(Mode == null, "Fault injector already active.");
        var harmony = new HarmonyLib.Harmony("runtime_test.pr2.fault." + System.Guid.NewGuid().ToString("N"));
        var nearest = HarmonyLib.AccessTools.Method(typeof(Point), "GetNearestPoint", new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(int) });
        System.Action restore = () => {
            try {
                harmony.UnpatchSelf();
                foreach (var target in new[] { Pr2DoomPlacementObserver.CreateMethod, nearest }) {
                    var patches = HarmonyLib.Harmony.GetPatchInfo(target);
                    RuntimeAssertions.Require(patches == null || !patches.Owners.Contains(harmony.Id), "Fault owner remains.");
                }
            } finally { Mode = null; }
        };
        ctx.RegisterRollback("pr2.fault_unpatch", restore);
        Mode = mode; Injections = 0;
        harmony.Patch(Pr2DoomPlacementObserver.CreateMethod,
            prefix: new HarmonyLib.HarmonyMethod(typeof(Pr2DoomPlacementFaults), "CreatePrefix"),
            postfix: new HarmonyLib.HarmonyMethod(typeof(Pr2DoomPlacementFaults), "CreatePostfix") { priority = HarmonyLib.Priority.First });
        RuntimeAssertions.Require(nearest != null, "Native nearest-point API missing.");
        harmony.Patch(nearest, postfix: new HarmonyLib.HarmonyMethod(typeof(Pr2DoomPlacementFaults), "NearestPostfix"));
        ctx.Log("coverage=fault_injection; mode=" + mode + "; owner=" + harmony.Id);
        return restore;
    }
}
#endif
