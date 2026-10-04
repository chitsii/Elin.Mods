public sealed class ArsPr4TemporaryLifecycleCase : RuntimeCaseBase
{
    public override string Id => "pr4.ars.temporary_lifecycle";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "destructive", "pr4", "servant" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var scope = ArsPr4FixtureScope.Start(ctx);
        var skeleton = scope.CastSummon(true);
        var undead = scope.CastSummon(false);
        ctx.Set("pr4.skeleton", skeleton);
        ctx.Set("pr4.undead", undead);
        ArsPr4Controls.Create(ctx, scope);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        var skeleton = ctx.Get<Chara>("pr4.skeleton");
        var undead = ctx.Get<Chara>("pr4.undead");
        RuntimeAssertions.Require(!mgr.SetServantStashedState(skeleton, true), "Temporary summon was accepted into stash.");
        ArsPr4FixtureScope.RequireTemporary(skeleton, ctx);
        skeleton.c_summonDuration = 1;
        skeleton.TickConditions();
        ctx.Log("native:TickConditions:uid=" + skeleton.uid);
        // Do not query/prune or call UntrackEndedTemporaryServant before testing the real Die postfix.
        var failures = new System.Collections.Generic.List<string>();
        try { ArsPr4FixtureScope.RequireEnded(skeleton, ctx); }
        catch (System.Exception ex) { failures.Add("expiry:" + ex.Message); ctx.Log("expiry:failed:" + ex.Message); }
        undead.DamageHP((long)undead.MaxHP * 10 + 1000, AttackSource.Euthanasia,
            ctx.Get<Chara>("pr4.enemyOwner"));
        ctx.Log("native:DamageHP(Euthanasia):uid=" + undead.uid);
        try { ArsPr4FixtureScope.RequireEnded(undead, ctx); }
        catch (System.Exception ex) { failures.Add("damage_death:" + ex.Message); ctx.Log("damage_death:failed:" + ex.Message); }
        RuntimeAssertions.Require(failures.Count == 0, string.Join("; ", failures));
    }

    public override void Verify(RuntimeTestContext ctx) { ArsPr4Controls.Verify(ctx); }
    public override void Cleanup(RuntimeTestContext ctx) { }
}

public sealed class ArsPr4PermanentControlCase : RuntimeCaseBase
{
    public override string Id => "pr4.ars.permanent_control";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "destructive", "pr4", "servant" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        var scope = ArsPr4FixtureScope.Start(ctx);
        RuntimeAssertions.Require(EClass.pc.homeBranch != null, "Permanent stash control requires a native PC home branch.");
        var permanent = scope.Spawn("permanent");
        ctx.Set("pr4.permanent", permanent);
        Elin_ArsMoriendi.NecromancyManager.Instance.RegisterRitualServant(permanent, 1);
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var card = ctx.Get<Chara>("pr4.permanent");
        ArsPr4FixtureScope.RequirePermanent(card);
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        RuntimeAssertions.Require(mgr.SetServantStashedState(card, true) && mgr.IsServantStashed(card.uid),
            "Permanent fixture could not enter home stash.");
        ArsPr4FixtureScope.RequirePermanent(card);
        RuntimeAssertions.Require(mgr.SetServantStashedState(card, false) && !mgr.IsServantStashed(card.uid),
            "Permanent fixture could not return from home stash.");
        RuntimeAssertions.Require(card.currentZone == EClass._zone, "Permanent recall did not return to PC zone.");
        card.Die(attackSource: AttackSource.Euthanasia);
        RuntimeAssertions.Require(card.isDead && !card.isDestroyed, "Native permanent death did not preserve the corpse/global card.");
        RuntimeAssertions.Require(Elin_ArsMoriendi.NecromancyManager.Instance.IsServant(card.uid), "Permanent death was untracked as temporary.");
        card.Revive();
        if (card.currentZone != EClass._zone || ArsPr4FixtureScope.CountUid(EClass._map.charas, card.uid) == 0)
            EClass._zone.AddCard(card, EClass.pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
    }
    public override void Verify(RuntimeTestContext ctx)
    {
        var card = ctx.Get<Chara>("pr4.permanent");
        RuntimeAssertions.Require(!card.isDead, "Native Revive did not revive permanent fixture.");
        ArsPr4FixtureScope.RequirePermanent(card);
        RuntimeAssertions.Require(ArsPr4FixtureScope.CountUid(Elin_ArsMoriendi.NecromancyManager.Instance.GetAllServants(), card.uid) == 1,
            "Permanent death/revival lost tracking.");
    }
    public override void Cleanup(RuntimeTestContext ctx) { }
}

public sealed class ArsPr4TemporaryDestroyCase : RuntimeCaseBase
{
    public override string Id => "pr4.ars.temporary_destroy";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "destructive", "pr4", "servant" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        var scope = ArsPr4FixtureScope.Start(ctx);
        ctx.Set("pr4.destroy", scope.CastSummon(true));
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var card = ctx.Get<Chara>("pr4.destroy");
        card.Destroy();
        // A direct native Destroy has no Die postfix. The next ordinary query must prune it.
        Elin_ArsMoriendi.NecromancyManager.Instance.GetAllServants();
        ctx.Log("native:Destroy:uid=" + card.uid);
    }
    public override void Verify(RuntimeTestContext ctx) { ArsPr4FixtureScope.RequireEnded(ctx.Get<Chara>("pr4.destroy"), ctx); }
    public override void Cleanup(RuntimeTestContext ctx) { }
}

public static class ArsPr4Controls
{
    public static void Create(RuntimeTestContext ctx, ArsPr4FixtureScope scope)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        var ordinary = scope.Spawn("ordinary");
        ordinary.MakeMinion(EClass.pc);
        ordinary.SetSummon(1000);
        var owner = scope.Spawn("enemy_owner");
        owner.hostility = Hostility.Enemy;
        owner.noMove = true;
        var enemy = scope.Spawn("enemy_summon");
        enemy.MakeMinion(owner);
        enemy.SetSummon(1000);
        enemy.noMove = true;
        // Even an Ars marker must not recover a summon belonging to an enemy.
        enemy.AddCondition<Elin_ArsMoriendi.ConUndeadServantPresence>(1, true);
        var permanent = scope.Spawn("permanent");
        mgr.RegisterRitualServant(permanent, 1);
        ctx.Set("pr4.ordinary", ordinary);
        ctx.Set("pr4.enemy", enemy);
        ctx.Set("pr4.enemyOwner", owner);
        ctx.Set("pr4.permanent", permanent);
        Verify(ctx);
    }

    public static void Verify(RuntimeTestContext ctx)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        var ordinary = ctx.Get<Chara>("pr4.ordinary");
        var enemy = ctx.Get<Chara>("pr4.enemy");
        mgr.GetAliveServants();
        mgr.GetAllServants();
        RuntimeAssertions.Require(!mgr.IsServant(ordinary.uid) && !mgr.IsServant(enemy.uid), "Non-Ars/enemy control was incorporated.");
        RuntimeAssertions.Require(ordinary.c_uidMaster == EClass.pc.uid && !ordinary.IsGlobal && ordinary.isSummon,
            "Ordinary control ownership/global state changed.");
        RuntimeAssertions.Require(enemy.c_uidMaster == ctx.Get<Chara>("pr4.enemyOwner").uid && !enemy.IsGlobal,
            "Enemy control ownership/global state changed.");
        ArsPr4FixtureScope.RequireNoFlags(ordinary.uid);
        ArsPr4FixtureScope.RequireNoFlags(enemy.uid);
        ArsPr4FixtureScope.RequirePermanent(ctx.Get<Chara>("pr4.permanent"));
    }
}

public sealed partial class ArsPr4Handoff
{
    public static string PathFor(RuntimeTestContext ctx)
    {
        return System.IO.Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr4-save-handoff.json");
    }

    public static ArsPr4Handoff Capture(RuntimeTestContext ctx)
    {
        var scope = ctx.Get<ArsPr4FixtureScope>("pr4.scope");
        var data = new ArsPr4Handoff
        {
            ownershipVersion = ArsPr4HandoffCodec.CurrentVersion,
            token = scope.Token,
            pcName = EClass.pc.Name,
            masterUid = EClass.pc.uid,
            zoneUid = EClass._zone.uid,
            gameIdentity = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game),
            baseline = scope.Baseline,
            uids = new int[6], names = new string[6], durations = new int[6], ownerUids = new int[6]
        };
        var roles = new[] { "pr4.skeleton", "pr4.undead", "pr4.ordinary", "pr4.enemy", "pr4.enemyOwner", "pr4.permanent" };
        for (int i = 0; i < roles.Length; i++)
        {
            var card = ctx.Get<Chara>(roles[i]);
            data.uids[i] = card.uid;
            data.names[i] = card.c_altName;
            data.durations[i] = card.c_summonDuration;
            data.ownerUids[i] = card.c_uidMaster;
        }
        return data;
    }
}

public sealed class ArsPr4TemporarySavePrepareCase : RuntimeCaseBase, IRuntimeCoroutineCase
{
    public override string Id => "pr4.ars.temporary_save_prepare";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "destructive", "save_reload", "pr4" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        ArsPr4FixtureScope.Guard();
        RuntimeAssertions.Require(!System.IO.File.Exists(ArsPr4Handoff.PathFor(ctx)), "Archive previous PR4 handoff before prepare.");
        new ArsPr4TemporaryLifecycleCase().Prepare(ctx);
    }
    public override void Execute(RuntimeTestContext ctx) { }
    public override void Verify(RuntimeTestContext ctx)
    {
        ArsPr4FixtureScope.RequireTemporary(ctx.Get<Chara>("pr4.skeleton"), ctx);
        ArsPr4FixtureScope.RequireTemporary(ctx.Get<Chara>("pr4.undead"), ctx);
        ArsPr4Controls.Verify(ctx);
    }
    public override void Cleanup(RuntimeTestContext ctx) { }
    public System.Collections.IEnumerator PrepareAsync(RuntimeTestContext ctx) { Prepare(ctx); yield break; }
    public System.Collections.IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        var data = ArsPr4Handoff.Capture(ctx);
        var path = ArsPr4Handoff.PathFor(ctx);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        ArsPr4HandoffCodec.WriteNew(path, data);
        ctx.Log("handoff:ready:" + path + ":token=" + data.token);
        float deadline = UnityEngine.Time.realtimeSinceStartup + 120f;
        // External runtime owner saves to a dedicated slot, then acknowledges the exact token.
        while (UnityEngine.Time.realtimeSinceStartup < deadline)
        {
            if (System.IO.File.Exists(path + ".saved") && System.IO.File.ReadAllText(path + ".saved").Trim() == data.token)
                yield break;
            yield return null;
        }
        throw new System.InvalidOperationException("Dedicated-save acknowledgement timed out; rollback removes live fixtures.");
    }
    public System.Collections.IEnumerator VerifyAsync(RuntimeTestContext ctx) { Verify(ctx); yield break; }
    public System.Collections.IEnumerator CleanupAsync(RuntimeTestContext ctx) { Cleanup(ctx); yield break; }
}

public sealed class ArsPr4TemporarySaveVerifyCase : RuntimeCaseBase
{
    public override string Id => "pr4.ars.temporary_save_verify";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "destructive", "save_reload", "pr4" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        ArsPr4FixtureScope.Guard();
        string path = ArsPr4Handoff.PathFor(ctx);
        RuntimeAssertions.Require(System.IO.File.Exists(path), "Prepare handoff missing.");
        var data = ArsPr4HandoffCodec.Read(path);
        RuntimeAssertions.Require(System.IO.File.Exists(path + ".saved") && System.IO.File.ReadAllText(path + ".saved").Trim() == data.token,
            "Dedicated save acknowledgement absent/mismatched.");
        RuntimeAssertions.Require(data.pcName == EClass.pc.Name && data.masterUid == EClass.pc.uid && data.zoneUid == EClass._zone.uid,
            "Wrong dedicated PC/zone loaded.");
        RuntimeAssertions.Require(data.gameIdentity != System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game),
            "New Game not observed; reload the prepared dedicated slot before verify.");
        var scope = ArsPr4FixtureScope.Start(ctx, data.baseline);
        var roles = new[] { "pr4.skeleton", "pr4.undead", "pr4.ordinary", "pr4.enemy", "pr4.enemyOwner", "pr4.permanent" };
        for (int i = 0; i < roles.Length; i++)
        {
            var candidate = ArsPr4FixtureScope.Resolve(data.uids[i]);
            if (candidate != null && candidate.c_altName == data.names[i]
                && candidate.c_uidMaster == data.ownerUids[i]
                && data.names[i].StartsWith(ArsPr4FixtureScope.NamePrefix + data.token, System.StringComparison.Ordinal))
                scope.Own(candidate, roles[i], false);
        }
        // Never re-register or reconcile a fixture here. Normal manager queries must recover it.
        for (int i = 0; i < roles.Length; i++)
        {
            var card = ArsPr4FixtureScope.Resolve(data.uids[i]);
            RuntimeAssertions.Require(card != null && card.c_altName == data.names[i]
                && card.c_uidMaster == data.ownerUids[i]
                && data.names[i].StartsWith(ArsPr4FixtureScope.NamePrefix + data.token, System.StringComparison.Ordinal),
                "Saved fixture missing/name mismatch: " + data.uids[i]);
            scope.Own(card, roles[i], false);
            ctx.Set(roles[i], card);
            RuntimeAssertions.Require(card.c_summonDuration <= data.durations[i], "Reload increased summon lifetime.");
        }
        ctx.Log("handoff:reload:newGame:token=" + data.token);
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        ArsPr4FixtureScope.RequireTemporary(ctx.Get<Chara>("pr4.skeleton"), ctx);
        ArsPr4FixtureScope.RequireTemporary(ctx.Get<Chara>("pr4.undead"), ctx);
        ArsPr4Controls.Verify(ctx);
    }
    public override void Verify(RuntimeTestContext ctx) { Execute(ctx); }
    public override void Cleanup(RuntimeTestContext ctx) { }
}

public sealed class ArsPr4CarryoverCleanupCase : RuntimeCaseBase, IRuntimeCoroutineCase
{
    public override string Id => "pr4.ars.carryover_cleanup";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "destructive", "zone_transition", "pr4" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        var scope = ArsPr4FixtureScope.Start(ctx);
        ctx.Set("pr4.skeleton", scope.CastSummon(true));
        ctx.Set("pr4.undead", scope.CastSummon(false));
        ctx.Set("pr4.startZone", EClass._zone.uid);
        var harmony = new HarmonyLib.Harmony("runtime.pr4.ars.travel." + scope.Token);
        var uids = new System.Collections.Generic.HashSet<int> { ctx.Get<Chara>("pr4.skeleton").uid, ctx.Get<Chara>("pr4.undead").uid };
        ctx.RegisterRollback("pr4.travel_observer", () =>
        {
            harmony.UnpatchSelf();
            ArsPr4TravelObserver.Uids = null;
        });
        ArsPr4TravelObserver.Uids = uids;
        ArsPr4TravelObserver.Seen.Clear();
        harmony.Patch(HarmonyLib.AccessTools.Method(typeof(Zone), "AddGlobalCharasOnActivate"),
            prefix: new HarmonyLib.HarmonyMethod(typeof(ArsPr4TravelObserver), "BeforeActivate"));
        ctx.Log("travel:ready:fromZone=" + EClass._zone.uid + ":uids=" + string.Join(",", uids));
    }
    public override void Execute(RuntimeTestContext ctx) { }
    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass._zone.uid != ctx.Get<int>("pr4.startZone"), "Real zone transition not observed.");
        foreach (var role in new[] { "pr4.skeleton", "pr4.undead" })
        {
            var original = ctx.Get<Chara>(role);
            RuntimeAssertions.Require(ArsPr4TravelObserver.Seen.Contains(original.uid), "Native carryover did not contain fixture UID.");
            var card = ArsPr4FixtureScope.Resolve(original.uid);
            RuntimeAssertions.Require(card != null, "Fixture missing after real zone travel.");
            ArsPr4FixtureScope.RequireTemporary(card, ctx);
            card.c_summonDuration = 1;
            card.TickConditions();
            ArsPr4FixtureScope.RequireEnded(card, ctx);
        }
    }
    public override void Cleanup(RuntimeTestContext ctx) { }
    public System.Collections.IEnumerator PrepareAsync(RuntimeTestContext ctx) { Prepare(ctx); yield break; }
    public System.Collections.IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        float deadline = UnityEngine.Time.realtimeSinceStartup + 120f;
        while (UnityEngine.Time.realtimeSinceStartup < deadline)
        {
            if (EClass._zone.uid != ctx.Get<int>("pr4.startZone") && !EClass.game.isLoading
                && ArsPr4TravelObserver.Seen.Count == 2) yield break;
            yield return null;
        }
        throw new System.InvalidOperationException("Real zone travel/carryover timed out; runtime owner must travel while case waits.");
    }
    public System.Collections.IEnumerator VerifyAsync(RuntimeTestContext ctx) { Verify(ctx); yield break; }
    public System.Collections.IEnumerator CleanupAsync(RuntimeTestContext ctx) { Cleanup(ctx); yield break; }
}

public static class ArsPr4TravelObserver
{
    public static System.Collections.Generic.HashSet<int> Uids;
    public static readonly System.Collections.Generic.HashSet<int> Seen = new System.Collections.Generic.HashSet<int>();
    public static void BeforeActivate()
    {
        if (Uids == null) return;
        foreach (var card in EClass.player.listCarryoverMap)
            if (Uids.Contains(card.uid)) Seen.Add(card.uid);
    }
}
