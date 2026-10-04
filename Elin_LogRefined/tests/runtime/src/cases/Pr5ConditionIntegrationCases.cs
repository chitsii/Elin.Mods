// Test-only observers run on native calls; no product patch is invoked manually.
public abstract class Pr5LogCaseBase : RuntimeCaseBase
{
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr5" };
    public override void Prepare(RuntimeTestContext ctx) { Pr5LogFixture.Start(ctx); }
    public override void Verify(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr5LogFixture>("pr5.fixture");
        RuntimeAssertions.Require(f.Completed, "Native outcome assertions did not complete.");
        f.RequireScopeEmpty();
    }
}

public sealed class Pr5ConditionOutcomesCase : Pr5LogCaseBase
{
    public override string Id => "pr5.log.condition_outcomes";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr5LogFixture>("pr5.fixture");
        var a = f.Spawn("wet");
        var wet = f.Create("ConWet", 100);
        f.SetThrottle(false);
        f.Begin("new", a, wet);
        var result = a.AddCondition(wet, true);
        f.Expect(a, wet, result, wet, 0, 1, false);
        RuntimeAssertions.Require(a.conditions.Contains(wet) && wet.owner == a && wet.value > 0 && a.isWet,
            "New native Wet/refresh state missing.");

        var attempted = f.Create("ConWet", 100);
        RuntimeAssertions.Require(wet.CanStack(attempted) && !wet.ShouldOverride(attempted)
            && !wet.AllowMultipleInstance, "Wet is not the required native null-return stack fixture.");
        int before = wet.value;
        int expected = before + wet.EvaluateTurn(attempted.power);
        if (wet.MaxDuration > 0) expected = System.Math.Min(expected, wet.MaxDuration);
        f.Begin("stack", a, attempted);
        result = a.AddCondition(attempted, true);
        f.Expect(a, attempted, result, null, 1, 1, false);
        RuntimeAssertions.Require(a.conditions.Contains(wet) && !a.conditions.Contains(attempted)
            && wet.owner == a && wet.value == expected && a.isWet,
            "Native stack did not update the existing Condition/refresh state.");

        var reject = f.Spawn("reject");
        attempted = f.Create("ConWet", 0);
        f.SetThrottle(true);
        f.Begin("zero_power_rejected", reject, attempted);
        result = reject.AddCondition(attempted, false);
        f.Expect(reject, attempted, result, null, 0, 0, false);
        RuntimeAssertions.Require(!reject.conditions.Contains(attempted), "Zero-power rejection added a Condition.");
        // A rejection must not consume the first genuine success's throttle window.
        wet = f.Create("ConWet", 100);
        f.Begin("success_after_rejection", reject, wet);
        result = reject.AddCondition(wet, true);
        f.Expect(reject, wet, result, wet, 0, 1, true);

        var resistant = f.Spawn("resist");
        resistant.elements.SetBase(950, 10000);
        RuntimeAssertions.Require(resistant.ResistLv(950) >= 3, "Native burning resistance not established.");
        attempted = f.Create("ConBurning", 100);
        f.Begin("resisted", resistant, attempted);
        result = resistant.AddCondition(attempted, false);
        f.Expect(resistant, attempted, result, null, 0, 0, false);
        RuntimeAssertions.Require(!resistant.conditions.Contains(attempted), "Resisted Burning was applied.");

        var nullified = f.Spawn("nullify");
        wet = f.Create("ConWet", 100);
        RuntimeAssertions.Require(System.Array.IndexOf(wet.source.nullify, "ConBurning") >= 0
            && nullified.ResistLv(950) < 3, "Native Wet->Burning nullify fixture unavailable.");
        f.SetThrottle(false);
        f.Begin("nullifier_seed", nullified, wet);
        result = nullified.AddCondition(wet, true);
        f.Expect(nullified, wet, result, wet, 0, 1, false);
        f.SetThrottle(true);
        attempted = f.Create("ConBurning", 100);
        f.Begin("nullified", nullified, attempted);
        result = nullified.AddCondition(attempted, true);
        f.Expect(nullified, attempted, result, null, 0, 0, false);
        RuntimeAssertions.Require(nullified.conditions.Contains(wet) && !nullified.conditions.Contains(attempted)
            && f.NullifyCalls == 1, "Native TryNullify refusal not observed.");

        var stanceOwner = f.Spawn("stance");
        var stance = f.Create("StanceIai", 100);
        RuntimeAssertions.Require(stance.Type == ConditionType.Stance, "Native StanceIai source is not Stance.");
        f.SetThrottle(false);
        f.Begin("stance_seed", stanceOwner, stance);
        result = stanceOwner.AddCondition(stance, true);
        f.Expect(stanceOwner, stance, result, stance, 0, 1, false);
        f.SetThrottle(true);
        attempted = f.Create("StanceIai", 100);
        f.Begin("stance_toggle_off", stanceOwner, attempted);
        result = stanceOwner.AddCondition(attempted, true);
        f.Expect(stanceOwner, attempted, result, null, 0, 0, false);
        RuntimeAssertions.Require(!stanceOwner.conditions.Contains(stance)
            && !stanceOwner.conditions.Contains(attempted) && f.KillCalls == 1,
            "Native stance removal/Kill was not observed.");

        var overrideOwner = f.Spawn("override");
        var dark = f.Create("ConDark", 1000);
        f.SetThrottle(false);
        f.Begin("override_seed", overrideOwner, dark);
        result = overrideOwner.AddCondition(dark, true);
        f.Expect(overrideOwner, dark, result, dark, 0, 1, false);
        attempted = f.Create("ConDark", 1);
        RuntimeAssertions.Require(dark.CanStack(attempted) && dark.ShouldOverride(attempted)
            && !dark.IsOverrideConditionMet(attempted, attempted.EvaluateTurn(attempted.power)),
            "Native override-not-met fixture unavailable.");
        before = dark.value;
        f.SetThrottle(true);
        f.Begin("override_not_met", overrideOwner, attempted);
        result = overrideOwner.AddCondition(attempted, true);
        f.Expect(overrideOwner, attempted, result, null, 0, 0, false);
        RuntimeAssertions.Require(overrideOwner.conditions.Contains(dark) && dark.value == before && f.OverrideCalls == 1,
            "Rejected override changed the existing Condition.");
        f.Completed = true;
    }
}

public sealed class Pr5ConditionCappedStackCase : Pr5LogCaseBase
{
    public override string Id => "pr5.log.capped_native_stack";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr5LogFixture>("pr5.fixture");
        Condition existing = null;
        string selected = null;
        foreach (var alias in new[] { "ConWet", "ConPoison", "ConBleed", "ConBurning", "ConFear", "ConConfuse", "ConSleep", "ConDrunk" })
        {
            if (!EClass.sources.stats.alias.ContainsKey(alias)) continue;
            var candidate = f.Create(alias, 100);
            var attempted = f.Create(alias, 100);
            var method = candidate.GetType().GetMethod("OnStacked", new[] { typeof(int) });
            if (candidate.MaxDuration <= 0 || !candidate.CanStack(attempted) || candidate.ShouldOverride(attempted)
                || candidate.AllowMultipleInstance || candidate.Type == ConditionType.Stance
                || candidate.IsToggle || method.DeclaringType != typeof(Condition)) continue;
            existing = candidate;
            selected = alias;
            break;
        }
        RuntimeAssertions.Require(existing != null, "No bounded native Condition with inherited OnStacked; capped-stack coverage blocked.");
        var owner = f.Spawn("capped");
        f.SetThrottle(false);
        f.Begin("capped_seed", owner, existing);
        var result = owner.AddCondition(existing, true);
        f.Expect(owner, existing, result, existing, 0, 1, false);
        existing.value = existing.MaxDuration;
        var input = f.Create(selected, 100);
        f.Begin("max_duration_stack", owner, input);
        result = owner.AddCondition(input, true);
        f.Expect(owner, input, result, null, 1, 1, false);
        RuntimeAssertions.Require(existing.value == existing.MaxDuration && owner.conditions.Contains(existing)
            && !owner.conditions.Contains(input) && existing.owner == owner,
            "Native capped-duration stack changed existing identity/duration.");
        ctx.Log("cap:alias=" + selected + ":duration=" + existing.value + ":maximum=" + existing.MaxDuration);
        f.Completed = true;
    }
}

public sealed class Pr5ConditionReentryCase : Pr5LogCaseBase
{
    public override string Id => "pr5.log.reentry_owner_isolation";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr5", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr5LogFixture>("pr5.fixture");
        var a = f.Spawn("outer");
        var b = f.Spawn("inner");
        var wetA = f.SeedWet(a);
        var wetB = f.SeedWet(b);
        var outer = f.Create("ConWet", 100);
        var inner = f.Create("ConWet", 100);
        int valueA = wetA.value;
        int valueB = wetB.value;
        f.Begin("nested_other_owner_stack", a, outer);
        f.Watch(b, inner);
        f.Arm(wetA, () =>
        {
            var nested = b.AddCondition(inner, true);
            RuntimeAssertions.Require(nested == null && b.conditions.Contains(wetB)
                && !b.conditions.Contains(inner), "Nested native stack returned/installed the attempted Condition.");
        });
        var result = a.AddCondition(outer, true);
        f.Expect(a, outer, result, null, 1, 1, false);
        f.ExpectObserved(b, inner, null, 1, 1, false);
        RuntimeAssertions.Require(f.InjectedCalls == 1 && wetA.value == f.StackValue(wetA, valueA, outer)
            && wetB.value == f.StackValue(wetB, valueB, inner), "Nested stack state/action count incorrect.");

        f.SetThrottle(true);
        outer = f.Create("ConWet", 100);
        inner = f.Create("ConWet", 0);
        valueB = wetB.value;
        f.Begin("nested_other_owner_rejected", a, outer);
        f.Watch(b, inner);
        f.Arm(wetA, () => RuntimeAssertions.Require(b.AddCondition(inner, false) == null,
            "Nested zero-power AddCondition did not return null."));
        result = a.AddCondition(outer, true);
        f.Expect(a, outer, result, null, 1, 1, true);
        f.ExpectObserved(b, inner, null, 0, 0, false);
        RuntimeAssertions.Require(f.InjectedCalls == 1 && wetB.value == valueB,
            "Rejected inner operation changed the other owner's existing Condition.");

        // Same-owner/different-condition rejection while outer successful evidence is live.
        f.RemoveFixtureThrottle(a, wetA.id);
        a.elements.SetBase(950, 10000);
        RuntimeAssertions.Require(a.ResistLv(950) >= 3, "Outer native burning resistance missing.");
        outer = f.Create("ConWet", 100);
        inner = f.Create("ConBurning", 100);
        f.Begin("nested_same_owner_resisted", a, outer);
        f.Watch(a, inner);
        f.Arm(wetA, () => RuntimeAssertions.Require(a.AddCondition(inner, false) == null,
            "Same-owner resisted AddCondition did not return null."));
        result = a.AddCondition(outer, true);
        f.Expect(a, outer, result, null, 1, 1, true);
        f.ExpectObserved(a, inner, null, 0, 0, false);
        RuntimeAssertions.Require(f.InjectedCalls == 1 && !a.conditions.Contains(inner),
            "Same-owner reentry resistance not exercised.");
        f.Completed = true;
    }
}

public sealed class Pr5ConditionExceptionCase : Pr5LogCaseBase
{
    public override string Id => "pr5.log.exception_finalizer";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr5", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr5LogFixture>("pr5.fixture");
        var a = f.Spawn("exception");
        var wet = f.SeedWet(a);
        f.SetThrottle(true);
        var attempted = f.Create("ConWet", 100);
        int before = wet.value;
        f.Begin("throw_after_native_stack", a, attempted);
        f.Arm(wet, () => { throw new Pr5InjectedException(); });
        bool caught = false;
        try { a.AddCondition(attempted, true); }
        catch (Pr5InjectedException) { caught = true; }
        RuntimeAssertions.Require(caught && f.InjectedCalls == 1 && f.ExceptionCalls == 1,
            "Fixture exception did not propagate through the real AddCondition finalizer.");
        RuntimeAssertions.Require(f.CountStack(a, attempted.id) == 1 && f.CountLogs(a, attempted.id) == 0
            && !f.HasThrottle(a, attempted.id) && a.conditions.Contains(wet)
            && wet.value == f.StackValue(wet, before, attempted),
            "Failed original should retain its native partial mutation but emit no success log/throttle.");
        f.RequireScopeEmpty();
        ctx.Log("throw_after_native_stack:uid=" + a.uid + ":stack=1:exception=1:log=0:throttle=false:scope=0");

        attempted = f.Create("ConWet", 0);
        f.Begin("rejection_after_exception", a, attempted);
        var result = a.AddCondition(attempted, false);
        f.Expect(a, attempted, result, null, 0, 0, false);
        attempted = f.Create("ConWet", 100);
        before = wet.value;
        f.Begin("retry_after_exception", a, attempted);
        result = a.AddCondition(attempted, true);
        f.Expect(a, attempted, result, null, 1, 1, true);
        RuntimeAssertions.Require(a.conditions.Contains(wet) && wet.value == f.StackValue(wet, before, attempted),
            "Normal native retry did not update the original Condition.");
        f.Completed = true;
    }
}

public sealed class Pr5InjectedException : System.Exception
{
    public Pr5InjectedException() : base("PR5 fixture-only OnStartOrStack injection") { }
}

public sealed class Pr5LogObservation
{
    public Chara Owner;
    public Condition Attempted;
    public string OwnerName;
    public string ConditionName;
    public int Calls;
    public Condition Result;
    public int Stacks;
    public int Logs;
}

public sealed class Pr5LogFixture
{
    public static Pr5LogFixture Active;
    private static Pr5LogFixture CleanupActive;
    public static bool CleanupFailed;
    public bool Completed;
    public int NullifyCalls;
    public int KillCalls;
    public int OverrideCalls;
    public int InjectedCalls;
    public int ExceptionCalls;
    private readonly RuntimeTestContext ctx;
    private readonly string token = System.Guid.NewGuid().ToString("N");
    private readonly System.Collections.Generic.List<Chara> cards = new System.Collections.Generic.List<Chara>();
    private readonly System.Collections.Generic.List<Pr5LogOwnedCard> ownedCards = new System.Collections.Generic.List<Pr5LogOwnedCard>();
    private readonly System.Collections.Generic.List<Condition> ownedConditions = new System.Collections.Generic.List<Condition>();
    private readonly System.Collections.Generic.List<Pr5LogObservation> observations = new System.Collections.Generic.List<Pr5LogObservation>();
    private readonly System.Collections.Generic.List<Pr5LogOriginalCard> originals = new System.Collections.Generic.List<Pr5LogOriginalCard>();
    private readonly System.Collections.Generic.List<Pr5LogConfigSnapshot> settings = new System.Collections.Generic.List<Pr5LogConfigSnapshot>();
    private readonly System.Collections.Generic.List<Chara> originalMap;
    private readonly System.Collections.Generic.List<Chara> originalParty;
    private readonly System.Collections.Generic.List<Chara> originalCarry;
    private readonly System.Collections.Generic.Dictionary<int, Chara> originalGlobal;
    private readonly System.Collections.IDictionary throttle;
    private readonly System.Collections.Generic.Dictionary<object, object> throttleBefore = new System.Collections.Generic.Dictionary<object, object>();
    private readonly System.Reflection.FieldInfo throttleCount;
    private readonly int countBefore;
    private readonly System.Reflection.FieldInfo scopes;
    private readonly HarmonyLib.Harmony observer;
    private readonly HarmonyLib.Harmony cleanupObserver;
    private readonly object configFile;
    private readonly System.Reflection.PropertyInfo saveOnSet;
    private readonly object saveOnSetBefore;
    private readonly string configPath;
    private readonly string configHashBefore;
    private string operation;
    private Condition armedCondition;
    private System.Action armedAction;

    private Pr5LogFixture(RuntimeTestContext context)
    {
        ctx = context;
        RuntimeAssertions.Require(EClass.pc != null && EClass._map != null && EClass._zone != null
            && (EClass.pc.Name ?? "").Contains("RUNTIME_TEST"), "PR5 requires a dedicated RUNTIME_TEST save/map.");
        RuntimeAssertions.Require(Active == null, "PR5 observer already active; reload dedicated save after cleanup failure.");
        RuntimeAssertions.Require(!CleanupFailed, "Previous PR5 cleanup failed; native operations blocked until dedicated-save reload/new suite.");
        RuntimeAssertions.Require(!Msg.ignoreAll, "Native message output disabled; log observation would not prove output.");
        var patch = RequireType("Elin_LogRefined.PatchChara");
        var guard = RequireType("Elin_LogRefined.RuntimeGuard");
        RuntimeAssertions.Require((bool)guard.GetMethod("IsGameplayReady").Invoke(null, null), "LogRefined gameplay guard false.");
        var add = HarmonyLib.AccessTools.Method(typeof(Chara), "AddCondition", new[] { typeof(Condition), typeof(bool) });
        var stacked = HarmonyLib.AccessTools.Method(typeof(Condition), "OnStacked", new[] { typeof(int) });
        RequireProductPatch(add, "Prefixes", "Elin_LogRefined.PatchChara", "Prefix");
        RequireProductPatch(add, "Postfixes", "Elin_LogRefined.PatchChara", "Postfix");
        RequireProductPatch(add, "Finalizers", "Elin_LogRefined.PatchChara", "Finalizer");
        RequireProductPatch(stacked, "Prefixes", "Elin_LogRefined.PatchConditionOnStackedEvidence", "Prefix");
        ctx.Log("loaded:" + patch.Assembly.FullName + ":location=" + patch.Assembly.Location);
        ctx.Log("native:" + add + ":stacked=" + stacked + ":productOwner=elin_log_refined");
        scopes = HarmonyLib.AccessTools.Field(patch, "_conditionScopes");
        RuntimeAssertions.Require(scopes != null, "PR5 scope field unavailable.");
        RequireScopeEmpty();
        var throttleType = RequireType("Elin_LogRefined.ConditionThrottle");
        throttle = (System.Collections.IDictionary)HarmonyLib.AccessTools.Field(throttleType, "_recent").GetValue(null);
        throttleCount = HarmonyLib.AccessTools.Field(throttleType, "_callCount");
        countBefore = (int)throttleCount.GetValue(null);
        foreach (System.Collections.DictionaryEntry entry in throttle) throttleBefore.Add(entry.Key, entry.Value);
        originalMap = new System.Collections.Generic.List<Chara>(EClass._map.charas);
        originalParty = new System.Collections.Generic.List<Chara>(EClass.pc.party.members);
        originalCarry = new System.Collections.Generic.List<Chara>(EClass.player.listCarryoverMap);
        originalGlobal = new System.Collections.Generic.Dictionary<int, Chara>(EClass.game.cards.globalCharas);
        foreach (var card in originalMap) originals.Add(new Pr5LogOriginalCard(card));
        if (!originalMap.Contains(EClass.pc)) originals.Add(new Pr5LogOriginalCard(EClass.pc));
        var config = RequireType("Elin_LogRefined.ModConfig");
        foreach (var name in new[] { "EnableMod", "ShowConditionLog", "ThrottleConditionLog", "ConditionThrottleCooldown", "DetailLevel", "EnableCommentary" })
            settings.Add(new Pr5LogConfigSnapshot(config, name));
        configFile = ReflectionCompat.GetFieldOrPropertyValue(settings[0].Entry, "ConfigFile");
        RuntimeAssertions.Require(configFile != null, "LogRefined ConfigFile unavailable.");
        saveOnSet = configFile.GetType().GetProperty("SaveOnConfigSet");
        RuntimeAssertions.Require(saveOnSet != null, "Cannot prevent ConfigFile writes.");
        saveOnSetBefore = saveOnSet.GetValue(configFile, null);
        configPath = (string)ReflectionCompat.GetFieldOrPropertyValue(configFile, "ConfigFilePath");
        RuntimeAssertions.Require(!string.IsNullOrEmpty(configPath), "Config file path unavailable for disk invariant.");
        configHashBefore = FileHash(configPath);
        observer = new HarmonyLib.Harmony("runtime.pr5.log." + token);
        cleanupObserver = new HarmonyLib.Harmony("runtime.pr5.log.cleanup." + token);
        // Register before any config, Harmony or world mutation; host rollback also runs on failed prepare.
        ctx.RegisterRollback("pr5.fixture_and_original_state", Cleanup);
        ctx.Set("pr5.fixture", this);
        Active = this;
        saveOnSet.SetValue(configFile, false, null);
        ctx.Log("config:SaveOnConfigSet=false:diskHashBefore=" + configHashBefore);
        Set("EnableMod", true);
        Set("ShowConditionLog", true);
        Set("ThrottleConditionLog", false);
        Set("ConditionThrottleCooldown", 60f);
        Set("EnableCommentary", false);
        var detailType = settings.Find(s => s.Name == "DetailLevel").Before.GetType();
        Set("DetailLevel", System.Enum.Parse(detailType, "WithTarget"));
        observer.Patch(add,
            postfix: Hook("AfterAdd", HarmonyLib.Priority.Last),
            finalizer: Hook("AfterException", HarmonyLib.Priority.Last));
        observer.Patch(stacked, postfix: Hook("AfterStack", HarmonyLib.Priority.Last));
        observer.Patch(HarmonyLib.AccessTools.Method(typeof(Msg), "SayRaw", new[] { typeof(string) }), prefix: Hook("BeforeMessage"));
        observer.Patch(HarmonyLib.AccessTools.Method(typeof(BaseCondition), "OnStartOrStack", System.Type.EmptyTypes), prefix: Hook("BeforeStartOrStack"));
        observer.Patch(HarmonyLib.AccessTools.Method(typeof(BaseCondition), "TryNullify", new[] { typeof(Condition) }), postfix: Hook("AfterNullify"));
        observer.Patch(HarmonyLib.AccessTools.Method(typeof(BaseCondition), "IsOverrideConditionMet", new[] { typeof(Condition), typeof(int) }), postfix: Hook("AfterOverride"));
        observer.Patch(HarmonyLib.AccessTools.Method(typeof(Condition), "Kill", new[] { typeof(bool) }), prefix: Hook("BeforeKill"));
    }

    public static void Start(RuntimeTestContext ctx) { new Pr5LogFixture(ctx); }
    private static System.Type RequireType(string name)
    {
        var t = HarmonyLib.AccessTools.TypeByName(name);
        RuntimeAssertions.Require(t != null, "Loaded Mod type absent: " + name);
        return t;
    }
    private static void RequireProductPatch(System.Reflection.MethodBase method, string bucket, string type, string name)
    {
        RuntimeAssertions.Require(method != null, "Native target absent: " + name);
        var info = HarmonyLib.Harmony.GetPatchInfo(method);
        var patches = ReflectionCompat.AsEnumerable(ReflectionCompat.GetFieldOrPropertyValue(info, bucket));
        bool found = false;
        if (patches != null) foreach (var entry in patches)
        {
            var m = ReflectionCompat.GetFieldOrPropertyValue(entry, "PatchMethod") as System.Reflection.MethodInfo;
            if (m != null && m.Name == name && m.DeclaringType.FullName == type
                && (string)ReflectionCompat.GetFieldOrPropertyValue(entry, "owner") == "elin_log_refined") found = true;
        }
        RuntimeAssertions.Require(found, "Required loaded product patch missing: " + bucket + ":" + type + "." + name);
    }
    private static HarmonyLib.HarmonyMethod Hook(string name, int priority = HarmonyLib.Priority.Normal)
    {
        return new HarmonyLib.HarmonyMethod(typeof(Pr5LogFixture), name)
        { priority = priority, after = new[] { "elin_log_refined" } };
    }
    private void Set(string name, object value) { settings.Find(s => s.Name == name).Set(value); }
    public void SetThrottle(bool enabled) { Set("ThrottleConditionLog", enabled); }
    public Chara Spawn(string role)
    {
        var card = CharaGen.Create("putty", 5);
        RuntimeAssertions.Require(card != null && card.uid > 0, "Native CharaGen fixture failed.");
        cards.Add(card);
        // Capture immediately after generation, before placement or condition operations.
        var owned = new Pr5LogOwnedCard(card, EClass._zone);
        ownedCards.Add(owned);
        foreach (var condition in card.conditions) ownedConditions.Add(condition);
        card.c_altName = "RUNTIME_TEST_PR5_" + token + "_" + role;
        var point = EClass.pc.pos.GetNearestPoint(allowBlock: false, allowChara: false);
        RuntimeAssertions.Require(point != null && point.IsValid, "No valid native fixture spawn point.");
        EClass._zone.AddCard(card, point);
        RuntimeAssertions.Require(EClass.pc.CanSee(card), "Fixture not visible to PC; no logging coverage.");
        RuntimeAssertions.Require(card.ride == null && card.parasite == null && card.c_uidMaster == 0,
            "Fixture unexpectedly linked to other cards.");
        ctx.Log("fixture:" + role + ":uid=" + card.uid + ":x=" + card.pos.x + ":z=" + card.pos.z);
        return card;
    }
    public Condition Create(string alias, int power)
    {
        RuntimeAssertions.Require(EClass.sources.stats.alias.ContainsKey(alias), "Native condition source absent: " + alias);
        var c = Condition.Create(alias, power);
        RuntimeAssertions.Require(c != null && c.source.alias == alias, "Condition.Create fallback/type mismatch: " + alias);
        ownedConditions.Add(c);
        return c;
    }
    public Condition SeedWet(Chara owner)
    {
        SetThrottle(false);
        var c = Create("ConWet", 100);
        Begin("wet_seed", owner, c);
        var result = owner.AddCondition(c, true);
        Expect(owner, c, result, c, 0, 1, false);
        return c;
    }
    public int StackValue(Condition existing, int before, Condition attempted)
    {
        int value = before + existing.EvaluateTurn(attempted.power);
        return existing.MaxDuration > 0 ? System.Math.Min(value, existing.MaxDuration) : value;
    }
    public void Begin(string label, Chara owner, Condition attempted)
    {
        RuntimeAssertions.Require(armedAction == null, "Unconsumed nested action from prior operation.");
        RequireScopeEmpty();
        operation = label;
        observations.Clear();
        NullifyCalls = KillCalls = OverrideCalls = InjectedCalls = ExceptionCalls = 0;
        Watch(owner, attempted);
        ctx.Log("operation:" + label + ":owner=" + owner.uid + ":condition=" + attempted.id
            + ":alias=" + attempted.source.alias + ":power=" + attempted.power + ":turn=" + attempted.EvaluateTurn(attempted.power));
    }
    public void Watch(Chara owner, Condition attempted)
    {
        RuntimeAssertions.Require(cards.Contains(owner), "Observer must target an owned fixture.");
        observations.Add(new Pr5LogObservation { Owner = owner, Attempted = attempted,
            OwnerName = owner.Name, ConditionName = attempted.Name });
    }
    public void Arm(Condition c, System.Action action) { armedCondition = c; armedAction = action; }
    private Pr5LogObservation Observation(Chara owner, int id)
    {
        return observations.Find(o => object.ReferenceEquals(o.Owner, owner) && o.Attempted.id == id);
    }
    public int CountStack(Chara owner, int id) { return Observation(owner, id).Stacks; }
    public int CountLogs(Chara owner, int id) { return Observation(owner, id).Logs; }
    public bool HasThrottle(Chara owner, int id) { return throttle.Contains((id, owner.uid)); }
    public void RemoveFixtureThrottle(Chara owner, int id)
    {
        RuntimeAssertions.Require(cards.Contains(owner), "Cannot remove nonfixture throttle key.");
        throttle.Remove((id, owner.uid));
    }
    public void Expect(Chara owner, Condition attempted, Condition result, Condition expectedResult, int stacks, int logs, bool throttled)
    {
        RuntimeAssertions.Require(object.ReferenceEquals(result, expectedResult), operation + ":native returned Condition mismatch.");
        ExpectObserved(owner, attempted, expectedResult, stacks, logs, throttled);
        RequireScopeEmpty();
    }
    public void ExpectObserved(Chara owner, Condition attempted, Condition result, int stacks, int logs, bool throttled)
    {
        var o = Observation(owner, attempted.id);
        RuntimeAssertions.Require(o.Calls == 1 && object.ReferenceEquals(o.Result, result)
            && o.Stacks == stacks && o.Logs == logs && HasThrottle(owner, attempted.id) == throttled,
            operation + ":uid=" + owner.uid + ":calls=" + o.Calls + ":stack=" + o.Stacks
            + ":log=" + o.Logs + ":throttle=" + HasThrottle(owner, attempted.id));
        ctx.Log("assert:" + operation + ":uid=" + owner.uid + ":id=" + attempted.id
            + ":result=" + (o.Result == null ? "null" : o.Result.id.ToString())
            + ":calls=1:stack=" + stacks + ":log=" + logs + ":throttle=" + throttled);
    }
    public void RequireScopeEmpty()
    {
        var stack = scopes.GetValue(null) as System.Collections.ICollection;
        RuntimeAssertions.Require(stack == null || stack.Count == 0, "LogRefined leaked AddCondition scope.");
    }
    public static void AfterAdd(Chara __instance, Condition c, Condition __result, bool __runOriginal)
    {
        var f = Active;
        if (f == null) return;
        var o = f.observations.Find(x => object.ReferenceEquals(x.Owner, __instance) && object.ReferenceEquals(x.Attempted, c));
        if (o != null)
        {
            RuntimeAssertions.Require(__runOriginal, "Another patch skipped the native AddCondition body; no native coverage.");
            o.Calls++; o.Result = __result;
        }
    }
    public static void AfterException(Chara __instance, System.Exception __exception)
    {
        if (Active != null && Active.cards.Contains(__instance) && __exception is Pr5InjectedException) Active.ExceptionCalls++;
    }
    public static void AfterStack(Condition __instance, bool __runOriginal)
    {
        var o = Active == null ? null : Active.Observation(__instance.owner, __instance.id);
        if (o != null)
        {
            RuntimeAssertions.Require(__runOriginal, "Another patch skipped the native OnStacked body; no stack coverage.");
            o.Stacks++;
        }
    }
    public static void BeforeMessage(string text)
    {
        var f = Active;
        if (f == null || text == null) return;
        string plain = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "").Trim();
        if (!plain.StartsWith("[ ", System.StringComparison.Ordinal) || !plain.EndsWith(" ]", System.StringComparison.Ordinal)) return;
        foreach (var o in f.observations)
            if (plain.Contains(o.OwnerName) && plain.Contains(o.ConditionName)) { o.Logs++; f.ctx.Log("tap:" + plain); }
    }
    public static void BeforeStartOrStack(BaseCondition __instance)
    {
        var f = Active;
        if (f == null || !object.ReferenceEquals(f.armedCondition, __instance) || f.armedAction == null) return;
        var action = f.armedAction;
        f.armedAction = null;
        f.armedCondition = null;
        f.InjectedCalls++;
        action();
    }
    public static void AfterNullify(BaseCondition __instance, Condition c, bool __result)
    {
        if (Active != null && __result && Active.cards.Contains(__instance.owner)
            && Active.Observation(__instance.owner, c.id) != null) Active.NullifyCalls++;
    }
    public static void BeforeKill(Condition __instance)
    {
        if (Active != null && Active.cards.Contains(__instance.owner)
            && Active.Observation(__instance.owner, __instance.id) != null) Active.KillCalls++;
    }
    public static void AfterOverride(BaseCondition __instance, Condition c, bool __result)
    {
        if (Active != null && !__result && Active.cards.Contains(__instance.owner)
            && Active.Observation(__instance.owner, c.id) != null) Active.OverrideCalls++;
    }
    private Pr5LogOwnedCard RequireCleanupCard(Card card)
    {
        var owned = ownedCards.Find(x => x.Tree.OwnsReference(card));
        RuntimeAssertions.Require(owned != null, "Unknown cleanup Card reference; restore external baseline.");
        owned.RequireSafe();
        return owned;
    }
    private Pr5LogOwnedCard RequireCleanupCondition(Condition condition)
    {
        RuntimeAssertions.Require(condition != null && ownedConditions.Exists(x => object.ReferenceEquals(x, condition)),
            "Unknown cleanup Condition; restore external baseline.");
        var owned = ownedCards.Find(x => object.ReferenceEquals(x.Card, condition.owner));
        RuntimeAssertions.Require(owned != null, "Condition owner changed away from the fixture; restore external baseline.");
        owned.RequireSafe();
        return owned;
    }
    public static void BeforeCleanupDestroy(Card __instance)
    {
        if (CleanupActive != null) CleanupActive.RequireCleanupCard(__instance);
    }
    public static void BeforeCleanupKill(Condition __instance)
    {
        if (CleanupActive != null) CleanupActive.RequireCleanupCondition(__instance);
    }
    private static HarmonyLib.HarmonyMethod CleanupHook(System.Reflection.MethodBase target, string name)
    {
        var info = HarmonyLib.Harmony.GetPatchInfo(target);
        var owners = info == null ? new string[0] : new System.Collections.Generic.List<string>(info.Owners).ToArray();
        return new HarmonyLib.HarmonyMethod(typeof(Pr5LogFixture), name)
            { priority = HarmonyLib.Priority.Last, after = owners };
    }
    private void Cleanup()
    {
        CleanupFailed = true;
        var errors = new System.Collections.Generic.List<string>();
        System.Action<string, System.Action> attempt = (label, action) =>
        { try { action(); } catch (System.Exception ex) { errors.Add(label + ":" + ex.Message); } };
        armedAction = null;
        armedCondition = null;
        attempt("unpatch", () =>
        {
            observer.UnpatchSelf();
            foreach (var method in HarmonyLib.Harmony.GetAllPatchedMethods())
                RuntimeAssertions.Require(!HarmonyLib.Harmony.GetPatchInfo(method).Owners.Contains(observer.Id),
                    "Test observer still installed: " + method);
            var add = HarmonyLib.AccessTools.Method(typeof(Chara), "AddCondition", new[] { typeof(Condition), typeof(bool) });
            RequireProductPatch(add, "Prefixes", "Elin_LogRefined.PatchChara", "Prefix");
            RequireProductPatch(add, "Postfixes", "Elin_LogRefined.PatchChara", "Postfix");
            RequireProductPatch(add, "Finalizers", "Elin_LogRefined.PatchChara", "Finalizer");
        });
        if (object.ReferenceEquals(Active, this)) Active = null;
        bool guardInstalled = false;
        try
        {
            attempt("install_cleanup_guard", () =>
            {
                CleanupActive = this;
                var destroy = HarmonyLib.AccessTools.Method(typeof(Card), "Destroy", System.Type.EmptyTypes);
                var kill = HarmonyLib.AccessTools.Method(typeof(Condition), "Kill", new[] { typeof(bool) });
                cleanupObserver.Patch(destroy, prefix: CleanupHook(destroy, "BeforeCleanupDestroy"));
                cleanupObserver.Patch(kill, prefix: CleanupHook(kill, "BeforeCleanupKill"));
                guardInstalled = true;
            });
            if (guardInstalled) foreach (var card in cards) attempt("fixture:" + card.uid, () =>
            {
                var owned = RequireCleanupCard(card);
                var conditions = new System.Collections.Generic.List<Condition>(card.conditions);
                // Validate all conditions before the first Kill or Destroy; no repair/adoption.
                foreach (var condition in conditions)
                    RuntimeAssertions.Require(object.ReferenceEquals(RequireCleanupCondition(condition), owned),
                        "Condition in fixture list belongs to a different fixture.");
                foreach (var condition in conditions)
                {
                    RequireCleanupCondition(condition);
                    owned.Tree.MutateOwned(card, condition.owner, card, () => condition.Kill(true));
                }
                // The immutable generation-time order contains children before their parents.
                foreach (var node in owned.Tree.DestructionOrder)
                {
                    RequireCleanupCard(node);
                    owned.Tree.DestroyOwned(node, () => node.Destroy());
                    RuntimeAssertions.Require(node.isDestroyed, "Fixture destruction did not complete: " + node.uid);
                }
                RuntimeAssertions.Require(card.isDestroyed && !EClass._map.charas.Contains(card)
                    && !EClass.game.cards.globalCharas.ContainsKey(card.uid) && !EClass.player.listCarryoverMap.Contains(card)
                    && !EClass.pc.party.members.Contains(card), "Fixture remained in world registries.");
            });
        }
        finally
        {
            if (object.ReferenceEquals(CleanupActive, this)) CleanupActive = null;
            attempt("unpatch_cleanup_guard", () =>
            {
                cleanupObserver.UnpatchSelf();
                foreach (var method in HarmonyLib.Harmony.GetAllPatchedMethods())
                    RuntimeAssertions.Require(!HarmonyLib.Harmony.GetPatchInfo(method).Owners.Contains(cleanupObserver.Id),
                        "Cleanup guard still installed: " + method);
            });
        }
        attempt("throttle_restore", () =>
        {
            throttle.Clear();
            foreach (var entry in throttleBefore) throttle.Add(entry.Key, entry.Value);
            throttleCount.SetValue(null, countBefore);
            RuntimeAssertions.Require(throttle.Count == throttleBefore.Count && (int)throttleCount.GetValue(null) == countBefore,
                "Throttle state/count restoration failed.");
            foreach (var entry in throttleBefore) RuntimeAssertions.Require(throttle.Contains(entry.Key)
                && object.Equals(throttle[entry.Key], entry.Value), "Original throttle timestamp changed.");
        });
        foreach (var setting in settings) attempt("config:" + setting.Name, () => setting.Restore());
        attempt("config_save_flag", () =>
        {
            saveOnSet.SetValue(configFile, saveOnSetBefore, null);
            RuntimeAssertions.Require(object.Equals(saveOnSet.GetValue(configFile, null), saveOnSetBefore), "Config save flag changed.");
        });
        attempt("config_disk", () => RuntimeAssertions.Require(FileHash(configPath) == configHashBefore,
            "LogRefined config file changed on disk; restore fixture backup before another run."));
        attempt("scope", RequireScopeEmpty);
        attempt("world_baseline", () =>
        {
            RequireSameCards(originalMap, EClass._map.charas, "map");
            RequireSameCards(originalParty, EClass.pc.party.members, "party");
            RequireSameCards(originalCarry, EClass.player.listCarryoverMap, "carryover");
            RuntimeAssertions.Require(EClass.game.cards.globalCharas.Count == originalGlobal.Count, "Global chara registry count changed.");
            foreach (var pair in originalGlobal) RuntimeAssertions.Require(EClass.game.cards.globalCharas.ContainsKey(pair.Key)
                && object.ReferenceEquals(EClass.game.cards.globalCharas[pair.Key], pair.Value), "Original global chara changed.");
            foreach (var snapshot in originals) snapshot.RequireUnchanged();
        });
        ctx.Log("cleanup:pr5:fixtures=" + cards.Count + ":observer=" + observer.Id + ":errors=" + errors.Count);
        CleanupFailed = errors.Count != 0;
        RuntimeAssertions.Require(errors.Count == 0, "PR5 cleanup failed; STOP and reload dedicated save: " + string.Join(";", errors));
    }
    private static string FileHash(string path)
    {
        if (!System.IO.File.Exists(path)) return "absent";
        var algorithm = System.Security.Cryptography.SHA256.Create();
        try { return System.BitConverter.ToString(algorithm.ComputeHash(System.IO.File.ReadAllBytes(path))); }
        finally { algorithm.Dispose(); }
    }
    private static void RequireSameCards(System.Collections.Generic.List<Chara> before,
        System.Collections.Generic.IEnumerable<Chara> after, string label)
    {
        var actual = new System.Collections.Generic.List<Chara>(after);
        RuntimeAssertions.Require(before.Count == actual.Count, "Original " + label + " count changed.");
        foreach (var card in before) RuntimeAssertions.Require(actual.Contains(card), "Original " + label + " card missing.");
    }
}

public sealed class Pr5LogOwnedCard
{
    public readonly Chara Card;
    public readonly Pr5OwnedCleanupGuard<Card> Tree;
    private readonly object faction;
    public Pr5LogOwnedCard(Chara card, object placementZone)
    {
        Card = card;
        faction = card.faction;
        Tree = new Pr5OwnedCleanupGuard<Card>(card, n => n.uid, Children, n => n.parent, n => n.isDestroyed, placementZone);
        RequireSafe();
    }
    private static System.Collections.Generic.IEnumerable<Card> Children(Card card)
    {
        var result = new System.Collections.Generic.List<Card>();
        foreach (var item in card.things) result.Add(item);
        return result;
    }
    public void RequireSafe()
    {
        Tree.RequireSafe();
        RuntimeAssertions.Require(object.ReferenceEquals(Card.faction, faction) && Card.c_uidMaster == 0
            && !Card.IsPC && !Card.IsPCParty && Card.host == null && Card.ride == null && Card.parasite == null
            && Card.held == null, "Fixture owner/faction/linked/held state changed; do not destroy, restore external baseline.");
    }
}

public sealed class Pr5LogConfigSnapshot
{
    public readonly string Name;
    public readonly object Entry;
    public readonly object Before;
    private readonly System.Reflection.PropertyInfo value;
    public Pr5LogConfigSnapshot(System.Type config, string name)
    {
        Name = name;
        Entry = HarmonyLib.AccessTools.Field(config, name).GetValue(null);
        RuntimeAssertions.Require(Entry != null, "LogRefined setting not loaded: " + name);
        value = Entry.GetType().GetProperty("Value");
        Before = value.GetValue(Entry, null);
    }
    public void Set(object v) { value.SetValue(Entry, v, null); }
    public void Restore()
    {
        Set(Before);
        RuntimeAssertions.Require(object.Equals(value.GetValue(Entry, null), Before), "Setting restoration failed: " + Name);
    }
}

public sealed class Pr5LogOriginalCard
{
    private readonly Chara card;
    private readonly int hp;
    private readonly int x;
    private readonly int z;
    private readonly int master;
    private readonly System.Collections.Generic.List<Condition> conditions;
    private readonly System.Collections.Generic.Dictionary<Condition, string> conditionValues = new System.Collections.Generic.Dictionary<Condition, string>();
    private readonly System.Collections.Generic.Dictionary<int, string> elements = new System.Collections.Generic.Dictionary<int, string>();
    public Pr5LogOriginalCard(Chara c)
    {
        card = c; hp = c.hp; x = c.pos.x; z = c.pos.z; master = c.c_uidMaster;
        conditions = new System.Collections.Generic.List<Condition>(c.conditions);
        foreach (var con in conditions) conditionValues.Add(con, State(con));
        foreach (var pair in c.elements.dict) elements.Add(pair.Key, pair.Value.vBase + ":" + pair.Value.Value);
    }
    private static string State(Condition c) { return c.id + ":" + c.power + ":" + c.value + ":" + c.phase; }
    public void RequireUnchanged()
    {
        RuntimeAssertions.Require(!card.isDestroyed && card.hp == hp && card.pos.x == x && card.pos.z == z
            && card.c_uidMaster == master && card.conditions.Count == conditions.Count,
            "Original card HP/location/master/condition count changed: " + card.uid);
        foreach (var con in conditions) RuntimeAssertions.Require(card.conditions.Contains(con)
            && State(con) == conditionValues[con], "Original condition changed: " + card.uid);
        RuntimeAssertions.Require(card.elements.dict.Count == elements.Count, "Original element count changed: " + card.uid);
        foreach (var pair in card.elements.dict) RuntimeAssertions.Require(elements.ContainsKey(pair.Key)
            && elements[pair.Key] == pair.Value.vBase + ":" + pair.Value.Value, "Original element changed: " + card.uid);
    }
}
