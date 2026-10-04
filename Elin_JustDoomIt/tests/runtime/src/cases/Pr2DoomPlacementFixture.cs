#if PR2_RUNTIME_TEST
// Mod-local native runtime fixtures for PR2. All non-template types are qualified.
public sealed class Pr2DoomCardState
{
    public int Uid, Num, X, Z, Material, ParentUid;
    public string Id, Name, Trait, ParentType;
    public bool Npc, Stolen, Lost, Installed;

    public static Pr2DoomCardState Read(Thing t)
    {
        var pc = t.parent as Card;
        var zone = t.parent as Zone;
        return new Pr2DoomCardState {
            Uid = t.uid, Num = t.Num, X = t.pos.x, Z = t.pos.z, Material = t.idMaterial,
            Id = t.id, Name = t.c_altName, Trait = t.trait.GetType().FullName,
            Npc = t.isNPCProperty, Stolen = t.isStolen, Lost = t.isLostProperty, Installed = t.IsInstalled,
            ParentUid = pc != null ? pc.uid : (zone != null ? zone.uid : 0),
            ParentType = t.parent == null ? null : t.parent.GetType().FullName
        };
    }

    public void AssertSame(Thing t)
    {
        RuntimeAssertions.Require(t != null && !t.isDestroyed, "Card disappeared: " + Uid);
        var actual = Newtonsoft.Json.JsonConvert.SerializeObject(Read(t));
        RuntimeAssertions.Require(actual == Newtonsoft.Json.JsonConvert.SerializeObject(this),
            "Card changed: UID=" + Uid + "; actual=" + actual);
    }
}
public sealed class Pr2DoomSaveManifest
{
    public string SaveId, Token;
    public int GameIdentity, PcUid, ZoneUid, OriginalLv;
    public System.Collections.Generic.List<Pr2DoomCardState> Cabinets, Baseline;
    public System.Collections.Generic.List<int> Created;
}

public sealed class Pr2DoomPlacementFixture
{
    public const string ArcadeId = "justdoomit_arcade", Key = "pr2.fixture";
    public Zone Zone;
    public string Token;
    public int OriginalLv;
    public bool Retain, Restored;
    public readonly System.Collections.Generic.List<Thing> Created = new System.Collections.Generic.List<Thing>();
    public System.Collections.Generic.List<Pr2DoomCardState> Baseline;
    private bool ignoreAutoSave;
    private int pcX, pcZ;

    public static void Guard(RuntimeTestContext ctx, bool casino = true)
    {
        RuntimeAssertions.Require(EClass.game != null && EClass.pc != null && EClass._zone != null,
            "Load the dedicated RUNTIME_TEST save first.");
        RuntimeAssertions.Require((EClass.pc.Name ?? "").Contains("RUNTIME_TEST") && !EClass.pc.isDead,
            "RUNTIME_TEST live PC required.");
        RuntimeAssertions.Require(RuntimeV2Config.CaseIdFilter == ctx.CaseId,
            "Run one explicit PR2 CaseId; reload baseline between cases.");
        RuntimeAssertions.Require((EClass._zone.name ?? "").StartsWith("RUNTIME_TEST_PR2_", System.StringComparison.Ordinal),
            "Refusing the user's casino: enter a separate RUNTIME_TEST_PR2_* zone.");
        RuntimeAssertions.Require(EClass._zone.id == (casino ? "casino" : "field") && EClass._zone.instance == null && EClass._zone.IsLoaded,
            "Dedicated loaded non-instance casino (or field control) required.");
        RuntimeAssertions.Require(!global::Zone.forceRegenerate && global::Zone.forceSubset == null &&
            EClass._zone.idCurrentSubset == EClass._zone.IDSubset, "Fixture must not regenerate/change subsets.");
        RuntimeAssertions.Require(EClass.pc.party == null || EClass.pc.party.members.Count <= 1,
            "Use a fixture PC without companions.");
        RuntimeAssertions.Require(LayerDrama.Instance == null, "Close drama before placement tests.");
        RuntimeAssertions.Require(typeof(Elin_JustDoomIt.Patch_Zone_Activate_CasinoPlacement).GetMethod(
            "CreateValidatedArcadeThing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) != null,
            "PR2 product DLL is not loaded.");
    }

    public static Pr2DoomPlacementFixture Capture(RuntimeTestContext ctx, bool casino = true)
    {
        Guard(ctx, casino);
        var f = new Pr2DoomPlacementFixture {
            Zone = EClass._zone, Token = "RUNTIME_TEST_PR2_" + System.Guid.NewGuid().ToString("N"),
            OriginalLv = EClass._zone.lv, ignoreAutoSave = EClass.debug.ignoreAutoSave,
            pcX = EClass.pc.pos.x, pcZ = EClass.pc.pos.z
        };
        f.Baseline = f.Snapshot();
        ctx.Set(Key, f);
        ctx.RegisterRollback("pr2.fixture_restore_and_assert", () => f.Restore(ctx));
        EClass.debug.ignoreAutoSave = true;
        Pr2DoomPlacementObserver.Install(ctx, f);
        ctx.Log("fixture=" + f.Token + "; zone_uid=" + f.Zone.uid + "; save=" + Game.id +
            "; product=" + typeof(Elin_JustDoomIt.Patch_Zone_Activate_CasinoPlacement).Assembly.FullName);
        return f;
    }

    public System.Collections.Generic.List<Pr2DoomCardState> Snapshot()
    {
        var result = new System.Collections.Generic.List<Pr2DoomCardState>();
        foreach (var t in Zone.map.things) if (t != null && !t.isDestroyed) result.Add(Pr2DoomCardState.Read(t));
        foreach (var t in EClass.pc.things) if (t != null && !t.isDestroyed) result.Add(Pr2DoomCardState.Read(t));
        return result;
    }

    public Thing Find(int uid)
    {
        foreach (var t in Zone.map.things) if (t.uid == uid) return t;
        foreach (var t in EClass.pc.things) if (t.uid == uid) return t;
        foreach (var t in Created) if (t != null && t.uid == uid && !t.isDestroyed) return t;
        return null;
    }

    public void Track(Thing t) { if (t != null && !Created.Contains(t)) Created.Add(t); }

    public System.Collections.Generic.List<Thing> Cabinets()
    {
        var result = new System.Collections.Generic.List<Thing>();
        foreach (var t in Zone.map.things)
            if (t != null && !t.isDestroyed && t.id == ArcadeId && t.ExistsOnMap) result.Add(t);
        return result;
    }

    public void RequireEmpty()
    {
        RuntimeAssertions.Require(Cabinets().Count == 0, "Prepare a cabinet-free test zone; never remove existing cabinets.");
    }

    public Point FreePoint()
    {
        for (int x = 2; x < Zone.map.Size - 2; x++)
            for (int z = 2; z < Zone.map.Size - 2; z++)
            {
                var p = new Point(x, z);
                if (Valid(p)) return p;
            }
        throw new System.InvalidOperationException("Fixture has no free native tile.");
    }

    public static bool Valid(Point p)
    {
        return p != null && p.IsValid && !p.IsBlocked && !p.HasChara && !p.HasThing;
    }

    public Thing AddCabinet(bool npc, bool stolen, bool lost, bool held = false)
    {
        var t = ThingGen.Create(ArcadeId);
        Track(t);
        RuntimeAssertions.Require(t != null && t.id == ArcadeId && t.trait is Elin_JustDoomIt.TraitJustDoomArcade,
            "Real CWL arcade source/trait required, not fallback.");
        t.c_altName = Token + "_" + Created.Count;
        t.SetNum(held ? 2 : 1);
        if (held) EClass.pc.AddThing(t);
        else { Zone.AddCard(t, FreePoint()); t.Install(); }
        t.isNPCProperty = npc; t.isStolen = stolen; t.isLostProperty = lost;
        return t;
    }

    public void Activate(RuntimeTestContext ctx, int lv)
    {
        Zone.lv = lv;
        RuntimeAssertions.Require(Zone.idCurrentSubset == Zone.IDSubset, "Floor would change native subset; abort fixture.");
        var map = Zone.map;
        Pr2DoomPlacementObserver.Begin();
        try { Zone.Activate(); }
        finally { Pr2DoomPlacementObserver.End(); }
        RuntimeAssertions.Require(EClass._zone == Zone && Zone.map == map && Zone.isStarted,
            "Native Zone.Activate did not finish on the same disposable map.");
        RuntimeAssertions.Require(Pr2DoomPlacementObserver.ActivateCalls == 1, "Expected one real Zone.Activate.");
        ctx.Log("native_activate:zone=" + Zone.uid + "; lv=" + lv + "; calls=" +
            Pr2DoomPlacementObserver.ActivateCalls + "; ThingGen=" + Pr2DoomPlacementObserver.CreateCalls +
            "; ThingGenAttempts=" + Pr2DoomPlacementObserver.CreateAttempts +
            "; AddCard=" + Pr2DoomPlacementObserver.AddCalls + "; cabinets=" + Cabinets().Count);
    }

    public void AssertBaseline() { foreach (var s in Baseline) s.AssertSame(Find(s.Uid)); }

    public void Restore(RuntimeTestContext ctx)
    {
        if (Restored) return;
        if (Retain) { EClass.debug.ignoreAutoSave = ignoreAutoSave; ctx.Log("handoff:retained; external save/reload then verify required"); return; }
        var errors = new System.Collections.Generic.List<string>();
        try
        {
            foreach (var t in Created)
            {
                try { if (t != null && !t.isDestroyed) t.Destroy(); }
                catch (System.Exception ex) { errors.Add("destroy_uid=" + t.uid + ":" + ex.Message); }
            }
            Zone.lv = OriginalLv;
            RuntimeAssertions.Require(Zone.lv == OriginalLv, "Fixture floor not restored.");
            try
            {
                AssertBaseline();
                RuntimeAssertions.Require(Snapshot().Count == Baseline.Count, "Unexpected nonfixture card remains.");
                RuntimeAssertions.Require(EClass._zone == Zone && EClass.pc.pos.x == pcX && EClass.pc.pos.z == pcZ,
                    "PC/active-zone changed; reload baseline.");
                foreach (var t in Created) RuntimeAssertions.Require(t == null || t.isDestroyed, "Created UID still alive: " + t.uid);
            }
            catch (System.Exception ex) { errors.Add(ex.Message); }
        }
        finally { EClass.debug.ignoreAutoSave = ignoreAutoSave; }
        RuntimeAssertions.Require(EClass.debug.ignoreAutoSave == ignoreAutoSave, "Autosave setting not restored.");
        RuntimeAssertions.Require(errors.Count == 0, "STOP/reload disposable baseline; cleanup: " + string.Join(";", errors));
        Restored = true;
        ctx.Log("cleanup:fixture UIDs removed; originals/count/PC/floor/autosave unchanged; baseline reload still required");
    }

    public static string ManifestPath(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(!string.IsNullOrEmpty(Game.id) && Game.id.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0,
            "Invalid save identity for manifest.");
        return System.IO.Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr2-save-" + Game.id + ".json");
    }
}
#endif
