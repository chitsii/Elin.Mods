#if PR2_RUNTIME_TEST
public abstract class Pr2DoomPlacementCase : RuntimeCaseBase
{
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr2", "integration", "destructive" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(!System.IO.File.Exists(Pr2DoomPlacementFixture.ManifestPath(ctx)),
            "A save/reload fixture is pending: run save_verify or save_abort first.");
        Pr2DoomPlacementFixture.Capture(ctx);
    }
    public override void Verify(RuntimeTestContext ctx) { ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key).AssertBaseline(); }
    public override void Cleanup(RuntimeTestContext ctx)
    {
        var f = ctx.GetOrDefault<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        if (f != null) f.Restore(ctx);
    }
    public static void RequireNoGeneration(Pr2DoomPlacementFixture f)
    {
        RuntimeAssertions.Require(Pr2DoomPlacementObserver.CreateAttempts == 0 && Pr2DoomPlacementObserver.CreateCalls == 0 && Pr2DoomPlacementObserver.AddCalls == 0,
            "Skipped placement still called real ThingGen/AddCard.");
    }
}

public sealed class Pr2DoomCabinetPreservationCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.cabinet_preservation";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.AddCabinet(false, false, false);
        f.AddCabinet(true, true, false);
        f.AddCabinet(false, false, true);
        var held = f.AddCabinet(false, true, true, true);
        var states = f.Snapshot();
        int count = f.Cabinets().Count;
        RuntimeAssertions.Require(count >= 3 && held.Num == 2 && held.GetRootCard() == EClass.pc,
            "Installed/held ownership fixtures are incomplete.");
        foreach (int lv in new[] { -2, -1, 0, 1, 2, 7 })
            for (int pass = 0; pass < 2; pass++)
            {
                f.Activate(ctx, lv);
                RequireNoGeneration(f);
                RuntimeAssertions.Require(f.Cabinets().Count == count, "Cabinet count changed on floor " + lv);
                foreach (var s in states) s.AssertSame(f.Find(s.Uid));
            }
        // Native relocation must also keep UID, quantity and ownership on the next activation.
        f.Zone.AddCard(held, f.FreePoint());
        held.Install();
        var relocated = Pr2DoomCardState.Read(held);
        f.Activate(ctx, 1);
        RequireNoGeneration(f);
        relocated.AssertSame(f.Find(held.uid));
        RuntimeAssertions.Require(f.Cabinets().Count == count + 1, "Relocated cabinet duplicated or vanished.");
        ctx.Log("assert:existing UID/pos/Num/name/material/trait/property/stolen/lost/parent/installed preserved; floors=-2,-1,0,1,2,7; held+relocated UID=" + held.uid);
    }
}

public sealed class Pr2DoomNewCabinetCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.new_cabinet_idempotent";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.RequireEmpty();
        foreach (int lv in new[] { -1, 0, 2, 7 }) { f.Activate(ctx, lv); RequireNoGeneration(f); f.RequireEmpty(); }
        f.Activate(ctx, 1);
        RuntimeAssertions.Require(Pr2DoomPlacementObserver.CreateCalls == 1 && Pr2DoomPlacementObserver.AddCalls == 1 &&
            Pr2DoomPlacementObserver.AddedOnValidTile && f.Cabinets().Count == 1, "Expected one real generation/AddCard on a valid tile.");
        var t = f.Cabinets()[0];
        RuntimeAssertions.Require(t.id == Pr2DoomPlacementFixture.ArcadeId && t.trait is Elin_JustDoomIt.TraitJustDoomArcade &&
            t.isNPCProperty && !t.isStolen && !t.isLostProperty && t.IsInstalled && t.ExistsOnMap,
            "New native cabinet ID/trait/NPC ownership/install mismatch.");
        var state = Pr2DoomCardState.Read(t);
        f.Activate(ctx, 1);
        RequireNoGeneration(f);
        RuntimeAssertions.Require(f.Cabinets().Count == 1, "Second Activate duplicated a cabinet.");
        state.AssertSame(f.Find(state.Uid));
        ctx.Log("assert:new UID=" + state.Uid + "; exactly_one=true; new_only_NPC=true; duplicate=false");
    }
}

public sealed class Pr2DoomNonCasinoCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.noncasino_control";
    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(!System.IO.File.Exists(Pr2DoomPlacementFixture.ManifestPath(ctx)), "Finish pending save/reload fixture first.");
        Pr2DoomPlacementFixture.Capture(ctx, false);
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.RequireEmpty();
        f.Activate(ctx, 1); RequireNoGeneration(f); f.RequireEmpty();
        var existing = f.AddCabinet(false, true, true);
        var before = Pr2DoomCardState.Read(existing);
        f.Activate(ctx, 1); RequireNoGeneration(f); before.AssertSame(f.Find(existing.uid));
        RuntimeAssertions.Require(f.Cabinets().Count == 1, "Non-casino control duplicated/removed cabinet.");
        ctx.Log("assert:real field zone generated zero automatic cabinets and preserved existing UID=" + existing.uid);
    }
}

public sealed class Pr2DoomMissingSourceCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.new_missing_source";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr2", "integration", "destructive", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.RequireEmpty();
        string id = Pr2DoomPlacementFixture.ArcadeId;
        var cards = EClass.sources.cards.map;
        var things = EClass.sources.things.map;
        RuntimeAssertions.Require(cards.ContainsKey(id) && things.ContainsKey(id), "Both real source registries must initially contain the arcade.");
        var card = cards[id]; var thing = things[id];
        int cardsCount = cards.Count, thingsCount = things.Count;
        System.Action restore = () => {
            cards[id] = card; things[id] = thing;
            RuntimeAssertions.Require(object.ReferenceEquals(cards, EClass.sources.cards.map) && object.ReferenceEquals(things, EClass.sources.things.map) &&
                cards.Count == cardsCount && things.Count == thingsCount, "Source registries/counts changed.");
            RuntimeAssertions.Require(object.ReferenceEquals(cards[id], card) && object.ReferenceEquals(things[id], thing), "Source identity restoration failed.");
        };
        ctx.RegisterRollback("pr2.exact_source_keys", restore);
        try
        {
            cards.Remove(id);
            f.Activate(ctx, 1); RequireNoGeneration(f); f.RequireEmpty();
            restore();
            things.Remove(id);
            f.Activate(ctx, 1); RequireNoGeneration(f); f.RequireEmpty();
        }
        finally { restore(); }
        ctx.Log("assert:missing cards/things independently => ThingGen=0/AddCard=0; exact original rows restored");
    }
}

public sealed class Pr2DoomOccupiedTileCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.occupied_tile_native";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.RequireEmpty();
        var p = new Point(49, 66);
        RuntimeAssertions.Require(f.Zone.map.bounds.Contains(p) && Pr2DoomPlacementFixture.Valid(p), "Use a fixture with a free (49,66) tile.");
        var blocker = ThingGen.Create("money"); f.Track(blocker);
        RuntimeAssertions.Require(blocker != null && blocker.id == "money", "Native blocker creation failed.");
        blocker.c_altName = f.Token;
        f.Zone.AddCard(blocker, p);
        var before = Pr2DoomCardState.Read(blocker);
        RuntimeAssertions.Require(p.HasThing, "Native tile occupancy not established.");
        f.Activate(ctx, 1);
        RuntimeAssertions.Require(Pr2DoomPlacementObserver.CreateCalls == 1 && Pr2DoomPlacementObserver.AddCalls == 1 &&
            Pr2DoomPlacementObserver.AddedOnValidTile && f.Cabinets().Count == 1, "Occupied tile did not use a valid native alternative.");
        var t = f.Cabinets()[0];
        RuntimeAssertions.Require(t.pos.x != p.x || t.pos.z != p.z, "Cabinet overlaps the blocker.");
        before.AssertSame(f.Find(blocker.uid));
        ctx.Log("assert:native occupied (49,66) avoided; actual new position=" + t.pos.x + "," + t.pos.z);
    }
}

public sealed class Pr2DoomGenerationFaultCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.generation_faults";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr2", "integration", "destructive", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.RequireEmpty();
        foreach (string mode in new[] { "wrong_id", "wrong_trait", "fallback", "exception" })
        {
            var restore = Pr2DoomPlacementFaults.Install(ctx, mode);
            try
            {
                f.Activate(ctx, 1);
                RuntimeAssertions.Require(Pr2DoomPlacementFaults.Injections == 1 && Pr2DoomPlacementObserver.CreateAttempts == 1 &&
                    Pr2DoomPlacementObserver.CreateCalls == (mode == "exception" ? 0 : 1),
                    "Fault did not reach the real native factory: " + mode);
                RuntimeAssertions.Require(Pr2DoomPlacementObserver.AddCalls == 0, "Rejected generated card was added to map: " + mode);
                f.RequireEmpty();
                string expected = mode == "wrong_id" ? "money" : (mode == "fallback" ? "869" : Pr2DoomPlacementFixture.ArcadeId);
                if (mode != "exception") RuntimeAssertions.Require(Pr2DoomPlacementObserver.GeneratedId == expected, "Native generated ID did not match injected branch.");
                if (mode == "wrong_trait") RuntimeAssertions.Require(Pr2DoomPlacementObserver.GeneratedTrait == typeof(Trait).FullName, "Wrong-trait injection not observed.");
                ctx.Log("fault=" + mode + "; native_id=" + Pr2DoomPlacementObserver.GeneratedId + "; trait=" + Pr2DoomPlacementObserver.GeneratedTrait + "; AddCard=0");
            }
            finally { restore(); }
        }
        f.Activate(ctx, 1);
        RuntimeAssertions.Require(Pr2DoomPlacementFaults.Mode == null && Pr2DoomPlacementObserver.CreateCalls == 1 &&
            Pr2DoomPlacementObserver.AddCalls == 1 && Pr2DoomPlacementObserver.AddedOnValidTile && f.Cabinets().Count == 1,
            "Real placement did not recover after exception/fault cleanup.");
        ctx.Log("assert:exception was fail-soft; next normal native placement succeeded after fault owners removed");
    }
}

public sealed class Pr2DoomInvalidTileFaultCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.invalid_tile_fault";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr2", "integration", "destructive", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.RequireEmpty();
        var p = new Point(49, 66);
        RuntimeAssertions.Require(f.Zone.map.bounds.Contains(p) && Pr2DoomPlacementFixture.Valid(p), "Free fixed coordinate required.");
        var blocker = ThingGen.Create("money"); f.Track(blocker);
        blocker.c_altName = f.Token;
        f.Zone.AddCard(blocker, p);
        var state = Pr2DoomCardState.Read(blocker);
        var restore = Pr2DoomPlacementFaults.Install(ctx, "invalid_tile");
        try
        {
            f.Activate(ctx, 1);
            RuntimeAssertions.Require(Pr2DoomPlacementFaults.Injections == 1, "Native nearest-point call not reached.");
            RequireNoGeneration(f); f.RequireEmpty(); state.AssertSame(f.Find(blocker.uid));
            ctx.Log("assert:invalid native nearest-result rejected => ThingGen=0/AddCard=0; coverage=fault_injection");
        }
        finally { restore(); }
    }
}

public sealed class Pr2DoomSavePrepareCase : Pr2DoomPlacementCase
{
    public override string Id => "pr2.doom.existing_save_prepare";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr2", "save_reload", "prepare_only", "destructive" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        f.RequireEmpty();
        f.AddCabinet(false, false, false); f.AddCabinet(true, true, false); f.AddCabinet(false, false, true);
        f.AddCabinet(false, true, true, true);
        f.Activate(ctx, 0); RequireNoGeneration(f);
        var manifest = new Pr2DoomSaveManifest {
            SaveId = Game.id, Token = f.Token, GameIdentity = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game),
            PcUid = EClass.pc.uid, ZoneUid = f.Zone.uid, OriginalLv = f.OriginalLv,
            Baseline = f.Baseline, Cabinets = f.Snapshot(), Created = new System.Collections.Generic.List<int>()
        };
        foreach (var t in f.Created) manifest.Created.Add(t.uid);
        var path = Pr2DoomPlacementFixture.ManifestPath(ctx);
        var temp = path + "." + f.Token + ".tmp";
        ctx.RegisterRollback("pr2.partial_handoff_manifest", () => {
            if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
            if (!f.Retain && System.IO.File.Exists(path)) {
                var saved = Newtonsoft.Json.JsonConvert.DeserializeObject<Pr2DoomSaveManifest>(System.IO.File.ReadAllText(path));
                RuntimeAssertions.Require(saved.Token == f.Token, "Refusing to remove another handoff manifest.");
                System.IO.File.Delete(path);
            }
        });
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        RuntimeAssertions.Require(!System.IO.File.Exists(path), "Handoff manifest already exists.");
        System.IO.File.WriteAllText(temp, Newtonsoft.Json.JsonConvert.SerializeObject(manifest, Newtonsoft.Json.Formatting.Indented));
        System.IO.File.Move(temp, path);
        ctx.Set("pr2.manifest", path);
        ctx.Log("handoff_manifest=" + path + "; externally save/reload this dedicated save; verify will reacquire UIDs");
    }
    public override void Verify(RuntimeTestContext ctx)
    {
        base.Verify(ctx);
        RuntimeAssertions.Require(System.IO.File.Exists(ctx.Get<string>("pr2.manifest")), "Manifest handoff missing.");
        ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key).Retain = true;
        ctx.Log("coverage=prepare_only; save/reload integration not yet passed");
    }
}

public abstract class Pr2DoomSaveFollowupCase : Pr2DoomPlacementCase
{
    protected abstract bool Abort { get; }
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr2", "save_reload", "destructive" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        Pr2DoomPlacementFixture.Guard(ctx);
        var path = Pr2DoomPlacementFixture.ManifestPath(ctx);
        RuntimeAssertions.Require(System.IO.File.Exists(path), "Run existing_save_prepare then externally save/reload first.");
        var m = Newtonsoft.Json.JsonConvert.DeserializeObject<Pr2DoomSaveManifest>(System.IO.File.ReadAllText(path));
        RuntimeAssertions.Require(m != null && m.Token.StartsWith("RUNTIME_TEST_PR2_", System.StringComparison.Ordinal) &&
            m.SaveId == Game.id && m.PcUid == EClass.pc.uid && m.ZoneUid == EClass._zone.uid, "Wrong save/zone/PC manifest.");
        var f = Pr2DoomPlacementFixture.Capture(ctx);
        f.Token = m.Token; f.OriginalLv = m.OriginalLv; f.Baseline = m.Baseline;
        foreach (var uid in m.Created) {
            var t = f.Find(uid);
            if (t != null && (t.c_altName ?? "").StartsWith(m.Token, System.StringComparison.Ordinal)) f.Track(t);
        }
        ctx.Set("pr2.saved", m); ctx.Set("pr2.manifest", path);
        RuntimeAssertions.Require(f.Created.Count == m.Created.Count, "Saved fixture UIDs missing/not owned; reload baseline.");
        if (!Abort) RuntimeAssertions.Require(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game) != m.GameIdentity,
            "No actual reload: Game object is unchanged.");
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        if (Abort) { ctx.Log("coverage=cleanup_only; no integration claim"); return; }
        var f = ctx.Get<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        var m = ctx.Get<Pr2DoomSaveManifest>("pr2.saved");
        foreach (var s in m.Cabinets) s.AssertSame(f.Find(s.Uid));
        foreach (int lv in new[] { 0, 1, 2 }) {
            f.Activate(ctx, lv); RequireNoGeneration(f);
            foreach (var s in m.Cabinets) s.AssertSame(f.Find(s.Uid));
        }
        RuntimeAssertions.Require(f.Snapshot().Count == m.Cabinets.Count, "Reload/Activate added or removed cards.");
        ctx.Log("assert:actual save/reload retained every cabinet UID/quantity/ownership/position/held-parent; no repair registration performed");
    }
    public override void Cleanup(RuntimeTestContext ctx)
    {
        base.Cleanup(ctx);
        var f = ctx.GetOrDefault<Pr2DoomPlacementFixture>(Pr2DoomPlacementFixture.Key);
        if (f == null || !f.Restored) return;
        var path = ctx.Get<string>("pr2.manifest");
        var saved = Newtonsoft.Json.JsonConvert.DeserializeObject<Pr2DoomSaveManifest>(System.IO.File.ReadAllText(path));
        RuntimeAssertions.Require(saved.Token == f.Token, "Refusing to delete a different manifest.");
        System.IO.File.Delete(path);
    }
}
public sealed class Pr2DoomSaveVerifyCase : Pr2DoomSaveFollowupCase
{
    public override string Id => "pr2.doom.existing_save_verify";
    protected override bool Abort => false;
}
public sealed class Pr2DoomSaveAbortCase : Pr2DoomSaveFollowupCase
{
    public override string Id => "pr2.doom.existing_save_abort";
    protected override bool Abort => true;
}
#endif
