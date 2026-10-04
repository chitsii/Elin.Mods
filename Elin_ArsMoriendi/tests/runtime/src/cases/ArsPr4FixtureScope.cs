// Test-only UID-scoped isolation. Register cleanup before adding cards to the world.
public sealed class ArsPr4FixtureScope
{
    public const string NamePrefix = "RUNTIME_TEST_PR4_ARS_";
    public readonly string Token = System.Guid.NewGuid().ToString("N");
    public readonly System.Collections.Generic.List<Chara> Cards = new System.Collections.Generic.List<Chara>();
    private readonly RuntimeTestContext ctx;
    private readonly ArsPr4Baseline baseline;

    private ArsPr4FixtureScope(RuntimeTestContext context, ArsPr4Baseline savedBaseline)
    {
        ctx = context;
        Guard();
        bool originalIgnoreAutoSave = EClass.debug.ignoreAutoSave;
        ctx.RegisterRollback("pr4.autosave_restore", () =>
        {
            EClass.debug.ignoreAutoSave = originalIgnoreAutoSave;
            RuntimeAssertions.Require(EClass.debug.ignoreAutoSave == originalIgnoreAutoSave,
                "PR4 autosave flag restoration failed.");
            ctx.Log("autosave:restored=" + originalIgnoreAutoSave);
        });
        EClass.debug.ignoreAutoSave = true;
        ctx.Log("autosave:suppressed:original=" + originalIgnoreAutoSave);
        baseline = savedBaseline ?? ArsPr4Baseline.Capture();
        ctx.RegisterRollback("pr4.original_state", () => baseline.RequireRestored());
        ctx.Set("pr4.scope", this);
    }

    public static ArsPr4FixtureScope Start(RuntimeTestContext ctx, ArsPr4Baseline baseline = null)
    {
        return new ArsPr4FixtureScope(ctx, baseline);
    }

    public static void Guard()
    {
        RuntimeAssertions.Require(EClass.pc != null && EClass._map != null && EClass._zone != null,
            "PR4 requires an active map and PC.");
        RuntimeAssertions.Require((EClass.pc.Name ?? "").IndexOf("RUNTIME_TEST", System.StringComparison.Ordinal) >= 0,
            "PR4 dedicated-save name guard rejected.");
        RuntimeAssertions.Require(EClass.pc.host == null && EClass.pc.ride == null,
            "PR4 requires unmounted PC.");
    }

    public Chara Own(Chara card, string role, bool rename = true)
    {
        RuntimeAssertions.Require(card != null && card.uid > 0, "Invalid PR4 fixture.");
        if (Cards.Contains(card))
        {
            if (rename) card.c_altName = NamePrefix + Token + "_" + role;
            return card;
        }
        var ownedThings = new System.Collections.Generic.List<Thing>();
        ctx.RegisterRollback("pr4.fixture_items:" + card.uid, () => RunOwnedCleanup(card, (owned, unusedRemoval) =>
        {
            foreach (var thing in ownedThings)
            {
                if (!thing.isDestroyed) thing.Destroy();
                RuntimeAssertions.Require(thing.isDestroyed, "Fixture inventory/drop cleanup failed: " + thing.uid);
            }
        }));
        ctx.RegisterRollback("pr4.fixture:" + card.uid, () => Cleanup(card));
        Cards.Add(card);
        CollectThings(card, ownedThings);
        if (rename) card.c_altName = NamePrefix + Token + "_" + role;
        ctx.Log("fixture:" + role + ":uid=" + card.uid + ":name=" + card.c_altName);
        return card;
    }

    public Chara Spawn(string role, int level = 5)
    {
        var card = Own(CharaGen.Create("putty", level), role);
        EClass._zone.AddCard(card, EClass.pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        return card;
    }

    public Chara CastSummon(bool skeleton)
    {
        RuntimeAssertions.Require(EClass._zone.CountMinions(EClass.pc) < EClass.pc.MaxSummon,
            "PC summon slots full; do not delete existing minions to make this pass.");
        var beforeMap = new System.Collections.Generic.List<Chara>(EClass._map.charas);
        var protectedUids = new System.Collections.Generic.HashSet<int>();
        protectedUids.Add(EClass.pc.uid);
        foreach (var card in EClass.game.cards.globalCharas.Values) if (card != null) protectedUids.Add(card.uid);
        foreach (var card in beforeMap) protectedUids.Add(card.uid);
        foreach (var card in EClass.player.listCarryoverMap) protectedUids.Add(card.uid);
        Spell spell = skeleton ? (Spell)new Elin_ArsMoriendi.ActSummonSkeletonWarrior()
            : new Elin_ArsMoriendi.ActSummonUndead();
        var proof = new ArsPr4SummonOwnership(protectedUids, spell);
        // Only native births returned at this exact Perform instance's generation sites are ours.
        ctx.RegisterRollback("pr4.partial_cast", () =>
        {
            foreach (Chara card in proof.OwnedObjects) if (!Cards.Contains(card)) Cleanup(card);
        });
        var observer = new HarmonyLib.Harmony("runtime.pr4.ars.summon." + Token + "." + System.Guid.NewGuid().ToString("N"));
        ctx.RegisterRollback("pr4.summon_observer", () =>
        {
            observer.UnpatchSelf();
            ArsPr4SummonObserver.Reset(proof);
        });
        RuntimeAssertions.Require(ArsPr4SummonObserver.Proof == null, "Another PR4 summon observer is active.");
        ArsPr4SummonObserver.Proof = proof;
        ArsPr4SummonObserver.Scope = this;
        ArsPr4SummonObserver.Role = skeleton ? "skeleton" : "undead";
        ArsPr4SummonObserver.ExpectedResultSites = skeleton ? 1 : 2;
        try
        {
            observer.Patch(HarmonyLib.AccessTools.Method(typeof(CharaGen), "_Create", new[] { typeof(string), typeof(int), typeof(int) }),
                transpiler: new HarmonyLib.HarmonyMethod(typeof(ArsPr4SummonObserver), "ObserveNativeConstructor"));
            observer.Patch(HarmonyLib.AccessTools.DeclaredMethod(spell.GetType(), "Perform"),
                transpiler: new HarmonyLib.HarmonyMethod(typeof(ArsPr4SummonObserver), "ObserveSpellGenerationSites"));
            Cast(spell, skeleton ? "actSummonSkeletonWarrior" : "actSummonUndead", EClass.pc);
        }
        finally
        {
            observer.UnpatchSelf();
            ArsPr4SummonObserver.Reset(proof);
        }
        bool unownedAddition = false;
        foreach (var card in EClass._map.charas)
        {
            bool wasOnMap = false;
            foreach (var previous in beforeMap) if (object.ReferenceEquals(previous, card)) { wasOnMap = true; break; }
            if (wasOnMap || proof.IsOwned(card, card.uid)) continue;
            unownedAddition = true;
            ctx.Log("unowned_map_addition:uid=" + card.uid + ":unchanged_by_test:restore_dedicated_save");
        }
        RuntimeAssertions.Require(!proof.RejectedResult && !unownedAddition,
            "Summon ownership not proven/unrelated map addition detected. Unknown cards were NOT renamed or destroyed; restore dedicated save.");
        RuntimeAssertions.Require(proof.OwnedObjects.Count == 1, "Real summon produced no single proven fixture; restore dedicated save.");
        var found = (Chara)proof.OwnedObjects[0];
        Own(found, skeleton ? "skeleton" : "undead");
        RequireTemporary(found, ctx);
        return found;
    }

    public static void Cast(Spell spell, string alias, Chara caster)
    {
        SourceElement.Row row = null;
        foreach (var candidate in EClass.sources.elements.rows)
            if (candidate.alias == alias) { row = candidate; break; }
        RuntimeAssertions.Require(row != null, "Spell source absent: " + alias);
        spell.id = row.id;
        RuntimeAssertions.Require(spell.source == row, "Spell ID did not resolve native source: " + alias);
        var oldCC = Act.CC;
        var oldTC = Act.TC;
        var oldTP = Act.TP;
        try
        {
            Act.CC = caster;
            Act.TC = caster;
            Act.TP = caster.pos;
            RuntimeAssertions.Require(spell.Perform(), "Real spell Perform returned false: " + alias);
        }
        finally
        {
            Act.CC = oldCC;
            Act.TC = oldTC;
            Act.TP = oldTP;
        }
    }

    public static int CountUid(System.Collections.Generic.IEnumerable<Chara> cards, int uid)
    {
        int n = 0;
        if (cards != null) foreach (var card in cards) if (card != null && card.uid == uid) n++;
        return n;
    }

    public static int CountUid(System.Collections.Generic.IEnumerable<(Chara chara, bool isAlive)> cards, int uid)
    {
        int n = 0;
        foreach (var entry in cards) if (entry.chara != null && entry.chara.uid == uid) n++;
        return n;
    }

    public static Chara Resolve(int uid)
    {
        Chara card;
        if (EClass.game.cards.globalCharas.TryGetValue(uid, out card)) return card;
        foreach (var c in EClass._map.charas) if (c.uid == uid) return c;
        foreach (var c in EClass.player.listCarryoverMap) if (c.uid == uid) return c;
        return null;
    }

    public static void RequireTemporary(Chara card, RuntimeTestContext ctx)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        RuntimeAssertions.Require(card != null && !card.isDestroyed && !card.isDead, "Temporary fixture ended unexpectedly.");
        int global = CountUid(EClass.game.cards.globalCharas.Values, card.uid);
        int map = CountUid(EClass._map.charas, card.uid);
        int carry = CountUid(EClass.player.listCarryoverMap, card.uid);
        ctx.Log("location:uid=" + card.uid + ":global=" + global + ":map=" + map + ":carry=" + carry
            + ":duration=" + card.c_summonDuration + ":master=" + card.c_uidMaster);
        RuntimeAssertions.Require(!card.IsGlobal && global == 0, "Temporary summon became global.");
        RuntimeAssertions.Require(map + carry == 1, "Temporary UID missing or duplicated in map/carryover.");
        RuntimeAssertions.Require(card.isSummon && card.c_summonDuration > 0 && card.c_uidMaster == EClass.pc.uid,
            "Native summon lifetime/master not preserved.");
        RuntimeAssertions.Require(card.HasCondition<Elin_ArsMoriendi.ConUndeadServantPresence>(), "Ars marker missing.");
        RuntimeAssertions.Require(CountUid(mgr.GetAliveServants(), card.uid) == 1
            && CountUid(mgr.GetAllServants(), card.uid) == 1, "Normal queries lost or duplicated temporary UID.");
        RuntimeAssertions.Require(!EClass.player.dialogFlags.ContainsKey("chitsii.ars.sv." + card.uid),
            "Temporary summon persisted an sv flag.");
        RuntimeAssertions.Require(!mgr.IsServantStashed(card.uid), "Temporary summon entered permanent stash.");
    }

    public static void RequirePermanent(Chara card)
    {
        RuntimeAssertions.Require(card.IsGlobal && !card.isSummon && card.c_summonDuration == 0,
            "Ritual fixture not global/permanent.");
        RuntimeAssertions.Require(CriticalCaseHelpers.GetDialogFlag("chitsii.ars.sv." + card.uid) == 1,
            "Permanent ritual sv flag missing.");
        RuntimeAssertions.Require(card.c_uidMaster == EClass.pc.uid && card.trait is Elin_ArsMoriendi.TraitUndeadServant,
            "Ritual master/trait missing.");
        if (EClass.pc.homeBranch != null)
            RuntimeAssertions.Require(card.homeBranch == EClass.pc.homeBranch && card.homeBranch.members.Contains(card),
                "Ritual home membership missing.");
    }

    public static void RequireEnded(Chara card, RuntimeTestContext ctx)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        ctx.Log("ended_observed:uid=" + card.uid + ":destroyed=" + card.isDestroyed
            + ":tracked=" + mgr.IsServant(card.uid)
            + ":marker=" + card.HasCondition<Elin_ArsMoriendi.ConUndeadServantPresence>()
            + ":map=" + CountUid(EClass._map.charas, card.uid)
            + ":carry=" + CountUid(EClass.player.listCarryoverMap, card.uid)
            + ":global=" + CountUid(EClass.game.cards.globalCharas.Values, card.uid)
            + ":dead=" + CountUid(EClass._map.deadCharas, card.uid));
        try { RequireNoFx(card.uid); ctx.Log("ended_observed:fx=0"); }
        catch (System.Exception ex) { ctx.Log("ended_observed:fx_error=" + ex.Message); }
        try { RequireNoFlags(card.uid); ctx.Log("ended_observed:flags=0"); }
        catch (System.Exception ex) { ctx.Log("ended_observed:flags_error=" + ex.Message); }
        // Check raw tracking before queries can prune and mask a missing Die postfix.
        RuntimeAssertions.Require(card.isDestroyed && !mgr.IsServant(card.uid), "Native expiry/Die did not untrack immediately.");
        // A marker on an unreachable destroyed instance is diagnostic, not a live servant registration.
        ctx.Log("ended_marker_diagnostic:uid=" + card.uid + ":marker="
            + card.HasCondition<Elin_ArsMoriendi.ConUndeadServantPresence>());
        RequireNoFx(card.uid);
        RuntimeAssertions.Require(CountUid(EClass._map.charas, card.uid) == 0
            && CountUid(EClass.player.listCarryoverMap, card.uid) == 0
            && CountUid(EClass.game.cards.globalCharas.Values, card.uid) == 0, "Ended summon left runtime remnants.");
        RuntimeAssertions.Require(CountUid(EClass._map.deadCharas, card.uid) == 0,
            "Ended summon remained in native zone revival candidates.");
        RuntimeAssertions.Require(CountUid(mgr.GetAliveServants(), card.uid) == 0, "Ended summon remained in alive query.");
        RuntimeAssertions.Require(CountUid(mgr.GetAllServants(), card.uid) == 0, "Ended summon remained in UI query.");
        RuntimeAssertions.Require(CountUid(mgr.GetCombatServantsInCurrentZone(), card.uid) == 0,
            "Ended summon remained in combat query.");
        RequireNoFlags(card.uid);
        ctx.Log("ended:uid=" + card.uid + ":destroyed=" + card.isDestroyed + ":tracked=" + mgr.IsServant(card.uid)
            + ":marker=" + card.HasCondition<Elin_ArsMoriendi.ConUndeadServantPresence>() + ":fx=0:dead=0");
    }

    public static void RequireNoFx(int uid)
    {
        foreach (var field in new[] { "ActiveAttachedFx", "LoopAttachedFx", "LoopLeaseUntilTurn" })
        {
            var dict = HarmonyLib.AccessTools.Field(typeof(Elin_ArsMoriendi.CustomAssetFx), field).GetValue(null)
                as System.Collections.IDictionary;
            RuntimeAssertions.Require(dict != null, "FX registry unavailable: " + field);
            foreach (var key in dict.Keys)
                RuntimeAssertions.Require(!((string)key).EndsWith(":" + uid, System.StringComparison.Ordinal),
                    "Fixture FX/lease remained in " + field);
        }
    }

    public static void RequireNoFlags(int uid)
    {
        foreach (var key in EClass.player.dialogFlags.Keys)
            RuntimeAssertions.Require(key != "chitsii.ars.sv." + uid
                && !key.StartsWith("chitsii.ars.enh." + uid + ".", System.StringComparison.Ordinal),
                "Fixture servant/enhancement flag remained: " + key);
    }

    private static void Cleanup(Chara original)
    {
        RunOwnedCleanup(original, CleanupVerified);
    }

    private static void RunOwnedCleanup(Chara original, System.Action<Chara, System.Action> mutation)
    {
        ArsPr4CleanupBoundary.Run(original, original.uid, c => c.uid,
            EClass.game.cards.globalCharas.Values, EClass._map.charas, EClass.player.listCarryoverMap, mutation);
    }

    private static void CleanupVerified(Chara card, System.Action removeOwnedCarryover)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        var failures = new System.Collections.Generic.List<string>();
        System.Action<string, System.Action> attempt = (step, action) =>
        {
            try { action(); }
            catch (System.Exception ex) { failures.Add(step + ":" + ex.Message); }
        };
        attempt("untrack", () => mgr.RemoveServant(card.uid));
        attempt("fx", () => Elin_ArsMoriendi.CustomAssetFx.StopAllAttachedFx(card));
        attempt("presence", () => card.RemoveCondition<Elin_ArsMoriendi.ConUndeadServantPresence>());
        attempt("party", () => { if (EClass.pc.party?.members.Contains(card) == true) EClass.pc.party.RemoveMember(card); });
        attempt("home", () => card.homeBranch?.RemoveMemeber(card));
        attempt("reserve", () => EClass.Home.RemoveReserve(card));
        attempt("master", () => { if (card.c_uidMaster != 0) card.UnmakeMinion(); });
        attempt("faction", () => card.SetFaction(EClass.game.factions.Wilds));
        attempt("global", () => card.RemoveGlobal());
        attempt("carryover", removeOwnedCarryover);
        attempt("destroy", () => { if (!card.isDestroyed) card.Destroy(); });
        RuntimeAssertions.Require(card.isDestroyed && card.c_uidMaster == 0 && Resolve(card.uid) == null && !mgr.IsServant(card.uid),
            "PR4 fixture cleanup left card/tracking: " + card.uid);
        RuntimeAssertions.Require(EClass.pc.party?.members.Contains(card) != true
            && card.homeBranch?.members.Contains(card) != true, "PR4 cleanup left party/home membership.");
        RequireNoFlags(card.uid);
        RequireNoFx(card.uid);
        RuntimeAssertions.Require(failures.Count == 0, "PR4 cleanup errors: " + string.Join(";", failures));
    }

    public ArsPr4Baseline Baseline => baseline;

    private static void CollectThings(Card card, System.Collections.Generic.List<Thing> result)
    {
        foreach (var thing in card.things)
        {
            result.Add(thing);
            CollectThings(thing, result);
        }
    }
}

public static class ArsPr4SummonObserver
{
    public static ArsPr4SummonOwnership Proof;
    public static ArsPr4FixtureScope Scope;
    public static string Role;
    public static int ExpectedResultSites;

    public static void Reset(ArsPr4SummonOwnership proof)
    {
        if (!object.ReferenceEquals(Proof, proof)) return;
        Proof = null; Scope = null; Role = null; ExpectedResultSites = 0;
    }

    public static void RecordBirth(Chara card) { Proof?.ObserveBirth(card); }

    public static void RecordResult(Chara card, Spell producer)
    {
        if (Proof != null && Proof.ObserveSpellResult(card, card == null ? 0 : card.uid, producer))
            Scope.Own(card, Role, false);
    }

    public static System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction> ObserveNativeConstructor(
        System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction> instructions)
    {
        int sites = 0;
        var output = new System.Collections.Generic.List<HarmonyLib.CodeInstruction>();
        foreach (var instruction in instructions)
        {
            output.Add(instruction);
            var ctor = instruction.operand as System.Reflection.ConstructorInfo;
            if (instruction.opcode != System.Reflection.Emit.OpCodes.Newobj || ctor == null
                || ctor.DeclaringType != typeof(Chara) || ctor.GetParameters().Length != 0) continue;
            sites++;
            output.Add(new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Dup));
            output.Add(new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Call,
                HarmonyLib.AccessTools.Method(typeof(ArsPr4SummonObserver), "RecordBirth")));
        }
        RuntimeAssertions.Require(sites == 1, "Native CharaGen._Create birth site changed; ownership cannot be proven.");
        return output;
    }

    public static System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction> ObserveSpellGenerationSites(
        System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction> instructions)
    {
        int sites = 0;
        var output = new System.Collections.Generic.List<HarmonyLib.CodeInstruction>();
        foreach (var instruction in instructions)
        {
            output.Add(instruction);
            var method = instruction.operand as System.Reflection.MethodInfo;
            if (instruction.opcode != System.Reflection.Emit.OpCodes.Call || method == null
                || method.DeclaringType != typeof(CharaGen) || method.ReturnType != typeof(Chara)
                || (method.Name != "Create" && method.Name != "CreateFromFilter")) continue;
            sites++;
            output.Add(new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Dup));
            output.Add(new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0));
            output.Add(new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Call,
                HarmonyLib.AccessTools.Method(typeof(ArsPr4SummonObserver), "RecordResult")));
        }
        RuntimeAssertions.Require(sites == ExpectedResultSites, "Spell generation sites changed; ownership cannot be proven.");
        return output;
    }
}

public sealed partial class ArsPr4Baseline
{
    public static ArsPr4Baseline Capture()
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        var snapshot = new ArsPr4Baseline();
        var trackedCards = new System.Collections.Generic.List<Chara>();
        foreach (var entry in mgr.GetAllServants()) trackedCards.Add(entry.chara);
        snapshot.tracked = Uids(trackedCards);
        snapshot.party = Uids(EClass.pc.party.members);
        snapshot.home = Uids(EClass.pc.homeBranch?.members);
        var keys = new System.Collections.Generic.List<string>();
        var values = new System.Collections.Generic.List<int>();
        foreach (var kv in EClass.player.dialogFlags)
        {
            if (!IsGuardedFlag(kv.Key)) continue;
            keys.Add(kv.Key);
            values.Add(kv.Value);
        }
        snapshot.keys = keys.ToArray();
        snapshot.values = values.ToArray();
        return snapshot;
    }

    private static bool IsGuardedFlag(string key)
    {
        return key.StartsWith("chitsii.ars.", System.StringComparison.Ordinal)
            && !key.StartsWith("chitsii.ars.vfx.", System.StringComparison.Ordinal);
    }

    private static int[] Uids(System.Collections.Generic.IEnumerable<Chara> cards)
    {
        var ids = new System.Collections.Generic.List<int>();
        if (cards != null) foreach (var card in cards) ids.Add(card.uid);
        ids.Sort();
        return ids.ToArray();
    }

    private static void RequireIds(int[] expected, int[] actual, string label)
    {
        RuntimeAssertions.Require(expected.Length == actual.Length, "Original " + label + " count changed.");
        for (int i = 0; i < expected.Length; i++)
            RuntimeAssertions.Require(expected[i] == actual[i], "Original " + label + " UID changed.");
    }

    public void RequireRestored()
    {
        var actual = Capture();
        RequireIds(tracked, actual.tracked, "servants");
        RequireIds(party, actual.party, "party");
        RequireIds(home, actual.home, "home");
        var flags = EClass.player.dialogFlags;
        RuntimeAssertions.Require(keys.Length == actual.keys.Length, "Original Ars flag count changed (quest/servants).");
        for (int i = 0; i < keys.Length; i++)
            RuntimeAssertions.Require(flags.ContainsKey(keys[i]) && flags[keys[i]] == values[i],
                "Original Ars flag changed: " + keys[i]);
    }
}
